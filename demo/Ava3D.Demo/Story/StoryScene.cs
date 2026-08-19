using System.Numerics;
using Ava3D.Demo.Engine;
using Ava3D.Demo.Scenes;

namespace Ava3D.Demo.Story;

/// <summary>
/// The exhibition, as something the shell can select: one <see cref="DemoScene"/> wrapping the whole film.
///
/// It is a <see cref="DemoScene"/> because everything the shell already knows how to do — build once,
/// advance per frame on the compositor's clock, drive the camera, show a caption, time out and move on —
/// is exactly what a film needs, and none of it needed changing. What it adds is one number.
///
/// That number is <see cref="StartAt"/>, and it is the whole of what makes the contents work. Picking a
/// feature does not build that feature's scene; it builds the film and starts it at the second where the
/// feature is on screen. The walk is a pure function of time and so is every chapter, so starting at 96
/// seconds is not a fast-forward — nothing has to be caught up, because nothing has accumulated. It is an
/// assignment to a clock.
/// </summary>
public sealed class StoryScene : DemoScene
{
    /// <summary>How fast he walks when the viewer is driving, in metres a second. Rather quicker than the
    /// film walks him, because the film is looking at things and the viewer is going somewhere.</summary>
    private const float Pace = 1.7f;

    /// <summary>
    /// How far in front of the eye the camera aims when the viewer has the controls.
    ///
    /// The view's orbit turns the camera's position around its target, which is what an orbit is for and is
    /// not what looking around is. Put the target a third of a metre in front of the face and the two become
    /// nearly the same gesture — the eye swings on a very short arm — and then the eye is put back where it
    /// belongs on the next frame anyway, because the walk keeps its own position and takes only the
    /// <i>direction</i> from the camera. So the drag turns the head and nothing else, which is what a first
    /// person look is, out of a control that only knows how to orbit.
    /// </summary>
    private const float Pivot = 0.35f;

    private readonly float _startAt;
    private readonly Ground _ground = new();

    private Film _film = null!;
    private Soundtrack? _sound;
    private int _chapter = -1;
    private float _now;
    private float _last;

    /// <summary>Whether the viewer has been handed the building. Set once, at the end, and never unset —
    /// there is no taking it back, which is the point of the moment.</summary>
    private bool _free;

    private Vector3 _eye;
    private Vector2 _steer;

    /// <summary>The heading the free walk last handed the camera, in radians about up. Null until it has
    /// handed out one — see <see cref="Facing"/>, which needs a previous one to measure a drag against.</summary>
    private float? _facing;

    /// <param name="startAt">Which second of the film to open on.</param>
    public StoryScene(float startAt = 0f) => _startAt = Math.Max(0f, startAt);

    public override string Title => "The Exhibition";

    public override string Summary => "One building, first person, and every feature in a room";

    public override string Notes =>
        """
        A nine-minute walk through an exhibition of everything this control can draw, in first person,
        with no cuts until the last chapter.

        The building is laid out so that no two exhibits are ever in the same frame: every room's exit is
        on a different wall from its entrance, so you cross a threshold, turn, and only then does the next
        room have a sightline to you. That is a rule about storytelling and it pays for itself twice — the
        set that can reach your eye is one room, so a browser tab is never drawing a hangar while it looks
        at a cube.

        Four lights are the other constraint the plan is built on. One room at a time needs about four
        lamps, and rooms hand their lights over at the threshold rather than adding to them. That was the
        renderer's number when the building was drawn and it is the film's own now — Scene.Lights will take
        as many as you ask it for.

        Switch the story off in the toolbar and the same list shows the same features on a black
        background, one at a time, which is what this demo has always been. Neither is a copy of the
        other: the exhibits in the rooms are the standalone scenes, mounted.
        """;

    public override SceneLook Look => SceneLook.Studio;

    public override bool Animates => true;

    public override bool DrivesCamera => true;

    public override bool FramesItself => true;

    /// <summary>
    /// What is left of the film from where it was started, so Auto sits through the rest of it rather than
    /// moving on after nine seconds — and then three quarters of a minute more.
    ///
    /// The extra is the free walk. Two seconds used to be enough because the last thing the film did was
    /// hold on a frame; now the last thing it does is hand the building over, and a tour that takes it back
    /// before anybody has crossed the room has not handed anything over. Forty-five seconds is roughly the
    /// length of the gallery there and back at walking pace. It is still bounded, because Auto is a tour and
    /// a tour that stops is a tour that is broken — somebody who wants longer turns Auto off, which is the
    /// same answer this demo has always given.
    /// </summary>
    public override TimeSpan TourDuration =>
        TimeSpan.FromSeconds(Math.Max(1f, (_film?.Duration ?? 0f) - _startAt) + 45f);

    /// <summary>
    /// How long the film runs, in seconds, or zero before it has been built.
    ///
    /// The film's own length rather than this scene's: it is a property of the chapter list and does not
    /// move when somebody opens the story part way through. <see cref="TourDuration"/> is the other
    /// question — how long a visitor should be left here — and adds the free walk on the end, which is why
    /// it is not this and cannot be turned into it.
    /// </summary>
    public float Length => _film?.Duration ?? 0f;

    public override Scene Build()
    {
        _film = new Film();
        _chapter = -1;

        // The soundtrack is opened here rather than in the constructor because it is bound to this film —
        // it reads the chapter list to know which score to run and needs the walk to put footsteps under
        // it. Rebuilt with the building, and null on a machine with no audio device, in a measured run, or
        // when the switch in the toolbar is off; everything downstream of it is a null-conditional, so
        // silence costs the film no code at all. See Soundtrack for what it does with the film's clock.
        Hush();

        if (DemoSettings.Sound)
            Listen();

        // Put the building into the state the opening second wants before the first frame is drawn, rather
        // than on it. A film that starts at 96 seconds and spends its first frame lit like second zero has
        // a visible flash in it, and a probe run that captures one frame would capture the wrong one.
        Advance(_startAt);

        return _film.Hall.Scene;
    }

    /// <summary>
    /// The first frame's camera, before any chapter has spoken.
    ///
    /// All four are reasserted every frame from the chapter — see Update — so this is not where the
    /// numbers live. It is here because the shell frames a scene once before it updates it, and a film
    /// whose opening frame was shot on whatever lens the last scene left behind would flash.
    /// </summary>
    public override void Frame(Camera camera)
    {
        var opening = _film.Chapters[_film.At(_startAt).Chapter];

        camera.FieldOfView = opening.Lens;
        camera.NearPlane = opening.Near;
        camera.FarPlane = opening.Far;
        camera.Roll = 0f;
    }

    /// <summary>The building is his once the film has finished telling him about it.</summary>
    public override bool WantsControl => _free;

    public override void Steer(Vector2 move) => _steer = move;

    /// <summary>
    /// Taken off the screen. The building can be collected; the audio device cannot, and would go on
    /// playing a room tone from a room nobody is in. See <see cref="DemoScene.Retire"/>.
    /// </summary>
    public override void Retire() => Hush();

    public override void Update(Scene scene, Camera camera, double elapsed)
    {
        _last = _now;
        _now = _startAt + (float)elapsed;
        Advance(_now);

        var (index, into) = _film.Beyond(_now);
        var chapter = _film.Chapters[index];

        // The lens and the two planes, from the chapter rather than from Frame. Frame runs once, and the
        // film is not one camera: eight chapters are a person's eye five centimetres from a doorframe and
        // one is a film camera with a planet in it — see Chapter.Lens, Near and Far. Assigning all three
        // every frame costs three floats and keeps them honest under seeking, which is the only way
        // anybody reaches the last chapters from the picker.
        camera.FieldOfView = chapter.Lens;
        camera.NearPlane = chapter.Near;

        // <b>And the far plane comes back in once the film is over.</b> The last chapter can see nineteen
        // hundred metres because it is looking out of a window at a planet, and the free walk inherits it
        // — which means the whole building is drawn inside a depth range sized for a solar system. Depth
        // precision goes as the distance squared over the near plane and the far plane barely enters into
        // it on a twenty-four-bit buffer; on a sixteen-bit one, which is what a browser hands out when it
        // cannot do better, it enters into it a great deal. Two hundred and twenty is the whole building
        // with room to spare, and the two rooms that can actually see out get the long range back.
        camera.FarPlane = _free && !_film.Rounds.Outward ? Chapter.Indoors : chapter.Far;

        // Nothing over the picture unless somebody is actively holding it there. Two chapters put a
        // curtain across the lens and eight do not, and clearing it here rather than asking the other
        // eight to is what makes seeking safe: jumping out of the fade at the end of the cut, or into the
        // black at the top of the morning and then back to the antechamber, cannot leave a screen dark
        // with no chapter that knows it did it. See Curtain.
        _film.Curtain.Clear();

        if (_free)
        {
            Wander(camera);

            // The score, after the walk rather than before it, and that order is the whole of what makes
            // the free walk audible. Every chapter above is a function of the second and can be scored
            // whenever; this one is a function of where he is standing, and where he is standing is what
            // the line above has just worked out. See Soundtrack.Stands.
            //
            // The last chapter is still running underneath all of it — the ship is still flying — which is
            // why this is the same Advance and not a second one. See Film.Beyond.
            _sound?.Stands(_eye, _film.Rounds.Room, _film.Rounds.Ball, _film.Rounds.Show);
            _sound?.Advance(_now);

            return;
        }

        _sound?.Advance(_now);

        chapter.Shoot(camera, into);

        // The hand-over. The film has run out, he is standing at the last window, and the last thing it does
        // is stop steering — see Wander for what happens then.
        //
        // It needs the film to have ended somewhere a person is standing, which is what the walk being
        // there says. A film whose last chapter had no walk would be one that ended in open space, and
        // handing somebody the controls of a body that is not anywhere is not a gift.
        //
        // The rooms are surveyed here, once, on the frame the viewer takes over, and they are surveyed with
        // the whole building standing — see Rounds.Open, which puts all eight rooms up and opens the three
        // powered doors before this line runs. Taking its shape now costs one walk of eight room graphs
        // rather than one per frame forever, and it has to be all eight: a wall stops being solid the moment
        // its room is hidden, and Rounds goes straight back to drawing three of them.
        if (_now < _film.Duration || chapter.Walk is not { } walk)
            return;

        _free = true;
        _eye = walk.At(into).Eye;

        if (Standing is var (where, bearing))
        {
            _eye = where;

            if (bearing is { } degrees)
                camera.LookFrom(
                    _eye,
                    _eye + new Vector3(
                        MathF.Sin(float.DegreesToRadians(degrees)),
                        0f,
                        MathF.Cos(float.DegreesToRadians(degrees))));
        }

        _film.Rounds.Open();
        _ground.Survey(_film.Hall.Scene);
        _film.Rounds.Stand(_eye, _now, walking: true);
    }

    /// <summary>
    /// The viewer walking the building himself.
    ///
    /// Two lines of it are load-bearing. The direction comes from the camera, which the view's orbit has
    /// been turning; the <i>position</i> comes from here, so the orbit's own movement of the eye is thrown
    /// away every frame and only the aim survives. And the forward vector is flattened before it is walked
    /// along, so looking at the ceiling and pressing forward walks forward rather than into it — a person on
    /// a deck goes where his feet point, not where his eyes do.
    /// </summary>
    private void Wander(Camera camera)
    {
        // Clamped, because the first frame after a stall would otherwise teleport him through a wall — the
        // slide test is a test of the place he is going, not of the line he took to get there.
        var elapsed = Math.Clamp(_now - _last, 0f, 0.1f);
        var step = elapsed * Pace;

        var aim = Facing(camera);

        var forward = aim;
        forward.Y = 0f;

        if (forward.LengthSquared() < 1e-6f)
            forward = -Vector3.UnitZ;

        forward = Vector3.Normalize(forward);

        // Screen-right on a level deck: the forward vector turned a quarter turn about up. Written out
        // rather than taken from Camera.Right because that one tips with the pitch and this one must not.
        var right = new Vector3(-forward.Z, 0f, forward.X);

        var push = right * _steer.X + forward * _steer.Y;
        var walking = push.LengthSquared() > 1e-6f;

        if (walking)
        {
            var was = _eye;
            _eye = _ground.Slide(_eye, _eye + Vector3.Normalize(push) * step);

            // How far he actually went, not how far he pushed. Walking into a wall and sliding along it
            // covers ground and sounds like it; walking into a wall head on covers none and should be
            // silent, which is what makes the wall read as a wall rather than as the controls sticking.
            _sound?.Walked(Vector3.Distance(was, _eye), elapsed);
        }

        // Which rooms are drawn, which four lamps are lit and how much bounce there is, off where he is
        // standing rather than off the clock. It runs after the last chapter's own Update — which is still
        // going, because the ship is still flying — so the chapter keeps the window and this keeps the
        // building. See Rounds.
        var seat = _film.Rounds.Stand(_eye, _now, walking);

        // Sitting down, and getting up again. Both are the same two lines because both are the same thing:
        // an eye eased towards a height, at a rate a person moves at rather than at a rate the frame does.
        // The aim is deliberately left alone through all of it — he can look wherever he likes from the
        // sofa, which is most of the point of putting him on it.
        if (seat is { } sofa)
            _eye = Vector3.Lerp(_eye, sofa, 1f - MathF.Exp(-elapsed * 2.4f));
        else if (MathF.Abs(_eye.Y - Deck.Eye) > 0.002f)
            _eye.Y = float.Lerp(_eye.Y, Deck.Eye, 1f - MathF.Exp(-elapsed * 3.2f));

        camera.LookFrom(_eye, _eye + aim * Pivot);
    }

    /// <summary>
    /// Where he is looking, with the drag's horizontal turned round.
    ///
    /// <b>An orbit and a head turn the opposite way and both are correct.</b> Dragging right on an orbit
    /// carries the camera round to the right of the target, so the thing being looked at appears to turn
    /// left — which is what taking hold of an object means and is what <c>Ava3DView</c> does. Dragging right
    /// on a head means look right. The free walk is a head built out of an orbit — see <see cref="Pivot"/>
    /// for the other half of that trick — so it is the one place in the demo where the control's own answer
    /// is the wrong one, and it is fixed here rather than in the control, where it would be wrong for
    /// everybody who has an object rather than a room.
    ///
    /// It is done as a mirrored <i>delta</i> rather than a negated yaw, and that is the whole of why it
    /// works: the view owns the camera's yaw and adds to it on every drag, so there is nothing to negate at
    /// the source. What this keeps is the heading it last handed out; whatever the orbit has added since is
    /// measured and subtracted twice, which turns the head the other way and leaves the view's own number
    /// free to wander. Pitch is untouched — nobody has ever complained that looking up is backwards.
    /// </summary>
    private Vector3 Facing(Camera camera)
    {
        var forward = camera.Forward;

        if (forward.LengthSquared() < 1e-6f)
            return -Vector3.UnitZ;

        forward = Vector3.Normalize(forward);

        var pitch = MathF.Asin(Math.Clamp(forward.Y, -1f, 1f));
        var yaw = MathF.Atan2(forward.X, forward.Z);

        if (_facing is { } was)
        {
            // The shortest way round, so a drag across the ±180° seam does not spin him a full turn.
            var turned = MathF.IEEERemainder(yaw - was, MathF.Tau);
            yaw = was - turned;
        }

        _facing = yaw;

        var flat = MathF.Cos(pitch);

        return new Vector3(MathF.Sin(yaw) * flat, MathF.Sin(pitch), MathF.Cos(yaw) * flat);
    }

    public override string? Caption
    {
        get
        {
            if (_free)
                return "The ship is yours. Look with the mouse, walk with W A S D or the two buttons";

            var (chapter, into) = _film.At(_now);
            return _film.Chapters[chapter].Caption(into);
        }
    }

    /// <summary>
    /// Puts the building into the state <paramref name="seconds"/> asks for.
    ///
    /// <see cref="Chapter.Enter"/> only when the chapter actually changes, and <see cref="Chapter.Update"/>
    /// every time. That is why a chapter's update has to be able to produce any moment from the time alone:
    /// jumping into the middle of one runs its enter once and then asks it for a moment it has never
    /// played up to.
    /// </summary>
    /// <summary>
    /// Renders the film's soundtrack to a .wav file, without a window, a renderer or an audio device.
    ///
    /// The counterpart to <c>AVA3D_CAPTURE</c>, and it exists for the same reason: the only report anybody
    /// can give about a sound is that it did or did not sound right on their machine, which is not
    /// something a second person can check. A file is.
    ///
    /// The film is stepped at sixty hertz — the rate it would be updated at on screen — and audio is pulled
    /// a frame at a time between steps, so the score is sampled exactly as the demo samples it. Nothing is
    /// drawn, so ten minutes of film renders in a few seconds.
    ///
    /// <b>The building is stepped too, and for nine months it was not.</b> This loop used to advance only
    /// the score, on the reasoning that a soundtrack is a function of the clock and the rooms are a picture.
    /// That held exactly as long as no chapter's sound depended on where anything was: chapter 7 asks the
    /// walk where the ear is, and a walk is a pure function of its own second, so nothing noticed. The last
    /// chapter asks the <i>room</i> how far past the mouth of a docking bay the window has got — see
    /// <c>Soundtrack.Afterwards</c> — and a room that is never updated answers with whatever its
    /// constructor left, which is "still in the berth", forever. The tape came back with eighty seconds of
    /// station hum over a ship that had left an hour ago, and it came back that way <i>consistently</i>,
    /// which is the worst kind of wrong a measuring instrument can be.
    ///
    /// So it runs <see cref="Chapter.Enter"/> and <see cref="Chapter.Update"/> exactly as
    /// <see cref="Advance"/> does on screen, and costs a few hundred milliseconds for the whole film. A tape
    /// of a film that was not playing is not a tape of the film.
    ///
    /// <b>And it can be asked for more film than there is</b>, which is how the free walk became something a
    /// second person can check. Past the last second the building is handed over exactly as
    /// <see cref="Update"/> hands it over, and the visitor retraces the film's own route backwards at the
    /// pace it was walked forwards — see <see cref="Retracing"/>. Every room, in reverse order, with a real
    /// position in it, which is precisely what the free walk's score is a function of. Before this there was
    /// no way to render a second of it at all: the loop stopped at the film's last frame, and the one part of
    /// the demo whose sound depends on nothing but where somebody is standing was the one part no tape could
    /// reach.
    /// </summary>
    /// <param name="path">Where to write it.</param>
    /// <param name="from">The first second of film to render.</param>
    /// <param name="to">The last, or zero for the whole film. Past <c>Film.Duration</c> is the free walk.</param>
    /// <param name="sampleRate">Samples a second. The cues are generated at whatever this is.</param>
    /// <param name="speed">
    /// Film seconds per real second. One is the film; nine is the film at nine times, and the tape comes out
    /// nine times shorter.
    ///
    /// <b>This is not the same thing as speeding up the finished tape, and the difference is the whole
    /// reason it is a parameter here rather than a filter afterwards.</b> The score is ten looping beds and
    /// a bank of one-shots — see <see cref="Soundtrack"/> — so what the clock actually drives is *when*
    /// things are triggered and where the ten levels are, never the rate anything is played back at. Run the
    /// clock fast and every sound keeps its own pitch and its own length: a bed is still a bed, a relay
    /// still takes as long to close as a relay takes. Resampling the finished file transposes the lot up
    /// three octaves, and time-compressing it — <c>atempo</c>, which is the usual answer — puts nine passes
    /// of a windowing artefact over a soundtrack that is mostly held tone, which is the material that shows
    /// it worst.
    ///
    /// What does change is density, and it should: the events are the picture's, and the picture is nine
    /// times faster. A man crossing the gallery still puts a foot down every stride, and at nine times he
    /// crosses it in four seconds, so the footfalls arrive nine times as often. That is what the film looks
    /// like at this speed, so it is what it should sound like.
    /// </param>
    /// <returns>A line describing what was written, or why nothing was.</returns>
    public static string RecordSoundtrack(
        string path, float from = 0f, float to = 0f, int sampleRate = 48000, float speed = 1f)
    {
        var film = new Film();
        var until = to > from ? to : film.Duration;

        speed = speed > 0f ? speed : 1f;

        using var sound = Soundtrack.Recording(film, sampleRate);

        if (sound == null)
            return "this build has no player to record with — see Story/Sound/Speaker.Silent.cs";

        const float frame = 1f / 60f;

        // Counted, not accumulated, and that is not fastidiousness — it is the same rule the film itself is
        // built on, applied to the thing that measures it.
        //
        // This loop used to advance a float by a sixtieth every step. A sixtieth is not representable, so the
        // rounding has a direction, and fifty-seven seconds of it drifts about fourteen milliseconds — most
        // of a frame. Nothing in the render sounds wrong, and it makes the tape useless for the one thing a
        // tape is for: rendering the same stretch of film from two different starting seconds gave two tapes
        // whose events were a whole frame apart, so the question "did this survive a seek" could not be
        // answered by comparing them. It does; the tape was lying about it.
        var standing = -1;

        var handed = false;
        var eye = Vector3.Zero;

        // Which rooms the retrace got into and when, for the report. A tape of a free walk is only worth
        // opening if the walk went somewhere, and "594 seconds, 57 MB" does not say whether it did.
        var crossed = new List<string>();
        var room = "";

        for (var step = 0; ; step++)
        {
            // Still counted rather than accumulated, for the reason above; speed multiplies the count, so a
            // fast render is sampled on exactly the same grid as a slow one and lands on the same instants.
            //
            // The step this puts between one Advance and the next is speed/60 film seconds, and Soundtrack
            // treats a step over 0.4 s as a seek rather than as the film running — which suppresses one-shots
            // for that frame, on purpose, so that jumping into the middle of a chapter does not replay every
            // relay since the top of it. Past twenty-four times, therefore, every frame looks like a jump and
            // the tape comes back with the beds and nothing else. That is a real ceiling and it is a long way
            // above anything worth watching.
            var at = (float)(from + step / 60.0 * speed);

            if (at >= until)
                break;

            // Beyond rather than At, so the last chapter goes on running under the free walk exactly as it
            // does on screen. Below the film's own length the two are the same call.
            var (index, into) = film.Beyond(at);

            if (index != standing)
            {
                standing = index;

                // Clear air first, then let the chapter ask for whatever it wants. See Hall.Clear.
                film.Hall.Clear();
                film.Chapters[index].Enter(film.Hall);
            }

            film.Chapters[index].Update(film.Hall, into);

            if (at >= film.Duration)
            {
                if (!handed)
                {
                    // The same two lines Update runs on the frame the film runs out, in the same order and
                    // for the same reason: every room standing and every door open before anything measures
                    // the place. There is no ground survey here because nothing is being walked into — the
                    // route below is the film's own, and the walk audit has already proved it clears.
                    handed = true;
                    film.Rounds.Open();
                }

                var was = eye;

                eye = Retracing(film, at);

                film.Rounds.Stand(eye, at, walking: true);

                if (film.Rounds.Room != room)
                {
                    room = film.Rounds.Room;
                    crossed.Add($"{room} {at:0}s");
                }

                sound.Stands(eye, room, film.Rounds.Ball, film.Rounds.Show);
                sound.Walked(Vector3.Distance(was, eye), frame);
            }

            sound.Advance(at);
            sound.Record(frame);
        }

        sound.Save(path);

        var over = until - film.Duration;

        return $"{path}: {until - from:0.#} s of film from {from:0.#} s"
               + (over > 0f ? $", the last {over:0.#} s of it the free walk retraced" : "")
               + (speed != 1f ? $", at {speed:0.##}× into {(until - from) / speed:0.#} s of tape" : "")
               + $", {sampleRate} Hz mono, {new FileInfo(path).Length / 1_000_000f:0.#} MB"
               + (crossed.Count > 0 ? $"\n  through {string.Join(", ", crossed)}" : "");
    }

    /// <summary>
    /// Where the visitor is standing at second <paramref name="at"/> of a rendered free walk: the film's own
    /// route, run backwards at the pace it was walked forwards.
    ///
    /// <b>A route rather than a stroll, and that is the point.</b> What the free walk's score is a function
    /// of is a position — see <c>Soundtrack.Wandering</c> — so a tape of it is worth nothing unless the
    /// position visits the building. Retracing gets every room in reverse order, in a line that is already
    /// known to clear the furniture, for no route table anybody has to keep in step with the plan. It also
    /// walks him back into the lounge and under the ball, which is the one place in the building where the
    /// lighting changes without a threshold being crossed.
    ///
    /// The cut has no walk in it — a minute of vacuum with nobody standing anywhere — so those seconds hold
    /// wherever the retrace last was, which is the window. That is also true of the film: the cut is where
    /// the camera stops being somebody's eye.
    /// </summary>
    private static Vector3 Retracing(Film film, float at)
    {
        var back = Math.Clamp(film.Duration - (at - film.Duration), 0f, film.Duration);
        var (index, into) = film.At(back);

        for (var i = index; i >= 0; i--)
            if (film.Chapters[i].Walk is { } walk)
                return walk.At(i == index ? into : film.Chapters[i].Duration).Eye;

        return Vector3.Zero;
    }

    /// <summary>
    /// AVA3D_STAND=&lt;x&gt;,&lt;z&gt;[,&lt;bearing&gt;] puts the visitor somewhere else when the film hands over.
    ///
    /// It is the free walk's <c>AVA3D_STORY_AT</c>, and it exists for the same reason: a capture can only be
    /// taken of a state something can be put into, and everything else in this film can be reached by
    /// setting a clock. The free walk cannot — the only thing that moves the visitor is a key, and there is
    /// nobody to press one in a headless run. Without this, the seven rooms the last chapter does not stand
    /// in have no lit frame anybody can look at, which is most of what this switch was written to check.
    ///
    /// Deck coordinates and eye height, which is how <see cref="Deck"/> and every room's waypoints are
    /// written; the bearing is degrees clockwise from the deck's +Z, so 0 looks up the ship and 180 looks
    /// back down it. Read once, at the hand-over, and never again.
    /// </summary>
    private static (Vector3 Eye, float? Bearing)? Standing
    {
        get
        {
            var where = Environment.GetEnvironmentVariable("AVA3D_STAND")?.Split(',');

            if (where is not { Length: 2 or 3 }
                || !float.TryParse(where[0], out var x)
                || !float.TryParse(where[1], out var z))
                return null;

            return (new Vector3(x, Deck.Eye, z),
                where.Length == 3 && float.TryParse(where[2], out var bearing) ? bearing : null);
        }
    }

    /// <summary>
    /// Turns the soundtrack on or off where the film stands, without touching the building.
    ///
    /// The switch used to be answered by building the film again, which gave "off" the meaning it should
    /// have — the device is closed, not a score playing to nobody — and charged the whole exhibition for
    /// it. <see cref="Hush"/> already closes the device, and the score is a pure function of the film's
    /// clock, so one opened mid-film starts at the second the picture is on rather than at the beginning.
    /// What the rebuild added was three glTF models, seven plated hulls and a starfield: a fifth of a
    /// second on this machine and nearly a whole one in a debug build, spent on the UI thread with the
    /// window frozen, every time somebody touched a checkbox.
    ///
    /// It also cost the viewer their place. The rebuild restarted the film at the selected entry's cue, so
    /// turning the sound on to hear the room you were standing in moved you out of it.
    /// </summary>
    public void Sound(bool on)
    {
        // Before the first Build there is no film to score, and Build will read the setting itself.
        if (_film is null)
            return;

        Hush();

        if (on)
            Listen();
    }

    /// <summary>Opens the machine's audio for this film, and says whether it got any.</summary>
    private void Listen()
    {
        _sound = Soundtrack.Open(_film);

        // Said out loud, because "asked for sound and did not get any" is the one state nobody can see.
        // A silent film looks exactly like a film whose switch is off, and on the browser and mobile
        // heads there is no other way to find out which of the two happened — the console is the whole
        // instrument panel there. It is one line, once, on a switch nobody turns on by accident.
        Console.WriteLine(_sound is null
            ? "[Ava3D.Demo] sound was asked for and there is no device to play it — the film is silent."
            : $"[Ava3D.Demo] sound is going to {_sound.Describe()}.");
    }

    /// <summary>Stops the sound and lets go of the device, if there was one.</summary>
    private void Hush()
    {
        _sound?.Dispose();
        _sound = null;
    }

    private void Advance(float seconds)
    {
        var (chapter, into) = _film.Beyond(seconds);

        if (chapter != _chapter)
        {
            _chapter = chapter;

            // Clear air first, then let the chapter ask for whatever it wants. See Hall.Clear.
            _film.Hall.Clear();
            _film.Chapters[chapter].Enter(_film.Hall);
        }

        _film.Chapters[chapter].Update(_film.Hall, into);
    }
}
