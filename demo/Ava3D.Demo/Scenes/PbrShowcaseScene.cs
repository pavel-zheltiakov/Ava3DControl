using System.Numerics;
using Ava3D.Demo.Textures;
using Avalonia.Media;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// All five texture channels at once, on a generated riveted-panel material: base colour, metallic-roughness,
/// normal, emissive and occlusion.
/// </summary>
public sealed class PbrShowcaseScene : DemoScene
{
    private Node? _turntable;

    public override string Title => "Full PBR";

    public override string Summary => "All five maps on one generated material";

    public override string Notes =>
        """
        One material, five maps, all produced by Textures/Procedural.cs from a single height field so they
        agree with each other:

        base colour   — plate grey, darker in the grooves, brighter on the rivet heads
        metallic-roughness — plates polished and fully metallic, grooves rough and grimy, rivets polished
        normal        — the grooves and rivet heads, which are shading rather than geometry
        emissive      — the cyan strip along every other seam, unaffected by the lighting
        occlusion     — the grooves, which the sky cannot reach

        Watch the seam strip stay lit as the turntable carries it into shadow; that is emissive doing what
        nothing else in the model can. And the rivets are flat: the silhouette is a smooth cylinder throughout.

        The CPU fallback renders this scene with base colour only. Skia's DrawVertices modulates one image by a
        vertex colour, so there is no channel for the other four maps to arrive through — switch the renderer
        to Software to see exactly how much of this is the maps.
        """;

    public override bool Animates => true;

    public override Scene Build()
    {
        var scene = new Scene { Background = Color.FromRgb(14, 16, 21) };

        scene.Environment = new EnvironmentLight
        {
            SkyColor = new Vector3(0.34f, 0.40f, 0.52f),
            GroundColor = new Vector3(0.16f, 0.13f, 0.10f),
            Intensity = 1.1f
        };
        scene.Light.Direction = Vector3.Normalize(new Vector3(-0.5f, -0.75f, -0.42f));
        scene.Light.Color = new Vector3(1.05f, 1.0f, 0.94f);

        var maps = Procedural.Panels();

        _turntable = new Node { Name = "Turntable" };
        scene.Children.Add(_turntable);

        // Tangents are required for the normal map and the primitives do not carry them.
        var drum = Primitives.Sphere(1.05f, 64, 32).WithGeneratedTangents();
        var plate = Primitives.Box(2.6f, 0.22f, 2.6f).WithGeneratedTangents();
        var post = Primitives.Box(0.3f, 1.5f, 0.3f).WithGeneratedTangents();

        _turntable.Children.Add(new MeshNode(drum, maps.ToMaterial("panelled drum"))
        {
            Name = "Drum",
            Position = new Vector3(0f, 0.35f, 0f)
        });

        _turntable.Children.Add(new MeshNode(plate, maps.ToMaterial("panelled base"))
        {
            Name = "Base",
            Position = new Vector3(0f, -0.9f, 0f)
        });

        foreach (var (x, z) in new[] { (-1.0f, -1.0f), (1.0f, -1.0f), (-1.0f, 1.0f), (1.0f, 1.0f) })
            _turntable.Children.Add(new MeshNode(post, maps.ToMaterial("panelled post"))
            {
                Position = new Vector3(x, -0.05f, z)
            });

        return scene;
    }

    public override void Update(Scene scene, double elapsed)
    {
        if (_turntable != null)
            _turntable.RotationDegrees = new Vector3(0f, (float)(elapsed * 16.0), 0f);
    }
}
