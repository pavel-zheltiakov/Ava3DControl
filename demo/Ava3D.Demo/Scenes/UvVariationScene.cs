using System.Numerics;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// Forty crates, one material, one mesh — and every one of them different.
///
/// The variation that used to cost either a draw call each or a copy of the geometry each. A material is
/// shared by every node wearing it, so breaking up a repeat meant cloning the material — which costs a
/// draw, because draws are grouped by material — or rewriting the mesh's own coordinates, which costs a
/// copy of the vertices. This scene does neither.
/// </summary>
public sealed class UvVariationScene : DemoScene
{
    private const int Across = 8;
    private const int Deep = 5;
    private const float Spacing = 1.15f;

    public override string Title => "Uv variation";

    public override string Summary => "forty crates, one material, forty phases";

    public override string Notes =>
        """
        MeshNode.UvScale, UvOffset and UvRotation are the material's three, at node scope, composed with
        the material's as one matrix rather than applied after them. So forty crates can wear forty
        different corners of one texture while sharing one material and one mesh.

        The left half slides each crate's coordinates by a different amount. The right half also turns them
        by a quarter, a half or three quarters — and the difference between the two halves is the point. A
        phase offset moves a pattern the eye has already learned; it is still the same pattern. A
        quarter-turn is what makes two of a kind stop being two of a kind, which is why the consumer that
        asked for this spends a hundred and seventy lines of vertex rewriting to get one.

        Two things in the arithmetic are worth knowing. The rotation turns about UvPivot, which defaults to
        the middle of the tile rather than the origin — glTF's own extension rotates about the origin, which
        sends an atlas frame off the sheet unless the author precomputes a compensating offset. And the
        sampled normal turns with the coordinate: a normal map's x and y are a gradient in texture space, so
        a rotated coordinate needs the gradient rotated too, or the surface lights from a direction
        unrelated to how it was painted. Every mainstream engine with a UV rotation leaves that out, and it
        reads as a lighting fault rather than a texturing one.

        What this costs, stated plainly, because the honest version is more useful than the flattering one.
        A node's texture transform is a per-draw value, so forty crates with forty phases are forty draws —
        the same forty they were. What it saves is the two alternatives: forty cloned materials, which is
        forty draws *and* forty materials to keep, or forty rewritten copies of the mesh, which is forty
        copies of the vertex data and nothing left that can be moved or hidden.

        For variation that costs no draws at all, the answer is the Instancing scene next door, or a
        world-space projection — under which two identical crates at two positions read different parts of
        the texture for free, because their positions differ.
        """;

    public override Node BuildSubject()
    {
        var root = new Node { Name = "variation" };

        var crate = Primitives.Box(0.8f, 0.8f, 0.8f, 0.05f);
        var sheet = Textures.Procedural.UvGrid(512);

        var material = new Material
        {
            BaseColorTexture = sheet,
            BaseColor = new Vector4(0.9f, 0.88f, 0.84f, 1f),
            Roughness = 0.45f,
            Metallic = 0.05f,
            Name = "crate"
        };

        var random = new Random(20260904);

        for (var half = 0; half < 2; half++)
        {
            var turning = half == 1;
            var group = new Node
            {
                Name = turning ? "phase and a quarter turn" : "phase only",
                Position = new Vector3(turning ? Across * Spacing * 0.5f + 0.9f : -(Across * Spacing * 0.5f + 0.9f), 0f, 0f)
            };

            for (var z = 0; z < Deep; z++)
            for (var x = 0; x < Across / 2; x++)
            {
                group.Children.Add(new MeshNode(crate, material)
                {
                    Position = new Vector3(
                        (x - (Across / 2 - 1) * 0.5f) * Spacing,
                        0f,
                        (z - (Deep - 1) * 0.5f) * Spacing),

                    // The whole of the variation: a phase, and on the right half a quarter turn as well.
                    UvOffset = new Vector2((float)random.NextDouble(), (float)random.NextDouble()),
                    UvRotation = turning ? random.Next(4) * MathF.PI * 0.5f : 0f
                });
            }

            root.Children.Add(group);
        }

        return root;
    }

    public override void Stage(Scene scene)
    {
        Staging.Neutral(scene);

        scene.Background = Avalonia.Media.Color.FromRgb(20, 22, 27);
        scene.Light.Direction = new Vector3(-0.4f, -0.75f, -0.52f);
        scene.Light.Intensity = 2.1f;
        scene.Light.Ambient = 0.1f;

        scene.Environment.SkyColor = new Vector3(0.38f, 0.44f, 0.55f);
        scene.Environment.GroundColor = new Vector3(0.14f, 0.13f, 0.11f);
    }

    public override void Frame(Camera camera)
    {
        camera.LookFrom(new Vector3(0f, 4.2f, 7.6f), Vector3.Zero);
        camera.FieldOfView = 50f;
    }
}
