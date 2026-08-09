using System.Numerics;
using Avalonia.Media;
using Ava3D.Demo.Textures;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// Five metal spheres from mirror to matte inside the room that is lighting them, with the room turning.
/// The scene where you can see what a reflection is made of.
/// </summary>
public sealed class EnvironmentScene : DemoScene
{
    private Node? _sky;

    public override string Title => "Environment map";

    public override string Summary => "One image lights the scene and is what the metal reflects";

    public override string Notes =>
        """
        A metal has no diffuse colour of its own. It is only what it reflects — which means that without
        something to reflect, a chrome sphere is a black ball with one highlight on it.

        The sphere around the outside is the environment image, drawn as a sky. The same image is on
        EnvironmentLight.Texture, so the row in the middle is reflecting the room you can see. Nothing is
        painted twice: the four bright panels in the reflection are the four panels behind you.

        Roughness runs from 0.03 on the left to 0.85 on the right. That ramp is what the prefiltering
        buys. The image is blurred once, into eight progressively wider copies, and a surface reads
        whichever two its roughness falls between — so a polished sphere reflects four rectangles, a
        brushed one reflects four smears, and a matte one reflects the room as a single gradient. Sampling
        one sharp image would make all five mirrors.

        The room is turning, which is Rotation — a scalar, not a re-render. The reflections follow because
        they are the same image.

        Leave Texture null and the control goes back to its two-colour hemisphere: sky above, ground
        below, no assets, and every metal a smooth vertical gradient. Correct, and characterless.
        """;

    public override SceneLook Look => SceneLook.Studio;

    public override bool Animates => true;

    public override bool FramesItself => true;

    public override void Frame(Camera camera)
    {
        camera.Target = Vector3.Zero;
        camera.Distance = 8.6f;
        camera.Yaw = 0.34f;
        camera.Pitch = 0.13f;
        camera.NearPlane = 0.4f;
        camera.FarPlane = 90f;
    }

    /// <summary>
    /// The image, twice: once as the thing that lights the row and once as the sphere around it, so that
    /// what is being reflected is in the frame with the reflection.
    ///
    /// All of it is stage, which is a slightly surprising thing to say about the sky — it is the biggest
    /// object in the scene. But the subject here is five metals and nothing else; the sky is what is
    /// showing them, and mounted in a room the room does that job instead. The material gallery gives the
    /// same five spheres a probe baked from the gallery itself, and they reflect that room in the same way
    /// they reflect this one.
    /// </summary>
    public override void Stage(Scene scene)
    {
        var studio = Environments.Studio();

        scene.Background = Color.FromRgb(10, 11, 14);
        scene.Environment = EnvironmentLight.FromTexture(studio, 1.15f);

        // One key light, and a weak one. The environment is doing the work here, and a strong key would
        // put a highlight on every sphere bright enough to hide what they are reflecting.
        scene.Light.Direction = Vector3.Normalize(new Vector3(-0.4f, -0.7f, -0.55f));
        scene.Light.Intensity = 0.55f;
        scene.Light.Ambient = 0f;

        // The room, seen from inside: Cull.Front keeps only the far hemisphere, and Unlit puts the image
        // on screen as it is rather than lighting it. RenderOrder −10 draws it before everything, so the
        // far plane it needs does not have to fight the row for depth precision.
        _sky = new MeshNode(Primitives.Sphere(40f, 96, 48), new Material
        {
            Name = "sky",
            BaseColorTexture = studio,
            Unlit = true,
            Cull = CullMode.Front
        })
        {
            Name = "sky",
            RenderOrder = -10
        };

        scene.Children.Add(_sky);
    }

    public override Node BuildSubject()
    {
        var row = new Node { Name = "roughness" };

        float[] roughness = [0.03f, 0.15f, 0.35f, 0.60f, 0.85f];

        for (var i = 0; i < roughness.Length; i++)
        {
            row.Children.Add(new MeshNode(Primitives.Sphere(0.85f, 56, 36), new Material
            {
                Name = $"roughness {roughness[i]:0.00}",
                BaseColor = new Vector4(0.92f, 0.90f, 0.86f, 1f),
                Metallic = 1f,
                Roughness = roughness[i]
            })
            {
                Position = new Vector3((i - 2) * 2.05f, 0f, 0f)
            });
        }

        return row;
    }

    public override void Update(Scene scene, double elapsed)
    {
        var rotation = (float)elapsed * 0.18f;
        scene.Environment.Rotation = rotation;

        // The sky sphere has to turn with it, and against it: the environment lookup rotates the direction
        // by −Rotation, which makes the image appear turned by +Rotation, and System.Numerics' rotation
        // about Y goes the other way round from atan2(z, x). Get the sign wrong and the two counter-rotate
        // — which is a very good way to notice that the reflection really is the sky and not a copy of it.
        if (_sky is { } sky)
            sky.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, -rotation);

        // Environment is a plain object the graph cannot observe, so the change has to be announced.
        scene.Invalidate();
    }
}
