using System.Numerics;
using Ava3D.Demo.Textures;
using Avalonia.Media;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// The two material terms that exist because a planet needs them: a Fresnel rim on a shell above the
/// surface, and an emissive map masked to the hemisphere facing away from the light.
/// </summary>
public sealed class RimScene : DemoScene
{
    private Material _nightSide = null!;
    private int _phase = -1;

    public override string Title => "Rim and night side";

    public override string Summary => "A limb glow, and cities that only light after dark";

    public override string Notes =>
        """
        Left: an atmosphere. It is a second sphere at 1.03× the radius whose base colour is fully
        transparent, so the only thing it contributes is RimColor × RimIntensity, falling off as
        (1 − N·V) raised to RimPower. Additive, DepthWrite off, back faces culled — drawing the far side
        as well would put the glow through the planet twice.

        RimLightBias is why the glow is brightest on the sunward limb rather than evenly round the
        circle. At 0 the rim is pure Fresnel and the night limb glows just as hard, which reads as a
        neon outline rather than as air.

        Right: the same planet with a city-lights map, and EmissiveNightSide toggling every four
        seconds. With it off the cities burn through the daylight, which is the one thing a city map must
        never do. With it on they fade in across the terminator — Start and End are the N·L values the
        crossfade runs between, and putting End slightly below zero is what stops a hard line appearing
        exactly at the terminator.

        Both are material properties rather than a shader hook, which is the whole point: no GLSL, no
        MSL, and the same two spheres render on the CPU fallback too.
        """;

    public override SceneLook Look => SceneLook.Cosmic;

    public override bool Animates => true;

    public override bool FramesItself => true;

    public override void Frame(Camera camera)
    {
        camera.Target = Vector3.Zero;
        camera.Distance = 5.4f;
        camera.Yaw = 0f;
        camera.Pitch = 0.05f;
        camera.NearPlane = 0.4f;
        camera.FarPlane = 30f;
    }

    /// <summary>
    /// Space: a near-black card, one sun almost side-on, and an environment low enough to be a star field
    /// rather than a room.
    ///
    /// The sun's angle is the scene. A rim is brightest on the limb the light comes from and the cities
    /// only appear where it does not reach, so both halves of this scene are readings of one direction —
    /// which is why it is set here and swung in <see cref="Update"/> rather than left to whatever is
    /// holding the planets up. Ambient is 0.02 for the same reason: the night side has to be night.
    /// </summary>
    public override void Stage(Scene scene)
    {
        scene.Background = Color.FromRgb(4, 5, 9);

        scene.Lights.Clear();
        scene.Lights.Add(new DirectionalLight
        {
            Direction = Vector3.Normalize(new Vector3(1f, -0.08f, -0.15f)),
            Color = new Vector3(1.00f, 0.96f, 0.90f),
            Intensity = 2.2f,
            Ambient = 0.02f
        });

        scene.Environment = new EnvironmentLight
        {
            SkyColor = new Vector3(0.06f, 0.08f, 0.12f),
            GroundColor = new Vector3(0.03f, 0.03f, 0.05f)
        };
    }

    public override Node BuildSubject()
    {
        var pair = new Node { Name = "planets" };
        var (albedo, _, lights) = Space.Planet();

        // Left: body plus atmosphere shell. Tilted so a pole is not sitting in the middle of the disc,
        // where an equirectangular map's convergence is the only thing anyone would look at.
        var left = new Node
        {
            Position = new Vector3(-1.35f, 0f, 0f),
            RotationDegrees = new Vector3(-22f, 40f, 12f)
        };

        pair.Children.Add(left);

        left.Children.Add(new MeshNode(Primitives.Sphere(1.0f, 48, 32), new Material
        {
            Name = "body",
            BaseColorTexture = albedo,
            Roughness = 0.95f,
            Cull = CullMode.Back
        }));

        left.Children.Add(new MeshNode(Primitives.Sphere(1.035f, 40, 28), new Material
        {
            Name = "atmosphere",
            BaseColor = Vector4.Zero,
            RimColor = new Vector3(0.40f, 0.66f, 1.00f),
            RimPower = 3.0f,
            RimIntensity = 2.0f,
            RimLightBias = 0.4f,
            Blend = BlendMode.Additive,
            DepthWrite = false,
            Cull = CullMode.Back
        })
        {
            RenderOrder = 1
        });

        // Right: body plus city lights.
        _nightSide = new Material
        {
            Name = "cities",
            BaseColorTexture = albedo,
            Roughness = 0.95f,
            EmissiveTexture = lights,
            EmissiveColor = new Vector3(1.3f),
            EmissiveNightSide = true,
            EmissiveNightSideStart = 0.20f,
            EmissiveNightSideEnd = -0.10f,
            Cull = CullMode.Back
        };

        pair.Children.Add(new MeshNode(Primitives.Sphere(1.0f, 48, 32), _nightSide)
        {
            Position = new Vector3(1.35f, 0f, 0f),
            RotationDegrees = new Vector3(-22f, 40f, 12f)
        });

        return pair;
    }

    public override string? Caption => _phase == 0
        ? "EmissiveNightSide = true — the cities are masked to the dark hemisphere"
        : "EmissiveNightSide = false — the same map, glowing straight through the daylight";

    public override void Update(Scene scene, double elapsed)
    {
        var phase = (int)(elapsed / 4.0) % 2;
        if (phase != _phase)
        {
            _phase = phase;
            _nightSide.EmissiveNightSide = phase == 0;
        }

        // The light sweeps across rather than round, so the terminator stays on the face you can see and
        // crosses the cities instead of taking them behind the sphere.
        var angle = MathF.Sin((float)elapsed * 0.22f) * 1.15f;
        scene.Light.Direction = Vector3.Normalize(new Vector3(
            MathF.Cos(angle), -0.08f, -0.15f - MathF.Sin(angle) * 0.25f));

        scene.Invalidate();
    }
}
