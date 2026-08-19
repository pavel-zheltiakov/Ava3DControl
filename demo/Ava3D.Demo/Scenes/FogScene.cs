using System.Numerics;
using Avalonia.Media;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// A colonnade receding into fog, with the exposure sliding underneath it.
/// </summary>
public sealed class FogScene : DemoScene
{
    private const int Pairs = 9;

    public override string Title => "Fog and exposure";

    public override string Summary => "Distance fades to a colour; the whole scene brightens";

    public override string Notes =>
        """
        Scene.FogColor, FogStart and FogEnd fade geometry into a colour by its distance from the camera.
        Both are scene properties rather than material ones, because both are facts about the frame
        rather than about any surface in it.

        Fog is mixed after the tone map, which is worth stating because it is the arguable half.
        Physically it belongs before — fog is light, and light is tone mapped like everything else — but
        then the colour that comes out is never the colour that went in, and a fog set to match the
        background lands somewhere near it instead of on it. Here the background and the fog are the same
        value, and the far end of the colonnade disappears into it exactly.

        Scene.Exposure, sliding between 0.6 and 1.8, multiplies the linear colour before the curve. That
        is what makes it an exposure rather than a brightness: at the top of the range the highlights
        compress into the shoulder of the tone curve instead of clipping to flat white. It is the right
        knob for "this scene is too dark" and the wrong one for "this lamp is too dim".

        Fog applies to unlit and matcap surfaces too — the emissive markers down the colonnade fade with
        everything else, because a lit panel at the far end of a corridor is still at the far end of it.

        On the CPU renderer fog is per vertex rather than per pixel, so a large flat quad fogs from its
        corners. The columns here are dense enough that it barely shows; the renderer panel says so
        anyway.
        """;

    public override bool Animates => true;

    public override bool FramesItself => true;

    public override void Frame(Camera camera)
    {
        camera.Target = new Vector3(0f, 1.1f, -8f);
        camera.Distance = 11f;
        camera.Yaw = 0.06f;
        camera.Pitch = 0.10f;
        camera.NearPlane = 0.3f;
        camera.FarPlane = 60f;
    }

    public override Node BuildSubject()
    {
        var hall = new Node { Name = "colonnade" };

        var stone = new Material
        {
            Name = "stone",
            BaseColor = new Vector4(0.62f, 0.60f, 0.56f, 1f),
            Roughness = 0.72f
        };

        var lamp = new Material
        {
            Name = "lamp",
            BaseColor = new Vector4(1.00f, 0.86f, 0.55f, 1f),
            Unlit = true
        };

        for (var i = 0; i < Pairs; i++)
        {
            var z = -i * 2.4f;

            foreach (var side in new[] { -1.9f, 1.9f })
            {
                hall.Children.Add(new MeshNode(Primitives.Box(0.55f, 3.2f, 0.55f), stone)
                {
                    Position = new Vector3(side, 1.6f, z)
                });

                hall.Children.Add(new MeshNode(Primitives.Box(0.18f, 0.18f, 0.18f), lamp)
                {
                    Position = new Vector3(side * 0.72f, 2.6f, z)
                });
            }
        }

        hall.Children.Add(new MeshNode(Primitives.Box(6.4f, 0.2f, Pairs * 2.4f + 4f), stone)
        {
            Position = new Vector3(0f, -0.1f, -(Pairs - 1) * 1.2f)
        });

        return hall;
    }

    public override void Stage(Scene scene)
    {
        Staging.Neutral(scene);

        // The background and the fog are the same value on purpose: it is what makes "the fog colour is
        // the colour that appears" checkable by eye rather than only in a diff.
        scene.Background = Color.FromRgb(122, 130, 146);
        scene.FogColor = new Vector3(0.48f, 0.51f, 0.57f);
        scene.FogStart = 4f;
        scene.FogEnd = 22f;
    }

    public override void Update(Scene scene, double elapsed)
    {
        scene.Exposure = 1.2f + MathF.Sin((float)elapsed * 0.9f) * 0.6f;
        scene.Invalidate();
    }
}
