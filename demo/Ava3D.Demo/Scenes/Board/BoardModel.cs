using System.Globalization;
using System.Numerics;
using Avalonia.Platform;

namespace Ava3D.Demo.Scenes.Board;

/// <summary>One part on the board: what the model calls it, what a person calls it, and where it sits.</summary>
/// <param name="Node">The node name in <c>board.glb</c>. A part is this node and everything under it.</param>
/// <param name="Designator">The name printed beside it, or the one it would carry: <c>U1</c>, <c>C217</c>.</param>
/// <param name="Label">What it is, in a few words.</param>
/// <param name="X">Left edge of the footprint, in board millimetres from the bottom-left corner.</param>
/// <param name="Y">Bottom edge of the footprint.</param>
/// <param name="W">Footprint width.</param>
/// <param name="D">Footprint depth.</param>
public sealed record BoardPart(
    string Node, string Designator, string Label, float X, float Y, float W, float D);

/// <summary>
/// One run of copper, as the router left it: a polyline in board millimetres between two parts.
/// </summary>
/// <param name="A">The part it leaves.</param>
/// <param name="B">The part it arrives at, or empty for a stub that ends at a via.</param>
/// <param name="Points">Corners, not cells — a straight leg is two points however long it is.</param>
public sealed record BoardRun(string A, string B, Vector2[] Points)
{
    /// <summary>Length in millimetres, which is the number a board is actually designed around.</summary>
    public float Length
    {
        get
        {
            var total = 0f;
            for (var i = 0; i + 1 < Points.Length; i++)
                total += Vector2.Distance(Points[i], Points[i + 1]);

            return total;
        }
    }
}

/// <summary>
/// The board's part list and its copper, read from <c>Assets/board.txt</c>.
///
/// This is the half of the board that geometry cannot carry. A .glb node is a name, a mesh and a matrix;
/// there is nowhere in it to say "this is C217, a 0603 ceramic capacitor, and these two runs of copper
/// leave it". And the copper is worse — it is drawn into the board's texture, so by the time it reaches
/// the renderer it is pixels, and pixels cannot be highlighted one net at a time.
///
/// Both facts are in <c>tools/models/pcb_layout.py</c>, which is where the board is decided and what
/// built the model and the texture. <c>build-pcb-manifest.py</c> writes them out beside the model and
/// <c>check-pcb.py</c> fails the build if the file has drifted from the layout — which matters more than
/// it sounds, because a manifest naming the part next door would read as a bug in picking.
/// </summary>
public sealed class BoardData
{
    /// <summary>Board width in millimetres. 305 for ATX.</summary>
    public float Width { get; private init; }

    /// <summary>Board depth in millimetres. 244 for ATX.</summary>
    public float Depth { get; private init; }

    /// <summary>Laminate thickness in millimetres, which is where the top face is.</summary>
    public float Thickness { get; private init; }

    /// <summary>Scene units per millimetre. One unit is 100 mm, so this is 0.01.</summary>
    public float Scale { get; private init; }

    /// <summary>Every part, major and small, in the order the layout lists them.</summary>
    public IReadOnlyList<BoardPart> Parts { get; private init; } = [];

    /// <summary>Every run of copper.</summary>
    public IReadOnlyList<BoardRun> Runs { get; private init; } = [];

    /// <summary>
    /// A point on the board, in millimetres, as a position in the model's own space.
    ///
    /// Three things happen at once here and all three came from the build. The board is centred on the
    /// origin, so half its size comes off first. Millimetres become units. And Z becomes −Z, because the
    /// exporter turned the model Y-up on the way out: what the layout calls "up the board" is what the
    /// scene calls "towards the camera".
    ///
    /// Getting this wrong is not subtle and it is not obvious either — a sign error puts every trace on a
    /// board mirrored about its middle, which still looks like a board.
    /// </summary>
    public Vector3 At(float x, float y, float z) =>
        new((x - Width / 2f) * Scale, z * Scale, -(y - Depth / 2f) * Scale);

    /// <summary>The part with this designator, or null. Case-insensitive, because a person types it.</summary>
    public BoardPart? ByDesignator(string designator) => Parts.FirstOrDefault(
        part => string.Equals(part.Designator, designator, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Reads the manifest out of the application's own resources.
    ///
    /// Hand-parsed rather than JSON: the file has no nesting in it, so a parser for it is the twenty
    /// lines below and not a dependency. Unknown line kinds are skipped rather than rejected, so a later
    /// version of the writer can add one without this having to be updated in step.
    /// </summary>
    public static BoardData Read()
    {
        using var stream = AssetLoader.Open(new Uri("avares://Ava3D.Demo/Assets/board.txt"));
        using var reader = new StreamReader(stream);

        var parts = new List<BoardPart>();
        var runs = new List<BoardRun>();
        float width = 305f, depth = 244f, thickness = 1.6f, scale = 0.01f;

        while (reader.ReadLine() is { } line)
        {
            if (line.Length == 0 || line[0] == '#')
                continue;

            var fields = line.Split(' ');
            switch (fields[0])
            {
                case "board" when fields.Length >= 5:
                    width = Number(fields[1]);
                    depth = Number(fields[2]);
                    thickness = Number(fields[3]);
                    scale = Number(fields[4]);
                    break;

                // Split with a limit, because the description is the rest of the line and has spaces in
                // it. Everything before it is fixed-width in fields, which is what makes that work.
                case "part":
                    var part = line.Split(' ', 8);
                    if (part.Length == 8)
                    {
                        parts.Add(new BoardPart(
                            part[1], part[2], part[7],
                            Number(part[3]), Number(part[4]), Number(part[5]), Number(part[6])));
                    }

                    break;

                case "run" when fields.Length >= 4:
                    var points = new Vector2[fields.Length - 3];
                    for (var i = 0; i < points.Length; i++)
                    {
                        var pair = fields[i + 3].Split(',');
                        points[i] = new Vector2(Number(pair[0]), Number(pair[1]));
                    }

                    runs.Add(new BoardRun(fields[1], fields[2] == "-" ? "" : fields[2], points));
                    break;
            }
        }

        return new BoardData
        {
            Width = width,
            Depth = depth,
            Thickness = thickness,
            Scale = scale,
            Parts = parts,
            Runs = runs
        };
    }

    private static float Number(string text) => float.Parse(text, CultureInfo.InvariantCulture);
}

/// <summary>
/// Loading the board, and the three things its scenes do to it.
///
/// Every other scene in this folder builds from nothing and stands on its own, which is the point of the
/// shape — see <see cref="DemoScene"/>. These four are one subject seen four ways, and the loading, the
/// manifest and the edge extraction are identical in all of them. Copying forty lines into each would not
/// make any of them clearer; it would make two of them stale the first time the third was fixed.
/// </summary>
public static class BoardModel
{
    /// <summary>
    /// Loads <c>board.glb</c> and hands back the scene, the file's own root node, and a line about it.
    ///
    /// The loader gives the file one root of its own and hangs all 610 nodes under it, so that root is
    /// both the thing to turn and the thing to search. Counting <c>scene.Children</c> instead reports one
    /// node and finds no components at all, which is a mistake worth making only once.
    /// </summary>
    public static (Scene Scene, Node Root, string Detail) Load()
    {
        using var stream = AssetLoader.Open(new Uri("avares://Ava3D.Demo/Assets/board.glb"));
        using var memory = new MemoryStream();
        stream.CopyTo(memory);

        var bytes = memory.ToArray();
        var scene = GltfLoader.Load(bytes, "board.glb");
        var root = scene.Children.Count > 0
            ? scene.Children[0]
            : throw new InvalidDataException("board.glb has no nodes");

        var parts = root.Descendants.OfType<MeshNode>().ToList();
        var detail = $"{bytes.Length / 1024:N0} KB of .glb: {parts.Count} nodes, " +
                     $"{parts.Select(node => node.Mesh).OfType<Mesh>().Distinct().Count()} distinct " +
                     $"meshes, {parts.Sum(node => node.Mesh?.TriangleCount ?? 0):N0} triangles.";

        return (scene, root, detail);
    }

    /// <summary>
    /// Every mesh's edges at one crease angle, gathered into a single array in the root's own space.
    ///
    /// One array, and therefore one <see cref="LineNode"/> and one draw for the whole board, rather than
    /// 610 of each. The saving is not in the vertex count — that is the same either way — but in the
    /// number of times the renderer has to be told to draw something, which is the thing that costs.
    ///
    /// <see cref="Mesh.GetEdges"/> is called once per distinct mesh and the result reused across every
    /// node sharing it, which for this model is the difference between 194 extractions and 610: the four
    /// hundred small parts have four meshes between them.
    /// </summary>
    /// <param name="root">The node to gather under. Its own transform is divided out.</param>
    /// <param name="crease">
    /// How far two faces must fold before the edge between them is kept. 0 is every edge in the
    /// triangulation; 30 keeps the folds and drops what is buried inside a smooth surface.
    /// </param>
    public static Vector3[] Edges(Node root, float crease)
    {
        // Relative to the root rather than to the world, so the caller is free to put the root inside
        // something that moves — which all three of these scenes do.
        Matrix4x4.Invert(root.WorldTransform, out var intoRoot);

        var cache = new Dictionary<Mesh, Vector3[]>();
        var gathered = new List<Vector3>();

        foreach (var node in root.Descendants.OfType<MeshNode>())
        {
            if (node.Mesh is not { } mesh)
                continue;

            if (!cache.TryGetValue(mesh, out var edges))
                cache[mesh] = edges = mesh.GetEdges(crease);

            var transform = node.WorldTransform * intoRoot;
            foreach (var point in edges)
                gathered.Add(Vector3.Transform(point, transform));
        }

        return [.. gathered];
    }

    /// <summary>
    /// The whole board as one mesh, with every node's transform baked into its vertices.
    ///
    /// For a drawing that is exactly right: nothing in it moves independently, so nothing needs to stay
    /// its own object, and the entire board becomes a single draw. It is the same trade the Motherboard
    /// scene makes for the small parts and states the cost of — merged geometry cannot be moved, hidden
    /// or picked apart afterwards.
    /// </summary>
    public static Mesh Solid(Node root)
    {
        Matrix4x4.Invert(root.WorldTransform, out var intoRoot);

        return Mesh.Merge(root.Descendants
            .OfType<MeshNode>()
            .Where(node => node.Mesh is not null)
            .Select(node => node.Mesh!.Transformed(node.WorldTransform * intoRoot)));
    }

    /// <summary>
    /// Copper runs as line endpoints, lifted clear of the board's face.
    ///
    /// The lift is what keeps them from fighting the surface they are drawn on. It is in millimetres and
    /// it is small — a twentieth of a millimetre on a board 305 mm across — because a line that stands
    /// off far enough to be safe at one camera distance detaches visibly at another.
    /// <see cref="Material.DepthBias"/> is the right answer for a coplanar <i>surface</i>, and it does not
    /// apply to lines: OpenGL ES and WebGL guarantee the polygon offset for filled triangles only.
    /// </summary>
    public static Vector3[] Copper(BoardData board, IEnumerable<BoardRun> runs, float lift = 0.05f)
    {
        var z = board.Thickness + lift;
        var gathered = new List<Vector3>();

        foreach (var run in runs)
        {
            for (var i = 0; i + 1 < run.Points.Length; i++)
            {
                gathered.Add(board.At(run.Points[i].X, run.Points[i].Y, z));
                gathered.Add(board.At(run.Points[i + 1].X, run.Points[i + 1].Y, z));
            }
        }

        return [.. gathered];
    }
}
