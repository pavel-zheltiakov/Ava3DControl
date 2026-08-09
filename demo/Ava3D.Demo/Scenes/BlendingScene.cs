using System.Numerics;
using Avalonia.Media;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// The three blend modes over one bright bar, and a pair of panels whose <see cref="Node.RenderOrder"/>
/// swaps every three seconds.
/// </summary>
public sealed class BlendingScene : DemoScene
{
    private MeshNode _near = null!, _far = null!;
    private int _flip = -1;

    public override string Title => "Blending";

    public override string Summary => "Opaque, alpha and additive, and who draws first";

    public override string Notes =>
        """
        One bright bar runs behind all three panels. The first is Opaque and hides it. The second is
        Alpha at 0.45 and tints it. The third is Additive and adds to it — additive can only ever
        brighten, which is why it is what light is drawn with: glows, tracers, coronas.

        Both blended panels also set DepthWrite = false. A blended surface that writes depth occludes
        everything drawn after it, so two overlapping glows stop adding up and the nearer one punches a
        hole in the further one.

        Below, two half-transparent panels overlap at different distances, and their RenderOrder swaps
        every three seconds. Watch the blue one come to the front while staying further away. That is the
        rule: RenderOrder first, then opaque before blended, then back to front by the distance to each
        node's origin — per object, not per triangle. A scene that needs per-triangle sorting needs
        different geometry, not a different renderer.
        """;

    public override SceneLook Look => SceneLook.Blueprint;

    public override bool Animates => true;

    public override bool FramesItself => true;

    public override void Frame(Camera camera)
    {
        camera.Target = new Vector3(0f, 0.1f, 0f);
        camera.Distance = 9.5f;
        camera.Yaw = 0.05f;
        camera.Pitch = 0.04f;
        camera.NearPlane = 0.5f;
        camera.FarPlane = 40f;
    }

    /// <summary>
    /// A dark card, so an additive panel has somewhere to add to. Nothing else: a floor under transparent
    /// panels would be a second thing showing through them, and there is already a bright bar doing that
    /// on purpose.
    /// </summary>
    public override void Stage(Scene scene) => scene.Background = Color.FromRgb(8, 9, 14);

    public override Node BuildSubject()
    {
        var wall = new Node { Name = "blending" };

        // The thing all three panels are drawn over. Emissive so it is bright regardless of the light.
        wall.Children.Add(new MeshNode(Primitives.Box(10.4f, 1.7f, 0.2f), new Material
        {
            BaseColor = new Vector4(0.08f, 0.10f, 0.14f, 1f),
            EmissiveColor = new Vector3(0.30f, 0.42f, 0.62f)
        })
        {
            Position = new Vector3(0f, 1.5f, -1.2f)
        });

        wall.Children.Add(Panel(-3.4f, 1.5f, new Vector4(1.00f, 0.52f, 0.22f, 1.00f), BlendMode.Opaque));
        wall.Children.Add(Panel(0f, 1.5f, new Vector4(1.00f, 0.52f, 0.22f, 0.45f), BlendMode.Alpha));
        wall.Children.Add(Panel(3.4f, 1.5f, new Vector4(1.00f, 0.52f, 0.22f, 1.00f), BlendMode.Additive));

        // The pair that swaps. Different distances, so ordering by distance and ordering by RenderOrder
        // disagree — which is the only way to show which one wins.
        _far = Panel(-0.75f, -1.6f, new Vector4(0.30f, 0.55f, 1.00f, 0.62f), BlendMode.Alpha, z: -0.9f);
        _near = Panel(0.75f, -1.6f, new Vector4(1.00f, 0.34f, 0.62f, 0.62f), BlendMode.Alpha, z: 0.9f);

        wall.Children.Add(_far);
        wall.Children.Add(_near);

        return wall;

        MeshNode Panel(float x, float y, Vector4 color, BlendMode blend, float z = 0f) =>
            new(Primitives.Plane(2.6f, 2.6f), new Material
            {
                BaseColor = color,
                Blend = blend,
                // Opaque geometry writes depth; blended geometry must not.
                DepthWrite = blend == BlendMode.Opaque,
                Roughness = 0.6f,
                EmissiveColor = new Vector3(color.X, color.Y, color.Z) * 0.22f
            })
            {
                Position = new Vector3(x, y, z),
                RotationDegrees = new Vector3(90f, 0f, 0f)
            };
    }

    public override void Update(Scene scene, double elapsed)
    {
        // Flipped rather than eased: the point is that the swap is a single integer, not a fade.
        var phase = (int)(elapsed / 3.0) % 2;
        if (phase == _flip)
            return;

        _flip = phase;
        _far.RenderOrder = phase == 0 ? 0 : 1;
        _near.RenderOrder = 0;

        scene.Invalidate();
    }
}
