using System;
using System.Numerics;
using Avalonia.Media;

namespace Ava3D.Demo.Scenes.Arcade;

/// <summary>
/// Something a game did. <b>Not a sound</b> — see <see cref="ArcadeScene.Moves"/>.
/// </summary>
public enum Move
{
    /// <summary>A character left the ground.</summary>
    Jump,

    /// <summary>And arrived back on it.</summary>
    Land,

    /// <summary>Went under something instead of over it.</summary>
    Duck,

    /// <summary>Something was picked up.</summary>
    Coin,

    /// <summary>Something that is chasing got close.</summary>
    Near,

    /// <summary>A piece came to rest.</summary>
    Drop,

    /// <summary>A row went. The weight is how many.</summary>
    Clear
}

/// <summary>
/// A game in a pixel buffer: a picture drawn a texel at a time, handed over as a texture, and shown on
/// something flat.
///
/// This folder is the third exception to one self-contained file per scene, and for the same reason
/// <c>Board/</c> is: four games that each write their own copy of "a grid of pixels, a sprite, and the
/// arithmetic that turns a clock into a texture" would be four copies of one idea, three of which go stale.
/// What is worth reading in <see cref="GrassBricksScene"/> is the game — the level, the jump, the palette —
/// and that is what is left in it.
///
/// <b>The picture is a pure function of the clock.</b> Nothing here integrates, accumulates or remembers a
/// frame: ask for two hundred seconds in and you get the picture at two hundred seconds without having
/// drawn the first one. That is not tidiness, it is the requirement that makes these mountable. The story
/// is a film you can seek, and a screen inside a room you have jumped into the middle of has to already be
/// mid-game — a television that starts from its title card every time the viewer scrubs the timeline is a
/// television nobody believes.
///
/// So these games play themselves, on a script, like an arcade cabinet left alone in a corner. A jump is a
/// parabola of the clock rather than a velocity; a stack of blocks is a recording, replayed. Nothing is
/// playable and nothing needs to be — the demo is showing you a screen with a game on it, and the screen is
/// the subject.
///
/// <b>Standalone it is the whole scene.</b> One quad, filling the frame, against black — the picture and
/// nothing else, which is what somebody who opened this file wanted to see. Mounted, the same object hands
/// the same pictures to a television's face in a dark room and never learns that it did. See
/// <see cref="Show"/>: the story calls it with the film's clock, the reference shell calls it with the time
/// since the scene was selected, and the two are the same call.
/// </summary>
public abstract class ArcadeScene : DemoScene
{
    /// <summary>Which picture is on screen, or nothing before the first one.</summary>
    private int _tick = int.MinValue;

    /// <summary>The one buffer and the one texture over it, both made on the first <see cref="Show"/>.</summary>
    private PixelCanvas? _canvas;

    private Texture? _picture;

    private Material _standalone = null!;

    /// <summary>Pixels across. Small on purpose — the whole look is that you can count them.</summary>
    protected virtual int Columns => 128;

    /// <summary>Pixels down. Four to three, because the thing this ends up on is a television.</summary>
    protected virtual int Rows => 96;

    /// <summary>
    /// Pictures a second.
    ///
    /// Ten, which is slower than anything renders and is the point: motion lands on a grid of tenths, so a
    /// character crosses the screen in visible steps rather than gliding. Chunky pixels moving smoothly
    /// look like a modern game wearing a costume. The other half of it is arithmetic — a screen redrawn
    /// every frame is a texture built, uploaded and thrown away every frame, four times over once there
    /// are four televisions.
    /// </summary>
    protected virtual float Rate => 10f;

    /// <summary>How long the game takes to come back to where it started. Auto holds for one loop.</summary>
    protected abstract float Loop { get; }

    /// <summary>
    /// Draws the game at this moment. Called with a time already rounded down to a whole picture, so a
    /// position computed from it lands on the same texel every time that picture is asked for.
    /// </summary>
    protected abstract void Paint(PixelCanvas screen, float seconds);

    /// <summary>
    /// Points a material at the picture for this moment, if it is not already showing it.
    ///
    /// This is the mount. Whoever is showing the game owns the surface — a quad on a black background here,
    /// the glass of a television in the story — and hands it over to be filled in.
    /// </summary>
    /// <param name="screen">The material to put the picture on. Should be unlit; see <see cref="Glass"/>.</param>
    /// <param name="seconds">The clock. Any value, in any order: the picture depends on nothing else.</param>
    /// <returns>True when the picture changed, so a caller can invalidate only when there is a reason to.</returns>
    /// <remarks>
    /// One instance drives one material, because the buffer and the picture on screen are remembered per
    /// instance. Four televisions are four games, which is what they are anyway.
    /// </remarks>
    public bool Show(Material screen, float seconds)
    {
        var tick = (int)MathF.Floor(seconds * Rate);
        if (tick == _tick)
            return false;

        _tick = tick;

        if (_canvas is null)
        {
            _canvas = new PixelCanvas(Columns, Rows);
            _picture = _canvas.Freeze(Title);
        }

        Paint(_canvas, tick / Rate);

        // One buffer and one texture, rewritten — not a new texture ten times a second. A texture is cached
        // by identity, so a fresh instance is one nothing has uploaded yet, and until its turn on the
        // upload queue comes round it draws as flat white: a screen that flashes six times a second. See
        // Texture.Refresh, which is the whole of the answer.
        _picture!.Refresh();

        screen.BaseColorTexture = _picture;
        screen.EmissiveTexture = _picture;

        // Switched on. Until the first picture the glass is dark, and the texture is multiplied by the base
        // colour — so leaving it dark would show the game through a filter that takes it back to black.
        screen.BaseColor = Vector4.One;
        screen.EmissiveColor = Vector3.One;

        return true;
    }

    /// <summary>
    /// Everything this game did between two moments of its own clock.
    ///
    /// <b>A game reports moves and never sounds.</b> It says the runner left the ground, not that a rising
    /// square wave should play — because a game in this folder is not allowed to know that anything is
    /// listening. The story mounts these on televisions and gives each cabinet a voice; the reference shell
    /// mounts the same object with nothing attached and it stays silent. Whoever is listening owns the
    /// mapping, which is the same division <see cref="Show"/> makes for the picture: the game fills in a
    /// surface it was handed and never learns what the surface was.
    ///
    /// <b>It takes a window rather than a moment, and that is what makes it exact.</b> Every game here is
    /// already a pure function of its clock, so what happened between two clocks is arithmetic on those two
    /// numbers — no counters, no last-frame state, nothing to resynchronise after a seek. A jump is not
    /// "played when the picture changed"; it is <i>the moment the parabola starts</i>, and the sound lands on
    /// the frame the character leaves the ground on whatever route the film took to get there. That could not
    /// be built on top of a game that remembered anything.
    ///
    /// Nothing by default. A game with no audible moves is a game that makes no noise, which is a legitimate
    /// thing for a screen in the corner to be.
    /// </summary>
    /// <param name="from">The clock at the end of the last look. May be before zero; treat it as zero.</param>
    /// <param name="to">The clock now.</param>
    /// <param name="made">Called once per move, with how much of it there was — one for the ordinary case.</param>
    public virtual void Moves(float from, float to, Action<Move, float> made)
    {
    }

    /// <summary>
    /// How many whole multiples of <paramref name="every"/> the clock passed, and where each one was.
    ///
    /// The workhorse of the four <see cref="Moves"/> overrides, because most of what a game on a script does
    /// happens on a beat of its own: a piece a second and a bit, a coin every cell, a stride every other
    /// picture. Derived from the clock rather than counted, so a slow frame reports two and a seek reports
    /// the ones in the window it asked about.
    /// </summary>
    protected static IEnumerable<long> Ticks(float from, float to, float every)
    {
        if (every <= 0f || to <= from)
            yield break;

        var first = (long)MathF.Floor(from / every) + 1;
        var last = (long)MathF.Floor(to / every);

        // A window wider than a second of these is a stall or a seek, and firing two hundred coins at once
        // is worse than missing them. The score suppresses seeks itself; this is the backstop.
        if (last - first > 64)
            yield break;

        for (var n = first; n <= last; n++)
            yield return n;
    }

    /// <summary>
    /// Whether a value the clock is sweeping past crossed <paramref name="mark"/> in this window, allowing
    /// for the level wrapping round.
    ///
    /// The other half of what the overrides need, and the wrap is the whole reason it is a method. A
    /// side-scroller's world repeats, so "did the runner reach twenty-six pixels short of the pipe" is a
    /// question about a position modulo the level's width — and asked naively it is false on the lap where
    /// the answer wraps, which is one jump in three going silent every ten seconds.
    /// </summary>
    protected static bool Passed(float from, float to, float mark, float period)
    {
        if (to <= from)
            return false;

        // A window a whole lap wide passed everything on the lap, once.
        if (to - from >= period)
            return true;

        var a = from - period * MathF.Floor(from / period);
        var b = a + (to - from);
        var at = mark - period * MathF.Floor(mark / period);

        return (at > a && at <= b) || (at + period > a && at + period <= b);
    }

    /// <summary>
    /// A material for a screen: unlit, so what is on it is exactly what was drawn.
    ///
    /// Unlit is right in both places it is used, which is a happy accident worth stating. Standalone it
    /// means the picture is the pixel data and not the pixel data times a light. In a dark room it means a
    /// television reads as a thing that is lit from inside, which is what a television is.
    /// </summary>
    public static Material Glass() => new()
    {
        // Off, not white. An unlit material with no texture on it draws full white, which in a dark room
        // makes every set that has not been switched on yet the brightest thing in the frame. A screen with
        // no picture is a dark sheet of glass, and <see cref="Show"/> is what turns it on.
        BaseColor = new Vector4(0.035f, 0.037f, 0.045f, 1f),
        Unlit = true,
        Name = "screen"
    };

    /// <summary>The screen, as a quad wide enough to be the right shape, facing the camera.</summary>
    public override Node BuildSubject()
    {
        _standalone = Glass();
        Show(_standalone, 0f);

        const float height = 2.4f;

        return new MeshNode(Primitives.Plane(height * Columns / Rows, height), _standalone)
        {
            Name = "screen",
            RotationDegrees = new Vector3(90f, 0f, 0f)
        };
    }

    /// <summary>
    /// Black, and nothing else. No floor, no key light, no environment: an unlit quad is not lit by any of
    /// them, and a ground plane under a television screen would be a stage the subject is not standing on.
    /// </summary>
    public override void Stage(Scene scene)
    {
        scene.Background = Colors.Black;
        scene.Lights.Clear();
        scene.Environment = EnvironmentLight.None;
    }

    public override SceneLook Look => SceneLook.Blueprint;

    public override bool Animates => true;

    public override bool FramesItself => true;

    public override TimeSpan TourDuration => TimeSpan.FromSeconds(Loop);

    public override void Frame(Camera camera)
    {
        camera.Target = Vector3.Zero;
        camera.Distance = 3.55f;
        camera.Yaw = 0f;
        camera.Pitch = 0f;
        camera.NearPlane = 0.5f;
        camera.FarPlane = 20f;
    }

    public override void Update(Scene scene, double elapsed)
    {
        if (Show(_standalone, (float)elapsed))
            scene.Invalidate();
    }
}
