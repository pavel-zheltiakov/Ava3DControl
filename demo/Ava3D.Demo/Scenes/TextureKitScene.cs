using System.Numerics;
using Avalonia.Media;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// The two textures the library builds so a scene does not have to: a radial glow and tileable noise.
/// </summary>
public sealed class TextureKitScene : DemoScene
{
    public override string Title => "Texture kit";

    public override string Summary => "Texture.Glow and Texture.Noise, and what they are for";

    public override string Notes =>
        """
        Texture.Glow is a radial falloff, opaque and white at the centre and gone at the rim. Three
        sprites here share one image and differ only in colour, opacity and size — which is the point of
        it being white rather than tinted: a warm lamp, a cold beacon and a wide corona are one upload
        and one texture binding between them.

        The shape lives entirely in the alpha channel, and that is what makes it correct under both blend
        modes. An additive draw ignores alpha as coverage and multiplies by it as intensity, so a flat
        white image gives the same falloff either way. The falloff exponent decides whether it reads as a
        light or as a disc: the row runs 1, 2 and 4.

        Texture.Noise is tileable fractal value noise. On the floor it is a base-colour map at four
        octaves; the sphere behind uses it as a bump map, which is the same field read as height.

        Tileable by construction rather than by mirroring — the lattice wraps at each octave's period, so
        the left edge is the right edge exactly. Mirroring is cheaper and it is visible, because a
        mirrored field has an axis of symmetry through every tile boundary.

        Two builders and not twenty, deliberately. This is not a procedural texture library: checkers,
        bricks and grass belong wherever is drawing them. These two are here because they are about
        lighting and about surface.
        """;

    public override bool FramesItself => true;

    /// <summary>Framed on the row itself: the staging floor is context, not the subject.</summary>
    public override void Frame(Camera camera)
    {
        camera.Target = new Vector3(0f, -0.2f, 0f);
        camera.Distance = 9.0f;
        camera.Yaw = 0f;
        camera.Pitch = 0.16f;
        camera.NearPlane = 0.3f;
        camera.FarPlane = 60f;
    }

    public override Node BuildSubject()
    {
        var kit = new Node { Name = "texture kit" };
        // Five hundred and twelve at six octaves, and it was two hundred and fifty-six at four. This
        // field is stretched over nine metres of floor and read again as height on a sphere a metre wide
        // at two metres — so its finest octave was one part in a thousand of the floor and one part in
        // sixteen of the ball, which is a cloud with nothing in it smaller than a hand. What reads as
        // surface is the smallest structure.
        var noise = Texture.Noise(512, octaves: 6, seed: 3);

        // Primitives.Plane already lies in XZ, so a floor is a plane with no rotation on it at all. It
        // used to carry −90° about X, which stands a plane up: the noise was a nine-metre backdrop
        // through the middle of the scene with the staging floor cutting across it and the three glows
        // half buried in it, and the notes underneath said "on the floor". Nothing reported it because it
        // rendered — a plane's material is double sided, so a wall built by accident is a wall.
        //
        // Two tiles across and two down, which is the claim: the field is tileable by construction, so the
        // seam down the middle of this floor is the thing that is not there.
        kit.Children.Add(new MeshNode(Primitives.Plane(9f, 9f), new Material
        {
            Name = "noise floor",
            BaseColorTexture = noise,
            BaseColor = new Vector4(0.34f, 0.37f, 0.42f, 1f),
            Roughness = 0.8f,
            UvScale = new Vector2(2f, 2f)
        })
        {
            Position = new Vector3(0f, -1.4f, 0f)
        });

        kit.Children.Add(new MeshNode(Primitives.Sphere(1.1f, 56, 40), new Material
        {
            Name = "noise as bump",
            BaseColor = new Vector4(0.50f, 0.48f, 0.45f, 1f),
            Roughness = 0.55f,
            BumpTexture = noise,
            BumpScale = 0.9f
        })
        {
            // Sitting on the floor rather than floating over it: the two exhibits in this scene are one
            // field read two ways, and they should be touching.
            Position = new Vector3(-2.4f, -0.3f, -0.6f)
        });

        (float Falloff, Vector3 Color, float Size)[] glows =
        [
            (1f, new Vector3(1.00f, 0.62f, 0.35f), 1.5f),
            (2f, new Vector3(0.55f, 0.85f, 1.00f), 1.3f),
            (4f, new Vector3(1.00f, 0.95f, 0.70f), 1.1f)
        ];

        for (var i = 0; i < glows.Length; i++)
            kit.Children.Add(new SpriteNode
            {
                Texture = Texture.Glow(128, glows[i].Falloff),
                Position = new Vector3(0.9f + i * 1.5f, 0.4f, 0f),
                Size = new Vector2(glows[i].Size, glows[i].Size),
                Color = glows[i].Color,
                Blend = BlendMode.Additive,
                DepthWrite = false,

                // <b>Soft, and the floor's far edge is why.</b> An additive quad meets whatever is behind
                // it at a hard line — the quad simply stops — and from a low angle this scene's own floor
                // cuts straight across all three of these. What that draws is a bright horizontal rule
                // through the middle of the picture, which was reported as a line on the background and is
                // exactly what SoftParticles exists to remove. Fading each one out over the last metre
                // before whatever is behind it costs one number and is the honest fix: a glow is a volume
                // and a volume has no edge where it meets a surface.
                SoftDistance = 1.0f,
                RenderOrder = 1
            });

        return kit;
    }

    public override void Stage(Scene scene)
    {
        // No staging floor: this scene brings its own, and it is one of the two exhibits. Two floors four
        // tenths of a metre apart is what was here before, and the top one was hiding the one with the
        // noise on it.
        Staging.Neutral(scene, floor: false);
        scene.Background = Color.FromRgb(10, 11, 16);
    }
}
