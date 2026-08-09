using System.Numerics;
using Avalonia.Media;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// The metallic-roughness chart: metallic across, roughness down, nothing else varying.
///
/// Every PBR renderer should be able to produce this picture, and comparing it against a reference is the
/// fastest way to find out whether a shading model is actually the one it claims to be.
/// </summary>
/// <param name="prop">
/// What the chart is drawn on. A sphere by default, because a sphere is the honest shape for reading
/// roughness — it presents every angle between the light and the eye at once, so one object shows the whole
/// of what a highlight does.
///
/// It is a parameter because the story charts something else. The material gallery sets the same
/// forty-nine materials into a panel in its wall, at a size that fits the wall rather than the viewport,
/// and a chart that had its prop and its spacing baked in would have had to be copied to get there — at
/// which point there are two of them and one goes stale. One argument is the whole cost of not forking it.
/// </param>
/// <param name="pitch">How far apart the cells are. Scales with the prop.</param>
public sealed class PbrChartScene(Mesh? prop = null, float pitch = 0.95f) : DemoScene
{
    private const int Steps = 7;

    public override string Title => "PBR chart";

    public override string Summary => "Metallic across, roughness down — no textures at all";

    public override string Notes =>
        """
        Metallic runs 0 to 1 left to right. Roughness runs 0.03 to 1 top to bottom. Base colour is one warm
        grey everywhere, and there is not a single texture in this scene.

        Read the corners. Top left is polished plastic: a small white highlight on a bright diffuse body. Top
        right is a mirror, showing the sky above and the ground below and almost nothing of its own colour.
        Bottom left is chalk. Bottom right is brushed metal — the highlight has spread across the whole
        surface and the reflection has blurred into it.

        The middle columns are physically meaningless for any real substance; nothing is half a conductor.
        They are in the model because authored assets use them to fake rust, wear and dust, so the renderers
        interpolate rather than snapping to one end.
        """;

    public override SceneLook Look => SceneLook.Studio;

    /// <summary>
    /// A near-black card and a hard horizon, and no floor at all — the chart is read, not toured, and a
    /// ground plane under it would put a reflection of itself in the bottom row.
    /// </summary>
    public override void Stage(Scene scene)
    {
        scene.Background = Color.FromRgb(24, 26, 32);

        // Deliberately high contrast between sky and ground. A metal shows only what it reflects, so a
        // bright top and a near-black bottom give it a visible horizon — and the roughness axis then reads
        // as that horizon blurring away, which is the clearest demonstration of roughness there is.
        //
        // Mounted, the room supplies this instead, and supplies something much better: the gallery's own
        // baked probe, so the top-right cell reflects the gallery rather than a painted horizon.
        scene.Environment = new EnvironmentLight
        {
            SkyColor = new Vector3(0.62f, 0.67f, 0.76f),
            GroundColor = new Vector3(0.05f, 0.05f, 0.06f)
        };
    }

    public override Node BuildSubject()
    {
        var chart = new Node { Name = "chart" };
        var sphere = prop ?? Primitives.Sphere(0.42f, 48, 24);

        // A mid-grey, not a near-white. The whole point of the chart is the range between chalk and mirror,
        // and a bright albedo pushes the top rows into the tone map's shoulder where they all read the same.
        var baseColor = new Vector4(0.55f, 0.53f, 0.50f, 1f);

        for (var row = 0; row < Steps; row++)
        for (var col = 0; col < Steps; col++)
        {
            var metallic = col / (Steps - 1f);
            var roughness = 0.03f + row / (Steps - 1f) * 0.97f;

            chart.Children.Add(new MeshNode(sphere, new Material
            {
                Name = $"metallic {metallic:0.00}, roughness {roughness:0.00}",
                BaseColor = baseColor,
                Metallic = metallic,
                Roughness = roughness
            })
            {
                Name = $"M{metallic:0.00} R{roughness:0.00}",
                Position = new Vector3(
                    (col - (Steps - 1) / 2f) * pitch,
                    ((Steps - 1) / 2f - row) * pitch,
                    0f),
                IsPickable = false
            });
        }

        return chart;
    }

    public override bool FramesItself => true;

    /// <summary>
    /// Square-on and close. A chart read from three-quarters is a chart whose columns are not comparable, and
    /// a narrow field of view keeps the spheres at the edges from leaning.
    /// </summary>
    public override void Frame(Camera camera)
    {
        camera.Yaw = 0f;
        camera.Pitch = 0f;
        camera.Target = System.Numerics.Vector3.Zero;
        camera.Distance = 9.4f;
        camera.FieldOfView = 40f;
    }
}
