using System.Numerics;

namespace Ava3D.Demo.Story;

/// <summary>
/// The building once it belongs to the visitor rather than to the film.
///
/// A chapter knows where the visitor is because it put him there, so it can name the two rooms he can see
/// and spend the four light slots on the one he is in. Nobody knows where he is going once the controls are
/// handed over, and the film's answer to that was to leave the last chapter's arrangement standing — one
/// room lit, seven rooms switched off, and three shut doors between him and any of them. What that produced
/// was a building with exactly one room in it, and a doorway that showed the sky through the wall of the
/// ship, because a doorway with nothing behind it draws whatever is furthest away.
///
/// So this is the same three jobs the chapters do — which rooms exist, where the four lights are, and how
/// much bounce there is — done off the visitor's own position instead of off the clock. It runs after the
/// last chapter's <see cref="Chapter.Update"/> every frame and overrules it, which is the whole reason the
/// film can go on playing underneath: the ship keeps flying, the traffic keeps moving and the hologram keeps
/// turning, and the lights stop being the last chapter's business.
///
/// <b>Three rooms at a time, not eight.</b> The route through the building is a line, so the rooms the
/// visitor can see are the one he is in and the two either side of it — the same load every chapter carries
/// and the reason this costs nothing. There is no frustum culling in the renderer; a building drawn all at
/// once is a building drawn all at once, in a browser tab, at sixty hertz.
///
/// <b>The ground is surveyed with all eight standing, once.</b> That is the one thing that must not follow
/// the occupancy: a wall stops being solid the moment its room is hidden, so a visitor walking away from a
/// room he has just left would walk through its far wall a moment later. The building does not change after
/// the hand-over, so it is measured once with everything up and then drawn three rooms at a time.
/// </summary>
internal sealed class Rounds
{
    /// <summary>
    /// One room, as the free walk needs it: where it is, what it spends its four slots on, and how much
    /// light there is in it that is not coming from a lamp.
    ///
    /// <see cref="Probe"/> is the gallery's and only the gallery's. Plaster is happy with a hemisphere and
    /// metal is not — see <see cref="Hall.Ambient(EnvironmentLight)"/> — and setting a textured environment
    /// prefilters it, so it is assigned when the room changes rather than on every frame.
    ///
    /// <see cref="Live"/> is what the room's own chapter used to do to it every frame and now nobody does:
    /// the drum turning, the televisions playing, the machines breathing. It is run for the room the visitor
    /// is standing in and for no other, which is the same arithmetic the lights are on — an exhibit nobody
    /// is in the room for does not need to be animated, and four of them do not need to be animated at
    /// once on a browser tab.
    /// </summary>
    private sealed record Quarter(
        string Name,
        Light[] Slots,
        float Sky,
        float Ground,
        EnvironmentLight? Probe = null,
        Action<float>? Live = null,
        (float Near, float Far, Vector3 Colour)? Air = null)
    {
        /// <summary>The room's plan, in world metres. Taken from the geometry rather than written down,
        /// for the reason <see cref="Story.Ground"/> gives: a second description of a room's size is a
        /// second description that can disagree with the first.</summary>
        public Vector2 Min { get; set; }

        public Vector2 Max { get; set; }

        /// <summary>How far outside this room a point on the deck is, and nought when it is inside.</summary>
        public float Beyond(float x, float z)
        {
            var dx = MathF.Max(MathF.Max(Min.X - x, x - Max.X), 0f);
            var dz = MathF.Max(MathF.Max(Min.Y - z, z - Max.Y), 0f);

            return MathF.Sqrt(dx * dx + dz * dz);
        }
    }

    /// <summary>How near the ball the visitor has to get for the room to become the show, and how far he
    /// has to get back out for it to stop. Two radii rather than one, because a visitor standing on a single
    /// line switches every lamp in the room on and off at the frame rate.</summary>
    private const float Enters = 4.5f;

    private const float Leaves = 5.2f;

    /// <summary>How long the visitor has to stand still under the ball before he sits down to watch, and how
    /// long the room takes to change over either way.</summary>
    private const float Settles = 4.5f;

    private const float Fades = 1.6f;

    /// <summary>How far apart the four televisions are started, in seconds, so the wall is not four copies
    /// of one loop. Named rather than typed into the lambda below because the score reads it too — a set
    /// whose sound is on a different clock from its picture is a set that reports moves it is not making.
    /// See <c>Soundtrack.Cabinets</c>.</summary>
    public const float Stagger = 7.3f;

    private readonly Hall _hall;
    private readonly Node _outside;
    private readonly Node _air;
    private readonly Door[] _doors;
    private readonly Quarter[] _quarters;
    private readonly Action _wake;
    private readonly ScreenRoom _lounge;
    private readonly Planetarium _dome;
    private readonly int _stage;
    private readonly int _dais;

    private int _standing = -1;

    /// <summary>
    /// The free walk's own state, and the only state in this file.
    ///
    /// Everything else here is a function of where the visitor is standing this frame, which is why it can
    /// be asserted every frame and never drifts. These four cannot be: a fade is a function of <i>when</i> he
    /// crossed the line, and sitting down is a function of how long he has been still — both of which are
    /// facts about the past. They are kept as the moment something happened rather than as a counter that is
    /// added to, so they are still read the same way: a value minus a remembered clock.
    /// </summary>
    private bool _party;

    private float _crossed = float.MinValue;

    private float? _restless;

    private bool _sitting;

    /// <summary>The planetarium's own version of all of the above: whether somebody has come far enough
    /// into the seats for the lights to go down, when they crossed, whether they have settled, and what
    /// haze the room was last given. See <see cref="Showtime"/>.</summary>
    private bool _dark;

    private float _curtain = float.MinValue;

    private float? _still;

    private bool _watching;

    private bool _running;

    private float _haze = -1f;

    public Rounds(
        Hall hall,
        Antechamber antechamber,
        Passage passage,
        Rotunda rotunda,
        Gallery gallery,
        Studio studio,
        PatternShop shop,
        Planetarium dome,
        Link link,
        ScreenRoom lounge,
        Corridor corridor,
        EngineRoom engine,
        Illuminator illuminator)
    {
        _hall = hall;
        _outside = illuminator.Outside;
        _air = illuminator.Air;
        _doors = [gallery.Gate, corridor.Gate, engine.Way];
        _lounge = lounge;
        _dome = dome;

        // In route order, because that is what makes a neighbour a neighbour. The building switchbacks but
        // it does not branch: every room has exactly one before it and one after it, so "the rooms he can
        // see" is a window of three on this list and does not need a graph to say so.
        //
        // Each room spends its slots on its own lamps, in the arrangement the chapter that owns it settled
        // on — the engine room's two high bays and its fill, the lounge's four warm ones, the gallery's four
        // that are not the pair at the entrance. Nothing is reserved for the room next door, and that is
        // deliberate: a doorway onto an unlit room is not a black rectangle, it is a dim room, because the
        // ambient below is a hemisphere over the whole building rather than a light in one of them.
        _quarters =
        [
            new Quarter(Deck.AntechamberRoom,
                [
                    antechamber.Spot.Light, antechamber.House[0].Light,
                    antechamber.House[1].Light, antechamber.House[2].Light
                ],
                0.034f, 0.015f),

            new Quarter(Deck.ThresholdRoom, [passage.Lamp.Light], 0.034f, 0.017f),

            new Quarter(Deck.RotundaRoom,
                [
                    rotunda.Centre.Light, rotunda.Lanterns[0].Light,
                    rotunda.Lanterns[1].Light, rotunda.Lanterns[2].Light
                ],
                0.046f, 0.034f,
                Live: clock =>
                {
                    rotunda.Arm.Update(hall.Scene, clock);
                    rotunda.Pairs.Update(hall.Scene, clock);
                }),

            new Quarter(Deck.MaterialsRoom,
                [
                    gallery.Entry.Light, gallery.OverCase.Light,
                    gallery.OverDrum.Light, gallery.ByDoor.Light
                ],
                0.040f, 0.024f, gallery.Environment,
                clock => gallery.Panels.Update(hall.Scene, clock)),

            // The studio, which is the one room in the building whose exhibits do not all need the lights.
            // It gets its four anyway: three of the eight things standing in here are matcaps and the other
            // five are not, and a free walk that handed somebody a dark room with three glowing spheres in
            // it would be handing them the last ten seconds of chapter 4 rather than the room.
            //
            // The band lamp is put back to the middle of its rail, because the film leaves it at the east
            // end and the rail is the one thing in the building that is somewhere different depending on
            // when you arrived.
            new Quarter(Deck.StudioRoom,
                [
                    studio.Band.Light, studio.Palette.Light,
                    studio.Entry.Light, studio.Way.Light
                ],
                0.030f, 0.018f,
                Live: _ => studio.Slide(0f)),

            // The pattern shop, and the only room in the building with an exhibit that has to be driven
            // for the picture to be right rather than for it to be alive: the flipbook shows frame zero
            // until somebody advances it, and a sixteen-cell sheet stopped on its first cell is a sprite
            // with a number on it.
            new Quarter(Deck.PatternRoom,
                [
                    shop.OverColours.Light, shop.OverWindows.Light,
                    shop.OverKit.Light, shop.OverPrint.Light
                ],
                0.034f, 0.020f,
                Live: clock => shop.Windows.Update(hall.Scene, clock)),

            // The planetarium, and the one room in the building whose four slots are not four lamps.
            //
            // Three coves and a star. The star is the show's own — see Sky.Star — and it is here for the
            // same reason the sun is in the engine room's list: it lights something that is drawn whether
            // or not anybody is standing under it, and a light that is not in the four slots is not
            // shining on anything at all. Without it the planet crossing the dome is a flat disc.
            //
            // The show runs on the film's clock rather than on a clock of its own, at full, for ever. That
            // is the whole point of Sky having no beginning: the free walk does not start it, join it or
            // loop it, it just reads it, and somebody who comes back an hour later finds a sky an hour
            // further round.
            //
            // The house lights stay up, at a fifth. A visitor with the keys walking into a blacked-out
            // room could not see the four photographs the room is half made of; at full they would wash
            // out the thing over them. See the hand-over below, which is the only lamp in the building not
            // put back to full.
            new Quarter(Deck.PlanetariumRoom,
                [
                    dome.Cove[0].Light, dome.Cove[1].Light, dome.Cove[2].Light, dome.Cove[3].Light
                ],
                0.050f, 0.026f),

            new Quarter(Deck.LinkRoom, [link.Turn.Light, link.Run.Light], 0.030f, 0.017f),

            new Quarter(Deck.ScreensRoom,
                [
                    lounge.Bench[0].Light, lounge.Bench[1].Light,
                    lounge.Lounge.Light, lounge.Doorway.Light
                ],
                0.062f, 0.030f,
                Live: clock =>
                {
                    // Four televisions playing four different games, each started at its own moment so the
                    // wall is not four copies of one loop. The offsets are arbitrary and only have to be
                    // unequal, which is the whole of what makes a room full of screens read as a room full
                    // of screens.
                    for (var set = 0; set < lounge.Games.Length; set++)
                        lounge.Games[set].Show(lounge.Screens[set], clock + set * Stagger);
                }),

            // The corridor keeps its air. It is the only room in the building with any — see Hall.Air and
            // Alarm.Haze — and the free walk is the one place somebody can stand at the lounge end of it
            // and look the whole twenty-one metres down, which is the shot the fog was put there for and
            // the one the film only ever takes while running.
            new Quarter(Deck.CorridorRoom, [.. corridor.Lights], 0.030f, 0.015f,
                Air: (2f, 20f, new Vector3(0.26f, 0.115f, 0.085f))),

            // <b>The star is in this room's list and it is not a mistake.</b> The window is one open
            // doorway away, and while it is drawn the planet on the far side of it is drawn too — so the
            // slot the star occupies is not this room's lighting, it is the daylight for everything outside
            // the ship. Spending it here was the answer to two reports that turned out to be one: crossing
            // out of the gallery put the engine room into darkness and put the planet out with it, because
            // a directional light that is not in the four slots is not shining on anything at all. Where a
            // sun is does not depend on which room somebody is standing in.
            //
            // It costs the room's own fill lamp, which is the one of the four with nothing under it.
            new Quarter(Deck.EngineRoom,
                [
                    illuminator.Sunlight,
                    engine.High[0].Light, engine.High[1].Light, engine.Bay.Light
                ],

                // Nearly twice what chapter 6 gives it, and the room is the reason. It is seventeen metres
                // by thirteen by six and a half with four lamps in it, and the film gets away with that
                // because the walk never leaves the bench at the north end. Somebody with the keys crosses
                // the floor, and at the chapter's own ambient the middle of the room is a place where the
                // torch has gone out.
                0.045f, 0.022f,
                Live: clock =>
                {
                    // A room at work with nobody in it, which is what an engine room is most of the time.
                    // Every one of these was left where chapter 7 dropped it — and chapter 7 leaves by
                    // switching the bench off behind him, so what the visitor walked back into was four
                    // high bays over a black floor and not one lit face anywhere. The paper on the bench
                    // stays off, because that is a job that was finished; the machines, the indicators and
                    // the fan are the room itself.
                    //
                    // The terminal is left on the screenful chapter 7 finished with rather than put back
                    // to an earlier one. Nobody has touched it since he walked out.
                    engine.Backlight(1f);
                    engine.Show(EngineRoom.Onscreen.Overview, 1f);
                    engine.Seat(1f, 1f, 1f);
                    engine.Plug(1f);
                    engine.Fan(clock, 1f);
                    engine.Machines(clock, 1f, 1f);
                    engine.Panel.Update(hall.Scene, clock);
                }),

            // Two, and there is no third worth having. The berth's floods and the gate the ship came out
            // through are both a long way astern by now and outside their own range; what lights this room
            // is the star and the escort's drive, and everything else in it is emission — see
            // Illuminator.Trim, which the last chapter holds at full.
            new Quarter(Deck.IlluminatorRoom, [illuminator.Sunlight, illuminator.Running], 0.052f, 0.022f)
        ];

        foreach (var quarter in _quarters)
        {
            var (min, max) = Plan(hall.Room(quarter.Name));

            quarter.Min = min;
            quarter.Max = max;
        }

        _stage = Array.FindIndex(_quarters, q => q.Name == Deck.ScreensRoom);
        _dais = Array.FindIndex(_quarters, q => q.Name == Deck.PlanetariumRoom);

        // Every lamp in the building, up, and the two rooms that end their chapters mid-performance put
        // back to rest. It is one action rather than eight because it happens once, at the hand-over: a
        // chapter dims what it is finished with, so by the last frame of the film the antechamber's house
        // lights, the rotunda's lanterns and four of the gallery's six are all at nought, and a visitor who
        // walks back to them would find not a dark room but a room with dead fittings hanging in it.
        _wake = () =>
        {
            foreach (var lamp in new[] { antechamber.Spot, passage.Lamp, rotunda.Centre, engine.Bay, engine.Task, engine.Fill })
                lamp.Dim(1f);

            foreach (var lamp in antechamber.House)
                lamp.Dim(1f);

            foreach (var lamp in rotunda.Lanterns)
                lamp.Dim(1f);

            foreach (var lamp in gallery.All)
                lamp.Dim(1f);

            // The studio, the pattern shop and the link between them, which were missing and were missing
            // in the way that is hardest to see from the code: every other room in this list is left part
            // lit by the chapter that finishes in it, and these three are the only ones the film leaves at
            // nought. Chapter 4 ends by turning the studio off — that is its whole beat — and chapter 5
            // hands the shop's five lamps away one at a time on its way out of the door. So a visitor who
            // walked back into either of them found a room that was not dim, it was black, with four light
            // slots correctly assigned to four lamps that were all switched off.
            //
            // It was reported as the lamp being heard and not seen, which is exactly right and is the score
            // telling the truth: Soundtrack.Stands plays a room the moment he crosses into it, and the room
            // it was announcing had nothing in it.
            foreach (var lamp in studio.All)
                lamp.Dim(1f);

            foreach (var lamp in shop.All)
                lamp.Dim(1f);

            // The dome's four coves at full and its fifth off, which is what the film leaves it at and is
            // now the right answer rather than a compromise. It used to be a fifth: the sky ran for ever
            // under this room and house lights at full washed it out, so the room was handed over dim and
            // a visitor with the keys could not read the four photographs it is half made of. There is no
            // sky here until somebody sits down now — see Showtime — so a visitor walks into a lit room
            // with a white ceiling, which is what a planetarium looks like when there is nothing on.
            for (var i = 0; i < dome.Cove.Length; i++)
                dome.Cove[i].Dim(i < 4 ? 1f : 0f);

            dome.Show.Off();
            dome.Running(0f);

            foreach (var lamp in link.All)
                lamp.Dim(1f);

            foreach (var lamp in lounge.Warm)
                lamp.Dim(1f);

            foreach (var lamp in engine.High)
                lamp.Dim(1f);

            // The alcove's show, stopped rather than left wherever chapter 4 abandoned it — see
            // ScreenRoom.Blackout, which exists for exactly this and is called by every chapter that shows
            // the room without driving it.
            lounge.Blackout();

            // The alarm is over. The beacons stop turning, and the four slots they own become what a
            // corridor has when nothing is wrong: working lights, warm, one every five metres.
            //
            // Rewriting a light rather than adding one is what the four-slot budget costs here, and it is
            // safe because the film is behind us — nothing seeks back into chapter 5 from the free walk,
            // and picking the exhibition again builds a new Film with a new corridor in it.
            corridor.Glow(0f, 0f);
            corridor.Rest();

            for (var i = 0; i < corridor.Lights.Length; i++)
            {
                corridor.Lights[i].Position = Corridor.Ahead(3f + i * 5f) + new Vector3(0f, 0.6f, 0f);
                corridor.Lights[i].Color = new Vector3(1f, 0.90f, 0.78f);
                corridor.Lights[i].Intensity = 3.2f;
                corridor.Lights[i].Range = 7f;
            }
        };
    }

    /// <summary>
    /// The building handed over: every room standing, every powered door open, every lamp lit.
    ///
    /// Called once, on the frame the film runs out, and the caller surveys the ground immediately after —
    /// which is why everything is visible when this returns and why the doors are opened before it does. A
    /// survey taken with the doors shut is a survey with three walls across the route.
    /// </summary>
    public void Open()
    {
        _standing = -1;

        _hall.Occupy([.. _quarters.Select(q => q.Name)]);

        // Wide open and stopped there. A door in this film is a function of its chapter's clock and there
        // is no chapter left to drive these, so they are set once — which also means the leaves are inside
        // the walls when the ground is measured, and a doorway is a doorway rather than a slab.
        foreach (var door in _doors)
            door.Open(1f);

        _wake();

        // <b>And then say so, because nothing else will.</b> Every chapter finishes its own Update with
        // this call and the hand-over is the one moment in the film with no chapter left to make it — so
        // the several dozen materials the wake-up rewrites were being written and not uploaded, and what
        // is on screen was whatever they were built with. It cost one frozen alarm beacon across a quiet
        // corridor to find, which is the cheapest possible version of this bug: the same omission would
        // hide any lamp, any lens and any screen the hand-over touches.
        _hall.Scene.Invalidate();
    }

    /// <summary>
    /// Where the visitor is standing, and everything that follows from it.
    ///
    /// Run every frame, after the last chapter has had its say. The ambient is asserted every frame because
    /// the chapter asserts its own every frame and the last writer wins; the rooms and the light list are
    /// only rebuilt when he actually changes room, because both are a list the renderer walks.
    /// </summary>
    /// <param name="eye">Where he is standing, in world metres.</param>
    /// <param name="clock">The film's clock, which goes on running after the film has stopped.</param>
    /// <param name="walking">Whether he is pushing the controls this frame. See <see cref="Party"/>.</param>
    /// <returns>Where he should be if he has sat down to watch, and nothing if he is on his feet.</returns>
    public Vector3? Stand(Vector3 eye, float clock, bool walking)
    {
        var here = 0;
        var best = float.MaxValue;

        for (var i = 0; i < _quarters.Length; i++)
        {
            // Nought for every room he is inside the plan of, so ties are broken by the middle — the
            // passage overlaps the antechamber's doorway and the corridor's end wall stands inside the
            // engine room's bulkhead, and a threshold is in both rooms by construction.
            var beyond = _quarters[i].Beyond(eye.X, eye.Z) + 0.001f * Middle(i, eye);

            if (beyond >= best)
                continue;

            best = beyond;
            here = i;
        }

        var quarter = _quarters[here];

        if (here != _standing)
        {
            _standing = here;

            _hall.Occupy(Neighbourhood(here));
            _hall.Use(quarter.Slots);

            // Everything past the glass is nineteen hundred metres of station, planet, starfield and
            // traffic, and it is drawn whenever its room is. Hanging it on the last two rooms rather than
            // on the illuminator alone is the doorway rule again: the engine room's north door is open now
            // and the window is straight through it.
            _outside.IsVisible = here >= _quarters.Length - 2;

            // The dust, on the other hand, is only for the room it is in — see Illuminator.Air. It is the
            // same three hundred points either way; what changes is whether there is enough parallax to
            // read them as air rather than as sky.
            _air.IsVisible = here == _quarters.Length - 1;

            if (quarter.Probe is { } probe)
                _hall.Ambient(probe);

            // And the air, which one room has and the rest do not. Set on the threshold rather than every
            // frame, because Hall.Air invalidates the scene and a scene invalidated every frame is a scene
            // rebuilt every frame — see Hall.Ambient(float, float), which mutates in place for exactly
            // that reason and is the only thing in this method that can afford to be asserted.
            if (quarter.Air is { } air)
                _hall.Air(air.Near, air.Far, air.Colour);
            else
                _hall.Clear();
        }

        if (quarter.Probe is null)
            _hall.Ambient(quarter.Sky, quarter.Ground);

        quarter.Live?.Invoke(clock);

        return Party(eye, clock, walking, here == _stage)
               ?? Showtime(eye, clock, walking, here == _dais);
    }

    /// <summary>
    /// The ball's half of the lounge: walk under it and the room becomes the show, stand still and you sit
    /// down to watch it, touch the controls and you are back on your feet.
    ///
    /// It is the one place in the building that answers to the visitor rather than to where he is, and the
    /// reason is the four slots. The show needs all four and the room's warm lamps are all four, so this is
    /// not a light being added — it is the room being handed over and handed back, and something has to
    /// decide when. A radius round the stage is that something: it is what a person crossing a floor towards
    /// a thing already means, so nothing has to be pressed.
    ///
    /// <b>The sitting is not a cutscene.</b> He is eased to the seat and the look is never touched, so the
    /// orbit goes on working the whole time he is in it — which matters, because the show is behind and above
    /// and around him and a fixed head would see about a fifth of it. The first push on the controls takes
    /// the seat away again and the walk carries on from where the sofa is, which is what standing up is.
    /// </summary>
    private Vector3? Party(Vector3 eye, float clock, bool walking, bool inside)
    {
        _restless ??= clock;

        var stage = new Vector2(
            Deck.Screens.X + ScreenRoom.ApseAt.X,
            Deck.Screens.Z + ScreenRoom.ApseAt.Z);

        var near = inside &&
                   Vector2.Distance(stage, new Vector2(eye.X, eye.Z)) < (_party ? Leaves : Enters);

        if (near != _party)
        {
            _party = near;
            _crossed = clock;

            // The handover, and it only happens while he is in the room. Crossing out of the lounge
            // altogether also ends the show — but the room he has walked into has just taken the four slots
            // for itself, and giving them back to a party in the room behind him would put it in the dark.
            if (inside)
                _hall.Use(near ? [.. _lounge.Beams] : _quarters[_stage].Slots);
        }

        var into = Ramp(clock - _crossed, Fades);
        var level = _party ? into : 1f - into;
        var beat = clock * Screens.Tempo;

        Show = level;

        _lounge.Mirror.RotationDegrees = new Vector3(0f, clock * Glitter.Spin, 0f);
        _lounge.Show.Update(clock * Glitter.Spin, level, beat, eye - Deck.Screens);

        foreach (var lamp in _lounge.Warm)
            lamp.Dim(1f - level);

        for (var i = 0; i < _lounge.Beams.Length; i++)
        {
            _lounge.Beams[i].Position = ScreenRoom.Orbit(clock * -121f + i * 90f);
            _lounge.Beams[i].Intensity = level * 1.5f * (0.22f + 0.78f * Thump(beat, i));
        }

        if (walking)
        {
            _restless = clock;
            _sitting = false;
        }
        else if (_party && level > 0.98f && clock - _restless > Settles)
        {
            _sitting = true;
        }

        if (!_party)
            _sitting = false;

        return _sitting ? ScreenRoom.Seat : null;
    }

    /// <summary>How near the middle of the seat row somebody has to come for the lights to go down, how
    /// far back out before they come up again, and how long the fade takes.
    ///
    /// The two radii are different on purpose and it is the same hysteresis the lounge's ball uses: one
    /// number means somebody standing exactly on the line switches the room on and off at whatever rate
    /// they happen to be breathing.</summary>
    private const float Books = 2.4f;

    private const float Wanders = 3.1f;
    private const float Dims = 2.2f;

    /// <summary>
    /// The planetarium in the free walk: come and sit down, and it runs.
    ///
    /// <b>It is <see cref="Party"/> again and it is deliberately the same shape.</b> Walk far enough into
    /// the seats and the coves fade out, the haze comes in, the show starts from nought, and standing
    /// still for a few seconds drops the camera into the chair. Walk back out and it all reverses. That is
    /// the one interaction this building has, it was written once for a mirror ball, and a second room
    /// that wants it should not get a second implementation of it.
    ///
    /// <b>The show's clock starts at the crossing and not at the film's origin.</b> <c>_curtain</c> is the
    /// moment the room began to go dark, so <c>clock - _curtain</c> is exactly what the chapter hands
    /// <see cref="Sky.Update"/> as <c>seconds - Curtain</c> — same expression, same staging, and a visitor
    /// who walks in gets the show from its beginning rather than from wherever it had got to.
    ///
    /// <b>The haze is applied on a change and not every frame</b>, which the chapter does not have to care
    /// about and this does. <see cref="Hall.Air"/> rebuilds the scene's fog state and invalidates it, and
    /// a free walk is a loop with no chapter boundaries in it to hide that behind — so it is asserted
    /// whenever the fade has moved two per cent, which is fifty calls across a two-second fade and none at
    /// all the rest of the time.
    /// </summary>
    private Vector3? Showtime(Vector3 eye, float clock, bool walking, bool inside)
    {
        _still ??= clock;

        // <b>Out of the room is out of the show, with no fade.</b> Everywhere else here a fade is the
        // right answer and here it is a bug: this method asserts the room's bounce and its haze, and a
        // two-second fade that keeps running after somebody has crossed a threshold is two seconds of one
        // room's atmosphere applied to the next one. The lounge's ball has no such problem because it only
        // ever touches things that are in the lounge. So crossing out puts the room straight back to
        // standing — which nobody sees, because they are looking the other way.
        if (!inside)
        {
            if (_running)
            {
                _dome.Show.Off();
                _dome.Running(0f);

                for (var i = 0; i < 4; i++)
                    _dome.Cove[i].Dim(1f);

                _running = false;
            }

            _dark = false;
            _watching = false;
            _haze = -1f;

            return null;
        }

        var row = new Vector2(Planetarium.Auditorium.X, Planetarium.Auditorium.Z);
        var near = Vector2.Distance(row, new Vector2(eye.X, eye.Z)) < (_dark ? Wanders : Books);

        if (near != _dark)
        {
            _dark = near;
            _curtain = clock;

            // The hand-over. Three coves and the show's own lamp: the coves are about to be switched off
            // and keep their slots anyway, because a lamp handed a slot while it is already bright arrives
            // as a pop and these three come back up at the end of it.
            _hall.Use(near
                ?
                [
                    _dome.Cove[0].Light, _dome.Cove[1].Light, _dome.Cove[2].Light, _dome.Show.Star
                ]
                : _quarters[_dais].Slots);
        }

        var into = Ramp(clock - _curtain, Dims);
        var level = _dark ? into : 1f - into;

        if (level <= 0f && !_running)
            return null;

        for (var i = 0; i < 4; i++)
            _dome.Cove[i].Dim(1f - level);

        _dome.Show.Update(clock - _curtain, level);
        _dome.Running(level, MathF.Max(clock - _curtain, 0f));
        _running = level > 0f;

        // The bounce, over the top of what Stand already asserted for this room, and it goes to a
        // thousandth — which is the chapter's own number and is darker than anywhere else in the building.
        _hall.Ambient(0.050f - 0.0489f * level, 0.026f - 0.0254f * level);

        if (MathF.Abs(level - _haze) > 0.02f)
        {
            _haze = level;

            _hall.Air(
                3f + 45f * (1f - level),
                34f + 60f * (1f - level),
                new Vector3(0.012f, 0.015f, 0.034f),
                1f + 0.35f * level);
        }

        if (walking)
        {
            _still = clock;
            _watching = false;
        }
        else if (_dark && level > 0.98f && clock - _still > Settles)
        {
            _watching = true;
        }

        if (!_dark)
            _watching = false;

        return _watching ? Planetarium.Seat : null;
    }

    /// <summary>Nought before, one after, and a smooth ride between. The free walk's own, because a chapter's
    /// is a chapter's.</summary>
    private static float Ramp(float since, float over)
    {
        var t = Math.Clamp(since / over, 0f, 1f);

        return t * t * (3f - 2f * t);
    }

    /// <summary>One orbiting light's answer to the beat, on a different division of the bar for each of the
    /// four. The same four divisions chapter 4 uses, which is why the room sounds the same in both.</summary>
    private static float Thump(float beat, int index)
    {
        var phase = index switch
        {
            0 => beat,
            1 => beat + 0.5f,
            2 => beat * 0.5f,
            _ => beat * 0.25f
        };

        return MathF.Pow(1f - (phase - MathF.Floor(phase)), 2.4f);
    }

    /// <summary>
    /// Which room the visitor is standing in, by name, and empty until the building has been handed over.
    ///
    /// It is here for the score, which needs to know two things about the free walk that nothing else can
    /// tell it: which room's lamps are the four that are lit, and whether the lounge has gone over to the
    /// ball. Both are lighting, and lighting is the one thing in this file that a listener can hear.
    /// </summary>
    public string Room => _standing < 0 ? "" : _quarters[_standing].Name;

    /// <summary>
    /// Whether anything past the glass is being drawn — which is to say, whether the camera needs to be
    /// able to see nineteen hundred metres. True in the last two rooms and nowhere else.
    ///
    /// <c>StoryScene</c> reads it to keep the rest of the building inside a depth range a sixteen-bit
    /// buffer can resolve a wall thickness in. The film does not need this because every chapter but two
    /// declares its own far plane; the free walk has no chapter of its own and inherits the last one's,
    /// which is the one sized for a planet.
    /// </summary>
    public bool Outward => _outside.IsVisible;

    /// <summary>Whether the lounge has gone over to the light show. A moment rather than a level — it flips
    /// on the frame the four slots change hands, which is the frame the relay closes.</summary>
    public bool Ball => _party;

    /// <summary>And the same change as a fade, nought for the room's four warm lamps and one for the ball.
    /// A hum belongs to the fittings that make it, so it has to go out on the same ramp they do.</summary>
    public float Show { get; private set; }

    /// <summary>Every room's footprint on the deck, in the order the route runs. Read by the walk audit,
    /// which floods the floor from where the film hands over and asks which of these it got into.</summary>
    public IEnumerable<(string Name, Vector2 Min, Vector2 Max)> Plans =>
        _quarters.Select(q => (q.Name, q.Min, q.Max));

    /// <summary>The room and the two either side of it: the most that can be seen from anywhere on a route
    /// that does not branch.</summary>
    private string[] Neighbourhood(int here) =>
    [
        .. _quarters
           .Skip(Math.Max(0, here - 1))
           .Take(here == 0 ? 2 : 3)
           .Select(q => q.Name)
    ];

    /// <summary>How far the eye is from the middle of room <paramref name="index"/>, as a tie-break only.</summary>
    private float Middle(int index, Vector3 eye)
    {
        var mid = (_quarters[index].Min + _quarters[index].Max) / 2f;
        return Vector2.Distance(mid, new Vector2(eye.X, eye.Z));
    }

    /// <summary>
    /// A room's footprint on the deck, from its meshes.
    ///
    /// <c>outside</c> is skipped, and it is the only reason this is not two lines. The illuminator's window
    /// looks onto a station four kilometres off and a sphere six hundred metres across, both of which are
    /// children of that room; a plan taken with them in it would say the last room covers the whole deck
    /// and every other room with it.
    /// </summary>
    private static (Vector2 Min, Vector2 Max) Plan(Node room)
    {
        var min = new Vector2(float.MaxValue);
        var max = new Vector2(float.MinValue);

        Walk(room);

        return min.X > max.X
            ? (new Vector2(room.Position.X, room.Position.Z), new Vector2(room.Position.X, room.Position.Z))
            : (min, max);

        void Walk(Node node)
        {
            if (node.Name == "outside")
                return;

            if (node is MeshNode { Mesh: not null } mesh)
            {
                var box = mesh.LocalBounds.Transform(mesh.WorldTransform);

                if (!box.IsEmpty)
                {
                    min = Vector2.Min(min, new Vector2(box.Min.X, box.Min.Z));
                    max = Vector2.Max(max, new Vector2(box.Max.X, box.Max.Z));
                }
            }

            foreach (var child in node.Children)
                Walk(child);
        }
    }
}
