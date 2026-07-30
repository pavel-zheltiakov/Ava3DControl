using System.Numerics;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// The same colours as dielectrics and as metals, side by side, because that difference is the one thing
/// about metallic-roughness that has to be seen rather than explained.
/// </summary>
public sealed class MaterialsScene : DemoScene
{
    public override string Title => "Materials";

    public override string Summary => "Dielectrics above, metals below, same colours";

    public override string Notes =>
        """
        Top row: Metallic = 0. The base colour is diffuse albedo, and the specular highlight is white and
        weak — a 4% reflection, which is what a non-conductor does.

        Bottom row: Metallic = 1, identical base colours. There is no diffuse term at all now; the base
        colour has become the tint of the reflection, so each sphere shows the sky-and-ground environment in
        its own hue. The gold reads as gold for exactly this reason, and a metal in a scene with nothing to
        reflect reads as black.
        """;

    public override Scene Build()
    {
        var scene = new Scene();
        var sphere = Primitives.Sphere(0.45f);

        Vector4[] colors =
        [
            new(0.95f, 0.93f, 0.88f, 1f), // silver / white paint
            new(1.00f, 0.77f, 0.34f, 1f), // gold
            new(0.95f, 0.64f, 0.54f, 1f), // copper
            new(0.56f, 0.57f, 0.58f, 1f), // iron
            new(0.30f, 0.55f, 0.90f, 1f), // blue
            new(0.85f, 0.30f, 0.32f, 1f)  // red
        ];

        for (var i = 0; i < colors.Length; i++)
        {
            var x = (i - (colors.Length - 1) / 2f) * 1.05f;

            scene.Children.Add(new MeshNode(sphere, new Material
            {
                Name = "dielectric",
                BaseColor = colors[i],
                Metallic = 0f,
                Roughness = 0.4f
            })
            {
                Position = new Vector3(x, 0.58f, 0f)
            });

            scene.Children.Add(new MeshNode(sphere, new Material
            {
                Name = "metal",
                BaseColor = colors[i],
                Metallic = 1f,
                Roughness = 0.25f
            })
            {
                Position = new Vector3(x, -0.58f, 0f)
            });
        }

        return scene;
    }

    /// <summary>Square-on, so the two rows can be compared column by column.</summary>
    public override void Frame(Camera camera)
    {
        camera.Yaw = 0f;
        camera.Pitch = 0.06f;
    }
}
