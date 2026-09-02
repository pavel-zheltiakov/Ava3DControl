using System.Numerics;

namespace Ava3D.Demo.Story;

/// <summary>
/// The corridor between the planetarium and the lounge: three metres west, a right angle, and five and
/// seven tenths south.
///
/// It is <see cref="Passage"/> a second time, and it is the same three sentences. The planetarium's way out
/// faces west; the lounge is a long way south of it. Put the two rooms on one axis and the doorways line
/// up, and a lined-up pair of doorways is an enfilade — you see the next room from inside the last one,
/// which is the single thing this film must never do. So the two rooms do not touch, and what joins them
/// turns.
///
/// It is also a room in its own right as far as <see cref="Hall"/> is concerned, which is what makes the
/// threshold work: chapter 6 shows the planetarium and this, chapter 7 shows this and the lounge, and
/// there is no frame in the film from which two exhibition rooms could be seen even by a viewer who takes
/// the mouse and looks the wrong way.
///
/// <b>Its north leg was thirteen and a half metres and it is five and seven tenths.</b> An L between two
/// doorways is the Manhattan distance between them, so there was nothing to be done about it from inside
/// this file: the planetarium is pinned at z 40.2 by its own radius against the pattern shop's north wall,
/// and the only thing left that could move was the lounge. It did — seven metres north, see
/// <see cref="Deck.Screens"/> — and this corridor is a little over half what it was.
///
/// Two and a half metres to the ceiling, which is the passage's number and is doing the passage's job at
/// the other end of the building. He comes out of the tallest room on the deck into the lowest, and then
/// out of this into the lounge — so the room with the armchairs in it reads as somewhere to stay, which
/// costs nothing but a number and is most of what that room is for.
/// </summary>
internal sealed class Link
{
    /// <summary>Clear width, and the height of the ceiling over it.</summary>
    private const float Width = 1.6f;

    private const float Height = 2.5f;
    private const float Thickness = 0.2f;

    /// <summary>How far north the first leg runs from the lounge's doorway, and how far east the second
    /// runs from the turn — as far as the planetarium's west doorway, which is where it stops.
    ///
    /// Neither is chosen. The planetarium's west opening is due west of that room's centre, so the mouth is
    /// fixed at (7.4, 40.2) on the deck and both of these are that point minus <see cref="Deck.Link"/>,
    /// with the half width of the corridor taken off the north leg because the east leg runs down the
    /// middle of it.</summary>
    private const float North = 6.5f;

    private const float East = 3.2f;

    /// <summary>
    /// How far short of the planetarium the east leg's own walls stop.
    ///
    /// <b>They used to stop exactly on that room's inner face, and what that drew was a checkerboard.</b>
    /// Two boxes that meet flush have a face each in the same plane, the depth test has nothing to choose
    /// between them, and every second pixel picks the other one — which is the hatching this building has
    /// already paid for twice, at the engine room's bulkhead and at the corridor's two ends. The fix is the
    /// same one both of those use: stop inside the neighbour's wall rather than against it. A hundred and
    /// ten millimetres puts the end cap of each of these walls in the middle of the planetarium's
    /// two-hundred-and-fifty-millimetre one, where it is inside solid geometry and cannot be seen at all,
    /// and the last hundred millimetres of corridor wall anybody sees belongs to the round room. Which is
    /// right: a reveal belongs to the room it is cut in.
    /// </summary>
    private const float Bury = 0.11f;

    public Link(Hall hall)
    {
        var root = hall.Add(Deck.LinkRoom, Deck.Link);

        // The gallery's rung rather than the lounge's, and it is the one place on the route where the
        // ladder does not climb. Everything in this corridor is seen for four seconds at walking pace with
        // one lamp round a corner, and a room nobody stops in has nothing to say about occlusion maps. See
        // <see cref="Grade"/>.
        var plaster = Finish.Plaster(Grade.Dressed);
        var tile = Finish.Floor(Grade.Dressed);

        var half = Width / 2f;
        var edge = half + Thickness / 2f;

        // Floors and ceilings in two rectangles rather than one bounding box, for the reason the passage
        // gives: this is an L, and a rectangle over the whole of it lays tile in the quarter that is walled
        // off — which nobody would ever see and which would still be there, wrong, for whoever reads this
        // next.
        Floor(new Vector3(0f, 0f, North / 2f), Width, North);
        Floor(new Vector3((half + East) / 2f, 0f, North - half), East - half, Width);

        // The outer corner: one wall down the west side and along the north, unbroken round the turn. It is
        // the wall he is facing as he comes round it, so it is the one that must not have a seam in it.
        Wall(new Vector3(-edge, 0f, North / 2f), Thickness, North + Thickness);

        // The north wall, which is the one that changed when the clock room went in behind it. It was a
        // slab; it is the same slab with a doorway cut on the north leg's own centre line, so the opening
        // is square to the corridor he is walking rather than square to the room it leads into — see
        // <see cref="Deck.ClockTower"/>, which is a metre and six west of this line and is why.
        //
        // <b>It is the whole of the tower's doorway and it is not the wall he walks through.</b> The clock
        // room's south wall is six hundred millimetres of masonry centred on this two hundred, so this
        // opening sits inside that one and the reveal anybody sees belongs to the tower. Cutting it here as
        // well is what stops this wall closing across the back of it.
        var head = Fabric.PiercedWall(
            East - Bury + edge, Height, Thickness,
            -(East - Bury - edge) / 2f, Deck.DoorWidth, Deck.DoorHeight, plaster);

        head.Position = new Vector3((East - Bury - edge) / 2f, 0f, North + Thickness / 2f);
        root.Children.Add(head);

        // The inner corner: two short returns that stop where they meet.
        Wall(new Vector3(edge, 0f, (North - Width) / 2f), Thickness, North - Width + Thickness);
        Wall(new Vector3((half + East - Bury) / 2f, 0f, North - Width - Thickness / 2f), East - Bury - half, Thickness);

        // Two lamps, and where they are is arithmetic rather than taste.
        //
        // The first is in the corner, which is the only place in an L a single light can be: anywhere down
        // a leg and the other leg is a black rectangle, and in the corner it reaches both. That was the
        // whole of the lighting while this corridor was seven metres long.
        //
        // The second is two fifths of the way up the north leg from the lounge. It was put there when that
        // leg was thirteen and a half metres and one lamp in the corner left everything south of the middle
        // of it dark; the leg is five and seven tenths now and one lamp would very nearly do. It stays,
        // because two lamps in nine metres of corridor is what a corridor has, and because the corner one
        // is doing the turn rather than the run.
        Turn = Fabric.Ceiling(Deck.Link, new Vector3(0.1f, Height - 0.12f, North - half), 2.6f, 6f);
        root.Children.Add(Turn.Fixture);

        Run = Fabric.Ceiling(Deck.Link, new Vector3(0f, Height - 0.12f, North * 0.38f), 2.4f, 6.5f);
        root.Children.Add(Run.Fixture);

        return;

        void Floor(Vector3 centre, float width, float depth)
        {
            var plane = Primitives.Plane(width, depth);

            root.Children.Add(new MeshNode(Fabric.Map(plane, tile, centre), tile)
            {
                Position = centre,
                Name = "floor"
            });

            var overhead = centre + new Vector3(0f, Height, 0f);

            root.Children.Add(new MeshNode(Fabric.Map(plane, plaster, overhead), plaster)
            {
                Position = overhead,
                RotationDegrees = new Vector3(180f, 0f, 0f),
                Name = "ceiling"
            });
        }

        void Wall(Vector3 centre, float width, float depth) =>
            root.Children.Add(Fabric.Slab(
                new Vector3(width, Height, depth),
                centre + new Vector3(0f, Height / 2f, 0f),
                plaster,
                "wall"));
    }

    /// <summary>The lamp in the corner, which is the one that reaches both legs.</summary>
    public Lamp Turn { get; }

    /// <summary>And the one down the north leg, which is the one that stops the walk to the lounge running
    /// into the dark. See the note where they are hung.</summary>
    public Lamp Run { get; }

    /// <summary>Both, for switching the corridor off in one line.</summary>
    public Lamp[] All => [Turn, Run];

    /// <summary>The middle of the turn, in world coordinates. Both chapters aim at it.</summary>
    public static Vector3 Corner => Deck.Link + new Vector3(0f, 0f, North - Width / 2f);

    /// <summary>The doorway into the clock room, in the north wall, at eye height.</summary>
    public static Vector3 Tower => Deck.Link + new Vector3(0f, Deck.Eye, North + Thickness / 2f);

    /// <summary>
    /// Where the east leg meets the planetarium's west opening, in world coordinates.
    ///
    /// <b>This is the one doorway in the building the corridor does not decide.</b> Everywhere else — the
    /// rotunda and the gallery, the pattern shop and this corridor as it was — one room cuts the hole and
    /// the other reads it, and it is always the corridor, because a corridor is the piece of the plan whose
    /// whole job is to arrive somewhere. A round room cannot take that instruction: its openings are on
    /// radials thirty degrees apart and there is nowhere else on the ring to put one. So here the corridor
    /// is what moves, and <see cref="North"/> and <see cref="East"/> are both read off
    /// <see cref="Deck.Planetarium"/> rather than chosen.
    /// </summary>
    public static Vector3 Mouth => Deck.Link + new Vector3(East, Deck.Eye, North - Width / 2f);

    /// <summary>A point on the link's floor at eye height, so far north of the lounge's doorway.</summary>
    public static Vector3 Along(float north, float east = 0f) =>
        Deck.Link + new Vector3(east, Deck.Eye, north);
}
