using System.Numerics;
using Avalonia.Media;
using Avalonia.Platform;
using Ava3D.Demo.Textures;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// An ATX motherboard, and the four hundred identical parts on it. The scene about draw calls.
/// </summary>
public sealed class MotherboardScene : DemoScene
{
    private Node? _root;
    private Node? _loose;
    private Node? _merged;
    private string _detail = "";
    private string _cost = "";
    private bool _showMerged = true;

    public override string Title => "Motherboard";

    public override string Summary => "Four hundred identical parts, and what merging them is worth";

    public override string Notes =>
        $"""
         {_detail}

         Four hundred and twenty of those nodes are chip resistors and capacitors, and between them they
         have four meshes — two sizes, each in two orientations. That is instancing, and it is free: the
         geometry goes to the GPU once and every node after the first costs nothing but a matrix.

         What it does cost is a draw call each, and a draw call is the unit that makes a scene slow. So
         this scene alternates every four seconds between the board as the file describes it and the same
         board with those parts merged into one mesh per material. Watch the draws and the frame time in
         the statistics panel rather than the picture — the picture is identical, which is the point.

         {_cost}

         Merging is not free either, and the trade is worth stating: the parts can no longer move, be
         hidden, or be picked apart, because they are one object now. Merge what is soldered down. The
         heatsinks, slots and connectors are left alone here for exactly that reason.

         Two other things are being leaned on rather than demonstrated. The silkscreen — every white
         outline on the board — is flat geometry lying in the board's own surface, so it needs
         Material.DepthBias to win the depth test; without it the outlines crawl, and on the CPU fallback
         they still do. And the gold contacts, the aluminium heatsinks and the tinned
         pads are only convincing because there is an environment map for them to reflect. Turn it off and
         they go to flat grey, because that is what metal is without a room around it.

         Two things about the materials are worth knowing, because both were wrong here first. A metal's
         base colour is its reflectance, and metals are bright — aluminium is 0.80, not the mid-grey a
         heatsink photographs as; set it to what it looks like and, with no diffuse term left at metallic
         1, it renders nearly black. And a dielectric's roughness decides how much of the room it shows:
         at 0.40 the near-black nylon on these connectors mirrored the environment hard enough to render
         as pale grey bars.

         The board is ours. tools/models/pcb_layout.py holds every position and routes the copper — a maze
         router over a one-millimetre grid, one net at a time, each net's cells taken out before the next
         one starts, so two traces cannot cross. build-pcb-texture.py draws the mask and the pads from that
         same table, build-pcb.py builds the geometry, and check-pcb.py fails the build on an overlap, a
         mounting hole under a part, a solid wound inside out, or any two runs crossing. It has caught all
         four.

         The six indicators are on a board of their own, off the right-hand edge, joined by a flex jumper
         — a strip of polyimide with ten conductors printed on it, translucent because polyimide is. Three
         more scenes follow this one and all four are the same file: Board inspector names a part and
         lights up the copper leaving it, Indicators runs the lamps on a timer, and Draftsman and
         Wireframe are the same model as a drawing and as its edges, at three draws and one.
         """;

    public override SceneLook Look => SceneLook.Studio;

    public override bool Animates => true;

    public override bool FramesItself => true;

    public override TimeSpan TourDuration => TimeSpan.FromSeconds(16);

    public override string? Caption =>
        _showMerged ? "passives merged — one draw per material" : "as loaded — one draw per part";

    public override void Frame(Camera camera)
    {
        // Off-centre to the right, because the model is: the indicator panel and its jumper hang past
        // the motherboard's right-hand edge, and framing the motherboard alone leaves them clipped.
        camera.Target = new Vector3(0.30f, 0.10f, 0f);
        camera.Distance = 3.9f;
        camera.Yaw = 0.55f;
        camera.Pitch = 0.58f;
        camera.FieldOfView = 42f;
        camera.NearPlane = 0.2f;
        camera.FarPlane = 40f;
    }

    public override Scene Build()
    {
        Scene scene;

        try
        {
            using var stream = AssetLoader.Open(new Uri("avares://Ava3D.Demo/Assets/board.glb"));
            using var memory = new MemoryStream();
            stream.CopyTo(memory);

            var bytes = memory.ToArray();
            scene = GltfLoader.Load(bytes, "board.glb");

            // The loader gives the file one root node of its own and hangs everything under it, so this is
            // both the thing to turn and the thing to search. Counting scene.Children instead, as the first
            // draft did, reports one node and finds no components at all.
            _root = scene.Children.Count > 0 ? scene.Children[0] : null;

            var parts = _root?.Descendants.OfType<MeshNode>().ToList() ?? [];
            _detail = $"{bytes.Length / 1024:N0} KB of .glb: {parts.Count} nodes, " +
                      $"{parts.Select(node => node.Mesh).OfType<Mesh>().Distinct().Count()} distinct " +
                      $"meshes, {parts.Sum(node => node.Mesh?.TriangleCount ?? 0):N0} triangles drawn.";
        }
        catch (Exception e)
        {
            scene = new Scene();
            _detail = $"The board failed to load: {e.GetType().Name}: {e.Message}";
            _cost = "";
            scene.Background = Color.FromRgb(14, 16, 20);
            return scene;
        }

        scene.Background = Color.FromRgb(13, 15, 19);

        // The environment is most of the lighting here, because most of what is interesting on a board is
        // metal — gold pads, aluminium fins, the aluminium cans on the capacitors — and metal is only what
        // it reflects. The key is there to put an edge on the fins and a highlight on the socket frame.
        var studio = Environments.Studio();
        scene.Environment = EnvironmentLight.FromTexture(studio, 1.15f);

        // The environment and the key were balanced the wrong way round at first: the environment was
        // carrying almost everything at 1.85, which is what a board needs only if its metals are too
        // dark to reflect anything. With the metals at their real reflectance that much environment
        // instead lit up the *dielectrics* — every connector on the board showed so much of the room
        // that near-black nylon rendered as pale grey. The environment still does the metals; the key
        // is what puts an edge on the fins and a highlight on the socket frame.
        scene.Light.Direction = Vector3.Normalize(new Vector3(-0.42f, -0.78f, -0.46f));
        scene.Light.Color = new Vector3(1.00f, 0.98f, 0.94f);
        scene.Light.Intensity = 1.70f;
        scene.Light.Ambient = 0.02f;

        Silkscreen();
        Split();

        return scene;
    }

    /// <summary>
    /// The silkscreen is coplanar with the board by construction, so it needs the bias or it crawls.
    ///
    /// It is found by material name rather than by node name because the name is the thing the build
    /// script and this file agreed on — <c>board.silk</c> is written once in build-pcb.py and read once
    /// here. Rename it there and this stops finding it, which is a failure that shows immediately.
    /// </summary>
    private void Silkscreen()
    {
        if (_root is null)
            return;

        foreach (var node in _root.Descendants)
        {
            if (node is not MeshNode { Material: { Name: "board.silk" } material })
                continue;

            material.DepthBias = 1f;
            material.DepthBiasSlope = 1.4f;
        }
    }

    /// <summary>
    /// Moves the small parts under one holder, and builds the merged alternative beside it.
    ///
    /// The passives are picked out by the mesh they share rather than by their own names: they are the
    /// nodes whose geometry is one of the four <c>passive.*</c> meshes, which is the same fact that makes
    /// them instances in the first place.
    /// </summary>
    private void Split()
    {
        if (_root is null)
            return;

        var passives = _root.Descendants
            .OfType<MeshNode>()
            .Where(node => node.Mesh is { Name: { } name }
                           && name.StartsWith("passive.", StringComparison.Ordinal))
            .ToList();

        if (passives.Count == 0)
            return;

        _loose = new Node { Name = "passives.loose" };
        _merged = new Node { Name = "passives.merged" };
        _root.Children.Add(_loose);
        _root.Children.Add(_merged);

        // The world transform is read before the move, because merging bakes it into the vertices and the
        // move is what would change it. Both holders sit at their parent's origin unrotated, so in fact
        // nothing does change — but reading first costs nothing and does not depend on that staying true.
        //
        // Add detaches each node from wherever it was on the way in. That is one line here and it was a
        // bug until 12.1.0-preview.3: the node stayed in its old parent's list as well, so it drew twice.
        var byMaterial = new Dictionary<Material, List<Mesh>>();
        foreach (var node in passives)
        {
            if (!byMaterial.TryGetValue(node.Material, out var group))
                byMaterial[node.Material] = group = [];

            group.Add(node.Mesh!.Transformed(node.WorldTransform));
            _loose.Children.Add(node);
        }

        foreach (var (material, meshes) in byMaterial)
        {
            _merged.Children.Add(new MeshNode(Mesh.Merge(meshes), material)
            {
                Name = $"passives.{material.Name}"
            });
        }

        _cost = $"For these parts that is {passives.Count} draws against {byMaterial.Count}, " +
                $"for the same {Triangles(_merged):N0} triangles either way.";

        Show(null);
    }

    public override void Update(Scene scene, double elapsed)
    {
        var merged = (int)(elapsed / 4.0) % 2 == 0;
        if (merged != _showMerged)
        {
            _showMerged = merged;
            Show(scene);
        }

        // Slowly, and not all the way round. A board is a flat thing: past about a radian it is edge-on and
        // there is nothing to look at, which is why this swings rather than spins.
        if (_root is not null)
        {
            // Half a radian was right when the model ended at the board's edge. The panel is two units
            // out from the axis this turns about, so the same angle swings it a full unit sideways and
            // out of frame; less angle, same look, everything stays on screen.
            _root.Rotation =
                Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.Sin((float)elapsed * 0.22f) * 0.32f);
        }

        scene.Invalidate();
    }

    private void Show(Scene? scene)
    {
        if (_loose is not null)
            _loose.IsVisible = !_showMerged;

        if (_merged is not null)
            _merged.IsVisible = _showMerged;

        scene?.Invalidate();
    }

    private static int Triangles(Node root) => root.Descendants
        .OfType<MeshNode>()
        .Sum(node => node.Mesh?.TriangleCount ?? 0);
}
