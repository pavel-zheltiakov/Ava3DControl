using System.Numerics;
using Ava3D.Demo.Scenes.Arcade;

namespace Ava3D.Demo.Story;

/// <summary>
/// What the film sounds like, second by second. The score.
///
/// It is one file rather than a method on each chapter, and that is the same decision a film crew makes:
/// the script is ten scenes written by whoever was writing that scene, and the sound is one pass over the
/// whole thing by somebody who has watched it end to end. Half of what is in here only exists as a
/// relationship between chapters — the alarm that starts under the mirror ball in chapter 4, is walked
/// towards in chapter 5, and is finally switched off by a board coming up in chapter 6 — and a per-chapter
/// method would have that spread across three files with nothing naming it.
///
/// <b>Ten beds and a handful of events.</b> The beds are looping voices that exist for the whole film and
/// are never stopped; what a chapter does is write a number into each one, every frame, and the player
/// glides to it. So the entire ambience of the film is a table of ten levels per chapter, the fades cost
/// nothing to write, and the loudest moment in the picture — the window in chapter 7 — is a line that takes
/// five of them to nearly nothing and lets the other three up to a tenth of what they replaced. Everything
/// else is a one-shot fired when the clock crosses a moment.
///
/// <b>Seeking.</b> The film can be jumped into at any second, so this has to be able to arrive in the
/// middle of a chapter. Beds are safe by construction, because they are a function of the time. One-shots
/// are not — replaying every relay and every footfall since the top of the chapter would be a burst of
/// noise — so a jump suppresses them for one frame and the counters resynchronise to where the film now
/// is. That is the same split <see cref="Chapter.Enter"/> and <see cref="Chapter.Update"/> are built on.
///
/// The one-shots that are not on a moment are on a <see cref="Sparse"/> schedule, which is a moment derived
/// from a hash instead of from a number somebody typed. It is still a pure function of the clock, so it
/// seeks like everything else here, and it is how the hull gets to tick eight times in a chapter without
/// ticking evenly.
///
/// <b>What is not here.</b> No panning, no reverb, no listener. The player takes a buffer and a level;
/// where something is in a room is this file dividing by a distance — see <see cref="Near"/> — which is
/// three lines and can be read, and is the whole of what the four cabinets and the exhibit motor do.
/// </summary>
internal sealed class Soundtrack : IDisposable
{
    /// <summary>How far he goes between footfalls at <see cref="Pace"/>, in metres. A shortish stride, because
    /// the film walks him slowly and a long one at that pace reads as a limp.</summary>
    private const float Stride = 0.76f;

    /// <summary>The speed <see cref="Stride"/> is the stride at, in metres a second. An unhurried walk.</summary>
    private const float Pace = 1.15f;

    /// <summary>
    /// The shortest stride there is, in metres. What stops a creep from becoming a shuffle.
    ///
    /// Below this the honest answer is that he is barely moving, and the honest sound is a step every couple
    /// of seconds rather than a stream of tiny ones.
    /// </summary>
    private const float Shortest = 0.30f;

    /// <summary>Seconds a beat, matching <c>Screens.Tempo</c>: a hundred and twenty a minute.</summary>
    private const float Beat = 0.5f;

    /// <summary>How long the longest one-shot in the bank runs for, in seconds — <c>Cues.Groans</c>. What a
    /// chapter has to stop firing before it ends if nothing is to be heard after the cut. See
    /// <see cref="Hull"/>, which is the only thing that needs it and the only chapter it matters in.</summary>
    private const float Settling = 3.6f;

    /// <summary>
    /// The shortest gap between two lamp cues in the free walk, in seconds.
    ///
    /// A contactor that has just closed does not close again, and a visitor standing with one foot either
    /// side of a threshold changes rooms at the frame rate. Half a second is long enough that pacing a
    /// doorway cannot make a machine gun of it and short enough that walking briskly through two rooms
    /// still lights both.
    /// </summary>
    private const float Rethrow = 0.5f;

    private readonly Speaker _speaker;
    private readonly Cues _cues;
    private readonly Film _film;

    /// <summary>The chapter that owns the four televisions, kept for the free walk — which has no chapter
    /// and still has four televisions playing in it. See <see cref="Cabinets"/>.</summary>
    private readonly Screens _lounge;

    private readonly Voice _air;
    private readonly Voice _ballast;
    private readonly Voice _plant;
    private readonly Voice _motor;
    private readonly Voice _pad;
    private readonly Voice _drive;
    private readonly Voice _draught;
    private readonly Voice _plot;
    private readonly Voice _berth;
    private readonly Voice _console;

    private int _chapter = -1;
    private float _at;

    private float _walked;
    private int _foot;

    private long _turn = -1;
    private long _beat = -1;

    /// <summary>
    /// The free walk, as the score needs it: whether the building has been handed over at all, and if it
    /// has, where he is standing, which room that is, and what the lounge is doing. Written once a frame by
    /// <see cref="Stands"/> and read by <see cref="Wandering"/>.
    /// </summary>
    private bool _handed;

    private Vector3 _stood;
    private string _room = "";
    private bool _ball;
    private float _show;

    /// <summary>The room whose lamps have been heard to strike, and which way the lounge was last heard to
    /// go. What a lighting change is measured against — the same shape <see cref="Crossed"/> has, with a
    /// state instead of a clock, because where somebody walks is not a number anybody can derive.</summary>
    private string _lit = "";

    private bool _danced;

    private float _relay = float.MinValue;

    private Soundtrack(Speaker speaker, Film film)
    {
        _speaker = speaker;
        _film = film;
        _cues = new Cues(speaker.SampleRate);
        _lounge = film.Chapters.OfType<Screens>().Single();

        // Started once, at silence, and never stopped. A bed that is started when a chapter needs it has
        // to be faded up from a standing start on the frame the chapter changes, which is the frame the
        // renderer is busiest; started here, a chapter change is ten assignments and the glide does the
        // rest. Ten voices summing at zero costs a few thousand multiplies a buffer, which is nothing.
        _air = speaker.Play(_cues.Air, 0f, loop: true);
        _ballast = speaker.Play(_cues.Ballast, 0f, loop: true);
        _plant = speaker.Play(_cues.Machines, 0f, loop: true);
        _motor = speaker.Play(_cues.Motor, 0f, loop: true);
        _pad = speaker.Play(_cues.Void, 0f, loop: true);

        // The three that only chapter 7 is allowed to open. Started here with the rest of them anyway, and
        // that is the point of starting them all in one place: whether a voice is silent for nine minutes or
        // for none of them is a number in a chapter's own line, not a decision made in a constructor.
        _drive = speaker.Play(_cues.Drive, 0f, loop: true);
        _draught = speaker.Play(_cues.Draught, 0f, loop: true);
        _plot = speaker.Play(_cues.Projector, 0f, loop: true);

        // And the two the last chapter opens. The berth is the only voice in the film that is a place the
        // film is not in — a station the ship happens to be parked inside — and the consoles are the same
        // gallery chapter 7 walked, awake.
        _berth = speaker.Play(_cues.Berth, 0f, loop: true);
        _console = speaker.Play(_cues.Consoles, 0f, loop: true);
    }

    /// <summary>Opens the machine's audio and builds the bank, or returns null when there is no sound to
    /// be had — a headless run, a platform with no device yet, the public build of this demo.</summary>
    public static Soundtrack? Open(Film film)
    {
        var speaker = Speaker.Open();
        return speaker == null ? null : new Soundtrack(speaker, film);
    }

    /// <summary>
    /// The same score, rendered to a file instead of played — see <see cref="Speaker.Recording"/>.
    ///
    /// It runs the identical code path: the same beds, the same events, driven by the same
    /// <see cref="Advance"/>. What differs is only where the samples end up, which is the whole point —
    /// a render nobody can trust is worse than no render, and this one cannot diverge from what the demo
    /// plays because there is no second implementation for it to diverge from.
    /// </summary>
    public static Soundtrack? Recording(Film film, int sampleRate)
    {
        var speaker = Speaker.Recording(sampleRate);
        return speaker == null ? null : new Soundtrack(speaker, film);
    }

    /// <summary>Renders <paramref name="seconds"/> onto the tape. Only does anything on a recording
    /// soundtrack.</summary>
    public void Record(float seconds) => _speaker.Record(seconds);

    /// <summary>Writes what has been recorded to a .wav file.</summary>
    public void Save(string path) => _speaker.Save(path);

    /// <summary>Where the sound is going, for the line the demo prints about itself.</summary>
    public string Describe() => _speaker.Describe();

    /// <summary>Silence, without losing the film's place.</summary>
    public bool Muted
    {
        get => _speaker.Muted;
        set => _speaker.Muted = value;
    }

    /// <summary>
    /// Puts the sound where <paramref name="now"/> is.
    ///
    /// Called every frame with the film's own clock, so this never has a clock of its own to disagree with
    /// the picture's.
    /// </summary>
    public void Advance(float now)
    {
        // Beyond rather than At, so the score is on the same clock as the picture after the film has ended
        // and the viewer is walking the last room — see Film.Beyond. Below the end of the film the two are
        // the same call, so nothing the recorder renders is touched by this.
        var (index, into) = _film.Beyond(now);

        // Three things are a jump rather than the film running: a different chapter, time going backwards,
        // and a step too big to be a frame. The last one catches the case nobody thinks of — a scene build,
        // a garbage collection or a window being dragged — where the film is playing normally and simply
        // lost half a second, and where firing every event in that half second at once is worse than
        // missing them.
        var jumped = index != _chapter || into < _at || into - _at > 0.4f;
        var from = jumped ? into : _at;

        if (jumped)
        {
            _chapter = index;
            _walked = 0f;
            _turn = _beat = -1;
        }

        _at = into;

        var chapter = _film.Chapters[index];

        // The building after the film, which is not a chapter and cannot be scored like one. The last
        // chapter is still running underneath it — the ship is still flying — but where the ear is stopped
        // being a function of the second the moment somebody else got the controls, so everything below is
        // written against a position instead. See Wandering.
        if (_handed)
        {
            Wandering(now, into, from, jumped);
            return;
        }

        switch (chapter)
        {
            case Dark: InTheDark(); break;
            case Houselights: Lamps(into, from, jumped); break;
            case Forms: Rotating(into); break;
            case MaterialWall: Plates(into, from, jumped); break;
            case Ink: Painted(into); break;
            case Patterns: Workshop(into); break;
            case Stars: Dome(into); break;
            case Screens lounge: Lounge(lounge, into, from, jumped); break;
            case Alarm: Amber(into, jumped); break;
            case Repair: Machines(into, from, jumped); break;
            case Outside: Window(into, from, jumped); break;
            case Cut: Nowhere(); break;
            case Morning morning: Afterwards(morning, into, from, jumped); break;
        }

        Footfalls(chapter, into, from, jumped);
    }

    /// <summary>
    /// The viewer walking the building himself, once the film has handed it over.
    ///
    /// Steps only, and no bed of its own: the free walk begins where the last chapter ended, at a window
    /// with almost nothing running, and the right sound for a man standing in a quiet room is the quiet
    /// room. What he gets is his own feet, which is also the only thing that tells him the building has
    /// stopped being a film and started being a place.
    /// </summary>
    /// <param name="metres">How far he moved this frame.</param>
    /// <param name="seconds">How long the frame was. <see cref="Tread"/> needs the speed, not the distance.</param>
    public void Walked(float metres, float seconds) => Tread(metres, seconds, 0.44f);

    /// <summary>
    /// Where the visitor is standing and what the building is doing round him, now that it is his.
    ///
    /// Called every frame of the free walk and <b>before</b> <see cref="Advance"/>, which is why the walk
    /// runs first in <c>StoryScene.Update</c>: this is the whole of what the free walk's score is written
    /// against, and a frame of it taken before he moved is a frame of somebody else's room.
    /// </summary>
    /// <param name="eye">Where he is standing, in world metres.</param>
    /// <param name="room">Which room that is — see <see cref="Rounds.Room"/>.</param>
    /// <param name="ball">Whether the lounge has gone over to the light show.</param>
    /// <param name="show">And how far over, so a hum can fade with the fittings that make it.</param>
    public void Stands(Vector3 eye, string room, bool ball, float show)
    {
        _handed = true;
        _stood = eye;
        _room = room;
        _ball = ball;
        _show = show;
    }

    /// <summary>Everything off — the film has been left. The beds go with it, so this instance is finished.</summary>
    public void Dispose()
    {
        _speaker.StopAll();
        _speaker.Dispose();
    }

    // ---- the chapters -------------------------------------------------------------------------------

    /// <summary>
    /// Chapter 0. Air, and one lamp somebody left on.
    ///
    /// The quietest thing in the film, and it is the reference every level after it is set against. Twenty
    /// five seconds of almost nothing is what makes three breakers closing in the next chapter an event.
    /// </summary>
    private void InTheDark()
    {
        Beds(air: 0.20f, ballast: 0.035f);
    }

    /// <summary>
    /// Chapter 1. Three breakers, a second and a half apart, and the room's hum arriving with the light.
    ///
    /// The relays are on the same numbers the lamps are — <c>Houselights.Update</c> ramps bank <c>i</c>
    /// from <c>1.2 + i * 1.5</c> — because a switch you hear a beat after the light comes on is a switch
    /// somebody else is throwing in another room. The fourth is at the hand-over, where the lamp over the
    /// plinth goes out and the one in the passage comes up.
    ///
    /// The ballast climbs with the same ramp the chapter lights the room with, which is what makes the hum
    /// belong to the lamps rather than to the building.
    /// </summary>
    private void Lamps(float into, float from, bool jumped)
    {
        Beds(air: 0.20f, ballast: 0.035f + 0.13f * Ramp(into, 1.2f, 5.4f));

        for (var i = 0; i < 3; i++)
            if (Crossed(1.2f + i * 1.5f, into, from, jumped))
                _speaker.Play(_cues.Lamp, 0.68f);

        if (Crossed(Houselights.Reveal, into, from, jumped))
            _speaker.Play(_cues.Lamp, 0.5f);
    }

    /// <summary>
    /// Chapter 2. The arm, which has turned every night for nine hundred nights and is the first thing in
    /// this building that makes a noise because it is doing something.
    ///
    /// Its level is a distance, and the distance is to the arm. That is the whole of what this player calls
    /// positional audio and it is deliberately in the open: <see cref="Near"/> is a division, the answer
    /// goes into a voice's volume, and a reader who wants a different falloff changes one line rather than
    /// looking for a listener object.
    /// </summary>
    private void Rotating(float into)
    {
        Beds(air: 0.17f, ballast: 0.15f, motor: 0.30f * Near(Eye(into), Rotunda.ArmAt, 3.2f));
    }

    /// <summary>
    /// Chapter 3. Nothing but the room, and one door.
    ///
    /// Forty-nine plates of metal and paint make no sound at all, and the temptation to give the best-lit
    /// room in the building something to do is exactly the temptation to worry. A score needs a rest in it
    /// or nothing in it is an event, and this is the rest.
    ///
    /// Which is why the door was missed for so long. It is the <b>first powered door in the film</b> — the
    /// moment the building stops being a museum, and the chapter's own note says neither the walk nor the
    /// caption says so — and it opened in silence in the one chapter with nothing else in it to cover for it.
    /// A rest is not a chapter where nothing happens; it is a chapter where one thing does.
    ///
    /// The level is the distance to the door rather than a number, because he is two and a half metres short
    /// of it when it starts to part and walks up to it over the next five seconds. That is what makes it a
    /// door hearing him coming rather than a door he is standing at, and it comes out within three decibels
    /// of the other two powered doors without anybody having matched them by ear.
    /// </summary>
    private void Plates(float into, float from, bool jumped)
    {
        Beds(air: 0.16f, ballast: 0.15f);

        if (Crossed(MaterialWall.Opens, into, from, jumped))
            _speaker.Play(_cues.Servo, 0.72f * Near(Eye(into), Gallery.Way, 5f));
    }

    /// <summary>
    /// Chapter 4. The quietest room since the antechamber, and it gets quieter.
    ///
    /// A studio has nothing in it that runs. The gallery had a powered door and a turning drum; this has
    /// two stone stands, a rail with a lamp on it and four fittings, and none of those is a sound. So the
    /// bed comes down as the room does — the ballast goes with the lamps at the douse, which is the one
    /// place in the film where a room's own hum can be taken away without anybody wondering what happened
    /// to it, because they just watched somebody take the light away too.
    ///
    /// The air stays. The building keeps breathing whether or not there is a light on in this part of it,
    /// which is the same thing the lounge's blackout says and is most of why the last ten seconds of this
    /// chapter read as a dark room rather than as a stopped film.
    /// </summary>
    private void Painted(float into) =>
        Beds(air: 0.15f, ballast: 0.13f * (1f - 0.8f * Ramp(into, 33f, 4f)));

    /// <summary>
    /// Chapter 5. A workshop, which is the first room since the rotunda with more than one thing turning
    /// in it — and none of them audible either.
    ///
    /// Level with the gallery and no lower, because the room is twice as wide and a wide room with a low
    /// bed reads as an outdoor one. It comes down over the last ten seconds with the walk into the link,
    /// which is the corridor's own level arriving early: the two rooms are one continuous shot and the
    /// bed has to be at the corridor's number by the frame the corridor is what you are looking at.
    /// </summary>
    private void Workshop(float into) =>
        Beds(air: 0.16f - 0.03f * Ramp(into, 58f, 10f), ballast: 0.15f - 0.04f * Ramp(into, 58f, 10f));

    /// <summary>
    /// Chapter 6. A room getting quieter, which is the only chapter in the film where that is the event.
    ///
    /// Everything else here is a bed being added to. This one takes two away and puts a third in their
    /// place: the air comes down by two fifths as the house lights go, the ballast goes with the fittings
    /// that make it, and what is left is a fan and a coil — <see cref="Cues.Projector"/>, which is the
    /// gallery's plot table two rooms further on and is the same machine. A planetarium projector is a
    /// small motor turning something heavy in a dark room, and so is a chart table; there was no argument
    /// for building a second one.
    ///
    /// <b>The ramp is <c>Stars</c>'s own.</b> It reads the same two constants the chapter's lights read, so
    /// the hum stops on the frame the coves stop rather than a second either side — and a ballast still
    /// buzzing over a dark dome is the one sound that would say "a room with the lights off" at exactly the
    /// moment the picture has stopped being a room at all.
    /// </summary>
    private void Dome(float into)
    {
        var down = Ramp(into, Stars.Curtain - 3f, 5.5f)
                   * (1f - Ramp(into, Stars.Curtain + Sky.Watched, 2.5f));

        // The ballast goes all the way out with the coves, which is the picture's own change and is the
        // whole of what this method is doing: a room with no light in it has no hum in it either, because
        // the hum was the lamps. What is left underneath is the air, and then the projector, which is the
        // only thing running.
        Beds(
            air: 0.15f - 0.07f * down,
            ballast: 0.14f * (1f - down),
            plot: 0.062f * down);
    }

    /// <summary>
    /// Chapter 7. Four cabinets talking to an empty room, a light show with a tempo, and — under both of
    /// them, from the moment the lamps go down — something turning at the end of a corridor.
    ///
    /// The alarm arrives here rather than in the chapter named after it, and thirty seconds before the
    /// caption notices it. That is the point: the audience hears it first, faintly, through a doorway, and
    /// spends half a minute wondering whether they did. It comes up on exactly the ramp
    /// <c>Screens.Update</c> brings the corridor's glow up on, so the first sound and the first light are
    /// the same event.
    ///
    /// The beat is fired one thump at a time rather than played as a looping bar, and that is what keeps it
    /// locked to the lights through a seek. The lights are a pure function of the second; a loop started
    /// when a chapter was entered is a phase, and a phase is exactly the kind of state this film does not
    /// keep anywhere else.
    /// </summary>
    private void Lounge(Screens lounge, float into, float from, bool jumped)
    {
        // The lamps go out at Blackout and the hum goes with them. The air does not — the building keeps
        // breathing whether anybody has left a light on or not, and that difference is most of why the
        // room still feels occupied after it goes dark.
        Beds(air: 0.14f, ballast: 0.14f * (1f - Ramp(into, Screens.Blackout, 5f)));

        Games(lounge, into, from, jumped);

        // On the beat and off it, from the moment the alcove starts. Two voices a beat, four a second.
        var show = Ramp(into, Screens.Curtain, 6f);

        if (show > 0.01f && Every(Beat * 0.5f, into, jumped, ref _beat, out var half))
            _speaker.Play(half % 2 == 0 ? _cues.Kick : _cues.Tick, show * (half % 2 == 0 ? 0.44f : 0.26f));

        Turning(into, jumped, 0.15f * Ramp(into, Screens.Blackout + 2f, 7f));
    }

    /// <summary>
    /// Chapter 8. Twenty-one metres of corridor with the thing he has been half hearing at the end of it.
    ///
    /// Same clock as the chapter before — <c>Screens.Length</c> ahead — so the alarm does not restart, skip
    /// or change phase at the join. It is the one number in the film that has to be kept in step by hand,
    /// and the picture already keeps it; this only has to agree.
    ///
    /// The air thins because a corridor is not a room. There is no hum at all: nothing in here is lit by
    /// anything with a ballast in it, which is what the chapter's own comment says about the light and
    /// happens to be true of the sound for the same reason.
    /// </summary>
    private void Amber(float into, bool jumped)
    {
        Beds(air: 0.10f);
        Turning(into + Screens.Length, jumped, 0.55f);
    }

    /// <summary>
    /// Chapter 9. The biggest room there is, and it is full of machines nobody is looking after.
    ///
    /// Two rates beating against each other, and no third one, because the middle machine is out — see
    /// <c>Cues.BuildMachines</c>. The bed comes up as the door opens rather than at the top of the chapter,
    /// so what is heard is a room being let into.
    ///
    /// And then the alarm stops. Not because the chapter ends — it stops at <c>Ready</c>, on the same ramp
    /// the corridor's glow dies on, which is five seconds after the last lamp on the board comes up. The
    /// board being alive is what switches it off, and that is the only thing in ten chapters that answers
    /// anything.
    /// </summary>
    private void Machines(float into, float from, bool jumped)
    {
        Beds(air: 0.07f, plant: 0.26f * Ramp(into, 2f, 7f));

        // The gate at the end of the corridor, which chapter 5 walked up to and did not open.
        if (Crossed(Repair.Opens, into, from, jumped))
            _speaker.Play(_cues.Servo, 0.6f);

        Turning(into + Screens.Length + Alarm.Length, jumped, 0.2f * (1f - Ramp(into, Repair.Ready, 3f)));

        // The four parts arriving, on the second each one's ramp finishes rather than the second it starts
        // moving. A part makes its noise when it lands.
        if (Crossed(Repair.Cooled, into, from, jumped))
            _speaker.Play(_cues.Latch, 0.62f);

        if (Crossed(Repair.Filled, into, from, jumped))
            _speaker.Play(_cues.Click, 0.66f);

        if (Crossed(Repair.Ranked, into, from, jumped))
            _speaker.Play(_cues.Latch, 0.55f);

        if (Crossed(Repair.Seated, into, from, jumped))
            _speaker.Play(_cues.Latch, 0.66f);

        // The board coming up. One small sound for the moment the whole chapter has been working towards,
        // because the caption is right that nothing says it — the board says it by being lit.
        if (Crossed(Repair.Power, into, from, jumped))
            _speaker.Play(_cues.Click, 0.45f);
    }

    /// <summary>
    /// Chapter 10. The walk to the window, and the sound going with it.
    ///
    /// This is the reveal, and the reveal is a subtraction. Everything in the bank so far has been chosen
    /// so that a large building at night would make it — air in a duct, a contactor, a motor, a plant room
    /// — because the sixth rule of this story says nothing may admit where this is until the illuminator
    /// does. Sound gives a place away faster than a caption: one hull creak in chapter 1 and the surprise
    /// is over eight minutes early.
    ///
    /// So the window is not a new sound arriving. It is all of them stopping. He walks out of the engine
    /// room, the plant fades behind him, the air thins to nearly nothing — and into the hole that leaves, at
    /// a tenth of what used to be in it, comes the thing the building was standing on the whole time.
    ///
    /// <b>Everything in the second half of the bank lives in this one chapter, and it is quieter than the
    /// chapter before it.</b> That is the part worth being careful about, because "the last room needs more
    /// ambience" and "the reveal is a subtraction" read as opposites and are not. The plant room ran its beds
    /// at three hundred and seventy thousandths; the gallery's four voices together come to about fifty, and
    /// the difference is what makes any of them audible at all. A drive a hundred metres aft, a plate ticking
    /// as it comes round into the light, a vent washing the coldest glass on board and a projector left on
    /// over a table are all sounds you can only hear in a room where nothing else is running. Turning the
    /// room down is not a cost paid to reveal them. It is the mechanism.
    ///
    /// The gate on all four of them is the door, and the door is <see cref="Outside.Handover"/> — the frame
    /// he steps through it, which is the frame the film is allowed to sound like a spacecraft and not one
    /// second before. Rule 6 would be better served still by running the drive under chapter 6 and letting
    /// the plant mask it, which is what actually happens on a ship; the rule exists because of the listener
    /// with headphones who would pick it out anyway, so the fade stands in for the masking and is honest
    /// about it.
    ///
    /// Two of the four are distances rather than levels. The projector is over the plot table, inboard and
    /// near the door; the draught is at the glass. So walking the gallery is walking from one to the other —
    /// the last machine on board going out behind him over twenty-three decibels while the coldest surface on
    /// it comes up in front. Nothing arrives and nothing fades; the room simply is not the same at both ends
    /// of itself, which is the only reward the film offers for the last three metres of a nine-minute walk.
    /// </summary>
    private void Window(float into, float from, bool jumped)
    {
        var away = Ramp(into, 6f, 10f);
        var arrived = Ramp(into, 19f, 7f);

        // Through the door. One ramp gates every sound in the film that belongs to a ship.
        var ship = Ramp(into, Outside.Handover - 1f, 8f);
        var eye = Eye(into);

        Beds(
            air: 0.11f * (1f - 0.94f * arrived),
            plant: 0.26f * (1f - away),
            drive: 0.030f * ship,
            draught: 0.024f * ship * Falloff(Illuminator.OffGlass(eye), 2.4f),
            plot: 0.026f * ship * Near(eye, Illuminator.Table, 2.2f));

        // The way out, which had no sound at all until now — the one powered door in the film that opened
        // silently, in the chapter whose whole subject is a door being gone through.
        if (Crossed(Outside.Opens, into, from, jumped))
            _speaker.Play(_cues.Servo, 0.55f);

        Hull(into, from, jumped, ship, eye, Outside.Length);
    }

    /// <summary>
    /// The hull, ticking and occasionally taking a load.
    ///
    /// <b>Sparse and uneven, and the unevenness is the whole thing.</b> Anything on a period is a machine —
    /// the alarm is on a period because a beacon is, the lounge's beat is on a period because a light show
    /// is — and a hull cooling is the opposite of a machine. On an even four and a half seconds this reads as
    /// a dripping tap within three of them. So the moments come out of <see cref="Sparse"/>, which is a hash
    /// rather than a counter: still a pure function of the clock, still identical on every run and after every
    /// seek, and never twice the same gap.
    ///
    /// <b>Each tick comes from somewhere.</b> A hash roll picks which quarter of the glass let go, and the
    /// level is the distance to it — so the eight ticks in the chapter are spread along thirteen metres of
    /// window rather than all arriving in the middle of his head, and the ones from the far end are the quiet
    /// dull ones. That is four lines and it is the difference between a hull and a sound effect.
    ///
    /// The levels are squared before they are used, which makes most of them very quiet and every so often
    /// one of them loud. A plate that ticks at the same strength eight times running is a plate that has been
    /// sampled once.
    /// </summary>
    /// <param name="level">The chapter's ship gate: nought until the door, one in the gallery.</param>
    private void Hull(float into, float from, bool jumped, float level, Vector3 eye, float until)
    {
        // Nothing may still be ringing at the cut, and this is the one place in the film where that has to be
        // said out loud. Chapter 8 is a minute of vacuum with nobody in it — it is the only chapter that is
        // not somebody's ears — so a hull tick that arrives two tenths of a second after the film has left
        // the ship is not a tail, it is a claim that there is somebody out there to hear it. The schedule
        // therefore stops a groan's length short of the end, which measured as the last two hundred
        // milliseconds of a plate ringing across the hardest cut in the film.
        //
        // What it buys back is the better ending anyway: the escort settles, the last caption lands, and the
        // building has three and a half seconds of nothing in it before the cut.
        // A quarter open rather than merely non-zero, and the difference is one tick that was not there. The
        // gate multiplies the level, so an event fired on the frame the ramp begins arrives at a
        // twenty-fifth of what it asked for — which measured at four ten-thousandths, or a voice started to
        // play nothing. Waiting until the door is a quarter open costs a second and a half and no ticks.
        if (level < 0.25f || into > until - Settling)
            return;

        // About one every four and a half seconds, so nine of them across the gallery.
        foreach (var word in Sparse(from, into, jumped, 4.6f, 0x1D0C7A))
        {
            var loud = Roll(word, 0);

            // Twelve metres of reach for a room fifteen long, so the far end of the glass is half as loud as
            // the near end and not a fifth. It is the one place in the film where the falloff is set by
            // audibility rather than by physics: at the reach the cabinets use, the quiet ticks from the far
            // end measured six decibels <i>under</i> the room's own bed, which is a voice nobody will ever
            // hear playing. A hull that ticks where he is not standing still has to tick.
            _speaker.Play(
                _cues.Plates[word % (uint)_cues.Plates.Length],
                level * (0.07f + 0.10f * loud * loud) * Near(eye, Illuminator.Pane((int)(Roll(word, 1) * 4f)), 12f));
        }

        // And one groan, or two if the hash feels like it. Not positional: this one is the whole structure,
        // and a distance to it would be claiming the ship has a place in the ship.
        //
        // Half of what it was first written at, which measured as the loudest single thing in the chapter —
        // two and a half times a footfall, and more than the powered door. A creak is not a larger impact
        // than a tick; it is a longer one, and three and a half seconds of it does not need to be loud to be
        // the biggest thing in the room.
        foreach (var word in Sparse(from, into, jumped, 24f, 0x6B31F5))
            _speaker.Play(_cues.Groans[word % (uint)_cues.Groans.Length], level * (0.038f + 0.032f * Roll(word, 0)));
    }

    /// <summary>
    /// Chapter 11. He has stopped being the camera, so the sound stops being his ears.
    ///
    /// Every other chapter is what a man standing in a room can hear. This one is a film about the thing he
    /// is standing in, and giving it room tone would be claiming somebody is out there listening. What it
    /// gets instead is the only cue in the bank that is not a thing in the building.
    /// </summary>
    private void Nowhere()
    {
        Beds(pad: 0.30f);
    }

    /// <summary>
    /// Chapter 12. Inside somebody else's building, and then not.
    ///
    /// <b>It is chapter 7's trick run backwards.</b> That chapter revealed a ship by taking a building away;
    /// this one opens with a building the ship is <i>parked in</i> — seventy metres of docking bay with a
    /// station's plant on the other side of its wall — and reveals open space by taking that away. Same
    /// mechanism, opposite direction, and the same rule underneath both: nothing is added at the threshold.
    /// The berth is a third of the mix while he is tied on and nothing at all a minute later, and what is
    /// left in the hole is a pad, a vent and a drive that were always going to be there.
    ///
    /// <b>Every level here is a distance, not a second.</b> They all hang off <see cref="Morning.Past"/> —
    /// how far past the mouth of the bay the window is — rather than off timings of their own, so the sound
    /// and the picture cannot be retuned into disagreeing. Move the departure and the bay goes quiet at the
    /// moment it should whether or not anybody remembered this file.
    ///
    /// <b>And the drive comes on.</b> Chapter 7 opened the ship's half of the bank and ran the drive at
    /// three hundredths, which is a ship holding station. This one is a ship leaving, so it climbs to half
    /// as much again over the four hundred metres after the door — the only place in the film where that
    /// voice does anything but sit there, and the reason it was worth building as a machine rather than as
    /// a rumble.
    /// </summary>
    private void Afterwards(Morning chapter, float into, float from, bool jumped)
    {
        var past = chapter.Past;

        // One is inside the building and nought is out of it. Six metres short of the mouth to forty past
        // it, which is the five seconds the collar takes to go by.
        var inside = 1f - Ramp(past, -6f, 46f);
        var eye = Eye(into);

        Beds(
            air: 0.040f,
            ballast: 0.045f,

            // Space, at a thirtieth and not at chapter 8's three tenths.
            //
            // That is the whole of what was wrong with the first pass and it is worth writing down, because
            // the number looked right and measured wrong. Chapter 8 is a minute in vacuum with nobody in
            // it, so the pad is the entire mix; this chapter is a man in a lit room with a window, and a
            // pad at anything like that level buries everything the chapter is about. Measured, the drive
            // was seventeen decibels under it — a machine the size of a house, inaudible, because the sky
            // was louder than the ship.
            pad: 0.028f * (1f - inside),

            // And the drive, which is the one voice in the film that does something. Chapter 7 ran it at
            // three hundredths and never moved it, because a ship holding station is a ship holding
            // station. This is a ship leaving, so it comes up over the four hundred metres after the door
            // — distance rather than seconds, so it is tied to the departure and not to a stopwatch.
            drive: 0.055f * Ramp(past, 0f, 340f),
            draught: 0.019f * (1f - inside) * Falloff(Illuminator.OffGlass(eye), 2.4f),
            plot: 0.026f * Near(eye, Illuminator.Table, 2.2f),

            // Nine hundredths, and it was three tenths on the first pass. The number that settled it was not this
            // chapter's at all — it was the engine room's. A-weighted, a berth at three tenths came out ten
            // decibels <i>above</i> the plant room, which would have made a parked ship the loudest place
            // in the film and the room with three machines running in it the second loudest. The bay's
            // noise reaches fifteen hundred hertz where the plant room's stops at a couple of hundred, and
            // A-weighting is exactly the instrument that notices, which is why it is the one this bank has
            // always been set with. It now sits a decibel under the engine room: the biggest space in the
            // film and not the busiest one.
            berth: 0.092f * inside,

            // And the consoles, at half of what they were, for the same reason and measured the same way.
            // Their band is where the ear is most sensitive, so a level that looks modest next to a drive
            // is not modest at all — and the last fifty seconds of the film have to end up within a few
            // decibels of chapter 7's window, because it is the same room.
            console: 0.030f);

        // The clamps, and then the door. Both are one-shots on the moment the picture has them, and the
        // door is fired Door.Opening early so that the <i>stop</i> — the only part of that cue that carries
        // any meaning — lands on the frame the gate goes green.
        if (Crossed(Illuminator.Unclamps, into, from, jumped))
            _speaker.Play(_cues.Clamp, 0.80f);

        if (Crossed(Morning.Cleared - Door.Opening, into, from, jumped))
            _speaker.Play(_cues.Servo, 0.68f);

        Consoles(into, from, jumped, eye);

        // The hull, once there is a star on one side of it again. Inside the bay there is nothing to tick:
        // a plate that has been sitting in a heated berth all night is the same temperature all over, which
        // is the one place in the film where the reason a sound is absent is thermodynamics.
        Hull(into, from, jumped, 1f - inside, eye, chapter.Duration);
    }

    /// <summary>
    /// The consoles answering, four tones and about one every five seconds.
    ///
    /// Sparse rather than periodic, for the same reason the hull is: a machine that reports on the beat is
    /// a metronome. And quiet — a fortieth at the console bank and less than half that from the far end of
    /// the room — because these are somebody else's screens acknowledging somebody else's checklist, and
    /// the moment they are loud enough to be counted they become a thing that is happening to him.
    /// </summary>
    /// <param name="reach">How far the bank carries. Fourteen metres for the chapter, which is standing in
    /// that room and wants the far end of it audible; half that for the free walk, which is the same bank
    /// heard from eight rooms away and has no business being heard in most of them.</param>
    /// <param name="wall">What is between him and it — see <see cref="Through"/>. One for the chapter, which
    /// is in the room with them.</param>
    private void Consoles(float into, float from, bool jumped, Vector3 eye, float reach = 14f, float wall = 1f)
    {
        foreach (var word in Sparse(from, into, jumped, 5.1f, 0x2FA6D3))
            _speaker.Play(
                _cues.Pips[word % (uint)_cues.Pips.Length],
                (0.050f + 0.030f * Roll(word, 0)) * Near(eye, Illuminator.Table, reach) * wall);
    }

    // ---- the building, once it is his ---------------------------------------------------------------

    /// <summary>
    /// The free walk. Nine minutes of film end, somebody takes the controls, and the score stops being a
    /// timeline and becomes a map.
    ///
    /// <b>Every chapter above is a function of a second, and not one line of it survives here.</b> A chapter
    /// knows where the ear is because it put it there; nobody knows where this ear is going. So the two
    /// numbers a room contributes on its own — the air in it and the hum of its own lighting — come out of
    /// <see cref="Ambience"/> by name, and everything else in the building is a distance to the thing making
    /// it. Walk towards the plant room and the machines come up; walk to the glass and the vent does. Six
    /// voices, no schedule, and nothing to keep in step.
    ///
    /// <b>What it was written for is the lights.</b> The rule this demo is built to is that a light that
    /// changes has to be heard changing, and the free walk was the one place in nine minutes where none of
    /// them were. Two things change: the room he walks into lights up, and the lounge hands its four slots
    /// to the mirror ball and takes them back. Both are a bank of lamps striking, both fire
    /// <see cref="Cues.Lamp"/>, and the lounge's mains hum goes out on the same ramp its fittings do — which
    /// is the half of it nobody would notice and everybody would hear.
    ///
    /// The ship is the one thing that is not a distance. A shaft the size of a house runs through the
    /// structure he is standing on, so it is the same in every room in the building, which is also the only
    /// thing in the free walk that says he is still on board.
    /// </summary>
    /// <param name="now">The film's own clock, which goes on running. The show's beat is on it rather than
    /// on chapter time, because <c>Rounds</c> drives the lights from it and the two have to agree.</param>
    private void Wandering(float now, float into, float from, bool jumped)
    {
        var eye = _stood;
        var (air, ballast) = Ambience(_room);

        Beds(
            air: air,

            // The hum belongs to the fittings and not to the room, so the lounge loses it for as long as its
            // four warm lamps are off. Everywhere else the fade is nought and this is the table's number.
            ballast: ballast * (1f - _show),

            // Two tenths of what it is at the bench, six metres out. Chapter 6 stands at that bench and runs
            // this bed at 0.26 flat, and that is what the reach was set against — walk to where the film
            // stood and you get the film's plant room, walk up to the machines and you get twice it, which
            // is the reward for a room the film only ever looks at from the door.
            plant: 0.55f * Near(eye, EngineRoom.Engines, 6f) * Through(Deck.EngineRoom),

            // Chapter 2's own expression, unchanged. It was already a distance, and the free walk stands
            // where that chapter stood.
            motor: 0.30f * Near(eye, Rotunda.ArmAt, 3.2f) * Through(Deck.RotundaRoom),

            // The window's four, all measured off the glass or off the table rather than given a level.
            // Walking the gallery is walking from a projector to a vent, which is what chapter 7 does with
            // the same two expressions and is the only reward that room offers for crossing it.
            pad: 0.028f * Falloff(Illuminator.OffGlass(eye), 3f) * Through(Deck.IlluminatorRoom),
            draught: 0.024f * Falloff(Illuminator.OffGlass(eye), 2.4f) * Through(Deck.IlluminatorRoom),
            // Two projectors, one cue. The gallery's chart table is a fan and a coil measured by how close
            // he is standing to it; the dome's is the same machine at a fixed level, because a planetarium
            // projector is in the middle of the room and there is nowhere in that room to get away from it.
            plot: 0.026f * Near(eye, Illuminator.Table, 2.2f) * Through(Deck.IlluminatorRoom)
                  + 0.050f * Through(Deck.PlanetariumRoom),
            console: 0.034f * Near(eye, Illuminator.Table, 6f) * Through(Deck.IlluminatorRoom),

            // And the ship, which is the one voice here that is not a distance to anything. A shaft the size
            // of a house runs through the structure he is standing on: it arrives through the deck rather
            // than through the air, so it is the same in the antechamber as it is at the window, and it is
            // the only thing in the free walk that says he is still on board.
            drive: 0.050f);

        Relay(now, jumped);
        Cabinets(eye, into, from, jumped);

        // The show, on the tempo its own lights are flashing to — see Rounds.Thump, which is the same four
        // divisions of the bar chapter 4 uses. It is gated on the fade rather than on the room, so it comes
        // up with the lamps and is inaudible everywhere else because the fade is nought there.
        if (_show > 0.01f && Every(Beat * 0.5f, now, jumped, ref _beat, out var half))
            _speaker.Play(half % 2 == 0 ? _cues.Kick : _cues.Tick, _show * (half % 2 == 0 ? 0.40f : 0.24f));

        Consoles(into, from, jumped, eye, 7f, Through(Deck.IlluminatorRoom));

        // And the hull, everywhere, for as long as he cares to walk about in it. No end to stop short of:
        // there is no cut after this one, which is the whole of what the hand-over means.
        Hull(into, from, jumped, 1f, eye, float.MaxValue);
    }

    /// <summary>
    /// How much of something standing in <paramref name="where"/> reaches the room he is actually in: all of
    /// it in that room, and a fifth of it anywhere else.
    ///
    /// <b>A wall, and the free walk is the only part of this film that needed one.</b> Every chapter's
    /// distance is to something in the room the chapter is standing in, so nine minutes of score got away
    /// with an inverse square and no geometry at all. Handing the building over breaks that the moment two
    /// rooms are back to back — and two of them are. The engine room's bench stands <i>three metres</i> from
    /// the gallery's plot table with a bulkhead between them, and on distance alone a bank of consoles in the
    /// next room came out within a couple of decibels of what it is when you are leaning on it.
    ///
    /// So the honest answer is not a steeper falloff, because the thing in the way is not more air. It is a
    /// steel wall with an open door in it, and about fourteen decibels is what that is worth. One factor,
    /// applied to everything that lives in a room, and to nothing that does not: the air and the hum are the
    /// room's own — see <see cref="Ambience"/> — and the drive arrives through the deck.
    /// </summary>
    private float Through(string where) => _room == where ? 1f : 0.2f;

    /// <summary>
    /// A bank of lamps striking, whenever the building changes which ones are lit.
    ///
    /// Two things do that and there is no third. He walks into a room, and the room lights for him — which
    /// is not a trick played on the light budget but the plainest reading of what the budget already does,
    /// a building at night lighting whoever is in it. And the lounge goes over to the ball, or comes back,
    /// which swaps four warm lamps for four coloured ones.
    ///
    /// Both are watched rather than announced. <c>Rounds</c> is the picture and says nothing to this file;
    /// what it exposes is a room name and a flag, and a change in either is a change in the lighting the
    /// same way a clock crossing a number is a change in a chapter. Which means there is nothing to keep in
    /// step and nothing that can be forgotten when somebody adds a ninth room.
    ///
    /// The one piece of state is when the last one fired, and <see cref="Rethrow"/> says why.
    /// </summary>
    private void Relay(float now, bool jumped)
    {
        // Arriving rather than moving: the first free frame has walked into a room from nowhere, and a jump
        // has walked into one from somewhere nobody was listening. Neither is a lamp.
        if (jumped)
        {
            _lit = _room;
            _danced = _ball;
            return;
        }

        var moved = _room != _lit;

        if ((!moved && _ball == _danced) || now - _relay < Rethrow)
            return;

        // A room is four fittings and the alcove's changeover is four smaller ones, which is most of the
        // difference between the two levels. The rest of it is that one of them happens where he is
        // standing and the other happens over his head.
        _speaker.Play(_cues.Lamp, moved ? 0.52f : 0.42f);

        _relay = now;
        _lit = _room;
        _danced = _ball;
    }

    /// <summary>
    /// The four televisions once nobody is scheduling them.
    ///
    /// Same games and the same question <see cref="Games"/> asks — what did you do between these two
    /// seconds — on the clock <c>Rounds</c> draws them at, so the sound cannot report a move the screen is
    /// not making. What is different is that there is no waking. The sets are on, all four of them, whether
    /// he is in the room with them or in the plant room, and the only thing that decides whether he hears
    /// one is how far away he is standing.
    ///
    /// <b>That is the report this was written for.</b> The free walk used to run the last chapter's score
    /// forever, so the cabinets played at whatever level the film's last walk had left them — which was a
    /// man at a window sixty metres away, and therefore silence, in the one room in the building with four
    /// televisions in it.
    /// </summary>
    private void Cabinets(Vector3 eye, float into, float from, bool jumped)
    {
        if (jumped)
            return;

        var wall = Through(Deck.ScreensRoom);

        for (var set = 0; set < _lounge.Sets; set++)
        {
            var near = 0.85f * Near(eye, ScreenRoom.Glass(set), 2.4f) * wall;

            // A voice started to play a four-thousandth is a voice started to play nothing, and two rooms
            // away every set is already under it. Asking four games what they did is cheap and it is not
            // free, and this is the only test in the free walk that exists to save work rather than to be
            // true — it happens to be both.
            if (near < 0.004f)
                continue;

            var (game, _) = _lounge.Set(set);
            var offset = set * Rounds.Stagger;

            game.Moves(from + offset, into + offset, (move, weight) =>
                _speaker.Play(
                    _cues.Cabinet[set][(int)move],
                    Math.Clamp(Loudness(move) * weight, 0f, 1f) * near));
        }
    }

    /// <summary>
    /// What a room sounds like with nothing happening in it: the air moving through it, and the hum of its
    /// own lighting.
    ///
    /// Two numbers a room and no third, because everything else in the free walk is a distance to something
    /// that is running. A table here rather than a property on each room, for the reason the top of this
    /// file gives: the score is one pass over the whole building by somebody who has walked all of it, and
    /// eight numbers spread across eight room files are eight numbers nobody can compare.
    ///
    /// They descend, and that is the film's own shape kept rather than rediscovered — the antechamber is the
    /// noisiest room in the building and the window is the quietest, which is what made the reveal work in
    /// chapter 7 and is still true of the place when nobody is telling a story about it.
    /// </summary>
    private static (float Air, float Ballast) Ambience(string room) => room switch
    {
        Deck.AntechamberRoom => (0.20f, 0.15f),
        Deck.ThresholdRoom => (0.19f, 0.10f),
        Deck.RotundaRoom => (0.17f, 0.15f),
        Deck.MaterialsRoom => (0.16f, 0.15f),

        // The studio, the pattern shop and the link between them. They descend with everything else — see
        // the remarks above — and the studio is the one number in this table that is not the room's size.
        // It is a room whose whole beat is somebody taking the light out of it, and the free walk hands it
        // over lit; a hair under the gallery is what says "this is the quiet end of the floor" without
        // saying it twice.
        Deck.StudioRoom => (0.15f, 0.13f),
        Deck.PatternRoom => (0.16f, 0.15f),

        // The planetarium, and the one number in this table that breaks the descent on purpose. Every other
        // room here is as quiet as its size and its machinery make it; an auditorium is as quiet as it was
        // built to be, because that is what an auditorium is. A carpeted floor, a domed ceiling and no hard
        // parallel surfaces anywhere is a room with the reverberation taken out of it, and the number has
        // to say so or the sound will be describing a different room from the picture.
        Deck.PlanetariumRoom => (0.085f, 0.05f),

        Deck.LinkRoom => (0.13f, 0.10f),

        Deck.ScreensRoom => (0.14f, 0.14f),
        Deck.CorridorRoom => (0.10f, 0.09f),

        // The plant room's own bed is the machines, and they are loud. Its air is the thinnest indoors.
        Deck.EngineRoom => (0.07f, 0.05f),

        // And the gallery, which has four voices of its own arriving by distance and needs the room out of
        // their way to be heard at all. Chapter 7 takes the air to a fifteenth to make that point; this
        // keeps a little more of it, because the point has been made and he is only standing there.
        Deck.IlluminatorRoom => (0.060f, 0.030f),

        _ => (0.12f, 0.08f)
    };

    // ---- the machinery ------------------------------------------------------------------------------

    /// <summary>
    /// Sets the eight beds. Anything not named is silent, which is what makes a chapter's line readable as
    /// the whole of its ambience rather than as a diff against the chapter before it.
    ///
    /// It is also what enforces the story's sixth rule for free. The last three are the ship — see
    /// <see cref="Cues.Drive"/> — and a rule that says "no spacecraft before the window" is checkable by
    /// searching this file for their names, because a chapter that does not name a bed is a chapter in which
    /// that bed is nought. Only a chapter at or after the illuminator is allowed to be in the search result.
    /// </summary>
    private void Beds(
        float air = 0f,
        float ballast = 0f,
        float plant = 0f,
        float motor = 0f,
        float pad = 0f,
        float drive = 0f,
        float draught = 0f,
        float plot = 0f,
        float berth = 0f,
        float console = 0f)
    {
        _air.Volume = air * Room;
        _ballast.Volume = ballast * Room;
        _plant.Volume = plant * Room;
        _motor.Volume = motor * Room;
        _pad.Volume = pad * Room;
        _drive.Volume = drive * Room;
        _draught.Volume = draught * Room;
        _plot.Volume = plot * Room;
        _berth.Volume = berth * Room;
        _console.Volume = console * Room;
    }

    /// <summary>
    /// One trim across every bed, and the only thing in the score that is a mix decision rather than a fact
    /// about a room.
    ///
    /// The film's ambience was five decibels too loud for everything happening in front of it. Measured off
    /// the rendered tape: the beds held a median fifty-millisecond peak of 0.15 while a footstep — which
    /// plays at four tenths times an effort that is well under one at this walking pace — arrived at about
    /// the same number. Doors and lamps did better, at 0.5 to 0.6, but that is only eleven decibels over the
    /// room, and eleven decibels under a continuous wideband bed is where a listener stops hearing events
    /// and starts hearing weather. The report was "it is just background sound", which is exactly right.
    ///
    /// <b>A single factor rather than ten edited numbers</b>, because the ten were not guessed. Chapter by
    /// chapter they were set against each other by A-weighted measurement — the berth against the engine
    /// room, the vacuum against the gallery — and every one of those relationships survives being multiplied
    /// by the same constant. What changes is only how far the room sits below the things in it.
    /// </summary>
    private const float Room = 0.62f;

    /// <summary>
    /// The four televisions, each making the noise its own game is making.
    ///
    /// <b>What was here before is the thing this replaces</b>, and it is worth writing down because it looked
    /// perfectly reasonable. Every one and seven tenths of a second, one of three square-wave blips came out
    /// of one of the four sets, chosen off a counter so it was at least the same on every run. It was cabinet
    /// noise, and it had two faults that no amount of tuning would have fixed. It came out of televisions that
    /// were <i>switched off</i> — the sets wake at sixteen, twenty-three, twenty-nine and thirty-five seconds,
    /// and the blips started at nought. And it had nothing to do with what was on any screen: a maze being
    /// cleared, a piece landing and a man jumping a pipe all sounded the same, on somebody else's clock.
    ///
    /// Now each set is asked what its game did since the last frame, and the answer comes from the same
    /// expressions that drew it — see <see cref="ArcadeScene.Moves"/>. A jump is heard on the frame the
    /// character leaves the ground; a coin goes as the coin stops being drawn; a row clears when the row
    /// clears. Nothing is counted and nothing is kept, so a seek needs no resynchronising: ask a game what it
    /// did between two seconds and it will tell you, whether or not anybody watched the ones before.
    ///
    /// A set that has not woken makes no sound at all, which is what a dark television does.
    ///
    /// The level is the game's own, times how far away that set is standing. So walking down the bench is the
    /// four of them arriving one at a time and each one loudest as he reaches it, and sitting down at the far
    /// end of the room is all four at a quarter, which is what four televisions across a lounge sound like.
    /// </summary>
    private void Games(Screens lounge, float into, float from, bool jumped)
    {
        if (jumped)
            return;

        var eye = Eye(into);

        for (var set = 0; set < lounge.Sets; set++)
        {
            var (game, wake) = lounge.Set(set);

            if (into < wake)
                continue;

            var near = 0.85f * Near(eye, ScreenRoom.Glass(set), 2.4f);

            // Clamped, because a weight is a quantity and not a level: four rows going at once is four times
            // one row and is not four times as loud as anything ever gets.
            game.Moves(from - wake, into - wake, (move, weight) =>
                _speaker.Play(
                    _cues.Cabinet[set][(int)move],
                    Math.Clamp(Loudness(move) * weight, 0f, 1f) * near));
        }
    }

    /// <summary>
    /// How loud each kind of move is, before distance.
    ///
    /// A coin is the quiet one and it has to be: the maze eats seven a second for as long as it is on, and at
    /// the level a jump wants it would be the only thing in the room. Everything here is a judgement about
    /// how often the move happens as much as about what it is.
    /// </summary>
    private static float Loudness(Move move) => move switch
    {
        Move.Coin => 0.15f,
        Move.Drop => 0.30f,
        Move.Land => 0.26f,
        Move.Near => 0.34f,
        Move.Clear => 0.30f,
        _ => 0.40f
    };

    /// <summary>
    /// One alarm cue per turn of the beacons, at whatever level the chapter is holding it at.
    ///
    /// The clip is exactly a turn long, so consecutive firings tile with no gap and no overlap, and the
    /// clock is the same one the beacons are turned by — so what is heard cannot drift against what is
    /// seen. Three chapters call this with three clocks that are the same clock offset by the chapters
    /// before them, which is how the alarm survives two joins without restarting.
    /// </summary>
    private void Turning(float clock, bool jumped, float level)
    {
        if (level <= 0.005f)
        {
            // Kept in step while it is inaudible, so that coming back up mid-chapter does not land on a
            // half turn.
            Every(Cues.Turn, clock, true, ref _turn, out _);
            return;
        }

        if (Every(Cues.Turn, clock, jumped, ref _turn, out _))
            _speaker.Play(_cues.Klaxon, level);
    }

    /// <summary>
    /// Footsteps, from the walk rather than from a cadence.
    ///
    /// The film's walk is a pure function of the second, so how far he went this frame is two evaluations
    /// of it and a subtraction — and a step fired every three quarters of a metre of that is a step that
    /// speeds up when he does, hesitates at every waypoint, and stops dead when he stands still and looks
    /// at something. None of which anybody has to write down. A footstep timer would need all three
    /// behaviours as special cases and would still be wrong at the joins.
    ///
    /// Height is thrown away before the distance is taken. Nothing in this building has stairs, and the
    /// walk raises and lowers the eye a little to lean in at exhibits; counting that as ground covered puts
    /// a footstep in the middle of a man leaning towards a plinth.
    /// </summary>
    private void Footfalls(Chapter chapter, float into, float from, bool jumped)
    {
        if (jumped || chapter.Walk is not { } walk)
            return;

        var here = walk.At(into).Eye;
        var there = walk.At(from).Eye;

        Tread(
            MathF.Sqrt((here.X - there.X) * (here.X - there.X) + (here.Z - there.Z) * (here.Z - there.Z)),
            into - from,
            0.40f);
    }

    /// <summary>
    /// Puts a foot down every stride, where <b>the stride depends on how fast he is going</b>.
    ///
    /// A fixed stride is the obvious model and it is wrong in the way that produced the complaint this was
    /// written for. Distance over a fixed stride means the cadence is proportional to the speed, so the walk
    /// into the alarm corridor — one and a fifth metres in seven seconds, because he is crossing a threshold
    /// into a room that is already doing something — got a single footstep in the middle of it, and every
    /// eased waypoint in the film got a run of quick steps, a hole, and the identical run again. Measured in
    /// the corridor it was five steps two thirds of a second apart, a second and a half of silence, and then
    /// the same five: not a walk, but a sample being triggered in bursts.
    ///
    /// People do not do that. Slowing down shortens the stride at least as much as it drops the cadence, so
    /// the interval between feet goes as the square root of the speed rather than as the speed — at a third
    /// of the pace the steps are half as far apart as the naive model says, not a third. And below
    /// <see cref="Walk.Standing"/> nothing fires at all, because a man moving at a tenth of a metre a second
    /// is leaning, not stepping. That one line is what silences a hundred seconds of somebody working at a
    /// bench, which no stride length could have.
    ///
    /// The level goes with the pace and alternates between the feet. Both are small and both are the
    /// difference between a gait and a metronome: a creeping step is not as loud as a striding one, and
    /// nobody's two feet weigh the same.
    ///
    /// The while, rather than an if, is for the frame a stall made long.
    /// </summary>
    /// <param name="metres">How far he moved.</param>
    /// <param name="seconds">How long that took, so this can know a walk from a lean.</param>
    /// <param name="level">The loudest a step in this chapter gets.</param>
    private void Tread(float metres, float seconds, float level)
    {
        if (seconds <= 1e-4f)
            return;

        var speed = metres / seconds;

        // Not walking. The distance already banked is kept rather than cleared, so the first step after a
        // pause lands promptly — which is what starting to walk sounds like.
        if (speed < Walk.Standing)
            return;

        var stride = Math.Clamp(Stride * MathF.Sqrt(speed / Pace), Shortest, Stride);
        var effort = 0.72f + 0.28f * MathF.Min(1f, speed / Pace);

        _walked += metres;

        while (_walked >= stride)
        {
            _walked -= stride;

            // Cycled rather than chosen at random, and three does not divide four — so a left and a right
            // are never the same sound twice running, and the pattern takes six steps to come round.
            _speaker.Play(
                _cues.Steps[_foot % _cues.Steps.Length],
                level * effort * (_foot % 2 == 0 ? 1f : 0.86f));

            _foot++;
        }
    }

    /// <summary>Where the visitor's eye is at this second of the current chapter, or the middle of the
    /// building when the chapter has no walk.</summary>
    private Vector3 Eye(float into) =>
        _film.Chapters[_chapter].Walk is { } walk ? walk.At(into).Eye : Vector3.Zero;

    /// <summary>
    /// How loud something at <paramref name="what"/> is from <paramref name="ear"/>: one at the source,
    /// a quarter at <paramref name="reach"/>, and never quite nothing.
    ///
    /// An inverse square with a metre of softening in it, which is what a real falloff is and is three
    /// operations. It is here rather than in the player on purpose — a distance model is the first thing
    /// an audio library grows and the first thing that makes it impossible to lift out, and this demo
    /// needs exactly this much of one.
    /// </summary>
    private static float Near(Vector3 ear, Vector3 what, float reach) =>
        Falloff(Vector3.Distance(ear, what), reach);

    /// <summary>
    /// The same falloff over a distance somebody else measured.
    ///
    /// It exists because one thing in the film is not at a point. A window thirteen metres long has a vent
    /// down the whole of it, and how loud that is depends on how far he is standing <i>off the glass</i> and
    /// not at all on how far along it he is — so the distance that matters is to a plane, which
    /// <see cref="Illuminator.OffGlass"/> knows and this does not need to.
    /// </summary>
    private static float Falloff(float away, float reach)
    {
        var d = away / MathF.Max(0.01f, reach);
        return 1f / (1f + d * d);
    }

    /// <summary>Whether <paramref name="when"/> falls in the slice of chapter time this frame covered.</summary>
    private static bool Crossed(float when, float into, float from, bool jumped) =>
        !jumped && when > from && when <= into;

    /// <summary>
    /// Whether a new whole multiple of <paramref name="period"/> has been reached, and which one.
    ///
    /// Rather than a timer, because a timer accumulates and the whole film refuses to. The count is derived
    /// from the clock, so a slow frame skips a beat instead of playing two late, and arriving in the middle
    /// of a chapter resynchronises rather than replaying everything since the top of it.
    /// </summary>
    private static bool Every(float period, float clock, bool jumped, ref long counter, out long n)
    {
        n = (long)MathF.Floor(clock / period);

        if (jumped || counter < 0)
        {
            counter = n;
            return false;
        }

        if (n <= counter)
            return false;

        counter = n;
        return true;
    }

    /// <summary>
    /// The moments of an uneven schedule that fall in the slice of chapter time this frame covered, as one
    /// hashed word each.
    ///
    /// <b>It is a period with the evenness taken out of it, and it keeps every property the rest of this file
    /// depends on.</b> The nth event is at <c>(n + a half + a jitter) * every</c>, where the jitter comes out
    /// of a hash of n — so where an event is depends on nothing except which event it is. There is no state,
    /// no counter, and nothing to resynchronise: asking "which events are in this sixtieth of a second" is
    /// two floors and a comparison, and it gives the same answer whether the film has been playing for a
    /// minute or was dropped into this second from the picker.
    ///
    /// The jitter is kept inside plus or minus forty-two per cent of the period, which is what makes the
    /// schedule <i>monotonic</i> in n — event n is always before event n+1, so a window can be answered by
    /// looking at the two or three values of n that could possibly land in it rather than by searching. Push
    /// it past a half and events start overtaking each other, and then a frame can miss one entirely.
    ///
    /// The word that comes back is thirty-two bits of hash, and <see cref="Roll"/> takes independent numbers
    /// out of it — one for how loud, one for where, and the third is the jitter that put it here. That is
    /// what stops the loud ticks from all being the near ones: three properties of one event, uncorrelated,
    /// out of one integer.
    /// </summary>
    /// <param name="every">Seconds between events on average.</param>
    /// <param name="seed">Which schedule this is. Two schedules with the same period and different seeds are
    /// unrelated, which is how the ticks and the groans share a clock without lining up.</param>
    private static IEnumerable<uint> Sparse(float from, float into, bool jumped, float every, uint seed)
    {
        if (jumped || into <= from || every <= 0f)
            yield break;

        // One either side of the window's own two, because the jitter can carry an event across the boundary
        // of the period it belongs to.
        var first = (long)MathF.Floor(from / every) - 1;
        var last = (long)MathF.Floor(into / every) + 1;

        for (var n = first; n <= last; n++)
        {
            var word = Hash(seed ^ (uint)n);
            var at = (n + 0.5f + (Roll(word, 2) - 0.5f) * 0.84f) * every;

            if (at > from && at <= into)
                yield return word;
        }
    }

    /// <summary>
    /// One of three independent numbers between nought and one out of a hashed word.
    /// </summary>
    /// <param name="nth">Which ten bits to read: 0, 1 or 2.</param>
    private static float Roll(uint word, int nth) => ((word >> (nth * 10)) & 1023) / 1023f;

    /// <summary>
    /// An integer stirred until its bits are unrelated to the ones it came in with.
    ///
    /// Two rounds of multiply-and-shift, which is the standard cheap one. It has to be written out rather
    /// than taken from the framework for the same reason <see cref="Tone.Noise"/> is: the film has to sound
    /// the same on every machine and after every framework update, and a hash whose algorithm is allowed to
    /// change is a hull that ticks in different places next year.
    /// </summary>
    private static uint Hash(uint n)
    {
        n ^= n >> 16;
        n *= 0x7FEB352Du;
        n ^= n >> 15;
        n *= 0x846CA68Bu;
        n ^= n >> 16;

        return n;
    }

    /// <summary>Nought before <paramref name="at"/>, one after it plus <paramref name="over"/>, smooth
    /// between. The same shape <see cref="Chapter.Ramp"/> uses, so a fade here can be written against the
    /// same numbers a chapter fades a lamp on.</summary>
    private static float Ramp(float seconds, float at, float over)
    {
        if (over <= 0f)
            return seconds >= at ? 1f : 0f;

        var u = Math.Clamp((seconds - at) / over, 0f, 1f);
        return u * u * (3f - 2f * u);
    }
}
