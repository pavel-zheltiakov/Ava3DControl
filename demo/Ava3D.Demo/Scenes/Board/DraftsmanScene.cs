using System.Numerics;
using Avalonia.Media;

namespace Ava3D.Demo.Scenes.Board;

/// <summary>
/// The same board as a drawing-office print: white paper, black lines, and no lighting at all.
/// </summary>
public sealed class DraftsmanScene : DemoScene
{
    private Node? _pivot;
    private string _detail = "";
    private string _cost = "";

    public override string Title => "Draftsman";

    public override string Summary => "The board as a line drawing — one crease angle, two draws";

    public override string Notes =>
        $"""
         {_detail}

         Every surface is the same unlit white. Material.Unlit emits base colour exactly, with no key
         light, no environment and no shading term of any kind, which is what makes a drawing a drawing:
         a lit white board has a bright side and a dark side, and the moment it does it stops reading as
         paper. Emissive is the substitute that looks equivalent and is not — emissive is added to a
         lighting result that is not zero, so the terminator is still there under it.

         The lines are Mesh.GetEdges(26), and 26 is the whole of the scene. That argument is the angle
         two faces must fold through before the edge between them is drawn: at 0 you get the full
         triangulation, at 180 only the boundary of an open surface, and in between you get the folds a
         person would draw and nothing else. A sphere at 26 gives nothing at all, which is correct — it
         has no edges, it has curvature.

         A detail the board forced: every part here has a 0.13 mm bevel on it, so nothing on this model
         has a 90 degree fold left. Each original corner is two 45 degree ones, a tenth of a millimetre
         apart, which is why the crease angle has to be under 45 and why the outlines come out slightly
         heavy — they are two lines that meet within a pixel at any distance the board is visible from.

         {_cost}

         Hidden lines are removed by the depth buffer rather than by geometry: the white fill is
         opaque, so it hides the edges behind it, and it carries Material.DepthBias so the edges lying
         exactly on its surface win the depth test instead of fighting it. That is the pair the property
         exists for, and it is not available everywhere — the browser has no glPolygonOffset it can
         safely call and the CPU fallback has no depth buffer to bias — so on those two the coplanar
         lines break up. RenderInfo.Features says so at runtime.

         The lines carry RenderOrder 1 for the same reason, and it costs the GPU path nothing. Without
         a depth buffer a line node is ordered once, as a whole object, at its own origin — so half the
         paper was painted over the whole drawing and the print came out blank. Last in the order, the
         fallback draws every line and removes none of the hidden ones. That is the right way round to
         lose it: a wireframe of a board is still a board, and a blank sheet is not.
         """;

    public override SceneLook Look => SceneLook.Blueprint;

    public override bool Animates => true;

    public override bool FramesItself => true;

    public override TimeSpan TourDuration => TimeSpan.FromSeconds(12);

    public override void Frame(Camera camera)
    {
        camera.Target = new Vector3(0.30f, 0.05f, 0f);
        camera.Distance = 4.2f;
        camera.Yaw = 0.30f;
        camera.Pitch = 0.92f;
        camera.FieldOfView = 38f;
        camera.NearPlane = 0.2f;
        camera.FarPlane = 40f;
    }

    public override Scene Build()
    {
        Scene scene;
        Node root;

        try
        {
            (scene, root, _detail) = BoardModel.Load();
        }
        catch (Exception e)
        {
            scene = new Scene();
            _detail = $"The board failed to load: {e.GetType().Name}: {e.Message}";
            return scene;
        }

        // Paper, and light enough that the black lines carry the whole drawing. Not pure white: a
        // page that reaches the top of the range leaves the fills nowhere to sit behind the lines.
        scene.Background = Color.FromRgb(243, 244, 240);

        var edges = BoardModel.Edges(root, 26f);
        var solid = BoardModel.Solid(root);

        // The whole board as one mesh and one material, which nothing here loses: a drawing has no
        // moving parts, nothing in it is picked, and merging is the difference between 610 draws and
        // one. The trade is the same one the Motherboard scene states — merged geometry cannot be
        // moved, hidden or taken apart afterwards, and none of that is wanted here.
        _pivot = new Node { Name = "drawing" };
        _pivot.Children.Add(new MeshNode(solid, new Material
        {
            Name = "paper",
            BaseColor = new Vector4(0.965f, 0.968f, 0.960f, 1f),
            Unlit = true,
            DepthBias = 1f,
            DepthBiasSlope = 1.4f
        })
        {
            Name = "drawing.fill"
        });

        // RenderOrder above the fill, which changes nothing where there is a depth buffer and is the
        // whole drawing where there is not. A line node is sorted as one object by the CPU fallback —
        // it has no depth buffer to test each segment against — and this drawing's lines all sit at the
        // board's own origin, so at order 0 the near half of the paper was painted over every one of
        // them and the print came out blank. Ordered last, the fallback loses hidden-line removal and
        // keeps the drawing, which is the right way round to lose it.
        _pivot.Children.Add(new LineNode
        {
            Name = "drawing.lines",
            Positions = edges,
            Color = new Vector3(0.055f, 0.060f, 0.075f),
            Width = 1f,
            Blend = BlendMode.Alpha,
            Opacity = 0.92f,
            RenderOrder = 1
        });

        // The copper, in the grey a fabrication drawing prints it in. It comes from the router's own
        // output rather than from the board's texture, which is the only way it can be lines at all:
        // in every other scene these are pixels painted on the laminate.
        var board = BoardData.Read();
        _pivot.Children.Add(new LineNode
        {
            Name = "drawing.copper",
            Positions = BoardModel.Copper(board, board.Runs),
            Color = new Vector3(0.44f, 0.46f, 0.50f),
            Width = 1f,
            Blend = BlendMode.Alpha,
            Opacity = 0.75f,
            RenderOrder = 1
        });

        // The loaded root goes away entirely: its 610 nodes are all inside the merged mesh now, and
        // leaving them in the scene would draw the board twice, once shaded and once as paper.
        scene.Children.Remove(root);
        scene.Children.Add(_pivot);

        _cost = $"That is {edges.Length / 2:N0} edges and {solid.TriangleCount:N0} triangles in two " +
                $"draws, plus one more for {board.Runs.Count:N0} runs of copper.";

        return scene;
    }

    public override void Update(Scene scene, double elapsed)
    {
        if (_pivot is not null)
        {
            _pivot.Rotation =
                Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.Sin((float)elapsed * 0.20f) * 0.26f);
        }

        scene.Invalidate();
    }
}
