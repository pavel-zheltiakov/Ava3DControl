using System.Numerics;
using Ava3D.Demo.Scenes.Board;

namespace Ava3D.Demo.Story;

/// <summary>
/// The engine room: seventeen metres by thirteen and six and a half to the ceiling, two engine housings
/// down one side, and a service bench at the far end with three server chassis on it and a board out of one
/// of them.
///
/// It is the room the whole building has been on top of, and it is the first one that is bigger than a
/// building. That reading is bought entirely by the corridor: twenty-one metres of two-metre passage at two
/// seven to the ceiling, then six hundred millimetres of bulkhead, then this. Nothing in here has to be
/// told it is large.
///
/// <b>Four lamps light a hangar, and three of them are pointed at a bench.</b> That is the room's argument
/// and it is the third and last time the film makes it. Everything you can see that is not the bench is
/// <i>emission</i> — the strip under the gantry, the coolant channels along the housings, the indicator
/// grids on the racks by the door — so the room has depth, and scale, and machinery in it that is plainly
/// running, and none of it is spending a light slot. The antechamber made this argument with one glowing
/// bulb, the lounge made it with four televisions in a dark room, and here it is the difference between a
/// room and a rendering of a room.
///
/// <b>The bench is two objects and they are not the same size.</b> On the display is the board as
/// information: <see cref="DraftsmanScene"/>'s drawing, then <see cref="WireframeScene"/>'s edges over it,
/// then <see cref="InspectorScene"/> walking it and naming what is broken — all of it a metre across,
/// because a schematic is as big as the screen it is on. On the bench in front of it is the board as an
/// object, four hundred and sixty millimetres of it, lying flat with its indicator card unplugged beside
/// it, because that is how big a board is. He reads the one and then repairs the other, which is what a
/// service manual is for.
///
/// That difference costs one number. <see cref="ViewScale"/> and <see cref="Actual"/> are the same model at
/// two sizes, and a mounted subject that carries anything in world units has to be told which — see
/// <see cref="IndicatorsScene"/>, where a point light's range and a bloom sprite's size both are.
/// </summary>
internal sealed class EngineRoom
{
    /// <summary>The room, in its own coordinates. X runs with the deck's; Z runs in from the bulkhead.</summary>
    public const float West = -4.5f;

    public const float East = 12.5f;
    public const float Depth = 13f;
    public const float Height = 6.5f;

    /// <summary>
    /// The south wall, and the one wall in the building that is thick enough to walk through.
    ///
    /// Six hundred millimetres does two things at once. It is a reveal — after twenty-one metres of
    /// corridor, half a second of being <i>inside</i> a wall is most of what says the next room is a
    /// different kind of space — and it is where the corridor's own end wall goes. That wall is a quarter
    /// of a metre thick and stands entirely within this one, with a hundred and fifty millimetres to spare
    /// on both faces, so nothing anywhere near the threshold is coplanar with anything. See
    /// <see cref="Deck.Engine"/>, which is what the two rooms are measured against.
    /// </summary>
    private const float Bulkhead = 0.6f;

    private const float Thickness = 0.25f;

    /// <summary>
    /// Where the way out is along the north wall, and it is this room's number rather than the gallery's.
    ///
    /// A doorway belongs to the wall it is cut in. The illuminator gallery is bolted to this wall the same
    /// way the corridor was bolted to the lounge's, so it reads the opening from here — see
    /// <see cref="Illuminator.Doorway"/>, which is this constant and nothing else.
    ///
    /// Five metres nine puts it clear of the bench, which ends at four metres six, and clear of the first
    /// engine housing, whose barrel starts at six ninety-five. It is the only metre and a bit of that wall
    /// with nothing already standing against it.
    /// </summary>
    public const float Doorway = 5.9f;

    /// <summary>How far south of the room the floor reaches: to where the corridor's floor stops, edge to
    /// edge, so the seam falls under the door.</summary>
    private const float Apron = -0.45f;

    /// <summary>The bench along the far wall, and how high its top is.</summary>
    private const float Top = 0.92f;

    private static readonly Vector3 BenchAt = new(1.4f, 0f, 12.35f);
    private const float BenchLong = 6.4f;
    private const float BenchDeep = 1.1f;

    /// <summary>The display standing at the back of the bench, and the size of its glass.</summary>
    private static readonly Vector3 ScreenAt = new(-0.9f, 1.62f, 12.72f);

    private const float ScreenWide = 1.3f;
    private const float ScreenTall = 1f;

    /// <summary>
    /// How big the board is on the display: a metre and a tenth across, which is what fits the glass.
    ///
    /// The model is authored at a hundred millimetres to the unit, so this is three tenths of full size
    /// and the board on the bench is fifteen hundredths of it. Both numbers are here rather than in the
    /// scenes, because the scenes have no opinion about how big a board is — they build one at the size
    /// the file says and let whoever mounts it decide.
    /// </summary>
    private const float ViewScale = 0.30f;

    /// <summary>
    /// How far the views are shifted along the glass to sit in the middle of it.
    ///
    /// The model's origin is the <i>motherboard's</i> centre, and there is a panel hanging two hundred
    /// millimetres off its right-hand edge that is part of the same object — so a view centred on the
    /// origin is a board in the middle of the screen with its indicator card off the side of it. Half the
    /// overhang, in the direction the yaw sends it, is the offset that centres the whole picture.
    /// </summary>
    private const float ViewShift = 0.10f;

    /// <summary>
    /// And how big it is on the bench: four hundred and fifty-eight millimetres, which is a server board.
    ///
    /// It was three metres for one round of this room and it was wrong for a reason worth writing down. A
    /// board scaled up to the size of a wall is legible from anywhere and is no longer a component — you
    /// cannot pick it up, the socket is at head height, and the visitor is not repairing a machine, he is
    /// standing in front of a monument to one. Small enough to lie on a bench is what makes plugging a card
    /// into it an action rather than an installation.
    /// </summary>
    private const float Actual = 0.15f;

    private static readonly Vector3 BoardAt = new(1.75f, Top + 0.015f, 12.15f);

    /// <summary>
    /// Nine degrees off square, and the hundred and eighty that go with them.
    ///
    /// The nine are so that a board on a bench is a board somebody put down rather than a board that was
    /// installed. The hundred and eighty are not taste: the model's own +X points along the room's +X,
    /// which is the visitor's <i>left</i> — he is looking up the deck's +Z and this is a right-handed
    /// world — so without them every designator on the silkscreen comes out written backwards and the
    /// indicator card ends up on the opposite side from the one the screen behind it is showing. Turning
    /// the board round costs nothing and fixes both.
    /// </summary>
    private const float BoardYaw = 171f;

    /// <summary>
    /// Where the indicator card lies before it is fitted, in the board's own units — which at this mount
    /// are a hundred and fifty millimetres each.
    ///
    /// A hundred and ten millimetres outboard along the board's own +X, and <b>nothing in Y or Z</b>. That
    /// is the whole of what makes it read as a connector being mated: a plug goes in along its own axis,
    /// flat, in a straight line, and a card that arrives from above has been dropped on a socket rather
    /// than pushed into one.
    ///
    /// <b>The flex jumper does not travel with it.</b> The cable is already run and lying where it will be;
    /// what moves is the card, onto the end of it. Carrying the ribbon along instead makes it a subassembly
    /// being lowered into place, which is a different job and reads as one.
    /// </summary>
    private static readonly Vector3 Loose = new(0.75f, 0f, 0f);

    /// <summary>
    /// Where the four parts the film brings with it belong, and where they are lying before they do.
    ///
    /// All of it is in the board's own coordinates, straight out of <c>board.txt</c>: the processor socket
    /// is at 82,158 and is 45 across, so its middle is at 104.5,180.5 and
    /// <see cref="BoardData.At"/> turns that into the numbers below. The memory slots are at 190 and 216
    /// with their channels running 133 millimetres up the board, which is why the modules are long in Z
    /// and thin in X.
    ///
    /// The loose positions are a lay-out rather than a scatter: far enough off the board to be clear, near
    /// enough to be in the same shot, and every part lying against the edge of the board it goes in from —
    /// which is what a bench looks like when somebody has put the parts out before starting. The processor
    /// and the cooler go into the middle and lie along the far edge; the memory goes into the near half and
    /// lies along the near one. That last split is clearance rather than composition — see
    /// <see cref="ModuleLoose"/>, which is where the cooler came into it.
    /// </summary>
    private static readonly Vector3 ChipHome = new(-0.48f, 0.016f, -0.585f);

    private static readonly Vector3 ChipLoose = new(-1.95f, 0f, -1.25f);
    private static readonly Vector3 CoolerHome = new(-0.48f, 0.052f, -0.585f);
    private static readonly Vector3 CoolerLoose = new(-2.6f, 0f, -0.45f);

    /// <summary>Half the module's thickness over its relief, so a module lying on its side rests on the
    /// bench rather than half inside it.</summary>
    private const float Flat = 0.0215f;

    /// <summary>How many segments of lit diffuser run along the top of a module. Eight is enough for the
    /// hue to be a gradient down the stick rather than a row of coloured blocks.</summary>
    private const int Segments = 8;

    /// <summary>
    /// Where a module stands when it is in: the middle of a slot's channel, resting on its floor.
    ///
    /// Channel A first and then channel B, which is what a pair of modules goes into and is why the step
    /// is two slots wide rather than one. The Z is the middle of a run that starts 96 mm up the board and
    /// is 133.35 long, to the quarter-millimetre — the slot leaves three tenths of clearance at each end
    /// and a module rounded to the nearest centimetre of model would eat all of it at one of them.
    /// </summary>
    private static Vector3 ModuleHome(int index) => new(0.41f + index * 0.26f, 0.03f, -0.40675f);

    /// <summary>
    /// Where a module lies before it is fitted: flat on the mat, off the board's other long edge.
    ///
    /// <b>The other edge, and it is the cooler that decided it.</b> All four parts were laid out in one
    /// row along the far side, which composes well and puts the memory on the wrong side of a tower. A
    /// part in this film is picked up, carried across at height and set straight down — a straight line in
    /// plan — and a straight line from the far side of the board to a slot on the near side runs through
    /// the middle of it, which is where a hundred and seventeen millimetres of fin stack is now standing.
    /// The modules flew through the cooler, by fifty millimetres at the worst of it. Clearing it by lifting
    /// higher would need a hundred and ninety, which is not a part being carried over a board, it is a
    /// part being flown over one; so the modules are laid out on the side they go in from instead, which
    /// is what somebody who has done this does anyway. Measured over the whole travel they now miss the
    /// cooler by thirty-nine and sixty-five millimetres.
    ///
    /// The half-unit spacing is the other clearance: a module lying on its side is as wide as it is tall,
    /// and at nearly forty millimetres tall the old four tenths had the two of them touching.
    /// </summary>
    private static Vector3 ModuleLoose(int index) => new(2.05f + index * 0.5f, Flat, 0.75f);

    /// <summary>How many dots each rack by the door carries, and where the racks are.</summary>
    private const int Dots = 12;

    private static readonly float[] Racks = [-3.4f, 7.5f, 10.3f];

    /// <summary>Where the three server chassis stand on the bench. The middle one is open.</summary>
    private static readonly float[] Chassis = [2.9f, 3.72f, 4.54f];

    private readonly Material[] _rack = new Material[6];
    private readonly Material[] _coolant = new Material[4];
    private readonly Material _strip;
    private readonly Material _glass;
    private readonly Material _chrome;
    private readonly Material _ink;
    private readonly Material _accent;
    private readonly Material _live;

    /// <summary>The lit ring round the fan, in twelve arcs so it can carry twelve colours at once.</summary>
    private readonly Material[] _ring = new Material[12];

    /// <summary>One material a blade, so the whole rotor carries the hue circle rather than the frame
    /// alone — and so that nine colours sweeping past is what tells you it is turning.</summary>
    private readonly Material[] _blades = new Material[9];

    /// <summary>The lit diffusers along the two memory modules, eight segments each, so the hue runs down
    /// a stick instead of a stick being one colour.</summary>
    private readonly Material[] _bars = new Material[2 * Segments];

    private readonly Material? _paper;
    private readonly LineNode? _folds;
    private readonly LineNode? _copper;
    private readonly LineNode? _edges;
    private readonly Node? _wireframe;
    private readonly Node? _schematic;

    private readonly Node? _card;
    private readonly Vector3 _cardHome;

    private readonly Node? _cpu;
    private readonly Node? _cooler;
    private readonly Node? _rotor;
    private readonly Node?[] _memory = new Node?[2];

    public EngineRoom(Hall hall)
    {
        var root = hall.Add(Deck.EngineRoom, Deck.Engine);

        // Darker than the corridor's, and by more than the corridor is darker than the lounge. A wall is
        // only as dark as the lamp on it: this room's are the same panelling three shades down, and at the
        // first colour they were tried at — the corridor's own, which is right in a two-metre passage —
        // seventeen metres of it under five high-bay lamps came out as a white tiled bathroom. What a
        // hangar looks like is a surface the light barely gets to, and the way to draw that is to give it
        // less to reflect and less light to reflect it.
        var plate = Finish.Panelling();
        plate.BaseColor = new Vector4(0.125f, 0.130f, 0.148f, 1f);
        plate.Roughness = 0.62f;
        plate.Name = "engine.plate";

        var deck = Finish.Floor(Grade.Dressed);
        deck.BaseColor = new Vector4(0.105f, 0.105f, 0.120f, 1f);
        deck.Metallic = 0.22f;
        deck.Roughness = 0.56f;
        deck.Name = "engine.deck";

        _strip = Glow(0.55f, 0.72f, 0.95f);
        _glass = Glow(0.30f, 0.52f, 0.72f);
        _chrome = Glow(0.46f, 0.68f, 0.86f);
        _ink = Glow(0.72f, 0.86f, 1f);
        _accent = Glow(0.42f, 1f, 0.62f);
        _live = Glow(0.34f, 0.76f, 1f);

        Shell(root, plate, deck);
        Roof(root);
        Gantry(root);
        Housings(root);
        Cabinets(root);
        Furniture(root);

        // Five lamps, four slots, and never more than four of them burning. Every one is well under what
        // the same fixture is worth in the lounge, which is the other half of the fix for a room that first
        // rendered as a bathroom: a high bay at three and a half over a black floor is a lamp with nothing
        // to be brighter than.
        High =
        [
            Fabric.Ceiling(Deck.Engine, new Vector3(1f, Height - 0.55f, 3.6f), 3f, 13f),
            Fabric.Ceiling(Deck.Engine, new Vector3(8f, Height - 0.55f, 8.2f), 2.4f, 13f)
        ];

        Bay = Fabric.Ceiling(Deck.Engine, new Vector3(1.4f, 3.4f, 11.9f), 2.6f, 6.5f);
        Task = Fabric.Ceiling(Deck.Engine, new Vector3(1.75f, 1.62f, 11.95f), 0.75f, 1.6f);
        Fill = Fabric.Ceiling(Deck.Engine, new Vector3(-0.9f, 2.5f, 12f), 1f, 3.2f);

        foreach (var lamp in new[] { High[0], High[1], Bay, Fill })
        {
            root.Children.Add(lamp.Fixture);
            Drop(root, lamp.Fixture.Position);
        }

        // The task lamp is on an arm off the back of the bench rather than hung from six and a half metres
        // of ceiling, because it is thirty centimetres from what it is lighting.
        root.Children.Add(Task.Fixture);
        Arm(root, Task.Fixture.Position);

        Draft = new DraftsmanScene();
        Mesh = new WireframeScene();
        Probe = new InspectorScene();
        Panel = new IndicatorsScene();

        // Three views on one screen, all three of them the same model and none of them built here. They
        // are stacked a few millimetres apart in front of the glass so no two are ever coplanar, and only
        // one is ever switched on.
        if (Draft.BuildSubject() is { } drawing)
        {
            View(root, drawing, 0.012f);

            _paper = drawing.Find<MeshNode>("drawing.fill")?.Material;
            _folds = drawing.Find<LineNode>("drawing.lines");
            _copper = drawing.Find<LineNode>("drawing.copper");
        }

        if (Mesh.BuildSubject() is { } wireframe)
        {
            View(root, wireframe, 0.028f);
            wireframe.IsVisible = false;

            _wireframe = wireframe;
            _edges = wireframe.Find<LineNode>("wireframe.edges");
        }

        if (Probe.BuildSubject() is { } schematic)
        {
            View(root, schematic, 0.052f);
            schematic.IsVisible = false;

            _schematic = schematic;
        }

        // And the board itself, on the bench, at its own size. It is IndicatorsScene's subject rather than
        // the Inspector's, and the two are separate boards for the first time — which is what the display
        // is for. The one on the screen is a drawing of a machine; the one on the bench is the machine.
        if (Panel.BuildSubject() is { } board)
        {
            board.Name = "board";

            // The motherboard's printing comes off, and the number is why.
            //
            // The silkscreen is flat geometry lying exactly on the laminate with a depth bias to win the
            // depth test — the surface that feature exists for, and the two board scenes are built to show
            // it. Here it is a bug, and measuring it says so: the bars are 0.36 mm, this mount is 305 mm of
            // board over 457 of bench, and the lens is 55° over thirteen hundred lines, so a bar is a
            // pixel wide at three quarters of a metre and half of one at a metre and a half. The copper and
            // the legends beside it are texture and mip down to a smooth grey at that size; geometry has no
            // mip-maps and cannot, so a bar is either in a pixel or not, and what the camera sees when it
            // moves is a line of white dashes crawling over the board. Rendering the same second at four
            // times the resolution and averaging it back down makes them vanish, which is the whole proof:
            // a depth test that was being lost would not care how many samples it was being lost at.
            //
            // The indicator card keeps its own — see build-pcb.py, which is why they are two meshes. It is
            // 95 mm across, the film looks at it from 150, and there the same bar is five pixels and holds
            // still. This is the one part of the board the film ever gets close enough to read.
            foreach (var node in board.Descendants)
            {
                if (node is MeshNode { Name: "silkscreen" } printing)
                    printing.IsVisible = false;
            }

            board.Position = BoardAt;
            board.RotationDegrees = new Vector3(0f, BoardYaw, 0f);
            board.Scale = new Vector3(Actual);
            root.Children.Add(board);

            _card = Card(board);
            _cardHome = _card?.Position ?? Vector3.Zero;

            // The parts the board is missing, which the model has no opinion about: a socket is a part and
            // a processor is not one, because a processor is what you put in it, and an empty memory slot
            // is a slot. All four hang off the board, so their travel is written in the board's own
            // coordinates and inherits the yaw and the scale of the bench without either being mentioned.
            _cpu = Chip();
            _cooler = Cooler(out _rotor);
            _memory[0] = Module(0);
            _memory[1] = Module(1);

            board.Children.Add(_cpu);
            board.Children.Add(_cooler);
            board.Children.Add(_memory[0]!);
            board.Children.Add(_memory[1]!);

            Seat(0f, 0f, 0f);
            Plug(0f);
            Live(0f);
            Fan(0f, 0f);
        }
    }

    /// <summary>The two high bays over the length of the room. Both go out when he reaches the bench.</summary>
    public Lamp[] High { get; }

    /// <summary>The work light over the bench, on from the first frame — it is what he walks towards.</summary>
    public Lamp Bay { get; }

    /// <summary>The lamp on its arm, thirty centimetres off the board, and the only one still burning during
    /// the power-up.</summary>
    public Lamp Task { get; }

    /// <summary>The one on the display.</summary>
    public Lamp Fill { get; }

    /// <summary>The powered door in the north wall. Shut for the whole of chapter 6; chapter 7 is what
    /// opens it.</summary>
    public Door Way { get; private set; } = null!;

    /// <summary>The board as a drawing. First on the screen.</summary>
    public DraftsmanScene Draft { get; }

    /// <summary>The same model as every edge it has. Second.</summary>
    public WireframeScene Mesh { get; }

    /// <summary>The board named part by part, with the copper lit. Third, and it is what tells him what is
    /// wrong.</summary>
    public InspectorScene Probe { get; }

    /// <summary>The board on the bench, and the six lamps on the card he fits to it.</summary>
    public IndicatorsScene Panel { get; }

    /// <summary>A point on the way in, on the corridor's centre line, at eye height.</summary>
    public static Vector3 Ahead(float metres) => Deck.Engine + new Vector3(0f, Deck.Eye, metres);

    /// <summary>Standing at the bench: <paramref name="x"/> along it, <paramref name="back"/> off the far
    /// wall, at whatever height a person is looking from.</summary>
    public static Vector3 At(float x, float back, float eye = Deck.Eye) =>
        Deck.Engine + new Vector3(x, eye, Depth - back);

    /// <summary>The middle of the display's glass.</summary>
    public static Vector3 Display => Deck.Engine + ScreenAt;

    /// <summary>The middle of the board on the bench.</summary>
    public static Vector3 Board => Deck.Engine + BoardAt;

    /// <summary>
    /// The indicator card once it is fitted, which is what the last twenty seconds of the chapter are
    /// looking at.
    ///
    /// Its middle is a hundred and ninety-three millimetres past the board's own centre line in the model,
    /// which the bench's scale turns into twenty-nine — so the six lamps end up just off the board's
    /// right-hand edge and about a hand's width across, and the camera has to come to within a quarter of a
    /// metre of them. It does. That is what looking at six small lights is.
    /// </summary>
    public static Vector3 Lamps =>
        Deck.Engine + BoardAt + Turn(new Vector3(1.93f, 0.06f, -0.08f) * Actual, BoardYaw);

    /// <summary>The processor socket, which is what the first two fittings land in.</summary>
    public static Vector3 Socket =>
        Deck.Engine + BoardAt + Turn(new Vector3(-0.48f, 0.12f, -0.585f) * Actual, BoardYaw);

    /// <summary>The two memory slots that get filled, between them.</summary>
    public static Vector3 Slots =>
        Deck.Engine + BoardAt + Turn(new Vector3(0.54f, 0.16f, -0.407f) * Actual, BoardYaw);

    /// <summary>The middle of the bench: the board and the parts laid out beside it, in one shot.</summary>
    public static Vector3 Laid =>
        Deck.Engine + BoardAt + Turn(new Vector3(-1.1f, 0.08f, -0.3f) * Actual, BoardYaw);

    /// <summary>Where the card is lying before he fits it.</summary>
    public static Vector3 Spare =>
        Deck.Engine + BoardAt + Turn((new Vector3(1.93f, 0.06f, -0.08f) + Loose) * Actual, BoardYaw);

    /// <summary>The three chassis on the bench, for the shot he arrives on.</summary>
    public static Vector3 Servers => Deck.Engine + new Vector3(3.72f, Top + 0.16f, 12.2f);

    /// <summary>Down the room, at the housings.</summary>
    public static Vector3 Engines => Deck.Engine + new Vector3(9f, 2.1f, 7.5f);

    /// <summary>The roof beams, for the one look upward the film takes.</summary>
    public static Vector3 Roofline => Deck.Engine + new Vector3(7.5f, Height - 0.5f, 4.5f);

    /// <summary>The middle of the way out, for the walk that leaves.</summary>
    public static Vector3 Exit =>
        Deck.Engine + new Vector3(Doorway, 1.45f, Depth + Thickness / 2f);

    /// <summary>
    /// The drawing on the display: the paper, its folds and its copper, together.
    ///
    /// It is the one exhibit in the building that needs no lamp pointed at it. The fill is
    /// <see cref="Material.Unlit"/> — base colour, emitted exactly, with no shading term — so a metre of
    /// drawing is perfectly legible on a screen in a room whose four light slots are all somewhere else.
    /// Chapter 4 spends eighty seconds arguing that; this is a service terminal running on none of the
    /// budget.
    ///
    /// The paper is taken well down from the white the standalone scene prints it on. At 0.965 a metre of
    /// unlit white in a dark hangar is not a drawing, it is a window, and the tone mapping takes the rest
    /// of the frame down to meet it.
    /// </summary>
    public void Print(float level)
    {
        level = Math.Clamp(level, 0f, 1f);

        if (_paper is not null)
            _paper.BaseColor = new Vector4(new Vector3(0.62f, 0.63f, 0.62f) * level, 1f);

        if (_folds is not null)
            _folds.Opacity = 0.92f * level;

        if (_copper is not null)
            _copper.Opacity = 0.75f * level;
    }

    /// <summary>
    /// The wireframe over the drawing, and the one node in the building that has to be switched off rather
    /// than dimmed.
    ///
    /// It carries <see cref="LineNode.DepthTest"/> false, which is right on a black background and is a
    /// liability in a room: a line node that does not test depth is drawn over whatever is in front of it,
    /// including the bench, the board and the wall. So it exists for six seconds, while he is standing
    /// square to the display with nothing between him and it, and the rest of the time it is not in the
    /// scene at all. That is a real constraint on where a subject like this can be hung, and it is worth
    /// meeting honestly rather than by turning the depth test on and losing the density that is the whole
    /// point of the scene.
    /// </summary>
    public void Grid(float level)
    {
        level = Math.Clamp(level, 0f, 1f);

        if (_edges is null || _wireframe is null)
            return;

        _edges.Opacity = 0.34f * level;
        _wireframe.IsVisible = level > 0.002f;
    }

    /// <summary>
    /// The third view: the board with its parts named and its copper lit.
    ///
    /// It is a shaded model on a screen rather than a drawing, and it reads as one because almost
    /// everything the scene changes is emission — the tint on a selected part carries an emissive term and
    /// the copper is an additive line node — so the selection is bright whatever the room is doing to the
    /// rest of it.
    /// </summary>
    public void Schematic(bool on)
    {
        if (_schematic is not null)
            _schematic.IsVisible = on;
    }

    /// <summary>How lit the screen is: the glass behind the view and the window furniture around it.</summary>
    public void Backlight(float level)
    {
        level = Math.Clamp(level, 0f, 1f);

        // The bars are dimmer than what is written on them, which is the way round every window ever
        // drawn has it and is also the fix for a title bar that came out as a band of clipped white with
        // nothing legible in it. Additive blending cannot darken, so the only way to have furniture read
        // against its own background is for the background to be the quiet one.
        Paint(_glass, new Vector3(0.30f, 0.52f, 0.72f), level * 0.09f);
        Paint(_chrome, new Vector3(0.46f, 0.68f, 0.86f), level * 0.10f);
        Paint(_ink, new Vector3(0.72f, 0.86f, 1f), level * 0.30f);
        Paint(_accent, new Vector3(0.42f, 1f, 0.62f), level * 0.55f);
    }

    /// <summary>
    /// The machinery: the racks by the door ticking over, and the housings coming up.
    ///
    /// Not one slot between them. Every dot on every rack and every metre of coolant channel is emission
    /// with additive blending and no depth write, which is what lets a room this size be visibly running
    /// while all four lights in the building are inside a metre of a bench at the far end of it.
    /// </summary>
    /// <param name="clock">Film seconds, so the scan does not restart at a chapter boundary.</param>
    /// <param name="standby">Zero to one — the racks, which are on from the first frame.</param>
    /// <param name="running">Zero to one — the housings, which are not on until the board says so.</param>
    public void Machines(float clock, float standby, float running)
    {
        standby = Math.Clamp(standby, 0f, 1f);
        running = Math.Clamp(running, 0f, 1f);

        for (var i = 0; i < _rack.Length; i++)
        {
            // A slow scan down each column and a floor under it, so a rack reads as a machine idling
            // rather than as a grid of lamps. The colour goes from amber to green with the power-up, which
            // is the same thing the powered door's pilots say and is said here by thirty-six dots at once.
            var phase = Fraction(clock * 0.31f - i / (float)_rack.Length);
            var value = 0.18f + 0.82f * MathF.Pow(1f - phase, 4f);

            var colour = Vector3.Lerp(
                new Vector3(1f, 0.62f, 0.16f), new Vector3(0.30f, 1f, 0.48f), running);

            Paint(_rack[i], colour, standby * value * 0.5f);
        }

        for (var i = 0; i < _coolant.Length; i++)
        {
            // A pulse along the channels once the engines are up, and a flat trickle before that.
            //
            // The trickle is the shot he walks into. Seventeen metres of black room with two black
            // cylinders in it is a room with nothing in it; two nine-metre lines of cold blue running away
            // from the door is a machine hall, and the difference is one number that costs no light slot.
            // It is also true — a machine on standby is not a machine that is off.
            var wave = 0.5f + 0.5f * MathF.Sin((clock * 0.62f - i * 0.35f) * MathF.Tau);

            Paint(_coolant[i], new Vector3(0.36f, 0.74f, 1f),
                standby * 0.09f + running * (0.20f + 0.80f * wave) * 0.42f);
        }

        Paint(_strip, new Vector3(0.55f, 0.72f, 0.95f), 0.20f + 0.22f * standby);
    }

    /// <summary>
    /// Fitting the card: from where it is lying on the bench into its place on the board.
    ///
    /// It is the only thing that happens in this film because somebody did it. There is no body in the
    /// picture and there never has been — nine minutes of first person with nothing in frame but rooms — so
    /// the card travels on its own, and at a quarter of a metre from the camera that reads as a pair of
    /// hands rather than as telekinesis, which is the whole of what the distance is for.
    ///
    /// <b>It never leaves the bench and it never turns.</b> One axis, one straight line, flat on the mat,
    /// right to left into the connector on the end of the ribbon — because that is what plugging something
    /// in is. It travelled over an arc for one round of this, lifted a little and set down, and the arc is
    /// exactly what gave it away: an arc is a part being carried, and a mating connector cannot be carried,
    /// it has to be pushed along the axis its contacts are cut on. Everything about the animation that was
    /// not that motion was noise, so there is nothing here now but a lerp.
    /// </summary>
    /// <summary>
    /// The build: the processor into its socket, the cooler onto the processor, and two modules into the
    /// slots nearest it. In that order, because that is the order, and a film that got it wrong would be
    /// telling everybody who has ever done this that it had not.
    ///
    /// All three are picked up, carried across at height and set straight down — see <see cref="Place"/>,
    /// where the shape of that path is. The card at the end of the chapter is the one part that does not
    /// move like this, and the difference is the point: three things drop into place from above and one
    /// slides in from the side, because that is what their connectors are.
    ///
    /// The two modules are staggered inside one number rather than given two. Channel A first and then
    /// channel B, overlapping by a fifth, so the second is picked up while the first is still going down —
    /// which is what somebody with two hands and four slots does and is a great deal shorter than two
    /// complete journeys end to end.
    /// </summary>
    /// <param name="processor">Zero on the bench, one in the socket.</param>
    /// <param name="cooler">Zero on the bench, one on the processor.</param>
    /// <param name="memory">Zero on the bench, one both modules seated.</param>
    public void Seat(float processor, float cooler, float memory)
    {
        // Half a unit of clearance, which at this mount is seventy-five millimetres. It was a whole unit
        // for the cooler and the cooler left the top of the frame: the camera is four hundred millimetres
        // from the socket for this beat, and at that distance a part lifted a hundred and sixty
        // millimetres is not being carried over the board, it is being flown over it.
        Place(_cpu, ChipLoose, ChipHome, processor, 0.42f);
        Place(_cooler, CoolerLoose, CoolerHome, cooler, 0.55f);

        for (var i = 0; i < _memory.Length; i++)
            Place(_memory[i], ModuleLoose(i), ModuleHome(i),
                Span(memory, i * 0.4f, 0.6f + i * 0.4f), 0.5f, roll: 90f);
    }

    /// <summary>
    /// One part, from where it is lying to where it belongs: up, across at height, and straight down.
    ///
    /// Three phases rather than a straight line between two points, because a straight line between a
    /// bench and a socket goes <i>through</i> the board — and because the last centimetre of fitting
    /// anything is vertical. Everything a person does with a component has that shape: pick it up, take it
    /// over, lower it in. The lift is a trapezoid rather than an arc, so the descent is dead straight down
    /// the socket's own axis for the whole of the last fifth of the move.
    ///
    /// <paramref name="roll"/> is for the memory, which is lying flat on the bench and has to be standing
    /// on edge by the time it arrives. It turns while it is in the air and is upright before it starts
    /// down, which is the same rule again: the part is square to what it is going into before any of it is
    /// inside.
    /// </summary>
    private static void Place(Node? part, Vector3 loose, Vector3 home, float u, float clear, float roll = 0f)
    {
        if (part is null)
            return;

        u = Math.Clamp(u, 0f, 1f);

        var across = Smooth(Span(u, 0.18f, 0.82f));
        var lift = MathF.Min(Smooth(Span(u, 0f, 0.18f)), Smooth(Span(1f - u, 0f, 0.18f)));

        part.Position = Vector3.Lerp(loose, home, across) + new Vector3(0f, clear * lift, 0f);

        if (roll != 0f)
            part.RotationDegrees = new Vector3(0f, 0f, roll * (1f - Smooth(Span(u, 0.12f, 0.62f))));
    }

    /// <summary>
    /// The fan, spinning up, and the one moving thing in the film whose angle is not a straight multiple
    /// of the clock.
    ///
    /// A fan coming up to speed is an <i>acceleration</i>, so the rate is proportional to how long the
    /// power has been on and the angle is therefore proportional to the square of it. That is the whole
    /// of the method, and writing it closed-form rather than integrating a rate per frame is what keeps
    /// it seekable: dropping into the last five seconds of this chapter has to show a fan at exactly the
    /// speed and exactly the angle the film would have shown, and a counter that had been adding up since
    /// the power came on would show neither.
    ///
    /// It ends the chapter at about fourteen hundred degrees a second, which at any frame rate this runs
    /// at is well past the point where the eye stops resolving blades and starts seeing a disc — and on
    /// the way there it goes through every stroboscopic speed in between, backwards and forwards, because
    /// that is what a sampled wheel does and it is one of the few artefacts worth keeping.
    /// </summary>
    /// <param name="since">Seconds since the power came on. Zero or less and it does not turn.</param>
    /// <param name="level">How bright the ring is, 0 to 1.</param>
    public void Fan(float since, float level)
    {
        since = MathF.Max(since, 0f);
        level = Math.Clamp(level, 0f, 1f);

        // Forty degrees a second the moment it has power, and sixty a second faster for every second
        // after that: angle is the integral, which is the linear term plus half the quadratic one. The
        // constant is what makes it visibly turning in the first half-second rather than creeping — a fan
        // that starts from nothing takes three seconds to look like it started at all.
        if (_rotor is not null)
            _rotor.RotationDegrees = new Vector3(0f, 0f, -(40f * since + 30f * since * since));

        // And the ring's twelve arcs, a twelfth of the hue circle apart, the whole wheel turning once
        // every four seconds. It is exactly the effect every fan sold in the last ten years has on it and
        // it is twelve emissive materials with one number between them.
        for (var i = 0; i < _ring.Length; i++)
        {
            if (_ring[i] is null)
                continue;

            Paint(_ring[i], Hue(since * 0.25f + i / (float)_ring.Length), level * 0.5f);
        }

        // And the blades themselves, on the same wheel. They keep their own dark base colour and take the
        // hue as emission on top of it, so an unpowered fan is a black rotor and a powered one is nine
        // coloured vanes — which is what the reference is, and is the difference between a ring of light
        // stuck on a fan and a fan that is lit.
        for (var i = 0; i < _blades.Length; i++)
        {
            if (_blades[i] is null)
                continue;

            _blades[i].EmissiveColor = Hue(since * 0.25f + i / (float)_blades.Length) * (level * 0.46f);
        }

        // And the memory, on the same wheel and off the same clock. It is here rather than in Live for the
        // reason it is true on a real machine: one controller drives everything in the case, so the sticks
        // and the fan are the same colour at the same moment and it is obvious that they are. Eight
        // segments a stick over two thirds of the circle, which is the gradient down a module the
        // reference photographs have — a whole circle over 133 mm reads as a row of coloured blocks.
        for (var i = 0; i < _bars.Length; i++)
        {
            if (_bars[i] is null)
                continue;

            Paint(_bars[i], Hue(since * 0.25f + i % Segments / 12f), level * 0.55f);
        }
    }

    /// <summary>
    /// A colour at <paramref name="turn"/> round the hue circle, full saturation and full value.
    ///
    /// Six lines rather than a conversion out of a library, because at full saturation the whole of HSV is
    /// three ramps a third of a turn apart — which is also the clearest possible statement of what a hue
    /// <i>is</i>.
    /// </summary>
    private static Vector3 Hue(float turn) =>
        new(Ramp(turn), Ramp(turn - 1f / 3f), Ramp(turn - 2f / 3f));

    private static float Ramp(float turn) =>
        Math.Clamp(MathF.Abs(Fraction(turn) * 6f - 3f) - 1f, 0f, 1f);

    /// <summary>
    /// The light in the parts: the line across the processor and the collar round the fan's hub, on one
    /// material and all of it emission. The fan's ring and the memory's diffusers are the other half of
    /// it and they run off a clock rather than a level — see <see cref="Fan"/>.
    ///
    /// It comes on with the power and it is the only thing in the assembly that says the machine is alive
    /// rather than merely finished. Not one light slot between them — the three the chapter has left are
    /// spent on the indicator card, which is the point being made — so a board that lights up costs
    /// exactly as much as a board that does not.
    /// </summary>
    public void Live(float level) =>
        Paint(_live, new Vector3(0.34f, 0.76f, 1f), Math.Clamp(level, 0f, 1f) * 0.42f);

    /// <param name="home">Zero lying loose, one fitted.</param>
    public void Plug(float home)
    {
        if (_card is null)
            return;

        _card.Position = _cardHome + Loose * (1f - Math.Clamp(home, 0f, 1f));
    }

    /// <summary>
    /// One of the three views, standing in front of the display's glass at a third of full size.
    ///
    /// A pitch of ninety stands the model up and a yaw of a hundred and eighty turns it to face him, and
    /// the yaw is not optional. Without it the model's own +X points along the room's +X, which is the
    /// visitor's <i>left</i> — he is looking up the deck's +Z and this is a right-handed world — so the
    /// whole board comes out mirrored. On the drawing nothing would have given it away, because a drawing
    /// is geometry and the silkscreen is a texture; on the schematic every designator was written
    /// backwards, which is a thing you see instantly and attribute to anything but a sign.
    /// </summary>
    private static void View(Node root, Node subject, float proud)
    {
        subject.Position = ScreenAt + new Vector3(ViewShift, 0f, -proud);
        subject.RotationDegrees = new Vector3(90f, 180f, 0f);
        subject.Scale = new Vector3(ViewScale);

        root.Children.Add(subject);
    }

    /// <summary>
    /// The indicator card and everything on it, gathered so it can be fitted as one piece.
    ///
    /// It is a whole board of its own in the model — its own laminate, its own pads, its own copper and six
    /// lamps standing on it — and the flex jumper that reaches it is deliberately <i>not</i> part of the
    /// group. The cable stays where it is, run and waiting, and the card comes onto the end of it. Bringing
    /// the ribbon along would make this a subassembly being lowered into place; leaving it puts the whole
    /// of the action in a connector, which is what fitting a card is.
    ///
    /// The glows count as part of the card and are gathered by name — see
    /// <see cref="IndicatorsScene.MountOn"/>, which names them after the lamp they belong to for this
    /// reason. A bloom that stayed behind would be two bright dots hanging over an empty bench.
    /// </summary>
    private static Node? Card(Node board)
    {
        // The loader hangs all six hundred nodes directly under the file's root, so the card's pieces are
        // the board's own children and re-parenting them changes no transform.
        var carried = board.Children
            .Where(node => node.Name is { } name &&
                           (name.StartsWith("panel", StringComparison.Ordinal) ||
                            name.StartsWith("led.", StringComparison.Ordinal)))
            .ToList();

        if (carried.Count == 0)
            return null;

        var card = new Node { Name = "card" };

        foreach (var node in carried)
            card.Children.Add(node);

        board.Children.Add(card);

        return card;
    }

    /// <summary>Floor, ceiling and four walls, with the way in through the thick one.</summary>
    private void Shell(Node root, Material plate, Material deck)
    {
        var middle = (West + East) / 2f;
        var width = East - West;
        var run = Depth - Apron;
        var at = (Apron + Depth) / 2f;

        root.Children.Add(new MeshNode(
            Fabric.Map(Primitives.Plane(width, run), deck, new Vector3(middle, 0f, at)), deck)
        {
            Position = new Vector3(middle, 0f, at),
            Name = "floor"
        });

        root.Children.Add(new MeshNode(
            Fabric.Map(Primitives.Plane(width, run), plate, new Vector3(middle, Height, at)), plate)
        {
            Position = new Vector3(middle, Height, at),
            RotationDegrees = new Vector3(180f, 0f, 0f),
            Name = "ceiling"
        });

        // The way in. PiercedWall measures its opening from its own middle, and this wall's middle is four
        // metres east of the door — so the offset is negative and is the room's own arithmetic rather than
        // a number anybody chose.
        var south = Fabric.PiercedWall(
            width + 2f * Thickness, Height, Bulkhead,
            doorCentre: -middle, Deck.DoorWidth, Deck.DoorHeight, plate);

        south.Position = new Vector3(middle, 0f, -Bulkhead / 2f);
        root.Children.Add(south);

        // The way out, and it is the fourth door in the building and the second powered one. It is at the
        // far end of the north wall from the bench, which is what rule 2 costs here: the way in is at x
        // nought and this is at five metres nine, so no two doorways in this room are on a line and the
        // gallery beyond it cannot be seen from the bulkhead he came in by.
        // The hole is fifty millimetres wider and thirty taller than the standard one, and this is the wall
        // that gets the bigger one because it is the wall that is buried: the gallery's back wall is six
        // hundred millimetres thick and swallows this one whole — see Deck.Illuminator — so nothing of this
        // opening is ever seen and the reveal anybody walks through is the gallery's.
        //
        // Without it the two walls cut the same hole in the same place, which puts two reveals and two
        // soffits at one depth apiece inside the overlap, and the depth test decides per pixel and per
        // frame. See ScreenRoom's east wall, which is the same fault at the other end of the ship and where
        // this was found.
        var north = Fabric.PiercedWall(
            width + 2f * Thickness, Height, Thickness,
            doorCentre: Doorway - middle, Deck.DoorWidth + 0.05f, Deck.DoorHeight + 0.03f, plate);

        north.Position = new Vector3(middle, 0f, Depth + Thickness / 2f);
        root.Children.Add(north);

        Way = Door.Powered(new Vector3(Doorway, 0f, Depth + Thickness / 2f));
        Way.Open(0f);
        root.Children.Add(Way.Root);

        foreach (var side in new[] { West - Thickness / 2f, East + Thickness / 2f })
        {
            var wall = Fabric.Wall(run + 2f * Thickness, Height, Thickness, plate);
            wall.Position = new Vector3(side, 0f, at);
            wall.RotationDegrees = new Vector3(0f, 90f, 0f);
            root.Children.Add(wall);
        }
    }

    /// <summary>
    /// Five beams across the ceiling and two pipe runs along it.
    ///
    /// The film looks up exactly once, at nine seconds into this chapter, and this is what it is looking
    /// at. Every ceiling before this one has been three metres two of flat plaster and there was nothing up
    /// there worth the shot; a bare ceiling at six and a half is the same picture with more of it. What
    /// makes a room tall is having something overhead to be under.
    /// </summary>
    private void Roof(Node root)
    {
        var steel = Steel();
        var middle = (West + East) / 2f;
        var width = East - West;

        for (var z = 1.4f; z < Depth; z += 2.4f)
        {
            root.Children.Add(Fabric.Slab(
                new Vector3(width, 0.34f, 0.16f),
                new Vector3(middle, Height - 0.2f, z),
                steel,
                "beam"));

            root.Children.Add(Fabric.Slab(
                new Vector3(width, 0.08f, 0.42f),
                new Vector3(middle, Height - 0.38f, z),
                steel,
                "flange"));
        }

        foreach (var x in new[] { middle - 3.4f, middle + 3.1f })
            root.Children.Add(new MeshNode(
                Primitives.Cylinder(0.13f, 0.13f, Depth - 0.6f, 10), steel)
            {
                Position = new Vector3(x, Height - 0.72f, Depth / 2f),
                RotationDegrees = new Vector3(90f, 0f, 0f),
                Name = "pipe"
            });
    }

    /// <summary>
    /// The walkway down the east side, three metres four up, with a lit edge under it.
    ///
    /// It is what puts a second storey in the picture, and a second storey is most of what makes six and a
    /// half metres read as six and a half rather than as a tall room. The strip under it is emission, so
    /// the gantry is legible from the door with no lamp anywhere near it.
    /// </summary>
    private void Gantry(Node root)
    {
        var steel = Steel();
        const float y = 3.4f;
        const float from = 0.8f;
        const float to = Depth - 0.6f;
        const float x = East - 1.1f;

        root.Children.Add(Fabric.Slab(
            new Vector3(2f, 0.10f, to - from), new Vector3(x, y, (from + to) / 2f), steel, "walkway"));

        root.Children.Add(Fabric.Slab(
            new Vector3(0.08f, 0.9f, to - from), new Vector3(x - 0.96f, y + 0.55f, (from + to) / 2f),
            steel, "rail"));

        for (var z = from; z <= to; z += 1.9f)
        {
            root.Children.Add(Fabric.Slab(
                new Vector3(0.10f, 0.95f, 0.10f), new Vector3(x - 0.96f, y + 0.52f, z), steel, "stanchion"));

            root.Children.Add(Fabric.Slab(
                new Vector3(0.14f, y, 0.14f), new Vector3(x + 0.9f, y / 2f, z), steel, "leg"));
        }

        root.Children.Add(Fabric.Slab(
            new Vector3(0.05f, 0.05f, to - from), new Vector3(x - 1.02f, y - 0.07f, (from + to) / 2f),
            _strip, "edge"));
    }

    /// <summary>
    /// Two engine housings down the east side: a barrel, three collars, a bell and two coolant channels.
    ///
    /// Cylinders built about +Y and laid over onto +Z, which is how they get to run away from him in
    /// perspective — the one thing a room this shape can do that no exhibition room in the building could.
    /// The channels are the only part of them that is ever bright, and none of it is lit.
    /// </summary>
    private void Housings(Node root)
    {
        var steel = Steel();

        var casing = new Material
        {
            BaseColor = new Vector4(0.135f, 0.140f, 0.155f, 1f),
            Metallic = 0.55f,
            Roughness = 0.44f,
            Name = "housing"
        };

        for (var i = 0; i < _coolant.Length; i++)
            _coolant[i] = Glow(0.36f, 0.74f, 1f);

        var barrel = 0;

        foreach (var x in new[] { 8.2f, 11f })
        {
            const float radius = 1.25f;
            const float length = 9.4f;
            const float axis = 2.05f;
            const float centre = 6.6f;

            root.Children.Add(new MeshNode(Primitives.Cylinder(radius, radius, length, 22), casing)
            {
                Position = new Vector3(x, axis, centre),
                RotationDegrees = new Vector3(90f, 0f, 0f),
                Name = "housing"
            });

            foreach (var z in new[] { centre - 3.4f, centre, centre + 3.4f })
                root.Children.Add(new MeshNode(
                    Primitives.Cylinder(radius + 0.16f, radius + 0.16f, 0.34f, 22), steel)
                {
                    Position = new Vector3(x, axis, z),
                    RotationDegrees = new Vector3(90f, 0f, 0f),
                    Name = "collar"
                });

            // The bell at the north end. Cylinder puts its first radius at the +Y end and a rotation of
            // ninety about X sends +Y to +Z, so the wide end comes out pointing away up the room. Minus
            // ninety builds the same cone facing him, which is a funnel.
            root.Children.Add(new MeshNode(
                Primitives.Cylinder(radius + 0.55f, radius * 0.9f, 1.5f, 22), steel)
            {
                Position = new Vector3(x, axis, centre + length / 2f + 0.7f),
                RotationDegrees = new Vector3(90f, 0f, 0f),
                Name = "bell"
            });

            foreach (var z in new[] { centre - 3.4f, centre + 3.4f })
                root.Children.Add(Fabric.Slab(
                    new Vector3(0.7f, axis - radius, 1.1f),
                    new Vector3(x, (axis - radius) / 2f, z),
                    steel,
                    "mount"));

            // Two channels a housing, on the shoulders that can be seen from the middle of the room. They
            // are straight boxes lying along the barrel rather than curved bands, because at nine metres
            // long and a hand wide the difference is below a pixel and a swept band is a mesh.
            foreach (var side in new[] { -1f, 1f })
            {
                var channel = _coolant[barrel++ % _coolant.Length];

                root.Children.Add(Fabric.Slab(
                    new Vector3(0.10f, 0.10f, length - 1.2f),
                    new Vector3(x + side * radius * 0.72f, axis + radius * 0.70f, centre),
                    channel,
                    "coolant"));
            }
        }
    }

    /// <summary>
    /// Three cabinets by the door, each with a grid of indicators on its face.
    ///
    /// They are the first thing in shot and the last thing to change, and both are on purpose. He comes
    /// through a bulkhead into a room that is black except for two rows of amber dots ticking over, which
    /// is how a room says it is running without a lamp in it; and when the board comes up they go green,
    /// all thirty-six at once, which is how a room says what the board just said.
    /// </summary>
    private void Cabinets(Node root)
    {
        var steel = Steel();

        for (var i = 0; i < _rack.Length; i++)
            _rack[i] = Glow(1f, 0.62f, 0.16f);

        foreach (var x in Racks)
        {
            const float w = 2f;
            const float h = 2.6f;
            const float z = 0.42f;

            root.Children.Add(Fabric.Slab(new Vector3(w, h, 0.7f), new Vector3(x, h / 2f, z), steel, "rack"));

            root.Children.Add(Fabric.Slab(
                new Vector3(w - 0.16f, h - 0.5f, 0.06f),
                new Vector3(x, h / 2f + 0.06f, z - 0.37f),
                new Material
                {
                    BaseColor = new Vector4(0.075f, 0.078f, 0.085f, 1f),
                    Roughness = 0.38f,
                    Name = "rack.face"
                },
                "face"));

            for (var i = 0; i < Dots; i++)
            {
                var column = i % 4;
                var row = i / 4;

                root.Children.Add(Fabric.Slab(
                    new Vector3(0.30f, 0.05f, 0.03f),
                    new Vector3(x - 0.66f + column * 0.44f, 0.85f + row * 0.55f, z - 0.41f),
                    _rack[(row * 4 + column) % _rack.Length],
                    "indicator"));
            }
        }
    }

    /// <summary>
    /// The service bench: a top on two frames, three server chassis standing on it with the middle one
    /// open, and the display at the back.
    ///
    /// The chassis are why there is a board on the bench at all. A motherboard lying on a table in an
    /// empty room is a still life; a motherboard lying in front of the machine it came out of, with the lid
    /// off and the bay it left visibly empty, is a repair in progress and needs no caption.
    /// </summary>
    private void Furniture(Node root)
    {
        var steel = Steel();

        var bench = new Node { Name = "bench", Position = BenchAt };

        // A dark mat over a steel top, and it is a fix as much as it is a fitting. The lamp on its arm is
        // eighty centimetres above this and the board is four hundred and fifty millimetres across, so a
        // painted steel bench renders as a sheet of white with a small dark object on it — the exposure
        // the eye settles on is the bench's, and the subject loses. An anti-static mat is what a bench you
        // put a board down on actually has, and it is a fifth of the reflectance.
        var mat = new Material
        {
            BaseColor = new Vector4(0.022f, 0.024f, 0.028f, 1f),
            Roughness = 0.82f,
            Name = "bench.mat"
        };

        bench.Children.Add(Fabric.Slab(
            new Vector3(BenchLong, 0.06f, BenchDeep), new Vector3(0f, Top - 0.03f, 0f),
            steel, "top", Finish.Close));

        bench.Children.Add(Fabric.Slab(
            new Vector3(BenchLong - 0.3f, 0.008f, BenchDeep - 0.12f), new Vector3(0f, Top + 0.004f, 0f),
            mat, "mat", Finish.Close));

        bench.Children.Add(Fabric.Slab(
            new Vector3(BenchLong - 0.5f, 0.04f, BenchDeep - 0.3f), new Vector3(0f, 0.22f, 0f),
            steel, "shelf", Finish.Close));

        foreach (var x in new[] { -BenchLong / 2f + 0.24f, BenchLong / 2f - 0.24f })
        foreach (var z in new[] { -BenchDeep / 2f + 0.18f, BenchDeep / 2f - 0.18f })
            bench.Children.Add(new MeshNode(Primitives.Cylinder(0.035f, 0.035f, Top - 0.06f, 8), steel)
            {
                Position = new Vector3(x, (Top - 0.06f) / 2f, z),
                Name = "leg"
            });

        root.Children.Add(bench);

        // The three machines. Two shut, one with its lid off and the board out of it — the middle one, so
        // the bay it came from is between two that are still closed and the difference is a comparison
        // rather than a claim.
        for (var i = 0; i < Chassis.Length; i++)
            root.Children.Add(Server(steel, Chassis[i], open: i == 1));

        Screen(root, steel);
    }

    /// <summary>One rack unit standing on the bench: a case, a vented face, and a lid unless it is open.</summary>
    private Node Server(Material steel, float x, bool open)
    {
        const float w = 0.72f;
        const float h = 0.30f;
        const float d = 0.78f;

        var unit = new Node
        {
            Name = open ? "server.open" : "server",
            Position = new Vector3(x, Top, 12.3f),
            RotationDegrees = new Vector3(0f, open ? 6f : 0f, 0f)
        };

        var shell = new Material
        {
            BaseColor = new Vector4(0.105f, 0.112f, 0.125f, 1f),
            Metallic = 0.45f,
            Roughness = 0.40f,
            Name = "server"
        };

        var dark = new Material
        {
            BaseColor = new Vector4(0.065f, 0.068f, 0.075f, 1f),
            Roughness = 0.42f,
            Name = "server.dark"
        };

        // The tub: floor and four sides. An open unit is a box with nothing on top of it, which is only
        // read as open if there is a visible inside to be looking into.
        unit.Children.Add(Fabric.Slab(new Vector3(w, 0.02f, d), new Vector3(0f, 0.01f, 0f), steel, "pan",
            Finish.Close));

        foreach (var side in new[] { -1f, 1f })
            unit.Children.Add(Fabric.Slab(
                new Vector3(0.02f, h, d), new Vector3(side * (w / 2f - 0.01f), h / 2f, 0f), shell, "side",
                Finish.Close));

        unit.Children.Add(Fabric.Slab(
            new Vector3(w, h, 0.02f), new Vector3(0f, h / 2f, d / 2f - 0.01f), shell, "back", Finish.Close));

        unit.Children.Add(Fabric.Slab(
            new Vector3(w, h, 0.03f), new Vector3(0f, h / 2f, -d / 2f + 0.015f), dark, "face", Finish.Close));

        // Vents and a pair of drive bays on the face, which is what the front of one of these is.
        for (var i = 0; i < 7; i++)
            unit.Children.Add(Fabric.Slab(
                new Vector3(0.03f, h - 0.10f, 0.01f),
                new Vector3(-0.30f + i * 0.035f, h / 2f, -d / 2f + 0.032f), steel, "vent", Finish.Close));

        foreach (var y in new[] { 0.09f, 0.21f })
            unit.Children.Add(Fabric.Slab(
                new Vector3(0.30f, 0.09f, 0.012f), new Vector3(0.16f, y, -d / 2f + 0.032f), steel, "bay",
                Finish.Close));

        // Three lamps on the face of every machine that is still shut, sharing the racks' own materials —
        // so they tick over on the same scan and go green with everything else. They are the only light at
        // this end of the room on the first frame of the chapter, and that is what they are for: a bench
        // thirteen metres away has to say it is a bench somebody works at before he has crossed to it, and
        // a lamp pointed at it would spend a slot to say so.
        if (!open)
            for (var i = 0; i < 3; i++)
                unit.Children.Add(Fabric.Slab(
                    new Vector3(0.022f, 0.022f, 0.012f),
                    new Vector3(-0.32f + i * 0.05f, h - 0.055f, -d / 2f + 0.034f),
                    _rack[i * 2],
                    "pilot"));

        if (open)
        {
            // The lid, off and leaning against the back of the bench.
            unit.Children.Add(Fabric.Slab(
                new Vector3(w, 0.02f, d), new Vector3(0.02f, 0.42f, 0.36f), shell, "lid", Finish.Close));

            return unit;
        }

        unit.Children.Add(Fabric.Slab(
            new Vector3(w, 0.02f, d), new Vector3(0f, h - 0.01f, 0f), shell, "lid", Finish.Close));

        return unit;
    }

    /// <summary>
    /// The display: a foot, a stem, a bezel and a sheet of glass with a backlight behind it.
    ///
    /// The glass is emission and additive and writes no depth, like every other lit surface in this room —
    /// so a screen that is on costs nothing, which is the only reason a room with four lamps can afford a
    /// service terminal at all. What is drawn on it stands a few millimetres in front of it and is one of
    /// the three mounted subjects.
    /// </summary>
    private void Screen(Node root, Material steel)
    {
        var display = new Node { Name = "display", Position = ScreenAt };

        display.Children.Add(Fabric.Slab(
            new Vector3(ScreenWide + 0.10f, ScreenTall + 0.10f, 0.06f), new Vector3(0f, 0f, 0.04f),
            steel, "bezel", Finish.Close));

        display.Children.Add(Fabric.Slab(
            new Vector3(ScreenWide, ScreenTall, 0.01f), Vector3.Zero, _glass, "glass"));

        Chrome(display);

        display.Children.Add(new MeshNode(
            Primitives.Cylinder(0.05f, 0.05f, ScreenAt.Y - Top, 10), steel)
        {
            Position = new Vector3(0f, -(ScreenAt.Y - Top) / 2f - ScreenTall / 2f + 0.1f, 0.08f),
            Name = "stem"
        });

        display.Children.Add(Fabric.Slab(
            new Vector3(0.5f, 0.03f, 0.34f), new Vector3(0f, Top - ScreenAt.Y + 0.015f, 0.1f),
            steel, "foot", Finish.Close));

        root.Children.Add(display);
    }

    /// <summary>
    /// The window furniture: a title bar, three lamps at the corner of it, a status line and a rule.
    ///
    /// It is what makes the screen read as a program rather than as a lightbox with a board painted on it,
    /// and it is nine slabs. There is no text on any of them — this control draws no text and the film has
    /// been careful for nine minutes not to write words on things — so what carries it is the layout: a bar
    /// along the top of the glass, three small marks where the buttons of a window go, a row of short
    /// blocks where a title is, and a line of them along the bottom where a status is. A person reads that
    /// as an application in about a tenth of a second and could not tell you which one.
    ///
    /// All of it is emission and additive, like everything else in this room that is bright, so a screen
    /// that is on costs no light slot. It sits five millimetres in front of the glass and thirty behind
    /// the views, and outside their extent — a title bar with a board drawn over it is not a title bar.
    /// </summary>
    private void Chrome(Node display)
    {
        const float bar = 0.085f;
        const float edge = ScreenWide / 2f - 0.03f;
        var top = ScreenTall / 2f - 0.055f;
        var bottom = -ScreenTall / 2f + 0.05f;

        display.Children.Add(Fabric.Slab(
            new Vector3(ScreenWide - 0.06f, bar, 0.004f), new Vector3(0f, top, -0.006f), _chrome, "titlebar"));

        // Where the buttons of a window are. Three of them, and the leftmost is the one that is a different
        // colour on every window anybody has ever used.
        for (var i = 0; i < 3; i++)
            display.Children.Add(Fabric.Slab(
                new Vector3(0.022f, 0.022f, 0.004f),
                new Vector3(edge - 0.04f - i * 0.038f, top, -0.011f),
                i == 0 ? _accent : _ink,
                "button"));

        // A title, as five short blocks of the length words are.
        var at = -edge + 0.05f;

        foreach (var word in new[] { 0.10f, 0.055f, 0.13f, 0.04f, 0.08f })
        {
            display.Children.Add(Fabric.Slab(
                new Vector3(word, 0.020f, 0.004f), new Vector3(at + word / 2f, top, -0.011f), _ink, "word"));

            at += word + 0.026f;
        }

        display.Children.Add(Fabric.Slab(
            new Vector3(ScreenWide - 0.06f, 0.006f, 0.004f), new Vector3(0f, bottom + 0.055f, -0.006f),
            _chrome, "rule"));

        // And a status line: a lamp that says the tool is live, and three fields after it.
        display.Children.Add(Fabric.Slab(
            new Vector3(0.020f, 0.020f, 0.004f), new Vector3(-edge + 0.03f, bottom, -0.011f),
            _accent, "status"));

        at = -edge + 0.062f;

        foreach (var field in new[] { 0.16f, 0.09f, 0.22f })
        {
            display.Children.Add(Fabric.Slab(
                new Vector3(field, 0.016f, 0.004f), new Vector3(at + field / 2f, bottom, -0.011f),
                _ink, "field"));

            at += field + 0.05f;
        }
    }

    /// <summary>A rod from the ceiling down to a fixture, so the lamps are hung rather than floating.</summary>
    private static void Drop(Node root, Vector3 at)
    {
        var length = Height - at.Y;

        if (length < 0.05f)
            return;

        root.Children.Add(new MeshNode(Primitives.Cylinder(0.022f, 0.022f, length, 8), Steel())
        {
            Position = at + new Vector3(0f, length / 2f, 0f),
            Name = "drop"
        });
    }

    /// <summary>
    /// A post at the back of the bench with two arms off it, for the lamp that is eighty centimetres from
    /// the board rather than six metres above it.
    ///
    /// The post stands a metre to the side of the board rather than directly behind it, and that is worth
    /// a sentence because it was directly behind it first. The last shot of the chapter looks down at the
    /// board from just above it, and a vertical rod behind the subject in a downward shot does not read as
    /// behind — it reads as growing out of the middle of the thing you are looking at. A metre sideways
    /// puts it at the edge of the frame, which is where the stand of a bench lamp is.
    /// </summary>
    private static void Arm(Node root, Vector3 at)
    {
        var steel = Steel();
        var post = new Vector3(at.X + 1.05f, 0f, at.Z + 0.55f);
        var elbow = at.Y + 0.09f;

        root.Children.Add(new MeshNode(Primitives.Cylinder(0.026f, 0.026f, elbow, 8), steel)
        {
            Position = new Vector3(post.X, elbow / 2f, post.Z),
            Name = "post"
        });

        root.Children.Add(Fabric.Slab(
            new Vector3(0.045f, 0.045f, post.Z - at.Z),
            new Vector3(post.X, elbow, (post.Z + at.Z) / 2f),
            steel,
            "arm",
            Finish.Close));

        root.Children.Add(Fabric.Slab(
            new Vector3(post.X - at.X, 0.045f, 0.045f),
            new Vector3((post.X + at.X) / 2f, elbow, at.Z),
            steel,
            "arm",
            Finish.Close));
    }

    /// <summary>
    /// Painted steel, and the same argument the corridor's makes: a metal at nine tenths metallic has
    /// almost no diffuse term, and with nothing behind it to reflect it renders black under point lights.
    /// Structure that is meant to be seen is painted, and paint is a dielectric.
    /// </summary>
    private static Material Steel() => new()
    {
        BaseColor = new Vector4(0.135f, 0.145f, 0.160f, 1f),
        Metallic = 0.25f,
        Roughness = 0.55f,
        Name = "steel"
    };

    /// <summary>A surface that is added to the frame rather than lit in it. Costs no slot, which is the
    /// whole reason this room can be the size it is.</summary>
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

    /// <summary>A point in the board's own frame, turned by the board's yaw. A yaw of φ sends a local +X
    /// to (cos φ, 0, −sin φ) — the same convention every wall in this building is placed with.</summary>
    private static Vector3 Turn(Vector3 point, float degrees)
    {
        var a = degrees * MathF.PI / 180f;
        var c = MathF.Cos(a);
        var s = MathF.Sin(a);

        return new Vector3(point.X * c + point.Z * s, point.Y, -point.X * s + point.Z * c);
    }

    /// <summary>
    /// A processor: a green substrate, a stepped heat spreader on it, the pad field underneath and a
    /// handful of decoupling capacitors in the middle of it.
    ///
    /// The model has an empty socket, which is what a board file has — a socket is a part and a processor
    /// is not one, because a processor is what you put in it. So the film brings its own, and it is built
    /// from a photograph: the lid is two steps rather than one, because the flange a spreader is soldered
    /// down by is what stops it reading as a chrome tile, and there is a bevel on one corner because every
    /// processor has one and it is the only thing on it that says which way round it goes.
    ///
    /// <b>The pad field is one slab, not a thousand pads.</b> A real one has upwards of a thousand, and a
    /// thousand boxes is a thousand draws for a surface that is in shot for a second and a half at an
    /// angle. What sells it is the colour and the four capacitors sitting in the middle of it, which are
    /// four boxes.
    ///
    /// Nothing on it is written. That is the same rule the lounge's console is under — a silhouette is a
    /// genre and a badge is somebody's property — and it applies to a processor exactly as it does to a
    /// games machine.
    ///
    /// Its origin is its <i>bottom</i> face rather than its centre, which is what makes both of its
    /// positions readable: on the bench it stands at zero and in the socket it stands on the laminate.
    /// </summary>
    private Node Chip()
    {
        var node = new Node { Name = "processor" };

        var substrate = new Material
        {
            BaseColor = new Vector4(0.055f, 0.115f, 0.070f, 1f),
            Roughness = 0.52f,
            Name = "cpu.substrate"
        };

        var spreader = new Material
        {
            BaseColor = new Vector4(0.86f, 0.87f, 0.89f, 1f),
            Metallic = 1f,
            Roughness = 0.20f,
            Name = "cpu.lid"
        };

        var passive = new Material
        {
            BaseColor = new Vector4(0.055f, 0.055f, 0.060f, 1f),
            Roughness = 0.40f,
            Name = "cpu.passive"
        };

        node.Children.Add(Fabric.Slab(
            new Vector3(0.38f, 0.010f, 0.38f), new Vector3(0f, 0.005f, 0f), substrate, "substrate"));

        // The pad field, and the four capacitors in the middle of it. One slab for a thousand pads — see
        // the remarks — and the capacitors are what make it read as an underside rather than as gold paint.
        node.Children.Add(Fabric.Slab(
            new Vector3(0.345f, 0.003f, 0.345f), new Vector3(0f, -0.0015f, 0f), Fabric.Brass, "pads"));

        foreach (var x in new[] { -0.035f, 0.035f })
        foreach (var z in new[] { -0.035f, 0.035f })
            node.Children.Add(Fabric.Slab(
                new Vector3(0.05f, 0.008f, 0.028f), new Vector3(x, -0.004f, z), passive, "decoupling"));

        // The spreader, in two steps: the flange it is fixed down by and the raised face over the die.
        node.Children.Add(Fabric.Slab(
            new Vector3(0.355f, 0.008f, 0.355f), new Vector3(0f, 0.014f, 0f), spreader, "flange"));

        node.Children.Add(Fabric.Slab(
            new Vector3(0.285f, 0.020f, 0.285f), new Vector3(0f, 0.028f, 0f), spreader, "lid"));

        // The keyed corner, which is the only mark on it and is a fact about orientation rather than a
        // name. And a line of light let into the substrate, which is not a fact about anything — it is
        // the film saying what century this is. It comes on with the power; see Live.
        node.Children.Add(Fabric.Slab(
            new Vector3(0.05f, 0.004f, 0.05f), new Vector3(-0.155f, 0.011f, -0.155f), Fabric.Brass, "key"));

        node.Children.Add(Fabric.Slab(
            new Vector3(0.30f, 0.004f, 0.008f), new Vector3(0f, 0.011f, 0.181f), _live, "trace"));

        return node;
    }

    /// <summary>
    /// The cooler: a base, six heat pipes, a stack of twenty-four fins and a nine-bladed fan in a frame.
    ///
    /// It is a tower, which is what a cooler that size is, and it is the one object in the film built from
    /// a photograph rather than from a description. Everything in it is a box or a cylinder — the fins are
    /// plates stacked up the pipes, the pipes are cylinders through them, the blades are thin boxes at a
    /// radius with a pitch on them — and between them they come to about forty-five draws, which for the
    /// piece of hardware the whole bench is arranged around is the right price.
    ///
    /// <b>The fan is a separate node.</b> That is the only structural decision in here: a rotor that is a
    /// child of the frame rather than part of it can be given one number a frame and everything on it
    /// turns, which is the same trick the mirror ball in the lounge uses and the same trick the corridor's
    /// beacons use. Nine blades, nine boxes, one rotation.
    ///
    /// The blades need two nodes each and it is worth saying why. A blade is at an angle round the hub
    /// <i>and</i> pitched about its own radius, and those two rotations do not commute — the Euler
    /// convention here applies roll before pitch, which would tilt the whole assembly rather than the
    /// blade. An arm that carries the angle and a blade inside it that carries the pitch is the
    /// composition written down, and it costs no draws, because an empty node draws nothing.
    /// </summary>
    /// <param name="rotor">The part of it that turns, for <see cref="Fan"/>.</param>
    private Node Cooler(out Node rotor)
    {
        const float span = 0.95f;
        const float deep = 0.72f;
        const float bottom = 0.34f;
        const float top = 1.16f;
        const int fins = 24;

        var node = new Node { Name = "cooler" };

        var nickel = new Material
        {
            BaseColor = new Vector4(0.86f, 0.87f, 0.88f, 1f),
            Metallic = 1f,
            Roughness = 0.26f,
            Name = "cooler.fin"
        };

        var copper = new Material
        {
            BaseColor = new Vector4(0.88f, 0.86f, 0.82f, 1f),
            Metallic = 1f,
            Roughness = 0.18f,
            Name = "cooler.pipe"
        };

        var frame = new Material
        {
            BaseColor = new Vector4(0.085f, 0.088f, 0.098f, 1f),
            Metallic = 0.2f,
            Roughness = 0.44f,
            Name = "cooler.frame"
        };

        var blade = new Material
        {
            BaseColor = new Vector4(0.13f, 0.14f, 0.16f, 1f),
            Roughness = 0.38f,
            Name = "cooler.blade"
        };

        // The block that touches the processor, and the bracket over it.
        node.Children.Add(Fabric.Slab(
            new Vector3(0.52f, 0.05f, 0.52f), new Vector3(0f, 0.025f, 0f), copper, "block"));

        node.Children.Add(Fabric.Slab(
            new Vector3(0.68f, 0.03f, 0.16f), new Vector3(0f, 0.065f, 0f), nickel, "bracket"));

        // Six pipes, two rows of three, running the whole height. They are what carries the heat and they
        // are also what the fins are threaded on to, which is why they are drawn before the fins are.
        foreach (var z in new[] { -0.18f, 0.18f })
        foreach (var x in new[] { -0.28f, 0f, 0.28f })
            node.Children.Add(new MeshNode(Primitives.Cylinder(0.038f, 0.038f, top - 0.04f, 10), copper)
            {
                Position = new Vector3(x, 0.05f + (top - 0.04f) / 2f, z),
                Name = "pipe"
            });

        // The stack. Thin plates at a close pitch, which is what a fin stack is and is also the one place
        // in this model where the draw count buys the whole silhouette.
        for (var i = 0; i < fins; i++)
            node.Children.Add(Fabric.Slab(
                new Vector3(span, 0.012f, deep),
                new Vector3(0f, bottom + i * (top - bottom) / (fins - 1), 0f),
                nickel,
                "fin"));

        // The fan on the front face, looking out at the bench.
        var face = deep / 2f + 0.075f;
        var half = span / 2f;
        var axis = (bottom + top) / 2f;
        var at = new Vector3(0f, axis, face);

        // The frame: four rails and four corner bosses, which is what turns a square hole into the shape
        // everybody recognises. The bosses are cylinders and they are the whole of the difference — a fan
        // frame with square corners is a picture frame.
        //
        // <b>How thin the rails are is arithmetic, not taste.</b> What they leave between them is the hole
        // the rotor turns in, so it has to be bigger than the rotor. At a tenth of a unit each they left an
        // opening 75 mm across in a 95 mm frame while the blades swept a hundred — the rotor was wider than
        // the whole part — and every blade passed straight through the rails and stuck out of the far side
        // of the thing it is supposed to be inside of. See <see cref="Blade"/> for the other half of it.
        const float rail = 0.06f;
        var bore = half - rail;

        foreach (var (w, h, x, y) in new[]
                 {
                     (span, rail, 0f, half - rail / 2f),
                     (span, rail, 0f, -half + rail / 2f),
                     (rail, span - rail * 2f, half - rail / 2f, 0f),
                     (rail, span - rail * 2f, -half + rail / 2f, 0f)
                 })
        {
            node.Children.Add(Fabric.Slab(
                new Vector3(w, h, 0.15f), at + new Vector3(x, y, 0f), frame, "rail"));
        }

        // The corners, which fill what the rails do not. A cylinder at a tenth in from each corner comes no
        // nearer the axis than the rails do, so the bore stays the widest circle in the frame.
        foreach (var x in new[] { -1f, 1f })
        foreach (var y in new[] { -1f, 1f })
        {
            node.Children.Add(new MeshNode(Primitives.Cylinder(0.11f, 0.11f, 0.15f, 12), frame)
            {
                Position = at + new Vector3(x * (half - 0.10f), y * (half - 0.10f), 0f),
                RotationDegrees = new Vector3(90f, 0f, 0f),
                Name = "boss"
            });

            node.Children.Add(new MeshNode(Primitives.Cylinder(0.035f, 0.035f, 0.17f, 8), nickel)
            {
                Position = at + new Vector3(x * (half - 0.10f), y * (half - 0.10f), 0f),
                RotationDegrees = new Vector3(90f, 0f, 0f),
                Name = "screw"
            });
        }

        // The ring, in twelve arcs of thirty degrees, each with its own material. One torus would be one
        // colour; twelve are what makes it the thing everybody has seen on a fan since about 2016, and the
        // hue runs round them on a timer — see Fan. Emission and additive, so a ring of light on a
        // component costs exactly nothing, which is this room's whole argument said one more time on a
        // part four hundred millimetres across.
        for (var i = 0; i < _ring.Length; i++)
        {
            _ring[i] = Glow(1f, 1f, 1f);

            node.Children.Add(new MeshNode(
                Primitives.Torus(bore + 0.02f, 0.026f, 8, 8, 360f / _ring.Length, i * 360f / _ring.Length),
                _ring[i])
            {
                Position = at + new Vector3(0f, 0f, 0.062f),
                RotationDegrees = new Vector3(90f, 0f, 0f),
                Name = "ring"
            });
        }

        rotor = new Node { Name = "rotor", Position = at };

        // Nine blades, one mesh. They are identical, so the geometry goes to the card once and every node
        // after the first costs a matrix — which is what instancing is, and it is free here because a
        // shared Mesh instance is how this control recognises one.
        var sweep = Blade();

        for (var i = 0; i < _blades.Length; i++)
        {
            // A material each rather than one shared. The mesh is still shared — which is what instancing
            // costs nothing for — but the colour is not, and nine hues a ninth of a turn apart sweeping
            // past the frame is both the look the reference has and the only thing on a fan that says
            // which way and how fast it is going. A rotor in one colour at speed is a still disc.
            _blades[i] = new Material
            {
                BaseColor = new Vector4(0.085f, 0.090f, 0.105f, 1f),
                Roughness = 0.36f,
                Name = "cooler.blade"
            };

            rotor.Children.Add(new MeshNode(sweep, _blades[i])
            {
                RotationDegrees = new Vector3(0f, 0f, i * 40f),
                Name = "blade"
            });
        }

        // The hub, its cap and the three screws in it, which is what the middle of a fan looks like and is
        // also the only part of one that is still legible when the blades are a disc.
        //
        // <b>All three are on the front.</b> They were on the back, where the fin stack is: a cap and a lit
        // collar and three screws, all correct, all built, and every one of them buried in a block of
        // aluminium a hundred millimetres thick. What the camera saw instead was the plain end of the hub —
        // a flat dark disc in the middle of a coloured ring, which is the one thing the collar exists to
        // stop. Nothing about the parts was wrong except which way along the axis they were put.
        rotor.Children.Add(new MeshNode(Primitives.Cylinder(0.16f, 0.16f, 0.11f, 18), blade)
        {
            Position = new Vector3(0f, 0f, -0.012f),
            RotationDegrees = new Vector3(90f, 0f, 0f),
            Name = "hub"
        });

        rotor.Children.Add(new MeshNode(Primitives.Cylinder(0.145f, 0.145f, 0.02f, 18), frame)
        {
            Position = new Vector3(0f, 0f, 0.053f),
            RotationDegrees = new Vector3(90f, 0f, 0f),
            Name = "cap"
        });

        // A collar of light round the hub, which every one of these has and which is what stops the middle
        // of the rotor being a black disc in the middle of a coloured one. It turns with the fan and is
        // therefore the one lit thing in the room that moves because a part moves rather than because a
        // number changed.
        rotor.Children.Add(new MeshNode(Primitives.Torus(0.172f, 0.013f, 20, 8), _live)
        {
            Position = new Vector3(0f, 0f, 0.043f),
            RotationDegrees = new Vector3(90f, 0f, 0f),
            Name = "collar"
        });

        for (var i = 0; i < 3; i++)
        {
            var a = (i * 120f + 30f) * MathF.PI / 180f;

            rotor.Children.Add(new MeshNode(Primitives.Cylinder(0.018f, 0.018f, 0.026f, 8), nickel)
            {
                Position = new Vector3(MathF.Cos(a) * 0.075f, MathF.Sin(a) * 0.075f, 0.062f),
                RotationDegrees = new Vector3(90f, 0f, 0f),
                Name = "screw"
            });
        }

        node.Children.Add(rotor);

        return node;
    }

    /// <summary>
    /// A memory module: a laminate with the polarising notch cut in its contact edge, gold fingers either
    /// side of it, an anodised heat spreader over both faces and a lit diffuser along the top.
    ///
    /// The notch is the detail worth having and it is two slabs with a gap between them rather than one
    /// slab with a hole. This control has no mesh boolean and needs none: the opening is where the
    /// geometry is not, which is the same thing <see cref="Fabric.PiercedWall"/> does for a doorway six
    /// times over in this building.
    ///
    /// Its origin is the middle of its <i>bottom edge</i>, which is the axis it turns about on the way to
    /// the slot and the point that has to land in it. Getting that wrong makes a part that pivots about
    /// its own middle, and a component that rotates about its centre while it is being fitted is a
    /// component nobody is holding.
    ///
    /// <b>The spreader is why the packages went.</b> The first version was a bare green stick with fourteen
    /// black chips on it, which is what memory looked like when it was sold by the chip. Nothing anybody
    /// puts in a machine like this one is bare: it comes in a black anodised jacket with the relief milled
    /// into it and a diffuser along the top, and fourteen packages behind two solid plates are fourteen
    /// draws of geometry nobody can see. What replaced them is the jacket, three slashes of relief a side,
    /// and eight segments of lit bar — the same trade the fan's ring makes and for the same reason.
    /// </summary>
    /// <param name="index">Which of the two this is, so its bar segments can be found again.</param>
    private Node Module(int index)
    {
        var node = new Node { Name = "dimm" };

        var laminate = new Material
        {
            BaseColor = new Vector4(0.048f, 0.098f, 0.062f, 1f),
            Roughness = 0.55f,
            Name = "dimm.pcb"
        };

        // Anodised aluminium, which is a metal with a dyed oxide over it: nearly black, and glossy enough
        // that the whole of what you see on one is the bench's own lamp sliding along the relief.
        var jacket = new Material
        {
            BaseColor = new Vector4(0.058f, 0.060f, 0.068f, 1f),
            Metallic = 0.65f,
            Roughness = 0.34f,
            Name = "dimm.jacket"
        };

        var label = new Material
        {
            BaseColor = new Vector4(0.62f, 0.64f, 0.66f, 1f),
            Roughness = 0.72f,
            Name = "dimm.label"
        };

        // A hundred and thirty-three and a third millimetres, which is what a DIMM is and is the number
        // the socket in the model is built outwards from — see the note on the slot in build-pcb.py.
        const float length = 1.334f;
        const float half = length / 2f;

        // Where the notch is cut, and it is not a free choice. The slot has a polarising key standing
        // in it, one and two fifths of a millimetre thick, at forty-two hundredths of the card's length
        // measured from the end nearest the board's bottom edge — and the board's Y runs the opposite
        // way to the model's Z, so that end is this module's <i>positive</i> one. The first version cut
        // the notch at forty-four hundredths from the other end, which is seventeen millimetres out:
        // the key stood straight through the laminate, and the one part of a memory module that exists
        // to stop it going in the wrong way round was going in the wrong way round.
        const float key = half - (0.42f * length + 0.007f);

        // Twice the key's width, so nothing touches.
        const float notch = 0.014f;

        // The body, above the contacts.
        node.Children.Add(Fabric.Slab(
            new Vector3(0.012f, 0.245f, length), new Vector3(0f, 0.1775f, 0f), laminate, "laminate"));

        // The contact edge, in two runs with the key between them. Gold over laminate on both faces, so
        // the fingers are a surface rather than a stripe painted on the edge.
        foreach (var (from, to) in new[] { (-half, key - notch), (key + notch, half) })
        {
            var mid = (from + to) / 2f;
            var run = to - from;

            node.Children.Add(Fabric.Slab(
                new Vector3(0.012f, 0.055f, run), new Vector3(0f, 0.0275f, mid), laminate, "tab"));

            node.Children.Add(Fabric.Slab(
                new Vector3(0.0145f, 0.040f, run - 0.02f), new Vector3(0f, 0.024f, mid),
                Fabric.Brass, "fingers"));
        }

        // The jacket: a plate over each face, from just clear of the contact edge to just over the top of
        // the laminate, and a cap across the two that closes the top and carries the diffuser.
        foreach (var side in new[] { -1f, 1f })
        {
            node.Children.Add(Fabric.Slab(
                new Vector3(0.011f, 0.258f, length - 0.026f),
                new Vector3(side * 0.0115f, 0.204f, 0f), jacket, "jacket"));

            // Four slashes of relief, raked over. They are the whole of what makes a black plate read as a
            // milled one: a flat face of anodised aluminium under a single lamp is a flat grey rectangle,
            // and each of these takes the light at a different angle to the plate behind it.
            //
            // <b>They are the jacket's own material and they are sunk into it.</b> Both matter and the
            // first attempt got both wrong: a lighter material and a two-hundredth of a millimetre of
            // overlap turned them into four pale rectangles hovering off the face, which is a sticker and
            // not a facet. Relief is one piece of metal at two angles, so it has to be one colour.
            for (var i = 0; i < 4; i++)
            {
                node.Children.Add(new MeshNode(Primitives.Box(0.006f, 0.046f, 0.34f), jacket)
                {
                    Position = new Vector3(side * 0.018f, 0.205f, -0.45f + i * 0.30f),
                    RotationDegrees = new Vector3(34f, 0f, 0f),
                    Name = "relief"
                });
            }
        }

        node.Children.Add(Fabric.Slab(
            new Vector3(0.034f, 0.014f, length - 0.026f), new Vector3(0f, 0.340f, 0f), jacket, "crest"));

        node.Children.Add(Fabric.Slab(
            new Vector3(0.0025f, 0.070f, 0.30f), new Vector3(-0.0175f, 0.150f, -0.30f), label, "label"));

        // And the diffuser, which is the module's whole claim to being from this century. Eight segments
        // rather than one bar, because one bar is one colour: the hue runs along the stick and round the
        // wheel — see <see cref="Fan"/>, which drives these off the same clock the fan's ring turns on,
        // for the reason that on real hardware one controller drives the whole case and everything in it
        // is in step. It costs no light slot, like everything else in this room that glows.
        for (var i = 0; i < Segments; i++)
        {
            var bar = Glow(1f, 1f, 1f);
            _bars[index * Segments + i] = bar;

            node.Children.Add(Fabric.Slab(
                new Vector3(0.030f, 0.042f, (length - 0.046f) / Segments),
                new Vector3(0f, 0.368f,
                    -(length - 0.046f) / 2f + (i + 0.5f) * (length - 0.046f) / Segments),
                bar,
                "bar"));
        }

        return node;
    }

    /// <summary>
    /// One fan blade, as a mesh built here rather than out of <see cref="Primitives"/>.
    ///
    /// It is the only piece of geometry in the whole building that is not a box, a cylinder, a plane or a
    /// sphere, and the reason is that a fan blade is none of those and does not look like any of them. It
    /// is a surface with three things happening along its length at once: it sweeps backwards, it twists,
    /// and it widens. Nine flat rectangles at an angle is a paddle wheel; the difference between that and
    /// this is entirely in those three curves, and every one of them is a lerp.
    ///
    /// The surface is a grid over (u along the blade, v across the chord), and the point at each node is
    ///
    ///   radius outward, plus chord tangentially, plus chord in the axis — the last two split by the
    ///   pitch, which is what a twist is.
    ///
    /// Normals come from the cross product of the two surface derivatives, taken numerically because
    /// writing them out is three pages of chain rule for a result the finite difference gets right to more
    /// places than a normal is stored in.
    ///
    /// One mesh for all nine, because they are identical: a shared <see cref="Mesh"/> instance is how this
    /// control recognises instancing, so the geometry is uploaded once and the other eight cost a matrix.
    ///
    /// <b>How big it is comes from the frame, and the frame is not the tip radius.</b> A blade reaches
    /// further than <c>tip</c>, because half a chord hangs off the end of the radius as well: the circle it
    /// actually sweeps is the hypotenuse of the two, √(tip² + (chordTip·cos twistTip)²), which for the
    /// numbers below is 0.400 against a bore of 0.415. The first set of numbers made that 0.499 in a frame
    /// 0.475 across, so the rotor was wider than the part holding it and the blades were plainly visible
    /// outside it — a mistake that is invisible in the parameters and obvious in one picture. The same
    /// sum bounds the twist: chordRoot·sin(twistRoot) is how far a blade reaches along the axis, 0.067
    /// here, against a frame half a hundredth deeper.
    /// </summary>
    private static Mesh Blade()
    {
        const int along = 10;
        const int across = 4;
        const float root = 0.15f;
        const float tip = 0.36f;
        const float rake = 46f;
        const float twistRoot = 34f;
        const float twistTip = 15f;
        const float chordRoot = 0.12f;
        const float chordTip = 0.18f;

        var positions = new Vector3[(along + 1) * (across + 1)];
        var normals = new Vector3[positions.Length];
        var indices = new uint[along * across * 6];

        for (var i = 0; i <= along; i++)
        for (var j = 0; j <= across; j++)
        {
            var u = (float)i / along;
            var v = (float)j / across * 2f - 1f;
            var n = i * (across + 1) + j;

            positions[n] = Point(u, v);

            // The two derivatives, one step of a thousandth each. Clamped so the ends take the difference
            // that exists rather than the one that runs off the surface.
            var du = Point(MathF.Min(u + 0.002f, 1f), v) - Point(MathF.Max(u - 0.002f, 0f), v);
            var dv = Point(u, MathF.Min(v + 0.004f, 1f)) - Point(u, MathF.Max(v - 0.004f, -1f));

            var normal = Vector3.Cross(du, dv);
            normals[n] = normal.LengthSquared() > 1e-12f ? Vector3.Normalize(normal) : Vector3.UnitZ;
        }

        var at = 0;

        for (var i = 0; i < along; i++)
        for (var j = 0; j < across; j++)
        {
            var a = (uint)(i * (across + 1) + j);
            var b = (uint)(a + across + 1);

            indices[at++] = a;
            indices[at++] = b;
            indices[at++] = a + 1;
            indices[at++] = a + 1;
            indices[at++] = b;
            indices[at++] = b + 1;
        }

        return new Mesh
        {
            Positions = positions,
            Normals = normals,
            Indices = indices,
            Name = "fan.blade"
        };

        static Vector3 Point(float u, float v)
        {
            // The blade leans back faster the further out it goes, which is what makes the leading edge a
            // curve rather than a diagonal. Squared is enough; linear reads as a bent rectangle.
            var angle = rake * u * u * MathF.PI / 180f;
            var radius = root + (tip - root) * u;
            var chord = (chordRoot + (chordTip - chordRoot) * u) * v;
            var pitch = (twistRoot + (twistTip - twistRoot) * u) * MathF.PI / 180f;

            var outward = new Vector3(MathF.Cos(angle), MathF.Sin(angle), 0f);
            var round = new Vector3(-MathF.Sin(angle), MathF.Cos(angle), 0f);

            return outward * radius + round * (chord * MathF.Cos(pitch)) +
                   new Vector3(0f, 0f, chord * MathF.Sin(pitch));
        }
    }

    /// <summary>Where <paramref name="u"/> has got to between two thresholds, 0 to 1.</summary>
    private static float Span(float u, float from, float to) =>
        to <= from ? (u >= to ? 1f : 0f) : Math.Clamp((u - from) / (to - from), 0f, 1f);

    private static float Smooth(float u) => u * u * (3f - 2f * u);

    private static float Fraction(float t) => t - MathF.Floor(t);
}
