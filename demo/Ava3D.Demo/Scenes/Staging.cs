using System.Numerics;
using Avalonia.Media;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// What a subject stands in when nothing else is showing it: a backdrop, a floor, and enough light to
/// read a shape by.
///
/// This exists because of the story. A scene used to build four things — its subject, a ground plane for
/// the subject to sit on, the lights that lit it, and a background colour — and standalone that is exactly
/// right. Mounted in a room it is exactly wrong: the ground is a grey plane lying through the room's
/// floor, the background is a colour the room is not, and the lights spend slots the room needs, of which
/// there are only four in the whole scene.
///
/// So the two are separated, and this is the half that belongs to whoever is showing the scene rather
/// than to the scene. The reference shell calls it. The story never does, because in the story the room
/// is the stage.
///
/// The values are the ones the scenes already agreed on, near enough: twenty-nine of them built a floor
/// of their own and no two of them quite matched. One neutral grey is not a compromise between
/// twenty-nine slightly different greys — it is the thing they were all approximating.
/// </summary>
public static class Staging
{
    /// <summary>The backdrop. Dark enough that an unlit material reads as emitting, light enough to see a
    /// silhouette against.</summary>
    public static readonly Color Backdrop = Color.FromRgb(18, 20, 28);

    /// <summary>
    /// Backdrop, key light, environment and a floor, sized to the subject.
    /// </summary>
    /// <param name="scene">The scene to dress. Its existing key light is aimed rather than replaced.</param>
    /// <param name="extent">How wide the floor is. Roughly twice the subject's radius reads best.</param>
    /// <param name="floor">False for a subject that is in space, or is itself a floor.</param>
    public static void Neutral(Scene scene, float extent = 9f, bool floor = true)
    {
        scene.Background = Backdrop;

        // The key light is aimed, not added: a Scene arrives with one directional light already in it,
        // and adding a second here would spend two of the four slots on what is one light.
        scene.Light.Direction = new Vector3(-0.45f, -0.8f, -0.4f);
        scene.Light.Intensity = 2.6f;
        scene.Light.Ambient = 0.03f;

        // Metals have no diffuse response at all, so without this a metal subject on a neutral stage is a
        // black shape with one hot spot. See EnvironmentLight.
        scene.Environment = EnvironmentLight.Studio(0.42f);

        if (!floor)
            return;

        var ground = new MeshNode(
            Primitives.Plane(extent, extent),
            new Material { BaseColor = new Vector4(0.16f, 0.17f, 0.19f, 1f), Roughness = 0.9f })
        {
            Name = "stage.floor",
            Position = new Vector3(0f, -1f, 0f)
        };

        scene.Children.Add(ground);
    }
}
