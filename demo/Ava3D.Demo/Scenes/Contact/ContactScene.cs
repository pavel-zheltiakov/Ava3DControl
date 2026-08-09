using System.Numerics;
using Ava3D.Demo.Textures;
using Avalonia.Media;

namespace Ava3D.Demo.Scenes.Contact;

/// <summary>
/// Sixty seconds with a script.
///
/// Ashfall is a mining world. Relay Nine hangs in low orbit over its terminator. The freighter Kestrel is
/// inbound with ore under escort by Harrier One and Two. Three raiders are waiting in the planet's shadow,
/// where a transponder does not carry. They come up under the convoy, the escorts break to meet them, two
/// raiders die and the third breaks off, and only then does the bay go green and Kestrel dock. Then the sun
/// clears the limb and it starts again.
///
/// This is the only scene in the demo split across more than one file, and deliberately so: the others
/// exist to be read in one sitting, and this one exists to be watched. <see cref="Flight"/> is how a ship
/// moves, <see cref="Fleet"/> is what everything is built from, <see cref="Effects"/> is the tracers and
/// the explosions, and <see cref="Shots"/> is the camera.
///
/// Every position in it is a pure function of the time through the cycle. Nothing integrates and nothing
/// accumulates, so the sixtieth second joins the first and the film is the same every time it runs — which
/// is what makes it usable as a test as well as a demonstration.
/// </summary>
public sealed class ContactScene : DemoScene
{
    private const float Duration = Flight.CycleSeconds;

    /// <summary>How long the film runs. Public because the story cuts to it and has to know how much of
    /// itself to give up to it.</summary>
    public const float Length = Duration;

    /// <summary>
    /// The lens, and the two clipping planes, named rather than buried in <see cref="Frame"/>.
    ///
    /// They are public for one reason: the story mounts this film as a chapter and therefore has to set
    /// the same three numbers, and a chapter that copied them would be a second place they could be
    /// wrong. There is no version of this scene that reads correctly at the building's near plane of five
    /// centimetres — depth resolution is set by that plane almost entirely, and a scene with a
    /// two-and-a-half-million-unit sky sphere in it would spend the whole of its depth buffer on the first
    /// metre.
    /// </summary>
    public const float Near = 5f;

    public const float Far = 3_000_000f;

    /// <summary>2·atan(224/420) ≈ 56.1°, the vertical field of view of the build this scale came from.</summary>
    public static readonly float Lens = 2f * MathF.Atan(224f / 420f) * 180f / MathF.PI;
    private const float CombatStart = 23.5f;
    private const float CombatEnd = 41f;
    /// <summary>
    /// Who dies, when they start taking hits, when they go, and how the wreck tumbles.
    ///
    /// Two of the three, and the third runs. That is not a detail of the story, it is the story: the
    /// bay does not go green while there is anything hostile still in the neighbourhood, so the last
    /// raider breaking off is what clears Kestrel to dock. At forty-five seconds the survivor is 4,800
    /// units out and opening — beyond gun range, beyond any range at which it is a threat, and visibly
    /// leaving rather than merely absent.
    ///
    /// Both kills sit inside a firing solution the escort actually holds — 2.20 seconds of the three
    /// Raider A spends taking hits, 1.10 of Raider B's 2.8 — and check-guns.py <i>asserts</i> that
    /// rather than reporting it. It used to be a sentence here and nothing else, which is worth
    /// nothing: rebuilding the flight model left Harrier One holding its solution ten seconds early
    /// and four kilometres away at the moment of the kill, and every summary line still looked healthy.
    /// </summary>
    private static readonly (int Raider, float Hit, float Kill, Vector3 Spin)[] Casualties =
    [
        (0, 33.0f, 36.0f, new Vector3(0.9f, 0.5f, 1.4f)),
        (1, 36.0f, 38.8f, new Vector3(-1.2f, 0.8f, 0.6f)),
    ];

    /// <summary>When the bay goes green. After the last raider has broken off and is well clear.</summary>
    private const float ClearedAt = 45f;

    /// <summary>
    /// How often somebody gets the trigger. Five armed ships share it in rotation, so this over five is
    /// each ship's rate of fire: at 0.11 s that is a shot every half second each, and with two guns
    /// alternating it reads as a burst rather than as a metronome.
    /// </summary>
    private const float ShotInterval = 0.11f;

    /// <summary>
    /// How far off its nose a ship will still take a shot, and how far a bolt is worth firing.
    ///
    /// Six degrees is generous for a fixed gun and mean enough to matter: at two kilometres it is a
    /// two-hundred-metre basket, so a fighter has to be pointing very nearly at something to fire at
    /// all. The range is set by the bolt — 4,600 units a second over a 1.15-second life is 5,300, and
    /// firing at anything past four thousand is firing at where somebody used to be.
    /// </summary>
    private const float GunCone = 6f * MathF.PI / 180f;

    private const float GunRange = 4_000f;

    /// <summary>
    /// Where the two escorts sit in Kestrel's frame while they are flying with it, and when they leave.
    ///
    /// Nothing here reads these — they are the input to <c>tools/models/fly-paths.py</c>, which bakes
    /// the slot into the escorts' first eighteen waypoints and flies the rest. They are written down
    /// here because they are a decision about the film rather than about the tool.
    /// </summary>
    private static readonly Vector3[] Slots = [new(-680f, -70f, 420f), new(700f, 50f, 480f)];

    private const float BreakAt = 22f;

    /// <summary>
    /// What a <see cref="PointLight.Intensity"/> of 1 has to be to mean anything in a scene this size.
    ///
    /// <see cref="PointLight.Decay"/> is three.js's, so a decay of 2 is real inverse-square: the falloff
    /// is 1/d² with d in whatever unit the scene is written in. This one is written in units of about a
    /// metre — the station is 620 across, a fighter 250 long, the fight four kilometres wide — so a lamp
    /// on the station lighting a wall 300 units away is divided by ninety thousand. Every point light in
    /// this film was authored at an intensity between 1 and 26 and every one of them was therefore
    /// contributing about a hundred-thousandth of a unit of radiance. They were switched on, they were
    /// the right colour, they moved to the right places, and not one of them ever lit anything.
    ///
    /// Nothing is wrong with the falloff — inverse-square in metre-sized units simply needs candela-sized
    /// numbers, which is what a real luminaire is quoted in. So intensities here are written as a
    /// brightness times this constant, and the brightness is the thing worth reading: 1.4 means "as
    /// bright as a white matte wall 300 units away facing it".
    /// </summary>
    private const float Candela = 300f * 300f;

    private static readonly Vector3 StationPosition = new(2_800f, 150f, -3_200f);

    private Node _world = null!;
    private Node _planet = null!;
    private Node _dust = null!;
    private Material[] _lamps = [];
    private Material _bay = null!;
    private Material _collar = null!;
    private GateField _gateField = null!;

    private readonly DirectionalLight _sun;
    private readonly DirectionalLight _bounce;
    private readonly PointLight _flash;
    private readonly PointLight _gate;
    private readonly EnvironmentLight _sky;

    private Effects _effects = null!;
    private Ship _kestrel = null!;
    private Ship[] _escorts = [];
    private Ship[] _raiders = [];

    private Shot[] _shots = [];

    private float _lastCycle;

    /// <summary>How much of a second the last <see cref="Advance"/> covered. Held rather than passed
    /// because the two halves of a frame are called separately — see <see cref="Aim"/>.</summary>
    private float _step;

    private float _nextShotAt;
    private int _shotCount;

    /// <summary>
    /// A round that is going to land: which ship, from which direction, and at what point on the clock.
    ///
    /// The direction is the interesting field. A flash at the centre of a hull is a flash on whichever
    /// side of it the camera happens to be, which for most of the fight is the far one — light landing
    /// on the back of a ship from a gun in front of it. Carrying the bearing the round arrived on is
    /// what lets the flash be put where it belongs.
    /// </summary>
    private readonly record struct Impact(Ship Target, Vector3 Along, float At);

    private readonly List<Impact> _inbound = [];

    private string? _caption;

    /// <summary>
    /// The lighting rig, built before anything it lights.
    ///
    /// It is in the constructor rather than in <see cref="Build"/> because none of it depends on the
    /// geometry — the sun's bearing, the planet's, the station's and the docking bay's are all constants
    /// of the setting rather than of the scene graph — and because the story needs to hand these four to
    /// a room the moment it cuts here, which is before this film has been asked to build anything.
    /// </summary>
    public ContactScene()
    {
        _sun = new DirectionalLight
        {
            Direction = -Fleet.SunDirection,
            Color = new Vector3(1.00f, 0.95f, 0.87f),
            Intensity = 2.1f,
            // Tinted rather than grey: a neutral lift reads as fog, and a lift in the scene's own sky
            // colour reads as light somebody forgot to model.
            AmbientColor = new Vector3(0.50f, 0.56f, 0.66f),
            Ambient = 0.13f
        };

        // The planet bouncing sunlight back at whatever is near it: a second key, from the planet's
        // bearing, which is a thing one light could never express.
        _bounce = new DirectionalLight
        {
            Direction = Vector3.Normalize(-Fleet.PlanetCentre),
            Color = new Vector3(0.62f, 0.80f, 0.74f),
            Intensity = 0.85f
        };

        // Both of the point lights below are driven in units of Candela — see the constant. Written
        // plainly they were off by five orders of magnitude and looked, at a glance, entirely reasonable.

        _flash = new PointLight
        {
            Position = Vector3.Zero,
            Color = new Vector3(1.00f, 0.80f, 0.45f),
            Intensity = 0f,
            Range = 2_600f,
            Decay = 2f
        };

        // In the plane of the mouth rather than back inside it. Inset, the throat wall nearest the lamp
        // took most of the light and the door around the opening — the surface a pilot actually reads —
        // barely changed; in the plane it lights both, because half of what it throws goes down the tube
        // and half falls across the face. The range is about twice the station's radius: far enough to
        // wrap the arms, short enough that nothing out in the fight is lit by the dock.
        _gate = new PointLight
        {
            Position = StationPosition + Fleet.BayMouth,
            Color = new Vector3(1.00f, 0.13f, 0.06f),
            Intensity = 0.25f * Candela,
            // Short. The station is 1,200 units across and this light sits at one end of it, so a range
            // of 1,500 reached the far cap, the pods and both wings: the whole relay went red, which is
            // a station on fire rather than a bay that is shut. 820 wraps the mouth and the two docking
            // arms and stops before the drum.
            Range = 820f,
            Decay = 2f
        };

        _sky = new EnvironmentLight
        {
            SkyColor = new Vector3(0.09f, 0.11f, 0.16f),
            GroundColor = new Vector3(0.04f, 0.05f, 0.07f),
            Intensity = 1f
        };
    }

    /// <summary>
    /// All four lights this film may hold: the sun, a bounce off the planet, one that belongs to whatever
    /// is exploding, and the docking gate.
    ///
    /// The last two are the interesting ones, and they are both ranged point lights because that is the
    /// only kind of light whose contribution is exactly zero past a distance — which is what lets a film
    /// have local light without every hull in it knowing.
    ///
    /// The flash rides the newest tracer and jumps to a detonation when there is one, so a shot lights
    /// the hull it passes and nothing else.
    ///
    /// The gate sits in the mouth of the docking bay and carries the bay's state as *light* rather than
    /// as paint. The collar and the six lamps are <see cref="Material.Unlit"/>, which is right for a lamp
    /// lens — it puts the colour on the screen unchanged instead of letting the tone map wash it out —
    /// but an unlit surface by definition throws nothing onto anything else, so without a light there the
    /// door around it stayed the same grey whether the bay was open or shut. This one fills the throat,
    /// spills red or green across the surround and the near arms with a falloff no emissive can imitate,
    /// and puts the station's answer on Kestrel's own hull as it slides in.
    ///
    /// It also, for most of this scene's life, did none of those things. It was authored at intensity
    /// 3.2, which under real inverse-square at station scale is a falloff of about six millionths — the
    /// light was on, the right colour, in the right place, and contributing nothing that would survive
    /// being written down to four decimal places. Everything the paragraph above claims was being done by
    /// emissive alone. See <see cref="Candela"/>: the falloff was never wrong, the units were.
    ///
    /// Exposed as an array because it is exactly the four a scene is allowed, and because the story hands
    /// them straight to <c>Hall.Use</c> — a film that needed a fifth could not be cut to at all.
    /// </summary>
    public Light[] Lights => [_sun, _bounce, _flash, _gate];

    /// <summary>The bounce that has no direction: what fills the side of a hull the sun is not on.</summary>
    public EnvironmentLight Sky => _sky;

    public override string Title => "Contact";

    public override string Summary => "Sixty seconds with a story, a shot list and a camera that flies itself";

    public override string Notes =>
        """
        A film rather than a turntable. Seven shots, sixty seconds, on a loop: the planet at night, the
        convoy inbound, three contacts out of the shadow, a pass over Harrier One's shoulder, a kill, a
        docking, and the sun coming over the limb.

        Everything the last ten scenes showed one at a time is here doing a job. The stars are a
        PointsNode at a fixed two pixels. The tracers and the fireball are additive SpriteNodes. The sky
        is an unlit sphere with its front faces culled. The planet's cities are masked to its night side
        and its relief is a bump map. The sun's disc is unlit, so it stays a flat circle instead of
        becoming a lit ball.

        The four ships and the station are glTF, loaded from .glb through GltfLoader. They were modelled
        for this demo — tools/models/build-models.py is the Blender script that makes them, it runs
        headless in a second, and all four together are 175 KB and 2,200 triangles. Their material names
        are how the film reaches into them: relay.lamp.0 through relay.lamp.5 are the docking lamps it
        chases, kestrel.engine is the throttle, raider.hull is what lights up from inside when one is
        being shot at.

        Every lamp on every hull is geometry with an emissive material, and not one of them is a sprite.
        A billboard standing in for a lamp is convincing at two kilometres and a smear at twenty metres,
        and shot four flies the camera to twenty metres. What a sprite is right for is a thing with no
        shape of its own, which is why the dust round the station is one and the fireball is one.

        The one sprite on a ship is its contact marker, and it is not pretending to be a lamp. Its opacity
        ramps in with range — nothing inside eight ship lengths, everything past forty — so you never see
        it at a range where you can see the ship. That is the case emissive geometry genuinely cannot
        cover: once a hull is sub-pixel there is nothing left to light.

        It has to be in front of one object and behind every other one, and there is no flag for that. For
        a while it was drawn with the depth test off, which is the same thing right up until something
        else gets between the camera and the fleet — and in the film something does, because the last
        chapter watches ships through a window with a metre of hull round it. Markers ignoring depth is
        markers painted on the wall of the room you are standing in. So it depth-tests like everything
        else and is stood off six tenths of a ship length toward the camera instead. See Ship.Mark.

        A two-sided material is not an inside. The docking bay was one tube drawn two-sided, which is the
        obvious way to make the far wall of a hole draw at all — and it is a trap, because both GPU
        backends turn a two-sided normal round to face whoever is looking at it. The wall at the back of
        the bay was therefore handed a normal aimed at the camera, and the key light lit it as though the
        sun were shining down a pipe it cannot reach. A tube is seen almost entirely at a grazing angle,
        Fresnel goes to 1 at grazing incidence, and a dark wall came back as a white mirror whose bright
        facets moved when the camera moved. The bay is now two walls — one facing out, one facing in —
        which costs sixteen triangles and behaves like a wall. Cull.None is for a surface with genuinely
        no inside, like a solar panel; it is not a shortcut for a hole.

        That fix then did nothing twice, because the builder was repairing it away. build-models.py ended
        its export with Blender's normals_make_consistent, which turns every shell in a joined mesh
        outward — a sensible safety net for a file full of hand-written index lists, and fatal to the one
        surface that is meant to face inward. The lining came out a solid cone, back-face culling threw
        away the wall the camera was looking at, and the sky showed through the door. The repair is gone;
        check-models.py now measures the signed volume of every part in the exported file instead, and
        knows which ones are supposed to be holes. A repair that cannot tell a mistake from an intention
        is not a safety net.

        The same checker asks whether the model is one object. Every part is a connected run of polygons,
        welded by position, and two parts count as joined if any of their triangles overlap — a BVH pair
        test, not a distance, because a truss buried a tenth of a unit into a drum has no vertex anywhere
        near the drum's skin. It found the station in seven pieces: all four corner platforms, both
        docking arms and the six gate lamps were flying in formation with a hull they never touched. From
        most angles that reads as a station. It reads as a broken one from the angles where the gap lines
        up with the sky, which is exactly how it was reported.

        The gate is six quads and three textures. A plate that fades out to open the bay, two copies of
        one swirl counter-rotating at rates with no common period, and three rings scaling out on offset
        thirds. It is not a strip of animation frames, and that is the same constraint as the vertex one
        arriving from a new direction: the texture caches release anything that did not appear in the
        frame just drawn, so a twenty-frame cycle swapped into a material would delete and re-upload
        nineteen textures every frame. Transforms and colours are free; data is not.

        Alpha layers draw in the order they were added, not by distance. SceneSnapshot sorts by render
        order and then by insertion, deliberately — a depth sort per frame is a cost every scene would pay
        for the few that need it — so the back-to-front order of the gate's stack is the order its nodes
        go into the tree. And RenderOrder is read off whichever node made the draw and is never inherited,
        so setting it on a parent looks like it groups a subtree and does nothing whatever.

        A sky sphere makes a texture's left edge meet its right, and value noise asked for u times six
        does not. u = 0 and u = 1 are unrelated numbers, so the nebula had a join down its prime meridian
        — and wrapped round the camera, a meridian is a great circle: a dead straight line clean across
        the frame at whatever angle the camera was holding, with the cloud stepping as it crossed. The
        step measured 0.138 against a largest ordinary neighbour step of 0.0067, which is twenty-one times
        the biggest change anywhere else in the image. The clouds are now sampled on a cylinder — the
        noise is fed cos and sin of the longitude, so u is genuinely periodic — and the meridian measures
        0.0141 against 0.0141. The planet had always been sampled that way; the sky, which is the thing
        you look at for the whole minute, had not.

        The exhaust is a card that rolls and a disc that does not, and it is two pieces because neither
        works alone. A flame is not a shape you can model once and look at from anywhere: from the beam it
        is a long tapering jet and from dead astern it is a bright disc. A cone was tried and failed for a
        reason worth writing down — additive shading counts the surfaces a ray crosses rather than the
        distance it travels through them, so a cone shell contributes exactly two samples everywhere
        inside its silhouette and arrives as a flat triangle of paint with a hard edge. So the card is
        rolled about the ship's thrust axis until it is broadside to the camera, which is the one degree
        of freedom a jet actually has, and the disc covers the angles where the roll has run out — which
        is where a real jet reads as a disc anyway. Its colour is taken off the bell's own emissive rather
        than chosen twice, raised to a power on the way, because additive light on black cannot be more
        saturated than what is added: 28% red against 100% blue comes out of the display's transfer curve
        as 55% red, which is pale grey-blue and reads as steam.

        A hit is booked rather than flashed. Bolts here are geometry that travels, so nothing in the scene
        knows the moment one arrives — the gunnery does, because a shooter with a firing solution is a
        shooter whose rounds are going to land, so the arrival goes in a list with the bearing it came in
        on and the clock reading it lands at. Both halves matter. A hit at four thousand units is most of
        a second in the air, and the flash used to beat its own bolt to the target by that whole second;
        and a flash at the ship's centre is drawn on whichever side of the hull the camera can see, which
        through the middle of a dogfight is the far one. It now lands on an ellipsoid roughly the shape of
        a hull, out along the round's own line — on the flank for a beam shot, on the nose for a head-on
        one.

        Point lights here are quoted in candela, because Decay = 2 is real inverse-square and this scene
        is written in units of about a metre. A lamp on the station lighting a wall three hundred units
        away is divided by ninety thousand, so an intensity that reads sensibly — 3.2, say — is a light
        that is switched on, the right colour, in the right place, and contributes about a hundred
        thousandth of a unit of radiance. All three point lights in this film were authored that way and
        not one of them ever lit anything; what looked like local lighting was emissive doing the whole
        job. Nothing was wrong with the falloff. A real luminaire is quoted in candela for the same
        reason, and the constant that converts is worth writing down where the numbers live.

        The panel lines are LineNodes, but nobody drew them. Models.Creases welds the mesh by position —
        a flat-shaded export shares no vertices at all — and keeps the edges where two faces fold by more
        than 65 degrees, plus the boundaries of open surfaces. The threshold is set against the geometry
        rather than by eye: a hull section here is a six- or eight-sided prism whose facets meet at 60 and
        45 degrees, and anything below those traces every one of them and gives you a wireframe.

        The ships are rigid bodies. Each has a mass, a moment of inertia about each axis, a main engine on
        the centreline and manoeuvring thrusters at the stern, and nothing anywhere rotates a hull: the
        autopilot works out the attitude it wants, the difference between that and the attitude the ship
        has is a quaternion, and the only thing it may do about it is ask the stern thrusters for a
        torque. What the nose does is whatever Euler's equation says it does. So what a ship can do
        follows from what it is — a Harrier swings its nose at 36 degrees per second squared and the
        freighter at 0.6, because of their masses and their lengths and not because anybody chose those
        numbers.

        A ship goes where it points. The main engines are the only source of thrust and they lie along the
        hull, so velocity is speed times the direction the ship faces and can be nothing else. There is no
        sideslip and no drift, because there is no second quantity that could disagree with the first: the
        model keeps a speed and an attitude and no velocity vector at all.

        The bank is a rigid body too, and every workaround it used to need is gone. It was a function of
        the path — speed squared over radius, through an arctangent, then convolved with a nine-tap window
        because a Catmull–Rom spline is C1 and a bank taken straight from curvature steps at every
        waypoint. The hull now really rolls, against a roll inertia that is a fiftieth of its pitch
        inertia because a hull is a rod and not a disc. That ratio is the physical reason aircraft bank
        into turns instead of skidding round them, and here it falls out of the shape of the ship. The
        curvature step is still in what the autopilot asks for and simply cannot reach the screen, because
        a torque cannot move a body's angle discontinuously. The filter was deleted and the smoothing got
        better.

        Because a rolling body has no closed form, the bank is integrated once, offline, and shipped in
        Paths.cs as a track. Nothing integrates at runtime, so the film is still a pure function of time
        and the same second of the loop looks the same every time round.

        There are no Euler angles and no inverse trigonometry left. Orientation was a yaw from atan2, a
        pitch from asin and a roll, composed in the one order that gives a bank rather than a skid; it
        worked, and it is still what the replacement is checked against. But Euler angles have an order to
        remember, a singularity to avoid, a wrap to handle, and no way to answer "how far is this attitude
        from that one" — which is what an autopilot asks every tick. Flight.Orient is two shortest-arc
        quaternions and costs no trigonometric call at all.

        Nobody flies through the station, nobody flies through anybody else, nobody flies backwards, and
        nothing jumps between frames — none of it taken on trust. tools/models/check-flight.py samples
        every path against Relay Nine's real mesh; check-motion.py walks the whole cycle at 120 fps and
        fails on an attitude that steps, a nose off its velocity, two consecutive legs that double back,
        or two hulls in the same place; check-models.py measures the exported hulls. The fighters clear
        the station over the top or under the belly. Kestrel is the one exception, down the docking axis
        from +Z, the one direction the station is open in.

        Keeping ships apart is two rules, and it has to be two. What a ship is attacking is held off by
        the miss distance its attack is flown to; everything else is held off by a bubble of half a
        hull's length plus its widest radius. Both are predictions of where the pass ends up rather than
        tests of how far away something is now — a rule that waits for range gives a hull with inertia no
        time to answer it — and the two are kept off each other's pairs, because two rules on one pair is
        two rules fighting. Applied to the same pair they cost the film two thirds of its gunfire.

        The guns are fixed. They sit in the wing roots and the underwing pods, they point where the hull
        points, and nothing traverses — so a ship fires along its own nose and the only question is
        whether the nose is on anything. It has to be inside six degrees, inside four kilometres, and
        aimed at the lead point rather than at the target, because a bolt takes most of a second to
        arrive. Off the beam there is no shot however much a pilot would like one.

        That makes the paths responsible for the fight — and it is why the five combat paths are not
        drawn at all. tools/models/fly-paths.py flies each ship as the rigid body above, steering at
        whatever it is trying to kill, and the waypoints are just where it got to every second and a
        quarter. Guns then work by construction, because steering at the lead point is pointing at the
        target. Hand-placed waypoints gave 0.2 seconds of firing solution across an eighteen-second
        engagement, with tracers leaving ships sideways, and two of the escort paths doubled back on
        themselves by 148 and 174 degrees to get there.

        Getting an autopilot to hold a nose on something is less obvious than it looks, and two attempts
        were wrong in ways worth keeping. A single PD from attitude error straight to torque saturates —
        a ninety-degree error asks a fighter for five times the torque it owns — so it degenerates into
        bang-bang control that overshoots and comes back; the fighters swung their noses at 95 degrees a
        second and lost every solution. And a proportional loop cannot follow a moving target at all
        without a standing error, because it needs an error to produce an output: against a raider
        crossing at 0.41 radians a second the lag was 14 degrees, and the guns' cone is 6. What works is
        a cascade — attitude error to a capped rate, rate error to a torque — with the line-of-sight rate
        handed forward, which is what a pilot leading a turn is doing.

        The fight also had to become a fight. A raider flies its run at the freighter and then breaks,
        because an attacker that carries straight on is impossible to shoot with a fixed gun — the aspect
        changes too fast for anything to hold a nose on it. It is the break that gives an escort its shot,
        which is why real gun kills happen in turns. tools/models/check-guns.py reports 21.9 seconds
        of firing solution spread over twelve shooter-and-target pairs, most of which nobody designed,
        and asserts that both ships the film kills were being shot at when they died.

        Most effects here move a transform rather than a vertex, because that is the cheap way and it is
        enough: a tracer is a fixed segment on a node that travels. The debris is the exception. Scaling a
        node moves every point at a speed proportional to how far out it already was, so the cloud can only
        stay the shape it was made; stepping the 220 points and calling InvalidateGeometry() gives each
        fragment its own speed and its own drag, and refills one buffer to do it.

        The camera belongs to the shot list here, and the demo leaves it there — orbit, pan and zoom are
        all switched off, because a film you can drag is a film you can end up watching from behind the
        planet. There is no camera roll either, in any shot: up is world +Y, which is what an orbit camera
        means and not something the film is choosing.

        Depth range: the sun's corona is nine hundred thousand units out, the sky sphere is at 2.4
        million, and the near plane is at 5. Six hundred thousand to one, no logarithmic depth buffer.
        """;

    public override SceneLook Look => SceneLook.Cosmic;

    public override bool Animates => true;

    public override bool FramesItself => true;

    public override bool DrivesCamera => true;

    public override string? Caption => _caption;

    public override TimeSpan TourDuration => TimeSpan.FromSeconds(Duration + 2);

    public override void Frame(Camera camera)
    {
        camera.FieldOfView = Lens;

        // Explicit, and they have to stay explicit — derived planes come from the scene radius, and a
        // scene with a 2.4-million-unit sky sphere in it would put the near plane kilometres out.
        camera.NearPlane = Near;
        camera.FarPlane = Far;

        camera.Target = Vector3.Zero;
        camera.Distance = 3_000f;
        camera.Yaw = 0f;
        camera.Pitch = 0f;
    }

    /// <summary>
    /// The sky, the star, the planet, the station and six ships — everything in the film that is not a
    /// light.
    ///
    /// It is <see cref="BuildSubject"/> rather than <see cref="Build"/> so that the story can cut to it:
    /// what comes back is one node with a world under it, and mounting a world in a building is the same
    /// operation as mounting a cube on a plinth. There is no floor to leave behind and no backdrop to
    /// strip, because in space there is nothing to stand on — which makes this the one scene in the demo
    /// where the subject and the whole scene are very nearly the same thing.
    /// </summary>
    public override Node? BuildSubject()
    {
        var glow = Space.Glow();

        // One plating map between the station and all six hulls. Built here rather than inside Fleet
        // because it is two 512-square textures and seven copies of it would be fourteen megabytes and
        // seven GPU uploads of the same thing.
        var plating = Space.Plating();

        // Likewise the exhaust. Six ships, twelve bells, two textures between the lot of them — the
        // tint that tells a raider's jet from an escort's comes off each bell's own emissive, not off a
        // texture, so there is nothing per-ship in the images to keep them apart.
        var exhaust = (Space.Plume(), Space.PlumeCap());

        _world = new Node { Name = "contact.world" };

        Fleet.BuildSky(_world);
        Fleet.BuildStars(_world);
        _planet = Fleet.BuildPlanet(_world);
        Fleet.BuildSun(_world, glow);

        // The station's node is discarded: it holds attitude, so nothing ever moves it again.
        (_, _lamps, _bay, _collar) = Fleet.BuildStation(_world, StationPosition, plating);
        _gateField = Fleet.BuildGate(_world, StationPosition,
            Space.PortalPlate(), Space.PortalSwirl(), Space.PortalRing());
        _dust = Fleet.BuildDust(_world, StationPosition, glow);

        var friendly = new Vector3(0.42f, 1.00f, 0.62f);
        var hostile = new Vector3(1.00f, 0.26f, 0.20f);

        // Hardpoints, in fractions of the ship's length in its own frame — read off the models. The
        // escorts carry a pair in the wing roots and the raiders a pair in the underwing pods. Kestrel
        // has none: it is a freighter, and that is the whole reason it needs an escort.
        Vector3[] harrierGuns = [new(-0.10f, -0.010f, -0.24f), new(0.10f, -0.010f, -0.24f)];
        Vector3[] raiderGuns = [new(-0.285f, -0.038f, -0.14f), new(0.285f, -0.038f, -0.14f)];

        _kestrel = Fleet.BuildShip(
            _world, glow, plating, exhaust, "kestrel.glb", "kestrel", 420f, friendly, Paths.Kestrel, Paths.KestrelRoll, 0f);
        Fleet.AddCargo(_kestrel, plating, count: 3);

        _escorts =
        [
            Fleet.BuildShip(_world, glow, plating, exhaust, "harrier.glb", "harrier", 250f, friendly, Paths.HarrierOne, Paths.HarrierOneRoll, 0f, harrierGuns),
            Fleet.BuildShip(_world, glow, plating, exhaust, "harrier.glb", "harrier", 250f, friendly, Paths.HarrierTwo, Paths.HarrierTwoRoll, 0f, harrierGuns)
        ];

        _raiders =
        [
            Fleet.BuildShip(_world, glow, plating, exhaust, "raider.glb", "raider", 215f, hostile, Paths.RaiderA, Paths.RaiderARoll, 0f, raiderGuns),
            Fleet.BuildShip(_world, glow, plating, exhaust, "raider.glb", "raider", 215f, hostile, Paths.RaiderB, Paths.RaiderBRoll, 0f, raiderGuns),
            Fleet.BuildShip(_world, glow, plating, exhaust, "raider.glb", "raider", 215f, hostile, Paths.RaiderC, Paths.RaiderCRoll, 0f, raiderGuns)
        ];


        _effects = new Effects(_world, glow);
        _shots = BuildShotList();

        _lastCycle = 0;
        _step = 0;
        _nextShotAt = CombatStart;

        return _world;
    }

    /// <summary>
    /// The lights and the black behind them: everything that is not an object.
    ///
    /// Called when the film is shown on its own and not when the story mounts it — the story hands the
    /// same four lights to the room it cuts to instead, which is what <see cref="Lights"/> is for. There
    /// is no floor and no backdrop, so this is the shortest stage in the demo and the only one that is
    /// nothing but a lighting rig.
    /// </summary>
    public override void Stage(Scene scene)
    {
        scene.Background = Color.FromRgb(3, 4, 8);

        scene.Lights.Clear();
        foreach (var light in Lights)
            scene.Lights.Add(light);

        scene.Environment = _sky;
    }

    public override void Update(Scene scene, Camera camera, double elapsed)
    {
        Advance((float)(elapsed % Duration));
        Aim(camera);

        scene.Invalidate();
    }

    /// <summary>
    /// Everything in the film that is not the camera, at <paramref name="cycle"/> seconds through it: the
    /// ships fly, the guns fire, the rounds land, and the station and the hulls are dressed.
    ///
    /// <b>It works out its own step</b> from the second it was last given, rather than being handed one.
    /// That is not tidiness — it is what makes the film mountable. The story does not thread a delta
    /// through its chapters and must not start, because a chapter that took one would be a chapter whose
    /// output depended on how the clock got to a number rather than on the number; here the two callers
    /// are a scene running on the compositor and a chapter running on a film clock, and neither has to
    /// know what the other does.
    ///
    /// A step that comes out negative is the clock going backwards — the end of a cycle joining its
    /// beginning, or somebody seeking — and it means the same thing in both cases: put the film back to
    /// its first frame. That is the whole of <see cref="Rewind"/> and it is why this scene can be jumped
    /// into at all.
    /// </summary>
    public void Advance(float cycle)
    {
        if (cycle < _lastCycle)
            Rewind();

        // Clamped at a tenth. Everything below is a pure function of the cycle except the tracers and the
        // explosions, and those are the two things that would teleport across a stall.
        _step = Math.Clamp(cycle - _lastCycle, 0f, 0.1f);
        _lastCycle = cycle;

        var u = cycle / Duration;

        FlyEveryone(u, cycle, _step);
        Shoot(cycle);

        // After the ships have moved, because a round lands on a hull where the hull is now — and after
        // the trigger, so a shot fired at point-blank range can land on the same frame it left.
        Land(cycle);

        Dress(cycle);
    }

    /// <summary>
    /// The half of a frame that needs to know where the camera is: the shot list, the effects that face
    /// it, the one light that follows the action, and the contact markers.
    ///
    /// Separate from <see cref="Advance"/> because the order inside a frame is load-bearing and the story
    /// splits a frame in two — it updates every chapter's world and then places the camera. The camera is
    /// aimed before the effects are advanced, because a shockwave ring is billboarded at it and aiming
    /// afterwards would face every ring at where the camera was on the previous frame.
    /// </summary>
    public void Aim(Camera camera)
    {
        Direct(camera, _lastCycle);

        _effects.Update(_step, camera.Position);
        AimTheLight();

        // Contact markers last, because they depend on where the camera ended up this frame rather than
        // where it was when the shot list ran.
        _kestrel.Mark(camera.Position);
        foreach (var escort in _escorts)
            escort.Mark(camera.Position);
        foreach (var raider in _raiders)
            raider.Mark(camera.Position);
    }

    /// <summary>Puts the film back to its first frame. Called when the cycle wraps.</summary>
    private void Rewind()
    {
        _effects.Reset();
        _nextShotAt = CombatStart;
        _shotCount = 0;

        // Anything still in the air belongs to the run that has just finished. Kept, it would land at a
        // clock reading a minute in the past and go off on the first frame of the next one.
        _inbound.Clear();

        foreach (var raider in _raiders)
        {
            raider.Revive();
            raider.Contact.Color = new Vector3(1.00f, 0.26f, 0.20f);
        }
    }

    private void FlyEveryone(float u, float cycle, float dt)
    {
        _kestrel.Follow(u);

        // The escorts are ordinary ships on ordinary paths. Their first eighteen waypoints happen to be
        // the formation slot on Kestrel, sampled at the knots by tools/models/fly-paths.py, so they fly
        // the wing without any of this code knowing that they are — which is the entire fix for two
        // motion bugs that survived every attempt to smooth them at the pose. See Flight.Follow.
        foreach (var escort in _escorts)
            escort.Follow(u);

        foreach (var raider in _raiders)
        {
            raider.Follow(u);
            raider.Tumble(dt);
        }

        // The planet turns, slowly, because a planet that does not is a painting. The dust field turns
        // the other way and much slower, which is the cheapest way to say the two are not one object.
        //
        // The station does not turn at all, and that is a decision rather than an omission: it has a
        // docking bay on one face, and a relay whose bay wanders off the traffic lane every forty
        // seconds is not a relay anybody would file an approach with. It holds attitude.
        _planet.Rotation = Quaternion.CreateFromYawPitchRoll(cycle * 0.012f, 0f, 0f);
        _dust.Rotation = Quaternion.CreateFromYawPitchRoll(cycle * -0.015f, 0f, 0f);
    }

    /// <summary>
    /// Who shoots at whom, and when.
    ///
    /// Nobody fires at a target. A gun is bolted to a hull, so a ship fires <i>along its own nose</i>,
    /// and the only question is whether the nose happens to be on something — which is
    /// <see cref="Ship.CanHit"/>, and which is decided by where the ship has flown itself. Off the
    /// beam there is no shot however much a pilot wants one; the answer is to turn, and turning is what
    /// the attack runs in <see cref="Paths"/> are for.
    ///
    /// The consequence is that the fight is quiet when the geometry is wrong and busy when two ships
    /// come round onto each other, which is what a fight looks like. Tracers used to leave every ship
    /// on schedule regardless of where it was pointing, and the giveaway was bolts departing sideways.
    ///
    /// Everything about it is still a function of the cycle clock rather than of a random number, so
    /// every run of the film is the same film — which matters when the thing is also used to compare
    /// three renderers frame by frame.
    /// </summary>
    private void Shoot(float cycle)
    {
        if (cycle < CombatStart || cycle > CombatEnd)
            return;

        while (_nextShotAt <= cycle)
        {
            _nextShotAt += ShotInterval;

            var index = _shotCount++;

            // One ship gets the trigger each tick, in rotation, so no ship can monopolise the fight and
            // the load is flat however many of them happen to have a solution at once.
            var friendly = index % 5 < 2;
            var shooter = friendly ? _escorts[index % 5] : _raiders[index % 5 - 2];

            if (!shooter.Alive)
                continue;

            // Whatever it is pointing closest to. Not an assigned target: a pilot shoots at what is in
            // front, and what is in front is a consequence of the last few seconds of flying.
            var target = Quarry(shooter, friendly);
            if (target == null)
                continue;

            // Two guns, alternating, so a burst walks across the target the way a real one does.
            var muzzle = shooter.Gun(index);
            var aim = shooter.Pose.Forward;

            // A little spread, from the shot number rather than from a random source. Nobody is meant
            // to hit with every round, and bolts stacked on one line read as a single thick beam. Three
            // channels of the one index, which is what channels are for: the shot number stays the shot
            // number instead of being multiplied by three to make room.
            aim += new Vector3(
                Scatter.Value(index, 0) - 0.5f,
                Scatter.Value(index, 1) - 0.5f,
                Scatter.Value(index, 2) - 0.5f) * 0.012f;

            _effects.Fire(
                muzzle, aim,
                friendly ? new Vector3(1.00f, 0.86f, 0.42f) : new Vector3(1.00f, 0.35f, 0.30f));

            // Every third shot lands. A bolt is geometry that travels, so nothing tells the scene when
            // one arrives — so the scene books the arrival: the range over the bolt's speed is when, the
            // aim is from where, and Land does the rest. Without it the engagement is light passing
            // between ships and never touching one.
            //
            // It used to flash the moment the trigger came down, at the point the target would be by the
            // time the round got there. Both halves of that were wrong on screen. A hit at four thousand
            // units arrives most of a second after it is fired, and the flash beat its own bolt to the
            // target by that whole second; and a flash at the ship's centre is drawn over whichever side
            // of the hull the camera can see, which through the middle of the fight is the far one.
            if (index % 3 == 0)
            {
                var flight = Vector3.Distance(muzzle, target.Pose.Position) / Effects.BoltSpeed;
                _inbound.Add(new Impact(target, Vector3.Normalize(aim), cycle + flight));
            }
        }

        // The kills, at fixed moments, so the camera can be pointing at them. Both are earned: the
        // escort that gets each one has a firing solution running through the second it dies, which
        // tools/models/check-guns.py reports and tools/models/fly-paths.py is what arranges.
        foreach (var (index, _, killAt, spin) in Casualties)
        {
            if (cycle < killAt || !_raiders[index].Alive)
                continue;

            var doomed = _raiders[index];

            // One big one and three small ones along the hull, a fraction apart in size and place, so
            // the ship comes apart rather than being replaced by a ball. They all start on the same
            // frame; what separates them is that the sparks are brief and the detonation is not.
            _effects.Detonate(doomed.Pose.Position, 760f);
            for (var k = -1; k <= 1; k++)
                _effects.Spark(
                    doomed.Pose.Position + doomed.Pose.Forward * (k * doomed.Length * 0.34f),
                    doomed.Length * (0.85f + 0.2f * k));

            doomed.Kill(doomed.Pose.Forward * 210f + new Vector3(60f, 40f, 0f), spin);
        }
    }

    /// <summary>
    /// The rounds whose flight time has run out: they land now, on the hull, on the side they came from.
    ///
    /// Where exactly is a ray from the middle of the ship out along the bearing the round arrived on,
    /// stopped at an ellipsoid roughly the shape of a hull — two thirds as wide as it is long and a
    /// quarter as deep. A sphere would do for the ships to be hit somewhere on their skin, but not for
    /// them to be hit somewhere <i>plausible</i>: these are flat, long things, and a sphere fitted to the
    /// length puts a beam-on hit out past the wingtip while a sphere fitted to the width buries a
    /// head-on hit a third of the way into the nose. The ellipsoid costs three divides and a square root
    /// and lands the flash on the flank when the shot came from the flank and on the nose when it came
    /// from ahead.
    ///
    /// A ship that dies before its rounds arrive simply drops them. There is nothing left to hit.
    /// </summary>
    private void Land(float cycle)
    {
        for (var i = _inbound.Count - 1; i >= 0; i--)
        {
            var round = _inbound[i];
            if (cycle < round.At)
                continue;

            _inbound.RemoveAt(i);

            var target = round.Target;
            if (!target.Alive)
                continue;

            // Back along the round's own line, in the hull's frame.
            var bearing = Vector3.Transform(-round.Along, Quaternion.Inverse(target.Pose.Rotation));
            var axes = new Vector3(0.34f, 0.11f, 0.46f) * target.Length;

            var scale = 1f / MathF.Sqrt(
                bearing.X * bearing.X / (axes.X * axes.X) +
                bearing.Y * bearing.Y / (axes.Y * axes.Y) +
                bearing.Z * bearing.Z / (axes.Z * axes.Z));

            _effects.Spark(
                target.Pose.Position + Vector3.Transform(bearing * scale, target.Pose.Rotation),
                target.Length * 0.30f);
        }
    }

    /// <summary>
    /// The best target <paramref name="shooter"/> currently has a shot at, or null for none.
    ///
    /// "Best" is the one furthest inside the cone rather than the nearest, because a nose that is nearly
    /// on something is what a pilot is trying to achieve and a target that happens to be close but off
    /// the beam is not a shot at all.
    /// </summary>
    private Ship? Quarry(Ship shooter, bool friendly)
    {
        var hostiles = friendly ? _raiders : _escorts;

        Ship? best = null;
        var bestAlignment = -1f;

        foreach (var candidate in hostiles)
            Consider(candidate);

        // The raiders are here for the ore, so the freighter is a target for them too — and being slow
        // and enormous, it is the one they are most often lined up on.
        if (!friendly)
            Consider(_kestrel);

        return best;

        void Consider(Ship candidate)
        {
            if (!shooter.CanHit(candidate, GunCone, GunRange, Effects.BoltSpeed, out var lead))
                return;

            var alignment = Vector3.Dot(
                shooter.Pose.Forward, Vector3.Normalize(lead - shooter.Gun(0)));

            if (alignment <= bestAlignment)
                return;

            bestAlignment = alignment;
            best = candidate;
        }
    }

    /// <summary>The things that change colour or brightness without moving: lamps, engines, a dying hull.</summary>
    private void Dress(float cycle)
    {
        // The station's lamps say whether the bay is safe to fly into: red while there is a fight going
        // on outside it, green once Kestrel is cleared. They chase round the mouth either way, which is
        // what a lamp ring is for — it tells you which way the slot runs — but a red chase reads as a
        // warning and a green one as an invitation, and that is the whole message of the last act.
        //
        // They are emissive geometry, so what changes is a material's EmissiveColor and not a sprite's
        // opacity — which is the difference between a lamp that dims and a billboard that fades. The
        // values run past 1 on purpose: a lit lamp is meant to clip, because a real one does. And they
        // are saturated on purpose too — the housings are nearly black in the model, so nothing but the
        // emissive decides the colour and a green stays green instead of washing out to mint.
        var cleared = cycle > ClearedAt;
        var idle = cleared ? new Vector4(0.02f, 0.42f, 0.09f, 1f) : new Vector4(0.44f, 0.015f, 0.01f, 1f);
        var burning = cleared ? new Vector4(0.10f, 1.00f, 0.26f, 1f) : new Vector4(1.00f, 0.06f, 0.02f, 1f);

        for (var i = 0; i < _lamps.Length; i++)
        {
            var phase = (cycle * 1.6f - i * 0.5f) % _lamps.Length;
            var lit = phase is >= 0f and < 1f ? 1f - phase : 0f;

            _lamps[i].BaseColor = Vector4.Lerp(idle, burning, lit);
        }

        // And the doorway takes the colour, which is what makes the state readable from the range the
        // film actually watches the station at. Two surfaces, not one: the collar carries the signal at
        // full strength and the throat lining only takes a wash of it. A tube lit uniformly to the signal
        // colour has no shading left to read its shape by, and comes out as a flat disc — which is
        // exactly what happened the first time the gate light was given a working intensity, because the
        // emissive floor underneath it had been set high enough to carry the throat on its own.
        _collar.BaseColor = cleared
            ? new Vector4(0.06f, 0.80f, 0.20f, 1f)
            : new Vector4(0.82f, 0.05f, 0.02f, 1f);

        // A wash, not a light source — and now a third of what it was. It was carrying the whole throat
        // on its own while the gate light was contributing nothing measurable; with a light in there that
        // actually reaches, the same emissive floor was enough to flatten the tube into a disc. It now
        // does only the job it is for: keeping the far wall off pure black where the falloff has run out.
        _bay.EmissiveColor = cleared
            ? new Vector3(0.004f, 0.024f, 0.007f)
            : new Vector3(0.030f, 0.004f, 0.003f);

        // And the gate light, which is what turns all of that from paint into lighting. It breathes
        // rather than sitting still — a warning light that does not move is a sticker — and it breathes
        // faster while the bay is shut, which is the only difference between the two states that is not
        // just hue.
        //
        // The intensity is set by the brightest thing the light can reach rather than by the darkest.
        // It sits a few dozen units off the mouth plating, which is ordinary light-grey hull, and at
        // inverse-square that near a surface it takes very little to drive it to white — the throat came
        // back as pale mint, which is the same wash-out the lamps were made Unlit to escape, arriving by
        // the other road. Lighting the dark bay wall properly and blowing out the plate beside it is not
        // a compromise worth making: the wall has an emissive floor and the plate does not.
        _gate.Color = cleared
            ? new Vector3(0.16f, 1.00f, 0.32f)
            : new Vector3(1.00f, 0.11f, 0.04f);

        // The green is dimmer than the red for the same reason a green laser looks stronger than a red
        // one at equal power: most of the eye's luminance response sits under it. Matched by number,
        // "cleared" read a stop and a half hotter than "shut".
        _gate.Intensity = Candela * (cleared
            ? 0.17f + 0.03f * MathF.Sin(cycle * 2.2f)
            : 0.25f + 0.08f * MathF.Sin(cycle * 6.5f));

        DressTheGate(cycle);

        // Engines flicker; Kestrel's cut once it is in the slot.
        var docked = Math.Clamp((cycle - 52f) / 3f, 0f, 1f);
        Flicker(_kestrel, cycle, 1f - docked);

        foreach (var escort in _escorts)
            Flicker(escort, cycle, 1f);

        foreach (var raider in _raiders)
            Flicker(raider, cycle, 1f);

        // A doomed raider takes hits for a few seconds before it goes: the hull lights up from inside
        // and its transponder washes out from red to white.
        foreach (var (index, hitStart, killAt, _) in Casualties)
        {
            var raider = _raiders[index];
            if (cycle < hitStart || cycle >= killAt || !raider.Alive)
                continue;

            var hurt = (cycle - hitStart) / (killAt - hitStart);
            var pulse = 0.5f + 0.5f * MathF.Sin(cycle * 26f);

            raider.Hull.EmissiveColor = raider.HullRest + new Vector3(0.85f, 0.24f, 0.07f) * hurt * pulse;

            raider.Contact.Color = Vector3.Lerp(
                new Vector3(1.00f, 0.26f, 0.20f), new Vector3(1.00f, 0.95f, 0.90f), hurt);
        }

        // The ship's length doubles as its phase. Every ship in the scene is a different length, so no two
        // sets of engines flicker in unison — which is the whole point, and cheaper than carrying a seed.
        static void Flicker(Ship ship, float cycle, float level)
        {
            ship.Burn(level * (0.74f + 0.26f * MathF.Sin(cycle * 11f + ship.Length)));
            ship.Beacon(0.45f + 0.55f * (0.5f + 0.5f * MathF.Sin(cycle * 2.2f + ship.Length)));
        }
    }

    /// <summary>
    /// The gate: a barrier when the bay is shut, a doorway when it is not.
    ///
    /// Shut, the field runs at alpha 0.94 and you cannot see past it, which is the only honest way to
    /// draw a closed door — a bright red glow you can see the room through is a window with a light on.
    /// Cleared, it drops to 0.22 and the hall comes through it, and that transition is what the shot at
    /// 45 seconds is of. The ring on the outside stays at full strength either way: the signal is the
    /// ring, the field is the door, and they are two different statements.
    ///
    /// Nothing in here writes a vertex. Two swirls counter-rotate — 0.55 and −0.31 radians a second,
    /// which share no common period, so the pattern they make together never comes back round — and
    /// three rings scale out and fade on thirds of a phase. Everything else is a colour.
    /// </summary>
    private void DressTheGate(float cycle)
    {
        // Half a second to open, and it wants to be a moment rather than a frame: the lamps have been
        // chasing amber for eleven seconds by now and the audience is watching them.
        var open = Math.Clamp((cycle - ClearedAt) / 0.5f, 0f, 1f);
        open *= open * (3f - 2f * open);

        var tint = Vector3.Lerp(new Vector3(1.00f, 0.15f, 0.07f), new Vector3(0.24f, 1.00f, 0.46f), open);

        // The door goes; the field stays. Shut, the plate is opaque and the hall behind it does not
        // exist as far as the camera is concerned. Cleared, it is gone entirely and what is left is
        // the swirl at a quarter strength — a curtain of light you can see the room through, which is
        // the difference between a gate that has opened and a gate that has changed colour.
        _gateField.PlateSkin.BaseColor = new Vector4(tint * (0.45f + 0.55f * (1f - open)), 1f - open);

        var body = 0.62f - 0.34f * open;

        // The two layers do not carry the same weight. Index 1 is the near one and is the field itself;
        // index 0 sits deeper at half the alpha, and that is what gives it a thickness rather than a
        // face. They turn opposite ways at rates with no common period.
        for (var i = 0; i < _gateField.Swirls.Length; i++)
        {
            var spin = i == 0 ? -0.31f : 0.55f;
            _gateField.Swirls[i].Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, cycle * spin);
            _gateField.SwirlSkins[i].BaseColor = new Vector4(tint, body * (i == 0 ? 0.55f : 1f));

            // A slow breath, and out of step between the layers so the two never pulse together.
            var breathe = 1f + 0.02f * MathF.Sin(cycle * (i == 0 ? 2.3f : 1.7f));
            _gateField.Swirls[i].Scale = new Vector3(breathe, breathe, 1f);
        }

        for (var i = 0; i < _gateField.Rings.Length; i++)
        {
            var phase = (cycle * 0.5f + i / 3f) % 1f;

            // Out from a fifth of the mouth to the whole of it, brightest halfway, gone at either end.
            // Fading in as well as out matters: a ring that appears at full strength in the middle of
            // the door reads as a pop, and this is meant to look like something rising through it.
            var reach = 0.20f + 0.80f * phase;
            _gateField.Rings[i].Scale = new Vector3(reach, reach, 1f);
            _gateField.RingSkins[i].BaseColor = new Vector4(tint, 0.85f * phase * (1f - phase) * 4f);
        }
    }

    /// <summary>Parks the one point light on whatever is currently the brightest thing.</summary>
    private void AimTheLight()
    {
        if (_effects.Flashpoint is { } point)
        {
            _flash.Position = point;
            _flash.Color = _effects.FlashColor;
            _flash.Intensity = 0.9f * Candela * _effects.FlashStrength;
            _flash.Range = 1_200f + 2_400f * _effects.FlashStrength;
            return;
        }

        // Nothing firing: a running light on Kestrel, which keeps the light doing something rather than
        // switching a quarter of the lighting rig on and off.
        _flash.Position = _kestrel.Pose.Position;
        _flash.Color = new Vector3(0.62f, 0.78f, 1.00f);
        // Kestrel's own hull is a few dozen units from this, not three hundred, so the brightness that
        // reads as a running light is far under one: inverse-square cuts both ways.
        _flash.Intensity = 0.03f * Candela;
        _flash.Range = 900f;
    }

    /// <summary>Runs the shot list: finds the shot this second belongs to, aims, and sets the caption.</summary>
    private void Direct(Camera camera, float cycle)
    {
        var shot = _shots[^1];
        foreach (var candidate in _shots)
        {
            if (cycle < candidate.End)
            {
                shot = candidate;
                break;
            }
        }

        var span = MathF.Max(shot.End - shot.Start, 0.001f);
        shot.Aim(camera, Math.Clamp((cycle - shot.Start) / span, 0f, 1f));

        // Captions hold for four seconds and then get out of the way.
        _caption = cycle - shot.Start < 4f ? shot.Caption : null;
    }

    /// <summary>
    /// The shot list.
    ///
    /// Data, not a switch: each entry knows when it runs, what it says, and how to aim. Chase and orbit
    /// shots read the live pose of whatever they are following rather than duplicating its path, so a
    /// change to how a ship flies cannot leave the camera pointing where it used to be.
    /// </summary>
    private Shot[] BuildShotList() =>
    [
        // 1 · Nightside. A slow push toward Ashfall while its cities are still the brightest thing on it.
        new(0f, 10f, "Ashfall, nightside. Relay Nine, 04:12 station time.", (camera, u) =>
            camera.LookFrom(
                Vector3.Lerp(new Vector3(3_400f, 1_900f, 5_200f), new Vector3(1_500f, 900f, 2_600f), Ease.InOut(u)),
                Fleet.PlanetCentre)),

        // 2 · Convoy. Locked to Kestrel's own frame, easing from astern to alongside.
        //
        // Locked means rolled, too. The eye has always been in the freighter's frame; now the horizon is
        // as well, so the long banked turn reads as the convoy turning rather than as the sky sliding past
        // three ships that happen to be tilted.
        new(10f, 22f, "Convoy Kestrel, inbound. Two escorts.", (camera, u) =>
        {
            var eased = Ease.InOut(u);
            var eye = Shots.Rider(_kestrel.Pose,
                Vector3.Lerp(new Vector3(-260f, 620f, 2_600f), new Vector3(1_500f, 330f, 1_400f), eased));

            camera.LookFrom(eye, _kestrel.Pose.Position + _kestrel.Pose.Forward * 300f);
            camera.RollToward(Vector3.Transform(Vector3.UnitY, _kestrel.Pose.Rotation));
        }),

        // 3 · Contact. Wide and slowly drifting, so the shape of the engagement reads before it is cut up.
        //
        // Level, and every shot below it is. A roll belongs to a camera riding something; on an orbit or a
        // crane it is just a tilted picture, and the reset matters because Roll is state that survives a cut.
        new(22f, 28f, "Three contacts, out of the shadow.", (camera, u) =>
        {
            var centre = (_kestrel.Pose.Position + _raiders[0].Pose.Position) * 0.5f;
            camera.Roll = 0f;
            camera.LookFrom(Shots.Around(centre, 3_100f, 0.55f + u * 0.30f, 1_250f), centre);
        }),

        // 4 · The pass. Over Harrier One's shoulder, onto the raider it is shooting at.
        //
        // It ends two seconds before the kill, and that gap is the whole point of where it ends. The cut
        // used to fall on 36.0 s and Raider A used to die on 36.0 s, so the ship blew up on the same
        // frame the camera jumped: the viewer sees it hit in one framing and sees the fireball in
        // another, and reads that as the explosion happening somewhere else. Cut first, then kill —
        // the audience is already settled in the new angle when it goes.
        // The scissor is where the bank is worth the most: the escort rolls hard onto its new heading, and
        // riding that roll is the difference between watching a dogfight and being in one.
        new(28f, 34f, "Harrier One, engaging.", (camera, u) =>
        {
            var eye = Shots.Rider(_escorts[0].Pose,
                Vector3.Lerp(new Vector3(280f, 150f, 780f), new Vector3(-220f, 210f, 900f), Ease.InOut(u)));

            camera.LookFrom(eye, _raiders[0].Pose.Position);
            camera.RollToward(Vector3.Transform(Vector3.UnitY, _escorts[0].Pose.Rotation));
        }),

        // 5 · Splash one. An orbit that pulls back as the debris expands, so the burst stays framed.
        new(34f, 46f, "Splash one.", (camera, u) =>
        {
            var centre = _raiders[0].Pose.Position;
            var eased = Ease.InOut(u);

            camera.Roll = 0f;
            camera.LookFrom(
                Shots.Around(centre, float.Lerp(1_400f, 3_000f, eased), 1.1f + u * 1.6f, float.Lerp(240f, 800f, eased)),
                centre);
        }),

        // 6 · Home. A crane: low behind Kestrel, rising and pulling back as it slides into the slot.
        new(46f, 54f, "Kestrel, you are cleared to dock.", (camera, u) =>
        {
            var eased = Ease.InOut(u);
            var eye = Vector3.Lerp(
                Shots.Rider(_kestrel.Pose, new Vector3(-120f, 160f, 1_100f)),
                StationPosition + new Vector3(2_600f, 1_900f, 3_100f),
                eased);

            camera.Roll = 0f;
            camera.LookFrom(eye, Vector3.Lerp(_kestrel.Pose.Position, StationPosition, eased));
        }),

        // 7 · Sunrise. The station between the camera and the sun, and the sun a hair off the planet's
        // limb — which is where the scale of this scene stops being an abstraction.
        new(54f, Duration, null, (camera, u) =>
        {
            var eye = StationPosition + new Vector3(2_900f, -260f, 4_100f);
            camera.Roll = 0f;
            camera.LookFrom(eye, Vector3.Lerp(StationPosition, Fleet.SunCentre, Ease.InOut(u) * 0.92f));
        })
    ];
}
