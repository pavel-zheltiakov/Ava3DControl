using System.Numerics;

namespace Ava3D.Demo.Scenes.Contact;

/// <summary>
/// How a ship moves: a Catmull–Rom path, a heading taken from the tangent, and a bank read off a track.
///
/// <b>Where a ship points is where it is going.</b> The heading is always the tangent of the curve the
/// ship is actually on — never a direction kept alongside the position, never slerped between two
/// orientations — so there is no second quantity that can drift out of step with the first and no way
/// for a hull to crab, skid or fly sideways. That is not a thing this file checks for; it is a thing it
/// cannot express.
///
/// The path underneath is not drawn, it is flown. <c>tools/models/fly-paths.py</c> integrates each of
/// the five combat ships as a rigid body — a mass, a moment of inertia, a main engine on the centreline
/// and manoeuvring thrusters at the stern — and the waypoints are where it got to. Nothing in that model
/// rotates a ship: an attitude error becomes a torque, a torque becomes an angular acceleration, and
/// what the nose does is whatever Euler's equation says it does. The main engines are the only source of
/// thrust and they point along the hull, so velocity is speed times the direction the ship faces and can
/// be nothing else.
///
/// The bank is the part that matters to the eye. A ship that changes heading without rolling into it
/// reads as a model being dragged along a curve, and no amount of detail on the hull fixes that. It is a
/// rigid body too — the roll thrusters push the hull toward the lean the autopilot wants, against its own
/// moment of inertia — and because that has no closed form it is integrated once, offline, and shipped
/// in Paths.cs beside the waypoints.
///
/// So the film is still a pure function of time. Nothing integrates at runtime, nothing accumulates,
/// nothing remembers the last frame, and the same second of the loop looks the same every time round.
/// </summary>
internal static class Flight
{
    /// <summary>
    /// How long one turn of <c>u</c> takes. <c>u</c> runs 0..1 over the film, so this is what turns a
    /// rate expressed per unit-u into one per second — which gunnery needs and nothing else does.
    /// </summary>
    public const float CycleSeconds = 60f;

    /// <summary>
    /// A position, an orientation, and how fast it is going. What a node needs, plus the one thing that
    /// cannot be recovered from a node afterwards.
    /// </summary>
    internal readonly record struct Pose(
        Vector3 Position, Quaternion Rotation, Vector3 Forward, Vector3 Velocity);

    /// <summary>
    /// The pose at <paramref name="u"/>: where the ship is, which way it points, and how far it is
    /// rolled into its turn.
    ///
    /// <b>The nose is the trajectory's own tangent, and that is the whole of it.</b> Not a heading kept
    /// beside the position and stepped along with it, not an orientation slerped between two poses —
    /// the direction the curve is going, this instant. So a ship cannot fly sideways: there is no
    /// second quantity to get out of step with the first, because there is only one.
    ///
    /// That tangent used to be two secants taken a thousandth of a cycle either side and averaged, with
    /// the step size a constant in this file and a fallback for where the secant came out zero.
    /// <see cref="Spline.Direction"/> is the same quantity differentiated rather than sampled, so the
    /// step size is gone — and with it the question of whether it was small enough to be a derivative
    /// and large enough not to be noise.
    ///
    /// The bank is read from <paramref name="roll"/> rather than worked out here. It used to be
    /// computed from the shape of the path — speed squared times curvature, through an arctangent,
    /// averaged over a nine-tap window to hide the step a Catmull–Rom's discontinuous curvature puts
    /// at every knot. All of that is gone. A ship's roll is now the state of a rigid body pushed by
    /// its roll thrusters, which has no closed form, so <c>tools/models/fly-paths.py</c> integrates it
    /// once and Paths.cs carries the result. Nothing integrates at runtime, the film is still a pure
    /// function of time, and the smoothing that used to be a filter is now a moment of inertia.
    /// </summary>
    /// <remarks>
    /// Every ship in the film goes through here, wingmen included, and that is the point.
    ///
    /// A wingman used to be posed as "the leader's pose, plus an offset in the leader's frame", which
    /// is how formation flying is usually written down and is a trap. It makes the wingman's motion a
    /// function of the leader's <i>attitude</i>, so the derivatives that give a heading and a bank end
    /// up measuring an eight-hundred-unit arm swinging rather than a ship flying. Both came out badly:
    /// the escorts rolled 34° and 41° <i>per frame</i>, continuously, while the freighter they were
    /// sitting on rolled a tenth of a degree, and their noses oscillated through fifteen degrees of
    /// pitch about two and a half times a second.
    ///
    /// Neither was fixable in the pose. The fix is to stop treating a wingman as a special case: the
    /// formation slot is baked into its <i>waypoints</i> by <c>tools/models/fly-paths.py</c>, so an
    /// escort flies an ordinary path like everybody else and this function has one job again. The slot
    /// is still held — the escorts' splines stay within 7 and 12 units of the true station on an
    /// 800-unit arm — and it is held by a curve, which cannot oscillate about something it is not
    /// looking at.
    ///
    /// One piece of the same bug outlived that fix and is worth recording, because it is the reason
    /// this was reported three separate times. Baking the slot into waypoints removed the arm from the
    /// <i>runtime</i>, but the slot was still being <i>computed</i> in the leader's rolled frame, so the
    /// arm was still in the waypoints: Kestrel banking through its turn swung the two stations 216 and
    /// 267 units vertically, and the wingmen's noses went with them. A slot belongs in the leader's
    /// heading frame. A wingman keeps station on a flight path, not on how far the ship in front has
    /// rolled.
    ///
    /// None of that is reachable any more, which is the point of writing it down. A wingman's motion
    /// is not a function of anything the leader does: it is a rigid body under its own thrusters, on a
    /// path of its own, and the slot only ever decides where that path begins.
    /// </remarks>
    public static Pose Follow(Vector3[] path, float[] roll, float u, bool closed = true)
    {
        var here = Spline.Sample(path, u, closed);
        var forward = Spline.Direction(path, u, closed);

        // A trajectory that is momentarily stationary has no direction to give — a freighter at the end
        // of its dock — and a ship still has to point somewhere. Down its own −Z is the model's rest
        // attitude, which is the least surprising thing to hold for the frame or two it lasts.
        if (forward == Vector3.Zero) forward = -Vector3.UnitZ;

        // Per second, not per unit-u: the only consumer is gunnery, which works in seconds.
        var velocity = Spline.Tangent(path, u, closed) / CycleSeconds;

        return new Pose(here, Orient(forward, Spline.Sample(roll, u, closed)), forward, velocity);
    }

    /// <summary>
    /// The attitude of a ship flying along <paramref name="forward"/> with its wings rolled
    /// <paramref name="bank"/> radians out of level.
    ///
    /// <see cref="Rotations.LookAlong"/> is the wings-level part — nose onto the heading, top as near
    /// world up as that heading allows — and everything left here is the bank, which is a flight-model
    /// idea rather than a geometric one and so stays in the demo. It is applied on the left, so it
    /// happens about the nose the heading already established rather than about the model's own −Z.
    ///
    /// Two things this is not. It is not Euler angles: those have an order to remember, a singularity to
    /// stay away from, a wrap to handle, and no way to answer "how far is this attitude from that one",
    /// which is the question the autopilot asks every tick and which one quaternion multiplication
    /// answers. And it is not a problem straight up or straight down, where <c>LookAlong</c> gives up on
    /// levelling: no ship on these paths flies vertically, and the film would rather have the singularity
    /// documented in the library than a special case here pretending it is not there.
    /// </summary>
    public static Quaternion Orient(Vector3 forward, float bank)
    {
        var f = Normalize(forward);
        var level = Rotations.LookAlong(f);

        return bank == 0f ? level : Quaternion.CreateFromAxisAngle(f, -bank) * level;
    }

    private static Vector3 Normalize(Vector3 v)
    {
        var length = v.Length();
        return length > 1e-5f ? v / length : -Vector3.UnitZ;
    }
}
