using System.Numerics;
using Avalonia.Media;

namespace Ava3D.Demo.Scenes.Board;

/// <summary>
/// The board as every edge it is made of. The other end of the crease-angle argument from Draftsman.
/// </summary>
public sealed class WireframeScene : DemoScene
{
    private Node? _pivot;
    private string _detail = "";
    private string _cost = "";

    public override string Title => "Wireframe";

    public override string Summary => "Every triangle edge on the board, in one line node";

    public override string Notes =>
        $"""
         {_detail}

         Mesh.GetEdges(0) — the same call the Draftsman scene makes at 26 degrees, and the whole
         difference between the two scenes is that number. At zero nothing is folded enough to skip, so
         this is the triangulation itself: every edge of every triangle, the diagonals across flat
         quads included. It is what a modeller means by a wireframe and it is not what a draughtsman
         means by one, which is why both are here.

         {_cost}

         Nothing is filled, so nothing is hidden. Everything is drawn over everything and the density is
         the picture: the four hundred small parts are dense because they are near each other, the
         heatsinks are dense because a comb of fins is a great many boxes, and the memory slots are
         nearly solid because their contacts are.

         The long diagonals sweeping right across the board are not an error and they are worth
         following. The laminate is one slab with nine mounting holes cut out of it, so its top face is
         a single polygon with nine holes in it, and something had to cut that into triangles. Those
         lines are how. They are invisible in every other scene because they lie in a flat surface —
         which is exactly the case Draftsman's crease angle drops.

         The edges are extracted once per distinct mesh and reused across every node sharing it, which
         for this board is 194 extractions rather than 610 — the small parts have four meshes between
         the four hundred of them. Then every node's transform is baked into its own copy and the lot
         goes into one array, so a wireframe of the whole board is a single draw call. The vertex count
         is the same either way; the draw count is not, and the draw count is what costs.

         Worth knowing before you reach for this on a large model: an edge is two vertices, and a closed
         triangulated mesh has about one and a half edges per triangle. A wireframe is three times the
         vertex data of the surface it describes.
         """;

    public override SceneLook Look => SceneLook.Blueprint;

    public override bool Animates => true;

    public override bool FramesItself => true;

    public override TimeSpan TourDuration => TimeSpan.FromSeconds(12);

    public override void Frame(Camera camera)
    {
        camera.Target = new Vector3(0.30f, 0.05f, 0f);
        camera.Distance = 4.0f;
        camera.Yaw = 0.52f;
        camera.Pitch = 0.62f;
        camera.FieldOfView = 40f;
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

        scene.Background = Color.FromRgb(9, 12, 16);

        var edges = BoardModel.Edges(root, 0f);

        // Below full brightness and blended, so that where lines pile up the picture gets brighter
        // rather than merely staying the same colour. That is what makes density readable at all: at
        // full opacity a hundred overlapping edges look exactly like one.
        _pivot = new Node { Name = "wireframe" };
        _pivot.Children.Add(new LineNode
        {
            Name = "wireframe.edges",
            Positions = edges,
            Color = new Vector3(0.32f, 0.86f, 0.72f),
            Width = 1f,
            Blend = BlendMode.Additive,
            Opacity = 0.34f,
            DepthTest = false
        });

        scene.Children.Remove(root);
        scene.Children.Add(_pivot);

        _cost = $"{edges.Length / 2:N0} edges — {edges.Length:N0} vertices — in one draw, against " +
                $"the 610 the shaded board takes.";

        return scene;
    }

    public override void Update(Scene scene, double elapsed)
    {
        if (_pivot is not null)
        {
            _pivot.Rotation =
                Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.Sin((float)elapsed * 0.22f) * 0.32f);
        }

        scene.Invalidate();
    }
}
