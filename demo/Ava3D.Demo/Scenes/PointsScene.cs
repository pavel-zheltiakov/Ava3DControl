using System.Numerics;
using Avalonia.Media;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// <see cref="PointsNode"/> on its own: one cloud built twice, receding from the camera, differing only
/// in <see cref="PointsNode.SizeAttenuation"/>.
/// </summary>
public sealed class PointsScene : DemoScene
{
    public override string Title => "Points";

    public override string Summary => "Constant pixels or constant world units";

    public override string Notes =>
        """
        The same 600 points twice, running away from the camera. On the left SizeAttenuation is off and
        every point is four pixels wherever it is; on the right it is on and the size is in world units,
        so the far end of the corridor fades to nothing.

        Off is what a starfield wants, and the reason is not performance. Stars on a sphere two million
        units out have to look the same as stars on one at twenty million, because that radius is an
        implementation detail of how the sky is drawn and not a thing the player should be able to see.
        On is what a spray of sparks or a scanned point cloud wants, where the points really are objects
        of a size.

        Neither is reproducible with a texture. Painted into a sky map at this density the points land
        sub-texel and average into grey haze — no amount of resolution fixes it, because the look is that
        each one is a hard point rather than a filtered smudge.

        The backends cache a GPU buffer against the array's identity, so a 1,660-point starfield uploads
        once and is drawn every frame from then on. A cloud that moves writes into the array and calls
        InvalidateGeometry(), which refills that same buffer — one upload, no allocation. That is how a
        particle spray gets a speed per particle, which scaling the node cannot do.
        """;

    public override SceneLook Look => SceneLook.Blueprint;

    public override bool FramesItself => true;

    public override void Frame(Camera camera)
    {
        // Down the corridor, slightly off-axis, so the two columns separate and the recession reads.
        camera.Target = new Vector3(0f, 0f, -20f);
        camera.Distance = 24f;
        camera.Yaw = 0.09f;
        camera.Pitch = 0.08f;
        camera.NearPlane = 0.4f;
        camera.FarPlane = 120f;
    }

    public override Scene Build()
    {
        var scene = new Scene { Background = Color.FromRgb(6, 7, 11) };

        scene.Children.Add(new PointsNode
        {
            Positions = Corridor(-3.2f, seed: 7),
            Color = new Vector3(0.62f, 0.80f, 1.00f),
            Size = 4f,
            SizeAttenuation = false,
            Blend = BlendMode.Additive,
            DepthWrite = false
        });

        scene.Children.Add(new PointsNode
        {
            Positions = Corridor(3.2f, seed: 7),
            Color = new Vector3(1.00f, 0.78f, 0.48f),
            // World units this time, chosen so the nearest points match the four pixels opposite.
            Size = 0.055f,
            SizeAttenuation = true,
            Blend = BlendMode.Additive,
            DepthWrite = false
        });

        return scene;
    }

    /// <summary>The same cloud either side of the centre line, from the same seed, so only the flag differs.</summary>
    private static Vector3[] Corridor(float x, int seed)
    {
        var random = new Random(seed);
        var points = new Vector3[600];

        for (var i = 0; i < points.Length; i++)
            points[i] = new Vector3(
                x + (float)(random.NextDouble() - 0.5) * 4.0f,
                (float)(random.NextDouble() - 0.5) * 5.0f,
                -5f + (float)random.NextDouble() * -42f);

        return points;
    }
}
