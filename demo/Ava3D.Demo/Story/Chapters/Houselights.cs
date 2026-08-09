using System.Numerics;

namespace Ava3D.Demo.Story;

/// <summary>
/// Chapter 1. The lamps come up in banks and the dark turns out to have been a room.
///
/// The reveal is the comparison. Every other scene in this demo that shows what a light does shows it
/// twice in one frame — lit on the left, unlit on the right — because that is the only way to be sure
/// what changed. This one shows the same geometry thirty seconds apart instead, which is weaker evidence
/// and much better television, and it can afford to be because the room it reveals is small enough to be
/// taken in at once.
///
/// What comes up is three lamps, one at a time, on top of the one that was already lit. Four is the cap,
/// and the room was laid out around it rather than trimmed to fit: three ceiling lamps light a six-metre
/// room to the corners with nothing left over, which is why the fourth can stay on the exhibit throughout.
/// </summary>
internal sealed class Houselights(Antechamber room, Passage passage) : Chapter
{
    /// <summary>When the lamp over the plinth hands its slot to the one in the passage.</summary>
    public const float Reveal = 26f;

    private int _bank = -1;

    public override string Title => "Houselights";

    public override float Duration => 35f;

    /// <summary>
    /// A slow turn on the spot while the light arrives — the lamps are behind and above him, so what he
    /// watches is the far wall stopping being black — and then a walk to the door he could not previously
    /// see.
    ///
    /// <b>The walk to the door goes round the plinth, and it used to go through it.</b> The exhibit stands
    /// at the room's origin and the door is cut in the middle of the north wall, so the straight line
    /// between where he is standing and where he is going passes through both. It was not reported as a
    /// fault because nothing was ever <i>inside</i> anything: the plinth is a metre high, the cube on top of
    /// it reaches 1.54, and an eye at 1.7 clears the lot by a hundred and sixty millimetres — so the camera
    /// flew over an exhibit at knee height to it, which reads exactly like walking through it, and a man
    /// walked through a stone plinth from the shoulders down. See <c>Ground.Audit</c>, which now says so.
    ///
    /// It passes on the lamp's side. The spot is off-axis at x 0.85 and rakes the cube's +X and -Z faces, so
    /// going by to the east keeps a lit face turned toward him the whole way past; to the west he would walk
    /// the length of the one face that is in shadow, and past the door standing open on that side.
    ///
    /// A metre out at the closest point, which is two thirds of a metre of daylight from the lip — a wide
    /// berth on purpose. The near plane is five centimetres and this is the first object anybody sees.
    /// </summary>
    public override Walk Walk { get; } = new(
        new Step(0f, new Vector3(0f, Deck.Eye, -2.8f), new Vector3(0f, 1.20f, 0f)),
        new Step(6f, new Vector3(0f, Deck.Eye, -2.8f), new Vector3(-2.6f, 1.5f, 1.2f)),
        new Step(13f, new Vector3(0f, Deck.Eye, -2.8f), new Vector3(2.6f, 1.6f, 1.4f)),
        new Step(19f, new Vector3(0f, Deck.Eye, -2.8f), new Vector3(0f, 2.6f, 2.2f)),

        // He is looking at the door from here on and his feet do the avoiding, which is what a person
        // does — the head goes where it is going and the body steps round what is in the way.
        new Step(24f, new Vector3(0.30f, Deck.Eye, -1.75f), new Vector3(0f, 1.5f, 3f)),
        new Step(29f, new Vector3(1.00f, Deck.Eye, -0.50f), new Vector3(0f, 1.5f, 3.4f)),
        new Step(32f, new Vector3(1.00f, Deck.Eye, 0.55f), new Vector3(0f, 1.5f, 3.9f)),

        // And back onto the centre line, because chapter 2 starts exactly here and the two are consecutive
        // seconds of one shot.
        new Step(35f, new Vector3(0f, Deck.Eye, 1.4f), new Vector3(0f, 1.5f, 4.5f)));

    public override void Enter(Hall hall)
    {
        // The passage is standing too, unlit and with the room's own lamps spilling a few metres up it.
        // That is what puts something behind the doorway when it appears — a door onto black reads as a
        // painted rectangle, and the four slots are all spent in here, so the something has to be light
        // that is already in the room reaching through the opening.
        _bank = -1;

        hall.Occupy(Deck.AntechamberRoom, Deck.ThresholdRoom);

        room.Spot.Dim(1f);
        passage.Lamp.Dim(0f);

        foreach (var lamp in room.House)
            lamp.Dim(0f);
    }

    public override void Update(Hall hall, float seconds)
    {
        // The three houselights hold their slots for the whole chapter; the fourth changes hands once.
        //
        // It is the lamp over the plinth, and it goes to the passage at the moment he turns away from the
        // exhibit and starts for the door — so the last thing lit in here goes out behind him while
        // something warm appears through the opening. A door onto black is a painted rectangle, and there
        // is no fifth slot to light the corridor with, so the only way to have both is to stop lighting
        // the thing he has finished looking at.
        var bank = seconds < Reveal ? 0 : 1;

        if (bank != _bank)
        {
            _bank = bank;

            if (bank == 0)
                hall.Use(room.Spot.Light, room.House[0].Light, room.House[1].Light, room.House[2].Light);
            else
                hall.Use(passage.Lamp.Light, room.House[0].Light, room.House[1].Light, room.House[2].Light);
        }

        // Both sides of that swap are faded across it, so neither lamp is contributing on the frame the
        // list is rebuilt.
        room.Spot.Dim(1f - Ramp(seconds, Reveal - 2.5f, 2.5f));
        passage.Lamp.Dim(Ramp(seconds, Reveal, 3f));

        // Bank by bank, a second and a half apart. Simultaneous would read as a master switch; this reads
        // as somebody walking along a wall of breakers.
        for (var i = 0; i < room.House.Length; i++)
            room.House[i].Dim(Ramp(seconds, 1.2f + i * 1.5f, 1.8f));

        // The ambient comes up with them, because the bounce off four walls is real and a forward renderer
        // with four point lights has no other way to account for it. It is what stops the corners reading
        // as holes.
        var lit = Ramp(seconds, 1.2f, 5.4f);
        hall.Ambient(0.032f * lit, 0.013f * lit);

        room.Cube.RotationDegrees = new Vector3(0f, 350f + seconds * 14f, 0f);
        hall.Scene.Invalidate();
    }

    public override string? Caption(float seconds) => seconds switch
    {
        < 1f => null,
        // Rule one of six, and the whole chapter is about lights so that the one light that disobeys it
        // has somewhere to be noticed. See Dark.Caption for what the six are doing.
        < 12f => "Rule one. The main lights come on in the order someone wired them",
        < 24f => "All except that one over the crate. That one is always on",
        _ => "The man before me left it on. I wrote a report. Nobody closed the report"
    };
}
