using System.Numerics;
using System.Text;

namespace Ava3D.Demo.Story;

/// <summary>
/// The parts of the building nothing can pass through, and the two different questions that are asked of
/// them.
///
/// It is deliberately the crudest thing that works: every solid surface in the rooms that exist right now,
/// kept as the box the renderer already computed for culling. No mesh is consulted, nothing is swept,
/// nothing bounces. That is the correct amount of machinery for this building, and the reason is that the
/// building is one storey of flat deck with boxes standing on it — a wall, a bench, a console, a crate.
/// Everything anyone can hit is already an axis-aligned box, so a full collision system would be a second
/// description of the same thing that could disagree with the first.
///
/// <b>Two questions, and they are not the same question.</b> <see cref="Clearance"/> asks how much room a
/// <i>person</i> has: the visitor is a standing capsule, every solid is a box, and what comes back is the
/// gap between them. That is what the free walk needs. <see cref="Pierces"/> asks whether a <i>point</i> is
/// inside solid geometry, in three dimensions and with no allowance at all. The difference between them is
/// the difference between a camera that sits on a sofa and a camera that goes through a wall — the first is
/// a shot and the second is a mistake, and a single test cannot tell them apart.
///
/// <b><see cref="Audit"/> asks both, and it did not always.</b> Asking only the second one is how the film
/// shipped for a while with its opening walk going straight through the first object anybody sees: the eye
/// cleared the exhibit and the man carrying it did not, and nothing in three dimensions was ever inside
/// anything. A camera is not the only thing on a walk. The pair are reported separately because only one of
/// them is a fault by itself.
///
/// <b>A box is an over-estimate of a round thing, and the audit says so twice.</b> The rotunda's table has a
/// rim at 1.17 and the walk stands 1.5 from its centre, which is a third of a metre clear of the actual
/// table — but at 225° that is x and z of 1.06 each, inside a box whose half-extent is 1.17 on both. The
/// report calls it <c>table.lip</c> and it is not a fault; it is what a bound <i>is</i>, and the alternative
/// is a second description of the geometry that can disagree with the renderer's. The engine room's
/// <c>top</c> is the other kind of not-a-fault: a hundred seconds of a man leaning over a bench at a hundred
/// and fifty millimetres, which is the shot. So is the lounge's <c>roll</c>, which is him sitting down.
///
/// Two filters decide what is solid at all, and each is a claim about the room rather than a tuning number.
/// Only <see cref="MeshNode"/>s count, so the hologram's wireframe, the dust and every sprite are passed
/// through, which is what they are. And anything <see cref="Material.Unlit"/> is skipped: half the
/// furnishing of the last room is additive quads standing in for light lying on a floor — see
/// <c>Illuminator.Gleam</c> — and a reflection you can bump into would be the funniest bug in the film.
/// </summary>
internal sealed class Ground
{
    /// <summary>The capsule's radius. Half a pace, which is a shoulder and a bit of politeness.</summary>
    public const float Reach = 0.34f;

    /// <summary>
    /// The bottom and top of the capsule's axis.
    ///
    /// It does not start at the floor, and that is a step height rather than an anatomical claim: anything
    /// whose top is below the knee is walked over rather than into, which is how a body gets across a
    /// threshold strip or a cable run without the film having to model either. The crown is what lets him
    /// walk under the header over every doorway instead of into it.
    /// </summary>
    private const float Knee = 0.36f;

    private const float Crown = 1.80f;

    private readonly List<(BoundingBox Box, string What)> _solids = [];

    /// <summary>How many solids the last survey found. Printed by the audit; nothing else reads it.</summary>
    public int Count => _solids.Count;

    /// <summary>
    /// Takes the shape of the building as it stands right now.
    ///
    /// Called when the visitor is handed the controls and not per frame, which is the one thing that makes
    /// this affordable: it walks every node in the visible rooms, and the visible rooms are only ever one or
    /// two of them. Nothing moves afterwards except doors, and a door that opens while you are walking
    /// through it is a problem this film does not have.
    /// </summary>
    public void Survey(Scene scene)
    {
        _solids.Clear();

        foreach (var room in scene.Children)
            if (room.IsVisible)
                Gather(room);
    }

    private void Gather(Node node)
    {
        // Everything past the glass. The station is nineteen metres of geometry three hundred metres away
        // and the planet is a sphere six hundred metres across; as solids they would fill the room and every
        // room next to it, and the visitor would find himself unable to move at all with nothing visible to
        // explain why.
        if (!node.IsVisible || node.Name == "outside")
            return;

        if (node is MeshNode { Mesh: not null } mesh && !mesh.Material.Unlit)
        {
            var box = mesh.LocalBounds.Transform(mesh.WorldTransform);

            if (!box.IsEmpty)
                _solids.Add((box, mesh.Name ?? mesh.Material.Name ?? "unnamed"));
        }

        foreach (var child in node.Children)
            Gather(child);
    }

    /// <summary>Whether the visitor's capsule standing here would be inside something.</summary>
    public bool Blocked(float x, float z) => Clearance(x, z).Gap < Reach;

    /// <summary>What a person standing here would be standing in, or null for nothing.</summary>
    public string? Hits(float x, float z) => Meets(x, z, Reach);

    /// <summary>
    /// The same test at a radius the caller chooses, which is how the two strictnesses are one piece of code.
    ///
    /// At <see cref="Reach"/> it is the capsule: <b>does the body intersect the box.</b> That is the question
    /// the free walk has to answer, because a shoulder through a wall is a shoulder through a wall. At zero
    /// it is the capsule's <b>axis</b>: is the centre line itself inside the box. That is the question to ask
    /// of an author's camera, because <b>standing close to a wall is a shot</b> — auditing the scripted walk
    /// at a shoulder's width reported the antechamber against its own south wall, which is a man two hundred
    /// millimetres off the plaster looking the other way, and nine hundred samples of that buried the one
    /// that mattered.
    /// </summary>
    public string? Meets(float x, float z, float radius)
    {
        var (gap, what) = Clearance(x, z);
        return gap < radius ? what : null;
    }

    /// <summary>
    /// The gap between the visitor's capsule axis and the nearest solid, and which solid it is.
    ///
    /// <b>Round, not square.</b> The old test expanded the box by a margin on each axis independently, which
    /// is a square of side twice the margin laid over the box — so at a corner the body was held off by the
    /// diagonal, half again as far as anywhere else. On a wall it is invisible; on a pillar or the end of a
    /// bench it is a body that sticks going past it at an angle and slides free once it is square on, which
    /// is the exact feel of catching on nothing. Measuring the real distance to the box costs one square root
    /// and makes the clearance the same in every direction, which is what a radius means.
    ///
    /// <b>A cylinder rather than a true capsule</b>, and that is on purpose. Rounding the ends as well would
    /// take the bottom cap out to the radius, so a curb a hundred millimetres high — well under the step the
    /// <see cref="Knee"/> exists to allow — would come within a third of a metre of the cap and stop him.
    /// Flat ends and a round side is the shape a character controller actually wants, and the two ends are
    /// where the film's two decisions live: what you step over, and what you duck under.
    ///
    /// Negative when the axis is inside the box, and the magnitude is then how far in — the distance to the
    /// nearest face rather than to the nearest corner, because that is the direction the body came from and
    /// the shortest way back out.
    /// </summary>
    public (float Gap, string? What) Clearance(float x, float z)
    {
        var gap = float.MaxValue;
        string? what = null;

        foreach (var (box, name) in _solids)
        {
            // Does the column a body occupies overlap this box at all in height? Everything else in here is
            // a plan view, and this one line is what keeps it honest.
            if (box.Max.Y <= Knee || box.Min.Y >= Crown)
                continue;

            // How far outside the box on each axis, negative when between the faces.
            var dx = MathF.Max(box.Min.X - x, x - box.Max.X);
            var dz = MathF.Max(box.Min.Z - z, z - box.Max.Z);

            float here;

            if (dx > 0f || dz > 0f)
            {
                // Outside on at least one axis, so the closest point on the box is a face or a corner and
                // the distance to it is the hypotenuse of whichever overshoots are real.
                var ox = MathF.Max(dx, 0f);
                var oz = MathF.Max(dz, 0f);
                here = MathF.Sqrt(ox * ox + oz * oz);
            }
            else
            {
                // Inside both, so both are negative and the shallower of the two is the nearest face.
                here = MathF.Max(dx, dz);
            }

            if (here >= gap)
                continue;

            gap = here;
            what = name;
        }

        return (gap, what);
    }

    /// <summary>What this point is inside, or null. No allowance and all three axes: a camera passing within
    /// a shoulder's width of a wall is a shot, and only a camera actually inside one is a fault.</summary>
    public string? Pierces(Vector3 at)
    {
        foreach (var (box, what) in _solids)
            if (at.X > box.Min.X && at.X < box.Max.X &&
                at.Y > box.Min.Y && at.Y < box.Max.Y &&
                at.Z > box.Min.Z && at.Z < box.Max.Z)
                return what;

        return null;
    }

    /// <summary>
    /// Where the visitor actually ends up, having tried to go from one place to another.
    ///
    /// Each axis is tested on its own, and that is what produces sliding rather than stopping: walking into
    /// a wall at an angle, the component along the wall survives the test and the component into it does
    /// not, so you scrape along instead of sticking. Doing it as one test on the pair would give a body that
    /// jams in every corridor it enters off-square, which is most of them.
    ///
    /// A body that is <i>already</i> inside something is let through. It should not happen — the film hands
    /// over at a place the audit says is clear — but the alternative is a visitor frozen in a wall with no
    /// way out, and being wrong for one step is a smaller failure than being wrong for ever.
    /// </summary>
    public Vector3 Slide(Vector3 from, Vector3 to)
    {
        if (_solids.Count == 0 || Blocked(from.X, from.Z))
            return to;

        var at = from;

        if (!Blocked(to.X, at.Z))
            at.X = to.X;

        if (!Blocked(at.X, to.Z))
            at.Z = to.Z;

        return new Vector3(at.X, to.Y, at.Z);
    }

    /// <summary>
    /// Walks every chapter's scripted camera through the building and reports anything it goes inside.
    ///
    /// <b>The scripted walk is not run through <see cref="Slide"/>, and must never be.</b> A camera that is
    /// pushed out of a wall is a camera whose position depends on where it was a moment ago, and the whole
    /// film rests on the opposite: every pose is a pure function of the clock, which is what lets the picker
    /// jump to the ninety-sixth second instead of playing up to it. Sliding the author's camera would make
    /// seeking produce a different shot from playing.
    ///
    /// So the walk is checked instead of corrected, here, off the clock, and what it produces is a list of
    /// waypoints to go and move. Run it with <c>AVA3D_WALK=1</c>.
    ///
    /// The building is re-surveyed four times a second as the audit runs, and that is not thoroughness — it
    /// is the only way the answer means anything. Every door in the film opens on the chapter clock, so a
    /// building surveyed once at second zero has every door in it shut, and the first thing it reports is
    /// that the camera goes through all four of them.
    /// </summary>
    public static string Audit()
    {
        const float Resurvey = 0.25f;
        const float Sample = 0.05f;

        var film = new Film();
        var ground = new Ground();
        var report = new StringBuilder();
        var faults = 0;
        var bodies = 0;

        report.AppendLine();
        report.AppendLine("================ walk ================");

        for (var i = 0; i < film.Chapters.Count; i++)
        {
            var chapter = film.Chapters[i];

            // A chapter with no walk is a chapter where the visitor is not a body — there is one, and it
            // is the cut. Asking whether a camera three thousand metres above a planet is inside
            // something would report that it is inside the sky, which is true and is not a fault.
            // When it starts, in film seconds. Printed because Contents keys the picker off exactly these
            // numbers and keeps them in a hand-written table — deliberately, because the alternative is
            // building the whole film at startup to ask it where something is. A table that can go stale
            // is worth having and is worth being able to check, and this is the check.
            var start = $"{film.StartOf(i),4:F0}s";

            if (chapter.Walk is not { } walk)
            {
                report.AppendLine(
                    $"  {i}. {chapter.Title,-22}{start}  not a walk — nobody is standing anywhere");
                continue;
            }

            chapter.Enter(film.Hall);

            var inside = 0;
            var first = -1f;
            var named = new SortedSet<string>(StringComparer.Ordinal);
            var solids = 0;

            // And the same walk asked the other question — see the loop below for why one of them was not
            // enough.
            var through = 0;
            var firstThrough = -1f;
            var walked = new SortedSet<string>(StringComparer.Ordinal);

            // The tightest the capsule ever gets to anything, which is the same measurement one step
            // earlier: by the time the axis is inside a box the shoulder has been through it for a third of
            // a metre. One number rather than a list, because it is monotone and comparable — a chapter
            // whose worst clearance is 0.02m is a different problem from one whose worst is 0.31m, and a
            // count of samples cannot tell them apart.
            var tightest = float.MaxValue;
            var tightestAt = 0f;
            string? tightestOn = null;

            // How far off level the aim ever gets, and when. Nothing about geometry — it is the third
            // way a walk goes wrong and the only one that leaves no trace in the building.
            var steepest = 0f;
            var steepestAt = 0f;

            // <b>And how much of this is walking at all</b>, which is here because it turned out to be
            // audible. The score puts a foot down every stride of ground covered, so a chapter that crosses
            // a metre in seven seconds gets one footstep in the middle of it and reads as a sample being
            // triggered rather than as a man walking — see Soundtrack.Tread. Nothing in the picture shows
            // it, and three numbers do: how far he goes, how fast he ever goes, and how much of the chapter
            // he is standing still for. A chapter that is ninety per cent still is a chapter to check the
            // feet in.
            var covered = 0f;
            var fastest = 0f;
            var stills = 0;
            var paces = 0;
            var previous = walk.At(0f).Eye;

            for (var window = 0f; window <= chapter.Duration; window += Resurvey)
            {
                // The state of the building at this moment: which rooms are up, where the doors are, what
                // has moved. Both calls are needed — Enter decides which rooms exist and Update is what
                // opens the doors.
                chapter.Update(film.Hall, window);
                ground.Survey(film.Hall.Scene);
                solids = Math.Max(solids, ground.Count);


                for (var t = window; t < window + Resurvey && t <= chapter.Duration; t += Sample)
                {
                    var pose = walk.At(t);
                    var eye = pose.Eye;

                    // Flat, and for the same reason the footfalls are: the walk raises and lowers the eye to
                    // lean in at things, and counting that as ground covered would report a man peering at a
                    // plinth as a man walking.
                    var went = MathF.Sqrt(
                        (eye.X - previous.X) * (eye.X - previous.X) +
                        (eye.Z - previous.Z) * (eye.Z - previous.Z));

                    previous = eye;
                    covered += went;
                    paces++;

                    var pace = went / Sample;
                    fastest = MathF.Max(fastest, pace);

                    if (pace < Walk.Standing)
                        stills++;

                    // <b>Where the aim is pointing, which is a fault the building cannot report.</b> A walk
                    // can clear every solid in the room and still be unwatchable, and the way it happens is
                    // always the same: two subjects on opposite sides of the visitor, and an aim that goes
                    // between them by the wrong route. The symptom is pitch — the shot dives at the floor
                    // and comes back — so pitch is what is measured. See Walk.Aim for the route it takes
                    // now and for why the obvious one is wrong.
                    //
                    // Reported rather than judged, because a number cannot tell a mistake from a decision:
                    // the engine room looks up at its own roof beams on purpose and that is the largest
                    // angle in the film. What a person is looking for here is a chapter whose aim goes
                    // somewhere its walk never mentions.
                    var toward = pose.Look - eye;
                    var range = toward.Length();

                    if (range > 1e-4f)
                    {
                        var degrees = float.RadiansToDegrees(
                            MathF.Asin(Math.Clamp(toward.Y / range, -1f, 1f)));

                        if (MathF.Abs(degrees) > MathF.Abs(steepest))
                        {
                            steepest = degrees;
                            steepestAt = t;
                        }
                    }

                    if (ground.Pierces(eye) is { } what)
                    {
                        inside++;
                        named.Add(what);

                        if (first < 0f)
                            first = t;
                    }

                    // <b>And whether a body could be standing there</b>, which is a different question and
                    // is the one that was missing. Pierces asks about a point, so a camera can clear
                    // everything in the room and the man carrying it can still walk through a plinth: the
                    // antechamber's exhibit is a metre of stone with a cube on top reaching 1.54, and an eye
                    // at 1.7 goes over the lot with a hundred and sixty millimetres to spare. That audited
                    // clear for as long as this audit existed, and on screen it was a camera flying over the
                    // first object in the film at knee height to it.
                    //
                    // It is reported and not counted as a fault, because the two cases genuinely cannot be
                    // told apart from here — chapter 4 sits down in an armchair, which is this test failing
                    // and is also the shot. So the audit says where a body was inside something and a person
                    // decides which kind it is. Every one of them is worth looking at once.
                    // Asked once and read twice: how much room the capsule has, and — when that goes
                    // negative — the axis being inside the box, which is the hard version of the same
                    // fault. See Meets for why the author's camera is judged at the axis and the free walk
                    // at the shoulder.
                    var (gap, near) = ground.Clearance(eye.X, eye.Z);

                    if (near is null)
                        continue;

                    if (gap < tightest)
                    {
                        tightest = gap;
                        tightestAt = t;
                        tightestOn = near;
                    }

                    if (gap >= 0f)
                        continue;

                    through++;
                    walked.Add(near);

                    if (firstThrough < 0f)
                        firstThrough = t;
                }
            }

            faults += inside;
            bodies += through;

            var notes = new List<string>();

            if (inside > 0)
                notes.Add($"INSIDE {string.Join(", ", named.Take(4))} from {first:F1}s ({inside} samples)");

            if (through > 0)
                notes.Add(
                    $"through {string.Join(", ", walked.Take(4))} from {firstThrough:F1}s ({through} samples)");

            // Always printed, whether or not it is a fault, because the interesting reading is the one that
            // is nearly a fault: half a chapter at 0.05m is a walk that happens to miss rather than a walk
            // that was routed. A star marks the ones where the capsule is genuinely in the box.
            var room = tightestOn is null
                ? "nothing in reach"
                : $"{(tightest < Reach ? "*" : " ")}{tightest,5:F2}m from {tightestOn} at {tightestAt:F0}s";

            var pacing = paces == 0
                ? "nowhere"
                : $"{covered,5:F1}m up to {fastest:F2}m/s, still {100f * stills / paces,3:F0}%";

            report.AppendLine(
                $"  {i}. {chapter.Title,-22}{start}  " +
                $"{(notes.Count == 0 ? "clear" : string.Join("; ", notes))} — {solids} solids, " +
                $"aim {steepest,5:F0}° at {steepestAt:F0}s, {room}, walks {pacing}");
        }

        report.AppendLine(faults == 0
            ? "  no scripted camera is ever inside anything."
            : $"  {faults} samples inside geometry — move a waypoint, do not push the camera.");

        if (bodies > 0)
            report.AppendLine(
                $"  {bodies} samples where the capsule's own axis is inside something. Not a fault on its " +
                "own — sitting down is one — but go and look at every one.");

        report.AppendLine(
            $"  a starred clearance is under the capsule's {Reach:F2}m radius: the body is in the box, not " +
            "beside it.");

        report.AppendLine(Wander(film, ground));
        report.Append("======================================");

        return report.ToString();
    }

    /// <summary>
    /// The other half of the same question, from the other end: given the building as the film leaves it,
    /// how far can the visitor get before something stops him?
    ///
    /// Four walks of twelve metres, one down each axis, taken in the same one-centimetre steps the free walk
    /// takes and through the same <see cref="Slide"/>. What it proves is not that the numbers are right —
    /// nobody knows what they should be — but that they are <i>finite</i>, and that the one number that
    /// should not be is: the walk down the room has to run the length of the gallery, and the walk into the
    /// window has to stop at the window. A build where every direction reports twelve metres is a build
    /// where the collision is not running at all, and that is the failure this catches.
    /// </summary>
    private static string Wander(Film film, Ground ground)
    {
        const float Far = 12f;
        const float Step = 0.01f;

        // The last chapter the visitor is a body in, which is where the film hands over. It is not always
        // the last chapter in the list — see StoryScene, which will not hand the controls to somebody who
        // is not standing anywhere.
        var last = film.Chapters[^1];
        foreach (var chapter in film.Chapters.Reverse())
        {
            if (chapter.Walk is null)
                continue;

            last = chapter;
            break;
        }

        if (last.Walk is not { } route)
            return "  no chapter in the film is a walk, so there is nothing to hand over.";

        last.Enter(film.Hall);
        last.Update(film.Hall, last.Duration);

        // And then the hand-over, exactly as StoryScene does it: every room standing, every powered door
        // open, and the survey taken with all of that up. Auditing the last chapter's own two rooms was
        // auditing a building the visitor never gets — it reported the gallery boxed in by three walls and
        // a shut door, which is what the free walk was, and said nothing at all about the seven rooms
        // behind him.
        film.Rounds.Open();
        ground.Survey(film.Hall.Scene);

        var from = route.At(last.Duration).Eye;
        var start = ground.Hits(from.X, from.Z);
        var legs = new List<string>();

        foreach (var (name, heading) in new[]
                 {
                     ("west", -Vector3.UnitX), ("east", Vector3.UnitX),
                     ("in", -Vector3.UnitZ), ("out", Vector3.UnitZ)
                 })
        {
            var at = from;
            var went = 0f;
            string? stopper = null;

            for (var i = 0; i < (int)(Far / Step); i++)
            {
                var want = at + heading * Step;
                var next = ground.Slide(at, want);

                if (Vector3.DistanceSquared(next, at) < Step * Step * 0.25f)
                {
                    // What it walked into. Worth printing, because a leg that stops at nought is either a
                    // wall doing its job or the visitor handed the controls with his nose against a rail,
                    // and a number on its own cannot tell those apart.
                    stopper = ground.Hits(want.X, want.Z);
                    break;
                }

                went += Step;
                at = next;
            }

            legs.Add($"{name} {went:F1}m{(stopper is null ? "" : $" ({stopper})")}");
        }

        return $"  free walk from {from.X:F1},{from.Z:F1} " +
               $"({start ?? "clear"}) — {string.Join(", ", legs)}, {ground.Count} solids"
               + Environment.NewLine
               + Flood(ground, from, film.Rounds.Plans);
    }
    /// <summary>
    /// Every square of floor a body can stand on, flooded out from where the film hands the controls over —
    /// and then which of the eight rooms that reached.
    ///
    /// The four legs above prove the collision is running. This proves the building is <i>connected</i>,
    /// which is a different claim and the one that was broken: three of the doorways on the route are
    /// powered doors that spend the whole film shut, so the visitor was handed a room with the rest of the
    /// ship walled off behind it and nothing anywhere said so.
    ///
    /// <b>Flooded rather than walked.</b> The first version of this was a list of waypoints — the doorways
    /// and stands the chapters already aim at — walked leg by leg through <see cref="Slide"/>. It found two
    /// real faults and then spent longer than the fix did arguing about the route: a hand-drawn line that
    /// scrapes a stool is a fault in the line, and there is no way to tell that from a fault in the room
    /// without drawing another line. A flood does not have an opinion about where anybody would walk. It
    /// answers the question actually being asked, which is whether a body can get there at all.
    ///
    /// A tenth of a metre, and four neighbours rather than eight. Both are the capsule rather than the grid:
    /// a diagonal step is what lets a flood squeeze through the corner between two boxes that touch, which
    /// is a gap no shoulder fits through, and a cell coarser than this can straddle a doorway it cannot
    /// actually pass.
    /// </summary>
    private static string Flood(Ground ground, Vector3 from, IEnumerable<(string Name, Vector2 Min, Vector2 Max)> rooms)
    {
        const float Cell = 0.1f;
        const int Span = 1_200;

        // The whole deck in one grid, indexed off a corner well clear of it. Everything in this building is
        // between about minus twelve and plus seventy in z, so a hundred and twenty metres square at a tenth
        // of a metre is the deck with room to spare — and the flood only ever touches the cells it reaches,
        // so the size of the grid costs nothing but the array.
        var origin = new Vector2(-20f, -20f);
        var seen = new bool[Span * Span];
        var queue = new Queue<(int X, int Z)>();

        var start = ((int)((from.X - origin.X) / Cell), (int)((from.Z - origin.Y) / Cell));

        seen[start.Item1 * Span + start.Item2] = true;
        queue.Enqueue(start);

        var floor = 0;
        var reached = new List<(float X, float Z)>();

        while (queue.Count > 0)
        {
            var (cx, cz) = queue.Dequeue();

            floor++;
            reached.Add((origin.X + cx * Cell, origin.Y + cz * Cell));

            foreach (var (dx, dz) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
            {
                var nx = cx + dx;
                var nz = cz + dz;

                if (nx < 0 || nz < 0 || nx >= Span || nz >= Span || seen[nx * Span + nz])
                    continue;

                seen[nx * Span + nz] = true;

                if (!ground.Blocked(origin.X + nx * Cell, origin.Y + nz * Cell))
                    queue.Enqueue((nx, nz));
            }
        }

        var notes = new List<string>();
        var got = 0;
        var total = 0;

        foreach (var (name, min, max) in rooms)
        {
            total++;

            // Counted a room at a time rather than assigned a room per cell, because the rooms overlap: a
            // threshold is in both rooms it joins and the corridor's end wall stands inside the engine
            // room's bulkhead. A cell in two rooms is two rooms reached, which is the truth.
            var area = reached.Count(c => c.X >= min.X && c.X <= max.X && c.Z >= min.Y && c.Z <= max.Y)
                       * Cell * Cell;

            if (area < 1f)
            {
                notes.Add($"{name} UNREACHED");
                continue;
            }

            got++;
            notes.Add($"{name} {area:F0}m²");
        }

        return $"  reachable floor {floor * Cell * Cell:F0}m² over {got}/{total} rooms — "
               + string.Join(", ", notes);
    }
}
