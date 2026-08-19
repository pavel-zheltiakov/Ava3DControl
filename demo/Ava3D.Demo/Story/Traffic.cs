using System.Numerics;
using Ava3D.Demo.Scenes.Contact;
using Ava3D.Demo.Textures;

namespace Ava3D.Demo.Story;

/// <summary>
/// The lane outside Relay Nine on the morning the ship leaves: five hulls going about their business and
/// two more shooting at each other a quarter of a kilometre off.
///
/// None of it exists until the ship is out of the station. The morning opens twenty metres inside a
/// docking bay — see <see cref="Illuminator.Leave"/> — and a lane of traffic seen through a wall is the
/// one thing that would give that away, so <see cref="Show"/> is driven off how far past the mouth the
/// window is rather than off a second of the chapter.
///
/// <b>It is the last thing the film builds and the cheapest.</b> Everything here is a ship out of
/// <c>Contact</c> — the same three models, the same plating map, the same exhaust cards — placed by hand
/// rather than flown. <see cref="Ship.Place"/> is what makes that possible and it is worth saying why it
/// matters: <c>Contact</c>'s flight model integrates, and a film that can be seeked into cannot contain
/// anything that does. A ship that can be <i>told</i> where it is instead of asked to fly there can be
/// dropped into a pure function of a clock, and so seven of them can.
///
/// <b>Traffic is a count, not a speed.</b> The instinct is to make the lane read busy by making things
/// move, and it is wrong twice over: relative motion between two ships in the same orbit is a few metres a
/// second, and a window full of things whipping past reads as a road. What makes a lane look worked is how
/// many hulls are in it and how many of them have their lamps on. So five ships drift, one crosses at a
/// pace you can see, and the rest of the impression is running lights.
///
/// <b>And the shooting is far away on purpose.</b> Three degrees of raider at two hundred and seventy-five
/// metres is a shape you cannot read, which is the point — the film has already shown a battle from inside
/// it, one chapter ago, in close-up and with a shot list. Showing a second one the same way would be
/// repeating the trick. Showing it as two specks and a lot of light, out of a window, from a freighter
/// that is leaving, is what the same event looks like to somebody it is not happening to.
/// </summary>
internal sealed class Traffic
{
    /// <summary>
    /// One ship in the lane: what it is, how far out, how high, and how fast it slides.
    ///
    /// <see cref="Drift"/> is metres a second along the ship's own axis, positive being aft — everything
    /// in the lane is nearly station-keeping with everything else, so the numbers are small and the
    /// difference between them is what makes one hull overtake another. <see cref="Phase"/> is where it
    /// is in its own run at the start of the chapter, so the lane is populated on the first frame rather
    /// than filling up over the first half-minute.
    /// </summary>
    private readonly record struct Lane(
        string File, string Prefix, float Length, float Out, float Up, float Drift, float Phase);

    /// <summary>
    /// The lane, read outward.
    ///
    /// Two freighters a long way off and high or low of the line of sight, three small ships closer in,
    /// and one of those three going the other way. The one that overtakes is the one that makes the rest
    /// of them read as moving at all: five hulls all sliding the same way is a pan, and one of them
    /// sliding against the others is traffic.
    /// </summary>
    private static readonly Lane[] Lanes =
    [
        new("kestrel.glb", "kestrel", 30f, 205f, -28f, 6.0f, 0.18f),
        new("kestrel.glb", "kestrel", 30f, 315f, 42f, 4.0f, 0.61f),
        new("harrier.glb", "harrier", 13f, 82f, 7f, 9.5f, 0.34f),
        new("harrier.glb", "harrier", 13f, 158f, -15f, 7.0f, 0.02f),
        new("harrier.glb", "harrier", 13f, 246f, 26f, -5.0f, 0.47f)
    ];

    /// <summary>How far along the ship's axis a lane runs before it comes round again, as a multiple of
    /// how far out it is. Two and a bit, which is about seventy degrees each side of the beam — the whole
    /// of what a window down one side of a hull can see.</summary>
    private const float Reach = 2.3f;

    /// <summary>How long a raider is, out here. Eighteen metres at two hundred and seventy-five is three
    /// and three quarter degrees, which is the same apparent size Relay Nine had on the way in — small
    /// enough to be a shape and big enough to be a ship.</summary>
    private const float DuelLength = 18f;

    /// <summary>
    /// Where the two of them are circling, and how wide the circle is.
    ///
    /// Fifty-eight degrees forward of the beam and two hundred and seventy-five metres off, which is the
    /// same place <see cref="Illuminator.Fight"/> aims at — the two numbers are the same fight and have to
    /// stay the same fight, so this is that bearing written out in the gallery's own coordinates.
    /// </summary>
    private static readonly Vector3 Ring = new(-237.8f, 32.8f, 150.2f);

    private const float RingRadius = 24f;

    /// <summary>How long they take to come round. Nine seconds, which does not divide into the chapter,
    /// so the fight never visibly repeats inside it.</summary>
    private const float RingPeriod = 9f;

    /// <summary>Where each of the two has its guns, in its own frame and in model units — the same
    /// offsets <c>ContactScene</c> gives the same two hulls, because they are the same two hulls.</summary>
    private static readonly Vector3[] HarrierGuns =
        [new(-0.10f, -0.010f, -0.24f), new(0.10f, -0.010f, -0.24f)];

    private static readonly Vector3[] RaiderGuns =
        [new(-0.285f, -0.038f, -0.14f), new(0.285f, -0.038f, -0.14f)];

    private readonly Node _lane = new() { Name = "lane" };
    private readonly Ship[] _hulls;
    private readonly Ship[] _duel;

    /// <summary>One per ship, so the two of them do not fire on the same beat.</summary>
    private readonly Material[] _muzzle = new Material[2];

    /// <summary>
    /// A path none of these ships ever flies. <see cref="Fleet.BuildShip"/> wants one because most ships
    /// in this demo follow a spline; every ship in here is placed by hand every frame, for the reason at
    /// the top of this file.
    /// </summary>
    private static readonly Vector3[] Idle =
        [Vector3.Zero, Vector3.UnitZ, Vector3.UnitZ * 2f, Vector3.UnitZ * 3f];

    public Traffic(Illuminator gallery)
    {
        gallery.Outside.Children.Add(_lane);

        var plating = Space.Plating();
        var glow = Space.Glow();
        var exhaust = (Space.Plume(), Space.PlumeCap());

        var friendly = new Vector3(0.42f, 1f, 0.62f);
        var hostile = new Vector3(1f, 0.26f, 0.20f);

        _hulls = new Ship[Lanes.Length];

        for (var i = 0; i < Lanes.Length; i++)
        {
            var lane = Lanes[i];

            _hulls[i] = Fleet.BuildShip(
                _lane, glow, plating, exhaust, lane.File, lane.Prefix, lane.Length, friendly,
                Idle, [0f, 0f, 0f, 0f], 0f);

            // The freighters carry what this ship has just finished carrying. Three containers on a hull
            // two hundred metres off is four hundred triangles and it is the whole of what says the lane
            // is a working one rather than a parade.
            if (lane.Prefix == "kestrel")
                Fleet.AddCargo(_hulls[i], plating, count: 3);
        }

        _duel =
        [
            Fleet.BuildShip(_lane, glow, plating, exhaust, "raider.glb", "raider", DuelLength, hostile,
                Idle, [0f, 0f, 0f, 0f], 0f, RaiderGuns),
            Fleet.BuildShip(_lane, glow, plating, exhaust, "harrier.glb", "harrier", DuelLength, friendly,
                Idle, [0f, 0f, 0f, 0f], 0f, HarrierGuns)
        ];

        Muzzles();

        // No contact markers on anything out of this window.
        //
        // A marker is for a hull you cannot find, and every ship out here is one the chapter is pointing
        // at: a freighter at two hundred metres with three containers on its back, two more having an
        // argument at two hundred and seventy-five. What the marker does at that range is hang a glow in
        // front of a ship you were already looking at — see Ship.Marked. The battle keeps them, because
        // in the battle half the fleet really is a pixel.
        foreach (var ship in _hulls.Concat(_duel))
        {
            ship.Marked = false;

            // <b>And every glowing surface on them comes back inside range.</b>
            //
            // The models are authored for the battle, and in the battle they are right: three kilometres
            // out a bridge window and a nav lamp are one pixel each, and the only way a pixel reads as a
            // light is if it clips — an emissive at three times over range is a point of pure colour,
            // which is exactly what a light at that distance is.
            //
            // Out of this window the same ship is two hundred metres away and forty of them across the
            // frame. A surface you can resolve does not read as bright when it clips, it reads as
            // <i>flat</i>: the kestrel's bridge glass is at two point eight and came out as a white
            // rectangle stuck to the hull with no glass in it at all, and the starboard nav lamp did the
            // same in cyan. That is the note about the window on the ship, and it is a scale problem
            // rather than a modelling one — so it is fixed here, where the scale is, and not in the file.
            //
            // <b>Down to under a third, and not merely to under one.</b> Stopping the clipping was not
            // enough and the measurement says why: out here the hull beside a lit window comes back at
            // twenty-four out of two hundred and fifty-five, because the only thing lighting it is a star
            // four kilometres away. An emissive at nine tenths against that is two hundred and twenty —
            // ten times its surroundings — and ten times its surroundings is a flat card whether or not it
            // technically clips. What a lit bridge looks like from two hundred metres is brighter than the
            // hull and still a surface.
            //
            // Only the lamps and the glass. An engine bell is a hole with a fire in it, it is meant to
            // blow out, and it is driven by Burn.
            foreach (var skin in ship.Skin)
            {
                if (skin.Name is not { } name || !(name.Contains(".lamp") || name.Contains(".glass")))
                    continue;

                var peak = MathF.Max(
                    skin.EmissiveColor.X, MathF.Max(skin.EmissiveColor.Y, skin.EmissiveColor.Z));

                if (peak > 0.28f)
                    skin.EmissiveColor *= 0.28f / peak;
            }

            // Remember, after, so the rest state the ship goes back to between manoeuvres is this one.
            ship.Remember();
        }

        // The gunfire, as a light.
        //
        // Range forty-six, which is the width of the circle the two of them are turning inside and
        // nothing at all beyond it. The gallery is two hundred and seventy-five metres away and is
        // therefore outside this light entirely; the only two objects it can ever reach are the two ships
        // doing the shooting, which is the whole point of it. A gun that goes off lights the ship it is
        // bolted to and the ship it is pointed at, and there is no other way to say that here — an
        // emissive surface puts colour on itself and nothing on the plating round it.
        Gunfire = new PointLight
        {
            // World, and every ship position in this file is room-local — the lane hangs off the
            // gallery's own node, sixty-two metres up the deck. A light is on the Scene and a node is
            // not, and forgetting that puts this one a room away from the thing it is lighting.
            Position = Deck.Illuminator + Ring,
            Color = new Vector3(1f, 0.80f, 0.46f),
            Intensity = 0f,
            Range = 46f,
            Decay = 2f
        };

        Show(0f);
    }

    /// <summary>The gunfire out at the ring, as a light rather than a billboard. See the constructor for
    /// why its range is smaller than its distance from the room.</summary>
    public PointLight Gunfire { get; }

    /// <summary>
    /// Whether there is a lane out there at all.
    ///
    /// Nought hides every hull in it and puts the floods out, which is what chapter 7 asserts: on the way
    /// in, the window has a planet, a station and one escort in it and nothing else, and a chapter that
    /// only ever <i>added</i> traffic would leave the run-in full of ships the moment anybody seeked past
    /// the morning and came back.
    /// </summary>
    public void Show(float level)
    {
        _lane.IsVisible = level > 0.01f;

        foreach (var ship in _hulls)
            ship.Beacon(level);

        foreach (var ship in _duel)
            ship.Beacon(level);

        // Asserted rather than left, like everything else across a chapter boundary: the light is out
        // whenever the lane is, so a seek back into chapter 7 cannot leave somebody else's gun burning.
        if (!_lane.IsVisible)
            Gunfire.Intensity = 0f;
    }

    /// <summary>
    /// Where everything in the lane is at <paramref name="clock"/> seconds into the morning.
    ///
    /// Every position here is arithmetic on the clock and nothing carries over from the frame before, so
    /// the whole lane can be jumped into at any second — which is the same claim the walk makes, made
    /// again for eight ships. The wrap is the only part worth reading twice: a ship that runs off one end
    /// of the lane comes back on the other, and because the run is a remainder rather than a counter, the
    /// answer at forty seconds does not depend on having been asked at thirty-nine.
    /// </summary>
    public void Move(float clock)
    {
        for (var i = 0; i < _hulls.Length; i++)
        {
            var lane = Lanes[i];
            var span = lane.Out * Reach;

            // Modulo into [-span, span). MathF.IEEERemainder would be shorter and would come back
            // negative half the time; this one is the remainder somebody actually wants.
            var run = 2f * span;
            var along = (lane.Phase * run + lane.Drift * clock) % run;

            if (along < 0f)
                along += run;

            var at = Place(along - span, lane.Up, lane.Out);

            // Nose along the drift. A yaw of minus ninety points the ship's own forward — which is minus
            // Z, as it is for everything that came out of the modeller — down positive X.
            var heading = Quaternion.CreateFromYawPitchRoll(
                (lane.Drift > 0f ? -90f : 90f) * MathF.PI / 180f, 0f, 0f);

            var forward = Vector3.Transform(-Vector3.UnitZ, heading);

            _hulls[i].Place(at, heading, forward, forward * MathF.Abs(lane.Drift));

            // Under power, but not hard. These are ships holding a lane, not ships going anywhere in a
            // hurry, and a full jet on all five would light the window up like a launch.
            _hulls[i].Burn(0.34f);
        }

        Duel(clock);
    }

    /// <summary>
    /// The two of them, turning inside each other.
    ///
    /// A circle each, half a period apart, so they are always across the ring from one another and always
    /// nose-on to where the other one is going. It is a dogfight described in two cosines, and at this
    /// range that is not a shortcut — two ships at three degrees, closing and opening, is every bit of
    /// the manoeuvre a window can carry.
    /// </summary>
    private void Duel(float clock)
    {
        var turn = clock / RingPeriod * MathF.Tau;

        for (var i = 0; i < _duel.Length; i++)
        {
            var angle = turn + i * MathF.PI;

            var at = Ring + new Vector3(
                MathF.Cos(angle) * RingRadius,
                MathF.Sin(angle * 0.5f) * RingRadius * 0.35f,
                MathF.Sin(angle) * RingRadius);

            // Tangent to the circle, which is where a ship turning inside one is pointed.
            var forward = Vector3.Normalize(new Vector3(-MathF.Sin(angle), 0.06f, MathF.Cos(angle)));

            var heading = Quaternion.CreateFromYawPitchRoll(
                MathF.Atan2(-forward.X, -forward.Z), 0f, 0.9f);

            _duel[i].Place(at, heading, forward, forward * 22f);
            _duel[i].Burn(1f);
        }

        // And the shooting.
        //
        // <b>It used to be three billboards and they were wrong.</b> Two of the three were placed a third
        // and two thirds of the way along the line between the ships — in open space, on nothing — and
        // the third sat exactly on a hull's centre. What that looks like is a soft round blob flying
        // alongside a ship it is not attached to, which is precisely what it was. A muzzle flash is not
        // a free-floating light: it is a cone of burning gas coming out of a barrel, and a barrel is a
        // place on a model.
        //
        // So the flare is geometry now, bolted to each ship's guns and turning with the hull — see
        // Muzzles — and the shooting itself is a light. Two ships firing on beats that do not divide into
        // each other, each lighting the other as it goes.
        var lead = 0f;

        for (var i = 0; i < _duel.Length; i++)
        {
            // A ninth power was the first try and it is what a muzzle flash actually is: on for a fiftieth
            // of a second and off for the rest. It is also invisible — flashes that sharp are dark in
            // almost every frame, so the fight read as two ships circling in silence. A fifth power is
            // the same shape with a tail on it, which is what a gun at a quarter of a kilometre looks
            // like once the eye has been given time to catch it.
            var beat = clock * (2.3f + i * 0.41f) + i * 0.37f;
            var lit = MathF.Pow(1f - (beat - MathF.Floor(beat)), 5f);

            Paint(_muzzle[i], new Vector3(1f, 0.84f, 0.46f), lit);

            if (lit <= lead)
                continue;

            // The light goes to whichever of them is firing hardest this frame, which is the closest a
            // single slot can get to two guns. At forty-eight metres apart on the ring both hulls are
            // inside it either way, so what changes with the choice is which of them is the brighter.
            lead = lit;
            Gunfire.Position = Deck.Illuminator + _duel[i].Gun(0);
        }

        Gunfire.Intensity = 26f * lead;
    }

    /// <summary>
    /// The muzzle flares: a short cone at every gun, parented to the ship that owns it.
    ///
    /// Four of them — two guns on each of the two ships — and each is a child of its ship's root node, so
    /// it inherits the pose and can never be anywhere but on the barrel. That is the entire fix for what
    /// they replaced, and it is the reason they are geometry and not billboards: a sprite has a position
    /// and no orientation, so a sprite standing in for a muzzle flash is a ball of light near a gun, while
    /// a cone has a direction and points where the ship points.
    ///
    /// A cylinder stands on its end unrotated, so a quarter turn back about X lays it along −Z, which is
    /// the way every hull in this demo faces. Wide at the front and narrow at the barrel, because that is
    /// which way the gas is going.
    /// </summary>
    private void Muzzles()
    {
        for (var i = 0; i < _duel.Length; i++)
        {
            _muzzle[i] = new Material
            {
                BaseColor = new Vector4(1f, 0.84f, 0.46f, 1f),
                EmissiveColor = new Vector3(1f, 0.84f, 0.46f),
                Unlit = true,
                Blend = BlendMode.Additive,
                DepthWrite = false,
                Name = "traffic.muzzle"
            };

            foreach (var gun in _duel[i].Guns)
                _duel[i].Root.Children.Add(new MeshNode(
                    Primitives.Cylinder(0.92f, 0.06f, 1.4f, 10), _muzzle[i])
                {
                    Position = gun * DuelLength + new Vector3(0f, 0f, -0.70f),
                    RotationDegrees = new Vector3(-90f, 0f, 0f),
                    RenderOrder = 3,
                    Name = "muzzle"
                });
        }
    }

    private static void Paint(Material material, Vector3 colour, float value)
    {
        var lit = colour * Math.Clamp(value, 0f, 1f);

        material.EmissiveColor = lit;
        material.BaseColor = new Vector4(lit, 1f);
    }

    /// <summary>
    /// The one thing out here that depends on where the camera ended up: the contact markers, and the
    /// roll of every exhaust card in the lane.
    ///
    /// The eye arrives in world coordinates and every hull out here is placed in the gallery's, so it is
    /// moved into theirs before anything is measured against it. Sixty-two metres of deck is not a
    /// rounding error at the range these ships are at, and a marker that fades with range has to be given
    /// the range.
    /// </summary>
    public void Watch(Vector3 eye)
    {
        var here = eye - Deck.Illuminator;

        foreach (var ship in _hulls)
            ship.Mark(here);

        foreach (var ship in _duel)
            ship.Mark(here);
    }

    /// <summary>A point in the lane, in the gallery's own frame: <paramref name="along"/> metres aft of
    /// the window's anchor, <paramref name="up"/> above the eye, <paramref name="outboard"/> outboard of
    /// the hull.</summary>
    private static Vector3 Place(float along, float up, float outboard) =>
        new(-6.1f + along, Deck.Eye + up, Illuminator.Bore + Illuminator.Hull + outboard);
}
