using System.Numerics;
using Avalonia.Media;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// A photograph of a lit sphere, worn by geometry that has no lighting of its own.
/// </summary>
public sealed class MatcapScene : DemoScene
{
    private static readonly (string Name, Vector3 Warm, Vector3 Cool, float Tightness)[] Recipes =
    [
        ("pewter", new Vector3(0.86f, 0.88f, 0.94f), new Vector3(0.14f, 0.16f, 0.22f), 26f),
        ("brass",  new Vector3(1.00f, 0.82f, 0.42f), new Vector3(0.22f, 0.13f, 0.05f), 14f),
        ("clay",   new Vector3(0.94f, 0.60f, 0.48f), new Vector3(0.24f, 0.12f, 0.12f), 6f)
    ];

    private static readonly Lazy<Texture[]> Built = new(() =>
        [.. Recipes.Select(r => Cap(r.Warm, r.Cool, r.Tightness, r.Name))]);

    /// <summary>
    /// The three lit spheres this scene's subjects are wearing, in the order they stand in.
    ///
    /// Public, and shared by identity, because the story hangs them on a wall above the objects wearing
    /// them — see <c>Story.Studio</c>. Three images built the same way would be a picture that
    /// <i>agrees</i> with the exhibit, and the whole claim being made is that what is on the wall is what
    /// is on the sphere. Shared rather than rebuilt for the second reason too: a <see cref="Texture"/> is
    /// uploaded per instance, and this scene is rebuilt from scratch every time it is selected.
    /// </summary>
    public static IReadOnlyList<Texture> Palette => Built.Value;

    public override string Title => "Matcap";

    public override string Summary => "Wear a lit sphere: material and lighting from one image";

    public override string Notes =>
        """
        ShadingModel.Matcap colours a surface by looking up an image with its own normal in view space.
        No lights, no environment, no shading maths at all — a matcap is a photograph of a lit sphere,
        so a mesh wearing one inherits that sphere's material and lighting for nothing.

        It is what product viewers, CAD previews and model browsers reach for, which is most of what an
        Avalonia 3D control spends its life being. The one thing it is not is lit by the scene: rotate
        the camera and it changes, move a lamp and it does not. Watch the key light circle — the
        Standard sphere on the right follows it and the three matcaps ignore it entirely.

        The lookup uses the perturbed normal, so a normal or bump map still shapes a matcap surface.

        MatcapTextureMode decides whether the image takes a sampler unit of its own or shares the base
        colour's. Dedicated is the default and is a request rather than a guarantee: this is the eighth
        map, and eight is exactly what GL ES 2 and WebGL 1 guarantee a fragment shader — so a device
        offering fewer falls back to sharing and says so in the renderer panel. The three images here are
        built in code; the demo ships no image files.
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
        var row = new Node { Name = "matcap" };

        for (var i = 0; i < Palette.Count; i++)
        {
            row.Children.Add(new MeshNode(Primitives.Sphere(0.85f, 48, 32), new Material
            {
                Name = Recipes[i].Name,
                Shading = ShadingModel.Matcap,
                MatcapTexture = Palette[i]
            })
            {
                Position = new Vector3((i - 1.5f) * 1.95f, 0f, 0f)
            });
        }

        row.Children.Add(new MeshNode(Primitives.Sphere(0.85f, 48, 32), new Material
        {
            Name = "standard",
            BaseColor = new Vector4(0.80f, 0.80f, 0.84f, 1f),
            Metallic = 0.9f,
            Roughness = 0.25f
        })
        {
            Position = new Vector3(1.5f * 1.95f, 0f, 0f)
        });

        return row;
    }

    public override void Update(Scene scene, double elapsed)
    {
        var angle = (float)elapsed * 0.7f;
        scene.Light.Direction = Vector3.Normalize(new Vector3(
            MathF.Cos(angle) * 0.9f, -0.42f, MathF.Sin(angle) * 0.9f));

        scene.Invalidate();
    }

    public override void Stage(Scene scene)
    {
        Staging.Neutral(scene);
        scene.Background = Color.FromRgb(18, 18, 22);
    }

    /// <summary>
    /// A lit sphere, rendered flat: the disc is the hemisphere facing the camera, so the pixel at
    /// (x, y) is the surface whose view-space normal points that way and z falls out of the other two.
    /// </summary>
    private static Texture Cap(Vector3 warm, Vector3 cool, float tightness, string name, int size = 192)
    {
        var pixels = new byte[size * size * 4];
        var centre = (size - 1) * 0.5f;
        var light = Vector3.Normalize(new Vector3(-0.45f, 0.55f, 0.70f));

        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            var nx = (x - centre) / centre;
            var ny = -(y - centre) / centre;
            var r2 = nx * nx + ny * ny;
            var nz = r2 < 1f ? MathF.Sqrt(1f - r2) : 0f;
            var n = new Vector3(nx, ny, nz);

            var lambert = MathF.Max(0f, Vector3.Dot(n, light));
            var spec = MathF.Pow(lambert, tightness);
            // A rim term, so the sphere's edge separates from whatever it is drawn against.
            var rim = MathF.Pow(1f - nz, 3f) * 0.35f;

            var c = Vector3.Lerp(cool, warm, lambert) + new Vector3(spec + rim);

            var o = (y * size + x) * 4;
            pixels[o + 0] = Channel(c.X);
            pixels[o + 1] = Channel(c.Y);
            pixels[o + 2] = Channel(c.Z);
            pixels[o + 3] = 255;
        }

        // Clamped, because the corners outside the disc are never sampled and the edge must not wrap.
        return Texture.FromPixels(pixels, size, size, name, TextureWrap.ClampToEdge);

        static byte Channel(float v) => (byte)(Math.Clamp(v, 0f, 1f) * 255f + 0.5f);
    }
}
