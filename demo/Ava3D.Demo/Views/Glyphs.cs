using System.Collections.Generic;
using System.Numerics;
using Avalonia;
using Avalonia.Media;

namespace Ava3D.Demo.Views;

/// <summary>
/// A stroke alphabet, drawn rather than typeset.
///
/// The caption band wanted a display face — angular, wide, the sort of thing a ship's console would
/// have — and there are three ways to get one. Ship a font file, which means shipping somebody's
/// licence with a demo whose whole point is that it can be copied out without one. Name a font in the
/// hope the machine has it, which on a project that runs the same code on desktop, browser, Android and
/// iOS means four different answers and no way to know which one a reader is looking at. Or draw it,
/// which is what this is: forty-nine glyphs on a five-by-seven grid, about a hundred and sixty line
/// segments in total, and identical on every platform because it is geometry rather than a lookup.
///
/// Each glyph is a set of polylines. x runs 0..4 across the cell and y runs 0..6 down it, with 7
/// available for the one descender that needs it. A pair of digits is a point, spaces separate points
/// along a stroke, and '|' starts a new stroke — so "00 40 06 46" is a Z and "00 06|40 46|03 43" is an
/// H. Nothing is filled: a period is a small square traced as a closed polyline, because a stroke font
/// with one filled glyph in it is two renderers.
/// </summary>
internal static class Glyphs
{
    /// <summary>Across the cell. The advance adds a gap on top of this.</summary>
    public const float CellWidth = 4f;

    /// <summary>Down the cell, from cap height to baseline. Descenders reach 7.</summary>
    public const float CellHeight = 6f;

    /// <summary>Cell plus the gap to the next one, in the same units.</summary>
    public const float Advance = 6f;

    private static readonly Dictionary<char, string> Strokes = new()
    {
        ['A'] = "06 02 20 42 46|04 44",
        ['B'] = "00 30 41 42 33 03|33 43 44 35 06 00|00 06",
        ['C'] = "41 30 10 01 05 16 36 45",
        ['D'] = "00 30 41 45 36 06 00",
        ['E'] = "40 00 06 46|03 33",
        ['F'] = "40 00 06|03 33",
        ['G'] = "41 30 10 01 05 16 36 45 43 23",
        ['H'] = "00 06|40 46|03 43",
        ['I'] = "10 30|20 26|16 36",
        ['J'] = "40 45 36 16 05 04",
        ['K'] = "00 06|40 03 46",
        ['L'] = "00 06 46",
        ['M'] = "06 00 22 40 46",
        ['N'] = "06 00 46 40",
        ['O'] = "10 30 41 45 36 16 05 01 10",
        ['P'] = "06 00 30 41 42 33 03",
        ['Q'] = "10 30 41 45 36 16 05 01 10|24 46",
        ['R'] = "06 00 30 41 42 33 03|23 46",
        ['S'] = "41 30 10 01 02 13 33 44 45 36 16 05",
        ['T'] = "00 40|20 26",
        ['U'] = "00 05 16 36 45 40",
        ['V'] = "00 03 26 43 40",
        ['W'] = "00 06 24 46 40",
        ['X'] = "00 46|40 06",
        ['Y'] = "00 23 40|23 26",
        ['Z'] = "00 40 06 46",

        ['0'] = "10 30 41 45 36 16 05 01 10|15 31",
        ['1'] = "01 20 26|06 46",
        ['2'] = "01 10 30 41 42 06 46",
        ['3'] = "00 40 23 44 45 36 16 05",
        ['4'] = "36 30 04 44",
        ['5'] = "40 00 03 33 44 45 36 16 05",
        ['6'] = "41 30 10 01 05 16 36 45 44 33 03",
        ['7'] = "00 40 26",
        ['8'] = "13 02 01 10 30 41 42 33 13 04 05 16 36 45 44 33",
        ['9'] = "05 16 36 45 41 30 10 01 02 13 43",

        // Punctuation. The dot is a one-by-one square rather than a stub of line, because a vertical
        // tick at the foot of a cell reads as an apostrophe that has fallen over.
        ['.'] = "25 35 36 26 25",
        [','] = "35 36 27",
        [':'] = "22 32 33 23 22|24 34 35 25 24",
        [';'] = "22 32 33 23 22|34 35 26",
        ['\''] = "20 22",
        ['-'] = "13 33",
        ['/'] = "40 06",
        ['!'] = "20 24|25 35 36 26 25",
        ['?'] = "01 10 30 41 42 24|25 35 36 26 25",
        ['('] = "30 12 14 36",
        [')'] = "10 32 34 16",
        ['·'] = "23 33 34 24 23"
    };

    /// <summary>
    /// The text as one geometry, laid out left to right at a cap height of <paramref name="size"/>.
    ///
    /// Everything is upper-cased on the way in. That is not laziness about a lower case: a console face
    /// with two cases is two alphabets to draw and to keep consistent, and every reference for this look
    /// — a mission briefing, a warning plate, a codec — is set in caps anyway.
    ///
    /// Anything with no glyph advances and draws nothing, so an unexpected character costs a space
    /// rather than an exception. A caption is not worth crashing a demo over.
    /// </summary>
    public static (Geometry Geometry, double Width) Build(string text, double size, Point origin)
    {
        var unit = size / CellHeight;
        var geometry = new StreamGeometry();
        var pen = origin.X;

        using (var context = geometry.Open())
        {
            foreach (var raw in text)
            {
                var glyph = char.ToUpperInvariant(raw);

                if (Strokes.TryGetValue(glyph, out var strokes))
                    foreach (var stroke in strokes.Split('|'))
                    {
                        var points = stroke.Split(' ');
                        var closed = points.Length > 2 && points[0] == points[^1];

                        context.BeginFigure(At(points[0]), isFilled: false);

                        for (var i = 1; i < points.Length; i++)
                            context.LineTo(At(points[i]));

                        context.EndFigure(closed);

                        Point At(string point) => new(
                            pen + (point[0] - '0') * unit,
                            origin.Y + (point[1] - '0') * unit);
                    }

                pen += Advance * unit;
            }
        }

        // The trailing gap is not part of the word, so the width is the last cell's right-hand edge.
        return (geometry, text.Length == 0 ? 0 : pen - origin.X - (Advance - CellWidth) * unit);
    }

    /// <summary>How wide <paramref name="text"/> will be at that cap height, without building it.</summary>
    public static double Measure(string text, double size) => text.Length == 0
        ? 0
        : (text.Length * Advance - (Advance - CellWidth)) * (size / CellHeight);

    /// <summary>
    /// The same alphabet as bare line segments, for a caller that is not drawing on a canvas.
    ///
    /// One reader wants that, and it is the reason this exists: the film's title card is geometry in the
    /// scene rather than an overlay on top of it — see <c>Story.Curtain</c> — because a frame grab of the
    /// demo has to be able to show it. An overlay is invisible to <c>AVA3D_CAPTURE</c>, so a title drawn
    /// there is a title nobody can check.
    ///
    /// The pairs are what <see cref="LineNode.Positions"/> wants: [0]–[1] is one segment, [2]–[3] the
    /// next, so a four-point stroke comes back as three pairs rather than as four points. Units are the
    /// cell's: x runs right from nought, y runs <b>down</b> from nought, and the word is
    /// <see cref="Advance"/> per glyph wide by <see cref="CellHeight"/> tall. Scaling and flipping it is
    /// the caller's business, because the caller is the one that knows which way up its own space is.
    /// </summary>
    public static Vector2[] Segments(string text)
    {
        var pairs = new List<Vector2>();
        var pen = 0f;

        foreach (var raw in text)
        {
            if (Strokes.TryGetValue(char.ToUpperInvariant(raw), out var strokes))
                foreach (var stroke in strokes.Split('|'))
                {
                    var points = stroke.Split(' ');

                    for (var i = 1; i < points.Length; i++)
                    {
                        pairs.Add(At(points[i - 1]));
                        pairs.Add(At(points[i]));
                    }

                    Vector2 At(string point) => new(pen + (point[0] - '0'), point[1] - '0');
                }

            pen += Advance;
        }

        return pairs.ToArray();
    }
}
