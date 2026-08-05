using System.Numerics;
using Ava3D.Demo.Textures;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// A checkerboard and a UV grid on each primitive, plus the three wrap modes on coordinates that run past
/// the edge of the image.
/// </summary>
public sealed class TexturesScene : DemoScene
{

    public override string Title => "Textures";

    public override string Summary => "Checkerboard, UV layout and the three wrap modes";

    public override string Notes =>
        """
        Top row: a checkerboard on each primitive, showing how Primitives lays out its texture coordinates.
        The sphere's seam runs down its back; the box maps each face to the whole image.

        Bottom row: the same UV grid on three planes whose coordinates run 0 to 3, with WrapU and WrapV set
        to Repeat, ClampToEdge and MirroredRepeat. Textures are shared by identity — all three planes here
        reference one image object, so it is decoded and uploaded once.

        Both maps are generated in code. See Textures/Procedural.cs; the demo ships no image files.
        """;

    public override SceneLook Look => SceneLook.Studio;

    public override Scene Build()
    {
        var scene = new Scene();

        var checker = Procedural.Checker();
        var checkerMaterial = new Material
        {
            Name = "checker",
            BaseColorTexture = checker,
            Roughness = 0.45f
        };

        scene.Children.Add(new MeshNode(Primitives.Sphere(0.55f), checkerMaterial)
        {
            Position = new Vector3(-1.5f, 0.85f, 0f)
        });
        scene.Children.Add(new MeshNode(Primitives.Box(1.0f, 1.0f, 1.0f), checkerMaterial)
        {
            Position = new Vector3(0f, 0.85f, 0f),
            RotationDegrees = new Vector3(-18f, 28f, 0f)
        });
        scene.Children.Add(new MeshNode(Primitives.Plane(1.3f, 1.3f), checkerMaterial)
        {
            Position = new Vector3(1.5f, 0.85f, 0f),
            RotationDegrees = new Vector3(-75f, 0f, 0f)
        });

        // One quad whose UVs run to 3, reused for all three wrap modes.
        var tiled = Tiled(1.2f, 3f);

        TextureWrap[] modes = [TextureWrap.Repeat, TextureWrap.ClampToEdge, TextureWrap.MirroredRepeat];
        for (var i = 0; i < modes.Length; i++)
        {
            var image = Procedural.UvGrid(wrap: modes[i], name: $"uv grid ({modes[i]})");
            scene.Children.Add(new MeshNode(tiled, new Material
            {
                Name = modes[i].ToString(),
                BaseColorTexture = image,
                Roughness = 0.55f
            })
            {
                Name = modes[i].ToString(),
                Position = new Vector3((i - 1) * 1.5f, -0.85f, 0f)
            });
        }

        return scene;
    }

    /// <summary>
    /// A flat quad facing the camera whose texture coordinates run 0..<paramref name="tiles"/>.
    ///
    /// Written out rather than taken from <see cref="Primitives"/> because the point of this scene is UVs
    /// outside 0..1, and a primitive that produced those would be a bad primitive.
    /// </summary>
    private static Mesh Tiled(float size, float tiles)
    {
        var h = size / 2f;

        return new Mesh
        {
            Positions =
            [
                new Vector3(-h, -h, 0f), new Vector3(h, -h, 0f),
                new Vector3(h, h, 0f), new Vector3(-h, h, 0f)
            ],
            Normals = [Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ],
            TexCoords =
            [
                new Vector2(0f, tiles), new Vector2(tiles, tiles),
                new Vector2(tiles, 0f), new Vector2(0f, 0f)
            ],
            Indices = [0, 1, 2, 0, 2, 3],
            Name = "tiled quad"
        };
    }
}
