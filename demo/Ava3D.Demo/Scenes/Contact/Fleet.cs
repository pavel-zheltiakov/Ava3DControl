using System.Numerics;
using Ava3D.Demo.Textures;

namespace Ava3D.Demo.Scenes.Contact;

/// <summary>
/// One ship: a modelled hull, its panel lines, its lamps, and the state that decides whether it is
/// still flying.
///
/// A ship is a node with a transform. Everything hanging off it is authored once in the ship's own
/// space and never touched again, so a frame of flight costs one position and one quaternion however
/// much detail is bolted to it.
///
/// Nothing on a ship is a sprite. The engine bells and the nav lamps are geometry with an emissive
/// material, because a billboard standing in for a lamp is convincing at two kilometres and a smear at
/// twenty metres, and this film flies the camera to twenty metres. What brightness they run at is a
/// number on a material, which is why <see cref="Burn"/> and <see cref="Beacon"/> exist and why the
/// hull can be lit from inside when it is taking hits.
/// </summary>
internal sealed class Ship
{
    public required Node Root { get; init; }
    public required Vector3[] Path { get; init; }

    /// <summary>
    /// How far it is rolled into its turn, every half second of the cycle. Integrated offline as a
    /// rigid body under its own roll thrusters — see <see cref="Paths.KestrelRoll"/> for why a bank is
    /// a track and not an expression.
    /// </summary>
    public required float[] Roll { get; init; }

    /// <summary>The hull, so a dying ship can be lit from inside and then go dark.</summary>
    public required Material Hull { get; init; }

    /// <summary>The engine bells. Emissive geometry, one material each.</summary>
    public required Material[] Engines { get; init; }

    /// <summary>What comes out of them. Null for anything without engines.</summary>
    public Plume? Jet { get; init; }

    /// <summary>The nav lamps — red to port, green to starboard, and whatever the film wants.</summary>
    public required Material[] Lamps { get; init; }

    /// <summary>
    /// The one sprite a ship has, and it is not a lamp: it is the contact marker. It fades out as the
    /// camera closes, so at any range where you can see the ship at all you are seeing the lamps and not
    /// a billboard over them — which is both the honest way to do it and the reason the feature exists.
    /// Emissive geometry cannot hold a ship on screen once its hull is sub-pixel; this can.
    ///
    /// <b>It depth-tests, and for a while it did not.</b> The marker has to survive the hull it belongs
    /// to, and the cheap way to arrange that is to switch the depth test off — which works exactly as
    /// long as the only thing between the camera and the ship is that ship. It is not: the film puts a
    /// window in front of the fleet and a metre of hull round the window, and a marker that ignores depth
    /// ignores <i>that</i> too. What it looks like is a coloured smudge sitting on the wall of the room
    /// you are standing in, unattached to anything. See <see cref="Mark"/> for what replaced it.
    /// </summary>
    public required SpriteNode Contact { get; init; }

    /// <summary>
    /// Whether this ship wants a contact marker at all.
    ///
    /// True for a battle three thousand metres across, where half the fleet is sub-pixel and finding it
    /// is the point. False for anything meant to be <i>looked at</i> — the traffic outside the gallery
    /// window is eight hulls at two hundred metres, close enough to read, and a glow floating in front of
    /// each of them is an aid nobody asked for standing between the viewer and the ship.
    /// </summary>
    public bool Marked { get; set; } = true;

    public required float Length { get; init; }

    /// <summary>
    /// Where its guns are, in its own frame — nose or wing roots. A gun is bolted to the hull and
    /// points where the hull points; none of them traverses. So a ship shoots along its own nose and
    /// nowhere else, and hitting anything is a question of flying, not of aiming.
    /// </summary>
    public Vector3[] Guns { get; init; } = [];

    /// <summary>Where on its path the ship sits at the start of the cycle.</summary>
    public float Phase { get; init; }

    /// <summary>
    /// The ship's own geometry, broken out so that it can come apart: one node per material group, plus
    /// the panel lines. They are the parts the model was built from — a fuselage, a canopy, nacelles,
    /// trim, lamps — so a ship that breaks up scatters pieces of <i>itself</i> rather than a puff of
    /// something else.
    /// </summary>
    public Node[] Pieces { get; init; } = [];

    /// <summary>Every material on the ship, so the whole hull can go white-hot and then fade out.</summary>
    public Material[] Skin { get; init; } = [];

    /// <summary>Its pose this frame, kept so the camera and the guns can read it without re-sampling.</summary>
    public Flight.Pose Pose { get; private set; }

    public bool Alive { get; private set; } = true;

    private Vector3[] _engineRest = [], _lampRest = [];
    private Vector3 _hullRest;
    private Vector4 _hullColorRest;

    private Vector3 _wreckPosition, _wreckVelocity, _wreckSpin;
    private Quaternion _wreckRotation = Quaternion.Identity;
    private float _wreckAge;

    // How the pieces come apart: a drift and a spin each, and where they started.
    private Vector3[] _drift = [], _tumbleAxis = [], _pieceRest = [];
    private float[] _tumbleRate = [];
    private Vector3[] _skinEmissiveRest = [];
    private Vector4[] _skinColorRest = [];

    /// <summary>
    /// How long the breakup lasts. Long enough to watch, short enough that nothing is still drifting
    /// when the shot cuts.
    /// </summary>
    private const float BreakupSeconds = 2.4f;

    /// <summary>The nose, in world space.</summary>
    public Vector3 Muzzle => Pose.Position + Vector3.Transform(new Vector3(0f, 0f, -Length * 0.5f), Pose.Rotation);

    /// <summary>Gun <paramref name="index"/> in world space, wrapping round however many there are.</summary>
    public Vector3 Gun(int index) => Guns.Length == 0
        ? Muzzle
        : Pose.Position + Vector3.Transform(Guns[index % Guns.Length] * Length, Pose.Rotation);

    /// <summary>
    /// Whether this ship can currently hit <paramref name="target"/>, and where it should aim.
    ///
    /// Two conditions, both of them the real ones. The target has to be inside <paramref name="range"/>,
    /// and it has to be inside a cone of <paramref name="cone"/> radians about the ship's <i>nose</i> —
    /// not about the line between them. A ship with a target off its beam has no shot however much it
    /// would like one; it has to turn, and turning is the paths' job.
    ///
    /// The cone is tested against where the target will be when a bolt gets there, not where it is now.
    /// That is what leading a target means, and it falls out of the geometry rather than being faked:
    /// the flight time is the range over the bolt's speed, and one pass of that is accurate enough at
    /// these closing rates.
    /// </summary>
    public bool CanHit(Ship target, float cone, float range, float boltSpeed, out Vector3 lead)
    {
        lead = target.Pose.Position;

        if (!Alive || !target.Alive)
            return false;

        var muzzle = Gun(0);
        var separation = Vector3.Distance(muzzle, lead);
        if (separation > range)
            return false;

        // Where it will be in the time a bolt takes to cross what is between them.
        lead = target.Pose.Position + target.Velocity * (separation / boltSpeed);

        var bearing = lead - muzzle;
        var distance = bearing.Length();
        if (distance < 1f || distance > range)
            return false;

        return Vector3.Dot(Pose.Forward, bearing / distance) >= MathF.Cos(cone);
    }

    /// <summary>How fast it is going, in world units a second. Straight off its own trajectory.</summary>
    public Vector3 Velocity => Pose.Velocity;

    /// <summary>Remembers what everything glows at when nothing is happening to it.</summary>
    public void Rest()
    {
        _engineRest = [.. Engines.Select(m => m.EmissiveColor)];
        _lampRest = [.. Lamps.Select(m => m.EmissiveColor)];
        _hullRest = Hull.EmissiveColor;
        _hullColorRest = Hull.BaseColor;
    }

    /// <summary>Engine brightness, 1 being full power.</summary>
    public void Burn(float level)
    {
        // A wreck's engines belong to the breakup, which is writing every material on the ship. The
        // film flickers engines for every ship each frame without asking whether it is still flying,
        // and Dress runs after Tumble, so without this the plasma glow was overwritten on exactly the
        // parts of a dying ship that ought to be brightest.
        if (!Alive)
            return;

        for (var i = 0; i < Engines.Length; i++)
            Engines[i].EmissiveColor = _engineRest[i] * level;

        // The bell and the jet are one thing, so they take one number. A bell that dims while its plume
        // holds is a lamp with a flame painted behind it.
        Jet?.Throttle(level);
    }

    /// <summary>Lamp brightness, and the colour the contact marker shows at.</summary>
    public void Beacon(float level)
    {
        if (!Alive)
            return;

        for (var i = 0; i < Lamps.Length; i++)
            Lamps[i].EmissiveColor = _lampRest[i] * level;
    }

    /// <summary>
    /// The two things on a ship that depend on where the camera ended up this frame: the contact marker
    /// and the roll of the exhaust cards.
    ///
    /// The marker fades in with range — absent inside eight ship lengths, full beyond forty. The numbers
    /// are in ship lengths rather than world units so one rule covers a 420-unit freighter and a
    /// 215-unit raider: the marker matters at the range where the hull stops being readable, and that
    /// range is a property of how big the ship is.
    ///
    /// <b>And it is moved rather than exempted.</b> The marker's whole problem is that it has to be in
    /// front of one object — its own hull — and behind every other one, and there is no flag that says
    /// that. So instead of switching the depth test off, it is put where the answer is already right:
    /// six tenths of a ship length off the centre, along the line to the camera. A hull reaches half its
    /// length in its longest direction, so that clears it from every angle, and everything else in the
    /// world — a station, an asteroid, the metre of hull round a gallery window — occludes it the way it
    /// occludes the ship.
    /// </summary>
    public void Mark(Vector3 cameraPosition)
    {
        if (!Alive)
            return;

        var range = Vector3.Distance(cameraPosition, Pose.Position) / Length;
        var fade = Math.Clamp((range - 8f) / 32f, 0f, 1f);

        Contact.IsVisible = Marked && fade > 0.01f;
        Contact.Opacity = 0.75f * fade;

        if (Contact.IsVisible)
        {
            var toEye = cameraPosition - Pose.Position;

            // Into the ship's own frame, because the marker hangs off the node the pose is on. Degenerate
            // only if the camera is exactly at the ship's centre, which is a shot this film does not have.
            var stand = toEye.LengthSquared() > 1e-4f
                ? Vector3.Transform(Vector3.Normalize(toEye), Quaternion.Conjugate(Pose.Rotation))
                : Vector3.UnitY;

            Contact.Position = stand * (Length * 0.6f);
        }

        Jet?.Face(cameraPosition, Pose);
    }

    public void Follow(float u)
    {
        if (!Alive)
            return;

        Pose = Flight.Follow(Path, Roll, u + Phase, closed: false);
        Root.Position = Pose.Position;
        Root.Rotation = Pose.Rotation;
    }

    /// <summary>Places the ship directly, for a wingman flying formation on somebody else.</summary>
    public void Place(Vector3 position, Quaternion rotation, Vector3 forward, Vector3 velocity)
    {
        if (!Alive)
            return;

        Pose = new Flight.Pose(position, rotation, forward, velocity);
        Root.Position = position;
        Root.Rotation = rotation;
    }

    /// <summary>
    /// Kills the ship: the engines go out, the lamps go dark, and it comes apart.
    ///
    /// It used to simply stop being drawn half a second later, on the reasoning that what is left of a
    /// hull which has come apart is a cloud and not a hull. The reasoning was right and the execution
    /// was not: a ship that vanishes has not come apart, it has been switched off, and that is what it
    /// looked like. So the pieces the model was built from now separate — each with its own drift and
    /// its own tumble — while the whole thing goes white-hot and then cools and fades. Nothing here is
    /// a new asset; it is the same six nodes the ship was always drawn from, allowed to go their own
    /// ways.
    /// </summary>
    public void Kill(Vector3 velocity, Vector3 spin)
    {
        if (!Alive)
            return;

        Burn(0f);
        Beacon(0f);

        Alive = false;
        _wreckPosition = Pose.Position;
        _wreckRotation = Pose.Rotation;
        _wreckVelocity = velocity;
        _wreckSpin = spin;
        _wreckAge = 0f;
        Contact.IsVisible = false;

        // Where each piece goes. Deterministic from its index, because the film is a pure function of
        // time and has to look the same every time round the loop.
        _drift = new Vector3[Pieces.Length];
        _tumbleAxis = new Vector3[Pieces.Length];
        _tumbleRate = new float[Pieces.Length];
        _pieceRest = new Vector3[Pieces.Length];

        for (var i = 0; i < Pieces.Length; i++)
        {
            _pieceRest[i] = Pieces[i].Position;

            // Outward, mostly sideways, and never all at the same speed — a burst in which every piece
            // travels together is a scale, not a breakup. Squashed on Y rather than renormalised: the
            // point is a bias toward the plane, not a shape with a name.
            var drift = Scatter.Direction(i, 1);
            drift.Y *= 0.7f;

            // In the model's own units, not the world's. These nodes hang under a root scaled to the
            // ship's length, so a drift written in world units is multiplied by that length a second
            // time — for a raider, 215 — and the pieces leave for the next county at twenty thousand
            // units while the camera holds on empty space where the wreck used to be. The model is
            // authored about one unit long, so a fraction here is a fraction of the ship.
            _drift[i] = drift * (0.22f + 0.40f * Scatter.Value(i, 2));

            // A direction, which is what a tumble axis is. This used to be a vector of three signed
            // numbers normalised, with a thousandth added to X so that the one case in a billion where
            // all three came out zero did not divide by it.
            _tumbleAxis[i] = Scatter.Direction(i, 3);

            _tumbleRate[i] = (1.2f + 3.4f * Scatter.Value(i, 4)) * (Scatter.Value(i, 5) < 0.5f ? -1f : 1f);
        }
    }

    /// <summary>
    /// The breakup: the wreck as a whole is ballistic and spinning — no drag, because there is none —
    /// and every piece of it is doing the same thing again on its own account.
    ///
    /// The colour is the part that sells it. A hull that has just been opened up is not lit, it is
    /// <i>incandescent</i>, so the emissive goes to white with a blue lift for the first breath, cools
    /// through orange over the next second, and is dull red by the time the alpha has taken it. Cooling
    /// and fading are deliberately not the same curve: the glow is gone at about a second and the
    /// geometry lingers to two and a half, so what fades out at the end is wreckage rather than embers.
    /// </summary>
    public void Tumble(float dt)
    {
        if (Alive)
            return;

        _wreckAge += dt;
        _wreckPosition += _wreckVelocity * dt;
        _wreckRotation = Quaternion.Normalize(Quaternion.Concatenate(
            _wreckRotation,
            Quaternion.CreateFromYawPitchRoll(_wreckSpin.Y * dt, _wreckSpin.X * dt, _wreckSpin.Z * dt)));

        Root.Position = _wreckPosition;
        Root.Rotation = _wreckRotation;
        Pose = new Flight.Pose(_wreckPosition, _wreckRotation, Pose.Forward, _wreckVelocity);

        var age = _wreckAge;

        // The pieces go their own ways. Their separation eases off rather than running at a constant
        // rate, because whatever pushed them apart did so once.
        var spread = 1f - MathF.Exp(-age * 1.6f);

        for (var i = 0; i < Pieces.Length && i < _drift.Length; i++)
        {
            Pieces[i].Position = _pieceRest[i] + _drift[i] * spread;
            Pieces[i].Rotation = Quaternion.CreateFromAxisAngle(_tumbleAxis[i], _tumbleRate[i] * age);
        }

        // White-hot, then orange, then out.
        var heat = MathF.Max(0f, 1f - age / 1.05f);
        heat *= heat;

        var glow = new Vector3(1.00f, 0.62f, 0.24f) * heat * 3.2f
                   + new Vector3(0.55f, 0.70f, 1.00f) * MathF.Max(0f, 1f - age / 0.22f) * 2.4f;

        // Fading starts once the glow is mostly gone: a piece that fades while it is still white-hot
        // reads as a light going out, not as debris going away.
        var alpha = 1f - Math.Clamp((age - 0.9f) / (BreakupSeconds - 0.9f), 0f, 1f);
        alpha *= alpha;

        for (var i = 0; i < Skin.Length; i++)
        {
            Skin[i].EmissiveColor = glow;
            Skin[i].Blend = BlendMode.Alpha;

            var rest = _skinColorRest[i];
            Skin[i].BaseColor = new Vector4(rest.X, rest.Y, rest.Z, rest.W * alpha);
        }

        Root.IsVisible = age < BreakupSeconds;
    }

    /// <summary>Puts the ship back the way it was built, for the loop.</summary>
    public void Revive()
    {
        Alive = true;
        _wreckAge = 0f;

        for (var i = 0; i < Skin.Length; i++)
        {
            Skin[i].EmissiveColor = _skinEmissiveRest[i];
            Skin[i].BaseColor = _skinColorRest[i];
            Skin[i].Blend = BlendMode.Opaque;
        }

        for (var i = 0; i < Pieces.Length && i < _pieceRest.Length; i++)
        {
            Pieces[i].Position = _pieceRest[i];
            Pieces[i].Rotation = Quaternion.Identity;
        }

        Hull.EmissiveColor = _hullRest;
        Hull.BaseColor = _hullColorRest;
        Root.IsVisible = true;
        Root.Scale = Vector3.One;

        Burn(1f);
        Beacon(1f);
        Contact.IsVisible = true;
    }

    /// <summary>Remembers what every material looked like alive, so the breakup can be undone.</summary>
    public void Remember()
    {
        _skinEmissiveRest = [.. Skin.Select(m => m.EmissiveColor)];
        _skinColorRest = [.. Skin.Select(m => m.BaseColor)];
        _pieceRest = [.. Pieces.Select(p => p.Position)];
    }

    /// <summary>The hull's resting emissive, for the damage flicker to be measured against.</summary>
    public Vector3 HullRest => _hullRest;
}

/// <summary>
/// A bell mouth in the model's own units — where it opens, and how wide.
///
/// The models are authored about one unit long and scaled to the ship's length when they are loaded, so
/// these are fractions of a ship rather than distances. The numbers come straight off the <c>bell.*</c>
/// tubes in <c>tools/models/build-models.py</c>, converted the one way Blender's exporter converts:
/// Blender (x, y, z) leaves as glTF (x, z, −y), so the nose that was authored at +Y arrives at −Z and
/// the bells that were authored aft at −Y arrive at +Z.
/// </summary>
internal readonly record struct Nozzle(float Across, float Rise, float Aft, float Radius);

/// <summary>
/// A ship's exhaust: one card per bell, and one disc per bell in the plane of the nozzles.
///
/// The card is the flame and the disc is the same flame seen up the pipe, and both are there because
/// neither works alone. A flame is not a shape that can be modelled once and looked at from anywhere:
/// what it looks like from the beam is a long tapering jet, and what it looks like from dead astern is a
/// bright disc, and no single piece of geometry is both. A cone tried it and failed for a reason worth
/// writing down — additive shading counts surfaces crossed rather than distance travelled through them,
/// so a cone shell contributes exactly two samples everywhere inside its silhouette and comes out as a
/// flat triangle of paint with a hard edge, which is the one thing a flame never is.
///
/// So the card rolls. Not to face the camera — a flame does not swing round to look at you, it burns
/// straight out of the bell — but about the thrust axis, which is the one degree of freedom a jet
/// actually has and which is enough to hold the card broadside on from anywhere off the axis. The disc
/// covers the angles where the card has run out of roll, which is exactly where a jet would be reading
/// as a disc anyway.
///
/// The colour is taken off the bell's own emissive rather than chosen. A plume that was tuned by eye is
/// a plume that goes wrong the next time somebody edits the model.
/// </summary>
internal sealed class Plume
{
    /// <summary>One card per bell. Each rolls about its own thrust axis — not about the ship's.</summary>
    public required Node[] Cards { get; init; }

    /// <summary>The discs. They never turn, so all of a ship's are one mesh and one draw.</summary>
    public required Node Discs { get; init; }

    public required Material Flame { get; init; }
    public required Material Disc { get; init; }

    /// <summary>The bell's emissive, scaled so its brightest channel is 1.</summary>
    public required Vector3 Tint { get; init; }

    /// <summary>
    /// Well past 1, so the middle of the jet clips to white and only its edges keep the tint. A flame
    /// that stays inside the display's range everywhere is a coloured shape; a flame that blows out in
    /// the middle is a flame, and blowing out is what a real one does to a real sensor.
    /// </summary>
    private const float FlameGain = 1.55f;

    private const float DiscGain = 1.35f;

    private float _level = 1f;

    /// <summary>Throttle, 0 to 1. Brightness and length together, because that is what a throttle does.</summary>
    public void Throttle(float level)
    {
        _level = Math.Clamp(level, 0f, 1f);

        var lit = _level > 0.02f;
        var reach = new Vector3(1f, 1f, 0.55f + 0.45f * _level);

        foreach (var card in Cards)
        {
            card.IsVisible = lit;

            // Only along the thrust axis, and the axis the cards roll about is the same one — so the
            // scale and the roll commute and there is no shear to worry about.
            card.Scale = reach;
        }

        Discs.IsVisible = lit;

        Disc.BaseColor = new Vector4(Tint * DiscGain, _level * 0.85f);

        // Face overwrites this every frame with the edge-on fade applied. Set here too so that a ship
        // which has been throttled but not yet looked at — the first frame, or a capture of it — is not
        // drawing a white card at full strength.
        Flame.BaseColor = new Vector4(Tint * FlameGain, _level);
    }

    /// <summary>Rolls every card about its own bell until its face is as near the camera as it can get.</summary>
    public void Face(Vector3 cameraPosition, Flight.Pose pose)
    {
        // Where the camera is in the ship's frame. The cards start face-up — their normal is +Y — so
        // the roll wanted is the one that takes +Y round to the camera's bearing across the jet.
        var local = Vector3.Transform(cameraPosition - pose.Position, Quaternion.Inverse(pose.Rotation));
        var roll = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.Atan2(-local.X, local.Y));

        // One angle for every bell on the ship, because they are parallel and the camera is a long way
        // further off than they are apart — but applied to each card about its own bell rather than to
        // one node about the centreline, which would swing the outboard engines round the hull.
        foreach (var card in Cards)
            card.Rotation = roll;

        // And the card goes out as it turns edge-on, because a card seen edge-on is not a thin flame —
        // it is a hairline at the flame's full brightness, drawn straight across the disc that is
        // supposed to be carrying the shot. The measure is the sine of the angle off the thrust axis,
        // which is also how much of the card's width is left on screen, and it takes the last thirty
        // degrees to do the fade — by which point the disc is doing the whole job anyway.
        var range = local.Length();
        var broadside = range < 1e-3f
            ? 1f
            : MathF.Sqrt(MathF.Max(0f, 1f - local.Z / range * (local.Z / range)));

        Flame.BaseColor = new Vector4(Tint * FlameGain, _level * MathF.Min(1f, broadside * 2f));
    }
}

/// <summary>
/// The layers of the docking gate, and the materials that carry their colour. Built by
/// <see cref="Fleet.BuildGate"/> and driven entirely from <c>ContactScene.Dress</c>: everything here
/// is a transform or a <see cref="Material.BaseColor"/>, which is the only kind of animation this
/// scene allows itself.
/// </summary>
internal sealed class GateField
{
    public required Node Root { get; init; }

    /// <summary>The opaque door: full strength when the bay is shut, gone when it is cleared.</summary>
    public required Node Plate { get; init; }

    public required Material PlateSkin { get; init; }

    /// <summary>Two counter-rotating copies of the same swirl, at two depths.</summary>
    public required Node[] Swirls { get; init; }

    public required Material[] SwirlSkins { get; init; }

    /// <summary>Three rings, each scaling out and fading a third of a cycle behind the last.</summary>
    public required Node[] Rings { get; init; }

    public required Material[] RingSkins { get; init; }
}

/// <summary>
/// Everything in the scene that is built once and then only ever moved: the sky, the stars, the planet,
/// the sun, the station and the ships.
/// </summary>
internal static class Fleet
{
    public const float PlanetRadius = 24_576f;
    public const float SunRadius = 24_000f;
    public const float SkyRadius = 2_400_000f;

    public static readonly Vector3 PlanetCentre = new(-30_000f, -9_000f, -70_000f);
    public static readonly Vector3 SunDirection = Vector3.Normalize(new Vector3(-0.55f, 0.20f, -0.81f));
    public static Vector3 SunCentre => SunDirection * 900_000f;

    /// <summary>How heavy a panel line is, in pixels. Honoured on every backend since 12.1.0-preview.2.</summary>
    private const float PanelWidth = 1.4f;

    /// <summary>
    /// The sky sphere, seen from inside. <see cref="CullMode.Front"/> is what makes that work: only the
    /// far hemisphere draws, so the near half is not sitting between the camera and everything else.
    /// </summary>
    public static void BuildSky(Node world)
    {
        world.Children.Add(new MeshNode(Primitives.Sphere(SkyRadius, 48, 32), new Material
        {
            Name = "nebula",
            BaseColorTexture = Space.Nebula(),
            BaseColor = new Vector4(0.9f, 0.9f, 1.0f, 1f),
            Unlit = true,
            Cull = CullMode.Front,
            DepthWrite = false
        })
        {
            RenderOrder = -10
        });
    }

    /// <summary>Two star layers: many faint, a few bright, both at a fixed size on screen.</summary>
    public static void BuildStars(Node world)
    {
        world.Children.Add(new PointsNode
        {
            Positions = OnSphere(1_500, 2_000_000f, seed: 1),
            Color = new Vector3(0.55f, 0.71f, 0.80f),
            Size = 2.0f,
            SizeAttenuation = false,
            Blend = BlendMode.Additive,
            DepthWrite = false,
            RenderOrder = -5
        });

        world.Children.Add(new PointsNode
        {
            Positions = OnSphere(160, 1_960_000f, seed: 2),
            Color = new Vector3(0.87f, 0.93f, 1.00f),
            Opacity = 0.9f,
            Size = 3.6f,
            SizeAttenuation = false,
            Blend = BlendMode.Additive,
            DepthWrite = false,
            RenderOrder = -4
        });
    }

    /// <summary>The planet: a bump-mapped body with night-side cities, and an atmosphere that is only a rim.</summary>
    public static Node BuildPlanet(Node world)
    {
        var (albedo, bump, cityLights) = Space.Planet();

        var planet = new Node
        {
            Position = PlanetCentre,
            RotationDegrees = new Vector3(-18f, 0f, 8f)
        };

        world.Children.Add(planet);

        planet.Children.Add(new MeshNode(Primitives.Sphere(PlanetRadius, 64, 48), new Material
        {
            Name = "planet",
            BaseColorTexture = albedo,
            Roughness = 0.95f,
            BumpTexture = bump,
            BumpScale = 14f,
            EmissiveTexture = cityLights,
            EmissiveColor = new Vector3(1.15f),
            EmissiveNightSide = true,
            EmissiveNightSideStart = 0.20f,
            EmissiveNightSideEnd = -0.10f,
            Cull = CullMode.Back
        }));

        planet.Children.Add(new MeshNode(Primitives.Sphere(PlanetRadius * 1.025f, 48, 32), new Material
        {
            Name = "atmosphere",
            BaseColor = Vector4.Zero,
            RimColor = new Vector3(0.42f, 0.68f, 1.00f),
            RimPower = 3.2f,
            RimIntensity = 1.4f,
            RimLightBias = 0.4f,
            Blend = BlendMode.Additive,
            DepthWrite = false,
            Cull = CullMode.Back
        })
        {
            RenderOrder = 1
        });

        return planet;
    }

    /// <summary>The sun: a flat unlit disc and a corona five times its width.</summary>
    public static void BuildSun(Node world, Texture glow)
    {
        world.Children.Add(new MeshNode(Primitives.Sphere(SunRadius, 32, 24), new Material
        {
            Name = "sun",
            BaseColor = new Vector4(1.00f, 0.95f, 0.78f, 1f),
            Unlit = true
        })
        {
            Position = SunCentre
        });

        world.Children.Add(new SpriteNode
        {
            Texture = glow,
            Position = SunCentre,
            Color = new Vector3(1.00f, 0.85f, 0.56f),
            Size = new Vector2(SunRadius * 5.4f),
            Blend = BlendMode.Additive,
            DepthWrite = false
        });
    }

    /// <summary>
    /// Relay Nine: the modelled station, its panel lines, and the six lamps round the mouth of its
    /// docking bay that the film chases.
    /// </summary>
    /// <summary>How large Relay Nine is built, in world units per model unit.</summary>
    public const float StationScale = 620f;

    /// <summary>
    /// Where the mouth of the docking bay sits, as an offset from the station's own origin.
    ///
    /// The bay is authored at Blender <c>y = −1.50</c> and the glTF exporter maps (x, y, z) to
    /// (x, z, −y), so the mouth comes out on <c>+Z</c> — the one bearing Relay Nine is open in, and the
    /// bearing the convoy arrives on. A light placed a little inside this is the gate light; see
    /// <c>ContactScene.BuildLights</c>.
    /// </summary>
    public static readonly Vector3 BayMouth = new(0f, 0f, 1.42f * StationScale);

    public static (Node Root, Material[] Lamps, Material Bay, Material Collar) BuildStation(
        Node world, Vector3 position, (Texture Plate, Texture Relief) plating)
    {
        var model = Models.Load("relay.glb", StationScale);

        var root = new Node { Position = position };
        root.Children.Add(model.Root);
        world.Children.Add(root);

        // Plating on the parts that are hull, and the height field that goes with it. A station is the
        // largest thing in the film that the camera gets close to, and at that range a flat facet is
        // the one place the eye can tell it is looking at a model rather than at a place.
        var (plate, relief) = plating;

        Skin(model.Material("relay.hull"), plate, relief, 5f);
        Skin(model.Material("relay.trim"), plate, relief, 4f);

        // The docking surround takes the plating too — it is hull, and a bare facet beside a plated one
        // is the join showing. What it does not take is the hull's brightness: see relay.port in the
        // builder for why the surround of a signal is painted dark.
        Skin(model.Material("relay.port"), plate, relief, 4f);

        // The solar wings take the relief but not the plating: the bump gives them cell divisions, and
        // a panel texture over the top would turn an array back into hull.
        Skin(model.Material("relay.panel"), null, relief, 2.5f);

        model.Root.Children.Add(Panels(model.Edges, 0.34f));

        // The six docking lamps are unlit, and that is the whole reason they read as signal lamps
        // rather than as pink tiles. A lit material's emissive is added to its shaded colour and the
        // result goes through the tone map, which pulls anything bright toward white — so a saturated
        // red asked for as emissive comes back as salmon, and asking louder only makes it pinker.
        // Unlit puts the colour on the screen unchanged, which is what a lamp lens does.
        var lamps = model.Group("relay.lamp.");
        foreach (var lamp in lamps)
        {
            lamp.Unlit = true;
            lamp.EmissiveColor = Vector3.Zero;
        }

        var collar = model.Material("relay.collar");
        collar.Unlit = true;
        collar.EmissiveColor = Vector3.Zero;

        // The lit panels in the hall behind the gate, for the same reason as the lamps: a light panel
        // is a lens, and a lens puts its colour on the screen rather than adding it to a shaded surface
        // and letting the tone map take the top off.
        foreach (var panel in model.Group("relay.hall.lamp"))
            panel.Unlit = true;

        return (root, lamps, model.Material("relay.bay"), collar);
    }

    /// <summary>
    /// The gate itself: an energy door across the mouth of the bay, red and all but solid when the bay
    /// is shut, thin green when it is cleared and the hall behind it shows through.
    ///
    /// It is six quads and three textures, and the reason it is not one animated texture is the scene's
    /// own rule arriving from a new direction. The texture caches release anything that did not appear
    /// in the frame just drawn, so a cycle of pre-baked frames swapped into the material would delete
    /// and re-upload the whole set every frame — the same trap as <see cref="LineNode.Positions"/>,
    /// wearing different clothes. So nothing here animates data: two copies of one swirl counter-rotate
    /// at rates that do not divide into each other, which never visibly repeats, and three rings scale
    /// out and fade on offset phases. Twelve triangles, six transforms and six colours a frame.
    ///
    /// The plate and the swirls blend <see cref="BlendMode.Alpha"/> because a shut door has to be able
    /// to hide what is behind it, and additive never can. The rings blend additive because a ring of
    /// light is light.
    ///
    /// They are added deepest first, and that is not tidiness. <c>SceneSnapshot</c> sorts by render
    /// order and then by the order things were added — never by distance, deliberately, because a
    /// depth sort per frame is a cost every scene would pay for the few that need it. So the back-to-
    /// front order of an alpha stack is the order its nodes go into the tree, and getting it wrong here
    /// draws the door in front of the field it is supposed to be behind.
    /// </summary>
    public static GateField BuildGate(Node world, Vector3 position, Texture plate, Texture swirl,
                                      Texture ring)
    {
        var root = new Node { Position = position + BayMouth };
        world.Children.Add(root);

        // The door, deepest of the layers and the only opaque one. It fades to nothing as the bay
        // clears, which is what turns the gate from a wall into an opening.
        var plateNode = Layer(root, plate, 0.375f, -0.10f, BlendMode.Alpha, out var plateSkin);

        // Two swirls, set back from the lip rather than flush with it. A single card in the plane of the
        // mouth reads as a sticker on the door from any angle off the axis; two, a sixteenth of the
        // station's radius apart, have parallax, and parallax is what says the light occupies the
        // doorway instead of being painted across it. Index 0 is the far one, because that is the order
        // they have to be drawn in and the order they have to be added in is the same order.
        var swirls = new Node[2];
        var swirlSkins = new Material[2];

        swirls[0] = Layer(root, swirl, 0.320f, -0.08f, BlendMode.Alpha, out swirlSkins[0]);

        var rings = new Node[3];
        var ringSkins = new Material[3];
        for (var i = 0; i < 3; i++)
            rings[i] = Layer(root, ring, 0.385f, -0.05f, BlendMode.Additive, out ringSkins[i]);

        swirls[1] = Layer(root, swirl, 0.375f, -0.02f, BlendMode.Alpha, out swirlSkins[1]);

        return new GateField
        {
            Root = root,
            Plate = plateNode,
            PlateSkin = plateSkin,
            Swirls = swirls,
            SwirlSkins = swirlSkins,
            Rings = rings,
            RingSkins = ringSkins
        };
    }

    /// <summary>One layer of the gate: a card at a depth, on its own material so it can be tinted alone.</summary>
    private static Node Layer(Node root, Texture texture, float radius, float depth, BlendMode blend,
                              out Material skin)
    {
        skin = new Material
        {
            BaseColorTexture = texture,
            Unlit = true,
            Blend = blend,
            DepthWrite = false,
            Cull = CullMode.None
        };

        var node = new MeshNode(Card(StationScale * radius, "gate-card"), skin)
        {
            Position = new Vector3(0f, 0f, StationScale * depth),
            IsPickable = false,

            // On the card, not on the node it hangs from. RenderOrder is read off whichever node
            // produced the draw and is not inherited — setting it on a parent looks like it groups a
            // subtree and does nothing at all. Left at the default, the gate drew before the station's
            // panel lines at 1, so the edges of the fittings in the hall were drawn over the shut door
            // and the closed gate had a wireframe of the room behind it printed across it.
            RenderOrder = GateOrder
        };

        root.Children.Add(node);
        return node;
    }

    /// <summary>Above the panel lines at 1, so the gate covers the room it is closing off.</summary>
    private const int GateOrder = 2;

    /// <summary>
    /// A square facing +Z with its texture across it once, which is all a gate layer is — and, translated
    /// and merged, all a plume disc is either.
    ///
    /// Not <see cref="Primitives.Plane"/>: that one lies in XZ facing up, and the rotation that stands it
    /// up also swaps which way v runs, so the gate would scroll sideways instead of inward.
    /// </summary>
    private static Mesh Card(float radius, string name) => new()
    {
        Positions =
        [
            new(-radius, -radius, 0f), new(radius, -radius, 0f),
            new(radius, radius, 0f), new(-radius, radius, 0f)
        ],
        Normals = [Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ],
        TexCoords = [new(0f, 0f), new(1f, 0f), new(1f, 1f), new(0f, 1f)],
        Indices = [0, 1, 2, 0, 2, 3],
        Name = name
    };

    /// <summary>Puts a detail map and its height field on a material, either of them optional.</summary>
    private static void Skin(Material material, Texture? albedo, Texture? bump, float bumpScale)
    {
        if (albedo != null)
            material.BaseColorTexture = albedo;

        material.BumpTexture = bump;
        material.BumpScale = bumpScale;
    }

    /// <summary>
    /// Where each model's bells are, and how wide. Read off the <c>bell.*</c> tubes in
    /// <c>tools/models/build-models.py</c> and confirmed against the exported files rather than trusted:
    /// the radius is the tube's <c>radius_a</c>, which is the end at <c>−length/2</c> along the axis,
    /// which for a bell authored along −Y is the mouth.
    /// </summary>
    private static Nozzle[] Nozzles(string prefix) => prefix switch
    {
        "harrier" => [new(-0.128f, -0.022f, 0.545f, 0.050f), new(0.128f, -0.022f, 0.545f, 0.050f)],
        "raider" => [new(0f, 0f, 0.590f, 0.086f)],
        "kestrel" =>
        [
            new(-0.115f, -0.020f, 0.567f, 0.0546f),
            new(0f, -0.020f, 0.567f, 0.0735f),
            new(0.115f, -0.020f, 0.567f, 0.0546f)
        ],
        _ => throw new ArgumentException($"no bells known for '{prefix}'", nameof(prefix))
    };

    /// <summary>
    /// How long a jet is, as a fraction of the ship's length — not of the bell's radius.
    ///
    /// A raider's single bell is nearly twice the mouth of an escort's pair, and scaling the length off
    /// the radius gave it a jet longer than the ship it was pushing while the freighter's three ran off
    /// the bottom of the frame. How far a flame reaches is a property of the ship, not of one hole in it.
    /// </summary>
    private const float PlumeReach = 0.36f;

    /// <summary>How wide the flame card is, as a multiple of its bell's mouth radius. Width <i>is</i> the bell's.</summary>
    private const float PlumeSpread = 1.75f;

    /// <summary>And the disc, which has to hold a soft edge inside a square.</summary>
    private const float PlumeDisc = 1.35f;

    /// <summary>One ship, with everything bolted to it.</summary>
    public static Ship BuildShip(
        Node world, Texture glow, (Texture Plate, Texture Relief) plating,
        (Texture Flame, Texture Disc) exhaust, string file, string prefix,
        float length, Vector3 contactColor, Vector3[] path, float[] roll, float phase,
        Vector3[]? guns = null)
    {
        var model = Models.Load(file, length);

        var root = new Node();
        root.Children.Add(model.Root);
        world.Children.Add(root);

        // The same plating as the station, at a shallower relief: a ship's plates are smaller and its
        // seams are tighter, and the map is shared, so it costs one texture between all six hulls.
        var (plate, relief) = plating;

        Skin(model.Material(prefix + ".hull"), plate, relief, 2.2f);
        Skin(model.Material(prefix + ".trim"), plate, relief, 1.6f);

        model.Root.Children.Add(Panels(model.Edges, 0.26f));

        // Not a lamp and not attached to the hull's look — a marker for the ship as a contact. It
        // depth-tests like everything else and is stood off the hull each frame instead. See Ship.Mark.
        var contact = new SpriteNode
        {
            Texture = glow,
            Position = new Vector3(0f, length * 0.6f, 0f),
            Color = contactColor,
            Size = new Vector2(length * 0.30f),
            Opacity = 0f,
            Blend = BlendMode.Additive,
            DepthWrite = false,
            RenderOrder = 3
        };

        root.Children.Add(contact);

        var engines = model.Group(prefix + ".engine");

        var ship = new Ship
        {
            Root = root,
            Path = path,
            Roll = roll,
            Hull = model.Material(prefix + ".hull"),
            Engines = engines,
            Jet = BuildExhaust(root, Nozzles(prefix), length, Chroma(engines[0].EmissiveColor), exhaust),
            Lamps = model.Group(prefix + ".lamp"),
            Contact = contact,
            Length = length,
            Guns = guns ?? [],
            Phase = phase,

            // The pieces a breakup scatters are the nodes the model is already drawn from — one per
            // material group, plus the panel lines — so it costs no geometry and no extra draws.
            Pieces = [.. model.Root.Children],
            Skin = [.. model.Materials.Values]
        };

        ship.Rest();
        ship.Remember();

        // So a capture of the very first frame shows a ship under power. Everything after this is
        // driven by ContactScene.Dress, which runs before anything is drawn anyway — but a scene that
        // is only correct once it has been ticked is a scene with a wrong first frame in it.
        ship.Burn(1f);

        return ship;
    }

    /// <summary>
    /// The exhaust: a card standing on each bell, and one mesh of discs lying in the plane of them.
    ///
    /// Both are additive and unlit, and neither writes depth — a jet is light, and light does not
    /// occlude the light behind it. They do <i>test</i> depth, so a plume goes behind the hull of the
    /// ship in front of it, which is the half of the depth buffer that is still wanted here.
    ///
    /// One material for every card on the ship and one for every disc, so a three-engined freighter
    /// costs four draws of exhaust and not six: the discs share a mesh because they never move relative
    /// to each other, and the cards cannot because each one rolls about its own bell.
    /// </summary>
    private static Plume BuildExhaust(Node root, Nozzle[] nozzles, float length, Vector3 tint,
                                      (Texture Flame, Texture Disc) exhaust)
    {
        var flame = new Material
        {
            Name = "plume",
            BaseColorTexture = exhaust.Flame,
            Unlit = true,
            Blend = BlendMode.Additive,
            DepthWrite = false,
            Cull = CullMode.None
        };

        var disc = new Material
        {
            Name = "plume disc",
            BaseColorTexture = exhaust.Disc,
            Unlit = true,
            Blend = BlendMode.Additive,
            DepthWrite = false,
            Cull = CullMode.None
        };

        var cards = new Node[nozzles.Length];

        for (var i = 0; i < nozzles.Length; i++)
        {
            var bell = nozzles[i];

            cards[i] = new MeshNode(FlameCard(bell.Radius * length, length * PlumeReach), flame)
            {
                Position = new Vector3(bell.Across, bell.Rise, bell.Aft) * length,
                RenderOrder = 1,
                IsPickable = false
            };

            root.Children.Add(cards[i]);
        }

        var discs = new MeshNode(Discs(nozzles, length), disc)
        {
            RenderOrder = 1,
            IsPickable = false
        };

        root.Children.Add(discs);

        return new Plume { Cards = cards, Discs = discs, Flame = flame, Disc = disc, Tint = tint };
    }

    /// <summary>
    /// A bell's emissive turned into a plume's tint: the same hue, at full chroma.
    ///
    /// Two steps, and the second one is the interesting one. Scaling so the brightest channel is 1 gives
    /// the hue at the brightness the plume wants to set for itself. Then the whole thing is raised to a
    /// power, which deepens the colour without moving it — and it needs deepening, because additive light
    /// on black cannot be more saturated than what is added. An escort's bell is authored
    /// (0.28, 0.62, 1.00), and 28% red against 100% blue survives the trip out through the display's
    /// transfer curve as <i>55%</i> red against 100% blue, which is a pale blue-grey and reads as steam.
    /// At 1.8 the same hue arrives as 10% red against 100% blue, and the core still clips to white
    /// because the gain puts it over 1 — which is the arrangement wanted: a white-hot throat in a
    /// coloured jet, rather than a uniformly pale one.
    /// </summary>
    private static Vector3 Chroma(Vector3 color)
    {
        var peak = MathF.Max(color.X, MathF.Max(color.Y, color.Z));
        if (peak <= 1e-4f)
            return Vector3.One;

        color /= peak;

        return new Vector3(
            MathF.Pow(color.X, 1.8f), MathF.Pow(color.Y, 1.8f), MathF.Pow(color.Z, 1.8f));
    }

    /// <summary>
    /// One flame card: a quad standing on the nozzle, face up, running aft along +Z.
    ///
    /// Face up because that is the orientation <see cref="Plume.Face"/> rolls from, and aft along +Z
    /// because that is where a ship's stern is once the exporter has finished with it. v runs 0 at the
    /// throat to 1 at the tip, which is the way <see cref="Space.Plume"/> paints it.
    /// </summary>
    private static Mesh FlameCard(float radius, float reach)
    {
        var half = radius * PlumeSpread;

        return new Mesh
        {
            Positions =
            [
                new(-half, 0f, 0f), new(half, 0f, 0f),
                new(half, 0f, reach), new(-half, 0f, reach)
            ],
            Normals = [Vector3.UnitY, Vector3.UnitY, Vector3.UnitY, Vector3.UnitY],
            TexCoords = [new(0f, 0f), new(1f, 0f), new(1f, 1f), new(0f, 1f)],
            Indices = [0, 1, 2, 0, 2, 3],
            Name = "plume-card"
        };
    }

    /// <summary>
    /// Every bell's disc in one mesh: squares in the plane of the nozzles, a little aft of them.
    ///
    /// One mesh rather than one node each because they never move relative to one another — which is the
    /// exact condition <see cref="Mesh.Merge"/> asks for. The freighter's three bells are three draws that
    /// become one, and a raider's single bell falls straight back out of the merge as itself.
    /// </summary>
    private static Mesh Discs(Nozzle[] nozzles, float length) => Mesh.Merge(nozzles.Select(bell =>
    {
        var centre = new Vector3(bell.Across, bell.Rise, bell.Aft) * length;

        // A fifth of the mouth's width behind the rim. Flush with it, the disc and the bell's own end
        // cap are the same plane and fight over it from anywhere near side-on.
        centre.Z += bell.Radius * length * 0.20f;

        return Card(bell.Radius * PlumeDisc * length, "plume-disc")
            .Transformed(Matrix4x4.CreateTranslation(centre));
    }));

    /// <summary>
    /// The ore the whole thing is about: a train of containers slung under the freighter, each with its
    /// own edges and a marker light.
    ///
    /// They hang off the ship's own node, so they cost one transform between them however many there are —
    /// which is the argument for a scene graph, made in six lines.
    /// </summary>
    public static void AddCargo(Ship ship, (Texture Plate, Texture Relief) plating, int count)
    {
        // Subordinate to the hull on purpose. Three containers each a third of the ship long and a
        // quarter of it deep swallow the freighter they are slung under, and what reads then is a row
        // of boxes with a nose on it.
        var size = new Vector3(ship.Length * 0.26f, ship.Length * 0.19f, ship.Length * 0.24f);

        var pod = Primitives.Box(size.X, size.Y, size.Z);

        // The twelve edges of that same box. Any fold sharper than the default finds them, and there is
        // nothing else on a box to find — its face diagonals are flat and get dropped.
        var edges = pod.GetEdges();

        // The same plating as everything else. It used to be Space.Hull(), which is stored raw and
        // comes back around 0.04 once the renderer takes it from sRGB to linear — so whatever base
        // colour was multiplied into it, a pod came out black. A detail map has to sit near white to
        // be a detail map.
        var material = new Material
        {
            Name = "ore pod",
            BaseColorTexture = plating.Plate,
            BaseColor = new Vector4(0.62f, 0.56f, 0.46f, 1f),
            BumpTexture = plating.Relief,
            BumpScale = 1.8f,
            Roughness = 0.85f,
            Metallic = 0.15f,
            Cull = CullMode.Back
        };

        // One lamp material between all of them: the pods are identical and so are their marker lights,
        // and a material shared is a draw call shared.
        var marker = new Material
        {
            Name = "ore pod lamp",
            BaseColor = new Vector4(0.30f, 0.22f, 0.06f, 1f),
            // Dim. A marker light on a container is the least important lamp on the ship, and when it
            // was the brightest thing on it the eye read the middle of the hull as the front.
            EmissiveColor = new Vector3(0.85f, 0.52f, 0.12f),
            Unlit = false,
            Cull = CullMode.Back
        };

        var bulb = Primitives.Box(size.X * 0.16f, size.Y * 0.13f, size.X * 0.16f);

        for (var i = 0; i < count; i++)
        {
            // Into the cradle, not onto the belly. kestrel.glb carries a keel beam and four ribs, and
            // these sit in the three bays between them — which is why the spacing is 0.26 and why the
            // row is offset a tenth of a length aft: the ribs are at −0.480, −0.230, 0.030 and 0.285 in
            // the model, and a container floating a hand's breadth off a hull is the single thing that
            // most says "these were two different files".
            var z = ship.Length * (0.10f + ((count - 1) * 0.5f - i) * 0.26f);
            var centre = new Vector3(0f, -ship.Length * 0.22f, z);

            ship.Root.Children.Add(new MeshNode(pod, material) { Position = centre });

            ship.Root.Children.Add(new LineNode
            {
                Positions = edges,
                Color = new Vector3(0.70f, 0.76f, 0.80f),
                Opacity = 0.30f,
                Width = PanelWidth,
                Blend = BlendMode.Alpha,
                DepthWrite = false,
                RenderOrder = 1,
                Position = centre
            });

            ship.Root.Children.Add(new MeshNode(bulb, marker)
            {
                Position = centre + new Vector3(0f, size.Y * 0.55f, 0f)
            });
        }
    }

    /// <summary>
    /// Dust: what a station that has been worked for thirty years leaves in its own orbit.
    ///
    /// This is what a particle sprite is for, and the reason none of the lamps above is one. A mote is a
    /// thing with no shape of its own — it has a position and a brightness and nothing else — so a
    /// camera-facing quad is not standing in for geometry, it <i>is</i> the right primitive. A lamp is
    /// not that: a lamp is a fitting on a hull, it has a size and a place and an orientation, and the
    /// only honest way to draw one is as the object it is.
    ///
    /// Nothing here is attached to a model. The field is a slab of world the station happens to sit in,
    /// and it turns on its own.
    /// </summary>
    public static Node BuildDust(Node world, Vector3 centre, Texture glow)
    {
        var field = new Node { Position = centre };
        world.Children.Add(field);

        // A thin belt rather than a cloud, and faint. The first version of this was eight hundred motes
        // over five kilometres at half opacity, which from any distance reads as a second starfield
        // parked in front of the first one — and a viewer's eye goes to it instead of to the station.
        // Dust is meant to be noticed once and then stop being noticed.
        field.Children.Add(new PointsNode
        {
            Positions = Disc(260, 3_000f, 260f, seed: 11),
            Color = new Vector3(0.60f, 0.62f, 0.68f),
            Opacity = 0.24f,
            Size = 1.6f,
            SizeAttenuation = false,
            Blend = BlendMode.Additive,
            DepthWrite = false,
            RenderOrder = -3
        });

        // Six larger pieces catching the sun, small enough to read as tumbling debris rather than as
        // smudges on the lens.
        foreach (var position in Disc(6, 2_600f, 200f, seed: 12))
            field.Children.Add(new SpriteNode
            {
                Texture = glow,
                Position = position,
                Color = new Vector3(0.74f, 0.70f, 0.62f),
                Size = new Vector2(54f),
                Opacity = 0.16f,
                Blend = BlendMode.Additive,
                DepthWrite = false,
                RenderOrder = -2
            });

        return field;
    }

    /// <summary>The panel lines that go over a model, as one node however many segments there are.</summary>
    private static LineNode Panels(Vector3[] edges, float opacity) => new()
    {
        Positions = edges,
        Color = new Vector3(0.66f, 0.74f, 0.82f),
        Opacity = opacity,
        Width = PanelWidth,
        Blend = BlendMode.Alpha,
        DepthWrite = false,
        RenderOrder = 1
    };

    /// <summary>
    /// A shell of points at one radius: the starfield.
    ///
    /// This used to be nine lines with a <c>Random</c> in them and a comment explaining why the height
    /// is drawn uniformly rather than the angle. <see cref="Scatter.Direction"/> is that, and the
    /// explanation now lives where it can be read by somebody who is not looking at this file.
    /// </summary>
    public static Vector3[] OnSphere(int count, float radius, int seed)
    {
        var points = new Vector3[count];

        for (var i = 0; i < count; i++)
            points[i] = Scatter.Direction(i, seed) * radius;

        return points;
    }

    /// <summary>
    /// A flat-ish annulus of points, for a dust field in an orbital plane.
    ///
    /// Still written out, because a disc is not a sphere and the library has no opinion about one. The
    /// square root does for area what the cube root does for volume: without it the dust piles into the
    /// middle of the ring.
    /// </summary>
    public static Vector3[] Disc(int count, float radius, float thickness, int seed)
    {
        var points = new Vector3[count];

        for (var i = 0; i < count; i++)
        {
            var angle = Scatter.Value(i, seed) * MathF.Tau;
            var r = radius * (0.35f + 0.65f * MathF.Sqrt(Scatter.Value(i, seed + 1)));

            points[i] = new Vector3(
                MathF.Cos(angle) * r,
                (Scatter.Value(i, seed + 2) * 2f - 1f) * thickness,
                MathF.Sin(angle) * r);
        }

        return points;
    }

    /// <summary>A closed circle of unit radius in the XY plane, as endpoint pairs.</summary>
    public static Vector3[] Ring(int segments)
    {
        var points = new Vector3[segments * 2];

        for (var i = 0; i < segments; i++)
        {
            var a = i / (float)segments * MathF.Tau;
            var b = (i + 1) / (float)segments * MathF.Tau;

            points[i * 2 + 0] = new Vector3(MathF.Cos(a), MathF.Sin(a), 0f);
            points[i * 2 + 1] = new Vector3(MathF.Cos(b), MathF.Sin(b), 0f);
        }

        return points;
    }

}
