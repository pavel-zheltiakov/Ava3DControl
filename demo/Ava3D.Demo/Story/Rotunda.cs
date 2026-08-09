using System.Numerics;
using Ava3D.Demo.Scenes;

namespace Ava3D.Demo.Story;

/// <summary>
/// The rotunda of forms: a round room with six niches in its wall, one solid turning in each, an arm on a
/// table out on the floor, and a low table in the middle where the same shapes stand twice, shaded two
/// ways.
///
/// The room is the argument. It is built out of boxes, cylinders, planes and two discs — which is the whole
/// of <see cref="Primitives"/> — and what it is built to show is <see cref="Primitives"/>. Six solids on
/// six plinths in a row would demonstrate the same builders and prove nothing about them, because nobody
/// arranges six solids in a line by accident and everybody recognises a room. The exhibit and the vitrine
/// are made of the same parts here, and that is the only way to say the parts are enough.
///
/// The niches do the work rule 2 does everywhere else. They are a metre and a quarter deep and a metre and
/// a half wide in a wall sector of two and a quarter, so nearly half a metre of wall stands between each
/// one and the next — and that pier is the whole of it. From anywhere on the walk the niche in front of you
/// is open and its neighbours are shut by their own jambs: you see two at a time and never all six, in a
/// room with no doors in it. The film's plan is normally enforced by walls between rooms; here it is
/// enforced by the wall of one room folding.
///
/// Twelve sectors of thirty degrees: six niches running from the entrance round the south side to the
/// east, two doorways a quarter turn apart, and four blank spans. The entrance faces west and the exit
/// faces north, so the exit is behind him for the whole of the walk and only comes into shot when he has
/// finished with the last niche and turned.
/// </summary>
internal sealed class Rotunda
{
    /// <summary>The inner face of the wall ring. Nine metres across, which is a room and not a hall.</summary>
    public const float Radius = 4.4f;

    /// <summary>
    /// Higher than the rest of the building, and only because of what he walks out of to get here.
    ///
    /// Height in architecture is comparative and there is no other way to spend it: three-nine after the
    /// passage's two-five reads as a room opening up, and the same three-nine entered from the antechamber
    /// would read as nothing at all. That is the entire reason <see cref="Passage"/> has a low ceiling.
    /// </summary>
    public const float Height = 3.9f;

    private const float NicheDepth = 1.25f;

    /// <summary>The opening. Narrower than the sector it is cut in, which is what leaves the piers.</summary>
    private const float NicheWidth = 1.5f;

    /// <summary>
    /// The head of the opening — and the only number in this room that took three goes.
    ///
    /// At two-seven, with a lid over the recess, the six niches had a metre and a quarter of downward-
    /// facing plaster over them, under lamps that are above it. It receives nothing from them, so it came
    /// out at about a tenth of the wall's brightness: correct shading, and six black bands in a lit room.
    /// Taking the openings to the ceiling removed the surface rather than lighting it, and cost the wall —
    /// six full-height slots read as a ring of fins with dark between them rather than as a wall with
    /// niches in it.
    ///
    /// Three-two is both. The recess still runs to the ceiling, so nothing is roofed and there is no lid;
    /// what closes the top of the opening is the wall's own lintel, and its underside is a quarter of a
    /// metre deep because that is how thick the wall is. Foreshortened from eye height three metres away
    /// it is a line rather than a band.
    /// </summary>
    private const float NicheHead = 3.2f;

    /// <summary>
    /// How much wider than its chord each wall slab is built, so adjacent ones overlap.
    ///
    /// A round wall made of flat slabs has a notch at every joint: two chords tangent to the same circle
    /// meet at their shared point on the circle, but the slabs are boxes tangent at their middles, so the
    /// inner corner is short by radius × (1 − cos 15°) — about fifteen centimetres here, and fifteen
    /// centimetres of nothing at eleven joints is eleven black seams up a lit wall. Overlapping the slabs
    /// closes it, and boxes that interpenetrate cost nothing and cannot z-fight, because neither of them
    /// is a face lying on the other.
    /// </summary>
    private const float Overlap = 0.3f;

    /// <summary>Where a niche's turntable stands, from the room's centre.</summary>
    private const float Stand = Radius + NicheDepth / 2f;

    /// <summary>Height of a turntable's top face.</summary>
    private const float TableTop = 0.34f;

    /// <summary>
    /// The circle he walks. Two and a bit metres out, which puts him just under three from whatever is in
    /// the niche beside him — the distance at which a metre-high solid is a metre-high solid rather than a
    /// wall of colour. The first pass had him at two point nine and every exhibit filled the frame with no
    /// room round it, which is the same mistake as standing too close to a painting and is just as easy to
    /// make when the person standing there is a number.
    /// </summary>
    private const float Walking = 2.2f;

    private const float TableRadius = 1.1f;

    /// <summary>
    /// How much of the table's width the exhibit on it is allowed to take, edge to edge.
    ///
    /// Just over three quarters, which leaves about a quarter of a metre of stone outside the widest solid.
    /// That margin is not decoration: it is the only thing that says the row is <i>on</i> the table from a
    /// view that cannot see where the two meet. A solid whose silhouette runs off the rim has floor behind
    /// it, and floor behind a thing is what an eye reads as air under it — which is the whole of why the
    /// middle of this room used to look like a conjuring trick.
    ///
    /// <b>The exhibit is fitted to the table and not the other way round</b>, which was the first thing
    /// tried and is the one the walk would not have. Widening the stone to 1.35 seats the row just as well
    /// and puts the rim where he stands: <c>AVA3D_WALK</c> had the capsule thirty-six centimetres inside
    /// <c>table.lip</c> against eleven before, over three times as many samples. The stop in front of this
    /// table is a shot that took some finding, and a room is not allowed to move a camera to flatter itself.
    /// </summary>
    private const float Seated = 0.78f;
    private const float TableHeight = 0.6f;

    /// <summary>
    /// The six niches, in the order he passes them, and the two doorways.
    ///
    /// Degrees round the room from due east, counting towards the south — so the entrance at 180 is due
    /// west, the exit at 90 is due north, and the six between them run the long way round. He turns right
    /// on the way in and the niches arrive one at a time for two hundred and seventy degrees, which is what
    /// makes the exit something he finds rather than something he was shown.
    /// </summary>
    private static readonly float[] Niches = [210f, 240f, 270f, 300f, 330f, 0f];

    private static readonly float[] Blanks = [30f, 60f, 120f, 150f];

    /// <summary>Where the arm stands: out on the floor, in front of the pier between the first two
    /// niches.</summary>
    private const float ArmAngle = 225f;

    private const float ArmRadius = 3.55f;

    public const float EntranceAngle = 180f;
    public const float ExitAngle = 90f;

    public Rotunda(Hall hall)
    {
        var root = hall.Add(Deck.RotundaRoom, Deck.Rotunda);

        // One material instance per role rather than one per wall. Fabric's properties build a new Material
        // every time they are read, which is right for a room made of six pieces and wrong for one made of
        // fifty: two walls that share a material are one state change and two that merely agree are two.
        // Two rungs up: a roughness map as well as a colour one. This is the room where the ladder starts
        // being visible rather than merely present — three lanterns raking a curved wall pick out where the
        // plaster is polished and where it is not, and the ring stops reading as one piece of card bent
        // round. Still no relief anywhere: everything you can see the edge of here is geometry, which is
        // what a room about <see cref="Primitives"/> owes its own argument. See <see cref="Grade"/>.
        var plaster = Finish.Plaster(Grade.Worked);
        var tile = Finish.Floor(Grade.Worked);
        var stone = Finish.Stone(Grade.Worked);

        // A disc, not a square. The floor reaches past the niches so their recesses have something under
        // them, and the corners a square would leave outside the wall would be visible through both
        // doorways — which is the one direction in this room where there is nothing to hide them.
        var pan = Primitives.Disc(Radius + NicheDepth + 0.4f, 64);

        root.Children.Add(new MeshNode(Fabric.Map(pan, tile, Vector3.Zero), tile)
        {
            Name = "floor"
        });

        // The ceiling reaches over the niches too, because the niches go all the way up to it. That is the
        // second answer to a problem the first answer only moved: a niche with a soffit over it has a
        // downward-facing surface under lamps that are above it, which receives nothing from them — right
        // shading, and six black bands across an otherwise lit room. Raising the environment's ground term
        // lifted them to about a tenth of the wall's brightness, which is a dark band rather than a hole
        // and still the first thing the eye goes to. Taking the niches to full height removes the surface
        // instead of lighting it, and a tall narrow recess is the better-looking of the two anyway.
        root.Children.Add(new MeshNode(Fabric.Map(pan, plaster, new Vector3(0f, Height, 0f)), plaster)
        {
            Name = "ceiling",
            Position = new Vector3(0f, Height, 0f),
            RotationDegrees = new Vector3(180f, 0f, 0f)
        });

        foreach (var angle in Blanks)
            root.Children.Add(Tangential(
                Chord(Radius) + Overlap, Height, Radius + Deck.WallThickness / 2f, Height / 2f, angle, plaster));

        foreach (var angle in new[] { EntranceAngle, ExitAngle })
        {
            // Fifty millimetres wider and thirty taller than the standard opening, which is the same fix
            // the lounge's east wall and the corridor's bulkhead take. The north one of these two meets the
            // gallery's south wall, and the two overlap by a quarter of a metre because that is what this
            // building does instead of abutting — so cutting the same hole in both puts two reveals at one
            // depth and two soffits at another, and what that looks like is hatching that crawls. The wider
            // hole loses; the reveal anybody sees is the gallery's.
            var door = Fabric.PiercedWall(
                Chord(Radius) + Overlap, Height, Deck.WallThickness,
                doorCentre: 0f, Deck.DoorWidth + 0.05f, Deck.DoorHeight + 0.03f, plaster);

            door.Position = Ring(angle, Radius + Deck.WallThickness / 2f);
            door.RotationDegrees = new Vector3(0f, Facing(angle), 0f);
            root.Children.Add(door);
        }

        // The second door, standing open in the west opening: oak, and heavier than the plastic one two
        // rooms back in every way a door can be heavier. It stands into the room rather than back against
        // the wall, because the wall it is in is curved and there is nothing for it to lie against — which
        // is a thing about round rooms and not a compromise. The visitor walks past its edge with three
        // tenths of a metre to spare, which is what walking through a doorway is actually like.
        root.Children.Add(Door.Hinged(
            new Vector3(-4.30f, 0f, Deck.DoorWidth / 2f + 0.02f), Finish.Timber(), swing: 15f));

        Tables = new Node[Niches.Length];
        Focus = new Vector3[Niches.Length];

        // The exhibits. The room asks each scene for its subject and then decides where it goes, which is
        // the whole of what mounting is — and here it decides to take one of them apart.
        var solids = new PrimitivesScene().BuildSubject()!;
        var pieces = solids.Children.ToArray();

        if (pieces.Length != Niches.Length)
            throw new InvalidOperationException(
                $"the rotunda has {Niches.Length} niches and Primitives brought {pieces.Length} solids");

        for (var i = 0; i < Niches.Length; i++)
        {
            Recess(root, Niches[i], plaster);

            Tables[i] = new Node { Name = $"turntable.{i}", Position = Ring(Niches[i], Stand) };
            Tables[i].Children.Add(Drum(0.62f, stone));
            root.Children.Add(Tables[i]);

            // Six children of one turntable become six exhibits with a niche each. Re-parenting is a
            // supported move — NodeCollection.Add detaches from the old parent — and it is legitimate here
            // for a reason worth stating: the row is PrimitivesScene's arrangement of its solids and not a
            // fact about them, and the scene is rebuilt from a factory every time it is selected, so the
            // copy the room takes apart is the room's own.
            //
            // Seated on the drum by its own lowest point rather than by the height the scene gave it.
            //
            // The two disagree, and taking the scene's number is what left the sphere floating a third of
            // a metre over its plinth: PrimitivesScene puts the sphere's centre on its own ground line, so
            // the sphere hangs above the floor there too — which reads as a design choice in a row of six
            // and as a mistake on a plinth of its own. LocalBounds is the mesh's own extent, and it is the
            // only thing in the exchange that knows where the bottom of a torus is.
            var rest = TableTop - pieces[i].LocalBounds.Min.Y;

            Tables[i].Children.Add(pieces[i]);
            pieces[i].Position = new Vector3(0f, rest, 0f);

            // What the walk aims at. Taken from where the solid actually ended up rather than from a
            // height that reads about right for all six, because it is not the same for all six — aiming
            // at a fixed 1.2 puts the box low in frame and the sphere high, and both look like a mistake
            // nobody can name.
            Focus[i] = Deck.Rotunda + Ring(Niches[i], Stand, TableTop + pieces[i].LocalBounds.Size.Y / 2f);
        }

        // The arm, on a table of its own out on the floor. It is the only exhibit in the room not in a
        // niche and that is the point of it: a niche is for something that stands still, and this is three
        // rotations composing — the table, the hub bolted to it, and each arm on its own axis. Standing it
        // where you can walk round it is what makes the composing visible.
        ArmTable = new Node { Name = "turntable.arm", Position = Ring(ArmAngle, ArmRadius) };
        ArmTable.Children.Add(Drum(0.62f, stone));
        ArmTable.Children.Add(new MeshNode(Primitives.Cylinder(0.05f, 0.06f, 0.5f, 12), stone)
        {
            Position = new Vector3(0f, TableTop + 0.25f, 0f),
            Name = "post"
        });

        Arm = new TransformsScene();

        var hub = Arm.BuildSubject()!;
        hub.Position = new Vector3(0f, TableTop + 0.5f, 0f);
        hub.Scale = new Vector3(0.30f);
        ArmTable.Children.Add(hub);
        root.Children.Add(ArmTable);

        ArmFocus = ArmAt;

        // The middle of the room, and the one place in it where two exhibits have to be seen at once. The
        // pairs differ in their shading and not in their silhouette, so they are only a comparison if they
        // are in the same frame in the same light — which is why they are on a table he walks up to rather
        // than in two niches he sees one at a time.
        root.Children.Add(Drum(TableRadius, stone, TableHeight - 0.05f));
        root.Children.Add(new MeshNode(Primitives.Cylinder(TableRadius + 0.07f, TableRadius + 0.07f, 0.05f, 48), stone)
        {
            Position = new Vector3(0f, TableHeight - 0.025f, 0f),
            Name = "table.lip"
        });

        Pairs = new FlatShadingScene();

        // Sized to the table and seated on it, rather than scaled by a number and floated above it.
        //
        // Both halves of that were wrong and they were wrong together, which is why the six solids read as
        // hanging in the air over the middle of the room. The scale was a flat 0.24, and the row is eight
        // and four fifths of a unit across — two metres eleven on a table two metres twenty wide, so the
        // outermost solid finished four centimetres from the rim and any view from below put it past the
        // edge with floor behind it. The height was a flat fifteen centimetres above the top face, which at
        // that scale left the drum floating a centimetre clear of a surface it is supposed to be standing
        // on. Neither number asked the exhibit how big it is.
        //
        // This is the same lesson the niches learned and the same fix they use — see the seating above, and
        // the note there about the sphere floating over its plinth. LocalBounds is no good here because a
        // group node owns no mesh and reports nothing; Bounds is the subtree's extent with the node's own
        // transform applied, which is exactly the question being asked.
        var drums = Pairs.BuildSubject()!;
        drums.Position = Vector3.Zero;

        var row = drums.Bounds;
        var reach = MathF.Max(row.Size.X, row.Size.Z);

        drums.Scale = new Vector3(reach > 1e-4f ? TableRadius * 2f * Seated / reach : 0.24f);
        drums.Position = new Vector3(0f, TableHeight - drums.Bounds.Min.Y, 0f);
        root.Children.Add(drums);

        // Four lamps in a nine-metre room: one over the table and three round the ring. The niches are
        // recesses and stay dimmer than the floor, which is what a recess is for and is why six lamps —
        // one a niche — would have been the wrong answer even if there had been six slots to spend.
        Centre = Fabric.Ceiling(Deck.Rotunda, new Vector3(0f, Height - 0.12f, 0f), 5.2f, 9f);
        root.Children.Add(Centre.Fixture);

        Lanterns =
        [
            Lantern(root, 240f),
            Lantern(root, 330f),
            Lantern(root, 60f)
        ];

        return;

        Lamp Lantern(Node parent, float angle)
        {
            var lamp = Fabric.Ceiling(Deck.Rotunda, Ring(angle, 3.2f, Height - 0.12f), 4.2f, 8.5f);
            parent.Children.Add(lamp.Fixture);

            return lamp;
        }
    }

    /// <summary>The six niche turntables, in walk order. The chapter turns them.</summary>
    public Node[] Tables { get; }

    /// <summary>The table the arm stands on, which turns with everything on it.</summary>
    public Node ArmTable { get; }

    /// <summary>The nested hierarchy on that table, kept so the chapter can run its own animation rather
    /// than reimplementing it.</summary>
    public TransformsScene Arm { get; }

    /// <summary>The smooth-and-faceted pairs on the centre table, likewise.</summary>
    public FlatShadingScene Pairs { get; }

    /// <summary>The lamp over the table.</summary>
    public Lamp Centre { get; }

    /// <summary>The three round the wall, in walk order: over the second niche, the fifth, and the way
    /// out.</summary>
    public Lamp[] Lanterns { get; }

    /// <summary>The middle of each niche's solid, in world coordinates. What the walk looks at.</summary>
    public Vector3[] Focus { get; }

    /// <summary>The middle of the arm.</summary>
    public Vector3 ArmFocus { get; }

    /// <summary>
    /// The same point, without needing the room to have been built.
    ///
    /// <see cref="ArmFocus"/> is what the walk aims at and is an instance property because everything else
    /// the chapter uses is; this is for <see cref="Soundtrack"/>, which has the film but not the rooms in
    /// it and needs somewhere to measure a motor from.
    /// </summary>
    public static Vector3 ArmAt => Deck.Rotunda + Ring(ArmAngle, ArmRadius, TableTop + 0.5f);

    /// <summary>Where he stands to look into the niche at <paramref name="index"/>, at eye height.</summary>
    public static Vector3 Standing(int index) => Inside(Niches[index]);

    /// <summary>Where he stands to look at the arm. Further in than the walking circle, because the arm is
    /// out on the floor rather than back in a niche and two metres from it is the same distance from the
    /// subject as the niches get.</summary>
    public static Vector3 FacingArm => Inside(ArmAngle, 1.5f);

    /// <summary>The middle of the doorway he comes in by, in world coordinates.</summary>
    public static Vector3 Entrance =>
        Deck.Rotunda + Ring(EntranceAngle, Radius + Deck.WallThickness / 2f, Deck.Eye);

    /// <summary>The middle of the doorway out.</summary>
    public static Vector3 Exit =>
        Deck.Rotunda + Ring(ExitAngle, Radius + Deck.WallThickness / 2f, Deck.Eye);

    /// <summary>A point on the walking circle, at eye height.</summary>
    public static Vector3 Inside(float degrees, float radius = Walking) =>
        Deck.Rotunda + Ring(degrees, radius, Deck.Eye);

    /// <summary>The drums on the centre table, to look at.</summary>
    public static Vector3 Table => Deck.Rotunda + new Vector3(0f, TableHeight + 0.2f, 0f);

    /// <summary>How wide one sector's chord is at a given radius. Twelve sectors, so fifteen degrees each
    /// side of the middle.</summary>
    private static float Chord(float radius) => 2f * radius * MathF.Sin(15f * MathF.PI / 180f);

    private static Vector3 Ring(float degrees, float radius, float y = 0f)
    {
        var t = degrees * MathF.PI / 180f;
        return new Vector3(MathF.Cos(t) * radius, y, MathF.Sin(t) * radius);
    }

    /// <summary>
    /// The yaw that turns a slab built along X into one lying along the wall at <paramref name="degrees"/>.
    ///
    /// Negative angle plus a quarter turn, and it is worth deriving once rather than guessing six times: a
    /// yaw of φ sends the slab's own X to (cos φ, 0, −sin φ), the wall at θ runs along (−sin θ, 0, cos θ),
    /// and the only φ that matches both components is −(θ + 90).
    /// </summary>
    private static float Facing(float degrees) => -(degrees + 90f);

    private static MeshNode Tangential(
        float width, float height, float radius, float y, float degrees, Material material)
    {
        var slab = Fabric.Slab(new Vector3(width, height, Deck.WallThickness), Ring(degrees, radius, y), material);
        slab.RotationDegrees = new Vector3(0f, Facing(degrees), 0f);

        return slab;
    }

    /// <summary>A low stone drum to stand something on.</summary>
    private static MeshNode Drum(float radius, Material material, float height = TableTop) =>
        new(Fabric.Map(Primitives.Cylinder(radius, radius, height, 40), material, Vector3.Zero), material)
        {
            Position = new Vector3(0f, height / 2f, 0f),
            Name = "drum"
        };

    /// <summary>
    /// One niche: two piers, a back and two sides, floor to ceiling.
    ///
    /// All of it is built in the niche's own frame — +X along the wall, −Z into the recess — and the node
    /// carrying it is what gets turned to face out of the room. Doing the trigonometry once, on the node,
    /// rather than six times on its pieces is most of why this reads at all.
    ///
    /// The piers are the part that makes it six niches rather than one recess. The first pass gave every
    /// sector a niche the full width of the sector, so nothing was left between them to be a jamb, and a
    /// hundred and eighty degrees of wall came out as one continuous slot with thin fins in it. An opening
    /// narrower than the sector it is cut in leaves half a metre of wall each side, and that half metre is
    /// the whole of what closes the neighbour you are not standing in front of.
    ///
    /// The recess itself runs to the ceiling — see <see cref="NicheHead"/> for why there is no lid over it
    /// and why the opening is nonetheless shorter than the room.
    /// </summary>
    private static void Recess(Node root, float angle, Material plaster)
    {
        var t = Deck.WallThickness;

        var niche = new Node
        {
            Name = "niche",
            Position = Ring(angle, Radius),
            RotationDegrees = new Vector3(0f, Facing(angle), 0f)
        };

        var front = Fabric.PiercedWall(
            Chord(Radius) + Overlap, Height, t, doorCentre: 0f, NicheWidth, NicheHead, plaster);

        front.Position = new Vector3(0f, 0f, -t / 2f);
        niche.Children.Add(front);

        niche.Children.Add(Fabric.Slab(
            new Vector3(NicheWidth + 2f * t, Height, t),
            new Vector3(0f, Height / 2f, -(NicheDepth + t / 2f)),
            plaster,
            "niche.back"));

        foreach (var side in new[] { -1f, 1f })
            niche.Children.Add(Fabric.Slab(
                new Vector3(t, Height, NicheDepth),
                new Vector3(side * (NicheWidth + t) / 2f, Height / 2f, -NicheDepth / 2f),
                plaster,
                "niche.side"));

        root.Children.Add(niche);
    }
}
