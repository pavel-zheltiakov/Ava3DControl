using System.Numerics;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// A three-deep node hierarchy, turning, so that inherited transforms are visible rather than asserted.
/// </summary>
public sealed class TransformsScene : DemoScene
{
    private Node? _hub;
    private readonly List<Node> _arms = [];

    public override string Title => "Transforms";

    public override string Summary => "Nested nodes inherit their parent's transform";

    public override string Notes =>
        """
        Position, Rotation and Scale on a Node compose with everything above it, so rotating the hub carries
        the arms and everything on them. Nothing here recomputes a world matrix by hand.

        This is also why a loaded CAD assembly stays one draw call per part: transforms live on nodes rather
        than being baked into vertices, so identical geometry can share a single Mesh and still be placed
        dozens of times.
        """;

    public override bool Animates => true;

    /// <summary>
    /// Nothing. There is no floor and no backdrop, and there never was — a hierarchy hanging in the dark is
    /// the whole picture, and a ground plane under it would only invite the question of what it is standing
    /// on. Which is also why this is the exhibit the rotunda bolts to a turning table: it has no opinion
    /// about what is underneath it, so a table can be.
    /// </summary>
    public override void Stage(Scene scene) { }

    public override Node BuildSubject()
    {
        _arms.Clear();

        var shaft = Primitives.Box(0.16f, 0.16f, 1.3f);
        var ball = Primitives.Sphere(0.22f);

        _hub = new Node { Name = "Hub" };

        _hub.Children.Add(new MeshNode(
            Primitives.Sphere(0.35f),
            new Material { BaseColor = new Vector4(0.8f, 0.8f, 0.84f, 1f), Metallic = 1f, Roughness = 0.3f }));

        for (var i = 0; i < 4; i++)
        {
            var arm = new Node
            {
                Name = $"Arm {i}",
                RotationDegrees = new Vector3(0f, i * 90f, 0f)
            };
            _hub.Children.Add(arm);
            _arms.Add(arm);

            // Child of the arm, so it inherits the arm's rotation and the hub's on top of that.
            arm.Children.Add(new MeshNode(shaft, Material.FromColor(0.35f, 0.37f, 0.42f))
            {
                Position = new Vector3(0f, 0f, 0.8f)
            });

            var tip = new Node { Position = new Vector3(0f, 0f, 1.5f) };
            arm.Children.Add(tip);

            tip.Children.Add(new MeshNode(ball, new Material
            {
                BaseColor = Hue(i),
                Roughness = 0.28f
            }));
        }

        return _hub;
    }

    public override void Update(Scene scene, double elapsed)
    {
        if (_hub == null)
            return;

        _hub.RotationDegrees = new Vector3(0f, (float)(elapsed * 26.0), 0f);

        for (var i = 0; i < _arms.Count; i++)
        {
            // Each arm also tilts on its own axis, so the two rotations visibly compose.
            var phase = (float)(elapsed * 55.0 + i * 90.0);
            _arms[i].RotationDegrees = new Vector3(MathF.Sin(phase * MathF.PI / 180f) * 28f, i * 90f, 0f);
        }
    }

    private static Vector4 Hue(int index) => index switch
    {
        0 => new Vector4(0.90f, 0.32f, 0.28f, 1f),
        1 => new Vector4(0.30f, 0.72f, 0.45f, 1f),
        2 => new Vector4(0.30f, 0.55f, 0.90f, 1f),
        _ => new Vector4(0.92f, 0.72f, 0.28f, 1f)
    };
}
