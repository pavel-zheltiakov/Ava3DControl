using System.Numerics;

namespace Ava3D.Demo.Story;

/// <summary>
/// The planetarium: a round room nine and a half metres across with a perforated white dome over it, a
/// star projector in the middle of the floor, five chairs under it, and four photographs on the wall you
/// pass on the way to them.
///
/// <b>It is the only room in the building you are meant to stop moving in.</b> Every other room is walked:
/// the walk is the argument, the exhibits arrive one at a time as he reaches them, and standing still is
/// something the film does for four seconds at a stand before moving on. Here he sits down, and for
/// eighteen seconds nothing in the frame is a room at all. That is not a change of pace for its own sake —
/// it is the only staging under which the thing being exhibited can be seen, because what is being
/// exhibited is <i>depth in something that has no surface</i>, and a camera walking past a cloud of gas
/// reads the parallax and stops believing in it. A camera sitting under it does not.
///
/// The room is two halves of one idea and they are the same three functions composed twice:
///
/// <list type="number">
/// <item><b>The photographs</b>, which are what a nebula looks like when somebody has already taken the
/// picture — four prints in frames, lit by the cove, flat on a wall.</item>
/// <item><b>The dome</b>, which is what one looks like when nobody has. See <see cref="Sky"/>.</item>
/// </list>
///
/// Walking from one to the other is the whole chapter. The prints are on the east arc because that is the
/// wall he turns towards coming through the south door, and the chairs are in the middle because the
/// middle of a dome is the only seat in the house.
///
/// <b>The dome is a lit surface and not a picture, and that is the single decision this room turns on.</b>
/// It was an unlit sphere with a sky painted on it and a brightness the chapter faded up and down, which
/// works and is wrong in a way that shows the moment anybody walks in: what you meet coming through the
/// door is supposed to be a <i>screen</i> — white, blank, waiting — and a screen with a tenth of a nebula
/// already on it is a room that has started without you. So the dome is now what a real one is: perforated
/// white sheet, lit by the coves in the cornice, PBR like every other surface in the building. It goes
/// dark because the room goes dark, with nothing driving it at all, and the sky arrives on a shell of its
/// own inside it. See <see cref="Finish.Screen"/>, and see <see cref="Sky"/>, which no longer owns this
/// material and used to.
///
/// <b>Four metres nine to the crown, and the engine room is still the reveal.</b> Nothing else on this
/// deck is over three metres six, so a dome that clears four is the tallest thing in the exhibition half —
/// and it is a curve, which reads taller again. The engine room is six and a half over seventeen metres by
/// thirteen and it survives this easily, for the reason it always did: it is not the ceiling that makes
/// that room, it is the twenty-one metres of two-metre corridor in front of it.
/// </summary>
internal sealed class Planetarium
{
    /// <summary>The inner face of the wall ring, at the middle of a sector. Nine and a half metres across,
    /// which is the rotunda's number to within a hand's breadth and is not a coincidence — both are what
    /// twelve sectors of a walkable room comes to.</summary>
    public const float Radius = 4.8f;

    /// <summary>
    /// The wall head, and where the cornice is.
    ///
    /// Two seventy-five and not two-four, and the reason is the doorways: <see cref="Deck.DoorHeight"/> is
    /// two-four, a pierced wall builds its lintel out of whatever is left above the opening, and a wall the
    /// height of its own door has no lintel at all — which renders not as a tall doorway but as a slot with
    /// the sky showing through the top of it.
    /// </summary>
    public const float Height = 2.75f;

    /// <summary>
    /// Where the dome's rim sits and how far out it is — both of them behind the wall.
    ///
    /// This pair is the whole of how a curved ceiling meets a straight one without a seam, and it took
    /// getting wrong twice to write down. A cap that springs from the wall's inner face leaves a gap at
    /// every sector joint, because a twelve-sided room's corners are a hundred and seventy millimetres
    /// further out than its flats and a circle cannot be both. A cap that springs from the joints leaves a
    /// ledge at every flat. So it springs from neither: the rim is at 5.15, which is outside the wall
    /// everywhere, and it is a hundred and fifty millimetres <i>below</i> the wall head, so the last of the
    /// dome before it turns over is inside the masonry and cannot be seen from the floor.
    /// </summary>
    private const float Springing = 2.6f;

    private const float Rim = 5.15f;

    /// <summary>The top of the dome, off the floor.</summary>
    public const float Crown = 4.9f;

    /// <summary>
    /// The sphere the dome is a cap of, and where its centre is.
    ///
    /// Both are derived and neither is chosen, which is the only way a cap stays a cap: a rim radius and a
    /// rise fix the sphere completely — R = (a² + h²) / 2h is the sagitta, and it is the same arithmetic a
    /// stonemason does — and typing a radius instead would be typing a number that has to agree with two
    /// others every time one of them moves.
    /// </summary>
    public const float Bowl = (Rim * Rim + (Crown - Springing) * (Crown - Springing)) / (2f * (Crown - Springing));

    public static readonly Vector3 Vault = new(0f, Crown - Bowl, 0f);

    /// <summary>How far down from the crown the cap reaches, in radians. <see cref="Sky"/> hangs its stars
    /// inside this and short of the rim — see there.</summary>
    public static float Sweep => MathF.Asin(Math.Clamp(Rim / Bowl, 0f, 1f));

    /// <summary>
    /// And how far down the stars are allowed, which is not the same number.
    ///
    /// The last tenth of the dome is behind the wall head from anywhere on the floor — the rim is at 5.15
    /// and the wall's inner face is at 4.8 — so a star hung on the rim is a star inside the plaster. Six
    /// sevenths of the sweep puts the lowest of them at three metres up and four and a half out, which is
    /// clear of the cornice from every seat in the room.
    /// </summary>
    public static float Starry => Sweep * 0.86f;

    /// <summary>
    /// Where the gores stop and the crown ring begins, in degrees off the top of the dome.
    ///
    /// <b>It is here because of a moiré and it is the fix every real dome already has.</b> A cap's texture
    /// coordinates are polar, so any pattern tiled across one is sampled at a rate that goes to infinity
    /// at the pole — and a perforation grid sampled that way is not a fine texture at the crown, it is a
    /// ring of crawling interference in the one part of the ceiling everybody in the room is looking at.
    /// Twelve degrees off the top is a disc two and nine tenths across, and it is plain sheet: no tiling,
    /// no pinch, nothing to alias. A thin ring covers the joint, which is exactly what the piece is called
    /// on a dome that was actually built — the gores are trapezoids and they have to stop somewhere.
    /// </summary>
    private const float CrownAngle = 12f;

    /// <summary>How much wider than its chord each wall slab is built, so adjacent ones overlap and the
    /// twelve joints are not twelve black seams. Same fix, same number, as <see cref="Rotunda"/>.</summary>
    private const float Overlap = 0.3f;

    /// <summary>
    /// The two openings, in degrees round the room from due east counting towards the south — the same
    /// convention <see cref="Rotunda"/> uses, and the same quarter turn between them.
    ///
    /// Neither is free. The south one has to be due south because the pattern shop's north wall is a
    /// straight wall and the only point on this ring that touches it is the one nearest its centre; the
    /// west one has to be due west because a round room's openings sit on radials thirty degrees apart,
    /// because a corridor that met the wall on any other one would meet it obliquely — and an oblique
    /// junction puts one of the corridor's own walls a quarter of a metre inside this room. See
    /// <see cref="Link"/>, which is as short as the plan lets it be and no shorter.
    /// </summary>
    public const float EntranceAngle = 270f;

    public const float ExitAngle = 180f;

    /// <summary>
    /// Where the four photographs hang, in walk order.
    ///
    /// The east arc, running anticlockwise from the south-east, because that is the wall he turns towards
    /// when he comes through the south door — a person entering a round room turns the way the room opens,
    /// and this room opens east because the other way is the door he has to leave by.
    /// </summary>
    private static readonly float[] Pictures = [300f, 330f, 0f, 30f];

    private static readonly float[] Blanks = [60f, 90f, 120f, 150f, 210f, 240f];

    /// <summary>Where the five cove fittings are, and how far out. They are not lamps on a ceiling: the
    /// ceiling is a dome and a pendant under a dome is a pendant hanging in mid air.</summary>
    private static readonly float[] Coves = [210f, 285f, 345f, 45f, 120f];

    private const float CoveRadius = 4.55f;
    private const float CoveHeight = 2.18f;

    /// <summary>
    /// The chair the film sits in, and the row is built out from it rather than it being found in the row.
    ///
    /// <b>It is the end of the row and not the middle of it, and the walk is why.</b> The middle chair is
    /// the better seat and it is unreachable: a camera that walks into the third of five has to pass
    /// through two chair backs to get there, and there is no staging of that which does not look like a
    /// man wading. Sitting in the end one means he steps in from outside the row, which is what anybody
    /// arriving alone at an empty show does anyway.
    ///
    /// A metre and a half south-west of the room's axis, so the seated eye is very nearly under the crown
    /// — which is the whole reason a dome has a good seat at all.
    /// </summary>
    public static readonly Vector3 Rake = new(-0.6f, 0f, -1.5f);

    /// <summary>
    /// Which way the chairs look, in the ring's degrees: just north of north-west, at
    /// <see cref="Sky.Heart"/>, thirty degrees up.
    ///
    /// It is measured rather than picked. From <see cref="Rake"/> the bearing to the middle of the show is
    /// ninety-five degrees, and a chair five degrees off what its occupant is looking at is a chair; one
    /// forty degrees off is a chair somebody has turned round.
    /// </summary>
    public const float Facing = 100f;

    /// <summary>How far apart the five are, and how high the dais under them is.</summary>
    private const float Apart = 0.82f;

    private const float Dais = 0.12f;

    /// <summary>How high the projector's fork sits, off the dais it stands on.</summary>
    private const float Pillar = 1.02f;

    private readonly Node _ball;
    private readonly Material _pinholes;

    public Planetarium(Hall hall)
    {
        var root = hall.Add(Deck.PlanetariumRoom, Deck.Planetarium);

        // <b>Three surfaces nothing else in the building has, and they are the room.</b> A planetarium is a
        // dark box with one bright thing in it. Plaster at a third albedo and tile at a quarter — which is
        // what every other room here is lined with — fill a dome with the room's own bounce, and a dome
        // full of bounce is a grey ceiling whatever is projected on it. So the walls are acoustic lining at
        // a tenth, the floor is contract carpet, and the ceiling is the only thing in here that is light
        // coloured. See Finish.Acoustic, Finish.Carpet and Finish.Screen, which are written for this room
        // and used nowhere else.
        var lining = Finish.Acoustic();
        var carpet = Finish.Carpet();
        var brushed = Finish.Brushed();

        // <b>Cloth and not the lounge's leather.</b> Hide came off the shelf and it is a warm dark red:
        // under a cove that is the colour of a tungsten lamp it reads orange, and five chairs with orange
        // headrests standing in an otherwise grey room look like five chairs somebody has re-covered. Seat
        // fabric in a room like this is a dark blue-grey wool, for the same reason the walls are: nothing
        // in here is allowed to have a colour except what is on the ceiling.
        var wool = Finish.Seating(0.150f, 0.145f, 0.185f);
        var moulded = Finish.Plastic(0.085f, 0.090f, 0.108f);
        var t = Deck.WallThickness;

        // Carpet, wall to wall, and it is the only carpeted floor in the building. Two reasons and both are
        // the room: a hard floor under a dome is a whispering gallery, and — the one that shows — every
        // other floor here is tile or board with a specular highlight on it, which is a second set of lamps
        // reflected up at somebody who is supposed to be looking at a sky.
        var pan = Primitives.Disc(Radius + 0.45f, 64);

        root.Children.Add(new MeshNode(Fabric.Map(pan, carpet, Vector3.Zero), carpet) { Name = "floor" });

        // The wall: ten flats and two openings, twelve sectors of thirty degrees.
        foreach (var angle in Blanks.Concat(Pictures))
            root.Children.Add(Tangential(
                Chord(Radius) + Overlap, Height, Radius + t / 2f, Height / 2f, angle, lining));

        // <b>The skirting, and the architraves.</b> Both are what a lined wall has where it stops, and
        // both were missing: the fabric ran to the carpet and was cut off square at each doorway, which is
        // a wall of cloth with its edges showing, and nobody who has fitted one leaves that. A hundred and
        // forty millimetres of black-stained timber round the foot, twelve lengths mitred at the corners
        // — see the cornice above for why twelve slabs of brushed metal were wrong there and why twelve
        // of stained timber are right here: a stain has no grain to step at a joint, and a skirting is cut
        // in lengths anyway. The doorways get posts and a head in the cornice's metal, standing proud of
        // the lining on the room side, because every change of material in this building happens at a
        // piece of metal and a doorway is where the lining changes to a corridor.
        var stained = Finish.Stained();
        const float skirt = 0.14f;
        const float proud = 0.024f;

        foreach (var angle in Blanks.Concat(Pictures))
            root.Children.Add(Tangential(
                Chord(Radius - proud / 2f) + 0.06f, skirt, Radius - proud / 2f, skirt / 2f, angle, stained, proud));

        // A guide light let into the skirting of every sector that has one: the low blue ring a dark
        // auditorium keeps on so that a row can be found in it, and the one thing in this room that stays
        // lit whatever the chapter has switched off. Unlit, so it is exactly as bright in a blackout as
        // it is with the coves up, which is what a fitting on its own circuit is.
        var guide = Fabric.Emissive(0.22f, 0.40f, 0.88f);

        foreach (var angle in Blanks.Concat(Pictures))
            root.Children.Add(Tangential(0.14f, 0.020f, Radius - proud - 0.004f, 0.095f, angle, guide, 0.008f));

        foreach (var angle in new[] { EntranceAngle, ExitAngle })
        {
            // Fifty millimetres wider and thirty taller than the standard opening, which is the fix every
            // wall in this building that meets another wall's doorway takes: two rooms cutting the same
            // hole put two reveals at two depths, and what that looks like is hatching that crawls. The
            // wider hole loses, so the reveal anybody sees belongs to the neighbour.
            var opening = Deck.DoorWidth + 0.05f;
            var head = Deck.DoorHeight + 0.03f;

            var door = Fabric.PiercedWall(
                Chord(Radius) + Overlap, Height, t,
                doorCentre: 0f, opening, head, lining);

            door.Position = Ring(angle, Radius + t / 2f);
            door.RotationDegrees = new Vector3(0f, Yaw(angle), 0f);

            // The architrave, on the room side: local +Z is inward here, as it is on every mount on this
            // ring — see Hang. Twenty millimetres of it buried in the wall so no face is level with one.
            const float leg = 0.09f;

            foreach (var side in new[] { -1f, 1f })
                door.Children.Add(Fabric.Slab(
                    new Vector3(leg, head, 0.05f),
                    new Vector3(side * (opening + leg) / 2f, head / 2f, t / 2f + 0.005f),
                    brushed, "architrave", Finish.Close));

            door.Children.Add(Fabric.Slab(
                new Vector3(opening + 2f * leg, leg, 0.05f),
                new Vector3(0f, head + leg / 2f, t / 2f + 0.005f),
                brushed, "architrave.head", Finish.Close));

            // And the skirting either side of the opening, stopping at the architrave.
            var run = (Chord(Radius - proud / 2f) - opening - 2f * leg) / 2f;

            foreach (var side in new[] { -1f, 1f })
                door.Children.Add(Fabric.Slab(
                    new Vector3(run + 0.03f, skirt, proud),
                    new Vector3(side * (opening / 2f + leg + run / 2f), skirt / 2f, t / 2f - proud / 2f),
                    stained, "skirting"));

            // And the sign, beside the opening rather than over it, because over it is where the cornice
            // is. Green on black rather than a lit box: it has to be legible in a blackout and not be a
            // lamp during a show, and additive lettering on a plaque is both.
            door.Children.Add(Fabric.Label(
                "EXIT", new Vector3(opening / 2f + leg + 0.24f, 2.18f, t / 2f + 0.02f), 0.05f,
                colour: new Vector3(0.40f, 1f, 0.50f)));

            root.Children.Add(door);
        }

        // <b>The cornice, which used to be a remark, then twelve slabs, and is now one ring.</b> The note
        // here said the top face of the wall was a cornice and that nobody had designed it, which was true
        // and was the problem: what a dome springing off a bare wall head actually looks like is a ceiling
        // resting on a wall, and every room ever built with a dome in it puts a moulding at that line
        // precisely so it does not.
        //
        // Twelve slabs was the obvious way to build it, one to a sector like the wall, and it was wrong for
        // a reason that only shows on brushed metal: each one is mapped from its own world position, so
        // twelve adjacent pieces of the same material take the image at twelve different offsets — and what
        // draws is not a moulding, it is a row of light and dark plates with a step at every joint. They
        // also overlapped each other by three hundred millimetres, which put two horizontal faces in the
        // same plane twelve times over.
        //
        // A torus has none of that. It is one surface, one mapping, no joints anywhere, and it is what a
        // cornice actually is — a moulding is turned on a lathe or run on a bench, and either way it is
        // continuous. That the wall behind it is twelve flats and this is a circle is not a mismatch: the
        // ring stands off the wall by a hundred and seventy millimetres at the middle of each sector and
        // meets it at the corners, which is what a circular moulding scribed to a faceted wall does.
        var band = Finish.Brushed();

        // Twenty repeats round thirty metres of circumference, which is a metre and a half a repeat — the
        // building's own pitch. The torus's own coordinates run once round in u, so without this the
        // brushed grain is stretched over the whole ring.
        band.UvScale = new Vector2(20f, 1f);

        root.Children.Add(new MeshNode(
            Primitives.Torus(4.74f, 0.11f, 120, 14).WithGeneratedTangents(), band)
        {
            Position = new Vector3(0f, 2.60f, 0f),
            Name = "cornice"
        });

        // And a shadow gap under it: a thin dark reveal, half buried in the wall, so the moulding reads as
        // applied rather than as the wall getting thicker at the top.
        root.Children.Add(new MeshNode(
            Primitives.Torus(4.80f, 0.035f, 120, 8), Fabric.DarkMetal)
        {
            Position = new Vector3(0f, 2.44f, 0f),
            Name = "cornice.reveal"
        });

        // ---- the dome -----------------------------------------------------------------------------
        //
        // Two pieces and a ring between them, and the split is CrownAngle's story rather than this one.
        // Both are inverted before they are hung: Primitives.Sphere builds a ball, its normals point out,
        // and every one of them is facing away from the only room that can see it. That was invisible while
        // this surface was unlit — an unlit fragment is base colour times base image and has no normal in
        // it anywhere — and it is the first thing that shows the moment it is not.
        var screen = Finish.Screen();

        // Sixteen gores round and three courses down, which lands a panel at about two metres square at the
        // rim. The cap's own coordinates run longitude across and latitude down and are useless as they
        // stand — one image stretched over thirty-two metres of circumference — so this is the number that
        // makes a generated surface a physical one.
        screen.UvScale = new Vector2(16f, 3f);

        var gores = Fabric
            .Inverted(Primitives.Sphere(
                Bowl, 96, 40,
                latitudeDegrees: float.RadiansToDegrees(Sweep) - CrownAngle,
                latitudeStartDegrees: CrownAngle))
            .WithGeneratedTangents();

        root.Children.Add(new MeshNode(gores, screen) { Position = Vault, Name = "dome" });

        // The crown: plain sheet, no maps, no tiling and therefore nothing to alias.
        // A shade under the gores rather than equal to them, because the gores are perforated and this
        // is not: a fifth of that surface is holes, so its average is darker than its base colour and a
        // crown at the same number reads as a lighter disc.
        var plain = new Material
        {
            BaseColor = new Vector4(0.83f, 0.83f, 0.82f, 1f),
            Metallic = 0f,
            Roughness = 0.94f,
            Name = "crown"
        };

        root.Children.Add(new MeshNode(
            Fabric.Inverted(Primitives.Sphere(
                Bowl, 96, 10, latitudeDegrees: CrownAngle + 0.4f)),
            plain)
        {
            Position = Vault,
            Name = "dome.crown"
        });

        var joint = float.DegreesToRadians(CrownAngle);

        root.Children.Add(new MeshNode(
            Primitives.Torus(Bowl * MathF.Sin(joint), 0.011f, 72, 6), brushed)
        {
            Position = Vault + new Vector3(0f, Bowl * MathF.Cos(joint), 0f),
            Name = "dome.ring"
        });

        // The photographs, and the cove that lights them.
        for (var i = 0; i < Pictures.Length; i++)
            Hang(root, Pictures[i], i, brushed, stained);

        Cove = new Lamp[Coves.Length];

        for (var i = 0; i < Coves.Length; i++)
            Cove[i] = Fitting(root, Coves[i], brushed);

        // The projector, on the room's own axis, which is the only place it can be.
        root.Children.Add(Projector(brushed, moulded, out _ball, out _pinholes));

        // And the counter, in the one sector nothing else wants.
        root.Children.Add(Kiosk(brushed, moulded));
        root.Children.Add(Skyline());
        root.Children.Add(Lectern(brushed, moulded, stained));

        // Five chairs in one row, and the row is straight rather than curved. A curved row aims every seat
        // at one point, which is right for a screen and wrong for a dome: the thing being watched is the
        // whole ceiling, and a chair turned two degrees off its neighbour buys nothing and costs a walk
        // that has to thread between five different footprints.
        // Local +X runs along the row and local +Z is the way the chairs look, which is the yaw that sends
        // a node's own Z to Ring(Facing) — ninety less the bearing, and it is the one turn in this file
        // that is not the wall's.
        var bank = new Node
        {
            Name = "seats",
            Position = Rake,
            RotationDegrees = new Vector3(0f, 90f - Facing, 0f)
        };

        // The dais is a metre and a half deep and not two, and the four hundred millimetres it gives up are
        // the aisle behind the back of the chairs — which is where the walk comes along. A platform that
        // reached under his feet would be a platform he is standing inside, and Ground.Audit says so.
        bank.Children.Add(Fabric.Slab(
            new Vector3(Apart * 5f + 0.5f, Dais, 1.5f),
            new Vector3(2f * Apart, Dais / 2f, 0.05f),
            carpet,
            "dais"));

        // One beam under the whole rank rather than legs under each chair, which is what a rank of seating
        // actually is and is also four fewer boxes a walk can be inside.
        bank.Children.Add(Fabric.Slab(
            new Vector3(Apart * 5f + 0.16f, 0.10f, 0.16f),
            new Vector3(2f * Apart, Dais + 0.20f, -0.06f),
            brushed,
            "seats.beam",
            Finish.Close));

        for (var i = 0; i < 5; i++)
            bank.Children.Add(Chair(i * Apart, wool, brushed, moulded, cup: i == 2));

        root.Children.Add(bank);

        // And the sky under the dome, which is everything in this room that moves. It hangs its own shell
        // inside the cap and owns no part of the fabric — see the note at the top about what changed.
        Show = new Sky(root);

        foreach (var lamp in Cove)
            lamp.Dim(0f);

        Running(0f);
    }

    /// <summary>The five cove fittings, which are this room's four slots and its fifth. In the order the
    /// walk meets them: by the two doorways, then the three over the photographs.</summary>
    public Lamp[] Cove { get; }

    /// <summary>The show, kept so the chapter and the free walk can drive the same one.</summary>
    public Sky Show { get; }

    /// <summary>The one nearest both doorways, which is what is lit before he is through either.</summary>
    public Lamp Way => Cove[0];

    /// <summary>All five, for switching the room off in one line.</summary>
    public Lamp[] All => Cove;

    /// <summary>
    /// The projector, running: its pinholes lit and its ball turning.
    ///
    /// <b>It is driven and not decorative, and one of the seven notes on the last cut is why.</b> A
    /// modelled machine standing in the middle of a room doing nothing is read immediately as a machine
    /// that is broken — it was said about a gear in the engine room and it is true of anything with a
    /// mechanism in it. This one has an obvious axis and an obvious job, so it turns while the show is on
    /// and stops when the house lights come up, which is what the real thing does.
    ///
    /// <paramref name="level"/> is the show's own, so the pinholes come up exactly as the sky does. The
    /// turn is slow — a degree and a half a second — because the thing it is standing in for takes an hour
    /// to go round and the point is that it is not still.
    /// </summary>
    public void Running(float level, float clock = 0f)
    {
        level = Math.Clamp(level, 0f, 1f);

        _pinholes.EmissiveColor = new Vector3(0.95f, 0.90f, 0.78f) * level;
        _ball.RotationDegrees = new Vector3(0f, clock * 1.5f * level, 0f);
    }

    /// <summary>The doorway in, from the pattern shop.</summary>
    public static Vector3 Entrance =>
        Deck.Planetarium + Ring(EntranceAngle, Radius + Deck.WallThickness / 2f, Deck.Eye);

    /// <summary>The doorway out, into the link.</summary>
    public static Vector3 Exit =>
        Deck.Planetarium + Ring(ExitAngle, Radius + Deck.WallThickness / 2f, Deck.Eye);

    /// <summary>The middle of one of the four photographs, in world coordinates. What the walk looks at.</summary>
    public static Vector3 Picture(int index) =>
        Deck.Planetarium + Ring(Pictures[Math.Clamp(index, 0, Pictures.Length - 1)], Radius - 0.10f, 1.52f);

    /// <summary>
    /// Where he stands to look at one: three metres three out from the middle, which is a metre and a half
    /// from the picture.
    ///
    /// <b>It is also as far out as any walk in this room is allowed to go, and the reason is the audit
    /// rather than the wall.</b> <c>Ground</c> keeps every solid as the axis-aligned box the renderer
    /// already computed, and the box of a two-metre-eight wall slab turned thirty degrees is nearly a metre
    /// deeper than the slab — so eight of this room's twelve sectors have bounds that reach in to radius
    /// three metres seven, and a camera at three metres seven is reported as inside plaster while standing
    /// a clear metre from it. That is not a fault in the audit; it is what a bound <i>is</i>, and the
    /// alternative is a second description of the geometry that can disagree with the renderer's.
    ///
    /// So the walking circle is three metres three and every waypoint in <c>Stars</c> is inside it. A round
    /// room built out of flat slabs has that ceiling and the next one will have it too.
    /// </summary>
    public static Vector3 Before(int index) =>
        Deck.Planetarium + Ring(Pictures[Math.Clamp(index, 0, Pictures.Length - 1)], 3.3f, Deck.Eye);

    /// <summary>A point on the floor at eye height: how far round the room, and how far out.</summary>
    public static Vector3 At(float degrees, float radius) =>
        Deck.Planetarium + Ring(degrees, radius, Deck.Eye);

    /// <summary>Where his eye is once he has sat down.</summary>
    public static Vector3 Seat => Deck.Planetarium + Rake + new Vector3(0f, 1.30f, 0f);

    /// <summary>Which way the row runs, as a unit vector on the deck.</summary>
    private static Vector3 Across => Ring(Facing - 90f, 1f);

    /// <summary>
    /// The middle of the seat row, on the floor: the third chair of five.
    ///
    /// It is what the free walk measures "has somebody come to watch" against, and the middle rather than
    /// <see cref="Rake"/> because <see cref="Rake"/> is one end of a rank three metres three long — a
    /// visitor standing at the far end of it would be further from the end chair than from the door. See
    /// <c>Rounds.Showtime</c>, which is the lounge's own trick applied to a room with a ceiling.
    /// </summary>
    public static Vector3 Auditorium => Deck.Planetarium + Rake + Across * (Apart * 2f);

    /// <summary>
    /// Where he stands before he sits, and where he is standing again when he gets up: a chair's width off
    /// the open end of the row, in line with it.
    ///
    /// In line, and that is the point of it. Coming at a chair from in front means climbing over the arm
    /// and coming at it from behind means climbing over the back; coming at the end one along the row is
    /// one step sideways, and it is the only approach to a seat in a rank that is not a vault.
    /// </summary>
    public static Vector3 Approach =>
        Deck.Planetarium + Rake - Across * (Apart * 1.25f) + new Vector3(0f, Deck.Eye, 0f);

    /// <summary>And what a walk aims at while he is still on his feet and coming to it — the chair, not the
    /// eye, because a camera that looks at where it is about to be is a camera looking at nothing.</summary>
    public static Vector3 Sitting => Deck.Planetarium + Rake + new Vector3(0f, 0.62f, 0f);

    /// <summary>What somebody in that chair is looking at: the middle of the show, thirty degrees up and
    /// to the north-west. See <see cref="Sky.Heart"/>.</summary>
    public static Vector3 Zenith => Deck.Planetarium + Sky.Heart;

    /// <summary>
    /// What a walk looks at when it wants the dome rather than the show: a point up the north face of it,
    /// not the crown.
    ///
    /// The crown is directly over the middle of the room, so aiming at it from anywhere on the floor is a
    /// pitch of forty-five degrees and upward — <c>Ground.Audit</c> measured exactly that, and forty-five
    /// degrees is a person with their head right back rather than a person noticing a ceiling. Two thirds
    /// of the way up the far side is twenty, which is a glance.
    /// </summary>
    public static Vector3 Overhead => Deck.Planetarium + new Vector3(-1.4f, Crown - 1.1f, 2.4f);

    // ---- the ring ------------------------------------------------------------------------------------

    /// <summary>How wide one sector's chord is at a given radius. Twelve sectors, so fifteen degrees each
    /// side of the middle.</summary>
    private static float Chord(float radius) => 2f * radius * MathF.Sin(15f * MathF.PI / 180f);

    private static Vector3 Ring(float degrees, float radius, float y = 0f)
    {
        var t = degrees * MathF.PI / 180f;
        return new Vector3(MathF.Cos(t) * radius, y, MathF.Sin(t) * radius);
    }

    /// <summary>The yaw that turns a slab built along X into one lying along the wall at
    /// <paramref name="degrees"/>. Derived in <c>Rotunda.Facing</c>, which is the same ring.</summary>
    private static float Yaw(float degrees) => -(degrees + 90f);

    private static MeshNode Tangential(
        float width, float height, float radius, float y, float degrees, Material material,
        float thickness = Deck.WallThickness)
    {
        var slab = Fabric.Slab(
            new Vector3(width, height, thickness), Ring(degrees, radius, y), material);

        slab.RotationDegrees = new Vector3(0f, Yaw(degrees), 0f);

        return slab;
    }

    // ---- what is in it -------------------------------------------------------------------------------

    /// <summary>
    /// One photograph: a frame against the wall, a mount inside it, the print a centimetre proud of that,
    /// and a plate under the lot saying what it is.
    ///
    /// Four planes at four depths rather than one textured slab, and the millimetres between them are not
    /// a detail. A print painted onto the front face of its own frame is a print coplanar with the frame,
    /// which is the fault the pattern shop's stands had and is the one this building keeps finding — see
    /// <c>PatternShop.Clearance</c>. Five millimetres at a metre and a half is a shadow line, which is what
    /// a framed picture has anyway, and a mount is the reason a photograph in a frame does not touch it.
    ///
    /// <b>All of them are hung at positive local Z and the sign is the whole of it.</b> A mount on this
    /// ring is turned by <see cref="Yaw"/>, and that yaw sends the mount's own +Z <i>inward</i>, toward the
    /// middle of the room — so a picture at −0.03 is a picture thirty millimetres inside the plaster.
    /// Written that way first, and what it produced was four framed photographs that were built, textured,
    /// lit and completely invisible, in a room whose other half is a nebula bright enough that nobody
    /// would have gone looking for them.
    /// </summary>
    private static void Hang(Node root, float angle, int index, Material brushed, Material stained)
    {
        var mount = new Node
        {
            Name = "print",
            Position = Ring(angle, Radius, 1.52f),
            RotationDegrees = new Vector3(0f, Yaw(angle), 0f)
        };

        // Dark, and it was brushed metal, and then it was moulded plastic. A bright frame round a bright
        // mount is not a frame, it is a white border with a lighter line in it — the picture had no edge
        // at all from three metres. And a plastic one under a cove light was a crumpled one: four tenths
        // of a millimetre of orange peel is the right relief for a chair shell and the wrong one for the
        // calmest object on a wall. What a framed photograph reads as is a dark stained rebate, a light
        // mount and then the print, in that order and with a step between each — so the frame is two
        // steps itself, a wider back and a narrower face, which is the cheapest moulding profile there is
        // and reads as one from the walking circle.
        // <b>Forty millimetres deep and not sixty, and the layers do not share a millimetre.</b> The
        // mount was set at fifty-eight with a twenty-millimetre body, which put it straddling the frame's
        // own front face — two surfaces crossing inside each other along the top rail, which is a
        // speckled band under a cove light and is the same fault as everything else on this list. Back,
        // face, mount, print: nought to eighteen, eighteen to forty, forty-five to sixty-five, seventy to
        // eighty-two, with five clear millimetres between each pair that is not one piece of wood.
        mount.Children.Add(Fabric.Slab(
            new Vector3(1.36f, 1.06f, 0.018f), new Vector3(0f, 0f, 0.009f), stained, "frame.back", Finish.Close));

        mount.Children.Add(Fabric.Slab(
            new Vector3(1.30f, 1.00f, 0.022f), new Vector3(0f, 0f, 0.029f), stained, "frame", Finish.Close));

        // The mount board: rag paper, all but white, and matte enough to have no highlight of its own. It
        // is the piece that makes a print read as a print — a photograph that runs to the edge of its frame
        // is a poster.
        var board = new Material
        {
            BaseColor = new Vector4(0.60f, 0.59f, 0.56f, 1f),
            Roughness = 0.94f,
            Metallic = 0f,
            Name = "mount"
        };

        mount.Children.Add(new MeshNode(Primitives.Box(1.20f, 0.90f, 0.02f), board)
        {
            Position = new Vector3(0f, 0f, 0.055f),
            Name = "print.mount"
        });

        // The picture itself. Roughness high and metallic nought, because it is paper under a cove light
        // and the one thing it must not do is throw that light back as a hot spot across its own middle.
        var paper = new Material
        {
            BaseColor = Vector4.One,
            BaseColorTexture = Sky.Print(index),
            Roughness = 0.86f,
            Metallic = 0f
        };

        mount.Children.Add(new MeshNode(Primitives.Box(0.98f, 0.68f, 0.012f), paper)
        {
            Position = new Vector3(0f, 0.02f, 0.076f),
            Name = "print.face"
        });

        // And the label. Nobody can read it and everybody has seen one, which is the whole of what a
        // caption plate is worth on a wall at a metre and a half.
        mount.Children.Add(Fabric.Slab(
            new Vector3(0.34f, 0.07f, 0.012f), new Vector3(0f, -0.62f, 0.030f), brushed, "print.plate",
            Finish.Close));

        root.Children.Add(mount);
    }

    /// <summary>
    /// One cove fitting: a brushed trough against the wall with a lamp behind it.
    ///
    /// It is the only kind of light in the building that is not a box on a ceiling, and it is the room. A
    /// pendant lamp needs a flat ceiling to hang from and there is not one here — the dome starts at two
    /// seventy-five and reaches four metres nine in the middle, so a fitting on the room's axis would be a
    /// box hanging in mid air with two metres of nothing above it. The answer is the one every real
    /// planetarium uses: put the fittings in the cornice, aim them at the wall, and let the room be lit by
    /// what comes back off it.
    ///
    /// The light itself sits <i>inboard</i> of the trough rather than inside it, which is the difference
    /// between a cove and a lamp with a shade on it: what is being modelled is the wash, and the wash comes
    /// off the wall a third of a metre in front of the fitting.
    ///
    /// <b>Five and not three and a fifth, and the walls are why.</b> Every other room in the building is
    /// lined with plaster that gives back a third of what falls on it and this one is lined with cloth that
    /// gives back a tenth, so the same lamp in this room is two thirds darker for reasons that have nothing
    /// to do with the lamp. What was tuned against plaster has to be retuned against what is actually
    /// there, and the number that came back is five.
    /// </summary>
    private static Lamp Fitting(Node root, float angle, Material brushed)
    {
        var mount = new Node
        {
            Name = "cove",
            Position = Ring(angle, CoveRadius, CoveHeight),
            RotationDegrees = new Vector3(0f, Yaw(angle), 0f)
        };

        mount.Children.Add(Fabric.Slab(
            new Vector3(1.30f, 0.10f, 0.22f), Vector3.Zero, brushed, "cove.trough", Finish.Close));

        var strip = Fabric.Slab(
            new Vector3(1.12f, 0.03f, 0.16f),
            new Vector3(0f, 0.062f, 0f),
            Fabric.Emissive(1f, 0.88f, 0.70f),
            "cove.strip");

        mount.Children.Add(strip);
        root.Children.Add(mount);

        // <b>A metre and a quarter off the wall, and it was half a metre.</b> Half a metre from a
        // surface is not a cove, it is a lamp pressed against plaster: the inverse square across the first
        // metre of wall is six to one, so what draws is a white patch under every fitting and a dark wall
        // between them, which is the exact opposite of what a cove is for. Far enough back and the same
        // lamp lights two metres of wall to within a stop of itself.
        var light = new PointLight
        {
            Position = Deck.Planetarium + Ring(angle, CoveRadius - 1.0f, CoveHeight + 0.12f),
            Color = new Vector3(1f, 0.90f, 0.76f),
            Intensity = 4f,
            Range = 9f,
            Decay = 2f
        };

        return new Lamp(mount, strip, light, 4f);
    }

    /// <summary>
    /// The star projector: a plinth, a column, a fork and a perforated ball, on the room's own axis.
    ///
    /// <b>It is here because the room did not explain itself.</b> What was in the middle of this floor was
    /// carpet — five chairs to one side of it and a dome overhead, and nothing anywhere saying what the
    /// dome was for or where the sky was supposed to be coming from. A planetarium without a projector in
    /// it is a round room with a light-coloured ceiling; a planetarium with one is unmistakable from the
    /// doorway, before a single star has been shown.
    ///
    /// The shape is the shape of the machine that gave these rooms their name: a ball with a few thousand
    /// holes drilled through it and a lamp inside, hung in a fork so that it can be turned to any hour of
    /// any night. The holes are an emissive map rather than geometry, which is the only sane way to draw a
    /// few thousand of anything — and they are driven, so the thing lights up when the show does. See
    /// <see cref="Running"/>.
    ///
    /// Three hundred and eighty millimetres across the foot, and that number is the walk's. Every waypoint
    /// in this room is at least a metre and a half out from the middle, so a machine that keeps inside four
    /// tenths is a machine nobody can be standing in — which is the check <c>Ground.Audit</c> makes and the
    /// reason the first thing put in the middle of a room in this building is measured before it is drawn.
    /// </summary>
    private static Node Projector(Material brushed, Material moulded, out Node ball, out Material pinholes)
    {
        var mount = new Node { Name = "projector" };

        mount.Children.Add(new MeshNode(Primitives.Cylinder(0.30f, 0.38f, 0.09f, 40), moulded)
        {
            Position = new Vector3(0f, 0.045f, 0f),
            Name = "projector.foot"
        });

        var column = Pillar - 0.09f;

        mount.Children.Add(new MeshNode(Primitives.Cylinder(0.10f, 0.135f, column, 32), brushed)
        {
            Position = new Vector3(0f, 0.09f + column / 2f, 0f),
            Name = "projector.column"
        });

        mount.Children.Add(new MeshNode(Primitives.Cylinder(0.155f, 0.155f, 0.07f, 32), moulded)
        {
            Position = new Vector3(0f, Pillar, 0f),
            Name = "projector.collar"
        });

        // The fork: two arms and the trunnion between them. It is the piece that says the ball turns.
        foreach (var side in new[] { -1f, 1f })
            mount.Children.Add(Fabric.Slab(
                new Vector3(0.045f, 0.40f, 0.12f),
                new Vector3(side * 0.30f, Pillar + 0.21f, 0f),
                brushed,
                "projector.arm",
                Finish.Close));

        ball = new Node { Name = "projector.ball", Position = new Vector3(0f, Pillar + 0.32f, 0f) };

        // Near black, half a metal, and glossy — which is what a machined shell under one lamp looks like
        // and is also what keeps a dark ball from disappearing into a dark room. What is actually seen of
        // this from a chair is the rim light down one side of it and the pinholes.
        pinholes = new Material
        {
            BaseColor = new Vector4(0.055f, 0.058f, 0.065f, 1f),
            Metallic = 0.55f,
            Roughness = 0.32f,
            EmissiveTexture = Drilled.Value,
            EmissiveColor = Vector3.Zero,
            Name = "projector.shell"
        };

        ball.Children.Add(new MeshNode(Primitives.Sphere(0.26f, 48, 32), pinholes) { Name = "projector.globe" });

        // Two lens turrets on the axis, because a fork without something for it to hold is a bracket.
        foreach (var side in new[] { -1f, 1f })
            ball.Children.Add(new MeshNode(Primitives.Cylinder(0.05f, 0.07f, 0.06f, 24), brushed)
            {
                Position = new Vector3(side * 0.27f, 0f, 0f),
                RotationDegrees = new Vector3(0f, 0f, 90f),
                Name = "projector.lens"
            });

        mount.Children.Add(ball);

        return mount;
    }

    /// <summary>The pinholes: a few thousand of them, hashed onto a sphere's own coordinates. Emissive
    /// only, so the shell stays a shell and the holes are what is lit.</summary>
    private static readonly Lazy<Texture> Drilled = new(() =>
        Grain.Colour(256, "projector.holes", (u, v) =>
        {
            const int cells = 74;

            var x = u * cells;
            var y = v * cells;

            var cx = (int)MathF.Floor(x);
            var cy = (int)MathF.Floor(y);

            var pick = Grain.Pick(cx, cy, 617);

            if (pick < 0.62f)
                return Vector3.Zero;

            var px = cx + 0.25f + 0.5f * Grain.Pick(cx, cy, 618);
            var py = cy + 0.25f + 0.5f * Grain.Pick(cx, cy, 619);

            var dx = x - px;
            var dy = y - py;

            return new Vector3(MathF.Exp(-(dx * dx + dy * dy) * 46f) * (0.4f + 0.6f * pick));
        }));

    /// <summary>
    /// One chair, in the row's own frame: +X across the row, +Z the way it is looking.
    ///
    /// Reclined by twenty-two degrees, which is not styling. A seat that is upright puts a dome's crown
    /// behind the top of your head, so everything the chapter is showing would be off the top of the frame
    /// for anybody who took the mouse — and the show is deliberately staged thirty degrees up rather than
    /// overhead for the same reason.
    ///
    /// <b>It is a moulded shell with rolled edges now and it was seven boxes.</b> The note that these
    /// looked bad is right and the reason is specific: a chair is the one object in this building that is
    /// looked at from arm's length — the film sits in one and the free walk stands over the row — and at
    /// six hundred millimetres a box is a box. What makes furniture read as furniture is that it has no
    /// sharp edges anywhere a person touches: the front of a seat is a roll, the top of a back is a roll,
    /// an arm is a half-round, and the sides of a squab are bolsters. Every one of those is a cylinder,
    /// and they are the difference between a seat and a crate with a cushion on it.
    ///
    /// The envelope is unchanged to the millimetre except at the headrest, which grew forty. Everything
    /// added is inset from the box it replaced, so the aisle <c>Ground.Audit</c> measured is the aisle
    /// that is still there.
    /// </summary>
    private static Node Chair(float across, Material wool, Material brushed, Material moulded, bool cup)
    {
        var chair = new Node { Name = "chair", Position = new Vector3(across, Dais, 0f) };

        // The pan: a moulded tray under the cushion, with the front edge rolled over. It is what you see
        // of a seat from in front and below, which — in a rank on a dais — is most of the time.
        chair.Children.Add(Fabric.Slab(
            new Vector3(0.54f, 0.09f, 0.48f), new Vector3(0f, 0.415f, -0.02f), moulded, "chair.pan",
            Finish.Close));

        chair.Children.Add(Roll(0.048f, 0.54f, new Vector3(0f, 0.415f, 0.215f), Along.X, moulded, "chair.pan.roll"));

        // The cushion, and its own roll. Two rolls one above the other at the front of a seat is what
        // upholstery over a frame actually does, and it is the silhouette everybody recognises.
        chair.Children.Add(Fabric.Slab(
            new Vector3(0.50f, 0.11f, 0.44f), new Vector3(0f, 0.505f, -0.02f), wool, "chair.squab",
            Finish.Close));

        chair.Children.Add(Roll(0.058f, 0.50f, new Vector3(0f, 0.500f, 0.195f), Along.X, wool, "chair.squab.roll"));

        var back = new Node
        {
            Name = "chair.rake",
            Position = new Vector3(0f, 0.50f, -0.24f),
            RotationDegrees = new Vector3(-22f, 0f, 0f)
        };

        // The shell is what the room sees of four of these five, and the pad is what the fifth one's
        // occupant sits against. Bolsters down each side, which is the other half of what makes a seat
        // back read as upholstered rather than as a panel.
        back.Children.Add(Fabric.Slab(
            new Vector3(0.56f, 0.60f, 0.055f), new Vector3(0f, 0.30f, -0.045f), moulded, "chair.shell",
            Finish.Close));

        back.Children.Add(Fabric.Slab(
            new Vector3(0.44f, 0.50f, 0.085f), new Vector3(0f, 0.28f, 0.030f), wool, "chair.pad",
            Finish.Close));

        foreach (var side in new[] { -1f, 1f })
            back.Children.Add(Roll(0.045f, 0.52f, new Vector3(side * 0.235f, 0.28f, 0.015f), Along.Y, wool,
                "chair.bolster"));

        back.Children.Add(Roll(0.050f, 0.56f, new Vector3(0f, 0.60f, -0.020f), Along.X, moulded, "chair.top"));

        back.Children.Add(Fabric.Slab(
            new Vector3(0.30f, 0.13f, 0.10f), new Vector3(0f, 0.705f, 0.015f), wool, "chair.headrest",
            Finish.Close));

        foreach (var side in new[] { -1f, 1f })
            back.Children.Add(Roll(0.065f, 0.10f, new Vector3(side * 0.150f, 0.705f, 0.015f), Along.Z, wool,
                "chair.headrest.end"));

        chair.Children.Add(back);

        foreach (var side in new[] { -1f, 1f })
        {
            chair.Children.Add(Fabric.Slab(
                new Vector3(0.075f, 0.085f, 0.44f), new Vector3(side * 0.315f, 0.545f, 0f), moulded,
                "chair.arm", Finish.Close));

            chair.Children.Add(Roll(0.038f, 0.44f, new Vector3(side * 0.315f, 0.588f, 0f), Along.Z, brushed,
                "chair.arm.roll"));

            // The cup holder, which is a ring let into the arm and is the one detail on this chair that
            // says what the room next to it sells. See Kiosk.
            chair.Children.Add(new MeshNode(Primitives.Torus(0.042f, 0.010f, 20, 6), brushed)
            {
                Position = new Vector3(side * 0.315f, 0.606f, 0.150f),
                Name = "chair.holder"
            });
        }

        // And one of them has a cup in it, left by whoever was in here last. It is the cheapest possible
        // piece of story and it is the only thing in this room that says anybody has ever used it.
        if (cup)
            chair.Children.Add(Tub(new Vector3(0.315f, 0.606f, 0.150f), 0.85f));

        return chair;
    }

    /// <summary>Which way a rolled edge lies, because a cylinder is built about +Y and has to be turned.</summary>
    private enum Along
    {
        X,
        Y,
        Z
    }

    /// <summary>
    /// A rolled edge: a capped cylinder lying along one axis.
    ///
    /// It is the whole of what separates this furniture from the first version. Every edge a person would
    /// touch on a real chair is a radius, because upholstery is fabric stretched over foam and foam has no
    /// corners — and a renderer that draws boxes gives you corners unless you ask for something else.
    /// </summary>
    private static MeshNode Roll(float radius, float length, Vector3 at, Along axis, Material material, string name) =>
        new(Primitives.Cylinder(radius, radius, length, 14), material)
        {
            Position = at,
            RotationDegrees = axis switch
            {
                Along.X => new Vector3(0f, 0f, 90f),
                Along.Z => new Vector3(90f, 0f, 0f),
                _ => Vector3.Zero
            },
            Name = name
        };

    /// <summary>
    /// A tub of popcorn: a tapered cup and a heap standing out of it.
    ///
    /// Six spheres and a cone, and it is worth every one of them. A room with seating and a counter and
    /// nothing on either is a room that has never been open; a cup left in the arm of the second chair is
    /// eleven years of "nobody has booked the dome" being told by an object instead of by a caption.
    /// </summary>
    private static Node Tub(Vector3 at, float scale)
    {
        var tub = new Node { Name = "tub", Position = at, Scale = new Vector3(scale) };

        tub.Children.Add(new MeshNode(Primitives.Cylinder(0.078f, 0.054f, 0.21f, 20), Striped.Value)
        {
            Position = new Vector3(0f, 0.105f, 0f),
            Name = "tub.cup"
        });

        // The heap, as one mesh rather than seven nodes. See Popcorn.
        tub.Children.Add(new MeshNode(
            Popcorn(9, new Vector3(0.052f, 0.030f, 0.052f), 811), Popped.Value)
        {
            Position = new Vector3(0f, 0.212f, 0f),
            Name = "tub.heap"
        });

        return tub;
    }

    /// <summary>
    /// The refreshment counter, in the one sector of this room nothing else wants.
    ///
    /// <b>It is here because a room with an audience in it has to have somewhere the audience came from.</b>
    /// Five chairs, a projector and four photographs is an exhibit; a counter with a warmer on it, three
    /// cups poured and nobody behind it is a place that opens. The note asked for popcorn and the note is
    /// right about more than popcorn — this is the only object in the building that implies a person who
    /// is not the visitor.
    ///
    /// At a hundred and fifty degrees, which is the south-west arc: behind the seats, out of every frame
    /// the film takes, and nowhere near the walking circle. See <see cref="Before"/> for why that circle
    /// is three metres three and why anything outside it is free.
    /// </summary>
    private static Node Kiosk(Material brushed, Material moulded)
    {
        // Two hundred and ten, which is the sector beside the way out. Two things put it there and both
        // are the room: it is under the cove that is lit whenever anybody is in here — a counter nobody
        // can see is a counter nobody knows about — and it is where a refreshment stand goes, which is
        // between the door and the seats rather than behind them.
        const float bearing = 210f;

        var stand = new Node
        {
            Name = "kiosk",
            Position = Ring(bearing, 4.32f),
            RotationDegrees = new Vector3(0f, Yaw(bearing), 0f)
        };

        stand.Children.Add(Fabric.Slab(
            new Vector3(2.10f, 0.86f, 0.58f), new Vector3(0f, 0.50f, 0f), moulded, "kiosk.body", Finish.Close));

        stand.Children.Add(Fabric.Slab(
            new Vector3(2.24f, 0.06f, 0.66f), new Vector3(0f, 0.96f, 0f), brushed, "kiosk.top", Finish.Close));

        // A recessed kick at the bottom, which is what stops a counter reading as a wardrobe.
        stand.Children.Add(Fabric.Slab(
            new Vector3(2.00f, 0.12f, 0.48f), new Vector3(0f, 0.06f, 0f), Fabric.DarkMetal, "kiosk.kick",
            Finish.Close));

        // <b>The warmer: a glazed cabinet with a kettle in it.</b> The first one was a metal box with a
        // pane leaned against the front, and it read as a metal box — because a single sheet of alpha over
        // a dark interior is very nearly invisible, and a machine you cannot see into is a crate. What
        // makes a glass case read as glass is not the glass. It is the frame round it: four posts and two
        // rails say "there is a pane here" before a single reflection does, and once the eye knows to
        // expect one it reads the sheen as one.
        //
        // Three sides glazed rather than one, so the light from the cove behind goes through it and the
        // heap inside is lit from more than the front. And the kettle, hanging under the lid, which is the
        // one part of the machine that says what the machine does.
        var warmer = new Node { Name = "kiosk.warmer", Position = new Vector3(-0.56f, 0.99f, 0f) };

        var pane = Finish.Glass();

        // A shade more present than a window's. This is a display case at a metre and a half rather than a
        // gallery window at four, and the whole job of it is to be noticed.
        pane.BaseColor = new Vector4(0.84f, 0.90f, 0.88f, 0.24f);
        pane.Roughness = 0.05f;

        warmer.Children.Add(Fabric.Slab(
            new Vector3(0.64f, 0.05f, 0.48f), new Vector3(0f, 0.025f, 0f), brushed, "warmer.base",
            Finish.Close));

        warmer.Children.Add(Fabric.Slab(
            new Vector3(0.68f, 0.05f, 0.52f), new Vector3(0f, 0.625f, 0f), brushed, "warmer.lid",
            Finish.Close));

        // Four posts and four rails: the cabinet's own frame, which is what the pane is set into.
        foreach (var x in new[] { -0.305f, 0.305f })
        foreach (var z in new[] { -0.225f, 0.225f })
            warmer.Children.Add(Fabric.Slab(
                new Vector3(0.032f, 0.56f, 0.032f), new Vector3(x, 0.325f, z), brushed, "warmer.post",
                Finish.Close));

        // The glazing itself: front, both sides and the back, so the case is seen through rather than
        // into. Alpha writes no depth, so the order they are added in does not matter.
        warmer.Children.Add(new MeshNode(Primitives.Box(0.58f, 0.54f, 0.008f), pane)
        {
            Position = new Vector3(0f, 0.325f, 0.225f),
            Name = "warmer.glass"
        });

        warmer.Children.Add(new MeshNode(Primitives.Box(0.58f, 0.54f, 0.008f), pane)
        {
            Position = new Vector3(0f, 0.325f, -0.225f),
            Name = "warmer.glass"
        });

        foreach (var side in new[] { -1f, 1f })
            warmer.Children.Add(new MeshNode(Primitives.Box(0.008f, 0.54f, 0.42f), pane)
            {
                Position = new Vector3(side * 0.305f, 0.325f, 0f),
                Name = "warmer.glass"
            });

        // The kettle, hung off the lid on a bracket, with its own lid tipped open. It is four primitives
        // and it is the difference between a glass box with popcorn in it and a popcorn machine.
        var kettle = new Node { Name = "warmer.kettle", Position = new Vector3(0f, 0.44f, 0f) };

        kettle.Children.Add(new MeshNode(Primitives.Cylinder(0.105f, 0.095f, 0.13f, 20), brushed)
        {
            Name = "kettle.drum"
        });

        kettle.Children.Add(new MeshNode(Primitives.Cylinder(0.112f, 0.112f, 0.018f, 20), Fabric.DarkMetal)
        {
            Position = new Vector3(0.02f, 0.085f, 0.02f),
            RotationDegrees = new Vector3(22f, 0f, 12f),
            Name = "kettle.lid"
        });

        foreach (var side in new[] { -1f, 1f })
            kettle.Children.Add(Fabric.Slab(
                new Vector3(0.022f, 0.16f, 0.022f), new Vector3(side * 0.125f, 0.08f, 0f), brushed,
                "kettle.arm", Finish.Close));

        warmer.Children.Add(kettle);

        // The lamp above it, which is emission rather than a light: this room has four slots and every one
        // of them is spoken for, and a warmer that only glows costs nothing.
        warmer.Children.Add(Fabric.Slab(
            new Vector3(0.46f, 0.02f, 0.36f), new Vector3(0f, 0.588f, 0f), Fabric.Emissive(1f, 0.72f, 0.36f),
            "warmer.lamp"));

        // And the heap, as one mesh. Twenty-six kernels of three lobes each is seventy-eight spheres and
        // one draw — see Popcorn, which is where the merge happens and why.
        warmer.Children.Add(new MeshNode(
            Popcorn(26, new Vector3(0.22f, 0.055f, 0.16f), 907), Popped.Value)
        {
            Position = new Vector3(0f, 0.075f, 0f),
            Name = "warmer.heap"
        });

        // Canopy and finial, striped, which is the one thing in this room allowed to be a colour.
        warmer.Children.Add(Fabric.Slab(
            new Vector3(0.76f, 0.05f, 0.60f), new Vector3(0f, 0.675f, 0f), Striped.Value, "warmer.canopy",
            Finish.Close));

        warmer.Children.Add(new MeshNode(Primitives.Cylinder(0.018f, 0.018f, 0.14f, 12), brushed)
        {
            Position = new Vector3(0f, 0.77f, 0f),
            Name = "warmer.finial"
        });

        stand.Children.Add(warmer);

        // Three poured and waiting, which is the detail that says somebody was expecting an audience.
        for (var i = 0; i < 3; i++)
            stand.Children.Add(Tub(new Vector3(0.34f + i * 0.30f, 0.99f, 0.02f), 1f));

        return stand;
    }

    /// <summary>
    /// A heap of popcorn, as one mesh.
    ///
    /// <b>Three lobes to a kernel, and that is the whole of it.</b> A sphere is a pea. What a popped
    /// kernel actually is is a burst — two or three irregular lobes of starch that have come out of one
    /// grain in different directions — and three overlapping spheres of falling size at small random
    /// offsets is the cheapest thing that reads as one. Twenty-six of those is seventy-eight spheres,
    /// which as separate nodes would be seventy-eight draws for something the size of a fist.
    ///
    /// <see cref="Mesh.Merge"/> makes it one, and this is the room where that matters: the counter is
    /// furniture at the edge of a frame and it is not allowed to cost what the show costs.
    /// </summary>
    private static Mesh Popcorn(int count, Vector3 spread, int seed)
    {
        var parts = new List<Mesh>(count * 3);

        for (var i = 0; i < count; i++)
        {
            // Seated in a flattened disc, weighted to the middle, which is what anything poured into a
            // container settles as.
            var a = Grain.Pick(i, 0, seed) * MathF.Tau;
            var r = MathF.Sqrt(Grain.Pick(i, 1, seed));

            var at = new Vector3(
                MathF.Cos(a) * spread.X * r,
                spread.Y * Grain.Pick(i, 2, seed) * (1.1f - r * 0.5f),
                MathF.Sin(a) * spread.Z * r);

            var size = 0.019f + 0.011f * Grain.Pick(i, 3, seed);

            for (var lobe = 0; lobe < 3; lobe++)
            {
                var shrink = 1f - lobe * 0.24f;

                var off = new Vector3(
                    (Grain.Pick(i, 10 + lobe * 3, seed) - 0.5f) * size * 1.6f,
                    (Grain.Pick(i, 11 + lobe * 3, seed) - 0.5f) * size * 1.4f,
                    (Grain.Pick(i, 12 + lobe * 3, seed) - 0.5f) * size * 1.6f);

                parts.Add(Primitives.Sphere(size * shrink, 9, 6)
                    .Transformed(Matrix4x4.CreateTranslation(at + off)));
            }
        }

        return Mesh.Merge(parts);
    }

    /// <summary>
    /// The town round the foot of the dome: a ring of black card standing on the cornice, cut into
    /// rooftops, chimneys, a church with a spire and clumps of trees, which is what every planetarium
    /// ever built has had round the bottom of its sky and is the thing that makes the show a sky over a
    /// town rather than a picture on a ceiling. From a seat it is a jagged black horizon against the
    /// lowest band of the dome; with the coves up it is a silhouette against a grey wall and reads the
    /// same.
    ///
    /// Laid out by walking round the ring: each piece takes its width in degrees at the cornice's radius
    /// and hands the angle on, with a gap now and then so the skyline has some sky in it. Seeded, so the
    /// town is the same town on every run. Every gable is a square stood on its corner with its lower
    /// half sunk into the cornice, which is a cheaper triangle than any mesh.
    /// </summary>
    private static Node Skyline()
    {
        var town = new Node { Name = "skyline" };

        var card = new Material
        {
            BaseColor = new Vector4(0.018f, 0.018f, 0.022f, 1f),
            Metallic = 0f,
            Roughness = 0.96f,
            Name = "skyline"
        };

        const float radius = 4.72f;
        const float foot = 2.71f;
        const float thick = 0.025f;
        var perDegree = MathF.Tau * radius / 360f;

        var random = new Random(41);
        var angle = 0f;
        var church = 26f + random.NextSingle() * 20f;
        var built = false;

        // A tangential piece of card, positioned by its left-hand edge along the ring and its bottom.
        MeshNode Piece(float from, float width, float height, float bottom, float roll = 0f)
        {
            var piece = Tangential(width, height, radius, bottom + height / 2f, from + width / 2f / perDegree, card, thick);
            piece.Name = "skyline.card";

            if (roll != 0f)
                piece.RotationDegrees = piece.RotationDegrees with { Z = roll };

            return piece;
        }

        while (angle < 360f)
        {
            float width;

            if (!built && angle >= church)
            {
                // The church: a tower with a spire, and the nave beside it.
                width = 0.66f;
                town.Children.Add(Piece(angle, 0.24f, 0.36f, foot));

                town.Children.Add(new MeshNode(Primitives.Cylinder(0.002f, 0.105f, 0.24f, 8), card)
                {
                    Name = "skyline.spire",
                    Position = Ring(angle + 0.12f / perDegree, radius, foot + 0.36f + 0.12f)
                });

                town.Children.Add(Piece(angle + 0.24f / perDegree, 0.40f, 0.17f, foot));
                built = true;
            }
            else
            {
                switch (random.Next(6))
                {
                    case 0 or 1 or 2:
                    {
                        // A house, some with a gable and some with a chimney.
                        width = 0.30f + random.NextSingle() * 0.34f;
                        var height = 0.11f + random.NextSingle() * 0.14f;

                        town.Children.Add(Piece(angle, width, height, foot));

                        if (random.Next(2) == 0)
                        {
                            var side = width * 0.60f;
                            var half = side * 0.7071f;
                            town.Children.Add(Piece(angle + (width / 2f - half) / perDegree, side, side, foot + height - half, 45f));
                        }

                        if (random.Next(3) == 0)
                            town.Children.Add(Piece(angle + (width - 0.10f) / perDegree, 0.05f, 0.08f, foot + height - 0.01f));

                        break;
                    }

                    case 3 or 4:
                    {
                        // Trees, two to four of them.
                        var trees = 2 + random.Next(3);
                        width = trees * 0.20f;

                        for (var i = 0; i < trees; i++)
                        {
                            var crown = 0.07f + random.NextSingle() * 0.05f;
                            var stem = 0.06f + random.NextSingle() * 0.08f;
                            var at = angle + (0.10f + i * 0.20f) / perDegree;

                            town.Children.Add(new MeshNode(Primitives.Cylinder(0.012f, 0.012f, stem + crown, 6), card)
                            {
                                Name = "skyline.trunk",
                                Position = Ring(at, radius, foot + (stem + crown) / 2f)
                            });

                            town.Children.Add(new MeshNode(Primitives.Sphere(crown, 12, 8), card)
                            {
                                Name = "skyline.crown",
                                Position = Ring(at, radius, foot + stem + crown * 0.85f)
                            });
                        }

                        break;
                    }

                    default:
                        // A gap of sky.
                        width = 0.15f + random.NextSingle() * 0.35f;
                        break;
                }
            }

            angle += width / perDegree;
        }

        return town;
    }

    /// <summary>
    /// The presenter's lectern, off to the side with a reading lamp on it: the one piece of furniture in
    /// the room that says somebody talks here, and the one warm light in a blackout. Stood on the blank
    /// side of the room at a hundred and twenty degrees, which no walk crosses, and turned to face the
    /// projector the way the presenter does.
    /// </summary>
    private static Node Lectern(Material brushed, Material moulded, Material stained)
    {
        const float angle = 120f;

        var lectern = new Node
        {
            Name = "lectern",
            Position = Ring(angle, 2.75f),
            RotationDegrees = new Vector3(0f, Yaw(angle), 0f)
        };

        lectern.Children.Add(new MeshNode(Primitives.Cylinder(0.22f, 0.25f, 0.03f, 32), moulded)
        {
            Name = "lectern.foot",
            Position = new Vector3(0f, 0.015f, 0f)
        });

        lectern.Children.Add(new MeshNode(Primitives.Cylinder(0.035f, 0.045f, 1.02f, 24), brushed)
        {
            Name = "lectern.post",
            Position = new Vector3(0f, 0.54f, 0f)
        });

        // The top, sloped down towards the presenter on the wall side, with a lip along the low edge so
        // the notes stay where they were put.
        var top = new Node
        {
            Name = "lectern.top",
            Position = new Vector3(0f, 1.10f, 0f),
            RotationDegrees = new Vector3(-14f, 0f, 0f)
        };

        top.Children.Add(Fabric.Slab(
            new Vector3(0.52f, 0.035f, 0.40f), Vector3.Zero, stained, "lectern.board", Finish.Close));

        top.Children.Add(Fabric.Slab(
            new Vector3(0.52f, 0.045f, 0.018f), new Vector3(0f, 0.012f, -0.191f), stained, "lectern.lip", Finish.Close));

        var paper = new Material
        {
            BaseColor = new Vector4(0.92f, 0.91f, 0.87f, 1f),
            Metallic = 0f,
            Roughness = 0.90f,
            Name = "paper"
        };

        top.Children.Add(Fabric.Slab(
            new Vector3(0.21f, 0.004f, 0.297f), new Vector3(0.03f, 0.0195f, -0.02f), paper, "lectern.notes"));

        // The lamp: an arm up from the high edge, a shade turned back towards the notes, and a bulb in
        // it that is unlit and warm, because a light here would be a fifth light in a room that has
        // spent its four.
        top.Children.Add(new MeshNode(Primitives.Cylinder(0.007f, 0.007f, 0.34f, 8), Fabric.DarkMetal)
        {
            Name = "lamp.arm",
            Position = new Vector3(-0.16f, 0.16f, 0.15f),
            RotationDegrees = new Vector3(-24f, 0f, 0f)
        });

        top.Children.Add(new MeshNode(Primitives.Cylinder(0.022f, 0.052f, 0.075f, 16, capped: false), Fabric.DarkMetal)
        {
            Name = "lamp.shade",
            Position = new Vector3(-0.16f, 0.33f, 0.07f),
            RotationDegrees = new Vector3(-58f, 0f, 0f)
        });

        top.Children.Add(new MeshNode(Primitives.Sphere(0.014f, 12, 8), Fabric.Emissive(1f, 0.84f, 0.60f))
        {
            Name = "lamp.bulb",
            Position = new Vector3(-0.16f, 0.315f, 0.085f)
        });

        lectern.Children.Add(top);

        return lectern;
    }

    /// <summary>Red and white, vertically, which is the only livery a popcorn cup has ever had.</summary>
    private static readonly Lazy<Material> Striped = new(() => new Material
    {
        BaseColor = Vector4.One,
        Roughness = 0.72f,
        Metallic = 0f,
        BaseColorTexture = Grain.Colour(128, "kiosk.stripe", (u, _) =>
        {
            var band = Grain.Cell(u, 1f / 12f);
            var red = Grain.Band(band, 0.06f, 0.5f, 0.03f);

            return Vector3.Lerp(new Vector3(0.88f, 0.86f, 0.82f), new Vector3(0.72f, 0.11f, 0.10f), red);
        }),
        Name = "stripe"
    });

    /// <summary>And the popcorn itself, which is off-white, matte and slightly translucent-looking because
    /// its own colour map is not flat.</summary>
    private static readonly Lazy<Material> Popped = new(() => new Material
    {
        BaseColor = new Vector4(0.92f, 0.88f, 0.74f, 1f),
        Roughness = 0.88f,
        Metallic = 0f,
        BaseColorTexture = Grain.Colour(64, "kiosk.popcorn", (u, v) =>
        {
            var burst = 0.80f + 0.30f * Grain.Fbm(u, v, 9, 47, 3);

            return new Vector3(burst, burst * 0.97f, burst * 0.86f);
        }),
        Name = "popcorn"
    });
}
