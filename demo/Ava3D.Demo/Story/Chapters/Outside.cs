using System.Numerics;

namespace Ava3D.Demo.Story;

/// <summary>
/// Chapter 7. He leaves the bench, walks the length of a window, and finds out where he has been.
///
/// Nothing is asked of him in it. Six chapters have been a museum and one was a repair; this one is a man
/// looking out of a window for forty seconds, and the only thing that happens is that three objects out
/// there are noticed in an order. A planet, which could be a painting. A station, which could be a light.
/// And a ship, coming onto station off the beam, which could not be either — and that is the one that
/// finishes the sentence the film has never said out loud: <b>he is on a cargo ship</b>. The exhibition is
/// a deck in a hull that is going somewhere, it has been for nine minutes, and the board he repaired was
/// never a museum's.
///
/// <b>It is the first frame in the film allowed to be large.</b> Every room before this one was kept small
/// deliberately — nothing further off than about fifteen metres, ceilings at three metres two, no sightline
/// that was not a doorway — so that six hundred metres would land as six hundred metres. Thirteen metres of
/// glass is the release of nine minutes of held breath, and it is the only chapter that moves the far
/// plane: see <see cref="Far"/>, which is the whole of what that costs.
///
/// <b>The reveal is what he is looking at.</b> With the whole outboard side open, everything out there is
/// in view from everywhere in the room, so nothing is staged by occlusion: the planet is unmissable and
/// arrives first because it is thirty-one degrees across; the station has to be looked for and gets a shot
/// of its own; and the escort arrives last because it is the only thing outside that is moving, and by the
/// end of the chapter it has stopped, in front of the planet, holding.
/// </summary>
internal sealed class Outside(EngineRoom room, Illuminator gallery, Traffic lane) : Chapter
{
    /// <summary>How long it runs, as a constant the chapters after it can add up. See
    /// <see cref="Morning"/>, whose planet is still the one this chapter started turning.</summary>
    public const float Length = 60f;

    /// <summary>Where the film clock stands when this chapter begins. The engine room's racks and the
    /// planet's rotation are both driven off it, so both carry on rather than restarting.</summary>
    private const float Preceding = Screens.Length + Alarm.Length + Repair.Length;

    /// <summary>When the way out opens, over <see cref="Door.Opening"/>. The fourth door in the building, and
    /// the third powered one. Public because the soundtrack puts a motor on it, the way it does the gate at
    /// the end of the corridor.</summary>
    public const float Opens = 8f;

    /// <summary>When the bench goes dark behind him: the work light and the terminal both. He has
    /// finished, and a room somebody has finished in is a room with the lights turned off in it.</summary>
    private const float Douse = 10f;

    /// <summary>
    /// When the four slots become one.
    ///
    /// He is in the doorway. Behind him is a room lit by nothing but its own indicators; in front of him
    /// is a gallery lit by a star. The hand-over needs no fade for the reason every hand-over in this film
    /// has needed none — the lights being given up are the indicator card's three, which have a range of a
    /// third of a metre and are seventeen metres away by now, so what they were contributing when they
    /// were dropped was nothing.
    ///
    /// It is public because it is the second where the story's sixth rule expires. Up to here nothing in the
    /// film may sound like a spacecraft; past here everything may, and <c>Soundtrack.Window</c> gates the
    /// whole of the ship's half of the sound bank on this one number rather than on a number of its own.
    /// </summary>
    public const float Handover = 16f;

    /// <summary>When the escort's approach begins and when it has stopped. It comes in from the west
    /// quarter and settles on the beam, which puts it in front of the planet at the last port.</summary>
    private const float Approach = 34f;

    private const float Settled = 56f;

    private int _bank = -1;

    public override string Title => "The illuminator";

    public override float Duration => Length;

    /// <summary>
    /// Nineteen hundred metres, and it is the only chapter that asks for anything but the building's two
    /// hundred and twenty.
    ///
    /// The near plane does not move with it, and that is the part worth knowing: depth precision is set by
    /// the <i>near</i> plane almost entirely — the resolution at a given distance goes as the square of
    /// that distance over the near plane — so pushing the far plane out by a factor of nine costs
    /// essentially nothing at the range the walls are at. What it buys is a starfield fourteen hundred
    /// metres out, which is the only way a planet six hundred metres away can have anything behind it.
    /// </summary>
    public override float Far => 1_900f;

    /// <summary>
    /// Out of the engine room, through the gallery, and stopped at the last port.
    ///
    /// The first waypoint is the last waypoint of chapter 6 — leaning over a board at a metre forty-two —
    /// and the first four seconds are him standing up. It is the only place in the film where the eye
    /// rises rather than falls, and it is doing what the shot at the end of chapter 6 set up: the same
    /// board, the same lamp, and now a man who has finished with it.
    ///
    /// The gallery is walked at about a third of a metre a second, which is half the pace of every other
    /// room in the building. Nothing is asked of him here and nothing is being shown to him; he is
    /// looking out of a window, and a walk that reads as looking has to be slower than a walk that reads
    /// as going somewhere.
    /// </summary>
    public override Walk Walk { get; } = new(
        new Step(0f, EngineRoom.At(1.6f, 1.45f, 1.42f), EngineRoom.Board),
        new Step(4f, EngineRoom.At(1.75f, 1.5f), EngineRoom.Board),

        // And he turns to the door, which is five and a half metres along the same wall the bench is on.
        new Step(8f, EngineRoom.At(2.6f, 1.75f), EngineRoom.Exit),
        new Step(14f, EngineRoom.At(5.3f, 1.15f), EngineRoom.Exit),

        // Through it, and the room arrives all at once: thirteen metres of glass running away west with
        // three posts down it. This one shot is why the window is a panorama and not four portholes — a
        // porthole has to be walked up to, and there is no version of walking up to something that is the
        // release this chapter is for.
        new Step(18f, Illuminator.Way, Illuminator.Pane(1)),

        // And the plot: a world turning over a table in the middle of the floor, going where the ship is
        // going. It is three seconds and it is the only thing in the room that is <i>about</i> the voyage
        // rather than of it — which is why it comes before the window and not after.
        new Step(21f, Illuminator.Along(5.5f, 3.3f), Illuminator.Table),

        // And then what is behind it. He is still by the door, so the planet is a long way down the glass
        // and small in the frame; the next twenty seconds are spent walking toward it, which is the whole
        // of the staging and needs nothing else.
        new Step(23f, Illuminator.Along(5.2f, 2.9f), Illuminator.Planetfall),
        new Step(28f, Illuminator.Along(4.2f, 2.2f), Illuminator.Planetfall),
        new Step(33f, Illuminator.Along(4.2f, 2.2f), Illuminator.Planetfall),

        // The station on the lit limb, which has to be looked for. It is the first man-made thing in nine
        // minutes that is not part of the building he has been walking around.
        new Step(38f, Illuminator.Along(0.5f, 2.1f), Illuminator.Relay),
        new Step(42f, Illuminator.Along(0.5f, 2.1f), Illuminator.Relay),

        // The escort, crossing. Fourteen metres of hull at sixty-seven, which is a range where it reads as
        // a vehicle and not as a mark — and it is the only thing out there that moves, so it is the only
        // thing in the chapter that arrives rather than being arrived at.
        new Step(47f, Illuminator.Along(-2.6f, 2f), Illuminator.Crossing),
        new Step(51f, Illuminator.Along(-2.6f, 2f), Illuminator.Crossing),

        // And the last bay, right up against the ledge: no mullion in the frame, no sill except a sliver at
        // the bottom, and everything the chapter has shown him in one picture. The planet across it, the
        // station on the limb, and the escort stopped in front of both.
        new Step(55f, Illuminator.Along(-6.1f, 2.6f, 1.7f), Illuminator.Holding),
        new Step(60f, Illuminator.Along(-6.1f, 2.05f, 1.64f), Illuminator.Planetfall));

    public override void Enter(Hall hall)
    {
        _bank = -1;

        // Both rooms for the whole chapter. The engine room is not scenery here — it is the argument: he
        // walks away from a room with no lamp burning in it that is nevertheless obviously running, and it
        // stays in frame through the doorway long enough to be seen doing it.
        hall.Occupy(Deck.EngineRoom, Deck.IlluminatorRoom);
    }

    public override void Update(Hall hall, float seconds)
    {
        var bank = seconds < Handover ? 0 : 1;

        if (bank != _bank)
        {
            _bank = bank;
            Spend(hall, bank);
        }

        // The bounce, which goes from a steel hangar's to a planet's. It is the only room in the building
        // whose ambient is not the colour of its own walls, and that is right — there is nothing in here
        // bright enough to bounce off, so what fills the shadows is the thing outside the windows.
        // And it climbs a long way, because the room he is walking into is white.
        //
        // Every other space in this film is dark steel under lamps and wants almost no bounce; this one is
        // moulded composite with a planet outside it, and a white room under a single directional light
        // with no shadowing anywhere is flat unless something fills the side that is not facing the star.
        // The ambient is the only term in this renderer that can, it has no position, and it costs no
        // slot — which is the whole reason the last room in the building can afford to be the bright one.
        var crossed = Ramp(seconds, Handover - 4f, 9f);
        hall.Ambient(0.016f + 0.038f * crossed, 0.008f + 0.016f * crossed);

        // The way out. A ramp off the clock like every other door in the film, so seeking into the middle
        // of the approach shows a door that is halfway open.
        room.Way.Open(Ramp(seconds, Opens, Door.Opening));

        // The engine room, held exactly where chapter 6 left it. Every one of these is the last value that
        // chapter wrote, restated rather than remembered — a room that carried state across a chapter
        // boundary would be the first thing in this film that could not be seeked into.
        var bench = 1f - Ramp(seconds, Douse, 5f);

        room.High[0].Dim(0f);
        room.High[1].Dim(0f);
        room.Bay.Dim(0f);
        room.Fill.Dim(0f);
        room.Task.Dim(0.48f * bench);

        room.Backlight(bench);
        room.Print(0f);
        room.Grid(0f);
        room.Schematic(bench > 0.01f);

        room.Seat(1f, 1f, 1f);
        room.Plug(1f);
        room.Live(1f);

        // The fan does not stop accelerating when the chapter does. It is a closed form in the time since
        // the power came on, so carrying it across a chapter boundary is arithmetic and not state: by the
        // end of this chapter it has been running for seventy-three seconds and is a blur, which is what a
        // machine somebody fixed an hour ago looks like.
        room.Fan(Repair.Length - Repair.Power + seconds, 1f);
        room.Panel.Update(hall.Scene, Repair.Fill);
        room.Machines(Preceding + seconds, 1f, 1f);

        // Outside. The planet turns, the station's bay lamps chase, and the escort flies the only
        // trajectory in the chapter.
        gallery.Turn(Preceding + seconds);
        gallery.Base(Preceding + seconds);
        gallery.Trim(Ramp(seconds, 12f, 4f));
        gallery.Readouts(Preceding + seconds, Ramp(seconds, 12f, 4f));
        gallery.Hologram(Preceding + seconds);
        gallery.Escort(Ramp(seconds, Approach, Settled - Approach));

        // Relay Nine on the run in, the star at full, and an empty lane. All three are asserted rather
        // than assumed, and all three are asserted because of the chapter after next: that one moves the
        // station, makes it fourteen times the size, puts a building between this room and the sun for
        // half a minute, and fills the window with traffic. A run-in that only ever read what was out
        // there would show a different sky depending on whether anybody had seeked forward to the morning
        // and come back. Every chapter in this film restates the world instead of remembering it — this
        // is the last set to need it and the easiest set to have forgotten.
        gallery.Approach();
        gallery.Star(1f);
        lane.Show(0f);

        // The two things on a ship that depend on where the camera ended up. The chapter has the camera
        // because the chapter owns the walk — the walk is a pure function of the time, so asking it where
        // he is standing is free and needs nothing threaded through the hall.
        gallery.Watch(Walk.At(seconds).Eye);

        hall.Scene.Invalidate();
    }

    public override string? Caption(float seconds) => seconds switch
    {
        < 14f => "That is all. Nothing else back there needs me tonight",

        // Eighteen seconds of nothing, over the door opening, the bench going dark, the hand-over to one
        // star and the first sight of the planet. The soundtrack makes the reveal by subtraction and the
        // captions have to agree with it: a line here is a man talking through the only thing in nine
        // minutes that does not need him to.
        //
        // Nothing about the lamp, the report, Hana, the exhibition or the day shift belongs in this
        // chapter. The note is over. What is left is a window.
        < 32f => null,
        < 44f => "There is a short way back to my bed. I take the long way",

        // The word "ship", once, here, and nowhere before it — rule 6 of the plan, and the only line in
        // the film that says where this is.
        < 54f => "Nine hundred nights on this ship. I stopped here every night",

        // The escort settles at 56, in front of the planet, holding. It is one of ours, which is what
        // makes it a question rather than a threat: nothing was scheduled to arrive tonight, and the cut
        // into Contact is the answer.
        _ => "And a ship, coming in and stopping. Nobody told me it was coming"
    };

    /// <summary>
    /// The last hand-over in the film, and it is the shortest list any room has asked for.
    ///
    /// Bank 0 is what chapter 6 finished on — the bench lamp and three of the indicator card's six — held
    /// unchanged across the boundary so nothing flickers at the cut that is not a cut. Bank 1 is one
    /// <see cref="DirectionalLight"/>.
    ///
    /// One light, for a room fifteen metres long with a planet in it. The antechamber opened the film by
    /// lighting a cube with a single bulb and arguing that emission is free; nine minutes later the same
    /// claim is being made by a hull, a station, an escort under power and four windows, on a budget of a
    /// quarter of what every other room in the building has spent.
    /// </summary>
    private void Spend(Hall hall, int bank)
    {
        if (bank == 0)
            hall.Use(room.Task.Light, room.Panel.Lights[0], room.Panel.Lights[1], room.Panel.Lights[2]);
        else
            hall.Use(gallery.Sunlight);
    }
}
