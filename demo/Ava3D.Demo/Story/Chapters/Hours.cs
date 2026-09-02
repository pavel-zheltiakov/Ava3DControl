using System.Numerics;

namespace Ava3D.Demo.Story;

/// <summary>
/// Chapter 7. A door on the way to somewhere else, and a room with the sun in it.
///
/// <b>It is the only detour in the film and the only room he leaves the way he came in.</b> Everything
/// before this is a route: he arrives at each exhibit because the plan walked him to it, and no room in the
/// building is optional. This one is off the corridor between the dome and the lounge, and the whole beat is
/// that he was going somewhere else. He comes round the link's corner heading south, there is light coming
/// out of a doorway on his right, and he turns.
///
/// The reason it is a dead end is that a tower is one. You go up because there is one thing at the top, you
/// stand in it, and you come back down past everything you already passed — a clock chamber with a second
/// door would be a corridor with a clock in it. See <see cref="Deck.ClockTower"/>.
///
/// <b>And it is the one room in the building that casts a shadow.</b> Every other chapter spends four point
/// lights; this one spends a sun. That is not a change of technique for its own sake — a shadow needs a
/// light with a direction, a direction needs somewhere for the light to be coming from, and the only honest
/// somewhere is a hole in a wall. The dome next door is the darkest room in the film and this is the
/// brightest, and they are ninety seconds apart on purpose: he walks out of eighteen seconds of black sky
/// into a shaft of afternoon, which is the largest change of light anywhere here.
///
/// <b>The middle of the chapter stands one room and nothing else, and that is the exhibit.</b> A shadow map
/// is fitted to what casts — see <c>ShadowView.For</c> — so a frustum that has to cover this tower and
/// twenty metres of corridor behind it is a frustum with the tower in a corner of it, and what that costs
/// is the whole picture: the clock hands come out as smears. Occupying the tower alone while he is inside
/// it is the same argument <c>ClockTowerScene</c>'s own notes make about its floor, one size up, made about
/// a building instead of a room.
/// </summary>
internal sealed class Hours(Link link, ClockRoom tower, ScreenRoom lounge) : Chapter
{
    /// <summary>How long it runs, as a constant the contents table can add up.</summary>
    public const float Length = 54f;

    /// <summary>When he is through the doorway and the corridor behind him stops mattering.</summary>
    private const float Inside = 9f;

    /// <summary>When he turns away from the movement and starts back towards the door.</summary>
    private const float Leaves = 37f;

    /// <summary>
    /// When the link stands again behind him, which is later than <see cref="Leaves"/> by the cloud.
    ///
    /// The tower's sun goes in at thirty-five and the lantern holds the shadow map until thirty-eight
    /// and a half — see the cloud constants in <c>ClockTowerScene</c> — and a point light's map is a cone
    /// fitted round whatever casts. A corridor standing behind the tower for the last of those seconds is
    /// a corridor inside that cone, and the wheels' shadows would go coarse in one frame as it arrived.
    /// So the link stays down until the sun has the map back, and its lamp waits with it.
    /// </summary>
    private const float Rejoins = 39.5f;

    /// <summary>When he is back in the corridor and the tower is behind him.</summary>
    private const float Out = 44f;

    /// <summary>And when the lounge is the room ahead.</summary>
    private const float Doorstep = 48.5f;

    private int _bank = -1;

    public override string Title => "The clock";

    public override float Duration => Length;

    /// <summary>
    /// Round the corner, a turn to the right, six hundred millimetres of stone, and then a room he crosses
    /// once and comes back out of.
    ///
    /// <b>He walks in looking at the floor and not at the window</b>, which is the staging the whole room
    /// was turned a half turn to get. The opening is in the east wall and the doorway is on the east side,
    /// so coming through it the glass is behind his shoulder and what is in front of him is three metres of
    /// clock drawn on the flagstones. The dial that is casting it is not in frame for another ten seconds.
    /// That order is the argument: a shadow you see before you see what made it is a shadow you have to
    /// account for.
    ///
    /// <b>And the turn at the door is two segments rather than one.</b> He arrives at the corner still
    /// walking south with his head already coming round to the north — the passage's trick in chapter 2 and
    /// the link's own in chapter 6 — because a man who stops dead and then rotates is a camera on a tripod.
    /// Split at the threshold it is two turns of fifty degrees; written as one it is a hundred in the
    /// segment that also walks through a doorway.
    /// </summary>
    public override Walk Walk { get; } = new(
        // Where the dome leaves him: at the corner, still heading down the leg to the lounge.
        new Step(0f, Link.Along(5.7f, 2.2f), Link.Along(3.2f)),

        // The light on his right. He keeps walking west to the corner and the head comes round.
        new Step(3f, Link.Along(5.7f, 0.5f), Link.Tower),
        new Step(5.5f, Link.Along(6.2f), ClockRoom.At(1.2f, -2.2f)),

        // Through the wall. A doorway is a thing you go through square, which is a waypoint in it.
        new Step(Inside, ClockRoom.At(1.55f, -2.6f), ClockRoom.Beam),

        // <b>West across the room, and he walks through the beam rather than round it.</b> The drawing is
        // three metres of clock lying on the flagstones and the doorway is on the far side of it, so the
        // only way in is across — which is the one thing you can do to a shadow that you cannot do to a
        // painting, and it is worth eleven seconds. His eye stays down on the floor for all of them.
        new Step(13f, ClockRoom.At(0.9f, -1.9f), ClockRoom.Beam),
        new Step(17f, ClockRoom.At(-0.4f, -1.5f), ClockRoom.Beam),
        new Step(20.5f, ClockRoom.At(-1.7f, -1.0f), ClockRoom.Beam),

        // And then he is past it, at the west wall, and turns round.
        //
        // <b>This is the shot the room was turned a half turn to get, and it only exists from here.</b>
        // Anywhere on the east half he is inside his own subject: the dial is two metres across and four
        // metres away, which fills the frame with a clock, and the drawing is behind and beneath him. From
        // the west wall it is five and a half metres to the window — the ring reads at a third of the
        // frame — and the floor between the two of them is the shadow. Both halves or neither. See
        // <see cref="ClockRoom.Face"/>, which is why the aim is a metre off the ground and not on the ring.
        new Step(24.5f, ClockRoom.At(-2.45f, -0.45f), ClockRoom.Face),
        new Step(28f, ClockRoom.At(-2.45f, -0.45f), ClockRoom.Face),

        // The pendulum, which is the only thing in the room whose shadow moves while he is standing still.
        new Step(31f, ClockRoom.At(-2.5f, -0.15f), ClockRoom.Bob),

        // And the movement, in the dark half, lit by the one lamp in here that is not a window. Four
        // seconds, because the escapement steps twice in that time and once is a twitch.
        new Step(34f, ClockRoom.At(-2.5f, -0.75f), ClockRoom.Wheels),
        new Step(Leaves, ClockRoom.At(-2.5f, -0.75f), ClockRoom.Wheels),

        // Back out, across the drawing a second time, with one look down at it on the way.
        new Step(40.5f, ClockRoom.At(-0.6f, -1.3f), ClockRoom.Beam),
        new Step(Out, ClockRoom.At(1.5f, -2.75f), ClockRoom.Doorway),

        // And down the leg to the lounge, which is where the dome used to put him.
        new Step(Doorstep, Link.Along(4.4f, 0.3f), Link.Along(1.2f)),
        new Step(51f, Link.Along(1.5f), ScreenRoom.Sitting),
        new Step(Length, ScreenRoom.Entrance + new Vector3(0.35f, 0f, -1.3f), ScreenRoom.Sitting));

    public override void Enter(Hall hall)
    {
        _bank = -1;

        // Where the dome hands the building over, which is what its own last frame asserts. A chapter that
        // opens on a different number from the one before it closed on is a step in the picture at the
        // boundary, and the boundary is the one frame nobody is looking away for.
        hall.Ambient(0.062f, 0.030f);

        foreach (var lamp in lounge.Warm)
            lamp.Dim(0f);

        lounge.Blackout();

        // The tower dark, and the sun with it. Both come up on the walk — see Update — and a room that is
        // already lit when he turns towards the door is a room with no reason to turn.
        tower.Door.Dim(0f);
        tower.Daylight(0f);
    }

    public override void Update(Hall hall, float seconds)
    {
        var bank =
            seconds < Inside ? 0 :
            seconds < Rejoins ? 1 :
            seconds < Out ? 2 :
            seconds < Doorstep ? 3 : 4;

        if (bank != _bank)
        {
            _bank = bank;
            Spend(hall, bank);
        }

        // The clock, running on the chapter's own second.
        //
        // It is the chapter's and not the film's, and that is the same decision the planetarium's show
        // makes: a clock is a thing that is going when you find it, but the pendulum starts at the extreme
        // of its swing — see ClockTowerScene.Update, which uses a cosine so a capture's first frame and its
        // last are the same picture — and handing it the film's clock would put the first frame anybody
        // sees of this room at an arbitrary point in the stroke. Nothing about seeking is lost: it is still
        // one number in and one frame out.
        tower.Running(hall.Scene, seconds);

        // The sun, up as he comes round the corner and down as he leaves. It is the slowest fade in the
        // film — six seconds — because it is the only one that is a room getting brighter rather than a
        // lamp being switched on, and an afternoon does not have a switch.
        tower.Daylight(Ramp(seconds, 1.5f, 6f) * (1f - Ramp(seconds, Out - 1f, 4f)));

        tower.Door.Dim(Ramp(seconds, 2f, 4f) * (1f - Ramp(seconds, Out, 3f)));

        // The corridor behind him, which stays up until he is through the wall and comes back for the walk
        // out. It is the same lamp doing both, which is what a corridor's lamp is for.
        link.Turn.Dim(1f - 0.75f * Ramp(seconds, Inside, 4f) * (1f - Ramp(seconds, Rejoins, 5f)));
        link.Run.Dim(Ramp(seconds, Out - 6f, 5f) * (1f - Ramp(seconds, Length - 1f, 2f)));

        // The bounce, up with the sun and then back down to what the lounge opens on. This room needs three
        // times what the dome does and it is the window: a beam that bright with nothing bouncing off the
        // walls round it is a lit floor in a black box, which is a render rather than a room.
        var day = Ramp(seconds, 2f, 7f) * (1f - Ramp(seconds, Out - 2f, 5f));
        var arriving = Ramp(seconds, Doorstep - 6f, 9f);

        hall.Ambient(
            0.062f + 0.098f * day + 0.012f * arriving,
            0.030f + 0.026f * day + 0.004f * arriving);

        // And the lounge, coming up through its own doorway in the order chapter 8 expects to find it in —
        // the four lamps at exactly what that chapter's first frame sets them to, which is the hand-over
        // the dome used to make and now makes one room later.
        lounge.Doorway.Dim(Ramp(seconds, Doorstep - 3f, 4f));
        lounge.Lounge.Dim(Ramp(seconds, Doorstep - 1f, 4f));
        lounge.Bench[0].Dim(Ramp(seconds, Doorstep + 1f, 3.5f));
        lounge.Bench[1].Dim(Ramp(seconds, Doorstep + 1f, 3.5f) * 0.92f);

        hall.Scene.Invalidate();
    }

    public override string? Caption(float seconds) => seconds switch
    {
        < 6f => "There is a door here I nearly left out of the notes",
        < 14f => "The clock is on the other side of that wall. This is its back",

        // On the floor, before the dial is in frame. The caption says what the picture is doing and not
        // what the feature is called, which is the rule the whole film's captions follow.
        < 21f => "Nobody sees this face. The town gets the other one",
        < 29f => "Three metres of clock, drawn on the floor, once a day for a hundred years",
        < Leaves => "It keeps time whether or not anybody is standing in it",

        // And out, on the running joke.
        < Out => "Four hundred words. I could spend them all in here",
        _ => null
    };

    /// <summary>
    /// The four lights for a stretch of the walk, and which rooms are standing while it runs.
    ///
    /// <b>The sun is spent first in every bank that has one</b>, because <see cref="Scene.ShadowCastingLight"/>
    /// is the first light in the list that casts and the other three here are point lights that never
    /// could. Ordering it anywhere else would work and would work by luck.
    ///
    /// <b>And bank 1 stands one room.</b> That is the exhibit rather than an economy: the depth pass is
    /// fitted to whatever casts, so a corridor left standing behind the tower is a corridor inside the
    /// frustum, and every texel it takes is a texel off the clock hands. Thirty seconds of this chapter
    /// are a room with nothing else in the building switched on, and the last few of them are the ones
    /// the lantern holds the map for — see <see cref="Rejoins"/>.
    /// </summary>
    private void Spend(Hall hall, int bank)
    {
        switch (bank)
        {
            case 0:
                hall.Occupy(Deck.LinkRoom, Deck.ClockRoom);
                hall.Use(tower.Sun, tower.Door.Light, link.Turn.Light, link.Run.Light);
                break;

            case 1:
                hall.Occupy(Deck.ClockRoom);
                hall.Use(tower.Sun, tower.Lantern, tower.Door.Light);
                break;

            case 2:
                hall.Occupy(Deck.LinkRoom, Deck.ClockRoom);
                hall.Use(tower.Sun, tower.Lantern, tower.Door.Light, link.Turn.Light);
                break;

            case 3:
                hall.Occupy(Deck.LinkRoom, Deck.ClockRoom, Deck.ScreensRoom);
                hall.Use(link.Turn.Light, link.Run.Light, lounge.Doorway.Light, lounge.Lounge.Light);
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
