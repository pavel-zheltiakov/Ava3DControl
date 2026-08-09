using System.Numerics;
using Avalonia.Media;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// The key light circling a fixed set of objects, with the sky-and-ground environment cycling under it, so
/// the two halves of the lighting model can be told apart.
/// </summary>
public sealed class LightingScene : DemoScene
{
    public override string Title => "Lighting";

    public override string Summary => "One key light plus a sky-and-ground environment";

    public override string Notes =>
        """
        The key light is a direction and a colour. It moves here; watch the highlight travel and the shadow
        side fall away.

        What keeps the shadow side from going black is Scene.Environment: a two-colour hemisphere, sky above
        and ground below, evaluated along the surface normal for diffuse and along the reflection vector for
        specular. Every third of the cycle it changes — overcast, then a warm studio, then off entirely.
        With it off, the metal sphere has nothing to reflect and shows what "no environment" really costs.
        """;

    public override SceneLook Look => SceneLook.Studio;

    public override bool Animates => true;

    /// <summary>
    /// A floor a mid grey rather than a dark one, because half of what this scene is about is the ground
    /// half of the environment hemisphere, and a near-black floor gives it nothing to be seen on.
    ///
    /// The lights themselves are not set here. <see cref="Update"/> swings the key and switches the
    /// environment under it every six seconds, which means this scene's lighting is its subject and the
    /// stage is only what that lighting lands on.
    /// </summary>
    public override void Stage(Scene scene)
    {
        scene.Background = Color.FromRgb(10, 11, 15);

        scene.Children.Add(new MeshNode(Primitives.Plane(10f, 10f), new Material
        {
            BaseColor = new Vector4(0.22f, 0.22f, 0.24f, 1f),
            Roughness = 0.85f
        })
        {
            Position = new Vector3(0f, -0.9f, 0f),
            IsPickable = false
        });
    }

    public override Node BuildSubject()
    {
        var still = new Node { Name = "still life" };

        still.Children.Add(new MeshNode(Primitives.Sphere(0.7f), new Material
        {
            BaseColor = new Vector4(0.90f, 0.90f, 0.92f, 1f),
            Metallic = 1f,
            Roughness = 0.18f
        })
        {
            Position = new Vector3(-1.3f, 0f, 0f)
        });

        still.Children.Add(new MeshNode(Primitives.Sphere(0.7f), new Material
        {
            BaseColor = new Vector4(0.82f, 0.80f, 0.76f, 1f),
            Roughness = 0.55f
        })
        {
            Position = new Vector3(0.3f, 0f, 0f)
        });

        still.Children.Add(new MeshNode(Primitives.Box(0.9f, 0.9f, 0.9f), new Material
        {
            BaseColor = new Vector4(0.35f, 0.55f, 0.80f, 1f),
            Roughness = 0.35f
        })
        {
            Position = new Vector3(1.7f, -0.15f, 0.2f),
            RotationDegrees = new Vector3(0f, 24f, 0f)
        });

        return still;
    }

    public override void Update(Scene scene, double elapsed)
    {
        var angle = (float)(elapsed * 0.7);
        scene.Light.Direction = Vector3.Normalize(new Vector3(
            MathF.Cos(angle) * 0.8f,
            -0.55f,
            MathF.Sin(angle) * 0.8f));

        // Three environments on a loop. Switching rather than blending, because the point is the contrast.
        var phase = (int)(elapsed / 6.0) % 3;
        scene.Environment = phase switch
        {
            0 => new EnvironmentLight(),
            1 => new EnvironmentLight
            {
                SkyColor = new Vector3(0.62f, 0.55f, 0.44f),
                GroundColor = new Vector3(0.20f, 0.16f, 0.13f),
                Intensity = 1.2f
            },
            _ => EnvironmentLight.None
        };

        scene.Invalidate();
    }
}
