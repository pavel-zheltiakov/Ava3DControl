using System.Numerics;
using Avalonia.Media;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// A torch in a room: the one light whose shadow map is its own aperture.
/// </summary>
public sealed class SpotlightScene : DemoScene
{
    private SpotLight _torch = null!;
    private Node _housing = null!;

    public override string Title => "Spotlight";

    public override string Summary => "A cone with a soft edge, casting through its own frustum";

    public override string Notes =>
        """
        A lamp on a stand, sweeping a room with two blocks in it. One light, and it is the only kind in
        this library that knows where it is pointing.

        A SpotLight has a position, a direction and two angles. Everything inside InnerConeDegrees gets
        the full beam, everything outside OuterConeDegrees gets nothing, and between them the falloff is
        smooth — the glTF KHR_lights_punctual shape, so a light tuned in Blender or three.js arrives
        meaning the same thing. Both angles are half-angles from the axis: a 35° outer cone is a 70° beam.
        Range and Decay work exactly as they do on a PointLight.

        The reason to reach for one is the shadow rather than the beam. This library draws one shadow map,
        and a point light casts in every direction — so its map has to be a cone aimed by guesswork at the
        middle of whatever casts, and geometry outside that cone is lit as though nothing blocked it. That
        guess is a fixed point in the world, so carrying a point light across a room swings its cone
        through that point and every shadow in view turns at once, for a reason nothing on screen explains.

        A spot has no such problem, because the cone is not a compromise imposed on it. Its shadow frustum
        is its own aperture, aimed along its own axis, at the resolution its own angle implies. Watch the
        blocks: the shadows stay attached to them as the beam sweeps, and the edge of the beam and the
        edge of the shadowed region are the same edge.

        RenderInfo.ShadowSummary says which of these you got, and whether the fit had to guess.
        """;

    public override SceneLook Look => SceneLook.Plain;

    public override bool Animates => true;

    public override bool FramesItself => true;

    public override void Frame(Camera camera)
    {
        camera.Target = new Vector3(0f, 0.7f, 0f);
        camera.Distance = 9.5f;
        camera.Yaw = 0.55f;
        camera.Pitch = 0.42f;
        camera.NearPlane = 0.4f;
        camera.FarPlane = 40f;
    }

    /// <summary>
    /// A dark room and one light. The hemisphere is turned right down rather than off, because a beam is
    /// only readable against something, and the something has to be far dimmer than the beam.
    /// </summary>
    public override void Stage(Scene scene)
    {
        scene.Background = Color.FromRgb(6, 7, 10);
        scene.Environment.SkyColor = new Vector3(0.05f, 0.06f, 0.08f);
        scene.Environment.GroundColor = new Vector3(0.02f, 0.02f, 0.03f);
        scene.Environment.Intensity = 0.30f;
        scene.Exposure = 1.1f;

        scene.Lights.Clear();

        _torch = new SpotLight
        {
            Position = new Vector3(-2.9f, 3.4f, 2.9f),
            Direction = Vector3.Normalize(new Vector3(0.62f, -0.72f, -0.62f)),
            Color = new Vector3(1f, 0.94f, 0.82f),
            Intensity = 52f,
            Range = 14f,
            Decay = 2f,
            InnerConeDegrees = 14f,
            OuterConeDegrees = 22f,
            CastsShadows = true
        };

        scene.Lights.Add(_torch);

        // A floor of light so the room is not black where the beam is not, and no shadow from it — one
        // map, one casting light, and the beam is the one worth spending it on.
        scene.Lights.Add(new DirectionalLight
        {
            Direction = new Vector3(-0.3f, -1f, -0.4f),
            Intensity = 0.05f,
            Ambient = 0f
        });

        scene.ShadowMapSize = 2048;
        scene.ShadowStrength = 0.92f;
    }

    public override Node BuildSubject()
    {
        var room = new Node { Name = "room" };

        room.Children.Add(new MeshNode(
            Primitives.Plane(16f, 16f, 4, 4),
            new Material { Name = "floor", BaseColor = new Vector4(0.62f, 0.60f, 0.57f, 1f), Roughness = 0.85f })
        {
            Name = "floor"
        });

        // Two blocks, at different distances from the axis, so the beam reaches one before the other and
        // the shadows are different lengths.
        room.Children.Add(Block(new Vector3(-0.80f, 0f, 0.45f), 1.1f, "near block"));
        room.Children.Add(Block(new Vector3(0.85f, 0f, -0.55f), 1.7f, "far block"));

        // The lamp itself, so the light has something visible to come out of. The bulb is unlit, because
        // a light source that is shaded reads as a pale ball rather than as a source.
        _housing = new Node { Name = "lamp", Position = new Vector3(-2.9f, 3.4f, 2.9f) };

        _housing.Children.Add(new MeshNode(
            Primitives.Cylinder(0.10f, 0.34f, 0.42f, 24),
            new Material { Name = "shade", BaseColor = new Vector4(0.20f, 0.20f, 0.22f, 1f), Metallic = 0.9f, Roughness = 0.35f })
        {
            Name = "shade",
            Position = new Vector3(0f, 0.18f, 0f)
        });

        _housing.Children.Add(new MeshNode(
            Primitives.Sphere(0.12f, 20, 14),
            new Material { Name = "bulb", BaseColor = new Vector4(1f, 0.95f, 0.85f, 1f), Unlit = true })
        {
            Name = "bulb",
            CastsShadow = false
        });

        room.Children.Add(_housing);

        return room;
    }

    private static MeshNode Block(Vector3 at, float height, string name) =>
        new(Primitives.Box(0.8f, height, 0.8f),
            new Material { Name = name, BaseColor = new Vector4(0.72f, 0.55f, 0.42f, 1f), Roughness = 0.7f })
        {
            Name = name,
            Position = at with { Y = height * 0.5f }
        };

    public override void Update(Scene scene, double elapsed)
    {
        // A slow sweep, and the cone opening and closing under it, so both halves of the light are visible
        // in one pass: where the beam points, and how wide it is.
        var t = (float)elapsed;

        // The beam sweeps across the pair rather than round the room, so both blocks pass through it and
        // their shadows stretch away from the lamp and across the lit floor, which is where a shadow is
        // readable. Aimed at a moving point on the floor rather than turned by an angle, because what a
        // lamp is pointed at is the thing anybody tuning this is thinking about.
        var at = new Vector3(MathF.Sin(t * 0.42f) * 1.35f, 0f, MathF.Cos(t * 0.31f) * 0.7f);

        _torch.Direction = Vector3.Normalize(at - _torch.Position);

        _torch.OuterConeDegrees = 20f + MathF.Sin(t * 0.33f) * 6f;
        _torch.InnerConeDegrees = _torch.OuterConeDegrees * 0.55f;

        // The housing turns with the beam, so the light is coming out of the thing that is pointing.
        var axis = Vector3.Cross(-Vector3.UnitY, _torch.Direction);
        _housing.Rotation = axis.LengthSquared() > 1e-8f
            ? Quaternion.CreateFromAxisAngle(
                Vector3.Normalize(axis),
                MathF.Acos(Math.Clamp(Vector3.Dot(-Vector3.UnitY, Vector3.Normalize(_torch.Direction)), -1f, 1f)))
            : Quaternion.Identity;

        scene.Invalidate();
    }
}
