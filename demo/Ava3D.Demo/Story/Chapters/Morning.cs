using System.Numerics;

namespace Ava3D.Demo.Story;

/// <summary>
/// Chapter 9. A new day: the ship lets go of Relay Nine, goes out through its door, and he watches all of
/// it from the forward end of the window.
///
/// It is the same room as chapter 7 and the opposite chapter. That one was a man finding out where he had
/// been — a planet he had not looked at, a station he had to search for, an escort that arrived. This one
/// is a man who knows exactly where he is, standing at the end of the glass on a working morning, while a
/// lane of traffic goes about its business outside and somebody else's fight happens a quarter of a
/// kilometre away and does not concern him.
///
/// <b>It opens inside the station.</b> The first half-minute has almost no sky in it: the window is twenty
/// metres inside a docking bay forty-one deep, and what is out there is a wall of Relay Nine forty-four
/// metres off with seven frames and two rows of guide lamps running down it, two clamps holding the ship,
/// and a door across the mouth that is shut and red. That is the whole of what the chapter had been missing. The
/// old cut opened alongside — a station already astern, already shrinking — and a station that is
/// <i>already</i> going is a station nobody watched leave; there was no frame anywhere in it that said
/// where the ship had been or what it was doing, so the traffic outside read as scenery and the morning
/// read as a different film. Being inside first is what turns all of that into a departure.
///
/// <b>And it goes out through the door.</b> The gate clears from red to green at nine seconds, the clamps
/// fold back at ten, and at sixteen the ship starts to move — at walking pace, on thrusters, because
/// nobody uses a drive inside somebody else's station. The mouth reaches the window at about twenty-seven
/// seconds and takes five to pass it: eighty-four metres of lit collar, six lamps chasing round it, and a
/// curtain of green light washing across every pane in the room. See <see cref="Illuminator.Leave"/> for
/// what moves and <see cref="Illuminator.Gate"/> for the door, both of which are Contact's own — the same
/// station model and the same gate the battle is cleared through, seen from the one side that film never
/// showed.
///
/// <b>Then it goes backwards.</b> Once clear, the ship comes round thirty-four degrees onto its course and
/// the station falls astern and swings into the aft end of the glass, from a wall the frame cannot hold to
/// three hundred metres of it to a shape on the quarter. That swing is the only thing in the film that
/// moves far enough to be a scale in itself, and now it starts from the one place a scale can be read
/// from, which is inside.
///
/// <b>Nothing in it happens to him.</b> The fight is out of somebody else's window, the freighters are
/// somebody else's cargo, and the escort ahead has been out there all night. That is deliberate and it is
/// what earns the ending: a film that finished on a crisis would have to resolve it, and this one finishes
/// by handing the controls over, which only reads as a gift if the room is a place rather than a
/// situation.
///
/// <b>And then it stops telling him things.</b> The last ten seconds have no event in them at all — the
/// lane thins, the escort takes station, and the caption changes from narration to an instruction. See
/// <see cref="StoryScene"/> for what happens on the frame after this chapter's last one.
/// </summary>
internal sealed class Morning(Illuminator gallery, Traffic lane, Curtain curtain) : Chapter
{
    /// <summary>Where the film clock stands when this chapter begins, so the things that were already
    /// turning carry on turning rather than restarting. The cut does not stop the planet.</summary>
    private const float Preceding =
        Screens.Length + Alarm.Length + Repair.Length + Outside.Length + ContactLength;

    private const float ContactLength = Scenes.Contact.ContactScene.Length;

    /// <summary>
    /// When the black goes, and when the two words over it do.
    ///
    /// The only title card in the film, and the only fade. Chapter 8 finishes on black — see
    /// <see cref="Cut"/> — so the two chapters meet with the curtain fully down on both sides of the
    /// boundary and the join is invisible; what makes it a night rather than a cut is that the card is up
    /// for three and a half seconds before anything is revealed underneath it.
    /// </summary>
    private const float Reveal = 3.6f;

    /// <summary>When the bay is cleared: the gate goes from red to green over half a second, which is
    /// where Contact has it too. Seven seconds ahead of the ship moving, because a door is a permission and
    /// a permission that arrives at the same moment as the thing it permits is not one. Public because the
    /// soundtrack puts a motor on it, the way it does the three doors inside the building.</summary>
    public const float Cleared = 9f;

    private int _bank = -1;

    public override string Title => "New day";

    public override float Duration => 84f;

    /// <summary>
    /// How far past the mouth of the bay the window is, in metres — negative while it is still inside.
    ///
    /// It is the room's own number, passed on rather than recomputed, and the score is the only reader.
    /// Every level in <c>Soundtrack.Afterwards</c> hangs off this instead of off seconds of its own, so
    /// the bay going quiet is tied to the bay actually being behind him: retime the departure and the
    /// sound follows without anybody having to remember that it should. Safe to read because the film runs
    /// a chapter's <see cref="Update"/> for a second before it asks the score about that second.
    /// </summary>
    public float Past => gallery.Past;

    /// <summary>The same nineteen hundred metres chapter 7 asked for, and for the same starfield. It is
    /// also what bounds how far the station is allowed to get — see <see cref="Illuminator.Leave"/>, which
    /// leaves it thirteen hundred metres astern at the last frame and therefore still a station.</summary>
    public override float Far => 1_900f;

    /// <summary>
    /// He barely moves, and that is the chapter.
    ///
    /// Chapter 7 walked the length of the gallery because it was showing him a room; this one has already
    /// shown him the room, so it stands him at the forward end and lets the window do the work. What
    /// movement there is is a man shifting his weight at a rail over a minute and a half — a metre and a
    /// half along the glass, a lean in and a lean back — which is the difference between a camera that is
    /// held and a camera that is planted.
    ///
    /// <b>Almost none of the look points here are used.</b> For the first fifty seconds the camera is
    /// tracking something that is moving — see <see cref="Shoot"/> — and these are what it blends out of
    /// at each end, so they are written as the nearest fixed thing to whatever is being watched rather
    /// than as the shot. The last four are the shot: the lane, the shooting, and the course.
    /// </summary>
    public override Walk Walk { get; } = new(
        // In the bow bay, looking down the far wall of the berth toward the door at the end of it —
        // fifteen degrees forward of the beam and forty-six metres out. Not at the door: eighty-four
        // metres of it seen from twenty is the whole frame and is a flat coloured rectangle. See
        // Illuminator.Berthside, which is the correction rather than the first idea.
        new Step(0f, Illuminator.Along(-5.9f, 2.2f), Illuminator.Berthside),
        new Step(10f, Illuminator.Along(-5.9f, 2.15f), Illuminator.Berthside),

        // A step back off the glass as the ship starts to move, which is what anybody does when something
        // the size of a wall starts sliding past a window a few metres away.
        new Step(18f, Illuminator.Along(-5.4f, 2.0f, 1.68f), Illuminator.Berthside),
        new Step(26f, Illuminator.Along(-4.8f, 1.95f, 1.7f), Illuminator.Berthside),

        // And round to the aft end of the room as the mouth goes past him and keeps going. Sixteen seconds
        // of it dropping away, which is the longest single hold in the film. Nothing else in the chapter
        // needs to happen while it does.
        new Step(34f, Illuminator.Along(-4.6f, 2.0f, 1.7f), Illuminator.Quarter),
        new Step(44f, Illuminator.Along(-5.2f, 2.05f, 1.68f), Illuminator.Quarter),

        // Forward again, to the traffic. A freighter comes down the lane past the window at two hundred
        // metres with three containers on its back, which is what this ship looked like yesterday.
        new Step(52f, Illuminator.Along(-5.7f, 2.1f, 1.66f), Illuminator.Lane),
        new Step(58f, Illuminator.Along(-5.9f, 2.15f, 1.66f), Illuminator.Lane),

        // The shooting, forward and high, just past the corner post. He watches it the way anybody
        // watches weather.
        new Step(64f, Illuminator.Along(-6.0f, 2.15f, 1.72f), Illuminator.Fight),
        new Step(70f, Illuminator.Along(-6.0f, 2.18f, 1.72f), Illuminator.Fight),

        // And back to the course, which is where the chapter finishes and where the film hands over. The
        // last two waypoints are a fifth of a metre apart and ten seconds long, so the picture is very
        // nearly still by the time the caption changes — and it finishes looking down the bay, which is
        // where the first thing anybody does with the controls is walk.
        new Step(74f, Illuminator.Along(-5.9f, 2.15f, 1.7f), Illuminator.Ahead),
        new Step(84f, Illuminator.Along(-5.8f, 2.1f, 1.66f), Illuminator.Ahead));

    /// <summary>
    /// The walk, with two things added: one live target, and the curtain.
    ///
    /// A <see cref="Walk"/> is a list of fixed places, which is right for everything in this film that is
    /// looked at, because everything in this film that is looked at stays still. Nothing in the first
    /// fifty seconds of this one does. The mouth of the bay comes past the glass at four metres a second
    /// at a range of twenty-four, and the station goes from there to thirteen hundred metres and swings
    /// thirty-four degrees while it does; a fixed look point would slide off the first inside half a
    /// second and off the second inside ten.
    ///
    /// So there is one point, and it is the room's — <see cref="Illuminator.Watching"/>, which is the
    /// doorway while the doorway is still a doorway and eases onto the whole station once it is small
    /// enough to be one thing. One rather than two, because a camera can only be pointed at one and the
    /// change between them is not a cut: it is a man whose eye stays where it was while the thing under it
    /// stops being a wall with a hole in it and becomes a station.
    ///
    /// It is still a pure function of the clock, and that is the only reason any of it is allowed. That
    /// point comes out of this chapter's own <see cref="Update"/>, which the story runs for this second
    /// before it asks for a camera; so seeking to the twenty-eighth second and playing to it give the same
    /// frame, which is the rule.
    /// </summary>
    public override void Shoot(Camera camera, float seconds)
    {
        var pose = Walk.At(seconds);

        // Onto it over three seconds and off it over six, so the head turns rather than snaps — and so
        // that the last third of the chapter is back on the walk's own fixed points, which is where the
        // film needs to be for the hand-over.
        var watching = Ramp(seconds, 1.2f, 3f) * (1f - Ramp(seconds, 48f, 6f));

        camera.Roll = 0f;
        camera.LookFrom(pose.Eye, Vector3.Lerp(pose.Look, gallery.Watching, watching));

        // Black, and then two words on it. Both are asserted every frame rather than switched on, because
        // the film clears the curtain before every chapter aims — so a seek out of this chapter cannot
        // leave the screen dark, and a seek into the middle of it arrives with the black already gone.
        curtain.Draw(camera,
            1f - Ramp(seconds, Reveal, 3.2f),
            Ramp(seconds, 0.5f, 1.2f) * (1f - Ramp(seconds, Reveal - 0.4f, 1.2f)));
    }

    public override void Enter(Hall hall)
    {
        _bank = -1;

        // One room, and it is the first time since chapter 0 that the film has occupied a single one.
        // There is nothing behind him worth keeping in the scene: the engine room is two doors back and
        // the door between is shut.
        hall.Occupy(Deck.IlluminatorRoom);
    }

    public override void Update(Hall hall, float seconds)
    {
        // Everything outside, and every one of these is asserted rather than left where the last chapter
        // put it. Two of them move a long way in this chapter — see Illuminator.Leave — so a chapter that
        // only ever read them would show a different sky depending on where anybody had seeked from.
        gallery.Turn(Preceding + seconds);
        gallery.Base(Preceding + seconds);
        gallery.Hologram(Preceding + seconds);

        // The berth, the station, the doorway and the light in it. Leave writes Past — how far past the
        // mouth the window is — and everything below hangs off that rather than off a second set of
        // seconds, so the light in the room and the sound in it cannot drift out of step with the picture.
        gallery.Leave(seconds);
        gallery.Gate(Ramp(seconds, Cleared, 0.5f), seconds);

        var outboard = Ramp(gallery.Past, -4f, 16f);

        var bank = gallery.Past < -10f ? 0 : seconds < 46f ? 1 : 2;

        if (bank != _bank)
        {
            _bank = bank;
            Spend(hall, bank);
        }

        // The star, which is switched off for the first half minute of the chapter because he is inside a
        // building. A directional light has infinite reach and this renderer casts no shadows, so the one
        // thing being in a docking bay has to mean is this line.
        gallery.Star(outboard);

        // Brighter while he is inside than after, and the reason is the walls rather than the hour. A bay
        // is a box thirty-four metres across with lamps down it and light coming back off every face;
        // open space has one star in it and nothing at all to bounce off, which is why the room settles to
        // what chapter 7 finished on the moment it is out.
        hall.Ambient(0.052f - 0.020f * (1f - outboard), 0.022f - 0.008f * (1f - outboard));

        // Everything in the room that is bright and is not a lamp: the trim round both windows, the deck
        // strips, the coves, the coffer panels, the reflections painted on the floor and the hologram
        // over the plot table. Full, and held there — chapter 7 brought them up over four seconds because
        // he was walking into the room for the first time, and this morning he has been in it for
        // twenty minutes.
        gallery.Trim(1f);

        // And what the screens are doing, which is the one thing in this room that is not a function of
        // where the ship is. It carries on past the end of the chapter without being told to, which is what
        // the free walk needs: after the hand-over this Update is still the one running, and a gallery whose
        // instruments stopped the second the film did would be the first thing anybody noticed.
        gallery.Readouts(Preceding + seconds, 1f);

        // The escort, on point where it has been all night — dead ahead through the bow port, which from
        // inside the bay means through the open mouth of it. Chapter 7 spent twenty seconds bringing this
        // same hull onto station; this one simply states that it is there, which is the whole difference
        // between an arrival and a morning.
        gallery.Lead(seconds);

        // And the lane, which does not exist until the ship is out of the building. It thins over the last
        // twelve seconds — not because anything leaves, but because the running lights come down, which is
        // the cheapest way a window has of saying that the busy part of the morning is over.
        lane.Show(Ramp(gallery.Past, 6f, 50f) * (1f - 0.55f * Ramp(seconds, Duration - 14f, 12f)));
        lane.Move(seconds);

        var eye = Walk.At(seconds).Eye;

        gallery.Watch(eye);
        lane.Watch(eye);

        hall.Scene.Invalidate();
    }

    public override string? Caption(float seconds) => seconds switch
    {
        // Nothing over the card. It is the one moment in the film where two things are competing for the
        // same frame, and the caption is the one that can wait.
        < Reveal + 1.4f => null,

        // What the night was for, and it is the answer to a line eight minutes ago in the material room:
        // <i>all of this is unloaded at Relay Nine, you get an empty room</i>. He is standing in the empty
        // room. Nothing else in the chapter has to explain where the ship is or why it is leaving.
        < 18f => "The hold is empty. They had it all off by two",

        // One beat before the paperwork, so the normalising has something to normalise. He was the only
        // person awake for the largest thing that has happened in nine hundred nights, and the phrase is
        // the one chapter 4 used twice about an empty room with four televisions in it.
        < 30f => "I watched all of it from here. Nobody came",

        // And then he goes back to the note, which is the only line in the film that says out loud that
        // it is resuming an interrupted thought. It is what licenses every jump after it.
        < 42f => "Where was I. Four hundred words",

        // What last night was worth, from both ends. No self-pity and no irony marked: the two halves of
        // the sentence are the whole of the film's argument about work nobody sees.
        < 54f => "The pilots who fought got an award. I completed a form for one small card",

        // Two things he never did, in a row — the second "either" is the link, and it is the only reason
        // the lamp can be raised here rather than at the window, where it interrupted the reveal.
        < 64f => "Nobody asked me if I wanted the day shift. I never told them either",
        < 73f => "And I never turned that lamp off. Not one night",

        // The last caption, and it holds through the free walk, so it has to work as an instruction to
        // somebody who has just been handed the controls. It is also the answer to the film's second
        // line: the man before him left that lamp on, he complained in writing and obeyed it for nine
        // hundred nights, and this is him asking for the same. The note was never four hundred words. It
        // was one sentence.
        _ => "Leave the lamp on over the first crate. You will write a report. Do it anyway"
    };

    /// <summary>
    /// Two lights, then four, then three.
    ///
    /// Bank 0 is the inside of the station: the berth's floods coming across the glass from a gantry
    /// thirteen metres off, and the gate. There is no star in it, because there is a station between him
    /// and the star. Bank 1 is the threshold — the same two, plus the star coming up and the escort's
    /// drive — and it is the only moment in the film that spends all four slots on a room. Bank 2 is what
    /// is left once the building is astern.
    ///
    /// <b>Every hand-over here is silent, and it is arithmetic rather than luck.</b> The two lights bank 2
    /// drops are a berth's floods and a doorway, both of which are by then a hundred and sixty metres
    /// behind the ship and outside their own range — so what is being switched off is already contributing
    /// nothing, which is the same argument every threshold in this film has made since the antechamber.
    /// </summary>
    private void Spend(Hall hall, int bank)
    {
        if (bank == 0)
            hall.Use(gallery.Dock, gallery.Portal);
        else if (bank == 1)
            hall.Use(gallery.Sunlight, gallery.Dock, gallery.Portal, gallery.Running);
        else
            hall.Use(gallery.Sunlight, gallery.Running, lane.Gunfire);
    }
}
