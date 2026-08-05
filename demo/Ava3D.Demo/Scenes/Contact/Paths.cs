using System.Numerics;

namespace Ava3D.Demo.Scenes.Contact;

/// <summary>
/// Where everybody flies.
///
/// Two kinds of path live here and they are made in two different ways.
///
/// <b>Kestrel's is written by hand</b>, thirteen waypoints evenly spaced over the sixty-second cycle,
/// so waypoint <c>i</c> is reached at second <c>i × 5</c>. That is the whole scheduling mechanism, and
/// it is the right one for a freighter: a ship under way from A to B really is flying a plan somebody
/// filed, and putting it somewhere at a particular second is a matter of counting waypoints rather
/// than solving for arc length.
///
/// <b>The five ships that fight are flown</b>, by <c>tools/models/fly-paths.py</c>, and their
/// forty-nine waypoints are a second and a quarter apart. Each is a <i>rigid body</i> — a mass, a
/// moment of inertia, a main engine on the centreline and manoeuvring thrusters at the stern —
/// integrated forward while its autopilot steers its nose at whatever it is trying to kill. Nothing in
/// that model rotates a hull: an attitude error becomes a torque, a torque becomes an angular
/// acceleration, and what the nose does is whatever Euler's equation says it does. The waypoints are
/// just where it got to. Nobody placed them, and the numbers below are therefore not worth reading —
/// the flight plans are in the tool, this file is its output, and <c>--write</c> puts it here.
///
/// <b>A ship goes where it points.</b> The main engines are the only source of thrust and they lie
/// along the hull, so a ship's velocity is its speed times the direction it is facing and can be
/// nothing else — there is no sideslip, no crabbing and no drift, because there is no second quantity
/// that could disagree with the first. The model keeps a speed and an attitude and no velocity vector
/// at all. Measured back off these splines, every nose sits on its own track to four decimal places.
///
/// The reason for flying them rather than drawing them is the guns. They are bolted to the hulls and
/// fire along the nose, so a ship can only shoot what it is pointing at, which makes "is there a
/// fight?" a question about control rather than about draughtsmanship. Hand-placed waypoints answered
/// it badly: 0.2 seconds of firing solution across an eighteen-second engagement, with tracers leaving
/// ships sideways. Steering at the lead point *is* pointing at the target, so flying the paths turned
/// that into 21.9 seconds spread over twelve pairings, without anybody choosing a coordinate.
///
/// Three other things come free with it. Angular <i>velocity</i> is what gets integrated, so it is
/// continuous whatever the aim point does, and no two consecutive legs can double back — which two
/// hand-drawn escort paths did, by 148° and 174°, and which reads on screen as a fighter flipping end
/// over end in a single frame. A bounded pitch command against a hull with that much inertia is a nose
/// that stays where a viewer expects it: steering in three unbounded dimensions swept the wingmen's
/// noses through 113° of pitch and took 233° of it back and forth over the cycle, which is the "ships
/// move nose up and down" this film was reported for three times. And an attack run ends where the
/// geometry says it ends: each raider's start is bisected until its closest approach to Kestrel lands
/// on the second the shot list wants, instead of being speed times time, which ignores the two hundred
/// a second the freighter is closing at and put every raider four seconds early.
///
/// What is still checked rather than trusted is everything the flight model does not know about.
/// <c>tools/models/check-flight.py</c> samples every path against Relay Nine's actual mesh and fails
/// if any ship's envelope reaches the station's skin; <c>check-motion.py</c> fails on a cusp, on an
/// attitude that steps between frames, on a nose off its velocity, or on two hulls in the same place;
/// <c>check-guns.py</c> reports who has a solution on whom, and asserts that the two ships the film
/// kills were being shot at when they died.
/// </summary>
internal static class Paths
{
    /// <summary>
    /// The freighter: a long straight run in, then a decelerating turn onto the docking axis.
    ///
    /// The last five legs are 1,010, 880, 560, 265 and 125 units, each a little over half the one
    /// before. That is the deceleration — uniform Catmull–Rom gives every leg the same five seconds, so
    /// leg length <i>is</i> speed, and a ship slows by having its waypoints crowd together. Ending on a
    /// geometric run rather than on one abrupt short leg is the difference between a freighter coming
    /// alongside and a freighter hitting a wall.
    ///
    /// It is also the one path that goes anywhere near the station, and the only one allowed to: it
    /// comes down the docking axis from +Z, which is the one direction Relay Nine is open in, and stops
    /// with its nose at the collar.
    ///
    /// The altitudes are chosen so the <i>pitch</i> never reverses, which is a stronger condition than
    /// it sounds and is not what they used to be. The freighter descended from 500 to 20 and climbed
    /// back to 150 — through the station's own altitude and out the other side — so its nose went down,
    /// came up, and settled, and the eye reads that as a ship bobbing rather than a ship arriving. Now
    /// the drop shrinks every leg (71, 61, 48, 37, 28, 21, 16, 11, 5, 2, 0), which against legs that are
    /// also shrinking gives a pitch that eases monotonically from two degrees nose-down to level. A
    /// freighter that never has to pull up never looks like it is falling.
    /// </summary>
    public static readonly Vector3[] Kestrel =
    [
        new(-7_000f, 450f, 5_200f),     //  0 s
        new(-5_600f, 379f, 3_700f),     //  5
        new(-4_200f, 318f, 2_400f),     // 10  the convoy shot opens here
        new(-2_900f, 270f, 1_350f),     // 15
        new(-1_700f, 233f, 500f),       // 20
        new(-700f, 205f, -250f),        // 25  contact
        new(200f, 184f, -900f),         // 30
        new(1_150f, 168f, -1_250f),     // 35
        new(2_000f, 157f, -1_470f),     // 40  coming round onto the approach
        new(2_530f, 152f, -1_640f),     // 45  lining up on the axis
        new(2_740f, 150f, -1_800f),     // 50  on the axis, slowing
        new(2_795f, 150f, -1_910f),     // 55  in the slot between the arms
        new(2_800f, 150f, -1_970f)      // 60  nose at the collar, barely moving
    ];

    /// <summary>
    /// Harrier One: holds the port slot, breaks at twenty-two seconds and works Raider A.
    ///
    /// The first eighteen waypoints are the formation slot in Kestrel's heading frame, sampled at the
    /// knots — so the spline the fighter flies and the station it is supposed to be holding are the
    /// same curve to within seven units on an eight-hundred-unit arm. Everything after that is flown:
    /// it steers at Raider A's lead point until thirty-seven seconds, then at whatever is left, and
    /// then out along a bearing away from the station.
    ///
    /// It holds a solution on Raider A for 2.20 of the three seconds the raider spends taking hits,
    /// which is what earns the kill at thirty-six. Not because anybody arranged for it — because the
    /// raider breaks, and a nose stays on a turning ship. <c>check-guns.py</c> asserts exactly that,
    /// rather than reporting it: a ship that dies while nobody is shooting at it is the fight failing
    /// in the one place the audience is looking, and it is not something a summary line will catch.
    /// </summary>
    public static readonly Vector3[] HarrierOne =
    [
        new(-7_785f, 395f, 5_045f),   // 0 s
        new(-7_535f, 382f, 4_779f),
        new(-7_174f, 363f, 4_382f),
        new(-6_767f, 342f, 3_936f),
        new(-6_379f, 323f, 3_518f),   // 5 s
        new(-6_023f, 306f, 3_156f),
        new(-5_665f, 290f, 2_809f),
        new(-5_311f, 275f, 2_478f),
        new(-4_965f, 261f, 2_164f),   // 10 s
        new(-4_625f, 247f, 1_863f),
        new(-4_292f, 235f, 1_581f),
        new(-3_966f, 223f, 1_315f),
        new(-3_647f, 211f, 1_063f),   // 15 s
        new(-3_326f, 201f, 812f),
        new(-3_015f, 191f, 586f),
        new(-2_718f, 182f, 383f),
        new(-2_441f, 173f, 198f),   // 20 s
        new(-2_179f, 165f, 12f),
        new(-1_856f, 155f, -237f),
        new(-1_603f, 112f, -706f),
        new(-1_735f, 20f, -1_227f),   // 25 s
        new(-1_826f, -74f, -1_754f),
        new(-1_493f, -141f, -2_158f),
        new(-960f, -154f, -2_279f),
        new(-473f, -122f, -2_073f),   // 30 s
        new(-374f, -34f, -1_558f),
        new(-506f, 42f, -1_030f),
        new(-420f, 102f, -497f),
        new(-220f, 154f, 13f),   // 35 s
        new(-115f, 196f, 550f),
        new(-123f, 233f, 1_098f),
        new(-69f, 259f, 1_637f),
        new(361f, 257f, 1_940f),   // 40 s
        new(838f, 223f, 1_723f),
        new(933f, 227f, 1_196f),
        new(871f, 296f, 655f),
        new(905f, 357f, 110f),   // 45 s
        new(934f, 419f, -436f),
        new(964f, 481f, -982f),
        new(993f, 542f, -1_527f),
        new(1_023f, 604f, -2_073f),   // 50 s
        new(1_047f, 668f, -2_619f),
        new(1_061f, 735f, -3_165f),
        new(1_082f, 799f, -3_710f),
        new(1_111f, 861f, -4_256f),   // 55 s
        new(1_141f, 923f, -4_802f),
        new(1_171f, 984f, -5_348f),
        new(1_201f, 1_046f, -5_893f),
        new(1_231f, 1_107f, -6_439f),   // 60 s
    ];

    /// <summary>Harrier Two: the starboard slot, and the same job on Raider B.</summary>
    public static readonly Vector3[] HarrierTwo =
    [
        new(-6_814f, 517f, 6_027f),   // 0 s
        new(-6_553f, 504f, 5_751f),
        new(-6_196f, 485f, 5_357f),
        new(-5_802f, 464f, 4_924f),
        new(-5_443f, 445f, 4_533f),   // 5 s
        new(-5_108f, 428f, 4_191f),
        new(-4_770f, 412f, 3_860f),
        new(-4_432f, 397f, 3_543f),
        new(-4_101f, 383f, 3_242f),   // 10 s
        new(-3_785f, 369f, 2_959f),
        new(-3_472f, 356f, 2_692f),
        new(-3_163f, 344f, 2_439f),
        new(-2_858f, 333f, 2_197f),   // 15 s
        new(-2_572f, 322f, 1_969f),
        new(-2_277f, 312f, 1_754f),
        new(-1_977f, 303f, 1_549f),
        new(-1_676f, 294f, 1_348f),   // 20 s
        new(-1_397f, 286f, 1_150f),
        new(-1_072f, 273f, 901f),
        new(-666f, 193f, 560f),
        new(-229f, 97f, 264f),   // 25 s
        new(154f, 4f, -101f),
        new(469f, -89f, -524f),
        new(686f, -181f, -1_007f),
        new(877f, -243f, -1_506f),   // 30 s
        new(1_003f, -274f, -2_023f),
        new(741f, -290f, -2_466f),
        new(227f, -291f, -2_468f),
        new(-31f, -259f, -2_026f),   // 35 s
        new(217f, -216f, -1_575f),
        new(688f, -191f, -1_319f),
        new(1_091f, -175f, -966f),
        new(1_509f, -170f, -631f),   // 40 s
        new(1_800f, -145f, -195f),
        new(1_619f, -68f, 280f),
        new(1_129f, 18f, 465f),
        new(651f, 70f, 266f),   // 45 s
        new(300f, 98f, -140f),
        new(-182f, 128f, -333f),
        new(-598f, 193f, -39f),
        new(-635f, 271f, 485f),   // 50 s
        new(-637f, 354f, 1_012f),
        new(-793f, 439f, 1_519f),
        new(-739f, 531f, 2_031f),
        new(-289f, 589f, 2_272f),   // 55 s
        new(169f, 616f, 2_024f),
        new(518f, 644f, 1_616f),
        new(945f, 673f, 1_292f),
        new(1_372f, 703f, 967f),   // 60 s
    ];

    /// <summary>
    /// The raider that dies: a run at the freighter, and then the break that kills it.
    ///
    /// It steers at Kestrel's lead point until 32.5 seconds and then hauls away up and to port. The
    /// break is the point of it. An attacker that carries straight on through a convoy is impossible to
    /// shoot with a fixed gun — the aspect changes too fast for anything to hold its nose on — and is
    /// also, in a story, a ship nobody has to deal with. Turning is what makes it catchable, which is
    /// why gun kills happen in turns.
    ///
    /// Everything past thirty-six seconds is flown by nobody: it is a tumbling wreck from there and its
    /// position comes from ballistics instead. It is kept clean anyway, because a path that is only
    /// correct while somebody is watching is one that will be wrong the day the timing changes.
    /// </summary>
    public static readonly Vector3[] RaiderA =
    [
        new(-378f, -1_666f, -12_347f),   // 0 s
        new(-412f, -1_593f, -11_855f),
        new(-565f, -1_534f, -11_383f),
        new(-714f, -1_473f, -10_910f),
        new(-854f, -1_410f, -10_434f),   // 5 s
        new(-990f, -1_347f, -9_957f),
        new(-1_117f, -1_282f, -9_478f),
        new(-1_235f, -1_216f, -8_996f),
        new(-1_344f, -1_150f, -8_513f),   // 10 s
        new(-1_442f, -1_082f, -8_028f),
        new(-1_530f, -1_012f, -7_540f),
        new(-1_605f, -942f, -7_051f),
        new(-1_666f, -870f, -6_560f),   // 15 s
        new(-1_712f, -796f, -6_068f),
        new(-1_740f, -721f, -5_574f),
        new(-1_748f, -644f, -5_080f),
        new(-1_734f, -566f, -4_587f),   // 20 s
        new(-1_697f, -486f, -4_095f),
        new(-1_634f, -404f, -3_605f),
        new(-1_539f, -320f, -3_122f),
        new(-1_407f, -234f, -2_648f),   // 25 s
        new(-1_241f, -147f, -2_184f),
        new(-1_044f, -60f, -1_733f),
        new(-800f, 26f, -1_305f),
        new(-519f, 98f, -899f),   // 30 s
        new(-383f, 150f, -425f),
        new(-330f, 181f, 71f),
        new(-125f, 214f, 524f),
        new(38f, 241f, 992f),   // 35 s
        new(-17f, 262f, 1_484f),
        new(-289f, 298f, 1_896f),
        new(-714f, 368f, 2_142f),
        new(-1_180f, 456f, 2_302f),   // 40 s
        new(-1_642f, 543f, 2_470f),
        new(-2_105f, 630f, 2_638f),
        new(-2_568f, 716f, 2_806f),
        new(-3_031f, 803f, 2_974f),   // 45 s
        new(-3_494f, 890f, 3_142f),
        new(-3_957f, 977f, 3_310f),
        new(-4_419f, 1_064f, 3_478f),
        new(-4_882f, 1_151f, 3_646f),   // 50 s
        new(-5_345f, 1_237f, 3_815f),
        new(-5_808f, 1_324f, 3_983f),
        new(-6_271f, 1_411f, 4_151f),
        new(-6_734f, 1_498f, 4_319f),   // 55 s
        new(-7_196f, 1_585f, 4_487f),
        new(-7_659f, 1_672f, 4_655f),
        new(-8_122f, 1_758f, 4_823f),
        new(-8_585f, 1_845f, 4_991f),   // 60 s
    ];

    /// <summary>The second raider: the same run four seconds later, and Harrier Two takes it.</summary>
    public static readonly Vector3[] RaiderB =
    [
        new(8_823f, -1_514f, -11_129f),   // 0 s
        new(8_494f, -1_465f, -10_765f),
        new(8_155f, -1_422f, -10_408f),
        new(7_818f, -1_376f, -10_050f),
        new(7_484f, -1_330f, -9_690f),   // 5 s
        new(7_154f, -1_284f, -9_326f),
        new(6_825f, -1_237f, -8_961f),
        new(6_497f, -1_189f, -8_594f),
        new(6_171f, -1_140f, -8_226f),   // 10 s
        new(5_847f, -1_091f, -7_858f),
        new(5_523f, -1_040f, -7_489f),
        new(5_200f, -988f, -7_119f),
        new(4_879f, -935f, -6_748f),   // 15 s
        new(4_561f, -880f, -6_374f),
        new(4_243f, -824f, -6_000f),
        new(3_931f, -767f, -5_622f),
        new(3_634f, -708f, -5_231f),   // 20 s
        new(3_350f, -650f, -4_832f),
        new(3_052f, -606f, -4_441f),
        new(2_688f, -603f, -4_109f),
        new(2_303f, -603f, -3_801f),   // 25 s
        new(1_972f, -560f, -3_439f),
        new(1_747f, -483f, -3_008f),
        new(1_555f, -394f, -2_561f),
        new(1_381f, -309f, -2_107f),   // 30 s
        new(1_240f, -223f, -1_642f),
        new(1_154f, -138f, -1_164f),
        new(1_254f, -76f, -689f),
        new(1_559f, -47f, -308f),   // 35 s
        new(2_004f, -24f, -104f),
        new(2_475f, 4f, 38f),
        new(2_853f, 34f, 347f),
        new(3_046f, 66f, 795f),   // 40 s
        new(3_011f, 100f, 1_282f),
        new(2_755f, 134f, 1_697f),
        new(2_345f, 171f, 1_967f),
        new(1_911f, 208f, 2_198f),   // 45 s
        new(1_480f, 245f, 2_436f),
        new(1_048f, 283f, 2_674f),
        new(617f, 320f, 2_911f),
        new(186f, 358f, 3_148f),   // 50 s
        new(-245f, 395f, 3_386f),
        new(-677f, 432f, 3_623f),
        new(-1_108f, 470f, 3_861f),
        new(-1_539f, 507f, 4_098f),   // 55 s
        new(-1_971f, 545f, 4_336f),
        new(-2_402f, 582f, 4_573f),
        new(-2_833f, 619f, 4_811f),
        new(-3_264f, 657f, 5_048f),   // 60 s
    ];

    /// <summary>
    /// The third: the last run in, pressed until the freighter is nearly at the collar, and the only
    /// raider still flying at the end.
    /// </summary>
    public static readonly Vector3[] RaiderC =
    [
        new(5_454f, -2_793f, -16_950f),   // 0 s
        new(5_260f, -2_704f, -16_431f),
        new(4_992f, -2_629f, -15_942f),
        new(4_737f, -2_550f, -15_447f),
        new(4_484f, -2_471f, -14_951f),   // 5 s
        new(4_234f, -2_390f, -14_453f),
        new(3_989f, -2_309f, -13_954f),
        new(3_748f, -2_226f, -13_452f),
        new(3_511f, -2_143f, -12_949f),   // 10 s
        new(3_280f, -2_058f, -12_443f),
        new(3_053f, -1_972f, -11_936f),
        new(2_833f, -1_884f, -11_426f),
        new(2_619f, -1_796f, -10_913f),   // 15 s
        new(2_411f, -1_705f, -10_398f),
        new(2_212f, -1_614f, -9_880f),
        new(2_021f, -1_521f, -9_359f),
        new(1_838f, -1_426f, -8_836f),   // 20 s
        new(1_662f, -1_329f, -8_310f),
        new(1_495f, -1_231f, -7_782f),
        new(1_338f, -1_134f, -7_251f),
        new(1_192f, -1_036f, -6_717f),   // 25 s
        new(1_058f, -938f, -6_179f),
        new(939f, -841f, -5_638f),
        new(835f, -743f, -5_094f),
        new(751f, -645f, -4_547f),   // 30 s
        new(695f, -548f, -3_996f),
        new(670f, -450f, -3_442f),
        new(676f, -352f, -2_888f),
        new(719f, -255f, -2_336f),   // 35 s
        new(818f, -157f, -1_792f),
        new(875f, -64f, -1_243f),
        new(720f, 1f, -708f),
        new(614f, 56f, -162f),   // 40 s
        new(590f, 119f, 396f),
        new(384f, 203f, 909f),
        new(111f, 301f, 1_391f),
        new(-153f, 399f, 1_878f),   // 45 s
        new(-418f, 497f, 2_364f),
        new(-683f, 594f, 2_851f),
        new(-948f, 692f, 3_337f),
        new(-1_213f, 790f, 3_824f),   // 50 s
        new(-1_478f, 887f, 4_310f),
        new(-1_743f, 985f, 4_797f),
        new(-2_008f, 1_083f, 5_283f),
        new(-2_273f, 1_180f, 5_770f),   // 55 s
        new(-2_538f, 1_278f, 6_256f),
        new(-2_803f, 1_376f, 6_743f),
        new(-3_068f, 1_473f, 7_229f),
        new(-3_332f, 1_571f, 7_716f),   // 60 s
    ];

    /// <summary>
    /// How far each ship is rolled into its turn, in radians, every half second of the cycle.
    ///
    /// The bank used to be worked out at runtime from the shape of the path — speed squared times
    /// curvature, through an arctangent, then averaged over a nine-tap window. It is a track now
    /// because it stopped being a function of the path and became the state of a rigid body: the
    /// autopilot asks for a lean proportional to how fast the flight path is turning, and the roll
    /// thrusters push the hull toward it against its own moment of inertia. There is no closed form
    /// for where that gets to; you have to integrate it, and integrating it every frame would make the
    /// film stop being a pure function of time. So it is integrated once, by
    /// <c>tools/models/fly-paths.py</c>, and sampled here.
    ///
    /// Two things fall out of that which the old arrangement had to work for. The nine-tap window is
    /// gone, because a torque cannot move a body's angle discontinuously — the curvature step that a
    /// Catmull–Rom has at every knot is still in what the autopilot asks for, and it simply cannot
    /// reach the screen. And the arctangent is gone, which is what used to put a twenty-two degree
    /// lean on a bend of ten kilometres' radius and needed a gain five times the physics to suppress.
    ///
    /// Half a second is dense enough that reading it back through the same curve as the waypoints
    /// costs under a tenth of a degree against the integrator, at a hundred and twenty-one numbers a
    /// ship. They are generated, like the waypoints, and nobody reads them either.
    /// </summary>
    public static readonly float[] KestrelRoll =
    [
        -0.0167f, -0.0169f, -0.0168f, -0.0160f, -0.0145f, -0.0125f, -0.0100f, -0.0073f,
        -0.0043f, -0.0009f, +0.0031f, +0.0071f, +0.0103f, +0.0128f, +0.0147f, +0.0161f,
        +0.0171f, +0.0176f, +0.0179f, +0.0180f, +0.0179f, +0.0179f, +0.0183f, +0.0187f,
        +0.0191f, +0.0194f, +0.0195f, +0.0194f, +0.0191f, +0.0187f, +0.0180f, +0.0181f,
        +0.0191f, +0.0202f, +0.0209f, +0.0209f, +0.0201f, +0.0184f, +0.0158f, +0.0124f,
        +0.0082f, +0.0039f, +0.0002f, -0.0027f, -0.0048f, -0.0060f, -0.0066f, -0.0064f,
        -0.0055f, -0.0041f, -0.0023f, -0.0021f, -0.0039f, -0.0058f, -0.0066f, -0.0057f,
        -0.0027f, +0.0023f, +0.0092f, +0.0180f, +0.0283f, +0.0394f, +0.0497f, +0.0581f,
        +0.0641f, +0.0676f, +0.0686f, +0.0673f, +0.0637f, +0.0581f, +0.0507f, +0.0447f,
        +0.0418f, +0.0402f, +0.0388f, +0.0371f, +0.0346f, +0.0311f, +0.0265f, +0.0207f,
        +0.0136f, +0.0071f, +0.0026f, -0.0007f, -0.0036f, -0.0065f, -0.0097f, -0.0134f,
        -0.0180f, -0.0234f, -0.0300f, -0.0389f, -0.0500f, -0.0621f, -0.0740f, -0.0849f,
        -0.0936f, -0.0993f, -0.1010f, -0.0974f, -0.0877f, -0.0811f, -0.0838f, -0.0920f,
        -0.1027f, -0.1133f, -0.1219f, -0.1264f, -0.1250f, -0.1159f, -0.0981f, -0.0843f,
        -0.0822f, -0.0864f, -0.0931f, -0.0996f, -0.1041f, -0.1049f, -0.1005f, -0.0886f,
        -0.0668f,
    ];

    public static readonly float[] HarrierOneRoll =
    [
        +0.0493f, +0.0473f, +0.0296f, +0.0004f, -0.0194f, -0.0200f, -0.0127f, -0.0051f,
        +0.0016f, +0.0058f, +0.0210f, +0.0435f, +0.0573f, +0.0584f, +0.0573f, +0.0541f,
        +0.0511f, +0.0484f, +0.0452f, +0.0435f, +0.0392f, +0.0324f, +0.0310f, +0.0374f,
        +0.0440f, +0.0468f, +0.0482f, +0.0480f, +0.0462f, +0.0454f, +0.0367f, +0.0188f,
        +0.0166f, +0.0386f, +0.0605f, +0.0676f, +0.0688f, +0.0654f, +0.0566f, +0.0512f,
        +0.0236f, -0.0088f, -0.0373f, -0.0299f, -0.0022f, -0.1187f, -0.2479f, -0.3545f,
        -0.4766f, -0.5631f, -0.4500f, -0.2945f, -0.0995f, +0.1642f, +0.3787f, +0.5079f,
        +0.5828f, +0.6133f, +0.6264f, +0.6292f, +0.6473f, +0.6864f, +0.6882f, +0.6496f,
        +0.6075f, +0.3691f, +0.0532f, -0.1689f, -0.2808f, -0.3385f, -0.2233f, -0.0369f,
        +0.1088f, +0.2242f, +0.2988f, +0.2181f, +0.1387f, -0.0036f, -0.2262f, -0.4123f,
        -0.5433f, -0.6415f, -0.6875f, -0.7187f, -0.7324f, -0.6619f, -0.5817f, -0.4601f,
        -0.2064f, +0.0101f, +0.0775f, +0.0509f, +0.0267f, +0.0139f, +0.0067f, +0.0030f,
        +0.0015f, +0.0008f, +0.0010f, +0.0017f, -0.0039f, -0.0109f, -0.0178f, -0.0290f,
        -0.0383f, -0.0270f, -0.0075f, +0.0079f, +0.0225f, +0.0328f, +0.0297f, +0.0205f,
        +0.0132f, +0.0070f, +0.0029f, +0.0010f, +0.0003f, +0.0001f, -0.0000f, -0.0001f,
        -0.0001f,
    ];

    public static readonly float[] HarrierTwoRoll =
    [
        +0.0510f, +0.0490f, +0.0306f, -0.0001f, -0.0214f, -0.0228f, -0.0151f, -0.0078f,
        -0.0032f, -0.0012f, +0.0156f, +0.0410f, +0.0568f, +0.0590f, +0.0583f, +0.0552f,
        +0.0522f, +0.0494f, +0.0463f, +0.0446f, +0.0391f, +0.0301f, +0.0281f, +0.0356f,
        +0.0432f, +0.0470f, +0.0490f, +0.0492f, +0.0482f, +0.0481f, +0.0358f, +0.0105f,
        +0.0068f, +0.0349f, +0.0618f, +0.0725f, +0.0759f, +0.0725f, +0.0625f, +0.0552f,
        +0.0265f, -0.0074f, -0.0363f, -0.0596f, -0.0709f, -0.0854f, -0.1137f, -0.0898f,
        +0.0114f, +0.1022f, +0.0510f, -0.0645f, -0.1559f, -0.2176f, -0.2545f, -0.2894f,
        -0.3313f, -0.3319f, -0.2514f, -0.1829f, -0.1724f, -0.1208f, -0.1621f, -0.3093f,
        -0.4460f, -0.5530f, -0.6414f, -0.6848f, -0.7179f, -0.7347f, -0.7362f, -0.7495f,
        -0.7352f, -0.7035f, -0.6702f, -0.4551f, -0.1449f, +0.0392f, +0.0193f, -0.0391f,
        +0.0418f, +0.1476f, +0.2686f, +0.4247f, +0.5474f, +0.6186f, +0.6656f, +0.6783f,
        +0.6846f, +0.6866f, +0.6573f, +0.6440f, +0.5192f, +0.2075f, -0.0727f, -0.3083f,
        -0.4951f, -0.6018f, -0.6675f, -0.6980f, -0.5724f, -0.4048f, -0.2369f, +0.0027f,
        +0.1870f, +0.1146f, -0.0607f, -0.2355f, -0.4161f, -0.5480f, -0.6285f, -0.6914f,
        -0.6964f, -0.6385f, -0.5858f, -0.3780f, -0.0975f, +0.0716f, +0.0767f, +0.0516f,
        +0.0313f,
    ];

    public static readonly float[] RaiderARoll =
    [
        -0.3096f, -0.2952f, -0.1831f, +0.0038f, +0.1606f, +0.1748f, +0.1063f, +0.0523f,
        +0.0102f, -0.0171f, -0.0269f, -0.0257f, -0.0257f, -0.0302f, -0.0341f, -0.0371f,
        -0.0392f, -0.0406f, -0.0417f, -0.0423f, -0.0434f, -0.0442f, -0.0454f, -0.0475f,
        -0.0491f, -0.0513f, -0.0532f, -0.0551f, -0.0575f, -0.0592f, -0.0620f, -0.0644f,
        -0.0671f, -0.0711f, -0.0740f, -0.0775f, -0.0804f, -0.0831f, -0.0865f, -0.0889f,
        -0.0925f, -0.0952f, -0.0985f, -0.1033f, -0.1067f, -0.1133f, -0.1192f, -0.1258f,
        -0.1364f, -0.1448f, -0.1473f, -0.1470f, -0.1457f, -0.1414f, -0.1368f, -0.1489f,
        -0.1698f, -0.1810f, -0.1977f, -0.2102f, -0.0765f, +0.1135f, +0.2400f, +0.2955f,
        +0.3195f, +0.1694f, -0.0593f, -0.1752f, -0.1136f, -0.0447f, +0.1029f, +0.2623f,
        +0.3749f, +0.4527f, +0.4992f, +0.5211f, +0.5422f, +0.5288f, +0.4590f, +0.4055f,
        +0.2906f, +0.1495f, +0.0627f, +0.0240f, +0.0071f, +0.0005f, -0.0013f, -0.0014f,
        -0.0013f, -0.0010f, -0.0007f, -0.0004f, -0.0002f, -0.0001f, -0.0000f, -0.0000f,
        -0.0000f, +0.0000f, +0.0000f, +0.0000f, +0.0000f, +0.0000f, +0.0000f, +0.0000f,
        +0.0000f, +0.0000f, +0.0000f, +0.0000f, -0.0000f, -0.0000f, -0.0000f, -0.0000f,
        -0.0000f, -0.0000f, -0.0000f, -0.0000f, -0.0000f, -0.0000f, -0.0000f, -0.0000f,
        +0.0000f,
    ];

    public static readonly float[] RaiderBRoll =
    [
        -0.0476f, -0.0455f, -0.0285f, +0.0044f, +0.0322f, +0.0310f, +0.0149f, +0.0025f,
        -0.0056f, -0.0103f, -0.0149f, -0.0199f, -0.0214f, -0.0185f, -0.0157f, -0.0132f,
        -0.0112f, -0.0100f, -0.0091f, -0.0086f, -0.0084f, -0.0087f, -0.0084f, -0.0067f,
        -0.0053f, -0.0048f, -0.0042f, -0.0049f, -0.0076f, -0.0099f, -0.0124f, -0.0159f,
        -0.0160f, -0.0100f, -0.0041f, -0.0086f, -0.0135f, -0.0242f, -0.0471f, -0.0660f,
        -0.0727f, -0.0813f, -0.0696f, -0.0352f, -0.0112f, +0.0663f, +0.1673f, +0.2186f,
        +0.2102f, +0.1967f, +0.0919f, -0.0335f, -0.1386f, -0.2421f, -0.3157f, -0.3006f,
        -0.2432f, -0.1979f, -0.1481f, -0.1126f, -0.1080f, -0.1136f, -0.1283f, -0.1415f,
        -0.1418f, -0.2156f, -0.3104f, -0.3845f, -0.4484f, -0.4905f, -0.5143f, -0.5415f,
        -0.5256f, -0.4369f, -0.3689f, -0.1656f, +0.0702f, +0.2477f, +0.3782f, +0.4595f,
        +0.5040f, +0.5308f, +0.5407f, +0.5474f, +0.5504f, +0.5440f, +0.5496f, +0.5174f,
        +0.3901f, +0.2976f, +0.1938f, +0.0899f, +0.0301f, +0.0084f, +0.0016f, -0.0008f,
        -0.0016f, -0.0014f, -0.0011f, -0.0007f, -0.0005f, -0.0002f, -0.0001f, -0.0001f,
        -0.0000f, -0.0000f, +0.0000f, +0.0000f, +0.0000f, +0.0000f, +0.0000f, +0.0000f,
        +0.0000f, +0.0000f, +0.0000f, +0.0000f, +0.0000f, +0.0000f, -0.0000f, -0.0000f,
        -0.0000f,
    ];

    public static readonly float[] RaiderCRoll =
    [
        -0.2055f, -0.1960f, -0.1222f, +0.0078f, +0.1171f, +0.1184f, +0.0573f, +0.0148f,
        -0.0034f, -0.0117f, -0.0140f, -0.0127f, -0.0125f, -0.0150f, -0.0173f, -0.0188f,
        -0.0197f, -0.0202f, -0.0204f, -0.0204f, -0.0207f, -0.0207f, -0.0211f, -0.0222f,
        -0.0230f, -0.0239f, -0.0246f, -0.0252f, -0.0260f, -0.0265f, -0.0275f, -0.0283f,
        -0.0293f, -0.0312f, -0.0326f, -0.0335f, -0.0340f, -0.0343f, -0.0343f, -0.0343f,
        -0.0341f, -0.0336f, -0.0337f, -0.0342f, -0.0347f, -0.0361f, -0.0375f, -0.0391f,
        -0.0416f, -0.0434f, -0.0460f, -0.0485f, -0.0505f, -0.0522f, -0.0533f, -0.0563f,
        -0.0589f, -0.0623f, -0.0674f, -0.0708f, -0.0794f, -0.0887f, -0.0963f, -0.1040f,
        -0.1094f, -0.1131f, -0.1145f, -0.1168f, -0.1195f, -0.1205f, -0.1380f, -0.1789f,
        -0.1677f, -0.0885f, -0.0283f, +0.1039f, +0.2651f, +0.3182f, +0.1842f, +0.0493f,
        -0.0664f, -0.1873f, -0.1770f, -0.0011f, +0.1716f, +0.2434f, +0.2613f, +0.2400f,
        +0.1459f, +0.0655f, +0.0247f, +0.0096f, +0.0031f, +0.0003f, -0.0005f, -0.0007f,
        -0.0006f, -0.0005f, -0.0003f, -0.0002f, -0.0001f, -0.0000f, -0.0000f, -0.0000f,
        -0.0000f, +0.0000f, +0.0000f, +0.0000f, +0.0000f, +0.0000f, +0.0000f, +0.0000f,
        +0.0000f, +0.0000f, +0.0000f, +0.0000f, -0.0000f, -0.0000f, -0.0000f, -0.0000f,
        -0.0000f,
    ];
}
