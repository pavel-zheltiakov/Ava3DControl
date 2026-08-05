namespace Ava3D.Demo.Textures;

/// <summary>
/// The maps the orbital scene needs, generated straight into RGBA rather than encoded.
///
/// Every one of these goes through <see cref="Texture.FromPixels"/>, which is the difference this scene
/// exists partly to demonstrate. <see cref="Procedural"/> emits PNG bytes because that is what the older
/// scenes were written against and it costs them nothing — they build their maps once, at startup, and
/// never again. A scene that rebuilds four 512×256 maps on the fly cannot afford a PNG encode in the
/// application followed immediately by a PNG decode in the renderer, and neither can a game generating a
/// new star system mid-jump.
/// </summary>
public static class Space
{
    /// <summary>
    /// The glow every sprite in the scene is drawn from: a radial falloff, white in the middle and warm
    /// at the edge, fading to nothing.
    ///
    /// One texture for the sun's corona, the engine bloom, the beacons and the docking lamps. They differ
    /// in size, tint and opacity, which is exactly the set of things <see cref="SpriteNode"/> makes cheap
    /// to vary — so a dozen distinct lights on screen share one 128×128 image.
    /// </summary>
    public static Texture Glow(int size = 128)
    {
        var pixels = new byte[size * size * 4];
        var centre = (size - 1) * 0.5f;

        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            var dx = (x - centre) / centre;
            var dy = (y - centre) / centre;
            var r = MathF.Min(MathF.Sqrt(dx * dx + dy * dy), 1f);

            // Three stops: white and opaque at the core, warm and half-transparent a third of the way
            // out, amber and gone at the rim.
            var (cr, cg, cb, ca) = r < 0.35f
                ? Mix(r / 0.35f, (255f, 255f, 255f, 1f), (255f, 230f, 170f, 0.55f))
                : Mix((r - 0.35f) / 0.65f, (255f, 230f, 170f, 0.55f), (255f, 200f, 90f, 0f));

            var o = (y * size + x) * 4;
            pixels[o + 0] = (byte)cr;
            pixels[o + 1] = (byte)cg;
            pixels[o + 2] = (byte)cb;
            pixels[o + 3] = (byte)(ca * 255f);
        }

        return Texture.FromPixels(pixels, size, size, "glow");

        static (float, float, float, float) Mix(
            float t, (float R, float G, float B, float A) a, (float R, float G, float B, float A) b)
        {
            t = Math.Clamp(t, 0f, 1f);
            return (a.R + (b.R - a.R) * t,
                    a.G + (b.G - a.G) * t,
                    a.B + (b.B - a.B) * t,
                    a.A + (b.A - a.A) * t);
        }
    }

    /// <summary>
    /// An equirectangular nebula for the sky sphere: a cool gradient with a warm band and a few clouds.
    ///
    /// Deliberately low-contrast. It is there to stop the background being flat black, and anything more
    /// assertive competes with the starfield, which is the thing actually worth looking at.
    ///
    /// The clouds are sampled on a cylinder, and that is the whole difference between a sky and a sky with
    /// a join down it. Value noise asked for <c>u × 6</c> gives unrelated answers at u = 0 and u = 1, so
    /// the left edge of the map and the right edge of the map are two different pieces of cloud — and a
    /// sky sphere wraps that map round the camera, which butts them against each other along the prime
    /// meridian. From inside, that meridian is a great circle: a dead straight line clean across the
    /// frame, at whatever angle the camera happens to be holding, with the cloud stepping as it crosses.
    /// Feeding the noise <c>(cos θ, sin θ)</c> instead makes u genuinely periodic — θ = 0 and θ = 2π are
    /// the same point in noise space, so there is nothing to butt together. <see cref="Planet"/> was
    /// always sampled this way; the sky was not, and the sky is the one you are looking at through the
    /// whole film.
    /// </summary>
    public static Texture Nebula(int width = 512, int height = 256)
    {
        var pixels = new byte[width * height * 4];

        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var u = x / (float)width;
            var v = y / (float)height;
            var angle = u * MathF.Tau;
            var around = MathF.Cos(angle);
            var along = MathF.Sin(angle);

            // A vertical gradient from deep blue at the poles to a slightly warmer equator.
            var band = 1f - MathF.Abs(v - 0.5f) * 2f;
            var r = 0.03f + 0.05f * band;
            var g = 0.04f + 0.05f * band;
            var b = 0.08f + 0.07f * band;

            var cloud = Fbm(around * 2.4f + 5f, along * 2.4f + v * 4f, 4);
            var magenta = Math.Clamp((cloud - 0.55f) * 2.4f, 0f, 1f);
            r += magenta * 0.16f;
            g += magenta * 0.05f;
            b += magenta * 0.20f;

            var teal = Math.Clamp(
                (Fbm(around * 1.7f + 31f, along * 1.7f + v * 3f + 17f, 3) - 0.60f) * 2.2f, 0f, 1f);
            g += teal * 0.10f;
            b += teal * 0.12f;

            Write(pixels, (y * width + x) * 4, r, g, b, 1f);
        }

        return Texture.FromPixels(pixels, width, height, "nebula");
    }

    /// <summary>
    /// The planet's three maps, generated together from one height field.
    ///
    /// Together because they have to agree: the coastline in the albedo is the same contour as the one in
    /// the height, and the cities sit on the land rather than in the sea. Three maps generated
    /// independently read as three overlays that happen to share a sphere.
    /// </summary>
    public static (Texture Albedo, Texture Bump, Texture Lights) Planet(int width = 512, int height = 256)
    {
        var albedo = new byte[width * height * 4];
        var bump = new byte[width * height * 4];
        var lights = new byte[width * height * 4];

        for (var y = 0; y < height; y++)
        {
            var v = y / (float)(height - 1);
            // Latitude drives the ice caps and the equatorial desert band.
            var polar = MathF.Abs(v - 0.5f) * 2f;

            for (var x = 0; x < width; x++)
            {
                var u = x / (float)width;

                // Sampled on a cylinder so the left and right edges meet — a seam down the prime meridian
                // is the one artefact an equirectangular map cannot hide.
                var angle = u * MathF.Tau;
                var elevation = Fbm(MathF.Cos(angle) * 3f + 8f, MathF.Sin(angle) * 3f + v * 5f, 5);

                var land = elevation > 0.52f;
                var o = (y * width + x) * 4;

                float r, g, b;
                if (polar > 0.86f)
                {
                    // Ice, with the height field showing through as crevasses.
                    var ice = 0.78f + elevation * 0.18f;
                    (r, g, b) = (ice, ice * 1.01f, ice * 1.05f);
                }
                else if (land)
                {
                    var altitude = (elevation - 0.52f) / 0.48f;
                    var desert = Math.Clamp(1f - polar * 1.6f, 0f, 1f);
                    r = 0.20f + altitude * 0.30f + desert * 0.16f;
                    g = 0.24f + altitude * 0.22f + desert * 0.08f;
                    b = 0.16f + altitude * 0.16f;
                }
                else
                {
                    var depth = (0.52f - elevation) / 0.52f;
                    r = 0.02f + (1f - depth) * 0.04f;
                    g = 0.06f + (1f - depth) * 0.10f;
                    b = 0.14f + (1f - depth) * 0.16f;
                }

                Write(albedo, o, r, g, b, 1f);

                // The bump map is the height field itself — that is the whole point of BumpTexture over
                // NormalTexture here, and it is why a sphere's poles come out clean.
                Write(bump, o, elevation, elevation, elevation, 1f);

                // Cities: clustered on temperate land, and sparse enough to read as points rather than a
                // wash. The second noise field is what breaks them into clusters.
                var populated = land && polar < 0.80f;
                var density = populated ? Fbm(MathF.Cos(angle) * 9f + 51f, MathF.Sin(angle) * 9f + v * 11f, 3) : 0f;
                var city = populated && density > 0.62f && Hash(x * 7919 + y * 104729) > 0.86f
                    ? Math.Clamp((density - 0.62f) * 4f, 0f, 1f)
                    : 0f;

                Write(lights, o, city, city * 0.86f, city * 0.55f, 1f);
            }
        }

        return (Texture.FromPixels(albedo, width, height, "planet-albedo"),
                Texture.FromPixels(bump, width, height, "planet-bump"),
                Texture.FromPixels(lights, width, height, "planet-lights"));
    }

    /// <summary>A panelled hull plate: dark metal with seams and a little grime in the corners.</summary>
    public static Texture Hull(int size = 256)
    {
        var pixels = new byte[size * size * 4];

        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            var u = x / (float)size;
            var v = y / (float)size;

            var seam = Seam(u, 8f) * Seam(v, 6f);
            var grime = Fbm(u * 12f, v * 12f, 3) * 0.18f;

            var shade = 0.22f + grime * 0.6f + (1f - seam) * -0.10f;
            Write(pixels, (y * size + x) * 4, shade * 0.92f, shade * 0.97f, shade * 1.08f, 1f);
        }

        return Texture.FromPixels(pixels, size, size, "hull");

        // 1 inside a panel, falling to 0 in the groove between panels.
        static float Seam(float t, float panels)
        {
            var f = MathF.Abs(t * panels % 1f - 0.5f) * 2f;
            return Math.Clamp((1f - f) * 12f, 0f, 1f);
        }
    }

    /// <summary>
    /// Station plating: a panelled skin and the height field that goes with it.
    ///
    /// Two maps rather than one, because a seam you can only see is a drawing and a seam that also
    /// catches the light is a joint. The albedo carries the colour variation between plates and the
    /// grime that collects in the grooves; the bump carries the grooves themselves, the rivet lines
    /// along them, and the few plates that stand proud of their neighbours.
    ///
    /// The albedo sits around 0.8 rather than around 0.2. It multiplies the material's own base colour,
    /// so a map centred on middle grey would halve everything it was put on; centred near white it
    /// modulates instead, which is what a detail map is for.
    /// </summary>
    public static (Texture Albedo, Texture Bump) Plating(int size = 512)
    {
        var albedo = new byte[size * size * 4];
        var bump = new byte[size * size * 4];

        // Panels on a 6×6 grid, with every other row offset by half a plate so the seams do not all
        // line up into one long cross — which is the difference between plating and graph paper.
        const float Plates = 6f;

        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            var u = x / (float)size;
            var v = y / (float)size;

            var row = (int)MathF.Floor(v * Plates);
            var shifted = u + (row % 2 == 0 ? 0f : 0.5f / Plates);
            var column = (int)MathF.Floor(shifted * Plates);

            // How far into the plate this pixel is, 0 at the seam and 1 well inside it.
            var inset = MathF.Min(
                Edge(shifted * Plates - MathF.Floor(shifted * Plates)),
                Edge(v * Plates - row));

            var groove = Math.Clamp(inset * 26f, 0f, 1f);

            // A stable per-plate value, so the same plate is the same shade wherever it appears.
            var plate = Hash(column, row);
            var raised = plate > 0.86f ? 0.16f : plate < 0.12f ? -0.10f : 0f;

            var grime = Fbm(u * 14f, v * 14f, 3);
            var streak = Fbm(u * 3f, v * 26f, 2);

            // Rivets: a run of small domes just inside each seam, spaced along it.
            var rivet = Rivets(shifted * Plates, v * Plates);

            var shade = 0.80f + (plate - 0.5f) * 0.16f + (grime - 0.5f) * 0.14f
                      + (streak - 0.5f) * 0.06f - (1f - groove) * 0.30f;

            Write(albedo, (y * size + x) * 4, shade * 0.98f, shade, shade * 1.04f, 1f);

            var height = 0.5f + raised + (groove - 1f) * 0.34f + rivet * 0.22f
                       + (grime - 0.5f) * 0.05f;

            Write(bump, (y * size + x) * 4, height, height, height, 1f);
        }

        return (Texture.FromPixels(albedo, size, size, "plating"),
                Texture.FromPixels(bump, size, size, "plating-bump"));

        // Distance to the nearer edge of the cell this coordinate falls in, 0 at a seam and 0.5 mid-plate.
        static float Edge(float t) => MathF.Min(t, 1f - t);

        static float Rivets(float px, float py)
        {
            // Six rivets a side, set in from the seam by a twentieth of a plate.
            var cx = px - MathF.Floor(px);
            var cy = py - MathF.Floor(py);

            return MathF.Max(Line(cx, cy), Line(cy, cx));

            static float Line(float across, float along)
            {
                var band = MathF.Min(across, 1f - across);
                var step = MathF.Abs(along * 6f % 1f - 0.5f) * 2f;
                var dot = MathF.Sqrt((band - 0.05f) * (band - 0.05f) * 400f + step * step);

                return Math.Clamp(1f - dot * 2.6f, 0f, 1f);
            }
        }

        static float Hash(int x, int y)
        {
            var h = (uint)(x * 374_761_393 + y * 668_265_263);
            h = (h ^ (h >> 13)) * 1_274_126_177u;
            return (h ^ (h >> 16)) / (float)uint.MaxValue;
        }
    }

    /// <summary>
    /// An engine flame, seen side-on: a hot plug at the throat drawn out into a long tapering jet.
    ///
    /// White for the same reason the gate layers are white — the unlit shader multiplies the texel by
    /// <see cref="Material.BaseColor"/>, so one image serves a blue escort, a magenta raider and an
    /// amber freighter, and the tint comes off the bell's own emissive rather than being chosen twice.
    ///
    /// v runs along the jet, 0 at the nozzle. u runs across it, and the silhouette is a function of v:
    /// a slight flare just aft of the throat where the exhaust leaves the bell and stops being confined,
    /// then a taper that never quite reaches zero width before the brightness has gone anyway. Inside
    /// that silhouette the falloff to the edge is smooth, which is what makes a flat card read as
    /// something with a core rather than as a triangle of paint.
    ///
    /// The bands near the throat are shock diamonds. A jet leaving a bell at a pressure that does not
    /// match the outside sets up a standing wave, and the compressions in it are visibly brighter; they
    /// fade out along the jet as the wave dissipates, which is why the exponent is there.
    /// </summary>
    public static Texture Plume(int width = 64, int height = 256)
    {
        var pixels = new byte[width * height * 4];

        for (var y = 0; y < height; y++)
        {
            var t = y / (float)(height - 1);

            // The silhouette. Full width at the throat, a flare over the first tenth, then a taper.
            var flare = t < 0.10f ? 0.72f + 2.6f * t : 0.98f * MathF.Pow(1f - (t - 0.10f) / 0.90f, 0.80f);

            // How bright the jet is along its length: a hot plug at the throat over a long tail.
            var along = MathF.Pow(1f - t, 1.15f) * (0.62f + 0.70f * MathF.Exp(-t * t * 70f));

            // Shock diamonds, damped along the jet.
            along *= 1f + 0.30f * MathF.Cos(t * 46f) * MathF.Exp(-t * 7f);

            for (var x = 0; x < width; x++)
            {
                var across = MathF.Abs(x / (float)(width - 1) * 2f - 1f) / MathF.Max(flare, 1e-3f);

                var body = across < 1f ? 1f - across * across : 0f;
                var alpha = Math.Clamp(body * body * along, 0f, 1f);

                Write(pixels, (y * width + x) * 4, 1f, 1f, 1f, alpha);
            }
        }

        return Texture.FromPixels(pixels, width, height, "plume", TextureWrap.ClampToEdge);
    }

    /// <summary>
    /// The same flame seen up the pipe: a disc, hottest in the middle.
    ///
    /// A flame card is a card, and a card turned edge-on is a line. The chase shot sits behind
    /// <i>Kestrel</i> and looks very nearly straight up its three bells, which is exactly the angle at
    /// which a billboarded jet has nothing to show — so there is a second piece of geometry lying in the
    /// plane of the nozzles that is invisible from the side and is the whole exhaust from dead astern.
    /// Between the two angles both contribute, which is what stops either of them being noticed.
    /// </summary>
    public static Texture PlumeCap(int size = 128)
    {
        var pixels = new byte[size * size * 4];
        var centre = (size - 1) * 0.5f;

        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            var dx = (x - centre) / centre;
            var dy = (y - centre) / centre;
            var r = MathF.Sqrt(dx * dx + dy * dy);

            // A bright core out to a third of the radius, then a soft falloff to nothing at the rim.
            var alpha = r >= 1f ? 0f : MathF.Pow(1f - r * r, 2.6f) * (0.55f + 0.45f * MathF.Exp(-r * r * 7f));

            Write(pixels, (y * size + x) * 4, 1f, 1f, 1f, Math.Clamp(alpha, 0f, 1f));
        }

        return Texture.FromPixels(pixels, size, size, "plume-cap", TextureWrap.ClampToEdge);
    }

    /// <summary>
    /// The swirl inside a docking gate: filaments spiralling into a bright core, transparent past the
    /// rim.
    ///
    /// White, deliberately — the film tints it red or green through <see cref="Material.BaseColor"/>,
    /// which the unlit shader multiplies straight into the texel, so one image is both states and the
    /// gate can cross-fade between them by moving four floats.
    ///
    /// And one image is all there can be. The obvious way to animate a portal is a strip of frames
    /// swapped into <see cref="Material.BaseColorTexture"/> each tick, and it does not work here: the
    /// texture caches release anything that did not appear in the frame just drawn, so a twenty-frame
    /// cycle would delete and re-upload nineteen textures every frame. The film's own rule covers this
    /// — animate a transform, an opacity or a colour, never the data — so the gate is two copies of
    /// this one texture counter-rotating at different rates, which is where the motion comes from.
    /// </summary>
    public static Texture PortalSwirl(int size = 256)
    {
        var pixels = new byte[size * size * 4];
        var centre = (size - 1) * 0.5f;

        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            var dx = (x - centre) / centre;
            var dy = (y - centre) / centre;
            var r = MathF.Sqrt(dx * dx + dy * dy);
            var i = (y * size + x) * 4;

            if (r >= 1f)
                continue;

            // Filaments that curve as they run out from the middle. Sampling the angular noise at
            // `angle + r * k` rather than at `angle` is the whole of the spiral: the same spokes,
            // rotated further the further out they are.
            var filament = Spokes(MathF.Atan2(dy, dx) + r * 3.6f, 44);
            filament = MathF.Pow(filament, 2.4f);

            var core = MathF.Exp(-r * r * 4.5f);
            var edge = MathF.Exp(-((r - 0.90f) * (r - 0.90f)) / 0.0026f);

            // Feathered rather than cut off. A hard circular edge on an alpha-blended quad reads as a
            // decal stuck to the door; two pixels of falloff reads as light.
            var fade = Math.Clamp((1f - r) / 0.05f, 0f, 1f);

            var value = 0.16f + 0.55f * filament + 0.80f * core + 0.85f * edge;
            var alpha = (0.14f + 0.52f * filament + 0.70f * core + 0.80f * edge) * fade;

            Write(pixels, i, value, value, value, alpha);
        }

        return Texture.FromPixels(pixels, size, size, "portal-swirl");
    }

    /// <summary>
    /// The plate the gate closes with: a filled disc, opaque nearly to its edge, with a slow ripple
    /// across it so it is a held field rather than a painted lid.
    ///
    /// The swirl on its own cannot shut a door. Its alpha runs from about a fifth to a half across the
    /// middle of the disc, because that is what makes it read as filaments in space, and a surface that
    /// is thirty per cent opaque shows thirty per cent of whatever is behind it — which here is a hall
    /// lit by unlit emissive panels, so the fittings came through the shut red gate like a room seen
    /// through smoked glass. This is the layer that says no, and it is the one the film fades out to
    /// open the bay.
    /// </summary>
    public static Texture PortalPlate(int size = 256)
    {
        var pixels = new byte[size * size * 4];
        var centre = (size - 1) * 0.5f;

        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            var dx = (x - centre) / centre;
            var dy = (y - centre) / centre;
            var r = MathF.Sqrt(dx * dx + dy * dy);

            if (r >= 1f)
                continue;

            // Concentric standing waves, and a little angular unevenness so the pattern is not a
            // bullseye. Both only shade the plate: the alpha stays near one until the very edge.
            var wave = 0.5f + 0.5f * MathF.Cos(r * 26f);
            var uneven = Spokes(MathF.Atan2(dy, dx), 18) * 0.16f;
            var value = 0.30f + 0.22f * wave + uneven + 0.45f * MathF.Exp(-r * r * 3.2f);

            Write(pixels, (y * size + x) * 4, value, value, value,
                  Math.Clamp((0.97f - r) / 0.04f, 0f, 1f));
        }

        return Texture.FromPixels(pixels, size, size, "portal-plate");
    }

    /// <summary>
    /// One ring of the gate: a bright annulus near the rim and nothing else. Three of these scaled out
    /// and faded on offset phases are the rings travelling up a teleport pad, and they cost three
    /// transforms and three opacities a frame.
    /// </summary>
    public static Texture PortalRing(int size = 256)
    {
        var pixels = new byte[size * size * 4];
        var centre = (size - 1) * 0.5f;

        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            var dx = (x - centre) / centre;
            var dy = (y - centre) / centre;
            var r = MathF.Sqrt(dx * dx + dy * dy);

            if (r >= 1f)
                continue;

            var band = MathF.Exp(-((r - 0.84f) * (r - 0.84f)) / 0.0016f);
            var wash = MathF.Exp(-((r - 0.84f) * (r - 0.84f)) / 0.030f) * 0.22f;
            var fade = Math.Clamp((1f - r) / 0.05f, 0f, 1f);

            Write(pixels, (y * size + x) * 4, 0.55f + band, 0.55f + band, 0.55f + band,
                  (band + wash) * fade);
        }

        return Texture.FromPixels(pixels, size, size, "portal-ring");
    }

    /// <summary>
    /// Smooth 0..1 noise around a circle, in `count` buckets, wrapping cleanly at the seam.
    ///
    /// Hashing the angle directly is the one-liner and it aliases into static: every pixel gets an
    /// independent value and a 256-pixel disc holds far more angles than it has pixels to resolve.
    /// Bucketing and interpolating gives filaments with a width instead.
    /// </summary>
    private static float Spokes(float angle, int count)
    {
        var t = (angle / MathF.Tau + 4f) * count;
        var bucket = (int)MathF.Floor(t);
        var f = t - bucket;
        f = f * f * (3f - 2f * f);

        var a = Hash(bucket % count * 374761393);
        var b = Hash((bucket + 1) % count * 374761393);
        return a + (b - a) * f;
    }

    private static void Write(byte[] pixels, int offset, float r, float g, float b, float a)
    {
        pixels[offset + 0] = Channel(r);
        pixels[offset + 1] = Channel(g);
        pixels[offset + 2] = Channel(b);
        pixels[offset + 3] = Channel(a);

        static byte Channel(float value) => (byte)Math.Clamp(value * 255f, 0f, 255f);
    }

    /// <summary>Value noise over several octaves, which is enough shape for a planet at this distance.</summary>
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

        var a = Hash(xi * 374761393 + yi * 668265263);
        var b = Hash((xi + 1) * 374761393 + yi * 668265263);
        var c = Hash(xi * 374761393 + (yi + 1) * 668265263);
        var d = Hash((xi + 1) * 374761393 + (yi + 1) * 668265263);

        return float.Lerp(float.Lerp(a, b, xf), float.Lerp(c, d, xf), yf);

        static float Fade(float t) => t * t * (3f - 2f * t);
    }

    /// <summary>A deterministic 0..1 from an integer, so every run of the demo builds the same planet.</summary>
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
