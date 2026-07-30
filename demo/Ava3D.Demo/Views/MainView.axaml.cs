using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Ava3D.Demo.Engine;
using Ava3D.Demo.Scenes;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace Ava3D.Demo.Views;

public partial class MainView : UserControl
{
    /// <summary>How long each scene gets during the automatic tour.</summary>
    private static readonly TimeSpan TourInterval = TimeSpan.FromSeconds(9);

    private Ava3DView _view = null!;
    private ComboBox _sceneList = null!, _engineList = null!;
    private ToggleSwitch _tourToggle = null!;
    private TextBlock _sceneTitle = null!, _sceneNotes = null!;
    private TextBlock _renderer = null!, _stats = null!, _limits = null!;
    private TextBlock _engineNote = null!, _errors = null!, _picked = null!;

    private readonly IReadOnlyList<DemoScene> _catalog = DemoCatalog.Describe();
    private DemoScene _current = null!;
    private Scene _currentScene = null!;
    private DateTime _sceneStarted = DateTime.UtcNow;

    private DispatcherTimer? _animationTimer;
    private DispatcherTimer? _tourTimer;

    private MeshNode? _highlighted;
    private Material? _originalMaterial;

    /// <summary>Set while the picker is being rebuilt, so its own selection changes are not acted on.</summary>
    private bool _suppressEngineChange;

    /// <summary>
    /// Latest render info from any live view, so a headless probe run can print it instead of relying
    /// on someone watching a window.
    /// </summary>
    public static RenderInfo? LastInfo { get; private set; }

    /// <summary>
    /// The same summary on every platform. Printed to the console, which is a terminal on desktop, the
    /// devtools console in a browser, logcat on Android and the device log on iOS — so one line of code
    /// gives every head a verifiable result without anyone watching a screen.
    /// </summary>
    public static string Describe(RenderInfo? info) => info is null
        ? "NO INFO — the view never rendered."
        : $"""

           ================ Ava3D ================
           renderer  : {info.Renderer}
           context   : {info.Context}
           size      : {info.Size}
           perf      : {info.FramesPerSecond:F1} fps, {info.FrameMilliseconds:F2} ms/frame, {info.FramesRendered} frames
           geometry  : {info.DrawCalls} draws, {info.Triangles:N0} triangles
           textures  : {info.Textures ?? "(none)"}
           backends  : {string.Join(", ", info.AvailableBackends.Select(b => b.ToString()))}
           skipped   : {info.PlatformLimitations}
           error     : {info.Error ?? "(none)"}
           =======================================
           """;

    public MainView()
    {
        AvaloniaXamlLoader.Load(this);

        _view = this.FindControl<Ava3DView>("View")!;
        _sceneList = this.FindControl<ComboBox>("SceneList")!;
        _engineList = this.FindControl<ComboBox>("EngineList")!;
        _tourToggle = this.FindControl<ToggleSwitch>("TourToggle")!;
        _sceneTitle = this.FindControl<TextBlock>("SceneTitle")!;
        _sceneNotes = this.FindControl<TextBlock>("SceneNotes")!;
        _renderer = this.FindControl<TextBlock>("Renderer")!;
        _stats = this.FindControl<TextBlock>("Stats")!;
        _limits = this.FindControl<TextBlock>("Limits")!;
        _engineNote = this.FindControl<TextBlock>("EngineNote")!;
        _errors = this.FindControl<TextBlock>("Errors")!;
        _picked = this.FindControl<TextBlock>("Picked")!;

        _sceneList.ItemsSource = _catalog.Select(s => s.Title).ToArray();
        _sceneList.SelectionChanged += (_, _) => Select(_sceneList.SelectedIndex);

        this.FindControl<Button>("PrevButton")!.Click += (_, _) => Step(-1);
        this.FindControl<Button>("NextButton")!.Click += (_, _) => Step(1);
        this.FindControl<Button>("FitButton")!.Click += (_, _) => _view.FitToScene();

        _tourToggle.IsCheckedChanged += (_, _) => UpdateTour();
        _engineList.SelectionChanged += (_, _) => OnEngineSelected();

        _view.Picked += OnPicked;
        _view.PropertyChanged += (_, e) =>
        {
            if (e.Property == Ava3DView.InfoProperty)
                ShowInfo(_view.Info);
        };

        // One timer for animation, running only while a scene wants it. A demo that spins a timer for
        // static scenes is a demo that reports a frame rate nobody can reproduce.
        _animationTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(16), DispatcherPriority.Background,
            (_, _) => _current.Update(_currentScene, (DateTime.UtcNow - _sceneStarted).TotalSeconds));

        // AVA3D_SCENE picks a starting scene by index or by name, so a probe run can land on the one it
        // wants to measure without anyone clicking.
        _sceneList.SelectedIndex = StartingScene();
        _tourToggle.IsChecked = Environment.GetEnvironmentVariable("AVA3D_TOUR") == "1";

        // On a phone the notes panel would cover most of the scene, so it starts collapsed there. Measured
        // rather than guessed from the platform: a narrow desktop window has the same problem.
        AttachedToVisualTree += (_, _) =>
        {
            if (TopLevel.GetTopLevel(this)?.Bounds.Width is { } width && width < 700)
                this.FindControl<ToggleButton>("NotesToggle")!.IsChecked = true;
        };

        // Once warm, say what happened. This is how the browser, Android and iOS heads get verified.
        DispatcherTimer.RunOnce(() => Console.WriteLine(Describe(LastInfo)), TimeSpan.FromSeconds(12));
    }

    /// <summary>
    /// The scene to open on. Accepts an index or a case-insensitive title prefix, because a probe script
    /// reads better as AVA3D_SCENE=pbr than as AVA3D_SCENE=11.
    /// </summary>
    private int StartingScene()
    {
        var wanted = Environment.GetEnvironmentVariable("AVA3D_SCENE");
        if (string.IsNullOrWhiteSpace(wanted))
            return 0;

        if (int.TryParse(wanted, out var index) && index >= 0 && index < _catalog.Count)
            return index;

        for (var i = 0; i < _catalog.Count; i++)
            if (_catalog[i].Title.StartsWith(wanted, StringComparison.OrdinalIgnoreCase))
                return i;

        return 0;
    }

    private void Step(int delta)
    {
        var next = (_sceneList.SelectedIndex + delta + _catalog.Count) % _catalog.Count;
        _sceneList.SelectedIndex = next;
    }

    private void Select(int index)
    {
        if (index < 0 || index >= _catalog.Count)
            return;

        ClearHighlight();

        // A fresh instance, so a scene's animation state cannot survive being revisited.
        _current = DemoCatalog.Scenes[index]();
        _currentScene = _current.Build();
        _sceneStarted = DateTime.UtcNow;

        // Angle first, then the scene: AutoFit runs when the first snapshot arrives and sets only the
        // target and distance, so a scene's chosen yaw and pitch survive being framed.
        _view.AutoFit = !_current.FramesItself;
        _current.Frame(_view.Camera);
        _view.Scene = _currentScene;
        _view.IsPickingEnabled = _current.WantsPicking;
        _view.InvalidateCamera();

        _sceneTitle.Text = $"{index + 1}/{_catalog.Count}  ·  {_current.Title}";
        _sceneNotes.Text = Reflow(_current.Notes);
        _picked.Text = _current.WantsPicking
            ? "Click an object · drag to orbit · wheel to zoom · right-drag to pan"
            : "Drag to orbit · wheel to zoom · right-drag to pan";

        if (_current.Animates)
            _animationTimer?.Start();
        else
            _animationTimer?.Stop();
    }

    /// <summary>
    /// Unwraps a scene's notes into paragraphs the TextBlock can wrap itself.
    ///
    /// The notes are raw string literals wrapped at a hundred columns for the benefit of whoever is reading
    /// the source, and those hard line breaks survive into the rendered text — where they collide with the
    /// TextBlock's own wrapping and produce a ragged left-hand column of orphans. Joining each paragraph back
    /// into one line and keeping only the blank-line breaks gives the layout something it can wrap properly.
    ///
    /// Lines that begin a list item or are visibly a table row are left alone: they are aligned on purpose.
    /// </summary>
    private static string Reflow(string notes)
    {
        var paragraphs = notes.Replace("\r\n", "\n").Split("\n\n");

        return string.Join("\n\n", paragraphs.Select(paragraph =>
        {
            var lines = paragraph.Split('\n').Select(l => l.TrimEnd()).ToList();

            // A paragraph where several lines carry an aligned em dash is a table, and reflowing it would
            // destroy the only thing making it readable.
            var aligned = lines.Count(l => l.Contains(" — ", StringComparison.Ordinal));
            if (aligned >= 2 || lines.Any(l => l.TrimStart().StartsWith("- ", StringComparison.Ordinal)))
                return string.Join("\n", lines);

            return string.Join(" ", lines.Select(l => l.Trim()).Where(l => l.Length > 0));
        }));
    }

    private void UpdateTour()
    {
        _tourTimer?.Stop();
        _tourTimer = null;

        if (_tourToggle.IsChecked != true)
            return;

        _tourTimer = new DispatcherTimer(TourInterval, DispatcherPriority.Background, (_, _) => Step(1));
        _tourTimer.Start();
    }

    /// <summary>
    /// Applies a picker selection.
    ///
    /// Three outcomes, and the difference matters. Software is a live switch and takes effect on the next
    /// frame. The GPU API this process already holds is also live. The other GPU API is not reachable from
    /// here at all — so either the head relaunches itself, or the demo says plainly that it cannot, rather
    /// than leaving a control that appears to do nothing.
    /// </summary>
    private void OnEngineSelected()
    {
        if (_suppressEngineChange || _engineList.SelectedItem is not EngineChoice choice)
            return;

        if (choice.Option is { Availability: BackendAvailability.RequiresRestart } option)
        {
            if (EngineRelauncher.Current is { CanRelaunch: true } relauncher)
            {
                _engineNote.Text = $"Restarting for {choice.Kind}…";
                _engineNote.IsVisible = true;
                relauncher.Relaunch(choice.Kind);
                return;
            }

            _engineNote.Text = EngineRelauncher.Describe(choice.Kind, option.Reason);
            _engineNote.IsVisible = true;
            RebuildEngineList(_view.Info);
            return;
        }

        _view.PreferredBackend = choice.Kind;
    }

    /// <summary>One row of the engine picker. <c>Option</c> is null for the Automatic entry.</summary>
    private sealed record EngineChoice(RenderBackendKind Kind, string Label, BackendOption? Option)
    {
        public override string ToString() => Label;
    }

    private void RebuildEngineList(RenderInfo info)
    {
        var choices = new List<EngineChoice>
        {
            new(RenderBackendKind.Automatic, "Automatic", null)
        };

        foreach (var option in info.AvailableBackends)
        {
            var suffix = option.Availability switch
            {
                BackendAvailability.Active => " — running",
                BackendAvailability.RequiresRestart =>
                    EngineRelauncher.Current is { CanRelaunch: true } ? " — needs restart" : " — unavailable here",
                BackendAvailability.Unavailable => " — unavailable",
                _ => ""
            };

            // An unreachable backend still appears, greyed by its label rather than removed. A picker that
            // hides Metal on Windows invites the question of whether Metal exists; one that shows it as
            // unavailable answers it.
            choices.Add(new EngineChoice(option.Kind, option.Name + suffix, option));
        }

        _suppressEngineChange = true;
        try
        {
            _engineList.ItemsSource = choices;
            var wanted = info.PreferredBackend;
            _engineList.SelectedIndex = Math.Max(0, choices.FindIndex(c => c.Kind == wanted));
        }
        finally
        {
            _suppressEngineChange = false;
        }
    }

    private void OnPicked(object? sender, PickEventArgs e)
    {
        ClearHighlight();

        if (e.Result is not { } hit)
        {
            _picked.Text = "clicked nothing";
            return;
        }

        // Give the picked node its own material, so the highlight cannot leak to siblings sharing one.
        _highlighted = hit.Node;
        _originalMaterial = hit.Node.Material;

        var highlight = _originalMaterial.Clone();
        highlight.BaseColor = new Vector4(1f, 0.55f, 0.15f, 1f);
        highlight.EmissiveColor = new Vector3(0.20f, 0.07f, 0f);
        hit.Node.Material = highlight;

        _picked.Text =
            $"{hit.Node.Name ?? "node"} · triangle {hit.TriangleIndex} · " +
            $"{hit.Distance:F2} away at ({hit.WorldPosition.X:F2}, {hit.WorldPosition.Y:F2}, {hit.WorldPosition.Z:F2})";
    }

    private void ClearHighlight()
    {
        if (_highlighted != null && _originalMaterial != null)
            _highlighted.Material = _originalMaterial;

        _highlighted = null;
        _originalMaterial = null;
    }

    private void ShowInfo(RenderInfo info)
    {
        LastInfo = info;

        _renderer.Text = info.Context is { } context
            ? $"{info.Renderer}\n{context}"
            : info.Renderer;

        _stats.Text =
            $"{info.FramesPerSecond,6:F1} fps  {info.FrameMilliseconds,6:F2} ms\n" +
            $"{info.DrawCalls,6} draws {info.Triangles,10:N0} tris\n" +
            $"{info.Size}";

        _limits.Text = info.Textures is { } textures
            ? $"{textures}\nskipped: {info.PlatformLimitations}"
            : $"skipped: {info.PlatformLimitations}";

        _errors.Text = info.Error;
        _errors.IsVisible = info.Error != null;

        // The list depends on what the platform handed back, which is only known once a frame has been
        // drawn — so it is built from the first RenderInfo rather than in the constructor.
        if (_engineList.ItemsSource == null && info.AvailableBackends.Count > 0)
            RebuildEngineList(info);
    }
}
