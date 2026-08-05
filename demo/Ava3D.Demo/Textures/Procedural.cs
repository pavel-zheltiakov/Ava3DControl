using SkiaSharp;

namespace Ava3D.Demo.Textures;

/// <summary>
/// The demo's texture maps, generated in code rather than shipped as files.
///
/// This is a licensing decision as much as a technical one. Ava3DControl is freeware licensed for
/// commercial use, and the demo's source is licensed for reuse without attribution — which rules out the
/// well-known glTF sample assets, most of which are CC BY-NC or need attribution carried forward. Generated
/// maps have no such strings attached, add nothing to the download, and have the side benefit of making the
/// PBR scenes legible: when the roughness map is four lines of arithmetic you can read what the shading is
/// responding to.
///
/// Everything is emitted as PNG bytes because that is what <see cref="Texture"/> takes — images stay encoded
/// until a renderer needs them, so a browser build never holds more than one decoded image at a time.
/// </summary>
public static class Procedural
{
    /// <summary>
    /// The five maps of one surface, generated together.
    ///
    /// Together matters. A base colour that darkens where the roughness map roughens and the occlusion map
    /// occludes reads as one material; five maps authored independently read as five overlays. Deriving them
    /// all from the same height field and masks is the cheap way to get that agreement.
    /// </summary>
    public sealed record MaterialMaps
    {
        public required Texture BaseColor { get; init; }
        public required Texture MetallicRoughness { get; init; }
        public required Texture Normal { get; init; }
        public required Texture Emissive { get; init; }
        public required Texture Occlusion { get; init; }

        /// <summary>Assembles a <see cref="Material"/> that uses all five.</summary>
        public Material ToMaterial(string name) => new()
        {
            Name = name,
            BaseColorTexture = BaseColor,
            MetallicRoughnessTexture = MetallicRoughness,
            NormalTexture = Normal,
            EmissiveTexture = Emissive,
            EmissiveColor = new System.Numerics.Vector3(1f),
            OcclusionTexture = Occlusion,
            Metallic = 1f,
            Roughness = 1f
        };
    }

    /// <summary>A two-colour checkerboard. The oldest texture test there is, and still the clearest.</summary>
    public static Texture Checker(
        int size = 512, int cells = 8,
        SKColor light = default, SKColor dark = default,
        TextureWrap wrap = TextureWrap.Repeat,
        string name = "checker")
    {
        if (light == default) light = new SKColor(226, 228, 232);
        if (dark == default) dark = new SKColor(38, 41, 48);

        return Encode(size, name, wrap, (x, y, px) =>
        {
            var cell = (x * cells / size + y * cells / size) % 2 == 0;
            var c = cell ? light : dark;
            px[0] = c.Red;
            px[1] = c.Green;
            px[2] = c.Blue;
            px[3] = 255;
        });
    }

    /// <summary>
    /// A grid with a coloured gradient, so a UV layout can be read directly off the surface: red increases
    /// with u, green with v, and the lines mark tenths.
    /// </summary>
    public static Texture UvGrid(int size = 512, TextureWrap wrap = TextureWrap.Repeat, string name = "uv grid")
        => Encode(size, name, wrap, (x, y, px) =>
        {
            var u = x / (float)(size - 1);
            var v = y / (float)(size - 1);

            var line = x * 10 % size < size / 96 || y * 10 % size < size / 96;
            var edge = x < size / 64 || y < size / 64 || x >= size - size / 64 || y >= size - size / 64;

            var r = 0.18f + u * 0.72f;
            var g = 0.18f + v * 0.72f;
            var b = 0.35f;

            if (line) (r, g, b) = (0.06f, 0.07f, 0.09f);
            if (edge) (r, g, b) = (0.95f, 0.62f, 0.15f);

            px[0] = Byte(r);
            px[1] = Byte(g);
            px[2] = Byte(b);
            px[3] = 255;
        });

    /// <summary>
    /// A riveted, grooved metal panel — base colour, metallic-roughness, normal, emissive and occlusion, all
    /// derived from one height field.
    /// </summary>
    /// <param name="size">Edge length in pixels. 512 is plenty; the detail is low-frequency by design.</param>
    /// <param name="cells">Panels across the map.</param>
    public static MaterialMaps Panels(int size = 512, int cells = 4)
    {
        // Sampled once and shared, so the five maps cannot drift out of agreement.
        var height = new float[size * size];
        var panel = new float[size * size];   // 1 on the plate, 0 in the groove
        var rivet = new float[size * size];
        var strip = new float[size * size];   // the emissive light strip

        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            var u = (x + 0.5f) / size * cells;
            var v = (y + 0.5f) / size * cells;
            var fu = u - MathF.Floor(u);
            var fv = v - MathF.Floor(v);
            var row = (int)MathF.Floor(v);

            // Distance to the nearest cell edge: 0 on the seam, 0.5 at the centre.
            var edge = MathF.Min(MathF.Min(fu, 1f - fu), MathF.Min(fv, 1f - fv));
            var plate = Ease.InOut(Ease.Ramp(0.028f, 0.055f, edge));

            var bump = 0f;
            foreach (var (cx, cy) in new[] { (0.11f, 0.11f), (0.89f, 0.11f), (0.11f, 0.89f), (0.89f, 0.89f) })
            {
                var d = MathF.Sqrt((fu - cx) * (fu - cx) + (fv - cy) * (fv - cy));
                bump = MathF.Max(bump, 1f - Ease.InOut(Ease.Ramp(0.020f, 0.038f, d)));
            }

            // A lit strip along the horizontal seam of every other row of panels.
            var lit = row % 2 == 0 && MathF.Min(fv, 1f - fv) < 0.020f ? 1f : 0f;

            var i = y * size + x;
            panel[i] = plate;
            rivet[i] = bump;
            strip[i] = lit;
            height[i] = plate * 0.55f + bump * 0.45f + Grain(x, y) * 0.05f;
        }

        var baseColor = Encode(size, "panel base colour", TextureWrap.Repeat, (x, y, px) =>
        {
            var i = y * size + x;
            var u = (x + 0.5f) / size * cells;
            var v = (y + 0.5f) / size * cells;
            var tint = 0.86f + Hash((int)MathF.Floor(u), (int)MathF.Floor(v)) * 0.20f;

            // sRGB values: this map is the one channel the renderers gamma-decode.
            var grey = (0.55f * panel[i] + 0.14f) * tint;
            grey = MathF.Max(grey, 0.10f);
            if (rivet[i] > 0.5f) grey *= 1.18f;

            var r = grey * 1.00f;
            var g = grey * 0.99f;
            var b = grey * 0.96f;

            if (strip[i] > 0.5f) (r, g, b) = (0.10f, 0.10f, 0.11f);

            px[0] = Byte(r);
            px[1] = Byte(g);
            px[2] = Byte(b);
            px[3] = 255;
        });

        var metallicRoughness = Encode(size, "panel metallic-roughness", TextureWrap.Repeat, (x, y, px) =>
        {
            var i = y * size + x;

            // glTF packing. Red is unused, green is roughness, blue is metallic — linear data, so no
            // gamma anywhere near it.
            var roughness = float.Lerp(0.78f, 0.30f, panel[i]);
            if (rivet[i] > 0.5f) roughness = 0.20f;

            var metallic = float.Lerp(0.45f, 1.00f, panel[i]);

            px[0] = 0;
            px[1] = ByteLinear(roughness);
            px[2] = ByteLinear(metallic);
            px[3] = 255;
        });

        var normal = NormalFromHeight(size, height, strength: 2.6f, name: "panel normal");

        var emissive = Encode(size, "panel emissive", TextureWrap.Repeat, (x, y, px) =>
        {
            var i = y * size + x;
            var glow = strip[i];
            px[0] = ByteLinear(glow * 0.20f);
            px[1] = ByteLinear(glow * 0.85f);
            px[2] = ByteLinear(glow * 0.95f);
            px[3] = 255;
        });

        var occlusion = Encode(size, "panel occlusion", TextureWrap.Repeat, (x, y, px) =>
        {
            var i = y * size + x;
            // Crevices are what ambient light cannot reach. Rivets get a contact ring rather than a hole.
            var ao = float.Lerp(0.42f, 1.00f, panel[i]);
            ao *= 1f - rivet[i] * 0.12f;
            var v = ByteLinear(ao);
            px[0] = v;
            px[1] = v;
            px[2] = v;
            px[3] = 255;
        });

        return new MaterialMaps
        {
            BaseColor = baseColor,
            MetallicRoughness = metallicRoughness,
            Normal = normal,
            Emissive = emissive,
            Occlusion = occlusion
        };
    }

    /// <summary>
    /// A tangent-space normal map from a height field, by central differences.
    ///
    /// The green channel points +V, matching glTF's convention. Getting that sign wrong is the classic
    /// normal-map bug: the surface lights as though every dent were a bump, which looks plausible until it
    /// is next to something correct.
    /// </summary>
    public static Texture NormalFromHeight(
        int size, float[] height, float strength = 2f,
        TextureWrap wrap = TextureWrap.Repeat, string name = "normal")
        => Encode(size, name, wrap, (x, y, px) =>
        {
            float H(int px2, int py) => height[Wrap(py, size) * size + Wrap(px2, size)];

            var dx = (H(x + 1, y) - H(x - 1, y)) * strength;
            var dy = (H(x, y + 1) - H(x, y - 1)) * strength;

            // The surface normal of a height field is (-dh/dx, -dh/dy, 1), normalised.
            var nx = -dx;
            var ny = -dy;
            const float nz = 1f;
            var len = MathF.Sqrt(nx * nx + ny * ny + nz * nz);

            px[0] = (byte)Math.Clamp((int)((nx / len * 0.5f + 0.5f) * 255f + 0.5f), 0, 255);
            px[1] = (byte)Math.Clamp((int)((ny / len * 0.5f + 0.5f) * 255f + 0.5f), 0, 255);
            px[2] = (byte)Math.Clamp((int)((nz / len * 0.5f + 0.5f) * 255f + 0.5f), 0, 255);
            px[3] = 255;
        });

    /// <summary>A field of round dents and bumps, as a normal map. Useful on its own for a "hammered" look.</summary>
    public static Texture Dimples(int size = 512, int count = 10, TextureWrap wrap = TextureWrap.Repeat)
    {
        var height = new float[size * size];
        var radius = size / (float)count * 0.42f;

        for (var cy = 0; cy < count; cy++)
        for (var cx = 0; cx < count; cx++)
        {
            var ox = (cx + 0.5f) * size / count;
            var oy = (cy + 0.5f) * size / count;
            var sign = (cx + cy) % 2 == 0 ? 1f : -1f;

            var lo = (int)MathF.Floor(oy - radius);
            var hi = (int)MathF.Ceiling(oy + radius);
            for (var y = lo; y <= hi; y++)
            for (var x = (int)MathF.Floor(ox - radius); x <= (int)MathF.Ceiling(ox + radius); x++)
            {
                var d = MathF.Sqrt((x - ox) * (x - ox) + (y - oy) * (y - oy)) / radius;
                if (d >= 1f)
                    continue;

                // A cosine cap rather than a cone: no crease at the rim, so the normals stay smooth.
                height[Wrap(y, size) * size + Wrap(x, size)] += sign * (MathF.Cos(d * MathF.PI) + 1f) * 0.5f;
            }
        }

        return NormalFromHeight(size, height, strength: 1.4f, wrap: wrap, name: "dimples");
    }

    /// <summary>Fills an RGBA image pixel by pixel and encodes it as a PNG.</summary>
    private static Texture Encode(int size, string name, TextureWrap wrap, Action<int, int, Span<byte>> shade)
    {
        var pixels = new byte[size * size * 4];

        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
            shade(x, y, pixels.AsSpan((y * size + x) * 4, 4));

        var info = new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var image = SKImage.FromPixelCopy(info, pixels);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        return new Texture
        {
            Encoded = data.ToArray(),
            WrapU = wrap,
            WrapV = wrap,
            Name = name
        };
    }

    private static int Wrap(int v, int size) => (v % size + size) % size;

    /// <summary>A stable per-cell value in 0..1. Not good randomness; good enough to break up a tiling.</summary>
    private static float Hash(int x, int y)
    {
        var h = x * 374761393 + y * 668265263;
        h = (h ^ (h >> 13)) * 1274126177;
        return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
    }

    /// <summary>Fine per-pixel noise, to keep large flat areas from looking like vector art.</summary>
    private static float Grain(int x, int y) => Hash(x * 7 + 3, y * 13 + 11) - 0.5f;

    /// <summary>
    /// Writes an sRGB-encoded channel. The colour values above were chosen by eye, which means they are
    /// already in the perceptual space a base-colour or emissive map is expected to be in, so there is no
    /// conversion to do — the renderer will gamma-decode them on the way in.
    /// </summary>
    private static byte Byte(float srgb) =>
        (byte)Math.Clamp((int)(srgb * 255f + 0.5f), 0, 255);

    /// <summary>
    /// Writes a linear channel, for maps that are data rather than colour: metallic-roughness, normal and
    /// occlusion. The arithmetic is identical to <see cref="Byte"/> — the two exist so that every call site
    /// states which kind of map it is writing, since getting that wrong is invisible until it is compared
    /// against something correct.
    /// </summary>
    private static byte ByteLinear(float value) =>
        (byte)Math.Clamp((int)(value * 255f + 0.5f), 0, 255);
}
