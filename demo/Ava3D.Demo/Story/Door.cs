using System.Numerics;

namespace Ava3D.Demo.Story;

/// <summary>
/// The doors, and the third thing the building says without saying it.
///
/// There are three of them and they are the same joke told three times, each time louder. The first is a
/// white plastic leaf standing open — the most ordinary door there is, and worth drawing for exactly that
/// reason, because a sequence needs somewhere dull to start. The second is oak, and it is heavier and
/// warmer and unremarkable. The third is a metre and a quarter of powered plate that hears you coming and
/// parts in the middle, one half to the left and one half to the right, and there is no house anywhere with
/// one of those in it.
///
/// They also run one rung ahead of the <see cref="Grade"/> the rooms are at, which is deliberate. The
/// plastic leaf is flat colour and two numbers in a room of flat colour, and it is honest: white moulded
/// plastic <i>is</i> flat, and a base-colour map on it would be a lie about how much a material needs. The
/// oak has grain and relief a room before the rotunda's plaster gets any. The powered door has every slot
/// the shading model has — colour, metallic-roughness, normal, occlusion and emissive — and opens into the
/// room where the walls finally catch up. The door announces, the room confirms.
/// </summary>
internal sealed class Door
{
    /// <summary>How far one leaf travels. Half the opening, plus the lap the two make in the middle.</summary>
    private const float Travel = Deck.DoorWidth / 2f + Lap;

    /// <summary>
    /// How long a powered door takes to open, in seconds.
    ///
    /// It is here rather than in the three chapters that open one because it is a fact about the mechanism
    /// and four unrelated things need to agree about it: the three chapters, and <c>Cues.BuildServo</c>,
    /// which builds a motor exactly this long with the stop on the end of it.
    ///
    /// <b>They disagreed, and it was audible.</b> The three doors travelled in two and two fifths, two and a
    /// half and three seconds, and the motor was one and a sixth long whatever the door did — so every door
    /// in the film announced that it had arrived while the picture went on opening it for another second and a
    /// half. <see cref="Cues.Servo"/>'s own note says the stop is the part that matters and that a door which
    /// arrives is different from one that fades out; it is, and it has to arrive at the same moment the leaves
    /// stop moving. One number, one door speed, and a ship whose doors are all the same door.
    /// </summary>
    public const float Opening = 2.5f;

    /// <summary>How far past the middle each leaf reaches when shut, so the joint is a line and not a gap.
    /// Interpenetrating boxes cannot z-fight — see <see cref="Rotunda"/> — so the lap is free.</summary>
    private const float Lap = 0.03f;

    /// <summary>
    /// How far the two leaves stand apart along the wall's own axis, so they can pass each other.
    ///
    /// <b>The lap was not free after all.</b> Two leaves of the same thickness, standing in the same plane
    /// and overlapping by thirty millimetres in the middle, do interpenetrate — but every face they own is
    /// level with the other's, and the thirty-millimetre strip where they overlap is two front faces at one
    /// depth and two back faces at another. That strip is exactly the dark hairline between the two lit
    /// edges, and it is the only part of this door anybody looks at. It crawled.
    ///
    /// Interpenetration is only a cure when the solids interpenetrate <i>everywhere</i>. Five millimetres
    /// of offset makes the right leaf run in front of the left one through the lap, which settles the depth
    /// test by geometry and is also how a bi-parting door actually works: two leaves that meet in the middle
    /// run in two tracks, because two leaves in one track cannot lap at all.
    /// </summary>
    private const float Pass = 0.005f;

    private const float Leaf = Deck.DoorWidth / 2f + Lap;
    private const float Thickness = 0.09f;

    private readonly Node _left;
    private readonly Node _right;
    private readonly Material _strip;
    private readonly Material[] _pilots;

    private Door(Node root, Node left, Node right, Material strip, Material[] pilots)
    {
        Root = root;
        _left = left;
        _right = right;
        _strip = strip;
        _pilots = pilots;
    }

    /// <summary>The whole assembly, for the room to add.</summary>
    public Node Root { get; }

    /// <summary>
    /// A powered door, closed, in an opening of the standard size.
    ///
    /// The leaves retract into the wall rather than into a housing on one side of it, which costs nothing
    /// and is the reason there is no pocket to draw: a leaf nine centimetres thick, standing in the middle
    /// of a wall a quarter of a metre thick, is <i>inside</i> the wall's own slab once it has travelled its
    /// own width. Nothing has to hide it, because the wall already is opaque and already is there.
    ///
    /// It needs the wall it is in to be at least a leaf's width of solid each side of the opening, which is
    /// worth stating because it is the one thing that could quietly break: a wall trimmed narrower than
    /// that would leave a slab of door sticking out past the jamb when it opened, in full view.
    /// </summary>
    /// <param name="at">The middle of the opening, at floor level, in the room's coordinates.</param>
    public static Door Powered(Vector3 at)
    {
        var root = new Node { Name = "door.powered", Position = at };

        // The surround, standing proud of the plaster. It is what makes the thing read as fitted rather
        // than as a hole with a lid: a powered door in a wall with no frame round it looks like a mistake
        // in the wall, and forty millimetres of dark metal round the opening is the whole fix.
        var frame = new Material
        {
            BaseColor = new Vector4(0.13f, 0.14f, 0.16f, 1f),
            Metallic = 0.85f,
            Roughness = 0.38f,
            Name = "door.frame"
        };

        const float jamb = 0.11f;
        const float proud = 0.055f;

        // How far the frame laps over the opening, and it is the whole of what stops this door z-fighting
        // with the wall it is in.
        //
        // The first version stood the frame exactly outside the opening: the jamb's inner face at ±0.6 and
        // the wall's cut face at ±0.6, in the same plane, both drawn. The depth test cannot choose between
        // two surfaces at the same depth, so the answer comes out per pixel and per frame, and what it looks
        // like is a band of diagonal hatching up the reveal — the same shimmer that a coplanar overlay makes
        // and the reason Material.DepthBias exists. The same mistake was under the head, where the frame's
        // underside sat exactly on the lintel's.
        //
        // Two and a half centimetres of lap fixes both, and does it by making the geometry right rather than
        // by biasing it: every face of this frame now either stands in free air or is buried inside the
        // wall, and none of them is level with anything. It also reads better. A door frame that laps its
        // opening is what a door frame is.
        const float bite = 0.025f;

        foreach (var side in new[] { -1f, 1f })
            root.Children.Add(Fabric.Slab(
                new Vector3(jamb + bite, Deck.DoorHeight + jamb, Deck.WallThickness + 2f * proud),
                new Vector3(
                    side * (Deck.DoorWidth / 2f - bite + (jamb + bite) / 2f),
                    (Deck.DoorHeight + jamb) / 2f,
                    0f),
                frame,
                "jamb"));

        root.Children.Add(Fabric.Slab(
            new Vector3(Deck.DoorWidth + 2f * (jamb + bite), jamb, Deck.WallThickness + 2f * proud),
            new Vector3(0f, Deck.DoorHeight - bite + jamb / 2f, 0f),
            frame,
            "head"));

        // Two pilot lights on the head, one each side. Amber shut, green open — the only piece of the
        // building that tells you what it is about to do, and the reason the door does not have to be
        // watched to be understood.
        var pilots = new Material[2];

        for (var i = 0; i < pilots.Length; i++)
        {
            pilots[i] = Fabric.Emissive(1f, 0.55f, 0.12f);

            // One each side of the head, on both faces of it — four boxes, two materials.
            //
            // They were on the +Z face alone, which put all four of them on the side of the wall nobody
            // stands on: this door lives in the gallery's south wall and is first seen from the rotunda,
            // which is behind it. And they were flush with the head rather than proud, and two coplanar
            // faces are the one case the depth test cannot decide. Two mistakes, one symptom, and the
            // symptom is a light that is definitely there and cannot be seen.
            foreach (var face in new[] { -1f, 1f })
                root.Children.Add(Fabric.Slab(
                    new Vector3(0.13f, 0.032f, 0.02f),
                    new Vector3(
                        (i == 0 ? -1f : 1f) * 0.38f,
                        Deck.DoorHeight - bite + jamb / 2f,
                        face * (Deck.WallThickness / 2f + proud + 0.012f)),
                    pilots[i],
                    "pilot"));
        }

        // The leaves. Every slot the shading model has, on a surface that will be a metre from the camera
        // when it opens — which is the only place in the building where a normal map is read at that range.
        var plate = Finish.Panelling();
        plate.BaseColor = new Vector4(0.34f, 0.36f, 0.40f, 1f);
        plate.Metallic = 0.5f;
        plate.Roughness = 0.46f;
        plate.NormalScale = 0.85f;
        plate.Name = "door.leaf";

        // The ribs are a shade darker than the plate rather than the same material. At the same colour they
        // vanished the moment the plate stopped being a mirror, and a leaf with no ribs on it is a slab.
        var rib = plate.Clone();
        rib.BaseColor = new Vector4(0.19f, 0.20f, 0.23f, 1f);
        rib.Roughness = 0.38f;
        rib.Name = "door.rib";

        var strip = Fabric.Emissive(0.42f, 0.72f, 1f);

        var left = Panel(-1f, -Pass / 2f, plate, rib, strip);
        var right = Panel(1f, Pass / 2f, plate, rib, strip);

        root.Children.Add(left);
        root.Children.Add(right);

        return new Door(root, left, right, strip, pilots);
    }

    /// <summary>
    /// How far open the door is: 0 shut, 1 into the wall.
    ///
    /// An assignment, not a step — the chapter hands it a ramp off the film clock, so seeking to the middle
    /// of the approach shows the door halfway open rather than shut and catching up. Everything in this
    /// film that moves is a function of the time and this is no exception; a door with its own state would
    /// be the first thing in the building that could disagree with the clock.
    /// </summary>
    public void Open(float fraction)
    {
        fraction = Math.Clamp(fraction, 0f, 1f);

        _left.Position = new Vector3(-Travel * fraction, 0f, 0f);
        _right.Position = new Vector3(Travel * fraction, 0f, 0f);

        // The leading edge goes out as it goes in, so the two strips do not glow from inside the wall.
        _strip.EmissiveColor = new Vector3(0.42f, 0.72f, 1f) * (1f - fraction);
        _strip.BaseColor = new Vector4(new Vector3(0.42f, 0.72f, 1f) * (1f - fraction), 1f);

        var green = Vector3.Lerp(new Vector3(1f, 0.55f, 0.12f), new Vector3(0.25f, 1f, 0.42f), fraction);

        foreach (var pilot in _pilots)
        {
            pilot.EmissiveColor = green;
            pilot.BaseColor = new Vector4(green, 1f);
        }
    }

    /// <summary>White moulded plastic. Flat colour and a roughness, which is the whole material.</summary>
    public static Material Plastic() => new()
    {
        BaseColor = new Vector4(0.78f, 0.78f, 0.76f, 1f),
        Roughness = 0.35f,
        Name = "plastic"
    };

    /// <summary>
    /// A leaf on hinges, standing open.
    ///
    /// It does not move, and that is not laziness — a door that opens is a thing the film has to spend a
    /// beat on, and the film has exactly one of those to spend and spends it on the third door. These two
    /// are propped open the way every interior door in a building people use is propped open, and what they
    /// are for is to be the first two terms of a sequence whose third term is the point.
    /// </summary>
    /// <param name="hinge">Where the hinge edge meets the floor, in the room's coordinates.</param>
    /// <param name="leaf">What the door is made of.</param>
    /// <param name="swing">
    /// Yaw of the leaf about its hinge. The leaf runs along its own +X, and a yaw of φ sends that to
    /// (cos φ, 0, −sin φ) — so the number a call site wants is worked out from where the leaf should point
    /// and not from how far open the door looks. Getting this backwards puts a door through a wall, which
    /// at least is obvious.
    /// </param>
    public static Node Hinged(Vector3 hinge, Material leaf, float swing)
    {
        const float thickness = 0.045f;

        var door = new Node
        {
            Name = "door.hinged",
            Position = hinge,
            RotationDegrees = new Vector3(0f, swing, 0f)
        };

        door.Children.Add(Fabric.Slab(
            new Vector3(Deck.DoorWidth, Deck.DoorHeight, thickness),
            new Vector3(Deck.DoorWidth / 2f, Deck.DoorHeight / 2f, 0f),
            leaf,
            "leaf"));

        // Two sunk panels, which is what makes a leaf a door rather than a board. They are proud rather
        // than sunk, by four millimetres, because a raised moulding and a sunk one are the same shape from
        // two metres and only one of them is a box.
        foreach (var (bottom, top) in new[] { (0.22f, 1.02f), (1.22f, 2.18f) })
            door.Children.Add(Fabric.Slab(
                new Vector3(Deck.DoorWidth - 0.24f, top - bottom, thickness + 0.008f),
                new Vector3(Deck.DoorWidth / 2f, (bottom + top) / 2f, 0f),
                leaf,
                "panel"));

        foreach (var y in new[] { 0.35f, 1.2f, 2.05f })
            door.Children.Add(Fabric.Slab(
                new Vector3(0.05f, 0.11f, thickness + 0.04f),
                new Vector3(0.02f, y, 0f),
                Fabric.Brass,
                "hinge"));

        door.Children.Add(Fabric.Slab(
            new Vector3(0.13f, 0.028f, 0.028f),
            new Vector3(Deck.DoorWidth - 0.12f, 1.02f, thickness / 2f + 0.03f),
            Fabric.Brass,
            "handle"));

        return door;
    }

    /// <param name="track">Where this leaf's own plane sits, either side of the wall's middle. See <see cref="Pass"/>.</param>
    private static Node Panel(float side, float track, Material plate, Material rib, Material strip)
    {
        var node = new Node { Name = side < 0f ? "leaf.left" : "leaf.right" };

        node.Children.Add(Fabric.Slab(
            new Vector3(Leaf, Deck.DoorHeight, Thickness),
            new Vector3(side * (Leaf / 2f - Lap / 2f), Deck.DoorHeight / 2f, track),
            plate,
            "leaf"));

        // Two ribs across each leaf, set back from the meeting edge. They cost four boxes and they are what
        // makes the two halves read as one door that has come apart rather than as two slabs that happen to
        // be adjacent, because the ribs line up across the joint.
        foreach (var y in new[] { 0.62f, 1.74f })
            node.Children.Add(Fabric.Slab(
                new Vector3(Leaf - 0.16f, 0.05f, Thickness + 0.012f),
                new Vector3(side * (Leaf / 2f + 0.04f), y, track),
                rib,
                "rib"));

        // The lit edge, on the face that meets the other leaf. It rides its own leaf's track, so the two
        // strips are as far apart in depth as the leaves are and never share a plane either.
        node.Children.Add(Fabric.Slab(
            new Vector3(0.022f, Deck.DoorHeight - 0.22f, Thickness + 0.016f),
            new Vector3(side * 0.024f, Deck.DoorHeight / 2f, track),
            strip,
            "edge"));

        return node;
    }
}
