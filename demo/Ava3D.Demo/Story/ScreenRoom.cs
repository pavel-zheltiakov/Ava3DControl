using System.Numerics;
using Ava3D.Demo.Scenes.Arcade;

namespace Ava3D.Demo.Story;

/// <summary>
/// The lounge: four televisions on a bench, an armchair, a bean bag, a console on a low table, a lit case
/// on the west wall, and a half-round alcove in the north wall with a mirror ball in it.
///
/// It is the only room in the building that is not an exhibition room, and everything about it follows from
/// that. The four exhibits along the south wall are still exhibits — four games, four files, four pixel
/// buffers handed to four materials — but the room they are in is furnished rather than lit, low rather
/// than tall, and has somewhere to sit. That is the argument it makes and it is not a technical one: nine
/// minutes of walking past things needs a room you would stay in, and a demo that is only ever a catalogue
/// is a catalogue.
///
/// The case is the one thing in here that is presented rather than lived with, and it is allowed because it
/// is about what is already on the walls: the first game's sprites, taken off the tube and stood up as
/// billboards, with that same tube's live picture behind them. See <see cref="Diorama"/>.
///
/// It is also the top of the <see cref="Grade"/> ladder. The walls are riveted panelling with a colour map,
/// a roughness map, a normal map and an occlusion map on them; the floor is boards; the chair is leather
/// and the bean bag is cloth, both of them carrying relief that is not geometry. Four rooms ago the
/// antechamber was flat colour and two numbers and nobody watching thought anything was missing.
///
/// The alcove is the beat. It is dark and empty for the whole of the time he is watching the games, and it
/// is directly behind the chair — so when he sits down and turns round there is nothing there, and then
/// there is.
///
/// <b>Nothing in this room has a name.</b> Not the games, not the genres, and not the console on the table,
/// which is a grey box with a slot in the top and two pads on leads and is recognisable to anyone who ever
/// owned one without being any of them. A jumping figure on bricks is a genre; a named jumping figure on
/// bricks is somebody's property, and the same is true of a moulding.
/// </summary>
internal sealed class ScreenRoom
{
    public const float Width = 9f;
    public const float Depth = 7f;

    /// <summary>Where the bench runs, and how high its top is.</summary>
    private const float BenchZ = -2.7f;

    private const float BenchTop = 0.55f;

    /// <summary>The alcove: a half cylinder let into the north wall, opening south into the room. Both of
    /// these are read by <see cref="Glitter"/>, which casts the ball's light at them.</summary>
    public const float ApseRadius = 2f;

    public static readonly Vector3 ApseAt = new(0f, 0f, Depth / 2f);

    /// <summary>
    /// The way out, moved west so the alcove can have the middle of the north wall.
    ///
    /// It was at −1.5 when this room's only job was four televisions. The alcove needs four metres of that
    /// wall and needs them centred, because it has to be what is behind the chair — and the chair has to
    /// face the bench. Two of those three are fixed by the room's furniture, so the door is the one that
    /// moves. Nothing had been built beyond it yet, which is the only reason this was free — the alarm
    /// corridor was measured from where it ended up rather than the other way round. See
    /// <see cref="Deck.Corridor"/>.
    /// </summary>
    private const float ExitX = -3.3f;

    /// <summary>
    /// And the way in, <b>which is in the north wall now and used to be in the east.</b>
    ///
    /// The east wall was where the material gallery met this room, back to back, and it was the whole of
    /// the route between chapters 3 and 4 — which left nowhere between them to put the two rooms that
    /// needed to go there. The route now goes east out of the gallery, through the studio and the pattern
    /// shop, and comes back down the link to this wall. See <see cref="Deck.Studio"/>.
    ///
    /// It is in the same wall as the way out, seven metres east of it, with the alcove between them. Two
    /// doors in one wall would normally be a breach of rule 2 and here it is not: what the rule forbids is
    /// seeing through one into the next room, and these two face the same way. Standing in either, the
    /// other is a slot in the wall beside you.
    ///
    /// Three and a fifth, which is what is left. The alcove owns the middle four metres of a nine-metre
    /// wall and the exit owns three metres three west of centre; between the alcove's east jamb and the
    /// corner there are two and a half metres of wall, and a doorway one and a fifth wide goes in the
    /// middle of them with a little over half a metre of return on each side.
    /// </summary>
    private const float EntranceX = 3.2f;

    /// <summary>
    /// Where a seat stands and where its console stands, in front of every set.
    ///
    /// Four stations rather than one seating group, and that is the difference between a room with a
    /// television in it and a room somebody plays in. Every set has something to sit in a metre in front of
    /// it, a crate under it with a console on the crate, and pads on leads reaching back to the seat. It is
    /// the same four objects four times and it costs almost nothing, because the four sets were already
    /// four of everything.
    ///
    /// A metre from the glass is close, and it is right. That is the distance somebody sits from a small
    /// tube with a pad in their hands, and it is nothing like the distance they stand at to look at one.
    /// </summary>
    private const float SeatZ = -1.3f;

    private const float CrateZ = -2.05f;

    /// <summary>
    /// The sofa, in front of the alcove and turned to face it, and standing there from the first frame of
    /// the chapter.
    ///
    /// It has to be there from the start. A room that grows a sofa when the music begins is a room the
    /// viewer cannot trust, and the whole of the beat that follows is that the alcove was empty <i>and the
    /// seat in front of it was not</i> — a sofa pointed at a blank wall is a question, and four seconds
    /// later it is answered.
    /// </summary>
    private static readonly Vector3 SofaAt = new(0f, 0f, 0.85f);

    /// <summary>A few degrees each, so four seats in a row are four seats somebody sat in rather than a
    /// parade. Fixed rather than random: everything in this film has to be the same every run.</summary>
    private static readonly float[] Askew = [-7f, 6f, -4f, 9f];

    /// <summary>
    /// The four sets, in the order the visitor reaches them — which is right to left, because he comes in
    /// through the east door. Waking them in walk order rather than in list order is the difference between
    /// a room responding to him and a room running a sequence he happens to be in.
    /// </summary>
    private static readonly float[] Along = [3f, 1f, -1f, -3f];

    /// <summary>How many panels a bean bag is sewn from, and how many segments each panel is drawn with.
    /// Six is what these are actually made of; eight segments is enough for a panel to be a pillow rather
    /// than a facet.</summary>
    private const int Gores = 6;

    private const int PerGore = 10;

    /// <summary>Bands up the side of a bag. Eighteen carries the hollow somebody left in the top of it;
    /// at eight the hollow is a crease.</summary>
    private const int Bands = 18;

    /// <summary>A bag at its widest, in metres of radius, and how tall it stands.</summary>
    private const float BagRadius = 0.60f;

    private const float BagHeight = 0.88f;

    /// <summary>Where the base panel is sewn on. The hem stands this far off the boards and the piping over
    /// the seam is a tube of the same radius, so the one that covers the join also closes the gap.</summary>
    private const float BagHem = 0.024f;

    /// <summary>How far the top of a bag leans away from the bench. A bean bag somebody watches a screen
    /// from slumps backwards, which is why these are turned as well as placed.</summary>
    private const float BagLean = 0.19f;

    public ScreenRoom(Hall hall)
    {
        var root = hall.Add(Deck.ScreensRoom, Deck.Screens);

        // The top of the ladder. See <see cref="Grade"/> — this is the only room in the film where every
        // map slot the shading model has is carrying something, and it is the last room before the film
        // stops being about rooms.
        var panel = Finish.Panelling();
        var boards = Finish.Boards();
        var plaster = Finish.Plaster(Grade.Dressed);

        var halfWidth = Width / 2f;
        var halfDepth = Depth / 2f;

        root.Children.Add(Fabric.Sheet(Width, Depth, 0f, boards, "floor"));
        root.Children.Add(Fabric.Lid(Width, Depth, Deck.Ceiling, plaster, "ceiling"));

        // <b>Blind, and it carried the way in for four rooms.</b> The gallery met this room here, back to
        // back, and the two walls overlapped by a hundred and twenty-five millimetres and then cut the same
        // hole in it — same width, same height, same centre, because both read Deck.DoorWidth and both were
        // on the same doorway. Inside the overlap the two reveals were one plane and the two soffits were
        // another, and the depth test answers that per pixel and per frame, which was a band of crawling
        // diagonal hatching down the jamb of the one doorway in the film that got walked through in both
        // directions. The fix was to make one of the openings bigger, because interpenetrating solids only
        // work if they interpenetrate everywhere and a hole is a solid turned inside out.
        //
        // The route turned east and took the opening with it, so what is left here is the simple case: two
        // solid slabs a quarter of a metre apart, overlapping, with nothing cut in either. Both halves of
        // the old fix are gone with the hole they were fixing.
        var east = Fabric.Wall(Depth, Deck.Ceiling, Deck.WallThickness, panel);
        east.Position = new Vector3(halfWidth, 0f, 0f);
        east.RotationDegrees = new Vector3(0f, 90f, 0f);
        root.Children.Add(east);

        // The north wall has three holes in it now and PiercedWall makes one, so it is written as explicit
        // runs along X — the same thing the gallery does with its sides, and for the same reason: a wall
        // with its own arithmetic in it is a wall whose openings are where the numbers say they are.
        //
        // The alcove's mouth runs floor to ceiling with no head over it. That is the lesson the rotunda's
        // niches paid for twice: a downward-facing soffit under lamps that are above it receives nothing
        // but the environment's ground term, so it renders as a dark band across the top of the opening —
        // right shading, and a hole in the picture. Here it would be worse, because the only light in the
        // alcove comes from below the soffit.
        var doorFrom = ExitX - Deck.DoorWidth / 2f;
        var doorTo = ExitX + Deck.DoorWidth / 2f;
        var wayFrom = EntranceX - Deck.DoorWidth / 2f;
        var wayTo = EntranceX + Deck.DoorWidth / 2f;

        North(-halfWidth, doorFrom);
        North(doorFrom, doorTo, Deck.DoorHeight);
        North(doorTo, -ApseRadius);
        North(ApseRadius, wayFrom);
        North(wayFrom, wayTo, Deck.DoorHeight);
        North(wayTo, halfWidth);

        var south = Fabric.Wall(Width, Deck.Ceiling, Deck.WallThickness, panel);
        south.Position = new Vector3(0f, 0f, -halfDepth);
        root.Children.Add(south);

        var west = Fabric.Wall(Depth, Deck.Ceiling, Deck.WallThickness, panel);
        west.Position = new Vector3(-halfWidth, 0f, 0f);
        west.RotationDegrees = new Vector3(0f, 90f, 0f);
        root.Children.Add(west);

        Alcove(root);

        // The bench, and the four sets on it.
        root.Children.Add(Fabric.Slab(
            new Vector3(Width - 1.2f, BenchTop, 0.9f),
            new Vector3(0f, BenchTop / 2f, BenchZ),
            boards,
            "bench"));

        Games = [new GrassBricksScene(), new CoinMazeScene(), new RunnerScene(), new BlocksScene()];
        Screens = new Material[Along.Length];

        for (var i = 0; i < Along.Length; i++)
        {
            Screens[i] = ArcadeScene.Glass();
            root.Children.Add(Television(Along[i], Screens[i], i));
        }

        // The case on the west wall, holding the first game's sprites, and handed that game's own screen
        // material rather than a copy of it. See <see cref="Diorama"/> for why it is a share.
        Diorama.Build(root, Screens[0]);

        Furnish(root);

        // Four warm lamps: two over the bench, one over the furniture, one at the way out. That is every
        // slot there is, which is exactly why the alcove's lights cannot be added to them and have to
        // replace them — and why the chapter can only turn the disco on once the room is dark. The
        // constraint wrote the beat.
        //
        // All four are lower than they were when this was only a room with televisions in it. It is meant
        // to be somewhere you would sit down, and nobody sits down under a working light.
        Bench =
        [
            Fabric.Ceiling(Deck.Screens, new Vector3(2.2f, Deck.Ceiling - 0.1f, -1.4f), 2.4f, 7f),
            Fabric.Ceiling(Deck.Screens, new Vector3(-2.2f, Deck.Ceiling - 0.1f, -1.4f), 2.4f, 7f)
        ];

        Lounge = Fabric.Ceiling(Deck.Screens, new Vector3(0.6f, Deck.Ceiling - 0.1f, 1.5f), 2.6f, 6.5f);
        Doorway = Fabric.Ceiling(Deck.Screens, new Vector3(ExitX, Deck.Ceiling - 0.1f, 2.4f), 1.9f, 5.5f);

        Warm = [Bench[0], Bench[1], Lounge, Doorway];

        foreach (var lamp in Warm)
            root.Children.Add(lamp.Fixture);

        return;

        void North(float from, float to, float bottom = 0f)
        {
            if (to - from < 0.01f || Deck.Ceiling - bottom < 0.01f)
                return;

            root.Children.Add(Fabric.Slab(
                new Vector3(to - from, Deck.Ceiling - bottom, Deck.WallThickness),
                new Vector3((from + to) / 2f, (bottom + Deck.Ceiling) / 2f, halfDepth + Deck.WallThickness / 2f),
                panel,
                "wall"));
        }
    }

    /// <summary>The four games, in the order their sets wake.</summary>
    public ArcadeScene[] Games { get; }

    /// <summary>The four screens, one per game, in the same order.</summary>
    public Material[] Screens { get; }

    /// <summary>The two over the bench.</summary>
    public Lamp[] Bench { get; }

    /// <summary>The one over the chair and the table.</summary>
    public Lamp Lounge { get; }

    /// <summary>The one at the way out.</summary>
    public Lamp Doorway { get; }

    /// <summary>All four warm lamps, for switching the room off in one line. The same four the chapter
    /// hands the slots to, and the same four it has to have at zero before the alcove can have them.</summary>
    public Lamp[] Warm { get; }

    /// <summary>
    /// The mirror ball and everything the mirror ball throws, on one node, turning together.
    ///
    /// The dots are children of the same node as the ball rather than being computed from it, which is the
    /// whole trick: a rotation applied once at the alcove's axis moves the ball on its own centre — it is
    /// standing on that axis — and sweeps every dot round the wall at the radius it was placed at. One
    /// number a frame, and the light and the thing throwing it cannot come apart.
    /// </summary>
    public Node Mirror { get; private set; } = null!;

    /// <summary>The four coloured lights that orbit inside the alcove. No fixtures: what a viewer is meant
    /// to think is throwing this light is the ball.</summary>
    public PointLight[] Beams { get; private set; } = null!;

    /// <summary>Every dot the ball throws and every shaft it throws them down, over the whole room. Handed
    /// the ball's own yaw so the two cannot come apart — see <see cref="Glitter.Update"/>.</summary>
    public Glitter Show { get; private set; } = null!;

    /// <summary>
    /// The light show off, which somebody has to say out loud and three callers do.
    ///
    /// <b>A dot's colour is state, and it is the only state in this room that outlives the chapter that
    /// sets it.</b> Everything else here is a light, and a light that is not in the four slots contributes
    /// nothing whatever it is set to. The dots are the exact opposite — additive, unlit, no depth write,
    /// which is what lets the alcove keep working when every lamp in the building has gone — so a dot left
    /// bright is bright in any room it can be seen from, for as long as it can be seen.
    ///
    /// That is what put a row of white squares on the alcove wall. The materials were built near-white
    /// because that is what the show fades <i>toward</i>, and chapter 3 has this room standing open through
    /// its doorway for the best part of a minute before anything in chapter 4 has run — so the first sight
    /// of the lounge was its dots at full, going out the instant the chapter changed. The same hole is there
    /// on the way back: seeking from chapter 4 to chapter 3 leaves them wherever the show had got to.
    ///
    /// So it is called from the builder, and by every chapter that shows this room without driving it.
    /// </summary>
    public void Blackout() => Show.Off();

    /// <summary>
    /// Where the visitor is when he is standing in front of set <paramref name="index"/>.
    ///
    /// Two and three quarter metres back, which is further than it feels like it should be and is the
    /// distance a person actually stands from a television. At a metre the set fills the frame and the room
    /// is gone, and the room is half of what this chapter is showing.
    /// </summary>
    public static Vector3 InFrontOf(int index) =>
        Deck.Screens + new Vector3(Along[index], Deck.Eye, -0.3f);

    /// <summary>The middle of set <paramref name="index"/>'s glass, for the walk to look at.</summary>
    public static Vector3 Glass(int index) =>
        Deck.Screens + new Vector3(Along[index], BenchTop + 0.52f, BenchZ + 0.4f);

    /// <summary>Where the doorway out is, in world coordinates.</summary>
    public static Vector3 Exit => Deck.Screens + new Vector3(ExitX, Deck.Eye, Depth / 2f);

    /// <summary>Where the doorway in is — in the north wall, east of the alcove. See
    /// <see cref="EntranceX"/> for why it is not in the east wall any more.</summary>
    public static Vector3 Entrance => Deck.Screens + new Vector3(EntranceX, Deck.Eye, Depth / 2f);

    /// <summary>The sofa, seen from the door. What the last second of chapter 3 and the first of chapter 4
    /// look at — so the thing the alcove is going to happen in front of is in the very first frame.</summary>
    public static Vector3 Sitting => Deck.Screens + SofaAt + new Vector3(0f, 0.75f, 0f);

    /// <summary>The console in front of set <paramref name="index"/>.</summary>
    public static Vector3 Console(int index) =>
        Deck.Screens + new Vector3(Along[index], 0.52f, CrateZ);

    /// <summary>
    /// Eye height on the sofa, forward of the middle of it and facing the alcove.
    ///
    /// A metre twenty-two rather than a metre seven, which is the whole of what sitting down is here. The
    /// sofa faces north, so this is the only seat in the room that is not pointed at a television — and
    /// sitting in it is what puts the four screens behind him and the empty alcove in front.
    /// </summary>
    public static Vector3 Seat => Deck.Screens + SofaAt + new Vector3(0f, 1.22f, 0.35f);

    /// <summary>What he watches from the chair: the middle of the bench, not any one set.</summary>
    public static Vector3 Bank => Deck.Screens + new Vector3(0.2f, BenchTop + 0.5f, BenchZ + 0.4f);

    /// <summary>The mirror ball, in world coordinates.</summary>
    public static Vector3 Ball => Deck.Screens + ApseAt + new Vector3(0f, 2.35f, 0f);

    /// <summary>
    /// What the walk aims at when he turns round.
    ///
    /// Well below the ball, and that was arrived at by aiming at the ball first and getting a frame that
    /// was four fifths blank wall. Half the light in this alcove lands on its floor — the pools sweep it
    /// and the dots run across it — and none of that is in shot from a chair if the camera is looking up.
    /// A metre and a half puts the ball in the top third and the stage under it.
    /// </summary>
    public static Vector3 Stage => Deck.Screens + ApseAt + new Vector3(0f, 1.45f, 0.25f);

    /// <summary>Where a beam is at <paramref name="degrees"/> round the alcove, in world coordinates.</summary>
    public static Vector3 Orbit(float degrees)
    {
        var t = degrees * MathF.PI / 180f;
        return Ball + new Vector3(MathF.Cos(t) * 1.15f, -0.15f, MathF.Sin(t) * 1.15f);
    }

    /// <summary>
    /// The alcove: one cylinder, half swept, turned inside out.
    ///
    /// Every other room in this building is boxes, and this one is a cylinder for a reason that is not
    /// decoration. The dots a mirror ball throws travel; on a flat wall they would have to travel in a
    /// straight line and change size as they went, and there is no way to do that with a rigid node. On a
    /// wall that is a constant distance from the ball they travel by <i>rotating</i>, which is one number a
    /// frame applied to the node they all hang off. The shape of the room is what makes the effect cost
    /// nothing, and it is also what makes it correct.
    ///
    /// <see cref="Fabric.Inverted"/> is doing the other half. A cylinder's normals point out of it, and a
    /// room built from one is lit as though the lamps were outside its wall — which renders black, however
    /// bright the room is, and is the same mistake <see cref="Fabric.Lid"/> exists to stop one dimension
    /// further round. Reversing all of them at once also turns the two end caps into a floor and a ceiling
    /// facing the right way, which is a nice accident and worth relying on: the top cap pointed up and now
    /// points down, and the bottom did the reverse.
    ///
    /// It is left untextured on purpose, and it is the one surface in this room that is. It is a screen for
    /// coloured light, and a screen with rivets on it is a wall.
    /// </summary>
    private void Alcove(Node root)
    {
        var shell = new Material
        {
            BaseColor = new Vector4(0.17f, 0.17f, 0.19f, 1f),
            Roughness = 0.78f,
            Name = "alcove"
        };

        root.Children.Add(new MeshNode(
            Fabric.Inverted(Primitives.Cylinder(
                ApseRadius, ApseRadius, Deck.Ceiling, 44, capped: true, sweepDegrees: 180f)),
            shell)
        {
            Name = "alcove",
            Position = ApseAt + new Vector3(0f, Deck.Ceiling / 2f, 0f)
        });

        // The floor of the little stage, spilling out of the alcove into the room. Polished, and much more
        // so than anything else in the building — a coloured light on a matt floor is a smudge and on a
        // polished one it is a pool with the ball reflected in it.
        // Polished, but not a mirror. At a roughness of a seventh the specular lobe is narrower than the
        // angle one pixel of this floor covers when it is seen from a chair — four lights moving across it
        // then read as a band of static rather than as reflections, which is specular aliasing and is the
        // one artefact in real-time shading that gets worse the better the material is.
        root.Children.Add(new MeshNode(Primitives.Disc(1.85f, 48), new Material
        {
            BaseColor = new Vector4(0.13f, 0.13f, 0.15f, 1f),
            Metallic = 0.05f,
            Roughness = 0.30f,
            Name = "stage"
        })
        {
            Name = "stage",
            Position = ApseAt + new Vector3(0f, 0.02f, 0f)
        });

        // The stem, which does not turn. It is bolted to the ceiling and the ball is not.
        root.Children.Add(new MeshNode(Primitives.Cylinder(0.014f, 0.014f, 0.62f, 8), Fabric.DarkMetal)
        {
            Name = "stem",
            Position = ApseAt + new Vector3(0f, 2.89f, 0f)
        });

        Mirror = new Node { Name = "mirror", Position = ApseAt };

        // A dark moulded globe with lenses in it, and it was a faceted chrome sphere until the show grew up.
        //
        // Flat normals on a coarse sphere were right when the ball was the only thing making the light: each
        // facet caught a different one of the four orbiting lamps at a different moment, which is the whole
        // reason anybody ever hung a mirror ball from a ceiling. What that does now that the ball is throwing
        // sixty beams of its own is put two or three hard white squares on it — a whole facet flaring at once
        // — and a square is the one shape nothing in this room is meant to have.
        //
        // Smooth normals and a plastic finish instead, dark enough that the lenses are the brightest thing on
        // it by a long way. See <see cref="Glitter.Lenses"/>: what makes this read as the source of the light
        // is the sixty coloured lenses in its skin, not a specular highlight.
        Mirror.Children.Add(new MeshNode(
            Primitives.Sphere(0.26f, 28, 16),
            new Material
            {
                BaseColor = new Vector4(0.07f, 0.075f, 0.09f, 1f),
                Metallic = 0.15f,
                Roughness = 0.55f,
                Name = "ball"
            })
        {
            Name = "ball",
            Position = new Vector3(0f, 2.35f, 0f)
        });

        // The show. It hangs off the room rather than off the ball — see Glitter for why the old ring of
        // patches parented to the ball could never leave the alcove — and the lenses it throws through hang
        // off the ball, so the colour a dot is and the colour the hole it came out of is are one material.
        Show = new Glitter(root);
        Show.Lenses(Mirror);

        root.Children.Add(Mirror);

        Beams =
        [
            Beam(1f, 0.24f, 0.86f),
            Beam(0.24f, 0.84f, 1f),
            Beam(1f, 0.68f, 0.20f),
            Beam(0.30f, 1f, 0.46f)
        ];

        // Born dark. The beams already were — see Beam, which builds them at no intensity — and the dots
        // were not, which is the whole of the bug described on Blackout.
        Blackout();

        return;

        static PointLight Beam(float r, float g, float b) => new()
        {
            Position = Ball,
            Color = new Vector3(r, g, b),
            Intensity = 0f,
            Range = 8f,
            Decay = 2f
        };
    }

    /// <summary>
    /// The furniture. A chair to sit in, a bag to fall into, a rug under both, and a low table with a
    /// console on it.
    ///
    /// All of it is boxes, cylinders and two spheres, like everything else in this building — which is
    /// worth saying once more here because this is the room where it stops being obvious. A leather
    /// armchair is fourteen primitives and a normal map, and the map is doing more of the work than the
    /// fourteen are.
    /// </summary>
    private static void Furnish(Node root)
    {
        var hide = Finish.Hide();
        var boards = Finish.Boards();
        var pile = Finish.Pile();
        var kit = Kit.Build();

        // The rug runs the length of the bench, under all four stations. Two rectangles, a darker one under
        // a lighter one, because a rug's border is a fact about its edges and an image is tiled — see
        // Finish.PileColour.
        var rugAt = new Vector3(0f, 0.006f, -1.35f);
        var band = pile.Clone();
        band.BaseColor = new Vector4(0.17f, 0.10f, 0.08f, 1f);

        root.Children.Add(new MeshNode(Fabric.Map(Primitives.Plane(8.2f, 1.9f), band, rugAt, Finish.Close), band)
        {
            Name = "rug.border",
            Position = rugAt
        });

        var fieldAt = rugAt + new Vector3(0f, 0.004f, 0f);

        root.Children.Add(new MeshNode(Fabric.Map(Primitives.Plane(7.8f, 1.5f), pile, fieldAt, Finish.Close), pile)
        {
            Name = "rug",
            Position = fieldAt
        });

        // Four stations: a seat, a crate, a console on the crate, and pads on leads back towards the seat.
        // Two armchairs and two bags rather than four of either, because four identical seats in a row is
        // a waiting area.
        //
        // And the two bags are two colours, for the same reason and one more. Nobody buys a matching pair of
        // these; they arrive one at a time. Both are also a good deal darker than the pale grey-blue they
        // were, which was not a colour choice so much as an unnoticed one — under four warm lamps a cloth
        // at that value came back brighter than the walls, the floor and the leather, so the two largest
        // soft objects in the room were also the two brightest things in it.
        var cloths = new[] { Finish.Cloth(0.16f, 0.19f, 0.11f), Finish.Cloth(0.14f, 0.18f, 0.27f) };

        for (var i = 0; i < Along.Length; i++)
        {
            var seat = i % 2 == 0
                ? Chair(hide, new Vector3(Along[i], 0f, SeatZ), Askew[i])
                : Bag(cloths[i / 2], new Vector3(Along[i], 0f, SeatZ), Askew[i]);

            root.Children.Add(seat);
            root.Children.Add(Crate(boards, kit, new Vector3(Along[i], 0f, CrateZ), -Askew[i] * 0.6f));
        }

        // The sofa, in front of the alcove, turned to face it. Built facing −Z like the chairs and then
        // turned right round, which is the only place in this room where that matters: everything else
        // faces the bench and this faces the opposite way, which is the whole point of it.
        var sofa = Sofa(hide);
        sofa.Position = SofaAt;
        sofa.RotationDegrees = new Vector3(0f, 180f, 0f);
        root.Children.Add(sofa);

        // And the clutter, which is what stops the four stations reading as a shop display. A pad left on a
        // seat, one on the floor where somebody dropped it, cartridges out of their machines on two of the
        // crates and one on the boards. None of it is randomised — the film is the same every run — but all
        // of it is placed as though it were.
        Loose(root, kit, new Vector3(Along[0] + 0.18f, 0.50f, SeatZ - 0.10f), 24f, cartridge: false);
        Loose(root, kit, new Vector3(Along[1] - 0.62f, 0.012f, SeatZ + 0.30f), -63f, cartridge: false);
        Loose(root, kit, new Vector3(Along[3] + 0.55f, 0.012f, SeatZ - 0.42f), 111f, cartridge: false);
        Loose(root, kit, new Vector3(0.72f, 0.44f, SofaAt.Z - 0.10f), -18f, cartridge: false);

        Loose(root, kit, new Vector3(Along[0] - 0.22f, 0.455f, CrateZ + 0.06f), 37f, cartridge: true);
        Loose(root, kit, new Vector3(Along[2] + 0.20f, 0.455f, CrateZ - 0.04f), -14f, cartridge: true);
        Loose(root, kit, new Vector3(Along[2] - 0.85f, 0.026f, SeatZ + 0.44f), 78f, cartridge: true);
        Loose(root, kit, new Vector3(Along[3] - 0.30f, 0.455f, CrateZ + 0.02f), 52f, cartridge: true);
    }

    /// <summary>
    /// One armchair, facing the bench, with a few degrees of <paramref name="askew"/> on it.
    ///
    /// Built facing −Z, which is towards the bench, so a seat that watches television needs no rotation at
    /// all and the one thing in the room that faces the other way says so by carrying a hundred and eighty.
    /// </summary>
    private static Node Chair(Material hide, Vector3 at, float askew)
    {
        var chair = new Node
        {
            Name = "armchair",
            Position = at,
            RotationDegrees = new Vector3(0f, askew, 0f)
        };

        foreach (var x in new[] { -0.42f, 0.42f })
        foreach (var z in new[] { -0.36f, 0.36f })
            chair.Children.Add(new MeshNode(Primitives.Cylinder(0.035f, 0.03f, 0.12f, 10), Fabric.DarkMetal)
            {
                Position = new Vector3(x, 0.06f, z),
                Name = "foot"
            });

        chair.Children.Add(Padded(new Vector3(0.98f, 0.16f, 0.86f), new Vector3(0f, 0.20f, 0f), "base"));
        chair.Children.Add(Padded(new Vector3(0.86f, 0.20f, 0.76f), new Vector3(0f, 0.38f, 0f), "cushion"));

        // The roll along the front of the seat, which is the one shape that separates an armchair from a
        // box with a back on it.
        chair.Children.Add(Roll(0.11f, 0.86f, new Vector3(0f, 0.38f, -0.36f), hide));

        // The back, leaning. Six degrees, which is barely a lean and is all a lean needs to be — at ten it
        // reads as a deckchair, and this is a chair somebody watches television from.
        var back = new Node { Position = new Vector3(0f, 0f, 0.34f), RotationDegrees = new Vector3(6f, 0f, 0f) };

        back.Children.Add(Padded(new Vector3(0.98f, 0.60f, 0.19f), new Vector3(0f, 0.70f, 0f), "back"));
        back.Children.Add(Padded(new Vector3(0.80f, 0.44f, 0.13f), new Vector3(0f, 0.66f, -0.14f), "back.cushion"));

        back.Children.Add(Roll(0.095f, 0.98f, new Vector3(0f, 1.00f, 0f), hide));
        chair.Children.Add(back);

        foreach (var side in new[] { -1f, 1f })
        {
            chair.Children.Add(Padded(new Vector3(0.16f, 0.30f, 0.80f), new Vector3(side * 0.41f, 0.50f, 0.02f), "arm"));

            chair.Children.Add(new MeshNode(
                Fabric.Map(Primitives.Cylinder(0.08f, 0.08f, 0.80f, 14), hide, Vector3.Zero, Finish.Close), hide)
            {
                Position = new Vector3(side * 0.41f, 0.65f, 0.02f),
                RotationDegrees = new Vector3(90f, 0f, 0f),
                Name = "arm.roll"
            });
        }

        return chair;

        MeshNode Padded(Vector3 size, Vector3 to, string name) =>
            Fabric.Slab(size, to, hide, name, Finish.Close);
    }

    /// <summary>
    /// The sofa. The armchair, stretched, with two cushions instead of one — which is what a sofa is.
    /// </summary>
    private static Node Sofa(Material hide)
    {
        const float length = 2.2f;

        var sofa = new Node { Name = "sofa" };

        foreach (var x in new[] { -1.0f, 1.0f })
        foreach (var z in new[] { -0.38f, 0.38f })
            sofa.Children.Add(new MeshNode(Primitives.Cylinder(0.038f, 0.032f, 0.12f, 10), Fabric.DarkMetal)
            {
                Position = new Vector3(x, 0.06f, z),
                Name = "foot"
            });

        sofa.Children.Add(Padded(new Vector3(length, 0.16f, 0.92f), new Vector3(0f, 0.20f, 0f), "base"));

        foreach (var side in new[] { -1f, 1f })
        {
            sofa.Children.Add(Padded(
                new Vector3(0.98f, 0.20f, 0.80f), new Vector3(side * 0.52f, 0.38f, 0f), "cushion"));

            sofa.Children.Add(Padded(
                new Vector3(0.16f, 0.30f, 0.86f), new Vector3(side * 1.03f, 0.50f, 0.02f), "arm"));

            sofa.Children.Add(new MeshNode(
                Fabric.Map(Primitives.Cylinder(0.08f, 0.08f, 0.86f, 14), hide, Vector3.Zero, Finish.Close), hide)
            {
                Position = new Vector3(side * 1.03f, 0.65f, 0.02f),
                RotationDegrees = new Vector3(90f, 0f, 0f),
                Name = "arm.roll"
            });
        }

        sofa.Children.Add(Roll(0.11f, length, new Vector3(0f, 0.38f, -0.39f), hide));

        var back = new Node { Position = new Vector3(0f, 0f, 0.37f), RotationDegrees = new Vector3(6f, 0f, 0f) };

        back.Children.Add(Padded(new Vector3(length, 0.60f, 0.19f), new Vector3(0f, 0.70f, 0f), "back"));

        foreach (var side in new[] { -1f, 1f })
            back.Children.Add(Padded(
                new Vector3(0.98f, 0.44f, 0.13f), new Vector3(side * 0.52f, 0.66f, -0.14f), "back.cushion"));

        back.Children.Add(Roll(0.095f, length, new Vector3(0f, 1.00f, 0f), hide));
        sofa.Children.Add(back);

        return sofa;

        MeshNode Padded(Vector3 size, Vector3 to, string name) =>
            Fabric.Slab(size, to, hide, name, Finish.Close);
    }

    /// <summary>
    /// A bean bag, built the way one is made: six panels sewn edge to edge onto a round base, filled until
    /// it slumps, and sat in.
    ///
    /// What this replaced was two squashed spheres and a disc under them, on the argument that a bean bag
    /// has no edge anywhere on it and nothing about it is straight. Both halves of that are true and the
    /// picture was still wrong, because a sphere has no <i>seams</i> either — and seams are most of what
    /// tells the eye that a smooth shape is cloth rather than plastic. Two pale spheres a metre and a
    /// quarter across came out as a pile of balloons.
    ///
    /// So: one lathe, with the three things a sphere cannot have. See <see cref="Surface"/> for each.
    ///
    /// It carries no texture coordinates of its own and is box-mapped at the weave's scale instead, for the
    /// same reason the spheres were. Coordinates that run once round a body of revolution turn a weave of
    /// twenty-six threads drawn into a square image into twenty-six threads round the whole bag —
    /// four-centimetre threads, which is not cloth, it is a golf ball. The projection has seams where its
    /// axis changes over and a weave this fine hides every one of them.
    /// </summary>
    private static Node Bag(Material cloth, Vector3 at, float askew)
    {
        var bag = new Node
        {
            Name = "beanbag",
            Position = at,
            RotationDegrees = new Vector3(0f, askew, 0f)
        };

        bag.Children.Add(new MeshNode(Fabric.Map(Slump(), cloth, Vector3.Zero, Finish.Snug), cloth)
        {
            Name = "bag"
        });

        // The piping round the hem, which is the one hard edge a bean bag has and is there for the same
        // reason the seams are: it is where the base panel is sewn on. It also closes the gap that a base
        // panel standing a couple of centimetres off the boards would otherwise leave under itself.
        bag.Children.Add(new MeshNode(
            Fabric.Map(Primitives.Torus(BagRadius * 0.60f, BagHem, 28, 6), cloth, Vector3.Zero, Finish.Snug),
            cloth)
        {
            Position = new Vector3(0f, BagHem, 0f),
            Name = "bag.piping"
        });

        return bag;
    }

    /// <summary>
    /// One bag's skin: a base panel, <see cref="Bands"/> rings up the side, and a crown.
    ///
    /// The top is one vertex with a fan under it rather than a ring of coincident points, which is the
    /// difference between a crown and a pucker — a lathe closed by collapsing its last ring has as many
    /// degenerate triangles as it has segments, and they contribute nothing to the smooth normals while
    /// costing exactly what a real triangle costs.
    /// </summary>
    private static Mesh Slump()
    {
        const int around = Gores * PerGore;

        var positions = new Vector3[2 + Bands * around];
        var indices = new List<uint>();

        // Where the base panel touches the boards, and the only point of a bean bag that does.
        positions[0] = Vector3.Zero;

        for (var band = 0; band < Bands; band++)
        for (var step = 0; step < around; step++)
            positions[1 + band * around + step] =
                Surface(band / (Bands - 0.5f), step * MathF.Tau / around);

        positions[^1] = Surface(1f, 0f);

        // The base panel: a fan from that point out to the hem, facing down.
        for (var step = 0; step < around; step++)
            Face(0, 1 + step, 1 + (step + 1) % around);

        // The side, band by band. Two triangles a panel segment, wound so the normals point out of the bag
        // — which is worth checking rather than trying, because a lathe with its winding reversed renders
        // as a perfectly convincing bag lit from inside.
        for (var band = 0; band + 1 < Bands; band++)
        for (var step = 0; step < around; step++)
        {
            var next = (step + 1) % around;
            var lower = 1 + band * around;
            var upper = lower + around;

            Face(lower + step, upper + step, upper + next);
            Face(lower + step, upper + next, lower + next);
        }

        // And the crown.
        for (var step = 0; step < around; step++)
            Face(1 + (Bands - 1) * around + step, positions.Length - 1,
                1 + (Bands - 1) * around + (step + 1) % around);

        return new Mesh
        {
            Positions = positions,
            Indices = [.. indices],
            Name = "beanbag"
        }.WithSmoothNormals();

        void Face(int a, int b, int c)
        {
            indices.Add((uint)a);
            indices.Add((uint)b);
            indices.Add((uint)c);
        }
    }

    /// <summary>
    /// One point on a bag's skin, <paramref name="up"/> of the way from the hem to the crown and
    /// <paramref name="around"/> radians round it.
    ///
    /// <b>The taper.</b> Wide where it stands and narrowing over, which is what a bag of beans does under
    /// its own weight and is the reason one has a front and a back at all. Two factors rather than one
    /// curve, because the two ends are two different facts: the foot is where the base panel pulls the
    /// cloth in, and the crown is where the filling runs out.
    ///
    /// <b>The seams, and the folds under them.</b> The surface is pinched wherever two panels meet, and
    /// that is not decoration. A seam is a double thickness of cloth pulled tight and the filling cannot
    /// reach it, so a filled panel is a pillow between two furrows; and where the panels meet the base
    /// there is more cloth than the hem needs, so it gathers into pleats. Both are the radius modulated by
    /// the angle, which costs nothing whatever, and between them they are the difference between cloth and
    /// a smooth rubber pear.
    ///
    /// <b>The hollow.</b> Somebody sat here. One Gaussian pressed into the top, and the beans it displaced
    /// standing up in a ring round it — which is the same Gaussian with a term in its own second moment
    /// taken off, so the hollow and its rim cannot come apart or be tuned against each other. Only the top
    /// is pressed: at the belly the surface is vertical and nothing ever sat on it.
    /// </summary>
    private static Vector3 Surface(float up, float around)
    {
        var foot = MathF.Max(0f, 1f - up / 0.24f);
        var belly = 1f - 0.40f * foot * foot;
        var crown = MathF.Pow(1f - up * up * up, 0.55f);
        // A furrow rather than a wave, which is what the fourth power is for: raising the cosine narrows
        // it without moving where it is, so the pinch is confined to a hand's width either side of the
        // stitching and the rest of the panel is left alone to be a pillow. The cosine on its own is a
        // fluted column, which is a shape with six of something on it and no seams anywhere.
        var seam = MathF.Pow((1f + MathF.Cos(Gores * around)) * 0.5f, 4f);

        // The gather. Where a panel meets the base there is more cloth than the hem's perimeter needs, so
        // it pleats — which is why every photograph of one of these has a ring of shallow folds round the
        // bottom and a smooth belly above them. It comes to nothing at the hem itself, where the cloth is
        // sewn flat and the piping has to find a circle, and again where the filling pulls the panel tight.
        var gather = Smooth(up, 0f, 0.10f) * MathF.Max(0f, 1f - up / 0.42f);
        var pleat = MathF.Cos(Gores * 2f * around + 1.7f);

        var radius = BagRadius * belly * crown * (1f - 0.075f * seam + 0.045f * gather * pleat);

        var x = MathF.Cos(around) * radius;
        var z = MathF.Sin(around) * radius + BagLean * MathF.Pow(up, 1.7f);
        var y = BagHem + (BagHeight - BagHem) * up;

        var across = x / 0.30f;
        var along = (z + 0.10f) / 0.24f;
        var reach = across * across + along * along;

        return new Vector3(
            x, y - Smooth(up, 0.40f, 0.95f) * (0.20f - 0.085f * reach) * MathF.Exp(-0.5f * reach), z);
    }

    /// <summary>Nought below <paramref name="from"/>, one above <paramref name="to"/>, and the cubic
    /// between — the ramp with no corner at either end.</summary>
    private static float Smooth(float value, float from, float to)
    {
        var into = Math.Clamp((value - from) / (to - from), 0f, 1f);

        return into * into * (3f - 2f * into);
    }

    /// <summary>A crate in front of a set, with a console standing on it.</summary>
    private static Node Crate(Material boards, Kit kit, Vector3 at, float askew)
    {
        var crate = new Node
        {
            Name = "crate",
            Position = at,
            RotationDegrees = new Vector3(0f, askew, 0f)
        };

        crate.Children.Add(Fabric.Slab(new Vector3(0.70f, 0.42f, 0.42f), new Vector3(0f, 0.21f, 0f), boards, "crate"));
        crate.Children.Add(Fabric.Slab(new Vector3(0.74f, 0.03f, 0.46f), new Vector3(0f, 0.435f, 0f), boards, "crate.top"));
        crate.Children.Add(Machine(new Vector3(0f, 0.45f, 0.02f), kit));

        return crate;
    }

    /// <summary>A pad or a cartridge, left where somebody left it.</summary>
    private static void Loose(Node root, Kit kit, Vector3 at, float yaw, bool cartridge)
    {
        var node = cartridge ? Cartridge(kit) : Pad(kit);

        node.Position = at;
        node.RotationDegrees = new Vector3(0f, yaw, 0f);

        root.Children.Add(node);
    }

    /// <summary>A padded roll along X: a cylinder laid on its side. Arms, seat fronts and chair backs.</summary>
    private static MeshNode Roll(float radius, float length, Vector3 at, Material material) =>
        new(Fabric.Map(Primitives.Cylinder(radius, radius, length, 14), material, Vector3.Zero, Finish.Close),
            material)
        {
            Position = at,
            RotationDegrees = new Vector3(0f, 0f, 90f),
            Name = "roll"
        };

    /// <summary>
    /// The console, and two pads on leads.
    ///
    /// It is the shape everybody who had one remembers — a flat grey slab with a hinged flap over a slot in
    /// the top, a cartridge standing up out of it, a red light and two square buttons — and it is not any
    /// of them. There is no lettering on it, no logo moulded into the lid, no name on the cartridge label
    /// and no name on this method. That is the same rule the four games are under and it applies to
    /// hardware for exactly the same reason: the silhouette of a home console is a genre and its badge is
    /// somebody's property.
    ///
    /// The leads are the part that makes it read at all. A grey box on a table is a grey box; a grey box
    /// with two pads trailing wires off the front edge is a thing somebody plays.
    /// </summary>
    private static Node Machine(Vector3 at, Kit kit)
    {
        var box = new Node { Name = "console", Position = at, RotationDegrees = new Vector3(0f, -14f, 0f) };

        box.Children.Add(Fabric.Slab(new Vector3(0.28f, 0.042f, 0.20f), new Vector3(0f, 0.021f, 0f), kit.Shell, "body"));

        // The raised back half, with the slot in it.
        box.Children.Add(Fabric.Slab(new Vector3(0.28f, 0.030f, 0.085f), new Vector3(0f, 0.057f, 0.055f), kit.Shell, "hood"));
        box.Children.Add(Fabric.Slab(new Vector3(0.16f, 0.010f, 0.050f), new Vector3(0f, 0.072f, 0.050f), kit.Dark, "slot"));

        // The cartridge, in and standing proud, because a console with nothing in it is a doorstop.
        box.Children.Add(Fabric.Slab(new Vector3(0.145f, 0.075f, 0.042f), new Vector3(0f, 0.104f, 0.050f), kit.Dark, "cartridge"));
        box.Children.Add(Fabric.Slab(
            new Vector3(0.105f, 0.038f, 0.046f), new Vector3(0f, 0.112f, 0.050f), kit.Label, "label"));

        // Two square buttons and a light, in a row along the front.
        box.Children.Add(Fabric.Slab(new Vector3(0.030f, 0.012f, 0.020f), new Vector3(-0.055f, 0.046f, -0.062f), kit.Dark, "button"));
        box.Children.Add(Fabric.Slab(new Vector3(0.030f, 0.012f, 0.020f), new Vector3(-0.010f, 0.046f, -0.062f), kit.Dark, "button"));
        box.Children.Add(Fabric.Slab(
            new Vector3(0.012f, 0.008f, 0.012f), new Vector3(0.060f, 0.046f, -0.062f), kit.Power, "power"));

        // Vents.
        for (var i = 0; i < 4; i++)
            box.Children.Add(Fabric.Slab(
                new Vector3(0.075f, 0.004f, 0.008f), new Vector3(0.088f, 0.043f, -0.02f + i * 0.02f), kit.Dark, "vent"));

        // The two pads, off the front of the crate, each with a lead arcing back to the machine.
        for (var i = 0; i < 2; i++)
        {
            var side = i == 0 ? -1f : 1f;
            var pad = Pad(kit);

            pad.Position = new Vector3(side * 0.21f + 0.05f, 0.011f, -0.16f - i * 0.03f);
            pad.RotationDegrees = new Vector3(0f, side * 22f, 0f);
            box.Children.Add(pad);

            // Three straight pieces read as one curve, which at this size is as much curve as a lead needs.
            var from = new Vector3(side * 0.21f + 0.05f, 0.014f, -0.13f - i * 0.03f);
            var to = new Vector3(side * 0.05f, 0.014f, -0.10f);

            var bend = new Vector3((from.X + to.X) / 2f + side * 0.05f, 0.014f, (from.Z + to.Z) / 2f - 0.04f);

            box.Children.Add(Wire(from, bend, kit.Lead));
            box.Children.Add(Wire(bend, to, kit.Lead));
        }

        return box;
    }

    /// <summary>
    /// One pad: a slab, a cross, and two round buttons. The layout, which is a layout, and not a mark.
    /// </summary>
    private static Node Pad(Kit kit)
    {
        var pad = new Node { Name = "pad" };

        pad.Children.Add(Fabric.Slab(new Vector3(0.125f, 0.020f, 0.058f), Vector3.Zero, kit.Shell, "pad"));
        pad.Children.Add(Fabric.Slab(new Vector3(0.030f, 0.008f, 0.010f), new Vector3(-0.038f, 0.013f, 0f), kit.Dark, "cross"));
        pad.Children.Add(Fabric.Slab(new Vector3(0.010f, 0.008f, 0.030f), new Vector3(-0.038f, 0.013f, 0f), kit.Dark, "cross"));

        foreach (var x in new[] { 0.030f, 0.048f })
            pad.Children.Add(new MeshNode(Primitives.Cylinder(0.008f, 0.008f, 0.008f, 10), kit.Button)
            {
                Position = new Vector3(x, 0.013f, 0f),
                Name = "button"
            });

        return pad;
    }

    /// <summary>One cartridge, out of its machine. A dark slab with a paler label on the face of it and no
    /// writing on the label, for the same reason nothing else in this room has any.</summary>
    private static Node Cartridge(Kit kit)
    {
        var cartridge = new Node { Name = "cartridge" };

        cartridge.Children.Add(Fabric.Slab(new Vector3(0.145f, 0.028f, 0.115f), Vector3.Zero, kit.Dark, "case"));
        cartridge.Children.Add(Fabric.Slab(
            new Vector3(0.105f, 0.006f, 0.070f), new Vector3(0f, 0.016f, -0.008f), kit.Label, "label"));

        return cartridge;
    }

    /// <summary>
    /// The five materials the hardware in this room is made of, built once and shared by every instance of
    /// it.
    ///
    /// There are four consoles, eight pads on leads, four more pads lying about and four cartridges, and
    /// every one of them used to make its own materials. Materials are cheap and the count is not the point
    /// — sharing them is what lets a renderer batch the draws, and thirty-odd objects that agree about
    /// their colour are worth rather more than thirty that merely match.
    /// </summary>
    private readonly record struct Kit(
        Material Shell, Material Dark, Material Lead, Material Label, Material Button, Material Power)
    {
        public static Kit Build() => new(
            new Material { BaseColor = new Vector4(0.60f, 0.59f, 0.55f, 1f), Roughness = 0.52f, Name = "console" },
            new Material { BaseColor = new Vector4(0.10f, 0.10f, 0.11f, 1f), Roughness = 0.44f, Name = "console.dark" },
            new Material { BaseColor = new Vector4(0.14f, 0.14f, 0.15f, 1f), Roughness = 0.62f, Name = "lead" },
            new Material { BaseColor = new Vector4(0.52f, 0.46f, 0.36f, 1f), Roughness = 0.70f, Name = "label" },
            new Material { BaseColor = new Vector4(0.55f, 0.09f, 0.08f, 1f), Roughness = 0.40f, Name = "button" },
            Fabric.Emissive(1f, 0.16f, 0.10f));
    }

    /// <summary>
    /// One straight run of cable between two points at the same height.
    ///
    /// A yaw of φ sends a slab's own X to (cos φ, 0, −sin φ), so the angle a caller needs is
    /// <c>atan2(−Δz, Δx)</c> and not the <c>atan2(Δz, Δx)</c> that anybody writes first. Getting it
    /// backwards leaves the lead pointing at the mirror image of where it was going, which on a table
    /// covered in small objects looks like a lead that goes somewhere else.
    /// </summary>
    private static MeshNode Wire(Vector3 from, Vector3 to, Material material)
    {
        var d = to - from;
        var length = d.Length();

        var wire = Fabric.Slab(
            new Vector3(length, 0.010f, 0.010f), (from + to) / 2f, material, "lead");

        wire.RotationDegrees = new Vector3(0f, MathF.Atan2(-d.Z, d.X) * 180f / MathF.PI, 0f);

        return wire;
    }

    /// <summary>
    /// One set: a case, a lip, and the glass, with the glass proud of the case by a centimetre.
    ///
    /// The angle is the only subtle part. Four sets square-on to the room are a wall of screens; four turned
    /// a few degrees towards where somebody would stand to watch them are furniture that was put there by
    /// somebody. It is a rotation of about eight degrees and it is most of the difference.
    /// </summary>
    private static Node Television(float x, Material glass, int index)
    {
        var set = new Node
        {
            Name = $"television.{index}",
            Position = new Vector3(x, BenchTop, BenchZ),
            RotationDegrees = new Vector3(0f, -x * 2.6f, 0f)
        };

        var case_ = Fabric.Slab(
            new Vector3(1.18f, 0.94f, 0.72f),
            new Vector3(0f, 0.47f, 0f),
            new Material
            {
                BaseColor = new Vector4(0.13f, 0.13f, 0.15f, 1f),
                Roughness = 0.62f
            },
            "case");

        set.Children.Add(case_);

        // The lip around the glass, a shade lighter than the case so the front face reads as a bezel rather
        // than as a hole cut in a box.
        set.Children.Add(Fabric.Slab(
            new Vector3(1.02f, 0.78f, 0.04f),
            new Vector3(0f, 0.50f, 0.365f),
            new Material
            {
                BaseColor = new Vector4(0.20f, 0.20f, 0.23f, 1f),
                Roughness = 0.5f
            },
            "bezel"));

        set.Children.Add(new MeshNode(Primitives.Plane(0.88f, 0.66f), glass)
        {
            Name = "glass",
            Position = new Vector3(0f, 0.50f, 0.392f),
            RotationDegrees = new Vector3(90f, 0f, 0f)
        });

        // A knob and a vent, because a television with nothing on it but a screen is a monitor.
        //
        // Both sit on the case, in the margin the bezel leaves: eight centimetres of case down each side of
        // it and five along the bottom. They used to sit <i>on</i> the bezel, and both were wrong in the way
        // the same mistake is always wrong twice. The knob's front face landed at z 0.385 and so does the
        // bezel's — the same plane, to the last bit — so which of the two a pixel belonged to was decided by
        // rounding, and the answer changed as the camera moved: a brass rectangle flickering on and off the
        // frame of every set in the room. The vents lost the same argument the other way and were simply
        // inside the bezel, invisible except for two centimetres poking out of its left edge.
        //
        // So the rule the rest of this building already follows — see Rotunda: solids interpenetrate, they
        // are never laid flush. Each of these is sunk half a centimetre into the case and stands proud of
        // it, and neither shares a plane, an edge or a footprint with the bezel.
        set.Children.Add(Fabric.Slab(
            new Vector3(0.07f, 0.07f, 0.03f),
            new Vector3(0.55f, 0.22f, 0.365f),
            Fabric.Brass,
            "knob"));

        for (var slot = 0; slot < 4; slot++)
            set.Children.Add(Fabric.Slab(
                new Vector3(0.22f, 0.015f, 0.02f),
                new Vector3(-0.42f, 0.03f + slot * 0.023f, 0.365f),
                Fabric.DarkMetal));

        return set;
    }
}
