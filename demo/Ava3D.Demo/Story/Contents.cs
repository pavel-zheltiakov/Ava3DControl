using Ava3D.Demo.Scenes;
using Ava3D.Demo.Scenes.Arcade;
using Ava3D.Demo.Scenes.Board;

namespace Ava3D.Demo.Story;

/// <summary>
/// One entry in the picker: a feature, the file that demonstrates it on its own, and — once the film has
/// reached it — the second at which it is on screen.
/// </summary>
/// <param name="Title">What the picker shows. The feature's name, not a chapter's.</param>
/// <param name="Described">A built instance, for the title, notes and panel dressing. Never rendered.</param>
/// <param name="Standalone">Builds it on a black background, one feature, nothing else in the frame.</param>
/// <param name="Cue">Where it happens in the film, or null for a feature the film has not reached yet.</param>
public sealed record ContentEntry(
    string Title, DemoScene Described, Func<DemoScene> Standalone, float? Cue);

/// <summary>
/// The table of contents: one list, and a switch that decides what picking an entry does.
///
/// The list is the same in both modes — same order, same numbers, same names — because they are the same
/// features. With the story on, picking one winds the film to the moment that feature is on screen; with
/// it off, picking one builds that feature's own scene against a black background, which is what this
/// demo has always been. The switch is a statement about presentation, not about content, and that is
/// exactly why there is one list rather than two lists that agree.
///
/// The times are a time code into the film, and they are not shown. A viewer wants a number and a name,
/// which is what the list has always given; the seconds behind each entry are how the shell gets there.
///
/// <b>While the film is being built</b>, most entries have no cue. That is a transitional state and it is
/// visible rather than hidden: an entry the film has not reached yet falls back to its standalone scene
/// even with the story switched on, so the demo is never broken and the list never has holes in it. As
/// each room lands, its features gain a cue and stop falling back. When the last room lands, the fallback
/// stops being reachable and this comment goes with it.
/// </summary>
public static class Contents
{
    /// <summary>
    /// Which features the film has reached, and when.
    ///
    /// Keyed by the scene's own type rather than by its title, because a title is prose and gets edited.
    /// The seconds are into the whole film, not into a chapter — see <see cref="Film.At"/>, which is what
    /// turns one back into the other.
    /// </summary>
    private static readonly Dictionary<Type, float> Filmed = new()
    {
        // Chapter 0 — Dark. The cube on the plinth, eight seconds in, before he steps back.
        [typeof(HelloCubeScene)] = 6f,

        // Chapter 1 — Houselights. Thirty-two seconds: three lamps up, the ambient arriving with them,
        // and the far wall in shot for the first time.
        [typeof(LightingScene)] = 32f,

        // Chapter 2 — Forms, which begins at sixty seconds and runs fifty-eight. The first niche is the
        // sphere with the turning arm bolted beside it; the middle of the run is where four of the six
        // solids have been seen; the table at the end is the two shadings side by side.
        [typeof(TransformsScene)] = 84f,
        [typeof(PrimitivesScene)] = 92f,
        [typeof(FlatShadingScene)] = 106f,

        // Chapter 3 — The material wall, which begins at a hundred and eighteen and runs fifty-five. Each
        // of these lands inside one of the chapter's three stops rather than on the walk between them,
        // which is the difference between a picker entry that arrives in front of an exhibit and one that
        // arrives on the way past it.
        [typeof(PbrChartScene)] = 139f,
        [typeof(MaterialsScene)] = 148f,
        [typeof(PbrShowcaseScene)] = 156f,
        [typeof(EnvironmentScene)] = 167f,

        // Chapter 4 — Screens, which begins at a hundred and seventy-three. Each set a few seconds after it
        // wakes, while he is still standing in front of it. They are later inside the chapter than they
        // used to be, because the chapter now spends its first thirteen seconds on a room rather than on a
        // bench: he comes in, looks at the furniture and the console, and only then starts down the line.
        //
        // These are the numbers that have to be revisited when a room before them lands, and the only
        // ones: everything else in the film is timed by the order of the chapter list. Chapter 3 arriving
        // moved all four of them along by its fifty-five seconds and changed nothing else in the film. A
        // cue that is a few seconds out is a picker entry that lands slightly early, which is worth a table
        // that can go stale — the alternative is building the whole film to ask it where something is, at
        // startup, before anything has been selected.
        [typeof(GrassBricksScene)] = 192f,
        [typeof(CoinMazeScene)] = 198f,
        [typeof(RunnerScene)] = 204f,
        [typeof(BlocksScene)] = 210f,

        // And the case on the west wall, one second into the pass along it. It is the one cue in this table
        // that points at a camera move rather than at a thing: a billboard standing still is a picture, and
        // the feature is what happens to it when the viewer moves.
        //
        // Near the start rather than in the middle, which is the opposite of where the other cues sit and
        // is deliberate. Halfway along he is square-on to the case and every angle in it is a right angle —
        // the flattest frame of the shot, and the one that proves least. A second in he is still at the
        // south end looking down the length of it: the bars are raked, the far bay is foreshortened to a
        // slot, and the row inside is standing dead square to him anyway. That is the whole argument in one
        // frame, and it is what the picker should land on.
        [typeof(SpritesScene)] = 218f,

        // Chapter 5 — the alarm, which begins at two hundred and sixty-six and runs sixteen.
        //
        // Both of these used to sit later and one of them used to be a stop: Unlit landed nine seconds in
        // while he stood under the first beacon looking up at the lens. He does not stand anywhere in this
        // corridor any more — see Alarm.Walk — so both are moments he passes through rather than moments he
        // waits at, which costs the entries nothing. What is being pointed at is a property of the room, and
        // the room is the same room at one and three quarter metres a second.
        //
        // Unlit at four seconds, with the first two beacons abreast of him. The lenses, the beams turning
        // inside them and twenty-eight strips along the skirting are all emission with no light reaching
        // them, and the frame has one lamp in it — which is the claim, and it is a better one here than in
        // the lounge because here the unlit things are the subject rather than the light source.
        //
        // Four lights at nine, mid-corridor, where the four slots have three beacons in front of him and
        // one behind and are being reassigned every frame as he walks. Later than Unlit by five seconds
        // rather than by eleven, because the chapter is less than half as long; both still land in the
        // stretch this corridor exists to show.
        [typeof(UnlitScene)] = 270f,
        [typeof(FourLightsScene)] = 275f,

        // Chapter 6 — the engine room, which begins at two hundred and eighty-two and runs a hundred and
        // one. Four of these five are the scene the film mounts at that second, running its own code on the
        // same model the picker would build. The first three are on the bench's display, at a third of full
        // size; the last is the board itself, at fifteen hundredths, lying on the mat.
        //
        // Depth bias lands with the drawing rather than on its own, and it is the same frame: a print is an
        // opaque white fill with its own edges lying exactly in it, and without the bias the outlines crawl
        // through the surface they are drawn on. The two entries are eighteen seconds apart because the
        // drawing is what you are looking at for both of them and there is no other way to point at a
        // property.
        [typeof(DraftsmanScene)] = 300f,
        [typeof(DepthBiasScene)] = 302f,
        [typeof(WireframeScene)] = 306f,
        [typeof(InspectorScene)] = 320f,
        [typeof(IndicatorsScene)] = 374f,

        // Chapter 8 — the cut, which begins at four hundred and forty-three and runs sixty. It is the one
        // entry in this table that points at the start of its chapter rather than into the middle of one,
        // because the film it cuts to has a first frame that is meant to be seen: the whole of Contact is
        // the feature, and arriving eleven seconds into it would be arriving after the establishing shot.
        [typeof(Scenes.Contact.ContactScene)] = 443f
    };

    /// <summary>Every feature, in the order the film reaches them, which is also the order they build on
    /// each other.</summary>
    public static IReadOnlyList<ContentEntry> Entries { get; } = Build();

    /// <summary>Whether any of it is filmed yet. Guards the toggle against being a switch that does
    /// nothing.</summary>
    public static bool AnyFilmed => Entries.Any(e => e.Cue is not null);

    private static ContentEntry[] Build()
    {
        var entries = new ContentEntry[DemoCatalog.Scenes.Count];

        for (var i = 0; i < DemoCatalog.Scenes.Count; i++)
        {
            var factory = DemoCatalog.Scenes[i];
            var described = factory();

            entries[i] = new ContentEntry(
                described.Title,
                described,
                factory,
                Filmed.TryGetValue(described.GetType(), out var cue) ? cue : null);
        }

        return entries;
    }
}
