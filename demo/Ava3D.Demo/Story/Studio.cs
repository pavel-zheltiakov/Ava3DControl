using System.Numerics;
using Ava3D.Demo.Scenes;

namespace Ava3D.Demo.Story;

/// <summary>
/// The studio: a dark room, seven metres by eight, with a row of cel-shaded solids along one end wall and
/// a row of matcapped ones along the other, facing each other across it.
///
/// <b>It is the first room in the building that is about shading rather than about surface.</b> The
/// gallery answers what a material is — colour, metal, roughness, and five images that stand in for the
/// numbers. Both rows in here are answers to a different question: what if the light is not where the
/// lamps are. A cel-shaded surface is still lit by the room and simply steps as it goes round; a matcap
/// surface is lit by a photograph and is not in the room's lighting at all.
///
/// <b>The walls are painted out, and that is the exhibit's doing.</b> Every other room in the film is
/// finished in something you are meant to look at — the gallery is made of what it is exhibiting on
/// purpose. This one is not, because both of its exhibits are read as silhouettes with light stepping
/// across them, and a wall with trowelled plaster relief behind a two-band sphere is a wall competing with
/// the only thing the room is for. Dark grey, one rung down in brightness and no rungs down in maps: the
/// relief is still there and there is simply nothing lighting it hard enough to see.
///
/// <b>The beat is the lamps going out.</b> Two thirds of the way through the chapter the four lamps come
/// down to almost nothing, and what happens is a comparison nothing else in the film can make: the band
/// row goes with them, because a toon surface is a lit surface; the metal sphere on the end of the matcap
/// row goes with them too; and the three matcaps do not change by one pixel. That is the antechamber's
/// one-bulb argument and the lounge's four-television argument taken one step further — here the exhibit
/// is not unlit, it is lit by an image.
///
/// The one thing a still frame cannot show is what the lamps do while they <i>are</i> on, so one of them
/// moves: the band row's lamp runs along a rail over it for the whole chapter, and the bands sweep round
/// the spheres as it goes. <see cref="ToonScene"/> says in its own file that a still cel-shaded sphere
/// says nothing about where the bands are, and it circles a light to prove it. This room does the same
/// thing with a fitting somebody could have installed.
/// </summary>
internal sealed class Studio
{
    /// <summary>Clear width and clear length, wall face to wall face.</summary>
    public const float Width = 7.2f;

    public const float Length = 8.4f;

    /// <summary>
    /// Two metres nine, which is the lowest ceiling in the exhibition half.
    ///
    /// It is the gallery's three-two coming down thirty centimetres at the door, and it is doing what
    /// every ceiling height in this building does: making the room after it read as bigger. The pattern
    /// shop is three metres six and is the tallest room in the first half; nobody notices either number and
    /// everybody notices the difference.
    /// </summary>
    private const float Height = 2.9f;

    private const float HalfWidth = Width / 2f;
    private const float HalfLength = Length / 2f;

    /// <summary>
    /// Where the way out is cut in the north wall.
    ///
    /// East of centre, which is what leaves the west three quarters of that wall for the palette. It is
    /// also what stops the two doorways being an enfilade: the entrance is in the middle of the west wall
    /// and this is in the north one, so from either of them the other is a slot seen edge-on.
    /// </summary>
    private const float WayX = 1.2f;

    /// <summary>The two stands, and how much of a four-solid row fits on one.</summary>
    private const float StandHeight = 0.85f;

    private const float StandLength = 3.8f;
    private const float RowScale = 0.46f;

    /// <summary>Where the palette's stand is centred, which is not the middle of its wall — the doorway has
    /// the east end of it. The band row opposite is centred, and the two rows are therefore a metre and a
    /// half out of line with each other, which is the one asymmetry in the room and is what a doorway in a
    /// seven-metre wall costs.</summary>
    private const float PaletteX = -1.55f;

    private const float BandZ = -3.5f;
    private const float PaletteZ = 3.5f;

    private static readonly Vector3 BandAt = new(0f, Height - 0.12f, -2.6f);

    /// <summary>How far the band row's lamp travels either side of centre.</summary>
    public const float Travel = 1.9f;

    public Studio(Hall hall)
    {
        var root = hall.Add(Deck.StudioRoom, Deck.Studio);

        // The gallery's rung, painted out. See the class remarks: the maps are all there and the colour
        // under them is a third of what the rest of the building's plaster is, so the relief survives and
        // does not compete. Dropping to a lower Grade instead would have made the wall flat as well as
        // dark, which is a different room and a worse one — the two exhibits in here are about light on a
        // surface, and a surface with nothing on it is the wrong thing to stand them against.
        var wall = Finish.Plaster(Grade.Dressed);
        wall.BaseColor = new Vector4(0.16f, 0.16f, 0.18f, 1f);

        var tile = Finish.Floor(Grade.Dressed);
        var stone = Finish.Stone(Grade.Dressed);
        var brushed = Finish.Brushed();

        var t = Deck.WallThickness;

        root.Children.Add(Fabric.Sheet(Width + 0.7f, Length + 0.7f, 0f, tile, "floor"));
        root.Children.Add(Fabric.Lid(Width + 0.7f, Length + 0.7f, Height, wall, "ceiling"));

        // The west wall, carrying the doorway out of the gallery — and the opening in it is fifty
        // millimetres wider and thirty taller than the standard one. That is not a size, it is the fix the
        // lounge's east wall used to carry: two walls that overlap and then cut the same hole put their
        // reveals in one plane and their soffits in another, and the depth test answers that per pixel and
        // per frame. Interpenetrating solids only work if they interpenetrate everywhere, and a hole is a
        // solid turned inside out. The extra is buried inside the gallery's wall where nothing can see it.
        var west = Fabric.PiercedWall(
            Length + 0.7f, Height, t,
            doorCentre: 0f, Deck.DoorWidth + 0.05f, Deck.DoorHeight + 0.03f, wall);
        west.Position = new Vector3(-(HalfWidth + t / 2f), 0f, 0f);
        west.RotationDegrees = new Vector3(0f, 90f, 0f);
        root.Children.Add(west);

        // The north wall, carrying the doorway into the pattern shop, with the same fix for the same
        // reason. Written unrotated, so WayX means what it says: a wall turned a quarter sends its X to
        // −Z and every offset in it reads backwards, which is a good way to write a bug that renders
        // perfectly. See the note in ScreenRoom, which had one for four rooms.
        var north = Fabric.PiercedWall(
            Width + 0.7f, Height, t,
            WayX, Deck.DoorWidth + 0.05f, Deck.DoorHeight + 0.03f, wall);
        north.Position = new Vector3(0f, 0f, HalfLength + t / 2f);
        root.Children.Add(north);

        var south = Fabric.Wall(Width + 0.7f, Height, t, wall);
        south.Position = new Vector3(0f, 0f, -(HalfLength + t / 2f));
        root.Children.Add(south);

        var east = Fabric.Wall(Length + 0.7f, Height, t, wall);
        east.Position = new Vector3(HalfWidth + t / 2f, 0f, 0f);
        east.RotationDegrees = new Vector3(0f, 90f, 0f);
        root.Children.Add(east);

        // A brushed skirting round the four walls, sunk into them so no two faces are level. It is the
        // whole of the room's fabric and it is there for one reason: a dark room with a dark floor has no
        // line where the two meet, and a corner you cannot find is a room with no size.
        foreach (var (centre, size) in new (Vector3, Vector3)[]
                 {
                     (new Vector3(0f, 0f, -HalfLength), new Vector3(Width, 0.12f, 0.05f)),
                     (new Vector3(0f, 0f, HalfLength), new Vector3(Width, 0.12f, 0.05f)),
                     (new Vector3(-HalfWidth, 0f, 0f), new Vector3(0.05f, 0.12f, Length)),
                     (new Vector3(HalfWidth, 0f, 0f), new Vector3(0.05f, 0.12f, Length))
                 })
            root.Children.Add(Fabric.Slab(
                size,
                centre + new Vector3(
                    -MathF.CopySign(0.015f, centre.X), 0.06f, -MathF.CopySign(0.015f, centre.Z)),
                brushed,
                "skirting",
                Finish.Close));

        // The band row: four solids on a stand, two bands, three, five and Standard, which is
        // ToonScene mounted. Same file, same materials, same order as the one the picker builds against a
        // black background — what this room supplies is a wall to stand it against and a lamp that moves.
        root.Children.Add(Fabric.Slab(
            new Vector3(StandLength + 0.6f, StandHeight, 0.7f),
            new Vector3(0f, StandHeight / 2f, BandZ),
            stone,
            "stand"));

        Bands = Mount(new ToonScene().BuildSubject()!, "exhibit.bands", 0f, BandZ);
        root.Children.Add(Bands);

        // The palette: the same arrangement facing it, which is MatcapScene mounted — three objects
        // wearing three lit spheres, and a fourth that is an ordinary metal so the comparison is in the
        // row rather than across the room.
        root.Children.Add(Fabric.Slab(
            new Vector3(StandLength + 0.6f, StandHeight, 0.7f),
            new Vector3(PaletteX, StandHeight / 2f, PaletteZ),
            stone,
            "stand"));

        var caps = Mount(new MatcapScene().BuildSubject()!, "exhibit.caps", PaletteX, PaletteZ);

        // <b>And both benches get a label.</b> The note about this room was a person standing in front of
        // three spheres asking whether the left one was supposed to be transparent — which is exactly what
        // a matcap looks like when nothing has told you it is one. See Fabric.Label.
        //
        // The two benches face opposite ways down the room, so the plates do too: the bands are at the
        // north end and read from the south, the matcaps at the south end and read from the north.
        root.Children.Add(Fabric.Label(
            "TOON SHADING", new Vector3(0f, StandHeight - 0.16f, BandZ + 0.36f), 0.030f, 0f));

        root.Children.Add(Fabric.Label(
            "MATCAP", new Vector3(PaletteX, StandHeight - 0.16f, PaletteZ - 0.36f), 0.030f, 180f));
        root.Children.Add(caps);

        // And the three images themselves, hung flat on the wall above the three objects wearing them.
        //
        // <b>They are the same Texture instances, not three more built the same way.</b> A picture that
        // agrees with the exhibit is not the exhibit, and the whole claim being made here is that what is
        // on the wall is what is on the sphere — see MatcapScene.Palette, which is shared for exactly this
        // and is one upload rather than two.
        //
        // Unlit, because a photograph of a lit sphere is a photograph. It is also what keeps them in the
        // picture when the lamps go out, which is the frame this room exists for: three images, three
        // objects wearing them, and nothing else in the room lit at all.
        for (var i = 0; i < MatcapScene.Palette.Count; i++)
        {
            var x = PaletteX + (i - 1.5f) * 1.95f * RowScale;

            root.Children.Add(Fabric.Slab(
                new Vector3(0.88f, 0.88f, 0.04f),
                new Vector3(x, 2.05f, PaletteZ + 0.66f),
                brushed,
                "frame"));

            root.Children.Add(Fabric.Panel(
                new Vector3(0.80f, 0.80f, 0.03f),
                new Vector3(x, 2.05f, PaletteZ + 0.63f),
                new Material
                {
                    Name = $"palette.{i}",
                    BaseColorTexture = MatcapScene.Palette[i],
                    Unlit = true
                },
                "palette",
                Vector2.Zero,
                Vector2.One,
                facing: -1f));
        }

        // The rail the band lamp runs on. It is the only fitting in the building that is not bolted where
        // it is, and it needs to look like one thing that a lamp could travel along rather than like a lamp
        // that has come loose.
        root.Children.Add(Fabric.Slab(
            new Vector3(Travel * 2f + 0.5f, 0.06f, 0.09f),
            new Vector3(0f, Height - 0.03f, BandAt.Z),
            brushed,
            "rail",
            Finish.Close));

        Band = Fabric.Ceiling(Deck.Studio, BandAt, 2.6f, 6f);
        Palette = Fabric.Ceiling(Deck.Studio, new Vector3(PaletteX, Height - 0.12f, 2.6f), 2.4f, 6f);
        Entry = Fabric.Ceiling(Deck.Studio, new Vector3(-2.5f, Height - 0.12f, 0f), 1.9f, 5.5f);
        Way = Fabric.Ceiling(Deck.Studio, new Vector3(WayX, Height - 0.12f, 3.1f), 1.8f, 5f);

        foreach (var lamp in All)
        {
            root.Children.Add(lamp.Fixture);
            lamp.Dim(0f);
        }

        return;

        // A mounted row: scaled to the stand, seated on it by its own lowest point, and named so the free
        // walk and the audit have something to report. Read after the scale and before the position,
        // because Bounds is the node's extent through its own transform and would otherwise be measuring
        // the answer.
        Node Mount(Node row, string name, float x, float z)
        {
            row.Name = name;
            row.Scale = new Vector3(RowScale);
            row.Position = new Vector3(x, StandHeight - row.Bounds.Min.Y, z);

            return row;
        }
    }

    /// <summary>The cel-shaded row, kept so the chapter can say what it is doing to it.</summary>
    public Node Bands { get; }

    /// <summary>The lamp over the band row, which travels. See <see cref="Slide"/>.</summary>
    public Lamp Band { get; }

    /// <summary>The one over the matcaps, which does not.</summary>
    public Lamp Palette { get; }

    /// <summary>The one inside the door he comes in by.</summary>
    public Lamp Entry { get; }

    /// <summary>And the one at the way out.</summary>
    public Lamp Way { get; }

    /// <summary>All four, for switching the room off in one line — which is what this room is for.</summary>
    public Lamp[] All => [Band, Palette, Entry, Way];

    /// <summary>The middle of the cel-shaded row, in world coordinates.</summary>
    public static Vector3 Row =>
        Deck.Studio + new Vector3(0f, StandHeight + 0.45f, BandZ);

    /// <summary>The middle of the matcap row.</summary>
    public static Vector3 Caps =>
        Deck.Studio + new Vector3(PaletteX, StandHeight + 0.45f, PaletteZ);

    /// <summary>The three images on the wall above it.</summary>
    public static Vector3 Wall => Deck.Studio + new Vector3(PaletteX, 2.05f, PaletteZ);

    /// <summary>The doorway in, in world coordinates.</summary>
    public static Vector3 Entrance => Deck.Studio + new Vector3(-HalfWidth, Deck.Eye, 0f);

    /// <summary>The doorway out.</summary>
    public static Vector3 Exit => Deck.Studio + new Vector3(WayX, Deck.Eye, HalfLength);

    /// <summary>A point on the studio's floor at eye height: how far up the room from its middle, and how
    /// far east of the centre line.</summary>
    public static Vector3 At(float along, float across = 0f) =>
        Deck.Studio + new Vector3(across, Deck.Eye, along);

    /// <summary>
    /// Moves the band row's lamp along its rail.
    ///
    /// Both halves, and that is the whole reason this is a method rather than a line in the chapter. A
    /// <see cref="Lamp"/>'s fixture is a node in the room's coordinates and its light is a
    /// <see cref="PointLight"/> on the scene in world ones — see <see cref="Fabric.Ceiling"/>, which takes
    /// the room's origin for exactly this reason. Move one and not the other and what you get is a fitting
    /// sliding along a rail with its light left behind, which reads as the lamp not working rather than as
    /// a mistake.
    /// </summary>
    public void Slide(float x)
    {
        x = Math.Clamp(x, -Travel, Travel);

        Band.Fixture.Position = new Vector3(x, BandAt.Y, BandAt.Z);
        Band.Light.Position = Deck.Studio + new Vector3(x, BandAt.Y - 0.1f, BandAt.Z);
    }
}
