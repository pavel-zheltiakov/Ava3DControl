using System.Numerics;

namespace Ava3D.Demo.Story;

/// <summary>
/// Chapter 6. Through the bulkhead into the engine room, and a repair: read the manual, then do the job.
///
/// It is the only chapter in the film in which anything is <i>done</i>. Five rooms and a corridor have been
/// looked at; this one is worked in. The alarm that has been running since the middle of chapter 4 is what
/// brought him here and it does not stop when he arrives — it stops when the last lamp on the board lights,
/// which is seventy seconds later and is the first thing in nine minutes that happens because of something
/// he did.
///
/// <b>The four board scenes are the chapter, and they are split across two objects.</b> On the display at
/// the back of the bench the board is <i>information</i>: <see cref="Scenes.Board.DraftsmanScene"/>'s
/// drawing, then <see cref="Scenes.Board.WireframeScene"/>'s edges over it, then
/// <see cref="Scenes.Board.InspectorScene"/> walking it and naming the part that is dead. On the bench in
/// front of it the board is an <i>object</i>, four hundred and fifty-eight millimetres of it, out of the
/// server it belongs to, with its indicator card lying unplugged beside it. He reads the screen and then he
/// fits the card, and <see cref="Scenes.Board.IndicatorsScene"/> is what happens next.
///
/// <b>And the room is the last statement of the film's oldest argument.</b> Four light slots, all four of
/// them within a metre of a bench at the end of a seventeen-metre hangar, and the hangar is nevertheless
/// full of running machinery — because every rack indicator, every coolant channel, the lit edge under the
/// gantry and the display's own glass is emission with additive blending and no light slot at all. The
/// antechamber made that point with one bulb. The lounge made it with four televisions. This makes it with
/// a room.
/// </summary>
internal sealed class Repair(Corridor corridor, EngineRoom room) : Chapter
{
    /// <summary>
    /// Where the film clock stands when this chapter begins, so the corridor's beacons do not jump.
    ///
    /// Three chapters now drive them and all three drive them from one clock. It is the one number in the
    /// film that is kept in step by hand, and the alternative is handing every chapter a film clock it has
    /// no other use for.
    /// </summary>
    private const float Preceding = Screens.Length + Alarm.Length;

    /// <summary>When the display wakes, and the three views it shows.</summary>
    private const float Wake = 14f;

    private const float Grid = 22f;
    private const float GridOut = 28f;

    /// <summary>
    /// When the schematic replaces the drawing and the probe starts walking it.
    ///
    /// Its eight stops at two and three fifths of a second each run twenty and four fifths, which is what
    /// the beat is long — the scene reads its own clock and the chapter gives it one that starts here.
    /// </summary>
    private const float Schematic = 30f;

    /// <summary>
    /// The build, in the order a board is built: the processor, the cooler over it, both memory modules,
    /// and only then the card that was the fault.
    ///
    /// Six seconds a part and they overlap by nothing, because the order <i>is</i> the content. A cooler
    /// cannot go on before the processor is in and memory cannot go in around a cooler that is not there
    /// yet, and anybody who has ever done this knows it — which means getting the order wrong is the one
    /// mistake in this chapter that would be noticed by everyone rather than by nobody.
    /// </summary>
    private const float Fitted = 56f;

    /// <summary>When the gate at the end of the corridor parts. The soundtrack had this number written out a
    /// second time; it is named now, so the motor and the leaves cannot drift apart.</summary>
    public const float Opens = 1.5f;

    public const float Cooled = 62f;
    public const float Filled = 68f;
    public const float Ranked = 78f;

    /// <summary>And the card, last, because the machine has to be a machine before the fault is the only
    /// thing left wrong with it.</summary>
    private const float Fit = 79f;

    public const float Seated = 84f;

    /// <summary>When the bench goes down to one lamp, and when the card comes up.</summary>
    private const float Dim = 82f;

    /// <summary>When the board comes up. Public because chapter 7 carries the fan's spin across the
    /// boundary, and the fan's angle is a closed form in the time since this second.</summary>
    public const float Power = 88f;

    /// <summary>
    /// How much of the indicator sequence to run.
    ///
    /// <see cref="Scenes.Board.IndicatorsScene"/> is eight steps of one and a tenth of a second, and the
    /// last of the eight is the row going out again — a power-on self test, which is what it is on its own
    /// and is exactly wrong here. Seven and two fifths lands inside the seventh step, where all six are
    /// lit and none of them has begun to fade, and holding the clock there holds the board on. Nothing in
    /// that file needed a flag: the scene is a function of its own elapsed time, so refusing to advance
    /// the time is the whole of how you stop it.
    /// </summary>
    public const float Fill = 7.4f;

    /// <summary>When the last lamp is lit and the room is allowed to answer.</summary>
    public const float Ready = 96f;

    private int _bank = -1;

    public override string Title => "The engine room";

    /// <summary>Public, and it is the same reason <see cref="Alarm.Length"/> is: the chapter after this
    /// one keeps this room running, and everything it holds is measured from a second inside this
    /// chapter.</summary>
    public const float Length = 101f;

    public override float Duration => Length;

    /// <summary>
    /// Through the door, up the middle of the room, and then a metre and a half of bench for the rest of
    /// the chapter.
    ///
    /// The first waypoint is the last waypoint of chapter 5 — three and a half metres short of a door that
    /// has been shut for the whole of it — and the door opens on the second second. It is the only door in
    /// the building that opens while he is looking at it, which is what the other two were the first two
    /// terms of.
    ///
    /// He looks up once, at nine seconds, and it is the only time in nine minutes the camera does. Every
    /// ceiling before this one has been three metres two with nothing on it; this one is six and a half
    /// with beams and pipe runs across it, and a room is not tall until somebody has looked.
    ///
    /// The last third drops his eye from a metre seven to a metre and a quarter and then to one twelve. It
    /// is the same mechanism as sitting down on the lounge's sofa and it needs no more than that one: a
    /// walk is positions, so leaning over a bench is a waypoint that happens to be lower than the one
    /// before it. Six indicators on a card ninety-five millimetres across are not visible from standing.
    /// </summary>
    public override Walk Walk { get; } = new(
        new Step(0f, EngineRoom.Ahead(-3.85f), Corridor.Gateway),

        // Through six hundred millimetres of bulkhead. Slower than a walk, because it is the only wall in
        // the film you are inside of for long enough to notice.
        new Step(5f, EngineRoom.Ahead(1f), EngineRoom.Ahead(7f)),

        // Up. See above.
        new Step(9f, EngineRoom.Ahead(2.6f), EngineRoom.Roofline),
        new Step(13f, EngineRoom.Ahead(4.4f), EngineRoom.Engines),

        // And across to the bench, arriving on the three machines rather than on the screen: the room has
        // to say what this bench is before the screen says anything at all.
        new Step(19f, EngineRoom.At(1.6f, 2.6f), EngineRoom.Servers),
        new Step(23f, EngineRoom.At(-0.5f, 1.9f), EngineRoom.Display),
        new Step(29f, EngineRoom.At(-0.7f, 1.6f), EngineRoom.Display),

        // The schematic, from a little closer, because the copper is a line two pixels wide.
        new Step(35f, EngineRoom.At(-0.85f, 1.35f), EngineRoom.Display),
        new Step(48f, EngineRoom.At(-0.85f, 1.35f), EngineRoom.Display),

        // And he turns to the bench. The board has been lying in the bottom of the frame for half a minute
        // and this is the first time it is the subject: the whole of it, at eight hundred millimetres, so
        // that the thing about to be worked on has been seen entire before any part of it is.
        // and this is the first time it is the subject: the board and the four parts laid out beside it in
        // the order they go on, all in one shot, before any of them moves.
        new Step(52f, EngineRoom.At(1.9f, 1.5f, 1.44f), EngineRoom.Laid),

        // The processor and then the cooler, both from the same place, because they go in the same hole.
        // Thirty centimetres, which is where somebody's head is when they are doing this.
        new Step(56f, EngineRoom.At(2.1f, 1.24f, 1.2f), EngineRoom.Socket),
        new Step(67f, EngineRoom.At(2.05f, 1.28f, 1.24f), EngineRoom.Socket),

        // The memory, which is at the other end of the board and a hand's width taller when it is in.
        new Step(72f, EngineRoom.At(1.7f, 1.34f, 1.3f), EngineRoom.Slots),
        new Step(78f, EngineRoom.At(1.7f, 1.34f, 1.3f), EngineRoom.Slots),

        // And in on the connector, to a quarter of a metre. Nothing else in nine minutes of film is looked
        // at from closer than a metre, and the reason to break it here is that the whole beat is a hundred
        // millimetres of card going onto the end of a cable — at any distance that keeps a person's whole
        // posture in shot, it is a green speck moving.
        new Step(82f, EngineRoom.At(1.5f, 1.1f, 1.1f), EngineRoom.Lamps),

        // Closer still for the lamps: a hundred and fifty millimetres, which is a magnifier's distance and
        // is what six indicators seven millimetres across need.
        new Step(88f, EngineRoom.At(1.45f, 1.005f, 1.03f), EngineRoom.Lamps),
        new Step(96f, EngineRoom.At(1.45f, 1.005f, 1.03f), EngineRoom.Lamps),

        // And out again, for the last shot of the chapter: the whole board, built and lit by its own
        // indicators and one work light, at three quarters of a metre. It is the same board and the same
        // lamp as the shot at fifty-two, and everything about it is different.
        new Step(101f, EngineRoom.At(1.6f, 1.45f, 1.42f), EngineRoom.Board));

    public override void Enter(Hall hall)
    {
        _bank = -1;

        // Both rooms, for the whole chapter, and the corridor is for what is behind him rather than for
        // the walk. It is twenty-one metres of geometry drawn for a doorway — which is what the gallery
        // pays for the same reason — and here it buys something the gallery's does not: the alarm is still
        // running out there, in red, with no lamp of its own, and the moment it stops is the moment the
        // board says it is finished. A room that goes quiet has to have been audible.
        hall.Occupy(Deck.CorridorRoom, Deck.EngineRoom);

        // Colder than the corridor's and no brighter. The bounce in a steel hangar is a steel hangar, and
        // this room is nine tenths empty air with nothing lit in it to bounce off.
        hall.Ambient(0.016f, 0.008f);
    }

    public override void Update(Hall hall, float seconds)
    {
        var bank = seconds < Wake ? 0 : seconds < Power - 0.5f ? 1 : 2;

        if (bank != _bank)
        {
            _bank = bank;
            Spend(hall, bank);
        }

        // The door, on the second second. It is the only one in the building that opens while somebody is
        // watching it, and the ramp is a function of the time rather than a state machine — so seeking to
        // the middle of the approach shows a door that is halfway open, not one that is shut and catching up.
        corridor.Gate.Open(Ramp(seconds, Opens, Door.Opening));

        // And the way out, held shut. It is never opened in this chapter and it is written down anyway,
        // because chapter 7 opens it and somebody scrubbing backwards would otherwise arrive in a room
        // whose far door is standing open for no reason. A state that is only ever set forwards is a
        // state, and this film does not have any.
        room.Way.Open(0f);

        // The alarm behind him. It has been running for two chapters and it goes out on the ready, over
        // three seconds, which is a light that stops rather than a light that is switched off.
        corridor.Glow(Preceding + seconds, 1f - Ramp(seconds, Ready, 3f));

        // The bench, and the way it leaves. Everything except the lamp on the board is taken to nothing
        // over the four seconds before the power-up, so the three slots the indicator card is about to be
        // handed are taken from lamps that are already contributing nothing — the same rule every
        // threshold in this building follows, and the reason no hand-over in this film needs a crossfade.
        var lit = 1f - Ramp(seconds, Dim, 4f);

        room.High[0].Dim(lit * (1f - Ramp(seconds, Wake, 4f)));
        room.High[1].Dim(lit * (0.85f - 0.45f * Ramp(seconds, Wake, 4f)));
        room.Bay.Dim(lit);
        room.Fill.Dim(lit * Ramp(seconds, Wake - 2f, 4f));

        // The task lamp survives the blackout at a fifth, and that is the one number in this chapter that
        // was arrived at twice. At zero the power-up is six coloured points floating in nothing —
        // correct, and unreadable, because a lamp on a board is only legible when there is a board under
        // it. At a half the board is lit well enough that six indicators are six small bright things on a
        // green surface rather than the only thing in the room.
        room.Task.Dim(Ramp(seconds, Wake - 1f, 4f) * (1f - 0.52f * Ramp(seconds, Dim, 4f)));

        // The display: backlight, then the drawing, then every edge over it, then the schematic instead of
        // both. Only one view is ever on, and the wireframe is switched off rather than faded because it
        // does not test depth — see EngineRoom.Grid.
        room.Backlight(Ramp(seconds, Wake - 1f, 2f));
        room.Print(Ramp(seconds, Wake, 3f) * (1f - Ramp(seconds, Schematic - 1f, 1.5f)));
        room.Grid(Ramp(seconds, Grid, 1.6f) * (1f - Ramp(seconds, GridOut, 1.6f)));
        room.Schematic(seconds >= Schematic - 0.5f);

        // The probe. Clamped at both ends rather than gated by an if, because the scene reads the clock it
        // is handed and nothing else: below zero its stop index goes negative, and past the end of the
        // walk it wraps round to the beginning and starts naming the processor again while he is already
        // holding a card.
        room.Probe.Update(hall.Scene, Math.Clamp(
            seconds - Schematic, 0f, (float)room.Probe.TourDuration.TotalSeconds - 0.01f));

        // The build, part by part, and every one of them a ramp off the film clock rather than a step in a
        // sequence. Seeking to the middle of the cooler going on shows a cooler halfway on.
        room.Seat(
            Ramp(seconds, Fitted, Cooled - Fitted),
            Ramp(seconds, Cooled, Filled - Cooled),
            Ramp(seconds, Filled, Ranked - Filled));

        // And the card, off the bench and into the connector on the end of the ribbon.
        room.Plug(Ramp(seconds, Fit, Seated - Fit));

        // Power. The lines in the parts come up over two seconds and the fan starts turning and does not
        // stop accelerating — see EngineRoom.Fan, which is the one thing in this film that moves as the
        // square of the time rather than as the time.
        room.Live(Ramp(seconds, Power, 2f));
        room.Fan(seconds - Power, Ramp(seconds, Power, 2f));

        // The board's own lamps, filling and then held. Below the start it is handed zero and does
        // nothing; above the fill it is handed the same number every frame and holds the row lit.
        room.Panel.Update(hall.Scene, Math.Clamp(seconds - Power, 0f, Fill));

        // And the room answering: thirty-six rack indicators going from amber to green, and two engines
        // that have been dark for the whole chapter coming up. Nothing says all systems ready. The board
        // says it by being fully lit and the room says it by agreeing.
        room.Machines(Preceding + seconds, 1f, Ramp(seconds, Ready, 3.5f));

        hall.Scene.Invalidate();
    }

    public override string? Caption(float seconds) => seconds switch
    {
        // The last rule. Nothing after this chapter is numbered — see Dark.Caption.
        < 14f => "Rule six. The door is half a metre thick. Behind it is the biggest room here",
        < 26f => "Three machines. The middle one is dead. This part came out of it",

        // The payoff for the turning display in chapter 2 and the mirror ball in chapter 4, and the only
        // caption in the film that closes a loop. One dying machine, three things running late, and the
        // only person aboard who noticed is the one who knows a display's timing to half a second. The
        // word "late" is the same word all three times, which is what makes it findable.
        //
        // It explains nothing about Contact, and it must not: what stays open there is whether the job
        // mattered, which is chapter 9's question and not this one's.
        < 38f => "That machine moves everything on this floor. That is why the display was late",
        < 50f => "The manual first. I have done this nine times. I still read it first",
        < 56f => "That is the broken part. It has a number, and I have the number",

        // Fitted at 56, Cooled at 62, Filled at 68, Seated at 84 — the two lines below name the four
        // parts in the order the bench fits them, so each caption is on screen while its own parts land.
        < 70f => "First the processor. Then the cooler. Both go straight down",
        < 88f => "Then the memory, one on each side. Last, the broken card. That one slides in",
        < 96f => "Six small lights. Nobody will ever see them but me. That is the job",
        _ => "All systems ready. Nothing says it out loud. The lights say it"
    };

    /// <summary>
    /// Which four lights are spending the slots, and the last of the three hand-overs is the chapter.
    ///
    /// Bank 0 is the room: two high bays and the bench light, so seventeen metres of hangar has shape in it
    /// while he crosses it. Bank 1 is the bench: the bench light, the lamp on its arm, the fill on the
    /// display and one high bay kept back so the room behind him is not a hole. Bank 2 is the answer to
    /// the whole film — three of the four slots go to the indicator card's own lights, which follow the
    /// three most recently lit lamps, and the fourth stays on the board so there is something for them to
    /// be lit on.
    ///
    /// That third bank is <see cref="Scenes.Board.IndicatorsScene"/>'s own arithmetic, stated in its notes
    /// and demonstrated there by a scene. Here it is done by a room, with a lamp of the room's own in the
    /// fourth slot, which is the same sum with something at stake.
    /// </summary>
    private void Spend(Hall hall, int bank)
    {
        switch (bank)
        {
            case 0:
                hall.Use(room.High[0].Light, room.High[1].Light, room.Bay.Light, room.Fill.Light);
                break;

            case 1:
                hall.Use(room.Bay.Light, room.Task.Light, room.Fill.Light, room.High[1].Light);
                break;

            default:
                hall.Use(room.Task.Light, room.Panel.Lights[0], room.Panel.Lights[1], room.Panel.Lights[2]);
                break;
        }
    }
}
