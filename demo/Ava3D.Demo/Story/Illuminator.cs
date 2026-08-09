using System.Numerics;
using Ava3D.Demo.Scenes.Contact;
using Ava3D.Demo.Textures;

namespace Ava3D.Demo.Story;

/// <summary>
/// The illuminator gallery: fifteen metres of hull with thirteen of it glazed, and a planet outside.
///
/// It is the last room in the building and it is the one that says what the building is. Nine minutes of
/// exhibition have been laid out so that the largest thing in shot is a wall six metres away; this room
/// opens the whole outboard side and what is behind it is a planet six hundred metres off, a station
/// station-keeping against its lit limb, and one of the escorts from <c>Contact</c> coming onto station
/// off the beam. Nobody says <i>he is on a cargo ship</i>. Three objects say it, and then the film cuts
/// outside.
///
/// <b>It is a panorama and not a porthole, and the two are different rooms.</b> An earlier cut had four
/// round ports in a metre of plate, which is the more nautical object and turned out to be the wrong one:
/// a metre-deep tube a metre and a half across is a <i>collimator</i>, and what it shows is decided
/// entirely by where you are standing — so half the walk was spent looking at the inside of a pipe with
/// the sky switched off behind a pier. The window is now thirteen metres by one metre nine, in four bays
/// with a mullion between each, and everything outside is in view from everywhere in the room. The reveal
/// stops being a matter of geometry and becomes what it should always have been: what he happens to be
/// looking at, and the one object out there that is moving.
///
/// <b>The hull is still a metre thick</b>, and that is the whole of what is kept from the porthole. The
/// difference between "a room with a view" and "a hull" is entirely in how much steel there is between
/// you and the outside, and you can see all of it here — at the ends of the glazing, in the reveals down
/// both sides of every mullion, and under the sill you can put your hands on.
///
/// <b>And it is lit by one light.</b> Every room in this film has spent four slots; this one spends a
/// single <see cref="DirectionalLight"/> — the star, ninety-seven degrees round, which is what puts the
/// terminator down the middle of the planet's disc and keeps the star itself behind the plate where it
/// can never be in frame. Everything else in here that is bright is emission: the trim round the glass,
/// the deck strips, the station's lamps, the escort's jets, and the engine room still running through the
/// door behind him. Four televisions made that argument in chapter 4. A star makes it here.
/// </summary>
internal sealed class Illuminator
{
    /// <summary>The ends of the gallery, in the room's own coordinates. Local X runs with the deck's.</summary>
    public const float West = -8.6f;

    public const float East = 7.0f;
    public const float Height = 3.4f;

    /// <summary>
    /// The inboard face of the hull — how far it is from the room's origin to the wall the window is in,
    /// and therefore how deep the room is.
    ///
    /// Four metres four, and it was three metres two until there was furniture in it. A gallery has to hold
    /// three lanes at once: seats facing the glass, a way past behind them, and the plot against the
    /// inboard wall. At three two the walk went straight through a chair — which is the kind of thing that
    /// is invisible in a plan and unmissable in the first frame.
    /// </summary>
    public const float Bore = 4.4f;

    /// <summary>
    /// How thick the hull is, and it is the only number in this room that has to be felt rather than seen.
    ///
    /// A metre. Every other wall in the building is a quarter of that; the engine room's bulkhead was six
    /// hundred millimetres and was the first one thick enough to be a <i>reveal</i>. This one is a metre,
    /// and because the opening in it is two metres tall rather than a metre and a half round, none of that
    /// depth is spent stopping him seeing out: the thickness shows at the ends of the glass and down the
    /// side of every mullion, which is where a hull's section is legible anyway.
    /// </summary>
    public const float Hull = 1f;

    /// <summary>The end walls, and the ceiling's own slab.</summary>
    private const float Thickness = 0.3f;

    /// <summary>
    /// How thick the wall between here and the engine room is.
    ///
    /// Six hundred millimetres, and it is a fix as much as a wall. The engine room already built a north
    /// wall a quarter of a metre thick, and two walls meeting face to face are two coplanar surfaces with
    /// nothing to choose between them. This one is thick enough to <i>contain</i> that one, with two
    /// hundred millimetres clear on the south face and a hundred and fifty on the north — so every face of
    /// the engine room's wall is inside solid geometry, which is the same fix the corridor uses at both
    /// ends of itself.
    /// </summary>
    private const float Back = 0.6f;

    /// <summary>
    /// How far south the deck reaches: to where the engine room's stops, edge to edge.
    ///
    /// It is the one piece of this room that cannot be buried in the wall it meets, and the corridor learnt
    /// why at both ends of itself. Two walls that overlap are fine — interpenetrating boxes cannot z-fight
    /// — but two floors that overlap are two coplanar <i>planes</i> at the same height, which is the one
    /// case the depth test has nothing to go on. So this one stops exactly where that one stops and the
    /// seam falls under the door.
    ///
    /// Getting it wrong does not look like a seam. It looks like a hole: the first cut of this room had the
    /// deck start at the room's origin, four hundred millimetres north of where the engine room's ends, and
    /// standing in the doorway put a quarter of the frame into empty space.
    /// </summary>
    private const float Apron = -0.4f;

    /// <summary>
    /// How thick the hull is at the bow port, and how far inboard that port reaches.
    ///
    /// The forward end of this gallery is on the shoulder of the ship, so the wall at that end is a hull
    /// wall and not a partition — eight hundred millimetres, which is most of what the panorama's own
    /// reveal is and is enough to be a reveal rather than a hole.
    ///
    /// <b>It exists because a window down one side of a ship cannot look where the ship is going.</b> The
    /// panorama is in the outboard wall, so a look forward is a look along it, and the sightline leaves
    /// the room through the west end before it ever reaches the glass — which is exactly what the first
    /// cut of the last chapter came back as: a lane of traffic in the left of the frame and two metres of
    /// blank partition in the right of it. The forward half of that frame is now a window, the two
    /// openings meet at the corner with one post between them, and standing at this end of the room means
    /// standing in a bay with glass on two sides of it.
    /// </summary>
    private const float Prow = 0.4f;

    private const float PortIn = 1.1f;

    /// <summary>How far the bow pane is bedded into its reveal. Less than the panorama's, because the
    /// reveal it is bedded into is less than half as deep.</summary>
    private const float PortBed = 0.16f;

    /// <summary>
    /// The glazed run.
    ///
    /// It stops well short of the aft end so the section of the hull is visible there, and it runs almost
    /// to the forward end so that it does not. That asymmetry is the corner: the panorama and the bow port
    /// meet at the forward end, and what is left between them is a hundred and fifty millimetres of hull
    /// seen edge-on with the bow reveal behind it. Any more than that and the two windows read as two
    /// windows with a wall between; any less and there is nothing carrying the corner down, which a hull
    /// under vacuum is not entitled to.
    /// </summary>
    private const float GlassWest = -8.45f;

    private const float GlassEast = 5.6f;

    /// <summary>
    /// Sill and head: half a metre off the deck to three metres five, which is two and a half metres of
    /// opening in a room three metres four tall.
    ///
    /// It was a metre nine with three posts in it, and both of those went for the same reason. A window
    /// that stops at chest height and is chopped into bays is a row of windows — the eye counts them, and
    /// counting is the opposite of what this room is for. Taking the head up to within a foot of the
    /// ceiling and the sill down below the knee leaves an opening the frame cannot hold all of from
    /// anywhere in the room, which is the whole difference between looking out of a window and being on
    /// the outside of a ship.
    /// </summary>
    private const float Sill = 0.72f;

    private const float Head = 3.2f;

    /// <summary>How far into the reveal the glass sits. Well in, so there is a lip of steel in front of it
    /// as well as behind — which is what a pane bedded into a hull looks like and is also what stops the
    /// pane and the plate's cut face landing in the same plane.</summary>
    private const float Bedded = 0.42f;

    /// <summary>How far the sill shelf stands into the room. Narrow, because the opening now starts below
    /// the knee and a wide one would be a bright band across the bottom of every shot.</summary>
    private const float Ledge = 0.2f;

    /// <summary>Where the visitor comes in, which is the engine room's north door and therefore not a
    /// number this room gets to choose.</summary>
    public const float Doorway = EngineRoom.Doorway;

    /// <summary>
    /// The point everything outside is placed from: the middle of the westmost bay, at the glass, which is
    /// where the film ends up standing.
    ///
    /// It is an anchor rather than the room's origin because every bearing in this chapter is a bearing
    /// from the window, and the place the shot finishes is the one worth being exact about. Six hundred
    /// metres away it makes no measurable difference which end of the gallery you measure from, which is
    /// the point of the distances being what they are.
    /// </summary>
    private static readonly Vector3 Anchor = new(-6.1f, Deck.Eye, Bore + Hull);

    /// <summary>How far off the planet is, and how big. A hundred and sixty-five metres of radius at six
    /// hundred and twenty gives a disc just under thirty-one degrees across, which is two thirds of the
    /// frame's width and leaves its own limb in shot on both sides.</summary>
    private const float PlanetRange = 620f;

    private const float PlanetRadius = 165f;

    /// <summary>Where the starfield is. Beyond the planet and inside the far plane the chapter asks
    /// for.</summary>
    private const float SkyRange = 1400f;

    /// <summary>What Relay Nine is scaled by on the way in, on top of the six hundred and twenty it is built
    /// at. It comes out about nineteen metres across, which at two hundred and eighty-five is four
    /// degrees.</summary>
    private const float StationSize = 0.0105f;

    /// <summary>
    /// And what it is scaled by on the way out. Fourteen times the size, at ten times the distance.
    ///
    /// <b>Chapter 7 draws a station that is not the size of a station, and it is not a cheat.</b> Ninety-
    /// three metres to the model unit puts Relay Nine at three hundred and twelve metres across, which is
    /// what it has to be for the film's own ship to be berthed inside its bay. At that size, four degrees
    /// — the angle chapter 7 puts it at — is four and a half kilometres away, and this room's far plane is
    /// at nineteen hundred metres because a starfield past a planet is what that budget was spent on. So
    /// the run-in draws the same station at a fourteenth of its size and a tenth of its distance, and the
    /// two errors cancel exactly in the only measurement a window can make. A viewer sees an angle; there
    /// is no shot in the film from which the difference is visible.
    ///
    /// What cannot be done that way is this chapter. You can shrink a station to fit a depth budget and
    /// you cannot fly a ship out of the bay of one that has been shrunk — the doorway comes down with it.
    /// So the morning asserts its own scale, the way it already asserts the station's position and
    /// everything else outside the glass, and pays for the honest size by never letting it get further
    /// away than about fourteen hundred metres. See <see cref="Leave"/>.
    /// </summary>
    private const float BerthSize = 0.15f;

    /// <summary>How far the mouth of the docking bay is from the middle of the station, at
    /// <see cref="BerthSize"/>. Read off the model rather than typed: <see cref="Fleet.BayMouth"/> is where
    /// the bay is authored, and this is that in the metres this chapter works in.</summary>
    private static readonly float Mouth = Fleet.BayMouth.Z * BerthSize;

    /// <summary>
    /// How far inside the mouth the window sits while the ship is still tied on: twenty metres, in a bay
    /// forty-one deep. The gallery is fifteen and a half metres long, so all of it is inside and there are
    /// eight metres of tube behind the aft end of the glass — which is what makes the first shot of the
    /// chapter a room with a wall of station outside every pane of it rather than a room with a view.
    /// </summary>
    private const float Berthed = 20f;

    /// <summary>
    /// How far astern the station is allowed to get: eleven hundred and fifty metres, which is a sixteenth
    /// of a second further out than the departure carries it by the last frame of the film.
    ///
    /// A ceiling rather than a distance the film aims for, and the only thing it exists for is the walk
    /// that starts when the film stops. See <see cref="Leave"/>.
    /// </summary>
    private const float Astern = 1150f;

    /// <summary>
    /// Where the bay's axis is, relative to the window: ten metres <i>outboard</i> of the glass and eight
    /// down.
    ///
    /// Outboard, which reads as wrong and is the whole shot. A ship centred in its berth has the bay's
    /// mouth dead ahead of it, and dead ahead of a ship whose windows are down its side is the one bearing
    /// those windows cannot see — the sightline leaves through the end wall long before it reaches the
    /// glass. Berthed against the inboard wall instead, with thirty-four metres of bay outboard of the
    /// window and the doorway forward and off the bow, the mouth is at sixty-three degrees when he is
    /// tied on and sixty when he is watching it go. Both of those are through the window.
    /// </summary>
    private const float AxisOut = 10f;

    /// <summary>And two and a half metres below the eye, which is very nearly nothing in a bay
    /// sixty-nine metres tall. The ship is parked in the middle of the space rather than on its floor,
    /// so the doorway is a shape a man looks <i>at</i> rather than down at — a mouth eight metres below
    /// the window puts the whole of the departure in the bottom third of the frame.</summary>
    private const float AxisDown = 2.5f;

    /// <summary>
    /// How far the ship swings off the line it left on, in degrees, and how the swing is shaped.
    ///
    /// A ship going straight out of a berth leaves the thing it left dead astern, which is the second
    /// bearing a window down one side cannot see. So it comes round thirty-four degrees over the twenty
    /// seconds after it is clear — the amount of turn that puts Relay Nine on the quarter and in the aft
    /// end of the glass, and the reason the longest hold in the film has something in it.
    /// </summary>
    private const float Sweep = 34f;

    /// <summary>The second of the last chapter the ship lets go on, and the shape of what happens after
    /// it. See <see cref="Leave"/> — <c>0.13 τ²</c> is manoeuvring thrust and <c>0.0018 τ³</c> is the drive
    /// coming up under it, which together are what leaving a mooring looks like from inside.</summary>
    private const float Release = 16f;

    /// <summary>
    /// When the clamps let go, six seconds before the ship does.
    ///
    /// Public because it is a moment two files have to agree on: this one folds the arms back into the
    /// wall on it and the score drops a hundred tonnes of latch on it. Two numbers would be two numbers
    /// that could be retuned apart, and a clamp whose sound is half a second off the picture is not a
    /// clamp, it is a fault.
    /// </summary>
    public const float Unclamps = Release - 6f;

    /// <summary>How many guide lamps run down each of the two rows on the bay's outboard wall. Enough to
    /// read as a line and few enough that the line can be counted, which is what makes it a scale.</summary>
    private const int Guides = 8;

    /// <summary>How many frames run across the wall the window faces. Seven, five metres apart, and see
    /// <see cref="Berthing"/> for why they matter more than everything else in the berth put together.</summary>
    private const int Ribs = 7;

    /// <summary>Half the bay's clear opening, in metres — the tube is authored at 0.37 model units either
    /// side of its axis. Everything built inside the berth is measured off this.</summary>
    private const float Clear = 0.37f * Fleet.StationScale * BerthSize;

    /// <summary>How deep the bay is, from the mouth back to its blind end.</summary>
    private const float Deep = 0.44f * Fleet.StationScale * BerthSize;

    /// <summary>How long the escort is. Fourteen metres, and it is the only object out there whose size the
    /// visitor can check against something: it is one bay of the window he is standing at.</summary>
    private const float EscortLength = 14f;

    /// <summary>A path the escort never flies. <see cref="Fleet.BuildShip"/> wants one, and this ship is
    /// placed by hand every frame because it is coming onto station rather than going anywhere.</summary>
    private static readonly Vector3[] Idle =
        [Vector3.Zero, Vector3.UnitZ, Vector3.UnitZ * 2f, Vector3.UnitZ * 3f];

    /// <summary>The colour of every indicator on the console band. See <see cref="Screen"/>.</summary>
    private static readonly Vector3 Indicator = new(1f, 0.66f, 0.22f);

    /// <summary>The colour of everything that moves on a screen: needles, wipes, markers, blocks.</summary>
    private static readonly Vector3 Refresh = new(0.45f, 0.80f, 1f);

    /// <summary>
    /// The six ways a screen in this room can be alive, one for each kind of instrument on the sheet.
    ///
    /// <b>Twenty-one panels doing the same thing at different phases is still twenty-one panels doing the
    /// same thing.</b> A phase offset stops them marching in step and does nothing at all about the second
    /// complaint, which is that a room where every instrument moves the same way has one instrument in it
    /// repeated twenty-one times. So the moving part is chosen by what it is standing on: a dial gets the
    /// one thing a dial does, a chart gets the one thing a chart does, and a status board — which is a
    /// thing with no moving parts — gets a block that comes on and goes off.
    /// </summary>
    private enum Motion
    {
        /// <summary>A needle, swinging across a dial's scale and back.</summary>
        Needle,

        /// <summary>A refresh line crossing a trace, pinched to nothing at both ends of its travel.</summary>
        Wipe,

        /// <summary>A rule riding up and down a bar chart, which is what a threshold on one does.</summary>
        Marker,

        /// <summary>A cap climbing a level gauge in whole segments, because a gauge reads in segments.</summary>
        Climb,

        /// <summary>A highlight jumping from one row of a list to the next.</summary>
        Step,

        /// <summary>A block on a status board, on and off. The one that does not move at all.</summary>
        Flash
    }

    /// <summary>
    /// One moving part, and the numbers that drive it.
    ///
    /// <see cref="Home"/> is the middle of the travel and <see cref="Travel"/> is the half-throw, so a
    /// position is <c>Home + Travel × s</c> for an <c>s</c> between −1 and 1 that comes out of the clock —
    /// which means nothing here is ever asked where it got to. <see cref="From"/> and <see cref="Span"/>
    /// are the same thing in degrees, for the needle.
    /// </summary>
    private sealed record Live
    {
        public required Motion Kind { get; init; }
        public required Node Part { get; init; }
        public Vector3 Home { get; init; }
        public Vector3 Travel { get; init; }
        public float Rate { get; init; }
        public float Phase { get; init; }
        public float Duty { get; init; }
        public float From { get; init; }
        public float Span { get; init; }
    }

    /// <summary>One screen's indicator: a lamp on a duty cycle, which is not the same as a blink. A lamp on
    /// as long as it is off reads as a metronome; one lit for a fifth of its cycle reads as a machine
    /// reporting something.</summary>
    private sealed record Lamp(Material Material, float Rate, float Phase, float Duty);

    private readonly Material[] _lamps;
    private readonly Material _collar;
    private readonly Material _bay;
    private readonly Material _trim;
    private readonly Material _deck;
    private readonly Material _spill;
    private readonly Material _cove;
    private readonly Material _holo;
    private readonly Material _beam;
    private readonly Material _screen;
    private readonly Material _wipe;
    private readonly Material _keys;
    private readonly Material _sheen;
    private readonly Material _lamp;
    private readonly Material _gleam;
    private readonly Ship _escort;
    private readonly Node _planet;
    private readonly Node _relay;
    private readonly GateField _gate;
    private readonly Node _berth = new() { Name = "berth", IsVisible = false };
    private readonly Node _gantry = new() { Name = "berth.gantry" };
    private readonly Material[] _guides = new Material[Guides * 2];
    private readonly Node _globe = new() { Name = "globe" };
    private readonly Node _orbits = new() { Name = "orbits" };
    private readonly Node _drift = new() { Name = "drift" };
    private readonly LineNode[] _chart = new LineNode[3];
    private readonly PointsNode[] _dust = new PointsNode[1];
    private readonly List<Live> _live = [];
    private readonly List<Lamp> _pips = [];

    public Illuminator(Hall hall)
    {
        var root = hall.Add(Deck.IlluminatorRoom, Deck.Illuminator);

        // White composite, and it is the one room in the building that is not made of the same plate as
        // the rest of it.
        //
        // Every space so far has been a hold: riveted steel getting darker as the ship gets deeper, which
        // is what a cargo deck is. This is the one place on board that was fitted out rather than built —
        // it is where the crew come to look at where they are — and it is finished the way the inside of a
        // cabin is finished: moulded panel, warm seams, a floor with a shine on it. The change of material
        // is the change of purpose, and it is the last thing the film says about surfaces after nine
        // minutes of saying things about them.
        //
        // The panel map is read at Finish.Snug rather than the building's pitch, so the seams are a
        // handspan apart instead of a stride: this is furniture-grade panelling seen from a metre, not
        // hull plate seen from six.
        // Dark blue-grey, glossy, and seamless. The room went white for a round and came back: a white
        // fit-out is somebody's lounge, and this is the observation deck of a working ship, which is a
        // darker and colder place with warm light let into it. The material is the same moulded laminate
        // either way — it is the colour and the gloss that say who the room is for.
        var plate = Finish.Composite();
        plate.BaseColor = new Vector4(0.115f, 0.125f, 0.145f, 1f);
        plate.Metallic = 0.25f;
        plate.Name = "gallery.shell";

        // The deck is the darkest surface in the building and it has to be, because it is the one plane in
        // here the star can see all of. A directional light has infinite reach and this renderer casts no
        // shadows, so the sun that is supposed to be laying a band of light along the floor under the
        // window lays it over all fifty square metres — and at the albedo the engine room's deck is
        // painted, what came back was a sheet of white tiles with a planet somewhere behind it.
        //
        // Down to a fortieth, off the metal, and rough. It reads as a dark deck with the strips and the
        // window doing the lighting, which is what it is meant to be a picture of anyway; the low number is
        // standing in for a shadow term that does not exist.
        // Grained rather than dressed, and that is a bug fix rather than a saving. The higher grades carry
        // a metallic-roughness map, and a map multiplies the factors rather than replacing them — so a
        // roughness of 0.85 written here came out at whatever the tile map says times 0.85, which is closer
        // to 0.4. What that looks like is not a shinier floor: it is a floor that catches the star in a
        // grazing Fresnel sheet the moment the camera drops near it, and half of every low shot came back
        // as white tiles. At this light level the relief was never going to be visible anyway.
        var floor = Finish.Floor(Grade.Grained);
        floor.BaseColor = new Vector4(0.020f, 0.021f, 0.024f, 1f);
        floor.Metallic = 0f;
        floor.Roughness = 0.9f;
        floor.Name = "gallery.deck";

        // Warm rather than cold, and it is the only lit trim in the film that is not blue.
        //
        // Every strip, lens and indicator in this ship has been ice blue or amber-as-a-warning. A room
        // people sit in is lit the colour a room people sit in is lit, and against a planet that is
        // entirely blue and white it is also the only accent that does not disappear into what is behind
        // the glass.
        _deck = Glow(1f, 0.62f, 0.20f);
        _trim = Glow(1f, 0.66f, 0.26f);

        _spill = Glow(0.52f, 0.66f, 0.88f);
        _spill.BaseColorTexture = Finish.Pool();
        _cove = Glow(1f, 0.70f, 0.32f);
        _holo = Glow(0.36f, 0.86f, 1f);
        _beam = Glow(0.30f, 0.72f, 0.95f);
        // The screens carry an image rather than a colour, which is the one change that moves this room
        // from "a bridge" to "a bridge somebody works at". One texture across all twenty-one of them, and
        // nine layouts on it — see Finish.Readouts for why those two facts are not in tension, and see
        // Screen for the two moving parts each one gets on top.
        _screen = Glow(0.34f, 0.62f, 0.92f);
        _screen.BaseColorTexture = Finish.Readouts();

        // The wipe that crosses each screen. Its own material rather than the screens' because it is the
        // one part of an instrument that is brighter than the instrument: a refresh line reads as a refresh
        // line by being hotter than what it is passing over, and a bar at the glass's own value is a bar.
        _wipe = Glow(Refresh.X, Refresh.Y, Refresh.Z);
        Paint(_wipe, Refresh, 0f);

        // The keys, and they are the one surface in this room that is lit <i>and</i> emissive at the same
        // time. Everything else in here has picked a side: the plate and the fittings are lit and dark,
        // the strips and the screens are unlit and additive. A console panel is neither — it is a moulded
        // grey thing standing in the room's own light with a handful of lamps under it — and the only way
        // to have both is an ordinary material carrying the same map twice. See Finish.Keys.
        _keys = new Material
        {
            BaseColor = new Vector4(1f, 1f, 1f, 1f),
            BaseColorTexture = Finish.Keys(),
            EmissiveTexture = Finish.Keys(),
            Metallic = 0f,
            Roughness = 0.55f,
            Name = "gallery.keys"
        };

        // Every one of these is light lying on a surface rather than a surface, so every one of them gets
        // the falloff — see Finish.Pool for what a hard-edged additive quad looks like on a floor, which is
        // exactly like somebody painted a white rectangle on the floor.
        _sheen = Glow(1f, 0.62f, 0.22f);
        _sheen.BaseColorTexture = Finish.Pool();

        // The ceiling lamps, and they are the one lit thing in the room that is not amber.
        //
        // A working ship lights its trim warm and its work surfaces cold, for the same reason a workshop
        // does: you want the amber to read as a marking and the white to read as light you can see by. The
        // references all do it — warm strips at knee and hand height, cold coffers overhead — and it is
        // also the only way to have this much lit area in frame without the whole room going gold.
        _lamp = Glow(0.82f, 0.90f, 1f);

        // What the floor does with all of it. See Gleam.
        _gleam = Glow(0.62f, 0.76f, 1f);
        _gleam.BaseColorTexture = Finish.Pool();

        var steel = Finish.Brushed();
        steel.BaseColor = new Vector4(0.30f, 0.315f, 0.35f, 1f);
        steel.Name = "gallery.fitting";

        Shell(root, plate, floor);
        Outboard(root, plate, steel);
        Coffers(root, plate);
        Inboard(root, plate, steel);
        Consoles(root, plate, steel);
        Sheen(root);
        Gleam(root);
        Spill(root);
        Strips(root);
        Plot(root);
        Motes(root);

        // Everything past the glass, under one node, so the whole outside is one thing to hide when he is
        // anywhere else in the building.
        var outside = new Node { Name = "outside" };
        root.Children.Add(outside);

        Sky(outside);
        _planet = Planet(outside);

        var plating = Space.Plating();
        var glow = Space.Glow();

        // Relay Nine is built at six hundred and twenty world units to the model unit, because in Contact
        // it is a kilometre and a half of station and the camera flies into its docking bay. The scale goes
        // on the node the builder returns rather than into the loader, so this is the same station object
        // it has always been, seen from two very different distances — see StationSize and BerthSize, and
        // see Approach and Leave, which are the two chapters that each assert one of them.
        //
        // A node's position is not scaled by its own scale — S then R then T — so the station stays where
        // it is put and only gets smaller, which is the property that makes mounting anything at another
        // size possible at all.
        (_relay, _lamps, _bay, _collar) = Fleet.BuildStation(outside, Station, plating);

        // The inside of the bay, which nothing has ever looked at before.
        //
        // In Contact the docking bay is a shape a freighter is cleared into from three kilometres away and
        // the camera never goes near it, so the tube is a plain material with an emissive tint on it. The
        // last chapter of the film stands a man twenty metres inside it, and at that range a flat facet is
        // the one thing the eye can tell is a model. It takes the same plating as the hull it is cut into.
        //
        // This is the gallery's own copy of the station and its own copy of the material — the film builds
        // one station and Contact builds another — so dressing it here cannot reach the battle.
        _bay.BaseColorTexture = plating.Albedo;
        _bay.BumpTexture = plating.Bump;
        _bay.BumpScale = 3.5f;
        _bay.Roughness = 0.82f;
        _bay.BaseColor = new Vector4(0.115f, 0.122f, 0.14f, 1f);

        // The hall at the back of the bay, which arrives switched off.
        //
        // Two of Relay Nine's materials are lit panels in the room behind the gate, and the builder makes
        // them Unlit so that a lens at a kilometre and a half puts its colour on the screen instead of
        // being shaded and tone-mapped into pink — which is right, and leaves them at the two hundredths
        // grey the model was authored with until something says otherwise. In Contact something does. In
        // here nothing did, so the back of the berth was a dark slab thirty metres wide hanging in the one
        // part of the frame the eye goes to, and it took a capture with the whole berth switched off to
        // prove it was the station's and not ours. They are found by walking the model rather than handed
        // over, because the builder's return is Contact's and this is the only caller that wants them.
        foreach (var lit in _relay.Descendants.OfType<MeshNode>())
            lit.Material.BaseColor = lit.Material.Name switch
            {
                "relay.hall.lamp" => new Vector4(0.30f, 0.35f, 0.42f, 1f),
                "relay.hall" => new Vector4(0.26f, 0.28f, 0.32f, 1f),
                _ => lit.Material.BaseColor
            };

        // And it is drawn from both sides, which it never had to be before.
        //
        // The tube is authored as an ordinary box with its faces pointing out of the station, because for
        // nine hundred frames of Contact the only way anybody looks at it is <i>into</i> the mouth from a
        // kilometre away — and from out there the far wall of the bay is a face pointing at you. Stand
        // inside it and every one of those faces is pointing away, so a back-face cull removes the entire
        // room and leaves a window full of stars where a wall should be. It cost an afternoon to find,
        // because "the bay did not draw" and "the bay is behind the camera" look identical from a frame
        // grab. Contact keeps its cull; this copy does not have one.
        _bay.Cull = CullMode.None;

        // And the gate across the mouth: Contact's own energy door, red while the bay is shut and thin
        // green when it is cleared, mounted here as a child of the station rather than as a thing placed
        // beside it. That is the whole reason it can be trusted at this range — the mouth, the collar, the
        // six lamps and the field are one object, so nothing this file does to the station's position, its
        // attitude or its size can slide the door off the doorway.
        _gate = Fleet.BuildGate(_relay, Vector3.Zero, Space.PortalPlate(), Space.PortalSwirl(),
            Space.PortalRing());

        Outside = outside;

        // Everything in the berth that the model does not have because nothing was ever meant to be in
        // there: the strips down the corners, two rows of guide lamps along the wall the window faces, and
        // the gantry that is folded against it. Its own node, not a child of the station, because it is
        // authored in metres and the station is authored at six hundred and twenty to one.
        Berthing(outside, steel);

        Sun(outside);

        _escort = Fleet.BuildShip(
            outside, glow, plating, (Space.Plume(), Space.PlumeCap()),
            "harrier.glb", "harrier", EscortLength, new Vector3(0.42f, 1f, 0.62f),
            Idle, [0f, 0f, 0f, 0f], 0f);

        Sunlight = new DirectionalLight
        {
            Direction = -SunBearing,
            Color = new Vector3(1f, 0.97f, 0.92f),
            Intensity = 1.15f,
            Ambient = 0.015f
        };

        Running = new PointLight
        {
            Position = Deck.Illuminator + Point,
            Color = new Vector3(0.60f, 0.80f, 1f),
            Intensity = 30f,
            Range = 24f,
            Decay = 2f
        };

        // The bay's own working light, and it is the only light in this film that is somebody else's.
        //
        // Everything the gallery has been lit by so far is a star ninety-seven degrees round. Being inside
        // a station means a hard cold light coming across the window from a wall forty metres away, at an
        // angle nothing in this room was designed for, and that difference is most of what says the first
        // shot of the morning is not the last shot of the night. It reaches seventy metres because that is
        // the width of the space it is lighting; the star it replaces has no range at all.
        Dock = new PointLight
        {
            Color = new Vector3(0.74f, 0.84f, 1f),
            Intensity = 0f,
            Range = 120f,
            Decay = 2f
        };

        // And the gate, as light rather than as paint. The field is a curtain hung across a doorway
        // eighty-four metres wide and the ship goes through it, so for about four seconds it is the
        // brightest thing anywhere near the glass — and an unlit additive card puts colour on the screen
        // and nothing at all on the plate around it. Its range stops well short of anything but the room
        // it is washing, which is what lets the chapter drop it from the slots without a fade.
        Portal = new PointLight
        {
            Color = new Vector3(0.30f, 1f, 0.52f),
            Intensity = 0f,
            Range = 80f,
            Decay = 2f
        };

        Escort(0f);
        Base(0f);
        Trim(1f);
        Approach();
    }

    /// <summary>The star, and the only light this room has of its own.</summary>
    public DirectionalLight Sunlight { get; }

    /// <summary>
    /// The escort's own drive, as an actual light rather than paint.
    ///
    /// Every lamp and every engine bell on every hull in this film is geometry with an emissive material
    /// — which puts colour on the screen but puts nothing on the plating round it, so a lit engine is a
    /// bright patch on a dark ship instead of a ship with a lit tail. This is the one that is not: a
    /// point light sitting on the escort sixty-two metres out, with a range of twenty-four so that it
    /// reaches its own hull and nothing else. The gallery is well outside it, the lane is well outside
    /// it, and the planet is six hundred metres past the end of it.
    ///
    /// <b>It exists because there is a slot.</b> Nothing else in the film can have one: a scene holds
    /// four lights, the battle spends all four, and eight ships in a lane would want eight. The last
    /// chapter spends two and then one, so the ship the film ends looking at can be lit properly — and
    /// what that buys is the difference between a shape with a glow on it and a vehicle.
    /// </summary>
    public PointLight Running { get; }

    /// <summary>The berth's floods, coming in through the glass while the ship is still inside the bay.
    /// See the constructor for why the last room in the film gets a light that is not its own star.</summary>
    public PointLight Dock { get; }

    /// <summary>The gate's green, as a light on the room the ship takes through it.</summary>
    public PointLight Portal { get; }

    /// <summary>
    /// The middle of the bay's mouth, wherever it is now — the doorway, as something a camera can be
    /// pointed at.
    ///
    /// Live rather than a constant, like <see cref="Mooring"/> and for a harder version of the same
    /// reason. The station is a long way off and moves slowly across the frame; the doorway is twenty
    /// metres away and goes past at four metres a second, so a shot aimed at where it used to be would be
    /// aimed at nothing at all within half a second.
    /// </summary>
    public Vector3 Threshold { get; private set; }

    /// <summary>
    /// What is worth looking at outside, this second: the doorway while it is still a doorway, and the
    /// whole station once it is small enough to be one thing.
    ///
    /// One point rather than two, because the camera can only be pointed at one and the hand-over between
    /// them is not a cut — it is a man whose eye stays on the same object while the object stops being a
    /// wall with a hole in it and becomes a station. Chapter 9 tracks this and nothing else; see
    /// <c>Morning.Shoot</c>, which is three lines because of it.
    /// </summary>
    public Vector3 Watching { get; private set; }

    /// <summary>
    /// How far past the mouth of the bay the window is, in metres — negative while it is still inside.
    ///
    /// The one number the last chapter reads back out of this file, and everything that has to happen at
    /// the threshold hangs off it rather than off a second copy of the timings: the star coming up, the
    /// berth's light going out, the lane appearing, and the hull tone under all of it. A chapter that
    /// wrote its own seconds for those would be a chapter that could be retuned into disagreeing with the
    /// picture.
    /// </summary>
    public float Past { get; private set; } = -Berthed;

    /// <summary>
    /// Everything past the glass, under one node.
    ///
    /// It is public so that a chapter can put more out there without this file having to know what — the
    /// traffic in the lane on the last morning is eight more hulls hung off this, built by
    /// <see cref="Traffic"/> and driven by its own chapter. The name matters as much as the node does:
    /// <c>Ground</c> refuses to make anything called "outside" solid, so a freighter three hundred metres
    /// off is not something the visitor can walk into.
    /// </summary>
    public Node Outside { get; }

    /// <summary>
    /// The dust in this room's air, so somebody outside the room can switch it off.
    ///
    /// Three hundred additive points at a fifth of a pixel are dust when you are standing in them and are
    /// a starfield when you are eleven metres away looking through a doorway — the eye has no parallax to
    /// read at that range and no reason to think they are indoors. The film never had the problem because
    /// nobody was ever in the engine room while this room was drawn; the free walk opens that door and
    /// stands in it. See <c>Rounds</c>, which shows this only to somebody who is in the room.
    /// </summary>
    public Node Air => _drift;

    /// <summary>
    /// Where the star is, as a unit vector out of the gallery.
    ///
    /// Ninety-seven degrees west and twenty-six up, and both numbers are doing a job. Ninety-odd off the
    /// line of sight is what puts the terminator down the middle of the planet's disc rather than round its
    /// edge; and <i>past</i> ninety is what keeps the star itself out of the picture. A window that spans
    /// the whole outboard side can see anything up to about eighty-five degrees round, so a sun at
    /// eighty-eight would appear in the frame from the far end of the glass. At ninety-seven it is behind
    /// the plane of the plate, where the hull occludes it from everywhere in the room.
    ///
    /// West rather than east so the lit limb is the side the station is on, which is what makes Relay Nine
    /// a silhouette against light instead of a dark shape against the night side.
    /// </summary>
    private static Vector3 SunBearing => Bearing(-97f, 26f);

    /// <summary>Relay Nine, twenty-one degrees west of the planet and station-keeping against it: fourteen
    /// degrees off a disc fifteen across, which lands it on the lit limb. That is what "against" means when
    /// the thing behind you is a planet.</summary>
    private static Vector3 Station => Anchor + Bearing(-21f, 3.5f) * 285f;

    /// <summary>A point on the gallery's floor, <paramref name="x"/> along it and <paramref name="off"/>
    /// metres in from the hull.</summary>
    public static Vector3 Along(float x, float off, float eye = Deck.Eye) =>
        Deck.Illuminator + new Vector3(x, eye, Bore - off);

    /// <summary>A point on the glass, a quarter of the way along it per step from the door end. The window
    /// has no bays in it any more, but a walk still wants somewhere along it to look at.</summary>
    public static Vector3 Pane(int quarter)
    {
        var u = (Math.Clamp(quarter, 0, 3) + 0.5f) / 4f;

        return Deck.Illuminator + new Vector3(
            GlassEast + (GlassWest - GlassEast) * u, (Sill + Head) / 2f, Bore + Bedded);
    }

    /// <summary>
    /// How far the eye is from the plane of the glass, in metres.
    ///
    /// The one measurement in this room that is a distance to a surface rather than to a thing. The vent that
    /// washes the pane runs the whole thirteen metres, so how loud it is depends on how far off the glass he
    /// is standing and not at all on how far along it — and a soundtrack that asked for the nearest point on
    /// the window would be asking the wrong question in a way that only shows up at the ends of the run. See
    /// <c>Soundtrack.Window</c>, which is the only caller and takes a plane rather than a point because of it.
    /// </summary>
    public static float OffGlass(Vector3 eye) => MathF.Abs(Deck.Illuminator.Z + Bore + Bedded - eye.Z);

    /// <summary>The middle of the planet, for the shots that are only of it.</summary>
    public static Vector3 Planetfall => Deck.Illuminator + Anchor + Bearing(-5f, 2f) * PlanetRange;

    /// <summary>Relay Nine, for the shot it arrives in.</summary>
    public static Vector3 Relay => Deck.Illuminator + Station;

    /// <summary>Where the escort is by the time it has stopped, for the last shot of the film's
    /// building.</summary>
    public static Vector3 Holding => Deck.Illuminator + Anchor + Bearing(-9f, 5f) * 56f;

    /// <summary>Where the escort is while it is still crossing, for the shot it crosses in.</summary>
    public static Vector3 Crossing => Deck.Illuminator + Anchor + Bearing(-26f, 3f) * 67f;

    /// <summary>
    /// Straight down the ship's course: forty-five degrees off the beam, forward, and a long way out.
    ///
    /// Forty-five and not more, and the number is set by the room rather than chosen. The glass is in the
    /// outboard wall, so a look forward is a look <i>through</i> it at an angle, and the shallower the
    /// angle the further west the sightline leaves the room — past about fifty-two degrees, from where
    /// the film stands, it leaves through the west wall instead of the window. That is the honest limit
    /// of what "ahead" can mean on a ship whose windows are down its side, and it is why this chapter
    /// stands its visitor at the forward end rather than trying to give him a bow.
    /// </summary>
    public static Vector3 Ahead => Deck.Illuminator + Anchor + Bearing(-72f, 2.5f) * 420f;

    /// <summary>The lane the traffic is in: forward of the beam and a hundred and eighty metres off, which
    /// is where a hull twelve metres long is still a hull.</summary>
    public static Vector3 Lane => Deck.Illuminator + Anchor + Bearing(-22f, 0.5f) * 180f;

    /// <summary>
    /// The far wall of the berth, forty-six metres out and fifteen degrees forward of the beam.
    ///
    /// <b>Not the doorway, and that is a correction rather than a preference.</b> The mouth is at
    /// sixty-three degrees forward and eighty-four metres across, so a shot aimed at it is a shot in which
    /// the door is the entire frame — and a door, seen from that close, is a flat coloured rectangle with
    /// nothing in it. Aimed here instead, the wall runs away down the left of the picture with the lit
    /// mouth at the end of it, which is the difference between standing in front of a door and standing in
    /// a hangar.
    /// </summary>
    public static Vector3 Berthside => Deck.Illuminator + Anchor + Bearing(-15f, -3f) * 46f;

    /// <summary>Where the station ends up: fifty-six degrees aft of the beam, which is as far round as this
    /// window can follow anything, and is therefore where a ship leaving has to put the thing it left.</summary>
    public static Vector3 Quarter => Deck.Illuminator + Anchor + Bearing(56f, -2f) * 300f;

    /// <summary>Where the shooting is. Forward-outboard and high, and far enough off that it is three
    /// degrees of ship and a great deal of light — which is what somebody else's fight looks like.
    /// Fifty-eight degrees forward puts it just past the corner post, so it is in the bow port from the
    /// bay and in the panorama from anywhere else in the room.</summary>
    public static Vector3 Fight => Deck.Illuminator + Anchor + Bearing(-58f, 6.5f) * 275f;

    /// <summary>Where the escort sits on the way out: on point, seventy-four degrees forward, so it is
    /// dead ahead through the bow port. It is the only thing in that window that is not a star, and a
    /// window with one ship in it reads as further away than a window with none.</summary>
    private static Vector3 Point => Anchor + Bearing(-74f, 2f) * 62f;

    /// <summary>
    /// The escort on point, holding position ahead of the ship on the morning it leaves.
    ///
    /// It is the same hull chapter 7 brought in from the west quarter, stated to be somewhere else rather
    /// than flown there. Both chapters place it every frame from their own clock, so neither can inherit
    /// where the other left it — which is the only way two chapters are allowed to share a ship.
    ///
    /// <b>Holding position is not the same as being still, and it used to be.</b> This was a fixed
    /// transform, and next to a window with traffic crossing it and a station falling astern, the one hull
    /// the shot is actually pointed at was the one object nailed to the glass — which reads as a hole in
    /// the picture rather than as a ship. Nothing about station-keeping is motionless: a hull sixty-two
    /// metres out with a drive lit is being held there by small corrections, for ever.
    ///
    /// So a wander of a couple of degrees on each axis, and about a metre of drift, from four sines whose
    /// periods share no common multiple worth waiting for — seven, nine, eleven and thirteen seconds — so
    /// the loop never comes round in front of anybody. It is a function of the clock like everything else
    /// in this film, so seeking to a second still puts the escort where that second puts it.
    ///
    /// The burn moves with the yaw rather than on a clock of its own. A ship that swings its nose has just
    /// used its engine, and lighting the two independently is how an animation announces that it is two
    /// animations.
    /// </summary>
    /// <param name="clock">Seconds into the chapter.</param>
    public void Lead(float clock)
    {
        // Degrees, converted once. Small on purpose: this is a ship holding a station-keeping box, not one
        // manoeuvring, and past about three degrees it stops reading as correction and starts reading as
        // drift.
        var swing = 1.6f * MathF.Sin(clock * MathF.Tau / 11f);
        var rise = 0.9f * MathF.Sin(clock * MathF.Tau / 7f + 1.1f);
        var tilt = 2.2f * MathF.Sin(clock * MathF.Tau / 9f + 2.4f);

        // Nose down the course, and it is the one attitude in the film where a ship is pointing at the
        // camera rather than across it: three quarters on, running lights toward you, engines lit.
        var heading = Quaternion.CreateFromYawPitchRoll(
            (106f + swing) * MathF.PI / 180f,
            rise * MathF.PI / 180f,
            -0.06f + tilt * MathF.PI / 180f);

        var forward = Vector3.Transform(-Vector3.UnitZ, heading);

        // And the box it is holding, which is about a metre across. Along the course rather than across it
        // for the larger of the two, because a ship trimming its distance is doing something a viewer can
        // name and a ship sliding sideways is not.
        var station = Point
                      + forward * (0.75f * MathF.Sin(clock * MathF.Tau / 13f))
                      + Vector3.UnitY * (0.35f * MathF.Sin(clock * MathF.Tau / 7f));

        _escort.Place(station, heading, forward, forward * 3f);

        // Up where the nose is swinging hardest, which is where the engine would be doing the work — the
        // derivative of the swing, normalised, rather than a second wobble laid over the first.
        _escort.Burn(0.42f + 0.10f * MathF.Cos(clock * MathF.Tau / 11f));
        _escort.Beacon(1f);

        // No contact marker. It is sixty-two metres away and it is the thing the last shot of the film is
        // pointed at — a glow hung in front of it would be an aid to finding something nobody is looking
        // for. See Ship.Marked, and see Traffic for the other eight hulls this is true of.
        _escort.Marked = false;

        // A little aft of the hull's centre, which is where a drive is.
        // From the station it is actually holding rather than from the middle of the box, so the drive light
        // travels with the hull instead of the hull sliding off it.
        Running.Position = Deck.Illuminator + station - forward * (EscortLength * 0.3f);
    }

    /// <summary>
    /// Relay Nine, wherever it is right now.
    ///
    /// Live rather than a constant, because on the last morning the station is the one thing outside that
    /// moves a long way — see <see cref="Berth"/>. A shot that looked at where it used to be would drift
    /// off it over ten seconds, which is exactly the ten seconds it is worth looking at.
    /// </summary>
    public Vector3 Mooring => Deck.Illuminator + _relay.Position;

    /// <summary>
    /// Relay Nine seen from a ship that is still hours out: twenty-one degrees west and two hundred and
    /// eighty-five metres off, which is where chapter 7 has it.
    ///
    /// It is asserted rather than left alone, and that is the rule the whole film runs on. The morning
    /// chapter moves this station, turns it, and makes it fourteen times the size, so a chapter that only
    /// ever read what was out there would show a different sky depending on whether anybody had seeked
    /// forward and come back — which is the one thing a film made of pure functions of a clock must never
    /// do. Four assertions, and the fourth is the one that is easy to forget: the berth is switched off,
    /// and so is the door across it.
    /// </summary>
    public void Approach()
    {
        _relay.Position = Station;
        _relay.Rotation = Quaternion.Identity;
        _relay.Scale = new Vector3(StationSize);

        _gate.Root.IsVisible = false;
        _berth.IsVisible = false;

        Dock.Intensity = 0f;
        Portal.Intensity = 0f;

        Threshold = Relay;
        Watching = Relay;
        Past = -Berthed;
    }

    /// <summary>
    /// Relay Nine on the morning the ship lets go of it: the bay it is standing in, the door across the
    /// mouth, and the whole of it falling astern once the door is open.
    ///
    /// <b>It is one straight line and one turn, and everything else follows from where the station is.</b>
    /// The ship starts twenty metres inside a bay forty-one deep, goes out along the bay's own axis, and
    /// comes round thirty-four degrees once it is clear. Nothing is animated: given a second, this puts
    /// the station, its doorway and its light exactly where that second wants them, which is why the last
    /// chapter of the film can be jumped into at the moment the mouth is passing the window.
    ///
    /// The motion is <c>0.13 τ²  +  0.0018 τ³</c> from rest, and the two terms are two different machines.
    /// The square is manoeuvring thrust — a constant push, which is what warps a hull out of a berth at
    /// walking pace and is all anybody would use inside somebody else's station. The cube is the drive
    /// coming up underneath it once there is room, so the acceleration itself climbs; forty seconds later
    /// the ship is doing thirty metres a second and the station is a shape rather than a wall. A single
    /// eased ramp between two numbers cannot say that, because a ramp arrives at nought speed and a ship
    /// leaving is still speeding up when the film stops watching it.
    /// </summary>
    /// <param name="seconds">How far into the last chapter it is.</param>
    public void Leave(float seconds)
    {
        _relay.Scale = new Vector3(BerthSize);
        _gate.Root.IsVisible = true;
        _berth.IsVisible = true;

        var t = MathF.Max(0f, seconds - Release);

        // Held at Astern, which the departure reaches a sixteenth of a second after the last frame of the
        // film — so inside the film this line changes nothing, and outside it the cube stops running away.
        // The free walk goes on updating this chapter for as long as anybody stands at the window (see
        // Film.Beyond), and t cubed would take the station through the far plane about fifteen seconds
        // later: a dot in the window one minute and gone the next, for no reason anybody watching could
        // name. Further astern than this is not a difference a window can make out anyway.
        Past = MathF.Min(-Berthed + 0.13f * t * t + 0.0018f * t * t * t, Astern);

        // The turn, in radians, and it is a turn of the ship rather than of anything outside it — so the
        // whole world past the glass is rotated about the window by the same angle, positions and
        // attitudes together. Anything else is a station that slides sideways while facing where it was.
        var swing = -Sweep * Ramp(seconds, Release + 10f, 20f) * MathF.PI / 180f;
        var turn = Matrix4x4.CreateRotationY(swing);

        // A little nose-up as it goes, so the station settles below the eye line instead of sitting on it.
        // Written into the offset rather than into the attitude: over fourteen hundred metres it is three
        // degrees, and three degrees of pitch on a hull the camera is inside is not a thing anybody can be
        // shown — where it went is a thing they can.
        var axis = new Vector3(0f, -AxisDown - 0.055f * MathF.Max(0f, Past), AxisOut);

        _relay.Position = Anchor + Vector3.Transform(axis + new Vector3(Mouth + Past, 0f, 0f), turn);

        // Model +Z is the way the bay opens, and while he is tied on that is straight down the ship's
        // course — a quarter turn west. The swing is added to it rather than replacing it, because the
        // station has not moved: he has.
        _relay.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, swing - MathF.PI / 2f);

        _berth.Position = _relay.Position;
        _berth.Rotation = _relay.Rotation;

        Threshold = Deck.Illuminator + Anchor + Vector3.Transform(axis + new Vector3(Past, 0f, 0f), turn);

        // The berth's floods, on the gantry and therefore part of the berth: fixed where the ship was
        // parked, so they slide aft with everything else and go out by inverse square rather than by
        // anybody deciding when. It is the whole of what makes the room's light stop being the station's.
        Dock.Position = Deck.Illuminator + Anchor +
                        Vector3.Transform(new Vector3(Past + Berthed - 6f, 5f, 13f), turn);

        Dock.Intensity = 210f;

        // And the bay's own emissive, taken almost all the way off.
        //
        // Base paints it a soft green, which is exactly right at the range Contact looks at it from: at a
        // kilometre and a half the bay is eight pixels, and a lit tint is the only way eight pixels say
        // there is a room behind that door. Twenty metres away the same number is a wall that glows, and
        // what it glows is the colour of the door — so the first shot of the chapter came back as a green
        // box with a green door in it and no plating visible anywhere. A wall is lit by lamps or it is not
        // lit. This one is, and Dock is the lamp.
        _bay.EmissiveColor = new Vector3(0.008f, 0.026f, 0.018f);

        // What there is to look at, in three stages and two blends.
        //
        // A point on the far wall of the bay while he is tied on, because a doorway eighty-four metres
        // across seen from twenty is a coloured rectangle and a wall running away toward it is a room. The
        // doorway itself once it is close enough to be one — it arrives at the window, so there is a
        // second where it is the only thing there is. And then the station, once it is far enough off to
        // be a shape. The wall is fixed in the berth rather than in front of the window, so it slides aft
        // with everything else and the eye is already travelling before the first blend starts.
        var wall = Deck.Illuminator + Anchor +
                   Vector3.Transform(new Vector3(Past + Berthed - 12f, -1f, AxisOut + Clear), turn);

        // Three metres above the middle of the doorway rather than at it. The mouth arrives beside the
        // window and the window's sill is a metre below the eye, so an aim at the true centre puts a
        // console band across the bottom third of the frame for the five seconds the door is going past —
        // which is the five seconds the chapter is for. Three metres is one degree at the range it starts
        // at and twenty at the range it passes at, which is the right way round.
        Watching = Vector3.Lerp(
            Vector3.Lerp(wall, Threshold + Vector3.UnitY * 3f, Ramp(Past, -16f, 14f)),
            Mooring,
            Ramp(Past, 30f, 90f));

        // The gantry, folded back into the wall as the clamps come off. It is the only moving part in the
        // berth and it is there so that the ship letting go is something that happens rather than
        // something that is announced by the ship starting to move.
        _gantry.Position = new Vector3(12f * Ramp(seconds, Unclamps, 5f), 0f, 0f);

        // The lamps down the wall, running toward the door. They chase the other way from the station's
        // own six, which is what a lane of lights on a floor does and what the six round a mouth do not.
        for (var i = 0; i < _guides.Length; i++)
        {
            var phase = Fraction(seconds * 0.55f + i / (float)Guides);
            Paint(_guides[i], new Vector3(1f, 0.72f, 0.24f), 0.16f + 0.84f * MathF.Pow(1f - phase, 5f));
        }
    }

    /// <summary>
    /// The door across the mouth, and the light it throws into the room going through it.
    ///
    /// Contact's gate, driven by Contact's own arithmetic — two counter-rotating swirls at rates with no
    /// common period, three rings scaling out on thirds of a phase, and one colour from red to green. What
    /// is different here is only what it is being looked at from: in the battle it is a signal a kilometre
    /// and a half off, and here the room goes through it.
    /// </summary>
    /// <param name="open">Nought while the bay is shut, one once it is cleared.</param>
    /// <param name="clock">The chapter's own second, for the swirls.</param>
    public void Gate(float open, float clock)
    {
        open = Math.Clamp(open, 0f, 1f);

        var tint = Vector3.Lerp(new Vector3(1.00f, 0.15f, 0.07f), new Vector3(0.24f, 1.00f, 0.46f), open);

        // The door itself, and it is <b>dark</b> while it is shut rather than bright.
        //
        // It was at full tint, on the reasoning that a shut gate should be a wall of red — and that made the
        // swirls and the rings turning on it invisible, which is a strange thing for the busiest object in
        // the shot to be. The reason is that they are the same colour it is. Red here is (1.00, 0.15, 0.07):
        // the channel carrying nearly all of it is already at one, so an additive ring drawn over it has
        // nowhere left to go and moves the picture by a few hundredths of green. Green is (0.24, 1.00, 0.46)
        // and has room in two channels, which is why the identical animation is unmissable the moment the bay
        // clears — the gate was never still, it was hiding.
        //
        // A third of the tint leaves the door reading as solid and gives the layers over it somewhere to be
        // brighter than. It costs the room nothing: what the shut gate throws into the gallery is Portal
        // below, not this card.
        _gate.PlateSkin.BaseColor = new Vector4(tint * (0.34f + 0.11f * open), 1f - open);

        var body = 0.62f - 0.34f * open;

        for (var i = 0; i < _gate.Swirls.Length; i++)
        {
            var spin = i == 0 ? -0.31f : 0.55f;
            _gate.Swirls[i].Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, clock * spin);
            _gate.SwirlSkins[i].BaseColor = new Vector4(tint, body * (i == 0 ? 0.55f : 1f));

            var breathe = 1f + 0.02f * MathF.Sin(clock * (i == 0 ? 2.3f : 1.7f));
            _gate.Swirls[i].Scale = new Vector3(breathe, breathe, 1f);
        }

        for (var i = 0; i < _gate.Rings.Length; i++)
        {
            var phase = Fraction(clock * 0.5f + i / 3f);
            var reach = 0.20f + 0.80f * phase;

            _gate.Rings[i].Scale = new Vector3(reach, reach, 1f);
            _gate.RingSkins[i].BaseColor = new Vector4(tint, 0.85f * phase * (1f - phase) * 4f);
        }

        // The light sits in the plane of the door, so it is brightest exactly as the room crosses it and
        // is behind everything a moment later. Falls off with the square like every other point light in
        // the building, which at eighty metres of range means it has stopped mattering by the time the
        // chapter drops it from the slots.
        Portal.Position = Threshold;
        Portal.Color = tint;

        // Lit while it is shut as well, at a bit over a quarter. A door eighty-four metres across and
        // twenty away throws its colour into the room whichever colour it is, and the version that only
        // came on when the bay cleared had a first shot lit by one lamp on a gantry and nothing else —
        // a dark room with a flat red rectangle in it. Now the room is red until it is green, which is
        // also the cheapest possible way of saying that the thing about to happen has not happened yet.
        Portal.Intensity = 70f * (0.28f + 0.72f * open);
    }

    /// <summary>
    /// How hard the star is allowed to shine into this room.
    ///
    /// A <see cref="DirectionalLight"/> has infinite reach and this renderer casts no shadows, so a sun
    /// ninety-seven degrees round lights the inside of a docking bay exactly as well as it lights open
    /// space — which is the one thing being inside a station has to mean. Both chapters that use the
    /// gallery assert it, and the run-in asserts it at full even though it never changes it, for the same
    /// reason it asserts everything else out there.
    /// </summary>
    public void Star(float level) => Sunlight.Intensity = 1.15f * Math.Clamp(level, 0f, 1f);

    /// <summary>Where the plot table stands: inboard of the walk and close to the door, so it is in the
    /// first frame of the room rather than something to be found.</summary>
    private static readonly Vector3 TableAt = new(3.4f, 0f, 1.0f);

    /// <summary>The globe over the plot table, for the one shot that is of it.</summary>
    public static Vector3 Table => Deck.Illuminator + TableAt + new Vector3(0f, 1.42f, 0f);

    /// <summary>Standing in the doorway, which is measured from the room's own back wall rather than from
    /// the hull — <see cref="Along"/> counts in from the glass, and the glass is four metres away.</summary>
    public static Vector3 Way => Deck.Illuminator + new Vector3(Doorway, Deck.Eye, -0.2f);

    /// <summary>The way back, for the one look over his shoulder.</summary>
    public static Vector3 Doorstep => Deck.Illuminator + new Vector3(Doorway, 1.4f, -Back / 2f);

    /// <summary>
    /// The escort, flying the only trajectory in this chapter: in from the west quarter, decelerating, and
    /// stopped by the time he reaches the last bay.
    ///
    /// It is a pure function of the chapter's clock like everything else that moves in this film, and it is
    /// worth saying why that matters more here than anywhere: this is a ship out of <c>Contact</c>, whose
    /// whole flight model integrates, and the reason it can be dropped into a seekable film is that
    /// <see cref="Ship.Place"/> exists — a ship can be told where it is instead of asked to fly there.
    /// </summary>
    /// <param name="u">Nought at the start of the approach, one when it is holding station.</param>
    public void Escort(float u)
    {
        u = Math.Clamp(u, 0f, 1f);

        // Eased at both ends rather than only at the stop. A ship that starts at full drift the instant the
        // chapter's clock passes a number is a ship that was teleported into motion; smoothstep gives it a
        // bearing rate that begins at nothing, peaks halfway across and is back at nothing by the time it
        // is holding, which is what a small ship coming onto station actually does.
        var closed = u * u * (3f - 2f * u);

        var azimuth = -52f + 43f * closed;
        var range = 78f - 22f * closed;
        var at = Anchor + Bearing(azimuth, 1.5f + 3.5f * closed) * range;

        // Nose along its own drift, which is very nearly due east, so it lies broadside to the window — the
        // one attitude in which a hull fourteen metres long reads as a vehicle rather than as a mark. A
        // shallow bank comes off as it settles, which is what stopping looks like.
        var heading = Quaternion.CreateFromYawPitchRoll(
            (90f + azimuth * 0.35f) * MathF.PI / 180f, 0f, (1f - closed) * -0.22f);

        var forward = Vector3.Transform(-Vector3.UnitZ, heading);

        _escort.Place(at, heading, forward, forward * (14f * (1f - closed)));

        // Full power on the way in, idle once it is stopped, and the running lights on the whole time — it
        // is waiting, and a ship that is waiting is a ship with its lamps on.
        _escort.Burn(0.16f + 0.84f * (1f - closed));
        _escort.Beacon(1f);
    }

    /// <summary>
    /// The two things on the escort that need to know where the camera ended up: the contact marker, and
    /// the roll of the exhaust cards.
    ///
    /// The chapter has the camera because the chapter owns the walk, which is the same reason
    /// <see cref="Corridor.Alarm"/> is handed an eye. At fifty-six metres the marker is inside eight ship
    /// lengths and stays at nothing, which is right — a contact marker is for a hull too far off to read,
    /// and this one is close enough to count its engines.
    /// </summary>
    public void Watch(Vector3 eye) => _escort.Mark(eye - Deck.Illuminator);

    /// <summary>
    /// Relay Nine's lamps: six round the mouth of the docking bay, chasing.
    ///
    /// They are the only thing on the station that can be made out at this range and they are unlit, which
    /// is the point of them being visible at all. Nineteen metres of hull at two hundred and eighty-five is
    /// four degrees of grey shape; six lamps running round its bay is what makes it read as manned.
    /// </summary>
    public void Base(float clock)
    {
        var idle = new Vector4(0.05f, 0.09f, 0.07f, 1f);
        var burning = new Vector4(0.30f, 1f, 0.52f, 1f);

        for (var i = 0; i < _lamps.Length; i++)
        {
            var phase = Fraction(clock * 0.42f - i / (float)_lamps.Length);
            _lamps[i].BaseColor = Vector4.Lerp(idle, burning, MathF.Pow(1f - phase, 6f));
        }

        _collar.BaseColor = new Vector4(0.10f, 0.42f, 0.26f, 1f);
        _bay.EmissiveColor = new Vector3(0.06f, 0.26f, 0.15f);
    }

    /// <summary>
    /// Everything in this room that is bright and is not outside it: the trim round the glass, the deck
    /// strips, the coves, the spill on the deck and the hologram over the plot table.
    ///
    /// Every one of them is an <see cref="Material.Unlit"/> additive surface, which is the whole reason a
    /// room with a single <see cref="DirectionalLight"/> in it can be this dressed. The film has been
    /// making that argument since a bulb in the antechamber; this is the room where the argument buys the
    /// most, because everything the eye reads as <i>furnished</i> in here is costing nothing at all.
    /// </summary>
    public void Trim(float level)
    {
        // A fifth of what it was, and the reason is the room going white.
        //
        // Additive emission is <i>added</i> to whatever it is drawn over. Over near-black plate a value of
        // a half is a warm line; over a white panel the same value is one and a bit, which clips, and what
        // clips is not a brighter gold — it is white. Every strip in here read as a fluorescent tube on the
        // first pass for exactly that reason. The colours are more saturated to match, because a hue that
        // is going to be summed with a light grey has to start further from it.
        Paint(_deck, new Vector3(1f, 0.62f, 0.20f), level * 0.20f);
        Paint(_trim, new Vector3(1f, 0.66f, 0.26f), level * 0.24f);
        Paint(_cove, new Vector3(1f, 0.70f, 0.32f), level * 0.11f);
        Paint(_spill, new Vector3(0.52f, 0.66f, 0.88f), level * 0.0065f);
        Paint(_holo, new Vector3(0.36f, 0.86f, 1f), level * 0.30f);
        Paint(_screen, new Vector3(0.34f, 0.62f, 0.92f), level * 0.32f);
        Paint(_sheen, new Vector3(1f, 0.60f, 0.18f), level * 0.016f);

        // The coffers are the largest lit area in the building by a long way — six panels of two square
        // metres each, overhead, in frame every time he looks along the room. So they run at a twentieth of
        // what a strip runs at. A strip is a line and reads as bright at any value; a panel is an area, and
        // an area at a strip's value is a ceiling made of daylight.
        Paint(_lamp, new Vector3(0.82f, 0.90f, 1f), level * 0.052f);
        Paint(_gleam, new Vector3(0.62f, 0.76f, 1f), level * 0.020f);

        // The keys are the one thing in here that is not painted, because they are the one thing in here
        // that is not additive: they are a lit material with an emissive map, so all that has to come up
        // with the room is how hard the map is driven. Their base colour is left alone — the caps are
        // moulded plastic and are as bright as the room is, which is the whole reason they were built
        // this way rather than as another glowing rectangle.
        _keys.EmissiveColor = new Vector3(level * 0.52f);

        // The projector column is a twentieth of what the globe is, and it was two thirds of it once. Two
        // nested additive cones over a lit disc add up wherever they overlap, so a value that is right on
        // one surface is right nowhere: down the axis it was four surfaces deep and came back as a solid
        // white funnel with a wireframe balanced on top. Air is not a surface — the corridor's beacons say
        // the same thing about the same mistake — and the fix is the same one, which is to make the thing
        // standing in for light dimmer than anything it is supposed to be lighting.
        Paint(_beam, new Vector3(0.30f, 0.72f, 0.95f), level * 0.016f);

        foreach (var line in _chart)
            line.Opacity = level * 0.5f;

        foreach (var cloud in _dust)
            cloud.Opacity = level * 0.30f;
    }

    /// <summary>
    /// What the twenty-one screens are doing this second: a wipe crossing each of them and an indicator
    /// beside it.
    ///
    /// It takes the room's level as well as the clock, and it takes it rather than remembering the last one
    /// <see cref="Trim"/> was given. Both callers hand the same number to both, which is one line of
    /// duplication and buys the thing this whole file is built on: a room that is a function of its
    /// arguments cannot be got into a state, and seeking into the middle of the morning shows exactly what
    /// playing to it would.
    ///
    /// The wipe is pinched to nothing at both ends of its travel — see <see cref="Screen"/> for why it is
    /// geometry and not a redrawn texture. A bar at full width that reappears at the far edge is not a
    /// refresh line, it is a bar teleporting, and at a tenth of a hertz there is plenty of time to watch it
    /// do it.
    /// </summary>
    public void Readouts(float clock, float level)
    {
        Paint(_wipe, Refresh, level * 0.15f);

        foreach (var part in _live)
        {
            var t = Fraction(clock * part.Rate + part.Phase);

            switch (part.Kind)
            {
                case Motion.Needle:
                    part.Part.RotationDegrees = new Vector3(0f, 0f, part.From + part.Span * Rock(t));
                    break;

                case Motion.Wipe:
                    part.Part.Position = part.Home + part.Travel * (t * 2f - 1f);

                    // Pinched to nothing at both ends of the travel. A bar at full width that reappears at
                    // the far edge is not a refresh line, it is a bar teleporting — and at a tenth of a
                    // hertz there is a great deal of time to watch it do it.
                    part.Part.Scale = new Vector3(
                        MathF.Max(Grain.Step(0f, 0.10f, t) * (1f - Grain.Step(0.90f, 1f, t)), 0.02f),
                        1f,
                        1f);
                    break;

                case Motion.Marker:
                    part.Part.Position = part.Home + part.Travel * (Rock(t) * 2f - 1f);
                    break;

                // Twelve whole segments, and the top one is a segment like the others: Rock reaches exactly
                // one at the turn, and a floor of twelve is a thirteenth segment on a gauge that has twelve.
                case Motion.Climb:
                    part.Part.Position = part.Home
                        + part.Travel * (MathF.Min(MathF.Floor(Rock(t) * 12f), 11f) / 5.5f - 1f);
                    break;

                // Down the list rather than up it, which is the way anything is ever read.
                case Motion.Step:
                    part.Part.Position = part.Home + part.Travel * (1f - MathF.Floor(t * 6f) / 2.5f);
                    break;

                default:
                    part.Part.IsVisible = t < part.Duty;
                    break;
            }
        }

        foreach (var pip in _pips)
            Paint(pip.Material, Indicator,
                level * (Fraction(clock * pip.Rate + pip.Phase) < pip.Duty ? 0.50f : 0.05f));
    }

    /// <summary>There and back: a triangle from 0 up to 1 and down again over one cycle. What a needle and a
    /// level both do, and what a refresh line never does.</summary>
    private static float Rock(float t) => 1f - MathF.Abs(t * 2f - 1f);

    /// <summary>
    /// The plot: a wireframe world turning over the table in the middle of the gallery, with three orbits
    /// round it going the other way.
    ///
    /// It is the most futuristic object in the building and it is the cheapest: two
    /// <see cref="LineNode"/>s, a handful of points and a glow, all additive and none of them lit. Nothing
    /// about it is animated except two node rotations — the globe turns one way, the orbits the other, at
    /// rates that do not divide into each other, so it never visibly repeats and never has to be told what
    /// frame it is on.
    /// </summary>
    public void Hologram(float clock)
    {
        _globe.RotationDegrees = new Vector3(0f, clock * 9f, 0f);
        _orbits.RotationDegrees = new Vector3(0f, -clock * 5.5f, 0f);

        // The motes, drifting. One node rotation about a centre a long way below the deck turns into a
        // slow sideways crawl up here, which is what dust in still air does and costs one matrix.
        //
        // <b>It rocks rather than turns, and that is a fix rather than a refinement.</b> The angle used to
        // be the clock times a third of a degree, which is a crawl for the first minute and is a lever
        // thirty metres long for the rest: by the last frame of the film the clock is past four hundred
        // and the cloud has come round a hundred and forty-five degrees, which at that radius carries every
        // mote clean out of this room and leaves it hanging in the engine room and the corridor. What that
        // looks like from inside the ship is a field of small white points floating through the bulkheads,
        // and it was reported as exactly that — a starfield indoors. Nobody saw it while the film was the
        // only way to watch, because the film never stands anywhere it had drifted to.
        //
        // Six tenths of a degree either side of nothing, over three minutes. At thirty metres that is a
        // third of a metre of travel at about a centimetre a second — the same crawl, made of a number the
        // clock cannot run away with.
        _drift.RotationDegrees = new Vector3(0f, 0.6f * MathF.Sin(clock * MathF.Tau / 180f), 0f);
    }

    /// <summary>The planet turning. A twelfth of a degree a second, which is a day of about three hours —
    /// slow enough that nothing appears to move and fast enough that the terminator is not painted
    /// on.</summary>
    public void Turn(float clock) =>
        _planet.RotationDegrees = new Vector3(-16f, clock * 0.085f, 7f);

    /// <summary>
    /// The inside of the berth, which the station model has no reason to have.
    ///
    /// Relay Nine's docking bay is authored for a shot from three kilometres away: a square tube with a
    /// lit hall at the back of it, correct at the size a station is a shape. The last chapter parks a man
    /// twenty metres inside it and gives him fifteen seconds to look, and at that range an empty tube is
    /// an empty tube. So it gets what a working berth has — a light down each corner, two rows of guide
    /// lamps running toward the door along the wall the window faces, and a pair of clamps holding the
    /// ship that let go before it moves.
    ///
    /// <b>It is built in metres and the station is built at six hundred and twenty to one</b>, which is
    /// why this is its own node rather than a child of the station. What keeps the two together is
    /// <see cref="Leave"/> writing the same position and the same rotation into both, every frame, from
    /// one calculation — the alternative is authoring a two-metre lamp as a number with four zeros in
    /// front of it and finding out at the far end of the film whether it was the right number of zeros.
    ///
    /// The frame is the station's own: <c>+z</c> is the way the bay opens, <c>+x</c> is outboard once the
    /// quarter-turn is on, and the mouth is at <see cref="Mouth"/>.
    /// </summary>
    private void Berthing(Node outside, Material steel)
    {
        outside.Children.Add(_berth);

        var back = Mouth - Deep;

        // Ribs across the wall the window faces, and they are the most important thing in here.
        //
        // The bay is a tube with no features on it at all — at Contact's range it never needed any — and a
        // featureless wall thirty-four metres off has one fatal property: it does not move. The ship pulls
        // out at four metres a second past a flat surface and the picture is a flat surface. Seven frames
        // across it, five metres apart, and the same four metres a second is a rhythm going past the
        // glass, which is the whole of what the departure is made of. Everything else in this method is
        // decoration; this is the shot.
        for (var i = 0; i < Ribs; i++)
            _berth.Children.Add(new MeshNode(Primitives.Box(1.6f, Clear * 2f, 1.5f), steel)
            {
                Position = new Vector3(
                    Clear - 0.8f, 0f, back + 3.5f + (Deep - 7f) * i / (Ribs - 1f)),
                Name = "berth.rib"
            });

        // A light down each corner of the tube. Four lines converging on a doorway is the cheapest
        // possible statement that a space has a length, and it is the one thing in here that will still
        // read when the whole bay is a rectangle a hundred metres astern.
        var edge = Glow(0.62f, 0.78f, 1f);
        Paint(edge, new Vector3(0.62f, 0.78f, 1f), 0.75f);

        for (var i = 0; i < 4; i++)
        {
            var x = (i & 1) == 0 ? -1f : 1f;
            var y = (i & 2) == 0 ? -1f : 1f;

            _berth.Children.Add(new MeshNode(Primitives.Box(1.1f, 1.1f, Deep - 2f), edge)
            {
                Position = new Vector3(x * Clear * 0.96f, y * Clear * 0.96f, back + Deep / 2f),
                Name = "berth.edge"
            });
        }

        // Two rows of guide lamps between the ribs, on the one wall the window is looking at. They are the
        // only thing in the berth that moves before the ship does — see Leave, where they chase toward the
        // door — and once the ship is out they are what makes the mouth read as lit from inside rather
        // than as a hole.
        for (var i = 0; i < _guides.Length; i++)
        {
            _guides[i] = Glow(1f, 0.72f, 0.24f);

            var along = i % Guides;
            var row = i / Guides;

            _berth.Children.Add(new MeshNode(Primitives.Box(1.4f, 3.4f, 2.6f), _guides[i])
            {
                Position = new Vector3(
                    Clear - 1.6f,
                    row == 0 ? 11f : -11f,
                    back + 6f + (Deep - 12f) * along / (Guides - 1f)),
                Name = "berth.guide"
            });
        }

        // And the two clamps, above and below the line of sight rather than across it. They are eighteen
        // metres off the glass, which is close enough to be machinery and far enough to be somebody
        // else's, and they fold back into the wall six seconds before anything else in the chapter happens
        // — so the ship letting go is a thing that is seen and then heard rather than a thing the ship
        // announces by starting to move.
        _berth.Children.Add(_gantry);

        foreach (var y in (float[])[13f, -13f])
        {
            _gantry.Children.Add(new MeshNode(Primitives.Box(9f, 1.3f, 1.3f), steel)
            {
                Position = new Vector3(Clear - 5.5f, y, back + Deep * 0.56f),
                Name = "berth.clamp"
            });

            _gantry.Children.Add(new MeshNode(Primitives.Box(1.2f, 2.6f, 2.8f), steel)
            {
                Position = new Vector3(Clear - 10.5f, y, back + Deep * 0.56f),
                Name = "berth.clamp.head"
            });
        }
    }

    /// <summary>
    /// A unit vector out of the gallery, from a bearing and an elevation in degrees.
    ///
    /// Azimuth is measured from straight out — local +Z, which is outboard — and runs positive toward +X,
    /// which is east and is behind him once he has turned to walk the gallery. Everything outside this hull
    /// is placed in two angles and a range, because a window is a direction and a distance and nothing
    /// else; writing them as vectors would be writing down the cosines by hand.
    /// </summary>
    private static Vector3 Bearing(float azimuth, float elevation)
    {
        var a = azimuth * MathF.PI / 180f;
        var e = elevation * MathF.PI / 180f;
        var flat = MathF.Cos(e);

        return new Vector3(MathF.Sin(a) * flat, MathF.Sin(e), MathF.Cos(a) * flat);
    }

    /// <summary>Floor, ceiling, two ends and the wall he comes through.</summary>
    private void Shell(Node root, Material plate, Material floor)
    {
        var middle = (West + East) / 2f;
        var width = East - West;
        var run = Bore + Hull;

        var deep = Bore - Apron;
        var at = (Apron + Bore) / 2f;

        root.Children.Add(new MeshNode(
            Fabric.Map(Primitives.Plane(width, deep), floor, new Vector3(middle, 0f, at)), floor)
        {
            Position = new Vector3(middle, 0f, at),
            Name = "floor"
        });

        root.Children.Add(new MeshNode(
            Fabric.Map(Primitives.Plane(width, deep), plate, new Vector3(middle, Height, at)), plate)
        {
            Position = new Vector3(middle, Height, at),
            RotationDegrees = new Vector3(180f, 0f, 0f),
            Name = "ceiling"
        });

        // The wall he comes through, pierced where the engine room's north door is. It swallows that wall
        // whole — see Back — so the doorway is a reveal five hundred and fifty millimetres deep with two
        // walls in it and no seam anywhere.
        var south = Fabric.PiercedWall(
            width + 2f * Thickness, Height, Back,
            doorCentre: Doorway - middle, Deck.DoorWidth, Deck.DoorHeight, plate);

        south.Position = new Vector3(middle, 0f, -Back / 2f);
        root.Children.Add(south);

        // The aft end, which is a partition and closes the room.
        //
        // <b>And it is painted a fifth of what the rest of the shell is, for the reason the deck is.</b>
        // The star is ninety-seven degrees west — see Star — so it comes down the room lengthways, and this
        // is the one large surface in the building standing square across it. A directional light has
        // infinite reach and this renderer casts no shadows, so a wall inside a hull is lit exactly as hard
        // as the hull is: at the shell's own albedo it came back as a flat pale rectangle five metres wide
        // with the composite's grain speckled over it, which from the far end of the room reads as a hole
        // in the ship with a starfield behind it. It was reported as one.
        //
        // The film never looks at it, which is why it stood for so long — chapter 7 walks away from this
        // wall and chapter 9 stands at the other end with its back to it. The free walk turns round.
        // Taking the colour down was not enough on its own and the measurement says why: at a fifth of the
        // albedo the wall only came down from 141 to 89 of 255, because a quarter of what was arriving was
        // the shell's own specular and specular does not care what colour a surface is. Matte and not metal
        // is the other half of it — this is a painted partition rather than the moulded laminate the rest
        // of the room is faced in, which is what a partition at the back of a room usually is.
        var partition = plate.Clone();
        partition.BaseColor = new Vector4(0.024f, 0.026f, 0.031f, 1f);
        partition.Metallic = 0f;
        partition.Roughness = 1f;
        partition.MetallicRoughnessTexture = null;
        partition.Name = "gallery.aft";

        var aft = Fabric.Wall(run - Apron + 2f * Thickness, Height, Thickness, partition);
        aft.Position = new Vector3(East + Thickness / 2f, 0f, (Apron + run) / 2f);
        aft.RotationDegrees = new Vector3(0f, 90f, 0f);
        root.Children.Add(aft);

        // And the forward end, which is a hull and has a window in it — so what is built here is only the
        // inboard part of it, back to where the port begins. See Prow for the rest and for why there is a
        // window in this wall at all.
        var forward = Fabric.Wall(PortIn - Apron + 2f * Thickness, Height, Thickness, plate);
        forward.Position = new Vector3(West - Thickness / 2f, 0f, (Apron + PortIn) / 2f);
        forward.RotationDegrees = new Vector3(0f, 90f, 0f);
        root.Children.Add(forward);
    }

    /// <summary>
    /// The bow port: three metres of glass in the forward end wall, meeting the panorama at the corner.
    ///
    /// It is the panorama's own detail turned through ninety degrees — a sill below, a header above, a
    /// jamb inboard, a pane bedded well into the reveal, and the same warm rebate line round the head —
    /// and that sameness is deliberate. Two windows in one room detailed two ways are two windows; two
    /// windows detailed the same way are one window that goes round a corner, which is what a bay is and
    /// is what this end of the room is meant to feel like.
    ///
    /// <b>The corner is left as a post rather than mitred</b>, and that is not a shortcut either. Glass
    /// meeting glass at a corner with nothing between is a thing a hull cannot do — something has to
    /// carry the load down — so the eight hundred millimetres of hull between the end of the panorama and
    /// the end wall stays, seen edge-on from inside as a single dark upright between two lit views. It is
    /// the only piece of structure in the last frame of the film and it is what stops that frame reading
    /// as a photograph of space.
    /// </summary>
    private void Prowport(Node root, Material plate, Material steel)
    {
        var span = Bore - PortIn;
        var at = (PortIn + Bore) / 2f;
        var x = West - Prow / 2f;

        // Reusing the panorama's reveal finish rather than the room's panel, because this is the same
        // piece of ship: a moulded composite lining a cut in the hull, not the wall of a room.
        var hull = Finish.Composite();
        hull.BaseColor = new Vector4(0.085f, 0.092f, 0.108f, 1f);
        hull.Metallic = 0.2f;
        hull.Name = "gallery.prow";

        root.Children.Add(Fabric.Slab(
            new Vector3(Prow, Sill, span), new Vector3(x, Sill / 2f, at), hull, "prow.sill"));

        root.Children.Add(Fabric.Slab(
            new Vector3(Prow, Height - Head, span),
            new Vector3(x, (Height + Head) / 2f, at), hull, "prow.header"));

        // The inboard jamb, which runs back past the wall behind it so there is no face to face seam.
        root.Children.Add(Fabric.Slab(
            new Vector3(Prow, Height, PortIn - Apron + Thickness),
            new Vector3(x, Height / 2f, (Apron - Thickness + PortIn) / 2f), hull, "prow.jamb"));

        // A shelf under it, level with the panorama's, so the two openings share a line all the way round
        // the corner.
        root.Children.Add(Fabric.Slab(
            new Vector3(Ledge, 0.06f, span + 0.4f),
            new Vector3(West + Ledge / 2f, Sill - 0.03f, at), steel, "prow.shelf", Finish.Close));

        root.Children.Add(Fabric.Slab(
            new Vector3(0.02f, 0.028f, span + 0.4f),
            new Vector3(West + Ledge + 0.011f, Sill - 0.085f, at), _trim, "prow.under", Finish.Close));

        // And the rebate at the head, which is the line that carries round the corner from the panorama's.
        root.Children.Add(Fabric.Slab(
            new Vector3(0.02f, 0.028f, span),
            new Vector3(West + 0.012f, Head - 0.055f, at), _trim, "prow.rebate", Finish.Close));

        // A rail across it at the same height as the one along the glass, on two stanchions. It is what
        // makes the bay somewhere to stand rather than a drop, and it gives the one view in the film with
        // nothing in it at all a horizontal to be measured against.
        root.Children.Add(new MeshNode(
            Primitives.Cylinder(0.028f, 0.028f, span - 0.5f, 12), steel)
        {
            Position = new Vector3(West + 0.34f, 1.02f, at),

            // About X, so it lies across the bay. A cylinder stands on its end unrotated, and this one
            // spent its first build as a two-and-a-half-metre post in the middle of the one view in the
            // film that is supposed to have nothing in it — which the walk audit found before a camera
            // did, by reporting that the visitor could get seven tenths of a metre forward and no more.
            RotationDegrees = new Vector3(90f, 0f, 0f),
            Name = "prow.rail"
        });

        foreach (var z in new[] { PortIn + 0.5f, Bore - 0.5f })
            root.Children.Add(new MeshNode(Primitives.Cylinder(0.026f, 0.034f, 1.02f, 10), steel)
            {
                Position = new Vector3(West + 0.34f, 0.51f, z),
                Name = "prow.stanchion"
            });

        Corner(root, hull);
    }

    /// <summary>
    /// The corner post, lit.
    ///
    /// It is the piece of hull between the two windows — the outboard jamb of the bow port on one face and
    /// the west return of the panorama on the other — and it is the largest single object in the last two
    /// minutes of the film. From the bay it is half a metre of plate two and a half metres tall standing
    /// dead centre between the traffic and the planet, and until it had this on it, it was exactly that:
    /// a black slab across a fifth of the frame with nothing on it to say what it was.
    ///
    /// <b>The fix is not to make it smaller.</b> Something has to carry the load down where two windows
    /// meet, and a corner only as wide as the render can get away with is the thing that makes spacecraft
    /// interiors read as sets. What it needed was the same treatment as everything else in this room that
    /// is structure: a channel of light down it, in a groove rather than on a face.
    ///
    /// <b>The groove is made of hull and not of steel</b>, and that is the correction rather than the
    /// design. The first cut flanked the light with two brushed-metal battens, on the reasoning that a
    /// fitting is a fitting — and brushed metal has an anisotropic streak in it that is invisible on a
    /// rail seen at two metres and is pure noise on a strip fifty millimetres wide. Worse, the star is
    /// ninety-seven degrees west, so a batten standing proud of a wall presents it a west-facing face at
    /// nearly full incidence: two blown white lines down the middle of the last frame of the film, which
    /// is what came back. The same two pieces in the reveal's own composite are matte, dark, and read as
    /// what they are, which is the edge of a slot.
    /// </summary>
    private void Corner(Node root, Material hull)
    {
        // Halfway across the post's inboard face, which is the plane the bow port's reveal ends on.
        var x = (West - Prow + GlassWest) / 2f;
        var span = (Sill + Head) / 2f;

        // At the cove's value rather than the trim's, which is half. It is the longest continuous run of
        // light in the room and it is in the middle of the one frame that has nothing else in it.
        root.Children.Add(Fabric.Slab(
            new Vector3(0.075f, Head - Sill - 0.5f, 0.018f),
            new Vector3(x, span, Bore - 0.012f),
            _cove,
            "corner.channel",
            Finish.Close));

        // The cheeks stand five millimetres proud of the light and no more. Deeper than that and the near
        // one occludes the whole channel the moment the camera is off-axis — which is every frame in the
        // last chapter, since the visitor is standing beside this post rather than in front of it.
        foreach (var side in new[] { -1f, 1f })
            root.Children.Add(Fabric.Slab(
                new Vector3(0.045f, Head - Sill - 0.32f, 0.028f),
                new Vector3(x + side * 0.06f, span, Bore - 0.017f),
                hull,
                "corner.cheek",
                Finish.Close));
    }

    /// <summary>
    /// The hull, and the thirteen metres of it that is glass.
    ///
    /// A sill below, a header above, a return at each end, three mullions between the bays, and a pane in
    /// each. Every piece of it is a box a metre deep, which is the only reason a window this size costs
    /// nothing: an opening is where the geometry is not, and this one is nine boxes and four quads.
    ///
    /// The panes are bedded four hundred and twenty millimetres into the reveal rather than flush with
    /// either face, and that is two things at once. It is what a pane set into a hull looks like — steel in
    /// front of the glass as well as behind it — and it keeps the glass out of the plane of anything else,
    /// which matters because a transparent quad sitting exactly in a cut face is two surfaces the depth
    /// test cannot choose between.
    /// </summary>
    private void Outboard(Node root, Material plate, Material steel)
    {
        var middle = (West + East) / 2f;
        var width = East - West + 2f * Thickness;
        var z = Bore + Hull / 2f;

        // The window's own surround is a shade below the room's and a good deal smoother: a moulded
        // composite reveal rather than a panelled one, because a reveal is a single piece and panelling it
        // would be claiming a seam that is not there.
        var hull = Finish.Composite();
        hull.BaseColor = new Vector4(0.085f, 0.092f, 0.108f, 1f);
        hull.Metallic = 0.2f;
        hull.Name = "gallery.reveal";

        root.Children.Add(Fabric.Slab(
            new Vector3(width, Sill, Hull), new Vector3(middle, Sill / 2f, z), hull, "sill"));

        root.Children.Add(Fabric.Slab(
            new Vector3(width, Height - Head, Hull),
            new Vector3(middle, (Height + Head) / 2f, z), hull, "header"));

        // The returns at each end of the glazing, which are where the section of the hull is read from.
        foreach (var (from, to) in new[]
                 {
                     (GlassEast, East + Thickness),

                     // West to the outer face of the bow port's own reveal rather than to the wall,
                     // because there is no wall there any more — this piece is the corner post between
                     // the two windows and it has to be as deep as both of them.
                     (West - Prow, GlassWest)
                 })
            root.Children.Add(Fabric.Slab(
                new Vector3(to - from, Head - Sill, Hull),
                new Vector3((from + to) / 2f, (Sill + Head) / 2f, z),
                hull,
                "return"));

        // The sill shelf, and it is a shelf now rather than the counter it used to be. With the opening
        // taken down to half a metre there is nothing to lean on at that height anyway, and a third of a
        // metre of bright deck plate across the bottom of the frame was competing with a planet.
        root.Children.Add(Fabric.Slab(
            new Vector3(GlassEast - GlassWest + 0.5f, 0.06f, Ledge),
            new Vector3((GlassWest + GlassEast) / 2f, Sill - 0.03f, Bore - Ledge / 2f),
            steel,
            "shelf",
            Finish.Close));

        root.Children.Add(Fabric.Slab(
            new Vector3(GlassEast - GlassWest + 0.5f, 0.028f, 0.02f),
            new Vector3((GlassWest + GlassEast) / 2f, Sill - 0.085f, Bore - Ledge - 0.011f),
            _trim,
            "under",
            Finish.Close));

        Vault(root, steel);
        Prowport(root, plate, steel);

        // A rail at a metre, half a metre off the glass. It replaces the counter and does the counter's
        // one indispensable job: a horizontal at a known distance, so six hundred metres has something to
        // be further away than.
        root.Children.Add(new MeshNode(
            Primitives.Cylinder(0.028f, 0.028f, GlassEast - GlassWest - 0.6f, 12), steel)
        {
            Position = new Vector3((GlassWest + GlassEast) / 2f, 1.02f, Bore - 0.34f),
            RotationDegrees = new Vector3(0f, 0f, 90f),
            Name = "rail"
        });

        for (var x = GlassWest + 1.1f; x <= GlassEast; x += 3.3f)
            root.Children.Add(new MeshNode(Primitives.Cylinder(0.026f, 0.034f, 1.02f, 10), steel)
            {
                Position = new Vector3(x, 0.51f, Bore - 0.34f),
                Name = "stanchion"
            });

        // A rebate line round the top of the reveal, running the whole glazed length.
        root.Children.Add(Fabric.Slab(
            new Vector3(GlassEast - GlassWest, 0.028f, 0.02f),
            new Vector3((GlassWest + GlassEast) / 2f, Head - 0.055f, Bore - 0.012f),
            _trim,
            "rebate",
            Finish.Close));

        Glazing(root);
    }

    /// <summary>
    /// The vault: four tapered ribs standing in front of the glass and fanning outward as they rise, each
    /// carrying a line of warm light up its inboard face and continuing across the ceiling as a beam.
    ///
    /// They are what is left of the mullions, and the difference between the two is the whole look of the
    /// room. A mullion is a quarter of a metre of hull with a hole either side of it, which the eye reads
    /// as <i>wall</i> — so a window with three of them in it is four windows. A rib is a strut in front of
    /// one continuous pane, which the eye reads as <i>structure</i>, and a window with four of them in it
    /// is still one window with a frame around it.
    ///
    /// <b>They taper and they lean, and both are doing work.</b> A parallel post is a post; a strut two
    /// hundred millimetres at the foot and seventy at the head is a thing carrying a load, which is what
    /// says the glass it stands in front of is holding vacuum out. And the lean is proportional to how far
    /// the rib is from the middle of the run, so the four of them splay like the ribs of a vault — which
    /// turns a rectangular hole with posts in it into an arcade, and is the single change that stops this
    /// reading as a corridor with a big window and starts it reading as somewhere people sit.
    ///
    /// <see cref="Primitives.Cylinder"/> does the taper for nothing: it takes a radius at each end. Eight
    /// segments rather than a smooth twenty, because a faceted strut catches the star along one facet and
    /// a round one catches it along a highlight, and the facet is what reads as moulded composite.
    /// </summary>
    private void Vault(Node root, Material steel)
    {
        const int bays = 5;
        const float lean = 9f;

        var span = (Sill + Head) / 2f;
        var middle = (GlassWest + GlassEast) / 2f;
        var half = (GlassEast - GlassWest) / 2f;

        // Both ends included, which they were not for the first four rounds of this room and should have
        // been from the start. A run of window terminated by nothing is a run of window that has been cut
        // off by the edge of the frame; the end ribs are what say the glazing <i>stops</i> there. They are
        // also where the lean is at its greatest, so the two most visible ribs in the room are the two
        // doing the most to say the wall is not flat.
        for (var i = 0; i <= bays; i++)
        {
            var x = GlassEast + (GlassWest - GlassEast) * (i / (float)bays);
            var tilt = lean * ((x - middle) / half);

            var rib = new Node
            {
                Name = "rib",
                Position = new Vector3(x, span, Bore + Bedded - 0.11f),
                RotationDegrees = new Vector3(0f, 0f, tilt)
            };

            rib.Children.Add(new MeshNode(
                Primitives.Cylinder(0.07f, 0.20f, Head - Sill, 8), steel)
            {
                Name = "strut"
            });

            // The line of light up the inboard face. It is set into the strut rather than laid on it, so
            // the strut occludes it from the sides and it reads as a channel rather than as a stripe.
            rib.Children.Add(Fabric.Slab(
                new Vector3(0.045f, Head - Sill - 0.5f, 0.02f),
                new Vector3(0f, 0f, -0.075f),
                _trim,
                "channel",
                Finish.Close));

            root.Children.Add(rib);
        }
    }

    /// <summary>
    /// The glass: one quad down the side of the ship and one across the front of the bay.
    ///
    /// Almost nothing — four and a half percent of a cold grey — because at this brightness it is not a
    /// pane you can see but a pane you can tell is there, and the difference between those two is the
    /// difference between a window and a hole in the wall.
    ///
    /// It draws after everything outside it, which is what an alpha surface has to do:
    /// <c>SceneSnapshot</c> sorts by render order and then by the order things went into the tree, never
    /// by distance, so back-to-front is the builder's job and not the renderer's.
    ///
    /// One material between the two, and it is two-sided. A pane is glass on both faces, and the corner
    /// where these two meet is a place a camera can end up looking at the back of one of them.
    /// </summary>
    private static void Glazing(Node root)
    {
        var glass = new Material
        {
            BaseColor = new Vector4(0.46f, 0.58f, 0.72f, 0.045f),
            Unlit = true,
            Blend = BlendMode.Alpha,
            DepthWrite = false,
            Cull = CullMode.None,
            Name = "gallery.glass"
        };

        root.Children.Add(new MeshNode(
            Primitives.Plane(GlassEast - GlassWest, Head - Sill), glass)
        {
            Position = new Vector3((GlassWest + GlassEast) / 2f, (Sill + Head) / 2f, Bore + Bedded),
            RotationDegrees = new Vector3(90f, 0f, 0f),
            RenderOrder = 2,
            Name = "pane"
        });

        // The bow pane, turned about Z rather than about two axes. A plane arrives lying in XZ, so one
        // quarter turn about Z stands it up facing along X — which is the whole of what this window needs
        // and is one rotation whose order nothing can get wrong.
        root.Children.Add(new MeshNode(
            Primitives.Plane(Head - Sill, Bore - PortIn), glass)
        {
            Position = new Vector3(West - PortBed, (Sill + Head) / 2f, (PortIn + Bore) / 2f),
            RotationDegrees = new Vector3(0f, 0f, 90f),
            RenderOrder = 2,
            Name = "pane.prow"
        });
    }

    /// <summary>
    /// The deck lighting: a strip along the base of the inboard wall and a run of markers under the ledge.
    ///
    /// Every one of them is additive and unlit, so the whole of what makes this gallery walkable costs
    /// nothing out of the one slot the room has. It is the antechamber's argument, made for the last time
    /// and with the most at stake: the room is lit by a star and by nothing else, and it is still a room
    /// you can see the floor of.
    /// </summary>
    private void Strips(Node root)
    {
        var run = East - West - 0.6f;

        root.Children.Add(Fabric.Slab(
            new Vector3(run, 0.035f, 0.02f),
            new Vector3((West + East) / 2f, 0.11f, -0.02f),
            _deck,
            "strip",
            Finish.Close));

        for (var x = GlassWest + 0.6f; x <= GlassEast; x += 2.4f)
            root.Children.Add(Fabric.Slab(
                new Vector3(0.9f, 0.028f, 0.02f),
                new Vector3(x, 0.09f, Bore - 0.95f),
                _deck,
                "marker",
                Finish.Close));
    }

    /// <summary>
    /// The ceiling, coffered: a downstand grid of six beams across and two along, with a lamp recessed in
    /// every cell of the middle band.
    ///
    /// Thirteen metres of unbroken ceiling reads as about six, and six ribs going away in perspective reads
    /// as exactly what it is. The corridor made that point with the same object, which is deliberate — this
    /// is the same ship, two rooms later, and the frames of a hull do not change their spacing because the
    /// room they are in got nicer. What is new here is the two beams running <i>along</i>, and they are the
    /// whole difference between a ribbed ceiling and a coffered one: ribs are a rhythm, a grid is a
    /// <i>surface with depth in it</i>, and depth overhead is most of what makes the reference rooms read
    /// as built rather than boxed.
    ///
    /// <b>The coffers are downstands, not recesses, and that is not a compromise.</b> There is no way to cut
    /// a hole in a plane here, so a sunk panel would have to be a second lid drawn above the first one, and
    /// two lids at two heights is exactly the picture a downstand grid gives — read from below, a beam
    /// hanging a hundred and ninety millimetres under a lid and a lid sitting a hundred and ninety
    /// millimetres over a beam are the same object. Building it the way that needs no cut also puts the
    /// lamps where a lamp goes: up in the cell, above the soffit line, so the beam nearest the camera hides
    /// the source and the eye gets the light without the tube.
    /// </summary>
    private void Coffers(Node root, Material plate)
    {
        var width = East - West;
        var middle = (West + East) / 2f;
        var deep = Bore - Apron;
        var at = (Apron + Bore) / 2f;

        // How far the grid hangs, and where the two long beams sit. The band between them is a metre thirty
        // wide and it is directly over the walk — which is not a coincidence and is the reason the lamps go
        // in that band and nowhere else. Light belongs over the part of the floor people are on.
        const float Drop = 0.19f;
        const float Beam = 0.17f;
        const float Inner = 1.45f;
        const float Outer = 2.75f;

        for (var x = West + 1.3f; x < East; x += 2.6f)
        {
            root.Children.Add(Fabric.Slab(
                new Vector3(Beam, Drop, deep),
                new Vector3(x, Height - Drop / 2f, at),
                plate,
                "rib",
                Finish.Close));

            // A line of amber down the side of every beam. It is the one piece of the old ribbed ceiling
            // that survived the rebuild, and it survives because a grid of bare soffits is architecture
            // with the lights off.
            root.Children.Add(Fabric.Slab(
                new Vector3(0.022f, 0.02f, deep - 0.2f),
                new Vector3(x + Beam / 2f + 0.011f, Height - Drop + 0.05f, at),
                _cove,
                "rib.cove",
                Finish.Close));
        }

        foreach (var z in new[] { Inner, Outer })
        {
            root.Children.Add(Fabric.Slab(
                new Vector3(width, Drop, Beam),
                new Vector3(middle, Height - Drop / 2f, z),
                plate,
                "beam",
                Finish.Close));

            // And along the inner face of each, pointing into the lit band, so the two beams that hold the
            // lamps are also the two that are edged in light.
            var inward = z < middle ? 1f : -1f;

            root.Children.Add(Fabric.Slab(
                new Vector3(width - 0.4f, 0.02f, 0.022f),
                new Vector3(middle, Height - Drop + 0.05f, z + inward * (Beam / 2f + 0.011f)),
                _cove,
                "beam.cove",
                Finish.Close));
        }

        // The lamps. One per cell, five cells, each one two metres three by ninety centimetres, sitting up
        // against the lid so the soffits stand a hundred and forty millimetres proud of it.
        //
        // This is the largest lit area in the film and by some way the cheapest: five additive quads, no
        // slot, no shadow map, no cost that scales with anything. See Trim for the value they run at and
        // why it is a twentieth of what a strip runs at.
        for (var x = West + 2.6f; x < East - 1f; x += 2.6f)
        {
            // Thirty millimetres clear of the lid rather than flush with it. Flush meant the panel's top
            // face and the ceiling were the same plane, and two coplanar faces are the one case the depth
            // test has nothing to choose between — which is what the mottling across the first pass was.
            root.Children.Add(Fabric.Slab(
                new Vector3(2.3f, 0.04f, Outer - Inner - 0.34f),
                new Vector3(x, Height - 0.05f, (Inner + Outer) / 2f),
                _lamp,
                "lamp",
                Finish.Close));

            // A brighter seam down the middle of each panel, which is the thing that stops a lit rectangle
            // reading as a hole. A luminaire has a lamp in it and a diffuser round the lamp; two values
            // across the same panel is the cheapest way to say so.
            root.Children.Add(Fabric.Slab(
                new Vector3(2.24f, 0.03f, 0.12f),
                new Vector3(x, Height - 0.085f, (Inner + Outer) / 2f),
                _lamp,
                "lamp.tube",
                Finish.Close));

            // Four blades across each panel, hanging below the tube.
            //
            // These are the largest bright areas anywhere in the film and without them they are also the
            // flattest: five white rectangles overhead with a line down each, which reads as paper rather
            // than as a fitting. A louvre is what a recessed luminaire has in it and what it does is
            // give the panel <i>parallax</i> — the blades slide across the light as the camera moves down
            // the room, and a bright surface that changes as you walk past it is one the eye stops
            // reading as a texture.
            foreach (var over in new[] { -0.85f, -0.28f, 0.28f, 0.85f })
                root.Children.Add(Fabric.Slab(
                    new Vector3(0.035f, 0.05f, Outer - Inner - 0.34f),
                    new Vector3(x + over, Height - 0.115f, (Inner + Outer) / 2f),
                    plate,
                    "lamp.louvre",
                    Finish.Close));
        }

        // Two continuous coves where the ceiling meets each side, which is what turns a flat lid into a
        // ceiling: the eye reads the line, not the plane.
        foreach (var z in new[] { Apron + 0.06f, Bore - 0.06f })
            root.Children.Add(Fabric.Slab(
                new Vector3(width - 0.4f, 0.024f, 0.02f),
                new Vector3(middle, Height - 0.10f, z),
                _cove,
                "cove",
                Finish.Close));
    }

    /// <summary>
    /// The inboard wall, which was the one thing wrong with this room for as long as it was empty.
    ///
    /// A gallery has two sides. Thirteen metres of glass on one of them and thirteen metres of unlit plate
    /// on the other is a frame that is half black wherever the camera is not pointed straight out, and the
    /// walk points straight out about a third of the time. So this side gets what the outboard side has:
    /// a dado with a lit line over it, and three charts.
    ///
    /// The charts are <see cref="LineNode"/>s, which is the right primitive for the job and not an obvious
    /// one. A plot drawn as geometry would be a hundred slabs a millimetre thick; a plot drawn as a texture
    /// goes soft the moment the camera is at an angle to it, which here it always is. Lines stay one pixel
    /// wide from any angle and at any range, because they are not surfaces.
    /// </summary>
    private void Inboard(Node root, Material plate, Material steel)
    {
        var width = East - West;
        var middle = (West + East) / 2f;

        // What a chart and a rack are both set into. One material rather than one per panel, which is
        // worth saying only because the first version made a new one inside the loop and therefore made
        // three identical materials that could never batch.
        var recess = new Material
        {
            BaseColor = new Vector4(0.035f, 0.040f, 0.052f, 1f),
            Metallic = 0.1f,
            Roughness = 0.35f,
            Name = "chart.face"
        };

        // A dado standing proud of the wall, with a line of light along its top edge.
        //
        // <b>It stops at the door's west jamb, and it used to run the whole wall — straight across the
        // opening.</b> A waist-high plate over the one gap in the building a person walks through, in the
        // room the film ends in. It never showed, because the camera comes through at eye height and clears
        // it by seven hundred millimetres; what found it was the capsule audit, which asks where a body is
        // rather than where a lens is. See <c>Ground.Clearance</c>.
        //
        // Ending at the jamb rather than resuming past it is not only the cheaper fix. The opening is a
        // metre and a fifth wide and its east side is half a metre from the end of the room, so the far
        // stub would be fifty millimetres of rail on its own, which reads as breakage rather than as
        // joinery. A dado that runs into a doorway and stops is what a real one does.
        var jamb = Doorway - Deck.DoorWidth / 2f - 0.05f;
        var inset = West + 0.25f;
        var run = jamb - inset;
        var mid = (inset + jamb) / 2f;

        root.Children.Add(Fabric.Slab(
            new Vector3(run, 1.02f, 0.08f),
            new Vector3(mid, 0.51f, 0.04f),
            plate,
            "dado",
            Finish.Close));

        root.Children.Add(Fabric.Slab(
            new Vector3(run, 0.022f, 0.02f),
            new Vector3(mid, 1.035f, 0.075f),
            _cove,
            "dado.edge",
            Finish.Close));

        // Conduit at shoulder height, because a wall on a ship with nothing running along it is a rendering
        // of a wall.
        foreach (var y in new[] { 2.42f, 2.58f })
            root.Children.Add(Fabric.Slab(
                new Vector3(width - 0.8f, 0.06f, 0.06f),
                new Vector3(middle, y, 0.04f),
                plate,
                "conduit",
                Finish.Close));

        for (var i = 0; i < _chart.Length; i++)
        {
            var x = 3.6f - i * 4.2f;

            // The panel the chart is drawn on: a recess, so the plot reads as being in something.
            root.Children.Add(Fabric.Slab(
                new Vector3(1.9f, 1.05f, 0.05f),
                new Vector3(x, 1.86f, 0.03f),
                recess,
                "chart",
                Finish.Close));

            root.Children.Add(Fabric.Slab(
                new Vector3(1.94f, 0.016f, 0.02f),
                new Vector3(x, 1.86f - 0.53f, 0.062f),
                _trim,
                "chart.rule",
                Finish.Close));

            _chart[i] = new LineNode
            {
                Positions = Plotted(i),
                Color = new Vector3(0.42f, 0.82f, 1f),
                Opacity = 0.5f,
                Width = 1.3f,
                Blend = BlendMode.Additive,
                DepthWrite = false,
                Position = new Vector3(x, 1.86f, 0.058f),
                Name = "chart.plot"
            };

            root.Children.Add(_chart[i]);
        }

        Racks(root, recess, steel);
    }

    /// <summary>
    /// Two equipment banks on the inboard wall, in the gaps the charts leave: a recess with two tiers of
    /// instrument in it and a lit rule underneath.
    ///
    /// <b>This is where the second screen tier had to go, and it is not where it was asked for.</b> The
    /// reference rooms all stack their displays — a working surface, then a bank above it at eye height —
    /// and doing that over the console here would put a wall of lit glass across the one thing this room
    /// exists to look at. So the tier goes on the wall behind the camera's shoulder instead, where it is
    /// in shot every time the walk turns inboard and in nobody's way when it turns out. A gallery is a
    /// room with a bright side and a dark side; the job was never to make both sides bright, it was to
    /// stop the dark one being empty.
    ///
    /// Each tier is one slab with the readout tiled across it rather than four slabs with one each. At
    /// <see cref="Finish.Snug"/> a metre and a half of panel gets seven copies of the image, and seven
    /// copies of a graticule with a dial in it is what a rack of identical instruments looks like — which
    /// is what a rack of identical instruments is.
    /// </summary>
    private void Racks(Node root, Material recess, Material steel)
    {
        // Between the charts, which sit at 3.6, −0.6 and −4.8 and are one metre nine wide. What is left
        // between them is two gaps of two metres three, and a bank one metre nine in each of them leaves
        // the same two hundred millimetres of wall either side that the charts have.
        var banks = new[] { 1.5f, -2.7f };
        var tiers = new[] { 1.50f, 2.10f };

        for (var bank = 0; bank < banks.Length; bank++)
        {
            var x = banks[bank];

            root.Children.Add(Fabric.Slab(
                new Vector3(1.9f, 1.18f, 0.05f),
                new Vector3(x, 1.83f, 0.03f),
                recess,
                "rack",
                Finish.Close));

            for (var tier = 0; tier < tiers.Length; tier++)
            {
                root.Children.Add(Fabric.Slab(
                    new Vector3(1.74f, 0.44f, 0.03f),
                    new Vector3(x, tiers[tier], 0.055f),
                    steel,
                    "rack.bezel",
                    Finish.Close));

                // Three instruments to a tier, in one bezel, with the steel showing between them. It was
                // one slab with the layout tiled across it, on the argument that seven copies of a
                // graticule is what a rack of identical instruments looks like — and a rack of identical
                // instruments is the thing no equipment bay has ever been. Every one of these carries its
                // own layout now and is fitted to it corner to corner, which costs eight more slabs on a
                // wall that already has hundreds and is the difference between a bay and a wallpaper.
                for (var slot = 0; slot < 3; slot++)
                    Screen(
                        root,
                        (bank * tiers.Length + tier) * 3 + slot,
                        new Vector3(0.50f, 0.263f, 0.02f),
                        new Vector3(x + (slot - 1) * 0.54f, tiers[tier], 0.068f),
                        1f,
                        "rack.screen");
            }

            // The rule under the bank, at exactly the height of the one under each chart, so the whole
            // inboard wall has a single lit line running its length whatever is hanging above it.
            //
            // Exactly, and it was twenty millimetres off for one round. Two additive lines that nearly
            // coincide are not two lines: from anywhere along the room they project onto each other and
            // sum, and what a warm line at twice its value looks like is a white one. Half the length of
            // that wall came back as a fluorescent tube.
            root.Children.Add(Fabric.Slab(
                new Vector3(1.94f, 0.016f, 0.02f),
                new Vector3(x, 1.86f - 0.53f, 0.062f),
                _trim,
                "rack.rule",
                Finish.Close));
        }
    }

    /// <summary>
    /// One screen: the glass with a layout of its own on it, a wipe that crosses it, and an indicator
    /// standing in the corner of it.
    ///
    /// <b>A screen that does not change is a poster of a screen</b>, and twenty-one of them is a room where
    /// nothing is happening in the one place the film says people work. Neither of the two things that move
    /// here is drawn: the layout underneath is a fixed image, because a texture that has to be redrawn is a
    /// texture that has to be uploaded again every frame, and this renderer would be doing twenty-one of
    /// those. What moves instead is geometry — one slab that slides and one that changes colour — which is
    /// two writes a frame each and reads, at the distance any of these is ever seen, as an instrument doing
    /// something. Everything the eye reports as <i>live</i> about a control room is motion and blink rate;
    /// none of it is the numbers, which nobody can read anyway.
    ///
    /// The rates are hashed off the panel so no two of them are ever in step. Twenty-one wipes crossing
    /// together is not a bank of instruments, it is a wave — the single clearest way to say that one hand
    /// is driving all of them.
    /// </summary>
    /// <param name="facing">Which way is out of the glass: −1 for the console, whose front is on the low
    /// side of the room, +1 for the racks on the inboard wall, where it is the other way about.</param>
    private void Screen(Node root, int cell, Vector3 size, Vector3 centre, float facing, string name)
    {
        var (origin, span) = Finish.Readout(cell);

        root.Children.Add(Fabric.Panel(size, centre, _screen, name, origin, span, facing));

        var (left, right) = Finish.Reading(cell);

        Moving(root, left, cell * 2, size, centre, facing, false);
        Moving(root, right, cell * 2 + 1, size, centre, facing, true);

        // The indicator, in the notch the sheet leaves clear for it — see Finish.Panel. Warm, because it is
        // a marking rather than a work surface, which is the rule the whole room is lit by; and its own
        // material, because the entire point of it is that it is not doing what its neighbours are doing.
        var pip = Glow(1f, 0.66f, 0.22f);
        pip.Name = "screen.pip";

        Paint(pip, Indicator, 0f);

        root.Children.Add(Fabric.Slab(
            new Vector3(size.X * 0.05f, size.Y * 0.09f, 0.004f),
            new Vector3(
                centre.X - facing * size.X * 0.445f,
                centre.Y - size.Y * 0.40f,
                centre.Z + facing * 0.012f),
            pip,
            "screen.pip"));

        _pips.Add(new Lamp(
            pip,
            0.35f + 1.1f * Grain.Pick(cell, _pips.Count, 83),
            Grain.Pick(cell, _pips.Count, 19),
            0.16f + 0.5f * Grain.Pick(cell, _pips.Count, 101)));
    }

    /// <summary>
    /// The one thing that moves on half a screen, cut and placed to the instrument it is standing on.
    ///
    /// Everything here is in the panel's own coordinates first and metres second, which is what keeps a
    /// needle at the middle of its dial on two differently sized screens. <see cref="Finish.Half"/> owns the
    /// rectangle; this reads it, and so does the function that drew the instrument inside it.
    ///
    /// <b>Sixteen millimetres off the glass</b>, and the number is not decoration. Every one of these is an
    /// additive surface standing in front of another additive surface, and two additive quads that nearly
    /// coincide are the family this film has already paid for once. The glass is twenty millimetres deep,
    /// so anything nearer than eleven is inside it.
    /// </summary>
    private void Moving(Node root, Finish.Gauge gauge, int seed, Vector3 size, Vector3 centre, float facing,
        bool right)
    {
        var (middle, extent) = Finish.Half(right);

        // Panel coordinates into metres. u runs the way the viewer's right runs, which is +X on one side of
        // a surface and −X on the other — the same mirror Fabric.Panel exists to deal with.
        var wide = extent.X * size.X;
        var tall = extent.Y * size.Y;

        var seat = new Vector3(
            centre.X + facing * (middle.X - 0.5f) * size.X,
            centre.Y + (0.5f - middle.Y) * size.Y,
            centre.Z + facing * 0.016f);

        var rate = 0.09f + 0.12f * Grain.Pick(seed, (int)gauge, 37);
        var phase = Grain.Pick(seed, 5, 19);

        switch (gauge)
        {
            case Finish.Gauge.Dial:
            {
                // The needle. A slab rotates about its own middle, so it hangs off a pivot node at the hub
                // and is pushed out by half its length — which is the whole of what makes it a needle
                // rather than a bar see-sawing across the dial.
                var reach = MathF.Min(wide, tall) * Finish.Rim * 0.68f;

                var pivot = new Node { Position = seat, Name = "screen.dial" };

                pivot.Children.Add(new MeshNode(Primitives.Box(reach, tall * 0.028f, 0.004f), _wipe)
                {
                    Position = new Vector3(reach / 2f, 0f, 0f),
                    Name = "screen.needle"
                });

                root.Children.Add(pivot);

                // The scale is drawn from a hundred and forty degrees round to thirty short of nothing, and
                // it is that way about on one side of the glass and the other way about on the other. The
                // needle has to agree with the face it is standing on, so the sweep is mirrored with it.
                _live.Add(new Live
                {
                    Kind = Motion.Needle,
                    Part = pivot,
                    Rate = rate * 0.55f,
                    Phase = phase,
                    From = facing > 0f ? 140f : 40f,
                    Span = facing > 0f ? -170f : 170f
                });

                break;
            }

            case Finish.Gauge.Trace:
            {
                var wipe = Fabric.Slab(
                    new Vector3(0.008f, tall * 0.94f, 0.004f), seat, _wipe, "screen.wipe");

                root.Children.Add(wipe);

                _live.Add(new Live
                {
                    Kind = Motion.Wipe,
                    Part = wipe,
                    Home = seat,
                    Travel = new Vector3(wide * 0.5f, 0f, 0f),
                    Rate = rate,
                    Phase = phase
                });

                break;
            }

            case Finish.Gauge.Bars:
            {
                var rule = Fabric.Slab(
                    new Vector3(wide * 0.96f, 0.005f, 0.004f), seat, _wipe, "screen.rule");

                root.Children.Add(rule);

                _live.Add(new Live
                {
                    Kind = Motion.Marker,
                    Part = rule,
                    Home = seat,
                    Travel = new Vector3(0f, tall * 0.44f, 0f),
                    Rate = rate * 0.7f,
                    Phase = phase
                });

                break;
            }

            case Finish.Gauge.Ladder:
            {
                // One column of the five, and the same one every frame: a cap that wandered between columns
                // would be a level that had changed tanks.
                var column = (int)(Grain.Pick(seed, 7, 53) * 5f) % 5;

                var cap = Fabric.Slab(
                    new Vector3(wide * 0.12f, 0.006f, 0.004f),
                    seat + new Vector3(facing * (column - 2f) * wide / 5f, 0f, 0f),
                    _wipe,
                    "screen.cap");

                root.Children.Add(cap);

                _live.Add(new Live
                {
                    Kind = Motion.Climb,
                    Part = cap,
                    Home = cap.Position,
                    Travel = new Vector3(0f, tall * 0.45f, 0f),
                    Rate = rate * 0.5f,
                    Phase = phase
                });

                break;
            }

            case Finish.Gauge.Rows:
            {
                var bar = Fabric.Slab(
                    new Vector3(wide * 0.62f, tall / 6f * 0.5f, 0.004f),
                    seat + new Vector3(facing * -wide * 0.17f, 0f, 0f),
                    _wipe,
                    "screen.row");

                root.Children.Add(bar);

                _live.Add(new Live
                {
                    Kind = Motion.Step,
                    Part = bar,
                    Home = bar.Position,
                    Travel = new Vector3(0f, tall * 0.42f, 0f),
                    Rate = rate * 0.8f,
                    Phase = phase
                });

                break;
            }

            default:
            {
                // A status board has nothing on it that moves, so what changes is which block is lit. The
                // one that is off is not dimmed but gone: the sheet has already drawn the block, and a
                // board where the same square is always the bright one is a board with a stuck lamp.
                var across = (int)(Grain.Pick(seed, 9, 71) * 4f) % 4;
                var down = (int)(Grain.Pick(seed, 11, 73) * 3f) % 3;

                var block = Fabric.Slab(
                    new Vector3(wide / 4f * 0.62f, tall / 3f * 0.42f, 0.004f),
                    seat + new Vector3(
                        facing * (across - 1.5f) * wide / 4f, -(down - 1f) * tall / 3f, 0f),
                    _wipe,
                    "screen.block");

                root.Children.Add(block);

                _live.Add(new Live
                {
                    Kind = Motion.Flash,
                    Part = block,
                    Home = block.Position,
                    Rate = rate * 0.9f,
                    Phase = phase,
                    Duty = 0.28f + 0.34f * Grain.Pick(seed, 13, 89)
                });

                break;
            }
        }
    }

    /// <summary>
    /// One chart's worth of line segments: an ellipse, a hull track across it, and a scale down the side.
    ///
    /// It is arithmetic rather than an asset because it has to be — three panels that were the same image
    /// would read as three copies of a poster, and three that were three images would be three textures for
    /// something the eye spends four seconds on. Varying the eccentricity and the phase off the index is
    /// enough that no two are alike and none of them had to be drawn.
    /// </summary>
    private static Vector3[] Plotted(int index)
    {
        var lines = new List<Vector3>();

        const int arc = 64;
        var wide = 0.62f + index * 0.07f;
        var tall = 0.30f - index * 0.05f;
        var lean = (index - 1) * 0.22f;

        Vector3 On(float t) => new(
            MathF.Cos(t) * wide,
            MathF.Sin(t) * tall + MathF.Cos(t) * lean,
            0f);

        for (var i = 0; i < arc; i++)
        {
            lines.Add(On(i / (float)arc * MathF.Tau));
            lines.Add(On((i + 1) / (float)arc * MathF.Tau));
        }

        // The track: a shallower path across the ellipse, which is what a course laid over an orbit is.
        //
        // <b>It has to end inside the panel it is drawn on, and for a long time it did not.</b> The sweep
        // ran from −0.9 to +1.7 and the x it produces is that times 0.78, so the right-hand end came out at
        // 1.33 — on a recess 1.9 wide, whose own half-width is 0.95. Four tenths of a metre of blue line
        // carried on past the edge of the chart and off across the plaster, over the conduit and behind the
        // screens, and because the line writes no depth there was nothing anywhere to stop it. It reads as
        // the diagram having escaped, which is exactly what it was.
        //
        // Symmetric now, and the number is derived rather than chosen: 0.9 each way times 0.78 is 0.70,
        // which leaves a quarter of a metre of margin inside the recess at both ends.
        for (var i = 0; i < 30; i++)
        {
            var a = -0.9f + i / 30f * 1.8f;
            var b = -0.9f + (i + 1) / 30f * 1.8f;

            lines.Add(new Vector3(a * 0.78f, MathF.Sin(a * 1.4f + index) * 0.16f, 0f));
            lines.Add(new Vector3(b * 0.78f, MathF.Sin(b * 1.4f + index) * 0.16f, 0f));
        }

        // A scale down the left-hand side and four ticks along the bottom, which is what stops a diagram
        // reading as a doodle.
        for (var i = 0; i <= 8; i++)
        {
            var y = -0.42f + i * 0.105f;
            var tick = i % 2 == 0 ? 0.07f : 0.035f;

            lines.Add(new Vector3(-0.9f, y, 0f));
            lines.Add(new Vector3(-0.9f + tick, y, 0f));
        }

        for (var i = 0; i <= 6; i++)
        {
            var x = -0.86f + i * 0.29f;

            lines.Add(new Vector3(x, -0.46f, 0f));
            lines.Add(new Vector3(x, -0.46f + (i % 2 == 0 ? 0.06f : 0.03f), 0f));
        }

        return [.. lines];
    }

    /// <summary>
    /// The console run: a desk the length of the glass, with screens set into it and a line of warm light
    /// under its lip.
    ///
    /// It is what the room is <i>for</i>, and it is the thing four chairs were standing in for. Somebody
    /// stands here for eight hours at a time watching a planet go past and a station keep station, and a
    /// bank of consoles under a window says that in a way no amount of seating does. It is also the one
    /// piece of furniture in this building that is a *run* rather than an object: it goes the whole length
    /// of the room, so wherever the walk stops there is a working surface in the bottom of the frame.
    ///
    /// <b>The light under the lip is the whole trick.</b> A strip along the front of a desk, six
    /// centimetres above a dark polished floor, is what every picture of a control room has in it — and
    /// the reason is not the strip. It is the smear the strip leaves on the deck, which reads as a
    /// reflection and is really a second quad lying flat. See <see cref="Sheen"/>.
    ///
    /// <b>Five bays, nine screens, eight keyboards.</b> Those three numbers do not divide into each other
    /// and that is deliberate. The uprights are the structure and are on their own rhythm; the
    /// instruments sit where an instrument goes, which is between the uprights and not aligned to them.
    /// A console whose every division lines up is a rendering of a console — the references all have a
    /// desk built to one grid with equipment fitted to another, because the desk was made in a factory
    /// and the equipment was fitted on board.
    /// </summary>
    private void Consoles(Node root, Material plate, Material steel)
    {
        const float top = 0.86f;
        const float deep = 0.72f;

        var z = Bore - 1.15f;

        // It stops two metres short of the forward end, and that gap is the bow bay: the one place in
        // this room where somebody can walk right up to the glass instead of standing behind a desk. A
        // continuous run of console is what an observation gallery has along its length and is exactly
        // what it must not have in the corner the ship is pointed at — the film ends standing in this gap.
        var west = GlassWest + 1.5f;
        var east = GlassEast + 0.7f;

        var run = east - west;
        var middle = (west + east) / 2f;

        // The inboard face of the desk, which is the one the camera is on the wrong side of all film and
        // is therefore the one every piece of this run is measured off.
        var face = z - deep / 2f;

        // The station pitch: nine of them across the run with a metre and a bit spare at each end.
        var pitch = (run - 2.4f) / 8f;

        root.Children.Add(Fabric.Slab(
            new Vector3(run, top - 0.12f, deep),
            new Vector3(middle, (top - 0.12f) / 2f + 0.12f, z),
            plate,
            "console",
            Finish.Close));

        root.Children.Add(Fabric.Slab(
            new Vector3(run + 0.06f, 0.05f, deep + 0.08f),
            new Vector3(middle, top, z),
            steel,
            "console.top",
            Finish.Close));

        // The kick light, and the recess it sits in. The recess is what stops it reading as a strip stuck
        // on the front: the light is behind the line of the desk, so what you see is the glow and not the
        // fitting, which is the difference between a lit desk and a desk with a tube taped to it.
        root.Children.Add(Fabric.Slab(
            new Vector3(run, 0.055f, 0.02f),
            new Vector3(middle, 0.155f, face + 0.03f),
            _trim,
            "console.kick",
            Finish.Close));

        // The bays: an upright every two and a half metres down the front of the desk, standing forty
        // millimetres proud of it.
        //
        // Thirteen metres of unbroken furniture is a plinth, and a plinth is what this was. Six uprights
        // make it five working positions, and five is a number the eye takes without counting — the same
        // argument the ceiling makes with six ribs two rooms running, made at knee height where a walking
        // camera can see it change.
        for (var i = 0; i < 6; i++)
            root.Children.Add(Fabric.Slab(
                new Vector3(0.07f, top - 0.20f, 0.05f),
                new Vector3(west + 0.35f + i * (run - 0.7f) / 5f, (top + 0.02f) / 2f + 0.08f, face - 0.015f),
                steel,
                "console.upright",
                Finish.Close));

        // A lit line along the back edge of the worktop, where it meets the window.
        //
        // The kick light under the front lip is the one every photograph has; this is the other one, and
        // it is the reason the references read as furnished rather than as lit from below. A desk with a
        // line under it is a desk with an effect on it. A desk with a line under it *and* a line along
        // its far edge has a top — the two of them together are what give the worktop a thickness the eye
        // can find in a dark room.
        root.Children.Add(Fabric.Slab(
            new Vector3(run - 0.4f, 0.016f, 0.02f),
            new Vector3(middle, top + 0.035f, z + deep / 2f + 0.025f),
            _trim,
            "console.edge",
            Finish.Close));

        Instruments(root, steel, top, face, middle, pitch);
        Seats(root, steel, west, run);

        for (var i = 0; i < 9; i++)
        {
            var x = middle + (i - 4) * pitch;

            // A panel standing at the back of the desk, which is where a panel you read while you are on
            // your feet has to be. The first version laid them flat into the worktop at the angle a
            // sit-down console uses, and nine glowing rectangles lying face up read as nine trays.
            // On the inboard edge with their backs to the glass, and small. The first version stood them at
            // the far edge of the desk facing the room, which put nine bright rectangles between the
            // viewer and the only thing this room exists to look at — the single worst thing a console can
            // do in an observation gallery, and it took one frame to see it.
            root.Children.Add(Fabric.Slab(
                new Vector3(0.62f, 0.36f, 0.03f),
                new Vector3(x, top + 0.2f, face + 0.09f),
                steel,
                "screen.bezel",
                Finish.Close));

            // Nine screens, nine layouts, and the walk goes past all of them at arm's length. This is the
            // one run in the building where a repeat would be read: a person walking a thirteen-metre desk
            // sees eight of these in sequence, and eight of anything identical is a pattern rather than a
            // room. See Finish.Readouts.
            Screen(
                root,
                i,
                new Vector3(0.55f, 0.29f, 0.02f),
                new Vector3(x, top + 0.21f, face + 0.075f),
                -1f,
                "screen");

            // A warm tick up each side of every screen, and they are warm on purpose. The screens are the
            // only cold thing at this height in the room and nine cold rectangles in a row is a wall of
            // television; a pair of amber marks either side of each of them is what turns the run back
            // into instruments — it is the same warm-marking-cold-work rule the ceiling lamps follow, at
            // the other end of the room's height.
            foreach (var over in new[] { -0.355f, 0.355f })
                root.Children.Add(Fabric.Slab(
                    new Vector3(0.028f, 0.17f, 0.02f),
                    new Vector3(x + over, top + 0.21f, face + 0.072f),
                    _cove,
                    "screen.lamp",
                    Finish.Close));

            foreach (var over in new[] { -0.3f, 0.3f })
                root.Children.Add(Fabric.Slab(
                    new Vector3(0.22f, 0.012f, 0.09f),
                    new Vector3(x + over, top + 0.035f, z + 0.06f),
                    _trim,
                    "readout",
                    Finish.Snug));
        }
    }

    /// <summary>
    /// What is between the screens: eight raked key panels and sixteen knobs, on the worktop where a pair
    /// of hands would be.
    ///
    /// They go in the gaps rather than in front of the screens, which is the only place on this desk they
    /// can go. The operator stands inboard with his back to the glass — see <see cref="Consoles"/> — so
    /// anything laid on the outboard half of the worktop is behind a screen from every seat in the film,
    /// and anything standing tall enough to be seen over one is in front of a planet. The gap between two
    /// screens is the one part of a console in an observation gallery that can carry detail for nothing.
    ///
    /// <b>The keys are a texture and the knobs are geometry</b>, and the line between the two is worth
    /// stating because it is not the obvious one. A key is flat, repeats, and is read as a field: a map
    /// does that better than eighty boxes and costs one draw. A knob is not flat — the whole of what says
    /// <i>knob</i> is that it stands off the surface and catches the light down one side — so a map of a
    /// knob is a circle, and a circle painted on a desk is a coaster.
    /// </summary>
    private void Instruments(Node root, Material steel, float top, float face, float middle, float pitch)
    {
        for (var i = 0; i < 8; i++)
        {
            var x = middle + (i - 3.5f) * pitch;
            var at = new Vector3(x, top + 0.072f, face + 0.20f);

            // Raked a quarter turn short of flat, which is the angle a panel somebody stands over is set
            // at. Turning about X tips the outboard edge up and the inboard edge down into the worktop,
            // so the low side is buried and the panel reads as let into the desk rather than laid on it.
            root.Children.Add(new MeshNode(
                Fabric.Map(Primitives.Box(0.62f, 0.022f, 0.24f), _keys, at, Finish.Snug), _keys)
            {
                Position = at,
                RotationDegrees = new Vector3(-26f, 0f, 0f),
                Name = "keys"
            });

            // A steel bead along the raised edge, which is what stops the panel reading as a decal. It is
            // the same trick as the rebate round the window: an edge is what makes a surface an object.
            root.Children.Add(Fabric.Slab(
                new Vector3(0.66f, 0.016f, 0.022f),
                new Vector3(x, top + 0.128f, face + 0.305f),
                steel,
                "keys.bead",
                Finish.Close));

            foreach (var over in new[] { -0.385f, 0.385f })
                root.Children.Add(new MeshNode(Primitives.Cylinder(0.034f, 0.040f, 0.052f, 10), steel)
                {
                    Position = new Vector3(x + over, top + 0.052f, face + 0.20f),
                    Name = "knob"
                });
        }
    }

    /// <summary>
    /// Three stools at five positions, and they are stools rather than chairs.
    ///
    /// The room had four chairs once and lost them to the console, on the argument that a bank of desks
    /// under a window says <i>somebody works here</i> in a way seating does not. That argument stands and
    /// this is not a reversal of it: a desk at eight hundred and sixty is a <b>standing</b> height, and
    /// what goes at a standing-height console is a high stool with a foot ring — which is a different
    /// object from a chair and makes a different claim. A chair says people sit here all day. A stool
    /// says they stand all day and sit down sometimes.
    ///
    /// <b>Three at five, and each one turned a different amount.</b> Five stools square to five desks is
    /// a showroom. Three of them, one pushed back and swung a quarter turn out as though somebody had got
    /// off it to go and look at something, is a room that was occupied twenty minutes ago — which is the
    /// only thing in this gallery that says anybody works here besides the lights being on.
    /// </summary>
    private static void Seats(Node root, Material steel, float west, float run)
    {
        var shell = Finish.Composite();
        shell.BaseColor = new Vector4(0.085f, 0.090f, 0.105f, 1f);
        shell.Metallic = 0.15f;
        shell.Roughness = 0.55f;
        shell.Name = "gallery.seat";

        // Bays two, three and four, so the west end of the run has none. That is not spacing for its own
        // sake: the bow bay is where somebody stands to look out and where the film hands the controls
        // over, and a stool two metres east of that hand-over is the first thing anybody walks into. The
        // walk audit says so in one number — the east leg went from twelve metres to two.
        foreach (var (bay, turn, back) in new[] { (2, -7f, 0f), (3, 5f, 0f), (4, 27f, 0.16f) })
        {
            var seat = new Node
            {
                Name = "seat",
                Position = new Vector3(west + (bay + 0.5f) * (run / 5f), 0f, Bore - 1.78f - back),
                RotationDegrees = new Vector3(0f, turn, 0f)
            };

            seat.Children.Add(new MeshNode(Primitives.Cylinder(0.22f, 0.20f, 0.05f, 16), steel)
            {
                Position = new Vector3(0f, 0.025f, 0f),
                Name = "seat.base"
            });

            seat.Children.Add(new MeshNode(Primitives.Cylinder(0.042f, 0.05f, 0.58f, 10), steel)
            {
                Position = new Vector3(0f, 0.30f, 0f),
                Name = "seat.stem"
            });

            // The foot ring, which is the one part of a stool nobody draws and everybody has used. It is
            // also the piece that gives the whole object a scale: a ring at two hundred and twenty
            // millimetres off the deck says the seat above it is high, without the seat having to be
            // measured against anything.
            seat.Children.Add(new MeshNode(Primitives.Torus(0.21f, 0.016f, 18, 6), steel)
            {
                Position = new Vector3(0f, 0.22f, 0f),
                Name = "seat.ring"
            });

            seat.Children.Add(Fabric.Slab(
                new Vector3(0.46f, 0.085f, 0.42f),
                new Vector3(0f, 0.615f, 0f),
                shell,
                "seat.pan",
                Finish.Close));

            seat.Children.Add(Fabric.Slab(
                new Vector3(0.44f, 0.44f, 0.075f),
                new Vector3(0f, 0.88f, -0.185f),
                shell,
                "seat.back",
                Finish.Close));

            root.Children.Add(seat);
        }
    }

    /// <summary>
    /// The sheen: what the deck would be reflecting if this renderer reflected anything.
    ///
    /// A polished floor under a line of light shows a smear of that light directly below it, softened and
    /// stretched by the roughness of the floor. There is no screen-space reflection here and there is not
    /// going to be one — so the smear is drawn: additive quads lying a centimetre above the deck, under
    /// the console's kick light and under the window, each a little wider and dimmer than the last so the
    /// stack falls off instead of ending.
    ///
    /// It is the same lie as the corridor's beacon shafts and the spill through this room's own window,
    /// told a third time, and it is worth stating plainly: <b>every soft light effect in this film is a
    /// surface standing where the light would be.</b> That is what a renderer that draws surfaces can do,
    /// and doing it deliberately is a great deal cheaper than the alternatives and — at this distance, in
    /// this light — indistinguishable from them.
    /// </summary>
    private void Sheen(Node root)
    {
        var run = GlassEast - GlassWest + 1.4f;
        var middle = (GlassWest + GlassEast) / 2f;
        var z = Bore - 1.15f - 0.72f / 2f;

        foreach (var (width, at) in new[] { (0.22f, 0.12f), (0.55f, 0.28f), (1.1f, 0.55f) })
            root.Children.Add(new MeshNode(Primitives.Plane(run, width), _sheen)
            {
                Position = new Vector3(middle, 0.012f, z - at),
                Name = "sheen"
            });
    }

    /// <summary>
    /// What the deck does with the ceiling and with the plot: the two cold reflections, painted on.
    ///
    /// The reference rooms all have the same floor — dark, wet-looking, with the light fittings smeared down
    /// it — and there is no way to get that here honestly. A reflection needs either a second pass from a
    /// mirrored camera or an environment probe, and this renderer has neither; a glossy dielectric under a
    /// single directional light gives one specular blob from the star and a Fresnel rim at grazing, which is
    /// not a reflection of the room, it is a reflection of the one thing outside it. That was tried, and
    /// what came back was a floor that lit up white the moment the camera dropped below a metre.
    ///
    /// So it is drawn, the same way the window's spill and the console's kick light are drawn. Three nested
    /// quads down the middle of the room, brightest at the centre line and gone by the beams, which is what
    /// a run of ceiling lamps looks like in a polished floor; and a disc under the plot table, because the
    /// brightest object in the room at head height has to be doing something to the deck under it. Both are
    /// elongated along the room rather than across it, and that is the one decision in here that is about
    /// the camera: he walks the length of this room and looks along it or out of it, so a streak that runs
    /// with the walk stays correct from everywhere he ever stands, and a pool would only be correct from
    /// directly above.
    /// </summary>
    private void Gleam(Node root)
    {
        var middle = (West + East) / 2f;
        var run = East - West - 1.2f;

        foreach (var (width, at) in new[] { (1.9f, 2.10f), (1.05f, 2.10f), (0.46f, 2.10f) })
            root.Children.Add(new MeshNode(Primitives.Plane(run, width), _gleam)
            {
                Position = new Vector3(middle, 0.016f, at),
                Name = "gleam"
            });

        // And under the plot. Two discs rather than one, because a reflection has no edge — a single disc
        // is a dinner plate on the floor, and two with the smaller one twice as bright is a fall-off.
        foreach (var radius in new[] { 1.35f, 0.72f })
            root.Children.Add(new MeshNode(Primitives.Disc(radius, 28), _gleam)
            {
                Position = new Vector3(TableAt.X, 0.018f, TableAt.Z),
                Name = "gleam.plot"
            });

        // Along the foot of the inboard wall, under the charts and the equipment banks. Narrower and
        // fainter than the streak down the middle, because what is over it is a wall rather than a lid:
        // the racks throw about half what the ceiling lamps do and they throw it at the floor a metre
        // out, not four.
        foreach (var (width, at) in new[] { (1.05f, 0.50f), (0.48f, 0.40f) })
            root.Children.Add(new MeshNode(Primitives.Plane(run - 1.6f, width), _sheen)
            {
                Position = new Vector3(middle, 0.015f, at),
                Name = "gleam.wall"
            });

        // And in the bow bay, which is the one square metre of this deck with a window on two sides of
        // it and is where the film puts the controls in somebody's hands. It gets a pool rather than a
        // streak: he is standing still there, and a streak is a thing you read while walking past.
        foreach (var radius in new[] { 1.6f, 0.85f })
            root.Children.Add(new MeshNode(Primitives.Disc(radius, 24), _gleam)
            {
                Position = new Vector3(West + 1.0f, 0.017f, (PortIn + Bore) / 2f),
                Name = "gleam.bay"
            });
    }

    /// <summary>
    /// The light through the window, laid on the deck and the ceiling by hand.
    ///
    /// This renderer casts no shadows, so the star reaches every surface in the room equally and none of
    /// them can tell there is a wall in the way — which is why the deck had to be painted almost black to
    /// stop it reading as lit. The consequence of solving it that way is that the floor under thirteen
    /// metres of window is exactly as dark as the floor behind the benches, and that is a picture with the
    /// most obvious thing in it missing.
    ///
    /// So the spill is drawn: five additive bands, each shallower than the last, stacking to a gradient
    /// that is brightest at the glass and gone by the inboard wall. They write no depth and cost no light
    /// slot, and the eye reads the result as a shaft coming in. It is the same lie the corridor's beacons
    /// tell with a cone — light is not a surface, and a renderer that draws surfaces has to say so with
    /// one anyway.
    /// </summary>
    private void Spill(Node root)
    {
        var run = GlassEast - GlassWest;
        var mid = (GlassWest + GlassEast) / 2f;

        foreach (var reach in new[] { 2.9f, 2.3f, 1.7f, 1.15f, 0.7f })
        {
            var from = Bore - reach;

            root.Children.Add(new MeshNode(Primitives.Plane(run * (0.86f + reach * 0.05f), reach), _spill)
            {
                Position = new Vector3(mid, 0.014f, (from + Bore) / 2f),
                Name = "spill"
            });
        }

        // And a fainter one overhead, because a window that puts light on the floor puts light on the
        // ceiling too — less of it, and further in, which two bands is enough to say.
        foreach (var reach in new[] { 1.5f, 0.9f })
            root.Children.Add(new MeshNode(Primitives.Plane(run * 0.9f, reach), _spill)
            {
                Position = new Vector3(mid, Height - 0.02f, Bore - reach / 2f),
                RotationDegrees = new Vector3(180f, 0f, 0f),
                Name = "spill.over"
            });
    }

    /// <summary>
    /// The plot table: a pedestal in the middle of the gallery with a world turning over it.
    ///
    /// It is placed against the inboard side rather than in the middle of the floor, which is a staging
    /// decision and not a spatial one — the walk runs along the glass, and a table in the middle of that
    /// is a thing to be avoided rather than a thing to be passed.
    /// </summary>
    private void Plot(Node root)
    {
        var table = new Node { Name = "plot", Position = TableAt };
        root.Children.Add(table);

        var shell = Finish.Composite();
        shell.BaseColor = new Vector4(0.135f, 0.145f, 0.168f, 1f);
        shell.Metallic = 0.3f;
        shell.Name = "plot.shell";

        table.Children.Add(new MeshNode(Primitives.Cylinder(0.42f, 0.34f, 0.86f, 24), shell)
        {
            Position = new Vector3(0f, 0.43f, 0f),
            Name = "pedestal"
        });

        table.Children.Add(new MeshNode(Primitives.Torus(0.42f, 0.02f, 32, 8), _holo)
        {
            Position = new Vector3(0f, 0.875f, 0f),
            Name = "rim"
        });

        table.Children.Add(new MeshNode(Primitives.Disc(0.40f, 28), _beam)
        {
            Position = new Vector3(0f, 0.884f, 0f),
            Name = "emitter"
        });

        // The globe, and it is a globe rather than a ball: five parallels and eight meridians, which is
        // what a hologram of a world is made of when the thing drawing it is honest about being a
        // projector.
        var hollow = new Node { Name = "hologram", Position = new Vector3(0f, 1.42f, 0f) };
        table.Children.Add(hollow);

        _globe.Children.Add(new LineNode
        {
            Positions = Wireframe(0.30f),
            Color = new Vector3(0.36f, 0.86f, 1f),
            Opacity = 0.55f,
            Width = 1.2f,
            Blend = BlendMode.Additive,
            DepthWrite = false,
            Name = "globe.lines"
        });

        _orbits.Children.Add(new LineNode
        {
            Positions = Orbits(0.30f),
            Color = new Vector3(0.62f, 0.92f, 1f),
            Opacity = 0.45f,
            Width = 1.1f,
            Blend = BlendMode.Additive,
            DepthWrite = false,
            Name = "orbit.lines"
        });

        hollow.Children.Add(_globe);
        hollow.Children.Add(_orbits);

        hollow.Children.Add(new SpriteNode
        {
            Texture = Space.Glow(),
            Color = new Vector3(0.20f, 0.52f, 0.72f),
            Size = new Vector2(1.05f),
            Opacity = 0.22f,
            Blend = BlendMode.Additive,
            DepthWrite = false,
            Name = "bloom"
        });

        // The column of light between the emitter and the globe, which is what says the one is making the
        // other. Two nested cones, so it has an edge that fades rather than a silhouette.
        foreach (var shellRadius in new[] { 1f, 0.6f })
            table.Children.Add(new MeshNode(
                Primitives.Cylinder(0.30f, 0.06f, 0.5f, 16, capped: false), _beam)
            {
                Position = new Vector3(0f, 1.14f, 0f),
                Scale = new Vector3(shellRadius, 1f, shellRadius),
                Name = "beam"
            });
    }

    /// <summary>Parallels and meridians on a sphere, as line segments.</summary>
    private static Vector3[] Wireframe(float radius)
    {
        var lines = new List<Vector3>();
        const int arc = 40;

        for (var band = 1; band <= 5; band++)
        {
            var lat = (band / 6f - 0.5f) * MathF.PI;
            var r = MathF.Cos(lat) * radius;
            var y = MathF.Sin(lat) * radius;

            for (var i = 0; i < arc; i++)
            {
                var a = i / (float)arc * MathF.Tau;
                var b = (i + 1) / (float)arc * MathF.Tau;

                lines.Add(new Vector3(MathF.Cos(a) * r, y, MathF.Sin(a) * r));
                lines.Add(new Vector3(MathF.Cos(b) * r, y, MathF.Sin(b) * r));
            }
        }

        for (var meridian = 0; meridian < 8; meridian++)
        {
            var turn = meridian / 8f * MathF.PI;

            for (var i = 0; i < arc; i++)
            {
                var a = i / (float)arc * MathF.Tau;
                var b = (i + 1) / (float)arc * MathF.Tau;

                lines.Add(new Vector3(MathF.Cos(a) * radius * MathF.Cos(turn),
                    MathF.Sin(a) * radius, MathF.Cos(a) * radius * MathF.Sin(turn)));
                lines.Add(new Vector3(MathF.Cos(b) * radius * MathF.Cos(turn),
                    MathF.Sin(b) * radius, MathF.Cos(b) * radius * MathF.Sin(turn)));
            }
        }

        return [.. lines];
    }

    /// <summary>Three orbits round the globe, each on its own inclination.</summary>
    private static Vector3[] Orbits(float radius)
    {
        var lines = new List<Vector3>();
        const int arc = 56;

        for (var orbit = 0; orbit < 3; orbit++)
        {
            var r = radius * (1.35f + orbit * 0.26f);
            var tilt = (18f + orbit * 26f) * MathF.PI / 180f;
            var swing = orbit * 1.1f;

            Vector3 On(float t)
            {
                var flat = new Vector3(MathF.Cos(t + swing) * r, 0f, MathF.Sin(t + swing) * r);

                return new Vector3(
                    flat.X,
                    flat.Z * MathF.Sin(tilt),
                    flat.Z * MathF.Cos(tilt));
            }

            for (var i = 0; i < arc; i++)
            {
                lines.Add(On(i / (float)arc * MathF.Tau));
                lines.Add(On((i + 1) / (float)arc * MathF.Tau));
            }
        }

        return [.. lines];
    }

    /// <summary>
    /// Dust in the air, drifting.
    ///
    /// Three hundred points at a fifth of a pixel of brightness, in a node that turns a third of a degree a
    /// second about a centre thirty metres below the deck — so up here the arc is indistinguishable from a
    /// straight line and the whole cloud crawls sideways at about a centimetre a second. It is the last
    /// thing anybody would notice and the first thing they would miss: a room with nothing moving in the
    /// air is a room made of glass and steel, which this one is, and it should still not feel like one.
    ///
    /// <see cref="PointsNode.SizeAttenuation"/> is left <i>on</i> here, which is the opposite of what the
    /// starfield does and is the same reasoning: a star has no size and a mote does, so a mote two metres
    /// away should be bigger than a mote eleven metres away, and a star should not.
    /// </summary>
    private void Motes(Node root)
    {
        var points = new Vector3[300];
        var seed = 20_260_808u;

        for (var i = 0; i < points.Length; i++)
        {
            points[i] = new Vector3(
                West + Next(ref seed) * (East - West),
                0.25f + Next(ref seed) * (Height - 0.6f),
                0.1f + Next(ref seed) * (Bore - 0.1f)) + new Vector3(0f, 30f, 0f);
        }

        _dust[0] = new PointsNode
        {
            Positions = points,
            Color = new Vector3(0.62f, 0.76f, 0.95f),
            Opacity = 0.3f,
            Size = 2.4f,
            Blend = BlendMode.Additive,
            DepthWrite = false,
            Name = "motes"
        };

        _drift.Position = new Vector3(0f, -30f, 0f);
        _drift.Children.Add(_dust[0]);
        root.Children.Add(_drift);
    }

    /// <summary>A deterministic scatter, so the dust is in the same place on every run and in every
    /// backend. A film that can be seeked cannot have a random number in it.</summary>
    private static float Next(ref uint state)
    {
        state = state * 1_664_525u + 1_013_904_223u;

        return (state >> 8) / 16_777_216f;
    }

    /// <summary>
    /// The starfield: two point layers on a sphere, and a nebula behind them.
    ///
    /// <see cref="PointsNode.SizeAttenuation"/> is off, which is what makes a star a star: a point that
    /// gets smaller with distance is a small object nearby, and a point that holds two pixels however far
    /// away it is, is a light. It is also the reason a starfield can be fourteen hundred metres out in a
    /// film whose next-largest distance is fifteen — nothing about the size depends on the range.
    /// </summary>
    private static void Sky(Node outside)
    {
        outside.Children.Add(new MeshNode(Primitives.Sphere(SkyRange + 100f, 40, 26), new Material
        {
            BaseColorTexture = Space.Nebula(),
            BaseColor = new Vector4(0.30f, 0.31f, 0.36f, 1f),
            Unlit = true,
            Cull = CullMode.Front,
            DepthWrite = false,
            Name = "nebula"
        })
        {
            Position = Anchor,
            RenderOrder = -10,
            Name = "sky"
        });

        outside.Children.Add(new PointsNode
        {
            Positions = Scatter(Fleet.OnSphere(1_400, SkyRange, seed: 1)),
            Color = new Vector3(0.55f, 0.71f, 0.80f),
            Size = 2f,
            SizeAttenuation = false,
            Blend = BlendMode.Additive,
            DepthWrite = false,
            RenderOrder = -5,
            Name = "stars"
        });

        outside.Children.Add(new PointsNode
        {
            Positions = Scatter(Fleet.OnSphere(150, SkyRange - 40f, seed: 2)),
            Color = new Vector3(0.87f, 0.93f, 1f),
            Opacity = 0.9f,
            Size = 3.6f,
            SizeAttenuation = false,
            Blend = BlendMode.Additive,
            DepthWrite = false,
            RenderOrder = -4,
            Name = "stars.bright"
        });
    }

    /// <summary>The same points, moved out to where the gallery is looking from.</summary>
    private static Vector3[] Scatter(Vector3[] points)
    {
        for (var i = 0; i < points.Length; i++)
            points[i] += Anchor;

        return points;
    }

    /// <summary>
    /// The planet: a bump-mapped body with cities on its night side, and an atmosphere that is nothing but
    /// a rim.
    ///
    /// It is <see cref="Scenes.RimScene"/>'s two materials at the size the shot needs, and the two of them
    /// are the whole of what makes a sphere read as a world. The rim is a second sphere two and a half
    /// percent larger whose base colour is fully transparent, so all it can ever contribute is
    /// <see cref="Material.RimColor"/> falling off with the angle — and <see cref="Material.RimLightBias"/>
    /// is what keeps the glow on the sunward limb instead of drawing a neon circle. The cities are an
    /// emissive map masked to the dark hemisphere, which is the one thing a city map must do and the one
    /// thing an ordinary emissive cannot.
    /// </summary>
    private static Node Planet(Node outside)
    {
        var (albedo, bump, cities) = Space.Planet();

        var planet = new Node
        {
            Position = Anchor + Bearing(-5f, 2f) * PlanetRange,
            RotationDegrees = new Vector3(-16f, 0f, 7f),
            Name = "planet"
        };

        outside.Children.Add(planet);

        planet.Children.Add(new MeshNode(Primitives.Sphere(PlanetRadius, 64, 44), new Material
        {
            BaseColorTexture = albedo,

            // Half again over the map, and it is what lets the star be dimmer than the picture needs.
            //
            // One light has to do two jobs here that pull opposite ways: make a planet read as sunlit, and
            // leave a steel gallery reading as dark. There is no shadowing, so every surface in the room
            // gets the full beam whether or not a wall is in the way, and the intensity that made the
            // planet right made the reveals round the window into bands of bright chrome. Base colour
            // multiplies the map and it is allowed above one — the tone map is downstream of it — so the
            // planet keeps its exposure while the room gets a fifth less light.
            BaseColor = new Vector4(2.1f, 2.1f, 2.1f, 1f),
            Roughness = 0.95f,
            BumpTexture = bump,
            BumpScale = 9f,
            EmissiveTexture = cities,
            EmissiveColor = new Vector3(1.1f),
            EmissiveNightSide = true,
            EmissiveNightSideStart = 0.2f,
            EmissiveNightSideEnd = -0.1f,

            // Out of reach of the room's lighting, and this is the one surface in the film that has to say
            // so. The environment probe belongs to the scene rather than to a room, so every lit material
            // gets it — and the base colour above is 2.1, so this one gets it two and a bit times over.
            // The result was a light switch on a ship changing the weather on a world: walk in with the
            // lamps down and the planet has a terminator and a night side with cities on it; bring them up
            // and the dark side lifts from a luminance of 19 to 27, grey-blue, with the cities muddied.
            //
            // Occlusion is the right instrument rather than a trick. It is defined as how much of the
            // surrounding environment a point can see, it is the one per-material term every backend
            // already multiplies into the indirect light and nothing else — the GL shader says so on the
            // line above it — and a planet six hundred metres out can see none of this gallery. The sun is
            // untouched, which is the whole difference between this and turning the ambient down.
            OcclusionTexture = Space.Elsewhere,
            OcclusionStrength = 1f,

            Cull = CullMode.Back,
            Name = "planet"
        }));

        planet.Children.Add(new MeshNode(Primitives.Sphere(PlanetRadius * 1.025f, 48, 32), new Material
        {
            BaseColor = Vector4.Zero,
            RimColor = new Vector3(0.42f, 0.68f, 1f),
            RimPower = 3.2f,
            RimIntensity = 1.5f,
            RimLightBias = 0.4f,
            Blend = BlendMode.Additive,
            DepthWrite = false,
            Cull = CullMode.Back,
            Name = "atmosphere"
        })
        {
            RenderOrder = 1
        });

        return planet;
    }

    /// <summary>
    /// The star itself: a disc and a corona, both unlit.
    ///
    /// It is behind the plane of the hull — see <see cref="SunBearing"/> — so the plate occludes it from
    /// everywhere in the gallery and nothing in the walk can find it. It is drawn anyway, because the light
    /// in this room has to come from an object and not from a setting, and because the frame this film cuts
    /// to next has it in shot.
    /// </summary>
    private static void Sun(Node outside)
    {
        var at = Anchor + SunBearing * 1_150f;

        outside.Children.Add(new MeshNode(Primitives.Sphere(26f, 24, 18), new Material
        {
            BaseColor = new Vector4(1f, 0.95f, 0.78f, 1f),
            Unlit = true,
            Name = "sun"
        })
        {
            Position = at,
            Name = "sun"
        });

        outside.Children.Add(new SpriteNode
        {
            Texture = Space.Glow(),
            Position = at,
            Color = new Vector3(1f, 0.85f, 0.56f),
            Size = new Vector2(150f),
            Blend = BlendMode.Additive,
            DepthWrite = false,
            Name = "corona"
        });
    }

    private static Material Glow(float r, float g, float b) => new()
    {
        BaseColor = new Vector4(r, g, b, 1f),
        EmissiveColor = new Vector3(r, g, b),
        Unlit = true,
        Blend = BlendMode.Additive,
        DepthWrite = false,
        Name = "glow"
    };

    private static void Paint(Material material, Vector3 colour, float value)
    {
        var lit = colour * Math.Clamp(value, 0f, 1f);

        material.EmissiveColor = lit;
        material.BaseColor = new Vector4(lit, 1f);
    }

    private static float Fraction(float t) => t - MathF.Floor(t);

    /// <summary>The same fade-in <see cref="Chapter.Ramp"/> is, for the parts of this room that move on
    /// their own clock rather than on a chapter's. Written out rather than reached for, because a room is
    /// not a chapter and the day it inherits from one is the day a wall can decide what second it is.</summary>
    private static float Ramp(float at, float from, float over)
    {
        if (over <= 0f)
            return at >= from ? 1f : 0f;

        var u = Math.Clamp((at - from) / over, 0f, 1f);
        return u * u * (3f - 2f * u);
    }
}
