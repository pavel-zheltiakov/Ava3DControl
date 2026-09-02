using System;
using System.Numerics;
using Ava3D.Demo.Story;
using Avalonia.Media;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// The room behind a tower clock: one window, one shaft of afternoon sun through the back of the dial,
/// and everything standing in it drawn on the floor in shadow.
///
/// <c>Shadows</c> says what the feature is and <c>Shadow probe</c> measures whether it is right. This is
/// the one that is meant to be looked at, and it is built on a single observation about clocks: a tower
/// movement is a machine nobody is meant to see. The dial faces the street, the room behind it is dark,
/// and the hands, the frame and the pendulum all stand between the window and the floor — so the room
/// draws itself on its own floor, four metres across, and keeps time while it does it.
///
/// Turn the Shadows switch off and the scene loses no detail at all. It loses its subject. The floor goes
/// evenly bright, the ellipse and its two hands are gone, and what is left is a lit box with some
/// ironwork in it.
/// </summary>
public sealed class ClockTowerScene : DemoScene
{
    // The tower, in metres: a square room seven and a half across and five and a half high, walled on
    // three sides and open towards the camera. The fourth wall is missing rather than hidden, which is
    // what every stage set does and is worth knowing before orbiting round the back of it.
    /// <summary>
    /// Half the tower's clear width, and its floor-to-ceiling height.
    ///
    /// Public because a room that mounts this scene has to know where its walls are: the story stands the
    /// tower up as a room the visitor walks into, and the wall it adds to close the open side has to land
    /// on the edge of a floor this file owns. See <c>ClockRoom</c>.
    /// </summary>
    public const float RoomHalf = 3.8f;

    public const float RoomHeight = 5.4f;

    /// <summary>
    /// How far every wall is buried below the floor, and it is here for one reason.
    ///
    /// A wall that stops exactly at the floor leaves a seam the sun can get through: rays passing under
    /// its bottom edge land on the first few millimetres of floor, and at any map resolution some of them
    /// win, which draws a bright hairline along the foot of the wall. Sinking the wall means the rays that
    /// pass beneath it land outside the room, where there is no floor to light.
    /// </summary>
    private const float WallFoot = 0.4f;

    // The window is the whole of the lighting design, so its four numbers are the ones to move. It sits
    // in the −X wall; the sun comes through it travelling +X and down.
    private const float OpeningBottom = 1.3f;
    private const float OpeningTop = 4.5f;
    private const float OpeningHalf = 1.7f;
    private const float DialCentre = (OpeningBottom + OpeningTop) * 0.5f;

    /// <summary>
    /// The dial's centreline radius, and it is set by the hole rather than by taste.
    ///
    /// The rim is a torus of tube 0.07, so the wheel is 2 × (DialRadius + 0.07) across, and the clear
    /// opening between the sill's top face and the lintel's underside is 2.88. Anything past 1.37 puts the
    /// top and bottom of the rim inside the stone, which does not read as a big clock — it reads as a ring
    /// that has been pushed through the wall.
    /// </summary>
    private const float DialRadius = 1.34f;

    /// <summary>
    /// How far the sun drops per metre it travels into the room, which is the only thing that decides
    /// where the light lands.
    ///
    /// At 0.70 the patch runs from about two metres inside the window to two and a half metres past the
    /// middle of the floor, and stops short of the far wall — so the beam is a shape lying on the ground
    /// with darkness all round it rather than a stripe running up a wall and out of frame.
    /// </summary>
    private const float SunDrop = 0.70f;

    private const float PendulumPivot = 4.24f;
    private const float PendulumLength = 3.0f;

    /// <summary>
    /// Seconds for one full swing, there and back, and it is arithmetic rather than a choice.
    ///
    /// A pendulum's period is 2π√(L/g), which for the three metres of rod below the pivot is 3.47 s and not
    /// the two a shorter one would give — two seconds is the seconds pendulum, and a seconds pendulum is
    /// 994 mm, a third of this one. The number is here rather than inside the escapement because the
    /// escapement is driven from it: one tooth per half swing, so the wheels step every 1.74 s.
    /// </summary>
    private const float Period = 3.47f;

    private const float Swing = 7f;

    /// <summary>Seconds per turn of the minute hand — a fast afternoon, so that a hand moves while you watch.</summary>
    private const float MinuteTurn = 20f;

    // Ten past four, which puts the hands at a comfortable angle to each other and neither of them along
    // a spoke.
    private const float StartMinutes = 60f;
    private const float StartHours = 125f;

    private const int GreatTeeth = 24;
    private const int EscapeTeeth = 12;

    /// <summary>
    /// The two wheels, and the one arrangement of them that is not two objects in the same metal.
    ///
    /// A tooth stands <see cref="ToothReach"/> proud of its wheel's centreline, so two wheels whose centres
    /// are closer than the sum of their tip radii interpenetrate — and these were set 0.79 apart with tips
    /// summing to 0.95, which put the great wheel's teeth through the escape wheel's rim and into its
    /// spokes. Moving them apart is only half of it. The rim is a solid torus <i>at</i> the centreline, so
    /// there is no gap beneath it for a tooth to drop into and no spacing at which these two genuinely
    /// intermesh: anything closer than tangency is a collision, and tangency is therefore the answer.
    ///
    /// So they are set tip to tip, and the escape wheel's teeth carry half a pitch of phase — which puts
    /// the space between two of its teeth where the great wheel's tooth arrives instead of putting a tooth
    /// there. Both wheels step in whole teeth, so that relationship is fixed for the life of the scene:
    /// get it right once and it cannot drift.
    /// </summary>
    private const float GreatRadius = 0.52f;

    private const float EscapeRadius = 0.26f;

    private const float ToothSeat = 0.04f;

    private const float ToothHeight = 0.09f;

    /// <summary>How far a tooth's tip stands past the centreline it is seated on.</summary>
    private const float ToothReach = ToothSeat + ToothHeight * 0.5f;

    private const float GreatTip = GreatRadius + ToothReach;

    private const float EscapeTip = EscapeRadius + ToothReach;

    private const float WheelCentres = GreatTip + EscapeTip;

    // Placed so the train is centred on the frame rather than the wheel centres being centred on it, which
    // is not the same point when one wheel is twice the other.
    private const float GreatX = (GreatTip - EscapeTip - WheelCentres) * 0.5f;

    private const float EscapeX = GreatX + WheelCentres;

    /// <summary>The plinth and the top plate, sized from the train rather than the train being fitted into
    /// a frame that was drawn first.</summary>
    private const float FrameWidth = GreatTip + WheelCentres + EscapeTip + 0.16f;

    /// <summary>The lantern's base brightness, read by the light that is placed and by the flicker that
    /// drives it. Spelled twice, it is one that gets tuned and one that quietly wins a frame later.</summary>
    private const float LanternIntensity = 2.3f;

    /// <summary>
    /// What the sun is worth, and the one number the beam is made of. Public for the reason the direction
    /// is: a room mounting this has no Stage and copies the sun by hand.
    /// </summary>
    public const float SunIntensity = 2.2f;

    /// <summary>
    /// The cloud: once every <see cref="CloudPeriod"/> seconds the sun goes in, and for the three and a
    /// half seconds it is fully in, the shadow map is handed to the lantern.
    ///
    /// This is the second half of the exhibit, and it answers the question the first half leaves. With
    /// the sun out, everything in the beam draws itself on the floor and everything the lantern lights
    /// does not, because the renderer maps one light and the sun is the one worth a map. So the wheels
    /// stand in a lamp's light with no shadow under them, which is the one thing in the room that looks
    /// wrong, and it is wrong in exactly the way a single map is. The cloud is how the room gets to show
    /// the other kind: the sun goes, the map goes to the lantern, and for a few seconds the movement is
    /// drawn on its own plinth and the floor in front of it from a bulb a metre and a half away — a point
    /// light's shadow, which is a different frustum and a different drawing — and then the sun comes back
    /// and takes the map with it.
    ///
    /// The seconds are the film's. The story mounts this scene and runs it on the chapter's own clock, and
    /// the chapter looks at the wheels from its thirty-fourth second to its thirty-seventh — so the cloud
    /// is timed to be over the sun then, and the sun to be back by the time he turns round to the beam at
    /// forty and a half. On the picker's clock the same cloud comes round every forty-eight seconds.
    /// </summary>
    private const float CloudPeriod = 48f;

    private const float CloudArrives = 33.5f;

    private const float CloudCovers = 35f;

    private const float CloudClears = 38.5f;

    private const float CloudGone = 40.5f;

    /// <summary>
    /// Seconds the lantern's shadows take to come up once it holds the map, and to go before it gives it
    /// back. The sun is already at nothing when the map changes hands, so the hand-over itself is
    /// invisible; what would show is a room's worth of shadows arriving in one frame, and this is what
    /// stops that. Driven through <see cref="Scene.ShadowStrength"/>, which is the one knob that fades a
    /// shadow without moving it.
    /// </summary>
    private const float HandOver = 0.8f;

    /// <summary>How many motes hang in the beam, and how much of the sun each one is.</summary>
    private const int Motes = 900;

    private const float MoteOpacity = 0.32f;

    /// <summary>
    /// Where the movement stands, and where the bulb hangs inside it.
    ///
    /// The lantern is a light rather than a node and cannot ride on the frame, so it is placed at the sum
    /// of these two — which is what keeps the lit point and the visible glass in the same place when the
    /// movement is moved. Spelling the world position out instead leaves the bulb dark and the room lit
    /// from empty air, with no compile error and nothing in the picture to say why.
    /// </summary>
    private static readonly Vector3 MovementOrigin = new(1.5f, 0f, -2.05f);

    private static readonly Vector3 BulbOffset = new(1.15f, 2.05f, 0f);

    /// <summary>
    /// Where the sun comes from, and where the lantern hangs — the two things <see cref="Stage"/> puts on
    /// the scene, published so that something mounting the subject alone can put them back.
    ///
    /// A mounted subject gets no Stage — see <see cref="DemoScene"/>, which says so — and this scene is
    /// most of the way to unlit without one: the beam is the picture, and the beam is a directional light
    /// that is not in the subject. Publishing the two vectors is what lets a room light this tower the way
    /// the scene lights it, at whatever scale the room stands it up at, instead of guessing.
    /// </summary>
    public static Vector3 SunDirection => Vector3.Normalize(new Vector3(1f, -SunDrop, 0.11f));

    /// <summary>Where the bulb is, in the subject's own coordinates.</summary>
    public static Vector3 Lantern => MovementOrigin + BulbOffset;

    /// <summary>What the lantern is, so a room mounting this can scale both: an intensity goes as the
    /// square of the scale and a range goes with it linearly.</summary>
    public static float LanternFull => LanternIntensity;

    public static float LanternRange => 4.6f;

    /// <summary>
    /// How much sun there is before the weather, nought to one.
    ///
    /// A room that mounts this fades the sun in as the visitor arrives and out as he leaves, and it does
    /// that here rather than on its own light alone because the sky outside the window and the dust in
    /// the beam have to go with it. What a light should actually be set to is <see cref="Sun"/>, which is
    /// this less the cloud.
    /// </summary>
    public float Daylight
    {
        get => _day;
        set
        {
            _day = Math.Clamp(value, 0f, 1f);
            Shade();
        }
    }

    /// <summary>How far the cloud is across the sun, nought to one. Set by <see cref="Update"/> from its clock.</summary>
    public float Cloud { get; private set; }

    /// <summary>What the sun is worth right now, as a fraction of full: the daylight, less the cloud.</summary>
    public float Sun => _day * (1f - Cloud);

    /// <summary>Whether the lantern holds the shadow map. True only while the cloud is fully over.</summary>
    public bool Overcast => Cloud >= 1f;

    /// <summary>
    /// How strong the lantern's shadows are while it holds the map, nought to one. What
    /// <see cref="Scene.ShadowStrength"/> should be set to whenever <see cref="Overcast"/> is true; it
    /// should be one the rest of the time.
    /// </summary>
    public float Handed { get; private set; }

    private Node? _pendulum;
    private Node? _minuteHand;
    private Node? _hourHand;
    private Node? _greatWheel;
    private Node? _escapeWheel;
    private PointLight? _lantern;

    private Material? _daylight;
    private PointsNode? _dust;
    private Vector3[] _motes = [];
    private Mote[] _seeds = [];
    private float _day = 1f;
    private bool _handed;

    /// <summary>
    /// Everything that casts and is not the movement, with what it was set to, so the map can be handed
    /// to the lantern and back — see <see cref="Hand"/>.
    /// </summary>
    private readonly List<(MeshNode Node, bool Casts)> _bystanders = [];

    public override string Title => "Clock tower";

    public override string Summary => "A dark room, one window, and the clock drawn on the floor in shadow";

    public override string Notes =>
        """
        The two lines are the same two as in Shadows — CastsShadows on the light, ShadowMapSize on the
        scene — and everything else here is a room built out of boxes, planes and cylinders. What this one
        adds is the case those two lines were written for, where the shadow is not an effect laid over the
        picture but is the picture.

        The surfaces are the building's: masonry, flagstones, oak, cast iron, brass, rope and galvanised
        steel, every one of them a few maps of arithmetic from the same kit the story's rooms are lined
        with. None of it touches the shadow. The depth pass records silhouettes and a floor's flags are
        not one, so the drawing on the floor is the same drawing it was on a flat grey plane — what the
        maps change is that the sun now has something to rake across on its way to the wall, and the
        joints between the flags throw a hair of shadow of their own.

        The window wall is four flat planes with a hole left between them, and that hole is the whole of
        the lighting. Sunlight arrives as a single directional light aimed through it, so the only lit part
        of the floor is the part the opening lets through, and the only things that can be seen to cast are
        the things standing in that beam: the dial's ring and spokes, its two hands, the pendulum, the
        drive weight hanging down the middle of the tower, the bucket on the floor beside it, the rungs
        of the ladder leaning beside the window, and the stool and the coil of rope in the corners of the
        patch. Everything outside it is lit by the lantern and the ambient sky, which is why the movement
        is legible at all.

        One light casts. Scene.ShadowCastingLight is the first light in the scene that has CastsShadows
        set, so a second lamp adds light to the room without adding a second depth pass — light is cheap
        here, shadow is one map — and that is why the movement stands in the lantern's light with no
        shadow under it. Once every forty-eight seconds the room shows the other half of that. A cloud
        goes over the sun, the beam fades out, and for the three and a half seconds it is fully gone the
        map is handed to the lantern: the wheels, the posts and the plinth are drawn on the floor from a
        bulb a metre and a half away, which is a point light's shadow and a different frustum, and then
        the sun comes back and takes the map with it. While the lantern holds it nothing but the movement
        casts, because a point light shadows a cone fitted round its casters and a cone round the whole
        room is thirty millimetres a texel — a spoke is thinner than that. The change of hands is made
        with the sun at nothing and the new shadows brought up through ShadowStrength over most of a
        second, so nothing appears in one frame.

        The dust is what makes the beam a beam. Nine hundred points, each on its own ray in through the
        opening, each a function of the second, additive and as bright as the sun is; without them the
        light is a shape on the floor, and with them it is a shaft of air with something in it. They are
        not in the map — a point casts nothing — and the floor does not care.

        The map is rebuilt every frame, so nothing needs to be told that anything moved. The pendulum
        swings on the period three metres of rod gives it, a little under three and a half seconds, and its
        shadow swings with it; the minute hand goes round once every twenty seconds and drags a shadow of
        itself a bit over two metres across the floor; the escapement steps twice a swing and so does the
        shadow of the wheel it is stepping. None of that is animated separately — the objects move, and the
        map that is drawn from the sun's point of view moves with them.

        Four metres is the dial, not the hand. The ring stands three metres tall in a wall the sun crosses
        at 0.7 per metre, so what it draws on the floor is four metres of ellipse; the minute hand inside it
        is 1.3 long and its shadow runs about 2.4 at noon and less at every other hour. Those are different
        numbers about different objects and it is worth keeping them apart, because the ellipse is the
        picture and the hand is the clock.

        What does not cast is the floor, the daylight panel outside the window, and the scenery above and
        behind the beam — the ceiling beams and their corbels, the course at the foot of the walls, the
        doorway through to the stair, the lamp with its wire, and the strap the pendulum hangs from. The
        room itself does. The ceiling and both blank walls are in
        the map and they have to be: taking the back wall out of it was measured, and what it did was let
        the sun in along the foot of a wall the sun cannot get through.

        So the rule is not "leave out whatever has no shadow anybody will see". It is that a room is what
        stops the light, so everything enclosing one belongs in the depth pass, and what comes out is what
        encloses nothing. Leaving the floor out saves eighteen thousand triangles a frame and changes the
        picture by nothing at all — the frustum is fitted to the casters, and a floor lying inside the
        sphere the walls already describe cannot make that sphere any smaller. The daylight panel is the
        one exclusion that buys resolution: it stands two metres outside the tower, and letting it into the
        map takes the fitted radius from six and a half metres to nine and costs every shadow in the room a
        third of its sharpness. All of them still receive — the flag is about what goes into the map, not
        about what is shaded by it.

        The wall itself is the reason this room can exist. Each panel of it is a single flat plane, which
        has a front and no back; a depth pass that kept only the faces pointing away from the light would
        record nothing for it at all, and the sun would light the room as though the wall were not there.
        The same goes for the bucket, which is a tube with a bottom and no lid. Both cast here because the
        depth pass keeps whatever surface the light reaches first.

        Only the floor is finely divided, and it is the one thing in the room the sun actually lands on.
        The CPU renderer shades per vertex, so a shadow it computes is only as sharp as the mesh under it —
        a four-vertex floor would interpolate this entire scene away. Ninety-six divisions puts a vertex
        every 78 mm, which draws the dial and the pendulum honestly and is at its limit on the hands: the
        minute hand's blade is 85 mm across, so on that path its shadow is about one vertex wide and comes
        out broken rather than drawn. The three GPU backends shade per pixel and give it as a line. The
        walls stay coarse because no direct light ever reaches them, which is a saving that would stop
        being true the moment the sun moved.

        ShadowStrength is 1 whenever the sun holds the map. A shadow here loses all of the sun and keeps the lantern and the sky,
        which is why the dial's shadow is exactly as dark as the rest of the room and reads as a hole cut
        in the beam rather than as a grey smudge on it.
        """;

    public override SceneLook Look => SceneLook.Studio;

    public override bool Animates => true;

    public override bool FramesItself => true;

    public override TimeSpan TourDuration => TimeSpan.FromSeconds(16);

    /// <summary>
    /// Inside the room, near the open corner, looking across the beam at the back of the dial.
    ///
    /// The camera is placed rather than fitted because there is one shot here and AutoFit does not know
    /// about it: fitted to the scene's sphere it backs off until the tower is a small object in the
    /// middle of the frame, seen from outside a wall that is not there.
    /// </summary>
    public override void Frame(Camera camera)
    {
        camera.Target = new Vector3(0f, 1.25f, 0.1f);
        camera.Distance = 4.2f;
        camera.Yaw = 0.86f;
        camera.Pitch = 0.22f;

        // Wide, because the camera is standing in the corner of a room seven metres across and cannot
        // back out of it: the fourth wall is missing but the tower is not, and at the default angle this
        // shot is a close-up of the floor.
        camera.FieldOfView = 58f;
        camera.NearPlane = 0.3f;
        camera.FarPlane = 60f;
    }

    /// <summary>
    /// One sun through one window, one lantern for everything the sun does not reach, and an ambient sky
    /// dim enough that the dark half of the room stays dark.
    /// </summary>
    public override void Stage(Scene scene)
    {
        scene.Background = Color.FromRgb(9, 10, 13);

        // Travelling +X and down at SunDrop per metre, with a little +Z on it so the beam lies across the
        // floor at an angle rather than square to the walls.
        scene.Light.Direction = Vector3.Normalize(new Vector3(1f, -SunDrop, 0.11f));
        scene.Light.Color = new Vector3(1f, 0.92f, 0.76f);
        scene.Light.Intensity = SunIntensity;
        scene.Light.Ambient = 0.02f;

        // The two lines this scene is about.
        scene.Light.CastsShadows = true;
        scene.ShadowMapSize = 2048;

        _lantern = new PointLight
        {
            Position = MovementOrigin + BulbOffset,
            Color = new Vector3(1f, 0.66f, 0.32f),
            Intensity = LanternIntensity,
            Range = LanternRange
        };

        scene.Lights.Add(_lantern);

        // Cold and weak: this is the light in the corners of a stone room on a bright day, and it has to
        // lose to the window by a long way or there is no beam.
        scene.Environment = new EnvironmentLight
        {
            SkyColor = new Vector3(0.42f, 0.50f, 0.64f),
            GroundColor = new Vector3(0.12f, 0.10f, 0.09f),
            Intensity = 0.34f
        };
    }

    public override Node BuildSubject()
    {
        var root = new Node { Name = "clock tower" };

        // The surfaces come out of the building's own kit — see Finish — because this is the one scene
        // that is a room, and a room is made of what the building is made of. Every map is arithmetic,
        // generated on first use and shared by identity, so seven materials across a hundred and forty
        // parts are twenty uploads. Nothing about the shadow pass sees any of it: a caster is a caster
        // whatever its skin, and the map is fitted to silhouettes.
        var stone = Finish.Masonry();
        var flags = Finish.Flagstones();
        var oak = Finish.Oak();
        var iron = Finish.CastIron();
        var brass = Finish.Brass();
        var zinc = Finish.Galvanised();

        // Rough-sawn boards over the beams: the lounge's floor, unwaxed and in the dark. The same maps at
        // half the brightness and twice the roughness are a different surface, which is the whole of what
        // a material's two numbers are for.
        var boards = Finish.Boards();
        boards.BaseColor = new Vector4(0.16f, 0.12f, 0.08f, 1f);
        boards.Roughness = 0.85f;

        // Unlit rather than emissive, because this is not a surface with a light on it — it is what is
        // left of the sky once the eye has settled for the inside of a tower.
        var daylight = new Material
        {
            BaseColor = new Vector4(1f, 0.97f, 0.90f, 1f),
            Shading = ShadingModel.Unlit
        };

        // Kept, because the sky is set from the sun and the sun is set by whoever is running the clock.
        _daylight = daylight;

        var flame = new Material
        {
            BaseColor = new Vector4(1f, 0.78f, 0.42f, 1f),
            Shading = ShadingModel.Unlit
        };

        root.Children.Add(Room(stone, flags, oak, boards, iron));
        root.Children.Add(WindowWall(stone, daylight));
        root.Children.Add(Dial(iron, brass));

        var movement = Movement(iron, stone, flame);

        root.Children.Add(movement);
        root.Children.Add(Pendulum(iron, brass));
        root.Children.Add(Bucket(zinc));
        root.Children.Add(Weight(iron));
        root.Children.Add(Keeper(oak, iron, brass, zinc));
        root.Children.Add(Dust());

        // Everything that casts and is not the movement, for the seconds the lantern holds the map.
        Bystander(root, except: movement);

        return root;
    }

    public override void Update(Scene scene, double elapsed)
    {
        var t = (float)elapsed;

        // Cosine rather than sine so the swing starts at its extreme and the first frame of a capture is
        // the same picture as the last one before it turns.
        _pendulum!.RotationDegrees = new Vector3(MathF.Cos(t * MathF.Tau / Period) * Swing, 0f, 0f);

        _minuteHand!.RotationDegrees = new Vector3(StartMinutes + t * 360f / MinuteTurn, 0f, 0f);
        _hourHand!.RotationDegrees = new Vector3(StartHours + t * 360f / (MinuteTurn * 12f), 0f, 0f);

        // The escapement, which is the one thing in a clock that does not move smoothly: one tooth per
        // half swing, and the great wheel it drives steps with it in the ratio the two tooth counts say.
        //
        // The half in the floor is the phase, and it is the difference between an escapement and a twitch.
        // A deadbeat escapement impulses as the pendulum crosses the middle, where the bob is moving
        // fastest. Without the half this steps at t = 0, 1, 2 …, which is exactly where the cosine is at
        // its extreme and the bob is momentarily stopped — the one instant in the swing at which a real
        // escapement does nothing at all.
        var ticks = MathF.Floor(t / (Period * 0.5f) + 0.5f);

        _escapeWheel!.RotationDegrees = new Vector3(0f, 0f, ticks * 360f / EscapeTeeth);
        _greatWheel!.RotationDegrees = new Vector3(0f, 0f, ticks * -360f / GreatTeeth);

        // The weather, the dust and who holds the map — see the cloud constants for the whole argument.
        Weather(t);
        Settle(t);
        Shade();
        Hand(Overcast);

        // A flame is never quite steady, and the whole dark half of the room is lit by this one.
        //
        // Null when the subject has been mounted somewhere else. Stage is what places the lantern and a
        // story mounts BuildSubject alone — see DemoScene, which says Stage is never called on that path —
        // so a room that hangs this movement in its own light gets the clock without the lamp, and sets
        // the sun and the map on its own lights from Sun, Overcast and Handed. Everything else this
        // method drives is built in BuildSubject and is therefore always there.
        if (_lantern is not null)
        {
            _lantern.Intensity =
                LanternIntensity * (0.94f + 0.06f * MathF.Sin(t * 7.3f) * MathF.Sin(t * 2.9f));

            scene.Light.Intensity = SunIntensity * Sun;
            scene.Light.CastsShadows = !Overcast;
            _lantern.CastsShadows = Overcast;
            scene.ShadowStrength = Overcast ? Handed : 1f;
        }

        scene.Invalidate();
    }

    /// <summary>
    /// Floor, ceiling and the two walls that only receive: none of them casts, and only the floor is
    /// divided finely enough to show what lands on it.
    /// </summary>
    private static Node Room(Material stone, Material flags, Material oak, Material boards, Material iron)
    {
        var room = new Node { Name = "room" };
        const float span = RoomHalf * 2f;

        // The floor stops a hand's breadth short of the walls, and the gap is not a modelling slip.
        //
        // A shadow map is compared with an offset, so the first few centimetres of any receiver measured
        // along the light from its blocker read as lit — which at the foot of a wall is a bright hairline
        // drawn along the whole length of it. Ending the floor before it reaches the wall puts that band
        // where there is no floor for it to land on, and what is left in its place is a dark joint, which
        // is what the corner of a stone room looks like anyway.
        //
        // Mapped in metres from the room's origin — see Fabric.Map — so a flag is a flag whatever the
        // floor is divided into, and the ninety-six divisions the CPU renderer needs cost the flags nothing.
        room.Children.Add(new MeshNode(
            Fabric.Map(Primitives.Plane(span - 0.14f, span - 0.14f, 96, 96), flags, Vector3.Zero), flags)
        {
            Name = "floor",
            CastsShadow = false
        });

        // Ceiling and the two blank walls, and all three of them cast.
        //
        // That is worth a sentence, because none of the three has a shadow anybody will ever see and the
        // obvious thing to do with a surface like that is to keep it out of the depth pass. Keeping the
        // back wall out was measured, and what it did was let the sun in: rays that reach the floor at
        // this end of the room enter the tower a little behind the window wall, and with nothing recorded
        // for the back wall there was nothing to stop them. It came out as a band of sunlight lying along
        // the foot of a wall the sun cannot get through.
        //
        // The rule that falls out of it is simpler than the exception was. A room is what stops the light,
        // so everything that encloses one belongs in the map; what to leave out is the floor, which
        // encloses nothing and can block nothing, and the daylight outside the window.
        //
        // None of them is finely divided. Nothing lands on any of them — the beam stops on the floor —
        // and a caster's own mesh density has no bearing on the shadow it casts.
        // Wider than the room, so that no line of sight can slip between the ceiling and the wall it
        // meets and find the daylight behind: at the join those two are a single edge, and an edge shared
        // exactly is an edge that leaks a few bright pixels.
        //
        // Boards laid across the beams, which is the way a ceiling is boarded: turned, so the planks run
        // along Z where the beams run along X.
        var lid = new Vector3(0f, RoomHeight, 0f);

        room.Children.Add(new MeshNode(
            Fabric.Map(Primitives.Plane(span + 0.5f, span + 0.5f, 4, 4), boards, lid, turned: true), boards)
        {
            Name = "ceiling",
            Position = lid
        });

        // Stood up by a quarter turn about X and mapped the way the room sees it — see Panel, which does
        // the same for the two walls that stand along Z.
        var back = new Vector3(0f, (RoomHeight - WallFoot) * 0.5f, -RoomHalf);

        room.Children.Add(new MeshNode(
            Fabric.Map(Stood(Primitives.Plane(span, RoomHeight + WallFoot, 6, 6), 90f, 0f), stone, back), stone)
        {
            Name = "back wall",
            Position = back
        });

        room.Children.Add(Panel(
            RoomHeight + WallFoot, span,
            new Vector3(RoomHalf, (RoomHeight - WallFoot) * 0.5f, 0f), stone, "far wall"));

        // Beams across the ceiling and a door through to the stair, both of them scenery: they are above
        // and behind the beam of light and will never cast anything anybody can see.
        // One mesh for the three of them, and the same everywhere below that a shape is hung more than
        // once. Every backend caches uploaded geometry by the mesh object it came from — GlMeshCache and
        // the two beside it are all Dictionary<Mesh, GpuMesh> — so a Primitives call inside a loop is
        // a fresh upload of identical triangles on every pass.
        var beam = Block(span, 0.22f, 0.26f, oak, new Vector3(0f, RoomHeight - 0.13f, 0f));

        for (var i = -1; i <= 1; i++)
            room.Children.Add(new MeshNode(beam, oak)
            {
                Name = "ceiling beam",
                Position = new Vector3(0f, RoomHeight - 0.13f, i * 2.3f),
                CastsShadow = false
            });

        // A corbel under each end of each beam, which is what carries a beam in a stone wall: the beam
        // sits on the stone rather than vanishing into it. Scenery, like the beams.
        var corbel = Block(0.30f, 0.30f, 0.36f, stone, Vector3.Zero);

        for (var i = -1; i <= 1; i++)
        foreach (var side in new[] { -1f, 1f })
            room.Children.Add(new MeshNode(corbel, stone)
            {
                Name = "corbel",
                Position = new Vector3(side * (RoomHalf - 0.15f), RoomHeight - 0.39f, i * 2.3f),
                CastsShadow = false
            });

        // A plinth course round the foot of the three walls: a hand's height of the floor's own harder
        // stone, standing a little proud, which is what the bottom of a masonry wall is and which covers
        // the joint the floor leaves. Under the sill and behind the beam, so it never casts.
        const float course = 0.34f;
        const float proud = 0.08f;

        void Course(float length, Vector3 centre, bool alongX)
        {
            var size = alongX ? new Vector3(length, course, proud) : new Vector3(proud, course, length);

            room.Children.Add(new MeshNode(Block(size.X, size.Y, size.Z, flags, centre), flags)
            {
                Name = "course",
                Position = centre,
                CastsShadow = false
            });
        }

        var foot = course * 0.5f - 0.01f;
        var inset = RoomHalf - proud * 0.5f - 0.005f;

        Course(span, new Vector3(inset, foot, 0f), alongX: false);
        Course(span, new Vector3(-inset, foot, 0f), alongX: false);

        // The back wall's, either side of the stair door.
        Course(0.87f, new Vector3(-3.365f, foot, -inset), alongX: true);
        Course(5.47f, new Vector3(1.065f, foot, -inset), alongX: true);

        room.Children.Add(Door(oak, boards, iron));

        return room;
    }

    /// <summary>
    /// The door to the stair: oak boards standing on end in an oak frame, two iron straps across them and
    /// a ring to pull, under the lintel that was already there.
    ///
    /// It was a black box let into the wall, and a black box reads as a hole — which was the intention and
    /// is the wrong object. A hole in the back wall of a tower is a way out that the film never takes; a
    /// shut door is a room with one way in, which is what this room is. Everything here is scenery and
    /// nothing casts: it stands in the dark half, behind the beam, lit by the lantern alone.
    /// </summary>
    private static Node Door(Material oak, Material boards, Material iron)
    {
        var door = new Node { Name = "stair door", Position = new Vector3(-2.3f, 0f, -RoomHalf) };

        // The leaf, five millimetres off the wall so no face of it is level with the plane it stands on.
        // Turned, so the lounge's floorboards stand on end; at eight tenths of a metre to the image the
        // boards come out a hundred millimetres wide, which is a ledged door and not a parquet one.
        var leaf = Fabric.Slab(
            new Vector3(1.00f, 2.10f, 0.06f), new Vector3(0f, 1.05f, 0.035f), boards, "leaf", 0.8f, turned: true);

        leaf.CastsShadow = false;
        door.Children.Add(leaf);

        // The frame: two jambs and the lintel, the lintel deeper than the jambs because it was a beam
        // before it was a door head. Oak, along the grain.
        foreach (var side in new[] { -1f, 1f })
            door.Children.Add(new MeshNode(Block(0.12f, 2.20f, 0.12f, oak, new Vector3(side * 0.56f, 1.10f, 0.06f)), oak)
            {
                Name = "jamb",
                Position = new Vector3(side * 0.56f, 1.10f, 0.06f),
                CastsShadow = false
            });

        door.Children.Add(new MeshNode(Block(1.40f, 0.18f, 0.62f, oak, new Vector3(0f, 2.29f, -0.22f)), oak)
        {
            Name = "door lintel",
            Position = new Vector3(0f, 2.29f, -0.22f),
            CastsShadow = false
        });

        // Two straps and a ring, which are the parts of a ledged door that say which way it opens and
        // that it does. Six millimetres proud of the boards: a strap is nailed on, not let in.
        var strap = Block(0.86f, 0.07f, 0.012f, iron, Vector3.Zero, Finish.Close);

        foreach (var y in new[] { 0.55f, 1.62f })
            door.Children.Add(new MeshNode(strap, iron)
            {
                Name = "strap",
                Position = new Vector3(-0.05f, y, 0.071f),
                CastsShadow = false
            });

        door.Children.Add(new MeshNode(Turned(Primitives.Torus(0.055f, 0.009f, 20, 8)), Lathe(iron, 0.35f, 0.06f))
        {
            Name = "ring",
            Position = new Vector3(0.34f, 1.05f, 0.09f),
            RotationDegrees = new Vector3(90f, 0f, 0f),
            CastsShadow = false
        });

        return door;
    }

    /// <summary>
    /// The wall the sun comes through: four flat panels with a hole left between them, a stone reveal
    /// standing proud of it, and the daylight behind.
    ///
    /// Every panel is one plane, one polygon thick, and every one of them casts.
    /// </summary>
    private static Node WindowWall(Material stone, Material daylight)
    {
        var wall = new Node { Name = "window wall" };

        // Wider than the room and buried below it, so that the only way past this wall is the opening.
        const float span = RoomHalf * 2f + 0.4f;
        const float jamb = span * 0.5f - OpeningHalf;
        const float opening = OpeningTop - OpeningBottom;

        wall.Children.Add(Panel(
            OpeningBottom + WallFoot, span,
            new Vector3(-RoomHalf, (OpeningBottom - WallFoot) * 0.5f, 0f), stone, "sill wall"));

        wall.Children.Add(Panel(
            RoomHeight - OpeningTop, span,
            new Vector3(-RoomHalf, (RoomHeight + OpeningTop) * 0.5f, 0f), stone, "head wall"));

        for (var side = -1; side <= 1; side += 2)
            wall.Children.Add(Panel(
                opening, jamb,
                new Vector3(-RoomHalf, DialCentre, side * (OpeningHalf + jamb * 0.5f)),
                stone, "jamb wall"));

        // The reveal: four blocks standing a hand's breadth into the room, so the edge of the beam has a
        // thickness to it and the opening reads as a hole in a wall rather than as a gap between planes.
        // Each is one dressed stone, mapped where it stands so its courses are the wall's.
        var depth = 0.28f;
        var reveal = -RoomHalf + depth * 0.5f;

        var sill = new Vector3(reveal, OpeningBottom + 0.07f, 0f);
        var lintel = new Vector3(reveal, OpeningTop - 0.08f, 0f);

        wall.Children.Add(new MeshNode(Block(depth, 0.14f, OpeningHalf * 2f + 0.28f, stone, sill), stone)
        {
            Name = "sill",
            Position = sill
        });

        wall.Children.Add(new MeshNode(Block(depth, 0.16f, OpeningHalf * 2f + 0.28f, stone, lintel), stone)
        {
            Name = "lintel",
            Position = lintel
        });

        var jambStone = Block(depth, opening, 0.14f, stone, new Vector3(reveal, DialCentre, 0f));

        for (var side = -1; side <= 1; side += 2)
            wall.Children.Add(new MeshNode(jambStone, stone)
            {
                Name = "reveal",
                Position = new Vector3(reveal, DialCentre, side * (OpeningHalf - 0.07f))
            });

        // The sky, which is one unlit rectangle two metres outside the tower. It is not a light and does
        // not cast; it is there because a window with the scene's background behind it reads as a hole
        // into space rather than as a bright afternoon.
        wall.Children.Add(new MeshNode(Primitives.Plane(9f, 12f), daylight)
        {
            Name = "daylight",
            Position = new Vector3(-RoomHalf - 2.4f, 3f, 0f),
            RotationDegrees = new Vector3(0f, 0f, 90f),
            CastsShadow = false,
            IsPickable = false
        });

        return wall;
    }

    /// <summary>
    /// The dial: a skeleton ring with eight spokes and twelve marks, and two hands on the street side of
    /// it. Nothing of it is solid, which is why its shadow is a drawing rather than a disc.
    /// </summary>
    private Node Dial(Material iron, Material brass)
    {
        var dial = new Node { Name = "dial" };

        // Standing in the wall plane: a torus is built lying flat, so a quarter turn about Z stands it up
        // with its axis along X and its own X and Z reading as the room's Y and Z.
        var upright = new Vector3(0f, 0f, 90f);
        var centre = new Vector3(-RoomHalf, DialCentre, 0f);

        // The rings take the casting on their own coordinates, once round and once through — see Lathe —
        // because a torus has no flat side for the room's grid to land on.
        dial.Children.Add(new MeshNode(
            Turned(Primitives.Torus(DialRadius, 0.07f, 64, 12)), Lathe(iron, MathF.Tau * DialRadius, MathF.Tau * 0.07f))
        {
            Name = "rim",
            Position = centre,
            RotationDegrees = upright
        });

        dial.Children.Add(new MeshNode(
            Turned(Primitives.Torus(DialRadius - 0.36f, 0.035f, 56, 10)),
            Lathe(iron, MathF.Tau * (DialRadius - 0.36f), MathF.Tau * 0.035f))
        {
            Name = "inner ring",
            Position = centre,
            RotationDegrees = upright
        });

        // Three meshes for twenty nodes: one arm, one quarter mark and one plain hour, each uploaded once
        // however many times it is hung on the dial.
        var arm = Block(0.05f, DialRadius - 0.14f, 0.045f, iron, Vector3.Zero, Finish.Close);
        var quarterMark = Block(0.06f, 0.34f, 0.11f, iron, Vector3.Zero, Finish.Close);
        var hourMark = Block(0.06f, 0.20f, 0.07f, iron, Vector3.Zero, Finish.Close);

        for (var i = 0; i < 8; i++)
        {
            var spoke = new Node { Name = "spoke", Position = centre, RotationDegrees = new Vector3(i * 45f, 0f, 0f) };

            spoke.Children.Add(new MeshNode(arm, iron)
            {
                Position = new Vector3(0f, (DialRadius + 0.14f) * 0.5f, 0f)
            });

            dial.Children.Add(spoke);
        }

        for (var i = 0; i < 12; i++)
        {
            var mark = new Node { Name = "hour", Position = centre, RotationDegrees = new Vector3(i * 30f, 0f, 0f) };

            mark.Children.Add(new MeshNode(i % 3 == 0 ? quarterMark : hourMark, iron)
            {
                Position = new Vector3(0f, DialRadius - 0.26f, 0f)
            });

            dial.Children.Add(mark);
        }

        // The hands hang on the street side of the wall, which is where a tower clock keeps them and is
        // also the only place they can be if they are to cast into the room.
        _hourHand = new Node { Name = "hour hand", Position = centre with { X = -RoomHalf - 0.09f } };
        _minuteHand = new Node { Name = "minute hand", Position = centre with { X = -RoomHalf - 0.15f } };

        Hand(_hourHand, 0.90f, 0.13f, brass);
        Hand(_minuteHand, 1.30f, 0.085f, brass);

        dial.Children.Add(_hourHand);
        dial.Children.Add(_minuteHand);

        dial.Children.Add(new MeshNode(Turned(Primitives.Cylinder(0.10f, 0.10f, 0.30f, 20)), Lathe(iron, 0.63f, 0.30f))
        {
            Name = "arbor",
            Position = centre with { X = -RoomHalf - 0.10f },
            RotationDegrees = new Vector3(0f, 0f, 90f)
        });

        return dial;
    }

    /// <summary>One hand: a tapering blade with a counterweight behind the arbor, built pointing at noon.</summary>
    private static void Hand(Node hand, float length, float width, Material brass)
    {
        hand.Children.Add(new MeshNode(Block(0.03f, length, width, brass, Vector3.Zero, Finish.Close), brass)
        {
            Position = new Vector3(0f, length * 0.5f - 0.10f, 0f)
        });

        hand.Children.Add(new MeshNode(Block(0.03f, 0.34f, width * 1.6f, brass, Vector3.Zero, Finish.Close), brass)
        {
            Position = new Vector3(0f, -0.28f, 0f)
        });
    }

    /// <summary>
    /// The movement, standing in the dark half of the room on its own plinth: two wheels in mesh, a frame
    /// to hold them, and the lantern that is the only reason any of it can be seen.
    /// </summary>
    private Node Movement(Material iron, Material stone, Material flame)
    {
        var movement = new Node { Name = "movement", Position = MovementOrigin };

        // One block of the same stone the walls are, mapped where it stands.
        var plinth = new Vector3(0f, 0.17f, 0f);

        movement.Children.Add(new MeshNode(Block(FrameWidth, 0.34f, 1.3f, stone, MovementOrigin + plinth), stone)
        {
            Name = "plinth",
            Position = plinth
        });

        var post = Turned(Primitives.Cylinder(0.05f, 0.05f, 1.4f, 12));
        var column = Lathe(iron, 0.31f, 1.4f);

        // The posts stand clear of the wheels in Z rather than in X — the train reaches within 80 mm of
        // the plinth's ends and there is nowhere in X for a post to be that is not somewhere a tooth goes.
        for (var x = -1; x <= 1; x += 2)
        for (var z = -1; z <= 1; z += 2)
            movement.Children.Add(new MeshNode(post, column)
            {
                Name = "post",
                Position = new Vector3(x * (FrameWidth * 0.5f - 0.19f), 1.04f, z * 0.48f)
            });

        var plate = new Vector3(0f, 1.78f, 0f);

        movement.Children.Add(new MeshNode(Block(FrameWidth - 0.26f, 0.09f, 1.1f, iron, plate, Finish.Close), iron)
        {
            Name = "top plate",
            Position = plate
        });

        // Tangent, and the escape wheel half a tooth out of step so what meets a tooth is a gap. See
        // GreatRadius, which is where the arithmetic and the reason for it are written down.
        _greatWheel = Wheel(GreatRadius, GreatTeeth, 6, 0f, iron);
        _greatWheel.Position = new Vector3(GreatX, 1.05f, 0.34f);

        _escapeWheel = Wheel(EscapeRadius, EscapeTeeth, 4, 180f / EscapeTeeth, iron);
        _escapeWheel.Position = new Vector3(EscapeX, 1.05f, 0.34f);

        movement.Children.Add(_greatWheel);
        movement.Children.Add(_escapeWheel);

        // The lantern, hung from the ceiling on a wire. The light itself is placed in Stage, in world
        // coordinates, because a light is not a node and does not ride on one.
        // To the ceiling exactly, rather than to nearly the ceiling. The wire is 6 mm across and it was
        // stopping 50 mm short, which is eight times its own width of daylight at the top of it — quite
        // enough to read as a lamp hanging off nothing.
        var flex = RoomHeight - BulbOffset.Y;

        movement.Children.Add(new MeshNode(Primitives.Cylinder(0.006f, 0.006f, flex, 6), Fabric.DarkMetal)
        {
            Name = "flex",
            Position = BulbOffset with { Y = BulbOffset.Y + flex * 0.5f },
            CastsShadow = false
        });

        movement.Children.Add(new MeshNode(Primitives.Sphere(0.075f, 20, 12), flame)
        {
            Name = "bulb",
            Position = BulbOffset,
            CastsShadow = false
        });

        return movement;
    }

    /// <summary>
    /// A spoked wheel with cut teeth, lying in the X-Y plane so it turns about Z.
    ///
    /// <paramref name="phase"/> turns the ring of teeth without turning the wheel under it, which is what
    /// puts a gap where the other wheel's tooth arrives. See <see cref="GreatRadius"/>.
    /// </summary>
    private static Node Wheel(float radius, int teeth, int spokes, float phase, Material iron)
    {
        var wheel = new Node { Name = "wheel" };
        var flat = new Vector3(90f, 0f, 0f);

        wheel.Children.Add(new MeshNode(
            Turned(Primitives.Torus(radius, 0.035f, 40, 8)), Lathe(iron, MathF.Tau * radius, MathF.Tau * 0.035f))
        {
            Name = "rim",
            RotationDegrees = flat
        });

        wheel.Children.Add(new MeshNode(Turned(Primitives.Cylinder(0.07f, 0.07f, 0.09f, 14)), Lathe(iron, 0.44f, 0.09f))
        {
            Name = "hub",
            RotationDegrees = flat
        });

        // One arm and one tooth, hung as many times as this wheel has of each.
        var arm = Block(0.04f, radius, 0.03f, iron, Vector3.Zero, Finish.Close);
        var cog = Block(0.05f, ToothHeight, 0.05f, iron, Vector3.Zero, Finish.Close);

        for (var i = 0; i < spokes; i++)
        {
            var spoke = new Node { RotationDegrees = new Vector3(0f, 0f, i * 360f / spokes) };

            spoke.Children.Add(new MeshNode(arm, iron)
            {
                Position = new Vector3(0f, radius * 0.5f, 0f)
            });

            wheel.Children.Add(spoke);
        }

        for (var i = 0; i < teeth; i++)
        {
            var tooth = new Node { RotationDegrees = new Vector3(0f, 0f, phase + i * 360f / teeth) };

            tooth.Children.Add(new MeshNode(cog, iron)
            {
                Position = new Vector3(0f, radius + ToothSeat, 0f)
            });

            wheel.Children.Add(tooth);
        }

        return wheel;
    }

    /// <summary>
    /// The pendulum, hung from a bracket near the ceiling so that the bob swings in the middle of the
    /// beam. It swings across the light rather than along it, which is the difference between a shadow
    /// that travels and one that only grows.
    /// </summary>
    private Node Pendulum(Material iron, Material brass)
    {
        var suspension = new Vector3(-0.1f, PendulumPivot, -0.55f);
        var mount = new Node { Name = "suspension" };

        var bracket = suspension with { Y = PendulumPivot + 0.09f };

        mount.Children.Add(new MeshNode(Block(0.46f, 0.12f, 0.3f, iron, bracket, Finish.Close), iron)
        {
            Name = "bracket",
            Position = bracket,
            CastsShadow = false
        });

        // And a strap from the bracket up to the ceiling, because the bracket was hanging from nothing:
        // its top is at 4.39, the nearest ceiling beam starts at 5.16, and the three quarters of a metre
        // between them is air the lantern lights and the eye reads at once.
        var strap = RoomHeight - (PendulumPivot + 0.15f);
        var hanger = suspension with { Y = PendulumPivot + 0.15f + strap * 0.5f };

        mount.Children.Add(new MeshNode(Block(0.10f, strap, 0.10f, iron, hanger, Finish.Close), iron)
        {
            Name = "hanger",
            Position = hanger,
            CastsShadow = false
        });

        _pendulum = new Node { Name = "pendulum", Position = suspension };

        _pendulum.Children.Add(new MeshNode(
            Turned(Primitives.Cylinder(0.017f, 0.017f, PendulumLength, 10)), Lathe(iron, 0.107f, PendulumLength))
        {
            Name = "rod",
            Position = new Vector3(0f, -PendulumLength * 0.5f, 0f)
        });

        // Turned on a lathe, so the brushing runs round the bob: its own coordinates, once round the
        // edge and once across the face.
        _pendulum.Children.Add(new MeshNode(
            Turned(Primitives.Cylinder(0.30f, 0.30f, 0.055f, 40)), Lathe(brass, MathF.Tau * 0.30f, 0.60f))
        {
            Name = "bob",
            Position = new Vector3(0f, -PendulumLength, 0f),
            RotationDegrees = new Vector3(0f, 0f, 90f)
        });

        _pendulum.Children.Add(new MeshNode(
            Turned(Primitives.Cylinder(0.055f, 0.055f, 0.07f, 12)), Lathe(brass, 0.35f, 0.11f))
        {
            Name = "rating nut",
            Position = new Vector3(0f, -PendulumLength - 0.33f, 0f)
        });

        mount.Children.Add(_pendulum);

        return mount;
    }

    /// <summary>
    /// A bucket standing in the beam: a tube with a bottom and no lid, on the floor.
    ///
    /// Both halves of that matter — an open shell casts here, and its shadow starts at the rim of its own
    /// base rather than a hand's breadth away from it. Galvanised, because it is the one thing in the room
    /// that is not a hundred years old.
    /// </summary>
    private static Node Bucket(Material zinc)
    {
        var bucket = new Node { Name = "bucket", Position = new Vector3(0.75f, 0f, 0.85f) };

        bucket.Children.Add(new MeshNode(
            Turned(Primitives.Cylinder(0.215f, 0.165f, 0.30f, 24, capped: false)), Lathe(zinc, 1.20f, 0.30f))
        {
            Name = "pail",
            Position = new Vector3(0f, 0.15f, 0f)
        });

        bucket.Children.Add(new MeshNode(Turned(Primitives.Disc(0.165f, 24)), Lathe(zinc, 0.33f, 0.33f))
        {
            Name = "base",
            Position = new Vector3(0f, 0.006f, 0f)
        });

        bucket.Children.Add(new MeshNode(
            Turned(Primitives.Torus(0.205f, 0.011f, 24, 8, sweepDegrees: 180f)), Lathe(zinc, 0.65f, 0.07f))
        {
            Name = "handle",
            Position = new Vector3(0f, 0.26f, 0f),

            // About X, and not about Z. A torus is built lying in the room's own X-Z plane, sweeping from
            // +X round through +Z, so a quarter turn about Z stands the arc up in the plane that contains
            // the pail's axis: both ends land on that axis, one in the air above the bucket and one down
            // inside it, with the arc bulging out sideways. A quarter turn the other way about X tips the
            // same arc onto its side. The ends come to rest at ±0.205, which is where the tapering wall is
            // at this height, and the apex goes over the top where a bail belongs.
            RotationDegrees = new Vector3(-90f, 0f, 0f)
        });

        return bucket;
    }

    /// <summary>
    /// The weight, which is the other half of a tower clock and the only part of it with a reason to be in
    /// the beam: it hangs where it can fall, and where it can fall is the empty middle of the tower. The
    /// line is lit as far up as the top of the window reaches and then goes into the dark, which is a
    /// thing this room does to everything tall.
    ///
    /// Rope rather than wire, and iron rather than stone. A stone weight on a wire is a mediaeval clock
    /// and this movement has a deadbeat escapement; a cast weight on a laid rope is what hung in every
    /// tower from the eighteenth century on, and the rope is the one thing in the room that is not stone,
    /// metal or timber.
    /// </summary>
    private static Node Weight(Material iron)
    {
        var drive = new Node { Name = "drive weight", Position = new Vector3(-2.2f, 0f, 1.25f) };

        // One image of rope is one lay, and a twenty-two millimetre rope lays every thirty-two.
        var rope = Finish.Rope();
        rope.UvScale = new Vector2(1f, 4.42f / 0.032f);

        drive.Children.Add(new MeshNode(Turned(Primitives.Cylinder(0.011f, 0.011f, 4.42f, 8)), rope)
        {
            Name = "line",
            Position = new Vector3(0f, 3.19f, 0f)
        });

        drive.Children.Add(new MeshNode(
            Turned(Primitives.Cylinder(0.175f, 0.175f, 0.62f, 20)), Lathe(iron, 1.10f, 0.62f))
        {
            Name = "weight",
            Position = new Vector3(0f, 0.66f, 0f)
        });

        drive.Children.Add(new MeshNode(Turned(Primitives.Torus(0.05f, 0.014f, 16, 8)), Lathe(iron, 0.31f, 0.09f))
        {
            Name = "eye",
            Position = new Vector3(0f, 1.01f, 0f),
            RotationDegrees = new Vector3(90f, 0f, 0f)
        });

        return drive;
    }

    /// <summary>
    /// The keeper's things, which are what make this a room somebody works in rather than a room with a
    /// clock in it: a ladder up to the dial, a bench under the lantern with the oil and the spanners on it,
    /// a stool, a spare coil of the drive line, the winding crank on its hook and a broom in the corner.
    ///
    /// Where each one stands is decided by the light and not by the furniture. The ladder leans beside
    /// the window with its inner rail just inside the opening, so a hand's breadth of every rung is in
    /// the sun and the floor gets a ladder drawn along the edge of the beam. The stool and the coil stand
    /// in the corners of the lit patch, outside the dial's drawing, and cast into it. The bench is against
    /// the back wall under the lantern, where the sun never reaches, and everything on it is there to be
    /// seen behind the wheels in the lantern's light — and, for the seconds the lantern holds the map, to
    /// be seen doing without a shadow while the movement beside it has one.
    /// </summary>
    private static Node Keeper(Material oak, Material iron, Material brass, Material zinc)
    {
        var keeper = new Node { Name = "keeper" };

        keeper.Children.Add(Ladder(oak));
        keeper.Children.Add(Bench(oak, iron, brass, zinc));
        keeper.Children.Add(Stool(oak, new Vector3(-1.75f, 0f, 1.45f)));

        // A spare coil, dropped inside the beam near the foot of the ladder. One image of rope is one lay,
        // so the scale round the coil is its circumference in lays.
        var rope = Finish.Rope();
        rope.UvScale = new Vector2(MathF.Tau * 0.20f / 0.032f, 1f);

        keeper.Children.Add(new MeshNode(Turned(Primitives.Torus(0.20f, 0.034f, 36, 10)), rope)
        {
            Name = "coil",
            Position = new Vector3(-1.35f, 0.034f, -1.22f)
        });

        // The winding crank on a hook above the bench: a square-ended shaft hanging down, the arm along
        // the wall and the handle standing out from it. Hung rather than laid down, because a crank on a
        // bench is a bar and a crank on a hook is a crank.
        var crank = new Node { Name = "crank", Position = new Vector3(-0.30f, 1.36f, -RoomHalf + 0.05f) };

        crank.Children.Add(new MeshNode(Block(0.022f, 0.06f, 0.05f, iron, Vector3.Zero, Finish.Close), iron)
        {
            Name = "hook",
            Position = new Vector3(0f, 0.44f, -0.02f)
        });

        crank.Children.Add(new MeshNode(
            Turned(Primitives.Cylinder(0.014f, 0.014f, 0.42f, 10)), Lathe(iron, 0.09f, 0.42f))
        {
            Name = "crank.shaft",
            Position = new Vector3(0f, 0.21f, 0f)
        });

        crank.Children.Add(new MeshNode(Block(0.26f, 0.026f, 0.024f, iron, Vector3.Zero, Finish.Close), iron)
        {
            Name = "crank.arm",
            Position = new Vector3(0.13f, 0f, 0f)
        });

        crank.Children.Add(new MeshNode(
            Turned(Primitives.Cylinder(0.016f, 0.016f, 0.13f, 10)), Lathe(oak, 0.10f, 0.13f))
        {
            Name = "crank.handle",
            Position = new Vector3(0.26f, 0f, 0.065f),
            RotationDegrees = new Vector3(90f, 0f, 0f)
        });

        keeper.Children.Add(crank);

        // A broom in the corner by the window, head down and leaning on the wall.
        var broom = new Node
        {
            Name = "broom",
            Position = new Vector3(-3.52f, 0f, -3.45f),
            RotationDegrees = new Vector3(0f, 0f, 9f)
        };

        broom.Children.Add(new MeshNode(
            Turned(Primitives.Cylinder(0.013f, 0.013f, 1.45f, 8)), Lathe(oak, 0.08f, 1.45f))
        {
            Name = "broom.stick",
            Position = new Vector3(0f, 0.83f, 0f)
        });

        var bristles = Finish.Cloth(0.58f, 0.48f, 0.30f);

        broom.Children.Add(new MeshNode(Block(0.07f, 0.11f, 0.30f, bristles, Vector3.Zero, Finish.Close), bristles)
        {
            Name = "broom.head",
            Position = new Vector3(0f, 0.055f, 0f)
        });

        keeper.Children.Add(broom);

        return keeper;
    }

    /// <summary>
    /// A ladder up to the dial, leaning on the window's surround beside the opening.
    ///
    /// On the surround rather than on the wall, because the surround stands a hand's breadth proud of it:
    /// a ladder through a lintel is the kind of thing a picture forgives and a walk does not. The foot is
    /// where the lean puts it, and the lean is what a ladder has when it is safe to climb.
    /// </summary>
    private static Node Ladder(Material oak)
    {
        const float length = 4.95f;
        const float lean = 14f;
        const float half = 0.20f;

        var tilt = float.DegreesToRadians(lean);

        // The rails pass the surround's face at the top of the lintel, and the foot follows from that.
        const float face = -RoomHalf + 0.28f;
        var foot = face + 0.03f + OpeningTop * MathF.Tan(tilt);

        var ladder = new Node
        {
            Name = "ladder",
            Position = new Vector3(foot, 0f, -1.62f),
            RotationDegrees = new Vector3(0f, 0f, lean)
        };

        var rail = Block(0.045f, length, 0.075f, oak, Vector3.Zero, Finish.Pitch);

        foreach (var side in new[] { -1f, 1f })
            ladder.Children.Add(new MeshNode(rail, oak)
            {
                Name = "rail",
                Position = new Vector3(0f, length * 0.5f, side * half)
            });

        var rung = Turned(Primitives.Cylinder(0.017f, 0.017f, half * 2f, 10));
        var round = Lathe(oak, 0.107f, half * 2f);

        for (var y = 0.32f; y < length - 0.2f; y += 0.33f)
            ladder.Children.Add(new MeshNode(rung, round)
            {
                Name = "rung",
                Position = new Vector3(0f, y, 0f),
                RotationDegrees = new Vector3(90f, 0f, 0f)
            });

        return ladder;
    }

    /// <summary>
    /// The keeper's bench against the back wall under the lantern, with the oil can, a tin of grease, a
    /// rag and two spanners on it. Nothing here is in the sun; it is what the lantern is for.
    /// </summary>
    private static Node Bench(Material oak, Material iron, Material brass, Material zinc)
    {
        var at = new Vector3(-0.75f, 0f, -RoomHalf + 0.38f);
        var bench = new Node { Name = "bench", Position = at };

        const float top = 0.86f;

        bench.Children.Add(new MeshNode(Block(1.50f, 0.055f, 0.58f, oak, at), oak)
        {
            Name = "bench.top",
            Position = new Vector3(0f, top - 0.0275f, 0f)
        });

        var leg = Block(0.07f, top - 0.055f, 0.07f, oak, Vector3.Zero, Finish.Close);

        foreach (var x in new[] { -0.66f, 0.66f })
        foreach (var z in new[] { -0.22f, 0.22f })
            bench.Children.Add(new MeshNode(leg, oak)
            {
                Name = "bench.leg",
                Position = new Vector3(x, (top - 0.055f) * 0.5f, z)
            });

        var stretcher = Block(1.32f, 0.05f, 0.05f, oak, Vector3.Zero, Finish.Close);

        foreach (var z in new[] { -0.22f, 0.22f })
            bench.Children.Add(new MeshNode(stretcher, oak)
            {
                Name = "bench.stretcher",
                Position = new Vector3(0f, 0.18f, z)
            });

        // The oil can: a tapered body, a neck and a long spout, which is the silhouette that says what
        // it is from across a room.
        var can = new Node { Name = "oil can", Position = new Vector3(-0.48f, top, 0.06f) };

        can.Children.Add(new MeshNode(
            Turned(Primitives.Cylinder(0.062f, 0.088f, 0.15f, 20)), Lathe(brass, 0.47f, 0.15f))
        {
            Name = "can.body",
            Position = new Vector3(0f, 0.075f, 0f)
        });

        can.Children.Add(new MeshNode(
            Turned(Primitives.Cylinder(0.022f, 0.030f, 0.05f, 12)), Lathe(brass, 0.16f, 0.05f))
        {
            Name = "can.neck",
            Position = new Vector3(0f, 0.175f, 0f)
        });

        can.Children.Add(new MeshNode(
            Turned(Primitives.Cylinder(0.004f, 0.011f, 0.26f, 8)), Lathe(brass, 0.05f, 0.26f))
        {
            Name = "can.spout",
            Position = new Vector3(0.10f, 0.20f, 0f),
            RotationDegrees = new Vector3(0f, 0f, -52f)
        });

        bench.Children.Add(can);

        // A tin of grease with the lid on.
        bench.Children.Add(new MeshNode(
            Turned(Primitives.Cylinder(0.08f, 0.08f, 0.10f, 20)), Lathe(zinc, 0.50f, 0.10f))
        {
            Name = "grease",
            Position = new Vector3(0.22f, top + 0.05f, -0.10f)
        });

        // A rag, and two spanners, one across the other.
        var rag = Finish.Cloth(0.60f, 0.54f, 0.42f);

        bench.Children.Add(new MeshNode(Block(0.36f, 0.028f, 0.26f, rag, Vector3.Zero, Finish.Close), rag)
        {
            Name = "rag",
            Position = new Vector3(0.52f, top + 0.014f, 0.10f),
            RotationDegrees = new Vector3(0f, 24f, 0f)
        });

        var spanner = Block(0.30f, 0.012f, 0.05f, iron, Vector3.Zero, Finish.Close);

        bench.Children.Add(new MeshNode(spanner, iron)
        {
            Name = "spanner",
            Position = new Vector3(-0.08f, top + 0.006f, 0.16f),
            RotationDegrees = new Vector3(0f, -28f, 0f)
        });

        bench.Children.Add(new MeshNode(spanner, iron)
        {
            Name = "spanner",
            Position = new Vector3(-0.02f, top + 0.018f, 0.14f),
            RotationDegrees = new Vector3(0f, 41f, 0f)
        });

        return bench;
    }

    /// <summary>A three-legged stool, the legs splayed the way a turned leg is set into a round seat.</summary>
    private static Node Stool(Material oak, Vector3 at)
    {
        var stool = new Node { Name = "stool", Position = at };

        stool.Children.Add(new MeshNode(
            Turned(Primitives.Cylinder(0.17f, 0.17f, 0.035f, 18)), Lathe(oak, 1.07f, 0.035f))
        {
            Name = "seat",
            Position = new Vector3(0f, 0.4525f, 0f)
        });

        var leg = Turned(Primitives.Cylinder(0.019f, 0.024f, 0.45f, 8));
        var turned = Lathe(oak, 0.13f, 0.45f);

        for (var i = 0; i < 3; i++)
        {
            var a = float.DegreesToRadians(i * 120f + 30f);

            // Eight degrees out from under the seat. A turn about Z moves the top of a leg towards -X and
            // a turn about X moves it towards +Z, so the two together, weighted by where the leg stands,
            // lean every top towards the middle.
            stool.Children.Add(new MeshNode(leg, turned)
            {
                Name = "leg",
                Position = new Vector3(MathF.Cos(a) * 0.125f, 0.225f, MathF.Sin(a) * 0.125f),
                RotationDegrees = new Vector3(-MathF.Sin(a) * 8f, 0f, MathF.Cos(a) * 8f)
            });
        }

        return stool;
    }

    /// <summary>One mote's seed: where it entered the opening, how far along its ray it started, and how it moves.</summary>
    private readonly record struct Mote(float Y, float Z, float Along, float Fall, float Drift, float Sway);

    /// <summary>
    /// The dust in the beam: nine hundred motes, each on its own ray in from the window, drifting.
    ///
    /// A beam through a window is not visible; what is visible is what is in it, and a tower room has a
    /// century of it. Each mote is a point on a ray that enters the opening at some height and offset and
    /// runs along the sun until it meets the floor — which is the beam exactly, because that is what the
    /// beam is — and where it is on that ray is a function of the second, so a capture at any time is the
    /// same picture on every renderer and a film that seeks lands on the frame it left. Additive and
    /// unlit, sized in the room's own metres so the near ones are bigger, and drawn only as bright as the
    /// sun is: dust in a dark room is not there. Not in the map, either — a point casts nothing.
    /// </summary>
    private Node Dust()
    {
        var random = new Random(1729);

        _seeds = new Mote[Motes];
        _motes = new Vector3[Motes];

        for (var i = 0; i < Motes; i++)
            _seeds[i] = new Mote(
                Y: OpeningBottom + random.NextSingle() * (OpeningTop - OpeningBottom),
                Z: (random.NextSingle() * 2f - 1f) * (OpeningHalf - 0.05f),
                Along: random.NextSingle(),
                Fall: 0.006f + 0.010f * random.NextSingle(),
                Drift: 0.010f + 0.025f * random.NextSingle(),
                Sway: random.NextSingle() * MathF.Tau);

        _dust = new PointsNode
        {
            Name = "dust",
            Positions = _motes,
            Color = new Vector3(1f, 0.88f, 0.66f),
            Opacity = MoteOpacity,
            Size = 0.004f,
            SizeAttenuation = true,
            Blend = BlendMode.Additive,
            DepthWrite = false
        };

        Settle(0f);

        return _dust;
    }

    /// <summary>Every mote where it is at this second.</summary>
    private void Settle(float t)
    {
        if (_dust is null)
            return;

        var ray = SunDirection;
        var opening = OpeningTop - OpeningBottom;

        for (var i = 0; i < _motes.Length; i++)
        {
            var seed = _seeds[i];

            // Settling slowly down the opening, and swaying a little across it.
            var y = OpeningBottom + Wrap(seed.Y - OpeningBottom - seed.Fall * t, opening);
            var z = seed.Z + 0.06f * MathF.Sin(seed.Sway + t * 0.23f);

            // The ray from that point in the opening to the floor, and how far along it this one is —
            // drifting inwards with the light, and back to the window when it reaches the flags.
            var reach = y / -ray.Y;
            var along = Wrap(seed.Along * reach + seed.Drift * t, reach);

            _motes[i] = new Vector3(-RoomHalf, y, z) + ray * along;
        }

        _dust.InvalidateGeometry();
    }

    /// <summary>The cloud at this second: in over a second and a half, over for three and a half, gone over two.</summary>
    private void Weather(float t)
    {
        var phase = Wrap(t, CloudPeriod);

        Cloud = Smooth((phase - CloudArrives) / (CloudCovers - CloudArrives))
              * (1f - Smooth((phase - CloudClears) / (CloudGone - CloudClears)));

        // Only while the cloud is fully over, and only inside that by the hand-over at each end.
        Handed = Overcast
            ? Math.Clamp(MathF.Min(phase - CloudCovers, CloudClears - phase) / HandOver, 0f, 1f)
            : 0f;
    }

    /// <summary>The sky outside and the dust inside, set from the sun: both are only there because it is.</summary>
    private void Shade()
    {
        if (_daylight is not null)
        {
            // Not to nothing. The sky keeps a twentieth at the bottom of a fade, because the opening is a
            // hole in a wall with an outside behind it, and an outside that is black is a hole into space.
            // Under the cloud it goes grey rather than dark: an overcast sky is still the brightest thing
            // a window shows.
            var sky = 0.05f + 0.95f * _day;
            var clear = new Vector3(1f, 0.97f, 0.90f);
            var overcast = new Vector3(0.50f, 0.52f, 0.55f);
            var colour = Vector3.Lerp(clear, overcast, Cloud) * sky;

            _daylight.BaseColor = new Vector4(colour, 1f);
        }

        if (_dust is not null)
            _dust.Opacity = MoteOpacity * Sun;
    }

    /// <summary>
    /// Gives the map to the lantern, or back to the sun.
    ///
    /// A point light shadows a cone aimed at the middle of whatever casts, as wide as it has to be to take
    /// all of it in — see ShadowView — so with a room's worth of casters and the bulb inside the room, the
    /// cone is as wide as one can be and the map is spread over most of the tower: thirty millimetres a
    /// texel, and a spoke is thinner than that. With only the movement casting the cone closes to what is
    /// under the lamp and a texel comes down to a few millimetres. So while the lantern holds the map
    /// nothing else casts; the sun is at nothing whenever that is true, and there is nothing for a wall to
    /// block.
    /// </summary>
    private void Hand(bool toLantern)
    {
        if (_handed == toLantern)
            return;

        _handed = toLantern;

        foreach (var (node, casts) in _bystanders)
            node.CastsShadow = casts && !toLantern;
    }

    /// <summary>
    /// Registers everything under <paramref name="root"/> that casts as a bystander to the hand-over,
    /// skipping <paramref name="except"/> and what is under it. Public so a room mounting this can add
    /// the wall it builds round the tower — the one caster in the room that is not the tower's own.
    /// </summary>
    public void Bystander(Node root, Node? except = null)
    {
        if (root == except)
            return;

        if (root is MeshNode { CastsShadow: true } mesh)
            _bystanders.Add((mesh, true));

        foreach (var child in root.Children)
            Bystander(child, except);
    }

    private static float Smooth(float x)
    {
        x = Math.Clamp(x, 0f, 1f);
        return x * x * (3f - 2f * x);
    }

    private static float Wrap(float value, float span) => value - span * MathF.Floor(value / span);

    /// <summary>
    /// One panel of the window wall: a plane standing in the Y-Z plane at −RoomHalf.
    ///
    /// A plane is built lying flat and stood up by a quarter turn about Z, which turns its width into the
    /// panel's height and leaves its depth running along Z. The turn is baked into the mesh rather than
    /// hung on the node, so that <see cref="Fabric.Map"/> sees the panel the way the room does — courses
    /// along Z, joints stacking down Y — instead of a floor lying on its side. Six segments each way is
    /// enough for geometry that is never lit from the front.
    /// </summary>
    private static MeshNode Panel(float height, float width, Vector3 centre, Material stone, string name) =>
        new(Fabric.Map(Stood(Primitives.Plane(height, width, 6, 6), 0f, 90f), stone, centre), stone)
        {
            Name = name,
            Position = centre
        };

    /// <summary>A mesh turned the way a node would turn it, by pitch about X and roll about Z, so that the
    /// turn is in the vertices rather than in the transform.</summary>
    private static Mesh Stood(Mesh mesh, float pitch, float roll) =>
        mesh.Transformed(Matrix4x4.CreateFromYawPitchRoll(0f, float.DegreesToRadians(pitch), float.DegreesToRadians(roll)));

    /// <summary>A box mapped in the room's metres from where it stands. See <see cref="Fabric.Map"/>.</summary>
    private static Mesh Block(float width, float height, float depth, Material of, Vector3 at, float metres = Finish.Pitch) =>
        Fabric.Map(Primitives.Box(width, height, depth), of, at, metres);

    /// <summary>A turned part, ready for a normal map: the primitive's own coordinates and the tangents
    /// that go with them.</summary>
    private static Mesh Turned(Mesh mesh) => mesh.WithGeneratedTangents();

    /// <summary>
    /// A material for a turned part — a ring, a rod, a drum — that takes the image on the part's own
    /// coordinates, so many times round and so many times along.
    ///
    /// The room's grid, which everything flat here is mapped by, has no answer for a torus: a surface that
    /// faces every way at once gets a different projection on every face and a seam wherever two meet. A
    /// primitive's own coordinates run once round and once along it, and a material is one clone away from
    /// scaling that to the metres the image was drawn for. The textures are shared by identity, so a clone
    /// is a handful of numbers and not an upload.
    /// </summary>
    private static Material Lathe(Material of, float around, float along, float metres = Finish.Close)
    {
        var turned = of.Clone();
        turned.UvScale = new Vector2(around / metres, along / metres);
        return turned;
    }
}