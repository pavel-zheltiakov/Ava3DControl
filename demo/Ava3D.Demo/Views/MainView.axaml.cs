using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Ava3D.Demo.Engine;
using Ava3D.Demo.Scenes;
using Ava3D.Demo.Story;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace Ava3D.Demo.Views;

public partial class MainView : UserControl
{
    private Ava3DView _view = null!;
    private ComboBox _sceneList = null!, _engineList = null!;

    /// <summary>The steppers either side of the scene list. Held because they are disabled with it.</summary>
    private Button _prevButton = null!, _nextButton = null!;
    private ToggleSwitch _autoToggle = null!, _storyToggle = null!, _soundToggle = null!;

    /// <summary>
    /// Whether the next build of the film should start at the beginning rather than at the selected
    /// entry's cue. Set when the story switch is turned on, and cleared by the build that uses it.
    ///
    /// Deliberately not set by anything else. A run that names a scene — <c>AVA3D_SCENE</c>, or
    /// <c>?scene=</c> — is asking for that scene and gets it, and nothing else in the toolbar rebuilds the
    /// film at all: the sound switch opens and closes the device where the film stands, and changing
    /// engine restarts the process.
    /// </summary>
    private bool _storyFromTop;
    private TextBlock _sceneTitle = null!, _sceneNotes = null!;
    private TextBlock _noticeTitle = null!, _noticeReason = null!;
    private Border _notesPanel = null!, _engineNotice = null!;
    private Button _noticeAction = null!;
    private Border _restartAsk = null!;
    private TextBlock _restartQuestion = null!, _restartDetail = null!;
    private Button _restartYes = null!, _restartNo = null!;
    private Diagnostics _probe = null!;
    private Codec _captionBand = null!;
    private StackPanel _stepPad = null!;
    private Border _stepForward = null!, _stepBack = null!;

    /// <summary>Which way the viewer is asking to walk: keys, and the two buttons, summed. Kept as separate
    /// halves so a key released while a button is held does not stop him.</summary>
    private Vector2 _keys, _taps;

    /// <summary>Whether the shell has already given the current scene the controls, so the handover is done
    /// once rather than on every frame.</summary>
    private bool _handedOver;

    /// <summary>What the notice is about, so its button knows which renderer to restart for.</summary>
    private BackendOption? _noticeOption;

    /// <summary>
    /// What the confirmation is about, and whether pressing yes now closes rather than restarts.
    ///
    /// The second is not the same question as <see cref="IEngineRelauncher.CanRelaunch"/>, because a head
    /// that believes it can restart can still fail to: the executable was there when the demo started and
    /// is not there now, the machine is out of process handles. The dialog answers that by changing into
    /// the one it can honour instead of closing on a promise it did not keep.
    /// </summary>
    private BackendOption? _askOption;

    private bool _askCloses;

    /// <summary>The caption currently on screen, so an unchanged one is not logged sixty times a second.</summary>
    private string? _shownCaption;

    /// <summary>
    /// The one list, and the switch that decides what picking an entry does. See <see cref="Contents"/>.
    /// </summary>
    private readonly IReadOnlyList<ContentEntry> _catalog = Contents.Entries;

    private DemoScene _current = null!;
    private Scene _currentScene = null!;

    /// <summary>
    /// An untouched camera, read for its defaults when a scene switch hands the next scene a clean one.
    ///
    /// A camera rather than four literals, so this cannot drift out of step with the control: whatever
    /// <see cref="Camera"/> considers a sensible field of view and clip range is by definition what a
    /// scene that sets neither should get. See <see cref="Select"/>.
    /// </summary>
    private static readonly Camera CameraDefaults = new();

    /// <summary>
    /// The frame clock's reading when the current scene opened, and whether we have one yet.
    ///
    /// Not a wall-clock time. See <see cref="Frame"/> — the difference is the whole reason animation
    /// looks smooth rather than merely running at the right average rate.
    /// </summary>
    private TimeSpan _sceneStarted;

    private bool _haveStart;

    /// <summary>Whether the current scene wants frames. Re-armed every frame while it does.</summary>
    private bool _animating;

    /// <summary>The scene that has actually been built, and the one asked for but not built yet.</summary>
    private int _shown = -1, _wanted = -1;

    /// <summary>
    /// How long the current scene has been on screen, for the automatic advance.
    ///
    /// Wall clock rather than the frame clock <see cref="Frame"/> runs on, because a scene that does not
    /// animate has no frame clock at all and still has to be moved on from.
    /// </summary>
    private readonly System.Diagnostics.Stopwatch _onScreen = new();

    private DispatcherTimer? _autoTimer;

    private MeshNode? _highlighted;
    private Material? _originalMaterial;

    /// <summary>Set while the picker is being rebuilt, so its own selection changes are not acted on.</summary>
    private bool _suppressEngineChange;

    /// <summary>What the engine picker was last built for, so it is only rebuilt when that changes.</summary>
    private RenderBackendKind? _listedActive;

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
    /// <summary>
    /// The film's scripted cameras, checked against the things a person cannot walk through.
    ///
    /// Here rather than in the head that calls it for the same reason <see cref="Describe"/> is: the story
    /// is internal to this assembly, and the desktop, browser and mobile heads are not. One public line so
    /// every head can run the same check.
    /// </summary>
    public static string DescribeWalks() => Ground.Audit();

    public static string Describe(RenderInfo? info) => info is null
        ? "NO INFO — the view never rendered."
        : $"""

           ================ Ava3D ================
           renderer  : {info.Renderer}
           context   : {info.Context}
           host      : {HostPlatform.Describe(info.Device)}, {RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant()}, {Cores()}
           size      : {info.Size}
           perf      : {info.FramesPerSecond:F1} fps, {info.FrameMilliseconds:F2} ms/frame, {info.RenderMilliseconds:F2} ms/render, {info.FramesRendered} frames
           geometry  : {info.DrawCalls} draws, {info.Triangles:N0} triangles
           snapshot  : {info.SceneRebuildSummary ?? "not rebuilt — the scene is static"}
           textures  : {info.Textures ?? "(none)"}
           backends  : {string.Join(", ", info.AvailableBackends.Select(b => b.ToString()))}
           features  : {Features(info)}
           notice    : {DescribeNotice(info)}
           error     : {info.Error ?? "(none)"}
           =======================================
           """;

    /// <summary>
    /// The message on the picture, in the report from the terminal.
    ///
    /// The probe exists so the control can be verified without somebody watching a window, and this is
    /// the one thing it was still missing: what the person watching the window would be reading. It is
    /// composed from the library's own catalogue rather than restated here, so a probe run proves the
    /// sentence a user sees rather than a second sentence that agrees with it today.
    /// </summary>
    private static string DescribeNotice(RenderInfo info)
    {
        if (info.PreferredBackend == RenderBackendKind.Automatic ||
            info.PreferredBackend == info.ActiveBackend ||
            info.AvailableBackends.FirstOrDefault(o => o.Kind == info.PreferredBackend) is not { } option)
            return "(none)";

        return $"{EngineRelauncher.Headline(option)} {EngineRelauncher.Explain(option)}";
    }

    /// <summary>
    /// How many cores the runtime will admit to, which is not always how many the machine has.
    ///
    /// Worth printing because it is the one number that explains a CPU-renderer frame rate before anyone
    /// reads the code: the software backend shares its vertex pass across cores only when there is more
    /// than one, and a WebAssembly tab reports exactly one unless the application was published with
    /// <c>WasmEnableThreads</c> <i>and</i> the page it landed on is cross-origin isolated. This demo
    /// arranges both — see the browser head's <c>coi.js</c> — so "1 core" in a browser now means the
    /// arrangement did not hold, which is a thing to see rather than to guess at, and it is invisible
    /// everywhere else.
    ///
    /// And a browser number is the browser's rather than the machine's. It comes from
    /// <c>navigator.hardwareConcurrency</c>, which WebKit clamps at eight however many the machine has,
    /// so one Apple silicon Mac reports sixteen on the desktop, sixteen in Chrome and eight in Safari.
    /// Eight is then really all there is: the thread pool is sized from this number, so the clamp costs
    /// frames rather than only honesty. Nothing to chase — it is the platform's answer, and printing it
    /// is what makes two browsers on one machine explain their own difference.
    /// </summary>
    private static string Cores()
        => Environment.ProcessorCount == 1 ? "1 core" : $"{Environment.ProcessorCount} cores";

    /// <summary>
    /// The feature score and, when it is not full marks, which rows are missing.
    ///
    /// Names only. The reasons are a paragraph each and this line is meant to be read at a glance in a
    /// terminal, a devtools console, logcat or a device log — the panel is where a reason belongs.
    /// </summary>
    private static string Features(RenderInfo info)
    {
        if (info.Features.Count == 0)
            return "not checked — no frame has been drawn yet";

        var missing = info.Features.Where(f => !f.Supported).Select(f => f.Name).ToArray();

        return missing.Length == 0
            ? $"{info.Features.Count} of {info.Features.Count} — nothing this renderer cannot do"
            : $"{info.Features.Count - missing.Length} of {info.Features.Count} — missing " +
              string.Join(", ", missing);
    }

    public MainView()
    {
        AvaloniaXamlLoader.Load(this);

        _view = this.FindControl<Ava3DView>("View")!;
        _sceneList = this.FindControl<ComboBox>("SceneList")!;
        _engineList = this.FindControl<ComboBox>("EngineList")!;
        _autoToggle = this.FindControl<ToggleSwitch>("AutoToggle")!;
        _storyToggle = this.FindControl<ToggleSwitch>("StoryToggle")!;
        _soundToggle = this.FindControl<ToggleSwitch>("SoundToggle")!;
        _sceneTitle = this.FindControl<TextBlock>("SceneTitle")!;
        _sceneNotes = this.FindControl<TextBlock>("SceneNotes")!;
        _notesPanel = this.FindControl<Border>("NotesPanel")!;
        _engineNotice = this.FindControl<Border>("EngineNotice")!;
        _noticeTitle = this.FindControl<TextBlock>("EngineNoticeTitle")!;
        _noticeReason = this.FindControl<TextBlock>("EngineNoticeReason")!;
        _noticeAction = this.FindControl<Button>("NoticeAction")!;
        _restartAsk = this.FindControl<Border>("RestartAsk")!;
        _restartQuestion = this.FindControl<TextBlock>("RestartQuestion")!;
        _restartDetail = this.FindControl<TextBlock>("RestartDetail")!;
        _restartYes = this.FindControl<Button>("RestartYes")!;
        _restartNo = this.FindControl<Button>("RestartNo")!;
        _probe = this.FindControl<Diagnostics>("Probe")!;
        _captionBand = this.FindControl<Codec>("CaptionBand")!;
        _stepPad = this.FindControl<StackPanel>("StepPad")!;
        _stepForward = this.FindControl<Border>("StepForward")!;
        _stepBack = this.FindControl<Border>("StepBack")!;

        // Numbered, because twenty-six is more than a list you can hold in your head, and the number is
        // what tells you how far through it you are without opening it.
        _sceneList.ItemsSource = _catalog.Select((s, i) => $"{i + 1}.  {s.Title}").ToArray();
        _sceneList.SelectionChanged += (_, _) => Want(_sceneList.SelectedIndex);

        _prevButton = this.FindControl<Button>("PrevButton")!;
        _nextButton = this.FindControl<Button>("NextButton")!;
        _prevButton.Click += (_, _) => Step(-1);
        _nextButton.Click += (_, _) => Step(1);

        // No camera input anywhere in the demo.
        //
        // Every scene here either frames itself from one angle that its notes are written about, or is
        // flown by a script. Handing the viewer the camera on top of that gives them one thing they can
        // do — leave — and half the scenes are a chart, a comparison or a shot list that means nothing
        // from the back. The Fit view button existed to undo it, which is the tell.
        //
        // Three lines, because orbit, pan and zoom are properties of the control rather than something it
        // insists on. An application that wants them leaves them alone; this one is a slide deck.
        //
        // With one exception, and it is the last thing the film does. When a scene says it has handed the
        // viewer the controls — see HandOver — orbit goes back on for exactly as long as that lasts. Pan
        // and zoom stay off either way: a person walking a deck cannot slide sideways without moving his
        // feet, and he certainly cannot pull the room towards him.
        _view.IsOrbitEnabled = false;
        _view.IsPanEnabled = false;
        _view.IsZoomEnabled = false;

        OnStep(_stepForward, 1f);
        OnStep(_stepBack, -1f);

        // The renderer the picker was last left on, applied before the first frame rather than after one,
        // so the demo opens on the engine you chose instead of switching under you a moment later.
        //
        // This is only half of remembering it, and it is the half a control can do. Moving a process
        // between Metal and OpenGL is an AppBuilder decision taken before any control exists, so the
        // desktop head reads the same setting on its way up. What is left for here is the CPU fallback,
        // which is the control's own decision, and putting the picker on what was asked for.
        if (DemoSettings.Engine is { } saved)
            _view.PreferredBackend = saved;

        // Before the first selection, because Select reads it to decide what to build. Assigned without
        // the handler attached, so restoring the setting is not itself a change to be acted on.
        _storyToggle.IsChecked = DemoSettings.Story && Contents.AnyFilmed;
        _storyToggle.IsEnabled = Contents.AnyFilmed;
        _storyToggle.IsCheckedChanged += (_, _) => OnStoryToggled();

        // Same shape as the story switch, and for the same reason: assigned before the handler is attached,
        // so restoring what was saved is not itself a change to be acted on.
        _soundToggle.IsChecked = DemoSettings.Sound;
        _soundToggle.IsCheckedChanged += (_, _) => OnSoundToggled();

        // AVA3D_THREADS=0 holds the CPU renderer to one core, so the comparison the picker offers can also
        // be made from a probe run — which is the only way to put a number on it that anyone can check.
        if (Environment.GetEnvironmentVariable("AVA3D_THREADS") == "0")
            _view.SoftwareThreading = false;

        // Dismissed by clicking it. Nothing else in the demo is modal and this should not be either: it is
        // an explanation, and an explanation you have read is one you should be able to put away. The
        // button inside it is safe from this — Avalonia's Button marks the press handled, so it never
        // reaches the border — which is the difference between an action and a dismissal.
        _engineNotice.PointerPressed += (_, _) => _engineNotice.IsVisible = false;
        _noticeAction.Click += (_, _) => AskToRestart(_noticeOption);

        // The dialog is the opposite: modal, and the only way past it is to answer it. Escape and a click
        // on the dim part are both "not now", which is the safe answer — the destructive one is a button
        // that says what it does.
        _restartYes.Click += (_, _) => ConfirmRestart();
        _restartNo.Click += (_, _) => CloseAsk();
        _restartAsk.PointerPressed += (_, e) =>
        {
            if (ReferenceEquals(e.Source, _restartAsk))
                CloseAsk();
        };

        _autoToggle.IsCheckedChanged += (_, _) => UpdateAuto();
        _engineList.SelectionChanged += (_, _) => OnEngineSelected();

        _view.Picked += OnPicked;
        _view.PropertyChanged += (_, e) =>
        {
            if (e.Property == Ava3DView.InfoProperty)
                ShowInfo(_view.Info);
        };

        // AVA3D_SCENE picks a starting scene by index or by name, so a probe run can land on the one it
        // wants to measure without anyone clicking.
        _sceneList.SelectedIndex = StartingScene();

        // Belt and braces: the assignment above raises SelectionChanged only when it changes something,
        // and a demo that opens on an empty viewport because the index was already right is a silly way
        // to lose an afternoon.
        Want(_sceneList.SelectedIndex);

        _autoToggle.IsChecked = Environment.GetEnvironmentVariable("AVA3D_TOUR") == "1";

        // There was no TopLevel to ask for frames from when the first scene was selected.
        //
        // This used to also measure the window and collapse the notes on anything small, because the notes
        // are three or four paragraphs and on a phone they were the whole screen. They now start closed
        // everywhere, so the rule has nothing left to decide.
        AttachedToVisualTree += (_, _) =>
        {
            RequestFrame();

            // The walk keys, taken at the top level and on the way down. See OnWalkKey.
            if (TopLevel.GetTopLevel(this) is { } top)
            {
                top.AddHandler(KeyDownEvent, OnWalkKey, RoutingStrategies.Tunnel);
                top.AddHandler(KeyUpEvent, OnWalkKey, RoutingStrategies.Tunnel);
            }
        };

        // Once warm, say what happened. This is how the browser, Android and iOS heads get verified.
        //
        // The self-test goes out with it, on every head, unasked. It is about two hundredths of a second
        // and it is the only way to compare two browsers on anything but screenshots: the software
        // renderer speckles wrongly-coloured triangles across the Safari build and draws the same
        // WebAssembly correctly in Chrome, and a picture of that is not evidence anybody can act on. Four
        // checksums over fixed inputs are. Open the page in both, copy both blocks, diff — the first line
        // that disagrees is the stage the fault is in. See Ava3D.SelfTest.
        DispatcherTimer.RunOnce(
            () =>
            {
                Console.WriteLine(Describe(LastInfo));
                Console.WriteLine(SelfTest.Run());
            },
            TimeSpan.FromSeconds(12));
    }

    /// <summary>
    /// AVA3D_STORY_AT=&lt;seconds&gt; opens the film at that second whatever the picker's cue says.
    ///
    /// The cues are the moments features are on screen, which is the right thing for a viewer and the
    /// wrong thing for checking a chapter: most of a chapter is the walk between the cues, and a capture
    /// can only be taken forwards from where the film was started. This is how a frame of the corridor, or
    /// of a hand-over, gets looked at at all.
    /// </summary>
    private static float? StoryAt =>
        float.TryParse(Environment.GetEnvironmentVariable("AVA3D_STORY_AT"), out var at) && at >= 0f
            ? at
            : null;

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

    /// <summary>
    /// One animation frame: advance the scene, hand it the camera, and show whatever it has to say.
    ///
    /// <paramref name="now"/> is the compositor's frame clock, and both halves of that matter.
    ///
    /// It is the <i>compositor's</i>, so a frame is produced for every frame that will be drawn, instead
    /// of by a timer running at whatever rate it was asked for. This used to be a 16 ms
    /// <c>DispatcherTimer</c> at background priority feeding a renderer running at 118 fps: the scene
    /// advanced about sixty-two times a second, was drawn about a hundred and eighteen, and the two had
    /// nothing to do with each other. Roughly every other drawn frame therefore showed the same instant
    /// as the one before it, in a pattern that drifted — which is exactly what "everything jitters up and
    /// down" looks like, and no amount of smoothing inside the scene can fix it, because the scene was
    /// already producing the right answer for the wrong times.
    ///
    /// And it is a <i>clock</i> rather than <c>DateTime.UtcNow</c> sampled inside the callback. A film
    /// that is a pure function of time is only smooth if the time it is asked for is the time the frame
    /// will be shown at; reading the wall clock at the top of a callback that runs behind input handling
    /// and layout measures when the timer got round to us, which is not the same thing and is not even
    /// monotonic in its spacing.
    ///
    /// The camera is pushed to the renderer only for a scene that says it drives one. Every other scene
    /// leaves the camera entirely to mouse input, and a message per frame telling the render thread that
    /// nothing moved is a message worth not sending.
    /// </summary>
    private void Frame(TimeSpan now)
    {
        if (!_animating)
            return;

        if (!_haveStart)
        {
            _sceneStarted = now;
            _haveStart = true;
        }

        _current.Update(_currentScene, _view.Camera, (now - _sceneStarted).TotalSeconds);

        if (_current.DrivesCamera)
            _view.InvalidateCamera();

        HandOver(_current.WantsControl);

        ShowCaption(_current.Caption, now.TotalSeconds);
        RequestFrame();
    }

    /// <summary>
    /// Asks for the next frame. Re-armed from inside <see cref="Frame"/> rather than repeating, because
    /// that is what stops frames queueing up behind a slow one.
    /// </summary>
    private void RequestFrame()
    {
        if (_animating && TopLevel.GetTopLevel(this) is { } top)
            top.RequestAnimationFrame(Frame);
    }

    /// <summary>Starts or stops the frame loop, and restarts the current scene's clock when it starts.</summary>
    private void Animate(bool on)
    {
        if (on && !_animating)
        {
            _animating = true;
            RequestFrame();
            return;
        }

        _animating = on;
    }

    /// <summary>
    /// Hands the caption and the frame clock to the panel, every frame.
    ///
    /// Every frame rather than on change, because the panel animates: it fades, it scans its text in
    /// behind a caret, its scanlines crawl and its signal tears. It is given the compositor's clock
    /// reading — the same one the scene is advanced with — so that what it does is locked to what is
    /// being drawn rather than to a timer of its own. The change detection that used to live here is
    /// down to the one thing that still needs it, which is not writing a line to the console sixty times
    /// a second.
    /// </summary>
    private void ShowCaption(string? text, double seconds)
    {
        if (text != _shownCaption)
        {
            _shownCaption = text;

            if (Environment.GetEnvironmentVariable("AVA3D_CAPTIONS") == "1")
                Console.WriteLine($"[caption] {(text ?? "(cleared)")}");
        }

        _captionBand.Tick(text, seconds);
    }

    private void Step(int delta)
    {
        var next = (_sceneList.SelectedIndex + delta + _catalog.Count) % _catalog.Count;
        _sceneList.SelectedIndex = next;
    }

    /// <summary>
    /// The story switch: remember it, and show the entry you are already on the other way.
    ///
    /// Keeping the selection is the whole point of one list. Somebody watching the colonnade who wants to
    /// see what the lighting scene looks like on its own should get exactly that, not be returned to the
    /// top — and somebody reading a scene file who wants to see where it lives should arrive at it in the
    /// building.
    /// </summary>
    private void OnStoryToggled()
    {
        DemoSettings.Story = _storyToggle.IsChecked == true;

        // Turning it on is asking to watch the film, so it goes to the top — and the list goes with it,
        // because a picker reading "38. Contact" over the opening shot is the film and the label
        // disagreeing about where you are. Turning it off keeps the selection, which is what the
        // paragraph above is about and is unchanged.
        if (_storyToggle.IsChecked == true)
        {
            _storyFromTop = true;

            // Assigning the index raises SelectionChanged, which rebuilds — but only if it changed
            // something, so the case where the film was already on entry one needs the call anyway.
            if (_sceneList.SelectedIndex == 0)
                Rebuild();
            else
                _sceneList.SelectedIndex = 0;

            return;
        }

        Rebuild();
    }

    /// <summary>
    /// The sound switch. Opens or closes the device on the film that is running.
    ///
    /// Closing rather than muting is what makes "off" mean the audio device is not open at all, which is
    /// the only version of off that is true on a laptop — a muted device still holds the output and still
    /// wakes its thread fifty times a second. That part was always right.
    ///
    /// What was wrong was the way it got there: this rebuilt the film, and the film is the most expensive
    /// build in the demo. Three thousand four hundred nodes, three glTF models and a starfield, measured at
    /// 215 ms in release and 880 ms in debug, on the UI thread, so the window stopped answering for the
    /// length of it — reported from the outside as the toolbar freezing for two or three seconds. And it
    /// restarted the film at the selected entry's cue, so hearing the room you were in moved you out of it.
    ///
    /// The switch is only enabled next to the film, so there is nothing to do when anything else is on
    /// screen: no other scene has a sound to open. See <see cref="StoryScene.Sound"/>.
    /// </summary>
    private void OnSoundToggled()
    {
        // The switch's own value, not the setting read back. AVA3D_SOUND wins over the settings file by
        // design — a measured run takes its configuration from its command line — and reading through it
        // here would mean that in a run started with the switch forced on, clicking the toolbar off turned
        // the sound off and immediately back on again. What somebody just clicked is not a setting to be
        // resolved; it is the answer.
        var on = _soundToggle.IsChecked == true;
        DemoSettings.Sound = on;

        if (_current is StoryScene film)
            film.Sound(on);
    }

    /// <summary>
    /// Builds the current entry again, when what it should build has changed rather than which entry it is.
    ///
    /// <see cref="Select"/> refuses to rebuild what is already shown, which is right — it is what stops
    /// the picker paying for a scene twice — so the way to ask for the same index again is to say that
    /// nothing is shown.
    /// </summary>
    private void Rebuild()
    {
        if (_wanted < 0)
            return;

        var index = _wanted;

        _shown = -1;
        _onScreen.Restart();
        Select(index);
    }

    /// <summary>
    /// Takes a selection from the picker or the steppers: acknowledges it now, builds it in a moment.
    ///
    /// Building a scene is a mesh or two, sometimes a procedurally generated texture, and a fresh upload
    /// of all of it — measured at 30 ms for Textures, 40 for Contact and 77 for Full PBR on this machine.
    /// That is one hitch and nobody minds one hitch. The problem was that walking the list with the
    /// arrows built every scene it passed <i>through</i>: stepping from the first to the last paid for
    /// all twenty-six, about a quarter of a second of building plus twenty-six rounds of uploading
    /// textures that were thrown away before they were ever drawn.
    ///
    /// So the title, the notes and the clock move immediately — that is what makes a click feel answered
    /// — and the build is posted once and re-pointed at whatever the latest selection is by the time it
    /// runs. Holding the arrow down now costs exactly one build: the scene you stopped on.
    /// </summary>
    private void Want(int index)
    {
        if (index < 0 || index >= _catalog.Count || index == _wanted)
            return;

        _wanted = index;
        _onScreen.Restart();

        _sceneTitle.Text = $"{index + 1}/{_catalog.Count}  ·  {_catalog[index].Title}";
        _sceneNotes.Text = Reflow(_catalog[index].Described.Notes);
        Dress(_catalog[index].Described.Look);

        // The first one is built on the spot. There is no popup to get out of the way of at start-up,
        // and an empty window while a posted job waits its turn is worse than the build.
        if (_shown < 0)
        {
            Select(index);
            return;
        }

        // Background is below the frame loop's own priority, which is the right way round — but only
        // because the loop leaves the queue empty. Measured with a probe posted at Input priority while
        // Contact ran at 120 fps: mean 0.0 ms, so nothing here is waiting behind rendering.
        Dispatcher.UIThread.Post(
            () => Select(_wanted), DispatcherPriority.Background);
    }

    private void Select(int index)
    {
        if (index < 0 || index >= _catalog.Count || index == _shown)
            return;

        _shown = index;
        ClearHighlight();

        // A fresh instance, so a scene's animation state cannot survive being revisited — and, with the
        // story on, so that the film is rebuilt at the second this entry happens rather than played from
        // the start. Rebuilding rather than seeking a running film is the honest way round: the walk is a
        // pure function of time, so there is no state to wind forward, and building the rooms is boxes.
        //
        // An entry the film has not reached yet falls back to its own scene even with the story on. That
        // is the demo being half-built rather than a mode that half-works, and it goes away as the rooms
        // land — see Contents.
        var entry = _catalog[index];

        // Whatever was on screen is let go of before the next thing is built, not after. A scene holding
        // something that is still running — the film's audio device is the one — would otherwise overlap
        // with its own replacement for the length of a build. See DemoScene.Retire.
        if (_current is not null)
            _current.Retire();

        // From the top when the film is being entered, from this entry's cue when it is being navigated.
        //
        // Those are two different acts and the cue is only right for one of them. Picking a scene with the
        // story already on is asking where that scene lives in the building, and the answer is the second it
        // happens. Opening the demo, or turning the switch on, is asking to watch the film — and a film that
        // opens three quarters of the way through has given away the one thing it is built around, which in
        // this case is a reveal nine minutes in the making.
        //
        // It mattered most where it was least visible: the landing page frames the demo with ?scene=Contact,
        // so the public film opened on its own climax. A deep link into a moment is ?at= and always has been.
        _current = _storyToggle.IsChecked == true && entry.Cue is { } cue
            ? new StoryScene(StoryAt ?? (_storyFromTop ? 0f : cue))
            : entry.Standalone();

        _storyFromTop = false;

        // Only the film has a soundtrack, so the switch is dead next to a standalone scene. Disabled rather
        // than hidden: a control that vanishes when you change something else is a control people stop
        // trusting, and this one still shows what it will do when the story goes back on.
        _soundToggle.IsEnabled = _current is StoryScene;

        // And the scene list is dead while the film is on.
        //
        // It used to jump the film to the picked entry's cue, which reads well written down and badly in
        // use: every one of those jumps rebuilds the building and throws away wherever the visitor had got
        // to, and there is nothing in the toolbar that says so before it happens. The switch beside it is
        // the honest control — turn the film off and the list is a list of scenes again.
        //
        // The steppers go with it. They are the same selection by another route, and a control that still
        // works when the thing next to it does not is a control nobody trusts.
        var filming = _current is StoryScene;
        _sceneList.IsEnabled = !filming;
        _prevButton.IsEnabled = !filming;
        _nextButton.IsEnabled = !filming;

        _currentScene = _current.Build();

        // The clock restarts on the next frame rather than now: `now` is the compositor's reading and
        // there is no honest way to take one from here.
        _haveStart = false;

        // Everything a scene did not ask for goes back to what a fresh Camera would have. Only about half
        // the scenes set a field of view or a clip range, so the other half inherit whatever the last one
        // left — and Contact leaves the worst of it: a near plane at 5 and a far plane three million out,
        // because its sun is nine hundred thousand units away. Wrap the tour from the film to Hello cube
        // and AutoFit puts the camera 4.6 units from a one-unit box that is not drawn nearer than 5, so
        // the front half is clipped off and the cube is seen from the inside. Roll is the same failure a
        // scene switch away: the film's shots leave it at up to forty degrees. So are the angles — a scene
        // that names none was being shown from wherever the last shot happened to end, which is why the
        // same cube looked different depending on which second of the film you left.
        //
        // This runs before Frame, so a scene that does name an angle still wins; a scene that does not
        // gets Camera's own 0.7 and 0.35, which is the three-quarter view it was written for. Reset here
        // rather than in each scene's Frame, because "the camera a scene is handed is the one the control
        // would have made" is a property of switching scenes, not something twenty-six scenes should each
        // have to remember.
        var camera = _view.Camera;
        camera.Yaw = CameraDefaults.Yaw;
        camera.Pitch = CameraDefaults.Pitch;
        camera.Roll = CameraDefaults.Roll;
        camera.FieldOfView = CameraDefaults.FieldOfView;
        camera.NearPlane = CameraDefaults.NearPlane;
        camera.FarPlane = CameraDefaults.FarPlane;

        // Angle first, then the scene: AutoFit runs when the first snapshot arrives and sets only the
        // target and distance, so a scene's chosen yaw and pitch survive being framed.
        _view.AutoFit = !_current.FramesItself;
        _current.Frame(_view.Camera);

        // AVA3D_BAKE=<yaw> takes the scene as it is and looks straight down at it, which is the one
        // camera no scene chooses for itself and the one a screen wants: a plan, with no perspective to
        // speak of and nothing standing out of the picture. It is how Assets/screen-*.png are made — see
        // Story.EngineRoom.Picture — and it is a switch rather than a tool because the scene, its lights
        // and its background are already right here, and only the camera is wrong.
        //
        // Eight degrees rather than an orthographic projection, which this control does not offer: at the
        // distance AutoFit then chooses, a component ten millimetres tall leans by about a pixel.
        if (Environment.GetEnvironmentVariable("AVA3D_BAKE")?.Split(',') is { Length: > 0 } bake)
        {
            _view.AutoFit = true;
            camera.Roll = 0f;

            if (bake.Length > 0 && float.TryParse(bake[0], out var yaw))
                camera.Yaw = yaw;

            camera.Pitch = bake.Length > 1 && float.TryParse(bake[1], out var pitch) ? pitch : -85f;

            if (bake.Length > 2 && float.TryParse(bake[2], out var fov))
                camera.FieldOfView = fov;
        }
        _view.Scene = _currentScene;
        _view.IsPickingEnabled = _current.WantsPicking;
        _view.InvalidateCamera();

        // The only interaction left in the demo, so the line only appears where there is one.
        _probe.Pick(_current.WantsPicking ? "Click an object" : null);

        _shownCaption = null;
        _captionBand.Reset();

        Animate(_current.Animates);
    }

    /// <summary>
    /// Puts the notes panel in the clothes this scene asked for.
    ///
    /// Style classes rather than colours set from here: the palettes belong in the XAML next to each
    /// other, where they can be compared, and this method's whole job is to know which one is on.
    /// </summary>
    private void Dress(SceneLook look)
    {
        _notesPanel.Classes.Set("blueprint", look == SceneLook.Blueprint);
        _notesPanel.Classes.Set("studio", look == SceneLook.Studio);
        _notesPanel.Classes.Set("cosmic", look == SceneLook.Cosmic);
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

    /// <summary>
    /// Starts or stops the automatic advance.
    ///
    /// One timer that ticks four times a second and asks whether the scene is done, rather than one
    /// alarm per scene set to go off at the right moment. Two reasons, and both of them are the same
    /// reason: the answer is allowed to change while the scene is on screen. It changes when the picker
    /// is open — a list that moves the selection out from under the pointer is worse than no automation
    /// at all — and it changes when a scene is still being built. An alarm cannot be asked to reconsider;
    /// a tick reconsiders every time.
    /// </summary>
    private void UpdateAuto()
    {
        if (_autoToggle.IsChecked != true)
        {
            _autoTimer?.Stop();
            _autoTimer = null;
            return;
        }

        if (_autoTimer != null)
            return;

        _autoTimer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(250), DispatcherPriority.Background, (_, _) => AutoTick());

        _autoTimer.Start();
    }

    /// <summary>
    /// Moves on when this scene has had its time.
    ///
    /// What "its time" means is the scene's own business, which is the point of
    /// <see cref="DemoScene.TourDuration"/>: nine seconds is enough to watch a turntable go round, and a
    /// scene with a script says how long its script is — Contact's is sixty-two, so Auto sits through the
    /// whole film and moves on as the sun comes back over the limb rather than cutting away mid-dogfight.
    /// </summary>
    private void AutoTick()
    {
        // Not while either picker is open, and not while the selection is still catching up with itself.
        //
        // Both pickers, not just the scene one. Advancing builds a scene, which is tens of milliseconds
        // on the thread that also delivers input — and a list that the viewer has open is a list they are
        // reading, whichever of the two it is.
        if (_current is null || _sceneList.IsDropDownOpen || _engineList.IsDropDownOpen || _wanted != _shown)
            return;

        // And never once the scene has been handed over.
        //
        // The film ends by giving the building to the visitor, and a tour that takes it back is a tour
        // interrupting the thing it was showing them. It did: forty-five seconds after the hand-over, or
        // forty-five seconds after they last touched a key, whichever came later — and standing still to
        // look at something is not a signal that you have finished.
        //
        // There is no timeout that is right here. Someone reading a console in the engine room and someone
        // who has walked away from the machine look identical from in here, and only one of those wants a
        // scene change. So the tour stops instead, which is what a tour does when the visitor takes over,
        // and Auto is a switch anybody can turn off if they want the scenes to keep moving.
        if (_current.WantsControl)
            return;

        if (_onScreen.Elapsed >= _current.TourDuration)
            Step(1);
    }

    /// <summary>
    /// Applies a picker selection.
    ///
    /// Four outcomes, and the difference between them is the whole reason this control is interesting.
    /// Software is a live switch and takes effect on the next frame; so is the GPU API this process
    /// already holds, because moving between the GPU and the CPU is the control's own decision. The
    /// <i>other</i> GPU API is not reachable from inside this process — Avalonia fixes that at
    /// AppBuilder time — so a desktop head offers to relaunch itself and every other head says so. And a
    /// backend this platform simply does not have says which platforms do.
    ///
    /// Every one of those four ends in something the reader can see. The last two used to end in nothing:
    /// selecting an unavailable backend set <see cref="Ava3DView.PreferredBackend"/> to something the
    /// control correctly ignored, so the picker moved, the picture did not, and nothing said why.
    ///
    /// All four are remembered, including the two that cannot take effect here. A renderer is not a
    /// per-session whim — somebody comparing Metal against OpenGL is doing it over an afternoon, and a
    /// picker that forgets on every launch makes them re-answer the question every time. Asking for one
    /// this machine does not have is still an answer worth keeping: it costs nothing, because a request
    /// for a GPU API the process does not hold leaves the picture exactly as it was, and it is undone by
    /// choosing something else. What it buys is a picker that shows what you chose rather than quietly
    /// dropping it, and a notice that keeps explaining why until you do choose something else.
    /// </summary>
    private void OnEngineSelected()
    {
        if (_suppressEngineChange || _engineList.SelectedItem is not EngineChoice choice)
            return;

        // Whatever was chosen is what was chosen. Saved before the outcome is worked out, because the two
        // outcomes that cannot take effect here are still answers: one of them relaunches into a process
        // that reads this on the way up, and the other is a request this machine cannot meet today.
        DemoSettings.Engine = choice.Kind;
        _view.SoftwareThreading = choice.Threading;
        _view.PreferredBackend = choice.Kind;

        switch (choice.Option?.Availability)
        {
            // Automatic, or a renderer this process can reach. Live, next frame.
            case null:
            case BackendAvailability.Active:
            case BackendAvailability.Available:
                Say(null);
                Notice(null);
                return;

            // Reachable, but not by this process. Asked rather than done: see AskToRestart.
            case BackendAvailability.RequiresRestart
                when EngineRelauncher.Current is { CanRelaunch: true } or { CanQuit: true }:
                Notice(choice.Option);
                AskToRestart(choice.Option);
                return;

            case BackendAvailability.RequiresRestart:
                Say(EngineRelauncher.Describe(choice.Kind, choice.Option));
                Notice(choice.Option);
                return;

            default:
                Say($"{choice.Option.Name} cannot run here — {choice.Option.Reason}.");
                Notice(choice.Option);
                return;
        }
    }

    /// <summary>
    /// Puts a line in the panel of numbers.
    ///
    /// This used to force the panel open, because it was the only place the demo said why a renderer you
    /// selected was not the one drawing, and a message written to a shut panel is the silence it was
    /// written to fix. <see cref="Notice"/> now carries that message on the picture, where somebody who
    /// closed the instruments still sees it — so this went back to being the log, and the log does not get
    /// to open panels the viewer shut.
    /// </summary>
    private void Say(string? text) => _probe.Note(text);

    /// <summary>
    /// Says on the picture that the engine being asked for is not the one drawing, and why. Null takes the
    /// message away.
    ///
    /// Two pieces of writing, and they are different jobs. The title is the sentence somebody would say
    /// out loud — "Metal can't be used on this platform" — and has to land from across the room. The
    /// reason underneath is the library's own, verbatim, and is there to be read once and answer the
    /// question for good. Neither is invented here: the reason comes from the backend catalogue, which is
    /// where the fact lives, and the half about restarting comes from the head, which is the only thing
    /// that knows whether restarting is something it can do.
    /// </summary>
    private void Notice(BackendOption? option)
    {
        _noticeOption = option;

        if (option is null)
        {
            _engineNotice.IsVisible = false;
            return;
        }

        _noticeTitle.Text = EngineRelauncher.Headline(option);
        _noticeReason.Text = $"Reason: {EngineRelauncher.Explain(option)}";
        _noticeAction.IsVisible = option.Availability == BackendAvailability.RequiresRestart &&
                                  EngineRelauncher.Current is { CanRelaunch: true };
        _engineNotice.IsVisible = true;
    }

    /// <summary>
    /// Asks whether to end this process to get <paramref name="option"/>'s renderer.
    ///
    /// Reached two ways — choosing a renderer that needs a restart, and pressing the notice's button when
    /// a remembered one did not take. Neither does anything on its own any more. The demo used to relaunch
    /// on the selection change itself, which is the shortest possible route from "I wonder what OpenGL
    /// looks like" to a window that is not there, and no amount of a replacement arriving a second later
    /// makes that a thing a drop-down should do.
    ///
    /// A head that can do neither — a browser tab, a phone — is left with the notice, which is the whole
    /// of what it can offer. Asking a question whose only honest answer is "nothing will happen" is worse
    /// than not asking.
    /// </summary>
    private void AskToRestart(BackendOption? option)
    {
        if (option is null || EngineRelauncher.Current is not ({ CanRelaunch: true } or { CanQuit: true }))
            return;

        _askOption = option;
        ShowAsk(option, failure: null);
    }

    /// <summary>Puts the question on screen, in whichever of its two shapes this head can honour.</summary>
    private void ShowAsk(BackendOption option, string? failure)
    {
        var (question, detail, yes) = EngineRelauncher.Ask(option, failure);

        _askCloses = failure is not null || EngineRelauncher.Current is not { CanRelaunch: true };
        _restartQuestion.Text = question;
        _restartDetail.Text = detail;
        _restartYes.Content = yes;
        _restartAsk.IsVisible = true;

        // So the answer is a keystroke as well as a click, and so Escape has somewhere to bubble from.
        _restartYes.Focus();
    }

    /// <summary>
    /// Yes: start the replacement and go, or — when there is no replacement to be had — just go.
    ///
    /// The choice is written again here as well as when the picker moved, because this is the line the
    /// replacement reads on its way up and it is worth being certain of. The relauncher also puts it in
    /// the environment, so a restart lands on the renderer that was asked for even on a machine where the
    /// settings file cannot be written.
    /// </summary>
    private void ConfirmRestart()
    {
        if (_askOption is not { } option || EngineRelauncher.Current is not { } host)
        {
            CloseAsk();
            return;
        }

        DemoSettings.Engine = option.Kind;

        if (!_askCloses && host.CanRelaunch)
        {
            Say($"Restarting for {option.Kind}…");

            if (host.Relaunch(option.Kind, out var failure))
                return;

            // Nothing started, and this window is still here. Ask the question that is left rather than
            // closing on the strength of a button that promised something else.
            Say($"Could not restart: {failure}");
            ShowAsk(option, failure ?? "the replacement could not be started");
            return;
        }

        if (host.CanQuit)
            host.Quit();
    }

    /// <summary>Not now. The notice stays behind it, still saying which renderer is not the one drawing.</summary>
    private void CloseAsk()
    {
        _restartAsk.IsVisible = false;
        _askOption = null;
    }

    /// <summary>Escape answers the confirmation, and only the confirmation.</summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (_restartAsk.IsVisible && e.Key == Key.Escape)
        {
            CloseAsk();
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    /// <summary>
    /// Turns the viewer's controls on and off as the scene asks for them.
    ///
    /// Three things at once, and the third is the one that is easy to forget: the view's orbit, the two
    /// step buttons, and the steering itself, which has to be zeroed on the way out. A scene that takes the
    /// controls back while a key is down would otherwise be handed a direction for ever, by a key nobody is
    /// pressing any more.
    ///
    /// The demo turns orbit off for every other scene on purpose — see where it does, in the constructor —
    /// so this is the one place in the application that turns it back on, and it does it for exactly as
    /// long as somebody has been given something to look around.
    /// </summary>
    private void HandOver(bool wanted)
    {
        if (wanted == _handedOver)
            return;

        _handedOver = wanted;

        _view.IsOrbitEnabled = wanted;
        _stepPad.IsVisible = wanted;

        // And the caption gets out of their way. Both live at the bottom of the window and both are pinned
        // to it, so the two circles landed on top of the last word of every line the film says — which is
        // the one moment in the demo where the caption is an instruction about those very buttons. The band
        // is stretched, so the fix is to stop stretching it that far: a hundred and fifty is the pad's own
        // width plus its margin, taken back only while the pad is up.
        _captionBand.Margin = wanted ? new Thickness(0, 0, 150, 14) : new Thickness(0, 0, 0, 14);

        _keys = Vector2.Zero;
        _taps = Vector2.Zero;
        _current.Steer(Vector2.Zero);
    }

    /// <summary>
    /// W A S D and the arrows, held.
    ///
    /// Bound on the top level rather than on this control, and tunnelling rather than bubbling, because a
    /// key press has to reach the walk whatever happens to have the focus — and in this demo that is
    /// usually the scene picker, which eats the arrows to change its selection. Nothing here types, so
    /// taking the keys first costs nothing.
    /// </summary>
    private void OnWalkKey(object? sender, KeyEventArgs e)
    {
        if (!_handedOver)
            return;

        var down = e.RoutedEvent == KeyDownEvent;

        switch (e.Key)
        {
            case Key.W or Key.Up: _keys.Y = down ? 1f : 0f; break;
            case Key.S or Key.Down: _keys.Y = down ? -1f : 0f; break;
            case Key.A or Key.Left: _keys.X = down ? -1f : 0f; break;
            case Key.D or Key.Right: _keys.X = down ? 1f : 0f; break;
            default: return;
        }

        e.Handled = true;
        Steer();
    }

    /// <summary>Press and hold on one of the two step buttons.</summary>
    private void OnStep(Border button, float forward)
    {
        button.PointerPressed += (_, e) =>
        {
            _taps.Y = forward;
            e.Pointer.Capture(button);
            Steer();
        };

        // Capture-lost as well as release, because a pointer dragged off the button or taken away by the
        // system never raises one — and a step that cannot be stopped is worse than one that never starts.
        button.PointerReleased += (_, _) =>
        {
            _taps.Y = 0f;
            Steer();
        };

        button.PointerCaptureLost += (_, _) =>
        {
            _taps.Y = 0f;
            Steer();
        };
    }

    private void Steer()
    {
        var move = _keys + _taps;

        _current.Steer(new Vector2(
            Math.Clamp(move.X, -1f, 1f),
            Math.Clamp(move.Y, -1f, 1f)));
    }

    /// <summary>
    /// Says something when the renderer that is drawing is not the one that was asked for.
    ///
    /// Which now happens without anybody touching the picker, because the choice is remembered: a settings
    /// file written on a Mac and opened on a Windows machine asks for Metal, a browser tab can be asked
    /// for a renderer the page was never given, and a GPU context that fails to come up leaves a perfectly
    /// good picture drawn by something other than what was requested. Every one of those used to arrive as
    /// silence — the scene drew, the picker said Metal, and Metal was not what drew it.
    ///
    /// Automatic cannot be wrong, by definition: it is the absence of a request.
    /// </summary>
    private void CheckPreference(RenderInfo info)
    {
        if (info.PreferredBackend == RenderBackendKind.Automatic ||
            info.PreferredBackend == info.ActiveBackend)
        {
            Notice(null);
            return;
        }

        if (info.AvailableBackends.FirstOrDefault(o => o.Kind == info.PreferredBackend) is { } option)
            Notice(option);
    }

    /// <summary>
    /// One row of the engine picker. <c>Option</c> is null for the Automatic entry.
    ///
    /// <c>Threading</c> is what makes the CPU renderer two rows instead of one. It is not a second backend —
    /// it is the same renderer with its vertex pass held to a single core — but it belongs in this list
    /// rather than in a settings panel, because the only way to understand what it does is to switch between
    /// them and watch the counter.
    /// </summary>
    private sealed record EngineChoice(
        RenderBackendKind Kind, string Label, BackendOption? Option, bool Threading = true)
    {
        public override string ToString() => Label;
    }

    /// <summary>
    /// Fills the picker: Automatic, then every renderer the library has, named and nothing else.
    ///
    /// The names used to carry their state — "Metal — running", "OpenGL — restarts the demo", "Metal — not
    /// here" — which put four different sentences in a box 215 points wide and made the list read like a
    /// status report you had to parse rather than a set of things you could choose. It is a list of
    /// renderers. Which one is drawing is the one that is selected, and the two cases where that is not the
    /// answer say so on the picture, in a sentence, with room to explain themselves.
    ///
    /// Every renderer on every platform, whether or not this one can run it: a picker that hides Metal on
    /// Windows invites the question of whether Metal exists, and one that shows it and answers when you
    /// choose it ends the question.
    ///
    /// The browser is the exception, and the reason is that the answer there is not actionable. On a
    /// desktop "not in this process" is an invitation — the demo offers to relaunch, and relaunching gets
    /// you the renderer. A tab cannot be relaunched into Metal at any point in the future by anybody, so
    /// offering the choice only to refuse it is a dead end dressed as a control. Anything the library
    /// reports as <see cref="BackendAvailability.Unavailable"/> is therefore left out here. What the page
    /// can actually run — WebGL 2, and the CPU renderer that needs no GPU at all — is the whole list.
    /// </summary>
    private void RebuildEngineList(RenderInfo info)
    {
        var choices = new List<EngineChoice>
        {
            new(RenderBackendKind.Automatic, "Automatic", null)
        };

        foreach (var option in info.AvailableBackends)
        {
            if (OperatingSystem.IsBrowser() && option.Availability == BackendAvailability.Unavailable)
                continue;

            // The CPU renderer is named by how many cores it is being given, because that is the only thing
            // about it anyone here is choosing between: CPU (×16) and CPU (×1) are one renderer with its
            // vertex pass shared or held to a single core. The count is whatever the runtime is offered —
            // the machine's on the desktop, the browser's in a tab, and those differ: Safari clamps it to
            // eight where Chrome hands over all sixteen. So the label is also the answer to what "all
            // cores" means here, which is the number the frame rate beside it has to be read against.
            //
            // One row where there is one core, because two rows would then be the same renderer under two
            // names. A browser tab used to be that case and no longer is: the head is published for
            // threads and its page arranges the isolation they need, so the choice is offered there too —
            // and it is the platform where the gap between the two rows is widest, because a WebAssembly
            // tab has no other way to use a second core at all.
            if (option.Kind == RenderBackendKind.Software && Environment.ProcessorCount > 1)
            {
                choices.Add(new EngineChoice(option.Kind, $"CPU (×{Environment.ProcessorCount})", option));
                choices.Add(new EngineChoice(option.Kind, "CPU (×1)", option, Threading: false));
                continue;
            }

            choices.Add(new EngineChoice(
                option.Kind,
                option.Kind == RenderBackendKind.Software ? "CPU (×1)" : option.Name,
                option));
        }

        _suppressEngineChange = true;
        try
        {
            _engineList.ItemsSource = choices;
            var wanted = info.PreferredBackend;
            var threading = _view.SoftwareThreading;

            // Both halves, then the kind alone. The second is not dead code: a single-core host lists one
            // CPU row and it is the shared one, so a process started with AVA3D_THREADS=0 there matches no
            // row on the first test — and falling back to Automatic would be the picker lying about which
            // renderer is drawing.
            var index = choices.FindIndex(c => c.Kind == wanted && c.Threading == threading);
            if (index < 0)
                index = choices.FindIndex(c => c.Kind == wanted);

            _engineList.SelectedIndex = Math.Max(0, index);
        }
        finally
        {
            _suppressEngineChange = false;
        }
    }

    private void OnPicked(object? sender, PickEventArgs e)
    {
        // A scene that knows what its objects mean gets the click first, and if it takes it the shell
        // does nothing at all — no tint of its own to undo, and its answer goes in the caption rather
        // than in the diagnostics line. See DemoScene.Picked for why the shell's default is not enough
        // for a model whose components are several nodes each.
        if (_current.Picked(_currentScene, e.Result))
            return;

        ClearHighlight();

        if (e.Result is not { } hit)
        {
            _probe.Pick("clicked nothing");
            return;
        }

        // Give the picked node its own material, so the highlight cannot leak to siblings sharing one.
        _highlighted = hit.Node;
        _originalMaterial = hit.Node.Material;

        var highlight = _originalMaterial.Clone();
        highlight.BaseColor = new Vector4(1f, 0.55f, 0.15f, 1f);
        highlight.EmissiveColor = new Vector3(0.20f, 0.07f, 0f);
        hit.Node.Material = highlight;

        _probe.Pick(
            $"{hit.Node.Name ?? "node"} · triangle {hit.TriangleIndex} · " +
            $"{hit.Distance:F2} away at ({hit.WorldPosition.X:F2}, {hit.WorldPosition.Y:F2}, {hit.WorldPosition.Z:F2})");
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
        _probe.Show(info);

        // The list depends on what the platform handed back, which is only known once a frame has been
        // drawn — so it is built from the first RenderInfo rather than in the constructor, and rebuilt
        // whenever what is running changes. That last part matters: dropping to the CPU and coming back
        // is a live decision, and a picker still marking OpenGL as "running" while Skia draws is lying.
        if (info.AvailableBackends.Count > 0 && info.ActiveBackend != _listedActive)
        {
            _listedActive = info.ActiveBackend;
            RebuildEngineList(info);
            CheckPreference(info);
        }
    }
}
