using System.Numerics;
using Ava3D.Demo.Scenes.Contact;

namespace Ava3D.Demo.Story;

/// <summary>
/// The whole exhibition: the building, standing, and the chapters that walk through it.
///
/// Built once. Every room in the plan is put up here whether or not the visitor ever reaches it, and then
/// all but one of them is switched off — which sounds wasteful and is the opposite. The rooms are boxes
/// and planes and cost almost nothing to build; what would cost is rebuilding them at every threshold, and
/// what that would cost is not milliseconds but continuity, because a room built twice can differ from
/// itself and the difference reads as the world jumping.
///
/// The chapters are a list in order, and where one ends the next begins. There are no gaps and no overlaps,
/// so a second of film maps to exactly one chapter and one moment inside it — which is what
/// <see cref="At"/> returns and what makes the contents able to jump to a feature rather than to a scene.
/// </summary>
internal sealed class Film
{
    private readonly float[] _starts;

    public Film()
    {
        Hall = new Hall();

        var antechamber = new Antechamber(Hall);
        var passage = new Passage(Hall);
        var rotunda = new Rotunda(Hall);
        var gallery = new Gallery(Hall);
        var screens = new ScreenRoom(Hall);
        var corridor = new Corridor(Hall);
        var engine = new EngineRoom(Hall);
        var illuminator = new Illuminator(Hall);

        // The lane outside the gallery's window, which belongs to the last chapter and is hung off the
        // gallery's own "outside" node. It is built with the room and hidden by every chapter but one —
        // the same arrangement every room in the building has, applied to seven ships.
        var lane = new Traffic(illuminator);

        // And the world outside, which is a room in the same sense the others are and in no other sense.
        // It is mounted at the deck's origin because its own coordinates are absolute — the station is
        // four kilometres from the middle of it and the sky is a sphere two and a half million units
        // across — so anywhere but the origin would move a solar system to make room for a corridor.
        //
        // It is built here with everything else rather than at the cut, and that costs the film's first
        // frame the most expensive build in the demo: three glTF models, seven plated hulls and a
        // starfield, for sixty seconds that arrive nine and a half minutes later. It is still right. The
        // alternative is building it at the cut, and the cut is the one frame in this film that cannot
        // afford to stall — a hitch anywhere else is a dropped frame and a hitch there is the joke landing
        // late.
        var outside = new ContactScene();

        Hall.Add(Deck.ContactRoom, Vector3.Zero).Children.Add(
            outside.BuildSubject() ?? throw new InvalidOperationException(
                "Contact has stopped building a subject, and the film has nowhere to cut to"));

        // The fade and the card, which belong to no room — the two chapters that use them are in two
        // different ones and one of those is a planet. See Curtain, and see Advance in StoryScene for the
        // one line that keeps it from being left up.
        Curtain = new Curtain(Hall, "Next day");

        // The list is in film order and the timings are computed from it, so appending one moves nothing
        // and inserting one moves everything after it along without a number needing to be edited
        // anywhere. The cues in Contents are the one exception, and they are a table for exactly that
        // reason.
        Chapters =
        [
            new Dark(antechamber),
            new Houselights(antechamber, passage),
            new Forms(antechamber, passage, rotunda, gallery),
            new MaterialWall(rotunda, gallery, screens),
            new Screens(screens, corridor),
            new Alarm(screens, corridor),
            new Repair(corridor, engine),
            new Outside(engine, illuminator, lane),
            new Cut(outside, Curtain),
            new Morning(illuminator, lane, Curtain)
        ];

        // And the building as it stands after the last chapter, which is nobody's chapter. It is built here
        // rather than at the hand-over because it needs every room object at once and this is the only place
        // they all exist; it does nothing at all until Open is called.
        Rounds = new Rounds(
            Hall, antechamber, passage, rotunda, gallery, screens, corridor, engine, illuminator);

        _starts = new float[Chapters.Count];

        var running = 0f;
        for (var i = 0; i < Chapters.Count; i++)
        {
            _starts[i] = running;
            running += Chapters[i].Duration;
        }

        Duration = running;
    }

    public Hall Hall { get; }

    /// <summary>The fade and the title card. One for the whole film, because there is one night in it.</summary>
    public Curtain Curtain { get; }

    /// <summary>The building once the film has finished with it: all eight rooms, the doors open, and the
    /// lights following whoever is walking rather than the clock. See <see cref="Story.Rounds"/>.</summary>
    public Rounds Rounds { get; }

    public IReadOnlyList<Chapter> Chapters { get; }

    /// <summary>The whole film, in seconds.</summary>
    public float Duration { get; }

    /// <summary>When a chapter begins, in film seconds.</summary>
    public float StartOf(int chapter) => _starts[Math.Clamp(chapter, 0, _starts.Length - 1)];

    /// <summary>
    /// Which chapter a moment of the film falls in, and how far into it.
    ///
    /// Past the end returns the last chapter at its last second rather than wrapping. The film has an
    /// ending — it is a story — and a tour that runs off the end should hold on the last frame and let the
    /// shell move on, not silently start again from the dark.
    /// </summary>
    public (int Chapter, float Into) At(float seconds)
    {
        if (seconds <= 0f)
            return (0, 0f);

        for (var i = Chapters.Count - 1; i >= 0; i--)
            if (seconds >= _starts[i])
                return (i, Math.Min(seconds - _starts[i], Chapters[i].Duration));

        return (0, 0f);
    }

    /// <summary>
    /// The same, with the last chapter's clock left running past the end of the film.
    ///
    /// <see cref="At"/> holds on the last second, which is what a tour wants and what the recorder wants —
    /// a film has an ending. It is the wrong answer for the one thing that carries on afterwards: the free
    /// walk. Everything a chapter does is a function of its own second, so a second that stops turns the
    /// whole room into a photograph — the traffic outside the glass stops in mid-air, the hologram over the
    /// table stops turning, the guide lamps stop chasing. The viewer is handed a building and finds it
    /// switched off, which is a worse ending than no ending.
    ///
    /// Only the last chapter, and only past the end: seeking into the middle of the film is unaffected, and
    /// a chapter that is not the last one still hands over to the next at its own boundary. What a chapter
    /// does with a second past its duration is its own business — see <c>Illuminator.Leave</c>, which lets
    /// the ship go on flying and stops the station receding once further away is not a difference a window
    /// can see.
    /// </summary>
    public (int Chapter, float Into) Beyond(float seconds)
    {
        var last = Chapters.Count - 1;

        return seconds > Duration ? (last, seconds - _starts[last]) : At(seconds);
    }
}
