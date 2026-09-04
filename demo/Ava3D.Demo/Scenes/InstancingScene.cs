using System.Numerics;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// The same field of crates twice: as separate nodes on the left, as instances on the right.
///
/// One feature, and the picture is the comparison. Both halves are the same mesh, the same material and the
/// same eight hundred boxes; the left half is eight hundred nodes and eight hundred draw calls, the right is
/// one node and one. The renderer panel's draw count is the whole point of the scene, so it is worth opening
/// while the camera turns.
/// </summary>
public sealed class InstancingScene : DemoScene
{
    /// <summary>Copies per half. Large enough that the draw count is a difference nobody can argue with.</summary>
    private const int Count = 400;

    private const float Reach = 4.2f;

    public override string Title => "Instancing";

    public override string Summary => "800 crates, 401 draws instead of 800";

    public override string Notes =>
        """
        The same four hundred crates on the left and on the right, built two ways. On the left each is its own
        MeshNode, which is four hundred draw calls. On the right they are one MeshNode carrying four hundred
        MeshInstance values, which is one — the mesh is uploaded once and the per-copy transforms and tints go
        to the card as a second vertex buffer that advances once per instance rather than once per vertex.

        Read the draw count in the renderer panel. A draw call costs between two and ten thousand triangles on
        the backends here, measured on both the Metal and the OpenGL paths, so the two halves cost very
        different amounts to submit and look identical doing it.

        What an instance is not is a node. It has no name, no children, no visibility of its own, and nothing
        can pick it or light it individually — that is the trade, and it is the right one for a crate and the
        wrong one for a door. What it does have is a transform and a tint, which is what stops four hundred of
        a thing reading as one thing drawn four hundred times.

        The CPU renderer has no instanced draw and loops instead, so it costs the same either way and says so
        in the feature list. The picture is the same on all four.
        """;

    public override Node BuildSubject()
    {
        var root = new Node { Name = "instancing" };
        var mesh = Primitives.Box(0.34f, 0.34f, 0.34f);

        // One material for both halves, so the only difference between them is how the copies are expressed.
        var material = new Material
        {
            BaseColor = new Vector4(0.72f, 0.68f, 0.62f, 1f),
            Roughness = 0.55f,
            Metallic = 0.05f,
            Name = "crate"
        };

        var loose = new Node { Name = "as nodes", Position = new Vector3(-Reach, 0f, 0f) };
        foreach (var (at, turn, tint) in Field())
            loose.Children.Add(new MeshNode(mesh, Tinted(material, tint))
            {
                Position = at,
                Rotation = turn
            });

        root.Children.Add(loose);

        var instances = new List<MeshInstance>(Count);
        foreach (var (at, turn, tint) in Field())
            instances.Add(new MeshInstance
            {
                Transform = Matrix4x4.CreateFromQuaternion(turn) * Matrix4x4.CreateTranslation(at),
                Tint = tint
            });

        root.Children.Add(new MeshNode(mesh, material)
        {
            Name = "as instances",
            Position = new Vector3(Reach, 0f, 0f),
            Instances = [.. instances]
        });

        return root;
    }

    /// <summary>
    /// Where the crates go, how they are turned and what colour each is.
    ///
    /// Generated from a fixed seed and walked twice, so the two halves are the same field rather than two
    /// fields that look alike — which is what makes the comparison a comparison.
    /// </summary>
    private static IEnumerable<(Vector3 At, Quaternion Turn, Vector4 Tint)> Field()
    {
        var random = new Random(20260904);

        for (var i = 0; i < Count; i++)
        {
            var at = new Vector3(
                (float)(random.NextDouble() - 0.5) * 6.4f,
                (float)(random.NextDouble() - 0.5) * 5.2f,
                (float)(random.NextDouble() - 0.5) * 6.4f);

            var turn = Quaternion.CreateFromYawPitchRoll(
                (float)random.NextDouble() * MathF.Tau,
                (float)random.NextDouble() * 0.6f,
                (float)random.NextDouble() * 0.6f);

            // A narrow band rather than a rainbow: the point is that four hundred of one thing are not one
            // thing, and a band says that where a rainbow would say something else entirely.
            var warmth = 0.78f + (float)random.NextDouble() * 0.22f;
            var tint = new Vector4(warmth, 0.86f + (float)random.NextDouble() * 0.14f, warmth * 0.92f, 1f);

            yield return (at, turn, tint);
        }
    }

    /// <summary>
    /// A clone of the material with the tint multiplied into its base colour.
    ///
    /// What the left half has to do to say what a MeshInstance.Tint says on the right — and it is the reason
    /// the left half cannot be batched either: four hundred colours are four hundred materials.
    /// </summary>
    private static Material Tinted(Material material, Vector4 tint)
    {
        var clone = material.Clone();
        clone.BaseColor *= tint;
        return clone;
    }

    public override void Stage(Scene scene)
    {
        Staging.Neutral(scene);

        scene.Background = Avalonia.Media.Color.FromRgb(20, 22, 28);

        scene.Light.Direction = new Vector3(-0.4f, -0.72f, -0.56f);
        scene.Light.Intensity = 2.1f;
        scene.Light.Ambient = 0.10f;

        scene.Environment.SkyColor = new Vector3(0.36f, 0.42f, 0.54f);
        scene.Environment.GroundColor = new Vector3(0.14f, 0.12f, 0.11f);
    }

    public override void Frame(Camera camera)
    {
        camera.LookFrom(new Vector3(0f, 3.4f, 14.5f), Vector3.Zero);
        camera.FieldOfView = 52f;
    }
}
