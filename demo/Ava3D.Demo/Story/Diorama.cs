using System.Numerics;
using Ava3D.Demo.Scenes.Arcade;

namespace Ava3D.Demo.Story;

/// <summary>
/// The case on the lounge's west wall: the first game taken apart, with its sprites lifted off the tube and
/// stood up in a row.
///
/// <b>What it is for.</b> A billboard is a quad that turns to face the camera every frame, and there is
/// exactly one way to watch that happen: move sideways past it. A camera that circles a sprite proves it —
/// which is what the reference scene does, because a scene on its own has nobody in it to walk — but a
/// person walking down a room past a case does it better, because the case is in the same frame being
/// foreshortened. The bars across the opening turn and thin as he goes by; the figures behind them do not
/// turn at all. Both are drawn by the same renderer in the same pass, and the difference between them is
/// the whole feature.
///
/// <b>Why these four pieces and no others.</b> <see cref="GrassBricksScene.Cast"/> is every sprite in that
/// game and it is four: a runner in three frames, and a cloud. Everything else on that screen — the bricks,
/// the pipes, the coins, the hills — is rectangles filled with a colour, which is what a tile is. So the
/// case is not a selection from the game's art, it is the game's art; and the row is not an arrangement,
/// it is the run cycle laid out in space instead of in time, which is the one thing these three pictures
/// can say here that they cannot say on the tube.
///
/// <b>It costs no light slot.</b> A sprite carries its own colour and is not lit by anything; the back of
/// the case is an unlit panel and so is the picture on it. There are four slots in this building, the
/// lounge is spending all four on its own lamps and will hand every one of them to the alcove before the
/// chapter is over, and this case never asks for one. That is the claim the four televisions make, made a
/// second time by something that is not a television — which is worth having, because a screen that lights
/// itself surprises nobody and a museum case that does is nearer the point.
///
/// <b>The one lit thing in it is the ground.</b> The strip of turf the figures stand on is geometry with an
/// ordinary material on it, so it goes down with the room while they do not. By the time he sits on the
/// sofa the case is three figures and a sky hanging in the dark, which is what the four sets across the
/// room are, and it got there without anybody switching anything.
///
/// It is on the west wall because that is the only wall of this room that is free — the south has the
/// bench, the east the way in, the north the alcove and the way out — and it is a lucky constraint: the
/// west wall is where he already is when he leaves the last set.
/// </summary>
internal static class Diorama
{
    /// <summary>The back of the case, a shade proud of the wall's inner face so the two do not fight.</summary>
    private const float Back = -4.36f;

    /// <summary>The plane of the opening. Everything inside stands between this and <see cref="Back"/>.</summary>
    public const float Front = -3.88f;

    /// <summary>
    /// The interior, floor and ceiling.
    ///
    /// The sill is a metre up, which puts the middle of the opening at a metre forty-six — a hand's width
    /// below a standing eye. That is what makes it a case somebody looks <i>into</i> rather than down at,
    /// and it matters because everything this exhibit does happens in the half-metre of depth between the
    /// bars at the front and the panel at the back, which a camera looking steeply down would flatten away.
    /// </summary>
    private const float Sill = 1.00f;

    private const float Head = 1.92f;

    /// <summary>Halfway up the opening. What a walk past it aims at.</summary>
    public const float Middle = (Sill + Head) / 2f;

    /// <summary>How far the case runs along the wall, either side of the room's centre line.</summary>
    private const float From = -0.70f;

    private const float To = 2.10f;

    /// <summary>The two bars across the opening. They divide nothing — the row runs straight past them —
    /// and they are there to be the thing that foreshortens.</summary>
    private static readonly float[] Bars = [0.02f, 1.38f];

    /// <summary>The top of the strip of ground inside, which is what the figures stand on.</summary>
    private const float Turf = Sill + 0.078f;

    /// <summary>
    /// The row: which of <see cref="GrassBricksScene.Cast"/>, how far forward of the back panel, how far
    /// along the case, and how tall.
    ///
    /// The depths are staggered rather than level, and that is the parallax — walking past, the near
    /// figures cross the far ones, which is the second half of the proof. A row all at one depth would
    /// slide as one card and could be a painting of a row.
    ///
    /// The last is alone at the north end and nearly twice the size. That is the reading-distance entry:
    /// twelve texels magnified to forty-four centimetres, a metre and a half from the eye, where the
    /// nearest-neighbour filter and the hole punched round the silhouette stop being inferences.
    /// </summary>
    private static readonly (int Frame, float Depth, float Along, float Height)[] Row =
    [
        (1, 0.10f, -0.35f, 0.26f),
        (0, 0.30f, 0.30f, 0.26f),
        (2, 0.20f, 0.70f, 0.26f),
        (1, 0.36f, 1.10f, 0.26f),
        (0, 0.34f, 1.74f, 0.44f)
    ];

    /// <summary>Three clouds, high and near the back, at the scale the case is rather than the scale a
    /// cloud is. This is a case, and everything in a case is the size of the case.</summary>
    private static readonly (float Depth, float Along, float Y)[] Weather =
    [
        (0.06f, -0.28f, 1.72f),
        (0.10f, 0.74f, 1.78f),
        (0.05f, 1.66f, 1.74f)
    ];

    /// <summary>Which entry of <see cref="Row"/> is the jump, which is the one that has to be off the
    /// ground.</summary>
    private const int Jumping = 2;

    /// <summary>Where the pass along the case starts and where it ends, so a walk can be written against
    /// the case rather than against two numbers kept in agreement with it by hand.</summary>
    public static float South => From + 0.35f;

    public static float North => To - 0.35f;

    /// <summary>Where a camera walking the case should look to be looking squarely into it: the middle of
    /// the opening, at the point along the wall the camera has reached.</summary>
    public static Vector3 At(float along) => Deck.Screens + new Vector3(Front, Middle, along);

    /// <summary>
    /// Builds the case and hangs it on the room's west wall.
    /// </summary>
    /// <param name="tube">
    /// The first television's own screen material, shared rather than copied.
    ///
    /// This is the detail the exhibit rests on. The panel at the back of the middle bay is not a picture of
    /// that game, it is that game — the same material, carrying the same texture, rewritten ten times a
    /// second by the same instance that is driving the set across the room. The source and the pieces taken
    /// out of it are therefore in step by construction, and the case comes on when the set does because
    /// there is only one thing there to come on.
    /// </param>
    public static void Build(Node root, Material tube)
    {
        var box = new Node { Name = "diorama" };

        var frame = new Material
        {
            BaseColor = new Vector4(0.11f, 0.11f, 0.13f, 1f),
            Roughness = 0.55f,
            Name = "case"
        };

        var middle = (From + To) / 2f;
        var length = To - From;
        var depth = Front - Back;
        var inside = (Back + Front) / 2f;

        // The box: a shelf, a hood and two ends. Half a metre deep, which is enough to stand a row in with
        // room to stagger it and shallow enough that a person out in the room can still see the back — the
        // depth of a shop window, arrived at for the same reason a shop window has it.
        box.Children.Add(Fabric.Slab(
            new Vector3(depth, 0.07f, length + 0.12f),
            new Vector3(inside, Sill - 0.035f, middle), frame, "shelf"));

        box.Children.Add(Fabric.Slab(
            new Vector3(depth, 0.07f, length + 0.12f),
            new Vector3(inside, Head + 0.035f, middle), frame, "hood"));

        foreach (var end in new[] { From - 0.03f, To + 0.03f })
            box.Children.Add(Fabric.Slab(
                new Vector3(depth, Head - Sill, 0.06f),
                new Vector3(inside, Middle, end), frame, "end"));

        // The bars sit at the opening rather than running the whole depth. A mullion half a metre deep
        // would shutter the row off completely from three paces along the wall, which is the difference
        // between a case somebody walks past and a fence they look through.
        foreach (var bar in Bars)
            box.Children.Add(Fabric.Slab(
                new Vector3(0.10f, Head - Sill, 0.045f),
                new Vector3(Front - 0.05f, Middle, bar), frame, "bar"));

        Backdrop(box, tube, length, middle);
        Strip(box, length, middle);
        Pieces(box);

        root.Children.Add(box);
    }

    /// <summary>
    /// The back of the case: the game's sky across the whole of it, and the game itself in the middle.
    ///
    /// The sky is handed over as a single texel rather than as a colour, and that is not a flourish. A
    /// colour written into <see cref="Material.BaseColor"/> and the same colour arriving in a texture do
    /// not necessarily leave the shader at the same value, and this panel is eight millimetres from a
    /// picture whose own sky is those three bytes. Sending both down the texture path is the only way to be
    /// certain they agree, and being certain is worth one texel.
    /// </summary>
    private static void Backdrop(Node box, Material tube, float length, float middle)
    {
        var sky = new Material
        {
            BaseColorTexture = Texel(GrassBricksScene.Sky, "diorama.sky"),
            Unlit = true,
            Name = "diorama.sky"
        };

        box.Children.Add(Panel(length, Head - Sill, Back, middle, sky, "sky"));

        // A dark surround, and it is not decoration — it is the whole difference between a picture and more
        // sky. The game's own sky is the same three bytes as the panel behind it, so without a border the
        // tube's top third dissolves into the back of the case and what is left reads as a hillside
        // floating on the wall. Three centimetres of black all round and it is a screen again.
        box.Children.Add(Panel(
            0.80f * 4f / 3f + 0.06f, 0.86f, Back + 0.004f, middle,
            new Material { BaseColor = new Vector4(0.02f, 0.02f, 0.025f, 1f), Unlit = true, Name = "bezel" },
            "bezel"));

        // Four to three, because what it is a picture of is a television. Eighty centimetres tall in an
        // opening of ninety-two, so a band of the case's own sky is still showing above and below the
        // surround — which is what says the picture is hung on the back of the case rather than being it.
        box.Children.Add(Panel(0.80f * 4f / 3f, 0.80f, Back + 0.008f, middle, tube, "tube"));
    }

    /// <summary>
    /// The strip the figures stand on: soil with grass on it, in the game's own two colours, and the only
    /// thing in the case that is lit.
    ///
    /// It is geometry rather than one more billboard on purpose, and it earns that twice. It is what a
    /// figure needs in order to be standing on something instead of hanging in front of it — a sprite is
    /// anchored at its centre, and half of getting that right is having a floor to put the other half on —
    /// and it is the control in the experiment: it recedes as he walks and the row does not, with nothing
    /// in between them to argue about.
    /// </summary>
    private static void Strip(Node box, float length, float middle)
    {
        box.Children.Add(Fabric.Slab(
            new Vector3(0.34f, 0.06f, length - 0.10f),
            new Vector3(Back + 0.22f, Sill + 0.03f, middle),
            Earth(GrassBricksScene.Soil, 0.85f),
            "soil"));

        box.Children.Add(Fabric.Slab(
            new Vector3(0.34f, 0.018f, length - 0.10f),
            new Vector3(Back + 0.22f, Turf - 0.009f, middle),
            Earth(GrassBricksScene.Turf, 0.90f),
            "turf"));
    }

    /// <summary>
    /// The row, and the weather over it.
    ///
    /// <see cref="SpriteNode.DepthWrite"/> stays off, which is the documented default and the right answer
    /// here: these are alpha-blended quads, and a quad that writes depth punches its own transparent
    /// corners into everything drawn after it. What takes its place is
    /// <see cref="Node.RenderOrder"/> read straight off how far forward the piece stands — centimetres of
    /// depth — so the row draws back to front and overlaps the right way round with nothing sorted at
    /// runtime. Nothing in this case ever moves, so an order decided once is an order that stays right.
    /// </summary>
    private static void Pieces(Node box)
    {
        var cast = GrassBricksScene.Cast;
        var sheets = new Texture[cast.Count];

        for (var i = 0; i < cast.Count; i++)
            sheets[i] = cast[i].Art.Sheet($"diorama.{cast[i].Name}");

        foreach (var (frame, depth, along, height) in Row)
        {
            var art = cast[frame].Art;

            // Six centimetres off the turf for the jump, because it is a jump. It is what makes the row
            // read as one figure doing something rather than as three figures in a queue.
            var lift = frame == Jumping ? 0.06f : 0f;

            box.Children.Add(Billboard(
                sheets[frame],
                new Vector3(Back + depth, Turf + height / 2f + lift, along),
                height * art.Width / (float)art.Height,
                height,
                depth,
                cast[frame].Name));
        }

        var cloud = cast[3].Art;

        foreach (var (depth, along, y) in Weather)
            box.Children.Add(Billboard(
                sheets[3],
                new Vector3(Back + depth, y, along),
                0.26f,
                0.26f * cloud.Height / cloud.Width,
                depth,
                "cloud"));
    }

    private static SpriteNode Billboard(
        Texture sheet, Vector3 at, float width, float height, float depth, string name) => new()
    {
        Texture = sheet,
        Position = at,
        Size = new Vector2(width, height),
        Blend = BlendMode.Alpha,
        DepthWrite = false,
        RenderOrder = (int)(depth * 100f),
        Name = name
    };

    /// <summary>
    /// A panel standing against the back of the case, facing east into the room.
    ///
    /// A plane's normal points up, so it takes both turns to get there: ninety degrees about X stands it
    /// upright facing +Z, and ninety about Y swings that round to +X. The order is not a choice —
    /// <see cref="Node.RotationDegrees"/> composes yaw, then pitch, then roll — and the useful consequence
    /// is that the length the plane was built with comes out running along the room's Z, which is along the
    /// wall, which is what a panel on a wall wants.
    /// </summary>
    private static MeshNode Panel(
        float length, float height, float x, float z, Material material, string name) =>
        new(Primitives.Plane(length, height), material)
        {
            Position = new Vector3(x, Middle, z),
            RotationDegrees = new Vector3(90f, 90f, 0f),
            Name = name
        };

    /// <summary>One of the game's colours as a material, lit — unlike everything else in here — and taking
    /// the same one-texel route to the shader that the sky takes.</summary>
    private static Material Earth(uint rgb, float roughness) => new()
    {
        BaseColorTexture = Texel(rgb, null),
        Roughness = roughness
    };

    /// <summary>One of the game's <c>0xRRGGBB</c> colours as a one-pixel opaque texture.</summary>
    private static Texture Texel(uint rgb, string? name) => Texture.FromPixels(
        [(byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255],
        1, 1, name, TextureWrap.ClampToEdge, TextureFilter.Nearest);
}
