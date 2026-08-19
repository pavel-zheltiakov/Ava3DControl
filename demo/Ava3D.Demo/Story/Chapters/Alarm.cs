using System.Numerics;

namespace Ava3D.Demo.Story;

/// <summary>
/// Chapter 8. Out of the lounge and up twenty-one metres of corridor with seven turning alarms in it, to a
/// door that stays shut.
///
/// It is the first chapter with a reason in it. Everything before this happened because it was the next
/// room: he walked into the rotunda because the antechamber had one door, and into the gallery because the
/// rotunda had one way out. Here something is going, and it is going in a direction, and he follows it —
/// which is a different verb from the one the first five rooms used, and it is the reason this chapter is
/// the shortest in the film: a man who is hurrying is not somewhere for very long.
///
/// <b>The lights are the exhibit and the plot at once.</b> Seven beacons, four slots, and the four are
/// handed from beacon to beacon as he walks — the same claim the colonnade was going to make with a bank of
/// lamps down twenty-eight metres of nothing, made here by something that has a reason to be turning. The
/// hand-over is not faded and does not need to be; see <see cref="Corridor.Alarm"/>.
///
/// Everything else in here is unlit. The lenses, the beams and the runway strips along the skirting are
/// emission and additive blending and no light slot at all, which is what let the alarm start eighty
/// seconds ago, in the previous chapter, while every lamp in the building was busy with a mirror ball.
/// </summary>
internal sealed class Alarm(ScreenRoom lounge, Corridor corridor) : Chapter
{
    /// <summary>
    /// When the film clock reaches this chapter.
    ///
    /// The beacons have been turning since the middle of chapter 4 and they must not jump when the chapter
    /// changes, so both chapters drive them from the same clock — chapter 4 from its own seconds and this
    /// one from its own plus everything before it. It is the one number in the film that has to be kept in
    /// step by hand, and the alternative is handing every chapter a film clock it has no other use for.
    /// </summary>
    private const float Preceding = Screens.Length;

    /// <summary>
    /// How long the chapter runs. A constant as well as a property for the same reason
    /// <see cref="Screens.Length"/> is one: chapter 6 keeps the beacons turning and has to know how far this
    /// one got.
    ///
    /// <b>Sixteen, and it used to be thirty-eight.</b> Nineteen metres in thirty-eight seconds is half a
    /// metre a second, which is not a man who has just been told that when that light turns he goes — it is
    /// a man on a tour of a corridor. He now covers it at one and three quarter metres a second, which is a
    /// hurry rather than a run, arrives at the door at eleven and a half, and has four and a half seconds
    /// there to notice it is shut. The captions were rewritten to be read at that speed rather than at the
    /// old one; see <see cref="Caption"/>.
    /// </summary>
    public const float Length = 16f;

    public override string Title => "The alarm";

    public override float Duration => Length;

    /// <summary>
    /// Through the doorway and straight up the middle without stopping, with the head following the beacons.
    ///
    /// The first waypoint is the last waypoint of chapter 4 — same eye, same aim — because the two are one
    /// shot, as every join in this film is except the one into <c>Contact</c>.
    ///
    /// The aim is the next beacon rather than the end of the corridor, and that is the whole of what makes
    /// it read as following rather than as walking. They alternate sides, so his head goes left, right, left
    /// as he goes up — which is what a person does in a corridor that is flashing at them and is not
    /// something a camera aimed down the centre line could ever show.
    ///
    /// It leans toward each one rather than arriving at it — see <see cref="Corridor.Toward"/>, which is
    /// where the number is and why. Aimed at the lens itself this walk swung seventy degrees a beacon and
    /// looked at the ceiling doing it; a glance is a fraction of the way there, and the fraction is what
    /// turns five head turns into five glances without moving a single waypoint.
    ///
    /// <b>The five middle waypoints are two point eight metres apart and one point six seconds apart, and
    /// that is not decoration.</b> This walk used to stop at every one of them — see <see cref="Walk.At"/>,
    /// which smoothstepped each segment and therefore brought him to a halt at both ends of all nine.
    /// The easing now looks at its neighbours and goes straight through where he is walking on both sides,
    /// and equal spacing in both distance and time is what makes "straight through" mean at one speed: he
    /// sets off, reaches one and three quarter metres a second, holds it up the whole corridor and puts it
    /// down at the door. The waypoints are here so his head can find the next lamp, and now that is all they
    /// do.
    /// </summary>
    public override Walk Walk { get; } = new(
        new Step(0f, ScreenRoom.Exit + new Vector3(0f, 0f, -1.5f), Corridor.Ahead(3.5f)),

        // Out of the lounge and through the opening, getting up to speed. The one segment that eases, and it
        // eases at this end only — it hands over to the next at the speed the next is travelling.
        new Step(1.8f, Corridor.Ahead(1.2f), Corridor.Toward(0)),

        // Up the corridor. Each waypoint puts the beacon he is looking at a metre and a fifth in front of
        // him, on the opposite side from the last one, so the flash he is walking into keeps changing
        // shoulder. He never looks at one as an object; there is no time and that is the point of the
        // chapter.
        new Step(3.4f, Corridor.Ahead(4.0f), Corridor.Toward(1)),
        new Step(5.0f, Corridor.Ahead(6.8f), Corridor.Toward(2)),
        new Step(6.6f, Corridor.Ahead(9.6f), Corridor.Toward(3)),
        new Step(8.2f, Corridor.Ahead(12.4f), Corridor.Toward(4)),

        // And the door, from the last third of the run. It has been in shot since the first frame of the
        // chapter — a corridor shows you its own end, which is the one thing no other room in this building
        // does — and this is where it stops being scenery.
        new Step(9.8f, Corridor.Ahead(15.2f), Corridor.Gateway),

        // Three and a half metres short, not one and a half. He stopped a metre and a half off it first and
        // the last frame of the chapter was a door filling the whole picture with the nearest beacon behind
        // his shoulder — a black rectangle with a blue line down it. Three and a half puts the bulkhead,
        // the frame, both pilots and the last alarm over it in one shot, which is the frame this chapter
        // has been walking toward.
        new Step(11.4f, Corridor.Ahead(17.6f), Corridor.Gateway),
        new Step(16f, Corridor.Ahead(17.6f), Corridor.Gateway));

    public override void Enter(Hall hall)
    {
        // Both rooms, for the whole chapter, and the lounge is for the mouse rather than for the walk. Rule
        // 4 says a viewer can look wherever they like without being able to break the film, and a doorway
        // with nothing behind it renders as Scene.Background — so turning round eight metres up this
        // corridor would put a rectangle of absolute black where the room he just left is. It costs a dark
        // room drawn behind him, which is what the gallery pays for the same reason.
        hall.Occupy(Deck.ScreensRoom, Deck.CorridorRoom);

        // The lounge, shut down. Its four warm lamps have been at zero since the middle of chapter 4 and
        // its four coloured ones are about to lose their slots, but the alcove's dots are unlit and
        // additive — they are added to the frame rather than lit in it, so nothing that happens to the
        // light list turns them off. A room that keeps throwing dots after its lights have gone is the
        // exact price of the thing chapter 4 spent eighty seconds proving, and it has to be paid explicitly.
        foreach (var lamp in lounge.Warm)
            lamp.Dim(0f);

        lounge.Blackout();

        // One hand-over, at the top of the chapter, and then nothing. The four lights belong to the
        // corridor and never change identity; what changes is where they are standing.
        hall.Use(corridor.Lights);

        // Cold and nearly nothing. The bounce in a steel corridor lit by red lamps is a red corridor, and
        // the temptation is to tint this to match — which flattens it, because then every surface is
        // already the colour the lamps are about to make it. Leaving the fill cold is what makes the sweep
        // read as light arriving.
        hall.Ambient(0.017f, 0.009f);

        corridor.Gate.Open(0f);
    }

    /// <summary>
    /// How far down the corridor the air goes, and what it goes to.
    ///
    /// Named <c>Haze</c> and <c>Gone</c> rather than near and far, because <see cref="Chapter.Near"/> and
    /// <see cref="Chapter.Far"/> are this chapter's clip planes and mean something else entirely — one
    /// pair is what the camera can see and the other is what it can see <i>through</i>.
    ///
    /// <b>This is the one room in the building with fog in it, and it is the only one where fog is not
    /// decoration.</b> Everything before it is six to fourteen metres across, and over that distance fog is
    /// a tint on the far wall; this is twenty-one metres of two-metre passage with seven turning beacons
    /// down it, and the question the whole chapter asks is how far away the next one is. Air is how a
    /// picture answers that.
    ///
    /// <b>Two metres to twenty, and the first pass said five to forty-five.</b> Those are reasonable
    /// numbers for a room and wrong for this one, and the reason is worth writing down: fog is a function
    /// of how far away a thing <i>is</i>, and by the middle of this chapter he has walked most of the
    /// corridor — the door he is looking at is seven metres off, not twenty-one. At five to forty-five that
    /// is a fog factor of eight hundredths and the measured difference over a whole frame was fifteen
    /// levels, which is nothing three times over. At two to twenty the far end of the corridor is fully
    /// hazed when he steps into it and clear by the time he reaches it, which is what air does and is the
    /// whole of the effect.
    /// The colour is a dark warm red — the beacons' own, taken right down — and it is <i>brighter</i> than
    /// the far end of this corridor was, which is the whole of why it can be seen at all. Fog against a
    /// black room is arithmetic nobody can look at: it darkens what is already dark and nothing happens.
    /// What air does in a passage like this is lift the far end off black, and the amount it lifts it by is
    /// the distance.
    ///
    /// It is also why <see cref="Hall.Air"/> takes the colour that comes out rather than the one that goes
    /// in — see <see cref="Scene.FogColor"/>, which mixes after the tone map for exactly that reason. That
    /// is worth knowing before choosing the number: a fog colour of a tenth is a tenth <i>of what you see</i>
    /// and not a tenth of a linear value, so the first pass here was five hundredths, arrived at by
    /// reasoning about light, and it came out as fifteen levels of difference across a whole frame. A
    /// quarter is what a haze looks like.
    ///
    /// It also does something no lamp in this film can: the beams now have a medium. Seven shafts of
    /// nested cones going round a corridor with clear air in them are seven pieces of coloured glass; the
    /// same shafts with a third of a fog between him and the far one are light going through something.
    /// </summary>
    private const float Haze = 2f;

    private const float Gone = 20f;

    /// <summary>
    /// What the camera stops down to, and how long it takes.
    ///
    /// He comes out of a lit lounge into an unlit corridor, and an eye does that in about two seconds. It
    /// is <see cref="Scene.Exposure"/> rather than a lamp because it is not a lamp: nothing in either room
    /// changes brightness, and what changes is the camera. Eighty-two hundredths is a stop and a third —
    /// enough that the first second of the corridor is darker than the last second of the lounge, which is
    /// what walking into a dark room is, and not so much that the beacons stop reading.
    /// </summary>
    private const float Stop = 0.82f;

    public override void Update(Hall hall, float seconds)
    {
        // The visitor's own position, taken from the walk rather than tracked. The chapter owns the walk
        // and the walk is a pure function of the second, so asking it where he is costs one interpolation
        // and cannot disagree with the camera — which is the whole reason nothing in this film integrates.
        var eye = Walk.At(seconds).Eye;

        corridor.Alarm(eye, Preceding + seconds, 1f);

        // The door does not open, and this is the version of that line to keep. It was a placeholder
        // once — the engine room was not built and a door opening onto nothing would have shown him
        // Scene.Background at the end of a forty-second walk toward it — and by the time the room landed
        // the shut door had become the best thing in the chapter. It is the one door in the building he
        // walks up to and does not go through, and the last caption is a man noticing that. Chapter 6
        // ramps it open a second and a half in; see Repair.Update.
        corridor.Gate.Open(0f);

        // The air, and the stop closing over the first two seconds of it. Both are asserted every frame
        // rather than once in Enter, because both have to be right at any second the contents can jump
        // into — and because Hall.Clear runs on the way in, so a chapter that set its air once would be a
        // chapter whose air was cleared and never asked for again.
        hall.Air(Haze, Gone, new Vector3(0.26f, 0.115f, 0.085f), 1f - (1f - Stop) * Ramp(seconds, 0f, 2f));

        hall.Scene.Invalidate();
    }

    /// <summary>
    /// Four lines, and every one of them shorter than it was.
    ///
    /// The chapter is sixteen seconds where it was thirty-eight, so each caption has about four rather than
    /// about ten — and a caption that cannot be finished is worse than one that was never written. What came
    /// out was the second half of each sentence, which in all three cases was the half that repeated the
    /// first: "Not a test. Not a fire" says one thing twice, and a man in a corridor at half past four in the
    /// morning does not have time to say it twice.
    ///
    /// The last one is unchanged and did not need touching. It was already six words, and it is the only
    /// door in the building he walks up to and does not go through — the only caption in the film that is a
    /// question without asking one. It comes up as he stops, and holds for the four and a half seconds he
    /// stands there.
    /// </summary>
    public override string? Caption(float seconds) => seconds switch
    {
        < 4f => "An alarm at the far end. Not a test",
        < 7.5f => "Rule five. When that light turns, you go",
        < 11.4f => "Fourth this year. The first three were nothing",
        _ => "This door is closed. That is new"
    };
}
