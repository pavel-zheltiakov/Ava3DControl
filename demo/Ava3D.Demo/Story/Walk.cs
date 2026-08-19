using System.Numerics;

namespace Ava3D.Demo.Story;

/// <summary>Where the visitor's eye is and what it is aimed at.</summary>
internal readonly record struct Pose(Vector3 Eye, Vector3 Look);

/// <summary>
/// One instant of a walk: when, where he is standing, and what he is looking at.
/// </summary>
/// <param name="Seconds">Seconds into the chapter.</param>
/// <param name="Eye">Where his eye is. Y is eye height, not floor height.</param>
/// <param name="Look">What he is looking at.</param>
internal readonly record struct Step(float Seconds, Vector3 Eye, Vector3 Look);

/// <summary>
/// A scripted walk through a room, evaluated as a pure function of the seconds through the chapter.
///
/// Nothing here integrates and nothing accumulates, which is the same rule <c>Contact</c>'s flight model
/// follows and it is not a stylistic preference. It buys three things at once. The walk is identical on
/// every run, so it can be asserted about rather than watched. It is unaffected by a dropped frame, a
/// slow build or a renderer switch, so the film does not drift when the machine is busy. And it can be
/// <i>seeked</i>: to be eleven seconds into a chapter is to stand where eleven seconds puts you, with no
/// state to wind forward to get there, which is the entire reason the picker can jump to the moment a
/// feature is on screen instead of having to build a separate scene for it.
///
/// A walk is written as an eye and a subject, because a film camera is an eye and a subject and
/// <see cref="Camera.LookFrom"/> takes exactly that. <b>What is interpolated is not the subject.</b> See
/// <see cref="Aim"/> — the authoring is in points and the motion is in angles, and the two are not the same
/// thing at all.
/// </summary>
internal sealed class Walk
{
    /// <summary>
    /// The speed below which somebody is not walking, in metres a second.
    ///
    /// A person crossing a room at a tenth of a metre a second is not taking small steps — they are leaning,
    /// turning on the spot, or shifting their weight, and none of those puts a foot down. The number is here
    /// rather than in the score because it is a fact about walking and two unrelated things need it: the
    /// footfalls, which must not fire while he is leaning over a bench, and <c>Ground.Audit</c>, which reports
    /// how much of each chapter is a walk at all.
    /// </summary>
    public const float Standing = 0.12f;

    private readonly Step[] _steps;

    public Walk(params Step[] steps)
    {
        if (steps.Length == 0)
            throw new ArgumentException("a walk needs at least one step", nameof(steps));

        _steps = steps;
    }

    /// <summary>A walk that does not walk: one pose, held.</summary>
    public static Walk Still(Vector3 eye, Vector3 look) => new(new Step(0f, eye, look));

    /// <summary>When the last step is reached.</summary>
    public float Duration => _steps[^1].Seconds;

    /// <summary>
    /// Where he is at <paramref name="seconds"/>, and what he is looking at.
    ///
    /// <b>He eases where he starts and stops, and nowhere else.</b> Every segment used to be smoothstepped,
    /// which sounds right — a walk that begins and ends at full speed reads as a camera being dragged — and
    /// is wrong for a reason that only shows up over a long room: smoothstep's derivative is zero at
    /// <i>both</i> ends, so a walk written as eight waypoints comes to a complete halt eight times. The
    /// corridor is the case that made it undeniable. Its waypoints are there so his head can pick up the next
    /// beacon, not because he stops walking, and a man who has just been told to go somewhere was stopping
    /// dead every three metres to look at a lamp.
    ///
    /// So the easing asks what the neighbouring segments are doing. Between two segments he is walking on,
    /// the eye goes straight through at speed; where the walk begins, or arrives, or a waypoint holds him
    /// still to turn his head, it eases into or out of rest exactly as before. The four cases are in
    /// <see cref="Pace"/>, and each one is a cubic that matches the speed of whatever it hands over to.
    ///
    /// The <i>look</i> is not treated this way and keeps its smoothstep. A head does settle on a subject and
    /// then leave it, and easing it is what makes a glance read as a glance — the complaint was never that
    /// he looked at things.
    /// </summary>
    public Pose At(float seconds)
    {
        if (seconds <= _steps[0].Seconds)
            return new Pose(_steps[0].Eye, _steps[0].Look);

        for (var i = 1; i < _steps.Length; i++)
        {
            var b = _steps[i];
            if (seconds > b.Seconds)
                continue;

            var a = _steps[i - 1];
            var span = b.Seconds - a.Seconds;
            var u = span <= 0f ? 1f : Math.Clamp((seconds - a.Seconds) / span, 0f, 1f);

            var eye = Vector3.Lerp(a.Eye, b.Eye, Pace(u, Walking(i - 1), Walking(i + 1)));

            return new Pose(eye, Aim(a.Eye, eye, a.Look, b.Look, Ease(u)));
        }

        var last = _steps[^1];
        return new Pose(last.Eye, last.Look);
    }

    /// <summary>
    /// Whether the segment arriving at step <paramref name="index"/> is one he is walking on.
    ///
    /// Measured in metres a second against <see cref="Standing"/> rather than by comparing two positions,
    /// because the question being asked is the same one the footfalls ask — is this a walk — and it should
    /// have the same answer. A waypoint that shifts the eye two centimetres while the head turns is not a
    /// step and must not stop him from easing to rest.
    ///
    /// Off either end of the walk is rest: he is standing still before the film reaches him and after it
    /// leaves.
    /// </summary>
    private bool Walking(int index)
    {
        if (index <= 0 || index >= _steps.Length)
            return false;

        var span = _steps[index].Seconds - _steps[index - 1].Seconds;

        return span > 0f
               && Vector3.Distance(_steps[index].Eye, _steps[index - 1].Eye) / span > Standing;
    }

    /// <summary>
    /// How far along a segment he is, given whether he arrived at it moving and whether he leaves it moving.
    ///
    /// All four are cubics through (0,0) and (1,1). What differs is the slope at the ends: zero where he is
    /// at rest, and one — the segment's own average speed — where he is not, so the segment hands over to
    /// its neighbour at the speed the neighbour is travelling. Two segments written at the same metres a
    /// second therefore join with no change of pace at all, which is what a corridor wants and is why the
    /// corridor's waypoints are evenly spaced in both distance and time.
    /// </summary>
    private static float Pace(float u, bool from, bool to) => (from, to) switch
    {
        // Standing to standing: the whole segment is an arrival, and this is smoothstep as it always was.
        (false, false) => Ease(u),

        // Walking to walking: straight through, at one speed, with no hesitation in the middle of it.
        (true, true) => u,

        // Setting off, and still going at the end of it.
        (false, true) => u * u * (2f - u),

        // Coming in at speed and stopping.
        (true, false) => u * (1f + u - u * u)
    };

    /// <summary>
    /// The head turning from one subject to another — as an angle, not as a point sliding between them.
    ///
    /// <b>Interpolating the subject is wrong and it is wrong worst exactly where it shows most.</b> Slide a
    /// point from something on your left to something on your right and its path is a straight chord, and
    /// that chord passes near the eye it is being looked at from. Near the eye the direction to it is barely
    /// defined and the <i>height</i> difference between the two subjects stops being a few degrees of pitch
    /// and becomes most of the angle: a subject a third of a metre away and a fifth of a metre lower is
    /// thirty degrees down, and one five centimetres away is nearly straight down. So the shot dives and
    /// recovers, which is what a rotunda of six niches and a gallery with a door behind it both did — and it
    /// is not what a person's head does at all.
    ///
    /// <b>Yaw the short way round, pitch straight across.</b> Decomposing it is what guarantees the fix
    /// rather than merely improving it: pitch goes from where it started to where it is going and cannot
    /// visit anything in between, so there is no arrangement of two subjects that can make the camera look
    /// at the floor on the way past. A slerp along the great circle would be the more usual answer and is
    /// not the right one here — between two nearly level directions half a turn apart, the great circle goes
    /// over the top.
    ///
    /// Half a turn is the one ambiguous case, and it stays ambiguous: the shorter way is both ways. It
    /// resolves consistently rather than correctly, because there is no correct, and a walk that wants the
    /// other one says so with a step in the middle.
    ///
    /// The distance is carried along so the subject stays a subject. Nothing in the film reads it —
    /// <see cref="Camera.LookFrom"/> only needs the direction — but a pose whose target is at some arbitrary
    /// range would be a trap for the first thing that does.
    ///
    /// <b>The subject being left keeps the bearing it had when he left it; the one he is turning to is
    /// tracked as he arrives.</b> That asymmetry is the whole of this method's second fix and it is not a
    /// refinement of the first one — it removes a fault that the yaw decomposition above cannot reach.
    /// Both bearings used to be measured from wherever the eye had got to this frame, which is right for
    /// the subject ahead and catastrophic for the one behind: a walk that goes <i>through</i> what it was
    /// last looking at has a direction-to-it that shrinks to nothing and comes out pointing the other way,
    /// so halfway along the segment the aim swings a hundred and eighty degrees and comes back.
    ///
    /// That is not a hypothetical. Every doorway in this building is a subject — a chapter aims at the way
    /// out, walks up to it and goes through it — so the fault sat on the joins, which are the frames a
    /// viewer is least willing to forgive: <c>Ground.Audit</c> measured sixteen hundred degrees a second
    /// leaving the gallery and twenty-eight hundred leaving the pattern shop, on segments whose authored
    /// turn is under sixty. A head does not do that, and no arrangement of waypoints could have stopped it,
    /// because the waypoints were not what was wrong.
    ///
    /// Freezing the outgoing eye costs the one thing it sounds like it should: a segment written with the
    /// <i>same</i> subject at both ends no longer tracks it exactly through the middle, because half the
    /// answer is now a bearing taken before he moved. The error is zero at both ends by construction, and
    /// in the middle it is half the segment's own turn — three or four degrees on the passes this film
    /// makes past a stand, against a hundred and eighty for the fault it replaces.
    ///
    /// It is also what keeps the joins seamless. At the end of a segment the answer is the bearing from
    /// that step's eye to that step's subject; at the start of the next it is the same bearing measured
    /// from the same two points. The two agree exactly, which is what makes a chapter boundary invisible.
    /// </summary>
    private static Vector3 Aim(Vector3 was, Vector3 eye, Vector3 from, Vector3 to, float u)
    {
        var a = from - was;
        var b = to - eye;

        var ra = a.Length();
        var rb = b.Length();

        // A subject the eye is standing on has no direction to it. It should not happen and the old
        // behaviour is as good as anything if it does.
        if (ra < 1e-4f || rb < 1e-4f)
            return Vector3.Lerp(from, to, u);

        var turn = MathF.Atan2(b.X, b.Z) - MathF.Atan2(a.X, a.Z);

        while (turn > MathF.PI)
            turn -= MathF.Tau;

        while (turn < -MathF.PI)
            turn += MathF.Tau;

        var yaw = MathF.Atan2(a.X, a.Z) + turn * u;

        var pitch = float.Lerp(
            MathF.Asin(Math.Clamp(a.Y / ra, -1f, 1f)),
            MathF.Asin(Math.Clamp(b.Y / rb, -1f, 1f)),
            u);

        var range = float.Lerp(ra, rb, u);
        var flat = MathF.Cos(pitch) * range;

        return eye + new Vector3(MathF.Sin(yaw) * flat, MathF.Sin(pitch) * range, MathF.Cos(yaw) * flat);
    }

    /// <summary>Smoothstep: soft at both ends, and its own derivative is zero there, so two segments meet
    /// without a visible kink.</summary>
    private static float Ease(float u)
    {
        u = Math.Clamp(u, 0f, 1f);
        return u * u * (3f - 2f * u);
    }
}
