using System.Numerics;

namespace Ava3D.Demo.Story;

/// <summary>
/// What happens under the dome once the house lights go: a sky, a galaxy across it, a nebula, three
/// planets one after another, and something falling through it every few seconds.
///
/// <b>It has a beginning now and it did not, and that is the change everything else here follows from.</b>
/// The first version was a set of periodic functions with no start anywhere in them — the sky on five
/// minutes, the nebula on ninety-six seconds, a planet on seventy-four — so that a viewer could join at any
/// moment and find a show already running. That is a good property and it bought the wrong thing. What a
/// person sitting down in a planetarium expects is that the room goes dark and then <i>something begins</i>,
/// and a sky that was already there when the lights went out is a sky that was always going to be there.
/// So this takes a clock that starts at nought when the room goes dark, and the eighteen seconds after
/// that are staged: the stars come out from the zenith down, the galaxy resolves behind them, the gas
/// fades up, and the planets arrive one at a time.
///
/// The seek-ability is not lost and was never the reason for the other design. Everything below is still a
/// pure function of one number — no state, no integration, no "what happened last frame" — so the film can
/// be scrubbed to any second of it and gets the same frame every time. What changed is only which number:
/// seconds since the curtain instead of seconds since the film began. See <c>Stars.Update</c>, which
/// subtracts one from the other, and <c>Rounds.Showtime</c>, which does the same thing from the moment
/// somebody sits down in the free walk.
///
/// <b>Nothing here owns the dome.</b> The dome is a lit white screen belonging to the room — see
/// <see cref="Planetarium"/> — and it goes dark because the coves go out, with nothing driving it. What
/// this hangs is a shell of its own a hundred millimetres inside it, additive, so that at level nought
/// there is provably nothing on the ceiling: not a dim sky, not a tenth of a nebula, nothing. That is the
/// difference between a screen and a screen with something already on it, and it is the whole of what a
/// person coming through the door is supposed to see.
///
/// <b>The stars are sprites and the galaxy is a texture, and it took both.</b> A dome's texture
/// coordinates are polar — the mesh is a spherical cap and <see cref="Primitives.Sphere"/> maps longitude
/// across and latitude down — so every image on it is pinched to nothing at the crown, which is the one
/// part of a planetarium dome everybody is looking at. Anything smooth survives that: the galaxy is a wide
/// soft ridge, it is nowhere near the crown, and its distortion is invisible. Points do not. Stars painted
/// into the image came out as commas at the rim and as a smear at the top, so they are four hundred
/// billboards on a shell instead, and the fourteen brightest carry a diffraction cross because that is
/// what the eye reads "bright" as and no amount of extra brightness will do it instead.
/// </summary>
internal sealed class Sky
{
    /// <summary>How long the show runs before the house lights come back, in seconds. The chapter's
    /// seated shot is exactly this and the free walk loops it.</summary>
    public const float Watched = 18f;

    /// <summary>How many stars, how many of them are named, and how much gas.</summary>
    private const int Stars = 380;

    private const int Bright = 14;
    private const int Puffs = 78;

    /// <summary>The gas sheet is this many plates square: nine clouds out of one upload.</summary>
    private const int Sheets = 3;

    /// <summary>And the dust in front of it, which is the only thing under this dome that subtracts.</summary>
    private const int Soots = 22;

    /// <summary>How many photographs hang on the wall next door. Four, and it is their own number rather
    /// than the gas sheet's — they were the same constant once and that was a coincidence waiting to be
    /// a bug.</summary>
    private const int Framed = 4;

    /// <summary>Three meteor tracks, each of them a head and twenty-one beads behind it.</summary>
    private const int Tracks = 3;

    private const int Beads = 22;

    /// <summary>
    /// The middle of the show, in the room's own coordinates: north-west of the seats, three metres up.
    ///
    /// It is a measured point and not a chosen one. From the chair it is three metres four away and thirty
    /// degrees above the horizon, which is where a person watching something on a ceiling actually wants
    /// it — straight up is a crick and straight ahead is a wall. <c>Planetarium.Facing</c> is the bearing
    /// from the seat to here, and <c>Planetarium.Zenith</c> is this in world coordinates.
    ///
    /// <b>It moved out by half a metre and the planets are why.</b> Everything here is staged around it,
    /// including a ringed planet a metre and six across, and at the old distance the outer edge of that
    /// ring came within two metres of somebody's face. What that looks like through a fifty-five-degree
    /// lens is not a planet going past, it is the ceiling falling in.
    /// </summary>
    public static readonly Vector3 Heart = new(-1.0f, 3.05f, 1.35f);

    /// <summary>Where the eye is in the chair, in the same coordinates, so the staging below can be
    /// measured against it rather than guessed at.</summary>
    private static readonly Vector3 Chair = Planetarium.Rake + new Vector3(0f, 1.30f, 0f);

    /// <summary>Away from the audience, along the floor: the direction the seats are looking.</summary>
    private static readonly Vector3 Away = Flatten(Heart - Chair);

    /// <summary>And across their view, along the floor. A planet crosses along this, which is the only
    /// axis a pass can be on that keeps the same distance from the chair at both ends.</summary>
    private static readonly Vector3 Sweepline = new(Away.Z, 0f, -Away.X);

    /// <summary>How far a planet travels each side of <see cref="Heart"/>, how far it bows away at the
    /// ends of that, and how far it drops. The bow is what keeps the ends of a pass off the wall — a
    /// straight chord this long would put the outer end within half a metre of the plaster.</summary>
    private const float Pass = 2.35f;

    private const float Bow = 0.95f;
    private const float Dip = 0.40f;

    /// <summary>
    /// The three of them, in the order they arrive and with the seconds each one owns.
    ///
    /// <b>One at a time, and it is a decision about a room rather than about a solar system.</b> Three
    /// planets in the air together is an orrery, which is a lovely thing and does not fit: the audience is
    /// three metres from the middle of the show and an orrery big enough to read is an orrery whose outer
    /// orbit passes through the second row. A procession has no such problem — each one gets the whole of
    /// the good staging in turn, at the size that staging can afford, and the two that are not on are not
    /// anywhere.
    ///
    /// The seconds are not equal. Saturn gets the longest because it has the most to look at, Mars the
    /// shortest because it is a ball with a cap on it, and they add to <see cref="Watched"/> exactly — so
    /// any eighteen seconds of this shows all three however it is joined.
    /// </summary>
    private static readonly (float From, float To)[] Turns =
    [
        (0f, 4.6f), (4.6f, 11f), (11f, Watched)
    ];

    /// <summary>How far the lamp stands off whichever planet is on, and how far it reaches.
    ///
    /// <b>Both are small and the dome is why.</b> There is a metre and six tenths of air between the show
    /// and the ceiling, and a lamp bright enough to model a sun at any useful distance from a planet is a
    /// lamp putting a hot spot on a white screen — which is the one surface in this room that must stay
    /// black while the show is on. Every direction was tried: above is the dome, below is the projector,
    /// sideways is the dome again at the ends of a pass. What is left is close. Three quarters of a metre
    /// off the middle of a planet with a range of ninety-five centimetres reaches the lit half of it and
    /// provably nothing else in the room, because <see cref="PointLight.Range"/> is a hard zero and not a
    /// falloff.</summary>
    private const float Standoff = 0.75f;

    private const float Reach = 1.40f;

    private const float Turning = 210f;
    private const float Drifting = 260f;
    private const float Breathing = 22f;

    /// <summary>
    /// The gas, as emission lines rather than as a palette.
    ///
    /// A nebula is not coloured, it is a handful of atoms each of which emits at exactly one wavelength,
    /// and the four here are the four every photograph of one is made of: hydrogen alpha, doubly ionised
    /// oxygen, the blue of dust reflecting a young star, and the sodium-ish gold of the warm edges. Mixing
    /// between neighbours in that list is what gives a cloud a temperature gradient; mixing between
    /// arbitrary colours gives it a tie-dye.
    /// </summary>
    private static readonly Vector3[] Gas =
    [
        new(0.95f, 0.22f, 0.30f),
        new(0.24f, 0.86f, 0.78f),
        new(0.32f, 0.46f, 0.95f),
        new(0.98f, 0.70f, 0.34f)
    ];

    private readonly Material _shell;

    private readonly SpriteNode[] _stars = new SpriteNode[Stars];
    private readonly float[] _twinkle = new float[Stars];
    private readonly float[] _steady = new float[Stars];
    private readonly float[] _depth = new float[Stars];

    private readonly SpriteNode[] _spikes = new SpriteNode[Bright];

    private readonly SpriteNode[] _gas = new SpriteNode[Puffs];
    private readonly Vector3[] _seated = new Vector3[Puffs];
    private readonly float[] _phase = new float[Puffs];
    private readonly float[] _weight = new float[Puffs];

    private readonly SpriteNode[] _soot = new SpriteNode[Soots];
    private readonly Vector3[] _settled = new Vector3[Soots];
    private readonly float[] _shade = new float[Soots];

    private readonly SpriteNode _core;

    private readonly Planet[] _planets;

    private readonly SpriteNode[][] _trail = new SpriteNode[Tracks][];
    private readonly SpriteNode[] _flare = new SpriteNode[Tracks];

    public Sky(Node root)
    {
        // <b>Two falloffs, and the stars get the tight one.</b> A star at the old size and softness came
        // out thirty pixels across with no core in it — which is not a star, it is a smudge, and four
        // hundred smudges is a fogged lens. Alpha to the fifth is a bright point with a small halo round
        // it, which is what a star looks like through anything.
        var spark = Texture.Glow(96, 5.2f);
        var glow = Texture.Glow(64, 2.6f);
        var soft = Texture.Glow(96, 1.5f);
        var cross = Spiked.Value;
        var plates = Sheet.Value;
        var dust = Soot.Value;

        // ---- the shell ------------------------------------------------------------------------------
        //
        // The galaxy, and everything too faint and too smooth to be worth a billboard. It is a cap of its
        // own a hundred millimetres inside the room's dome, additive and unlit — so at level nought it
        // contributes exactly nothing and the white screen behind it is the whole of what is on the
        // ceiling. Inverted, because a sphere's normals point out and this one is only ever seen from
        // inside; unlit means no normal is read, and it is still wrong to leave a mesh facing the wrong
        // way for whoever changes the shading model next.
        _shell = new Material
        {
            BaseColor = Vector4.Zero,
            BaseColorTexture = Vaulting.Value,
            Shading = ShadingModel.Unlit,
            Blend = BlendMode.Additive,
            DepthWrite = false,
            DoubleSided = true,
            Name = "sky.shell"
        };

        root.Children.Add(new MeshNode(
            Fabric.Inverted(Primitives.Sphere(
                Planetarium.Bowl - 0.10f, 96, 36,
                latitudeDegrees: float.RadiansToDegrees(Planetarium.Sweep))),
            _shell)
        {
            Position = Planetarium.Vault,
            RenderOrder = 1,
            Name = "sky.vault"
        });

        // ---- the stars ------------------------------------------------------------------------------
        //
        // On a shell a hundred and eighty millimetres inside the dome, which keeps them behind everything
        // else in the room and in front of the galaxy. They are depth tested — a star behind a planet is
        // behind the planet — and they write no depth, so two that overlap add rather than fight.
        var field = new Node { Name = "sky.stars" };
        var named = 0;

        for (var i = 0; i < Stars; i++)
        {
            // Hashed rather than random, because a sky that is a different sky on every run is a sky
            // nobody can be shown a picture of. Cosine-weighted down from the crown so the density is even
            // over the dome rather than even in the angle, which is the same correction the polar texture
            // could not have.
            var azimuth = Grain.Pick(i, 0, 907) * MathF.Tau;
            var toward = Grain.Pick(i, 1, 907);
            var zenith = MathF.Acos(1f - toward * (1f - MathF.Cos(Planetarium.Starry)));

            var ray = new Vector3(
                MathF.Sin(zenith) * MathF.Cos(azimuth),
                MathF.Cos(zenith),
                MathF.Sin(zenith) * MathF.Sin(azimuth));

            // Magnitude, cubed, which is the distribution a real sky has: a handful you could name and
            // several hundred you could not. Cubing is doing all the work — the same hash spread evenly
            // gives four hundred stars of the same brightness, which is a ceiling with holes in it.
            var pick = Grain.Pick(i, 2, 907);
            var mass = pick * pick * pick;

            _twinkle[i] = Grain.Pick(i, 4, 907) * MathF.Tau;
            _steady[i] = 0.28f + 0.72f * mass;
            _depth[i] = toward;

            _stars[i] = new SpriteNode
            {
                Texture = spark,
                Position = Planetarium.Vault + ray * (Planetarium.Bowl - 0.18f),

                // Two fifths of what it was. The shell is four and seven tenths above the seat, so a
                // sprite a tenth of a metre across subtends more than a degree — thirty pixels of soft
                // white where the eye is expecting a point. Small and sharp reads as far away; large and
                // soft reads as near and out of focus, which is the one thing a sky must never do.
                Size = new Vector2(0.018f + 0.066f * mass),
                Color = Spectrum(Grain.Pick(i, 3, 907)),
                Blend = BlendMode.Additive,
                DepthWrite = false,
                RenderOrder = 2,
                Name = "star"
            };

            field.Children.Add(_stars[i]);

            // And the named ones: a second, much larger, much fainter sprite carrying a four-armed cross.
            // It is a lens artefact and there is no lens, which is exactly why it belongs here — a
            // diffraction spike is the convention the eye has been taught to read as "this one is bright",
            // and brightness alone cannot say it because a sprite that is brighter is only a bigger blob.
            if (named >= Bright || pick < 0.985f)
                continue;

            _spikes[named] = new SpriteNode
            {
                Texture = cross,
                Position = _stars[i].Position,
                Size = new Vector2(0.34f),
                Color = _stars[i].Color,
                Blend = BlendMode.Additive,
                DepthWrite = false,
                RenderOrder = 2,
                Name = "star.spike"
            };

            field.Children.Add(_spikes[named]);
            named++;
        }

        // Whatever the hash did not hand out, parked at nothing rather than left null.
        for (var i = named; i < Bright; i++)
        {
            _spikes[i] = new SpriteNode { Texture = cross, Opacity = 0f, Name = "star.spike" };
            field.Children.Add(_spikes[i]);
        }

        root.Children.Add(field);

        // ---- the gas --------------------------------------------------------------------------------
        //
        // Seventy-eight billboards on nine plates, and the plates are one upload: a three-by-three sheet
        // read a ninth at a time by SpriteNode.UseFrame, which is the same feature the pattern shop
        // exhibits and is here doing the job it is actually for. Nine different clouds out of one texture
        // binding is the difference between a nebula and seventy-eight copies of one puff.
        //
        // <b>Soft, and that is the whole exhibit.</b> An additive billboard meets the dome, a planet and
        // its neighbours at a hard line — the quad simply stops — and seventy-eight of those lines is a
        // pile of cards, not a cloud. SoftDistance fades each one out over the last metre and a half
        // before whatever is behind it, so the gas goes *into* the dome instead of being stuck to it.
        var cloud = new Node { Name = "sky.gas" };

        for (var i = 0; i < Puffs; i++)
        {
            // Seated in an ellipsoid, flattened in Y, because gas under a dome that is as tall as it is
            // wide reads as a ball of cotton wool. Five metres across and one and a half deep is a cloud
            // lying in the sky rather than hanging in the room.
            var a = Grain.Pick(i, 0, 331) * MathF.Tau;
            var b = MathF.Acos(1f - 2f * Grain.Pick(i, 1, 331));
            var r = MathF.Pow(Grain.Pick(i, 2, 331), 0.42f);

            _seated[i] = new Vector3(
                MathF.Sin(b) * MathF.Cos(a) * 2.5f,
                MathF.Cos(b) * 0.85f,
                MathF.Sin(b) * MathF.Sin(a) * 2.3f) * r;

            _phase[i] = Grain.Pick(i, 3, 331) * MathF.Tau;

            // Bright in the middle and thin at the rim, so the cloud has a core.
            //
            // <b>The numbers are small and they are small because additive draws sum.</b> A sight line
            // through the middle of this crosses eight or nine puffs, so whatever one of them is worth is
            // worth nine times that on screen — and the first pass, at two thirds each, came out as a
            // white hole with a lilac edge and not one filament in it.
            // <b>A third of what it was, and the first version is why.</b> Additive draws sum, a sight
            // line through the middle of this crosses nine puffs, and nine puffs at a quarter each is a
            // white hole with a coloured edge — which is exactly what it came out as, twice. A cloud that
            // is blown out is not a bright cloud, it is a cloud nobody can see the shape of, and the shape
            // is the entire reason it is here.
            var inward = 1f - r;
            _weight[i] = 0.028f + 0.100f * inward * inward;

            // <b>Colour by which side of the cloud it is on, and that is a fix rather than a refinement.</b>
            // Coloured by radius — hot in the middle, cold at the rim — every sight line crossed the whole
            // palette, and four emission lines added together are white by construction. A real one is not
            // mixed, it is <i>regionalised</i>: hydrogen on one flank, oxygen on the other, dust blue at
            // the edges and the cluster's own light gold in the core. Spatially coherent colour is the
            // only kind that survives being summed.
            var side = 0.5f + 0.5f * MathF.Cos(a);

            var tint = Vector3.Lerp(Gas[0], Gas[1], side);
            tint = Vector3.Lerp(tint, Gas[2], MathF.Pow(r, 2.2f) * 0.65f);
            tint = Vector3.Lerp(tint, Gas[3], MathF.Pow(inward, 3f) * 0.75f);

            var puff = new SpriteNode
            {
                Texture = plates,
                Size = new Vector2(1.5f + 1.9f * Grain.Pick(i, 5, 331)),
                Color = tint,
                Blend = BlendMode.Additive,
                DepthWrite = false,
                SoftDistance = 1.5f,
                RenderOrder = 3,
                Name = "gas"
            };

            puff.UseFrame(Sheets, Sheets, i % (Sheets * Sheets));

            _gas[i] = puff;
            cloud.Children.Add(puff);
        }

        // The star cluster the whole thing is lit by, which is one sprite and is the reason the gas has a
        // direction to be bright in.
        _core = new SpriteNode
        {
            Texture = soft,
            Size = new Vector2(1.0f),
            Color = new Vector3(0.98f, 0.92f, 0.86f),
            Blend = BlendMode.Additive,
            DepthWrite = false,
            SoftDistance = 1.2f,
            RenderOrder = 3,
            Name = "gas.core"
        };

        cloud.Children.Add(_core);
        root.Children.Add(cloud);

        // ---- and the dust in front of it --------------------------------------------------------------
        //
        // <b>The one thing under this dome that takes light away.</b> Every photograph anybody has of a
        // nebula is half dark: the lanes and the globules are cold dust between us and the gas, and they
        // are not a darker gas — they are opacity. Additive cannot draw that at any weight, because
        // additive only ever adds; what draws it is a near-black sprite blended the ordinary way, over
        // the top, with an alpha map. See Grain.Alpha, which exists for these twenty-two quads.
        var lanes = new Node { Name = "sky.dust" };

        for (var i = 0; i < Soots; i++)
        {
            // Flattened much harder than the gas and seated across the middle of it, because a dust lane
            // is a sheet seen edge on rather than a blob.
            var a = Grain.Pick(i, 0, 457) * MathF.Tau;
            var r = MathF.Pow(Grain.Pick(i, 1, 457), 0.5f);

            _settled[i] = new Vector3(
                MathF.Cos(a) * 2.1f * r,
                (Grain.Pick(i, 2, 457) - 0.5f) * 0.75f,
                MathF.Sin(a) * 1.9f * r);

            _shade[i] = 0.20f + 0.30f * (1f - r);

            _soot[i] = new SpriteNode
            {
                Texture = dust,
                Size = new Vector2(1.5f + 1.6f * Grain.Pick(i, 3, 457)),
                Color = new Vector3(0.030f, 0.022f, 0.026f),
                Blend = BlendMode.Alpha,
                DepthWrite = false,
                SoftDistance = 1.2f,
                RenderOrder = 4,
                Name = "dust"
            };

            lanes.Children.Add(_soot[i]);
        }

        root.Children.Add(lanes);

        // ---- the planets ----------------------------------------------------------------------------
        _planets =
        [
            Mars(), Jupiter(), Saturn()
        ];

        foreach (var planet in _planets)
            root.Children.Add(planet.Orbit);

        // The star that lights whichever one is on, and the one thing under this dome that is a light
        // rather than a picture of one. What it is for is the terminator: a fully lit ball is a disc, and
        // the shadow across it is the entire difference between a sphere and a circle. See Standoff.
        Star = new PointLight
        {
            Color = new Vector3(1f, 0.95f, 0.88f),
            Intensity = 0f,
            Range = Reach,

            // Very nearly nought, which is the only place in this building a light is told to ignore the
            // inverse square, and the difference between a planet and a spotlight on a ball. It is three
            // quarters of a metre from something half a metre across: physical falloff is four to one
            // between the near pole and the terminator, and with the range window on top of that the
            // lit hemisphere went from clipped white to invisible across forty degrees of longitude — a
            // planet that reads as half lit when five sixths of its face is in the sun. What draws a
            // terminator is the cosine at the surface. Everything else has to get out of its way.
            Decay = 0.15f
        };

        // ---- and something falling --------------------------------------------------------------------
        //
        // <b>Three tracks of twenty-two beads, and it was eight.</b> The note said it looked like several
        // points, which is exactly what eight round sprites spread over four metres are. A meteor is a
        // line: what makes it read is that the head is bright and small and the thing behind it is
        // continuous, so the beads are packed into the first quarter of the path with an exponential
        // taper and there are enough of them to overlap. A single stretched quad would be the obvious
        // answer and is not available — a sprite is a billboard and a billboard has no orientation to
        // stretch along, so a long thin one is a rectangle that turns with the camera. A trail made of
        // round things is a trail from every angle, which is what the free walk needs.
        for (var track = 0; track < Tracks; track++)
        {
            var fat = track == Tracks - 1;
            var beads = new Node { Name = "sky.meteor" };

            _trail[track] = new SpriteNode[Beads];

            for (var i = 0; i < Beads; i++)
            {
                var taper = MathF.Exp(-i * 0.135f);

                _trail[track][i] = new SpriteNode
                {
                    Texture = glow,
                    Size = new Vector2((fat ? 0.30f : 0.19f) * (0.22f + 0.78f * taper)),
                    Color = Vector3.Lerp(
                        new Vector3(1f, 0.98f, 0.94f), new Vector3(1f, 0.58f, 0.26f),
                        MathF.Min(1f, i / 9f)),
                    Blend = BlendMode.Additive,
                    DepthWrite = false,
                    Opacity = 0f,
                    RenderOrder = 5,
                    Name = "bead"
                };

                beads.Children.Add(_trail[track][i]);
            }

            // The head's own flare, which is the bloom a bright one has round it and the only part of a
            // meteor that is not on the line.
            _flare[track] = new SpriteNode
            {
                Texture = soft,
                Size = new Vector2(fat ? 1.05f : 0.52f),
                Color = new Vector3(1f, 0.93f, 0.80f),
                Blend = BlendMode.Additive,
                DepthWrite = false,
                Opacity = 0f,
                RenderOrder = 5,
                Name = "bead.flare"
            };

            beads.Children.Add(_flare[track]);
            root.Children.Add(beads);
        }
    }

    /// <summary>The light the planets are lit by, which the room has to spend a slot on. See Rounds.</summary>
    public PointLight Star { get; }

    /// <summary>
    /// The whole show, as a function of seconds since the room went dark and how far up it is.
    ///
    /// Nothing is remembered between calls and nothing integrates, so this is safe to call with the clock
    /// going backwards, jumping, or repeating — which the recorder, the contents table and every seek in
    /// the shell all do.
    /// </summary>
    public void Update(float clock, float level)
    {
        level = Math.Clamp(level, 0f, 1f);
        clock = MathF.Max(clock, 0f);

        // The galaxy, turning, and brought up as a base colour rather than as an opacity. The shell is
        // unlit and additive, so its colour is its texture times this and nothing else.
        _shell.UvOffset = new Vector2(clock / Turning, 0f);
        _shell.BaseColor = new Vector4(new Vector3(level * Ramp(clock, 0f, 3.2f)), 1f);

        // The stars, coming out from the zenith down over a second and a half. It is one term and it is
        // the difference between a sky that fades up and a sky that <i>comes out</i>, which is what
        // anybody who has watched it get dark has actually seen.
        for (var i = 0; i < Stars; i++)
        {
            // Twinkling, which is the atmosphere this room does not have and the eye expects anyway. Two
            // sines a decade apart in period so no two stars are ever in step, and never all the way out:
            // a star that blinks is a fault light.
            var flicker =
                0.74f + 0.26f * MathF.Sin(clock * 2.3f + _twinkle[i]) * MathF.Sin(clock * 0.31f + _twinkle[i] * 3f);

            _stars[i].Opacity = level * (0.45f + 0.55f * _steady[i]) * flicker
                                * Ramp(clock - _depth[i] * 1.5f, 0.1f, 1.4f);
        }

        for (var i = 0; i < Bright; i++)
            _spikes[i].Opacity = level * 0.26f * Ramp(clock, 1.4f, 2.4f)
                                 * (0.8f + 0.2f * MathF.Sin(clock * 1.7f + i));

        // The gas, turning about the sky's own axis and breathing on a second period. Turning it as a
        // rigid body is what keeps it a cloud: every puff keeps its neighbours, so the structure the nine
        // plates make between them survives the motion instead of boiling.
        var lit = level * Ramp(clock, 1.6f, 3.6f);
        var turn = Matrix4x4.CreateRotationY(clock * MathF.Tau / Drifting);

        for (var i = 0; i < Puffs; i++)
        {
            var swell = 1f + 0.06f * MathF.Sin(clock * MathF.Tau / Breathing + _phase[i]);

            _gas[i].Position = Heart + Vector3.Transform(_seated[i] * swell, turn);
            _gas[i].Opacity = lit * _weight[i] * (0.74f + 0.26f * MathF.Sin(clock * 0.37f + _phase[i]));
        }

        _core.Position = Heart + Vector3.Transform(new Vector3(0.25f, 0.10f, -0.15f), turn);
        _core.Opacity = lit * 0.16f * (0.88f + 0.12f * MathF.Sin(clock * 0.53f));

        // The dust, up a little after the gas — it has nothing to be in front of until there is gas — and
        // never to full: a lane at complete opacity is a hole cut in the sky.
        var thick = level * Ramp(clock, 2.6f, 3.8f);

        for (var i = 0; i < Soots; i++)
        {
            _soot[i].Position = Heart + Vector3.Transform(_settled[i], turn);
            _soot[i].Opacity = thick * _shade[i];
        }

        // The planets, one at a time. Each owns a stretch of the eighteen seconds and is nowhere at all
        // outside it — see Turns.
        var beat = clock % Watched;
        var sunlit = false;

        for (var i = 0; i < _planets.Length; i++)
        {
            var (from, to) = Turns[i];
            var planet = _planets[i];

            if (beat < from || beat >= to)
            {
                planet.Orbit.IsVisible = false;
                continue;
            }

            // Minus one to plus one across the slot, which is the whole of the pass. The scale holds at
            // full across the middle two thirds and closes at the ends, so a planet arrives from too far
            // to see and leaves the same way rather than being switched on.
            var u = (beat - from) / (to - from) * 2f - 1f;
            var grow = 1f - Smooth((MathF.Abs(u) - 0.62f) / 0.38f);

            planet.Orbit.IsVisible = grow > 0.006f && level > 0.004f;

            if (!planet.Orbit.IsVisible)
                continue;

            var at = Heart
                     + Sweepline * (u * Pass)
                     + Away * (u * u * Bow)
                     - Vector3.UnitY * (u * u * Dip);

            planet.Orbit.Position = at;
            planet.Orbit.Scale = new Vector3(grow);

            planet.Globe.RotationDegrees = new Vector3(0f, clock * 360f / planet.Spinning, planet.Lean);

            // The moons, on their own little circle about the planet, in the plane the rings are in — so
            // they cross in front of it and behind it rather than orbiting the screen.
            for (var m = 0; m < planet.Moons.Length; m++)
            {
                var round = clock * MathF.Tau / (11f + m * 4.7f) + m * 1.9f;

                planet.Moons[m].Position = new Vector3(
                    MathF.Cos(round) * planet.Spread[m],
                    MathF.Sin(round) * planet.Spread[m] * 0.34f,
                    MathF.Sin(round) * planet.Spread[m] * 0.22f);
            }

            // And the lamp, which follows whichever one is on and is off the rest of the time. Its
            // direction is fixed in the room rather than in the orbit, so the phase a planet is showing
            // changes as it crosses — gibbous coming in, gibbous going out, and fullest in the middle,
            // which is what a planet passing between you and a sun actually does.
            Star.Position = Deck.Planetarium + at + Lamplight * Standoff;
            // Under one, and it is measured rather than picked. The lit face of a planet three
            // quarters of a metre from its own sun is very nearly flat-lit, so whatever is written here is
            // very nearly what lands on the screen — and at two and a half the whole lit hemisphere
            // clipped to white and took the bands, the storm and the polar cap with it. What a planet
            // needs is to be <i>just under</i> full, so that the brightest part of it still has detail.
            Star.Intensity = 1.2f * level * grow * grow;
            sunlit = true;
        }

        if (!sunlit)
            Star.Intensity = 0f;

        // And the meteors. Three tracks on three periods that share no factor, so they never arrive
        // together, and the slowest of them is a fireball.
        for (var track = 0; track < Tracks; track++)
            Falling(track, clock, level);
    }

    /// <summary>Everything off, in one call, for a room that is standing but not running.</summary>
    public void Off() => Update(0f, 0f);

    // ---- the pass ------------------------------------------------------------------------------------

    /// <summary>
    /// Which way the sun stands off a planet: across the audience's view, a little towards them, and a
    /// little down.
    ///
    /// Every one of those three is a thing it cannot do. It cannot go up, because there is a metre and six
    /// tenths of air between the show and a white dome. It cannot go straight down, because the projector
    /// is under there and would be the brightest thing in a room that is supposed to be dark. And it
    /// cannot go straight out to either side, because at the ends of a pass that is where the wall is. The
    /// direction below is what is left: mostly back over the audience's shoulder, a little to one side and
    /// a little under, and with a range of a metre and four tenths it reaches the planet and provably
    /// nothing else — the dome is two metres away from it, the projector one and a half, the floor two and
    /// eight, and Range is a hard zero rather than a falloff.
    ///
    /// <b>The angle it makes with the view is thirty degrees and it was ninety.</b> A sun square to the
    /// line of sight is the most dramatic light there is and the worst possible one here: it puts the
    /// brightest point of the planet on the limb, so the cosine has fallen to a third across the middle of
    /// the disc and what reads is a half-lit ball with no features on the lit half. Thirty degrees puts
    /// the sub-solar point a third of the way in from the edge, which leaves the bands, the storm and the
    /// polar cap in the part of the cosine that still has numbers in it, and still leaves a terminator —
    /// a thin one, which is what every photograph of an outer planet has.
    /// </summary>
    private static readonly Vector3 Lamplight =
        Vector3.Normalize(Sweepline * 0.26f - Away * 0.92f - Vector3.UnitY * 0.58f);

    private sealed record Planet(
        Node Orbit, Node Globe, float Spinning, float Lean, Node[] Moons, float[] Spread);

    /// <summary>
    /// Mars: rust, dark ground, and a cap.
    ///
    /// The smallest of the three and the shortest slot, because there is the least to see — which is the
    /// honest answer and is also why it goes first. A show that opens on its best object has nowhere to
    /// go.
    /// </summary>
    private static Planet Mars()
    {
        var orbit = new Node { Name = "sky.mars" };

        var globe = new MeshNode(
            Primitives.Sphere(0.26f, 48, 32),
            new Material
            {
                BaseColor = Vector4.One,
                BaseColorTexture = Rust.Value,
                Roughness = 0.94f,
                Metallic = 0f,
                Name = "mars"
            })
        {
            Name = "mars.globe"
        };

        orbit.Children.Add(globe);

        return new Planet(orbit, globe, 9f, 25f, [], []);
    }

    /// <summary>
    /// Jupiter: bands, a storm, and four moons in a line.
    ///
    /// The largest, and the moons are most of why it is worth its six seconds. Four points strung out
    /// beside a banded ball is the single most recognisable thing in the sky that is not the moon, and it
    /// costs four spheres of thirty-five millimetres.
    /// </summary>
    private static Planet Jupiter()
    {
        var orbit = new Node { Name = "sky.jupiter" };

        var globe = new MeshNode(
            Primitives.Sphere(0.46f, 56, 36),
            new Material
            {
                BaseColor = Vector4.One,
                BaseColorTexture = Banded.Value,
                Roughness = 0.90f,
                Metallic = 0f,
                Name = "jupiter"
            })
        {
            Name = "jupiter.globe"
        };

        orbit.Children.Add(globe);

        // Unlit, and it is the same argument the rings make below: these are three centimetres across at
        // three and a half metres, which is a point, and a point with a terminator on it is a point that
        // flickers. What a moon at that size actually is on a photograph is a dot of light.
        var pale = new Material
        {
            BaseColor = new Vector4(0.86f, 0.84f, 0.78f, 1f),
            Shading = ShadingModel.Unlit,
            Name = "moon"
        };

        var moons = new Node[4];
        var spread = new[] { 0.68f, 0.86f, 1.08f, 1.34f };

        for (var i = 0; i < moons.Length; i++)
        {
            moons[i] = new MeshNode(Primitives.Sphere(0.032f + 0.006f * (i % 2), 16, 10), pale)
            {
                Name = "jupiter.moon"
            };

            orbit.Children.Add(moons[i]);
        }

        return new Planet(orbit, globe, 6f, 3f, moons, spread);
    }

    /// <summary>
    /// Saturn: the one everybody came for, and the longest slot.
    ///
    /// <b>The rings are nine hoops and unlit, and both halves of that are the decision.</b> A ring is a
    /// <i>sheet</i> — a hundred million snowballs in a plane two hundred metres thick and a hundred
    /// thousand kilometres across — and there is no annulus in Primitives and no alpha cut-out on a mesh
    /// material to make one out of a disc. A stack of thin tori at increasing radius is the same shape
    /// built out of what there is, and the gap between the seventh and the eighth is the division every
    /// photograph of one has in it.
    ///
    /// Unlit is the half that looks like a shortcut and is the opposite. A lit tube has a highlight down
    /// its length and a shadow under it, and nine of those side by side is unmistakably a coil of wire —
    /// which is exactly what the lit version looked like from a chair two metres away. Unlit, every hoop
    /// is the same flat value edge to edge, so overlapping hoops are one band with no seam anywhere in it.
    /// It is also the truth about the subject: a ring at this distance is its own brightness, not a
    /// surface catching a lamp.
    /// </summary>
    private static Planet Saturn()
    {
        var orbit = new Node { Name = "sky.saturn" };

        var globe = new MeshNode(
            Primitives.Sphere(0.34f, 56, 36),
            new Material
            {
                BaseColor = Vector4.One,
                BaseColorTexture = Butter.Value,
                Roughness = 0.92f,
                Metallic = 0f,
                Name = "saturn"
            })
        {
            Name = "saturn.globe"
        };

        orbit.Children.Add(globe);

        var ice = Hoop(0.46f);
        var dust = Hoop(0.30f);

        foreach (var (radius, tube, outer) in
                 new[]
                 {
                     (0.50f, 0.020f, false), (0.53f, 0.020f, false), (0.56f, 0.020f, false),
                     (0.59f, 0.020f, false), (0.62f, 0.020f, false), (0.65f, 0.018f, false),
                     (0.68f, 0.016f, false), (0.755f, 0.015f, true), (0.785f, 0.013f, true)
                 })
            orbit.Children.Add(new MeshNode(Primitives.Torus(radius, tube, 80, 5), outer ? dust : ice)
            {
                // <b>Tilted away from the audience and not towards them.</b> The seats look up at this at
                // thirty degrees, so a ring plane tipped thirty degrees the other way is very nearly
                // square on to them — which is a white disc with a slot in it, and is the one view of
                // Saturn nobody has ever seen a photograph of. Tipped away, the opening drops to about
                // fifteen degrees, which is the ellipse everybody recognises.
                RotationDegrees = new Vector3(-22f, 0f, 11f),
                RenderOrder = 6,
                Name = "saturn.ring"
            });

        var titan = new MeshNode(
            Primitives.Sphere(0.038f, 16, 10),
            new Material
            {
                BaseColor = new Vector4(0.88f, 0.76f, 0.52f, 1f),
                Shading = ShadingModel.Unlit,
                Name = "titan"
            })
        {
            Name = "saturn.moon"
        };

        orbit.Children.Add(titan);

        return new Planet(orbit, globe, 7f, 27f, [titan], [1.22f]);
    }

    /// <summary>
    /// One value, flat, for a ring. Well under one, because unlit means what is written here is what lands
    /// on the screen — and a band that clips to white loses the division in it, which is what 0.62 did.
    ///
    /// <b>And partly transparent</b>, which is the last thing that stops a stack of hoops reading as a
    /// plate. A ring is a hundred million snowballs with gaps between them: stars go through it, and the
    /// planet's own limb shows faintly behind the near side of it. Three quarters of an alpha is enough to
    /// say so and not enough to make it look like glass.
    /// </summary>
    private static Material Hoop(float value) => new()
    {
        BaseColor = new Vector4(value, value * 0.97f, value * 0.90f, 0.78f),
        Shading = ShadingModel.Unlit,
        Blend = BlendMode.Alpha,
        DepthWrite = false,
        DoubleSided = true
    };

    /// <summary>
    /// One meteor track, for one moment.
    ///
    /// The cycle is the track's own period and the flight is a fraction of a second inside it, so most of
    /// the time there is no meteor at all — which is what makes the one that arrives an event rather than
    /// a feature of the room. The head runs the path in <c>flight</c> seconds and the tail keeps going for
    /// as long as it takes the last bead to reach the end, which is what stops a streak vanishing all at
    /// once like a switch.
    /// </summary>
    private void Falling(int track, float clock, float level)
    {
        var fireball = track == Tracks - 1;

        var period = track switch { 0 => 5.3f, 1 => 8.1f, _ => 12.7f };
        var flight = fireball ? 0.95f : 0.52f;
        var gap = fireball ? 0.016f : 0.012f;

        var cycle = clock / period;
        var which = (int)MathF.Floor(cycle);
        var into = (cycle - which) * period;

        var travel = into / flight;
        var live = level * (travel < 1f + Beads * gap ? 1f : 0f);

        // Where it goes: a chord across the upper dome, biased to the quarter the chairs are facing,
        // because a shooting star behind the audience is a shooting star nobody saw.
        var seed = 613 + track * 41;

        var azimuth = float.DegreesToRadians(Planetarium.Facing - 90f + 180f * Grain.Pick(which, 0, seed));
        var high = 0.16f + 0.34f * Grain.Pick(which, 1, seed);
        var swing = (0.55f + 0.85f * Grain.Pick(which, 2, seed)) * (Grain.Pick(which, 3, seed) < 0.5f ? -1f : 1f);
        var drop = 0.34f + 0.40f * Grain.Pick(which, 4, seed);

        var head = Shell(azimuth, high);
        var tail = Shell(azimuth + swing, MathF.Min(high + drop, 0.97f));

        for (var i = 0; i < Beads; i++)
        {
            var lag = travel - i * gap;
            var bead = _trail[track][i];

            if (live <= 0f || lag <= 0f || lag >= 1f)
            {
                bead.Opacity = 0f;
                continue;
            }

            bead.Position = Vector3.Lerp(head, tail, lag);

            // In fast and out slow: a meteor brightens over the first tenth of its path and fades over
            // the last third, which is what ablation looks like from underneath.
            bead.Opacity = live * MathF.Min(1f, lag * 12f) * MathF.Min(1f, (1f - lag) * 2.6f)
                           * (fireball ? 1f : 0.82f);
        }

        var lead = travel;

        if (live <= 0f || lead <= 0f || lead >= 1f)
        {
            _flare[track].Opacity = 0f;
            return;
        }

        _flare[track].Position = Vector3.Lerp(head, tail, lead);

        // The fireball flickers and the small ones do not, which is the one thing everybody who has seen a
        // bright one remembers about it.
        _flare[track].Opacity = live * (fireball ? 0.62f : 0.30f)
                                * MathF.Min(1f, lead * 10f) * MathF.Min(1f, (1f - lead) * 3f)
                                * (fireball ? 0.72f + 0.28f * MathF.Sin(clock * 47f) : 1f);
    }

    /// <summary>A point on the meteors' shell: how far round, and how far down from the crown as a
    /// fraction of what the dome allows.</summary>
    private static Vector3 Shell(float azimuth, float down)
    {
        var zenith = down * Planetarium.Starry;

        var ray = new Vector3(
            MathF.Sin(zenith) * MathF.Cos(azimuth),
            MathF.Cos(zenith),
            MathF.Sin(zenith) * MathF.Sin(azimuth));

        return Planetarium.Vault + ray * (Planetarium.Bowl - 0.35f);
    }

    /// <summary>A star's colour from its spectral class, which is one number and five stops.</summary>
    private static Vector3 Spectrum(float pick) => pick switch
    {
        < 0.07f => new Vector3(0.70f, 0.79f, 1f),
        < 0.22f => new Vector3(0.92f, 0.95f, 1f),
        < 0.55f => new Vector3(1f, 0.97f, 0.90f),
        < 0.83f => new Vector3(1f, 0.87f, 0.68f),
        _ => new Vector3(1f, 0.74f, 0.56f)
    };

    private static Vector3 Flatten(Vector3 v)
    {
        var level = new Vector3(v.X, 0f, v.Z);

        return level.LengthSquared() > 1e-6f ? Vector3.Normalize(level) : Vector3.UnitZ;
    }

    private static float Ramp(float t, float from, float over) => Smooth((t - from) / over);

    private static float Smooth(float t)
    {
        t = Math.Clamp(t, 0f, 1f);

        return t * t * (3f - 2f * t);
    }

    // ---- the images ---------------------------------------------------------------------------------
    //
    // All of them arithmetic, like everything else on a surface in this building — see PatternShop, which
    // is the room that says so out loud one doorway back. It matters more here than anywhere: a room whose
    // subject is astronomy is a room somebody will assume shipped a photograph, and the whole of what is on
    // these walls and under this dome is a few hundred lines of noise, a falloff and a palette.

    /// <summary>
    /// The galaxy across the dome: a band with a bulge in it, star clouds along it, and two rifts down
    /// the middle.
    ///
    /// Everything in it is a function of latitude with noise on top, which is what keeps it survivable on
    /// a polar cap: the band is nowhere near the crown, and what is at the crown is one flat number.
    /// </summary>
    private static readonly Lazy<Texture> Vaulting = new(() =>
        Grain.Colour(1280, "sky.vault", (u, v) =>
        {
            // <b>The band is a function of direction and not of v, and that is the whole of this.</b>
            // Written against v it is a stripe in the image — and a stripe in the image of a polar cap is
            // a <i>ring round the dome</i>, because v is distance from the crown. What that drew was a
            // grey halo filling the whole ceiling with the crown as its hole, which is not a galaxy in any
            // sky anybody has stood under. Turning the texture coordinates back into the direction they
            // came from costs three trigonometric calls per texel at build time and buys a great circle,
            // which is what a galaxy is.
            var zenith = v * Planetarium.Sweep;
            var azimuth = u * MathF.Tau;

            var ray = new Vector3(
                MathF.Sin(zenith) * MathF.Cos(azimuth),
                MathF.Cos(zenith),
                MathF.Sin(zenith) * MathF.Sin(azimuth));

            // Darker overhead than at the rim, which is backwards for a real sky and right for this one:
            // the rim is where the room's own lamps would reach if they were on, and a dome that is
            // uniformly black has no shape at all until something is drawn on it.
            var ground = new Vector3(0.005f, 0.007f, 0.017f) * (0.30f + 0.70f * v);

            // How far off the galactic plane, in the only units that mean anything: the sine of the
            // latitude. The pole is tilted so the band runs across the dome at an angle rather than
            // through the crown, which is where the audience is looking.
            var across = Vector3.Dot(ray, Pole) / 0.16f;

            // The band, and the star clouds in it: a falloff times a coarse field times a fine one, which
            // is three lines and is the difference between a stripe and a galaxy.
            var band = MathF.Exp(-across * across)
                       * (0.22f + 0.78f * Grain.Fbm(u, v, 9, 41, 4))
                       * (0.55f + 0.45f * Grain.Fbm(u, v, 26, 47, 3));

            // The bulge, which is the middle of it and the one place the band is bright enough to have a
            // colour of its own rather than the mean of a hundred billion stars.
            var bulge = MathF.Exp(-(1f - Vector3.Dot(ray, Centre)) * 7f - across * across * 0.5f);

            // Dust lanes down the middle of it, which is what a galaxy looks like and is one line: the
            // band times a second, narrower field subtracted from one.
            var lane = 1f - 0.62f * MathF.Exp(-across * across * 5.5f) * Grain.Fbm(u, v, 17, 53, 3);

            return ground
                   + new Vector3(0.072f, 0.078f, 0.104f) * band * lane
                   + new Vector3(0.068f, 0.058f, 0.044f) * bulge * lane;
        }));

    /// <summary>Which way the galactic pole points, and where the middle of the galaxy is. Both are
    /// directions in the room, because that is the only frame in which a great circle is one.</summary>
    private static readonly Vector3 Pole = Vector3.Normalize(new Vector3(0.60f, 0.58f, -0.55f));

    private static readonly Vector3 Centre = Vector3.Normalize(new Vector3(-0.62f, 0.52f, 0.59f));

    /// <summary>
    /// Nine clouds on one sheet, and every one of them a different cloud.
    ///
    /// Three by three rather than two by two, because seventy-eight puffs off four plates repeats often
    /// enough to see — and what you see is not a texture, it is the same comma appearing four times in a
    /// row along one filament.
    /// </summary>
    private static readonly Lazy<Texture> Sheet = new(() =>
        Grain.Colour(Sheets * 432, "sky.gas", (u, v) =>
        {
            var column = Math.Min((int)(u * Sheets), Sheets - 1);
            var row = Math.Min((int)(v * Sheets), Sheets - 1);
            var cell = row * Sheets + column;

            var su = (u - column / (float)Sheets) * Sheets;
            var sv = (v - row / (float)Sheets) * Sheets;

            var dx = (su - 0.5f) * 2f;
            var dy = (sv - 0.5f) * 2f;
            var r = MathF.Min(MathF.Sqrt(dx * dx + dy * dy), 1f);

            var edge = 1f - r;
            edge = edge * edge * (3f - 2f * edge);
            edge *= edge;

            // Domain warped twice: the fbm is sampled at coordinates that are themselves fbm, and those
            // are sampled at coordinates that are fbm as well. One warp gives filaments; two give
            // filaments that curl, which is the thing every photograph of a star-forming region is full of
            // and the thing an unwarped field can never produce at any octave count.
            var warp = Grain.Fbm(su, sv, 3, 71 + cell * 13, 3);
            var curl = Grain.Fbm(sv + warp * 0.4f, su, 5, 97 + cell * 13, 3);

            // Six octaves and not five, and the plates are half again as big to carry them. A puff is
            // three metres across at four metres from the eye, so one of these cells is magnified to a
            // quarter of the screen — and a field whose finest octave is one part in ninety-six is a
            // cloud with nothing in it smaller than a fist. What reads as gas is the smallest structure,
            // not the largest.
            var f = Grain.Fbm(
                su + 0.32f * warp + 0.16f * curl,
                sv + 0.32f * curl,
                3 + cell % 4,
                11 + cell * 7,
                6);

            // Ridged as well as thresholded, and the ridge is the new half. Thresholding a field keeps its
            // peaks; folding it about its own middle first turns every crossing into a crease, and a
            // nebula is made of creases. The two together give a cloud with a skeleton in it.
            var ridged = 1f - MathF.Abs(f * 2f - 1f);
            var wisp = MathF.Pow(Math.Clamp((f - 0.40f) / 0.40f, 0f, 1f), 1.6f) * 0.65f
                       + MathF.Pow(ridged, 3.4f) * 0.5f;

            // A last octave of speckle over the top, which is what stops a magnified plate reading as a
            // watercolour: real gas is grainy at every scale a photograph can resolve, and this one is
            // being looked at fifteen times bigger than it was drawn.
            wisp *= 0.80f + 0.34f * Grain.Fbm(su, sv, 48, 313 + cell, 3);

            return new Vector3(MathF.Min(wisp, 1.2f) * edge);
        }));

    /// <summary>
    /// One globule of dust: opaque in the middle, ragged at the edge, and nothing outside it.
    ///
    /// The alpha is the whole texture — the colour is very nearly black everywhere and does not matter.
    /// It is the only map in the building drawn by <see cref="Grain.Alpha"/>, and see there for why the
    /// fourth channel is not gamma encoded when the other three are.
    /// </summary>
    private static readonly Lazy<Texture> Soot = new(() =>
        Grain.Alpha(256, "sky.dust", (u, v) =>
        {
            var dx = (u - 0.5f) * 2f;
            var dy = (v - 0.5f) * 2f;
            var r = MathF.Min(MathF.Sqrt(dx * dx + dy * dy), 1f);

            var edge = 1f - r;
            edge = edge * edge * (3f - 2f * edge);

            var warp = Grain.Fbm(u, v, 4, 191, 3);
            var f = Grain.Fbm(u + 0.35f * warp, v + 0.35f * Grain.Fbm(v, u, 4, 199, 3), 5, 211, 4);

            return new Vector4(0.02f, 0.015f, 0.018f, edge * edge * Math.Clamp(f * 1.6f - 0.18f, 0f, 1f));
        }));

    /// <summary>A four-armed diffraction cross with a core, for the fourteen brightest stars.</summary>
    private static readonly Lazy<Texture> Spiked = new(() =>
        Grain.Colour(128, "sky.spike", (u, v) =>
        {
            var dx = (u - 0.5f) * 2f;
            var dy = (v - 0.5f) * 2f;

            var arms = MathF.Exp(-dx * dx * 320f) * MathF.Exp(-dy * dy * 2.4f)
                       + MathF.Exp(-dy * dy * 320f) * MathF.Exp(-dx * dx * 2.4f);

            var core = MathF.Exp(-(dx * dx + dy * dy) * 90f);

            return new Vector3(MathF.Min(arms * 0.55f + core, 1f));
        }));

    /// <summary>
    /// Mars: iron oxide, dark ground, a cap at one pole and a dust storm across a third of it.
    ///
    /// The albedo features are the thing. A rust-coloured ball is a marble; a rust-coloured ball with a
    /// dark wedge across the middle of it and a white cap is recognisable from across a room, and both of
    /// those are one threshold each on a field that is already there.
    /// </summary>
    private static readonly Lazy<Texture> Rust = new(() =>
        Grain.Colour(512, "sky.mars", (u, v) =>
        {
            var f = Grain.Fbm(u, v, 6, 331, 5);
            var fine = Grain.Fbm(u, v, 23, 337, 3);

            var ochre = new Vector3(0.74f, 0.42f, 0.24f);
            var dark = new Vector3(0.33f, 0.21f, 0.16f);

            // The dark ground: the low half of the field, curved so its edges are soft in some places and
            // sharp in others, which is what an albedo boundary is.
            var ground = Math.Clamp((0.47f - f) * 4.2f, 0f, 1f);
            var face = Vector3.Lerp(ochre, dark, ground * 0.85f) * (0.86f + 0.28f * fine);

            // A cap at each pole, and the south one bigger — Mars has an eccentric orbit and its southern
            // summer is short and violent, which is exactly the sort of fact a generated planet gets to
            // have for the price of one asymmetric constant.
            var cap = (1f - Grain.Step(0.035f, 0.080f, v)) * 0.75f
                      + Grain.Step(0.880f, 0.935f, v);

            cap *= 0.75f + 0.25f * fine;

            return Vector3.Lerp(face, new Vector3(0.93f, 0.94f, 0.96f), Math.Clamp(cap, 0f, 1f));
        }));

    /// <summary>
    /// Jupiter: zones, belts, festoons and the storm.
    ///
    /// Bands are a function of latitude alone, which is what makes them bands, and the warp is what makes
    /// it a gas giant rather than a beach ball: a little of the flow across, a lot along. The storm is an
    /// oval two thirds of the way down, turned into the flow, and a shade lighter than the belt it sits in
    /// — because it is the same gas going round faster and not a paint mark.
    /// </summary>
    private static readonly Lazy<Texture> Banded = new(() =>
        Grain.Colour(512, "sky.jupiter", (u, v) =>
        {
            // Stretched twenty to one, which is what shear does to turbulence and is the only reason these
            // read as bands with weather in them rather than as stripes with noise on them.
            var flow = Grain.Fbm(u, v * 14f, 5, 23, 4);
            var band = 0.5f + 0.5f * MathF.Sin((v * 13f + flow * 1.6f) * MathF.PI);

            var zone = new Vector3(0.92f, 0.86f, 0.72f);
            var belt = new Vector3(0.56f, 0.40f, 0.30f);

            var face = Vector3.Lerp(belt, zone, band * band);

            // Poles a shade cooler and flatter, because a banded planet seen whole is darker at the top
            // and bottom and a texture that is not is a texture that reads as a cylinder.
            var pole = MathF.Abs(v * 2f - 1f);
            face = Vector3.Lerp(face, new Vector3(0.44f, 0.40f, 0.42f), MathF.Pow(pole, 3.2f));

            var sx = MathF.Min(MathF.Abs(u - 0.62f), 1f - MathF.Abs(u - 0.62f)) / 0.075f;
            var sy = (v - 0.635f) / 0.040f;
            var storm = MathF.Exp(-(sx * sx + sy * sy)) * (0.6f + 0.4f * flow);

            return face * (0.80f + 0.20f * flow) + new Vector3(0.42f, 0.16f, 0.10f) * storm;
        }));

    /// <summary>
    /// Saturn: the same arithmetic as Jupiter with the contrast taken out of it.
    ///
    /// It is a pale planet and the temptation is to make it interesting. What makes Saturn recognisable is
    /// not its face, it is that its face is nearly blank and it has a ring round it — so the bands here
    /// are a tenth of Jupiter's depth and the hexagon at the north pole is the only feature on it.
    /// </summary>
    private static readonly Lazy<Texture> Butter = new(() =>
        Grain.Colour(512, "sky.saturn", (u, v) =>
        {
            var flow = Grain.Fbm(u, v * 12f, 4, 59, 4);
            var band = 0.5f + 0.5f * MathF.Sin((v * 9f + flow * 1.1f) * MathF.PI);

            var pale = new Vector3(0.90f, 0.82f, 0.62f);
            var deep = new Vector3(0.74f, 0.64f, 0.45f);

            var face = Vector3.Lerp(deep, pale, band);

            // The hexagon: a six-lobed radius about the north pole, which is a real feature of the real
            // planet and is two lines because a polar map is already in polar coordinates.
            var pole = v * 2f;
            var hex = MathF.Exp(-MathF.Pow((pole - 0.16f) / 0.05f, 2f))
                      * (0.5f + 0.5f * MathF.Cos(u * MathF.Tau * 6f));

            return face * (0.90f + 0.14f * flow) + new Vector3(0.10f, 0.11f, 0.14f) * hex;
        }));

    /// <summary>The four photographs on the wall, which are the other half of this room. See
    /// <c>Planetarium.Hang</c>.</summary>
    public static Texture Print(int index) => Prints[Math.Clamp(index, 0, Framed - 1)].Value;

    private static readonly Lazy<Texture>[] Prints =
    [
        .. Enumerable.Range(0, Framed).Select(i => new Lazy<Texture>(() => Plate(i)))
    ];

    /// <summary>
    /// One of the four photographs, and it is drawn with the same arithmetic as the thing it is a
    /// photograph of.
    ///
    /// <b>That is the room's whole argument and for two drafts it was not true.</b> The prints were an
    /// older, simpler field — one warp, one threshold, a core and a speckle — while the gas over the seats
    /// grew a second warp, a ridge term and a fine octave. What that produced was a wall of soft pastel
    /// blobs under a ceiling full of filaments, and a visitor who walks from one to the other reads the
    /// difference immediately even if they never work out what it is. A photograph of a nebula and a
    /// nebula have to be made of the same thing here, because they <i>are</i> the same thing: the claim
    /// this room makes is that the picture and the subject come out of one page of functions.
    ///
    /// So this is <see cref="Sheet"/>'s field, on a rectangle, with the four things a photograph has that
    /// a volume does not — a vignette, a star field behind it, a horizon of sky glow, and a composition
    /// that is not centred.
    /// </summary>
    private static Texture Plate(int index) =>
        Grain.Colour(704, $"sky.print.{index}", (u, v) =>
        {
            var seed = 211 + index * 37;

            // Off centre, and a different corner for each of the four. A row of four photographs each with
            // its subject dead in the middle is a row of four stamps.
            var cx = 0.5f + 0.17f * MathF.Cos(index * 2.1f);
            var cy = 0.5f + 0.17f * MathF.Sin(index * 2.1f);

            var dx = (u - cx) * 2.0f;
            var dy = (v - cy) * 2.0f;
            var r = MathF.Min(MathF.Sqrt(dx * dx + dy * dy), 1f);

            var edge = 1f - r;
            edge = edge * edge * (3f - 2f * edge);

            // Two warps, as the sheet has. One gives filaments; two give filaments that curl, which is the
            // thing every photograph of a star-forming region is full of and the thing an unwarped field
            // cannot produce at any octave count.
            var warp = Grain.Fbm(u, v, 3, seed, 3);
            var curl = Grain.Fbm(v + warp * 0.4f, u, 5, seed + 11, 3);

            var f = Grain.Fbm(
                u + 0.30f * warp + 0.15f * curl,
                v + 0.30f * curl,
                4,
                seed + 23,
                6);

            // Thresholded and ridged together: the threshold keeps the peaks, the ridge turns every
            // crossing into a crease, and a nebula is made of creases.
            var ridged = 1f - MathF.Abs(f * 2f - 1f);
            var wisp = MathF.Pow(Math.Clamp((f - 0.38f) / 0.42f, 0f, 1f), 1.5f) * 0.66f
                       + MathF.Pow(ridged, 3.6f) * 0.44f;

            wisp *= (0.78f + 0.36f * Grain.Fbm(u, v, 52, seed + 31, 3)) * edge;

            // Regionalised rather than mixed, which is the fix the gas needed and needs saying twice:
            // four emission lines added together are white, and a real one is hydrogen on one flank,
            // oxygen on the other, dust blue at the rim and the cluster's own light gold in the core.
            var side = 0.5f + 0.5f * MathF.Cos(MathF.Atan2(dy, dx) + index * 1.3f);

            var tint = Vector3.Lerp(Gas[index % Gas.Length], Gas[(index + 1) % Gas.Length], side);
            tint = Vector3.Lerp(tint, Gas[2], MathF.Pow(r, 2.0f) * 0.55f);
            tint = Vector3.Lerp(tint, Gas[3], MathF.Pow(1f - r, 3f) * 0.7f);

            // The dust, which is the half of every one of these pictures that is dark. It is a lane across
            // the field rather than a ring, because that is what a foreground cloud looks like.
            var across = (dy - dx * (0.35f + 0.5f * (index % 2))) / 0.30f;
            var lane = 1f - 0.62f * MathF.Exp(-across * across) * Grain.Fbm(u, v, 9, seed + 41, 4);

            var cloud = tint * wisp * lane * 0.95f;

            // The cluster: a hard little core with a halo, which is what is lighting all of the above.
            var core = MathF.Exp(-r * r * 13f);

            return new Vector3(0.010f, 0.012f, 0.022f)
                   + cloud
                   + new Vector3(0.95f, 0.90f, 0.84f) * core * core * 0.42f
                   + Speck(u, v, 190, seed, 0.9962f)
                   + Speck(u, v, 64, seed + 7, 0.986f) * 0.55f;
        });

    /// <summary>
    /// Stars, for a flat image: a jittered grid where nearly every cell is empty.
    ///
    /// A hash per cell and a threshold near one is the cheapest star field there is, and the jitter is
    /// what stops it being a grid. Three by three cells are checked rather than one, because a star near
    /// the edge of its own cell has to be drawn by its neighbours as well or the field has seams in it.
    /// </summary>
    private static Vector3 Speck(float u, float v, int cells, int seed, float rarity)
    {
        var x = u * cells;
        var y = v * cells;

        var cx = (int)MathF.Floor(x);
        var cy = (int)MathF.Floor(y);

        var lit = Vector3.Zero;

        for (var j = -1; j <= 1; j++)
        for (var i = -1; i <= 1; i++)
        {
            var ax = cx + i;
            var ay = cy + j;

            var pick = Grain.Pick(ax, ay, seed);

            if (pick < rarity)
                continue;

            var px = ax + 0.2f + 0.6f * Grain.Pick(ax, ay, seed + 1);
            var py = ay + 0.2f + 0.6f * Grain.Pick(ax, ay, seed + 2);

            var dx = x - px;
            var dy = y - py;

            var bright = (pick - rarity) / (1f - rarity);

            lit += MathF.Exp(-(dx * dx + dy * dy) * 34f) * bright * Spectrum(Grain.Pick(ax, ay, seed + 3));
        }

        return lit;
    }
}
