using System.Numerics;

namespace Ava3D.Demo.Story;

/// <summary>
/// How much of the material model a room's surfaces are using.
///
/// This is the building's second argument, running under the first one, and it is made by walking rather
/// than by being stated. The antechamber is flat colour and two numbers — which is a complete PBR material,
/// and the demo has always said so. The passage adds a base-colour map, and in a dark corridor you barely
/// register it: the wall is not quite one colour any more. The rotunda adds roughness, so the plaster
/// catches the lanterns unevenly. The material gallery adds a normal map, which is the first surface in the
/// film with relief that is not geometry. The lounge has all of it and is panelled rather than plastered,
/// and by then it is obvious that the finish has been climbing since the first room.
///
/// One axis, five stops, and every room states its own at the top of its constructor. That is the whole
/// point of it being a parameter: the ladder is legible in five lines spread across five files, and no
/// room can drift up or down it by accident.
/// </summary>
internal enum Grade
{
    /// <summary>Colour, metallic, roughness. No images at all.</summary>
    Flat,

    /// <summary>A base-colour map. The surface stops being one colour.</summary>
    Grained,

    /// <summary>And a roughness map. The surface stops taking light evenly.</summary>
    Worked,

    /// <summary>And a normal map. The surface stops being flat.</summary>
    Dressed,

    /// <summary>And occlusion, on panelling rather than plaster.</summary>
    Full
}

/// <summary>
/// The building's skins: the same handful of surfaces, at whatever <see cref="Grade"/> the room asks for.
///
/// Every map is generated once, on first use, and shared by identity from then on — which is the only
/// reason this is affordable. A <see cref="Texture"/> is uploaded per instance, so a hundred wall slabs
/// holding a hundred equal-but-separate images would be a hundred uploads; a hundred holding the same
/// instance is one. The <see cref="Lazy{T}"/> fields below are doing that job, and they are also what keeps
/// the cost off the standalone scene list: switch the story off and not one of these is ever built.
///
/// <see cref="Pitch"/> is the number all of them are drawn to. Every image below is one and six tenths of a
/// metre of real surface, and <see cref="Fabric.Slab"/> maps texture coordinates out of world positions at
/// that scale — so a tile is a tile whatever the wall it is on is shaped like, and two pieces of the same
/// wall have their grids in step. Authoring to one pitch is what makes that possible; the alternative is a
/// scale factor at every call site and a building whose panels are subtly different sizes in every room.
/// </summary>
internal static class Finish
{
    /// <summary>Metres of real surface one image covers. Everything in the building's fabric is drawn to
    /// it.</summary>
    public const float Pitch = 1.6f;

    /// <summary>
    /// The same thing for furniture, which is looked at from a metre rather than from four.
    ///
    /// One pitch for the whole building was the first version of this and it was wrong in a way that only
    /// showed up on the armchair: a leather grain drawn at one image per metre and six tenths has pebbles
    /// four centimetres across, which is not leather, it is camouflage. The wall scale and the arm's-length
    /// scale are genuinely different numbers and pretending otherwise costs one optional argument on
    /// <see cref="Fabric.Map"/> and buys every soft surface in the lounge.
    /// </summary>
    public const float Close = 0.4f;

    /// <summary>And closer still, for a weave. A thread is a millimetre and a bean bag is a metre.</summary>
    public const float Snug = 0.22f;

    private const int Fine = 128;
    private const int Detailed = 192;

    // ---- plaster -------------------------------------------------------------------------------------

    private static readonly Lazy<Texture> PlasterColour = new(() =>
        Grain.Colour(Fine, "plaster.colour", (u, v) => new Vector3(0.84f + 0.16f * Skin(u, v))));

    private static readonly Lazy<Texture> PlasterRough = new(() =>
        Grain.Rough(Fine, "plaster.rough", (u, v) => new Vector2(0.90f + 0.10f * (1f - Skin(u, v)), 1f)));

    /// <summary>
    /// The stipple only, and it has to be a stipple.
    ///
    /// <see cref="Skin"/> has two octaves in it and only one of them belongs in a normal map. The coarse one
    /// is a thirty-centimetre undulation, which is a real thing about a plastered wall and is a thing about
    /// its <i>colour</i> — where the skim went on thicker it is a shade different. Putting it in the relief
    /// instead says the wall bulges by a millimetre over thirty centimetres, which is far too gentle to see.
    ///
    /// <b>The first version of this was neither.</b> It ran the fine octave at thirty cycles an image, which
    /// is a feature five centimetres across, and gave it a millimetre of depth — then halved that again with
    /// a normal scale of seven tenths. Five centimetres and one millimetre is a two per cent slope: not a
    /// stipple, not an undulation, and about half a degree of normal, which is nothing. The material gallery
    /// is the room the whole surface ladder climbs to and it rendered as flat beige paint, which is what it
    /// looks like when the map that is supposed to be the point of the room is below the threshold of being
    /// seen at all. Twice the frequency and four times the depth is a two-centimetre stipple at four
    /// millimetres — about ten degrees under a raking lamp, which is a trowelled wall.
    /// </summary>
    private static readonly Lazy<Texture> PlasterBumps = new(() =>
        Grain.Bumps(Detailed, "plaster.bumps", (u, v) => Grain.Fbm(u, v, 62, 59, 3), 0.0038f, Pitch));

    // ---- tile ----------------------------------------------------------------------------------------

    private static readonly Lazy<Texture> TileColour = new(() =>
        Grain.Colour(Detailed, "tile.colour", (u, v) =>
        {
            var face = Slab(u, v, out var cell);

            // Per-tile tone, which is most of what stops a tiled floor reading as wallpaper. Real tiles
            // come out of the kiln slightly different from each other and a floor without that variation
            // is the single clearest tell of a generated texture.
            var tone = 0.88f + 0.24f * cell;

            return new Vector3((0.52f + 0.48f * face) * tone * (0.94f + 0.10f * Grain.Fbm(u, v, 24, 41, 3)));
        }));

    private static readonly Lazy<Texture> TileRough = new(() =>
        Grain.Rough(Detailed, "tile.rough", (u, v) =>
        {
            var face = Slab(u, v, out _);

            // Grout is matt and the tile is not, which is the whole reason a roughness map earns its
            // upload here: under a ceiling lamp the difference is a grid of highlights that stops at the
            // joints, and that grid is what says "floor" from across a room.
            return new Vector2(1f - 0.42f * face, 1f);
        }));

    private static readonly Lazy<Texture> TileBumps = new(() =>
        Grain.Bumps(
            Detailed, "tile.bumps",
            (u, v) => Slab(u, v, out _) * 0.85f + Grain.Fbm(u, v, 32, 19, 2) * 0.15f,
            0.0035f, Pitch));

    // ---- panelling -----------------------------------------------------------------------------------

    private static readonly Lazy<Texture> PanelColour = new(() =>
        Grain.Colour(Detailed, "panel.colour", (u, v) =>
        {
            var (face, rivet, cell) = Panel(u, v);
            var tone = 0.93f + 0.11f * cell;

            // The gutters are three quarters of the face rather than a third of it. At a third the wall
            // reads as a bank of lockers — the grid becomes the subject, and a room is not supposed to
            // have a subject. What is wanted is a wall you would have to look at twice to notice was made
            // of sheets, and the occlusion map is where the joint is allowed to be dark.
            return new Vector3((0.74f + 0.26f * face) * tone + rivet * 0.09f);
        }));

    private static readonly Lazy<Texture> PanelRough = new(() =>
        Grain.Rough(Detailed, "panel.rough", (u, v) =>
        {
            var (face, rivet, _) = Panel(u, v);

            // The gutters are dull and unpainted and the rivets are bare, so metallic climbs where the
            // paint is thin. Metallic as a blend rather than a flag is exactly what a worn painted panel
            // needs, and this is the map that says so.
            return new Vector2(
                (1f - 0.34f * face) * (0.94f + 0.12f * Grain.Fbm(u, v, 40, 63, 2)),
                0.24f + 0.46f * (1f - face) + rivet * 0.9f);
        }));

    private static readonly Lazy<Texture> PanelBumps = new(() =>
        Grain.Bumps(Detailed, "panel.bumps", (u, v) =>
        {
            var (face, rivet, _) = Panel(u, v);
            return face * 0.75f + rivet * 0.25f;
        }, 0.005f, Pitch));

    private static readonly Lazy<Texture> PanelMask = new(() =>
        Grain.Mask(Detailed, "panel.ao", (u, v) =>
        {
            var (face, _, _) = Panel(u, v);
            return 0.52f + 0.48f * face;
        }));

    // ---- composite: the illuminator's fit-out ----------------------------------------------------------

    private static readonly Lazy<Texture> CompositeColour = new(() =>
        Grain.Colour(Detailed, "composite.colour", (u, v) =>
            // Almost nothing, and the almost is the whole material. A moulded shell is one piece with one
            // colour; what varies across it is the thickness of the gel coat, which is a drift of about a
            // percent over half a metre and reads as depth rather than as pattern.
            new Vector3(0.985f + 0.015f * Grain.Fbm(u, v, 3, 91, 3))));

    private static readonly Lazy<Texture> CompositeRough = new(() =>
        Grain.Rough(Detailed, "composite.rough", (u, v) =>
        {
            // The cloth under the coat. Two frequencies of the same field — a coarse one for the tow and a
            // fine one for the filaments — moving the gloss and nothing else, because a laid-up laminate
            // has no colour variation at all and the only thing that gives away what is under the surface
            // is the way the highlight crawls when you move.
            var tow = Grain.Fbm(u, v, 14, 41, 2);
            var filament = Grain.Fbm(u * 4f, v * 4f, 120, 47, 2);

            // Between a third and a half, and it started at a fifth. A gel coat really is glossier than
            // that, and a fifth is what it took to find out what glossy costs in a renderer with one
            // directional light and no shadows: at grazing incidence the Fresnel term goes to one whatever
            // the base colour is, so every ceiling beam and every wall seen edge-on came back as a white
            // streak with the normal map boiling inside it. Roughness is the only control that touches
            // that, because it is the only one that widens the lobe instead of dimming it.
            return new Vector2(0.36f + 0.11f * tow + 0.06f * filament, 0.03f);
        }));

    private static readonly Lazy<Texture> CompositeBumps = new(() =>
        Grain.Bumps(Detailed, "composite.bumps", (u, v) =>
            Grain.Fbm(u, v, 14, 41, 2) * 0.7f + Grain.Fbm(u * 4f, v * 4f, 120, 47, 2) * 0.3f,
            0.0006f, Pitch));

    // ---- poured: the gallery deck ----------------------------------------------------------------------

    private static readonly Lazy<Texture> PouredColour = new(() =>
        Grain.Colour(Detailed, "poured.colour", (u, v) =>
            new Vector3(0.978f + 0.022f * Grain.Fbm(u, v, 4, 13, 3))));

    private static readonly Lazy<Texture> PouredRough = new(() =>
        Grain.Rough(Detailed, "poured.rough", (u, v) =>
            // A resin floor is polished and it is not polished evenly: the sheen wanders in patches a
            // couple of metres across, which is the one thing that stops a glossy floor reading as a
            // mirror somebody forgot to texture.
            new Vector2(0.16f + 0.16f * Grain.Fbm(u, v, 6, 67, 3), 0.06f)));

    private static readonly Lazy<Texture> PouredBumps = new(() =>
        Grain.Bumps(Detailed, "poured.bumps", (u, v) =>
            Grain.Fbm(u, v, 26, 71, 3), 0.0004f, Pitch));

    // ---- pools: what a painted light has instead of an edge ---------------------------------------------

    private static readonly Lazy<Texture> PoolFace = new(() =>
        Grain.Colour(128, "pool.face", (u, v) =>
        {
            var x = (u - 0.5f) * 2f;
            var y = (v - 0.5f) * 2f;
            var r = MathF.Min(MathF.Sqrt(x * x + y * y), 1f);

            // Smoothstep in from the rim, then squared. The square is the important half: a linear ramp
            // still has a visible start, and what has to disappear here is not the brightness at the edge
            // but the fact that there is an edge.
            var fall = 1f - r;
            fall = fall * fall * (3f - 2f * fall);

            return new Vector3(fall * fall);
        }));

    /// <summary>
    /// A soft blob, and the single most useful texture in the film.
    ///
    /// Everywhere this renderer has to draw light lying on a surface — the spill through the gallery
    /// window, the console's kick light on the deck, the reflection of the ceiling lamps, the pool under
    /// the plot table — the thing being drawn is an additive quad, and an additive quad has a hard straight
    /// edge. That edge is the entire difference between "light on a floor" and "somebody painted a white
    /// rectangle on the floor", and every one of those surfaces read as the second thing until this
    /// existed.
    ///
    /// It is a luminance falloff and not an alpha one, which matters for the unlit path: an unlit fragment
    /// is <c>baseColor × baseColorTexture</c> and nothing else, so a texture that goes to black at the rim
    /// takes the quad to black at the rim, and black added to anything is what it already was.
    /// </summary>
    public static Texture Pool() => PoolFace.Value;

    private static readonly Lazy<Texture> ShaftFace = new(() =>
        Grain.Colour(128, "shaft.face", (u, v) =>
        {
            // Across the bar: a parabola squared, which is <see cref="PoolFace"/>'s trick in one dimension —
            // no brightness at the rim and, more to the point, no rim.
            var across = (u - 0.5f) * 2f;
            var edge = MathF.Max(0f, 1f - across * across);

            // And along it, from the lens outward. This is the whole difference between a beam and a bar:
            // what is being drawn is light scattered out of the shaft by the air it is crossing, and there
            // is less of it left the further it has come. Every photograph of one of these shows the same
            // thing — a beam that is bright at the ball and has dissolved long before it arrives, with the
            // patch it makes sitting on the wall on its own.
            var along = MathF.Pow(1f - v, 2.4f);

            return new Vector3(edge * edge * along);
        }));

    /// <summary>
    /// A beam of light seen side-on: soft across, and dissolving along.
    ///
    /// Mapped so that <c>v = 0</c> is the end at the source. A shaft carrying <see cref="Pool"/> instead —
    /// which is what this had first — is brightest in the middle of its own length and dark at both ends,
    /// which is the one thing a beam never looks like: it makes the light appear to start a metre out in
    /// mid air.
    /// </summary>
    public static Texture Shaft() => ShaftFace.Value;

    // ---- readouts: what is on a screen -----------------------------------------------------------------

    /// <summary>How many layouts the sheet holds along each side. Three by three is nine, which is exactly
    /// the number of screens standing on the console run.</summary>
    private const int Panels = 3;

    /// <summary>The side of the whole sheet in pixels, so a cell is two hundred and fifty-six.</summary>
    private const int Sheet = Panels * 256;

    /// <summary>
    /// How much wider a screen is than it is tall.
    ///
    /// A cell of the sheet is square and every screen it lands on is not, so the image is stretched by this
    /// much on its way to the glass. Everything below that has to come out round — which is the dials, and
    /// only the dials — measures across with this factor folded in. Without it a bank of instruments has
    /// oval faces, which is not a thing any instrument has ever had.
    /// </summary>
    private const float Wide = 1.9f;

    /// <summary>
    /// Which gauge goes on the left of each panel and which on the right.
    ///
    /// Written out rather than hashed. Nine cells picked at random out of six kinds gives repeats — two
    /// dials side by side on the run is exactly the thing this sheet exists to stop — and the cost of
    /// guaranteeing otherwise is nine lines that can be read at a glance. Every kind appears three times
    /// across the eighteen slots, and no two panels carry the same pairing.
    /// </summary>
    private static readonly (Gauge Left, Gauge Right)[] Faces =
    [
        (Gauge.Dial,   Gauge.Trace),
        (Gauge.Bars,   Gauge.Dial),
        (Gauge.Rows,   Gauge.Dial),
        (Gauge.Trace,  Gauge.Ladder),
        (Gauge.Grid,   Gauge.Trace),
        (Gauge.Ladder, Gauge.Bars),
        (Gauge.Bars,   Gauge.Grid),
        (Gauge.Ladder, Gauge.Rows),
        (Gauge.Rows,   Gauge.Grid)
    ];

    /// <summary>The six things that go on a screen. See <see cref="Faces"/> for how they are dealt out.</summary>
    public enum Gauge { Dial, Bars, Trace, Rows, Ladder, Grid }

    /// <summary>How much of a dial's half-zone the outer ring takes. The room's needles are cut to it.</summary>
    public const float Rim = 0.46f;

    /// <summary>
    /// The rectangle one of a panel's two instruments stands in, in the panel's own coordinates.
    ///
    /// The room asks for this, which is the only reason it is not four literals in a draw function. Every
    /// screen carries a moving part on top of its picture — see <c>Illuminator.Screen</c> — and a needle
    /// that does not stand at the middle of the dial it belongs to is worse than no needle at all. One
    /// definition, read by the thing that draws the instrument and by the thing that animates it.
    /// </summary>
    public static (Vector2 Middle, Vector2 Size) Half(bool right) =>
        (new Vector2(right ? 0.735f : 0.265f, 0.595f), new Vector2(0.41f, 0.67f));

    /// <summary>Which instrument is on each side of a panel, so the room can put the right thing on it.</summary>
    public static (Gauge Left, Gauge Right) Reading(int index) =>
        Faces[((index % Faces.Length) + Faces.Length) % Faces.Length];

    private static readonly Lazy<Texture> ReadoutFace = new(() =>
        Grain.Colour(Sheet, "readout.face", (u, v) =>
        {
            var column = Math.Min((int)(u * Panels), Panels - 1);
            var row = Math.Min((int)(v * Panels), Panels - 1);

            return Panel(row * Panels + column, u * Panels - column, v * Panels - row);
        }));

    /// <summary>One panel's worth of instrumentation, in its own coordinates rather than the sheet's.</summary>
    private static Vector3 Panel(int cell, float u, float v)
    {
        // A dark margin, doing two jobs. It is the surround every instrument has inside its bezel — glass
        // that runs to the very edge of a panel reads as a hole cut in the desk rather than as a screen set
        // into it — and it is what keeps a sample taken at the edge of one cell from reaching into the next
        // one along. See Readout for the half-texel the mapping takes off as well.
        if (u < 0.035f || u > 0.965f || v < 0.045f || v > 0.955f)
            return Vector3.Zero;

        // And a notch, bottom left, kept clear on purpose: it is where the indicator lamp stands. A lamp
        // is a real object six millimetres off the glass rather than a mark drawn on it — see
        // Illuminator.Screen — so the sheet has to leave it somewhere to sit that is not covered in trace.
        if (u < 0.075f && v > 0.855f)
            return Vector3.Zero;

        var lit = Graticule(cell, u, v) + Caption(cell, u, v);

        var (left, right) = Faces[cell];

        lit += Draw(left, cell * 2, u, v, Half(false));
        lit += Draw(right, cell * 2 + 1, u, v, Half(true));

        lit = MathF.Min(lit, 1.4f);

        return new Vector3(lit * 0.44f, lit * 0.86f, lit);
    }

    private static float Draw(
        Gauge what, int seed, float u, float v, (Vector2 Middle, Vector2 Size) zone) =>
        Draw(what, seed, u, v,
            zone.Middle.X - zone.Size.X / 2f, zone.Middle.X + zone.Size.X / 2f,
            zone.Middle.Y - zone.Size.Y / 2f, zone.Middle.Y + zone.Size.Y / 2f);

    private static float Draw(Gauge what, int seed, float u, float v, float x0, float x1, float y0, float y1) =>
        what switch
        {
            Gauge.Dial => Dial(seed, u, v, x0, x1, y0, y1),
            Gauge.Bars => Bars(seed, u, v, x0, x1, y0, y1),
            Gauge.Trace => Trace(seed, u, v, x0, x1, y0, y1),
            Gauge.Rows => Rows(seed, u, v, x0, x1, y0, y1),
            Gauge.Ladder => Ladder(seed, u, v, x0, x1, y0, y1),
            _ => Grid(seed, u, v, x0, x1, y0, y1)
        };

    /// <summary>The grid everything else sits on, at a pitch that is this panel's rather than the run's.</summary>
    private static float Graticule(int cell, float u, float v)
    {
        var across = 0.09f + 0.05f * Grain.Pick(cell, 5, 61);
        var down = 0.15f + 0.09f * Grain.Pick(cell, 6, 67);

        return (1f - Grain.Band(Grain.Cell(u, across), 0.03f, 0.97f, 0.02f)) * 0.08f
               + (1f - Grain.Band(Grain.Cell(v, down), 0.04f, 0.96f, 0.02f)) * 0.08f;
    }

    /// <summary>
    /// The header: a run of blocks along the top with a rule under it and a state box at the far end.
    ///
    /// The blocks stand in for words, and they have to. Nothing in this building has a name — which has
    /// been true since the lounge — and a screen with legible text on it would be the first object in nine
    /// minutes to break it. Blocks are the convention every drawing of a screen has used since drawings of
    /// screens existed, and the run stops at a hashed point so no two panels have the same title on them.
    /// </summary>
    private static float Caption(int cell, float u, float v)
    {
        if (v > 0.20f)
            return 0f;

        if (v > 0.183f && v < 0.196f && u > 0.06f && u < 0.94f)
            return 0.45f;

        if (v < 0.085f || v > 0.16f)
            return 0f;

        if (u > 0.80f && u < 0.94f)
            return 0.5f;

        if (u < 0.06f || u > 0.28f + 0.36f * Grain.Pick(cell, 8, 89))
            return 0f;

        return Grain.Cell(u - 0.06f, 0.030f) < 0.62f ? 0.6f : 0f;
    }

    /// <summary>
    /// A round gauge: two rings, twelve ticks and a band marking the part of the scale that is in hand.
    ///
    /// <b>There is no needle on it, and that is the point.</b> The needle is a real object standing a
    /// centimetre and a half off the glass — see <c>Illuminator.Screen</c> — because a needle is the one
    /// part of a dial that has to move, and a painted one nailed to a hashed angle is what says louder than
    /// anything else on a panel that nothing here is running. The band is what stays: it is the range this
    /// instrument is supposed to be reading in, which is a property of the machine and not of the minute.
    /// </summary>
    private static float Dial(int seed, float u, float v, float x0, float x1, float y0, float y1)
    {
        var du = (u - (x0 + x1) / 2f) * Wide;
        var dv = v - (y0 + y1) / 2f;

        var r = MathF.Sqrt(du * du + dv * dv);
        var rim = MathF.Min((x1 - x0) * Wide, y1 - y0) * Rim;

        if (r > rim * 1.1f)
            return 0f;

        var a = MathF.Atan2(dv, du);
        var band = -2.3f + 2.4f * Grain.Pick(seed, 3, 17);

        var lit = Ring(r, rim, rim * 0.05f) * 0.8f
                  + Ring(r, rim * 0.64f, rim * 0.035f) * 0.35f
                  + Ring(r, 0f, rim * 0.13f) * 0.7f;

        if (a > band && a < band + 0.55f)
            lit += Ring(r, rim * 0.80f, rim * 0.11f) * 0.85f;

        for (var i = 0; i < 12; i++)
            if (MathF.Abs(Wrap(a - (-2.45f + i * 0.27f))) < 0.035f)
                lit += Ring(r, rim * 0.93f, rim * 0.14f) * 0.6f;

        return lit;
    }

    /// <summary>A bar chart standing on a baseline, its heights hashed off the panel and the bar.</summary>
    private static float Bars(int seed, float u, float v, float x0, float x1, float y0, float y1)
    {
        if (u < x0 || u > x1 || v < y0 || v > y1)
            return 0f;

        // A baseline, because a bar chart standing on nothing reads as a barcode.
        if (v > y1 - 0.014f)
            return 0.5f;

        var pitch = (x1 - x0) / 9f;
        var height = 0.20f + 0.76f * Grain.Pick(seed, Grain.Index(u - x0, pitch), 29);

        return Grain.Cell(u - x0, pitch) < 0.66f && (y1 - v) / (y1 - y0) < height ? 0.85f : 0f;
    }

    /// <summary>A wandering line with a fill under it, at a frequency and a phase that are this panel's.</summary>
    private static float Trace(int seed, float u, float v, float x0, float x1, float y0, float y1)
    {
        if (u < x0 || u > x1 || v < y0 || v > y1)
            return 0f;

        var t = (u - x0) / (x1 - x0);

        var y = (y0 + y1) / 2f + (y1 - y0) * 0.33f
            * MathF.Sin(t * (5.5f + 7f * Grain.Pick(seed, 1, 11)))
            * MathF.Cos(t * (2.2f + 3.4f * Grain.Pick(seed, 2, 23)) + seed);

        if (MathF.Abs(v - y) < 0.008f)
            return 1f;

        return v > y ? 0.13f : 0f;
    }

    /// <summary>Rows of blocks standing in for text, each row stopping at its own hashed point.</summary>
    private static float Rows(int seed, float u, float v, float x0, float x1, float y0, float y1)
    {
        if (u < x0 || u > x1 || v < y0 || v > y1)
            return 0f;

        var gap = (y1 - y0) / 6f;

        if (Grain.Cell(v - y0, gap) > 0.42f)
            return 0f;

        var row = Grain.Index(v - y0, gap);

        if ((u - x0) / (x1 - x0) > 0.3f + 0.68f * Grain.Pick(seed, row, 41))
            return 0f;

        var word = Grain.Index(u - x0, 0.032f);

        return Grain.Cell(u - x0, 0.032f) < 0.4f + 0.5f * Grain.Pick(seed, word * 7 + row, 13) ? 0.55f : 0f;
    }

    /// <summary>
    /// Five level gauges: framed columns of segments, filled from the bottom to a hashed height.
    ///
    /// <b>The frame is what makes it a gauge.</b> The first version was columns of segments and nothing
    /// else, and what that reads as is a field of blocks — the same thing <see cref="Grid"/> reads as, on a
    /// panel that had both. A level is a quantity against a capacity, so the capacity has to be drawn: it is
    /// the empty part of the tube above the full part that says the tank is a third full rather than saying
    /// there are four blocks here.
    /// </summary>
    private static float Ladder(int seed, float u, float v, float x0, float x1, float y0, float y1)
    {
        if (u < x0 || u > x1 || v < y0 || v > y1)
            return 0f;

        var wide = (x1 - x0) / 5f;
        var across = Grain.Cell(u - x0, wide);

        if (Grain.Band(across, 0.20f, 0.80f, 0.008f) < 0.5f)
            return 0f;

        const float cap = 0.012f;

        if (Grain.Band(across, 0.235f, 0.765f, 0.008f) < 0.5f || v < y0 + cap || v > y1 - cap)
            return 0.45f;

        var run = y1 - y0 - 2f * cap;
        var step = v - (y0 + cap);

        if (Grain.Cell(step, run / 12f) > 0.66f)
            return 0f;

        var level = 0.22f + 0.74f * Grain.Pick(seed, Grain.Index(u - x0, wide), 53);

        return 1f - step / run <= level ? 0.9f : 0f;
    }

    /// <summary>A status board: twelve outlined blocks at three brightnesses, hashed off their position.</summary>
    private static float Grid(int seed, float u, float v, float x0, float x1, float y0, float y1)
    {
        if (u < x0 || u > x1 || v < y0 || v > y1)
            return 0f;

        var wide = (x1 - x0) / 4f;
        var tall = (y1 - y0) / 3f;

        var across = Grain.Cell(u - x0, wide);
        var down = Grain.Cell(v - y0, tall);

        if (Grain.Band(across, 0.06f, 0.86f, 0.008f) * Grain.Band(down, 0.10f, 0.80f, 0.012f) < 0.5f)
            return 0f;

        // A dozen big outlined blocks rather than three dozen small ones, and the outline is the point. It
        // is what tells a block that is reading dark from a gap between blocks — which is the whole content
        // of a status board, and is invisible without it.
        if (Grain.Band(across, 0.115f, 0.80f, 0.008f) * Grain.Band(down, 0.22f, 0.68f, 0.012f) < 0.5f)
            return 0.45f;

        var pick = Grain.Pick(seed, Grain.Index(u - x0, wide) * 11 + Grain.Index(v - y0, tall), 71);

        return pick > 0.62f ? 0.95f : pick > 0.35f ? 0.30f : 0.06f;
    }

    /// <summary>A ring of a given radius and width, as a value between 0 and 1.</summary>
    private static float Ring(float r, float at, float width) =>
        1f - Grain.Step(width * 0.5f, width, MathF.Abs(r - at));

    private static float Wrap(float a) => a - MathF.Tau * MathF.Round(a / MathF.Tau);

    /// <summary>
    /// What is on the screens in the gallery: nine layouts on one sheet.
    ///
    /// <b>It was one layout for a long time and that was the wrong saving.</b> The argument for it was about
    /// uploads — a <see cref="Texture"/> is uploaded per instance, so twenty-one screens holding twenty-one
    /// images is twenty-one uploads — and the argument was sound. The conclusion was not: the way to keep
    /// one upload is to put every layout on one sheet, not to have one layout. What twenty-one copies of a
    /// single image looks like is not a bank of instruments, it is wallpaper with a dial in the repeat, and
    /// the eye finds a repeat across a thirteen-metre desk immediately.
    ///
    /// The screens were also tiled rather than fitted, which was the other half of it: a panel five hundred
    /// millimetres wide mapped at <see cref="Snug"/> got two and a half copies of the layout across it, cut
    /// wherever the desk happened to stand in the room. So no screen in the room was ever showing a whole
    /// anything. Every one of them now gets exactly one cell of this sheet, corner to corner — see
    /// <see cref="Readout"/> and <see cref="Fabric.Panel"/>.
    /// </summary>
    public static Texture Readouts() => ReadoutFace.Value;

    /// <summary>
    /// Where one of the nine layouts sits on the sheet, as an origin and a span in texture coordinates.
    ///
    /// Inset half a texel on every side. A screen is mapped corner to corner, so the sampler is asked for
    /// the very edge of a cell at the very edge of the glass — and bilinear filtering there reaches half a
    /// texel into whichever panel is next door on the sheet.
    /// </summary>
    public static (Vector2 Origin, Vector2 Span) Readout(int index)
    {
        const float inset = 0.5f / Sheet;
        const float step = 1f / Panels;

        var cell = ((index % (Panels * Panels)) + Panels * Panels) % (Panels * Panels);

        return (new Vector2(cell % Panels * step + inset, cell / Panels * step + inset),
            new Vector2(step - 2f * inset, step - 2f * inset));
    }

    // ---- keys: what a working surface has on it --------------------------------------------------------

    private static readonly Lazy<Texture> KeysFace = new(() =>
        Grain.Colour(256, "keys.face", (u, v) =>
        {
            // Eight caps by eight, which tiles because eight divides one. The panels it goes on are two
            // hundred and forty millimetres deep and read at Snug, so a cap comes out about twenty-seven
            // millimetres across — a key somebody could actually press, on a desk somebody stands at.
            const float pitch = 0.125f;

            var cap = Grain.Band(Grain.Cell(u, pitch), 0.10f, 0.90f, 0.05f)
                      * Grain.Band(Grain.Cell(v, pitch), 0.12f, 0.88f, 0.06f);

            // The gap between the caps, and it is the gap that does all the work. At two metres nobody
            // resolves a key; what they resolve is a grid of dark lines, and a grid of dark lines on a
            // dark surface is read as keys before anything on it is legible.
            if (cap <= 0f)
                return new Vector3(0.002f, 0.002f, 0.003f);

            // A lit lip along the top of every cap and a shadow under its foot. A key is a solid, and
            // nothing at this size is ever going to be lit from the side by the one light this room has —
            // so the relief is painted into the map, which is the only place there is room for it.
            var down = Grain.Cell(v, pitch);

            var body = new Vector3(0.014f, 0.015f, 0.018f)
                       + new Vector3(0.016f) * (1f - Grain.Step(0.10f, 0.32f, down))
                       - new Vector3(0.007f) * Grain.Step(0.62f, 0.90f, down);

            // And a scatter of lit ones, hashed off the cell so the field has a state rather than a
            // pattern. One in ten amber for what is running and one in thirty cold for what is only being
            // watched, which is about the mix every photograph of a console has on it — the point of a
            // board is that most of it is <i>not</i> lit, and the first cut of this had a quarter of it on.
            var pick = Grain.Pick(Grain.Index(u, pitch), Grain.Index(v, pitch), 41);

            if (pick > 0.90f)
                return (body + new Vector3(0.92f, 0.46f, 0.11f)) * cap;

            if (pick is > 0.845f and < 0.875f)
                return (body + new Vector3(0.20f, 0.62f, 0.90f)) * cap;

            return body * cap;
        }));

    /// <summary>
    /// A field of key caps, a few of them lit.
    ///
    /// It is used twice on the same material — once as the base colour and once as the emissive — and that
    /// is the whole of how a backlit panel is built here. Emissive multiplies its map, so the caps at five
    /// percent contribute nothing and the lit ones at ninety carry the glow; one texture, one upload, and
    /// a panel that is dark where it is dark and bright where it is on.
    ///
    /// It tiles, which is the property that matters and the reason the cell pitch is an eighth. The panels
    /// are read at <see cref="Snug"/> like the screens are, so one image covers 220 mm and a 620 mm panel
    /// gets about twenty-two keys across without anybody choosing that number.
    /// </summary>
    public static Texture Keys() => KeysFace.Value;

    // ---- brushed: every fitting in the gallery ---------------------------------------------------------

    private static readonly Lazy<Texture> BrushedColour = new(() =>
        Grain.Colour(Detailed, "brushed.colour", (u, v) =>
            new Vector3(0.96f + 0.04f * Grain.Fbm(u * 0.06f, v, 130, 71, 2))));

    private static readonly Lazy<Texture> BrushedRough = new(() =>
        Grain.Rough(Detailed, "brushed.rough", (u, v) =>
        {
            // Stretched a long way in u and not at all in v, which is the whole of what brushing is: the
            // grain runs one way, so the highlight smears across it and stays tight along it. It is the
            // nearest a renderer with an isotropic BRDF gets to anisotropy, and at this range it is close
            // enough that nobody has ever asked.
            var grain = Grain.Fbm(u * 0.05f, v, 150, 83, 3);

            return new Vector2(0.14f + 0.20f * grain, 1f);
        }));

    private static readonly Lazy<Texture> BrushedBumps = new(() =>
        Grain.Bumps(Detailed, "brushed.bumps", (u, v) =>
            Grain.Fbm(u * 0.05f, v, 150, 83, 3), 0.0004f, Close));

    // ---- boards --------------------------------------------------------------------------------------

    private static readonly Lazy<Texture> BoardColour = new(() =>
        Grain.Colour(Detailed, "boards.colour", (u, v) =>
        {
            var (face, grain, cell) = Board(u, v);
            var tone = 0.78f + 0.34f * cell;

            return new Vector3(
                (0.40f + 0.60f * face) * tone * (0.86f + 0.20f * grain),
                (0.40f + 0.60f * face) * tone * (0.86f + 0.20f * grain) * 0.90f,
                (0.40f + 0.60f * face) * tone * (0.86f + 0.20f * grain) * 0.78f);
        }));

    private static readonly Lazy<Texture> BoardRough = new(() =>
        Grain.Rough(Detailed, "boards.rough", (u, v) =>
        {
            var (face, grain, _) = Board(u, v);
            return new Vector2((1f - 0.30f * face) * (0.92f + 0.16f * grain), 1f);
        }));

    private static readonly Lazy<Texture> BoardBumps = new(() =>
        Grain.Bumps(Detailed, "boards.bumps", (u, v) =>
        {
            var (face, grain, _) = Board(u, v);
            return face * 0.88f + grain * 0.12f;
        }, 0.003f, Pitch));

    // ---- one-offs ------------------------------------------------------------------------------------

    private static readonly Lazy<Texture> WoodColour = new(() =>
        Grain.Colour(Fine, "wood.colour", (u, v) =>
        {
            var g = Fibre(u, v);
            return new Vector3(0.66f + 0.34f * g, (0.66f + 0.34f * g) * 0.88f, (0.66f + 0.34f * g) * 0.70f);
        }));

    private static readonly Lazy<Texture> WoodBumps = new(() =>
        Grain.Bumps(Fine, "wood.bumps", Fibre, 0.0007f, Pitch));

    /// <summary>Drawn at <see cref="Close"/> rather than <see cref="Pitch"/> — see <see cref="Hide"/>.</summary>
    private static readonly Lazy<Texture> HideColour = new(() =>
        Grain.Colour(Fine, "hide.colour", (u, v) =>
        {
            var g = Pebble(u, v);
            return new Vector3(0.88f + 0.12f * g, (0.88f + 0.12f * g) * 0.97f, (0.88f + 0.12f * g) * 0.95f);
        }));

    private static readonly Lazy<Texture> HideBumps = new(() =>
        Grain.Bumps(Detailed, "hide.bumps", Pebble, 0.0012f, Close));

    /// <summary>
    /// Moulded plastic, and the whole of what makes it plastic is four tenths of a millimetre of orange peel.
    ///
    /// It is the most useful specimen in the room for exactly that reason. A red plastic panel and a panel of
    /// red-painted steel have the same base colour, nearly the same roughness and no metal in either of them
    /// — every number in the material is within a rounding of the other — and nobody has ever confused the
    /// two in real life. What separates them is that a moulding cools against a tool and comes out with a
    /// gentle ripple a few millimetres across, and sheet steel under paint does not. That ripple is below the
    /// resolution of the geometry and it is the exact thing a normal map is for.
    /// </summary>
    private static readonly Lazy<Texture> PlasticBumps = new(() =>
        Grain.Bumps(Detailed, "plastic.bumps", (u, v) => Grain.Fbm(u, v, 34, 41, 2), 0.0007f, Close));

    /// <summary>The mould's own texture in the gloss rather than in the colour: a moulding is not equally
    /// shiny everywhere, and that is most of what stops it reading as painted card.</summary>
    private static readonly Lazy<Texture> PlasticRough = new(() =>
        Grain.Rough(Detailed, "plastic.rough", (u, v) =>
            new Vector2(0.22f + 0.10f * Grain.Fbm(u, v, 34, 41, 2), 0f)));

    private static readonly Lazy<Texture> WeaveColour = new(() =>
        Grain.Colour(Fine, "weave.colour", (u, v) => new Vector3(0.88f + 0.12f * Weave(u, v))));

    /// <summary>
    /// The rug: pile and nothing else.
    ///
    /// It had a woven border in it, drawn into the image, and that was a category error rather than a
    /// mistake of degree. An image is <i>tiled</i>, so a border drawn at the edge of one becomes a grid of
    /// borders across the middle of the rug — which is not a rug with a band round it, it is a rug printed
    /// with a lattice. A border is a fact about the object's edges and belongs to the geometry; the rug is
    /// two rectangles now, a dark one and a lighter one on top of it.
    /// </summary>
    private static readonly Lazy<Texture> PileColour = new(() =>
        Grain.Colour(Fine, "pile.colour", (u, v) =>
        {
            var fuzz = 0.84f + 0.26f * Grain.Fbm(u, v, 34, 23, 3);
            return new Vector3(fuzz, fuzz * 0.94f, fuzz * 0.88f);
        }));

    /// <summary>
    /// The walls, at whatever grade the room is at.
    ///
    /// The colour and the two numbers are the same at every grade, deliberately. What climbs is how much
    /// of the model is switched on, not what the plaster is — so the antechamber and the gallery are the
    /// same shade of off-white and only one of them has any relief in it.
    /// </summary>
    public static Material Plaster(Grade grade = Grade.Flat)
    {
        var material = new Material
        {
            BaseColor = new Vector4(0.36f, 0.35f, 0.33f, 1f),
            Roughness = 0.92f,
            Name = "plaster"
        };

        if (grade >= Grade.Grained)
            material.BaseColorTexture = PlasterColour.Value;

        if (grade >= Grade.Worked)
            material.MetallicRoughnessTexture = PlasterRough.Value;

        if (grade >= Grade.Dressed)
        {
            material.NormalTexture = PlasterBumps.Value;
        }

        return material;
    }

    /// <summary>The floor, likewise. Dark tile in every room that has it.</summary>
    public static Material Floor(Grade grade = Grade.Flat)
    {
        var material = new Material
        {
            BaseColor = new Vector4(0.22f, 0.22f, 0.24f, 1f),
            Roughness = 0.55f,
            Name = "tile"
        };

        if (grade >= Grade.Grained)
            material.BaseColorTexture = TileColour.Value;

        if (grade >= Grade.Worked)
            material.MetallicRoughnessTexture = TileRough.Value;

        if (grade >= Grade.Dressed)
            material.NormalTexture = TileBumps.Value;

        return material;
    }

    /// <summary>Plinths, benches and turntables. Grained from the rotunda on, and never more than that —
    /// a plinth with visible relief competes with whatever is standing on it.</summary>
    public static Material Stone(Grade grade = Grade.Flat)
    {
        var material = new Material
        {
            BaseColor = new Vector4(0.42f, 0.40f, 0.36f, 1f),
            Roughness = 0.85f,
            Name = "stone"
        };

        if (grade >= Grade.Worked)
        {
            material.BaseColorTexture = PlasterColour.Value;
            material.MetallicRoughnessTexture = PlasterRough.Value;
        }

        return material;
    }

    /// <summary>
    /// Painted metal panelling with rivets and gutters between the sheets: the lounge, and the first
    /// surface in the film that is not something a house is made of.
    ///
    /// This is where the occlusion map earns its place. The gutters between panels are two centimetres
    /// deep and no light in the room reaches into them, and there is no shadowing here to work that out —
    /// so the grid is baked into a map that darkens the ambient term and leaves the lamps alone, which is
    /// exactly what occlusion is for and exactly what it is wrong to apply to direct light.
    /// </summary>
    public static Material Panelling() => new()
    {
        BaseColor = new Vector4(0.30f, 0.31f, 0.34f, 1f),
        Metallic = 0.5f,
        Roughness = 0.62f,
        BaseColorTexture = PanelColour.Value,
        MetallicRoughnessTexture = PanelRough.Value,
        NormalTexture = PanelBumps.Value,
        OcclusionTexture = PanelMask.Value,
        OcclusionStrength = 0.85f,
        Name = "panel"
    };

    /// <summary>
    /// Moulded white composite: the illuminator gallery, and the only fit-out in the building.
    ///
    /// It is the one material here that carries every slot the shading model has — base colour,
    /// metallic-roughness, normal and occlusion — and it is the room where all five are worth having at
    /// once. A white surface has no colour information to hide behind: everything that tells you what it
    /// is made of is in the gloss, the seam and the way the corner goes dark, so a white room drawn with
    /// base colour alone is a white box, and a white room drawn with four maps is a moulding.
    ///
    /// <b>It has no seams in it at all</b>, and the first version's seams are why. A grid of panels with a
    /// joint round each one is what plate looks like, and this room is not made of plate — it is moulded,
    /// in pieces the size of a wall, and what it is made of is glass cloth under a gel coat. A cell map on
    /// it read exactly as what it was: brickwork. So the only thing varying across this surface is gloss,
    /// at two frequencies — the tow of the cloth and the filaments in it — and every joint in the room is
    /// geometry, put where a joint would actually be, rather than a pattern repeating every two thirds of
    /// a metre whether there is a piece edge there or not.
    /// </summary>
    public static Material Composite() => new()
    {
        BaseColor = new Vector4(0.76f, 0.77f, 0.79f, 1f),
        Metallic = 0.05f,
        Roughness = 1f,
        BaseColorTexture = CompositeColour.Value,
        MetallicRoughnessTexture = CompositeRough.Value,
        NormalTexture = CompositeBumps.Value,

        // Half strength, and for the same reason the boards are: this material ends up on ceilings and on
        // walls thirteen metres long, and a normal map read at a grazing angle turns every bump into a
        // strip of shimmer. The relief is six tenths of a millimetre. Nobody was ever going to see it.
        NormalScale = 0.35f,
        Name = "composite"
    };

    /// <summary>
    /// The gallery deck: poured resin, polished, and seamless.
    ///
    /// It is the only floor in the building with no joint in it, and that is the point of it. Every other
    /// deck here is tile or plate, which is what a floor is when it has to be lifted; this one is a floor
    /// in a room nobody services, so it is one surface from wall to wall.
    /// </summary>
    public static Material Poured() => new()
    {
        BaseColor = new Vector4(0.66f, 0.67f, 0.70f, 1f),
        Metallic = 0.12f,
        Roughness = 1f,
        BaseColorTexture = PouredColour.Value,
        MetallicRoughnessTexture = PouredRough.Value,
        NormalTexture = PouredBumps.Value,
        NormalScale = 0.5f,
        Name = "poured"
    };

    /// <summary>
    /// Brushed metal, for every fitting in the gallery: the vault ribs, the rail, the pod feet.
    ///
    /// Roughness comes off a map that is stretched twenty times in one axis, which is what makes the
    /// highlight smear across the grain and stay tight along it. This renderer's BRDF is isotropic, so
    /// that is a fake — but it is a fake made of the same thing the real effect is made of, which is a
    /// surface whose microfacets are not the same in both directions.
    /// </summary>
    public static Material Brushed() => new()
    {
        BaseColor = new Vector4(0.72f, 0.73f, 0.76f, 1f),
        Metallic = 0.85f,
        Roughness = 1f,
        BaseColorTexture = BrushedColour.Value,
        MetallicRoughnessTexture = BrushedRough.Value,
        NormalTexture = BrushedBumps.Value,
        NormalScale = 0.6f,
        Name = "brushed"
    };

    /// <summary>The lounge floor. Boards, because the room is meant to be sat down in.</summary>
    public static Material Boards() => new()
    {
        BaseColor = new Vector4(0.30f, 0.21f, 0.14f, 1f),
        Roughness = 0.42f,
        BaseColorTexture = BoardColour.Value,
        MetallicRoughnessTexture = BoardRough.Value,
        NormalTexture = BoardBumps.Value,

        // Half strength, because of one shot: the seated camera in the lounge looks along this floor at a
        // very grazing angle, and a normal map read edge-on turns every plank gap into a strip of shimmer.
        // The relief is right — the map is not being turned down because it is wrong, it is being turned
        // down because there is a frame in the film that sees it at nearly ninety degrees.
        NormalScale = 0.5f,
        Name = "boards"
    };

    /// <summary>
    /// Gold, and the only thing that makes it gold is two numbers.
    ///
    /// It is the specimen the material gallery most needs and the cheapest one in the room. A metal has no
    /// diffuse colour at all — every photon it does not absorb comes back off the surface — so what a
    /// metal's base colour actually tints is its <i>reflection</i>, which is why gold reflects a gold room
    /// and steel reflects a grey one. Metallic at one and a base colour of one, three quarters, a third is
    /// the entire difference between this and <see cref="Brushed"/>, and they share every map.
    ///
    /// Sharing the maps is the point rather than a saving: a reader comparing the two specimens on the shelf
    /// is looking at one surface finish under two base colours, which is the comparison the room is for.
    /// </summary>
    public static Material Gold() => new()
    {
        BaseColor = new Vector4(1f, 0.76f, 0.34f, 1f),
        Metallic = 1f,

        // Polished, where the steel it shares its maps with is brushed — and it has to be, for a reason
        // worth having in the file. A metal has no diffuse term at all, so everything it shows is a
        // reflection of the room; at the roughness of a brushed fitting, in a gallery lit by five small
        // lamps, there is nothing bright enough in front of it to reflect and a gold specimen renders
        // black. It did. What makes gold read as gold is not the base colour on its own, it is the base
        // colour tinting something the surface is smooth enough to actually pick up.
        Roughness = 0.34f,
        BaseColorTexture = BrushedColour.Value,
        MetallicRoughnessTexture = BrushedRough.Value,
        NormalTexture = BrushedBumps.Value,
        NormalScale = 0.45f,
        Name = "gold"
    };

    /// <summary>
    /// Glass, which this renderer cannot refract and does not need to.
    ///
    /// What makes a pane read as glass at gallery distance is not refraction. It is that you can see through
    /// it, that its edge is visible where the sheet is thick, and that it carries a hard specular highlight
    /// no dielectric this smooth could avoid — and all three of those are an alpha blend, a slab with
    /// thickness, and a roughness of three hundredths. Refraction would move what is behind it by a few
    /// millimetres.
    ///
    /// It is lit rather than <see cref="Material.Unlit"/>, which is the one decision here that matters. The
    /// illuminator's windows are unlit because they are a tint over a starfield and any shading on them would
    /// be shading on the sky; a specimen is an object in a room with a lamp over it, and an unlit one is a
    /// grey ghost with no highlight in it — which is what glass looks like when it has been switched off.
    /// </summary>
    public static Material Glass() => new()
    {
        // Three tenths of a tenth was the first value and it read as a pale card: over a plastered wall,
        // alpha that high is mostly the glass and hardly at all what is behind it, which is the one thing
        // a window has to be. And the roughness went up rather than down — at three hundredths the
        // highlight is a point a couple of pixels across that the camera has to be lucky to be standing in.
        BaseColor = new Vector4(0.80f, 0.88f, 0.86f, 0.15f),
        Metallic = 0f,
        Roughness = 0.07f,
        Blend = BlendMode.Alpha,
        DepthWrite = false,
        Cull = CullMode.None,
        Name = "glass"
    };

    /// <summary>
    /// Moulded plastic, in whatever colour is asked for. See <see cref="PlasticBumps"/> for the four tenths
    /// of a millimetre that are the whole of it.
    /// </summary>
    public static Material Plastic(float r, float g, float b) => new()
    {
        BaseColor = new Vector4(r, g, b, 1f),
        Metallic = 0f,
        Roughness = 1f,
        MetallicRoughnessTexture = PlasticRough.Value,
        NormalTexture = PlasticBumps.Value,
        Name = "plastic"
    };

    /// <summary>The second door.</summary>
    public static Material Timber() => new()
    {
        BaseColor = new Vector4(0.30f, 0.19f, 0.11f, 1f),
        Roughness = 0.38f,
        BaseColorTexture = WoodColour.Value,
        NormalTexture = WoodBumps.Value,
        NormalScale = 0.45f,
        Name = "wood"
    };

    /// <summary>The armchair, drawn at <see cref="Close"/>. Dark, and glossy enough to hold a highlight
    /// from across the room.</summary>
    public static Material Hide() => new()
    {
        BaseColor = new Vector4(0.22f, 0.11f, 0.08f, 1f),
        Roughness = 0.38f,
        BaseColorTexture = HideColour.Value,
        NormalTexture = HideBumps.Value,
        Name = "leather"
    };

    /// <summary>
    /// The bean bag: a colour map and nothing else, and the one surface in the building that tried both
    /// kinds of relief and ended up with neither.
    ///
    /// A normal map was out on its own merits — it is read in tangent space and needs
    /// <see cref="Mesh.Tangents"/>, and the tangents of anything turned about an axis degenerate where that
    /// axis comes out of it, so a weave mapped that way pinwheels at the top of the bag. <see cref="Material.BumpTexture"/> needs no tangents,
    /// which is exactly why it is the documented choice for anything round, and it is a height field
    /// perturbed by its own <i>screen-space</i> gradient — so its strength is read against how many texels
    /// a pixel happens to cover rather than against the surface. On the highest-frequency field in the
    /// building that is a cauliflower at half strength and invisible at a tenth, with nothing useful in
    /// between.
    ///
    /// A weave a centimetre across seen from three metres is below a pixel anyway. What sells cloth at that
    /// distance is that it is not one flat colour, and that is what is left.
    /// </summary>
    public static Material Cloth(float r, float g, float b) => new()
    {
        BaseColor = new Vector4(r, g, b, 1f),
        Roughness = 0.88f,
        BaseColorTexture = WeaveColour.Value,
        Name = "cloth"
    };

    /// <summary>The rug under both of them.</summary>
    public static Material Pile() => new()
    {
        BaseColor = new Vector4(0.26f, 0.16f, 0.13f, 1f),
        Roughness = 0.95f,
        BaseColorTexture = PileColour.Value,
        Name = "rug"
    };

    // ---- the fields the maps above are drawn from ----------------------------------------------------

    /// <summary>Plaster: a coarse undulation with a fine stipple over it, both tileable.</summary>
    private static float Skin(float u, float v) =>
        Grain.Fbm(u, v, 5, 11, 4) * 0.62f + Grain.Fbm(u, v, 22, 29, 3) * 0.38f;

    /// <summary>
    /// One tile of the floor: 1 on the face, 0 in the grout, and the tile's own index hashed into
    /// <paramref name="tone"/> so no two neighbours are quite the same.
    /// </summary>
    private static float Slab(float u, float v, out float tone)
    {
        const float pitch = 0.5f;
        const float joint = 0.016f;

        var cu = Grain.Cell(u, pitch);
        var cv = Grain.Cell(v, pitch);

        tone = Grain.Pick(Grain.Index(u, pitch), Grain.Index(v, pitch), 5);

        return Grain.Band(cu, joint, 1f - joint, 0.008f) * Grain.Band(cv, joint, 1f - joint, 0.008f);
    }

    /// <summary>
    /// One sheet of panelling: the face, its rivets, and the sheet's own tone.
    ///
    /// Eight tenths of a metre wide and a metre and six tenths tall, which is a sheet of plate standing on
    /// end. Square cells were the first version and they read unmistakably as a bank of lockers — a square
    /// grid on a wall is storage, and a wall of tall sheets with a seam between them is a wall.
    /// </summary>
    private static (float Face, float Rivet, float Tone) Panel(float u, float v)
    {
        const float wide = 0.5f;
        const float tall = 1f;
        const float gutter = 0.012f;

        var cu = Grain.Cell(u, wide);
        var cv = Grain.Cell(v, tall);

        var face = Grain.Band(cu, gutter, 1f - gutter, 0.006f)
                   * Grain.Band(cv, gutter * wide / tall, 1f - gutter * wide / tall, 0.004f);

        // Rivets down the two long edges rather than one at each corner: four a side, which is how a plate
        // is actually fixed and reads as fixings rather than as decoration. A disc rather than a square,
        // because a square rivet is the one shape that makes a generated texture look generated.
        var rivet = 0f;

        foreach (var ru in new[] { 0.045f, 1f - 0.045f })
        foreach (var rv in new[] { 0.10f, 0.37f, 0.63f, 0.90f })
        {
            // The cell is twice as tall as it is wide in surface metres, so the vertical distance has to be
            // scaled before it can be compared with the horizontal one. Without it the rivets are ellipses.
            var du = cu - ru;
            var dv = (cv - rv) * tall / wide;

            rivet = MathF.Max(rivet, 1f - Grain.Step(0.007f, 0.013f, MathF.Sqrt(du * du + dv * dv)));
        }

        return (face, rivet * face, Grain.Pick(Grain.Index(u, wide), Grain.Index(v, tall), 17));
    }

    /// <summary>
    /// One moulded composite panel: the face, and the panel's own tone.
    ///
    /// Wide and low — about two thirds of a metre by a third — because a fit-out panel is a piece somebody
    /// carried in through a door, and the proportion is what says so. It has no rivets in it and that is
    /// the point of it beside <see cref="Panel"/>: plate is fixed, moulding is bonded, and the absence of
    /// fixings is most of what separates the inside of a cabin from the inside of a hold.
    /// </summary>
    private static (float Face, float Tone) Moulded(float u, float v)
    {
        const float wide = 0.42f;
        const float tall = 0.21f;
        const float seam = 0.016f;

        var cu = Grain.Cell(u, wide);
        var cv = Grain.Cell(v, tall);

        var face = Grain.Band(cu, seam, 1f - seam, 0.009f)
                   * Grain.Band(cv, seam * wide / tall, 1f - seam * wide / tall, 0.006f);

        return (face, Grain.Pick(Grain.Index(u, wide), Grain.Index(v, tall), 23));
    }

    /// <summary>
    /// One floorboard: the face between the gaps, the grain along it, and the board's own tone.
    ///
    /// The grain is sampled with u divided down and v multiplied up, which stretches the noise along the
    /// board. That one asymmetry is the whole difference between wood and stone — the fibres run one way,
    /// and a field that is isotropic reads as granite however brown it is.
    /// </summary>
    private static (float Face, float Grain, float Tone) Board(float u, float v)
    {
        const float pitch = 0.125f;
        const float gap = 0.005f;

        var cv = Grain.Cell(v, pitch);
        var index = Grain.Index(v, pitch);

        // Ends, staggered by the board so they do not line up into a second set of joints across the floor.
        var shifted = u + Grain.Pick(index, 0, 33);
        var along = Grain.Cell(shifted, 1f);

        var face = Grain.Band(cv, gap, 1f - gap, 0.003f) * Grain.Band(along, 0.004f, 1f - 0.004f, 0.003f);
        var grain = Grain.Fbm(u * 0.2f + index * 0.37f, v * 5f, 16, 7 + index, 3);

        return (face, grain, Grain.Pick(index, Grain.Index(shifted, 1f), 3));
    }

    /// <summary>The door's timber: the same stretched field, without the boards.</summary>
    private static float Fibre(float u, float v) =>
        Grain.Fbm(u * 6f, v * 0.35f, 14, 53, 4);

    /// <summary>Leather: fine noise pushed towards its low end, so the field is pits between pebbles
    /// rather than an even fuzz.</summary>
    private static float Pebble(float u, float v) => MathF.Pow(Grain.Fbm(u, v, 24, 83, 3), 1.5f);

    /// <summary>Cloth: warp over weft, with a little fuzz so the weave is not a grid of identical dots.</summary>
    private static float Weave(float u, float v)
    {
        const float threads = 26f;

        var warp = MathF.Sin(u * MathF.Tau * threads);
        var weft = MathF.Sin(v * MathF.Tau * threads);

        return (0.5f + 0.5f * warp * weft) * 0.82f + Grain.Fbm(u, v, 34, 97, 2) * 0.18f;
    }
}
