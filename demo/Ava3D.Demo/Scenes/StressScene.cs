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
    private const float Radius = 0.34f;
    private const float Spacing = 0.8f;

    // Where the bottom of the bottom row is, and therefore where the ground goes. Derived rather than
    // typed: the three numbers above have to agree with it, and when they were three separate literals
    // they stopped agreeing — the floor sat 0.54 above the bottom of the spheres and cut through them.
    private const float GroundY = -((Side - 1) * 0.5f * Spacing) - Radius;

    public override string Title => "Stress test";

    public override string Summary => "128,002 triangles, 126 draws, one shared mesh";

    public override string Notes =>
        """
        A 5 x 5 x 5 grid of spheres, every one referencing the same Mesh instance, plus a ground plane. The
        mesh is uploaded to the GPU once and drawn 125 times: GPU buffers are cached against mesh identity,
        which is why Mesh exposes its arrays as init-only — nothing can swap one out from under a buffer that
        was sized for it. Rewriting the contents is fine, and InvalidateGeometry() refills the buffer in
        place; all 125 spheres change together, because they are one mesh.

        This is the scene behind the published frame rates — 120 fps on macOS under both Metal and OpenGL, 120
        in Chrome under WebGL 2, 60 on an iPhone (vsync-capped), and around 17 on the CPU fallback. Switch the
        renderer with the picker above and read the counter; that is the whole argument for having a GPU path.
        """;

    /// <summary>
    /// One plane, and it is load-bearing in two different ways.
    ///
    /// It is the 126th draw and the two triangles that make 128,000 into 128,002, so it is in the count
    /// every published frame rate was measured against and it stays. And <see cref="GroundY"/> is derived
    /// from the grid rather than typed, so the bottom row rests on it exactly — anything mounting the grid
    /// somewhere else has to put its own floor at that height or the spheres will hang above it or sink
    /// through it, which is precisely the bug the constant was introduced to fix.
    /// </summary>
    public override void Stage(Scene scene) =>
        scene.Children.Add(new MeshNode(Primitives.Plane(9f, 9f), new Material
        {
            BaseColor = new Vector4(0.13f, 0.14f, 0.16f, 1f),
            Roughness = 0.9f
        })
        {
            Position = new Vector3(0f, GroundY, 0f),
            IsPickable = false
        });

    public override Node BuildSubject()
    {
        var grid = new Node { Name = "grid" };

        // 32 x 16 is the default: 1,024 triangles each, 128,000 for the grid.
        var sphere = Primitives.Sphere(Radius);

        for (var z = 0; z < Side; z++)
        for (var y = 0; y < Side; y++)
        for (var x = 0; x < Side; x++)
        {
            // A colour ramp through the volume, so the grid reads as a solid rather than as noise.
            var u = x / (Side - 1f);
            var v = y / (Side - 1f);
            var w = z / (Side - 1f);

            grid.Children.Add(new MeshNode(sphere, new Material
            {
                BaseColor = new Vector4(0.25f + u * 0.65f, 0.25f + v * 0.65f, 0.30f + w * 0.60f, 1f),
                Metallic = w > 0.5f ? 0.85f : 0f,
                Roughness = 0.2f + v * 0.6f
            })
            {
                Position = new Vector3(
                    (x - (Side - 1) * 0.5f) * Spacing,
                    (y - (Side - 1) * 0.5f) * Spacing,
                    (z - (Side - 1) * 0.5f) * Spacing),
                IsPickable = false
            });
        }

        return grid;
    }
}
