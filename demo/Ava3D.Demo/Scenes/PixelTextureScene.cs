using System.Numerics;
using Avalonia.Media;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// <see cref="Texture.FromPixels"/>: sixteen bytes of RGBA typed by hand, a map generated in a loop, and
/// one that is thrown away and rebuilt while you watch.
/// </summary>
public sealed class PixelTextureScene : DemoScene
{
    private const int Live = 128;

    private MeshNode _live = null!;
    private Texture _liveTexture = null!;
    private readonly byte[] _livePixels = new byte[Live * Live * 4];
    private int _generation = -1;
    private double _lastBuildMs;

    public override string Title => "Pixel textures";

    public override string Summary => "RGBA straight in, no PNG on the way";

    public override string Notes =>
        """
        Left is a 4×4 texture — sixteen pixels, sixty-four bytes, written as a literal in the source and
        handed to Texture.FromPixels. That is the entire API: tightly packed RGBA8, row-major, top-left
        origin, width × height × 4 bytes.

        Middle is 256×256 generated in a loop, which is the ordinary case.

        Right is regenerated from a new seed every second and a half, and the panel below says what each
        rebuild cost. That is the case this exists for. Texture also takes PNG or JPEG bytes, and for a
        map that ships with the application that is the right thing — it stays compressed in memory until
        a renderer asks for it. For a map generated at runtime it means encoding a PNG in the application
        so that the renderer can immediately decode it again, and four 1024×512 maps of that is
        comfortably over a second of stall in a 32-bit WebAssembly heap.

        It is the same Texture instance throughout, written over and marked with Refresh. That matters more
        than it looks: textures are cached by identity, so a renderer that has uploaded one never reads its
        bytes again, and building a new instance each time means asking it to upload something it has never
        seen — which draws as flat white until its turn on the upload queue comes round. Once every second
        and a half that is a blink. At a game's frame rate it is a flicker.

        Nothing else changes. A raw texture and an encoded one are the same class, wrap the same way,
        filter the same way and are cached against the same identity.
        """;

    public override SceneLook Look => SceneLook.Blueprint;

    public override bool Animates => true;

    public override bool FramesItself => true;

    public override void Frame(Camera camera)
    {
        camera.Target = Vector3.Zero;
        camera.Distance = 6.2f;
        camera.Yaw = 0.04f;
        camera.Pitch = 0.03f;
        camera.NearPlane = 0.5f;
        camera.FarPlane = 30f;
    }

    /// <summary>
    /// A dark card, and no light of any kind that matters: all three panels are unlit, so what is on them
    /// is exactly what is in the arrays. Staging one of these would be staging a photograph.
    /// </summary>
    public override void Stage(Scene scene) => scene.Background = Color.FromRgb(8, 9, 14);

    public override Node BuildSubject()
    {
        var panels = new Node { Name = "pixels" };

        panels.Children.Add(Panel(-2.8f, Tiny()));
        panels.Children.Add(Panel(0f, Generated()));

        Noise(0, _livePixels);
        _liveTexture = Texture.FromPixels(_livePixels, Live, Live, "live");

        _live = Panel(2.8f, _liveTexture);
        _live.Material.EmissiveTexture = _liveTexture;
        panels.Children.Add(_live);

        return panels;

        // Unlit, so what is on the panel is exactly what is in the array — a lit surface would answer a
        // question about lighting instead of one about texture data.
        static MeshNode Panel(float x, Texture texture) =>
            new(Primitives.Plane(2.4f, 2.4f), new Material
            {
                BaseColorTexture = texture,
                Unlit = true
            })
            {
                Position = new Vector3(x, 0f, 0f),
                RotationDegrees = new Vector3(90f, 0f, 0f)
            };
    }

    public override string? Caption => _lastBuildMs > 0
        ? $"{Live}×{Live} regenerated in {_lastBuildMs:F2} ms — no encode, no decode"
        : null;

    public override void Update(Scene scene, double elapsed)
    {
        var generation = (int)(elapsed / 1.5);
        if (generation == _generation)
            return;

        _generation = generation;

        var started = DateTime.UtcNow;
        Noise(generation, _livePixels);
        _lastBuildMs = (DateTime.UtcNow - started).TotalMilliseconds;

        // The same array and the same Texture, written over and marked. A texture is cached by identity, so
        // without the mark the renderer would never look at the bytes a second time — and with a new
        // instance instead it would have to upload one it has never seen, which draws as flat white until
        // its turn on the upload queue comes round. Once every second and a half that is a blink; at a
        // game's frame rate it is a flicker. See Texture.Refresh.
        _liveTexture.Refresh();

        scene.Invalidate();
    }

    /// <summary>
    /// Four pixels by four pixels, typed out. The whole texture is visible in the source, which is the
    /// only way to be sure of the layout — row-major from the top-left corner, four bytes each.
    /// </summary>
    private static Texture Tiny()
    {
        byte[] pixels =
        [
             14,  16,  24, 255,   255,  70,  60, 255,    14,  16,  24, 255,   250, 180,  40, 255,
            120, 220,  90, 255,    14,  16,  24, 255,    40, 160, 255, 255,    14,  16,  24, 255,
             14,  16,  24, 255,   170,  90, 255, 255,    14,  16,  24, 255,   235, 235, 240, 255,
            255, 150,  20, 255,    14,  16,  24, 255,    60, 240, 220, 255,    14,  16,  24, 255
        ];

        return Texture.FromPixels(pixels, 4, 4, "sixteen pixels");
    }

    /// <summary>A 256×256 map built the ordinary way: a loop over the pixels.</summary>
    private static Texture Generated()
    {
        const int size = 256;
        var pixels = new byte[size * size * 4];

        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            var u = x / (float)(size - 1);
            var v = y / (float)(size - 1);
            var ring = MathF.Sqrt((u - 0.5f) * (u - 0.5f) + (v - 0.5f) * (v - 0.5f));
            var band = MathF.Abs(MathF.Sin(ring * 26f)) * 0.55f;

            var o = (y * size + x) * 4;
            pixels[o + 0] = (byte)(40 + u * 180 + band * 60);
            pixels[o + 1] = (byte)(50 + band * 150);
            pixels[o + 2] = (byte)(70 + v * 170);
            pixels[o + 3] = 255;
        }

        return Texture.FromPixels(pixels, size, size, "generated");
    }

    /// <summary>The one that is rebuilt while you watch, written into the buffer it already has.</summary>
    private static void Noise(int seed, byte[] pixels)
    {
        var random = new Random(seed * 7919 + 13);

        // A few blobs rather than per-pixel static, so consecutive generations are visibly different
        // pictures and not two samples of the same grey.
        var centres = new (float X, float Y, float R, float G, float B)[6];
        for (var i = 0; i < centres.Length; i++)
            centres[i] = ((float)random.NextDouble(), (float)random.NextDouble(),
                (float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble());

        for (var y = 0; y < Live; y++)
        for (var x = 0; x < Live; x++)
        {
            var u = x / (float)(Live - 1);
            var v = y / (float)(Live - 1);
            float r = 0.05f, g = 0.06f, b = 0.09f;

            foreach (var c in centres)
            {
                var d = (u - c.X) * (u - c.X) + (v - c.Y) * (v - c.Y);
                var weight = 1f / (1f + d * 90f);
                r += c.R * weight;
                g += c.G * weight;
                b += c.B * weight;
            }

            var o = (y * Live + x) * 4;
            pixels[o + 0] = (byte)Math.Clamp(r * 255f, 0f, 255f);
            pixels[o + 1] = (byte)Math.Clamp(g * 255f, 0f, 255f);
            pixels[o + 2] = (byte)Math.Clamp(b * 255f, 0f, 255f);
            pixels[o + 3] = 255;
        }
    }
}
