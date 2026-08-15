using Ava3D.Diagnostics;

namespace Ava3D.Demo.Story;

/// <summary>
/// Records the film to a numbered run of PNGs, one rendered frame at a time.
///
/// <b>Why this exists when a screen recorder exists.</b> The old way to get a video of this demo was
/// <c>tools/record-demo.sh</c>: run the application, point ffmpeg at the display, and film the screen for
/// as long as the thing lasts. That works, and everything about it is at the mercy of the machine. It needs
/// a granted Screen Recording permission, which is a dialog nobody can click from a script. It records
/// whatever else is on the display, at whatever rate the compositor managed, with a dropped frame
/// indistinguishable from a slow one. And it takes exactly as long as the film does — nine minutes and
/// forty-seven seconds of a screen nobody may touch.
///
/// This takes none of that from the machine. The film is a pure function of its clock, so the clock is
/// supplied here rather than read: frame <i>n</i> is the film at <c>from + n × speed / fps</c>, whatever the
/// wall clock says, and the renderer is asked for that frame and waited for. The run is therefore exactly as
/// regular as the arithmetic, a dropped frame is impossible rather than invisible, and it goes as fast as
/// the machine can draw instead of in real time.
///
/// <b>What it does not capture is the overlay.</b> <see cref="FrameCapture"/> is the control's own surface —
/// the 3D picture and nothing composited over it — so the caption band, the notes and the probe are not in
/// these frames. That is a loss worth taking and mostly not a loss at all: what a reel should show is the
/// picture, and the captions come back sharper burned in afterwards at whatever size the screen wants. So
/// the words are written out beside the frames, in <see cref="CaptionFile"/>, taken from the same property
/// the panel would have shown — see <c>MainView.Frame</c>, which hands them over as it goes.
///
/// The title card is in the frames, because it is geometry rather than an overlay. See <see cref="Curtain"/>,
/// which says why, and which decided that years before anything wanted to record this.
/// </summary>
internal sealed class Recorder
{
    /// <summary>Frames to draw and throw away before the first one is kept.
    ///
    /// Not superstition: the opening of the film is a room whose textures are still arriving on the first
    /// few frames, and <see cref="FrameCapture.Frame"/> defaults to 120 for exactly this reason. The film is
    /// held at its first second throughout, so what these frames buy is upload time and nothing else.</summary>
    private const int Warmup = 90;

    private readonly string _directory;
    private readonly float _fps;
    private readonly float _speed;
    private readonly float _from;
    private readonly float _to;
    private readonly List<string> _captions = [];

    private int _warmed;
    private int _index;
    private volatile bool _waiting;

    private Recorder(string directory, float fps, float speed, float from, float to)
    {
        _directory = directory;
        _fps = fps;
        _speed = speed;
        _from = from;
        _to = to;
    }

    /// <summary>
    /// A recorder if <c>AVA3D_FILM</c> asked for one, otherwise null.
    ///
    /// <c>AVA3D_FILM</c> is the directory to fill. <c>AVA3D_FILM_FPS</c> is the output rate,
    /// <c>AVA3D_FILM_SPEED</c> how many film seconds go into one of them, and <c>AVA3D_FILM_FROM</c> and
    /// <c>AVA3D_FILM_TO</c> narrow it to a stretch. The defaults record the whole film at nine times into
    /// thirty frames a second, which is the reel.
    /// </summary>
    public static Recorder? Open()
    {
        if (Environment.GetEnvironmentVariable("AVA3D_FILM") is not { Length: > 0 } directory)
            return null;

        var fps = Number("AVA3D_FILM_FPS", 30f);
        var speed = Number("AVA3D_FILM_SPEED", 9f);

        var recorder = new Recorder(
            directory,
            fps > 0f ? fps : 30f,
            speed > 0f ? speed : 9f,
            Math.Max(0f, Number("AVA3D_FILM_FROM", 0f)),
            Number("AVA3D_FILM_TO", 0f));

        Directory.CreateDirectory(directory);

        // This records the film, so it turns the film on, and neither of these is a convenience.
        //
        // A recording is a measured run — see DemoSettings.Measuring, which this switch is part of — and a
        // measured run deliberately ignores the settings file, so the story would otherwise arrive off and
        // the recorder would spend an afternoon filing away a rotating cube. That is the whole of what this
        // exists to record; there is no sense in which "the demo" is the alternative.
        //
        // And from the top, because the film's second is supplied here. StoryScene adds its start offset to
        // whatever it is handed — see StoryScene.Update — so a non-zero one would put every frame that far
        // past where this thinks it is. AVA3D_FILM_FROM is how a stretch is asked for; it is applied to the
        // clock this owns rather than to the scene's.
        Environment.SetEnvironmentVariable("AVA3D_STORY", "1");
        Environment.SetEnvironmentVariable("AVA3D_STORY_AT", "0");

        // Zero, not the default 120: every arm here is meant to take the very next frame, and a counter
        // still climbing to its default would silently eat the first two seconds of the film.
        FrameCapture.Frame = 0;
        FrameCapture.Captured += recorder.Wrote;

        return recorder;

        static float Number(string name, float fallback) =>
            float.TryParse(Environment.GetEnvironmentVariable(name), out var value) ? value : fallback;
    }

    /// <summary>Where the words went, beside the frames.</summary>
    public string CaptionFile => Path.Combine(_directory, "captions.tsv");

    /// <summary>The last second of film this will record — the film's own length unless narrowed.</summary>
    public float Until { get; private set; }

    /// <summary>Frames written so far.</summary>
    public int Count => _index;

    /// <summary>The film second frame <see cref="Count"/> is to be drawn at.</summary>
    public float Second => _from + _index * _speed / _fps;

    /// <summary>True once the film has been recorded up to <see cref="Until"/>.</summary>
    public bool AtEnd => Until > 0f && Second >= Until;

    /// <summary>
    /// How big to open the window, so the captured frames come out the size the reel wants.
    ///
    /// The capture is the control's own surface, which is the view's size in device pixels — the window's
    /// client area less the toolbar, times the display's scale. So the window is asked for in points and
    /// what lands on disk is that times two on any Apple display made this decade.
    ///
    /// 960 by 628 is what puts 1920×1080 on disk at that scale: the toolbar takes 88 points off the top,
    /// which leaves the view 960×540 — sixteen by nine exactly, doubled. It was measured rather than
    /// derived (620 gave 1064) and it is not trusted afterwards: a toolbar that rewrapped on some other
    /// machine would move it, so the encode crops to shape from whatever actually arrived.
    /// </summary>
    public static bool WantsWindow(out double width, out double height)
    {
        width = 960;
        height = 628;

        return Environment.GetEnvironmentVariable("AVA3D_FILM") is { Length: > 0 };
    }

    /// <summary>
    /// Whether the film may be moved on, or the last armed frame has still to be written.
    ///
    /// This is the lockstep, and it is the whole correctness of the run: the renderer draws on its own
    /// thread whenever it gets to it, so advancing the film on the next animation callback regardless would
    /// mean the frame that eventually lands is of whatever second the film had reached by then. Waiting for
    /// <see cref="FrameCapture.Captured"/> makes the run a conversation instead of a race.
    /// </summary>
    public bool Waiting => _waiting;

    /// <summary>Tells the film how long it is, once the scene has been built and can say.</summary>
    public void Measure(float duration) => Until = _to > _from ? Math.Min(_to, duration) : duration;

    /// <summary>Whether the opening frames have been drawn and thrown away yet. See <see cref="Warmup"/>.</summary>
    public bool Warm => _warmed >= Warmup;

    /// <summary>One more frame of warming.</summary>
    public void Warming() => _warmed++;

    /// <summary>
    /// Arms the next frame and remembers what the caption band would have been saying over it.
    ///
    /// Called after the film has been moved to <see cref="Second"/> and before the frame is asked for, so
    /// what the renderer draws next is what this is about to be handed.
    /// </summary>
    public void Arm(string? caption)
    {
        _captions.Add($"{_index}\t{Second:0.###}\t{caption?.Replace('\t', ' ') ?? ""}");
        _waiting = true;

#if !AVA3D_FROM_PACKAGE
        FrameCapture.Next(Path.Combine(_directory, $"frame-{_index:000000}.png"));
#endif
    }

    /// <summary>Writes the captions beside the frames and says what was recorded.</summary>
    public string Close()
    {
        FrameCapture.Captured -= Wrote;
        FrameCapture.Path = null;

        File.WriteAllLines(CaptionFile, _captions);

        var seconds = _index / _fps;

        return $"{_directory}: {_index} frames, {_from:0.#}–{Until:0.#} s of film at {_speed:0.##}× "
               + $"into {seconds:0.#} s at {_fps:0.#} fps";
    }

    /// <summary>The renderer, on its own thread, saying the frame is on disk.</summary>
    private void Wrote(string path)
    {
        _index++;
        _waiting = false;
    }
}
