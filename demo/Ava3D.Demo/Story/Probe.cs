using System.Numerics;

namespace Ava3D.Demo.Story;

/// <summary>
/// A light probe, baked: an equirectangular image of what a room looks like from one point in it, for the
/// metals in that room to reflect.
///
/// It exists because of one sentence in the plan — <i>the chrome shows the room you are standing in, so a
/// wrong reflection is wrong rather than merely unfamiliar</i> — and because there is no way to deliver
/// that sentence otherwise. A metal has no colour of its own; it is only what it reflects, and what it
/// reflects is <see cref="EnvironmentLight"/>. Give the gallery the two-colour hemisphere every other room
/// uses and five spheres of ascending roughness all show the same vertical gradient, blurred five ways,
/// which demonstrates nothing at all. The exhibit needs an image or it is not an exhibit.
///
/// This library has no offscreen render target, so the image cannot be rendered — it is traced. Six planes,
/// a ray a texel, the lamps as discs at their own angular size, and the doorway as a hole. That is a
/// smaller renderer than the one in <c>Rendering/</c> by several orders of magnitude and it is enough,
/// because a reflection is read for its <i>shape</i>: what makes the roughness ramp legible is that the
/// polished sphere shows five separate lamps and the matte one shows one smear, and both of those are true
/// of a traced room.
///
/// What it is not is a lie about the room. The box it traces is the gallery's own extent, the lamps are at
/// the gallery's own lamp positions, and the dark patch is where the gallery's own door is — so when the
/// visitor moves and the reflection does not, the thing that is stale is the parallax and not the contents.
/// Standing at the far end where the exhibit is, the parallax is small; walking the length of the room with
/// the probe baked at the near end would not be, which is why it is baked from where he stands to look.
/// </summary>
internal static class Probe
{
    /// <summary>How the brightness of a surface falls with how far away it is. Not physics — the walls are
    /// lit by lamps this trace does not model — but a long corridor reflecting as a dark tunnel and a near
    /// wall reflecting as a bright one is what the room actually looks like.</summary>
    private const float Falloff = 0.17f;

    /// <summary>
    /// Traces the room from a point inside it.
    /// </summary>
    /// <param name="from">Where the probe is baked, in the same frame as everything else here.</param>
    /// <param name="min">One corner of the room's inside.</param>
    /// <param name="max">The other.</param>
    /// <param name="lamps">Where the lamps are. Drawn at the angular size a lamp of this room is.</param>
    /// <param name="doorway">The middle of the opening in the wall, which reflects as a hole.</param>
    public static Texture Bake(
        Vector3 from,
        Vector3 min,
        Vector3 max,
        IReadOnlyList<Vector3> lamps,
        Vector3 doorway,
        int width = 256,
        int height = 128)
    {
        var pixels = new byte[width * height * 4];

        // Precomputed once rather than per texel: a direction and how wide the thing is from here. The
        // angular radius is what makes near lamps big and far ones small, which is the only cue in the
        // image that the room has length.
        var beacons = new (Vector3 Direction, float Cosine, float Edge)[lamps.Count];

        for (var i = 0; i < lamps.Count; i++)
            beacons[i] = Beacon(lamps[i] - from, 0.34f);

        var hole = Beacon(doorway - from, 0.95f);

        for (var y = 0; y < height; y++)
        {
            // v = 0 is straight up, v = 1 straight down. Half a texel in, so the poles are sampled at the
            // middle of the top and bottom rows rather than exactly at the singularity.
            var theta = (y + 0.5f) / height * MathF.PI;
            var cosTheta = MathF.Cos(theta);
            var sinTheta = MathF.Sin(theta);

            for (var x = 0; x < width; x++)
            {
                // u runs once round the horizon from +X toward +Z, which is the layout the control's
                // environment lookup and Primitives.Sphere both use.
                var phi = (x + 0.5f) / width * MathF.Tau;
                var direction = new Vector3(sinTheta * MathF.Cos(phi), cosTheta, sinTheta * MathF.Sin(phi));

                var (level, distance) = Surface(from, direction, min, max);

                // The doorway, subtracted before the lamps are added, because a lamp in front of a dark
                // opening is still a lamp and a dark opening over a lamp is a mistake.
                var opening = Fall(Vector3.Dot(direction, hole.Direction), hole.Cosine, hole.Edge);
                level *= 1f - opening * 0.88f;

                var r = level;
                var g = level * 0.99f;
                var b = level * 0.97f;

                foreach (var beacon in beacons)
                {
                    var lit = Fall(Vector3.Dot(direction, beacon.Direction), beacon.Cosine, beacon.Edge);
                    if (lit <= 0f)
                        continue;

                    r += lit * 1.55f;
                    g += lit * 1.33f;
                    b += lit * 1.02f;
                }

                // The floor gets a little of the lamps back, so the bottom of a polished sphere is not a
                // black cap. It is the one term here that is standing in for a bounce rather than tracing
                // anything, and it is worth the two lines: a mirror with a black underside reads as a
                // sphere floating over a hole.
                if (direction.Y < 0f)
                {
                    var bounce = -direction.Y * 0.055f / (1f + distance * Falloff);
                    r += bounce;
                    g += bounce * 0.95f;
                    b += bounce * 0.86f;
                }

                Write(pixels, (y * width + x) * 4, r, g, b);
            }
        }

        return Texture.FromPixels(pixels, width, height, "gallery probe");
    }

    /// <summary>A direction, and the cosines that bound a disc of the given radius at that distance.</summary>
    private static (Vector3 Direction, float Cosine, float Edge) Beacon(Vector3 offset, float radius)
    {
        var distance = MathF.Max(offset.Length(), 0.05f);
        var half = MathF.Atan2(radius, distance);

        return (offset / distance, MathF.Cos(half), MathF.Cos(half * 0.45f));
    }

    /// <summary>One inside the disc, zero outside it, smooth across the rim.</summary>
    private static float Fall(float cosine, float outer, float inner)
    {
        if (cosine <= outer)
            return 0f;

        if (cosine >= inner)
            return 1f;

        var t = (cosine - outer) / MathF.Max(inner - outer, 1e-6f);
        return t * t * (3f - 2f * t);
    }

    /// <summary>
    /// Which of the room's six surfaces a ray leaves by, how bright it is, and how far away it was.
    ///
    /// The slab method, from a point known to be inside: every axis gives the distance at which the ray
    /// crosses that pair of planes in the direction it is going, and the nearest of the three is the one it
    /// actually hits. No hit test is needed and no surface can be missed, which is the whole reason a box
    /// is the right room to trace.
    /// </summary>
    private static (float Level, float Distance) Surface(Vector3 from, Vector3 direction, Vector3 min, Vector3 max)
    {
        var t = float.MaxValue;
        var axis = 0;

        for (var i = 0; i < 3; i++)
        {
            var d = Component(direction, i);
            if (MathF.Abs(d) < 1e-5f)
                continue;

            var plane = d > 0f ? Component(max, i) : Component(min, i);
            var hit = (plane - Component(from, i)) / d;

            if (hit <= 0f || hit >= t)
                continue;

            t = hit;
            axis = i;
        }

        if (t == float.MaxValue)
            return (0f, 0f);

        // Plaster overhead and on the walls, dark tile underfoot. The floor is a fifth of the wall and that
        // is not artistic licence — it is what Fabric.Tile is next to Fabric.Plaster.
        var level = axis == 1
            ? direction.Y > 0f ? 0.140f : 0.050f
            : 0.115f;

        return (level / (1f + (t * Falloff) * (t * Falloff)), t);
    }

    private static float Component(Vector3 v, int axis) => axis == 0 ? v.X : axis == 1 ? v.Y : v.Z;

    /// <summary>Writes one texel, encoding to sRGB — the values above are linear light and a texture is
    /// read back through the same 2.2 the shaders use.</summary>
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
