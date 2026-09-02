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
///
/// <b>The ladder is climbed by the first four rooms and then it stops.</b> The studio, the pattern shop
/// and the link between them all stand on the gallery's rung, and none of them is a mistake: a ladder with
/// five stops on it has nowhere to put a sixth room, and a room that climbed past the lounge would take
/// the top off the one place in the film that is furnished rather than lit. What those three do instead is
/// spend the rung differently — the studio paints its plaster out because both its exhibits are read as
/// silhouettes, and the pattern shop lays boards wall to wall because it is a workshop. Same maps, other
/// decisions.
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

    // ---- the planetarium ----------------------------------------------------------------------------
    //
    // Three surfaces that exist nowhere else in the building, and they are here rather than in the room
    // because they are the room. A planetarium is a dark box with one bright thing in it, and the whole of
    // what makes that work is that every surface which is not the dome gives light back grudgingly and the
    // dome gives it back evenly. That is a materials problem before it is a lighting one.

    /// <summary>
    /// The dome: perforated white sheet on a frame, in gores.
    ///
    /// <b>It is the only surface in the building whose job is to have no character at all.</b> Everything
    /// else here is trying to say what it is made of; this is trying to be a screen, and a screen with a
    /// story in its own surface is a screen competing with what is on it. So the colour map is flat to
    /// within a per cent, all the relief is a millimetre and a half, and the only thing that varies at any
    /// scale a person can see is where two panels meet.
    ///
    /// The perforations are not decoration and they are not for show either. A dome nine and a half metres
    /// across is a whispering gallery — it focuses every sound in the room back at the middle of it, which
    /// is where the seats are — and the answer every real one uses is to punch a million two-millimetre
    /// holes in it and hang absorbent behind. They read here as the faintest possible tooth, which is
    /// exactly what they look like from four metres, and they are the reason the light off this surface is
    /// soft rather than sheeny.
    ///
    /// <b>It carries occlusion, which is the map plaster never gets.</b> A hole is a place no light in the
    /// room reaches into, and there is no shadowing here to work that out — see <see cref="Panelling"/>,
    /// which makes the same argument about a gutter between two plates.
    /// </summary>
    public static Material Screen() => new()
    {
        BaseColor = new Vector4(0.90f, 0.90f, 0.89f, 1f),
        Metallic = 0f,
        Roughness = 1f,
        BaseColorTexture = ScreenColour.Value,
        MetallicRoughnessTexture = ScreenRough.Value,
        NormalTexture = ScreenBumps.Value,
        OcclusionTexture = ScreenMask.Value,
        OcclusionStrength = 0.75f,

        // Half strength, and the dome is the surface in this building that most needs it. A cap seen from
        // underneath is read at every angle at once — square on overhead and edge on at the rim — and a
        // relief of a millimetre and a half read at eighty degrees is a millimetre and a half of shimmer
        // all the way round the springing.
        NormalScale = 0.45f,
        Name = "screen"
    };

    /// <summary>
    /// The walls: stretched fabric over absorbent board, in panels with a reveal between them, which is
    /// what the inside of every dark room that people listen in is lined with.
    ///
    /// Dark on purpose and dark by a lot — a tenth, where the building's plaster is a third. A wall that
    /// gives back a third of what falls on it is a wall that fills a dome with its own bounce, and the one
    /// thing this room cannot afford is light arriving at the screen from the room. It is also the only
    /// reason the four photographs work: a print at a third of a metre from a black wall under one cove is
    /// the brightest thing in its own quarter of the room.
    ///
    /// <b>It was fluted, and the flutes are why it is not.</b> A hundred and sixty millimetres of
    /// half-round profile at twelve deep is a real acoustic section and it rendered as what it is — a
    /// wall of vertical stripes, lit one flank at a time by five coves, which from the door read as
    /// corrugated sheet and from a metre as a roller blind. A stripe is the one thing a wall must never
    /// be. What a lined auditorium actually shows is panels: eight hundred by sixteen hundred, a shadow
    /// line between each, the cloth bowed a few millimetres over the board — and nothing else varying at
    /// any scale a person can see, because the nap of a cloth is below the texel and drawing it anyway is
    /// drawing moiré. The reveals carry the occlusion, which is the map that makes a joint a joint.
    /// </summary>
    public static Material Acoustic() => new()
    {
        BaseColor = new Vector4(0.105f, 0.108f, 0.128f, 1f),
        Metallic = 0f,
        Roughness = 1f,
        BaseColorTexture = AcousticColour.Value,
        MetallicRoughnessTexture = AcousticRough.Value,
        NormalTexture = AcousticBumps.Value,
        OcclusionTexture = AcousticMask.Value,
        OcclusionStrength = 0.8f,

        // A round room is read at every angle at once — square on across the floor and edge on where
        // the wall curves away — and relief read edge on is shimmer. Most of this map is the reveal,
        // which is a line and survives; the bow over a panel is gentle enough not to.
        NormalScale = 0.7f,
        Name = "acoustic"
    };

    /// <summary>
    /// Seat cloth: a wool twill with a nap, for the five chairs under the dome.
    ///
    /// <see cref="Cloth"/> is a colour map and nothing else, and its own note explains why — a weave a
    /// centimetre across seen from three metres is below a pixel, and what sells cloth at that distance is
    /// only that it is not one flat colour. That argument is exactly right for a bean bag across a lounge
    /// and exactly wrong here: the film sits <i>in</i> one of these chairs and the free walk stands over
    /// them, so the nearest seat back is six hundred millimetres from the camera and every one of these is
    /// looked at from arm's length. At that distance a flat colour is moulded plastic, and the note about
    /// the chairs looking bad is that reading, correctly made.
    ///
    /// So this is the one soft surface in the building with relief on it: a twill at four millimetres and
    /// a nap under it, drawn at <see cref="Close"/> because it is furniture.
    /// </summary>
    public static Material Seating(float r, float g, float b) => new()
    {
        BaseColor = new Vector4(r, g, b, 1f),
        Metallic = 0f,
        Roughness = 1f,
        BaseColorTexture = TwillColour.Value,
        MetallicRoughnessTexture = TwillRough.Value,
        NormalTexture = TwillBumps.Value,
        Name = "seating"
    };

    /// <summary>
    /// The floor: contract carpet, deep, dark and laid wall to wall.
    ///
    /// It is <see cref="Pile"/>'s job done for a room rather than for a rug, and the difference is the
    /// relief. The lounge's rug is two metres of soft thing seen from a chair and a colour map is enough
    /// of it; this is forty square metres walked across at eye height in a room whose lamps are all at
    /// head height and aimed sideways, which is the exact arrangement that makes a floor either read as
    /// carpet or read as paper. Three millimetres of pile is what does it.
    /// </summary>
    public static Material Carpet() => new()
    {
        // Three times the tenth it used to be, against a map drawn at a third — see CarpetColour.
        BaseColor = new Vector4(0.30f, 0.28f, 0.33f, 1f),
        Metallic = 0f,
        Roughness = 1f,
        BaseColorTexture = CarpetColour.Value,
        MetallicRoughnessTexture = CarpetRough.Value,
        NormalTexture = CarpetBumps.Value,
        Name = "carpet"
    };

    // ---- the clock tower ---------------------------------------------------------------------------
    //
    // Seven surfaces, and they are the first in the building that are not something a gallery is lined
    // with. The tower is the one room the story mounts whole rather than builds — see ClockRoom — so the
    // scene dresses itself from this kit and the wall the story adds is cut from the same stone. Every
    // other room here is plaster over a frame; this one is masonry, flags, oak and cast iron, which is
    // what the back of a tower clock is made of, and it is also the one place in the film with a light
    // strong enough to show a surface at four metres. A sun raking across a wall is the whole reason a
    // normal map exists, and until this room nothing in the building had one to rake.

    /// <summary>
    /// Ashlar: coursed dressed stone with the joints raked and the arrises worn.
    ///
    /// Four courses of unequal height add up to one image, so the wall tiles without a course repeating
    /// on a rhythm the eye can count; two or three blocks to a course, each course shifted so the
    /// perpends never stack — running bond, which every wall of cut stone is laid to because a joint over
    /// a joint is a crack. The face of a block is pillowed by a few millimetres and has a tooth to it, and
    /// both are below what a person sees from the middle of the room. What reads from there is the tone
    /// changing block by block and the joints going dark under a raking sun, and those are the two things
    /// the colour map and the occlusion map are for.
    ///
    /// The pitch is the building's, so the courses are three hundred and seventy to four hundred and
    /// thirty millimetres and the blocks half a metre to eight tenths — a tower, not a cottage.
    /// </summary>
    public static Material Masonry() => new()
    {
        BaseColor = new Vector4(0.50f, 0.46f, 0.40f, 1f),
        Metallic = 0f,
        Roughness = 1f,
        BaseColorTexture = MasonryColour.Value,
        MetallicRoughnessTexture = MasonryRough.Value,
        NormalTexture = MasonryBumps.Value,
        OcclusionTexture = MasonryMask.Value,
        OcclusionStrength = 0.7f,
        NormalScale = 0.85f,
        Name = "masonry"
    };

    /// <summary>
    /// Flagstones: big slabs of unequal size, laid in rows, worn where feet go.
    ///
    /// It is the receiver of the only shadow in the building, and that decides everything about it. The
    /// drawing on the floor is a hard-edged silhouette and it has to stay one, so nothing here is allowed
    /// to compete with an edge: the slabs differ from each other by a fifth in tone and no more, the
    /// joints are dark but narrow, and the relief is a dish a few millimetres deep across each slab with
    /// the joint dropped below it. What that gives the sun is a floor it can rake — every slab catches
    /// the beam a shade differently and each joint throws a hair of shadow — without the clock's outline
    /// ever having to fight a pattern. Worn slabs are smoother than fresh ones, which is a roughness map
    /// doing what a hundred years of boots do.
    /// </summary>
    public static Material Flagstones() => new()
    {
        BaseColor = new Vector4(0.37f, 0.34f, 0.30f, 1f),
        Metallic = 0f,
        Roughness = 1f,
        BaseColorTexture = FlagColour.Value,
        MetallicRoughnessTexture = FlagRough.Value,
        NormalTexture = FlagBumps.Value,
        OcclusionTexture = FlagMask.Value,
        OcclusionStrength = 0.6f,

        // Seen across the room at a grazing angle for most of the chapter, which is where a normal map
        // turns to shimmer — the same reason the lounge's boards are at half.
        NormalScale = 0.65f,
        Name = "flags"
    };

    /// <summary>
    /// Cast iron: a pitted skin with rust breaking through in patches, drawn at <see cref="Close"/>
    /// because it is the ironwork a visitor stands next to.
    ///
    /// A casting is not a machined surface. It comes out of the sand with the sand's texture on it, and a
    /// century in a tower puts oxide in every pit, so the finish is a matte grey with a fine tooth and
    /// the odd bloom of orange where the water got in. The metallic map is what makes the rust rust: the
    /// skin is a metal and reflects, the oxide is a dielectric and does not, and one metallic number for
    /// the whole surface would have to be wrong about one of them.
    /// </summary>
    public static Material CastIron() => new()
    {
        BaseColor = new Vector4(0.38f, 0.38f, 0.40f, 1f),
        Metallic = 1f,
        Roughness = 1f,
        BaseColorTexture = IronColour.Value,
        MetallicRoughnessTexture = IronRough.Value,
        NormalTexture = IronBumps.Value,
        NormalScale = 0.7f,
        Name = "cast iron"
    };

    /// <summary>
    /// Brass, turned and polished: the hands, the bob and the rating nut.
    ///
    /// It shares every map with <see cref="Brushed"/> and <see cref="Gold"/> — the comparison the gallery
    /// makes on purpose, made a third time by thrift — and differs from them in the two numbers a metal
    /// has. <c>Fabric.Brass</c> is the flat version and stays where it is: this one is for parts that are
    /// looked at from a metre, under a sun, and a flat metal under a sun is a cut-out.
    /// </summary>
    public static Material Brass() => new()
    {
        BaseColor = new Vector4(0.82f, 0.62f, 0.28f, 1f),
        Metallic = 1f,
        Roughness = 0.62f,
        BaseColorTexture = BrushedColour.Value,
        MetallicRoughnessTexture = BrushedRough.Value,
        NormalTexture = BrushedBumps.Value,
        NormalScale = 0.5f,
        Name = "brass"
    };

    /// <summary>
    /// Old oak: the beams, the lintel and the door frame. Dark, dry and open-grained, with the grain
    /// running along <c>u</c>, which is along a beam when the beam is mapped by the room.
    ///
    /// <see cref="Timber"/> is a varnished door and its grain runs up; this is a field turned through a
    /// right angle, at twice the roughness and half the brightness, with a growth-ring banding under it
    /// that the door does not need. A beam reads as a beam because of the rings — the long waver of dark
    /// and light down its length is the one thing that separates timber seen from four metres from a
    /// brown box.
    /// </summary>
    public static Material Oak() => new()
    {
        BaseColor = new Vector4(0.27f, 0.19f, 0.12f, 1f),
        Metallic = 0f,
        Roughness = 1f,
        BaseColorTexture = OakColour.Value,
        MetallicRoughnessTexture = OakRough.Value,
        NormalTexture = OakBumps.Value,
        NormalScale = 0.6f,
        Name = "oak"
    };

    /// <summary>
    /// Galvanised steel: the bucket. A spangle of zinc crystals, each a few centimetres across and each a
    /// shade different, which is the only finish in the building anybody could name from the pattern
    /// alone.
    ///
    /// It is a nearest-point cell field — every pixel takes the tone and the gloss of whichever hashed
    /// point it is closest to — and that is the whole of what a spangle is: zinc freezing on a sheet from
    /// a few thousand seeds at once, each crystal a flat facet at its own angle. The roughness map does
    /// most of the work, because what a person sees of a galvanised pail is that the facets catch the
    /// light one at a time.
    /// </summary>
    public static Material Galvanised() => new()
    {
        BaseColor = new Vector4(0.66f, 0.67f, 0.69f, 1f),
        Metallic = 1f,
        Roughness = 1f,
        BaseColorTexture = ZincColour.Value,
        MetallicRoughnessTexture = ZincRough.Value,
        NormalTexture = ZincBumps.Value,
        NormalScale = 0.5f,
        Name = "galvanised"
    };

    /// <summary>
    /// Hemp rope, three strands laid right-handed: the line the drive weight hangs from.
    ///
    /// Mapped to a cylinder's own coordinates rather than the room's — <c>u</c> round the rope and <c>v</c>
    /// along it — because a rope is the one surface here whose pattern is a helix, and a helix has no
    /// planar projection. One image is one lay; the scene sets <see cref="Material.UvScale"/> to however
    /// many lays there are in the length. See <see cref="Laid"/>.
    /// </summary>
    public static Material Rope() => new()
    {
        BaseColor = new Vector4(0.58f, 0.48f, 0.32f, 1f),
        Metallic = 0f,
        Roughness = 1f,
        BaseColorTexture = RopeColour.Value,
        MetallicRoughnessTexture = RopeRough.Value,
        NormalTexture = RopeBumps.Value,
        Name = "rope"
    };

    /// <summary>
    /// Black-stained timber, satin: the planetarium's picture frames and its skirting.
    ///
    /// <see cref="Timber"/>'s grain under a stain dark enough that the grain is a texture in the highlight
    /// and nothing in the colour, which is what a stained frame is. It replaced moulded plastic on the
    /// frames, and the reason is in the picture: orange peel at four tenths of a millimetre under a cove
    /// light is a crumpled surface, and a frame round a photograph has to be the calmest thing on the wall.
    /// </summary>
    public static Material Stained() => new()
    {
        // Three hundredths. The film exposes a planetarium so that a tenth reads as mid-grey — see
        // Acoustic — and a frame at eight hundredths came out walnut. Ebonised is a number, and it is
        // this one.
        BaseColor = new Vector4(0.035f, 0.028f, 0.024f, 1f),
        Metallic = 0f,

        // Satin, not gloss, and the number is the cove's doing rather than the wood's. A picture light a
        // metre and a quarter off the wall puts a broad highlight on anything glossier than this, and on a
        // black frame a warm highlight is the whole of what shows — at a half it came out walnut.
        Roughness = 0.68f,
        BaseColorTexture = WoodColour.Value,
        NormalTexture = WoodBumps.Value,
        NormalScale = 0.35f,
        Name = "stained"
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

    // ---- the planetarium's fields -------------------------------------------------------------------

    /// <summary>
    /// One tile of the dome: how much of a perforation is under this point, and how near it is to a joint
    /// between two panels.
    ///
    /// The tile is one panel, and the room scales the image so that one panel is about two metres of real
    /// dome — see <c>Planetarium</c>, which sets <see cref="Material.UvScale"/> on a cap whose own
    /// coordinates are polar and therefore useless as they stand.
    /// </summary>
    private static (float Hole, float Joint) Punched(float u, float v)
    {
        const float pitch = 1f / 22f;

        var cu = Grain.Cell(u, pitch) - 0.5f;
        var cv = Grain.Cell(v, pitch) - 0.5f;

        var r = MathF.Sqrt(cu * cu + cv * cv);
        var hole = 1f - Grain.Step(0.13f, 0.21f, r);

        // The panel edge, on both axes, and it is the image's own edge — so the seam is where two copies of
        // this tile meet and there is exactly one of them per panel however the room scales it.
        var joint = 1f - Grain.Band(u, 0.010f, 0.990f, 0.006f) * Grain.Band(v, 0.010f, 0.990f, 0.006f);

        return (hole, joint);
    }

    /// <summary>The height field the dome's relief comes off: holes a millimetre and a half, joints four.</summary>
    private static float Punch(float u, float v)
    {
        var (hole, joint) = Punched(u, v);

        return -(hole * 0.38f + joint);
    }

    private static readonly Lazy<Texture> ScreenColour = new(() =>
        Grain.Colour(Detailed, "screen.colour", (u, v) =>
        {
            var (hole, joint) = Punched(u, v);

            // Within a per cent of flat, and the per cent is the point. A screen that is visibly mottled is
            // a screen somebody will read as a wall.
            var tone = 0.985f + 0.015f * Grain.Fbm(u, v, 7, 61, 3);

            return new Vector3(tone - 0.30f * hole - 0.22f * joint);
        }));

    private static readonly Lazy<Texture> ScreenRough = new(() =>
        Grain.Rough(Detailed, "screen.rough", (u, v) =>
        {
            var (hole, _) = Punched(u, v);

            return new Vector2(0.90f + 0.08f * Grain.Fbm(u, v, 15, 67, 2) + 0.05f * hole, 0f);
        }));

    private static readonly Lazy<Texture> ScreenBumps = new(() =>
        Grain.Bumps(Detailed, "screen.bumps", Punch, 0.004f, 2f));

    private static readonly Lazy<Texture> ScreenMask = new(() =>
        Grain.Mask(Detailed, "screen.mask", (u, v) =>
        {
            var (hole, joint) = Punched(u, v);

            return 1f - 0.75f * hole - 0.45f * joint;
        }));

    /// <summary>
    /// One panel of the wall lining: the face inside its reveal, where on the panel the sample is, and
    /// the panel's own tone.
    ///
    /// Eight hundred wide and sixteen hundred tall, which is a sheet of fabric-wrapped board a fitter
    /// carries, stood on end; and the reveal between two is ten millimetres of shadow, which is how every
    /// acoustic wall ever fitted is jointed, because cloth cannot be butted.
    /// </summary>
    private static (float Face, float Across, float Down, float Tone) Stretched(float u, float v)
    {
        const float wide = 0.5f;
        const float tall = 1f;
        const float reveal = 0.010f / Pitch;

        var cu = Grain.Cell(u, wide);
        var cv = Grain.Cell(v, tall);

        var face = Grain.Band(cu, reveal / wide, 1f - reveal / wide, 0.012f)
                   * Grain.Band(cv, reveal / tall, 1f - reveal / tall, 0.006f);

        return (face, cu, cv, Grain.Pick(Grain.Index(u, wide), Grain.Index(v, tall), 231));
    }

    /// <summary>The cloth's nap: fine fibre at two scales and no weave — a weave a millimetre across is
    /// below the texel at the building's pitch.</summary>
    private static float Nap(float u, float v) =>
        Grain.Fbm(u, v, 30, 233, 3) * 0.6f + Grain.Fbm(u, v, 90, 235, 2) * 0.4f;

    /// <summary>The height field: a panel bows out where the cloth is stretched over the board, and the
    /// reveal drops behind it. Twelve millimetres, most of it the reveal.</summary>
    private static float Padded(float u, float v)
    {
        var (face, across, down, _) = Stretched(u, v);

        return face * (0.70f + 0.24f * (1f - 0.5f * Bow(across, down)) + 0.06f * Nap(u, v));
    }

    private static readonly Lazy<Texture> AcousticColour = new(() =>
        Grain.Colour(Detailed, "acoustic.colour", (u, v) =>
        {
            var (face, _, _, tone) = Stretched(u, v);

            var lit = (0.74f + 0.26f * Nap(u, v)) * (0.94f + 0.12f * tone);
            var panel = new Vector3(lit, lit * 1.02f, lit * 1.16f);

            return Vector3.Lerp(new Vector3(0.28f, 0.28f, 0.30f), panel, face);
        }));

    private static readonly Lazy<Texture> AcousticRough = new(() =>
        Grain.Rough(Detailed, "acoustic.rough", (u, v) => new Vector2(0.94f + 0.06f * Nap(u, v), 0f)));

    private static readonly Lazy<Texture> AcousticBumps = new(() =>
        Grain.Bumps(Detailed, "acoustic.bumps", Padded, 0.012f, Pitch));

    private static readonly Lazy<Texture> AcousticMask = new(() =>
        Grain.Mask(Detailed, "acoustic.mask", (u, v) => 0.40f + 0.60f * Stretched(u, v).Face));

    /// <summary>
    /// One tile of seat cloth: a twill, which is a weave whose floats step sideways one thread a row and
    /// is therefore a diagonal rather than a grid.
    ///
    /// A plain weave is a checkerboard and reads as gingham at any scale you can see it. Every piece of
    /// contract seating fabric ever specified is a twill or a boucle for that reason, and a twill is one
    /// line: the row's offset is a function of the column.
    /// </summary>
    private static float Twill(float u, float v)
    {
        const float pitch = 1f / 46f;

        var row = MathF.Floor(v / pitch);
        var thread = Grain.Cell(u + row * pitch * 0.5f, pitch);
        var course = Grain.Cell(v, pitch);

        // Each thread is a half-round, so the profile across it is a cosine; the course is a shallower one,
        // because a woven face shows the warp much more than the weft.
        var weave = MathF.Pow(MathF.Sin(thread * MathF.PI), 0.7f) * 0.72f
                    + MathF.Pow(MathF.Sin(course * MathF.PI), 0.7f) * 0.28f;

        // And the nap, which is what makes wool wool: loose fibre standing off the weave, finer than it
        // and not aligned with it at all.
        return weave * 0.82f + Grain.Fbm(u, v, 96, 401, 3) * 0.18f;
    }

    private static readonly Lazy<Texture> TwillColour = new(() =>
        Grain.Colour(Detailed, "twill.colour", (u, v) =>
        {
            var face = 0.76f + 0.42f * Twill(u, v);

            // Two tones in the yarn, a shade apart, which is what a woven cloth is made of and is most of
            // what stops a close-up reading as painted card.
            var dye = 0.94f + 0.12f * Grain.Fbm(u, v, 23, 409, 2);

            return new Vector3(face * dye, face * dye * 0.99f, face * dye * 1.02f);
        }));

    private static readonly Lazy<Texture> TwillRough = new(() =>
        Grain.Rough(Detailed, "twill.rough", (u, v) =>
            new Vector2(0.90f + 0.10f * (1f - Twill(u, v)), 0f)));

    private static readonly Lazy<Texture> TwillBumps = new(() =>
        Grain.Bumps(Detailed, "twill.bumps", Twill, 0.004f, Close));

    /// <summary>Carpet: two frequencies of fibre, one for the tuft and one for the pile inside it.</summary>
    private static float Tufted(float u, float v) =>
        Grain.Fbm(u, v, 26, 137, 3) * 0.55f + Grain.Fbm(u, v, 74, 149, 2) * 0.45f;

    /// <summary>
    /// The pile, and a scatter of pale flecks through it.
    ///
    /// The field is drawn at a third and the base colour is three times what it was, and the arithmetic
    /// comes out where it started — a tenth — with one difference: a fleck can now be brighter than the
    /// carpet, which no map can do when the base colour is the ceiling. Every planetarium and cinema
    /// carpet ever laid has a scatter in it, for the same reason this one does — a plain dark floor forty
    /// square metres across is a floor a person cannot see the edge of.
    /// </summary>
    private static readonly Lazy<Texture> CarpetColour = new(() =>
        Grain.Colour(Detailed, "carpet.colour", (u, v) =>
        {
            var fibre = 0.30f + 0.16f * Tufted(u, v);
            var field = new Vector3(fibre, fibre * 0.97f, fibre * 1.04f);

            // One cell in seven carries a tuft of the pale yarn, at a hashed spot inside its cell so the
            // scatter has no grid in it, and never near enough the cell's edge to be cut by the tile.
            const int cells = 40;

            var cx = (int)MathF.Floor(u * cells);
            var cy = (int)MathF.Floor(v * cells);
            var pick = Grain.Pick(cx, cy, 241);

            if (pick < 0.86f)
                return field;

            var dx = u * cells - (cx + 0.25f + 0.5f * Grain.Pick(cx, cy, 243));
            var dy = v * cells - (cy + 0.25f + 0.5f * Grain.Pick(cx, cy, 245));
            var fleck = MathF.Exp(-(dx * dx + dy * dy) * 60f);

            return Vector3.Lerp(field, new Vector3(0.80f, 0.82f, 0.95f), fleck * (0.6f + 0.4f * pick));
        }));

    private static readonly Lazy<Texture> CarpetRough = new(() =>
        Grain.Rough(Detailed, "carpet.rough", (u, v) =>
            new Vector2(0.94f + 0.06f * Tufted(u, v), 0f)));

    private static readonly Lazy<Texture> CarpetBumps = new(() =>
        Grain.Bumps(Detailed, "carpet.bumps", Tufted, 0.003f, Pitch));

    // ---- the tower's fields -------------------------------------------------------------------------

    /// <summary>Drawn at two hundred and fifty-six rather than one ninety-two: a joint in stone is ten
    /// millimetres, and at the building's pitch that is a texel and a half at the smaller size, which is
    /// a joint that blurs into the block on one side of it.</summary>
    private const int Coarse = 256;

    /// <summary>The five courses of one image of ashlar, as fractions of its height, top down: two hundred
    /// and ninety to three hundred and fifty millimetres at the building's pitch, which is a tower.</summary>
    private static readonly float[] Courses = [0f, 0.22f, 0.40f, 0.62f, 0.81f, 1f];

    /// <summary>And the three rows of one image of flags: three hundred and fifty to six hundred and
    /// seventy.</summary>
    private static readonly float[] FlagRows = [0f, 0.22f, 0.58f, 1f];

    /// <summary>
    /// One block of a coursed surface — ashlar or flags — given the row boundaries: the face between
    /// joints, where on the block the sample is, and two hashed numbers that are the block's own.
    ///
    /// <b>The hashes are taken on the block's index wrapped to the row</b>, and the wrap is what keeps the
    /// image tileable. A row is shifted by a hashed fraction so its joints do not stack on the row below,
    /// which means one block always straddles the image's edge — and a block whose two halves are hashed
    /// differently is a block with a step in it at every repeat.
    /// </summary>
    private static (float Face, float Across, float Down, float Tone, float Hue, float Halo) Coursed(
        float u, float v, float[] rows, float joint, float wear, int most, int seed)
    {
        // <b>The joints wander, and it is the wander that makes this stone rather than tile.</b> A tile is
        // cut by a machine and a block is cut by a man, and the difference at four metres is that no joint
        // in a stone wall is a ruler line: it drifts by a few millimetres along its length, and the eye
        // reads the drift before it reads anything else. A tileable field, so the drift meets itself at
        // the image's edge — and the same field is used below to find the block, so a block's index is
        // decided in the same wandering coordinates its edges are drawn in.
        var drift = 0.012f;
        var su = u + drift * (Grain.Fbm(u, v, 7, seed + 6, 2) - 0.5f);
        var sv = v + drift * (Grain.Fbm(u, v, 7, seed + 8, 2) - 0.5f);

        var cv = Grain.Cell(sv, 1f);
        var row = 0;

        while (row < rows.Length - 2 && cv >= rows[row + 1])
            row++;

        var top = rows[row];
        var height = rows[row + 1] - top;

        var pick = Grain.Pick(row, 0, seed);
        var blocks = 2 + (int)MathF.Min(most - 2, MathF.Floor(pick * (most - 1)));
        var pitch = 1f / blocks;
        var along = su + Grain.Pick(row, 1, seed);

        var cu = Grain.Cell(along, pitch);
        var down = (cv - top) / height;

        var face = Grain.Band(cu, joint / pitch, 1f - joint / pitch, wear / pitch)
                   * Grain.Band(down, joint / height, 1f - joint / height, wear / height);

        // And a soft band inside every edge, for the grime that collects along a joint: nought at the
        // joint, one a hand's breadth in.
        const float reach = 0.09f;

        var halo = Grain.Band(cu, joint / pitch, 1f - joint / pitch, reach / pitch)
                   * Grain.Band(down, joint / height, 1f - joint / height, reach / height);

        var index = ((Grain.Index(along, pitch) % blocks) + blocks) % blocks;

        return (face, cu, down,
            Grain.Pick(index * 7 + row, row, seed + 2),
            Grain.Pick(index * 7 + row, row + 1, seed + 4),
            halo);
    }

    /// <summary>Ashlar: twelve millimetres of joint at the building's pitch, an arris worn back over
    /// twenty, and two or three blocks to a course.</summary>
    private static (float Face, float Across, float Down, float Tone, float Hue, float Halo) Ashlar(float u, float v) =>
        Coursed(u, v, Courses, 0.012f / Pitch, 0.020f / Pitch, 3, 91);

    /// <summary>Flags: sixteen millimetres of joint, the edges rounded over thirty, and two to four
    /// slabs to a row.</summary>
    private static (float Face, float Across, float Down, float Tone, float Hue, float Halo) Flag(float u, float v) =>
        Coursed(u, v, FlagRows, 0.016f / Pitch, 0.030f / Pitch, 4, 111);

    /// <summary>How far a sample is from the middle of its block, nought at the centre and one at the
    /// corners: the pillow on a block of stone and the dish in a flag are both this.</summary>
    private static float Bow(float across, float down)
    {
        var dx = across * 2f - 1f;
        var dy = down * 2f - 1f;

        return Math.Clamp((dx * dx + dy * dy) * 0.5f, 0f, 1f);
    }

    private static readonly Lazy<Texture> MasonryColour = new(() =>
        Grain.Colour(Coarse, "masonry.colour", (u, v) =>
        {
            var (face, _, _, tone, hue, halo) = Ashlar(u, v);

            // Sandstone: buff, with a run of warmer and cooler blocks through it, because a quarry is not
            // one colour and a mason lays what he is sent. The run is a sixth from block to block and no
            // more — the first cut of this was a third, and a third is a chequerboard, which is what glazed
            // tile does and stone does not. Then the bed's own mottle at two scales, the grime that gathers
            // along every joint, and a sand-grain tooth over the lot.
            var warm = new Vector3(1f, 0.95f, 0.87f);
            var cool = new Vector3(0.93f, 0.94f, 0.96f);

            var stone = Vector3.Lerp(cool, warm, Grain.Step(0.25f, 0.75f, hue))
                        * (0.86f + 0.16f * tone)
                        * (0.90f + 0.14f * Grain.Fbm(u, v, 2, 95, 3))
                        * (0.92f + 0.12f * Grain.Fbm(u, v, 6, 97, 3))
                        * (0.92f + 0.14f * Grain.Fbm(u, v, 48, 99, 2))
                        * (0.84f + 0.16f * halo);

            // The joint: raked mortar in shadow, which is dark whatever the mortar was. Pale joints are the
            // other half of what made the first cut read as tile — a tile's grout is flush and catches the
            // light; a raked joint is a groove and does not.
            var mortar = new Vector3(0.42f, 0.40f, 0.37f) * (0.85f + 0.30f * Grain.Fbm(u, v, 40, 101, 2));

            return Vector3.Lerp(mortar, stone, face);
        }));

    private static readonly Lazy<Texture> MasonryRough = new(() =>
        Grain.Rough(Coarse, "masonry.rough", (u, v) =>
        {
            var (face, _, _, _, _, _) = Ashlar(u, v);

            // Dead matt. A dressed stone has no sheen at any distance a person stands from a wall, and the
            // three hundredths this varies by is the tooth catching a raking light unevenly.
            return new Vector2(1f - face * 0.05f - 0.03f * Grain.Fbm(u, v, 30, 103, 2), 0f);
        }));

    /// <summary>The height field: a block stands nine millimetres proud of its raked joint, pillowed
    /// towards its middle, with a millimetre of tooth over it.</summary>
    private static float Dressed(float u, float v)
    {
        var (face, across, down, _, _, _) = Ashlar(u, v);

        return face * (0.55f + 0.35f * (1f - 0.35f * Bow(across, down)) + 0.10f * Grain.Fbm(u, v, 44, 105, 3));
    }

    private static readonly Lazy<Texture> MasonryBumps = new(() =>
        Grain.Bumps(Coarse, "masonry.bumps", Dressed, 0.009f, Pitch));

    private static readonly Lazy<Texture> MasonryMask = new(() =>
        Grain.Mask(Coarse, "masonry.mask", (u, v) =>
        {
            var (face, _, _, _, _, halo) = Ashlar(u, v);

            return 0.50f + 0.50f * face * (0.85f + 0.15f * halo);
        }));

    private static readonly Lazy<Texture> FlagColour = new(() =>
        Grain.Colour(Coarse, "flags.colour", (u, v) =>
        {
            var (face, across, down, tone, hue, halo) = Flag(u, v);

            var warm = new Vector3(1f, 0.95f, 0.88f);
            var cool = new Vector3(0.88f, 0.90f, 0.95f);

            var slab = Vector3.Lerp(cool, warm, Grain.Step(0.3f, 0.7f, hue))
                       * (0.84f + 0.20f * tone)
                       * (0.88f + 0.16f * Grain.Fbm(u, v, 2, 119, 3))
                       * (0.90f + 0.14f * Grain.Fbm(u, v, 7, 121, 3))
                       * (0.93f + 0.12f * Grain.Fbm(u, v, 52, 123, 2))
                       * (0.80f + 0.20f * halo);

            // A worn slab is paler in the middle, where the grit has come off it: the same hash that
            // lowers its roughness lifts its colour, because both are one thing — boots.
            slab *= 1f + 0.06f * Worn(hue, across, down);

            // The joints are dirt, not mortar. A floor's pointing is whatever has been trodden into it.
            var dirt = new Vector3(0.30f, 0.28f, 0.26f) * (0.80f + 0.30f * Grain.Fbm(u, v, 36, 125, 2));

            return Vector3.Lerp(dirt, slab, face);
        }));

    /// <summary>How worn a point on a flag is: the slab's own hash, strongest in its middle.</summary>
    private static float Worn(float hue, float across, float down) =>
        Grain.Step(0.35f, 0.8f, hue) * (1f - Bow(across, down));

    private static readonly Lazy<Texture> FlagRough = new(() =>
        Grain.Rough(Coarse, "flags.rough", (u, v) =>
        {
            var (face, across, down, _, hue, _) = Flag(u, v);

            return new Vector2(
                1f - face * (0.10f + 0.30f * Worn(hue, across, down)) - 0.04f * Grain.Fbm(u, v, 30, 127, 2),
                0f);
        }));

    /// <summary>The height field: a flag dished by a few millimetres and its joint ten below the rim.</summary>
    private static float Bedded(float u, float v)
    {
        var (face, across, down, _, _, _) = Flag(u, v);

        return face * (0.62f + 0.28f * (1f - 0.6f * Bow(across, down)) + 0.10f * Grain.Fbm(u, v, 40, 129, 3));
    }

    private static readonly Lazy<Texture> FlagBumps = new(() =>
        Grain.Bumps(Coarse, "flags.bumps", Bedded, 0.010f, Pitch));

    private static readonly Lazy<Texture> FlagMask = new(() =>
        Grain.Mask(Coarse, "flags.mask", (u, v) =>
        {
            var (face, _, _, _, _, halo) = Flag(u, v);

            return 0.55f + 0.45f * face * (0.85f + 0.15f * halo);
        }));

    /// <summary>Casting sand: fine noise pushed hard towards its low end, so the field is a scatter of
    /// pits in a skin rather than an even fuzz.</summary>
    private static float Pitted(float u, float v) => MathF.Pow(Grain.Fbm(u, v, 56, 201, 3), 2.6f);

    /// <summary>
    /// Where the rust is: a few patches, a few centimetres across, mottled inside.
    ///
    /// A tenth of the surface and not a third. The first cut of this covered a third and rendered as a
    /// leopard — a top plate blotched orange from end to end, which is scrap and not a clock. Iron in a
    /// dry tower blooms where water has stood on it, which is a corner here and a foot there.
    /// </summary>
    private static float Rusted(float u, float v) =>
        Grain.Step(0.66f, 0.82f, Grain.Fbm(u, v, 5, 203, 3)) * (0.5f + 0.5f * Grain.Fbm(u, v, 38, 205, 2));

    private static readonly Lazy<Texture> IronColour = new(() =>
        Grain.Colour(Detailed, "iron.colour", (u, v) =>
        {
            var pits = Pitted(u, v);
            var rust = Rusted(u, v);

            var skin = new Vector3(0.50f, 0.50f, 0.53f) * (1f - 0.55f * pits)
                       * (0.88f + 0.20f * Grain.Fbm(u, v, 24, 207, 2));

            var oxide = new Vector3(0.46f, 0.25f, 0.11f) * (0.70f + 0.50f * Grain.Fbm(u, v, 60, 209, 2));

            return Vector3.Lerp(skin, oxide, rust * 0.7f);
        }));

    private static readonly Lazy<Texture> IronRough = new(() =>
        Grain.Rough(Detailed, "iron.rough", (u, v) =>
        {
            var pits = Pitted(u, v);
            var rust = Rusted(u, v);

            return new Vector2(
                MathF.Min(1f, 0.58f + 0.32f * pits + 0.30f * rust),
                0.80f * (1f - rust) + 0.20f * rust);
        }));

    private static readonly Lazy<Texture> IronBumps = new(() =>
        Grain.Bumps(Detailed, "iron.bumps", (u, v) => 1f - Pitted(u, v), 0.0006f, Close));

    /// <summary>
    /// Oak, sawn along the trunk: growth rings cut lengthways, which is a set of long uneven stripes
    /// down the length, with the fibre under them.
    /// </summary>
    private static float Heart(float u, float v)
    {
        var waver = Grain.Fbm(u, v * 2f, 2, 211, 3);
        var rings = 0.5f + 0.5f * MathF.Sin((v * 11f + waver * 1.4f) * MathF.Tau);
        var fibre = Grain.Fbm(u, v * 6f, 4, 213, 4);

        return MathF.Pow(rings, 1.6f) * 0.55f + fibre * 0.45f;
    }

    private static readonly Lazy<Texture> OakColour = new(() =>
        Grain.Colour(Detailed, "oak.colour", (u, v) =>
        {
            var g = 0.60f + 0.40f * Heart(u, v);
            var tone = 0.90f + 0.15f * Grain.Fbm(u, v, 3, 215, 2);

            return new Vector3(g * tone, g * tone * 0.86f, g * tone * 0.68f);
        }));

    private static readonly Lazy<Texture> OakRough = new(() =>
        Grain.Rough(Detailed, "oak.rough", (u, v) => new Vector2(0.72f + 0.20f * (1f - Heart(u, v)), 0f)));

    private static readonly Lazy<Texture> OakBumps = new(() =>
        Grain.Bumps(Detailed, "oak.bumps", Heart, 0.0012f, Pitch));

    /// <summary>
    /// The spangle: which crystal a point is in, as that crystal's tone and gloss, and how near the point
    /// is to the boundary with the next one. A jittered grid of seeds, nearest of nine, wrapped so it
    /// tiles.
    /// </summary>
    private static (float Tone, float Gloss, float Edge) Spangle(float u, float v)
    {
        // Twenty-six to the image, which is fifteen millimetres a crystal at Close. Twelve was a mosaic.
        const int cells = 26;

        var x = u * cells;
        var y = v * cells;

        var cx = (int)MathF.Floor(x);
        var cy = (int)MathF.Floor(y);

        var best = float.MaxValue;
        var second = float.MaxValue;
        var tone = 0f;
        var gloss = 0f;

        for (var j = -1; j <= 1; j++)
        for (var i = -1; i <= 1; i++)
        {
            var ix = cx + i;
            var iy = cy + j;

            var wx = ((ix % cells) + cells) % cells;
            var wy = ((iy % cells) + cells) % cells;

            var dx = x - (ix + Grain.Pick(wx, wy, 301));
            var dy = y - (iy + Grain.Pick(wx, wy, 303));
            var d = dx * dx + dy * dy;

            if (d < best)
            {
                second = best;
                best = d;
                tone = Grain.Pick(wx, wy, 305);
                gloss = Grain.Pick(wx, wy, 307);
            }
            else if (d < second)
            {
                second = d;
            }
        }

        var edge = 1f - Grain.Step(0.02f, 0.10f, MathF.Sqrt(second) - MathF.Sqrt(best));

        return (tone, gloss, edge);
    }

    private static readonly Lazy<Texture> ZincColour = new(() =>
        Grain.Colour(Detailed, "zinc.colour", (u, v) =>
        {
            // A tenth between the brightest crystal and the dullest, and a shade at the boundary. A
            // spangle is a thing you notice in the highlight, not in the colour: the first cut had a
            // quarter in the colour and read as a disco ball.
            var (tone, _, edge) = Spangle(u, v);
            var c = (0.90f + 0.10f * tone) * (0.96f + 0.06f * Grain.Fbm(u, v, 40, 309, 2)) - 0.04f * edge;

            return new Vector3(c, c * 1.005f, c * 1.02f);
        }));

    private static readonly Lazy<Texture> ZincRough = new(() =>
        Grain.Rough(Detailed, "zinc.rough", (u, v) =>
        {
            var (_, gloss, edge) = Spangle(u, v);

            return new Vector2(0.32f + 0.24f * gloss + 0.08f * edge, 1f);
        }));

    private static readonly Lazy<Texture> ZincBumps = new(() =>
        Grain.Bumps(Detailed, "zinc.bumps", (u, v) =>
        {
            var (tone, _, _) = Spangle(u, v);

            return tone * 0.8f + 0.2f * Grain.Fbm(u, v, 30, 311, 2);
        }, 0.0002f, Close));

    /// <summary>
    /// Three strands, right-handed: a crest wherever <c>3u − v</c> is whole, which is three helices each
    /// going once round the rope in three lays. The fibre runs with the strand.
    /// </summary>
    private static float Laid(float u, float v)
    {
        var strand = 0.5f + 0.5f * MathF.Cos((u * 3f - v) * MathF.Tau);
        var fibre = Grain.Fbm(u * 3f - v, v * 4f, 6, 313, 2);

        return MathF.Pow(strand, 0.8f) * 0.8f + fibre * 0.2f;
    }

    private static readonly Lazy<Texture> RopeColour = new(() =>
        Grain.Colour(Fine, "rope.colour", (u, v) =>
        {
            var c = (0.72f + 0.30f * Laid(u, v)) * (0.90f + 0.20f * Grain.Fbm(u * 3f - v, v * 4f, 6, 315, 2));

            return new Vector3(c, c * 0.97f, c * 0.92f);
        }));

    private static readonly Lazy<Texture> RopeRough = new(() =>
        Grain.Rough(Fine, "rope.rough", (u, v) => new Vector2(0.86f + 0.14f * (1f - Laid(u, v)), 0f)));

    /// <summary>Sixty millimetres across, which is about a rope's circumference: the tile is one lay
    /// along and once round, and it is the round that sets the slope.</summary>
    private static readonly Lazy<Texture> RopeBumps = new(() =>
        Grain.Bumps(Fine, "rope.bumps", Laid, 0.004f, 0.06f));
}
