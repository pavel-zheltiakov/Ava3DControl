using System.Numerics;
using Ava3D.Demo.Views;

namespace Ava3D.Demo.Story;

/// <summary>
/// What the building is made of, and the handful of shapes it is made from.
///
/// Every room in the exhibition is boxes, cylinders and planes, and that is not thrift — it is the
/// argument chapter 2 is about. A room built out of <see cref="Primitives"/> is a better demonstration of
/// the primitive builders than a row of them on plinths could ever be, because nobody arranges six solids
/// in a line by accident and everybody recognises a wall.
///
/// The unit is one metre, as in <c>Contact</c>. That matters for the lamps: point lights fall off as
/// 1/d², so a lamp three metres up divides by nine, where a lamp three hundred units from a hull in the
/// film divides by ninety thousand. Room-scale lighting is authored in ordinary small numbers and the film
/// needs its candela constant, which is the same physics reading very differently at two scales.
/// </summary>
internal static class Fabric
{
    public static Material Brass => new()
    {
        BaseColor = new Vector4(0.72f, 0.56f, 0.24f, 1f),
        Metallic = 1f,
        Roughness = 0.34f
    };

    public static Material DarkMetal => new()
    {
        BaseColor = new Vector4(0.10f, 0.11f, 0.13f, 1f),
        Metallic = 0.9f,
        Roughness = 0.45f
    };

    /// <summary>
    /// A surface that is its own light source. Unlit, so the four slots do not have to reach it — which
    /// is what makes the blackout chapter possible and what makes the screens legible in a dark room.
    /// </summary>
    public static Material Emissive(float r, float g, float b) => new()
    {
        BaseColor = new Vector4(r, g, b, 1f),
        EmissiveColor = new Vector3(r, g, b),
        Unlit = true
    };

    /// <summary>A box, positioned by its centre.</summary>
    /// <param name="metres">How much surface one image covers. <see cref="Finish.Close"/> for furniture —
    /// see there for why one number does not do.</param>
    public static MeshNode Slab(
        Vector3 size, Vector3 centre, Material material, string? name = null, float metres = Finish.Pitch,
        bool turned = false) =>
        new(Map(Primitives.Box(size.X, size.Y, size.Z), material, centre, metres, turned), material)
        {
            Position = centre,
            Name = name
        };

    /// <summary>
    /// A box carrying one rectangle of an image across its own face, corner to corner.
    ///
    /// This is the opposite of what <see cref="Map"/> does, and the two are for opposite jobs. A wall wants
    /// the room's grid: a tile has a real size in metres and has to be that size wherever a piece of wall
    /// happens to stand, which is the whole reason <see cref="Map"/> works in world coordinates. A screen
    /// wants nothing of the kind. What is on a screen is <i>one image</i>, and it is the same image edge to
    /// edge whether the screen is a hand across or a metre — so the mapping has to come from the object
    /// rather than from where it is standing.
    ///
    /// Screens built the other way had two faults, and both of them are what the world grid is for
    /// elsewhere: the layout was cut off at the panel edge at whatever phase the desk happened to sit at in
    /// the room, and a panel wider than the pitch got several copies of it. Neither is a thing a screen has
    /// ever done.
    ///
    /// <paramref name="origin"/> and <paramref name="span"/> pick the rectangle, which is what lets one
    /// sheet carry nine layouts and still be one upload. See <see cref="Finish.Readout"/>.
    /// </summary>
    /// <param name="facing">
    /// Which way the front of the panel points along Z, as +1 or −1, and it is not optional.
    ///
    /// <b>A panel seen from the far side is a panel seen in a mirror.</b> Somebody standing where +Z is
    /// away from them has +X on their right; somebody on the other side of the same surface has it on
    /// their left. Mapping both the same way puts one of the two layouts back to front — the header at the
    /// top right, the left-hand instrument on the right — which nothing had ever noticed while the image
    /// on a screen was a repeating tile with no side to it.
    /// </param>
    public static MeshNode Panel(
        Vector3 size, Vector3 centre, Material material, string name, Vector2 origin, Vector2 span,
        float facing)
    {
        var mesh = Primitives.Box(size.X, size.Y, size.Z);
        var positions = mesh.Positions;
        var uvs = new Vector2[positions.Length];

        // From the vertex's own position, not the room's. v runs downward, as it does everywhere else here
        // and for the same reason — every image in Finish is drawn with its top at v = 0.
        //
        // The sides and the back of the box get the edge of the rectangle smeared along them, which is
        // right for the only thing this is used on: a screen is twenty millimetres deep, set into a bezel,
        // and nobody ever sees anything but its front.
        for (var i = 0; i < positions.Length; i++)
            uvs[i] = origin + span * new Vector2(
                0.5f + facing * positions[i].X / size.X,
                0.5f - positions[i].Y / size.Y);

        return new MeshNode(mesh.WithTexCoords(uvs), material) { Position = centre, Name = name };
    }

    /// <summary>A horizontal plane at a height, facing up. Floors.</summary>
    public static MeshNode Sheet(float width, float depth, float y, Material material, string? name = null) =>
        new(Map(Primitives.Plane(width, depth), material, new Vector3(0f, y, 0f)), material)
        {
            Position = new Vector3(0f, y, 0f),
            Name = name
        };

    /// <summary>
    /// The same plane, turned over so it faces down. Ceilings.
    ///
    /// A plane's normal points up, which is right for the floor and exactly wrong overhead: a ceiling built
    /// out of <see cref="Sheet"/> is lit as though the lamps were above it, so it renders black however
    /// bright the room is. Both faces are drawn — the material is double sided — which is why the mistake
    /// does not show up as a missing ceiling but as a room that feels like an open box, and takes a while
    /// to attribute to anything.
    /// </summary>
    public static MeshNode Lid(float width, float depth, float y, Material material, string? name = null) =>
        new(Map(Primitives.Plane(width, depth), material, new Vector3(0f, y, 0f)), material)
        {
            Position = new Vector3(0f, y, 0f),
            RotationDegrees = new Vector3(180f, 0f, 0f),
            Name = name
        };

    /// <summary>
    /// Gives a mesh texture coordinates in metres of the room it stands in, and tangents if it needs them.
    ///
    /// The maps in <see cref="Finish"/> are all authored at one scale — <see cref="Finish.Pitch"/> metres
    /// an image — and the builders below hand out boxes and planes of every size in the building, so
    /// something has to reconcile the two. This does, by throwing each vertex's position at the two axes
    /// its normal is not pointing along. A box comes out planar-mapped per face, a plane comes out mapped
    /// across itself, and a tile is the same size on a wall six metres long and on the strip of wall over
    /// a door.
    ///
    /// The <paramref name="offset"/> is what makes it a <i>room</i> coordinate rather than a mesh one, and
    /// it is the difference between panelling and wallpaper. <see cref="Primitives.Box"/> builds about its
    /// own centre, so three slabs making up one pierced wall would each start their grid from their own
    /// middle — three panel grids at three arbitrary phases, meeting at the door reveals, which reads
    /// exactly as wrong as it sounds. Folding the piece's position in first puts every piece of a wall on
    /// one grid. Rooms whose walls are turned still meet a corner at an arbitrary phase, and so does every
    /// panelled room that was ever built.
    ///
    /// Nothing happens at all to a mesh whose material carries no maps, which is most of the building: the
    /// antechamber keeps the texture coordinates <see cref="Primitives"/> gave it and pays for none of this.
    /// </summary>
    /// <param name="turned">
    /// Swap the two axes, so that whatever the image runs along runs the other way on the surface.
    ///
    /// Every image in <see cref="Finish"/> with a direction in it — boards, oak, brushing — runs along
    /// <c>u</c>, and <c>u</c> lands on the world's X on a floor and along the wall on a wall. That is
    /// right for a runner down a room and wrong for the boards of a ceiling laid across its beams, or a
    /// door whose planks stand on end; a quarter turn is the whole difference and it is one swap.
    /// </param>
    public static Mesh Map(
        Mesh mesh, Material material, Vector3 offset, float metres = Finish.Pitch, bool turned = false)
    {
        if (material.BaseColorTexture is null && material.NormalTexture is null)
            return mesh;

        var positions = mesh.Positions;
        var normals = mesh.Normals;
        var uvs = new Vector2[positions.Length];

        for (var i = 0; i < positions.Length; i++)
        {
            var p = positions[i] + offset;
            var n = normals is null ? Vector3.UnitY : normals[i];

            var ax = MathF.Abs(n.X);
            var ay = MathF.Abs(n.Y);
            var az = MathF.Abs(n.Z);

            // v runs downward on a wall, which is the convention every image here is drawn in and the
            // reason floorboards do not come out standing on end.
            var uv = ay >= ax && ay >= az
                ? new Vector2(p.X, p.Z)
                : ax >= az
                    ? new Vector2(p.Z, -p.Y)
                    : new Vector2(p.X, -p.Y);

            if (turned)
                uv = new Vector2(uv.Y, uv.X);

            uvs[i] = uv / metres;
        }

        mesh = mesh.WithTexCoords(uvs);

        // Tangents only where a normal map will read them. They are derived from the coordinates just
        // written, so this has to come second, and a mesh without them renders a normal map as no normal
        // map at all — silently, which is the kind of bug that gets attributed to the map being too weak.
        return material.NormalTexture is null ? mesh : mesh.WithGeneratedTangents();
    }

    /// <summary>
    /// The same mesh seen from inside: every normal reversed.
    ///
    /// <see cref="Primitives"/> builds solids, and a solid's normals point out of it. That is right for
    /// everything standing in a room and wrong for anything that <i>is</i> the room — a cylinder used as an
    /// apse is lit as though the lamps were outside its wall, which renders as black however bright the
    /// room is. It is the same mistake <see cref="Lid"/> exists to prevent, one dimension further round,
    /// and it has the same nice property: flipping all of them at once also turns the end caps into a
    /// floor and a ceiling that face the right way.
    /// </summary>
    public static Mesh Inverted(Mesh mesh)
    {
        var normals = mesh.Normals;

        if (normals is null)
            return mesh;

        var flipped = new Vector3[normals.Length];

        for (var i = 0; i < normals.Length; i++)
            flipped[i] = -normals[i];

        return mesh.WithNormals(flipped);
    }

    /// <summary>
    /// A wall with a doorway in it, built as three slabs: left of the opening, right of it, and the lintel
    /// over the top.
    ///
    /// Three boxes rather than one wall with a hole, because a hole is a mesh operation this control does
    /// not have and does not need — the opening is where the geometry is not. It also means a doorway
    /// costs three draws and no thought, which is the right price for something the plan uses six times.
    /// </summary>
    /// <param name="length">How wide the wall is.</param>
    /// <param name="height">Floor to ceiling.</param>
    /// <param name="thickness">How deep the wall is.</param>
    /// <param name="doorCentre">Where the opening's centre sits along the wall, from the wall's middle.</param>
    /// <param name="doorWidth">The opening.</param>
    /// <param name="doorHeight">The opening.</param>
    public static Node PiercedWall(
        float length, float height, float thickness,
        float doorCentre, float doorWidth, float doorHeight, Material material)
    {
        var wall = new Node { Name = "wall" };

        var leftEdge = -length / 2f;
        var rightEdge = length / 2f;
        var doorLeft = doorCentre - doorWidth / 2f;
        var doorRight = doorCentre + doorWidth / 2f;

        var leftWidth = doorLeft - leftEdge;
        if (leftWidth > 0.01f)
            wall.Children.Add(Slab(
                new Vector3(leftWidth, height, thickness),
                new Vector3(leftEdge + leftWidth / 2f, height / 2f, 0f),
                material));

        var rightWidth = rightEdge - doorRight;
        if (rightWidth > 0.01f)
            wall.Children.Add(Slab(
                new Vector3(rightWidth, height, thickness),
                new Vector3(doorRight + rightWidth / 2f, height / 2f, 0f),
                material));

        var lintel = height - doorHeight;
        if (lintel > 0.01f)
            wall.Children.Add(Slab(
                new Vector3(doorWidth, lintel, thickness),
                new Vector3(doorCentre, doorHeight + lintel / 2f, 0f),
                material));

        return wall;
    }

    /// <summary>
    /// A museum label: a small plate with the exhibit's name cut into it.
    ///
    /// <b>It is the answer to four separate notes that all said the same thing.</b> "This scene is not
    /// clear what is shown." "This flashing circle, not clear what is shown." "This ball at left must be
    /// transparent, or what does this scene show?" Every one of those is a person standing in front of an
    /// exhibit with no idea what it is exhibiting — which is not a failure of the exhibit, it is a museum
    /// with nothing written on the walls. A gallery does not solve this by making the objects more
    /// obvious. It writes a label and screws it to the plinth.
    ///
    /// The lettering is <see cref="Glyphs"/>, which is the caption band's own face and is already used to
    /// draw the title card in three dimensions — so it costs no font, no atlas and no text layout, and it
    /// is the same five-by-seven stroke alphabet everywhere in the demo. Drawn as lines and additive, so
    /// the label reads at any lamp level in any room and cannot go dark when its exhibit does.
    /// </summary>
    /// <param name="text">What it says. Upper case; the alphabet has no lower case in it.</param>
    /// <param name="at">Where the middle of the plate goes, in the room's own frame.</param>
    /// <param name="cap">Cap height in metres. Thirty millimetres reads from two.</param>
    /// <param name="yaw">Which way it faces, in degrees, if it is not facing the room's +Z.</param>
    public static Node Label(string text, Vector3 at, float cap = 0.030f, float yaw = 0f, Vector3? colour = null)
    {
        var wide = (float)Glyphs.Measure(text, cap);

        var node = new Node
        {
            Name = "label",
            Position = at,
            RotationDegrees = new Vector3(0f, yaw, 0f)
        };

        node.Children.Add(Slab(
            new Vector3(wide + cap * 2.4f, cap * 2.8f, 0.010f),
            Vector3.Zero,
            Plaque,
            "label.plate",
            Finish.Close));

        node.Children.Add(new LineNode
        {
            Positions = Lettering(text, cap),
            Color = colour ?? new Vector3(0.90f, 0.93f, 1f),
            Width = 2f,
            Blend = BlendMode.Additive,
            DepthWrite = false,
            RenderOrder = 4,
            Name = "label.text"
        });

        return node;
    }

    /// <summary>
    /// The word as segment pairs, centred about the origin and standing six millimetres proud of the
    /// plate.
    ///
    /// <see cref="Glyphs.Segments"/> lays a word out left to right from the origin with y running
    /// <i>down</i> the cell, which is what a canvas wants and is upside down in a scene — so this flips
    /// it, scales the cell height to the cap height asked for, and slides it half its own width left. The
    /// same three lines <c>Curtain.Card</c> does, for the same reason.
    /// </summary>
    private static Vector3[] Lettering(string text, float cap)
    {
        var flat = Glyphs.Segments(text);
        var wide = (float)Glyphs.Measure(text, Glyphs.CellHeight);
        var scale = cap / Glyphs.CellHeight;

        var word = new Vector3[flat.Length];

        for (var i = 0; i < flat.Length; i++)
            word[i] = new Vector3(
                (flat[i].X - wide / 2f) * scale,
                (Glyphs.CellHeight / 2f - flat[i].Y) * scale,
                0.006f);

        return word;
    }

    /// <summary>Anodised plate, dark enough that white lettering carries and matt enough that it does not
    /// throw an exhibit's own lamp back at whoever is reading it.</summary>
    private static Material Plaque => new()
    {
        BaseColor = new Vector4(0.09f, 0.095f, 0.105f, 1f),
        Metallic = 0.35f,
        Roughness = 0.62f,
        Name = "plaque"
    };

    /// <summary>A solid wall: the same thing with no opening.</summary>
    public static Node Wall(float length, float height, float thickness, Material material) =>
        new Node { Name = "wall", Children = { Slab(
            new Vector3(length, height, thickness),
            new Vector3(0f, height / 2f, 0f),
            material) } };

    /// <summary>
    /// A plinth with a top face to stand something on, and the height of that face in
    /// <see cref="Exhibit.Top"/>.
    ///
    /// A metre, because that is what a plinth is: high enough that the thing on it is at chest height and
    /// low enough to look down into. The story mounts a scene's subject at <see cref="Exhibit.Top"/> and
    /// never has to know what the scene thought its own floor was.
    /// </summary>
    public static Exhibit Plinth(Vector3 at, float width = 0.6f, float height = 1f, Grade grade = Grade.Flat)
    {
        var root = new Node { Name = "plinth", Position = at };
        var stone = Finish.Stone(grade);

        root.Children.Add(Slab(
            new Vector3(width, height, width),
            new Vector3(0f, height / 2f, 0f),
            stone));

        // A lip, so the top reads as a surface something was placed on rather than as the end of a post.
        root.Children.Add(Slab(
            new Vector3(width + 0.08f, 0.04f, width + 0.08f),
            new Vector3(0f, height + 0.02f, 0f),
            stone));

        return new Exhibit(root, at + new Vector3(0f, height + 0.04f, 0f));
    }

    /// <summary>
    /// A warm ceiling lamp: the fixture you can see and the light it casts, kept together.
    ///
    /// It takes the room's origin as well as the place in the room, and that is not tidiness — it is the
    /// only way the two halves can agree. A fixture is a <see cref="Node"/> and hangs off the room's root,
    /// so it is positioned in the room's coordinates; a <see cref="PointLight"/> lives on the
    /// <see cref="Scene"/> rather than in the node tree, so its position is in world coordinates and
    /// nothing transforms it. Hand one call the same vector for both and every room whose origin is not
    /// the deck origin gets a visible fixture with its light left behind at the origin.
    ///
    /// The antechamber sits at the deck origin, so it was right by accident, and the screen room was not:
    /// its three lamps were twenty-four metres south of the room they were drawn in, which reads not as
    /// darkness but as a room that will not brighten however hard you turn the lamps up. Two rounds of
    /// raising the brightness did nothing at all before the ambient — which is a scene property and has no
    /// position — made the room appear.
    /// </summary>
    /// <param name="room">The room's origin on the deck. <see cref="Deck"/> has them all.</param>
    /// <param name="at">Where the lamp is inside that room.</param>
    public static Lamp Ceiling(Vector3 room, Vector3 at, float brightness = 6f, float range = 7f)
    {
        var fixture = new Node { Name = "lamp", Position = at };

        fixture.Children.Add(Slab(
            new Vector3(0.26f, 0.05f, 0.26f),
            Vector3.Zero,
            DarkMetal));

        var bulb = Slab(
            new Vector3(0.20f, 0.03f, 0.20f),
            new Vector3(0f, -0.04f, 0f),
            Emissive(1f, 0.86f, 0.66f));

        fixture.Children.Add(bulb);

        var light = new PointLight
        {
            Position = room + at + new Vector3(0f, -0.1f, 0f),
            Color = new Vector3(1f, 0.88f, 0.72f),
            Intensity = brightness,
            Range = range,
            Decay = 2f
        };

        return new Lamp(fixture, bulb, light, brightness);
    }
}

/// <summary>A thing to stand on, and the height of the face to stand things on.</summary>
internal readonly record struct Exhibit(Node Root, Vector3 Top);

/// <summary>
/// A lamp: what you see, the part of it that glows, the light it casts, and how bright it is when it is
/// fully on.
///
/// The three are kept together because switching a lamp off means all three — the light stops
/// contributing, the bulb stops emitting, and the fixture stays exactly where it was. A room that dims its
/// lights but leaves glowing rectangles on the ceiling is the tell that somebody only remembered the first
/// of those.
/// </summary>
internal sealed record Lamp(Node Fixture, MeshNode Bulb, PointLight Light, float Full)
{
    /// <summary>Dims the lamp to a fraction of full, light and bulb together.</summary>
    public void Dim(float fraction)
    {
        fraction = Math.Clamp(fraction, 0f, 1f);

        Light.Intensity = Full * fraction;
        Bulb.Material.EmissiveColor = new Vector3(1f, 0.86f, 0.66f) * fraction;
        Bulb.Material.BaseColor = new Vector4(new Vector3(1f, 0.86f, 0.66f) * fraction, 1f);
    }
}
