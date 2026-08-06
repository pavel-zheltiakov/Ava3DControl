using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Ava3D.Demo.Views;

/// <summary>
/// The caption band: a ship's console panel rather than a subtitle.
///
/// It was a rounded grey box with twelve-point body text in it, which is what a tooltip looks like. A
/// film that has spent sixty seconds building a place has earned a caption that belongs to that place,
/// and the things that make one belong are cheap: an angular frame with brackets at its corners, a face
/// that was drawn rather than chosen, and the small dishonesties of a screen — scanlines, a sweep, and
/// a signal that tears now and then. None of it is a bitmap and none of it is a font file; it is about
/// two hundred lines of geometry, so it is the same on desktop, in a browser, and on a phone.
///
/// Everything here is a function of one clock reading, handed in from the same frame clock that drives
/// the scene. That is deliberate and it is the same discipline the film itself runs under: no timers of
/// its own, no wall clock sampled inside a callback, nothing that can drift against what is being drawn.
/// A caption that flickers on its own schedule while the picture moves on another is a caption you
/// notice.
/// </summary>
internal sealed class Codec : Control
{
    /// <summary>Cap height of the face, in device-independent pixels, when there is room for it.</summary>
    private const double CapHeight = 26;

    private const double PadX = 30;
    private const double PadY = 17;

    /// <summary>How long the panel takes to arrive, and how long it takes to go.</summary>
    private const double FadeIn = 0.28, FadeOut = 0.45;

    /// <summary>
    /// How long the text takes to scan in behind its caret.
    ///
    /// Half a second rather than a third of one. The caret is the only other thing on this panel that
    /// moves sideways, and a scene that changes its caption every four seconds — as Motherboard does,
    /// once per merge — sweeps it across the words often enough for the speed to matter.
    /// </summary>
    private const double Reveal = 0.55;

    /// <summary>
    /// The signal fault: how many chances a second it gets, how likely each one is, how far it moves
    /// the words, and how much of its slot it takes to swell and settle again.
    ///
    /// These four numbers were tuned against a film whose captions are on screen for four seconds and
    /// are read at a glance, and they were wrong for everything else: a tear a second, snapping seven
    /// pixels sideways, is a caption you cannot read at all when the caption is a component's
    /// designator and its dimensions and it is up for fourteen seconds while you look at the part.
    /// Now it is a fault about every six seconds, half as far, and it arrives and leaves on a sine
    /// rather than on a step — which keeps the effect on a still frame and takes it off a moving one.
    /// </summary>
    private const double TearRate = 1.6, TearChance = 0.11, TearWidth = 6, TearHold = 0.34;

    private string? _wanted;
    private string _shown = string.Empty;

    private double _clock;
    private double _arrived = double.NegativeInfinity;
    private double _left = double.NegativeInfinity;

    private string[] _lines = [];
    private Geometry[] _drawn = [];
    private double[] _widths = [];
    private double _widest;

    private string _builtFor = string.Empty;
    private double _builtAt;

    public Codec()
    {
        IsHitTestVisible = false;
    }

    /// <summary>
    /// Advances the panel to <paramref name="seconds"/> and tells it what the scene wants shown.
    ///
    /// Called every frame whether there is a caption or not, and cheap when there is not: a panel that
    /// is neither up nor going down asks for no redraw at all, which is what keeps this off the bill for
    /// the twenty-three scenes that never show one.
    /// </summary>
    public void Tick(string? caption, double seconds)
    {
        _clock = seconds;

        if (caption != _wanted)
        {
            _wanted = caption;

            if (caption != null)
            {
                _shown = caption;
                _arrived = seconds;
            }
            else
            {
                _left = seconds;
            }
        }

        // Idle: nothing wanted, and whatever was last shown has finished leaving.
        if (_wanted == null && seconds - _left > FadeOut)
            return;

        InvalidateVisual();
    }

    /// <summary>Clears the panel outright, for a scene change. No fade — the picture is cutting anyway.</summary>
    public void Reset()
    {
        _wanted = null;
        _shown = string.Empty;
        _arrived = double.NegativeInfinity;
        _left = double.NegativeInfinity;

        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        var fade = _wanted != null
            ? Ease(Math.Clamp((_clock - _arrived) / FadeIn, 0, 1))
            : 1 - Ease(Math.Clamp((_clock - _left) / FadeOut, 0, 1));

        if (fade <= 0.004 || _shown.Length == 0)
            return;

        // Wide captions break, then shrink. A phone in portrait is 390 points across and one of these
        // lines is thirty-nine characters: shrinking alone would put it on screen at a seven-pixel cap
        // height, which is on screen in the sense that a barcode is. Two lines first, then whatever
        // shrink is still needed after that.
        var room = Math.Max(120, Bounds.Width - 40 - PadX * 2);
        var lines = Glyphs.Measure(_shown, CapHeight) <= room ? [_shown] : Break(_shown);
        var widest = 1.0;

        foreach (var line in lines)
            widest = Math.Max(widest, Glyphs.Measure(line, CapHeight));

        var size = Math.Min(CapHeight, CapHeight * room / widest);

        Text(lines, size);

        var height = PadY * 2 + _lines.Length * size + (_lines.Length - 1) * size * 0.62;

        var panel = new Rect(
            (Bounds.Width - (_widest + PadX * 2)) * 0.5,
            Bounds.Height - height - 10,
            _widest + PadX * 2,
            height);

        // The panel rises the last few pixels into place as it arrives, which is the whole difference
        // between a thing that appeared and a thing that was switched on.
        var lift = (1 - fade) * 12;

        // The signal. A tear every so often, chosen from the clock rather than from a random source so
        // that two runs of the film glitch on the same frames — which matters, because the same film is
        // used to compare three renderers image by image.
        //
        // It swells and settles over the first third of its slot instead of switching on and off. A
        // step is what a real display does and it is also a jump in the middle of a word: the eye is
        // tracking the line, and there is nothing to track through a discontinuity. Half a second of
        // sine covers the same ground and can be read straight through.
        var slot = Math.Floor(_clock * TearRate);
        var into = _clock * TearRate - slot;
        var torn = Noise(slot) < TearChance && into < TearHold;
        var tear = torn
            ? (Noise(slot + 91) - 0.5) * TearWidth * Math.Sin(into / TearHold * Math.PI)
            : 0;

        // And a flicker in the backlight, always running, so the panel is never quite still. The dip
        // that goes with a tear rides on the tear's own strength rather than on the fact of it, so the
        // panel dims into the fault and comes back out of it instead of blinking.
        var glitch = Math.Abs(tear) / (TearWidth / 2);
        var lamp = fade * (0.93 + 0.07 * Math.Sin(_clock * 31) - 0.16 * glitch);

        using var _ = context.PushTransform(Matrix.CreateTranslation(0, lift));

        Frame(context, panel, lamp);
        Scanlines(context, panel, lamp);
        Sweep(context, panel, lamp);
        Caption(context, panel, size, lamp, tear);
    }

    /// <summary>The plate itself: a cut-cornered body, its border, and the brackets outside it.</summary>
    private void Frame(DrawingContext context, Rect panel, double lamp)
    {
        var cut = Math.Min(panel.Height * 0.34, 22);
        var body = new StreamGeometry();

        using (var pen = body.Open())
        {
            pen.BeginFigure(new Point(panel.Left + cut, panel.Top), isFilled: true);
            pen.LineTo(new Point(panel.Right - cut, panel.Top));
            pen.LineTo(new Point(panel.Right, panel.Top + cut));
            pen.LineTo(new Point(panel.Right, panel.Bottom - cut));
            pen.LineTo(new Point(panel.Right - cut, panel.Bottom));
            pen.LineTo(new Point(panel.Left + cut, panel.Bottom));
            pen.LineTo(new Point(panel.Left, panel.Bottom - cut));
            pen.LineTo(new Point(panel.Left, panel.Top + cut));
            pen.EndFigure(isClosed: true);
        }

        var fill = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Shade(0x3A, 0x2C6BA8, lamp), 0),
                new GradientStop(Shade(0x86, 0x0B1B34, lamp), 0.55),
                new GradientStop(Shade(0xB4, 0x06101F, lamp), 1)
            }
        };

        context.DrawGeometry(fill, new Pen(new SolidColorBrush(Shade(0xD8, 0x8FE3FF, lamp)), 1.4), body);

        // A second line inside the first. Two lines a couple of pixels apart is the cheapest thing that
        // says "instrument" rather than "box", and it costs one more geometry.
        var inner = panel.Deflate(new Thickness(5, 4));
        var lip = new StreamGeometry();

        using (var pen = lip.Open())
        {
            var c = Math.Min(inner.Height * 0.34, 18);
            pen.BeginFigure(new Point(inner.Left + c, inner.Top), isFilled: false);
            pen.LineTo(new Point(inner.Right - c, inner.Top));
            pen.LineTo(new Point(inner.Right, inner.Top + c));
            pen.LineTo(new Point(inner.Right, inner.Bottom - c));
            pen.LineTo(new Point(inner.Right - c, inner.Bottom));
            pen.LineTo(new Point(inner.Left + c, inner.Bottom));
            pen.LineTo(new Point(inner.Left, inner.Bottom - c));
            pen.LineTo(new Point(inner.Left, inner.Top + c));
            pen.EndFigure(isClosed: true);
        }

        context.DrawGeometry(null, new Pen(new SolidColorBrush(Shade(0x50, 0x6FC8F0, lamp)), 1), lip);

        // Corner brackets, standing off the plate. They are the detail that reads at a glance as a
        // targeting frame, and they are four L-shapes.
        var mark = new Pen(new SolidColorBrush(Shade(0xE6, 0xBEF2FF, lamp)), 2);
        var arm = Math.Min(panel.Height * 0.42, 18);

        foreach (var (x, sx) in new[] { (panel.Left - 7, 1.0), (panel.Right + 7, -1.0) })
        foreach (var (y, sy) in new[] { (panel.Top - 6, 1.0), (panel.Bottom + 6, -1.0) })
        {
            context.DrawLine(mark, new Point(x, y), new Point(x + arm * sx, y));
            context.DrawLine(mark, new Point(x, y), new Point(x, y + arm * sy));
        }
    }

    /// <summary>
    /// The scanlines: dark, one pixel, every third one, and scrolling.
    ///
    /// Scrolling matters more than the lines do. Static ones read as a texture over the panel; ones that
    /// crawl read as a raster being refreshed, which is the thing actually being suggested.
    /// </summary>
    private void Scanlines(DrawingContext context, Rect panel, double lamp)
    {
        using var _ = context.PushClip(panel);

        var line = new Pen(new SolidColorBrush(Shade(0x38, 0x000814, lamp)), 1);
        var drift = _clock * 9 % 3;

        for (var y = panel.Top - 3 + drift; y < panel.Bottom; y += 3)
            context.DrawLine(line, new Point(panel.Left, y), new Point(panel.Right, y));
    }

    /// <summary>A bright band crossing the plate every couple of seconds — the refresh going past.</summary>
    private void Sweep(DrawingContext context, Rect panel, double lamp)
    {
        var phase = _clock / 2.4 % 1;
        var band = new Rect(panel.X, panel.Top - 24 + phase * (panel.Height + 48), panel.Width, 24);

        using var _ = context.PushClip(panel);

        context.DrawRectangle(new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Shade(0x00, 0x9BE7FF, lamp), 0),
                new GradientStop(Shade(0x26, 0x9BE7FF, lamp), 0.5),
                new GradientStop(Shade(0x00, 0x9BE7FF, lamp), 1)
            }
        }, null, band);
    }

    /// <summary>
    /// The words: a glow pass, a colour fringe, the strokes themselves, and the caret that draws them.
    ///
    /// The fringe is the part that sells the tear. A display that has lost sync does not simply shift
    /// its picture, it separates the channels doing it, so the red goes one way and the blue the other —
    /// two extra passes of the same geometry at a couple of pixels' offset, and only while the tear is
    /// running.
    /// </summary>
    private void Caption(DrawingContext context, Rect panel, double size, double lamp, double tear)
    {
        // The scan runs through the whole caption rather than through each line, so a two-line caption
        // finishes its first line before it starts its second, the way a terminal would.
        var total = 0.0;
        foreach (var width in _widths)
            total += width;

        var scanned = total * (_wanted == null ? 1 : Math.Clamp((_clock - _arrived) / Reveal, 0, 1));

        var glow = new Pen(new SolidColorBrush(Shade(0x3C, 0x54C8F5, lamp)), 6.5,
            lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
        var stroke = new Pen(new SolidColorBrush(Shade(0xFF, 0xE9FBFF, lamp)), 2.4,
            lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
        var caret = new SolidColorBrush(Shade(0xE0, 0xBFF4FF, lamp));
        var fringe = Math.Abs(tear) * 0.9;

        for (var i = 0; i < _drawn.Length; i++)
        {
            var shown = Math.Clamp(scanned, 0, _widths[i]);
            scanned -= _widths[i];

            if (shown <= 0)
                break;

            // Lines are centred on each other, not ragged: a two-line caption in a panel cut to the
            // wider of them looks wrong hung on the left.
            var left = panel.Left + (panel.Width - _widths[i]) * 0.5;
            var top = panel.Top + PadY + i * size * 1.62;

            using var clip = context.PushClip(
                new Rect(left - 4, top - size, shown + 8, size * 2.4));
            using var place = context.PushTransform(Matrix.CreateTranslation(left + tear, top));

            // Wide and dim under narrow and bright: a glow without a blur, which is not available here
            // and would cost a surface if it were.
            context.DrawGeometry(null, glow, _drawn[i]);

            // The fringe separates as far as the tear has moved, so the channels come apart with it
            // and close up again behind it. Fixed offsets under a tear that now swells would appear
            // at their full separation on the tear's first frame, which is the snap taken out above.
            if (fringe > 0.05)
            {
                using (context.PushTransform(Matrix.CreateTranslation(-fringe, 0)))
                    context.DrawGeometry(null, new Pen(
                        new SolidColorBrush(Shade(0x7A, 0xFF3B4E, lamp)), 2.2,
                        lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round), _drawn[i]);

                using (context.PushTransform(Matrix.CreateTranslation(fringe, 0)))
                    context.DrawGeometry(null, new Pen(
                        new SolidColorBrush(Shade(0x7A, 0x36F0FF, lamp)), 2.2,
                        lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round), _drawn[i]);
            }

            context.DrawGeometry(null, stroke, _drawn[i]);

            if (shown < _widths[i])
                context.DrawRectangle(caret, null, new Rect(shown, -3, 3, size + 6));
        }
    }

    /// <summary>Rebuilds the glyph geometry, but only when the words or the size have actually changed.</summary>
    private void Text(string[] lines, double size)
    {
        if (_drawn.Length == lines.Length && _builtFor == _shown && Math.Abs(_builtAt - size) < 0.01)
            return;

        _lines = lines;
        _drawn = new Geometry[lines.Length];
        _widths = new double[lines.Length];
        _widest = 1;

        for (var i = 0; i < lines.Length; i++)
        {
            (_drawn[i], _widths[i]) = Glyphs.Build(lines[i], size, new Point(0, 0));
            _widest = Math.Max(_widest, _widths[i]);
        }

        _builtFor = _shown;
        _builtAt = size;
    }

    /// <summary>
    /// Splits a caption in two, at a full stop if there is one and otherwise at the space nearest the
    /// middle.
    ///
    /// Nearest the middle rather than at the last space that fits, which is what a text layout engine
    /// would do: these are one or two short sentences, and balancing them reads better than filling the
    /// first line and orphaning two words onto the second. Preferring a full stop is what turns
    /// "Ashfall, nightside. Relay / Nine, 04:12 station time." into the break a person would have made.
    /// </summary>
    private static string[] Break(string text)
    {
        var middle = text.Length / 2;

        // A stop or a comma is only a better break than the middle if it is somewhere near the middle.
        // "A. Then a very long second clause" breaks after two characters otherwise.
        var reach = text.Length * 0.32;

        var split = At('.', reach);

        if (split < 0)
            split = At(',', reach);

        if (split < 0)
            split = At('\0', text.Length);

        return split < 0 ? [text] : [text[..split], text[(split + 1)..]];

        int At(char after, double within)
        {
            var best = -1;

            for (var i = 1; i < text.Length - 1; i++)
                if (text[i] == ' ' && (after == '\0' || text[i - 1] == after))
                    if (Math.Abs(i - middle) <= within &&
                        (best < 0 || Math.Abs(i - middle) < Math.Abs(best - middle)))
                        best = i;

            return best;
        }
    }

    /// <summary>A colour with its alpha scaled by how bright the panel is at the moment.</summary>
    private static Color Shade(byte alpha, uint rgb, double lamp) => Color.FromArgb(
        (byte)Math.Clamp(alpha * lamp, 0, 255),
        (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);

    private static double Ease(double t) => t * t * (3 - 2 * t);

    /// <summary>A repeatable 0..1 from a slot number, so the same frame glitches on every run.</summary>
    private static double Noise(double slot)
    {
        var h = (uint)(int)slot * 2654435761u;
        h ^= h >> 15;
        h *= 2246822519u;
        h ^= h >> 13;
        return h / (double)uint.MaxValue;
    }
}
