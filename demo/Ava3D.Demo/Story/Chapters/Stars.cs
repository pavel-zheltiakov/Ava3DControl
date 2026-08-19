using System.Numerics;

namespace Ava3D.Demo.Story;

/// <summary>
/// Chapter 6. A dome, four photographs, a seat, and eighteen seconds in the dark.
///
/// <b>It is the only chapter in the film that stops.</b> Everything else here is a walk — the argument is
/// made by arriving at things — and this one arrives at a chair. That is not a change of pace for its own
/// sake: what is being exhibited is depth in something with no surface, and a camera that walks past a
/// cloud of gas reads the parallax and stops believing in it. A camera sitting under it does not.
///
/// The chapter is in three unequal parts and the middle one is the reason for the other two. He comes in
/// through the south door and walks the east arc, which is four framed photographs of nebulae — pictures
/// somebody already took. Then he sits down, the room goes black, and the same subject is drawn in the air
/// instead. Then he gets up and leaves by the west door, which is a corridor and a lounge.
///
/// <b>The room goes black and it used to go down to six per cent.</b> The argument for keeping a little
/// was the real one every planetarium makes — aisle lights, so nobody falls over, and a scrap of scale so
/// the dome does not read as wallpaper. It was still wrong here, and the note that said so is right: a
/// show you are watching from the only chair in an empty room does not need aisle lights, and six per cent
/// of five lamps on a white ceiling is six per cent of the contrast the whole chapter exists for. So the
/// coves go to nought, the bounce goes to a thousandth, and the only light in this room between thirty-six
/// seconds and fifty-seven is a lamp three quarters of a metre from a planet with a range that cannot
/// reach anything else. See <see cref="Sky"/>.
/// </summary>
internal sealed class Stars(PatternShop shop, Planetarium dome, Link link, ScreenRoom lounge) : Chapter
{
    /// <summary>How long it runs, as a constant the contents table can add up.</summary>
    public const float Length = 72.5f;

    /// <summary>
    /// When he is in the chair and the show's own clock starts.
    ///
    /// <b>It is a beginning and it did not used to be one.</b> The sky, the gas and the planets were all
    /// periodic functions of the film's clock with no start anywhere in them, so the eighteen seconds spent
    /// sitting were a slice of something that had been running since the antechamber. That is a good
    /// property for a free walk and it is the wrong thing to show somebody who has just sat down: a
    /// planetarium is a room where the lights go out and then something <i>begins</i>. So the show is
    /// handed <c>seconds - Curtain</c> and stages itself from nought. Nothing about seeking is lost —
    /// it is still one number in and one frame out.
    /// </summary>
    public const float Curtain = 39f;

    /// <summary>And when they come back up. Eighteen seconds is what was asked for and is what a person
    /// will sit still for without a story to follow.</summary>
    private const float Houselights = Curtain + Sky.Watched;

    /// <summary>When the four slots stop being the show's and start being the corridor's, which is also
    /// when he stands up.</summary>
    private const float Leaves = 59.5f;

    private const float Ahead = 66.5f;
    private const float Doorstep = 69.5f;

    private int _bank = -1;

    public override string Title => "The dome";

    public override float Duration => Length;

    /// <summary>
    /// In at the south door, round the east arc, four stops at four photographs, into the end chair, and
    /// out of the west door into the link.
    ///
    /// <b>The way out is eleven seconds shorter than it was, and seven metres of that is the plan.</b>
    /// The note said the corridor to the lounge was too long, twice, and the second time said plainly that
    /// walking it faster was not the answer. It was not: an L between two doorways is the Manhattan
    /// distance between them, so a corridor is only ever as short as the two rooms it joins allow. The
    /// planetarium cannot move — its z is fixed by its own radius against the pattern shop's north wall —
    /// so the lounge did, seven metres north, and took the alarm corridor, the engine room and the
    /// illuminator with it. See <see cref="Deck.Screens"/>. The link is eight metres nine where it was
    /// fifteen metres nine, and it is walked at a metre and three: ten seconds of corridor instead of
    /// twenty-one.
    /// </summary>
    public override Walk Walk { get; } = new(
        new Step(0f, PatternShop.Exit - new Vector3(0f, 0f, 0.6f), Planetarium.At(100f, 3.4f)),

        // Through the door on the door's own axis, and then round to the east.
        //
        // <b>The straight line from the pattern shop's threshold to the first point in this room misses the
        // opening by thirty-five millimetres</b>, which is a camera walking through a jamb — and it misses
        // it because the two rooms' doorways are five metres apart on different walls and a line between
        // them is not a line through either. A doorway is a thing you go through square, so there is a
        // waypoint in it: <c>Ground.Audit</c> had twenty-three samples of a man inside plaster and now has
        // none.
        new Step(1.8f, Planetarium.At(270f, 4.2f), Planetarium.At(280f, 1.5f)),

        // And the first thing he does inside is look up — at a blank white dome, which is the whole of what
        // this room shows anybody who walks into it before the lights go out. That is the note being
        // answered: what was here was a tenth of a nebula already turning, and a show that has started
        // without you is not a show.
        //
        // Three waypoints round the east arc and not two, and the third one is the last chair. A chord
        // between two points on a circle passes inside it, and the row of seats reaches to two metres eight
        // at three hundred and forty degrees — which is exactly the bearing the second of these was on. So
        // the chord from two ninety-two dipped through the far end of the row and <c>Ground.Audit</c> had
        // ninety-odd samples of a camera inside a chair back on the way to the pictures.
        new Step(4.5f, Planetarium.At(292f, 3.3f), Planetarium.Overhead),
        new Step(6.2f, Planetarium.At(316f, 3.4f), Planetarium.Overhead),

        // And the eye comes down off the dome onto the first picture five seconds before he reaches it,
        // which is what seeing a picture and walking to it is. Held on the dome to the last waypoint
        // instead, the head had to come round a hundred and forty degrees in the one segment that also
        // rounds the corner of the room, and <c>Ground.Audit</c> read seventy-three degrees a second — the
        // film's own worst, matched. Split across two segments it is thirty-seven.
        new Step(8.5f, Planetarium.At(340f, 3.45f), Planetarium.Picture(3)),

        new Step(11.5f, Planetarium.At(42f, 3.4f), Planetarium.Picture(3)),
        new Step(13.5f, Planetarium.Before(3), Planetarium.Picture(3)),
        new Step(16f, Planetarium.Before(3), Planetarium.Picture(3)),

        new Step(19f, Planetarium.Before(2), Planetarium.Picture(2)),
        new Step(21.5f, Planetarium.Before(2), Planetarium.Picture(2)),

        new Step(24.5f, Planetarium.Before(1), Planetarium.Picture(1)),
        new Step(27f, Planetarium.Before(1), Planetarium.Picture(1)),

        new Step(30f, Planetarium.Before(0), Planetarium.Picture(0)),
        new Step(32.5f, Planetarium.Before(0), Planetarium.Picture(0)),

        // A step back off the last print, then along to the end of the row, and then into it.
        //
        // <b>He looks at the sky and not at the chair, and that is a fix rather than a flourish.</b> A walk
        // that aims at the seat it is about to sit in aims at a subject a metre away that it is closing on,
        // and the bearing to a subject a metre away swings as fast as the walker moves: <c>Ground.Audit</c>
        // measured two thousand degrees a second here, on a segment whose authored turn is a hundred and
        // fifty. Aiming past it — at the dome, which is where he is going to be looking anyway — costs the
        // shot nothing.
        //
        // <b>And it is two segments because the turn is a hundred and seventy degrees.</b> He is facing out
        // at a picture on the wall and has to end up facing in and up, which is very nearly about-face
        // however it is written; done in one segment the easing puts the whole of it in the middle two
        // fifths and it peaks at a hundred and twenty-six degrees a second. Split at the glance back along
        // the row of prints — which is what somebody leaving four pictures does anyway — it is two turns of
        // eighty and peaks at fifty.
        new Step(34.5f, Planetarium.At(292f, 3f), Planetarium.Picture(1)),
        new Step(37f, Planetarium.Approach, Planetarium.Overhead),
        new Step(Curtain, Planetarium.Seat, Planetarium.Zenith),

        // Eighteen seconds, sitting, and the two middle waypoints are a head turning rather than a camera
        // move. The show crosses the dome from one side to the other three times — see <c>Sky.Turns</c> —
        // and a person watching that follows it a little and then comes back. Eight hundred millimetres of
        // aim at three metres four is fourteen degrees, which is a glance and takes five seconds.
        new Step(45f, Planetarium.Seat, Planetarium.Zenith),
        new Step(49.5f, Planetarium.Seat, Planetarium.Zenith + new Vector3(0.85f, 0.10f, 0.12f)),
        new Step(53.5f, Planetarium.Seat, Planetarium.Zenith - new Vector3(0.80f, -0.06f, 0.11f)),
        new Step(Houselights, Planetarium.Seat, Planetarium.Zenith),

        // Up, and out the other side.
        new Step(Leaves, Planetarium.Approach, Planetarium.Exit),
        new Step(62f, Planetarium.At(180f, 2.6f), Link.Along(3.2f)),

        // The corner, taken the way chapter 2 takes the passage's: still walking west when his head starts
        // round to the south.
        //
        // <b>Three waypoints and not two, and the middle one is the inside of the turn.</b> Written as one
        // step from the top of the east leg to a point down the north one, the straight line between them
        // cuts the corner — and the inside corner of an L is a wall, so <c>Ground.Audit</c> had twelve
        // samples of a camera in the plaster. A corner is walked round rather than across, which costs a
        // waypoint and half a second and is what anybody carrying anything already does.
        // <b>And the eye goes round it before the feet do.</b> Aimed at the corner itself, the head has
        // eighty-six degrees to turn in the two seconds it takes to get round — and a subject less than
        // half a metre away that is being closed on swings faster than the walker moves, which
        // <c>Ground.Audit</c> read as sixty-three degrees a second, the chapter's own worst. Aimed a
        // couple of metres down the leg he is about to be in, it is two turns of forty.
        new Step(64.5f, Link.Along(5.7f, 2.2f), Link.Along(3.2f)),
        new Step(Ahead, Link.Along(5.4f, 0.3f), Link.Along(1.2f)),

        // And five metres seven of corridor at a metre and three, which is a man leaving a show rather
        // than a man being shown out.
        new Step(Doorstep, Link.Along(1.5f), ScreenRoom.Sitting),
        new Step(Length, ScreenRoom.Entrance + new Vector3(0.35f, 0f, -1.3f), ScreenRoom.Sitting));

    public override void Enter(Hall hall)
    {
        _bank = -1;

        // Half again what this room had, and the walls are why. Everything else in the building is lined
        // with plaster at a third albedo; this room is lined with cloth at a tenth and floored with carpet,
        // so the same bounce number here is a third of the room it was measured in. What it has to be is
        // whatever makes a white ceiling read as white — see Finish.Screen, which is the only light
        // coloured surface in here and is the one thing a visitor is meant to notice on the way in.
        hall.Ambient(0.050f, 0.026f);

        foreach (var lamp in shop.All)
            lamp.Dim(0f);

        foreach (var lamp in dome.All)
            lamp.Dim(0f);

        foreach (var lamp in link.All)
            lamp.Dim(0f);

        foreach (var lamp in lounge.Warm)
            lamp.Dim(0f);

        // Nothing on the ceiling, nothing in the projector. The dome is a lit surface now and needs no
        // telling; what needs telling is everything that is not.
        dome.Show.Off();
        dome.Running(0f);

        // The lounge's alcove off, which chapter 5 used to hold and this one now does — it is the chapter
        // that has the lounge standing through a doorway without driving anything in it. See
        // ScreenRoom.Blackout.
        lounge.Blackout();
    }

    public override void Update(Hall hall, float seconds)
    {
        var bank =
            seconds < 12f ? 0 :
            seconds < 41f ? 1 :
            seconds < Leaves ? 2 :
            seconds < Ahead ? 3 :
            seconds < Doorstep ? 4 : 5;

        if (bank != _bank)
        {
            _bank = bank;
            Spend(hall, bank);
        }

        // The pattern shop's last lamp goes out behind him while he is still in the doorway, the same way
        // the studio's does one chapter back. It is already the only thing lit in that room.
        shop.Way.Dim(1f - Ramp(seconds, 1f, 4f));

        // <b>How far down the house lights are is one number and everything reads it.</b> The coves, the
        // bounce, the air, the projector and the show are all this or its complement, which is what stops a
        // room that takes five seconds to go dark and two and a half to come back from being five fades
        // that disagree about when they started.
        //
        // It starts three seconds before he sits and finishes two and a half after, which is the order a
        // real one does it in — you are in your seat before it is properly dark, and the last of the fade
        // is the part nobody notices.
        var down = Ramp(seconds, Curtain - 3f, 5.5f) * (1f - Ramp(seconds, Houselights, 2.5f));

        // To nothing, and that is the note. Not a tenth, not six per cent: nothing.
        var house = 1f - down;

        // The two by the doorways are asserted at full rather than ramped, because chapter 5 already
        // brought them up through the north door and a lamp that fades in from nought at a chapter
        // boundary is a lamp that goes out for a frame first.
        dome.Cove[0].Dim(house * (1f - Ramp(seconds, 62f, 4f)));
        dome.Cove[1].Dim(house * (1f - Ramp(seconds, 62f, 4f)));
        dome.Cove[2].Dim(Ramp(seconds, 4f, 4f) * house * (1f - Ramp(seconds, 55f, 3f)));
        dome.Cove[3].Dim(Ramp(seconds, 2f, 4f) * house * (1f - Ramp(seconds, 36f, 4f)));
        dome.Cove[4].Dim(0f);

        // The show, on its own clock, which starts when he sits down. Before that it is handed a negative
        // number and a level of nought and draws nothing at all — which is what a blank screen is.
        var show = seconds - Curtain;

        dome.Show.Update(show, down);
        dome.Running(down, MathF.Max(show, 0f));

        // And the air, asserted every frame rather than at the bank changes, which every other room in the
        // building does the other way round. The reason is that this one is a fade and not a state: the
        // haze and the stop both follow `down`, and setting them once per bank would put the whole change
        // on one frame.
        //
        // <b>The haze is the exhibit and not the mood.</b> Seventy-eight additive billboards with nothing
        // between them and the dome read as seventy-eight billboards; the same seventy-eight seen through
        // two per cent of air a metre have something to be *in*, and that is the whole of what makes a
        // picture of gas read as gas rather than as a decal.
        //
        // <b>What fades is where the fog starts and not what colour it is.</b> Written the other way round
        // first — the colour multiplied by <c>down</c>, so it went to black with the house lights — and
        // black is not the absence of fog, it is fog the colour of nothing: with the lights up the room had
        // a fog that began at five metres and took everything past it to black, which is a nine-metre room
        // with no far wall in it. <see cref="Scene.FogColor"/> is where distant geometry <i>ends up</i>, so
        // the only value that means "none" is a start plane further away than anything in the room.
        hall.Air(
            3f + 45f * (1f - down),
            34f + 60f * (1f - down),
            new Vector3(0.012f, 0.015f, 0.034f),
            1f + 0.35f * down);

        // The bounce goes with them, and it goes further down than any other room in the building — a
        // thousandth, which is a fortieth of what the antechamber opens the film on. The two darkest frames
        // in this film are its first one and this one, and this one is darker.
        //
        // And then it climbs past where it started, to the lounge's, which is warmer and brighter than any
        // other room here. That second term is not decoration: chapter 7's first frame asserts 0.062, and a
        // chapter that hands a room over has to hand it over at the light it is going to have or the join
        // is a step.
        var arriving = Ramp(seconds, Doorstep - 8f, 11f);

        hall.Ambient(
            0.050f - 0.0489f * down + 0.012f * arriving,
            0.026f - 0.0254f * down + 0.004f * arriving);

        // The corridor, coming up as he stands rather than while he is still in the chair. Every other
        // threshold in this film lights the room ahead before the walk turns towards it; this one cannot,
        // because until the house lights are back the four slots are the show's and there is no fifth.
        // What covers it is the cove by the exit, which is two metres from the doorway and throws enough
        // through it that the opening is not a black rectangle for the second and a half it takes him to
        // get up.
        link.Turn.Dim(Ramp(seconds, Leaves, 3.5f) * (1f - Ramp(seconds, Length - 3f, 3f)));
        link.Run.Dim(Ramp(seconds, Ahead - 4f, 4f) * (1f - Ramp(seconds, Length - 1f, 2f)));

        // And the lounge, coming up through its own doorway in the order chapter 7 expects to find it in.
        // Its four lamps are at exactly what that chapter's first frame sets them to, which is why the join
        // has nothing in it.
        lounge.Doorway.Dim(Ramp(seconds, Doorstep - 4f, 4f));
        lounge.Lounge.Dim(Ramp(seconds, Doorstep - 2f, 4f));
        lounge.Bench[0].Dim(Ramp(seconds, Doorstep, 3.5f));
        lounge.Bench[1].Dim(Ramp(seconds, Doorstep, 3.5f) * 0.92f);

        hall.Scene.Invalidate();
    }

    public override string? Caption(float seconds) => seconds switch
    {
        // No rule here. There are six of them and they belong to the six rooms with something operational
        // to say — see Ink.Caption. This room has one chair and a ceiling.
        < 16f => "Nobody has booked the dome in eleven years. I still run it",
        < 30f => "Four pictures on that wall. Somebody went a long way to take each one",

        // In the chair, and the one caption in the film that is not about the building. It is also where
        // the running joke stops for a chapter: he has given up on the four hundred words and is watching.
        < Curtain + 6f => "Sit in the back row and let it run. That is the whole of the training",
        < Houselights => null,

        // And up, on the joke again, which is the note refusing to be written.
        _ => "Four hundred words. I have written eleven and one of them was 'the'"
    };

    /// <summary>
    /// The four lights for a stretch of the walk, and which rooms are standing while it runs.
    ///
    /// <b>Six banks and the middle one is the whole chapter.</b> From forty-one seconds to fifty-nine and
    /// a half the four slots are three coves that are switched off and a lamp three quarters of a metre
    /// from a planet. That looks wasteful and is exactly right: a slot is not a lamp, it is permission to
    /// contribute, and the three coves have to keep theirs across the show because they are the room's own
    /// lights and they come back on at the end of it — a lamp that is handed a slot while it is already
    /// bright arrives as a pop.
    ///
    /// Every swap in this list happens at a moment when the lamp being dropped is at nought and the lamp
    /// being picked up is too. That is the only rule here and it is worth more than any arrangement: a
    /// hand-over you can see is worse than a room with one light in it.
    /// </summary>
    private void Spend(Hall hall, int bank)
    {
        switch (bank)
        {
            case 0:
                hall.Occupy(Deck.PatternRoom, Deck.PlanetariumRoom, Deck.LinkRoom);
                hall.Use(
                    shop.Way.Light, dome.Cove[0].Light, dome.Cove[1].Light, dome.Cove[3].Light);
                break;

            case 1:
                hall.Occupy(Deck.PatternRoom, Deck.PlanetariumRoom, Deck.LinkRoom);
                hall.Use(
                    dome.Cove[0].Light, dome.Cove[1].Light, dome.Cove[2].Light, dome.Cove[3].Light);
                break;

            case 2:
                hall.Occupy(Deck.PlanetariumRoom, Deck.LinkRoom);
                hall.Use(
                    dome.Cove[0].Light, dome.Cove[1].Light, dome.Cove[2].Light, dome.Show.Star);
                break;

            case 3:
                hall.Occupy(Deck.PlanetariumRoom, Deck.LinkRoom, Deck.ScreensRoom);
                hall.Use(
                    dome.Cove[0].Light, dome.Cove[1].Light, link.Turn.Light, link.Run.Light);
                break;

            case 4:
                hall.Occupy(Deck.LinkRoom, Deck.ScreensRoom);
                hall.Use(
                    link.Turn.Light, link.Run.Light, lounge.Doorway.Light, lounge.Lounge.Light);
                break;

            default:
                hall.Occupy(Deck.LinkRoom, Deck.ScreensRoom);
                hall.Use(
                    lounge.Doorway.Light, lounge.Lounge.Light,
                    lounge.Bench[0].Light, lounge.Bench[1].Light);
                break;
        }
    }
}
