using System.Numerics;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// The benchmark: 125 spheres sharing one mesh, 128,002 triangles, 126 draw calls.
///
/// This is the scene every performance number in the documentation was measured on, which is the reason it
/// is in the demo rather than in a private test — a claimed frame rate you cannot reproduce is a claim, not a
/// measurement.
/// </summary>
public sealed class StressScene : DemoScene
{
    private const int Side = 5;

    public override string Title => "Stress test";

    public override string Summary => "128,002 triangles, 126 draws, one shared mesh";

    public override string Notes =>
        """
        A 5 x 5 x 5 grid of spheres, every one referencing the same Mesh instance, plus a ground plane. The
        mesh is uploaded to the GPU once and drawn 125 times: GPU buffers are cached against mesh identity,
        which is why Mesh exposes its arrays as init-only.

        This is the scene behind the published frame rates — 120 fps on macOS under both Metal and OpenGL, 120
        in Chrome under WebGL 2, 60 on an iPhone (vsync-capped), and around 17 on the CPU fallback. Switch the
        renderer with the picker above and read the counter; that is the whole argument for having a GPU path.
        """;

    public override Scene Build()
    {
        var scene = new Scene();

        // 32 x 16 is the default: 1,024 triangles each, 128,000 for the grid.
        var sphere = Primitives.Sphere(0.34f);

        scene.Children.Add(new MeshNode(Primitives.Plane(9f, 9f), new Material
        {
            BaseColor = new Vector4(0.13f, 0.14f, 0.16f, 1f),
            Roughness = 0.9f
        })
        {
            Position = new Vector3(0f, -1.4f, 0f),
            IsPickable = false
        });

        for (var z = 0; z < Side; z++)
        for (var y = 0; y < Side; y++)
        for (var x = 0; x < Side; x++)
        {
            // A colour ramp through the volume, so the grid reads as a solid rather than as noise.
            var u = x / (Side - 1f);
            var v = y / (Side - 1f);
            var w = z / (Side - 1f);

            scene.Children.Add(new MeshNode(sphere, new Material
            {
                BaseColor = new Vector4(0.25f + u * 0.65f, 0.25f + v * 0.65f, 0.30f + w * 0.60f, 1f),
                Metallic = w > 0.5f ? 0.85f : 0f,
                Roughness = 0.2f + v * 0.6f
            })
            {
                Position = new Vector3((x - 2) * 0.8f, (y - 2) * 0.8f, (z - 2) * 0.8f),
                IsPickable = false
            });
        }

        return scene;
    }
}
