using System.Numerics;
using Ava3D.Demo.Textures;
using Avalonia.Media;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// The four light slots, switched on one at a time.
///
/// Four lights in one frame is not something a still picture can teach. Every surface receives all of them
/// at once, and the eye reads the sum as "it is lit" — which is why the honest way to show a light rig is
/// to build it, one light per beat, and let the watcher see what each one added.
/// </summary>
public sealed class FourLightsScene : DemoScene
{
    private const float Cycle = 18f;
    private const float Fade = 0.8f;

    // When each light joins. The key is already on at zero; the point light is last, because it is the one
    // with something to do.
    private static readonly float[] Starts = [0f, 4f, 8f, 12f];

    private const float LampStart = 12f;
    private const float LampEnd = 17f;
    private const float Outro = 17f;

    private const float LampY = 0.15f;
    private const float LampZ = 0.75f;

    private DirectionalLight _key = null!, _fill = null!, _rim = null!;
    private PointLight _lamp = null!;
    private Node _fixture = null!;
    private MeshNode _bulb = null!;
    private SpriteNode _halo = null!;

    private readonly float[] _base = new float[4];
    private string? _caption;

    public override string Title => "Four lights";

    public override string Summary => "Three directional and one ranged point, switched on in turn";

    public override string Notes =>
        """
        This scene holds four and turns them on one at a time, because four lights arriving together are
        four lights nobody can count. Four is this scene's number, not the library's — Scene.Lights takes
        as many as you want, and each one costs about what the last one did.

        A warm key from the left, a cool fill from the right, a green rim from behind, and last a lamp that
        travels the colonnade. Each one is a separate entry in Scene.Lights, and switching one off is
        Intensity = 0 rather than removing it — the collection stays four long, so Scene.Light keeps
        meaning the same object and nothing is reshuffled mid-scene.

        The environment light is not one of the four. It is what fills the shadow side during the first
        beat, when there is genuinely only one light in the scene, and it costs nothing: EnvironmentLight
        is a two-colour gradient evaluated per pixel, not a light with a direction.

        Watch where the lamp stops mattering. The falloff matches three.js exactly —

            attenuation = pow(saturate(1 − pow(d / Range, 4)), 2) / max(pow(d, Decay), 0.01)

        — so Range is a real boundary: the windowing term reaches exactly zero at Range and stays there,
        rather than trailing off asymptotically the way bare inverse-square does. That matters more than
        it sounds. "Not quite zero" spread over a planet-sized sphere is a night side that is not dark.
        Decay is the exponent: 2 is physical, 1 is the flatter falloff a game usually wants for a headlamp.
        Range 4.2 against columns 2.2 apart means the lamp reaches three columns and no more, which is
        something you can count rather than judge.

        The lamp itself is geometry — an unlit sphere for the bulb and a metal shade above it — not a glow
        sprite pretending to be a light. A light has no position you can see, so something has to mark it,
        and a solid object that occludes and is occluded reads as a lamp where an additive disc reads as a
        hole in the frame. The small halo around the bulb is the only sprite here, and it is sized to hug
        the bulb rather than to be the light.

        Adding a fifth light is ordinary. It used to throw, on the reasoning that a light which quietly
        goes missing gets diagnosed as a shader bug; the throw is gone and the reasoning survives as the
        features panel, which says in so many words how many this renderer will draw.
        """;

    public override SceneLook Look => SceneLook.Studio;

    public override bool Animates => true;

    public override bool FramesItself => true;

    public override bool DrivesCamera => true;

    public override string? Caption => _caption;

    public override TimeSpan TourDuration => TimeSpan.FromSeconds(19);

    public override void Frame(Camera camera)
    {
        camera.Target = new Vector3(0f, -0.15f, 0.9f);
        camera.Distance = 12.2f;
        camera.Yaw = 0.24f;
        camera.Pitch = 0.20f;
        camera.NearPlane = 0.6f;
        camera.FarPlane = 60f;
    }

    /// <summary>
    /// All four slots, the environment that is not one of them, and a floor dark enough to show a pool of
    /// lamplight on.
    ///
    /// This is the scene whose subject <em>is</em> the stage. Everything the notes claim is a property of
    /// these four objects — that a fill at half the key does not cancel it, that a rim tipped too far
    /// becomes a second key, that Range 4.2 reaches three columns — so the colonnade exists to be lit and
    /// the lights are the thing being shown. A room could not mount this; it would have to become it.
    /// </summary>
    public override void Stage(Scene scene)
    {
        scene.Background = Color.FromRgb(5, 5, 8);

        scene.Lights.Clear();

        // Nearly horizontal, so the floor is grazed rather than flooded: a floor lit face-on by three
        // directional lights is a pale sheet, and a pale sheet has nowhere to show a pool of lamplight.
        _key = new DirectionalLight
        {
            Direction = Vector3.Normalize(new Vector3(0.72f, -0.40f, -0.35f)),
            Color = new Vector3(1.00f, 0.78f, 0.48f),
            Intensity = 1.5f,
            Ambient = 0.02f,
            AmbientColor = new Vector3(0.45f, 0.55f, 0.80f)
        };

        // Half the key and from the opposite side, because a fill that matches its key cancels the shape
        // the key just made. Three lights the same strength are one light with a colour problem.
        _fill = new DirectionalLight
        {
            Direction = Vector3.Normalize(new Vector3(-0.82f, -0.22f, -0.26f)),
            Color = new Vector3(0.30f, 0.52f, 1.00f),
            Intensity = 0.75f
        };

        // From behind, tipped down just far enough to catch the caps. A pure back-light is invisible on
        // anything but a silhouette edge; tipped too far it stops being a rim and becomes a second key
        // pouring onto the floor, which turns the whole scene the colour of the third light.
        _rim = new DirectionalLight
        {
            Direction = Vector3.Normalize(new Vector3(0.10f, -0.40f, 0.91f)),
            Color = new Vector3(0.60f, 1.00f, 0.76f),
            Intensity = 0.45f
        };

        _lamp = new PointLight
        {
            Position = new Vector3(-9f, LampY, LampZ),
            Color = new Vector3(1.00f, 0.66f, 0.34f),
            Intensity = 13f,
            Range = 4.2f,
            Decay = 2f
        };

        scene.Lights.Add(_key);
        scene.Lights.Add(_fill);
        scene.Lights.Add(_rim);
        scene.Lights.Add(_lamp);

        _base[0] = _key.Intensity;
        _base[1] = _fill.Intensity;
        _base[2] = _rim.Intensity;
        _base[3] = _lamp.Intensity;

        // Low, and cool. This is what the first beat's shadow side is made of, and the caption says so.
        scene.Environment = new EnvironmentLight
        {
            SkyColor = new Vector3(0.055f, 0.070f, 0.110f),
            GroundColor = new Vector3(0.018f, 0.016f, 0.014f)
        };

        scene.Children.Add(new MeshNode(Primitives.Plane(21f, 10f), new Material
        {
            // Very dark, so the lamp's pool is the brightest thing on it by a wide margin.
            BaseColor = new Vector4(0.028f, 0.030f, 0.034f, 1f),
            Roughness = 0.96f
        })
        {
            Position = new Vector3(0f, -1.42f, 0f),
            IsPickable = false
        });
    }

    public override Node BuildSubject()
    {
        var colonnade = new Node { Name = "colonnade" };

        // Dark stone. Three lights add up, and a bright albedo under three of them saturates into the same
        // white the single-light beat already reached — which is exactly how four lights come to look
        // like one.
        var stone = new Material
        {
            BaseColor = new Vector4(0.30f, 0.30f, 0.32f, 1f),
            Roughness = 0.68f,
            Metallic = 0.05f,
            Cull = CullMode.Back
        };

        // Round columns rather than boxes. A box face has one normal across the whole of it, so a light
        // sliding past it only changes the face's brightness; a cylinder carries a highlight band that
        // moves round it, which is the lamp's position made visible on the surface itself.
        var shaft = Primitives.Cylinder(0.34f, 0.38f, 2.4f, 28);
        var block = Primitives.Box(0.88f, 0.16f, 0.88f);

        for (var i = -3; i <= 3; i++)
        {
            var column = new Node { Position = new Vector3(i * 2.2f, 0f, -0.7f) };

            column.Children.Add(new MeshNode(block, stone) { Position = new Vector3(0f, -1.34f, 0f) });
            column.Children.Add(new MeshNode(shaft, stone) { Position = new Vector3(0f, -0.06f, 0f) });
            column.Children.Add(new MeshNode(block, stone) { Position = new Vector3(0f, 1.22f, 0f) });

            colonnade.Children.Add(column);
        }

        // The subject: a sphere in the open floor in front of the colonnade, where a terminator has room
        // to be seen. Mid grey rather than white, so the three coloured lights land as colour instead of
        // piling up into a white ball.
        colonnade.Children.Add(new MeshNode(Primitives.Sphere(0.8f, 48, 32), new Material
        {
            BaseColor = new Vector4(0.46f, 0.46f, 0.48f, 1f),
            Roughness = 0.42f,
            Metallic = 0.0f,
            Cull = CullMode.Back
        })
        {
            Position = new Vector3(1.3f, -0.62f, 4.4f)
        });

        // The lamp, as an object. Bulb, shade, and a halo that hugs the bulb.
        _fixture = new Node { Position = new Vector3(-9f, LampY, LampZ), IsVisible = false };

        _bulb = new MeshNode(Primitives.Sphere(0.13f, 20, 12), new Material
        {
            // Unlit, so it is its own colour at full strength no matter which way it faces. A bulb shaded
            // like a stone would have a dark side, and a light with a dark side is a contradiction.
            BaseColor = new Vector4(1.00f, 0.80f, 0.52f, 1f),
            Unlit = true
        });

        _halo = new SpriteNode
        {
            Texture = Space.Glow(),
            Color = new Vector3(1.00f, 0.66f, 0.34f),
            Size = new Vector2(0.62f),
            Opacity = 0.5f,
            Blend = BlendMode.Additive,
            DepthWrite = false,
            RenderOrder = 1
        };

        _fixture.Children.Add(_bulb);
        _fixture.Children.Add(_halo);

        // A shade above it, in dark metal, so the thing reads as a fitting rather than as a floating orb.
        _fixture.Children.Add(new MeshNode(Primitives.Cylinder(0.09f, 0.26f, 0.20f, 20), new Material
        {
            BaseColor = new Vector4(0.16f, 0.16f, 0.18f, 1f),
            Roughness = 0.35f,
            Metallic = 0.85f
        })
        {
            Position = new Vector3(0f, 0.23f, 0f)
        });

        colonnade.Children.Add(_fixture);

        return colonnade;
    }

    public override void Update(Scene scene, Camera camera, double elapsed)
    {
        var t = (float)elapsed % Cycle;

        // The last second fades the three newcomers back out, so the loop returns to one light without a
        // cut. The key never fades: it is the light the scene starts from.
        var outro = 1f - Ramp((t - Outro) / (Cycle - Outro));

        _key.Intensity = _base[0];
        _fill.Intensity = _base[1] * Ramp((t - Starts[1]) / Fade) * outro;
        _rim.Intensity = _base[2] * Ramp((t - Starts[2]) / Fade) * outro;
        _lamp.Intensity = _base[3] * Ramp((t - Starts[3]) / Fade) * outro;

        // One pass down the colonnade during the lamp's beat, left to right.
        var travel = Math.Clamp((t - LampStart) / (LampEnd - LampStart), 0f, 1f);
        var x = -9f + travel * 18f;

        // The bulb and its halo track the light's own strength, so the fixture goes dark with the light
        // rather than staying lit next to a surface it is no longer illuminating — and once there is
        // nothing left to see, it leaves rather than hanging there as an unlit prop.
        var strength = _lamp.Intensity / _base[3];

        _lamp.Position = new Vector3(x, LampY, LampZ);
        _fixture.Position = _lamp.Position;
        _fixture.IsVisible = strength > 0.02f;

        // Colour only, never alpha: the bulb stays a solid object that occludes the shade behind it, and
        // dimming it through transparency would make it a hole instead of a lamp turning down.
        _bulb.Material.BaseColor = new Vector4(1.00f * strength, 0.80f * strength, 0.52f * strength, 1f);
        _halo.Opacity = 0.5f * strength;

        _caption = t switch
        {
            < 4f => "One light. A lit side, a dark side, and the sky filling in the rest.",
            < 8f => "Two. Now the shadow side has a colour of its own.",
            < 12f => "Three. The rim from behind puts an edge on everything.",
            _ => "Four. The lamp travels — count the columns it reaches."
        };

        // A slow sway, so the highlights on the columns and the sphere move. A light rig read from one
        // fixed angle is a photograph, and a photograph hides which way the light is coming from.
        camera.Yaw = 0.24f + MathF.Sin((float)elapsed * 0.16f) * 0.20f;

        scene.Invalidate();
    }

    /// <summary>A 0..1 ramp with both ends flat, so a light arrives without a step in its derivative.</summary>
    private static float Ramp(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return t * t * (3f - 2f * t);
    }
}
