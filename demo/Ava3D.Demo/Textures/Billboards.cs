namespace Ava3D.Demo.Textures;

/// <summary>
/// The textures a billboard is actually made of.
///
/// A sprite is not automatically a blurry dot. The reason billboards exist in the first place is grass and
/// foliage and clouds — things with far too much silhouette to model, seen from far enough away that the
/// only thing you would notice about the real geometry is its cost. Those need a shape with a hard edge and
/// a transparent surround, which is a very different picture from a radial falloff.
///
/// Everything here is RGBA straight into <see cref="Texture.FromPixels"/>, alpha included, because alpha is
/// most of the point.
/// </summary>
public static class Billboards
{
    /// <summary>
    /// A tuft of grass: blades rising from the bottom centre, transparent everywhere else.
    ///
    /// Drawn as tapering curved strokes rather than triangles, because a blade of grass read at billboard
    /// size is a silhouette and nothing else — the tip has to come to a point or the whole field looks like
    /// a row of little flags.
    /// </summary>
    public static Texture Grass(int size = 128, int seed = 5)
    {
        var pixels = new byte[size * size * 4];
        var random = new Random(seed);

        // Each blade: where it leaves the ground, how far it leans, how tall, how thick.
        var blades = new (float Root, float Lean, float Height, float Width, float Shade)[11];
        for (var i = 0; i < blades.Length; i++)
            blades[i] = (
                0.5f + (float)(random.NextDouble() - 0.5) * 0.62f,
                (float)(random.NextDouble() - 0.5) * 0.75f,
                0.55f + (float)random.NextDouble() * 0.42f,
                0.020f + (float)random.NextDouble() * 0.022f,
                0.72f + (float)random.NextDouble() * 0.38f);

        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            var u = (x + 0.5f) / size;
            // v measured from the ground up, which is how the blades are described.
            var v = 1f - (y + 0.5f) / size;

            var cover = 0f;
            var shade = 0f;

            foreach (var blade in blades)
            {
                if (v > blade.Height)
                    continue;

                var t = v / blade.Height;                     // 0 at the root, 1 at the tip
                // Quadratic lean, so the blade leaves the ground upright and bends over as it rises.
                var centre = blade.Root + blade.Lean * t * t;
                var width = blade.Width * (1f - t) + 0.0015f; // tapering to a point
                var d = MathF.Abs(u - centre);

                // One pixel of softness at the edge and no more: a blade with a soft edge reads as fog.
                var hit = 1f - Ease.InOut(Ease.Ramp(width * 0.6f, width, d));
                if (hit <= cover)
                    continue;

                cover = hit;
                // Darker at the root, and each blade slightly different, so the tuft has depth in it.
                shade = blade.Shade * (0.45f + 0.55f * t);
            }

            var o = (y * size + x) * 4;
            pixels[o + 0] = Channel(0.16f * shade + 0.06f);
            pixels[o + 1] = Channel(0.42f * shade + 0.10f);
            pixels[o + 2] = Channel(0.14f * shade + 0.05f);
            pixels[o + 3] = Channel(cover);
        }

        return Texture.FromPixels(pixels, size, size, "grass tuft");
    }

    /// <summary>
    /// A cloud puff: overlapping lobes, bright on top and grey underneath, fading to nothing at the edge.
    ///
    /// The lobes are what stop it being a smudge. One blob with a radial falloff is the sprite everybody
    /// writes first and nobody believes; three or four overlapping ones with the light coming from above
    /// read as cloud immediately.
    /// </summary>
    public static Texture Cloud(int width = 256, int height = 128, int seed = 9)
    {
        var pixels = new byte[width * height * 4];
        var random = new Random(seed);

        var lobes = new (float X, float Y, float R)[7];
        for (var i = 0; i < lobes.Length; i++)
        {
            var t = i / (lobes.Length - 1f);
            lobes[i] = (
                0.12f + t * 0.76f,
                0.42f + (float)(random.NextDouble() - 0.5) * 0.26f,
                0.10f + (float)random.NextDouble() * 0.13f);
        }

        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var u = (x + 0.5f) / width;
            var v = (y + 0.5f) / height;

            // Aspect-corrected, so the lobes are round rather than stretched with the texture.
            var aspect = width / (float)height;

            var density = 0f;
            foreach (var lobe in lobes)
            {
                var dx = (u - lobe.X) * aspect;
                var dy = v - lobe.Y;
                var d = MathF.Sqrt(dx * dx + dy * dy) / (lobe.R * aspect);
                density += MathF.Max(0f, 1f - d * d);
            }

            var alpha = Math.Clamp((density - 0.30f) * 1.45f, 0f, 1f);
            // Squared, which sharpens the edge without hardening it: cloud has an edge, just not a line.
            alpha *= alpha;

            // Lit from above: the top of a puff is white, the underside is a cool grey.
            var lit = Math.Clamp(1f - v * 1.25f, 0f, 1f);
            var grey = 0.58f + 0.42f * lit;

            var o = (y * width + x) * 4;
            pixels[o + 0] = Channel(grey);
            pixels[o + 1] = Channel(grey * 0.99f + 0.01f);
            pixels[o + 2] = Channel(grey * 0.97f + 0.03f);
            pixels[o + 3] = Channel(alpha);
        }

        return Texture.FromPixels(pixels, width, height, "cloud");
    }

    /// <summary>A brick wall: staggered courses, recessed mortar, and no two bricks the same colour.</summary>
    public static Texture Brick(int size = 256, int courses = 8)
    {
        var pixels = new byte[size * size * 4];
        var perCourse = courses / 2;

        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            var u = (x + 0.5f) / size;
            var v = (y + 0.5f) / size;

            var row = (int)MathF.Floor(v * courses);
            // Every other course offset by half a brick, which is the whole look.
            var shifted = u * perCourse + (row % 2 == 0 ? 0f : 0.5f);
            var column = (int)MathF.Floor(shifted);

            var inBrickX = shifted - column;
            var inBrickY = v * courses - row;

            // Distance to the nearest mortar joint, in brick-local units.
            var edge = MathF.Min(
                MathF.Min(inBrickX, 1f - inBrickX) * 2f,
                MathF.Min(inBrickY, 1f - inBrickY));

            var mortar = 1f - Ease.InOut(Ease.Ramp(0.03f, 0.075f, edge));

            var tint = Hash(column * 31 + row * 17);
            var r = 0.44f + tint * 0.20f;
            var g = 0.20f + tint * 0.10f;
            var b = 0.15f + tint * 0.07f;

            // Mortar is pale, flat and slightly recessed, so it darkens where it meets the brick.
            r = float.Lerp(r, 0.62f, mortar);
            g = float.Lerp(g, 0.60f, mortar);
            b = float.Lerp(b, 0.56f, mortar);

            var grain = (Hash(x * 7 + y * 13) - 0.5f) * 0.06f;

            var o = (y * size + x) * 4;
            pixels[o + 0] = Channel(r + grain);
            pixels[o + 1] = Channel(g + grain);
            pixels[o + 2] = Channel(b + grain);
            pixels[o + 3] = 255;
        }

        return Texture.FromPixels(pixels, size, size, "brick");
    }

    /// <summary>Turf, for the ground the tufts stand in: mottled green with worn patches.</summary>
    public static Texture Turf(int size = 256, int seed = 3)
    {
        var pixels = new byte[size * size * 4];

        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            var u = (x + 0.5f) / size;
            var v = (y + 0.5f) / size;

            var coarse = Fbm(u * 5f + seed, v * 5f, 4);
            var fine = Fbm(u * 34f, v * 34f, 2);

            // Worn earth where the coarse field dips, grass everywhere else.
            var worn = Math.Clamp((0.44f - coarse) * 3.6f, 0f, 1f);

            var r = float.Lerp(0.15f + fine * 0.10f, 0.34f, worn);
            var g = float.Lerp(0.29f + fine * 0.14f, 0.26f, worn);
            var b = float.Lerp(0.12f + fine * 0.07f, 0.18f, worn);

            var o = (y * size + x) * 4;
            pixels[o + 0] = Channel(r);
            pixels[o + 1] = Channel(g);
            pixels[o + 2] = Channel(b);
            pixels[o + 3] = 255;
        }

        // Mirrored, so tiling it across a field has no seams. The noise here is not written to be
        // tileable and does not need to be: mirroring makes every tile edge match its neighbour exactly.
        return Texture.FromPixels(pixels, size, size, "turf", TextureWrap.MirroredRepeat);
    }

    private static byte Channel(float value) => (byte)Math.Clamp(value * 255f + 0.5f, 0f, 255f);

    private static float Fbm(float x, float y, int octaves)
    {
        var sum = 0f;
        var amplitude = 0.5f;
        var total = 0f;

        for (var i = 0; i < octaves; i++)
        {
            sum += Noise(x, y) * amplitude;
            total += amplitude;
            x *= 2.03f;
            y *= 2.03f;
            amplitude *= 0.5f;
        }

        return sum / total;
    }

    private static float Noise(float x, float y)
    {
        var xi = (int)MathF.Floor(x);
        var yi = (int)MathF.Floor(y);
        var xf = Fade(x - xi);
        var yf = Fade(y - yi);

        var a = Hash(xi * 374761 + yi * 668265);
        var b = Hash((xi + 1) * 374761 + yi * 668265);
        var c = Hash(xi * 374761 + (yi + 1) * 668265);
        var d = Hash((xi + 1) * 374761 + (yi + 1) * 668265);

        return float.Lerp(float.Lerp(a, b, xf), float.Lerp(c, d, xf), yf);

        static float Fade(float t) => t * t * (3f - 2f * t);
    }

    /// <summary>A deterministic 0..1 from an integer, so every run builds the same textures.</summary>
    private static float Hash(int value)
    {
        var h = (uint)value;
        h ^= h >> 15;
        h *= 2246822519u;
        h ^= h >> 13;
        h *= 3266489917u;
        h ^= h >> 16;
        return h / (float)uint.MaxValue;
    }
}
