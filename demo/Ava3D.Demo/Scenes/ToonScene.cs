using System.Numerics;
using Avalonia.Media;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// Cel shading as a material property, with the band count varying across the row.
/// </summary>
public sealed class ToonScene : DemoScene
{
    public override string Title => "Cel shading";

    public override string Summary => "Quantised light, from two bands to smooth";

    public override string Notes =>
        """
        ShadingModel.Toon quantises the direct light response into ToonBands steps. Left to right: two
        bands, three, five, and Standard for comparison.

        The specular and environment terms are untouched, so a toon surface still has a highlight and
        still sits in its scene — it is the light on it that steps. Quantising inside the microfacet
        terms instead would change what the surface is rather than how light falls on it, and the
        highlight is the part of a cel look that should stay smooth.

        ToonBias slides the bands towards the light or away from it, which is what decides whether a
        shape reads as bright with a thin shadow or dark with a highlight. There is no right default, so
        the default is none.

        A band count rather than a ramp texture, deliberately: a ramp would spend the last guaranteed
        sampler unit, and a count plus the material's own colours covers the case that exists.

        On the CPU renderer the bands come out soft. That renderer lights per vertex, so what is
        quantised is the response at the three corners and what a pixel gets is those interpolated — a
        band edge becomes a gradient rather than a line. Close on this sphere; visibly not the same on a
        low-poly hull. The renderer panel says so rather than leaving it to be found out.
        """;

    public override bool FramesItself => true;

    /// <summary>Framed on the row itself: the staging floor is context, not the subject.</summary>
    public override void Frame(Camera camera)
    {
        camera.Target = new Vector3(0f, 0.0f, 0f);
        camera.Distance = 7.0f;
        camera.Yaw = 0f;
        camera.Pitch = 0.16f;
        camera.NearPlane = 0.3f;
        camera.FarPlane = 60f;
    }

    public override bool Animates => true;

    public override Node BuildSubject()
    {
        var row = new Node { Name = "cel shading" };
        var color = new Vector4(0.90f, 0.62f, 0.38f, 1f);
        int[] bands = [2, 3, 5, 0];

        for (var i = 0; i < bands.Length; i++)
        {
            var material = new Material
            {
                Name = bands[i] == 0 ? "standard" : $"{bands[i]} bands",
                BaseColor = color,
                Roughness = 0.42f
            };

            if (bands[i] > 0)
            {
                material.Shading = ShadingModel.Toon;
                material.ToonBands = bands[i];
            }

            row.Children.Add(new MeshNode(Primitives.Sphere(0.85f, 48, 32), material)
            {
                Position = new Vector3((i - 1.5f) * 1.95f, 0f, 0f)
            });
        }

        return row;
    }

    public override void Update(Scene scene, double elapsed)
    {
        // The light circles, because a still cel-shaded sphere says nothing about where the bands are.
        var angle = (float)elapsed * 0.7f;
        scene.Light.Direction = Vector3.Normalize(new Vector3(
            MathF.Cos(angle) * 0.9f, -0.42f, MathF.Sin(angle) * 0.9f));

        scene.Invalidate();
    }

    public override void Stage(Scene scene)
    {
        Staging.Neutral(scene);
        scene.Background = Color.FromRgb(16, 15, 20);
    }
}
