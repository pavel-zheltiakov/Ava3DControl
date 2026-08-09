using System.Numerics;

namespace Ava3D.Demo.Scenes;

/// <summary>A grid of nameable objects, one of them deliberately not pickable.</summary>
public sealed class PickingScene : DemoScene
{
    public override string Title => "Picking";

    public override string Summary => "Click an object — ray against triangles, not against pixels";

    public override string Notes =>
        """
        Click anything. The view raycasts the scene and raises Picked with the node, the triangle index, the
        distance and the world-space hit point — Möller-Trumbore against the actual triangles, rejected first
        by each node's bounding box, so a click is cheap even in a large scene.

        Geometry, not pixels: there is no colour buffer to read back, which is what makes this work identically
        on the GPU and on the CPU fallback, and in the browser where reading a pixel means a round trip.

        The dark ground plane sets IsPickable = false, so clicks pass through it. The selected object is
        highlighted by cloning its Material and tinting the clone — materials are shared by reference, so
        tinting the original would recolour everything using it.
        """;

    public override bool WantsPicking => true;

    /// <summary>
    /// A floor, and it is doing a job rather than filling space: it is the surface behind every miss, and
    /// it sets <see cref="Node.IsPickable"/> false so a click that lands on it reports nothing at all
    /// rather than reporting the floor. That is stage work — a room mounting these would want its own
    /// floor to answer a miss the same way, which is why the flag belongs to whatever is holding the
    /// scene up rather than to the twelve objects.
    /// </summary>
    public override void Stage(Scene scene) =>
        scene.Children.Add(new MeshNode(Primitives.Plane(9f, 9f), new Material
        {
            BaseColor = new Vector4(0.14f, 0.15f, 0.17f, 1f),
            Roughness = 0.9f
        })
        {
            Name = "Ground (not pickable)",
            Position = new Vector3(0f, -0.75f, 0f),
            IsPickable = false
        });

    public override Node BuildSubject()
    {
        var grid = new Node { Name = "targets" };
        var sphere = Primitives.Sphere(0.34f);
        var box = Primitives.Box(0.6f, 0.6f, 0.6f);

        for (var z = 0; z < 3; z++)
        for (var x = 0; x < 4; x++)
        {
            var useBox = (x + z) % 2 == 0;
            var index = z * 4 + x;

            grid.Children.Add(new MeshNode(useBox ? box : sphere, new Material
            {
                BaseColor = new Vector4(0.35f + x * 0.14f, 0.44f, 0.80f - z * 0.16f, 1f),
                Roughness = 0.4f
            })
            {
                Name = $"{(useBox ? "Box" : "Sphere")} {index}",
                Position = new Vector3((x - 1.5f) * 0.95f, -0.35f, (z - 1) * 0.95f),
                Tag = index
            });
        }

        return grid;
    }
}
