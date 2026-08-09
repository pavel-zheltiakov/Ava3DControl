using System.Numerics;

namespace Ava3D.Demo.Story;

/// <summary>
/// The texture kit: noise, and the four kinds of image a PBR material can be handed.
///
/// Every map in this building is generated here, at startup, from arithmetic. That is not an economy — it
/// is the same argument the rooms make about <see cref="Primitives"/>. A demo that shipped a folder of
/// photographs would be demonstrating a JPEG decoder; a demo whose walls are a value-noise field the
/// visitor can read the source of is demonstrating <see cref="Material"/>, which is the thing being shown.
/// It also means the whole exhibition is a few hundred kilobytes of code and no assets, which is what makes
/// it load in a browser tab.
///
/// The four builders below are the four things a texture can carry, and they differ in exactly one respect
/// that matters and is easy to get wrong: <see cref="Colour"/> writes sRGB and the other three write linear.
/// A base-colour map is a picture and is linearised when it is read; a roughness, normal or occlusion map is
/// <i>data</i> and is not. Encode one of those by mistake and nothing is obviously broken — the surfaces are
/// simply glossier than they were authored to be, everywhere, which is a very long way to chase from the
/// symptom.
/// </summary>
internal static class Grain
{
    /// <summary>
    /// Tileable value noise: a lattice of hashed values, smoothly interpolated, wrapping at
    /// <paramref name="cells"/>.
    ///
    /// Wrapping is the whole reason this is not one line of <c>Random</c>. The maps here are tiled across
    /// walls metres wide, so a field that does not meet itself at the edges puts a visible seam every two
    /// metres down every wall in the building — and a seam in a noise texture reads as a join in the
    /// plaster, which is the one thing plaster is not supposed to have.
    /// </summary>
    /// <param name="cells">Lattice points across the image. Higher is finer.</param>
    /// <param name="seed">Which field. Two maps of the same surface want different ones.</param>
    public static float Noise(float u, float v, int cells, int seed)
    {
        var x = u * cells;
        var y = v * cells;

        var x0 = (int)MathF.Floor(x);
        var y0 = (int)MathF.Floor(y);

        var fx = Smooth(x - x0);
        var fy = Smooth(y - y0);

        var a = Mix(Lattice(x0, y0, cells, seed), Lattice(x0 + 1, y0, cells, seed), fx);
        var b = Mix(Lattice(x0, y0 + 1, cells, seed), Lattice(x0 + 1, y0 + 1, cells, seed), fx);

        return Mix(a, b, fy);
    }

    /// <summary>
    /// Several octaves of <see cref="Noise"/>, each twice as fine and half as strong. The usual thing, and
    /// the reason a surface reads as a material rather than as a pattern: real dirt, wear and render have
    /// structure at every scale you look at them, and one octave has structure at exactly one.
    /// </summary>
    public static float Fbm(float u, float v, int cells, int seed, int octaves = 4)
    {
        var sum = 0f;
        var amplitude = 1f;
        var total = 0f;

        for (var i = 0; i < octaves; i++)
        {
            sum += Noise(u, v, cells << i, seed + i * 131) * amplitude;
            total += amplitude;
            amplitude *= 0.5f;
        }

        return sum / total;
    }

    /// <summary>A base-colour map. Linear values in, sRGB bytes out.</summary>
    public static Texture Colour(int size, string name, Func<float, float, Vector3> shade) =>
        Fill(size, name, shade, encode: true);

    /// <summary>
    /// A metallic-roughness map, in the glTF packing this renderer reads: green is roughness, blue is
    /// metallic. Red and alpha are unused and are written as whatever the caller returns, which costs
    /// nothing and keeps the callers below readable as <c>(_, rough, metal)</c>.
    /// </summary>
    public static Texture Rough(int size, string name, Func<float, float, Vector2> roughMetal) =>
        Fill(size, name, (u, v) =>
        {
            var rm = roughMetal(u, v);
            return new Vector3(0f, rm.X, rm.Y);
        }, encode: false);

    /// <summary>A single-channel map written to all three, for occlusion. Linear.</summary>
    public static Texture Mask(int size, string name, Func<float, float, float> value) =>
        Fill(size, name, (u, v) => new Vector3(value(u, v)), encode: false);

    /// <summary>
    /// A tangent-space normal map, derived from a height field by central differences.
    ///
    /// Written rather than authored because a height field is the thing a person can reason about — "the
    /// grout is a millimetre lower than the tile" — and a normal map is the thing a shader can use. The
    /// conversion is four samples and a normalise, and doing it here means every bumpy surface in the
    /// building is described by one readable function of two numbers.
    /// </summary>
    /// <param name="relief">
    /// How deep the height field is, <b>in metres</b>, from its floor to its ceiling.
    ///
    /// This is the number that has to be physical, and the first version of this was not: it took an
    /// arbitrary "strength" and every caller guessed one. The guesses were between five and twenty times
    /// too large, and what that looks like is not a strong normal map — it is a surface that has come out
    /// in blisters, because the shading normal is being tilted thirty degrees by a field whose real relief
    /// is a fraction of a millimetre. It reads as noise, and on a flat wall lit by a point lamp it reads as
    /// noise arranged in triangles, because the only thing varying across a triangle is the interpolated
    /// normal it is being multiplied by.
    ///
    /// With a real depth and a real tile size there is nothing to guess. Plaster is a millimetre of tooth.
    /// A grout line is three or four. A panel gutter is half a centimetre. Those are the numbers, and the
    /// slope follows from them and from how much surface one texel covers.
    /// </param>
    /// <param name="across">Metres of real surface the image covers — <see cref="Finish.Pitch"/> for
    /// anything mapped by <see cref="Fabric.Map"/> at the default scale.</param>
    public static Texture Bumps(
        int size, string name, Func<float, float, float> height, float relief, float across)
    {
        var step = 1f / size;

        // Rise over run, both in metres: the field's own depth against the width of the two texels the
        // central difference spans.
        var slope = relief / (2f * step * across);

        return Fill(size, name, (u, v) =>
        {
            var dx = (height(u + step, v) - height(u - step, v)) * slope;
            var dy = (height(u, v + step) - height(u, v - step)) * slope;

            var n = Vector3.Normalize(new Vector3(-dx, -dy, 1f));

            // −1..1 into 0..1, which is the encoding every normal map uses and the reason a flat one is
            // that particular shade of lavender.
            return n * 0.5f + new Vector3(0.5f);
        }, encode: false);
    }

    /// <summary>
    /// A soft-edged box: 1 inside, 0 outside, with <paramref name="edge"/> of falloff. The panel grids,
    /// plank gaps and grout lines below are all this function.
    /// </summary>
    public static float Band(float t, float from, float to, float edge)
    {
        var rise = Step(from - edge, from, t);
        var fall = 1f - Step(to, to + edge, t);

        return MathF.Min(rise, fall);
    }

    /// <summary>Smoothstep, spelt out because it is used often enough to be worth a name.</summary>
    public static float Step(float from, float to, float t)
    {
        if (to <= from)
            return t >= to ? 1f : 0f;

        var u = Math.Clamp((t - from) / (to - from), 0f, 1f);
        return u * u * (3f - 2f * u);
    }

    /// <summary>Position within a repeating cell of the given pitch, 0..1.</summary>
    public static float Cell(float t, float pitch)
    {
        var f = t / pitch;
        return f - MathF.Floor(f);
    }

    /// <summary>Which cell of the given pitch, as an integer that can be hashed for per-cell variation.</summary>
    public static int Index(float t, float pitch) => (int)MathF.Floor(t / pitch);

    /// <summary>A stable pseudo-random number for a pair of integers. For per-plank and per-panel tone.</summary>
    public static float Pick(int a, int b, int seed) => Lattice(a, b, int.MaxValue, seed);

    private static Texture Fill(int size, string name, Func<float, float, Vector3> shade, bool encode)
    {
        var pixels = new byte[size * size * 4];

        for (var y = 0; y < size; y++)
        {
            var v = (y + 0.5f) / size;

            for (var x = 0; x < size; x++)
            {
                var c = shade((x + 0.5f) / size, v);
                var o = (y * size + x) * 4;

                pixels[o] = Byte(c.X, encode);
                pixels[o + 1] = Byte(c.Y, encode);
                pixels[o + 2] = Byte(c.Z, encode);
                pixels[o + 3] = 255;
            }
        }

        return Texture.FromPixels(pixels, size, size, name);
    }

    private static byte Byte(float value, bool encode)
    {
        value = Math.Clamp(value, 0f, 1f);

        if (encode)
            value = MathF.Pow(value, 1f / 2.2f);

        return (byte)Math.Clamp(value * 255f + 0.5f, 0f, 255f);
    }

    /// <summary>One lattice point, wrapped so the field tiles.</summary>
    private static float Lattice(int x, int y, int cells, int seed)
    {
        if (cells > 0 && cells != int.MaxValue)
        {
            x = ((x % cells) + cells) % cells;
            y = ((y % cells) + cells) % cells;
        }

        var h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);

        h = (h ^ (h >> 13)) * 1274126177u;
        h ^= h >> 16;

        return (h & 0xFFFFFFu) / (float)0xFFFFFF;
    }

    private static float Smooth(float t) => t * t * (3f - 2f * t);

    private static float Mix(float a, float b, float t) => a + (b - a) * t;
}
