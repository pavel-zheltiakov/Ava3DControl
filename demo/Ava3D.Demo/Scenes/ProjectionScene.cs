using System.Numerics;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// One material on five shapes, twice: laid on the meshes' own coordinates, and projected.
///
/// The comparison is the scene. The left row takes its texture coordinates from each mesh, which is what a
/// material has always done; the right row projects them from position. Same texture, same density asked
/// for, and the difference is what a parameterisation does to a surface that was never unwrapped.
/// </summary>
public sealed class ProjectionScene : DemoScene
{
    private const float Gap = 2.3f;

    public override string Title => "Projection";

    public override string Summary => "the same material laid on the mesh, and projected";

    public override string Notes =>
        """
        Material.UvSource decides where a texture coordinate comes from. Mesh is the default and takes the
        mesh's own; PlanarX, PlanarY and PlanarZ project down one axis for one sample; Triplanar projects
        down all three and blends them by the surface normal for three.

        The back row is Mesh and the front row is projected. Look at the sphere's pole and the rounded box's
        top: a latitude-longitude wrap collapses at the poles, so a texture laid on it wears a sunburst
        there and a seam down its side. A projection has no parameterisation to collapse.

        Look at the density too. Under a projection the scale is UvDensity, in repeats per metre — the same
        number on the small cylinder as on the big box, right by construction. On the back row every shape
        wears one repeat of the texture stretched to whatever size it happens to be, which is why a
        consumer of this control spends two hundred and twenty-six lines re-laying every mesh's coordinates
        in metres before it can texture a ship.

        Two things are done here that are usually done wrong. Opposite faces are not mirrored — projected
        naively, the +x face of a box and its -x face read the same texels in opposite handedness, which is
        invisible on noise and glaring on anything with a direction to it. And the sphere and cylinder carry
        no tangents at all, yet wear a normal map: a projection brings its own tangent frame, so geometry
        generated in code gains normal mapping it cannot otherwise have.

        The CPU renderer generates the coordinate per vertex and picks one axis per vertex rather than
        blending three per pixel, so a box is exact there and a ball has a seam where the dominant axis
        changes. It says so in the renderer panel.
        """;

    public override Node BuildSubject()
    {
        var root = new Node { Name = "projection" };

        var checks = Textures.Procedural.Checker(256, 8);
        var relief = Texture.NormalFromHeight(Texture.Noise(256, 3, 0.5f, 4), 2.4f);

        Material Make(UvSource source) => new()
        {
            BaseColorTexture = checks,
            NormalTexture = relief,
            BaseColor = new Vector4(0.85f, 0.84f, 0.8f, 1f),
            Roughness = 0.5f,
            Metallic = 0.05f,
            UvSource = source,
            UvDensity = 1.4f,
            UvSharpness = 5f,
            Name = source == UvSource.Mesh ? "laid" : "projected"
        };

        Mesh[] shapes =
        [
            Primitives.Box(1.5f, 1.5f, 1.5f),
            Primitives.Box(1.6f, 1.6f, 1.6f, 0.22f),
            Primitives.Sphere(0.85f, 40, 26),
            Primitives.Cylinder(0.6f, 0.6f, 1.7f, 28),
            Primitives.Torus(0.7f, 0.28f, 32, 18)
        ];

        for (var row = 0; row < 2; row++)
        {
            var projected = row == 1;
            var material = Make(projected ? UvSource.Triplanar : UvSource.Mesh);
            var group = new Node
            {
                Name = projected ? "projected" : "on the mesh",
                Position = new Vector3(0f, 0f, projected ? 1.5f : -1.5f)
            };

            for (var i = 0; i < shapes.Length; i++)
                group.Children.Add(new MeshNode(shapes[i], material)
                {
                    Position = new Vector3((i - (shapes.Length - 1) * 0.5f) * Gap, 0f, 0f)
                });

            root.Children.Add(group);
        }

        return root;
    }

    public override void Stage(Scene scene)
    {
        Staging.Neutral(scene);

        scene.Background = Avalonia.Media.Color.FromRgb(22, 24, 30);
        scene.Light.Direction = new Vector3(-0.42f, -0.68f, -0.6f);
        scene.Light.Intensity = 2.0f;
        scene.Light.Ambient = 0.12f;

        scene.Environment.SkyColor = new Vector3(0.4f, 0.46f, 0.56f);
        scene.Environment.GroundColor = new Vector3(0.16f, 0.14f, 0.12f);
    }

    public override void Frame(Camera camera)
    {
        camera.LookFrom(new Vector3(0f, 4.6f, 9.4f), new Vector3(0f, -0.2f, 0f));
        camera.FieldOfView = 46f;
    }
}
