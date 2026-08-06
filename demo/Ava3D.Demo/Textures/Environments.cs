namespace Ava3D.Demo.Textures;

/// <summary>
/// Equirectangular environments, for <see cref="EnvironmentLight.Texture"/> and for the sky spheres that
/// show what a scene is being lit by.
///
/// The layout is the one <see cref="Primitives.Sphere"/> generates and the one the control's environment
/// lighting expects: u runs once round the horizon from +X toward +Z, v runs from straight up at 0 to
/// straight down at 1. One image is therefore both the backdrop and the light, which is the whole reason
/// the reflection in a hull agrees with the sky behind it instead of merely resembling it.
///
/// Raw pixels rather than PNG. Everything else in this folder encodes, because a renderer holds one
/// decoded image at a time and encoded bytes are cheaper to keep around — but an environment map is
/// prefiltered on the CPU the moment it is set, so encoding it would only mean decoding it again a
/// millisecond later.
/// </summary>
public static class Environments
{
    /// <summary>
    /// A photographer's studio: a dark room with six softboxes around it and a broad panel overhead.
    ///
    /// Rectangles, deliberately. A reflection is only legible when you can tell what is being reflected,
    /// and a hard-edged bright shape is the one thing an eye can follow across a curved surface — which is
    /// what makes the roughness ramp readable. On a polished sphere the boxes appear as boxes; by
    /// roughness 0.4 they are smears; by 0.8 the whole room is one soft gradient. A photograph of a
    /// landscape would light the scene just as correctly and demonstrate nothing.
    ///
    /// The boxes differ in size, brightness and warmth on purpose. Six identical ones would leave nothing
    /// to tell you which way the environment had turned.
    /// </summary>
    public static Texture Studio(int width = 512, int height = 256)
    {
        var pixels = new byte[width * height * 4];

        // Centre u, half width, top v, bottom v, brightness, warmth.
        (float U, float HalfWidth, float Top, float Bottom, float Level, float Warm)[] boxes =
        [
            (0.03f, 0.030f, 0.19f, 0.47f, 1.00f, 0.00f),
            (0.16f, 0.013f, 0.28f, 0.42f, 0.60f, 0.22f),
            (0.30f, 0.022f, 0.24f, 0.46f, 0.78f, 0.05f),
            (0.47f, 0.036f, 0.15f, 0.52f, 0.90f, 0.10f),
            (0.63f, 0.014f, 0.30f, 0.40f, 0.52f, 0.30f),
            (0.80f, 0.026f, 0.21f, 0.49f, 0.84f, 0.00f)
        ];

        for (var y = 0; y < height; y++)
        {
            var v = (y + 0.5f) / height;

            for (var x = 0; x < width; x++)
            {
                var u = (x + 0.5f) / width;

                // The room itself: a grey that falls off from the ceiling down to the floor, with a step
                // at the horizon so a reflection has a line in it to sit on.
                var wall = Lerp(0.085f, 0.030f, Smooth(0.10f, 0.70f, v));
                if (v > 0.615f)
                    wall *= 0.62f;

                var r = wall;
                var g = wall;
                var b = wall * 1.06f;

                // The overhead panel, as a disc round the pole rather than a rectangle: everything meets
                // at the top of an equirectangular image, and a rectangle there would come out as a
                // four-cornered star.
                var overhead = 1f - Smooth(0.015f, 0.085f, v);
                r += overhead * 0.36f;
                g += overhead * 0.36f;
                b += overhead * 0.38f;

                foreach (var box in boxes)
                {
                    // Distance round the circle, so a box straddling u = 0 is one box and not two.
                    var du = MathF.Abs(u - box.U);
                    du = MathF.Min(du, 1f - du);

                    var across = 1f - Smooth(box.HalfWidth * 0.72f, box.HalfWidth, du);
                    var down = Smooth(box.Top - 0.03f, box.Top + 0.02f, v) *
                               (1f - Smooth(box.Bottom - 0.02f, box.Bottom + 0.03f, v));

                    var light = across * down * box.Level;
                    r += light * (1f + box.Warm * 0.35f);
                    g += light;
                    b += light * (1f - box.Warm * 0.45f);
                }

                Write(pixels, (y * width + x) * 4, r, g, b);
            }
        }

        return Texture.FromPixels(pixels, width, height, "studio environment");
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

    private static float Smooth(float edge0, float edge1, float x)
    {
        var t = Math.Clamp((x - edge0) / MathF.Max(edge1 - edge0, 1e-6f), 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    /// <summary>
    /// Writes one texel, encoding to sRGB on the way — the values above are linear light, and a texture
    /// is read back through the same 2.2 the shaders use.
    /// </summary>
    private static void Write(byte[] pixels, int offset, float r, float g, float b)
    {
        pixels[offset] = Byte(r);
        pixels[offset + 1] = Byte(g);
        pixels[offset + 2] = Byte(b);
        pixels[offset + 3] = 255;

        static byte Byte(float linear) =>
            (byte)Math.Clamp(MathF.Pow(Math.Clamp(linear, 0f, 1f), 1f / 2.2f) * 255f + 0.5f, 0f, 255f);
    }
}
