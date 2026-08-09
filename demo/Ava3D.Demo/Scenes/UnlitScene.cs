using System.Numerics;
using Avalonia.Media;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// The same sphere lit, emissive and <see cref="Material.Unlit"/>, with the key light circling — the
/// three ways to make something bright, and the one that produces a flat disc.
/// </summary>
public sealed class UnlitScene : DemoScene
{
    public override string Title => "Unlit";

    public override string Summary => "Base colour straight through, no shading, no tone map";

    public override string Notes =>
        """
        Three spheres, one colour, and a light going round them.

        The first is lit. It has a highlight and a terminator, which is correct and is what you want for
        anything that is a surface.

        The second is emissive on top of that lighting. Emissive adds, it does not replace — the lighting
        underneath is still there, so the terminator is still there, just paler. This is the mistake worth
        seeing once: a sun made emissive is a lit ball with a soft edge on one side, which everybody
        notices and nobody can name.

        The third is Unlit. Base colour goes straight to the frame: no lights, no environment, no tone
        map, no terminator. A flat circle, which is exactly what a star at nine hundred thousand units
        should be. It is also what a UI panel, a hologram or a flat-shaded chart wants.

        Unlit is per material, so an unlit sun and a lit hull sit in the same scene under the same lights.
        """;

    public override SceneLook Look => SceneLook.Studio;

    public override bool Animates => true;

    public override bool FramesItself => true;

    public override void Frame(Camera camera)
    {
        camera.Target = Vector3.Zero;
        camera.Distance = 6.6f;
        camera.Yaw = 0.08f;
        camera.Pitch = 0.06f;
        camera.NearPlane = 0.5f;
        camera.FarPlane = 30f;
    }

    /// <summary>
    /// A dark card and nothing else. No floor, and the light is left exactly as a fresh scene supplies it,
    /// because the third sphere's whole claim is that it does not care what the lighting is — put a floor
    /// under these three and the picture starts arguing about bounce instead.
    /// </summary>
    public override void Stage(Scene scene) => scene.Background = Color.FromRgb(8, 9, 14);

    public override Node BuildSubject()
    {
        var three = new Node { Name = "three ways to be bright" };
        var color = new Vector4(1.00f, 0.86f, 0.52f, 1f);

        three.Children.Add(new MeshNode(Primitives.Sphere(0.95f, 40, 28), new Material
        {
            Name = "lit",
            BaseColor = color,
            Roughness = 0.45f
        })
        {
            Position = new Vector3(-2.4f, 0f, 0f)
        });

        three.Children.Add(new MeshNode(Primitives.Sphere(0.95f, 40, 28), new Material
        {
            Name = "emissive",
            BaseColor = color,
            Roughness = 0.45f,
            EmissiveColor = new Vector3(0.55f, 0.44f, 0.20f)
        }));

        three.Children.Add(new MeshNode(Primitives.Sphere(0.95f, 40, 28), new Material
        {
            Name = "unlit",
            BaseColor = color,
            Unlit = true
        })
        {
            Position = new Vector3(2.4f, 0f, 0f)
        });

        return three;
    }

    public override void Update(Scene scene, double elapsed)
    {
        var angle = (float)elapsed * 0.8f;
        scene.Light.Direction = Vector3.Normalize(new Vector3(
            MathF.Cos(angle) * 0.9f, -0.35f, MathF.Sin(angle) * 0.9f));

        scene.Invalidate();
    }
}
