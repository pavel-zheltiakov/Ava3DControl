using System.Numerics;
using Avalonia.Media;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// One mesh, one material, three shades — the case that used to be three meshes and three draw calls.
/// </summary>
public sealed class VertexColorScene : DemoScene
{
    public override string Title => "Vertex colours";

    public override string Summary => "Bands, gradients and fades without splitting the mesh";

    public override string Notes =>
        """
        Mesh.Colors is glTF's COLOR_0 and means what glTF says: a tint on the base colour and its map,
        not a replacement. White is the identity, and a mesh without colours is uploaded white, so the
        shader multiplies unconditionally and nothing changes until the values do.

        Left: banded by height into three flat shades. With the colour on the material this is three
        materials, three MeshNodes and three draw calls; with it here it is one of each. That is the
        arithmetic behind cel shading done in geometry, and it is why a game doing it that way ends up
        with a mesh-splitting pipeline in its scene builder.

        Middle: a continuous gradient, which the material route cannot express at all without a texture
        and the UV coordinates to sample it at.

        Right: alpha, fading from opaque at the base to nothing at the tip. This is the other half of a
        plume: the filaments scroll with a UV offset while the fade down the length stays put, and
        neither can do it alone.

        Four bytes a vertex, not four floats — RGBA8, read back as a normalised vec4. Twelve floats a
        vertex became thirteen, which is eight per cent; a Vector4 would have been sixteen and a third,
        paid by every mesh whether it carries colours or not.
        """;

    public override bool FramesItself => true;

    /// <summary>Framed on the row itself: the staging floor is context, not the subject.</summary>
    public override void Frame(Camera camera)
    {
        camera.Target = new Vector3(0f, 0.0f, 0f);
        camera.Distance = 7.2f;
        camera.Yaw = 0f;
        camera.Pitch = 0.16f;
        camera.NearPlane = 0.3f;
        camera.FarPlane = 60f;
    }

    public override Node BuildSubject()
    {
        var row = new Node { Name = "vertex colours" };

        row.Children.Add(new MeshNode(Tinted(Banded), new Material { Name = "banded", Roughness = 0.5f })
        {
            Position = new Vector3(-2.3f, 0f, 0f)
        });

        row.Children.Add(new MeshNode(Tinted(Gradient), new Material { Name = "gradient", Roughness = 0.5f }));

        // <b>Back faces off, and it is the fix for the one thing that made this look broken.</b> A
        // transparent solid with no culling draws its far side as well as its near one, in whatever order
        // the index buffer happens to be in — which on a sphere is a set of horizontal bands where the
        // back hemisphere wins the blend. There is no per-triangle sort in this renderer and there should
        // not be one; a closed transparent solid wants its back faces dropped, which is exact and free.
        row.Children.Add(new MeshNode(Tinted(Fade), new Material
        {
            Name = "faded",
            Roughness = 0.5f,
            Blend = BlendMode.Alpha,
            Cull = CullMode.Back,
            DepthWrite = false
        })
        {
            Position = new Vector3(2.3f, 0f, 0f)
        });

        return row;

        static Mesh Tinted(Func<float, Vector4> shade)
        {
            var mesh = Primitives.Sphere(0.95f, 48, 32);
            var colors = new Vector4[mesh.Positions.Length];

            for (var i = 0; i < colors.Length; i++)
                colors[i] = shade(mesh.Positions[i].Y / 0.95f * 0.5f + 0.5f);

            return mesh.WithColors(colors);
        }

        // Three flat steps: the light, base and dark a cel-shaded hull is painted in.
        static Vector4 Banded(float t) => t > 0.66f
            ? new Vector4(0.95f, 0.80f, 0.45f, 1f)
            : t > 0.33f
                ? new Vector4(0.72f, 0.52f, 0.28f, 1f)
                : new Vector4(0.42f, 0.28f, 0.18f, 1f);

        static Vector4 Gradient(float t) =>
            new(0.25f + t * 0.70f, 0.35f + t * 0.35f, 0.85f - t * 0.45f, 1f);

        // Opaque at the foot and gone at the crown, which is the way round a plume goes and the way round
        // the notes have always described it. It was written the other way and nobody caught it, because a
        // sphere fading upwards and a sphere fading downwards are the same picture until you read the
        // sentence under them.
        static Vector4 Fade(float t) => new(0.55f, 0.80f, 1.00f, (1f - t) * (1f - t));
    }

    public override void Stage(Scene scene)
    {
        Staging.Neutral(scene);
        scene.Background = Color.FromRgb(12, 14, 20);

        // Under half the standard key, and this is the one scene in the set that needs it. Every tint
        // here multiplies a white base colour, so the whole exhibit lives in the top of the range: at the
        // stage's own 2.6 the lit half of all three spheres clips to white and the three flat shades
        // become one. Turning the light down is the only fix that does not change what is being shown —
        // darkening the tints would be answering the question with the answer.
        scene.Light.Intensity = 1.2f;
    }
}
