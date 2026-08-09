using System.Numerics;
using Avalonia.Media;
using Ava3D.Demo.Textures;

namespace Ava3D.Demo.Scenes.Board;

/// <summary>
/// The board with its schematic attached: click a part to name it and light up the copper leaving it.
/// </summary>
public sealed class InspectorScene : DemoScene
{
    private static readonly Vector4 PartTint = new(1.00f, 0.62f, 0.16f, 1f);
    private static readonly Vector3 PartGlow = new(0.34f, 0.14f, 0.01f);
    // Well under full brightness, because the highlight is additive: over a dark green board a line at
    // full amber sums past white and the whole bundle reads as one flare instead of as conductors.
    private static readonly Vector3 NetColor = new(0.82f, 0.46f, 0.10f);

    /// <summary>How long each stop of the walk holds before the next one.</summary>
    private const double StopSeconds = 2.6;

    /// <summary>
    /// One stop on the walk: which parts to light up, whether to show their copper, and what to call
    /// the pair of them.
    ///
    /// The walk exists because a scene that selects one part at build and then waits for a click shows
    /// exactly one thing to anybody watching rather than clicking — and what it is showing is not
    /// "here is a socket", it is "any component in this model can be found, named and lit on its own,
    /// including several at once and including its copper". One frozen selection does not say that.
    /// Eight of them, two or three parts at a time, alternating between a part and the copper that
    /// leaves it, say all of it without a word.
    ///
    /// Alternating matters as much as the parts do. Lighting the copper at the same moment as the
    /// component makes one picture with two new things in it, and a viewer reads whichever is brighter;
    /// showing the part, then adding the copper to the part already on screen, makes the copper the
    /// only thing that changed.
    /// </summary>
    private sealed record Stop(string Lead, bool Copper, string[] Nodes);

    private static readonly Stop[] Walk =
    [
        new("the processor socket, on its own", false, ["cpu.socket"]),
        new("and the copper leaving it", true, ["cpu.socket"]),
        new("two of the four memory slots", false, ["dimm.0", "dimm.1"]),
        new("channel A's bus, both slots together", true, ["dimm.0", "dimm.1"]),
        new("three controllers, nowhere near each other", false,
            ["chip.super-io", "chip.lan", "chip.audio"]),
        new("and everything that reaches them", true, ["chip.super-io", "chip.lan", "chip.audio"]),
        new("both ends of the flex jumper, on two boards", false, ["fpc.0", "panel.j1"]),
        new("the panel it feeds, and the panel's own copper", true,
            ["panel.j1", "led.0", "led.2", "led.5"]),
    ];

    private BoardData _board = null!;
    private Node? _root;
    private LineNode? _net;
    private string _detail = "";
    private string _caption = "";

    // Where the walk is, and whether it is still running. A click stops it for good: the person
    // watching has become the person driving, and a scene that carried on changing the selection under
    // them would be arguing with the mouse.
    private int _stop = -1;
    private bool _walking = true;

    // Which nodes belong to which part, worked out once at build. A part is one name in the manifest
    // and between one and four nodes in the model — a memory slot is its body, its contacts and two
    // latches — so both directions are wanted: the part a clicked node belongs to, and every node to
    // tint once that part is known.
    private readonly Dictionary<MeshNode, BoardPart> _owner = [];
    private readonly Dictionary<string, List<MeshNode>> _group = [];
    private readonly List<(MeshNode Node, Material Material)> _tinted = [];

    public override string Title => "Board inspector";

    public override string Summary => "Click a part: its designator, and the copper that leaves it";

    public override string Notes =>
        $"""
         {_detail}

         It walks itself through eight selections — a component alone, then the copper leaving it, then
         two memory slots, then three controllers at opposite corners, then both ends of the flex
         jumper — and a click takes it over for good. Two or three parts at a time and never all of
         them, because the claim being made is not that the board is one model: it is that anything in
         it can be found by name, lit on its own or in a group, and asked what copper reaches it. A
         scene showing one frozen selection demonstrates none of that.

         Click anything. The view raycasts the actual triangles and hands back the node it hit; this
         scene turns that into a component, because a component is not a node. A memory slot is four of
         them and an indicator is four more, so the hit node's name is matched against the part list by
         its longest dotted prefix — led.3.lens belongs to led.3 — and every node under that name is
         tinted together. Tinting only what was hit highlights a plastic latch and leaves the slot it
         belongs to green, which is worse than not highlighting at all.

         The name it shows is the one a person uses. A .glb node is a name, a mesh and a matrix, and
         there is nowhere in the format to record that led.3 is D4, a green indicator, and that these
         two runs of copper reach it. That is in tools/models/pcb_layout.py, which is what built the
         model, and build-pcb-manifest.py writes it out beside the model as Assets/board.txt — 72 KB of
         designators, footprints and net endpoints, parsed here in twenty lines because a file with no
         nesting in it needs no parser.

         The copper is the part worth knowing about. Every trace on this board is drawn into its
         texture, so at runtime it is pixels — and pixels cannot be highlighted one net at a time. The
         glowing runs are real LineNode geometry, built from the same table of router output that drew
         those pixels, lifted a twentieth of a millimetre off the face. That is why they land exactly on
         the copper they are highlighting rather than nearly on it.

         Click the bare board instead and it finds the nearest run to where you clicked and names the
         net. A LineNode cannot be picked — picking is against triangles and a line has none — so the
         hit is on the laminate, converted back into board millimetres and measured against 1,483
         segments. Which is also how it can tell you the length: a millimetre of trace is a thing a
         board is designed around, and it is why the memory runs on a real board wander instead of
         going straight.
         """;

    public override SceneLook Look => SceneLook.Studio;

    public override bool WantsPicking => true;

    public override bool Animates => true;

    public override bool FramesItself => true;

    // The length of the walk, so Auto moves on when the scene is over rather than on a timeout.
    public override TimeSpan TourDuration => TimeSpan.FromSeconds(StopSeconds * Walk.Length);

    public override string? Caption => _caption;

    public override void Frame(Camera camera)
    {
        camera.Target = new Vector3(0.30f, 0.06f, 0f);
        camera.Distance = 3.6f;
        camera.Yaw = 0.42f;
        camera.Pitch = 0.72f;
        camera.FieldOfView = 40f;
        camera.NearPlane = 0.2f;
        camera.FarPlane = 40f;
    }

    public override Scene Build()
    {
        var scene = new Scene();
        Stage(scene);

        if (BuildSubject() is { } board)
            scene.Children.Add(board);

        return scene;
    }

    /// <summary>
    /// A dark room and one key, with the environment doing the metals.
    ///
    /// The story does none of this. It stands the board in a service cradle in the engine room and lights
    /// it with the bay's own lamps, which is the difference between a subject and a scene: what is on the
    /// board does not change, and everything around it does.
    /// </summary>
    public override void Stage(Scene scene)
    {
        scene.Background = Color.FromRgb(13, 15, 19);
        scene.Environment = EnvironmentLight.FromTexture(Environments.Studio(), 1.15f);
        scene.Light.Direction = Vector3.Normalize(new Vector3(-0.42f, -0.78f, -0.46f));
        scene.Light.Color = new Vector3(1.00f, 0.98f, 0.94f);
        scene.Light.Intensity = 1.70f;
        scene.Light.Ambient = 0.02f;
    }

    /// <summary>
    /// The board, indexed, with the highlight's line node already on it and the first stop selected.
    ///
    /// The story mounts it on a screen rather than on a bench, at a third of full size, and that is the
    /// whole of what this scene is in the film: the board as information. The board as an object is the
    /// one lying in front of the screen at fifteen hundredths, and it is a different instance because the
    /// two are different things.
    /// </summary>
    public override Node? BuildSubject()
    {
        try
        {
            (_, _root, _detail) = BoardModel.Load();
            _board = BoardData.Read();
        }
        catch (Exception e)
        {
            _detail = $"The board failed to load: {e.GetType().Name}: {e.Message}";
            return null;
        }

        foreach (var node in _root.Descendants)
        {
            if (node is MeshNode { Material.Name: "board.silk" } silk)
            {
                silk.Material.DepthBias = 1f;
                silk.Material.DepthBiasSlope = 1.4f;
            }
        }

        Index();

        // One line node for the whole highlight, refilled on every selection. The array is replaced
        // rather than written into, because the number of segments changes with what was selected — and
        // assigning a new array is what tells the backends their buffer is stale.
        //
        // Ordered after the board rather than with it. The depth test still hides a run behind a
        // heatsink where there is a depth buffer to test against; where there is not, a line node is
        // sorted once as a whole object, and at the board's own origin that put the highlight under the
        // near half of the laminate it is drawn on.
        _net = new LineNode
        {
            Name = "net.highlight",
            Positions = [],
            Color = NetColor,
            Width = 2.2f,
            Blend = BlendMode.Additive,
            DepthTest = true,
            RenderOrder = 1
        };
        _root.Children.Add(_net);

        // Standing on the first stop from the first frame, so there is never a moment where the scene
        // is a board with nothing selected and no explanation of why anyone would click it.
        Advance(null, 0);

        return _root;
    }

    public override void Update(Scene scene, double elapsed)
    {
        // Which stop the clock is on, not which one is next: driving it from elapsed time rather than
        // from a counter means a dropped frame or a slow first build costs the walk nothing, and the
        // CPU fallback at a quarter of the frame rate sees the same eight stops the GPU path does.
        if (_walking)
            Advance(scene, (int)(elapsed / StopSeconds) % Walk.Length);

        // Every tick, not only the ones that change something. A scene that says it animates has to
        // keep asking for frames, because the shell's frame counter and the headless capture switches
        // both count what was drawn — and a scene that only invalidates when its own state moves stops
        // the clock they are counting on.
        scene.Invalidate();
    }

    private void Advance(Scene? scene, int index)
    {
        if (index == _stop || _root is null)
            return;

        _stop = index;
        Restore();

        var stop = Walk[index];
        var parts = stop.Nodes
            .Select(node => _board.Parts.FirstOrDefault(part => part.Node == node))
            .OfType<BoardPart>()
            .ToList();

        if (parts.Count > 0)
            Select(scene, parts, stop.Copper, stop.Lead);
    }

    /// <summary>
    /// Files every mesh node under the part it belongs to, by the longest prefix of its name that is
    /// one.
    ///
    /// Longest first and not shortest: <c>cap.audio.3.score</c> has to find <c>cap.audio.3</c>, and
    /// there is no part called <c>cap</c> to be wrong about — but there is a <c>chip.bios</c> as well
    /// as a <c>chip.audio</c>, and a rule that stopped at the first dot would put them together.
    /// The laminate and the silkscreen match nothing, which is what makes a click on them mean
    /// "find me the nearest trace".
    /// </summary>
    private void Index()
    {
        if (_root is null)
            return;

        var known = _board.Parts.ToDictionary(part => part.Node, StringComparer.Ordinal);

        foreach (var node in _root.Descendants.OfType<MeshNode>())
        {
            for (var name = node.Name; !string.IsNullOrEmpty(name); name = Shorten(name))
            {
                if (!known.TryGetValue(name, out var part))
                    continue;

                _owner[node] = part;

                if (!_group.TryGetValue(name, out var group))
                    _group[name] = group = [];

                group.Add(node);
                break;
            }
        }

        _detail += $" {_owner.Count} of those belong to one of {_board.Parts.Count} parts; the " +
                   "laminate and the silkscreen belong to none.";

        static string? Shorten(string name) =>
            name.LastIndexOf('.') is var cut && cut > 0 ? name[..cut] : null;
    }

    public override bool Picked(Scene scene, PickResult? hit)
    {
        _walking = false;
        Restore();

        if (hit is null)
        {
            Show(scene, "Click a part, or the bare board beside a trace", []);
            return true;
        }

        // The laminate and the silkscreen belong to no part, and that is what makes a click on them
        // mean something else: find the nearest run rather than nothing at all.
        if (_owner.TryGetValue(hit.Node, out var part))
            Select(scene, [part], copper: true, lead: null);
        else
            SelectNet(scene, hit.WorldPosition);

        return true;
    }

    /// <summary>
    /// Lights up one part or several, and optionally the copper that leaves them.
    /// </summary>
    /// <param name="lead">
    /// What the walk is calling this selection, or null for a click — where the person already knows
    /// what they picked and the interesting half of the caption is the measurements.
    /// </param>
    private void Select(Scene? scene, IReadOnlyList<BoardPart> parts, bool copper, string? lead)
    {
        var nodes = 0;

        foreach (var node in parts.SelectMany(part => _group[part.Node]))
        {
            // A clone each, because materials are shared by identity: four hundred small parts have
            // four materials between them, and tinting one in place turns the whole board orange.
            var tinted = node.Material.Clone();
            tinted.BaseColor = PartTint;
            tinted.EmissiveColor = PartGlow;

            _tinted.Add((node, node.Material));
            node.Material = tinted;
            nodes++;
        }

        var names = string.Join(", ", parts.Select(part => part.Designator));
        var runs = copper
            ? _board.Runs.Where(run => parts.Any(part => run.A == part.Node || run.B == part.Node))
                .ToList()
            : [];

        // Two shapes of caption, because a click and a stop on the walk are answering two different
        // questions. A click has already told the person which part they mean, so the caption is its
        // measurements. A stop has to say what it is showing them and why these parts are together.
        string about;

        if (lead is null)
        {
            var part = parts[0];
            about = $"{part.Label} · {part.W:0.#} × {part.D:0.#} mm · " + (runs.Count == 0
                ? "no copper on this layer"
                : $"{runs.Count} run{(runs.Count == 1 ? "" : "s")}, {runs.Sum(r => r.Length):F0} mm of copper");
        }
        else
        {
            about = $"{lead} · {nodes} node{(nodes == 1 ? "" : "s")} tinted" + (runs.Count == 0
                ? ""
                : $", {runs.Count} runs and {runs.Sum(r => r.Length):F0} mm of copper lit");
        }

        Show(scene, $"{names} · {about}", BoardModel.Copper(_board, runs));
    }

    /// <summary>
    /// The run nearest a click on the bare board, if there is one within a couple of millimetres.
    ///
    /// The hit arrives in world space and the copper is in board millimetres, so it goes back through
    /// the root's transform first. That matters the moment anything moves the board — and reading
    /// <see cref="Node.WorldTransform"/> rather than assuming it is the identity is the difference
    /// between this working and this working until the scene is put inside something.
    /// </summary>
    private void SelectNet(Scene scene, Vector3 world)
    {
        if (_root is null || !Matrix4x4.Invert(_root.WorldTransform, out var intoRoot))
            return;

        var local = Vector3.Transform(world, intoRoot);
        var point = new Vector2(
            local.X / _board.Scale + _board.Width / 2f,
            -local.Z / _board.Scale + _board.Depth / 2f);

        BoardRun? best = null;
        var nearest = 2.5f;

        foreach (var run in _board.Runs)
        {
            for (var i = 0; i + 1 < run.Points.Length; i++)
            {
                var distance = Distance(point, run.Points[i], run.Points[i + 1]);
                if (distance >= nearest)
                    continue;

                nearest = distance;
                best = run;
            }
        }

        if (best is null)
        {
            Show(scene, $"bare board at ({point.X:F0}, {point.Y:F0}) mm — no copper within 2.5 mm", []);
            return;
        }

        var from = _board.Parts.FirstOrDefault(part => part.Node == best.A)?.Designator ?? best.A;
        var to = _board.Parts.FirstOrDefault(part => part.Node == best.B)?.Designator;

        Show(scene,
            $"{from} → {to ?? "via"} · {best.Points.Length - 1} segment" +
            $"{(best.Points.Length == 2 ? "" : "s")}, {best.Length:F1} mm",
            BoardModel.Copper(_board, [best]));
    }

    private static float Distance(Vector2 point, Vector2 a, Vector2 b)
    {
        var span = b - a;
        var length = span.LengthSquared();
        if (length < 1e-6f)
            return Vector2.Distance(point, a);

        var t = Math.Clamp(Vector2.Dot(point - a, span) / length, 0f, 1f);
        return Vector2.Distance(point, a + span * t);
    }

    private void Show(Scene? scene, string caption, Vector3[] copper)
    {
        _caption = caption;

        if (_net is not null)
            _net.Positions = copper;

        // Null while the subject is being built, because there is no scene yet — the story asks for the
        // board before it has anywhere to stand it.
        scene?.Invalidate();
    }

    private void Restore()
    {
        foreach (var (node, material) in _tinted)
            node.Material = material;

        _tinted.Clear();
    }
}
