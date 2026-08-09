using System.Numerics;
using Avalonia.Media;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// Three low-polygon solids, each built once and shaded both ways: smooth on the left of its pair,
/// faceted on the right.
/// </summary>
public sealed class FlatShadingScene : DemoScene
{
    private Node? _turntable;

    public override string Title => "Flat shading";

    public override string Summary => "The same geometry as a badly lit tube and as an octagonal drum";

    public override string Notes =>
        """
        Each pair is one mesh. The left of the two is what Primitives builds; the right is the same call
        with WithFlatNormals() on the end.

        Smooth shading averages the normals of the faces that meet at a vertex, which is right for anything
        that is meant to be round and is a lie when the shape has only eight sides. The shading says
        "cylinder" while the silhouette says "octagon", the eye believes the silhouette, and the object
        reads as badly lit rather than as few-sided. Flat shading gives every triangle its own normal, so
        each face is one flat shade and the edges between them are hard — which is what makes an eight-sided
        drum look deliberate.

        It is a mesh operation and not a material flag, and that is the interesting part. Splitting the
        vertices is arithmetic on a buffer, so all three renderers draw it identically by construction — and
        the CPU fallback, which lights once per vertex and has no fragment shader to take a derivative in,
        gets it for free rather than not at all.

        What it costs is vertices: three per triangle, with nothing shared. That is the price of a hard
        edge, it is what a flat-shading exporter does too, and it is why this belongs in the line that
        builds the mesh rather than in a property you can toggle.

        WithSmoothNormals() is the way back. It welds by position first, so a mesh that has already been
        split — by this, by an STL file, by an exporter — smooths as though it never had been.
        """;

    public override SceneLook Look => SceneLook.Studio;

    public override bool Animates => true;

    public override bool FramesItself => true;

    public override void Frame(Camera camera)
    {
        camera.Target = new Vector3(0f, -0.05f, 0f);
        camera.Distance = 7.5f;
        camera.Yaw = 0.16f;
        camera.Pitch = 0.28f;
        camera.NearPlane = 0.4f;
        camera.FarPlane = 40f;
    }

    /// <summary>
    /// One raking light and a studio environment, and no floor.
    ///
    /// The angle is the whole stage. What tells you a face is flat is the highlight sitting still on it and
    /// then stepping to the next one, and a light square on to the row gives every facet the same shade —
    /// which is the one lighting arrangement under which this comparison shows nothing.
    /// </summary>
    public override void Stage(Scene scene)
    {
        scene.Background = Color.FromRgb(22, 21, 20);

        scene.Light.Direction = Vector3.Normalize(new Vector3(-0.45f, -0.62f, -0.65f));
        scene.Light.Intensity = 1.15f;
        scene.Environment = EnvironmentLight.Studio(0.34f);
    }

    public override Node BuildSubject()
    {
        // The three shapes the low-polygon look is actually made of: a drum, a ring and a dome. All three
        // share vertices as Primitives builds them, so no choice of arguments gets to the faceted version.
        Mesh[] shapes =
        [
            Primitives.Cylinder(0.62f, 0.62f, 1.15f, 8),
            Primitives.Torus(0.62f, 0.22f, 10, 6),
            Primitives.Sphere(0.72f, 14, 8, latitudeDegrees: 90f)
        ];

        _turntable = new Node { Name = "pairs" };

        var brass = new Material
        {
            Name = "smooth",
            BaseColor = new Vector4(0.78f, 0.62f, 0.34f, 1f),
            Metallic = 0.35f,
            Roughness = 0.50f
        };

        for (var i = 0; i < shapes.Length; i++)
        {
            // Side by side rather than one behind the other. The two differ in their shading and not in
            // their silhouette, so they have to be compared at the same angle in the same light — and a
            // back row is half hidden by the front one exactly where the comparison is.
            var x = (i - 1) * 2.9f;

            _turntable.Children.Add(new MeshNode(shapes[i], brass)
            {
                Name = $"smooth {i}",
                Position = new Vector3(x - 0.78f, 0f, 0f)
            });

            _turntable.Children.Add(new MeshNode(shapes[i].WithFlatNormals(), brass)
            {
                Name = $"flat {i}",
                Position = new Vector3(x + 0.78f, 0f, 0f)
            });
        }

        return _turntable;
    }

    public override void Update(Scene scene, double elapsed)
    {
        if (_turntable is not { } turntable)
            return;

        // Slow, and back and forth rather than round: what says "facet" is the highlight stepping from
        // one face to the next, and a full rotation would keep swinging one of each pair behind the other.
        turntable.Rotation = Quaternion.CreateFromAxisAngle(
            Vector3.UnitY, MathF.Sin((float)elapsed * 0.4f) * 0.5f);

        scene.Invalidate();
    }
}
