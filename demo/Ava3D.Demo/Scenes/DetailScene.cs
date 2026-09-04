using System.Numerics;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// The same wall three times, receding: without the detail layer, with it, and with it fading the way it
/// should.
///
/// One feature and the picture is the argument. The layer carries the fine end of a surface — what a tiling
/// map cannot hold at any resolution anybody can afford — from one texture shared by the whole scene.
/// </summary>
public sealed class DetailScene : DemoScene
{
    private const int Panels = 5;
    private const float Step = 4.2f;

    public override string Title => "Detail layer";

    public override string Summary => "sub-millimetre surface from one texture, shared by the scene";

    public override string Notes =>
        """
        Scene.Detail holds one field of noise for the whole scene. Any material can then set DetailNormal,
        DetailRough and DetailTint to say how much of it that surface takes — so the fine end of every
        material in the scene costs one texture rather than one per material.

        The measurement behind it: a consumer of this control had 213 texels per metre over a whole ship,
        which is 4.7 mm per texel, against the 1024 to 2048 per metre a reference material wants. Closing
        that with base maps alone took its texture memory from 56 MB to 224 MB and its generation time from
        seven seconds to twenty-nine — and it was still a tiling map you could see repeating.

        Three columns of panels running away from the camera. The left column has no layer. The middle has
        it, with DetailVariance at zero — so as it fades with distance it simply disappears and the far
        panels go smooth and plastic, which is the fault this feature was meant to fix arriving at a
        different distance. The right column has DetailVariance at one: the microfacet response that the
        faded detail stood for turns into roughness instead, so the far panels stay the material they were.

        That last part is what nobody does. Normal variation and roughness are the same quantity at two
        scales — a surface covered in slopes too small to see is a rough surface — so fading a detail
        normal without giving the roughness back changes what the material is.

        The fade itself is by texel footprint rather than by distance. What makes a detail layer sparkle is
        its texels falling below the size of a pixel, and that depends on the window and the viewing angle
        as well as the range; a fade tuned in metres is tuned for one machine. Resize the window and the
        layer fades in the same place on the surface, not in the same place on the screen.

        The field carries its own gradient in two of its channels, so the normal costs one fetch and needs
        no screen-space derivatives — which is an extension a GL ES 2 context is allowed to refuse.
        """;

    public override Node BuildSubject()
    {
        var root = new Node { Name = "detail" };

        var stone = Textures.Procedural.Checker(
            256, 4,
            new SkiaSharp.SKColor(150, 146, 138),
            new SkiaSharp.SKColor(132, 128, 121));

        Material Panel(float detail, float variance) => new()
        {
            BaseColorTexture = stone,
            BaseColor = new Vector4(0.9f, 0.89f, 0.86f, 1f),
            Roughness = 0.32f,
            Metallic = 0.1f,
            UvSource = UvSource.Triplanar,
            UvDensity = 0.5f,
            DetailScale = 90f,
            DetailNormal = detail,
            DetailRough = detail * 0.5f,
            DetailTint = detail * 0.25f,
            DetailFade = 14f,
            DetailVariance = variance,
            Name = detail <= 0f ? "plain" : variance <= 0f ? "fades away" : "fades to roughness"
        };

        (string Name, Material Material, float X)[] columns =
        [
            ("none", Panel(0f, 0f), -3.4f),
            ("fading away", Panel(0.7f, 0f), 0f),
            ("fading to roughness", Panel(0.7f, 1f), 3.4f)
        ];

        foreach (var (name, material, x) in columns)
        {
            var column = new Node { Name = name, Position = new Vector3(x, 0f, 0f) };

            for (var i = 0; i < Panels; i++)
                column.Children.Add(new MeshNode(Primitives.Box(2.6f, 2.6f, 0.4f, 0.06f), material)
                {
                    Position = new Vector3(0f, 0f, -i * Step)
                });

            root.Children.Add(column);
        }

        return root;
    }

    public override void Stage(Scene scene)
    {
        Staging.Neutral(scene);

        // One field, bound once, read by every material in the scene that asks for it.
        scene.Detail.Texture = Texture.DetailField(512, 5, 0.55f, 20260904);

        scene.Background = Avalonia.Media.Color.FromRgb(16, 18, 24);
        scene.Light.Direction = new Vector3(-0.55f, -0.42f, -0.72f);
        scene.Light.Intensity = 2.4f;
        scene.Light.Ambient = 0.06f;

        scene.Environment.SkyColor = new Vector3(0.34f, 0.4f, 0.52f);
        scene.Environment.GroundColor = new Vector3(0.12f, 0.11f, 0.1f);
    }

    public override void Frame(Camera camera)
    {
        camera.LookFrom(new Vector3(0f, 1.6f, 6.2f), new Vector3(0f, 0f, -7f));
        camera.FieldOfView = 52f;
    }
}
