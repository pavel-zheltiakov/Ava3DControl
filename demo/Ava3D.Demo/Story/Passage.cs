using System.Numerics;

namespace Ava3D.Demo.Story;

/// <summary>
/// The corridor between the antechamber and the rotunda: four metres north, a right angle, and four metres
/// east.
///
/// It is the only piece of the building that is not an exhibit, and it is here because of rule 2. The
/// antechamber's one door faces north; the rotunda is east of it. Put them wall to wall and the two
/// doorways line up, and a lined-up pair of doorways is an enfilade — you see the next room from inside
/// the last one, which is the single thing this film must never do. So the two rooms do not touch, and
/// what joins them turns.
///
/// It is also a room in its own right as far as <see cref="Hall"/> is concerned, and that is what makes the
/// threshold work. Chapter 1 shows the antechamber and this; chapter 2 shows this and the rotunda. In
/// neither case are both exhibition rooms standing, so there is no frame in the film from which two of
/// them could be seen even by a viewer who takes the mouse and looks the wrong way.
///
/// Low, and deliberately. Two and a half metres after a room with a three-two ceiling is barely noticeable
/// going in; coming out of it into the rotunda's four is what makes the rotunda read as tall, and it costs
/// nothing but a number. Height in a building is comparative and there is no other way to spend it.
/// </summary>
internal sealed class Passage
{
    /// <summary>Clear width, and the height of the ceiling over it.</summary>
    private const float Width = 1.6f;

    private const float Height = 2.5f;
    private const float Thickness = 0.2f;

    /// <summary>How far north the first leg runs from the antechamber's doorway, and how far east the
    /// second runs from the turn — as far as the rotunda's outer wall, which is where it stops.</summary>
    private const float North = 3.8f;

    private const float East = 4.35f;

    public Passage(Hall hall)
    {
        var root = hall.Add(Deck.ThresholdRoom, Deck.Threshold);

        // One rung up the ladder: a base-colour map and nothing else. In a corridor lit by a single lamp
        // round a corner it barely registers, and that is what it is for — the wall stops being exactly one
        // colour a room before anybody could tell you which room it happened in. See <see cref="Grade"/>.
        var plaster = Finish.Plaster(Grade.Grained);
        var tile = Finish.Floor(Grade.Grained);

        var half = Width / 2f;
        var edge = half + Thickness / 2f;

        // Floors and ceilings in two rectangles rather than one bounding box. The passage is an L, and a
        // rectangle over the whole of it would lay tile in the quarter that is walled off — which nobody
        // would ever see and which would still be there, wrong, for whoever reads this next.
        Floor(new Vector3(0f, 0f, North / 2f), Width, North);
        Floor(new Vector3((half + East) / 2f, 0f, North - half), East - half, Width);

        // The outer corner: one wall down the west side and along the north, unbroken round the turn. It
        // is the wall he is facing for the whole of the second leg, so it is the one that must not have a
        // seam in it.
        Wall(new Vector3(-edge, 0f, North / 2f), Thickness, North + Thickness);
        Wall(new Vector3((East - edge) / 2f, 0f, North + Thickness / 2f), East + edge, Thickness);

        // The inner corner: two short returns that stop where they meet.
        Wall(new Vector3(edge, 0f, (North - Width) / 2f), Thickness, North - Width + Thickness);
        Wall(new Vector3((half + East) / 2f, 0f, North - Width - Thickness / 2f), East - half, Thickness);

        // One lamp, in the corner, which is the only place in an L-shaped corridor a single light can be.
        // Anywhere down a leg and the other leg is a black rectangle; in the corner it reaches both, and
        // it is what the antechamber's doorway has warm light behind it in chapter 1.
        Lamp = Fabric.Ceiling(Deck.Threshold, new Vector3(0.1f, Height - 0.12f, North - half), 2.8f, 6f);
        root.Children.Add(Lamp.Fixture);

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

    /// <summary>The one lamp, in the corner.</summary>
    public Lamp Lamp { get; }

    /// <summary>The middle of the turn, in world coordinates. Both chapters aim at it.</summary>
    public static Vector3 Corner => Deck.Threshold + new Vector3(0f, 0f, North - Width / 2f);
}
