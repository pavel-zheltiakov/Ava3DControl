using System.Numerics;
using Ava3D.Demo.Scenes.Arcade;

namespace Ava3D.Demo.Story;

/// <summary>
/// Chapter 7. A room to sit down in: four televisions waking one at a time along a bench, a case on the
/// west wall with the first game's sprites standing in it, a console on a low table, an armchair, and an
/// alcove behind the chair with a mirror ball in it.
///
/// The chapter has a shape the other four do not, and it is the shape of an evening rather than of a tour.
/// He comes in and the lights are on — properly on, for the only time in the film — and the room is
/// furnished and comfortable and has nothing in it that is being exhibited. Then he starts watching the
/// games, and the room goes down around him, one lamp at a time, until the only light in it is four screens
/// two and a half metres away. Then he sits down in the chair and watches all four at once in the dark,
/// which is the argument the room exists to make: an unlit material spends no light slot, so a room with
/// every lamp in it switched off still has a picture in it.
///
/// And then he turns round, and that is the beat. Behind the chair is a half-round alcove that has been
/// dark and empty since he walked in — literally empty, four metres of wall with nothing on it — and it
/// comes on. A ball turns, four coloured lights orbit it on a beat, and dots sweep the wall and the floor.
/// It costs four light slots, which is every slot there is, which is only affordable because the room got
/// dark first. The constraint wrote the beat rather than the beat working around the constraint.
///
/// Nothing in the room is named, and that includes the hardware. See <see cref="ScreenRoom"/>.
/// </summary>
internal sealed class Screens(ScreenRoom room, Corridor corridor) : Chapter
{
    /// <summary>How long the chapter runs. A constant as well as a property because chapter 8 needs it:
    /// the beacons in the corridor start turning here and must not jump when the chapter changes, so both
    /// chapters drive them from one clock and the next one has to know how far this one got.</summary>
    public const float Length = 93f;

    /// <summary>When each set comes on, in chapter seconds. Roughly when he arrives in front of it.</summary>
    private static readonly float[] Wakes = [16f, 23f, 29f, 35f];

    /// <summary>How many televisions there are.</summary>
    public int Sets => room.Games.Length;

    /// <summary>
    /// The game on one of the sets and the chapter second it came on at, for whoever needs to follow it.
    ///
    /// Which is the score, and this pair is the whole of what it needs — see <c>Soundtrack.Games</c>. It is
    /// two values rather than the room because the room is furniture and a soundtrack has no business with
    /// the sofa: what a chapter owes the sound is which games are running and what clock each of them is on.
    /// The clock is the interesting half, since a set that woke at twenty-three seconds is at
    /// <c>seconds − 23</c> of its own loop however the film arrived at this moment.
    /// </summary>
    public (ArcadeScene Game, float Wake) Set(int index) => (room.Games[index], Wakes[index]);

    /// <summary>When the room's four warm lamps have finished going out and their slots can be handed to
    /// the alcove. Nothing is lit but the screens and the case between here and <see cref="Curtain"/>.</summary>
    public const float Blackout = 65f;

    /// <summary>When the alcove starts. Four seconds after he has sat down in front of it and found
    /// nothing there, which is four seconds of a shot with no subject in it and is the whole reason the
    /// next twelve work.</summary>
    public const float Curtain = 70f;

    /// <summary>Beats a second. A hundred and twenty a minute, which is what this kind of light is always
    /// doing.</summary>
    public const float Tempo = 2f;

    private int _bank = -1;

    public override string Title => "Screens";

    public override float Duration => Length;

    /// <summary>
    /// How far out from the case the pass runs, and it is the one number in this walk that was arrived at
    /// by looking at the frame rather than by reasoning about it.
    ///
    /// A metre and a half from the opening. The first attempt was seventy-three centimetres, on the theory
    /// that a case is something you stand close to, and what that produced was a frame with nothing in it
    /// but sky: at that range this lens sees three quarters of a metre and the case is nine tenths of a
    /// metre tall, so the box and the room it hangs in were both outside the picture and what was left was
    /// a wall of blue with figures on it. Backed off to here the frame has the hood, the shelf, both ends
    /// and a stretch of panelling in it, and only then does what the row is doing read as something
    /// happening inside a box on a wall.
    ///
    /// It has to clear the fourth station on the way — the armchair and its crate reach to about
    /// eighty-seven centimetres short of where the case begins — which is what fixes where the pass can
    /// start, and it has to stay west of the sofa's arm, which is what fixes how far out it can go.
    /// </summary>
    private const float PassX = -2.4f;

    /// <summary>
    /// In through the east door, along the bench, west to the case, north along it, into the sofa, round,
    /// and out through the north-west door.
    ///
    /// The first second of it is the last second of chapter 3 — same position, same aim — because the two
    /// are one shot and the only cut in this film is the one into <c>Contact</c>. What he is looking at as
    /// he arrives is the furniture and not the televisions, which are dark: a room you would sit down in,
    /// before a room with four screens in it.
    ///
    /// <b>The pass along the case is the one lateral shot in the film.</b> Everything else here walks
    /// towards what it is looking at, which is what a person does; this walks <i>across</i> it, which is
    /// the only camera move that can show a billboard turning. Position and aim advance together and by the
    /// same two metres and a tenth, so the camera looks squarely in for the whole of it and the case is the
    /// thing that moves — the bars swing and thin, the row does not turn at all, and the near figures cross
    /// the far ones. A shot that walked in would have proved nothing: a sprite seen from one place is a
    /// picture.
    ///
    /// From <see cref="Seat"/> on, the eye is at a metre twenty-two rather than a metre seven. That is the
    /// only place in the film where the visitor's height changes and it needs no mechanism at all — a walk
    /// is positions and the position says where the eye is, so sitting down is a waypoint that happens to
    /// be lower than the one before it.
    /// </summary>
    public override Walk Walk { get; } = new(
        new Step(0f, ScreenRoom.Entrance + new Vector3(0.35f, 0f, -1.3f), ScreenRoom.Sitting),

        // The first station, from about a metre and three quarters — near enough to see there is a
        // cartridge standing out of the machine and far enough that the seat and the crate it belongs to
        // are still in the picture.
        new Step(5f, Deck.Screens + new Vector3(4.05f, Deck.Eye, -0.75f), ScreenRoom.Console(0)),
        new Step(9f, Deck.Screens + new Vector3(4.05f, Deck.Eye, -0.75f), ScreenRoom.Console(0)),

        new Step(13f, ScreenRoom.InFrontOf(0) + new Vector3(0.45f, 0f, 0.15f), ScreenRoom.Glass(0)),
        new Step(18f, ScreenRoom.InFrontOf(0), ScreenRoom.Glass(0)),
        new Step(24f, ScreenRoom.InFrontOf(1), ScreenRoom.Glass(1)),
        new Step(30f, ScreenRoom.InFrontOf(2), ScreenRoom.Glass(2)),
        new Step(36f, ScreenRoom.InFrontOf(3), ScreenRoom.Glass(3)),
        new Step(40f, ScreenRoom.InFrontOf(3), ScreenRoom.Glass(3)),

        // Off the bench: half a step back from the set and a quarter turn west, which puts him at the south
        // end of the case looking straight into it. Four seconds for sixty centimetres, because almost all
        // of what happens here is the head coming round.
        new Step(44f, Deck.Screens + new Vector3(PassX, Deck.Eye, Diorama.South), Diorama.At(Diorama.South)),

        // The pass. Eight seconds for two metres and a tenth — a quarter of a metre a second, which is slow
        // for a walk and is the speed somebody goes along a case they are reading. Then two seconds on the
        // big one at the north end, which is the only place in the film where a single texel of anything is
        // the better part of four centimetres across.
        new Step(52f, Deck.Screens + new Vector3(PassX, Deck.Eye, Diorama.North), Diorama.At(Diorama.North)),
        new Step(54f, Deck.Screens + new Vector3(PassX, Deck.Eye, Diorama.North), Diorama.At(Diorama.North)),

        // The turn. He does not move at all for four seconds and his head comes round off the case and onto
        // the alcove — which is where the sofa has been pointing since the first frame of the chapter. The
        // lamps have been going down since he stopped at the far end. Position and aim are interpolated
        // separately, so
        // a stop that only turns costs two waypoints with one position in them.
        new Step(58f, Deck.Screens + new Vector3(PassX, Deck.Eye, Diorama.North), ScreenRoom.Stage),

        // And across to the sofa, and down into it. Facing the alcove the whole way, which is why the
        // crossing runs east-south-east instead of doubling back the way he came: he is walking towards a
        // seat he can already see the back of, rather than reversing out of a corner.
        new Step(63f, Deck.Screens + new Vector3(-1.9f, Deck.Eye, 1.5f), ScreenRoom.Stage),
        new Step(66f, ScreenRoom.Seat, ScreenRoom.Stage),
        new Step(70f, ScreenRoom.Seat, ScreenRoom.Stage),
        new Step(82f, ScreenRoom.Seat, ScreenRoom.Stage),

        new Step(86f, Deck.Screens + new Vector3(-1.2f, Deck.Eye, 1.6f), ScreenRoom.Stage),
        new Step(89f, Deck.Screens + new Vector3(-2.9f, Deck.Eye, 2.6f), ScreenRoom.Exit),

        // Stopped short of the opening rather than standing in it. It was written that way when there was
        // a blind stub behind that door and a camera in the doorway filled the frame with it; the corridor
        // has been there for some time and the waypoint is kept, because what it gives is better than what
        // it was avoiding — a lit doorway in a dark room, a metre and a half off, with the alcove still
        // going behind him.
        new Step(93f, ScreenRoom.Exit + new Vector3(0f, 0f, -1.5f), ScreenRoom.Exit + new Vector3(0f, 0f, 3f)));

    public override void Enter(Hall hall)
    {
        _bank = -1;

        // Warmer and a little brighter than this room used to arrive, because it starts with its lamps on
        // now. It is taken down inside Update rather than here — see there.
        hall.Ambient(0.062f, 0.030f);

        foreach (var lamp in room.Warm)
            lamp.Dim(1f);

        foreach (var beam in room.Beams)
            beam.Intensity = 0f;

        corridor.Gate.Open(0f);
    }

    public override void Update(Hall hall, float seconds)
    {
        var bank = seconds < Blackout ? 0 : 1;

        if (bank != _bank)
        {
            _bank = bank;
            Spend(hall, bank);
        }

        // The room going down around him, and it starts at the far end of the case rather than during the
        // games.
        //
        // It used to fall away across the whole of the walk down the bench, and that was one beat too many:
        // the four sets are meant to be looked at in a room, and a room that is already going dark while he
        // is looking at them has started the next thing before this one finished. So it holds at three
        // quarters — enough that the screens are gaining on the lamps, not enough to notice — and then goes
        // properly once he has finished with the exhibits.
        //
        // Which now means the case rather than the bench, and it moved for a reason worth stating: the
        // strip of ground inside the case is the only lit thing in it, so a room that went dark during the
        // pass would take the floor out from under the row while he was walking past it. It starts the
        // second he reaches the far end and takes twelve — he reads the case in a lit room and crosses to
        // the sofa in a dark one.
        var house = (1f - 0.25f * Ramp(seconds, 16f, 18f)) * (1f - Ramp(seconds, 52f, 12f));

        room.Bench[0].Dim(house);
        room.Bench[1].Dim(house * 0.92f);
        room.Lounge.Dim(house * 0.85f);
        room.Doorway.Dim(house);

        // The ambient goes with them. It is the bounce off the walls, and a room whose lamps are off has
        // none — leaving it up is the single easiest way to end up with a "dark" room that is flat grey
        // everywhere and has no shadows in it.
        //
        // The floor it stops at is not zero, and that number was found by looking at the frame it decides.
        // At the moment he has turned round, every lamp in the building is off, every screen is behind him
        // and the alcove has not started: with no ambient at all that frame is <i>pure black</i>, all of it,
        // which is not a dark room — it is a dropped frame, and the audience reads it as the film having
        // broken rather than as the film holding its breath. Two hundredths is the alcove wall at about
        // fifteen of two hundred and fifty-five: the ball is a shape, the curve of the wall is a shape, and
        // there is visibly nothing on the stage. Which is the whole point of the four seconds.
        hall.Ambient(0.030f + 0.038f * house, 0.014f + 0.018f * house);

        var lit = false;

        for (var set = 0; set < room.Games.Length; set++)
        {
            if (seconds < Wakes[set])
                continue;

            // The game's own clock is the film's, offset by when this set came on. Which means a set that
            // has been on for twenty seconds is twenty seconds into its loop however you arrived at that
            // moment — walked to it, or dropped into it from the contents.
            lit |= room.Games[set].Show(room.Screens[set], seconds - Wakes[set]);
        }

        Alcove(seconds);

        // And the corridor beyond the north door, which starts running while he is sitting in front of the
        // mirror ball with his back to it.
        //
        // It comes up two seconds after the room goes properly black and takes seven to arrive, so it is at
        // full while the disco is at full and he has not turned round. The frame that matters is at
        // eighty-two, when he stands up: four coloured lights behind him, a hundred and twenty a minute,
        // and something red turning at the end of a corridor that was not doing that when he sat down.
        //
        // <b>It is the whole corridor, lit, and it used to be the emission only.</b> This chapter ran
        // Glow — the lenses, the beams and the floor strips, which are added to the frame rather than lit
        // in it and therefore cost no slot — and left the four red lamps to chapter 8. What that produced
        // was reported and is worth writing down: from in here the corridor was seven bright lenses hanging
        // in a black tube, and the moment the next chapter began, every wall in it turned red at once. The
        // alarm did not arrive, it was switched on, and it was switched on at exactly the frame a viewer is
        // most likely to notice a seam.
        //
        // So the lamps come up here instead, on the same ramp as the glass, and Alarm.Update goes on
        // driving them from where this leaves them. It is the first time in the film that a room he is not
        // standing in is given light of its own, and the reason it can be is that the reason it could not
        // has gone: four was the renderer's cap when this building was laid out, and the cap is sixty-four.
        // The four-slot arrangement everywhere else is a composition and stays one — what it was never
        // meant to be is a rule against a corridor at the far end of a room being visibly on fire.
        //
        // The eye is the walk's, so the four are assigned to the four beacons nearest him — which, from a
        // sofa twenty metres up the room, is the near end of the corridor. That is the half of it a doorway
        // can see, and it is the half a man about to walk in there should be looking at.
        corridor.Alarm(Walk.At(seconds).Eye, seconds, Ramp(seconds, Blackout + 2f, 7f));

        // Unconditionally, unlike the version of this chapter that only had televisions in it. Then the
        // only thing that ever changed was a screen, so a frame on which no game had advanced was a frame
        // identical to the last one; now the lamps are dimming, the ball is turning and four lights are
        // moving on every one of them. The <c>lit</c> flag is kept because it still says something true —
        // whether any game redrew — and it is now true of a strict subset of the frames that need drawing.
        _ = lit;

        hall.Scene.Invalidate();
    }

    /// <summary>
    /// The light show, and every part of it a pure function of the second.
    ///
    /// This was written when the demo had no audio, so "music-synced" had to mean what it turns out to be
    /// enough for: a tempo, and four lights that answer to different divisions of it. One on every beat,
    /// one on the offbeat, one every other beat and one swelling across four. A viewer reads that as a
    /// room with music in it within about two bars, which is interesting — the sync is not to a recording,
    /// it is to the regularity, and the regularity is the part the eye is actually reading.
    ///
    /// The demo now does have audio, and it changed nothing here, which is the part worth recording.
    /// <see cref="Soundtrack"/> puts a thump on this same <see cref="Tempo"/> from this same
    /// <see cref="Curtain"/>, and it is able to because both are derived from the second rather than
    /// counted — so the sound could be written against the picture's numbers a year later and land in
    /// step, through a seek, with nothing to keep synchronised at run time. That is the dividend of the
    /// paragraph below, collected.
    ///
    /// Being a function of the time is not a stylistic choice either. Seeking into the middle of this from
    /// the contents has to land on a frame that is exactly what the film would have shown had it played
    /// there, and anything accumulating — a phase counter, a beat index — would arrive at the wrong one.
    /// </summary>
    private void Alcove(float seconds)
    {
        var show = Ramp(seconds, Curtain, 6f);

        // Both turn at their own rate and neither is a multiple of the other, so the dots and the coloured
        // pools drift past each other instead of locking into one pattern and staying there.
        var spin = seconds * Glitter.Spin;
        var sweep = seconds * -121f;

        room.Mirror.RotationDegrees = new Vector3(0f, spin, 0f);

        var beat = seconds * Tempo;

        for (var i = 0; i < room.Beams.Length; i++)
        {
            room.Beams[i].Position = ScreenRoom.Orbit(sweep + i * 90f);

            // One and a half, not three and a half. A lamp a metre from a wall it is pointing at is doing
            // the same arithmetic as one three metres up in a corridor and arriving at nine times the
            // answer, and what that looks like is not a bright disco, it is an alcove with no shading left
            // in it at all.
            room.Beams[i].Intensity = show * 1.5f * (0.22f + 0.78f * Pulse(beat, i));
        }

        // And the dots, everywhere. The same yaw the ball was just turned by, handed over rather than worked
        // out again — see Glitter.Update for why one number in two places is the only arrangement in which
        // a dot and the lens it came out of stay in step.
        room.Show.Update(spin, show, beat, Walk.At(seconds).Eye - Deck.Screens);
    }

    /// <summary>One beam's answer to the beat: 0 to 1, sharp on and decaying off.</summary>
    private static float Pulse(float beat, int index) => index switch
    {
        0 => Decay(Fraction(beat)),
        1 => Decay(Fraction(beat + 0.5f)),
        2 => Decay(Fraction(beat * 0.5f)),
        _ => 0.5f + 0.5f * MathF.Sin(beat * MathF.PI * 0.25f)
    };

    private static float Decay(float t) => MathF.Pow(1f - t, 2.4f);

    private static float Fraction(float t) => t - MathF.Floor(t);

    public override string? Caption(float seconds) => seconds switch
    {
        < 16f => "Rule four. This is the only room with chairs",

        // The hinge of the chapter. Four seats for five people has never been a problem because nobody
        // comes — which is what motivates Hana's screen, the empty room and him sitting alone, three
        // thoughts that otherwise only follow each other.
        < 28f => "There are five of us and four seats. It has never been a problem",
        < 40f => "Hana leaves this screen on for her son. He is nine, and two hundred days away",

        // The diorama pass, which is hers as well, so the lateral walk needs no new subject.
        < 52f => "She made the box too. She took the little man out of his game and stood him in it",
        < 62f => "Walk past and they all turn to look at you. The bars in front do not",

        // Blackout, and then the chair. He is alone in a dark room on his last night, which is the only
        // place in the film where the reason for the note can be said.
        < 70f => "She never comes here. Nobody comes here now",
        < 80f => "So I sit alone and play. It is the one thing I do well that nobody needs",
        < 88f => "They are moving me to the day shift. That is the whole message",

        // The second of the night's three late things. It says "two tonight" because he is counting, and
        // because that tells the viewer they were right to.
        _ => "The mirror ball is late too. That is two things tonight"
    };

    /// <summary>
    /// Which four lights are spending the slots.
    ///
    /// Two banks and one swap, and the swap is the chapter. Every warm lamp in the room is at zero for four
    /// seconds before the alcove is handed anything, so there is no frame in which a light list is rebuilt
    /// under a lamp that is still contributing — the same rule every threshold in this building follows,
    /// except that here the two rooms either side of the threshold are the same room at two times of night.
    /// </summary>
    private void Spend(Hall hall, int bank)
    {
        if (bank == 0)
        {
            // Just the lounge for the first fifty-four seconds. The corridor is not up yet and does not
            // need to be: the north door is behind him for the whole of the walk down the bench, and a
            // twenty-one-metre room drawn behind a doorway nobody is looking through is a room drawn for
            // nothing.
            hall.Occupy(Deck.ScreensRoom);
            hall.Use(room.Bench[0].Light, room.Bench[1].Light, room.Lounge.Light, room.Doorway.Light);
        }
        else
        {
            // The alcove's four, and the corridor's four behind the north door. Eight, in a building whose
            // whole light budget is written as four — see Update, which is where the arithmetic and the
            // argument for it are. They are at nothing until the sixty-seventh second, so nothing about
            // this room changes: what it buys is a corridor that catches light before he is in it.
            hall.Occupy(Deck.ScreensRoom, Deck.CorridorRoom);
            hall.Use(
                room.Beams[0], room.Beams[1], room.Beams[2], room.Beams[3],
                corridor.Lights[0], corridor.Lights[1], corridor.Lights[2], corridor.Lights[3]);
        }
    }
}
