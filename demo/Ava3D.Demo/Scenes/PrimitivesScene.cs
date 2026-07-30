using System.Numerics;

namespace Ava3D.Demo.Scenes;

/// <summary>The three built-in meshes, on a ground plane, so their proportions can be compared.</summary>
public sealed class PrimitivesScene : DemoScene
{
    public override string Title => "Primitives";

    public override string Summary => "Sphere, box and plane";

    public override string Notes =>
        """
        Primitives.Sphere, Primitives.Box and Primitives.Plane are the whole set. They exist so a scene can
        be built without an asset pipeline, and because a viewer needs a ground plane more often than it
        needs anything else.

        All three carry normals and texture coordinates, so every material feature in the later scenes works
        on them. The sphere's segment and ring counts are arguments — the default 32 x 16 is 1,024 triangles.
        """;

    public override Scene Build()
    {
        var scene = new Scene();

        var ground = new MeshNode(Primitives.Plane(5f, 5f), Material.FromColor(0.16f, 0.17f, 0.19f))
        {
            Position = new Vector3(0f, -0.75f, 0f),
            // A backdrop should not be in the way of clicks aimed at the subject.
            IsPickable = false
        };
        ground.Material.Roughness = 0.9f;
        scene.Children.Add(ground);

        scene.Children.Add(new MeshNode(
            Primitives.Sphere(0.6f),
            new Material { BaseColor = new Vector4(0.85f, 0.35f, 0.30f, 1f), Roughness = 0.35f })
        {
            Name = "Sphere",
            Position = new Vector3(-1.6f, 0f, 0f)
        });

        scene.Children.Add(new MeshNode(
            Primitives.Box(1.1f, 1.1f, 1.1f),
            new Material { BaseColor = new Vector4(0.35f, 0.62f, 0.85f, 1f), Roughness = 0.45f })
        {
            Name = "Box",
            Position = new Vector3(0f, -0.2f, 0f)
        });

        scene.Children.Add(new MeshNode(
            Primitives.Plane(1.6f, 1.6f),
            new Material { BaseColor = new Vector4(0.85f, 0.75f, 0.35f, 1f), Roughness = 0.5f })
        {
            Name = "Plane",
            Position = new Vector3(1.7f, -0.1f, 0f),
            RotationDegrees = new Vector3(-70f, 0f, 0f)
        });

        return scene;
    }
}
