using System.Numerics;
using Avalonia.Media;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// Signage in a dark room: the scene where an emissive material stops looking like a bright patch painted
/// on and starts looking like something that is lit.
/// </summary>
public sealed class BloomScene : DemoScene
{
    private Node? _row;

    public override string Title => "Bloom";

    public override string Summary => "A bright thing that spills light onto what is around it";

    public override string Notes =>
        """
        An emissive material without bloom is a rectangle of bright colour. It has the right hue and the
        right brightness and it reads as paint, because nothing in the picture acknowledges it — a real
        lamp scatters in the air, in the lens and in the eye, and what arrives is a glow that reaches past
        the lamp's own edges. Three numbers on the scene put that back.

        BloomThreshold is what counts as bright. BloomIntensity is how much of the blurred result is added
        back, and zero switches the whole thing off. BloomRadius is how far the glow spreads, in device
        pixels rather than world units — a lens effect belongs to the picture, and a radius in world units
        would make a lamp's glow shrink as you backed away, which is the opposite of what happens.

        Vignette is the fourth, has no physical justification whatever, and is here because a darkened
        border holds the eye in the middle of the frame. Photography spent a century removing it and
        cinema put it back.

        Where it happens is worth knowing before tuning it. This is a compositing step: the frame is
        rendered, tone-mapped and gamma-encoded, and then the finished picture is blurred and added to
        itself. So what blooms is what came out bright on screen rather than what was bright in the scene,
        and the tone curve has already compressed the highlights — a lamp a hundred times over range and
        one twice over range both arrive near white and bloom about the same. Doing it properly means
        blurring the linear image before the curve, which needs a second render target on four backends;
        this needs none.

        It also means every renderer produces the same effect by construction rather than by four careful
        transcriptions, because all four finish by handing the same finished image to Skia. The panel will
        say OpenGL, Metal, Vulkan or Skia and the glow will be the same glow.
        """;

    public override bool Animates => true;

    public override bool FramesItself => true;

    public override void Frame(Camera camera)
    {
        camera.Target = new Vector3(0f, 0.1f, 0f);
        camera.Distance = 8.5f;
        camera.Yaw = 0.35f;
        camera.Pitch = 0.18f;
    }

    public override void Stage(Scene scene)
    {
        scene.Background = Color.FromRgb(6, 7, 11);

        // Dim, so the emissive strips are the brightest thing by a wide margin and the threshold has an
        // easy job. A bloom scene lit like a studio blooms the studio.
        scene.Light.Direction = Vector3.Normalize(new Vector3(-0.3f, -0.8f, -0.5f));
        scene.Light.Intensity = 0.35f;
        scene.Light.Ambient = 0.02f;
        scene.Environment = EnvironmentLight.Studio(0.08f);

        scene.BloomThreshold = 0.7f;
        scene.BloomIntensity = 0.9f;
        scene.BloomRadius = 22f;
        scene.Vignette = 0.35f;
    }

    public override Node BuildSubject()
    {
        var root = new Node { Name = "signage" };

        // A back wall for the glow to land on, so the effect is visible against something rather than
        // against the background colour.
        root.Children.Add(new MeshNode(
            Primitives.Plane(18f, 10f, 1, 1),
            new Material { BaseColor = new Vector4(0.16f, 0.17f, 0.20f, 1f), Roughness = 0.95f })
        {
            Name = "wall",
            Position = new Vector3(0f, 1f, -2.4f),
            RotationDegrees = new Vector3(90f, 0f, 0f)
        });

        root.Children.Add(new MeshNode(
            Primitives.Plane(18f, 8f, 1, 1),
            new Material { BaseColor = new Vector4(0.13f, 0.14f, 0.17f, 1f), Roughness = 0.9f })
        {
            Name = "floor",
            Position = new Vector3(0f, -1.6f, 0f),
            CastsShadow = false
        });

        _row = new Node { Name = "strips" };

        // Three strips at different brightnesses, so the threshold is visibly a threshold: the dimmest is
        // near it and the brightest is well past.
        var colours = new[]
        {
            (new Vector3(0.20f, 0.85f, 1.00f), 0.55f),
            (new Vector3(1.00f, 0.35f, 0.20f), 1.00f),
            (new Vector3(0.55f, 1.00f, 0.35f), 1.80f)
        };

        for (var i = 0; i < colours.Length; i++)
        {
            var (colour, strength) = colours[i];

            _row.Children.Add(new MeshNode(
                Primitives.Box(1.9f, 0.28f, 0.12f),
                new Material
                {
                    BaseColor = new Vector4(colour * strength, 1f),
                    // Unlit, so the strip is exactly the colour asked for and the threshold is being
                    // applied to a number the scene chose rather than to one the lighting produced.
                    Shading = ShadingModel.Unlit
                })
            {
                Name = $"strip{i}",
                Position = new Vector3((i - 1) * 2.7f, 0.6f, -2.2f)
            });
        }

        root.Children.Add(_row);

        return root;
    }

    public override void Update(Scene scene, double elapsed)
    {
        if (_row is null)
            return;

        // The middle strip pulses, so the glow is visibly following the brightness rather than being a
        // static halo drawn around a shape.
        if (_row.Find<MeshNode>("strip1") is { } strip)
        {
            var pulse = 0.55f + 0.65f * (float)(0.5 + 0.5 * Math.Sin(elapsed * 1.6));
            strip.Material.BaseColor = new Vector4(new Vector3(1.00f, 0.35f, 0.20f) * pulse, 1f);
        }
    }
}
