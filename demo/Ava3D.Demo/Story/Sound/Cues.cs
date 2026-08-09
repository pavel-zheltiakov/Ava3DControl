using Ava3D.Demo.Scenes.Arcade;

namespace Ava3D.Demo.Story;

/// <summary>
/// Everything the film can make a noise with, built once from <see cref="Tone"/>.
///
/// <b>The rule this bank is written under is the story's sixth</b> — nothing may say where this is until
/// the window says it. That rule was written for captions and it turns out to bind sound harder, because
/// sound gives a place away faster than any sentence: one hull creak, one engine throb under the floor,
/// one distant docking clamp, and the surprise nine minutes later is gone before the second chapter.
///
/// So the bank is in <b>two halves, and the second one is unusable for the first nine minutes</b>. Everything
/// down to <see cref="Servo"/> is a sound a large building at night also makes: air moving in a duct, a
/// contactor closing, a motor on a slow exhibit, machines running in a plant room. Not one of them is a
/// spacecraft. Everything from <see cref="Drive"/> onward is nothing else — a shaft the size of a house a
/// hundred metres aft, a metre of plate ticking as it comes round into the light, a gallery breathing over
/// its own glass — and none of it may be heard before chapter 7 opens the door. The rule stops applying at
/// the exact instant the illuminator says where this is, and what is on the other side of that instant is
/// the loudest argument the film has for it.
///
/// It is still not a new sound arriving over the old ones. What chapter 7 does is take the building away
/// and leave the ship, which was always underneath it: the plant fades, the air thins to a fifteenth, and
/// the whole second half of this bank fits into the hole that leaves at a tenth of the level the hole used
/// to be. Subtraction is what reveals it, and a floor with a ship in it is quieter than a plant room.
///
/// The bank is a set of properties rather than a method that builds on demand because a cue built during
/// the film is a cue built on the frame it is wanted, and generating four seconds of filtered noise takes
/// long enough to be a dropped frame. Building the lot costs a few milliseconds at the same moment the
/// building itself is put up, which is a moment the film has already reserved for being slow.
/// </summary>
internal sealed class Cues
{
    /// <summary>
    /// One turn of a corridor beacon, in seconds. <c>Corridor.Sweep</c> is 232 degrees a second.
    ///
    /// The alarm is one voice per turn rather than a loop, so this length is what makes consecutive firings
    /// tile without a gap or an overlap — and what locks the sound to the picture, since it is the same
    /// number the beacons are turned by. A klaxon on its own clock would drift against them within a
    /// chapter, and a drifting alarm is the one thing that would make the corridor look wrong.
    /// </summary>
    public const float Turn = 360f / 232f;

    private readonly int _rate;

    public Cues(int rate)
    {
        _rate = rate;

        Air = BuildAir();
        Ballast = BuildBallast();
        Machines = BuildMachines();
        Void = BuildVoid();

        Lamp = BuildLamp();
        Steps =
        [
            BuildStep(0x51F3A7, 196f, 0.052f),
            BuildStep(0x9C2E11, 231f, 0.061f),
            BuildStep(0x2A7D63, 174f, 0.046f)
        ];

        Motor = BuildMotor();
        Cabinet = [.. Voices.Select(BuildCabinet)];
        Kick = BuildKick();
        Tick = BuildTick();
        Klaxon = BuildKlaxon();
        Click = BuildClick();
        Latch = BuildLatch();
        Servo = BuildServo();

        // And the ship, which nothing before chapter 7 is allowed to touch.
        Drive = BuildDrive();
        Draught = BuildDraught();
        Projector = BuildProjector();

        Plates =
        [
            BuildPlate(0x3E71C4, 795f, 0.07f, 8200f),
            BuildPlate(0xA2185B, 612f, 0.11f, 6800f),
            BuildPlate(0x59CD02, 428f, 0.17f, 4400f),
            BuildPlate(0xD40E96, 291f, 0.27f, 2100f)
        ];

        Groans = [BuildGroan(0x7A2B4E, 47f, 3.4f, 17f), BuildGroan(0x18F5C9, 63f, 2.6f, 23f)];

        // And the morning, which is a ship inside somebody else's building.
        Berth = BuildBerth();
        Consoles = BuildConsoles();
        Clamp = BuildClamp();

        Pips =
        [
            BuildPip(0x4C1D77, 1480f, 0.055f),
            BuildPip(0x8A33E1, 1170f, 0.075f),
            BuildPip(0x21B95C, 2340f, 0.042f),
            BuildPip(0xF0742A, 880f, 0.095f)
        ];
    }

    /// <summary>The building breathing: air moving somewhere behind a wall. Under every chapter indoors,
    /// at a level the chapter chooses.</summary>
    public float[] Air { get; }

    /// <summary>Mains hum, added when the lamps are on. Not a sound the room makes — a sound the
    /// <i>lighting</i> makes, which is why the dark chapter does not have it and every lit one does.</summary>
    public float[] Ballast { get; }

    /// <summary>Three machines in a big room, two of them running. The engine room's bed.</summary>
    public float[] Machines { get; }

    /// <summary>The cut: no room, no air, and something very low that is not either. The only cue in the
    /// bank that is not a thing in the building.</summary>
    public float[] Void { get; }

    /// <summary>A lamp coming up. Three of them, a second and a half apart, is chapter 1.</summary>
    public float[] Lamp { get; }

    /// <summary>A footfall on a plated deck. Three, so a walk is not a machine gun, and each of them is two
    /// impacts — see <see cref="BuildStep"/>.</summary>
    public float[][] Steps { get; }

    /// <summary>The rotunda's arm, turning. Nine hundred nights of it.</summary>
    public float[] Motor { get; }

    /// <summary>
    /// The four televisions' voices: one bank a set, and inside a bank one clip per
    /// <see cref="Move"/> the game on it can make.
    ///
    /// Indexed <c>Cabinet[set][(int)move]</c>. Every set gets every move whether its game uses it or not,
    /// which costs a few hundred kilobytes of very short buffers and removes the only way this table can be
    /// wrong — a game that starts reporting a move nobody built a sound for.
    /// </summary>
    public float[][][] Cabinet { get; }

    /// <summary>The lounge's beat, on the beat. A hundred and twenty a minute.</summary>
    public float[] Kick { get; }

    /// <summary>The same beat's offbeat.</summary>
    public float[] Tick { get; }

    /// <summary>The alarm. Amber, not red — see <see cref="BuildKlaxon"/> for why that is a quiet sound.</summary>
    public float[] Klaxon { get; }

    /// <summary>A small part going into a socket.</summary>
    public float[] Click { get; }

    /// <summary>A larger one seating. What a board makes when it is pushed home.</summary>
    public float[] Latch { get; }

    /// <summary>A door's motor, and the stop at the end of it.</summary>
    public float[] Servo { get; }

    /// <summary>
    /// The ship's own drive, a hundred metres aft and through a metre of hull. The lowest thing in the film
    /// and the last thing in it to become audible.
    /// </summary>
    public float[] Drive { get; }

    /// <summary>The gallery washing its own glass. The one sound in the film that gets louder as the film
    /// gets quieter.</summary>
    public float[] Draught { get; }

    /// <summary>The plot table's projector: a small fan and a coil. The only machine in the last room.</summary>
    public float[] Projector { get; }

    /// <summary>The hull ticking, four ways. See <see cref="BuildPlate"/> for why a metre of steel with a
    /// star on one side of it cannot keep quiet.</summary>
    public float[][] Plates { get; }

    /// <summary>And the structure taking a load: the largest object in the film making the longest sound in
    /// it, at about the level of a tick.</summary>
    public float[][] Groans { get; }

    /// <summary>The inside of Relay Nine: a room seventy metres across with the ship's umbilical still on
    /// it. The only bed in the film that belongs to a building the film is not in.</summary>
    public float[] Berth { get; }

    /// <summary>The gallery's own consoles, awake for a departure. Nine minutes of night watch had them
    /// asleep; a working morning does not.</summary>
    public float[] Consoles { get; }

    /// <summary>A docking clamp letting go: the ram venting, then a hundred tonnes of latch dropping.</summary>
    public float[] Clamp { get; }

    /// <summary>Four console tones, so a row of screens is a row of screens and not one screen struck
    /// repeatedly.</summary>
    public float[][] Pips { get; }

    /// <summary>
    /// Four seconds of a duct, three rooms away.
    ///
    /// Two poles of low pass rather than one, because a single pole at this corner still passes enough
    /// above a kilohertz to read as hiss, and hiss is a tape rather than a building. What is left is
    /// almost all under two hundred hertz, which is what air in a duct actually is once it has been through
    /// a wall.
    /// </summary>
    private float[] BuildAir()
    {
        // Half a second longer than the loop, because the seam eats that much.
        var bed = Tone.Noise((int)(4.5f * _rate), 0xA10D1E)
            .Low(_rate, 300f)
            .Low(_rate, 220f)
            .Centre()
            .Peak(0.7f)
            .Seam(_rate, 0.5f);

        // The tonal part goes on after the length is settled, so both of these close exactly. Fifty and a
        // hundred: the frequency the building's own power is at, and its octave.
        return bed
            .Sine(_rate, Tone.Locked(49f, _rate, bed.Length), 0.18f)
            .Sine(_rate, Tone.Locked(98f, _rate, bed.Length), 0.06f)
            .Wander(_rate, 0.09f, 0.14f)
            .Peak(0.85f);
    }

    /// <summary>Two seconds of lit fluorescent-style ballast: a hundred hertz and its odd harmonics, which
    /// is what a rectified fifty sounds like.</summary>
    private float[] BuildBallast()
    {
        var length = 2 * _rate;
        var hum = new float[length];

        return hum
            .Sine(_rate, Tone.Locked(100f, _rate, length), 0.5f)
            .Sine(_rate, Tone.Locked(300f, _rate, length), 0.12f)
            .Sine(_rate, Tone.Locked(500f, _rate, length), 0.05f)
            .Peak(0.6f);
    }

    /// <summary>
    /// The plant room: a broad rumble with three machines in it.
    ///
    /// Two of the tones are close together on purpose — forty-one and forty-three hertz beat against each
    /// other twice a second, which is the sound of two large things running at nearly the same speed and is
    /// completely different from one thing running. The middle machine is out, and what says so is that
    /// there is no third rate in here.
    /// </summary>
    private float[] BuildMachines()
    {
        var bed = Tone.Noise((int)(4.6f * _rate), 0x3B77C2)
            .Low(_rate, 190f)
            .Low(_rate, 150f)
            .Centre()
            .Peak(0.55f)
            .Seam(_rate, 0.6f);

        return bed
            .Sine(_rate, Tone.Locked(41f, _rate, bed.Length), 0.4f)
            .Sine(_rate, Tone.Locked(43f, _rate, bed.Length), 0.34f)
            .Sine(_rate, Tone.Locked(86f, _rate, bed.Length), 0.12f)
            .Sine(_rate, Tone.Locked(129f, _rate, bed.Length), 0.05f)
            .Wander(_rate, 0.13f, 0.1f)
            .Peak(0.9f);
    }

    /// <summary>Six seconds of nothing in particular, very low. The cut is not in a room and should not
    /// sound like one.</summary>
    private float[] BuildVoid()
    {
        var bed = Tone.Noise((int)(6.8f * _rate), 0x77AA31)
            .Low(_rate, 120f)
            .Low(_rate, 70f)
            .Centre()
            .Peak(0.4f)
            .Seam(_rate, 0.8f);

        return bed
            .Sine(_rate, Tone.Locked(32f, _rate, bed.Length), 0.5f)
            .Sine(_rate, Tone.Locked(48f, _rate, bed.Length), 0.22f)
            .Sine(_rate, Tone.Locked(64.5f, _rate, bed.Length), 0.1f)
            .Wander(_rate, 0.07f, 0.3f)
            .Peak(0.8f);
    }

    /// <summary>
    /// A lamp coming up: a strike, a charge sliding up two octaves, and the light arriving.
    ///
    /// <b>This was a relay and it sounded like a footstep.</b> The old one was a clack, a body and an
    /// armature bouncing twenty-two milliseconds later at a quarter of the level — and two uneven impacts
    /// close together is exactly, precisely what makes a footstep a footstep. <see cref="BuildStep"/> says
    /// so in its own summary and cites the relay as where it learned the trick. Three of them go off in the
    /// first ten seconds of the film, in a dark room, while a man is standing still: what the audience heard
    /// was somebody walking about behind them.
    ///
    /// So there is exactly one impact in here, and it is twelve milliseconds of high noise — the strike, and
    /// the whole of the mechanical part. Everything after it is tonal and rising, which is the difference
    /// that matters: a step is percussive then percussive, and a thing being energised is percussive then
    /// <i>pitched</i>. Nothing else in the film does that, so it is recognisable the second time it happens,
    /// which is nine seconds later.
    ///
    /// The charge slides two octaves in a fifth of a second and the bloom lands on the note it arrives at,
    /// with a fifth above it and a detuned twin four hertz off for the beat. The ballast underneath is a
    /// low tone that catches and settles: it is not a hum loop, because these lamps are on for the next nine
    /// minutes and a room tone is the ballast's job rather than this cue's.
    /// </summary>
    private float[] BuildLamp()
    {
        var strike = Tone.Noise((int)(0.012f * _rate), 0x3FA9D1)
            .Band(_rate, 2600f, 11000f)
            .Fall(_rate, 0.006f);

        // Bent below one, so most of the climb happens early and the last of it eases into the note the
        // bloom is waiting on. A straight line in hertz already decelerates by ear, and this leans on that.
        var charge = new float[(int)(0.20f * _rate)]
            .Glide(_rate, 220f, 880f, 0.5f, 0.6f)
            .Fall(_rate, 0.13f, 0.010f);

        var bloom = new float[(int)(0.45f * _rate)]
            .Sine(_rate, 880f, 0.5f)
            .Sine(_rate, 884f, 0.32f)
            .Sine(_rate, 1320f, 0.15f)
            .Sine(_rate, 2640f, 0.05f)
            .Fall(_rate, 0.17f, 0.030f);

        var ballast = new float[(int)(0.35f * _rate)]
            .Sine(_rate, 96f, 1f)
            .Sine(_rate, 192f, 0.22f)
            .Fall(_rate, 0.15f, 0.020f);

        return new float[(int)(0.62f * _rate)]
            .Mix(strike, _rate, 0f, 0.5f)
            .Mix(charge, _rate, 0.004f, 0.7f)
            .Mix(bloom, _rate, 0.145f, 0.8f)
            .Mix(ballast, _rate, 0.02f, 0.45f)
            .Peak(0.85f);
    }

    /// <summary>
    /// A boot on a plated deck, and it is <b>two</b> impacts rather than one.
    ///
    /// That is the whole of what makes a footstep a footstep. A heel lands, and forty to sixty milliseconds
    /// later the rest of the foot arrives — softer, duller, and never at the same interval twice. One impact
    /// is a knock: a thing hitting a thing, which is the sound this used to make. Two, close together and
    /// uneven, is a person.
    ///
    /// It is also why <see cref="BuildLamp"/> has exactly one impact in it. The lamp cue used to be a relay
    /// built the same way — a clack and an armature bouncing after it — and three of those going off in a
    /// dark room read as somebody walking about behind you. Two impacts is a footstep whatever the thing
    /// making them is supposed to be, and no amount of choosing different frequencies gets around it.
    ///
    /// <b>The transient is short.</b> The first version decayed its impact over forty-five milliseconds,
    /// which is four times as long as anything hard hitting steel takes, and forty-five milliseconds of
    /// filtered noise is not a knock but a syllable — the step arrived as a burst of hiss rather than as a
    /// heel. The heel here is over in nine, with a fade-in short enough not to eat it: <see cref="Tone.Fall"/>
    /// defaults to two milliseconds of attack, which on a nine-millisecond decay would throw away two thirds
    /// of the peak.
    ///
    /// <b>The deck rings where the building does not.</b> The plate's modes used to be a single sine near a
    /// hundred hertz, which is precisely where the mains hum in <see cref="BuildBallast"/> and the octave in
    /// <see cref="BuildAir"/> both sit — so the one part of the step that says <i>floor</i> was buried under
    /// the one part of the room that never stops. It is a couple of hundred hertz now, inharmonic, three
    /// modes dying at three rates, with the low body kept below the hum rather than on it.
    ///
    /// The three variants differ in all three of those numbers and not only in their noise, because that is
    /// what makes them three feet on one floor rather than one sample played three ways.
    /// </summary>
    /// <param name="seed">The noise. Different per variant, so no two are the same shhh.</param>
    /// <param name="plate">The deck's lowest mode under this foot.</param>
    /// <param name="roll">Seconds from the heel landing to the rest of the foot.</param>
    private float[] BuildStep(uint seed, float plate, float roll)
    {
        // The heel. Hard, wide and gone.
        //
        // Two poles down rather than one, for the reason BuildAir gives: six decibels an octave from a
        // single pole still passes most of what is above the corner, and a boot measured with one had a
        // quarter of its power over five kilohertz. That is not a boot, it is a spark — the top end of an
        // impact is where a generated sound gives itself away, and steel is bright, not fizzy.
        var heel = Tone.Noise((int)(0.05f * _rate), seed)
            .Band(_rate, 260f, 4600f)
            .Low(_rate, 4600f)
            .Fall(_rate, 0.009f, 0.0008f);

        // The deck answering: two modes a little over half an octave apart, which beat rather than blend.
        var ring = new float[(int)(0.24f * _rate)]
            .Sine(_rate, plate, 1f)
            .Sine(_rate, plate * 1.58f, 0.45f)
            .Fall(_rate, 0.07f, 0.001f);

        // And its top mode, which is the metal in it. Short — a plate's high modes die first.
        var tang = new float[(int)(0.09f * _rate)]
            .Sine(_rate, plate * 2.71f, 1f)
            .Fall(_rate, 0.018f, 0.001f);

        // The weight going through the floor. Under the building's own hum, not on it — and given long
        // enough to be a note rather than a bump: sixty-eight hertz is a cycle every fifteen milliseconds,
        // so a fifty-millisecond decay is three cycles and most of them already faded. That measured as two
        // per cent of the step's power below eighty hertz, which is a boot with nothing underneath it.
        var body = new float[(int)(0.22f * _rate)].Sine(_rate, 68f, 1f).Sine(_rate, 102f, 0.3f).Fall(_rate, 0.075f);

        // The rest of the foot, a heel-to-toe later: the same event with the hard edge taken off it.
        //
        // It is mixed at more than the heel is, which looks wrong and is not. A twelve-hundred-hertz band
        // of noise carries a fraction of the amplitude of a four-kilohertz one, so the two numbers are not
        // comparable; what arrives is a bump well under a fifth of the heel's height, which is where a
        // second impact has to sit to be heard as part of the first sound rather than as another sound.
        var toe = Tone.Noise((int)(0.07f * _rate), seed ^ 0x5A5A5A)
            .Band(_rate, 170f, 1400f)
            .Fall(_rate, 0.022f, 0.001f);

        // The sole letting go of the plate on the way off. Two poles again, and quieter than the heel by a
        // long way: it is the part everybody hears and nobody notices.
        var scuff = Tone.Noise((int)(0.05f * _rate), seed ^ 0xFFFF)
            .Band(_rate, 2400f, 6800f)
            .Low(_rate, 6800f)
            .Fall(_rate, 0.015f);

        return new float[(int)(0.34f * _rate)]
            .Mix(heel, _rate, 0f)
            .Mix(body, _rate, 0f, 0.5f)
            .Mix(ring, _rate, 0f, 0.28f)
            .Mix(tang, _rate, 0.001f, 0.11f)
            .Mix(toe, _rate, roll, 1.15f)
            .Mix(scuff, _rate, roll + 0.03f, 0.14f)
            .Peak(0.8f);
    }

    /// <summary>The exhibit arm: a small geared motor and its bearing, three seconds of it, looped.</summary>
    private float[] BuildMotor()
    {
        var bearing = Tone.Noise((int)(3.4f * _rate), 0xC0FFEE)
            .Band(_rate, 900f, 3400f)
            .Peak(0.16f)
            .Seam(_rate, 0.4f);

        return bearing
            .Sine(_rate, Tone.Locked(46f, _rate, bearing.Length), 0.5f)
            .Sine(_rate, Tone.Locked(92f, _rate, bearing.Length), 0.22f)
            .Sine(_rate, Tone.Locked(138f, _rate, bearing.Length), 0.09f)
            .Wander(_rate, 0.55f, 0.18f)
            .Peak(0.7f);
    }

    /// <summary>
    /// How high each set's cabinet talks, and it is the whole of what makes four televisions four
    /// televisions.
    ///
    /// Four screens in one dark room, all of them one-bit hardware, will blur into one machine unless
    /// something separates them — and pitch separates them better than timbre does, because the ear will
    /// follow a voice up and down an octave and file it as the same voice. The order is the order the sets
    /// wake in: the platformer at concert pitch, the maze a fourth above it, the night run most of an octave
    /// below, and the well between them. So the room fills in from the middle outwards as he walks along the
    /// bench, and by the time he sits down the four are a chord rather than a noise.
    /// </summary>
    private static readonly float[] Voices = [1f, 1.34f, 0.62f, 0.83f];

    /// <summary>
    /// One television's whole vocabulary.
    ///
    /// Everything a screen in this room says is a square wave going somewhere, for the reason
    /// <see cref="BuildBlip"/> gives: one bit of output, on or off, is what the hardware these are pretending
    /// to be could actually make, and the room exists to argue that a picture drawn a texel at a time is worth
    /// looking at. A sampled orchestra hit over the top of it would give the whole thing away.
    ///
    /// The shapes are the genre's own and they are older than any of the games here. Up for leaving the
    /// ground and down for arriving. A pickup is two notes a fifth apart in forty milliseconds, which is the
    /// only sound in this bank that a listener will name before it has finished. A row going is a run up the
    /// scale, because clearing four at once has to be the best thing the screen has said all evening.
    /// </summary>
    /// <param name="voice">How much to multiply every frequency in the bank by. See <see cref="Voices"/>.</param>
    private float[][] BuildCabinet(float voice)
    {
        var bank = new float[Enum.GetValues<Move>().Length][];

        bank[(int)Move.Jump] = BuildBlip(0.13f, 300f * voice, 900f * voice);
        bank[(int)Move.Land] = BuildBlip(0.07f, 420f * voice, 170f * voice);
        bank[(int)Move.Duck] = BuildBlip(0.12f, 820f * voice, 240f * voice);
        bank[(int)Move.Coin] = BuildNotes(voice, 0.045f, (880f, 1f), (1320f, 1f));
        bank[(int)Move.Near] = BuildNotes(voice, 0.06f, (740f, 1f), (620f, 1f), (740f, 1f));
        bank[(int)Move.Drop] = BuildBlip(0.075f, 240f * voice, 130f * voice);
        bank[(int)Move.Clear] = BuildNotes(voice, 0.055f, (523f, 1f), (659f, 1f), (784f, 1f), (1047f, 1.6f));

        return bank;
    }

    /// <summary>
    /// A run of square-wave notes, one after another, each the same length.
    ///
    /// The last note's length is multiplied by its own factor, which sounds like a detail and is the
    /// difference between a run and a tune: a sequence of four equal notes stops, and a sequence whose last
    /// note is held arrives. Every arcade jingle ever written does this.
    /// </summary>
    /// <param name="voice">The bank's pitch multiplier.</param>
    /// <param name="each">Seconds a note.</param>
    /// <param name="notes">Hertz, and how many <paramref name="each"/> this note lasts.</param>
    private float[] BuildNotes(float voice, float each, params (float Hz, float Long)[] notes)
    {
        var total = 0f;

        foreach (var (_, length) in notes)
            total += each * length;

        var run = new float[(int)(total * _rate) + 1];
        var at = 0f;

        foreach (var (hz, length) in notes)
        {
            var span = each * length;
            var note = new float[(int)(span * _rate)];
            var step = 2f * MathF.PI * hz * voice / _rate;

            for (var i = 0; i < note.Length; i++)
                note[i] = MathF.Sign(MathF.Sin(i * step)) * 0.3f;

            // Shaped per note rather than over the run, so a held last note does not arrive quieter than the
            // ones before it. The attack is short — these are meant to be typed out, not swelled into.
            run.Mix(note.Low(_rate, 3200f).Fall(_rate, span * 0.8f, 0.003f), _rate, at);
            at += span;
        }

        return run.Low(_rate, 4200f).Peak(0.6f);
    }

    /// <summary>
    /// A cabinet's simplest utterance: a square wave going somewhere, over in a sixth of a second.
    ///
    /// Square rather than a sine because that is the shape the hardware these are pretending to be could
    /// actually make — one bit of output, on or off — and the demonstration in the lounge is of sprites
    /// drawn the way those machines drew them. A sine here would be a modern sound coming out of an old
    /// cabinet.
    /// </summary>
    private float[] BuildBlip(float seconds, float from, float to)
    {
        var count = (int)(seconds * _rate);
        var blip = new float[count];
        var phase = 0f;

        for (var i = 0; i < count; i++)
        {
            var u = (float)i / count;
            phase += 2f * MathF.PI * (from + (to - from) * u) / _rate;
            blip[i] = MathF.Sign(MathF.Sin(phase)) * 0.3f;
        }

        // Filtered, then shaped. An unfiltered square is all the odd harmonics at once and above about
        // four kilohertz they are just harshness — these should sound like a small speaker in a wooden box.
        return blip.Low(_rate, 3200f).Fall(_rate, seconds * 0.55f, 0.004f).Peak(0.65f);
    }

    /// <summary>The beat, on the beat: a low thump with a knock on the front of it.</summary>
    private float[] BuildKick()
    {
        var body = new float[(int)(0.26f * _rate)].Sine(_rate, 62f, 1f).Sine(_rate, 93f, 0.25f).Fall(_rate, 0.08f);
        var knock = Tone.Noise((int)(0.02f * _rate), 0x4411AA).Low(_rate, 900f).Fall(_rate, 0.006f);

        return new float[(int)(0.28f * _rate)]
            .Mix(body, _rate, 0f)
            .Mix(knock, _rate, 0f, 0.35f)
            .Peak(0.8f);
    }

    /// <summary>And its offbeat, which is nothing but air.</summary>
    private float[] BuildTick() =>
        Tone.Noise((int)(0.05f * _rate), 0x8899AB).Band(_rate, 3000f, 9000f).Fall(_rate, 0.016f).Peak(0.5f);

    /// <summary>
    /// The alarm, and it is a quiet one on purpose.
    ///
    /// Amber, the caption says, and not a drill and not a fire either — and the chapter after it says the
    /// others will sleep through it and are right to. A klaxon contradicts all three. What a building
    /// sounds when something needs a person but nothing is on fire is two low notes, the second below the
    /// first, repeated patiently for as long as it takes; the falling interval is the part that reads as
    /// an instruction rather than as an emergency.
    ///
    /// The clip is exactly one turn of a beacon long, so firing one per turn tiles it seamlessly and pins
    /// it to the light.
    /// </summary>
    private float[] BuildKlaxon()
    {
        var high = new float[(int)(0.34f * _rate)]
            .Sine(_rate, 311f, 1f).Sine(_rate, 313.5f, 0.7f).Sine(_rate, 622f, 0.12f)
            .Fall(_rate, 0.16f, 0.012f);

        var low = new float[(int)(0.42f * _rate)]
            .Sine(_rate, 233f, 1f).Sine(_rate, 234.8f, 0.7f).Sine(_rate, 466f, 0.12f)
            .Fall(_rate, 0.2f, 0.012f);

        return new float[(int)(Turn * _rate)]
            .Mix(high, _rate, 0f)
            .Mix(low, _rate, 0.3f, 0.9f)
            .Peak(0.7f);
    }

    /// <summary>A small part meeting its socket.</summary>
    private float[] BuildClick()
    {
        var tap = Tone.Noise((int)(0.04f * _rate), 0x6D2C90).Band(_rate, 1400f, 7000f).Fall(_rate, 0.009f);
        var ring = new float[(int)(0.1f * _rate)].Sine(_rate, 1180f, 1f).Fall(_rate, 0.022f);

        return new float[(int)(0.12f * _rate)]
            .Mix(tap, _rate, 0f)
            .Mix(ring, _rate, 0f, 0.3f)
            .Peak(0.55f);
    }

    /// <summary>Four hundred and fifty-eight millimetres of board going home. Heavier than a click, and it
    /// has the bench under it.</summary>
    private float[] BuildLatch()
    {
        var seat = Tone.Noise((int)(0.09f * _rate), 0xBEEF12).Band(_rate, 320f, 4200f).Fall(_rate, 0.026f);
        var bench = new float[(int)(0.22f * _rate)].Sine(_rate, 164f, 1f).Sine(_rate, 246f, 0.35f).Fall(_rate, 0.07f);

        return new float[(int)(0.26f * _rate)]
            .Mix(seat, _rate, 0f)
            .Mix(bench, _rate, 0.004f, 0.5f)
            .Peak(0.75f);
    }

    /// <summary>
    /// A door: a motor for as long as the door takes, and then the stop.
    ///
    /// The stop is the part that matters. A door that fades out has not finished; a door that arrives has,
    /// and everybody knows the difference without being able to say why.
    ///
    /// Which is why the length is <see cref="Door.Opening"/> and not a number of its own. This used to run for
    /// one and a sixth seconds against doors that took between two and two fifths and three — so the sound
    /// arrived, and then the picture went on opening the door in silence for another second and a half. The
    /// one part of the cue that carries all of its meaning was landing in the middle of the travel.
    /// </summary>
    private float[] BuildServo()
    {
        var travel = Door.Opening;
        var length = (int)((travel + 0.25f) * _rate);
        var run = new float[length];

        // The motor is built with its own envelope rather than Fall's, because it comes up, holds, and is
        // cut off by the stop — which is not a shape an exponential decay can make.
        for (var i = 0; i < length; i++)
        {
            var t = (float)i / _rate;
            var open = MathF.Min(1f, t * 14f) * MathF.Min(1f, MathF.Max(0f, (travel - t) * 10f));
            run[i] = MathF.Sin(2f * MathF.PI * 137f * t) * 0.5f * open
                     + MathF.Sin(2f * MathF.PI * 274f * t) * 0.12f * open;
        }

        var slide = Tone.Noise(length, 0x2E5C81).Band(_rate, 600f, 2600f).At(0.5f);

        for (var i = 0; i < length; i++)
        {
            var t = (float)i / _rate;
            slide[i] *= MathF.Min(1f, t * 10f) * MathF.Min(1f, MathF.Max(0f, (travel - t) * 10f));
        }

        var stop = Tone.Noise((int)(0.12f * _rate), 0x0FA37B).Band(_rate, 180f, 2000f).Fall(_rate, 0.035f);
        var thud = new float[(int)(0.28f * _rate)].Sine(_rate, 88f, 1f).Sine(_rate, 132f, 0.3f).Fall(_rate, 0.09f);

        return run
            .Mix(slide, _rate, 0f, 0.35f)
            .Mix(stop, _rate, travel - 0.02f, 0.9f)
            .Mix(thud, _rate, travel - 0.02f, 0.6f)
            .Peak(0.8f);
    }

    // ---- and the ship, which is chapter 7 onward and nowhere before it ---------------------------------

    /// <summary>
    /// What has been under the whole film and could not be admitted: a drive the size of a house, a hundred
    /// metres aft, heard through a metre of hull rather than through any air.
    ///
    /// <b>It is one thing running and it must not beat.</b> The plant room's rumble is built out of forty-one
    /// and forty-three hertz precisely so that it throbs twice a second, because that is two machines at
    /// nearly the same speed and is the sound of a room with several things in it. A ship has one drive. So
    /// there is a single fundamental with its own harmonics on top, and what keeps it from being a test tone
    /// is a breath nine seconds long — which is as slow as a wander can be, since <see cref="Tone.Wander"/>
    /// closes its cycle on the loop and the loop is nine seconds. That is why this is the longest buffer in
    /// the bank: the length is set by how slowly it has to move, not by how long it has to last.
    ///
    /// <b>Twenty-seven hertz is inaudible on most of what this will be played through</b>, and the harmonics
    /// are the answer rather than a compromise. A laptop speaker rolls off two octaves above the fundamental,
    /// so a drive written as a sub alone is a drive that does not exist on the machine most people will watch
    /// this on; fifty-four and eighty-one are what carry it there, and the twenty-seven is what anybody with
    /// a real woofer feels underneath them. Nothing else in the bank has anything at all below thirty-two.
    /// </summary>
    private float[] BuildDrive()
    {
        // Two poles at seventy: no top end whatever. Anything above a couple of hundred hertz has a metre of
        // plate and a hundred metres of ship between it and this room, and none of it arrives.
        var bed = Tone.Noise((int)(9.6f * _rate), 0x5D1A0F)
            .Low(_rate, 120f)
            .Low(_rate, 70f)
            .Centre()
            .Peak(0.4f)
            .Seam(_rate, 0.8f);

        return bed
            .Sine(_rate, Tone.Locked(27f, _rate, bed.Length), 0.55f)
            .Sine(_rate, Tone.Locked(54f, _rate, bed.Length), 0.28f)
            .Sine(_rate, Tone.Locked(81f, _rate, bed.Length), 0.10f)
            .Wander(_rate, 0.11f, 0.13f)
            .Peak(0.85f);
    }

    /// <summary>
    /// The gallery breathing over its own glass, and it is the one voice in the film whose level goes
    /// <i>up</i> as everything else goes down.
    ///
    /// It is a real fitting and not an effect. A pane with vacuum on one side of it and a warm room on the
    /// other is the coldest surface on board, so something has to wash it or the last room in the building
    /// spends the voyage fogged; aircraft do exactly this and it is audible from the seat. What that gives
    /// the chapter is a gradient instead of a cue — nothing arrives, but standing at the glass is measurably
    /// different from standing by the door, and the difference is the only reward in the film for walking
    /// the last three metres.
    ///
    /// No tone in it at all. It is moving air and nothing else, and it wanders half its own depth over four
    /// and a half seconds, which is what a vent that is not quite steady does.
    /// </summary>
    private float[] BuildDraught()
    {
        var bed = Tone.Noise((int)(5.2f * _rate), 0x2C9BE4)
            .Band(_rate, 420f, 2300f)
            .Low(_rate, 2300f)
            .Centre()
            .Peak(0.6f)
            .Seam(_rate, 0.6f);

        return bed.Wander(_rate, 0.22f, 0.45f).Peak(0.8f);
    }

    /// <summary>
    /// The plot table's projector: a small fan, its blades, and a coil.
    ///
    /// Every tone in it is a multiple of the fan's own hundred and thirty-two hertz except one, and the
    /// exception is the point. Four blades on a shaft turning at that rate put their blade-pass tone at four
    /// times it and the rest of the series above that; a switching supply is not connected to the shaft and
    /// whines wherever its own inductors happen to ring, which is why the twenty-nine hundred sits at no
    /// musical interval from anything else in the sound. That single unrelated tone is the whole of what
    /// makes this read as electronics rather than as a machine, and it is the only thing above two kilohertz
    /// anywhere in the last two minutes of the film.
    ///
    /// It is positional and it is the last machine on board. He is nearest it a metre and a half away, in the
    /// middle of the room at half past the chapter; by the last bay he is nine and a half metres off and it
    /// is down twenty-three decibels. So it is the subtraction the chapter is built on, done once as a
    /// gradient rather than as a fade — nothing is switched off, he simply walks away from the last thing on
    /// board that was still running.
    /// </summary>
    private float[] BuildProjector()
    {
        var bed = Tone.Noise((int)(2.9f * _rate), 0x71E4C3)
            .Band(_rate, 700f, 4200f)
            .Centre()
            .Peak(0.12f)
            .Seam(_rate, 0.35f);

        return bed
            .Sine(_rate, Tone.Locked(132f, _rate, bed.Length), 0.30f)
            .Sine(_rate, Tone.Locked(528f, _rate, bed.Length), 0.09f)
            .Sine(_rate, Tone.Locked(2900f, _rate, bed.Length), 0.05f)
            .Wander(_rate, 0.4f, 0.09f)
            .Peak(0.7f);
    }

    /// <summary>
    /// A metre of hull letting go of itself, once.
    ///
    /// <b>It is the most defensible sound in the film and the one nobody expects to be told why.</b> The star
    /// is ninety-seven degrees off the beam and the ship is turning under it; the plate on the lit side is a
    /// hundred kelvin warmer than the plate in shadow, and a metre of steel does not cross that gradient
    /// smoothly. It grips, it loads, and it slips — and what slips is a plate fifteen metres long, so what
    /// arrives in the room is a crack with the whole panel ringing behind it. Nothing else in this building
    /// could make this noise. It is the reason a spacecraft interior sounds like a spacecraft interior and
    /// not like a corridor, and it costs four short buffers.
    ///
    /// Three parts, and they are the same three the relay and the footfall are built from because that is
    /// what an impact is: the release, the panel's own modes, and the mass behind it. What separates the four
    /// variants is which panel let go — a nearer, smaller plate rings higher and dies sooner, a further one
    /// arrives with its top end already taken off it by the fifteen metres of room in between. So the same
    /// three numbers do the pitch, the length <i>and</i> the distance, and the score picks a pane for each
    /// one to have come from.
    /// </summary>
    /// <param name="seed">The crack's noise.</param>
    /// <param name="mode">The plate's lowest ringing mode.</param>
    /// <param name="ring">How long it rings for.</param>
    /// <param name="dull">Where the top of it is, which is how far away it reads as being.</param>
    private float[] BuildPlate(uint seed, float mode, float ring, float dull)
    {
        // Three and a half milliseconds. A tick is the shortest event in the bank by a factor of three —
        // steel releasing is not a hit but a snap, and anything with a body to it reads as somebody knocking.
        var crack = Tone.Noise((int)(0.02f * _rate), seed)
            .Band(_rate, 500f, 6500f)
            .Fall(_rate, 0.0035f, 0.0004f);

        // The panel. Three modes, none of them a whole multiple of another — a rectangle of plate clamped on
        // four edges has an inharmonic series, and it is the inharmonicity that says metal rather than string.
        var panel = new float[(int)(0.5f * _rate)]
            .Sine(_rate, mode, 1f)
            .Sine(_rate, mode * 1.47f, 0.5f)
            .Sine(_rate, mode * 2.31f, 0.18f)
            .Fall(_rate, ring, 0.0008f);

        // And what is behind it, which is the rest of the ship and rings for a good deal longer than the
        // plate does.
        var mass = new float[(int)(0.62f * _rate)]
            .Sine(_rate, mode * 0.29f, 1f)
            .Fall(_rate, ring * 2.4f, 0.002f);

        return new float[(int)(0.7f * _rate)]
            .Mix(crack, _rate, 0f)
            .Mix(panel, _rate, 0f, 0.42f)
            .Mix(mass, _rate, 0.001f, 0.22f)
            .Low(_rate, dull)
            .Peak(0.75f);
    }

    /// <summary>
    /// The structure taking a load: fifteen metres of hull moving against what it is bolted to, over three
    /// seconds.
    ///
    /// <b>A creak is not a note, it is a rate that slows down.</b> Two surfaces under load do not slide —
    /// they stick, release, and stick again, hundreds of times a second, and the rate of it falls through the
    /// event as the load comes off. That falling rate is the whole sound: the identical tone with a constant
    /// chop on it is a tremolo, which is a musical effect, and the ear files it as one immediately. Seventeen
    /// hertz dropping to nine over three seconds is a door in an old house, and a hull is the same physics
    /// with more steel in it.
    ///
    /// Which is why this one is a loop written by hand rather than a stack of <see cref="Tone"/> calls. The
    /// envelope has to come up, hold and let go — that is not a shape <see cref="Tone.Fall"/> can make, for
    /// the same reason <see cref="BuildServo"/> writes its motor out longhand — and the modulation rate has
    /// to be a function of how far through the event it is, which nothing in the vocabulary does.
    ///
    /// <b>The grip accumulates its phase rather than being handed a time.</b> Writing a falling rate straight
    /// into the argument of a sine — <c>sin(2π · rate(t) · t)</c> — does not give you that rate: the
    /// instantaneous frequency is the derivative of the whole argument, so a rate written to fall by a
    /// twentieth actually falls by a tenth, and this one measured as seventeen hertz dropping to under two
    /// rather than to nine. Adding the rate up a sample at a time is the fix and it is what
    /// <see cref="BuildBlip"/> already does with its sweep; the two places in this bank with a moving
    /// frequency now do it the same way.
    /// </summary>
    /// <param name="seed">The scrape's noise.</param>
    /// <param name="hz">The lowest thing in it.</param>
    /// <param name="seconds">How long it takes.</param>
    /// <param name="rasp">How fast it sticks and slips at the start.</param>
    private float[] BuildGroan(uint seed, float hz, float seconds, float rasp)
    {
        var count = (int)(seconds * _rate);
        var groan = new float[count];
        var slip = 0f;

        for (var i = 0; i < count; i++)
        {
            var t = (float)i / _rate;
            var u = t / seconds;

            // A load being taken rather than a note being played: a fifth of a second to come up, and the
            // last third of it letting go.
            var swell = MathF.Min(1f, u * 5f) * MathF.Min(1f, (1f - u) * 3.2f);

            slip += 2f * MathF.PI * rasp * (1f - 0.45f * u) / _rate;

            var grip = 0.5f + 0.5f * MathF.Sin(slip);

            groan[i] = (MathF.Sin(2f * MathF.PI * hz * t)
                        + 0.42f * MathF.Sin(2f * MathF.PI * hz * 1.98f * t)
                        + 0.16f * MathF.Sin(2f * MathF.PI * hz * 3.1f * t))
                       * swell * (0.55f + 0.45f * grip);
        }

        // A breath of the surfaces themselves through it, so it is two things rubbing and not an organ pipe.
        var scrape = Tone.Noise(count, seed).Band(_rate, 180f, 1200f);

        for (var i = 0; i < count; i++)
        {
            var u = (float)i / count;
            scrape[i] *= MathF.Min(1f, u * 5f) * MathF.Min(1f, (1f - u) * 3.2f);
        }

        return groan.Mix(scrape, _rate, 0f, 0.22f).Peak(0.7f);
    }

    // ---- and the morning, which is somebody else's building ---------------------------------------------

    /// <summary>
    /// Being inside a station, which is the one thing this film has never had to sound like.
    ///
    /// <b>It is the opposite of the drive.</b> A drive is one machine, so it must not beat; a docking bay is
    /// a room seventy metres across with a station's whole plant on the other side of its wall, an umbilical
    /// connected to the ship, and air being pushed through the lot of it — several machines, none of them at
    /// quite the same speed. So it is built the way the plant room's rumble is and for the same reason:
    /// forty-one, a hundred and twenty-three and two hundred and fourteen do not share a period, and what
    /// comes out is a wide, slightly unsteady hum rather than a note.
    ///
    /// The band goes up to fifteen hundred rather than stopping at two hundred like the drive, and that one
    /// number is what says the sound is arriving <i>through air</i> rather than through a metre of plate.
    /// It is the whole of the difference between being in a hull and being in a building, and it is why the
    /// bay going quiet as the ship clears the mouth is the loudest thing that happens in the chapter without
    /// anything being added.
    /// </summary>
    private float[] BuildBerth()
    {
        var bed = Tone.Noise((int)(7.4f * _rate), 0x3B90D2)
            .Band(_rate, 90f, 1500f)
            .Centre()
            .Peak(0.5f)
            .Seam(_rate, 0.7f);

        return bed
            .Sine(_rate, Tone.Locked(41f, _rate, bed.Length), 0.40f)
            .Sine(_rate, Tone.Locked(123f, _rate, bed.Length), 0.12f)
            .Sine(_rate, Tone.Locked(214f, _rate, bed.Length), 0.06f)
            .Wander(_rate, 0.19f, 0.22f)
            .Peak(0.8f);
    }

    /// <summary>
    /// A room with its computers on: a fan under a desk, a coil, and the very top of a screen.
    ///
    /// Chapter 7 walks the same gallery and has none of this, which is the point of it existing at all. That
    /// was a man on a night watch in a room nobody was using; this is the same room on the morning it takes
    /// a ship out of a berth, with every console at the window awake because somebody is about to need them.
    /// Nothing says <i>working</i> more cheaply than a hundred-hertz fan that was not there yesterday.
    ///
    /// Seven and a half kilohertz is deliberately near the top of what anybody hears and is mixed at a
    /// thirtieth. It is not meant to be noticed; it is meant to be missed the moment the chapter takes it
    /// away, which is what a coil whine does in a real room.
    /// </summary>
    private float[] BuildConsoles()
    {
        var bed = Tone.Noise((int)(3.7f * _rate), 0x6E21A5)
            .Band(_rate, 1600f, 6400f)
            .Centre()
            .Peak(0.10f)
            .Seam(_rate, 0.4f);

        return bed
            .Sine(_rate, Tone.Locked(96f, _rate, bed.Length), 0.26f)
            .Sine(_rate, Tone.Locked(192f, _rate, bed.Length), 0.08f)
            .Sine(_rate, Tone.Locked(7400f, _rate, bed.Length), 0.035f)
            .Wander(_rate, 0.33f, 0.10f)
            .Peak(0.7f);
    }

    /// <summary>
    /// One console tone: a key, and then the note it made.
    ///
    /// The key is twelve milliseconds of high noise in front of the sine, and it is what stops these being
    /// test tones. A machine that answers makes a sound with an <i>onset</i> — a contact, a relay, a
    /// speaker cone starting — and a pure sine faded up over four milliseconds has none, which is why the
    /// first version of these sounded like a hearing test.
    ///
    /// The second partial is at 2.01 rather than 2, and that is not a typo either. Exactly two octaves
    /// reinforces into one brighter note; a hundredth off beats slowly against itself over the length of
    /// the pip, which is what a small speaker in a plastic bezel actually does.
    /// </summary>
    private float[] BuildPip(uint seed, float hz, float seconds)
    {
        var body = new float[(int)(seconds * _rate)]
            .Sine(_rate, hz, 1f)
            .Sine(_rate, hz * 2.01f, 0.18f)
            .Fall(_rate, seconds * 0.42f, 0.004f);

        var key = Tone.Noise((int)(0.012f * _rate), seed).Band(_rate, 2400f, 9000f).Fall(_rate, 0.004f);

        return new float[(int)((seconds + 0.03f) * _rate)]
            .Mix(body, _rate, 0.004f)
            .Mix(key, _rate, 0f, 0.35f)
            .Peak(0.7f);
    }

    /// <summary>
    /// A docking clamp letting go, and it is two events a quarter of a second apart rather than one.
    ///
    /// First the ram vents — half a second of high noise falling away, which is the part that says a
    /// machine <i>decided</i> — and then the latch drops, which is fifty-eight hertz with a hundred tonnes
    /// behind it. In that order, always: a clamp that clunks and then hisses is a clamp closing, and the
    /// chapter needs one opening.
    /// </summary>
    private float[] BuildClamp()
    {
        var release = Tone.Noise((int)(0.55f * _rate), 0x9D4E07)
            .Band(_rate, 900f, 5200f)
            .Fall(_rate, 0.18f, 0.012f);

        var latch = new float[(int)(0.6f * _rate)]
            .Sine(_rate, 58f, 1f)
            .Sine(_rate, 87f, 0.40f)
            .Sine(_rate, 143f, 0.16f)
            .Fall(_rate, 0.16f);

        var ring = Tone.Noise((int)(0.35f * _rate), 0x1A76C3).Band(_rate, 260f, 1800f).Fall(_rate, 0.09f);

        return new float[(int)(1.1f * _rate)]
            .Mix(release, _rate, 0f, 0.5f)
            .Mix(latch, _rate, 0.24f, 1f)
            .Mix(ring, _rate, 0.24f, 0.45f)
            .Peak(0.8f);
    }
}
