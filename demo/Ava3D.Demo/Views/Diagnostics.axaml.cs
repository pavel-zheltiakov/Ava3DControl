using System;
using System.Collections.Generic;
using System.Linq;
using Ava3D.Demo.Engine;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Ava3D.Demo.Views;

/// <summary>
/// The probe panel: what is drawing, how fast, and what it cost.
///
/// Its own control rather than another twenty lines inside MainView, for one reason that turned out to
/// matter: a panel that can be built on its own can be rendered on its own. <see cref="RenderInfo"/> is a
/// record with init-only properties, so a test harness can hand this control a frame's worth of numbers
/// and photograph the result — which is the only way to see it at all, given that the control's own frame
/// capture happens where the 3D view draws and therefore contains nothing that composites over it.
/// </summary>
public partial class Diagnostics : UserControl
{
    private readonly TextBlock _renderer, _hostName, _fps, _frameTime;
    private readonly TextBlock _draws, _triangles, _resolution, _textures;
    private readonly TextBlock _engineNote, _errors, _picked;
    private readonly TextBlock _featureScore, _featureChevron;
    private readonly ToggleButton _featureToggle;
    private readonly StackPanel _featureList;

    /// <summary>
    /// The list the rows were last built from, so nine controls are not rebuilt sixty times a second.
    ///
    /// Reference equality on purpose. Every backend hands out the same instance for the life of the
    /// process — see <c>FeatureReport</c> — so this changes exactly when the renderer does, which is the
    /// only moment the rows can have changed.
    /// </summary>
    private IReadOnlyList<RendererFeature>? _shownFeatures;

    /// <summary>
    /// The headline and the host line as they currently read, so neither is rebuilt sixty times a second
    /// to say what it already says. Both change exactly once — when a renderer starts.
    /// </summary>
    private string? _shownHeadline, _shownDevice;

    public Diagnostics()
    {
        AvaloniaXamlLoader.Load(this);

        _renderer = this.FindControl<TextBlock>("Renderer")!;
        _hostName = this.FindControl<TextBlock>("HostName")!;
        _fps = this.FindControl<TextBlock>("Fps")!;
        _frameTime = this.FindControl<TextBlock>("FrameTime")!;
        _draws = this.FindControl<TextBlock>("Draws")!;
        _triangles = this.FindControl<TextBlock>("Triangles")!;
        _resolution = this.FindControl<TextBlock>("Resolution")!;
        _textures = this.FindControl<TextBlock>("Textures")!;
        _engineNote = this.FindControl<TextBlock>("EngineNote")!;
        _errors = this.FindControl<TextBlock>("Errors")!;
        _picked = this.FindControl<TextBlock>("Picked")!;

        _featureToggle = this.FindControl<ToggleButton>("FeatureToggle")!;
        _featureScore = this.FindControl<TextBlock>("FeatureScore")!;
        _featureChevron = this.FindControl<TextBlock>("FeatureChevron")!;
        _featureList = this.FindControl<StackPanel>("FeatureList")!;

        // A chevron that turns is the whole of the affordance, since the row has no border to open.
        _featureToggle.IsCheckedChanged += (_, _) =>
            _featureChevron.Text = _featureToggle.IsChecked == true ? "▾" : "▸";

        // Enough of the host to fill the line before any renderer exists — the machine is the half of it
        // only a renderer can answer, and the panel is built before the first frame is drawn.
        _hostName.Text = HostPlatform.Describe();
    }

    /// <summary>Shows a frame's worth of numbers. Called whenever the view publishes new ones.</summary>
    public void Show(RenderInfo info)
    {
        ShowRenderer(info);

        // The graphics card arrives with the first frame and cannot change afterwards, so this runs once.
        if (info.Device is { Length: > 0 } card && card != _shownDevice)
        {
            _shownDevice = card;
            _hostName.Text = HostPlatform.Describe(card);
        }

        // Before the first frame there is no rate to report, and a panel opening on "0.0 fps" reads as a
        // renderer that is not running rather than one that has not been asked yet.
        _fps.Text = info.FramesPerSecond > 0 ? $"{info.FramesPerSecond:F1} fps" : "— fps";
        _frameTime.Text = info.FrameMilliseconds > 0 ? $"{info.FrameMilliseconds:F2} ms/frame" : "";

        _draws.Text = info.DrawCalls.ToString("N0");
        _triangles.Text = info.Triangles.ToString("N0");
        _resolution.Text = info.Size ?? "—";
        _textures.Text = info.Textures ?? "none";
        ShowFeatures(info.Features);

        _errors.Text = info.Error;
        _errors.IsVisible = info.Error != null;
    }

    // Green for a tick, orange for a cross, and the panel's own grey for everything that is only text.
    private static readonly IBrush Yes = new SolidColorBrush(Color.Parse("#7FD8A0"));
    private static readonly IBrush No = new SolidColorBrush(Color.Parse("#E8996F"));
    private static readonly IBrush Bright = new SolidColorBrush(Color.Parse("#D8DCE6"));
    private static readonly IBrush Dim = new SolidColorBrush(Color.Parse("#9AA3B5"));
    private static readonly IBrush Faint = new SolidColorBrush(Color.Parse("#79839A"));

    /// <summary>
    /// The score, and the rows behind it.
    ///
    /// Closed, this is one number and its colour: nine of nine in green on a GPU backend, two of nine in
    /// orange on the CPU fallback. That is the comparison somebody actually wants — not a list of what was
    /// skipped, which reads as a fault report and says nothing at all on a renderer that skipped nothing.
    /// </summary>
    private void ShowFeatures(IReadOnlyList<RendererFeature> features)
    {
        if (ReferenceEquals(_shownFeatures, features))
            return;

        _shownFeatures = features;
        _featureList.Children.Clear();

        if (features.Count == 0)
        {
            _featureScore.Text = "—";
            _featureScore.Foreground = Dim;
            _featureToggle.IsEnabled = false;
            return;
        }

        var have = features.Count(f => f.Supported);
        _featureScore.Text = $"{have} of {features.Count}";
        _featureScore.Foreground = have == features.Count ? Yes : No;
        _featureToggle.IsEnabled = true;

        foreach (var feature in features)
            _featureList.Children.Add(Row(feature));
    }

    /// <summary>
    /// One feature: its mark, its name, and — when it is a cross — what happens instead.
    ///
    /// The reason is only shown for the rows that do not have the feature, which is what keeps the list
    /// readable at both ends. On Metal all nine are ticks and the panel is nine short lines; on the CPU
    /// fallback the seven crosses are the interesting part and each one carries its own sentence.
    /// </summary>
    private static Control Row(RendererFeature feature)
    {
        var mark = new TextBlock
        {
            Text = feature.Supported ? "✓" : "✗",
            Foreground = feature.Supported ? Yes : No,
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Top
        };

        var lines = new StackPanel { Spacing = 1 };
        lines.Children.Add(new TextBlock
        {
            Text = feature.Name,
            Foreground = feature.Supported ? Bright : Dim,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap
        });

        if (!feature.Supported)
            lines.Children.Add(new TextBlock
            {
                Text = feature.Detail,
                Foreground = Faint,
                FontSize = 10,
                LineHeight = 14,
                TextWrapping = TextWrapping.Wrap
            });

        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("18,*") };
        Grid.SetColumn(lines, 1);
        row.Children.Add(mark);
        row.Children.Add(lines);

        // Every reason is on the row somewhere, but the ticked rows keep theirs here rather than spending
        // nine lines of panel saying how a shader samples a texture.
        ToolTip.SetTip(row, feature.Detail);
        return row;
    }

    /// <summary>What the engine picker has to say, when a selection could not simply be applied.</summary>
    public void Note(string? text)
    {
        _engineNote.Text = text;
        _engineNote.IsVisible = text is { Length: > 0 };
    }

    /// <summary>The picking line: an invitation before the first click, the hit afterwards, or nothing.</summary>
    public void Pick(string? text)
    {
        _picked.Text = text;
        _picked.IsVisible = text is { Length: > 0 };
    }

    /// <summary>
    /// The engine, at the size of the frame rate, with whatever qualifies it in small type beside it.
    ///
    /// Rebuilt only when the words change, which is once: two runs and an InlineCollection per frame would
    /// be sixty allocations a second to draw the same two words.
    /// </summary>
    private void ShowRenderer(RenderInfo info)
    {
        var (name, detail) = Headline(info);
        var headline = detail is null ? name : $"{name} {detail}";

        if (headline == _shownHeadline)
            return;

        _shownHeadline = headline;

        var inlines = _renderer.Inlines ??= [];
        inlines.Clear();
        inlines.Add(new Run(name));

        if (detail is not null)
            inlines.Add(new Run($"  {detail}")
            {
                FontSize = 12, FontWeight = FontWeight.Normal, Foreground = Dim
            });
    }

    /// <summary>
    /// The engine's name, and the part of the answer that is not its name.
    ///
    /// <see cref="RenderInfo.Renderer"/> and <see cref="RenderInfo.Context"/> overlap on purpose — "OpenGL"
    /// and "OpenGL 4.6" — because an application wanting one word for a status bar and one wanting the
    /// version are both real callers. Printing both would say OpenGL twice, and printing only the first
    /// throws away the difference between a desktop driver and a phone's. So the name is the headline and
    /// what is left of the context after it is the qualifier: "OpenGL 4.6", "Metal", "Skia CPU+GPU".
    /// </summary>
    private static (string Name, string? Detail) Headline(RenderInfo info)
    {
        if (info.Context is not { Length: > 0 } context)
            return (info.Renderer, null);

        if (context.StartsWith(info.Renderer, StringComparison.OrdinalIgnoreCase))
            context = context[info.Renderer.Length..];

        return (info.Renderer, context.Trim() is { Length: > 0 } detail ? detail : null);
    }
}
