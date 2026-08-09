using System.Numerics;
using Ava3D.Demo.Scenes;

namespace Ava3D.Demo.Story;

/// <summary>
/// The material gallery: a long room entered at one end, with a chart of forty-nine materials set into one
/// wall, a case of twelve opposite it, a turning drum of maps halfway up, five metals at the far end
/// reflecting the room they are standing in, and six materials you can name on the wall behind them.
///
/// <b>The room is made of what it is exhibiting, and it was not.</b> For a long time this was five exhibits
/// standing in fourteen metres of plaster — every wall, the ceiling and both bay linings one surface, with
/// a millimetre of relief on it that measured as half a degree of normal and rendered as flat beige paint.
/// A gallery about materials finished in a single material nobody can see is the one room in the building
/// that cannot afford to be. It now has a timber dado with a brushed rail capping it, a boarded runner down
/// the tiled floor inside two metal strips, moulded plastic coffers across the ceiling at every fixture, and
/// metal frames round both cases — six surfaces, all of them carrying every map the shading model has, and
/// four of them within arm's reach of where the visitor walks.
///
/// It is the narrowest room in the building and the only one longer than it is wide, and both are the same
/// decision. A chart is <i>read</i>, not toured — you stand square-on to it and let your eye run down a
/// column — and a room you can stand square-on in is a room with a wall behind you to stand against. Six
/// metres of width is what puts the visitor three and a half from the chart, which is where a two-metre
/// grid of small samples resolves into forty-nine separate materials rather than into a texture.
///
/// The chart and the case face each other exactly, and that is what enforces rule 2 in a room with no
/// internal walls: two exhibits a hundred and eighty degrees apart cannot both be in one frame, whatever
/// the mouse does with the head. It is the cheapest version of the rule in the building — no corner, no
/// threshold, no hand-over, just two things that are opposite.
///
/// What the far end has that no other room does is an <see cref="EnvironmentLight"/> with an image in it.
/// Every other room gets the two-colour hemisphere from <see cref="Hall.Ambient"/>, which is right for
/// plaster and useless for chrome: a metal has no colour of its own, so five metals reflecting a vertical
/// gradient are five spheres that look the same. So this room bakes a probe of itself — see
/// <see cref="Probe"/> — and the metals reflect the lamps, the doorway and the length of the corridor they
/// are actually standing in.
/// </summary>
internal sealed class Gallery
{
    /// <summary>Clear width and clear length, wall face to wall face.</summary>
    public const float Width = 6f;

    public const float Length = 14.4f;

    /// <summary>
    /// The same three-two as the rest of the building, which after the rotunda's three-nine is the point.
    ///
    /// He comes out of the tallest room in the film into the lowest and narrowest, and a corridor is what
    /// that difference feels like. The plan asks for a gallery that "reads as a corridor"; the reading is
    /// done by the ceiling coming down seventy centimetres at the door, not by the length.
    /// </summary>
    private const float Height = Deck.Ceiling;

    private const float HalfWidth = Width / 2f;
    private const float HalfLength = Length / 2f;

    /// <summary>
    /// Where the doorways are in the room's own coordinates — derived from the rooms they meet rather than
    /// chosen. See <see cref="Deck.Materials"/> for why this room does not get to pick.
    /// </summary>
    private static readonly float EntranceX = Rotunda.Exit.X - Deck.Materials.X;

    private static readonly float ExitZ = ScreenRoom.Entrance.Z - Deck.Materials.Z;

    /// <summary>
    /// Where the chart and the case are set into their walls, facing each other.
    ///
    /// Well down the south half, so that the three stops in this room are three different places in it. The
    /// first pass had the bays, the drum and the stand within four metres of each other and the whole walk
    /// happened without anybody moving — which reads as a camera turning on the spot in a room that happens
    /// to be long, rather than as somebody walking down a gallery.
    /// </summary>
    private const float FacingZ = -3.4f;

    private const float BayWidth = 2.5f;
    private const float BayDepth = 0.34f;

    /// <summary>
    /// The chart's cells, and the ball in each. Small, because there are forty-nine of them and they have
    /// to fit between a sill and a ceiling in a three-metre room — and because a material sample is a small
    /// thing, which is the shape this exhibit is imitating.
    /// </summary>
    private const float CellPitch = 0.30f;

    private const float CellRadius = 0.125f;

    private const float CaseScale = 0.36f;

    /// <summary>Where the drum stands: off the centre line, so the walk goes past it rather than into
    /// it.</summary>
    private static readonly Vector3 DrumAt = new(0.85f, 0f, 1.4f);

    private const float DrumScale = 0.42f;
    private const float PlinthHeight = 0.85f;

    /// <summary>The stand at the far end, and how much of the five-metal row fits on it.</summary>
    private const float RowZ = 6.1f;

    private const float RowScale = 0.33f;
    private const float StandHeight = 0.9f;

    private const float LampY = Height - 0.1f;

    /// <summary>
    /// The dado: boarded timber to just over a metre, with a brushed rail capping it.
    ///
    /// It is decoration and it is also the room's own argument, which is why it is here and not in the
    /// lounge. Fourteen metres of one material is what this gallery used to be — plaster floor to ceiling
    /// down both sides — and the first honest look at it said the thing the exhibits are about is the one
    /// thing the room was not doing. Now the wall you stand nearest is wood, the line capping it is metal,
    /// the wall above it is plaster and the floor under it is tile with a boarded runner down the middle:
    /// four materials within two metres of the visitor, all of them carrying every map the model has.
    ///
    /// <see cref="Bite"/> is why none of it shimmers. A panel laid <i>on</i> a wall puts its back face in
    /// the same plane as the wall's front face, and the depth test cannot choose between two surfaces at
    /// one depth — it answers per pixel and per frame, which reads as hatching that crawls. Every piece
    /// added here is sunk two centimetres into what it is fixed to, so no two faces are ever level.
    /// </summary>
    private const float DadoHeight = 1.02f;

    private const float DadoProud = 0.032f;
    private const float RailHeight = 0.05f;
    private const float RailProud = 0.058f;
    private const float Bite = 0.02f;

    /// <summary>The runner down the floor: boards inside two metal strips, which is what a gallery lays
    /// where the visitors walk and a hold does not have anywhere else.</summary>
    private const float RunnerWidth = 1.9f;

    private const float RunnerProud = 0.014f;

    /// <summary>The specimen shelf on the blind north wall, and how far apart the six samples stand.</summary>
    private const float ShelfY = 1.46f;

    private const float ShelfDepth = 0.26f;
    private const float SamplePitch = 0.72f;

    public Gallery(Hall hall)
    {
        var root = hall.Add(Deck.MaterialsRoom, Deck.Materials);

        // Three rungs up, and the first surface in the film with relief in it that is not geometry: a normal
        // map on the plaster and grooves in the tile. It belongs in this room and not before it, because the
        // exhibits on these walls are sixty-one materials with no maps at all, and a wall that has one is
        // the demonstration standing behind the exhibition — the chart says what metallic and roughness do,
        // and the room it is hanging in says what happens when the numbers arrive as pictures instead. See
        // <see cref="Grade"/>.
        var plaster = Finish.Plaster(Grade.Dressed);
        var tile = Finish.Floor(Grade.Dressed);
        var stone = Finish.Stone(Grade.Dressed);

        // The room's own materials, as opposed to the ones it is exhibiting. Timber on the dado, brushed
        // metal on every edge and every shelf, boards down the middle of the floor and moulded plastic
        // round the fixtures — four more surfaces, all of them already in the building elsewhere, put
        // where the visitor is standing rather than where he is looking. See <see cref="DadoHeight"/>.
        var timber = Finish.Timber();
        var brushed = Finish.Brushed();
        var boards = Finish.Boards();
        var moulding = Finish.Plastic(0.80f, 0.81f, 0.83f);

        var t = Deck.WallThickness;

        root.Children.Add(Fabric.Sheet(Width + 1f, Length + 0.7f, 0f, tile, "floor"));

        // The ceiling is one rung down from the walls, and it is the only surface in the building that is
        // deliberately given less than the room it is in.
        //
        // A stipple is seen by being lit across, and a ceiling three metres two over an eye at one metre
        // seven is never seen across — it is seen at eight or ten degrees from every standing position in
        // a room fourteen metres long. A normal map read that far off the surface does not add relief, it
        // adds interference: the same bumps that read as trowelled plaster on a wall turn the ceiling into
        // a crawling popcorn texture, which is what came back the first time this room's relief was made
        // strong enough to see at all. It is the hazard Boards and Composite both turn their normal scale
        // down for; here the honest answer is that the map has nothing to contribute overhead and is left
        // off, which also costs nothing.
        root.Children.Add(Fabric.Lid(
            Width + 1f, Length + 0.7f, Height, Finish.Plaster(Grade.Worked), "ceiling"));

        // The runner: boards down the length of the room, sunk into the tile and edged both sides in metal.
        // It is drawn at Close rather than at the building's pitch — a plank read from two metres wants to
        // be a plank and not a floorboard seen from six.
        root.Children.Add(Fabric.Slab(
            new Vector3(RunnerWidth, RunnerProud + Bite, Length + 0.7f),
            new Vector3(0f, (RunnerProud - Bite) / 2f, 0f),
            boards,
            "runner",
            Finish.Close));

        foreach (var side in new[] { -1f, 1f })
            root.Children.Add(Fabric.Slab(
                new Vector3(0.05f, RunnerProud + Bite + 0.004f, Length + 0.7f),
                new Vector3(side * (RunnerWidth + 0.05f) / 2f, (RunnerProud - Bite) / 2f + 0.002f, 0f),
                brushed,
                "runner.edge",
                Finish.Close));

        // The two ends, built along X and therefore unrotated. The south one carries the rotunda's north
        // door; the north one is blind, and is what the five metals stand against.
        var south = Fabric.PiercedWall(
            Width + 0.7f, Height, t, EntranceX, Deck.DoorWidth, Deck.DoorHeight, plaster);
        south.Position = new Vector3(0f, 0f, -(HalfLength + t / 2f));
        root.Children.Add(south);

        // The third door, and it belongs to this room rather than to the rotunda even though it is the
        // rotunda he is standing in when he first sees it. Two reasons, and the second one is the reason.
        //
        // The rotunda is switched off nine seconds into chapter 3, while he is still in the opening — a
        // door that belonged to it would vanish out of the frame he is walking through. And this room is
        // already standing behind that wall for the last stretch of chapter 2, unlit, so that the way out
        // of the rotunda is not a rectangle of background. Putting the door here means what closes that
        // rectangle is a shut powered door with its edge lit, seen from across a round room — which is the
        // whole of the film's turn from a building to something else, delivered a chapter early and
        // without a word.
        Gate = Door.Powered(new Vector3(EntranceX, 0f, -(HalfLength + t / 2f)));
        root.Children.Add(Gate.Root);

        var north = Fabric.Wall(Width + 0.7f, Height, t, plaster);
        north.Position = new Vector3(0f, 0f, HalfLength + t / 2f);
        root.Children.Add(north);

        // The two sides, built as explicit runs along Z rather than as a wall turned a quarter.
        //
        // Both would work and the turned one is shorter, but a rotated wall's offsets are in the wall's own
        // frame, and a yaw of ninety sends its X to −Z — so every offset in it reads backwards from the room
        // it is in. One of them in the screen room had been three metres out since that room was built, and
        // it was invisible because nothing ever walked through that door until this room needed to. A room
        // whose long axis is Z should be written along Z.
        var bayFrom = FacingZ - BayWidth / 2f;
        var bayTo = FacingZ + BayWidth / 2f;
        var doorFrom = ExitZ - Deck.DoorWidth / 2f;
        var doorTo = ExitZ + Deck.DoorWidth / 2f;

        Side(-HalfWidth, -HalfLength, bayFrom);
        Side(-HalfWidth, bayTo, doorFrom);
        Side(-HalfWidth, doorTo, HalfLength);
        Side(-HalfWidth, doorFrom, doorTo, Deck.DoorHeight);

        Side(HalfWidth, -HalfLength, bayFrom);
        Side(HalfWidth, bayTo, HalfLength);

        // The chart, in a bay in the east wall. The room supplies the box and the scene supplies what is in
        // it, which is the whole of what mounting is — and what is in it is the same forty-nine materials
        // the reference chart shows, asked for at a size that suits a wall instead of a viewport.
        Bay(root, HalfWidth, FacingZ, 0.40f, plaster, brushed);

        // Twenty-four segments round and fourteen up, where the reference chart uses forty-eight and
        // twenty-four. It is the same mesh forty-nine times and it is the largest single object in the
        // film's triangle count — at the scene's own tessellation this one wall is fifty thousand
        // triangles, which is more than the hangar in chapter 12 spends on a hundred and twenty-six crates.
        // A sample here is a quarter of a metre across seen from three and a half, and at that size the
        // segment count stops being visible long before it stops being paid for.
        var samples = new PbrChartScene(Primitives.Sphere(CellRadius, 24, 14), CellPitch).BuildSubject()!;
        samples.Name = "exhibit.chart";
        samples.RotationDegrees = new Vector3(0f, -90f, 0f);
        samples.Position = new Vector3(HalfWidth + BayDepth - CellRadius, 1.56f, FacingZ);
        root.Children.Add(samples);

        // The case, opposite. Twelve spheres, six colours as dielectrics and the same six as metals, which
        // is the one comparison in this room that has to be read across rather than down.
        Bay(root, -HalfWidth, FacingZ, 0.95f, plaster, brushed);

        var pairs = new MaterialsScene().BuildSubject()!;
        pairs.Name = "exhibit.case";
        pairs.RotationDegrees = new Vector3(0f, 90f, 0f);
        pairs.Scale = new Vector3(CaseScale);
        pairs.Position = new Vector3(-(HalfWidth + BayDepth) + 0.45f * CaseScale, 1.5f, FacingZ);
        root.Children.Add(pairs);

        // The one exhibit in the room that is not against a wall, and the only one with a texture on it.
        // Standing it in the floor is the argument: the chart and the case are sixty-one materials with no
        // maps anywhere, and this is what the same shading model does when five images arrive. It turns, so
        // it wants to be somewhere you can walk round.
        var plinth = Fabric.Plinth(DrumAt, 1f, PlinthHeight, Grade.Dressed);
        root.Children.Add(plinth.Root);

        Panels = new PbrShowcaseScene();

        var drum = Panels.BuildSubject()!;
        drum.Name = "exhibit.panels";
        drum.Scale = new Vector3(DrumScale);

        // Seated by its own lowest point, as everything mounted in this building is — and read after the
        // scale and before the position, because Bounds is this node's extent through its own transform and
        // would otherwise be measuring the answer.
        drum.Position = plinth.Top + new Vector3(0f, -drum.Bounds.Min.Y, 0f);
        root.Children.Add(drum);

        // The far end: a low stand across the room, and five metals on it from mirror to matte.
        root.Children.Add(Fabric.Slab(
            new Vector3(4.2f, StandHeight, 0.85f),
            new Vector3(0f, StandHeight / 2f, RowZ),
            stone,
            "stand"));

        var metals = new EnvironmentScene().BuildSubject()!;
        metals.Name = "exhibit.metals";
        metals.Scale = new Vector3(RowScale);
        metals.Position = new Vector3(0f, StandHeight - metals.Bounds.Min.Y, RowZ);
        root.Children.Add(metals);

        // The ceiling, which was the last flat plane left in the room. A moulded plastic band across it at
        // every fixture, sunk into the slab so the two are never level: it gives the lamps something to be
        // mounted <i>on</i> rather than to hang out of nothing, and it puts the fifth material in the room
        // overhead, where the eye goes as soon as it has finished with a wall.
        foreach (var z in new[] { -6f, FacingZ, DrumAt.Z, RowZ - 1.4f, ExitZ })
        {
            root.Children.Add(Fabric.Slab(
                new Vector3(Width + 1f, 0.09f, 0.62f),
                new Vector3(0f, Height - 0.025f, z),
                moulding,
                "coffer",
                Finish.Close));

            // A metal edge down each side of it, which is the same detail as the dado rail and the runner's
            // strips: every change of material in this room happens at a piece of metal.
            foreach (var side in new[] { -1f, 1f })
                root.Children.Add(Fabric.Slab(
                    new Vector3(Width + 1f, 0.10f, 0.035f),
                    new Vector3(0f, Height - 0.03f, z + side * 0.325f),
                    brushed,
                    "coffer.edge",
                    Finish.Close));
        }

        Specimens(root, brushed);

        Entry = Hang(new Vector3(EntranceX, LampY, -6f), 2.2f, 7f);
        OverChart = Hang(new Vector3(1.55f, LampY, FacingZ), 2.7f, 6.5f);
        OverCase = Hang(new Vector3(-1.55f, LampY, FacingZ), 2.4f, 6.5f);
        OverDrum = Hang(new Vector3(DrumAt.X, LampY, DrumAt.Z), 2.6f, 6.5f);
        OverRow = Hang(new Vector3(0f, LampY, RowZ - 1.4f), 2.6f, 7f);
        ByDoor = Hang(new Vector3(-2.2f, LampY, ExitZ), 2f, 6f);

        // Dark until chapter 3 asks for them. The gallery is standing and visible through the rotunda's
        // north doorway for the last stretch of chapter 2 — that is what stops the way out of the rotunda
        // being a rectangle of background — and a dark corridor with six lit fixtures hanging in it would
        // be a room that had forgotten it was switched off.
        foreach (var lamp in All)
            lamp.Dim(0f);

        // The probe, baked once, from where he stands to look at the metals rather than from the middle of
        // the room. A probe is only right at the point it was taken; a metre and a half of parallax at the
        // far end is nothing, and seven metres of it would put the doorway on the wrong side of the frame.
        //
        // Four lamps, not six: the two at the entrance are out by the time he reaches the stand, and a
        // reflection containing lights that are not lit is the one kind of wrong a viewer can check.
        Environment = EnvironmentLight.FromTexture(
            Probe.Bake(
                new Vector3(0f, Deck.Eye, RowZ - 2.6f),
                new Vector3(-HalfWidth, 0f, -HalfLength),
                new Vector3(HalfWidth, Height, HalfLength),
                [
                    OverCase.Fixture.Position,
                    OverDrum.Fixture.Position,
                    OverRow.Fixture.Position,
                    ByDoor.Fixture.Position
                ],
                new Vector3(-HalfWidth, 1.2f, ExitZ)),
            0.72f);

        return;

        Lamp Hang(Vector3 at, float brightness, float range)
        {
            var lamp = Fabric.Ceiling(Deck.Materials, at, brightness, range);
            root.Children.Add(lamp.Fixture);

            return lamp;
        }

        // A run of side wall between two points along the room, optionally starting above the floor —
        // which is what makes the lintel over the doorway out.
        void Side(float x, float fromZ, float toZ, float bottom = 0f)
        {
            if (toZ - fromZ < 0.01f || Height - bottom < 0.01f)
                return;

            root.Children.Add(Fabric.Slab(
                new Vector3(t, Height - bottom, toZ - fromZ),
                new Vector3(x + MathF.CopySign(t / 2f, x), (bottom + Height) / 2f, (fromZ + toZ) / 2f),
                plaster,
                "wall"));

            // A run that starts off the floor is a lintel, and a lintel has no dado under it. Hanging the
            // panelling off the wall runs rather than writing it out separately is what keeps the two in
            // step: the bays and the doorway break the wall, and whatever breaks the wall breaks the dado
            // in the same place without anybody having to say so twice.
            if (bottom > 0f)
                return;

            var inward = -MathF.CopySign(1f, x);
            var middle = (fromZ + toZ) / 2f;

            root.Children.Add(Fabric.Slab(
                new Vector3(DadoProud + Bite, DadoHeight, toZ - fromZ),
                new Vector3(x + inward * (DadoProud - Bite) / 2f, DadoHeight / 2f, middle),
                timber,
                "dado"));

            root.Children.Add(Fabric.Slab(
                new Vector3(RailProud + Bite, RailHeight, toZ - fromZ),
                new Vector3(x + inward * (RailProud - Bite) / 2f, DadoHeight + RailHeight / 2f, middle),
                brushed,
                "rail",
                Finish.Close));
        }
    }

    /// <summary>The powered door in the south wall, shut. Chapter 3 opens it.</summary>
    public Door Gate { get; }

    /// <summary>The lamp inside the door he comes in by.</summary>
    public Lamp Entry { get; }

    public Lamp OverChart { get; }

    public Lamp OverCase { get; }

    public Lamp OverDrum { get; }

    /// <summary>The one in front of the five metals, which is most of what they reflect.</summary>
    public Lamp OverRow { get; }

    public Lamp ByDoor { get; }

    /// <summary>All six, for switching the room off in one line.</summary>
    public Lamp[] All => [Entry, OverChart, OverCase, OverDrum, OverRow, ByDoor];

    /// <summary>The drum of maps, kept so the chapter can run the scene's own animation rather than
    /// reimplementing it.</summary>
    public PbrShowcaseScene Panels { get; }

    /// <summary>The baked probe: what the five metals reflect, and the room's ambient while chapter 3 is
    /// running.</summary>
    public EnvironmentLight Environment { get; }

    /// <summary>The middle of the chart, in world coordinates.</summary>
    public static Vector3 Chart => Deck.Materials + new Vector3(HalfWidth, 1.56f, FacingZ);

    /// <summary>The middle of the case.</summary>
    public static Vector3 Case => Deck.Materials + new Vector3(-HalfWidth, 1.5f, FacingZ);

    /// <summary>The middle of the turning drum.</summary>
    public static Vector3 Drum => Deck.Materials + DrumAt + new Vector3(0f, PlinthHeight + 0.55f, 0f);

    /// <summary>The middle of the five metals.</summary>
    public static Vector3 Row => Deck.Materials + new Vector3(0f, StandHeight + 0.28f, RowZ);

    /// <summary>The doorway out, in world coordinates.</summary>
    public static Vector3 Exit => Deck.Materials + new Vector3(-HalfWidth, Deck.Eye, ExitZ);

    /// <summary>The powered door at the entrance, in world coordinates. The soundtrack takes the level of its
    /// motor from how far away this is, rather than from a number somebody chose — the walk is still several
    /// metres short of the door when it starts to move.</summary>
    public static Vector3 Way => Deck.Materials + new Vector3(EntranceX, Deck.Eye, -HalfLength);

    /// <summary>A point on the gallery floor at eye height: how far up the room, and how far off the centre
    /// line.</summary>
    public static Vector3 At(float along, float across = 0f) =>
        Deck.Materials + new Vector3(across, Deck.Eye, along);

    /// <summary>
    /// A bay in one of the side walls: two runs of wall each side of the opening, a sill under it, and a
    /// lined box behind.
    ///
    /// The sill is the difference between this and a doorway, and it is why <see cref="Fabric.PiercedWall"/>
    /// does not build it — a door goes to the floor and a case does not. What it deliberately does not have
    /// is a head: the recess runs to the ceiling, and that is the rotunda's lesson applied before it cost
    /// anything. A lintel over a recess is a downward-facing surface under lamps that are above it, so it
    /// receives nothing from them and comes out near black — correct shading, and a dark band across the
    /// top of the exhibit. A sill has the opposite problem, which is to say none: it faces up.
    /// </summary>
    /// <summary>
    /// The fifth exhibit: six materials you can name, on a shelf across the blind end of the room.
    ///
    /// <b>Everything else in this gallery exhibits a number.</b> The chart is metallic against roughness,
    /// forty-nine times; the case is six colours as dielectrics and again as metals; the row at the far end
    /// is one metal at five roughnesses. All of them are the model being taken apart, and none of them is
    /// the question a visitor actually arrives with, which is <i>can it do wood</i>. So this one answers
    /// that, six times, with no axis and no gradient: oak, leather, red plastic, glass, steel and gold.
    ///
    /// It goes on the north wall because that is the only wall the film ever looks at square-on. The walk
    /// stops twice at the far end with the five metals in frame, and from there the blind wall behind them
    /// fills the shot — so this is the one place in the room where an exhibit can be added without moving
    /// anything and still be looked at rather than passed.
    ///
    /// <b>They are slabs and not spheres, and that is a constraint rather than a preference.</b> A normal
    /// map is read in tangent space, a UV sphere's tangents degenerate at its poles, and four of these six
    /// carry relief that is the whole point of them — a leather sphere pinwheels at the top exactly as the
    /// bean bag in the lounge did. A flat specimen also happens to be what a material sample is: the thing
    /// in the tray at a builder's merchant is a rectangle, and it is a rectangle because you are meant to be
    /// comparing the surface and not the shape.
    ///
    /// The three that carry no relief are the three that should not. Gold and steel differ from each other
    /// in two numbers and share every map; glass has nothing on its face at all, because a pane that has
    /// been given a texture is not a pane.
    /// </summary>
    private static void Specimens(Node root, Material edge)
    {
        var shelf = new Node { Name = "exhibit.specimens" };
        root.Children.Add(shelf);

        var face = HalfLength - ShelfDepth / 2f + Bite / 2f;

        shelf.Children.Add(Fabric.Slab(
            new Vector3(4.7f, 0.05f, ShelfDepth + Bite),
            new Vector3(0f, ShelfY - 0.025f, face),
            edge,
            "shelf",
            Finish.Close));

        // Two brackets under it, because a shelf four metres long fixed to a wall by nothing is the detail
        // that makes a room read as drawn rather than as built.
        foreach (var side in new[] { -1f, 1f })
            shelf.Children.Add(Fabric.Slab(
                new Vector3(0.05f, 0.22f, ShelfDepth + Bite),
                new Vector3(side * 2.08f, ShelfY - 0.16f, face),
                edge,
                "shelf.bracket",
                Finish.Close));

        Material[] samples =
        [
            Finish.Timber(),
            Finish.Hide(),
            Finish.Plastic(0.62f, 0.14f, 0.11f),
            Finish.Glass(),
            Finish.Brushed(),
            Finish.Gold()
        ];

        const float wide = 0.40f;
        const float tall = 0.48f;
        const float deep = 0.055f;

        for (var i = 0; i < samples.Length; i++)
        {
            // Leaned back, and it is the difference between a metal specimen and a black rectangle.
            //
            // A metal has no diffuse term, so a flat plate shows nothing but the reflection of whatever is
            // in the mirror direction — and for a vertical plate viewed head on, that is whatever is behind
            // the camera. In this gallery that is fourteen metres of dim corridor, so the gold sample stood
            // upright rendered as a dark brown card and the steel one as a grey one, both of them under a
            // lamp they could not see. Fourteen degrees puts the ceiling in the mirror direction instead:
            // the coffers, the fixture over the stand, and the light on the plaster.
            //
            // It is also how a material sample is actually displayed, in every merchant's tray there has
            // ever been, and for the same reason — a board leaning back catches the room's light across its
            // face, which is the only way to see what a surface does.
            var holder = new Node
            {
                Name = "specimen.holder",
                Position = new Vector3(
                    (i - (samples.Length - 1) / 2f) * SamplePitch, ShelfY + tall / 2f + 0.02f, HalfLength - 0.19f),
                RotationDegrees = new Vector3(14f, 0f, 0f)
            };

            shelf.Children.Add(holder);

            const float x = 0f;
            const float y = 0f;
            const float z = 0f;

            // A frame round every one of them, which is a fitting and is also the fix for the one specimen
            // that could not be made to work without it. Glass over a plastered wall at any alpha that
            // reads as glass is a faint rectangle of nothing — there is no edge, so there is no object,
            // and what the eye reports is a smudge on the wall. Four strips of metal give it the edge a
            // sheet of glass has and the rest of the shelf gets the same treatment for free, because six
            // samples in six different frames would be six mountings and one exhibit is one mounting.
            //
            // Added before the sample rather than after it. The frame is opaque and the glass is not, and a
            // transparent surface has to be drawn after everything behind it — see Glazing in the
            // illuminator, which is the same rule stated for the same renderer.
            foreach (var (sx, sy, w, h) in new[]
                     {
                         (0f, (tall + 0.05f) / 2f, wide + 0.10f, 0.05f),
                         (0f, -(tall + 0.05f) / 2f, wide + 0.10f, 0.05f),
                         ((wide + 0.05f) / 2f, 0f, 0.05f, tall),
                         (-(wide + 0.05f) / 2f, 0f, 0.05f, tall)
                     })
                holder.Children.Add(Fabric.Slab(
                    new Vector3(w, h, deep + 0.02f),
                    new Vector3(x + sx, y + sy, z - 0.004f),
                    edge,
                    "specimen.frame",
                    Finish.Close));

            var sample = Fabric.Slab(
                new Vector3(wide, tall, deep),
                new Vector3(x, y, z),
                samples[i],
                "specimen",

                // Drawn at arm's length rather than at the building's pitch. A leather grain or a plank at
                // one image per metre and six tenths is camouflage — see Finish.Close, which exists for
                // exactly this and was written for the armchair.
                Finish.Close);

            // And marked, because SceneSnapshot sorts by render order and then by the order things went
            // into the tree, never by distance: back-to-front is the builder's job.
            if (samples[i].Blend == BlendMode.Alpha)
                sample.RenderOrder = 2;

            holder.Children.Add(sample);
        }
    }

    private static void Bay(Node root, float x, float z, float sill, Material material, Material edge)
    {
        var t = Deck.WallThickness;
        var inward = MathF.CopySign(1f, x);
        var back = x + inward * BayDepth;

        var bay = new Node { Name = "bay" };
        root.Children.Add(bay);

        // The sill: the wall's own thickness under the opening.
        bay.Children.Add(Fabric.Slab(
            new Vector3(t, sill, BayWidth),
            new Vector3(x + inward * t / 2f, sill / 2f, z),
            material));

        // And a metal nosing along it, standing a little into the room. It is what a case in a wall has and
        // a hole in a wall does not, and it is the one edge in this room the visitor could put a hand on.
        bay.Children.Add(Fabric.Slab(
            new Vector3(0.06f + Bite, 0.05f, BayWidth + 0.12f),
            new Vector3(x - inward * (0.06f - Bite) / 2f, sill - 0.025f, z),
            edge,
            "bay.nosing",
            Finish.Close));

        // The frame round the opening: two jambs and a head, in the same metal. Three slabs, and they are
        // what turns a recess into a case — the eye reads a lined opening as a fitting and an unlined one
        // as damage.
        foreach (var side in new[] { -1f, 1f })
            bay.Children.Add(Fabric.Slab(
                new Vector3(0.05f + Bite, Height - sill, 0.07f),
                new Vector3(x - inward * (0.05f - Bite) / 2f, (sill + Height) / 2f, z + side * (BayWidth + 0.07f) / 2f),
                edge,
                "bay.jamb",
                Finish.Close));

        // The back of the recess, and the two returns that line it.
        bay.Children.Add(Fabric.Slab(
            new Vector3(t, Height, BayWidth + 2f * t),
            new Vector3(back + inward * t / 2f, Height / 2f, z),
            material));

        // The two returns, and the one material in this room that is biased.
        //
        // A return lines the side of the recess, and the side of the recess is the wall's own cut face —
        // so the two are flush by design and not by accident: the return's face towards the room and the
        // wall's face towards the room are the same plane, and so are the return's face into the bay and
        // the wall's reveal. Both boxes are really there, over a quarter of a metre by three, and the depth
        // test cannot choose between two surfaces at one depth. It answers per pixel and per frame, and
        // what that looks like is a band of diagonal hatching up the jamb that crawls as the camera moves.
        //
        // Everywhere else in this building the answer is to overlap rather than abut — see Rotunda.Overlap.
        // It does not work here, because the two surfaces are meant to be one surface: any offset that
        // separates them puts a visible step in the reveal, and the step is worse than the shimmer. This is
        // exactly the case DepthBias is for, so the returns lose to the wall, everywhere, on purpose.
        var lining = material.Clone();
        lining.DepthBias = 4f;
        lining.DepthBiasSlope = 1f;
        lining.Name = "bay.return";

        foreach (var side in new[] { -1f, 1f })
            bay.Children.Add(Fabric.Slab(
                new Vector3(BayDepth, Height, t),
                new Vector3((x + back) / 2f, Height / 2f, z + side * (BayWidth + t) / 2f),
                lining));

        // The floor of the recess, level with the sill.
        bay.Children.Add(Fabric.Slab(
            new Vector3(BayDepth, sill, BayWidth),
            new Vector3((x + back) / 2f, sill / 2f, z),
            material));
    }
}
