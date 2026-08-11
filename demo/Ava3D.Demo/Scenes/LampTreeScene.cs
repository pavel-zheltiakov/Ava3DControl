using System.Numerics;
using Ava3D.Demo.Textures;
using Avalonia.Media;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// Sixteen coloured lamps on a tree, and sixteen lights to burn them with.
///
/// This scene used to be the one that made the cap concrete: sixteen bulbs, four slots, and three ways of
/// deciding which four were real. The cap is gone, so the lesson is inverted — a string of lights is the
/// most ordinary thing anyone would ask a renderer for, and now it is simply what you write. Every lamp
/// owns a <see cref="PointLight"/> that stays where it is; the sequences change what is burning, not what
/// is aimed where.
///
/// The last of the four sequences is the one that could not be drawn before, and it is deliberately last:
/// every lamp on the tree lit at once.
/// </summary>
public sealed class LampTreeScene : DemoScene
{
    private const int Lamps = 16;

    private const float Sequence = 5.5f;
    private const float Cycle = Sequence * 4f;
    private const float Strength = 2.6f;

    /// <summary>
    /// What each lamp is worth when all sixteen are burning.
    ///
    /// Sixteen lamps at the brightness of four is four times the light in the room, and a room with four
    /// times the light in it is white. This is the same total shared out rather than a cap in disguise —
    /// exactly what an application lighting a real room would do, and the reason the tree in the last beat
    /// reads as a tree rather than as a flare.
    /// </summary>
    private const float Share = 0.55f;

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

    private readonly PointLight[] _lights = new PointLight[Lamps];
    private readonly MeshNode[] _bulbs = new MeshNode[Lamps];
    private readonly SpriteNode[] _halos = new SpriteNode[Lamps];
    private readonly Vector3[] _place = new Vector3[Lamps];
    private readonly Vector3[] _colour = new Vector3[Lamps];
    private readonly float[] _live = new float[Lamps];

    private string? _caption;

    public override string Title => "Sixteen lamps";

    public override string Summary => "Sixteen bulbs, sixteen lights, four ways to burn them";

    public override string Notes =>
        """
        Sixteen lamps on the tree, and every one of them is a light.

        Scene.Lights is sixteen PointLights here, added once in Stage and never moved. Nothing is aimed,
        pooled or recycled: lamp eleven owns light eleven, and turning it on is Intensity. That is worth
        stating because this scene used to be the argument for the opposite. It held four lights and slid
        them along the string to whichever bulbs were meant to be burning, and the notes explained at
        length why four was a number you did not argue with. The renderer has stopped being the reason.

        Four sequences, five and a half seconds each:

          Chase      one lamp runs the string with five fading behind it. The tail is six lamps long
                     because six looked right, which is the entire justification available now.
          Quarters   every fourth lamp, stepping round — four pools of colour as far apart as sixteen
                     positions allow. This is what the whole scene used to look like at its busiest.
          Twinkle    all sixteen, each on its own period, nothing in step. Sixteen independent fades is
                     the pattern the old four-slot version could only imitate.
          All        every lamp at once. This is the frame that was not available at any price.

        The bulbs are still unlit geometry and still worth looking at. A lamp shaded like a stone has a
        dark side, and a bulb with a dark side is a contradiction — so Unlit is the correct material for a
        light source whatever the light count is, and it always was.

        Range is 2.4 and Decay is 2, which is why each lamp lights the branches around it and stops: the
        pools stay separate instead of merging into one wash, and you can see where each one ends. Turn
        Range up and the tree becomes evenly lit and the whole effect disappears — the falloff is not a
        performance trick, it is the look. It matters more now than it did with four, because sixteen
        unbounded lamps really would be one wash.

        What it costs. On either GPU backend, nothing you can measure — sixteen lights against one is the
        same frame rate at the same resolution. On the CPU renderer it is about a third of the frame rate,
        because that one shades per vertex and every light is another pass over each of them. Sixteen is
        the number worth designing to for that reason and not for a shader's.
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

    /// <summary>
    /// Sixteen point lights and no directional one at all, which is legal and worth seeing: what keeps
    /// the room from being black between the pools is <see cref="EnvironmentLight"/>, which is not one of
    /// the sixteen and costs the same whether there is a light in the scene or not.
    ///
    /// Each light is built where its lamp will be and left there for good, at zero intensity until
    /// <see cref="Update"/> gives it some. That is why <see cref="Place"/> runs here rather than in
    /// <see cref="BuildSubject"/> — the geometry can be built from the placement, but the placement
    /// cannot wait for the geometry.
    /// </summary>
    public override void Stage(Scene scene)
    {
        scene.Background = Color.FromRgb(6, 7, 12);

        Place();

        scene.Lights.Clear();

        for (var i = 0; i < Lamps; i++)
        {
            _lights[i] = new PointLight
            {
                Position = _place[i],
                Color = _colour[i],
                Intensity = 0f,
                Range = 2.4f,
                Decay = 2f
            };

            scene.Lights.Add(_lights[i]);
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
    }

    public override Node BuildSubject()
    {
        var tree = new Node { Name = "tree" };

        var needles = new Material
        {
            BaseColor = new Vector4(0.055f, 0.150f, 0.070f, 1f),
            Roughness = 0.86f,
            Cull = CullMode.Back
        };

        foreach (var (bottom, top, bottomRadius, topRadius) in Tiers)
            tree.Children.Add(new MeshNode(
                Primitives.Cylinder(topRadius, bottomRadius, top - bottom, 40),
                needles)
            {
                Position = new Vector3(0f, (bottom + top) * 0.5f, 0f)
            });

        tree.Children.Add(new MeshNode(Primitives.Cylinder(0.16f, 0.19f, 0.66f, 20), new Material
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
        AddBox(tree, new Vector3(-1.35f, 0.32f, 0.95f), new Vector3(0.9f, 0.64f, 0.72f), 0.5f,
            new Vector4(0.34f, 0.10f, 0.12f, 1f));
        AddBox(tree, new Vector3(1.20f, 0.24f, 1.15f), new Vector3(0.72f, 0.48f, 0.60f), -0.7f,
            new Vector4(0.14f, 0.20f, 0.34f, 1f));
        AddBox(tree, new Vector3(0.45f, 0.20f, -1.55f), new Vector3(0.86f, 0.40f, 0.66f), 0.25f,
            new Vector4(0.28f, 0.26f, 0.12f, 1f));

        var glow = Space.Glow();
        var bulb = Primitives.Sphere(0.085f, 16, 10);

        for (var i = 0; i < Lamps; i++)
        {
            // Unlit: a bulb shaded like a stone would have a dark side, and a lamp with a dark side is a
            // contradiction. Nothing to do with the light count — it was the right material when four of
            // these were faking sixteen and it is the right material now that all sixteen are real.
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

            tree.Children.Add(_bulbs[i]);
            tree.Children.Add(_halos[i]);
        }

        // The star, which never goes out and never takes a slot.
        tree.Children.Add(new MeshNode(Primitives.Sphere(0.15f, 20, 12), new Material
        {
            BaseColor = new Vector4(1.00f, 0.93f, 0.62f, 1f),
            Unlit = true
        })
        {
            Position = new Vector3(0f, 3.44f, 0f)
        });

        tree.Children.Add(new SpriteNode
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

        return tree;
    }

    public override void Update(Scene scene, Camera camera, double elapsed)
    {
        var t = (float)elapsed % Cycle;
        var beat = t % Sequence;

        Array.Clear(_live);

        // Every sequence is the same operation — decide how brightly each of the sixteen burns — and the
        // last one is only interesting because it is allowed to answer "all of them".
        var share = 1f;

        if (t < Sequence)
        {
            // Chase: a head lamp and five behind it. The tail length is a matter of taste now; it used to
            // be exactly the number of slots left over after the head had taken one.
            var head = (int)MathF.Floor(beat * 3.6f);
            ReadOnlySpan<float> tail = [1f, 0.68f, 0.46f, 0.30f, 0.20f, 0.13f];

            for (var k = 0; k < tail.Length; k++)
                Burn(head - k, tail[k]);

            _caption = "Chase — one lamp and a six-lamp tail.";
        }
        else if (t < Sequence * 2f)
        {
            // Quarters: four lamps a quarter of the string apart, stepping together. Kept exactly as it
            // was, because it is the scene's own before-and-after.
            var phase = (int)MathF.Floor(beat * 2.2f);

            for (var k = 0; k < 4; k++)
                Burn(phase + k * (Lamps / 4), 1f);

            _caption = "Quarters — every fourth lamp, four separate pools at once.";
        }
        else if (t < Sequence * 3f)
        {
            // Twinkle: every lamp on its own period, deliberately not a common multiple of any other's.
            for (var i = 0; i < Lamps; i++)
            {
                var period = 1.4f + Hash(i * 131) * 1.9f;
                var step = MathF.Floor(beat / period + Hash(i * 977));

                Burn(i, MathF.Max(MathF.Sin((beat / period + Hash(i * 977) - step) * MathF.PI), 0f));
            }

            _caption = "Twinkle — sixteen lamps, sixteen periods, nothing in step.";
        }
        else
        {
            // All sixteen. A slow wave up the tree rather than a flat hold, so it reads as a string of
            // lights that are all on and not as one light that got bigger.
            for (var i = 0; i < Lamps; i++)
                Burn(i, 0.72f + 0.28f * MathF.Sin(beat * 1.6f - i * 0.55f));

            share = Share;
            _caption = "All — every lamp on the tree at once.";
        }

        for (var i = 0; i < Lamps; i++)
        {
            var live = _live[i];

            _lights[i].Intensity = Strength * share * live;

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
    /// Where the lamps go and what colour each one is. Called from <see cref="Stage"/>, before there is
    /// any geometry, because the lights are placed from this and the bulbs are only drawn on top of it.
    /// </summary>
    private void Place()
    {
        for (var i = 0; i < Lamps; i++)
        {
            // The golden angle, so sixteen lamps spread round the tree instead of stacking into a stripe.
            var angle = i * 2.39996f;
            var height = 0.78f + i / (Lamps - 1f) * 2.28f;
            var radius = TreeRadius(height) + 0.17f;

            _place[i] = new Vector3(MathF.Cos(angle) * radius, height, MathF.Sin(angle) * radius);
            _colour[i] = Palette[i % Palette.Length];
        }
    }

    /// <summary>
    /// Turns one lamp up. The whole of the scene's light management, which is the point: a lamp's light
    /// never moves and never changes colour, so the only thing a sequence does is decide how bright.
    /// </summary>
    private void Burn(int lamp, float strength)
    {
        lamp = ((lamp % Lamps) + Lamps) % Lamps;

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

    private static void AddBox(Node root, Vector3 at, Vector3 size, float turn, Vector4 color) =>
        root.Children.Add(new MeshNode(Primitives.Box(size.X, size.Y, size.Z), new Material
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
