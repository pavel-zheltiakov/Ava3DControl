using System.Numerics;

namespace Ava3D.Demo.Story;

/// <summary>
/// Chapter 3. Out of the round room into a long one, past a chart of forty-nine materials and a case of
/// twelve, round a drum with five maps on it, and up to five metals reflecting the corridor he just walked.
///
/// The chapter is a single line down the middle of the gallery with two stops in it, and the interesting
/// decision is what happens at the first one: he stops walking and only his head turns. A chart is read the
/// way a page is read — square-on, eye moving, body still — and a camera that arcs past it while it reads
/// is a camera that has turned a table of numbers into scenery. The walk and the aim are interpolated
/// separately, so standing still while looking somewhere else costs nothing but two waypoints with the same
/// position in them.
///
/// It is also the first chapter whose room lights itself with an image. Every other room in the film gets
/// two colours and a horizon from <see cref="Hall.Ambient(float, float)"/>; this one gets a probe baked out
/// of its own walls, because the exhibit at the far end is five metals and a metal has nothing to show but
/// what it is standing in. See <see cref="Probe"/> for what that costs, which is one traced image and no
/// library feature at all.
/// </summary>
internal sealed class MaterialWall(Rotunda rotunda, Gallery gallery, ScreenRoom lounge) : Chapter
{
    /// <summary>When the powered door starts to part. Public because the soundtrack puts the motor on it —
    /// it is the first powered door in the film and it opened in silence for a long time.</summary>
    public const float Opens = 2f;

    /// <summary>When he is through the rotunda's north door and the round room can be let go of.</summary>
    private const float Doorstep = 9f;

    /// <summary>When the far end takes the slots — and when the room beyond the exit is put up behind its
    /// doorway, so the way out is a dark room rather than a rectangle of background.</summary>
    private const float FarEnd = 41f;

    private int _bank = -1;

    public override string Title => "The material wall";

    public override float Duration => 55f;

    /// <summary>
    /// Straight up the middle, and it begins exactly where chapter 2 stopped — the two are consecutive
    /// seconds of one shot, and the only cut this film is allowed is the one into <c>Contact</c>.
    ///
    /// Three stops, each of them the same position written twice. That is what a stop is here: the eye
    /// interpolates on to the next thing while the feet do not move, and the walk picks up again from where
    /// it was left. It is the only way to read a chart, and it is free, because the position and the aim
    /// are interpolated separately.
    ///
    /// Every stop is a little over three metres from what it is looking at, and that number was arrived at
    /// by being wrong first. The first pass stood him one and a quarter metres from the drum and one and
    /// nine tenths from the row of metals — close enough that a metre-wide exhibit filled the frame and the
    /// room it was in disappeared, which is the same mistake as standing with your nose against a painting
    /// and is much easier to make when the person standing there is three numbers.
    /// </summary>
    public override Walk Walk { get; } = new(
        new Step(0f, Rotunda.Inside(50f, 2.5f), Rotunda.Exit),
        new Step(5f, new Vector3(9.3f, Deck.Eye, 9.4f), new Vector3(9f, 1.6f, 12.6f)),

        // Through the door and into the room, still looking up it — the chart is off to one side and does
        // not come into shot until he is far enough in for the bay to open.
        new Step(10f, Gallery.At(-5.9f, 0.25f), new Vector3(9.6f, 1.55f, 14f)),
        new Step(14f, Gallery.At(-4.6f, -0.1f), Gallery.Chart),

        new Step(19f, Gallery.At(-3.4f, -0.35f), Gallery.Chart),
        new Step(24f, Gallery.At(-3.4f, -0.35f), Gallery.Chart),

        // A step and a half sideways and a hundred and eighty degrees of head. The case is directly behind
        // where he was standing to read the chart, which is what makes it impossible for the two of them to
        // be in one frame.
        new Step(28f, Gallery.At(-3.35f, 0.15f), Gallery.Case),
        new Step(32f, Gallery.At(-3.35f, 0.15f), Gallery.Case),

        new Step(36f, Gallery.At(-1.6f, -0.3f), Gallery.Drum),
        new Step(40f, Gallery.At(-1.6f, -0.3f), Gallery.Drum),

        new Step(45f, Gallery.At(1.9f, -0.25f), Gallery.Row),
        new Step(49f, Gallery.At(2.5f, -0.2f), Gallery.Row),

        // And the way out, which is in the wall beside him rather than at the end of the room. From the
        // entrance it is a doorway seen almost edge-on from thirteen metres, which is a slot four
        // centimetres wide behind a reveal — invisible without being hidden by anything. From here it is
        // the only thing in front of him.
        new Step(52f, Gallery.At(3.9f, -1f), Gallery.Exit),
        new Step(55f, ScreenRoom.Entrance + new Vector3(-0.9f, 0f, 0f), ScreenRoom.Sitting));

    public override void Enter(Hall hall)
    {
        _bank = -1;

        // The room's own probe, for the whole chapter. It is an image rather than a hemisphere and it is
        // therefore also the ambient — one environment, doing both jobs, which is what an environment is.
        hall.Ambient(gallery.Environment);

        foreach (var lamp in gallery.All)
            lamp.Dim(0f);

        gallery.Gate.Open(0f);

        // The two rotunda lamps that do not survive the doorway. They are dropped from the list here rather
        // than faded, because they are behind him from the first frame of this chapter and a lamp nobody
        // can see does not need three seconds to go out — but their bulbs would still be glowing on the
        // ceiling of a room he can still see into, so they have to be told.
        rotunda.Lanterns[0].Dim(0f);
        rotunda.Lanterns[1].Dim(0f);

        // And the lounge's alcove off. This chapter holds that room open through its doorway for the whole
        // of its fifty-five seconds without driving anything in it, and the alcove's dots are the one thing
        // in the building that stays lit on its own — see ScreenRoom.Blackout for what that looked like.
        lounge.Blackout();
    }

    public override void Update(Hall hall, float seconds)
    {
        var bank = seconds < Doorstep ? 0 : seconds < FarEnd ? 1 : 2;

        if (bank != _bank)
        {
            _bank = bank;
            Spend(hall, bank);
        }

        // The door hears him coming. It has been shut for the whole of chapter 2 — a lit blue line across
        // a dark round room — and it comes apart in the middle two seconds before he reaches it, which is
        // the exact moment this stops being a museum and neither the walk nor the caption says so.
        //
        // Two and a half seconds of travel, starting at two: he is at the opening at about seven, so it is
        // finished and standing still well before he walks through it. A door that is still moving as the
        // camera passes it is a door the camera is waiting for.
        gallery.Gate.Open(Ramp(seconds, Opens, Door.Opening));

        // The rotunda goes out behind him while he is still in its doorway, which is the one place in this
        // film where a room is switched off in shot. It works because he is walking away from it: what the
        // frame contains is the doorway getting darker while the corridor ahead gets brighter, and the eye
        // reads that as moving rather than as switching.
        rotunda.Centre.Dim(1f - Ramp(seconds, 3f, 3f));
        rotunda.Lanterns[2].Dim(1f - Ramp(seconds, 6f, 3f));

        // Six lamps and four slots, so the two at the entrance are at zero before the two at the far end
        // are asked for. Every fade straddles the swap it belongs to.
        var behind = 1f - Ramp(seconds, FarEnd - 4f, 4f);

        gallery.Entry.Dim(Ramp(seconds, 0.5f, 3f) * behind);
        gallery.OverChart.Dim(Ramp(seconds, 2.5f, 3.5f) * behind);
        gallery.OverCase.Dim(Ramp(seconds, Doorstep, 3f));
        gallery.OverDrum.Dim(Ramp(seconds, 26f, 4f));
        gallery.OverRow.Dim(Ramp(seconds, FarEnd, 3f));
        gallery.ByDoor.Dim(Ramp(seconds, FarEnd + 1f, 3f));

        // The drum turns on its own scene's clock, with the story's scene handed to it. A chapter that
        // reimplemented the animation would be a chapter that could disagree with the file it is showing.
        gallery.Panels.Update(hall.Scene, seconds);

        hall.Scene.Invalidate();
    }

    public override string? Caption(float seconds) => seconds switch
    {
        < 18f => "Rule three. The glass cases have no alarm. The doors have an alarm",

        // A memory, handed over as though it were operational information — with the shelf coordinates,
        // which is the joke and is also the note starting to become something else.
        < 38f => "One plate has the colour of the yard where I grew up. Second row, fourth one",

        // Over the chrome sphere at the far end, which is showing him the whole room. The room that is
        // about to be emptied.
        _ => "All of this is unloaded at Relay Nine. You get an empty room. And my bed"
    };

    /// <summary>
    /// The four lights for a stretch of the walk, and which rooms are standing while it runs.
    ///
    /// <b>Both neighbours stand for the whole chapter</b>, unlit, and neither is for the walk — they are
    /// for the mouse. Rule 4 says a viewer can look wherever they like without being able to break the
    /// film, and a doorway with nothing behind it renders as <see cref="Scene.Background"/>: turn round
    /// halfway up this gallery and the door he came in by would be a rectangle of absolute black. The
    /// rotunda used to be dropped at the doorstep, which was right when that door was a hole and became
    /// wrong the moment it became a powered door somebody might want to look back at.
    ///
    /// The screen room is up for the same reason and one better one besides. A doorway with nothing behind
    /// it renders as the background, and from the entrance this one is a hard black slot forty centimetres
    /// wide at the far end of a lit corridor, which is the first thing the eye goes to in a frame that is
    /// meant to be about a wall of materials.
    /// Nothing of the room behind it can be seen at any point before he turns into it: at that angle the
    /// reveal is a metre and a half of wall across a one-and-a-fifth-metre opening, so the doorway is shut
    /// by its own thickness. It costs one dark room drawn for fifty seconds and it removes the only hole in
    /// the picture.
    ///
    /// It is also where this chapter and rule 2 have their argument, and rule 2 loses on purpose. A long
    /// gallery entered at one end shows you its whole length, so from the door the case is lit, the drum is
    /// turning and the metals are catching what light reaches them — three exhibits in one frame, where the
    /// rotunda was built so you could never see more than two. The plan asks for exactly that room, in
    /// those words, and it is right to: these are not three exhibits, they are one shading model shown four
    /// ways, and a corridor that hid its own far end would be a corridor with a door in the middle of it.
    /// </summary>
    private void Spend(Hall hall, int bank)
    {
        switch (bank)
        {
            case 0:
                hall.Occupy(Deck.RotundaRoom, Deck.MaterialsRoom, Deck.ScreensRoom);
                hall.Use(
                    rotunda.Centre.Light, rotunda.Lanterns[2].Light,
                    gallery.Entry.Light, gallery.OverChart.Light);
                break;

            case 1:
                hall.Occupy(Deck.RotundaRoom, Deck.MaterialsRoom, Deck.ScreensRoom);
                hall.Use(
                    gallery.Entry.Light, gallery.OverChart.Light,
                    gallery.OverCase.Light, gallery.OverDrum.Light);
                break;

            default:
                hall.Occupy(Deck.RotundaRoom, Deck.MaterialsRoom, Deck.ScreensRoom);
                hall.Use(
                    gallery.OverCase.Light, gallery.OverDrum.Light,
                    gallery.OverRow.Light, gallery.ByDoor.Light);
                break;
        }
    }
}
