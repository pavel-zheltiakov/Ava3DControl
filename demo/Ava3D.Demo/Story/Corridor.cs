using System.Numerics;

namespace Ava3D.Demo.Story;

/// <summary>
/// The alarm corridor: twenty-one metres of ship with seven rotating beacons down it and a shut powered
/// door at the end.
///
/// It is the first room in the building that is not an exhibition space, and the first one he goes into
/// because something told him to rather than because it was next. Four rooms and a lounge have been laid
/// out so that the next thing is always round a corner; this one is a straight run with the next thing
/// visible from the first step of it, and that inversion is the point — a corridor that hides its own end
/// is a corridor with a door in the middle, and what this one is for is that you can see exactly where it
/// goes and it is a long way away.
///
/// <b>It inherits the colonnade's argument</b>, which was four light slots spread down twenty-eight metres.
/// Seven beacons and four lights, and the four are handed from beacon to beacon as he walks — see
/// <see cref="Alarm"/>, where two things are worth reading: the hand-over needs no fade at all, and the
/// lamp does not live in the fitting. It lives where the beam lands, which is the whole difference between
/// a corridor that gets brighter and dimmer and a corridor with a patch of light dragging down it.
///
/// Everything red in here that is not one of those four lights costs nothing. The lenses are unlit, the
/// runway strips down the skirting are unlit and additive, and the beams the beacons throw are unlit and
/// additive — so the corridor is fully dressed and running while every light slot in the building is still
/// spending itself on a mirror ball in the room next door. That is the same claim the lounge makes with
/// four televisions, made again one room later with something at stake.
/// </summary>
internal sealed class Corridor
{
    /// <summary>Clear width, floor to ceiling, and how far it runs from the lounge's wall to the bulkhead.</summary>
    public const float Width = 2f;

    public const float Height = 2.7f;
    public const float Length = 21f;

    /// <summary>
    /// How far the corridor's shell is buried in the lounge's north wall.
    ///
    /// It is not a fudge factor, it is the fix for a specific artefact. The corridor's shell ends where the
    /// lounge's wall begins, so a shell that stopped exactly at the boundary would present its end caps in
    /// the same plane as that wall's outer face — two coplanar surfaces, both drawn, and the depth test with
    /// no way to choose between them. Interpenetrating boxes cannot z-fight; touching ones can. Twenty
    /// centimetres into a wall a quarter of a metre thick puts every one of those caps inside solid
    /// geometry, where it is invisible for the honest reason.
    ///
    /// The floor is the exception and is the other half of the same problem. It cannot be pushed under the
    /// lounge, because two coplanar <i>planes</i> at the same height would fight the same way — so it stops
    /// exactly where the lounge's floor stops, edge to edge, and the seam falls under the door frame.
    /// </summary>
    private const float Buried = 0.2f;

    private const float Thickness = 0.25f;

    /// <summary>Where the beacons are down the run, and which side of it each is on. Alternating, so the
    /// flash reads as a zigzag going away from him rather than as a strobe going off in his face.</summary>
    private static readonly float[] Stations = [2.4f, 5.2f, 8f, 10.8f, 13.6f, 16.4f, 19.4f];

    /// <summary>How high the lenses are. Above head height, which is where a thing that is warning you
    /// about the room rather than lighting it belongs.</summary>
    private const float LensY = 2.25f;

    /// <summary>How far a lens sits off the centre line — just clear of the wall face.</summary>
    private const float LensX = 0.86f;

    /// <summary>Degrees a second. About a turn and a half a second, which is what these actually do.</summary>
    private const float Sweep = 232f;

    /// <summary>
    /// How much each beacon lags the one before it.
    ///
    /// Negative, so beacon <i>n</i> reaches any given bearing after beacon <i>n−1</i> does and the flash
    /// travels up the corridor away from him. Nothing in the film says to follow them; a light that keeps
    /// leaving in the same direction is an instruction that does not need a caption.
    /// </summary>
    private const float Lag = -34f;

    /// <summary>
    /// How far below level the mirror throws, in degrees.
    ///
    /// It is about where the camera is. These are at two and a quarter and the visitor's eye is at one and
    /// seven, so a beam sweeping level goes through his face twice a second — which is not dramatic, it is
    /// a pink bar wiping the screen. Sixteen degrees of nose-down puts the patch on the far wall at about
    /// eye height and on the near wall just over head height, which is where a beacon aims anyway.
    /// </summary>
    private const float Tilt = 16f;

    /// <summary>How many segments the runway is cut into, per side.</summary>
    private const int Segments = 14;

    /// <summary>The blade's own direction inside the fitting, before the fitting turns. Constant, because
    /// the only thing that moves is the node it hangs off.</summary>
    private static readonly Vector3 Along = new(
        MathF.Cos(Tilt * MathF.PI / 180f), -MathF.Sin(Tilt * MathF.PI / 180f), 0f);

    private readonly Node[] _spin = new Node[Stations.Length];
    private readonly Node[] _blade = new Node[Stations.Length];
    private readonly Material[] _lens = new Material[Stations.Length];
    private readonly Material[] _beam = new Material[Stations.Length];
    private readonly Material[] _runway = new Material[Segments];

    /// <summary>Where each beacon is pointing and how far it gets, worked out once a frame in
    /// <see cref="Glow"/> and read again in <see cref="Alarm"/>.</summary>
    private readonly Vector3[] _bearing = new Vector3[Stations.Length];

    private readonly float[] _reach = new float[Stations.Length];

    public Corridor(Hall hall)
    {
        var root = hall.Add(Deck.CorridorRoom, Deck.Corridor);

        // Off the top of the Grade ladder and out the other side. The lounge was the last room whose
        // surfaces were making an argument about surfaces; this is the same riveted plate three shades
        // darker, because a corridor is what the plate was always for and the lounge was the room that had
        // it painted.
        var plate = Finish.Panelling();
        plate.BaseColor = new Vector4(0.19f, 0.20f, 0.23f, 1f);
        plate.Roughness = 0.58f;
        plate.Name = "corridor.plate";

        var deck = Finish.Floor(Grade.Dressed);
        deck.BaseColor = new Vector4(0.17f, 0.17f, 0.19f, 1f);
        deck.Metallic = 0.25f;
        deck.Roughness = 0.52f;
        deck.Name = "corridor.deck";

        var half = Width / 2f;

        // The floor runs from where the lounge's floor stops, so the two meet on a line rather than
        // overlapping — see Buried. Everything else starts two hundred millimetres further back, inside the
        // wall.
        var floorFrom = -Thickness;
        var floorDepth = Length - floorFrom;

        root.Children.Add(new MeshNode(
            Fabric.Map(Primitives.Plane(Width, floorDepth), deck, new Vector3(0f, 0f, (floorFrom + Length) / 2f)),
            deck)
        {
            Position = new Vector3(0f, 0f, (floorFrom + Length) / 2f),
            Name = "floor"
        });

        var shellFrom = -Buried;
        var shellDepth = Length - shellFrom;
        var shellAt = (shellFrom + Length) / 2f;

        root.Children.Add(new MeshNode(
            Fabric.Map(Primitives.Plane(Width, shellDepth), plate, new Vector3(0f, Height, shellAt)),
            plate)
        {
            Position = new Vector3(0f, Height, shellAt),
            RotationDegrees = new Vector3(180f, 0f, 0f),
            Name = "ceiling"
        });

        foreach (var side in new[] { -1f, 1f })
            root.Children.Add(Fabric.Slab(
                new Vector3(Thickness, Height, shellDepth),
                new Vector3(side * (half + Thickness / 2f), Height / 2f, shellAt),
                plate,
                "wall"));

        // The bulkhead at the end, and the door in it. Shut, and shut for the whole chapter: this is the
        // one door in the building the visitor walks up to and does not go through, because what is behind
        // it is chapter 6 and chapter 6 is not built. It opens there and nowhere else.
        // Fifty millimetres wider and thirty taller than the standard opening, because this wall stands
        // entirely inside the engine room's six-hundred-millimetre bulkhead — see Deck.Engine — and the
        // buried wall is the one that takes the bigger hole. Two walls cutting the same hole put two
        // reveals at one depth, which crawls; the visible reveal here is the bulkhead's.
        var end = Fabric.PiercedWall(
            Width + 2f * Thickness, Height, Thickness,
            doorCentre: 0f, Deck.DoorWidth + 0.05f, Deck.DoorHeight + 0.03f, plate);

        end.Position = new Vector3(0f, 0f, Length + Thickness / 2f);
        root.Children.Add(end);

        Gate = Door.Powered(new Vector3(0f, 0f, Length + Thickness / 2f));
        Gate.Open(0f);
        root.Children.Add(Gate.Root);

        Frames(root);
        Runway(root);

        for (var i = 0; i < Stations.Length; i++)
            root.Children.Add(Beacon(i));

        // Four lights for seven beacons, which is the whole demonstration. They are built here and never
        // rebuilt: the chapter hands them to Hall.Use once and then moves them, so no frame in this corridor
        // ever rebuilds the light list.
        Lights = new PointLight[4];

        for (var i = 0; i < Lights.Length; i++)
            Lights[i] = new PointLight
            {
                Position = Deck.Corridor,
                Color = new Vector3(1f, 0.16f, 0.10f),
                Intensity = 0f,

                // Four and a half metres, which is short, and short is the point twice over.
                //
                // It is what keeps the corridor dark. A patch of light on a wall is only a patch if the
                // wall either side of it is not lit, and at six and a half these four reached everything —
                // twenty-one metres of steel came out uniformly salmon and the beams had nothing to be
                // brighter than. Cutting the range is what put the contrast back, and contrast is the only
                // thing a red corridor has.
                //
                // It is also what makes the hand-over invisible. The four are reassigned to whichever four
                // beacons are nearest him, and a reassignment moves a lamp several metres in one frame. At
                // the moment of a swap the fitting being dropped is about five and a half metres off, which
                // is past the range entirely: the light being taken away was contributing nothing, so there
                // is nothing to fade.
                Range = 4.5f,
                Decay = 2f
            };
    }

    /// <summary>The bulkhead door at the far end. Shut in chapter 5; chapter 6 is what opens it.</summary>
    public Door Gate { get; }

    /// <summary>The four slots this corridor spends, in the order the chapter hands them over.</summary>
    public PointLight[] Lights { get; }

    /// <summary>A point on the centre line at eye height, <paramref name="metres"/> up the corridor.</summary>
    public static Vector3 Ahead(float metres) =>
        Deck.Corridor + new Vector3(0f, Deck.Eye, metres);

    /// <summary>The lens of beacon <paramref name="index"/>, in world coordinates.</summary>
    public static Vector3 Lens(int index) =>
        Deck.Corridor + new Vector3(Side(index) * LensX, LensY, Stations[index]);

    /// <summary>
    /// What a head does about a beacon, which is not the same thing as looking straight at one.
    ///
    /// <see cref="Lens"/> is where the fitting is: eighty-six centimetres off the centre line and
    /// fifty-five above an eye. A waypoint a metre and a fifth short of one and aimed at it is therefore
    /// thirty-six degrees off axis and twenty-five degrees up — and the beacons alternate sides, so a walk
    /// written that way swings seventy degrees left, seventy right, five times in eleven seconds while the
    /// man doing it is meant to be hurrying somewhere. That was reported as the camera moving too much, and
    /// it was: nobody walking down a corridor turns their head to face each light in it. They notice it,
    /// which is a glance, and a glance is a fraction of the way there.
    ///
    /// So the aim leans toward the beacon rather than arriving at it. <see cref="Lean"/> of the offset and
    /// <see cref="Lean"/> of the rise, which keeps everything the shot was for — the aim still moves up the
    /// corridor with the beacons, still changes shoulder at each one, and still puts the flash he is
    /// walking into off to one side — at sixteen degrees and eleven rather than thirty-six and twenty-five.
    /// The head goes left, right, left. It does not go round.
    /// </summary>
    public static Vector3 Toward(int index) =>
        Deck.Corridor + new Vector3(
            Side(index) * LensX * Lean, Deck.Eye + (LensY - Deck.Eye) * Lean, Stations[index]);

    /// <summary>How much of the way to a beacon a glance at it goes. See <see cref="Toward"/>.</summary>
    private const float Lean = 0.45f;

    /// <summary>The middle of the door at the end, for the last third of the walk to aim at.</summary>
    public static Vector3 Gateway =>
        Deck.Corridor + new Vector3(0f, 1.5f, Length + Thickness / 2f);

    /// <summary>
    /// The corridor running, with the four slots in its hands: seven beacons turning and four lights
    /// following him down it.
    ///
    /// <b>The lamp is not at the beacon. It is where the beam lands.</b> That is the whole of what makes
    /// this read as a rotating light rather than as a room that gets brighter and dimmer, and the first
    /// version got it wrong in an instructive way: it put a point light at each fitting and modulated its
    /// brightness by how much the mirror was pointing at the camera. Which is a defensible approximation
    /// and looks nothing like a beacon, because what a beacon does to a corridor is drag a bright patch
    /// along the wall. Every wall was lit at once and the only thing that changed was how much.
    ///
    /// So the beam is cast — it is a ray against a box, six planes and a minimum, no library feature
    /// needed — and the lamp is parked a hand's breadth off the surface it hits. The patch it makes then
    /// travels along the wall, round the corner onto the ceiling, down the far side and across the floor,
    /// because that is what the impact point of a rotating ray does. Nothing about the light is animated;
    /// it is animated by being somewhere.
    ///
    /// The assignment of the four slots to seven beacons is nearest-four and is recomputed every frame,
    /// which sounds like it needs smoothing and does not. A light leaves the set when it becomes the fifth
    /// nearest, at about five and a half metres, and at five and a half metres a lamp with a range of six
    /// and a half is worth about a fifth of a percent of what it is worth at one. The inverse square has
    /// already done the fade; a fade on top of it would be fading something that is not there.
    /// </summary>
    /// <param name="eye">Where the visitor is. The chapter has this, because it owns the walk.</param>
    /// <param name="clock">Film seconds. Not chapter seconds — see the call sites.</param>
    /// <param name="level">Zero to one, for bringing the whole thing up.</param>
    public void Alarm(Vector3 eye, float clock, float level)
    {
        Glow(clock, level);

        // Nearest four, by distance to the fitting rather than to the patch it is throwing. The fitting
        // does not move, so the set changes only as he walks — picking by the patch would reshuffle the
        // four slots several times a second as the beams swept, which is a light list being rebuilt for no
        // reason anybody could see. A selection sort over seven, which is the right algorithm at this size.
        Span<int> nearest = stackalloc int[Lights.Length];
        Span<bool> taken = stackalloc bool[Stations.Length];

        for (var slot = 0; slot < Lights.Length; slot++)
        {
            var best = -1;
            var closest = float.MaxValue;

            for (var i = 0; i < Stations.Length; i++)
            {
                if (taken[i])
                    continue;

                var d = Vector3.DistanceSquared(Lens(i), eye);

                if (d >= closest)
                    continue;

                closest = d;
                best = i;
            }

            taken[best] = true;
            nearest[slot] = best;
        }

        for (var slot = 0; slot < Lights.Length; slot++)
        {
            var i = nearest[slot];

            // Off the surface by a fifth of the throw, up to a quarter of a metre. It has to stand off it:
            // a point light exactly on a wall lights the two triangles it is sitting on to white and
            // nothing else, because 1/d² with d at zero is not a bright patch, it is a hole. And the stand
            // off has to shrink when the beam is short, or a beacon pointing at the wall thirty centimetres
            // from it would put its lamp on the wrong side of its own housing.
            var throwLength = _reach[i];
            var bearing = _bearing[i];

            Lights[slot].Position = Lens(i) + bearing * (throwLength - MathF.Min(0.25f, throwLength * 0.2f));

            // Two and three fifths, and it was three and a half first. A red lamp reads as dim on a swatch
            // and it does not read as dim on a wall a metre away: at three and a half the patches clipped,
            // and what a clipped red patch looks like after tone mapping is a white one. The whole corridor
            // turned peach. Everything red in this room is tuned against the brightest thing it does rather
            // than against the dimmest.
            //
            // No pulse and no facing term any more. The beam is somewhere, and where it is decides what is
            // lit — which is the same arithmetic, done by the geometry instead of by a dot product.
            Lights[slot].Intensity = level * 2.6f;
        }
    }

    /// <summary>
    /// Everything the corridor does that costs no light slot: the lenses, the beams and the runway.
    ///
    /// It is a separate method because chapter 4 calls it. The alarm starts while he is still in the lounge
    /// with every slot in the building spent on the mirror ball, and it starts <i>visibly</i> — a corridor
    /// full of turning red seen through a doorway, drawn by nothing but emission. Two rooms both doing
    /// something is otherwise two rooms both wanting lamps, and the film gives the lamps to the room he is
    /// standing in — which is why the lounge spent eighty seconds arguing that an unlit material is free.
    /// A bigger light budget would not change this. Emission is not the cheap way to do it, it is the
    /// right way: these lenses are not lit surfaces, they are surfaces that are added to the frame.
    /// </summary>
    public void Glow(float clock, float level)
    {
        level = Math.Clamp(level, 0f, 1f);

        for (var i = 0; i < Stations.Length; i++)
        {
            _spin[i].RotationDegrees = new Vector3(0f, -(clock * Sweep + i * Lag), 0f);

            // Where this beacon is pointing and how far it gets before it hits something. Both are wanted
            // twice — here, to cut the shaft to length, and in Alarm, to park the lamp at the far end of it
            // — so they are worked out once and kept.
            _bearing[i] = Bearing(clock, i);
            _reach[i] = Cast(Lens(i) - Deck.Corridor, _bearing[i]);

            // The shaft, stretched to reach whatever it is pointing at and then a bit further.
            //
            // The overrun is the point. A beam that stops exactly at the wall leaves a hairline of
            // background between the two — floating point does not agree with itself about where a surface
            // is from two different directions — and what that looks like is a beam that stopped short of
            // the thing it is lighting. Fifteen centimetres past the impact buries the tip inside the
            // wall, where the depth test cuts it off exactly at the surface for free, and there is nothing
            // left to see a gap in.
            //
            // The mesh is a unit cone about its own axis, so Y is its length and X and Z are its spread.
            // Scaling all three together is what keeps the spread an angle rather than a width: scale only
            // the length and a long throw comes out as a needle and a short one as a stub, both with the
            // same girth, which is not how light leaves anything.
            //
            // The spread stops growing after two metres and a bit, and that is a lie told for one frame in
            // particular. When a beam happens to point along the corridor it does not hit anything for
            // seven or eight metres, and a cone with a fixed angle that long is half a metre across at the
            // far end — most of which is passing within arm's reach of the camera on its way there. A beam
            // that keeps its width past the point where anyone is standing next to it is the cheaper wrong
            // answer than a beam that fills a quarter of the frame.
            var length = _reach[i] + 0.15f;
            var spread = MathF.Min(length, 2.2f);

            _blade[i].Scale = new Vector3(spread, length, spread);
            _blade[i].Position = Along * (length / 2f);

            // The lens does not pulse. A rotating beacon's dome is lit the whole time it is running — what
            // turns is the mirror inside it, and a dome that flashes is a strobe, which is a different piece
            // of equipment making a different promise.
            //
            // Neither of these goes anywhere near one, and that is the tone mapping rather than taste. An
            // emissive surface at full red is (1, 0.13, 0.08) written twice — base colour and emission —
            // and a highlight that far over range comes back down the curve desaturated, which is to say
            // white. A red alarm that renders white is not a bright red alarm, it is a different colour.
            //
            // The shaft is the dimmest thing in the room by a long way, and that is the third attempt. Air
            // is not a surface. What is being drawn here is a cone standing in for something that has no
            // geometry at all, and the moment it is bright enough to have an edge it stops being light and
            // becomes a pink pipe — which is exactly what the first two versions looked like. Almost all of
            // the sweep is done by the patch of wall at the far end of it, which is a real lamp lighting a
            // real surface; the shaft is a hint that says where the patch came from.
            Paint(_lens[i], new Vector3(1f, 0.13f, 0.08f), level * 0.55f);
            Paint(_beam[i], new Vector3(1f, 0.22f, 0.12f), level * 0.0075f);
        }

        // The runway: a band of brightness running toward the door, once a second and a half. Same
        // instruction as the beacons' lag, said by the floor, and free for the same reason.
        for (var i = 0; i < _runway.Length; i++)
        {
            var phase = Fraction(clock * 0.65f - i / (float)_runway.Length);
            var value = MathF.Pow(1f - phase, 5f);

            Paint(_runway[i], new Vector3(1f, 0.18f, 0.10f), level * (0.12f + 0.88f * value));
        }
    }

    /// <summary>Which side of the centre line beacon <paramref name="index"/> is on.</summary>
    private static float Side(int index) => index % 2 == 0 ? -1f : 1f;

    /// <summary>
    /// How far a ray gets from inside the corridor before it hits the shell: six planes and a minimum.
    ///
    /// A corridor is a box, and the distance from a point inside a box to its surface along a direction is
    /// the smallest positive intersection with the three pairs of planes. That is all a ray cast is when
    /// the thing being cast against is not a mesh, and it is worth saying plainly because the alternative
    /// — asking a scene graph to intersect something — would be a general facility this needs no part of.
    ///
    /// It ignores the door at the far end, which is right: the door is shut for the whole chapter and is
    /// flush enough with the bulkhead that a beam landing on it lands where the bulkhead is anyway.
    /// </summary>
    /// <param name="from">Where the ray starts, in the corridor's own coordinates.</param>
    /// <param name="along">Unit direction.</param>
    private static float Cast(Vector3 from, Vector3 along)
    {
        var d = Plane(from.X, along.X, -Width / 2f, Width / 2f);
        d = MathF.Min(d, Plane(from.Y, along.Y, 0f, Height));
        d = MathF.Min(d, Plane(from.Z, along.Z, 0f, Length));

        return d;
    }

    /// <summary>One pair of parallel planes, or infinity if the ray runs between them.</summary>
    private static float Plane(float from, float along, float low, float high)
    {
        if (MathF.Abs(along) < 1e-4f)
            return float.MaxValue;

        var t = ((along > 0f ? high : low) - from) / along;

        return t > 0f ? t : float.MaxValue;
    }

    /// <summary>
    /// Painted steel, for the ribs, the brackets and the conduit.
    ///
    /// Not <see cref="Fabric.DarkMetal"/>, which is what these were first. A metal at nine tenths metallic
    /// has almost no diffuse term, and a metal with no environment behind it has nothing to reflect — so
    /// under four point lights it is black except where a highlight happens to land, and eight ribs down a
    /// corridor came out as eight black bars across the picture. They are painted, and paint is a
    /// dielectric: a quarter metallic and a base colour that is allowed to be lit.
    /// </summary>
    private static Material Steel() => new()
    {
        BaseColor = new Vector4(0.17f, 0.18f, 0.20f, 1f),
        Metallic = 0.25f,
        Roughness = 0.55f,
        Name = "steel"
    };

    /// <summary>
    /// Where beacon <paramref name="index"/>'s mirror is pointing at <paramref name="clock"/>, as a unit
    /// vector in the corridor's coordinates.
    ///
    /// It has to agree exactly with what the node tree does to the blade, and the two are written
    /// separately — the blade is a child of a node that yaws, this is trigonometry — so it is worth
    /// showing the working. The fitting yaws by −a, and a yaw of φ sends a local +X to (cos φ, 0, −sin φ).
    /// The blade's own direction inside the fitting is <see cref="Along"/>, which is +X tilted down. Put
    /// the two together with φ = −a and the z term comes out positive, which is the sign that would
    /// otherwise be found by watching a beam sweep the wrong way.
    /// </summary>
    private static Vector3 Bearing(float clock, int index)
    {
        var a = (clock * Sweep + index * Lag) * MathF.PI / 180f;

        return new Vector3(Along.X * MathF.Cos(a), Along.Y, Along.X * MathF.Sin(a));
    }

    /// <summary>
    /// The alarm is over: the mirrors stop and the shafts go away.
    ///
    /// <b>It hides them rather than dimming them, and the difference is what this cost to find.</b>
    /// <see cref="Glow"/> at level nought paints every one of these materials black, and black added to a
    /// frame is nothing — so a shaft ought to disappear on its own and for a long time it looked as if it
    /// did. What it actually does is disappear <i>on the next frame the scene is invalidated</i>, and the
    /// hand-over to the free walk is the one moment in this demo where nothing invalidates: the chapters
    /// all end their <c>Update</c> with a call and there is no chapter left. A material written but not
    /// uploaded is a material that still has whatever it was built with in it, which here is full alarm
    /// red — so somebody with the keys walked into a quiet white corridor with a frozen beam across it.
    ///
    /// <see cref="Rounds.Open"/> invalidates now, which is the general fix and the one that matters. This
    /// is the specific one, and it is worth having anyway: a beacon that is not running has no shaft, and
    /// hiding the node says so in a way that cannot be undone by a material somebody forgot to repaint.
    /// </summary>
    public void Rest()
    {
        for (var i = 0; i < _spin.Length; i++)
            _spin[i].IsVisible = false;
    }

    private static void Paint(Material material, Vector3 colour, float value)
    {
        var lit = colour * Math.Clamp(value, 0f, 1f);

        material.EmissiveColor = lit;
        material.BaseColor = new Vector4(lit, 1f);
    }

    private static float Fraction(float t) => t - MathF.Floor(t);

    /// <summary>
    /// One beacon: a bracket, a dark cap, a red lens, and two blades of light turning inside it.
    ///
    /// The blades are the whole illusion and they are a lie told properly. Light in air is volume, and this
    /// control draws surfaces — so a shaft of light is a thin additive box that writes no depth, sticking
    /// out of the lens and turning with it. From any angle in a dark corridor that reads as a beam, because
    /// what the eye is looking for is a bright wedge that sweeps, and a bright wedge that sweeps is exactly
    /// what it is.
    /// </summary>
    private Node Beacon(int index)
    {
        var side = Side(index);
        var at = new Vector3(side * LensX, LensY, Stations[index]);

        var root = new Node { Name = "beacon", Position = at };

        // The bracket back to the wall, and the cap over the lens. Both dark, so the only thing on this
        // fitting that is ever bright is the part that is meant to be.
        root.Children.Add(Fabric.Slab(
            new Vector3(0.16f, 0.10f, 0.10f),
            new Vector3(side * 0.10f, 0.02f, 0f),
            Steel(),
            "bracket",
            Finish.Close));

        root.Children.Add(new MeshNode(Primitives.Cylinder(0.11f, 0.11f, 0.035f, 16), Steel())
        {
            Position = new Vector3(0f, 0.095f, 0f),
            Name = "cap"
        });

        root.Children.Add(new MeshNode(Primitives.Cylinder(0.10f, 0.10f, 0.02f, 16), Steel())
        {
            Position = new Vector3(0f, -0.085f, 0f),
            Name = "base"
        });

        _lens[index] = Fabric.Emissive(1f, 0.13f, 0.08f);

        root.Children.Add(new MeshNode(Primitives.Cylinder(0.095f, 0.095f, 0.15f, 16), _lens[index])
        {
            Position = Vector3.Zero,
            Name = "lens"
        });

        _beam[index] = new Material
        {
            BaseColor = new Vector4(1f, 0.22f, 0.12f, 1f),
            EmissiveColor = new Vector3(1f, 0.22f, 0.12f),
            Unlit = true,
            Blend = BlendMode.Additive,
            DepthWrite = false,
            Name = "beam"
        };

        _spin[index] = new Node { Name = "mirror" };

        // Two blades, opposite each other, which is what these have inside them and is why a corridor with
        // one beacon in it gets two flashes a turn.
        //
        // One blade, not two, and the reason is that the lamp rides on the end of it.
        //
        // A beacon with two mirrors opposite each other throws two patches, and one light between them has
        // to choose — which means jumping from one to the other every half turn, at a moment when both are
        // the same distance from the fitting but nowhere near each other. With one blade the impact point
        // slides along the wall, round the corner and across the floor without ever leaving the surface it
        // is on, which is a continuous function of the bearing and needs no special case anywhere.
        //
        // The cone is built at unit length and scaled uniformly every frame, so its spread is an angle
        // rather than a width. That is the difference between a beam and a rod: scale only the length and a
        // long throw comes out as a needle and a short one as a stub, both with the same girth, which is
        // not how light leaves anything. Uniform scale keeps the cone angle fixed, which is what a fixed
        // reflector has.
        //
        // Cylinder builds about +Y with radiusTop at the +Y end, so a yaw cannot aim it and a roll can: a
        // roll of ninety plus the tilt sends +Y to Along, which puts the wide end away from the fitting.
        _blade[index] = new Node
        {
            Name = "blade",
            RotationDegrees = new Vector3(0f, 0f, -(90f + Tilt))
        };

        // Four cones inside each other rather than one, and this is the only way to get a soft edge out of
        // a renderer that draws surfaces.
        //
        // A shaft of light is brightest along its axis and fades to nothing at the rim, because near the
        // rim you are looking through less of it. A single hollow cone cannot do that: every ray through it
        // crosses exactly two of its surfaces whether it goes through the middle or grazes the side, so
        // what an additive shell gives is a slab of even brightness with a hard silhouette — which reads as
        // coloured glass. Nesting four of them makes the count vary: eight surfaces on the axis, six, four,
        // and two right at the edge. The brightness falls off in four steps from the middle out, and four
        // steps at this dimness is a gradient.
        //
        // They share one material, so the accumulation is doing all of it and there is still one number per
        // beacon to fade. Each surface is therefore worth an eighth of what the whole shaft should be.
        foreach (var shell in new[] { 1f, 0.72f, 0.48f, 0.26f })
            _blade[index].Children.Add(new MeshNode(
                Primitives.Cylinder(0.042f, 0.010f, 1f, 10, capped: false),
                _beam[index])
            {
                Scale = new Vector3(shell, 1f, shell),
                Name = "shell"
            });

        _spin[index].Children.Add(_blade[index]);

        root.Children.Add(_spin[index]);

        return root;
    }

    /// <summary>
    /// The structural frames, every two metres and eight. They are the only thing in here that says how
    /// long it is: twenty-one metres of unbroken wall reads as about eight, and eight ribs going away from
    /// you in perspective reads as exactly what it is.
    /// </summary>
    private static void Frames(Node root)
    {
        var rib = Steel();
        var half = Width / 2f;

        for (var z = 1f; z < Length; z += 2.8f)
        {
            foreach (var side in new[] { -1f, 1f })
                root.Children.Add(Fabric.Slab(
                    new Vector3(0.09f, Height - 0.24f, 0.16f),
                    new Vector3(side * (half - 0.045f), (Height - 0.24f) / 2f, z),
                    rib,
                    "rib",
                    Finish.Close));

            root.Children.Add(Fabric.Slab(
                new Vector3(Width, 0.09f, 0.16f),
                new Vector3(0f, Height - 0.045f, z),
                rib,
                "header",
                Finish.Close));
        }

        // A conduit run down both sides at shoulder height, because a corridor with nothing on its walls is
        // a rendering of a corridor.
        foreach (var side in new[] { -1f, 1f })
            root.Children.Add(Fabric.Slab(
                new Vector3(0.07f, 0.07f, Length - 0.4f),
                new Vector3(side * (half - 0.055f), 1.62f, Length / 2f),
                rib,
                "conduit",
                Finish.Close));
    }

    /// <summary>
    /// The runway: a dashed line of light along both skirtings, chasing toward the door.
    ///
    /// Fourteen segments, fourteen materials, twenty-eight boxes — each material used once a side, so the
    /// two runs are in step and the corridor has one direction rather than two. Unlit and additive, which
    /// is what lets it run at all while the four slots are somewhere else, and it is also the honest
    /// account of what it is: a light strip is not a lit surface, it is a surface that is added to the
    /// frame.
    /// </summary>
    private void Runway(Node root)
    {
        var half = Width / 2f;
        var pitch = (Length - 1.4f) / Segments;

        for (var i = 0; i < Segments; i++)
        {
            _runway[i] = new Material
            {
                BaseColor = new Vector4(1f, 0.18f, 0.10f, 1f),
                EmissiveColor = new Vector3(1f, 0.18f, 0.10f),
                Unlit = true,
                Blend = BlendMode.Additive,
                DepthWrite = false,
                Name = "runway"
            };

            var z = 0.8f + i * pitch;

            foreach (var side in new[] { -1f, 1f })
                root.Children.Add(Fabric.Slab(
                    new Vector3(0.025f, 0.045f, pitch * 0.62f),
                    new Vector3(side * (half - 0.012f), 0.14f, z),
                    _runway[i],
                    "runway",
                    Finish.Close));
        }
    }
}
