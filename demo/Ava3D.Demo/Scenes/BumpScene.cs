using System.Numerics;
using Ava3D.Demo.Textures;
using Avalonia.Media;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// One height field, shown twice: as a <see cref="Material.BumpTexture"/> and as a normal map derived
/// from the same data, both with a pole facing the camera.
/// </summary>
public sealed class BumpScene : DemoScene
{
    private const int Size = 256;

    public override string Title => "Bump mapping";

    public override string Summary => "A height field instead of a normal map, and no tangents";

    public override string Notes =>
        """
        The same craters four times. First smooth. Then as a BumpTexture — the height field itself.
        Then as a normal map baked from that identical height field. Then the same normal map on a mesh
        that was never given tangents.

        The middle two agree, and that is the first thing worth knowing: a bump map is the data you
        already have, and a normal map is that same data pushed through a convention — green points +V
        in glTF — which is easy to get backwards and invisible until it is next to something correct.

        The fourth sphere is the difference that matters. A normal map is read in a tangent frame the
        mesh has to carry; the other spheres call Mesh.WithGeneratedTangents and that one does not, so
        its normal map is ignored entirely and it renders as smooth as the first. A bump map needs
        nothing: it perturbs the normal by the screen-space gradient of the height, derived from the
        geometry the renderer is already rasterising. That is why it fits anything generated at runtime,
        anything loaded without tangents, and a UV sphere, whose tangent frame spins where the columns
        of the map converge at the poles.

        The trade is two extra texture fetches and a dependency on derivatives, and that bump cannot
        express a normal leaning sideways without a matching change in height — a woven or anisotropic
        surface still wants a real normal map. Both are here; they are not competing.

        BumpScale is in the height map's own units, so it is worth tuning per map rather than copying a
        number between them.
        """;

    public override SceneLook Look => SceneLook.Blueprint;

    public override bool Animates => true;

    public override bool FramesItself => true;

    public override void Frame(Camera camera)
    {
        // Nearly square on. The poles are the subject, and a three-quarter view hides them.
        camera.Target = Vector3.Zero;
        camera.Distance = 7.2f;
        camera.Yaw = 0.04f;
        camera.Pitch = 0.05f;
        camera.NearPlane = 0.5f;
        camera.FarPlane = 30f;
    }

    public override Scene Build()
    {
        var scene = new Scene { Background = Color.FromRgb(8, 9, 14) };

        var height = Craters();
        var bump = Grey(height);
        var normal = Procedural.NormalFromHeight(Size, height, strength: 2.2f, name: "crater normal");

        scene.Lights.Clear();
        scene.Lights.Add(new DirectionalLight
        {
            // Low and from the side: relief is invisible under a light behind the camera.
            Direction = Vector3.Normalize(new Vector3(-0.92f, -0.15f, -0.36f)),
            Color = new Vector3(1.00f, 0.97f, 0.92f),
            Intensity = 2.4f,
            Ambient = 0.04f
        });

        // Built once and shared: the four spheres differ only in their material, and the fourth in
        // whether its mesh carries a tangent frame at all.
        var tangented = Primitives.Sphere(0.95f, 64, 48).WithGeneratedTangents();
        var plain = Primitives.Sphere(0.95f, 64, 48);

        scene.Children.Add(Ball(-3.6f, tangented, new Material
        {
            Name = "smooth",
            BaseColor = new Vector4(0.62f, 0.63f, 0.66f, 1f),
            Roughness = 0.75f
        }));

        scene.Children.Add(Ball(-1.2f, tangented, new Material
        {
            Name = "bump",
            BaseColor = new Vector4(0.62f, 0.63f, 0.66f, 1f),
            Roughness = 0.75f,
            BumpTexture = bump,
            BumpScale = 6f
        }));

        scene.Children.Add(Ball(1.2f, tangented, new Material
        {
            Name = "normal map",
            BaseColor = new Vector4(0.62f, 0.63f, 0.66f, 1f),
            Roughness = 0.75f,
            NormalTexture = normal
        }));

        // The same material on a mesh with no tangents. The normal map is dropped rather than applied
        // wrongly, which is the right call and worth seeing once.
        scene.Children.Add(Ball(3.6f, plain, new Material
        {
            Name = "normal map, no tangents",
            BaseColor = new Vector4(0.62f, 0.63f, 0.66f, 1f),
            Roughness = 0.75f,
            NormalTexture = normal
        }));

        return scene;

        static MeshNode Ball(float x, Mesh mesh, Material material) =>
            new(mesh, material)
            {
                Position = new Vector3(x, 0f, 0f),
                // A pole toward the camera, which is where a UV sphere's tangent frame is worst.
                RotationDegrees = new Vector3(-90f, 0f, 0f)
            };
    }

    public override void Update(Scene scene, double elapsed)
    {
        // The light sweeps rather than the spheres turning: relief is a function of the light angle, and
        // spinning the geometry instead would let a reader think it was the geometry.
        var angle = (float)elapsed * 0.5f;
        scene.Light.Direction = Vector3.Normalize(new Vector3(
            MathF.Cos(angle) * 0.95f, MathF.Sin(angle) * 0.95f, -0.36f));

        scene.Invalidate();
    }

    /// <summary>A field of round craters, wrapped in u so the sphere has no seam.</summary>
    private static float[] Craters()
    {
        var height = new float[Size * Size];
        var random = new Random(11);

        for (var i = 0; i < 90; i++)
        {
            var cx = (float)random.NextDouble() * Size;
            var cy = (float)random.NextDouble() * Size;
            var radius = 8f + (float)random.NextDouble() * 16f;
            var sign = random.NextDouble() < 0.5 ? -1f : 1f;

            for (var y = (int)(cy - radius); y <= (int)(cy + radius); y++)
            for (var x = (int)(cx - radius); x <= (int)(cx + radius); x++)
            {
                var d = MathF.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / radius;
                if (d >= 1f)
                    continue;

                // A cosine cap: no crease at the rim, so the gradients stay smooth either way of it.
                height[Wrap(y) * Size + Wrap(x)] += sign * (MathF.Cos(d * MathF.PI) + 1f) * 0.5f;
            }
        }

        return height;

        static int Wrap(int v) => (v % Size + Size) % Size;
    }

    /// <summary>The height field as a greyscale texture, straight from pixels — no encode, no decode.</summary>
    private static Texture Grey(float[] height)
    {
        var pixels = new byte[Size * Size * 4];
        var (lo, hi) = (float.MaxValue, float.MinValue);

        foreach (var h in height)
        {
            lo = MathF.Min(lo, h);
            hi = MathF.Max(hi, h);
        }

        var range = MathF.Max(hi - lo, 1e-5f);

        for (var i = 0; i < height.Length; i++)
        {
            var value = (byte)Math.Clamp((height[i] - lo) / range * 255f, 0f, 255f);
            pixels[i * 4 + 0] = value;
            pixels[i * 4 + 1] = value;
            pixels[i * 4 + 2] = value;
            pixels[i * 4 + 3] = 255;
        }

        return Texture.FromPixels(pixels, Size, Size, "crater height");
    }
}
