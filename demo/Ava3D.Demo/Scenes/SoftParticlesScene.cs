using System.Numerics;
using Avalonia.Media;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// The same glow twice: once with the card's own edge showing against the rock, once fading into it.
/// </summary>
public sealed class SoftParticlesScene : DemoScene
{
    public override string Title => "Soft particles";

    public override string Summary => "A billboard that meets a surface without a seam";

    public override string Notes =>
        """
        A sprite is a flat card, and where it approaches a solid the card's own edge shows. That edge is
        the one thing that says "this is a quad" about every glow, spark, plume and dust cloud in a scene.

        SpriteNode.SoftDistance removes it. The sprite reads the depth of what is already drawn and fades
        as it approaches it, over that many world units. Left is zero — the default, and what every
        billboard in this demo looked like before. Right is the same sprite, same size, same texture, with
        SoftDistance set to a little over the gap between the card and the rock behind it.

        Look at where each glow crosses its sphere. On the left it is as bright over the rock as it is
        against the background, so the card reads as a decal pinned in front of it. On the right it thins
        over the rock, recovers past the silhouette, and is at full strength against the background —
        which is what a volume of lit dust does. The spread is the point: a uniform gap would only dim
        the sprite, not shape it.

        The mechanism is why this is a feature row rather than a given. A shader cannot read the depth
        buffer while that buffer is attached and being tested against — that is a feedback loop, undefined
        in OpenGL, Vulkan and Metal alike, whatever a particular driver tolerates. So the depth is copied
        once, after the last opaque draw and before the first blended one, and the copy is what the sprite
        samples. Both depth formats have to match for that copy, which is why the framebuffer asks for a
        sized depth format rather than letting the driver choose one.

        Where the copy is not possible the sprite keeps its hard edge, which is the picture it had before.
        GL ES 2 and WebGL 1 cannot copy depth into a texture at all. On Metal and Vulkan the copy cannot
        happen inside a render pass, so it needs the frame split in two — real work, not done. The
        renderer panel says which of those applies here.
        """;

    public override bool FramesItself => true;

    public override void Frame(Camera camera)
    {
        camera.Target = Vector3.Zero;
        camera.Distance = 9f;
        camera.Yaw = 0f;
        camera.Pitch = 0.05f;
        camera.NearPlane = 0.5f;
        camera.FarPlane = 40f;
    }

    public override Node BuildSubject()
    {
        var pair = new Node { Name = "hard and soft" };
        var glow = Texture.Glow(160, 1.5f);

        // Unlit and dark. An additive glow over a surface that is already near white clamps to white
        // whether it faded or not, and then the difference between softened and not is exactly nothing —
        // which is a real way to build a scene that cannot show the thing it is for.
        var rock = new Material
        {
            Name = "rock",
            BaseColor = new Vector4(0.16f, 0.17f, 0.21f, 1f),
            Unlit = true
        };

        foreach (var (x, soft) in new[] { (-2.4f, 0f), (2.4f, 1.6f) })
        {
            pair.Children.Add(new MeshNode(Primitives.Sphere(1.2f, 48, 32), rock)
            {
                Position = new Vector3(x, 0f, -0.5f)
            });

            pair.Children.Add(new SpriteNode
            {
                Texture = glow,
                Position = new Vector3(x, 0f, 1.4f),
                Size = new Vector2(3.4f, 3.4f),
                Color = new Vector3(1.00f, 0.66f, 0.30f),
                Blend = BlendMode.Additive,
                DepthWrite = false,
                SoftDistance = soft,
                RenderOrder = 1
            });
        }

        return pair;
    }

    public override void Stage(Scene scene)
    {
        Staging.Neutral(scene, floor: false);
        scene.Background = Color.FromRgb(6, 7, 11);
    }
}
