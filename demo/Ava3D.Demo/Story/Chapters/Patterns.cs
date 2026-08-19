using System.Numerics;

namespace Ava3D.Demo.Story;

/// <summary>
/// Chapter 5. Twelve metres of workshop with four exhibits down it, and a door in the north wall.
///
/// <b>It is the only chapter in the film with four stops in one room.</b> The gallery has three and is
/// half again as long; this one is wide rather than long and the exhibits alternate walls, so the walk is
/// a zig-zag and every stop faces the way the last one had its back to. That is what makes four stops
/// affordable: consecutive exhibits are a hundred and eighty degrees apart, so no frame in the chapter has
/// two of them in it, and the room needs no internal walls to say so.
///
/// The four are in the order they build on each other, which is also east to west, which is also the
/// direction he is already walking: colour with no image in it, one image read many ways, the two images
/// the library builds for itself, and a picture that was rendered rather than drawn. The last is the one
/// the room is for — everything else in this building is a thing being drawn thirty times a second, and
/// the print is a thing that was drawn once, at build time, with no window anywhere.
///
/// <b>It used to end in a corridor and now it ends at its own door.</b> The last thirteen seconds were the
/// link — two metres west, a right angle, five metres south, and the lounge — and none of that is on this
/// route any more: the planetarium is north of here, the way out moved from the west wall to the north one,
/// and the corridor it used to lead to is now something chapter 6 walks at the other end of the dome. What
/// is left is the four exhibits and seven seconds of crossing the room to the door, which is what a chapter
/// about a room ought to have been all along.
/// </summary>
internal sealed class Patterns(Studio studio, PatternShop shop, Planetarium dome) : Chapter
{
    /// <summary>How long it runs, as a constant the contents table can add up.</summary>
    public const float Length = 63f;

    /// <summary>When he turns away from the print and starts across the room for the door.</summary>
    private const float Turns = 56f;

    /// <summary>When the lamp inside the door he came in by hands its slot to the print's, and when the
    /// room's own lamps start handing theirs to the way out and the dome's.</summary>
    private const float Inside = 24f;

    private const float Handover = 45f;

    private int _bank = -1;

    public override string Title => "The pattern shop";

    public override float Duration => Length;

    /// <summary>
    /// In at the south-east corner, four stops west down the room, and then a diagonal back across it to
    /// the door in the north wall.
    ///
    /// It starts exactly where chapter 4 stopped, in the studio's north doorway, because the two are
    /// consecutive seconds of one shot.
    ///
    /// Every stop is written twice — the same position with two different times — which is what a stop is
    /// in this film: the feet do not move and the eye interpolates on to the next thing, and then the walk
    /// picks up from where it was left. Three metres or so from each exhibit, except the table, which is a
    /// table and is leaned over from one and a third.
    /// </summary>
    public override Walk Walk { get; } = new(
        new Step(0f, Studio.At(3.4f, 1.2f), PatternShop.Entrance),
        new Step(4f, PatternShop.At(4.2f, -2.4f), PatternShop.Exhibit(0)),

        new Step(9f, PatternShop.At(3.5f, 0.2f), PatternShop.Exhibit(0)),
        new Step(14f, PatternShop.At(3.5f, 0.2f), PatternShop.Exhibit(0)),

        new Step(19f, PatternShop.At(1.6f, -0.2f), PatternShop.Exhibit(1)),
        new Step(23f, PatternShop.At(0.5f, -0.4f), PatternShop.Exhibit(1)),
        new Step(28f, PatternShop.At(0.5f, -0.4f), PatternShop.Exhibit(1)),

        new Step(33f, PatternShop.At(-1.4f, 0f), PatternShop.Exhibit(2)),
        new Step(37f, PatternShop.At(-2.4f, -0.4f), PatternShop.Exhibit(2)),
        new Step(42f, PatternShop.At(-2.4f, -0.4f), PatternShop.Exhibit(2)),

        new Step(46f, PatternShop.At(-3.6f, -0.4f), PatternShop.Exhibit(3)),
        new Step(50f, PatternShop.At(-4.4f, -0.8f), PatternShop.Exhibit(3)),
        new Step(54f, PatternShop.At(-4.4f, -0.8f), PatternShop.Exhibit(3)),

        // Out, and it is the one exit in the building that crosses the room it is leaving. The print is on
        // the south wall at the west end and the door is in the north wall four and a half metres east of
        // it, so the last seven seconds are a walk back over his own floor with all four exhibits behind
        // him — which is the only frame in the chapter that shows the room as a room.
        //
        // <b>Three steps, and the shape of them is the third exhibit.</b> The straight line between the
        // print and the door goes through the texture kit's stand, which is two metres six of stone
        // standing at knee height across the middle of that diagonal — <c>Ground.Audit</c> had forty
        // samples of a man inside it and his waist six hundred millimetres in. So he goes along the south
        // side of the room first and turns north once he is past it, which is what anybody walking round a
        // table does and reads as nothing at all.
        // <b>And the head goes before the feet, in two turns rather than one.</b> The print is on the south
        // wall and the door is behind his right shoulder, which is a hundred and forty degrees of turn; done
        // in the segment he sets off on, the easing concentrates it and it peaks at eighty-nine degrees a
        // second, which is the fastest anything in the film turns. So he turns his head to look down the
        // room first, standing still, and only then walks — two turns of seventy, at sixty-seven, which is
        // what chapter 3 already does at its own worst moment.
        new Step(56f, PatternShop.At(-4.4f, -0.8f), PatternShop.At(-1f, -0.8f)),

        new Step(58.5f, PatternShop.At(-2.6f, -0.2f), PatternShop.Exit),
        new Step(61f, PatternShop.At(-0.6f, 1.2f), PatternShop.Exit),
        new Step(Length, PatternShop.Exit - new Vector3(0f, 0f, 0.6f), Planetarium.At(100f, 3.4f)));

    public override void Enter(Hall hall)
    {
        _bank = -1;

        hall.Ambient(0.034f, 0.020f);

        foreach (var lamp in shop.All)
            lamp.Dim(0f);

        foreach (var lamp in studio.All)
            lamp.Dim(0f);

        foreach (var lamp in dome.All)
            lamp.Dim(0f);

        // And the dome's show off outright, which is a line this chapter used to spend on the opposite.
        // It held the sky at a tenth so that a viewer who took the mouse and looked through the north
        // doorway saw something rather than a black hole — and what that room needs through a doorway is
        // not a dim sky, it is a lit white ceiling, which is what it now has without anybody driving it.
        // See Planetarium, where the dome became a surface instead of a picture.
        dome.Show.Off();
    }

    public override void Update(Hall hall, float seconds)
    {
        var bank =
            seconds < Inside ? 0 :
            seconds < Handover ? 1 :
            seconds < Turns ? 2 : 3;

        if (bank != _bank)
        {
            _bank = bank;
            Spend(hall, bank);
        }

        // The studio's last lamp goes out behind him while he is still in the doorway. It is already at a
        // tenth — chapter 4 finished in the dark — so what this is really doing is taking the matcaps'
        // room away rather than turning anything off.
        studio.Way.Dim(0.1f * (1f - Ramp(seconds, 1f, 3f)));

        // Four exhibit lamps for four slots, arriving as he reaches each one and leaving once he has
        // finished with it. Every fade straddles the swap it belongs to: a lamp is at zero on the frame
        // the list is rebuilt, which is the whole trick and the only thing standing between a hand-over
        // and a flicker.
        shop.Entry.Dim(1f - Ramp(seconds, 20f, 4f));
        shop.OverColours.Dim(Ramp(seconds, 0f, 3f) * (1f - Ramp(seconds, 20f, 5f)));
        shop.OverWindows.Dim(Ramp(seconds, 3f, 4f) * (1f - Ramp(seconds, 41f, 4f)));
        shop.OverKit.Dim(Ramp(seconds, 12f, 4f) * (1f - Ramp(seconds, 51f, 4f)));
        shop.OverPrint.Dim(Ramp(seconds, Inside, 4f) * (1f - Ramp(seconds, 57f, 4f)));
        shop.Way.Dim(Ramp(seconds, 44f, 4f));

        // The dome's cove nearest the doorway, lit while he is still at the print, so the opening he turns
        // and walks at has something warm behind it. A door onto black is a painted rectangle, and there is
        // no fifth slot to light a room somebody has finished with — which is why the print's lamp is what
        // pays for it.
        dome.Cove[1].Dim(Ramp(seconds, 52f, 5f));
        dome.Cove[0].Dim(Ramp(seconds, Turns, 5f));

        // The bounce climbs, and the chapter hands the room over at the number chapter 6 opens on rather
        // than at its own — a chapter that arrives at a room already lit must arrive at the light it is
        // going to have. It goes <i>up</i> now and used to go down, and the planetarium's walls are why:
        // that room is lined with cloth at a tenth albedo instead of plaster at a third, so it needs half
        // again the bounce to read as the same brightness. See Stars.Enter.
        var leaving = Ramp(seconds, 54f, 9f);
        hall.Ambient(0.034f + 0.016f * leaving, 0.020f + 0.006f * leaving);

        // The UV exhibit runs on its own scene's clock, with the story's scene handed to it — the drum in
        // the gallery is driven the same way and for the same reason. A chapter that reimplemented the
        // animation would be a chapter that could disagree with the file it is showing.
        shop.Windows.Update(hall.Scene, seconds);

        hall.Scene.Invalidate();
    }

    public override string? Caption(float seconds) => seconds switch
    {
        // No rule here either. See Ink.Caption: there are six of them and they belong to the six rooms
        // with something operational to say.
        < 14f => "Nothing on this floor was photographed. All of it was worked out",
        < 28f => "One picture, and each of those reads a different corner of it",
        < 42f => "The weather on that table is four lines of arithmetic",

        // At the print, which is the exhibit the room is for.
        < 56f => "And that one is a photograph. Of this. Taken by the thing drawing this",

        // And out, on the running joke, which is the note refusing to be four hundred words.
        _ => "They asked for four hundred words. I have not reached the dome yet"
    };

    /// <summary>
    /// The four lights for a stretch of the walk, and which rooms are standing while it runs.
    ///
    /// <b>Four exhibits and four slots</b>, which is the first room in the building where those two
    /// numbers are the same and nothing has to be handed over while he is looking at something. The
    /// hand-overs are all at the end: the way out, and then the dome's first cove, two swaps in the last
    /// sixteen seconds while he is walking rather than standing.
    ///
    /// Both neighbours stand through the first half and neither is for the walk — they are for the mouse.
    /// Rule 4 says a viewer can look wherever they like without being able to break the film, and a
    /// doorway with nothing behind it renders as <see cref="Scene.Background"/>.
    /// </summary>
    private void Spend(Hall hall, int bank)
    {
        switch (bank)
        {
            case 0:
                hall.Occupy(Deck.StudioRoom, Deck.PatternRoom, Deck.PlanetariumRoom);
                hall.Use(
                    shop.Entry.Light, shop.OverColours.Light,
                    shop.OverWindows.Light, shop.OverKit.Light);
                break;

            case 1:
                hall.Occupy(Deck.StudioRoom, Deck.PatternRoom, Deck.PlanetariumRoom);
                hall.Use(
                    shop.OverColours.Light, shop.OverWindows.Light,
                    shop.OverKit.Light, shop.OverPrint.Light);
                break;

            case 2:
                hall.Occupy(Deck.PatternRoom, Deck.PlanetariumRoom);
                hall.Use(
                    shop.OverKit.Light, shop.OverPrint.Light,
                    shop.Way.Light, dome.Cove[1].Light);
                break;

            default:
                hall.Occupy(Deck.PatternRoom, Deck.PlanetariumRoom);
                hall.Use(
                    shop.OverPrint.Light, shop.Way.Light,
                    dome.Cove[1].Light, dome.Cove[0].Light);
                break;
        }
    }
}
