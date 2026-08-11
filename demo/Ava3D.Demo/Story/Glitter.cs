using System.Numerics;

namespace Ava3D.Demo.Story;

/// <summary>
/// The light the ball throws, all over the room.
///
/// The first version of this was a ring of white patches parented to the ball, and it worked because the
/// alcove is a cylinder: on a wall that is a constant distance away, a thrown dot travels by <i>rotating</i>,
/// so one number a frame applied to one node moved all of them and nothing could come apart. That trick is
/// exact and it is also the reason the show stopped at the alcove's edge — the moment a dot leaves the
/// cylinder there is no rigid motion that keeps it on the room, because the room is a box and a box is not
/// a constant distance from anything.
///
/// So this does the arithmetic instead. The ball has sixty holes in it; each hole is a direction in the
/// ball's own frame; every frame each direction is turned by the ball's yaw and cast at the room, and the
/// patch is put where the ray lands, laid flat on whatever it landed on, and scaled by how far it went. Sixty
/// ray casts against a box and a cylinder is nothing — it is five compares and a divide each — and what it
/// buys is a room whose walls, floor, ceiling and apse are all in the show, with the dots crossing from one
/// to the next and growing as they go, because that is what a cone of light does.
///
/// <b>The pattern on the ceiling is not drawn, it is a consequence.</b> A hole at a fixed angle from the
/// ball's axis sweeps a cone, and a cone meeting a flat ceiling is a circle; sixty holes at sixty angles are
/// sixty concentric circles, which is exactly the flower every one of these things puts on a ceiling and none
/// of it is authored.
///
/// Everything here is unlit, additive and writes no depth, so the whole show costs no light at all — the
/// same property the alcove's original dots had and the same one chapter 7 turns the building off to prove.
/// It is still depth <i>tested</i>, so a patch behind the sofa is behind the sofa.
/// </summary>
internal sealed class Glitter
{
    /// <summary>
    /// How many holes the ball has.
    ///
    /// Sixty rather than a hundred, and the constraint is fill rate rather than nodes: these are
    /// additive quads, the browser build rasterises them on the CPU, and additive overdraw is the one cost
    /// in this film that scales with how much of the screen a thing covers rather than with how much of it
    /// there is.
    /// </summary>
    private const int Holes = 60;

    /// <summary>
    /// How fast the ball turns, in degrees a second.
    ///
    /// It belongs to the ball rather than to whoever is driving it, because two callers drive it — the
    /// chapter and the free walk — and a ball that turns at one rate in the film and another when the
    /// visitor walks back into the room is two balls.
    /// </summary>
    public const float Spin = 34f;

    /// <summary>
    /// How far inside the plan line a wall's inner face actually is.
    ///
    /// Every wall in this building is a slab <see cref="Deck.WallThickness"/> thick standing <i>centred</i>
    /// on the line the room is measured to, so the face somebody in the room can see is half a thickness
    /// nearer than the number the plan gives. Casting at the plan line put every patch on all four walls a
    /// hundred and twenty-five millimetres inside the plaster, where it was perfectly correct and completely
    /// invisible — the ceiling and the apse were the only surfaces the show reached, because a lid and a
    /// shell have no thickness to be buried in.
    /// </summary>
    private const float Facing = Deck.WallThickness / 2f;

    /// <summary>
    /// The half-angle of one hole's cone, as a tangent.
    ///
    /// It is what makes a patch on the near wall small and the same patch on the far wall large, and it is
    /// the only part of this that has to be a physical quantity rather than a look: a lens throws a cone,
    /// and a cone's footprint is its distance times this. At a metre and a half — the apse wall — it is a
    /// dot the size of a fist; across the room it is the size of a head.
    /// </summary>
    private const float Spread = 0.030f;

    /// <summary>Where the ball hangs, in the room's own coordinates. It stands on the apse's axis, which is
    /// what makes the apse the one surface here whose distance does not need solving for.</summary>
    private static readonly Vector3 Ball = ScreenRoom.ApseAt + new Vector3(0f, 2.35f, 0f);

    /// <summary>
    /// The colours, in the order the holes take them.
    ///
    /// Seven of them against sixty holes laid out on a golden-angle spiral, which is deliberate: neither the
    /// colours nor the turn between one hole and the next divides into the other, so no two neighbouring holes
    /// agree and the ball never grows a stripe.
    /// </summary>
    private static readonly Vector3[] Palette =
    [
        new(1f, 0.14f, 0.10f),
        new(0.16f, 1f, 0.30f),
        new(0.20f, 0.36f, 1f),
        new(1f, 0.66f, 0.12f),
        new(0.94f, 0.96f, 1f),
        new(1f, 0.18f, 0.74f),
        new(0.16f, 0.92f, 1f)
    ];

    private readonly Vector3[] _holes = new Vector3[Holes];
    private readonly Node[] _dots = new Node[Holes];
    private readonly Material[] _patch = new Material[Holes];
    private readonly Node[] _shafts = new Node[Holes];
    private readonly Material[] _shaft = new Material[Holes];
    private readonly Material?[] _lens = new Material?[Holes];

    /// <summary>
    /// Builds the show under <paramref name="root"/>, dark.
    ///
    /// The node it hangs off is the room, not the ball. That is the whole difference from the version this
    /// replaced: these patches are placed in the room's frame every frame, so the thing that turns is the
    /// arithmetic and not the node.
    /// </summary>
    public Glitter(Node root)
    {
        Root = new Node { Name = "glitter", IsVisible = false };

        var quad = Primitives.Plane();

        for (var i = 0; i < Holes; i++)
        {
            // A sunflower spiral round the ball: the golden angle between one hole and the next, so no two
            // neighbours line up and the pattern never repeats. No random number in it, because every frame
            // of this film has to be the same frame on every run.
            //
            // <b>The band matters more than the count.</b> Spread evenly over the whole sphere — which is
            // what this did first and is what a sphere of holes obviously wants — two thirds of the holes
            // point at the floor or the ceiling, because the ball hangs two and a third metres up in a room
            // three and a fifth high and those two surfaces are the nearest things to it by a long way.
            // Counted at a dozen different rotations, the room's four walls got five patches between them
            // and the apse and the ceiling got twenty-nine, which is exactly what it looked like: an alcove
            // putting on a show in a dark room. Thirty degrees up to fifty-five down triples what leaves the
            // alcove and still throws the flower on the ceiling, because the ceiling is close enough that
            // even a shallow ray reaches it. The bottom of the band is what puts light on the stage: the
            // apse's wall is two metres out, so anything shallower than about fifty degrees meets it before
            // it can reach the floor and the polished disc under the ball stays black.
            var lift = float.DegreesToRadians(30f - (i + 0.5f) / Holes * 85f);
            var turn = i * 2.399963f;
            var ring = MathF.Cos(lift);

            _holes[i] = new Vector3(MathF.Cos(turn) * ring, MathF.Sin(lift), MathF.Sin(turn) * ring);

            _patch[i] = Lit(Palette[i % Palette.Length]);
            _dots[i] = new MeshNode(quad, _patch[i]) { Name = "patch" };
            Root.Children.Add(_dots[i]);
        }

        // The shafts. One soft bar each, turned to face whoever is watching.
        //
        // <b>Crossed pairs were not physically correct and it showed.</b> Two bars at ninety degrees on the
        // same axis do cover every angle, but how much of the pair is presented to the eye depends on how
        // the pair happens to be rolled about its own axis — and the roll of a shaft pointing north-east is
        // not the roll of one pointing north-west. So two beams at the same angle to the same viewer came
        // out at different brightnesses, which is not something a beam of light does.
        //
        // What a shaft actually is, is a cylinder of glowing air, and a cylinder of glowing air looks the
        // same from every roll about its own axis. A single bar kept square to the eye is that: identical
        // from every direction that is the same angle off the beam, changing only with the angle, which is
        // the one thing that <i>should</i> change it. It is also half the drawing of the pair it replaced.
        //
        // <b>A bar rather than a cone, and it took building the cone to see why.</b> A solid cone is
        // geometrically the honest answer — what leaves a lens is the same cone that makes the patch at the
        // far end — and it renders as a tube. It has a silhouette, and a silhouette is the one thing a beam
        // of light in air does not have. These have the falloff map on them instead, so they are brightest
        // along their own middle and gone at their edges, which is what light in air looks like and is not
        // an approximation of anything: a shaft seen side-on <i>is</i> brightest where you are looking
        // through the most of it.
        //
        // What the cone was right about is the taper, and that is kept — see <see cref="Taper"/>.
        var bar = Taper();

        for (var i = 0; i < Holes; i++)
        {
            _shaft[i] = Lit(Palette[i % Palette.Length]);
            _shaft[i].BaseColorTexture = Finish.Shaft();
            _shafts[i] = new MeshNode(bar, _shaft[i]) { Name = "shaft" };

            Root.Children.Add(_shafts[i]);
        }

        root.Children.Add(Root);
    }

    /// <summary>Everything the show draws, on one node, so a room with the lights on can drop all of it in
    /// one assignment.</summary>
    public Node Root { get; }

    /// <summary>
    /// The lenses in the ball's own skin, hung off <paramref name="mirror"/> so they turn with it.
    ///
    /// They are not decoration. A room full of thrown light whose source is a plain chrome sphere is a room
    /// where the light has no cause — the eye looks for the colours <i>in</i> the thing and does not find
    /// them. Sixty coloured lenses in the ball, each one facing exactly the way its own dot went, is
    /// the cause, and it costs one quad per hole.
    /// </summary>
    public void Lenses(Node mirror)
    {
        var quad = Primitives.Plane();

        for (var i = 0; i < Holes; i++)
        {
            _lens[i] = Lit(Palette[i % Palette.Length]);

            mirror.Children.Add(new MeshNode(quad, _lens[i])
            {
                Name = "lens",
                Position = Ball - ScreenRoom.ApseAt + _holes[i] * 0.263f,
                Rotation = Turn(Vector3.UnitY, _holes[i]),
                Scale = new Vector3(0.075f, 1f, 0.075f)
            });
        }
    }

    /// <summary>
    /// The show at this instant: the ball turned to <paramref name="yaw"/>, at <paramref name="level"/>, with
    /// the dots answering the beat.
    ///
    /// The yaw is a parameter rather than a rate of its own because the ball is a node somebody else turns.
    /// One number, handed to both, is the only arrangement in which the lens a dot came out of and the dot
    /// itself cannot disagree — and they would disagree by a whole revolution within a minute if each of them
    /// worked the angle out from the clock at its own rate.
    /// </summary>
    /// <param name="yaw">Where the ball has turned to, in degrees, the same number it was rotated by.</param>
    /// <param name="level">0 for a dark room, 1 for the show at full.</param>
    /// <param name="beat">The music's phase, in beats. Whole numbers are downbeats.</param>
    /// <param name="eye">Where it is being watched from, in the room's own coordinates. The shafts are kept
    /// square to it — see the note in the constructor for why a beam has to be.</param>
    public void Update(float yaw, float level, float beat, Vector3 eye)
    {
        Root.IsVisible = level > 0.002f;

        if (!Root.IsVisible)
        {
            Off();
            return;
        }

        var turn = Quaternion.CreateFromAxisAngle(Vector3.UnitY, float.DegreesToRadians(yaw));

        for (var i = 0; i < Holes; i++)
        {
            var ray = Vector3.Transform(_holes[i], turn);
            var (range, face) = Land(ray);
            var wide = Math.Clamp(range * Spread, 0.06f, 0.44f);

            // Thirty-five millimetres off the surface, along that surface's own normal rather than back
            // along the ray. It is additive and writes no depth, but it is still depth tested, and a patch
            // laid exactly on a wall is the coplanar case the whole rest of this building is arranged to
            // avoid. The normal is what makes the number small enough to be invisible and large enough to
            // work: backed along a shallow ray, clearing two centimetres of floor would take a third of a
            // metre and would put the patch visibly off the wall it belongs to. It has to clear two
            // centimetres, because the apse's stage is a polished disc lying that far above the deck and
            // every patch the ball throws downward inside the alcove landed underneath it.
            _dots[i].Position = Ball + ray * range + face * 0.035f;
            _dots[i].Rotation = Turn(Vector3.UnitY, face);
            _dots[i].Scale = new Vector3(wide * 2f, 1f, wide * 2f);

            // The far wall is four times the distance of the apse, so a patch that keeps its brightness
            // over there is a patch four times the area at the same intensity — which is not what a lamp
            // does. Falling off with the distance is not exactly the inverse square either, and that is on
            // purpose: the square is right for the light and wrong for a screen, because the dot on the far
            // wall has to stay a dot rather than become a smudge.
            var reach = 1.35f / (0.9f + range * 0.20f);
            var value = level * reach * (0.42f + 0.58f * Beat(beat, i));

            Paint(_patch[i], Palette[i % Palette.Length] * value * 1.35f);
            // The lens breathes with the patch it threw, on the same beat term, because it is the same
            // light: a lens at a steady brightness over a dot that pulses is two things pretending to be
            // one. It keeps a floor the patch does not, so the ball reads as lit rather than as flashing.
            Paint(_lens[i], Palette[i % Palette.Length] * level * (0.55f + 0.45f * Beat(beat, i)) * 1.9f);

            var run = MathF.Max(0.05f, range - 0.26f);
            var middle = Ball + ray * (0.26f + run / 2f);

            _shafts[i].Position = middle;
            _shafts[i].Rotation = Square(ray, eye - middle);

            // Three fifths of the patch at the far end, and a fifth of that where it leaves the ball.
            // Narrower than what it lands on, because a beam has to arrive inside its own dot rather than
            // around it — the dot is the light and the shaft is only the part of it you are looking through
            // on the way.
            _shafts[i].Scale = new Vector3(wide * 0.6f, 1f, run);

            // A shaft is the light in the air on the way, so it is a fraction of what lands, and the
            // fraction is small. At anything above about a sixth this stops being a room with beams in it
            // and becomes a room full of coloured card.
            //
            // It is also washed towards its own grey, and that is not a fudge either. What is being drawn is
            // the light scattered out of the beam by whatever is in the air, and dust scatters every
            // wavelength — so what reaches the eye from the side of a beam is always paler than the beam,
            // while what lands on the wall keeps the full colour because it never left the cone. A shaft at
            // its patch's saturation reads as solid coloured glass.
            Paint(_shaft[i], Wash(Palette[i % Palette.Length], 0.34f) * value * 0.17f);
        }
    }

    /// <summary>
    /// The show off, which three chapters and the free walk all need to be able to say.
    ///
    /// Zeroing the materials as well as hiding the node is not belt and braces. These are additive and unlit,
    /// so a colour left in one of them is a colour in every frame that can see it — and this room stands open
    /// through its doorway for most of the chapter before the one that drives it.
    /// </summary>
    public void Off()
    {
        Root.IsVisible = false;

        foreach (var material in _patch) Paint(material, Vector3.Zero);
        foreach (var material in _shaft) Paint(material, Vector3.Zero);
        foreach (var material in _lens) Paint(material, Vector3.Zero);
    }

    /// <summary>
    /// Where a ray out of the ball meets the room, and which way that surface faces.
    ///
    /// The room is a box with a half-round apse let into one end, and the ball hangs on the apse's axis —
    /// so the two halves of this are not the same problem and are not solved the same way. Northward, the
    /// wall is a constant distance from the axis and the answer is one divide. Southward it is an ordinary
    /// slab test against five planes, and the north wall is not one of them because the ray starts on it.
    /// </summary>
    private static (float Range, Vector3 Face) Land(Vector3 ray)
    {
        if (ray.Z > 0f)
        {
            var flat = MathF.Sqrt(ray.X * ray.X + ray.Z * ray.Z);
            var best = flat > 1e-4f ? ScreenRoom.ApseRadius / flat : float.MaxValue;
            var face = new Vector3(-ray.X, 0f, -ray.Z) / MathF.Max(flat, 1e-4f);

            Nearer(ray.Y, Deck.Ceiling - Ball.Y, -Vector3.UnitY, ref best, ref face);
            Nearer(-ray.Y, Ball.Y, Vector3.UnitY, ref best, ref face);

            return (best, face);
        }

        var range = float.MaxValue;
        var normal = -Vector3.UnitZ;

        Nearer(ray.X, ScreenRoom.Width / 2f - Facing - Ball.X, -Vector3.UnitX, ref range, ref normal);
        Nearer(-ray.X, ScreenRoom.Width / 2f - Facing + Ball.X, Vector3.UnitX, ref range, ref normal);
        Nearer(-ray.Z, ScreenRoom.Depth / 2f - Facing + Ball.Z, Vector3.UnitZ, ref range, ref normal);
        Nearer(ray.Y, Deck.Ceiling - Ball.Y, -Vector3.UnitY, ref range, ref normal);
        Nearer(-ray.Y, Ball.Y, Vector3.UnitY, ref range, ref normal);

        return (range == float.MaxValue ? 4f : range, normal);
    }

    /// <summary>One plane of the slab test: keep it if the ray reaches it sooner than whatever is held.</summary>
    private static void Nearer(float toward, float away, Vector3 face, ref float best, ref Vector3 held)
    {
        if (toward <= 1e-4f) return;

        var range = away / toward;

        if (range <= 0f || range >= best) return;

        best = range;
        held = face;
    }

    /// <summary>
    /// One shaft's bar: a metre long along its own +Z, a fifth as wide where it starts as where it ends.
    ///
    /// It is four vertices because it has to be a trapezium and nothing in <see cref="Primitives"/> makes
    /// one. A cone is what a beam of light actually is, and a scaled rectangle is what it is cheapest to
    /// draw; this is the third thing, which is the rectangle's cost with the cone's shape. The width at each
    /// end is a fraction rather than a measurement, so the caller scales the whole bar by the size of the
    /// patch it lands in and the taper comes out right at any range.
    ///
    /// The texture coordinates run corner to corner, which puts <see cref="Finish.Pool"/>'s bright middle
    /// down the bar's own centre line and takes it to nothing at all four edges. That is the whole of why
    /// this reads as light rather than as a painted stripe.
    /// </summary>
    private static Mesh Taper()
    {
        const float lens = 0.2f;
        const float land = 1f;

        return new Mesh
        {
            Positions =
            [
                new Vector3(-lens, 0f, -0.5f), new Vector3(lens, 0f, -0.5f),
                new Vector3(-land, 0f, 0.5f), new Vector3(land, 0f, 0.5f)
            ],
            Normals = [Vector3.UnitY, Vector3.UnitY, Vector3.UnitY, Vector3.UnitY],
            TexCoords =
            [
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 1f), new Vector2(1f, 1f)
            ],
            Indices = [0, 1, 2, 1, 3, 2],
            Name = "shaft"
        };
    }

    /// <summary>
    /// The turn that lays a bar along <paramref name="ray"/> and rolls it square to <paramref name="toward"/>.
    ///
    /// Three axes written straight into a matrix, because that is what this is: the bar runs along its own
    /// +Z, is a bar across its own X, and faces along its own Y — so the frame is <c>across</c>, <c>facing</c>,
    /// <c>ray</c>, in that order, and <see cref="Quaternion.CreateFromRotationMatrix"/> turns it into the one
    /// number a node wants. The degenerate case is being looked at down the beam's own axis, where the cross
    /// product vanishes and every roll is as good as any other, which is exactly when it does not matter.
    /// </summary>
    private static Quaternion Square(Vector3 ray, Vector3 toward)
    {
        var across = Vector3.Cross(ray, toward);

        if (across.LengthSquared() < 1e-8f)
            across = Vector3.Cross(ray, Vector3.UnitY);

        if (across.LengthSquared() < 1e-8f)
            across = Vector3.Cross(ray, Vector3.UnitX);

        across = Vector3.Normalize(across);

        // Ray crossed into across, and not the other way round. The other way round is the same plane and
        // the opposite hand, and a rotation matrix built out of a left-handed frame is not a rotation —
        // CreateFromRotationMatrix does not reject it, it returns a quaternion that scatters the bars all
        // over the room in directions none of them point in.
        var facing = Vector3.Cross(ray, across);

        return Quaternion.CreateFromRotationMatrix(new Matrix4x4(
            across.X, across.Y, across.Z, 0f,
            facing.X, facing.Y, facing.Z, 0f,
            ray.X, ray.Y, ray.Z, 0f,
            0f, 0f, 0f, 1f));
    }

    /// <summary>
    /// The shortest rotation taking one unit vector to another.
    ///
    /// The half-way quaternion, which is the form that has no trigonometry in it and no branch except the
    /// one case it cannot express: a vector and its exact opposite, where every axis perpendicular to both
    /// is as good as any other and the formula divides by nothing.
    /// </summary>
    private static Quaternion Turn(Vector3 from, Vector3 to)
    {
        var along = Math.Clamp(Vector3.Dot(from, to), -1f, 1f);

        if (along > 0.999999f) return Quaternion.Identity;

        if (along < -0.999999f)
        {
            var across = Vector3.Cross(from, Vector3.UnitX);

            if (across.LengthSquared() < 1e-6f) across = Vector3.Cross(from, Vector3.UnitZ);

            return Quaternion.CreateFromAxisAngle(Vector3.Normalize(across), MathF.PI);
        }

        return Quaternion.Normalize(new Quaternion(Vector3.Cross(from, to), 1f + along));
    }

    /// <summary>One hole's answer to the beat: on sharply, off slowly, and a different quarter of the bar
    /// for every fourth hole so the ball breathes instead of blinking.</summary>
    private static float Beat(float beat, int index)
    {
        var phase = beat + index % 4 * 0.25f;
        var into = phase - MathF.Floor(phase);

        return (1f - into) * (1f - into);
    }

    /// <summary>A colour moved <paramref name="towards"/> of the way to its own grey, keeping the brightness
    /// it had. What scattering out of a beam does to it.</summary>
    private static Vector3 Wash(Vector3 colour, float towards)
    {
        var grey = colour.X * 0.30f + colour.Y * 0.59f + colour.Z * 0.11f;

        return Vector3.Lerp(colour, new Vector3(grey), towards);
    }

    /// <summary>Sets a colour on both slots an unlit additive surface reads. It reads base colour; the
    /// emissive is there so the same material is right if it is ever put in a lit pass.</summary>
    private static void Paint(Material? material, Vector3 colour)
    {
        if (material is null) return;

        material.BaseColor = new Vector4(colour, 1f);
        material.EmissiveColor = colour;
    }

    /// <summary>A surface that is light rather than a surface: unlit, additive, no depth write, and carrying
    /// the falloff that stops an additive quad having a straight edge. See <see cref="Finish.Pool"/>.</summary>
    private static Material Lit(Vector3 colour, bool soft = true) => new()
    {
        BaseColor = new Vector4(colour, 1f),
        EmissiveColor = colour,
        BaseColorTexture = soft ? Finish.Pool() : null,
        Unlit = true,
        Blend = BlendMode.Additive,
        DepthWrite = false,
        Name = "glitter"
    };
}
