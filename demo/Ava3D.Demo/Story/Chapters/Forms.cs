using System.Numerics;

namespace Ava3D.Demo.Story;

/// <summary>
/// Chapter 2. Out of the antechamber, round a corner, and into a round room with six niches in it.
///
/// This is the first chapter that crosses a threshold, so it is the first one that has to hand its lights
/// over rather than switch them. Four slots exist and three rooms are involved: the antechamber he is
/// leaving, the passage, and the rotunda. The answer is three sets of four, swapped at two moments — and
/// each swap is covered by a fade that puts the outgoing lamp at zero before it leaves the list, because a
/// light removed while it is still contributing goes out in one frame and reads as a fault. The swaps
/// themselves happen at the two turns, where the room being left is behind him.
///
/// The exhibits move the way they move on their own: the arm on the first turntable and the pairs on the
/// centre table are driven by their own scenes' <c>Update</c>, with the story's scene passed in. Not out of
/// thrift — a chapter reimplementing an exhibit's animation is an exhibit that can drift from itself, and
/// then the standalone file and the film disagree about a thing they are supposed to be two views of.
/// </summary>
internal sealed class Forms : Chapter
{
    /// <summary>When he is through the antechamber's door, and when he is through the rotunda's.</summary>
    private const float Doorstep = 6f;

    private const float Threshold = 16f;

    private readonly Antechamber _antechamber;
    private readonly Passage _passage;
    private readonly Rotunda _rotunda;
    private readonly Gallery _gallery;

    /// <summary>Which four lights are spending the slots. Rebuilt only when it changes, and derived from
    /// the time so that seeking backwards puts it back.</summary>
    private int _bank = -1;

    public Forms(Antechamber antechamber, Passage passage, Rotunda rotunda, Gallery gallery)
    {
        _antechamber = antechamber;
        _passage = passage;
        _rotunda = rotunda;
        _gallery = gallery;

        // Written here rather than as a field initialiser because it aims at where the exhibits actually
        // ended up, and the room only knows that once it has stood them there. The alternative is a table
        // of heights that reads about right for all six and is right for none of them.
        Walk = new Walk(
            new Step(0f, new Vector3(0f, Deck.Eye, 1.4f), new Vector3(0f, 1.5f, 4.5f)),
            new Step(5f, new Vector3(0f, Deck.Eye, 3.9f), new Vector3(0f, 1.55f, 6.9f)),

            // The turn. He is still walking north when his head starts round to the east, which is how a
            // person takes a corner and is why the corner is the only place in the passage for a lamp.
            new Step(10f, new Vector3(0f, Deck.Eye, 5.75f), new Vector3(2.6f, 1.55f, 6.15f)),
            new Step(14f, new Vector3(2.4f, Deck.Eye, 6f), new Vector3(5.4f, 1.5f, 5.7f)),
            new Step(18f, new Vector3(5.2f, Deck.Eye, 5.85f), new Vector3(7.4f, 1.45f, 4.6f)),

            new Step(22f, Rotunda.Standing(0), rotunda.Focus[0]),
            new Step(26f, Rotunda.FacingArm, rotunda.ArmFocus),
            new Step(30f, Rotunda.Standing(1), rotunda.Focus[1]),
            new Step(34f, Rotunda.Standing(2), rotunda.Focus[2]),
            new Step(38f, Rotunda.Standing(3), rotunda.Focus[3]),

            // In to the table, which is the only stop in the chapter and the only frame in the room with
            // two exhibits in it — a comparison has to be in one frame or it is not a comparison.
            new Step(42f, Rotunda.Inside(315f, 2.3f), Rotunda.Table),
            new Step(46f, Rotunda.Inside(322f, 1.95f), Rotunda.Table),

            new Step(50f, Rotunda.Standing(4), rotunda.Focus[4]),
            new Step(54f, Rotunda.Standing(5), rotunda.Focus[5]),

            // And the way out, which has been behind him for the whole walk. Two steps rather than one:
            // ninety degrees of head turn in a single segment sweeps across four metres of blank wall, and
            // a second of nothing at the end of a chapter reads as the film having lost him.
            new Step(56f, Rotunda.Inside(20f, 2.4f), Rotunda.Inside(62f, 4.0f)),
            new Step(58f, Rotunda.Inside(50f, 2.5f), Rotunda.Exit));
    }

    public override string Title => "Forms";

    public override float Duration => 58f;

    /// <summary>
    /// Out through the north door, right at the corner, and then two hundred and seventy degrees round the
    /// inside of the rotunda with a detour to the middle.
    ///
    /// It starts exactly where chapter 1 stopped. That is not a nicety: the two are consecutive seconds of
    /// one continuous shot, and a walk that began a step from where the last one ended would be a cut —
    /// the only cut this film is allowed is the one into <c>Contact</c>.
    ///
    /// From the first niche on, where he is and what he is looking at come apart: he walks along a circle
    /// and looks outwards from it, which is a head turning while a body keeps going. Interpolating the eye
    /// and the target separately is what makes that free — see <see cref="Story.Walk"/>.
    /// </summary>
    public override Walk Walk { get; }

    public override void Enter(Hall hall)
    {
        _bank = -1;

        _antechamber.Spot.Dim(0f);
        _rotunda.Centre.Dim(0f);

        foreach (var lamp in _rotunda.Lanterns)
            lamp.Dim(0f);

        // The gallery is standing behind the rotunda's north doorway for the last stretch of this chapter,
        // which is what stops the way out being a rectangle of background — but it is not lit, and a room
        // that is not lit has to have its bulbs told as well as its lights. This matters only when the film
        // is seeked backwards out of chapter 3, which is the whole reason a chapter's Enter puts the
        // building into a state rather than assuming the one before it left it there.
        foreach (var lamp in _gallery.All)
            lamp.Dim(0f);

        // And shut, for the same reason. What closes the rotunda's north opening for the whole of this
        // chapter is a powered door with a lit line down the middle of it, seen from across the room and
        // never remarked on — it is simply there, at the end, from the moment the houselights reach that
        // far. Chapter 3 is the one that walks up to it.
        _gallery.Gate.Open(0f);
    }

    public override void Update(Hall hall, float seconds)
    {
        var bank = seconds < Doorstep ? 0 : seconds < Threshold ? 1 : 2;

        if (bank != _bank)
        {
            _bank = bank;
            Spend(hall, bank);
        }

        // Every fade straddles the swap it belongs to: two seconds down before a lamp leaves the list, and
        // up again from the moment one joins it. A lamp that is at zero when it goes has no frame in which
        // its going is visible, which is the whole trick and the only thing standing between a hand-over
        // and a flicker.
        var leaving = 1f - Ramp(seconds, Doorstep - 2f, 2f);

        foreach (var lamp in _antechamber.House)
            lamp.Dim(leaving);

        // Already at full — chapter 1 brought it up when it handed the plinth's slot over. It comes down
        // as he crosses into the rotunda, which is the last thing that room's slot does.
        _passage.Lamp.Dim(1f - Ramp(seconds, Threshold - 2f, 2f));

        _rotunda.Centre.Dim(Ramp(seconds, Doorstep, 3.5f));
        _rotunda.Lanterns[0].Dim(Ramp(seconds, Doorstep + 0.5f, 3.5f));
        _rotunda.Lanterns[1].Dim(Ramp(seconds, Doorstep + 1f, 3.5f));
        _rotunda.Lanterns[2].Dim(Ramp(seconds, Threshold, 2.5f));

        // A round room has more wall bouncing light back into it than a square one, and it is taller. Both
        // say the same thing about the ambient term, and it arrives as he does.
        //
        // The ground half is raised a little more than the sky half, which is backwards for a room and
        // right for this one: a niche is a metre and a quarter of wall in shadow, no lamp reaches into it
        // from the side, and the environment's ground term is the only thing standing in for the bounce
        // off its own floor.
        var open = Ramp(seconds, Doorstep, 8f);
        hall.Ambient(0.030f + 0.016f * open, 0.016f + 0.018f * open);

        // Six tables and the arm's, all turning, none of them together. In step they would read as one
        // mechanism with seven ends; a sixty-degree offset each makes them tables that happen to share a
        // room.
        for (var i = 0; i < _rotunda.Tables.Length; i++)
            _rotunda.Tables[i].RotationDegrees = new Vector3(0f, seconds * 11f + i * 60f, 0f);

        _rotunda.ArmTable.RotationDegrees = new Vector3(0f, seconds * 9f, 0f);

        _rotunda.Arm.Update(hall.Scene, seconds);
        _rotunda.Pairs.Update(hall.Scene, seconds);

        hall.Scene.Invalidate();
    }

    public override string? Caption(float seconds) => seconds switch
    {
        < 8f => "Rule two. There are only four lamps for this whole floor. They follow you",
        < 24f => "You see the shapes one at a time. The floor was built that way",

        // The first of the night's three late things, and the only one nobody but him could see. It is a
        // caption and not a change to ArmTable above, deliberately: half a second on a table turning at
        // nine degrees a second is four and a half degrees, which no viewer can catch and which a man who
        // has watched it nine hundred times can. Animating it would make the claim checkable and turn a
        // character note into a bug report.
        < 38f => "That turning display is always on time. Tonight it is half a second late",

        // And the note cracks for the first time, caused by the thing that is wrong rather than by
        // nostalgia — he is telling his replacement about a display that is four degrees out.
        < 50f => "I used to walk past it. Now I stand and watch",
        _ => "That is not part of the four hundred words. I am writing it anyway"
    };

    /// <summary>
    /// The four lights for a stretch of the walk, and which rooms are standing while it runs.
    ///
    /// Three rooms exist at the widest point and that is not a breach of the rule — the rule is that no two
    /// <i>exhibits</i> share a frame, and the passage has nothing in it. What it costs is a dozen boxes
    /// drawn while he is in the antechamber's doorway, which is what buys the right angle that makes the
    /// rotunda invisible until he is inside it.
    /// </summary>
    private void Spend(Hall hall, int bank)
    {
        switch (bank)
        {
            case 0:
                hall.Occupy(Deck.AntechamberRoom, Deck.ThresholdRoom);
                hall.Use(
                    _antechamber.House[0].Light, _antechamber.House[1].Light, _antechamber.House[2].Light,
                    _passage.Lamp.Light);
                break;

            case 1:
                hall.Occupy(Deck.AntechamberRoom, Deck.ThresholdRoom, Deck.RotundaRoom);
                hall.Use(
                    _passage.Lamp.Light, _rotunda.Centre.Light,
                    _rotunda.Lanterns[0].Light, _rotunda.Lanterns[1].Light);
                break;

            default:
                // And the gallery, dark, on the far side of the exit. It replaces the blind stub this room
                // used to carry for the same job, and it is better than the stub was for a reason worth
                // having: the next chapter starts in that doorway, so what is behind it has to be the room
                // he is about to walk into rather than something that looks like it.
                hall.Occupy(Deck.ThresholdRoom, Deck.RotundaRoom, Deck.MaterialsRoom);
                hall.Use(
                    _rotunda.Centre.Light,
                    _rotunda.Lanterns[0].Light, _rotunda.Lanterns[1].Light, _rotunda.Lanterns[2].Light);
                break;
        }
    }
}
