using System.Numerics;

namespace Ava3D.Demo.Story;

/// <summary>
/// Chapter 4. Out of the gallery's east door into a dark room with a row of cel-shaded solids along one
/// end of it and a row of matcapped ones along the other — and then somebody turns the lights off.
///
/// <b>It is the first chapter whose beat is a subtraction.</b> Everything the film has done with light so
/// far has been to bring it: three banks in chapter 1, a hand-over at every threshold, six fittings in the
/// gallery arriving one bay at a time. Here four lamps come down to nothing two thirds of the way through,
/// and what is left in the frame is the argument. The cel-shaded row goes out, because a toon surface is
/// a lit surface and quantising the response does not stop it being one. The metal sphere on the end of
/// the matcap row goes out, because a metal with nothing to reflect is black. The three matcaps do not
/// move by a pixel, and neither do the three images of them on the wall above.
///
/// The lamps do not come back. He leaves the room dark and walks toward the light coming through the next
/// doorway, which is the hand-over chapter 1 does at the plinth and chapter 2 does at the corner: there is
/// no fifth slot to light a room he has finished with.
///
/// <b>And one lamp moves, for the whole chapter.</b> A still cel-shaded sphere says nothing about where
/// its bands are — <c>ToonScene</c> says so in its own file and circles a light to prove it — so the band
/// row's fitting runs along a rail over it and the bands sweep round the solids as it goes. It is the only
/// light in the building that is not bolted where it is, and it is the only way to make that exhibit move
/// without a chapter reimplementing an exhibit.
/// </summary>
internal sealed class Ink(Gallery gallery, Studio studio, PatternShop shop) : Chapter
{
    /// <summary>How long it runs. A constant as well as a property because the chapter after it starts
    /// where this one stops and the contents table adds these up by hand.</summary>
    public const float Length = 52f;

    /// <summary>When he is through the gallery's door and the room behind him can be let go of.</summary>
    private const float Doorstep = 7f;

    /// <summary>When he turns from the bands to the palette, and when the lamps come down.</summary>
    private const float Turn = 24f;

    /// <summary>
    /// When the room goes out, and how long it takes.
    ///
    /// Three seconds rather than one. A switch is a cut and reads as a fault; three seconds is somebody
    /// pulling a dimmer down, and it is long enough for the eye to follow the band row going while the
    /// matcaps stay — which is the whole comparison and is invisible if both states arrive at once.
    /// </summary>
    private const float Douse = 33f;

    private const float Over = 3f;

    /// <summary>When the pattern shop starts to light itself behind the doorway. Nine seconds after the
    /// room goes dark, which is long enough for the dark to be the picture and not a gap.</summary>
    private const float Ahead = 42f;

    private int _bank = -1;

    public override string Title => "The studio";

    public override float Duration => Length;

    /// <summary>
    /// In through the west door, down to the band row, along it, round to the palette, and out through the
    /// north one.
    ///
    /// It starts exactly where chapter 3 stopped — same eye, same aim — because the two are consecutive
    /// seconds of one shot, as every join in this film is except the one into <c>Contact</c>.
    ///
    /// The two stops are a hundred and eighty degrees apart and that is the room's whole plan: the rows
    /// are on facing walls, so no frame in this chapter has both of them in it. It is the gallery's chart
    /// and case a room later, in a room a third the length, and it costs nothing but deciding which wall.
    ///
    /// He stops twice at the palette rather than once. The first stop is the lamps going down, which he is
    /// looking at the exhibit for; the second is his head coming up to the three images on the wall above
    /// it, which is the only thing in the chapter that explains the first.
    /// </summary>
    public override Walk Walk { get; } = new(
        new Step(0f, Studio.At(0f, -2.7f), Studio.At(-1.2f, 3f)),
        new Step(4f, Studio.At(-0.5f, -1.7f), Studio.Row),

        // Three metres and a tenth back from the row and square on to the middle of it, which is the
        // gallery's number and was arrived at here the same way it was arrived at there — by being wrong
        // first. Two metres one was the first pass, and at this lens that is a frame three and a half
        // metres wide across a row three and a half metres long: the two-band sphere on the end was
        // outside the picture, which is the one solid in the room that has to be in it.
        new Step(9f, Studio.At(-0.4f, 0f), Studio.Row),
        new Step(15f, Studio.At(-0.4f, 0f), Studio.Row),

        // A step east along the row while the lamp is still running west of him, so the bands are moving
        // in two directions at once — his and the light's.
        new Step(20f, Studio.At(-0.4f, 0.9f), Studio.Row),

        new Step(Turn, Studio.At(0f, 0.6f), Studio.Caps),
        new Step(29f, Studio.At(0.4f, -0.6f), Studio.Caps),

        // And square on to the palette at the same three metres and a tenth. The two stops are a metre and
        // three quarters apart, which is a short walk for a chapter and is what a room eight metres long
        // with an exhibit at each end has to offer: what moves here is the light, not the man.
        new Step(34f, Studio.At(0.4f, -1.55f), Studio.Caps),
        new Step(41f, Studio.At(0.4f, -1.55f), Studio.Caps),
        new Step(45f, Studio.At(0.4f, -1.55f), Studio.Wall),
        new Step(47f, Studio.At(0.4f, -1.55f), Studio.Wall),

        // Out, at a walk. Two waypoints rather than one for the four metres to the door, because a single
        // one covered it at two metres a second — which Ground.Audit reports as a number and which reads
        // as a man leaving a room at a jog. Nothing else in the film goes faster than one and three
        // quarters, and that is a corridor with an alarm in it.
        new Step(50f, Studio.At(1.8f, 0.2f), Studio.Exit),
        new Step(Length, Studio.At(3.4f, 1.2f), PatternShop.Entrance));

    public override void Enter(Hall hall)
    {
        _bank = -1;

        // The gallery's own probe goes with the gallery. It is an image of that room baked from in front
        // of the five metals, and carrying it into a dark studio would light every surface in here with a
        // reflection of somewhere else — which nothing in this room would show except the one sphere that
        // is meant to be a metal and would then be the wrong metal.
        hall.Ambient(0.030f, 0.018f);

        foreach (var lamp in studio.All)
            lamp.Dim(0f);

        foreach (var lamp in shop.All)
            lamp.Dim(0f);

        studio.Slide(-Studio.Travel);
    }

    public override void Update(Hall hall, float seconds)
    {
        var bank = seconds < Doorstep ? 0 : seconds < Ahead ? 1 : 2;

        if (bank != _bank)
        {
            _bank = bank;
            Spend(hall, bank);
        }

        // The gallery's far end goes out behind him while he is still in the doorway, which is the same
        // shot chapter 3 opened with at the rotunda's: what the frame contains is a doorway getting darker
        // while the room ahead gets brighter, and the eye reads that as moving rather than as switching.
        var behind = 1f - Ramp(seconds, 1.5f, 3.5f);

        gallery.OverRow.Dim(behind);
        gallery.ByDoor.Dim(behind);

        // The room going out. Not quite to nothing: a tenth of a lamp is what keeps the walls, the stands
        // and the floor as a room rather than as three objects in a void, and it is below the level at
        // which anything on the band row can be read. Zero was tried and is a worse picture — with nothing
        // at all in here the matcaps have no scale, and a sphere with no scale is a circle.
        var lit = 1f - Ramp(seconds, Douse, Over) * 0.9f;

        studio.Band.Dim(Ramp(seconds, 0.5f, 3f) * lit);
        studio.Palette.Dim(Ramp(seconds, Doorstep, 3.5f) * lit);
        studio.Entry.Dim(Ramp(seconds, 0.5f, 3f) * lit * (1f - Ramp(seconds, Turn, 4f)));
        studio.Way.Dim(Ramp(seconds, Turn, 4f) * lit);

        // The bounce goes with the lamps, and it has to be said separately because a hemisphere is a scene
        // property and does not know the room's fittings exist. Leaving it up is what a first pass does,
        // and what it looks like is a room where the lights went off and the walls did not.
        var bounce = 1f - Ramp(seconds, Douse, Over) * 0.82f;
        hall.Ambient(0.030f * bounce, 0.018f * bounce);

        // The lamp on its rail: one pass west to east across the whole chapter, which is slow enough that
        // nobody catches it moving and fast enough that the bands are somewhere else every time he looks
        // back. A closed form in the time, like everything else here, so seeking puts it where it belongs.
        studio.Slide(-Studio.Travel + 2f * Studio.Travel * Math.Clamp(seconds / Length, 0f, 1f));

        // And the room ahead, lighting itself behind the doorway once this one has gone dark. It is the
        // only thing in the frame for the last ten seconds that is not a matcap.
        shop.Entry.Dim(Ramp(seconds, Ahead, 4f));
        shop.OverColours.Dim(Ramp(seconds, Ahead + 2f, 4f));

        hall.Scene.Invalidate();
    }

    public override string? Caption(float seconds) => seconds switch
    {
        // No rule in this chapter or the next one. Six of them are spread across the six rooms that have
        // something operational to say, and a made-up seventh in a room about shading would be the note
        // padding itself out — which is the one thing the man writing it keeps saying he is not doing.
        < 13f => "Somebody painted these before the shading was invented",
        < 24f => "The light on them comes in steps. Two, three, five, and then honest",

        // Over the turn, before the lamps go down, so the claim is made and then demonstrated rather than
        // the other way round.
        < 33f => "These three are wearing a photograph of a lamp",

        // On the douse itself.
        < 44f => "Which is why I can turn the room off and they carry on",
        _ => "Nine hundred nights. Still the best thing on this floor"
    };

    /// <summary>
    /// The four lights for a stretch of the walk, and which rooms are standing while it runs.
    ///
    /// Three banks for a room with four lamps in it, which sounds like one too many and is not: the fourth
    /// is the gallery's, for as long as the doorway behind him has a room on the other side of it, and the
    /// fourth after that is the pattern shop's, for the ten seconds this room has nothing left to spend a
    /// slot on. A room with its lights out does not need four of them.
    ///
    /// Both neighbours stand for the whole chapter, unlit, and neither is for the walk — they are for the
    /// mouse. Rule 4 says a viewer can look wherever they like without being able to break the film, and a
    /// doorway with nothing behind it renders as <see cref="Scene.Background"/>, which in a room this dark
    /// is the one hole a black wall cannot hide.
    /// </summary>
    private void Spend(Hall hall, int bank)
    {
        hall.Occupy(Deck.MaterialsRoom, Deck.StudioRoom, Deck.PatternRoom);

        hall.Use(bank switch
        {
            0 =>
            [
                gallery.OverRow.Light, gallery.ByDoor.Light,
                studio.Band.Light, studio.Entry.Light
            ],
            1 =>
            [
                studio.Band.Light, studio.Palette.Light,
                studio.Entry.Light, studio.Way.Light
            ],
            _ => new[]
            {
                studio.Palette.Light, studio.Way.Light,
                shop.Entry.Light, shop.OverColours.Light
            }
        });
    }
}
