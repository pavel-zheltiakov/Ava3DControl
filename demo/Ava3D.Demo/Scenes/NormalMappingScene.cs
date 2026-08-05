using System.Numerics;
using Ava3D.Demo.Textures;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// The same dimpled normal map at four strengths, and a pair showing what happens without tangents.
/// </summary>
public sealed class NormalMappingScene : DemoScene
{
    public override string Title => "Normal mapping";

    public override string Summary => "One map, four strengths, and the tangent requirement";

    public override string Notes =>
        """
        Top row: NormalScale at 0, 0.5, 1 and 2 on identical geometry. The silhouettes are the same in all
        four — a normal map changes shading, never shape.

        Bottom row: the same map with and without Mesh.Tangents. A normal map is authored against the
        surface's UV frame, so without tangents there is nothing to interpret it in, and the control declines
        to apply it rather than lighting the surface from an arbitrary direction. Call
        Mesh.WithGeneratedTangents() — exporters usually omit the TANGENT attribute, and the glTF loader
        calls it for you when a material has a normal map.
        """;

    public override SceneLook Look => SceneLook.Blueprint;

    public override bool Animates => true;

    public override Scene Build()
    {
        var scene = new Scene();
        var normalMap = Procedural.Dimples();

        // Tangents are generated from the UVs; the plain sphere below deliberately has none.
        var sphere = Primitives.Sphere(0.5f).WithGeneratedTangents();
        var untangented = Primitives.Sphere(0.5f);

        float[] strengths = [0f, 0.5f, 1f, 2f];
        for (var i = 0; i < strengths.Length; i++)
        {
            scene.Children.Add(new MeshNode(sphere, new Material
            {
                Name = $"scale {strengths[i]:0.#}",
                BaseColor = new Vector4(0.72f, 0.74f, 0.78f, 1f),
                Metallic = 0.9f,
                Roughness = 0.32f,
                NormalTexture = normalMap,
                NormalScale = strengths[i]
            })
            {
                Name = $"NormalScale = {strengths[i]:0.#}",
                Position = new Vector3((i - 1.5f) * 1.2f, 0.65f, 0f)
            });
        }

        scene.Children.Add(new MeshNode(sphere, Mapped(normalMap))
        {
            Name = "with tangents",
            Position = new Vector3(-0.6f, -0.7f, 0f)
        });

        scene.Children.Add(new MeshNode(untangented, Mapped(normalMap))
        {
            Name = "no tangents — map ignored",
            Position = new Vector3(0.6f, -0.7f, 0f)
        });

        return scene;
    }

    private static Material Mapped(Texture normalMap) => new()
    {
        BaseColor = new Vector4(0.80f, 0.66f, 0.36f, 1f),
        Metallic = 1f,
        Roughness = 0.28f,
        NormalTexture = normalMap,
        NormalScale = 1.5f
    };

    public override void Update(Scene scene, double elapsed)
    {
        // A moving light is the only way to be sure a normal map is doing anything: a still one is
        // indistinguishable from a base-colour map that happens to have shading painted into it.
        var angle = (float)(elapsed * 1.1);
        scene.Light.Direction = Vector3.Normalize(new Vector3(
            MathF.Cos(angle), -0.35f, MathF.Sin(angle) * 0.5f - 0.6f));
        scene.Invalidate();
    }
}
