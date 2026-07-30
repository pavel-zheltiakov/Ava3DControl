using System.Numerics;
using Avalonia.Media;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// A small orrery: bodies on nested nodes, moved every frame, to show what mutating a live scene costs.
/// </summary>
public sealed class AnimationScene : DemoScene
{
    private readonly record struct Body(Node Orbit, Node Spin, float OrbitSpeed, float SpinSpeed);

    private readonly List<Body> _bodies = [];

    public override string Title => "Animation";

    public override string Summary => "Mutate the scene each frame; one snapshot per UI frame";

    public override string Notes =>
        """
        Every body's node is moved once per frame and the scene is invalidated. There is no animation system
        here and there does not need to be one: the control coalesces however many changes arrive into a
        single immutable snapshot per UI frame, so a hundred small mutations cost one rebuild rather than a
        hundred.

        That coalescing is why this runs at the same frame rate as a static scene. The snapshot is what the
        render thread reads, so the UI thread is never blocked and the render thread never sees a half-updated
        scene graph.
        """;

    public override bool Animates => true;

    public override Scene Build()
    {
        var scene = new Scene { Background = Color.FromRgb(8, 9, 14) };
        _bodies.Clear();

        scene.Environment = new EnvironmentLight
        {
            SkyColor = new Vector3(0.10f, 0.12f, 0.18f),
            GroundColor = new Vector3(0.04f, 0.04f, 0.05f)
        };

        // The star. Emissive, so it reads as a light source rather than as a lit ball.
        scene.Children.Add(new MeshNode(Primitives.Sphere(0.5f), new Material
        {
            Name = "star",
            BaseColor = new Vector4(0.05f, 0.05f, 0.05f, 1f),
            EmissiveColor = new Vector3(2.4f, 1.7f, 0.7f),
            Roughness = 1f
        })
        {
            Name = "Star"
        });

        (float Radius, float Size, float OrbitSpeed, float SpinSpeed, Vector4 Color, float Metallic)[] planets =
        [
            (1.15f, 0.16f, 52f, 90f, new Vector4(0.72f, 0.55f, 0.42f, 1f), 0.1f),
            (1.75f, 0.24f, 34f, 60f, new Vector4(0.42f, 0.62f, 0.85f, 1f), 0f),
            (2.50f, 0.20f, 22f, 120f, new Vector4(0.85f, 0.45f, 0.32f, 1f), 0f),
            (3.35f, 0.34f, 14f, 40f, new Vector4(0.88f, 0.80f, 0.58f, 1f), 0.85f)
        ];

        foreach (var p in planets)
        {
            var orbit = new Node();
            scene.Children.Add(orbit);

            var spin = new Node { Position = new Vector3(p.Radius, 0f, 0f) };
            orbit.Children.Add(spin);

            spin.Children.Add(new MeshNode(Primitives.Sphere(p.Size), new Material
            {
                BaseColor = p.Color,
                Metallic = p.Metallic,
                Roughness = 0.42f
            }));

            // A moon on the outermost body, so the hierarchy is three deep and visibly so.
            if (p.Radius > 3f)
            {
                var moonOrbit = new Node();
                spin.Children.Add(moonOrbit);
                moonOrbit.Children.Add(new MeshNode(Primitives.Sphere(0.10f),
                    Material.FromColor(0.62f, 0.62f, 0.66f))
                {
                    Position = new Vector3(0.62f, 0f, 0f)
                });
                _bodies.Add(new Body(moonOrbit, moonOrbit, 190f, 0f));
            }

            _bodies.Add(new Body(orbit, spin, p.OrbitSpeed, p.SpinSpeed));
        }

        return scene;
    }

    public override void Update(Scene scene, double elapsed)
    {
        foreach (var body in _bodies)
        {
            body.Orbit.RotationDegrees = new Vector3(0f, (float)(elapsed * body.OrbitSpeed), 0f);

            if (body.SpinSpeed != 0f && !ReferenceEquals(body.Spin, body.Orbit))
                body.Spin.RotationDegrees = new Vector3(0f, (float)(elapsed * body.SpinSpeed), 0f);
        }
    }
}
