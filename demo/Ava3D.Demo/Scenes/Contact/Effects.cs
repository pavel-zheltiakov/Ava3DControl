using System.Numerics;

namespace Ava3D.Demo.Scenes.Contact;

/// <summary>
/// The tracers and the explosions.
///
/// Almost everything here animates a transform, an opacity, a colour or a sprite's Size, because that is
/// the cheap way and it is enough: a tracer is a fixed segment on a node that moves, and a fireball is one
/// sprite that grows. A game would do it the same way, for the same reason.
///
/// The debris is the exception, and it is worth knowing why. Scaling a node moves every point at a speed
/// proportional to how far out it already was — which is one specific answer, the one where the cloud stays
/// exactly the shape it was made and only gets bigger. Every other answer needs the points to move
/// independently, and that is what <see cref="PointsNode.InvalidateGeometry"/> is for: the positions are
/// rewritten in place each frame and declared, which re-uploads one buffer and allocates nothing. Here it
/// buys a fragment's own speed and its own drag, so the light pieces outrun the heavy ones and the shell
/// goes ragged instead of staying a sphere.
///
/// Both pools are fixed size and hide their idle members with <see cref="Node.IsVisible"/>, which the
/// snapshot builder skips along with everything under them — so an unfired tracer costs one stack push and
/// nothing else, and a burst that is not burning costs nothing to simulate either.
/// </summary>
internal sealed class Effects
{
    private const float BoltLength = 120f;
    private const float BoltLife = 1.15f;

    /// <summary>World units a second. Public because gunnery has to lead a target by the flight time.</summary>
    public const float BoltSpeed = 4_600f;

    private sealed class Bolt
    {
        public required Node Root { get; init; }
        public required LineNode Trail { get; init; }
        public required SpriteNode Head { get; init; }

        public Vector3 Position;
        public Vector3 Direction;
        public float Age;
        public bool Active;
    }

    private sealed class Burst
    {
        public required Node Root { get; init; }
        public required SpriteNode Fire { get; init; }
        public required PointsNode Debris { get; init; }
        public required LineNode Shockwave { get; init; }

        /// <summary>Where each fragment starts, in units of the burst's scale: a small shell.</summary>
        public required Vector3[] Seed { get; init; }

        /// <summary>
        /// Which way each fragment goes and how fast, as a unit direction times a 0.6..1 multiplier.
        ///
        /// The direction comes from the fragment's place in the shell and the speed does not, which is the
        /// whole point — a scale can only give a speed proportional to the starting radius, so decorrelating
        /// the two is what makes this look like an explosion rather than an expanding sphere.
        /// </summary>
        public required Vector3[] Throw { get; init; }

        /// <summary>Per-fragment multiplier on the drag: how quickly this one gives up.</summary>
        public required float[] Drag { get; init; }

        public Vector3 Position;
        public float Scale;
        public float Age;
        public bool Active;

        /// <summary>A hit, not a kill: shorter, no shockwave, and it does not throw much.</summary>
        public bool Spark;
    }

    private readonly Bolt[] _bolts;
    private readonly Burst[] _bursts;
    private int _nextBolt, _nextBurst;

    /// <summary>Where the newest live effect is, for the point light to sit on. Null when nothing is firing.</summary>
    public Vector3? Flashpoint { get; private set; }

    /// <summary>How bright that flash is, 0..1 — an explosion outshines a tracer by a long way.</summary>
    public float FlashStrength { get; private set; }

    /// <summary>The colour of whatever is flashing.</summary>
    public Vector3 FlashColor { get; private set; } = Vector3.One;

    public Effects(Node world, Texture glow, int bolts = 30, int bursts = 8)
    {
        _bolts = new Bolt[bolts];
        for (var i = 0; i < bolts; i++)
        {
            var root = new Node { IsVisible = false };
            world.Children.Add(root);

            // The segment runs from the node's origin forward along its own −Z, which is the axis
            // Flight.Orient points a direction down. One array, uploaded once, shared by every bolt.
            var trail = new LineNode
            {
                Positions = [Vector3.Zero, new Vector3(0f, 0f, -BoltLength)],
                Color = new Vector3(1.0f, 0.85f, 0.45f),
                Opacity = 0.95f,
                Blend = BlendMode.Additive,
                DepthWrite = false,
                RenderOrder = 4
            };

            var head = new SpriteNode
            {
                Texture = glow,
                Position = new Vector3(0f, 0f, -BoltLength),
                Color = new Vector3(1.0f, 0.9f, 0.6f),
                Size = new Vector2(52f),
                Opacity = 0.9f,
                Blend = BlendMode.Additive,
                DepthWrite = false,
                RenderOrder = 4
            };

            root.Children.Add(trail);
            root.Children.Add(head);

            _bolts[i] = new Bolt { Root = root, Trail = trail, Head = head };
        }

        _bursts = new Burst[bursts];
        for (var i = 0; i < bursts; i++)
        {
            var root = new Node { IsVisible = false };
            world.Children.Add(root);

            var fire = new SpriteNode
            {
                Texture = glow,
                Color = new Vector3(1.00f, 0.72f, 0.34f),
                Size = new Vector2(1f),
                Blend = BlendMode.Additive,
                DepthWrite = false,
                RenderOrder = 5
            };

            // The debris starts as a cloud inside a unit sphere and is stepped outward fragment by
            // fragment. A ship coming apart is the one moment worth spending particles on, and 220 of them
            // is what turns "a hull with a flash on it" into a hull that is not there any more.
            var seed = new Vector3[220];
            var scatter = new Vector3[seed.Length];
            var drag = new float[seed.Length];

            for (var f = 0; f < seed.Length; f++)
            {
                // Where the fragment starts, and where it goes: outward from where it started, because
                // a piece off the port side leaving to starboard is a piece that was never attached.
                seed[f] = Scatter.Point(f, 40 + i);

                var direction = seed[f].LengthSquared() > 1e-6f ? Vector3.Normalize(seed[f]) : Vector3.UnitY;
                scatter[f] = direction * (0.60f + 0.40f * Scatter.Value(f, 300 + i));
                drag[f] = 0.70f + 0.55f * Scatter.Value(f, 600 + i);
            }

            var debris = new PointsNode
            {
                // Its own array, not shared with the other bursts: this one gets written into every frame
                // it is alight, and two explosions do not run to the same clock.
                Positions = new Vector3[seed.Length],
                Color = new Vector3(1.00f, 0.78f, 0.46f),
                Size = 3.2f,
                SizeAttenuation = false,
                Blend = BlendMode.Additive,
                DepthWrite = false,
                RenderOrder = 5
            };

            var shockwave = new LineNode
            {
                Positions = Fleet.Ring(48),
                Color = new Vector3(1.00f, 0.86f, 0.62f),
                Blend = BlendMode.Additive,
                DepthWrite = false,
                RenderOrder = 5
            };

            root.Children.Add(fire);
            root.Children.Add(debris);
            root.Children.Add(shockwave);

            _bursts[i] = new Burst
            {
                Root = root,
                Fire = fire,
                Debris = debris,
                Shockwave = shockwave,
                Seed = seed,
                Throw = scatter,
                Drag = drag
            };
        }
    }

    /// <summary>
    /// Fires a tracer from <paramref name="origin"/> along <paramref name="direction"/>.
    ///
    /// A direction and not a target, because a gun is bolted to a hull: it goes where the ship was
    /// pointing when the trigger came down, and after that nothing steers it. Whether it hits is
    /// decided before this is called, by <see cref="Ship.CanHit"/>, and it is decided by geometry.
    /// </summary>
    public void Fire(Vector3 origin, Vector3 direction, Vector3 color)
    {
        var bolt = _bolts[_nextBolt];
        _nextBolt = (_nextBolt + 1) % _bolts.Length;

        if (direction.LengthSquared() < 1e-6f)
            return;

        direction = Vector3.Normalize(direction);

        bolt.Position = origin;
        bolt.Direction = direction;
        bolt.Age = 0f;
        bolt.Active = true;

        bolt.Trail.Color = color;
        bolt.Head.Color = color;

        bolt.Root.IsVisible = true;
        bolt.Root.Position = origin;
        // Oriented once, at the moment it is fired. A bolt does not steer.
        bolt.Root.Rotation = Flight.Orient(direction, 0f);
    }

    /// <summary>Detonates at <paramref name="position"/>. <paramref name="scale"/> is the final radius.</summary>
    public void Detonate(Vector3 position, float scale) => Burn(position, scale, spark: false);

    /// <summary>
    /// A hit rather than a kill: a small, brief flare with no shockwave behind it.
    ///
    /// Bolts here are geometry that travels, not rays that resolve, so nothing in the scene knows the
    /// moment one arrives. What does know is the gunnery — a shooter with a solution is a shooter whose
    /// bolts are going to land — so a spark is put where the target is a bolt's flight time later. It is
    /// the difference between a fight where light goes past ships and one where light lands on them.
    /// </summary>
    public void Spark(Vector3 position, float scale) => Burn(position, scale, spark: true);

    private void Burn(Vector3 position, float scale, bool spark)
    {
        var burst = _bursts[_nextBurst];
        _nextBurst = (_nextBurst + 1) % _bursts.Length;

        burst.Position = position;
        burst.Scale = scale;
        burst.Age = 0f;
        burst.Active = true;
        burst.Spark = spark;

        burst.Root.IsVisible = true;
        burst.Root.Position = position;
        burst.Fire.Position = Vector3.Zero;
        burst.Shockwave.IsVisible = !spark;

        // Back to the shell it starts from. A pool member that is fired again while its last cloud is
        // still spread out would otherwise flash the old one for the frame before the first step.
        Fling(burst);
    }

    /// <summary>
    /// Puts every fragment where its own speed and its own drag say it should be at the burst's age.
    ///
    /// Solved rather than accumulated: a fragment thrown at <c>v</c> against a drag of <c>k</c> has gone
    /// <c>v(1 − e^−kt) / k</c>, so the position comes from the age alone. That makes the cloud independent
    /// of frame rate and of how many frames it has been alive for, which is what stops a burst looking
    /// different on the Skia backend at 25 fps than it does on Metal at 120.
    /// </summary>
    private static void Fling(Burst burst)
    {
        // Tuned so the average fragment ends up about where the old single-scale cloud did: peak speed
        // over drag, times the scale, is roughly four radii for a kill and under one for a spark.
        var speed = burst.Spark ? 2.10f : 3.60f;
        var pull = burst.Spark ? 3.20f : 0.90f;

        var positions = burst.Debris.Positions;
        var start = burst.Scale * 0.2f;

        for (var i = 0; i < positions.Length; i++)
        {
            var drag = pull * burst.Drag[i];
            var travel = (1f - MathF.Exp(-burst.Age * drag)) / drag;
            positions[i] = burst.Seed[i] * start + burst.Throw[i] * (speed * burst.Scale * travel);
        }

        burst.Debris.InvalidateGeometry();
    }

    /// <summary>Advances everything and reports where the brightest thing is.</summary>
    public void Update(float dt, Vector3 cameraPosition)
    {
        Flashpoint = null;
        FlashStrength = 0f;

        foreach (var bolt in _bolts)
        {
            if (!bolt.Active)
                continue;

            bolt.Age += dt;
            if (bolt.Age >= BoltLife)
            {
                bolt.Active = false;
                bolt.Root.IsVisible = false;
                continue;
            }

            bolt.Position += bolt.Direction * BoltSpeed * dt;
            bolt.Root.Position = bolt.Position;

            // Fades over its last third, so a tracer runs out rather than blinking out.
            var fade = MathF.Min(1f, (BoltLife - bolt.Age) * 3f / BoltLife);
            bolt.Trail.Opacity = 0.95f * fade;
            bolt.Head.Opacity = 0.9f * fade;

            if (FlashStrength < 0.35f * fade)
            {
                Flashpoint = bolt.Position;
                FlashStrength = 0.35f * fade;
                FlashColor = bolt.Trail.Color;
            }
        }

        foreach (var burst in _bursts)
        {
            if (!burst.Active)
                continue;

            var life = burst.Spark ? 0.9f : 4.6f;

            burst.Age += dt;
            if (burst.Age >= life)
            {
                burst.Active = false;
                burst.Root.IsVisible = false;
                continue;
            }

            // The fireball: up fast, down slow, which is what an explosion looks like and what a
            // symmetric curve never does.
            var fire = MathF.Min(burst.Age / (burst.Spark ? 0.05f : 0.18f), 1f);
            var fireFade = MathF.Max(0f, 1f - burst.Age / (burst.Spark ? 0.55f : 1.9f));
            // A hit is a flash on a hull, not a small explosion: it stays inside the ship it lands on,
            // which is what stops a busy engagement turning into a screen of white blobs. Additive on
            // additive saturates fast, and a spark drawn at kill scale is the fastest way there.
            burst.Fire.Size = new Vector2(burst.Scale * (burst.Spark ? 0.30f + 0.34f * fire
                                                                    : 0.25f + 0.95f * fire));
            burst.Fire.Opacity = fireFade * fireFade;
            burst.Fire.IsVisible = fireFade > 0f;

            // The debris: every fragment stepped on its own, then faded together. Slowing, because the
            // fast pieces get ahead early and the picture wants them to spread rather than to keep
            // accelerating — and slowing by different amounts, which is what a single Scale could not do.
            Fling(burst);
            burst.Debris.Opacity = MathF.Max(0f, 1f - burst.Age / life);

            // The shockwave: a ring, billboarded at the camera, gone in a second.
            var wave = MathF.Min(burst.Age / 0.9f, 1f);
            burst.Shockwave.IsVisible = !burst.Spark && wave < 1f;
            if (wave < 1f)
            {
                burst.Shockwave.Scale = new Vector3(burst.Scale * (0.2f + 2.0f * wave));
                burst.Shockwave.Opacity = (1f - wave) * 0.85f;

                // The ring is authored in the XY plane, so its normal is +Z and pointing that at the
                // camera means orienting −Z away from it.
                var toCamera = cameraPosition - burst.Position;
                if (toCamera.LengthSquared() > 1e-3f)
                    burst.Shockwave.Rotation = Flight.Orient(-toCamera, 0f);
            }

            var strength = MathF.Max(0f, 1f - burst.Age / 0.55f);
            if (strength > FlashStrength)
            {
                Flashpoint = burst.Position;
                FlashStrength = strength;
                FlashColor = new Vector3(1.00f, 0.68f, 0.34f);
            }
        }
    }

    /// <summary>Puts every pool member back in its box, so the loop starts from nothing in flight.</summary>
    public void Reset()
    {
        foreach (var bolt in _bolts)
        {
            bolt.Active = false;
            bolt.Root.IsVisible = false;
        }

        foreach (var burst in _bursts)
        {
            burst.Active = false;
            burst.Root.IsVisible = false;
        }

        _nextBolt = 0;
        _nextBurst = 0;
        Flashpoint = null;
        FlashStrength = 0f;
    }
}
