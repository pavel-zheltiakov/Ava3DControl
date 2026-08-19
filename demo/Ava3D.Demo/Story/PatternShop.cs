using System.Numerics;
using Ava3D.Demo.Scenes;

namespace Ava3D.Demo.Story;

/// <summary>
/// The pattern shop: twelve metres by eight, four exhibits down it, and one claim made four ways —
/// <b>the demo ships no image files.</b>
///
/// Everything on a surface anywhere in this building came out of arithmetic. The plaster, the tile, the
/// panelling, the rivets, the four games on the lounge's televisions, the forty-nine samples on the
/// gallery's chart: all of them are pixels computed at startup and uploaded once. That has been true since
/// the first room and nothing has ever said so, because a room whose walls are procedural looks exactly
/// like a room whose walls are photographs. This is the room that says it.
///
/// The four are in the order they build on each other, east to west, which is also the order he walks
/// them:
///
/// <list type="number">
/// <item>colour with no image at all, carried by the mesh's own corners;</item>
/// <item>one image read many ways — tiled, slid, and a cell at a time;</item>
/// <item>the two images the library itself builds, a falloff and a field;</item>
/// <item>and a picture of a scene, rendered by the same renderer with no window at all.</item>
/// </list>
///
/// It is the widest room in the exhibition half and the tallest, and both are the four exhibits. Three
/// metres six after the studio's two metres nine is the room opening up as he comes through the door,
/// which is the passage-and-rotunda trick a second time and costs a number.
///
/// <b>The last one is the odd one and it is the reason the room is here.</b> Everything else in the
/// building is a thing being drawn; the print is a thing that <i>was</i> drawn, once, at build time, by
/// the CPU renderer with no graphics context and nowhere to put a frame — and then hung on a wall as an
/// ordinary base-colour map. It is the only exhibit in the exhibition that is a photograph of the
/// exhibition's own renderer.
/// </summary>
internal sealed class PatternShop
{
    /// <summary>Clear width and clear length, wall face to wall face.</summary>
    public const float Width = 12f;

    public const float Length = 8f;

    /// <summary>Three metres six — the tallest room in the first half, and the studio's two-nine is what
    /// makes it read that way.</summary>
    private const float Height = 3.6f;

    private const float HalfWidth = Width / 2f;
    private const float HalfLength = Length / 2f;

    /// <summary>Where the studio's doorway comes through the south wall, and where the way out is cut in
    /// the north one. Both are derived from the rooms they meet — see <see cref="Deck.Patterns"/>.
    ///
    /// <b>The way out moved from the west wall to the north one</b>, and the planetarium is why. That room
    /// is round and its openings sit on radials, so the only place the two can meet is due south of its
    /// centre — which is a point on this room's north wall and nowhere near its west one. The west wall is
    /// solid now, and the corridor that used to leave through it leaves through the planetarium instead.
    ///
    /// The number lands four metres and a half from the south doorway, and no two doorways in this building
    /// are allowed on one axis. It also lands in the only span of north wall with nothing against it: the
    /// colour stand runs from x 1.35 to 5.25 and the texture stand from −3.7 to −1.1, and the opening sits
    /// in the two and a half metres between them with two hundred millimetres to spare at the near end.
    /// </summary>
    private static readonly float DoorX = Studio.Exit.X - Deck.Patterns.X;

    private static readonly float WayX = Deck.Planetarium.X - Deck.Patterns.X;

    /// <summary>Where the four exhibits stand along the room, east to west, and which wall each is on.
    /// They alternate, which is what enforces rule 2 in a room with no internal walls: consecutive stops
    /// are a hundred and eighty degrees apart, so no frame in the chapter has two of them in it.</summary>
    private static readonly float[] Along = [3.3f, 0.4f, -2.4f, -4.4f];

    private const float NorthZ = 3.4f;
    private const float SouthZ = -3.4f;

    private const float StandHeight = 0.85f;

    /// <summary>How far above its stand a mounted exhibit is seated. See <c>Mount</c>.</summary>
    private const float Clearance = 0.006f;

    public PatternShop(Hall hall)
    {
        var root = hall.Add(Deck.PatternRoom, Deck.Patterns);

        // The gallery's rung, which is where the ladder stops climbing — see <see cref="Grade"/>. The
        // lounge is still the only room in the film finished in something a house is not made of, and this
        // one has no business taking that off it: what is on these walls is not the exhibit here, and a
        // room about where pictures come from that competes with its own pictures is the mistake the
        // gallery had to be rescued from in the other direction.
        var plaster = Finish.Plaster(Grade.Dressed);
        var boards = Finish.Boards();
        var stone = Finish.Stone(Grade.Dressed);
        var brushed = Finish.Brushed();

        var t = Deck.WallThickness;

        // Boarded rather than tiled, over the whole floor. Every other exhibition room in the building is
        // laid in tile with a boarded runner down the middle where the visitors walk; this one is boards
        // wall to wall, which is a workshop and not a gallery, and it is the cheapest thing in the room
        // that says the difference.
        root.Children.Add(Fabric.Sheet(Width + 0.7f, Length + 0.7f, 0f, boards, "floor"));
        root.Children.Add(Fabric.Lid(Width + 0.7f, Length + 0.7f, Height, plaster, "ceiling"));

        // The south wall, carrying the studio's doorway. Written unrotated, so DoorX means what it says.
        var south = Fabric.PiercedWall(
            Width + 0.7f, Height, t, DoorX, Deck.DoorWidth, Deck.DoorHeight, plaster);
        south.Position = new Vector3(0f, 0f, -(HalfLength + t / 2f));
        root.Children.Add(south);

        // The north wall, carrying the way out into the planetarium. Written unrotated like the south one,
        // so WayX means what it says.
        //
        // <b>It is a hundred millimetres inside the planetarium's own wall.</b> That room's ring is a
        // twelve-sided polygon and its south face lands at z 35.15, which is inside this slab rather than
        // against it — the same arrangement the corridor and the engine room have at the other end of the
        // building, and for the same reason: two walls that meet face to face are two coplanar surfaces
        // with nothing to choose between them, and what a depth buffer does with that is give a different
        // answer per pixel per frame.
        var north = Fabric.PiercedWall(
            Width + 0.7f, Height, t, WayX, Deck.DoorWidth, Deck.DoorHeight, plaster);
        north.Position = new Vector3(0f, 0f, HalfLength + t / 2f);
        root.Children.Add(north);

        // And the west wall, which is solid. It carried the way out until the planetarium arrived; there is
        // nothing on the other side of it now but the twelve hundred millimetres of dead deck the link runs
        // past, so a doorway here would be a doorway onto plaster.
        var west = Fabric.Wall(Length + 0.7f, Height, t, plaster);
        west.Position = new Vector3(-(HalfWidth + t / 2f), 0f, 0f);
        west.RotationDegrees = new Vector3(0f, 90f, 0f);
        root.Children.Add(west);

        var east = Fabric.Wall(Length + 0.7f, Height, t, plaster);
        east.Position = new Vector3(HalfWidth + t / 2f, 0f, 0f);
        east.RotationDegrees = new Vector3(0f, 90f, 0f);
        root.Children.Add(east);

        // 1. Colour in the corners. One mesh, one material, three shades — banded, graded, and faded to
        //    nothing at the top — with every one of those carried by the vertices rather than by anything
        //    the material knows. It is first because it is the only exhibit in the room with no image in
        //    it at all.
        Stand(Along[0], NorthZ, 3.9f);
        root.Children.Add(Mount(new VertexColorScene().BuildSubject()!, "exhibit.colours", 0.5f, Along[0], NorthZ));

        // 2. One sheet, many windows. The same image tiled three by two, the same image sliding under a
        //    second box, and a sixteen-cell sheet played a cell at a time as a flipbook.
        Windows = new UvTransformScene();

        Stand(Along[1], SouthZ, 3.8f);
        root.Children.Add(Mount(Windows.BuildSubject()!, "exhibit.windows", 0.55f, Along[1], SouthZ));

        // 3. Made, not loaded. A falloff and a tileable field, which are the two images the library builds
        //    so that a scene does not have to — here as a table-top landscape, with the field on the
        //    ground as colour and on the sphere as height, and three glows over it at three falloffs.
        //
        //    It is the only exhibit in the building that is a whole scene shrunk rather than a subject
        //    mounted, and it is because that scene is a floor with things standing on it: taking the floor
        //    out would leave three sprites and a sphere hanging in a room, and the floor is half of what
        //    is being shown.
        Stand(Along[2], NorthZ - 0.55f, 2.6f, 2.4f, 0.75f);

        var kit = Mount(
            new TextureKitScene().BuildSubject()!, "exhibit.kit", 0.24f, Along[2], NorthZ - 0.55f, 0.75f);

        //    The three glows come down to a third, and this is the one place in the building where a room
        //    changes a mounted scene's own numbers rather than only its position and size. They were staged
        //    against black at arm's length; here they are a quarter that size, on a lit table, two metres
        //    from somebody's face — and three additive sprites tuned for a void saturate into one white
        //    blob in a room. What is being exhibited is the falloff and the relative size, and both of
        //    those are untouched.
        foreach (var sprite in kit.Children.OfType<SpriteNode>())
            sprite.Color *= 0.34f;

        root.Children.Add(kit);

        // 4. The print. Two panels on the wall, each one a scene the CPU renderer drew at 512 by 288 with
        //    no window, no graphics context and nowhere to present a frame, handed back as a Texture and
        //    put on a material like any other image.
        //
        //    Hung rather than stood, and that is the argument in the staging: everything else in this
        //    building is a thing being drawn thirty times a second, and this is a thing that was drawn
        //    once. A picture hangs on a wall.
        var print = new RenderToTextureScene().BuildSubject()!;
        print.Name = "exhibit.print";
        print.Scale = new Vector3(0.62f);
        print.Position = new Vector3(Along[3], 1.5f, -(HalfLength - 0.1f));
        root.Children.Add(print);

        // <b>And a label on each of them, which is the thing this room did not have.</b> Four exhibits, no
        // names, and four separate notes from somebody walking round it saying they could not tell what
        // any of them was showing. That is not a failure of the exhibits — it is a museum with nothing
        // written on the walls, and a gallery does not fix it by making the objects more obvious. See
        // Fabric.Label, which is the caption band's own alphabet cut into a plate.
        //
        // The three on plinths go on the face turned towards the room, which is the far side for the two
        // against the north wall; the print's goes on the wall under it, because a picture's label is not
        // on a plinth.
        Sign("VERTEX COLOUR", Along[0], NorthZ - 0.36f, StandHeight - 0.16f, 180f);
        Sign("UV TRANSFORM", Along[1], SouthZ + 0.36f, StandHeight - 0.16f, 0f);
        Sign("TEXTURE KIT", Along[2], NorthZ - 0.55f - 1.21f, 0.75f - 0.16f, 180f);
        Sign("RENDER TO TEXTURE", Along[3], -(HalfLength - 0.11f), 0.66f, 0f);

        // One lamp over each exhibit and one at the way out, which is five for four slots. The chapter
        // spends four at a time and swaps the one he has finished with for the one he is walking towards —
        // the same hand-over every threshold in this film does, done inside a room for the first time
        // because this room is wide enough to need it.
        // Inside the door the studio arrives by. It is the one fitting in here with no exhibit under it,
        // and it exists because of the frame on the other side of that doorway: chapter 4 finishes in a
        // dark room, and a dark room with a black rectangle in the wall is a dark room with a hole in it.
        // Nothing else in this room reaches that corner — the nearest exhibit lamp is six and a half metres
        // off, which is exactly its range.
        Entry = Hang(new Vector3(DoorX - 0.4f, Height - 0.12f, -HalfLength + 1.2f), 2.1f, 5.5f);

        OverColours = Hang(new Vector3(Along[0], Height - 0.12f, NorthZ - 0.9f), 2.7f, 6.5f);
        OverWindows = Hang(new Vector3(Along[1], Height - 0.12f, SouthZ + 0.9f), 2.7f, 6.5f);
        OverKit = Hang(new Vector3(Along[2], Height - 0.12f, NorthZ - 1.6f), 2.6f, 6.5f);
        OverPrint = Hang(new Vector3(Along[3], Height - 0.12f, SouthZ + 0.9f), 2.6f, 6.5f);
        Way = Hang(new Vector3(WayX, Height - 0.12f, HalfLength - 1.2f), 2f, 6f);

        foreach (var lamp in All)
            lamp.Dim(0f);

        return;

        Lamp Hang(Vector3 at, float brightness, float range)
        {
            var lamp = Fabric.Ceiling(Deck.Patterns, at, brightness, range);
            root.Children.Add(lamp.Fixture);

            // A brushed collar round each fitting, sunk into the ceiling. It is the whole of this room's
            // ceiling detail and it is doing the gallery's coffers' job at a fifth of the geometry: a flat
            // slab overhead with five lamps hanging out of nothing is the one surface in a room nobody
            // looks at until it is wrong.
            root.Children.Add(Fabric.Slab(
                new Vector3(0.44f, 0.06f, 0.44f),
                new Vector3(at.X, Height - 0.02f, at.Z),
                brushed,
                "collar",
                Finish.Close));

            return lamp;
        }

        void Sign(string text, float x, float z, float y, float yaw) =>
            root.Children.Add(Fabric.Label(text, new Vector3(x, y, z), 0.030f, yaw));

        void Stand(float x, float z, float length, float depth = 0.7f, float height = StandHeight) =>
            root.Children.Add(Fabric.Slab(
                new Vector3(length, height, depth),
                new Vector3(x, height / 2f, z),
                stone,
                "stand"));

        // Scaled to its stand and seated on it by its own lowest point — read after the scale and before
        // the position, because Bounds is the node's extent through its own transform and would otherwise
        // be measuring the answer.
        //
        // <b>Seated a hair proud of it, not on it.</b> Six millimetres, and they are not a nicety: the
        // texture kit is a whole scene rather than a subject, its lowest thing is a nine-metre plane, and
        // seating that plane by its own lowest point puts it in exactly the plane of the stand's top face.
        // Two coplanar surfaces is a depth buffer being asked a question with no answer, and what it does
        // about it is give a different one per pixel per frame — which was reported as the noise texture
        // flickering and is the stand and the exhibit tearing through each other in stripes.
        //
        // Six millimetres of real gap is below anything an eye reads as a gap at two metres, and it is
        // four orders of magnitude above the depth precision at that range. It is applied to all four
        // exhibits rather than to the one that needed it, because the fault is a property of mounting a
        // flat-bottomed thing on a flat-topped thing and the next exhibit to have a flat bottom would find
        // it again.
        static Node Mount(Node subject, string name, float scale, float x, float z, float top = StandHeight)
        {
            subject.Name = name;
            subject.Scale = new Vector3(scale);
            subject.Position = new Vector3(x, top + Clearance - subject.Bounds.Min.Y, z);

            return subject;
        }
    }

    /// <summary>The UV exhibit, kept so the chapter can run the scene's own animation rather than a copy of
    /// it. A chapter reimplementing an exhibit is an exhibit that can drift from itself.</summary>
    public UvTransformScene Windows { get; }

    /// <summary>The one inside the door he comes in by, which chapter 4 lights before he is through it.</summary>
    public Lamp Entry { get; }

    public Lamp OverColours { get; }

    public Lamp OverWindows { get; }

    public Lamp OverKit { get; }

    public Lamp OverPrint { get; }

    /// <summary>The one at the way out, which is the fifth of four.</summary>
    public Lamp Way { get; }

    /// <summary>All six, for switching the room off in one line.</summary>
    public Lamp[] All => [Entry, OverColours, OverWindows, OverKit, OverPrint, Way];

    /// <summary>The middle of one of the four exhibits, in world coordinates, east to west.</summary>
    public static Vector3 Exhibit(int index) => Deck.Patterns + new Vector3(
        Along[Math.Clamp(index, 0, Along.Length - 1)],
        index == 3 ? 1.5f : StandHeight + 0.5f,
        index switch { 0 => NorthZ, 1 => SouthZ, 2 => NorthZ - 0.55f, _ => -HalfLength });

    /// <summary>The doorway in, in world coordinates.</summary>
    public static Vector3 Entrance => Deck.Patterns + new Vector3(DoorX, Deck.Eye, -HalfLength);

    /// <summary>The doorway out, in the north wall.</summary>
    public static Vector3 Exit => Deck.Patterns + new Vector3(WayX, Deck.Eye, HalfLength);

    /// <summary>A point on the shop's floor at eye height: how far along the room from its middle, and how
    /// far north of the centre line.</summary>
    public static Vector3 At(float along, float across = 0f) =>
        Deck.Patterns + new Vector3(along, Deck.Eye, across);
}
