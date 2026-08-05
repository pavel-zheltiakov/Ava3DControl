using System.Numerics;
using Ava3D.Demo.Textures;
using Avalonia.Media;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// Sixteen coloured lamps on a tree, and four light slots to spend on them.
///
/// This is the scene that makes the cap concrete. A string of lights is the most ordinary thing in the
/// world and it is already four times more emitters than a forward renderer will carry in one pass, so
/// the interesting question is not "how do I add a light" but "which four are real this frame". Three
/// chase patterns answer it three different ways.
/// </summary>
public sealed class LampTreeScene : DemoScene
{
    private const int Lamps = 16;
    private const int Slots = 4;

    private const float Sequence = 7f;
    private const float Cycle = Sequence * 3f;
    private const float Strength = 2.6f;

    // Each tier of the tree, bottom to top: where it starts and ends, and its radius at each end. The
    // lamps read this back so they sit just clear of the surface instead of half-buried in it.
    private static readonly (float Bottom, float Top, float BottomRadius, float TopRadius)[] Tiers =
    [
        (0.50f, 1.80f, 1.55f, 0.30f),
        (1.45f, 2.55f, 1.18f, 0.18f),
        (2.25f, 3.30f, 0.85f, 0.02f)
    ];

    // Seven colours, not eight. Sixteen lamps quartered gives 0, 4, 8, 12 — and with eight colours that
    // is two colours twice over, so the pattern that shows four lights at once would show two. Seven is
    // coprime with four, so every step of every sequence lands on four different colours.
    private static readonly Vector3[] Palette =
    [
        new(1.00f, 0.12f, 0.10f),
        new(1.00f, 0.55f, 0.10f),
        new(1.00f, 0.86f, 0.38f),
        new(0.15f, 1.00f, 0.28f),
        new(0.15f, 0.85f, 1.00f),
        new(0.22f, 0.35f, 1.00f),
        new(1.00f, 0.20f, 0.75f)
    ];

    private readonly PointLight[] _slots = new PointLight[Slots];
    private readonly MeshNode[] _bulbs = new MeshNode[Lamps];
    private readonly SpriteNode[] _halos = new SpriteNode[Lamps];
    private readonly Vector3[] _place = new Vector3[Lamps];
    private readonly Vector3[] _colour = new Vector3[Lamps];
    private readonly float[] _live = new float[Lamps];

    private string? _caption;

    public override string Title => "Sixteen lamps";

    public override string Summary => "Sixteen bulbs, four light slots, three ways to spend them";

    public override string Notes =>
        """
        Sixteen lamps on the tree. Four of them are lights.

        Scene.Lights holds four, and that is not a number you argue with — it is one uniform block and one
        loop in the shader, the same shape on OpenGL, Metal and the CPU backend. A fifth would cost a
        second pass over every surface. So a string of lights is not sixteen PointLights; it is sixteen
        pieces of unlit geometry plus four PointLights that get moved and recoloured every frame to sit on
        whichever bulbs are meant to be burning.

        That is the whole trick, and it is what a real application does too. The bulbs that are not lit
        this frame are still there, still their own colour, just dimmer — an unlit material is a flat
        colour with no shading and no cost, which is exactly what a bulb seen from across a room is.

        Three sequences, seven seconds each:

          Chase      one lamp runs the string with three fading behind it, so all four slots are spent on
                     one moving light and its tail. The tail is what tells you the slots are a budget and
                     not a switch.
          Quarters   every fourth lamp, stepping round. Four separate pools of colour on the tree at once,
                     as far apart as sixteen positions allow.
          Twinkle    each slot picks its own lamp on its own period and fades in and out. Nothing is
                     synchronised, which is why it reads as a string of lights rather than a machine.

        Range is 2.4 and Decay is 2, which is why each lamp lights the branches around it and stops: the
        pools stay separate instead of merging into one wash, and you can see where each one ends. Turn
        Range up and the tree becomes evenly lit and the whole effect disappears — the falloff is not a
        performance trick, it is the look.
        """;

    public override SceneLook Look => SceneLook.Studio;

    public override bool Animates => true;

    public override bool FramesItself => true;

    public override bool DrivesCamera => true;

    public override string? Caption => _caption;

    public override TimeSpan TourDuration => TimeSpan.FromSeconds(22);

    public override void Frame(Camera camera)
    {
        camera.Target = new Vector3(0f, 1.70f, 0f);
        camera.Distance = 6.6f;
        camera.Yaw = 0.5f;
        camera.Pitch = 0.10f;
        camera.NearPlane = 0.4f;
        camera.FarPlane = 60f;
    }

    public override Scene Build()
    {
        var scene = new Scene { Background = Color.FromRgb(6, 7, 12) };

        scene.Lights.Clear();

        // Every slot is a point light. There is no directional light at all here, which is legal and worth
        // seeing: the room's base light comes from EnvironmentLight, which is not one of the four.
        for (var k = 0; k < Slots; k++)
        {
            _slots[k] = new PointLight
            {
                Position = new Vector3(0f, 1.5f, 0f),
                Color = Vector3.One,
                Intensity = 0f,
                Range = 2.4f,
                Decay = 2f
            };

            scene.Lights.Add(_slots[k]);
        }

        scene.Environment = new EnvironmentLight
        {
            SkyColor = new Vector3(0.045f, 0.052f, 0.075f),
            GroundColor = new Vector3(0.020f, 0.018f, 0.016f)
        };

        scene.Children.Add(new MeshNode(Primitives.Plane(18f, 18f), new Material
        {
            BaseColor = new Vector4(0.075f, 0.072f, 0.080f, 1f),
            Roughness = 0.92f
        })
        {
            IsPickable = false
        });

        var needles = new Material
        {
            BaseColor = new Vector4(0.055f, 0.150f, 0.070f, 1f),
            Roughness = 0.86f,
            Cull = CullMode.Back
        };

        foreach (var (bottom, top, bottomRadius, topRadius) in Tiers)
            scene.Children.Add(new MeshNode(
                Primitives.Cylinder(topRadius, bottomRadius, top - bottom, 40),
                needles)
            {
                Position = new Vector3(0f, (bottom + top) * 0.5f, 0f)
            });

        scene.Children.Add(new MeshNode(Primitives.Cylinder(0.16f, 0.19f, 0.66f, 20), new Material
        {
            BaseColor = new Vector4(0.20f, 0.12f, 0.07f, 1f),
            Roughness = 0.9f,
            Cull = CullMode.Back
        })
        {
            Position = new Vector3(0f, 0.33f, 0f)
        });

        // Boxes at the foot of the tree, at angles, because a coloured light needs a surface facing some
        // other way before you can tell it is coloured.
        AddBox(scene, new Vector3(-1.35f, 0.32f, 0.95f), new Vector3(0.9f, 0.64f, 0.72f), 0.5f,
            new Vector4(0.34f, 0.10f, 0.12f, 1f));
        AddBox(scene, new Vector3(1.20f, 0.24f, 1.15f), new Vector3(0.72f, 0.48f, 0.60f), -0.7f,
            new Vector4(0.14f, 0.20f, 0.34f, 1f));
        AddBox(scene, new Vector3(0.45f, 0.20f, -1.55f), new Vector3(0.86f, 0.40f, 0.66f), 0.25f,
            new Vector4(0.28f, 0.26f, 0.12f, 1f));

        var glow = Space.Glow();
        var bulb = Primitives.Sphere(0.085f, 16, 10);

        for (var i = 0; i < Lamps; i++)
        {
            // The golden angle, so sixteen lamps spread round the tree instead of stacking into a stripe.
            var angle = i * 2.39996f;
            var height = 0.78f + i / (Lamps - 1f) * 2.28f;
            var radius = TreeRadius(height) + 0.17f;

            _place[i] = new Vector3(MathF.Cos(angle) * radius, height, MathF.Sin(angle) * radius);
            _colour[i] = Palette[i % Palette.Length];

            // Unlit: a bulb shaded like a stone would have a dark side, and a lamp with a dark side is a
            // contradiction. This is also what makes the twelve unlit ones free — flat colour, no lighting
            // maths at all.
            _bulbs[i] = new MeshNode(bulb, new Material
            {
                BaseColor = new Vector4(_colour[i] * 0.16f, 1f),
                Unlit = true
            })
            {
                Position = _place[i]
            };

            _halos[i] = new SpriteNode
            {
                Texture = glow,
                Position = _place[i],
                Color = _colour[i],
                Size = new Vector2(0.34f),
                Opacity = 0f,
                Blend = BlendMode.Additive,
                DepthWrite = false,
                IsVisible = false,
                RenderOrder = 1
            };

            scene.Children.Add(_bulbs[i]);
            scene.Children.Add(_halos[i]);
        }

        // The star, which never goes out and never takes a slot.
        scene.Children.Add(new MeshNode(Primitives.Sphere(0.15f, 20, 12), new Material
        {
            BaseColor = new Vector4(1.00f, 0.93f, 0.62f, 1f),
            Unlit = true
        })
        {
            Position = new Vector3(0f, 3.44f, 0f)
        });

        scene.Children.Add(new SpriteNode
        {
            Texture = glow,
            Position = new Vector3(0f, 3.44f, 0f),
            Color = new Vector3(1.00f, 0.88f, 0.55f),
            Size = new Vector2(0.85f),
            Opacity = 0.42f,
            Blend = BlendMode.Additive,
            DepthWrite = false,
            RenderOrder = 1
        });

        return scene;
    }

    public override void Update(Scene scene, Camera camera, double elapsed)
    {
        var t = (float)elapsed % Cycle;
        var beat = t % Sequence;

        Array.Clear(_live);

        if (t < Sequence)
        {
            // Chase: one head lamp and three behind it, dimmer, so the four slots are visibly a budget.
            var head = (int)MathF.Floor(beat * 3.6f);
            ReadOnlySpan<float> tail = [1f, 0.55f, 0.28f, 0.12f];

            for (var k = 0; k < Slots; k++)
                Assign(k, head - k, tail[k]);

            _caption = "Chase — one lamp and its tail, four slots on one moving light.";
        }
        else if (t < Sequence * 2f)
        {
            // Quarters: four lamps a quarter of the string apart, stepping together.
            var phase = (int)MathF.Floor(beat * 2.2f);

            for (var k = 0; k < Slots; k++)
                Assign(k, phase + k * (Lamps / Slots), 1f);

            _caption = "Quarters — every fourth lamp, four separate pools at once.";
        }
        else
        {
            // Twinkle: each slot on its own period, deliberately not a common multiple of the others.
            for (var k = 0; k < Slots; k++)
            {
                var period = 1.5f + k * 0.37f;
                var step = (int)MathF.Floor(beat / period);
                var lamp = (int)(Hash(k * 977 + step * 31) * Lamps);

                Assign(k, lamp, MathF.Sin((beat / period - step) * MathF.PI));
            }

            _caption = "Twinkle — four slots, four periods, nothing in step.";
        }

        for (var i = 0; i < Lamps; i++)
        {
            var live = _live[i];

            _bulbs[i].Material.BaseColor = new Vector4(_colour[i] * (0.16f + 0.84f * live), 1f);
            _halos[i].IsVisible = live > 0.03f;
            _halos[i].Opacity = 0.6f * live;
            _halos[i].Size = new Vector2(0.30f + 0.22f * live);
        }

        // A slow circle, because half the lamps are always round the back and a tree is not a relief.
        camera.Yaw = 0.5f + (float)elapsed * 0.13f;

        scene.Invalidate();
    }

    /// <summary>
    /// Points one of the four slots at a lamp. This is the whole of the scene's light management: the
    /// PointLight objects never move in or out of Scene.Lights, they are just re-aimed.
    /// </summary>
    private void Assign(int slot, int lamp, float strength)
    {
        lamp = ((lamp % Lamps) + Lamps) % Lamps;

        _slots[slot].Position = _place[lamp];
        _slots[slot].Color = _colour[lamp];
        _slots[slot].Intensity = Strength * strength;

        _live[lamp] = MathF.Max(_live[lamp], strength);
    }

    /// <summary>The tree's outer radius at a height, so a lamp sits on the branches rather than inside.</summary>
    private static float TreeRadius(float y)
    {
        var radius = 0.05f;

        foreach (var (bottom, top, bottomRadius, topRadius) in Tiers)
        {
            if (y < bottom || y > top)
                continue;

            // Tiers overlap; the widest one at this height is the one the eye reads as the surface.
            radius = MathF.Max(radius,
                bottomRadius + (topRadius - bottomRadius) * ((y - bottom) / (top - bottom)));
        }

        return radius;
    }

    private static void AddBox(Scene scene, Vector3 at, Vector3 size, float turn, Vector4 color) =>
        scene.Children.Add(new MeshNode(Primitives.Box(size.X, size.Y, size.Z), new Material
        {
            BaseColor = color,
            Roughness = 0.75f,
            Cull = CullMode.Back
        })
        {
            Position = at,
            Rotation = Quaternion.CreateFromYawPitchRoll(turn, 0f, 0f)
        });

    /// <summary>A deterministic 0..1 from an integer, so the twinkle is the same on every run and backend.</summary>
    private static float Hash(int value)
    {
        var h = (uint)value;
        h ^= h >> 15;
        h *= 2246822519u;
        h ^= h >> 13;
        h *= 3266489917u;
        h ^= h >> 16;
        return h / (float)uint.MaxValue;
    }
}
