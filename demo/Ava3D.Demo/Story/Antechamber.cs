using System.Numerics;
using Ava3D.Demo.Scenes;

namespace Ava3D.Demo.Story;

/// <summary>
/// The first room: six metres square, one exhibit, one door.
///
/// It is deliberately small, and that is the only interesting decision in it. A film that opens on a grand
/// hall has spent its scale in the first thirty seconds and has nothing left for the moment the roof comes
/// off; a film that opens in a room you could cross in four paces can keep the building's real size until
/// chapter 12. So the houselights coming up in chapter 1 reveal a room rather than a museum, and everyone
/// who has watched it says the same thing about the ceiling being lower than they expected. That is the
/// effect working.
///
/// What stands on the plinth is <see cref="HelloCubeScene"/>, mounted — the same file that runs on its own
/// against a black background when the story is switched off, with no knowledge that a plinth exists.
/// </summary>
internal sealed class Antechamber
{
    public const float Size = 6f;

    private static readonly Vector3 PlinthAt = new(0f, 0f, 0f);

    public Antechamber(Hall hall)
    {
        var root = hall.Add(Deck.AntechamberRoom, Deck.Antechamber);
        var half = Size / 2f;

        // The bottom of the ladder: colour, metallic and roughness, and not one image in the room. That is
        // a complete PBR material and this demo has always said so — the point of starting here is that
        // nobody watching chapter 0 thinks anything is missing, and by the lounge it is obvious that
        // something was. See <see cref="Grade"/>.
        var plaster = Finish.Plaster(Grade.Flat);
        var tile = Finish.Floor(Grade.Flat);

        root.Children.Add(Fabric.Sheet(Size, Size, 0f, tile, "floor"));
        root.Children.Add(Fabric.Lid(Size, Size, Deck.Ceiling, plaster, "ceiling"));

        // North wall carries the only door. The other three are solid, which is what makes the room a room
        // and not a set — you can turn all the way round in it and there is nowhere else to look.
        var north = Fabric.PiercedWall(
            Size, Deck.Ceiling, Deck.WallThickness,
            doorCentre: 0f, Deck.DoorWidth, Deck.DoorHeight, plaster);
        north.Position = new Vector3(0f, 0f, half);
        root.Children.Add(north);

        // And the first door in the building, standing open against the passage side of that wall. White
        // plastic, one leaf, three hinges — the most ordinary door there is, which is the only reason it is
        // worth drawing. It is the first term in a sequence, and a sequence needs somewhere dull to start.
        root.Children.Add(Door.Hinged(
            new Vector3(-Deck.DoorWidth / 2f, 0f, half - Deck.WallThickness / 2f),
            Door.Plastic(),
            swing: 104f));

        var south = Fabric.Wall(Size, Deck.Ceiling, Deck.WallThickness, plaster);
        south.Position = new Vector3(0f, 0f, -half);
        root.Children.Add(south);

        foreach (var side in new[] { -1f, 1f })
        {
            var wall = Fabric.Wall(Size, Deck.Ceiling, Deck.WallThickness, plaster);
            wall.Position = new Vector3(side * half, 0f, 0f);
            wall.RotationDegrees = new Vector3(0f, 90f, 0f);
            root.Children.Add(wall);
        }

        var plinth = Fabric.Plinth(PlinthAt);
        root.Children.Add(plinth.Root);

        // The exhibit, mounted. Half a metre on a side rather than the one metre the scene builds, because
        // a plinth is a metre high and the thing on it has to be smaller than the thing under it — which is
        // a decision about this room, taken here, and not something HelloCubeScene should have to know.
        Cube = new HelloCubeScene().BuildSubject()!;
        Cube.Scale = new Vector3(0.5f);
        Cube.Position = plinth.Top + new Vector3(0f, 0.25f, 0f);
        Cube.Name = "exhibit.cube";
        root.Children.Add(Cube);

        // The card. No text on it — the caption band under the picture is where this film says words, and
        // a plate with unreadable engraving on it says "museum" perfectly well from two metres.
        //
        // <b>It is on the plinth's face, and it used to be lying flat on the top at the back.</b> There is
        // no frame in the film where that could be seen. Chapters 0 and 1 both stand between z = −2.1 and
        // −2.8 and look north at the exhibit for their whole length, so a card at +0.20 is a card behind
        // the exhibit; the only view that ever reached it was somebody coming back through the door in the
        // last chapter, and from there a plate lying flat is a dark sliver a centimetre thick against the
        // foot of the cube — the stray dark triangle. Nor is the top of the plinth free, whatever side it
        // is put on: the cube turns at fourteen degrees a second, and a turning cube half a metre on a side
        // sweeps a circle 708 millimetres across, which is wider than the plinth's lip.
        //
        // The face is square to the camera for the whole of both chapters, cannot be masked by whatever is
        // standing on top of the plinth, and is where a plinth carries its label anyway.
        //
        // <b>And it is lacquered rather than bare brass.</b> Bare brass is <see cref="Material.Metallic"/>
        // at one, which has no diffuse term at all: with three point lamps and no environment to reflect,
        // a metal is black everywhere its specular lobe does not land, and on a vertical face lit from
        // above and to one side it does not land — the half-vector is forty-seven degrees off the normal
        // and this lobe is a few degrees wide. Lacquer is a dielectric coat over metal and that is exactly
        // what these numbers are: enough metal left to stay gold, enough diffuse to be lit at all.
        var plate = new Material
        {
            BaseColor = new Vector4(0.62f, 0.47f, 0.20f, 1f),
            Metallic = 0.35f,
            Roughness = 0.42f,
            Name = "card"
        };

        root.Children.Add(Fabric.Slab(
            new Vector3(0.16f, 0.10f, 0.008f),
            PlinthAt + new Vector3(0f, 0.72f, -0.306f),
            plate,
            "card"));

        // One lamp over the exhibit, and three that light the room. The first is the whole of chapter 0 and
        // the other three are the whole of chapter 1, which is exactly four — the number of slots there
        // are. The room was designed around the cap rather than trimmed to fit it.
        //
        // Off-axis, and short. Both matter and both were wrong first time round. A lamp directly over a
        // cube lights its top face and nothing else — every side has the light at ninety degrees to its
        // normal, so a box under a downlight is a black box with a white lid, which is correct shading and
        // a useless photograph. Moved forward and to one side it rakes two faces and leaves the third in
        // shadow, which is what makes a cube read as a solid.
        //
        // The range is what keeps chapter 0 dark. Falloff alone does not: at three metres an unbounded
        // lamp still puts a ninth of its light on the far wall, which is plenty to see a room by, and the
        // whole of the opening is that there is no room yet. Three metres of range reaches the floor and
        // the plinth and stops short of every wall.
        Spot = Fabric.Ceiling(
            Deck.Antechamber, new Vector3(0.85f, Deck.Ceiling - 0.35f, -0.95f), brightness: 1.9f, range: 3f);
        root.Children.Add(Spot.Fixture);

        // Two and a half, not four and a half. A six-metre room lit to the corners by three lamps needs
        // about as much light as one lamp puts on a wall two metres away, and the first pass at this had
        // the plaster reading as white paper — which loses the shading on every surface in the room and
        // takes the exhibit with it. The range is a little over the room's diagonal, so a lamp reaches the
        // far corner and stops rather than lighting through the wall.
        House =
        [
            Fabric.Ceiling(Deck.Antechamber, new Vector3(-1.8f, Deck.Ceiling - 0.1f, -1.8f), 2.4f, 6.5f),
            Fabric.Ceiling(Deck.Antechamber, new Vector3(1.8f, Deck.Ceiling - 0.1f, -1.8f), 2.4f, 6.5f),
            Fabric.Ceiling(Deck.Antechamber, new Vector3(0f, Deck.Ceiling - 0.1f, 2.1f), 2.4f, 6.5f)
        ];

        foreach (var lamp in House)
            root.Children.Add(lamp.Fixture);
    }

    /// <summary>The lamp over the plinth. The only light in chapter 0.</summary>
    public Lamp Spot { get; }

    /// <summary>The three that come up in chapter 1, in the order they come up.</summary>
    public Lamp[] House { get; }

    /// <summary>The mounted exhibit, kept so the chapter can turn it.</summary>
    public Node Cube { get; }

    /// <summary>Where the doorway is, in world coordinates. The walk aims at it. Named for the hole and not
    /// for the thing standing open in it, which is now a <see cref="Story.Door"/> and needs the name.</summary>
    public static Vector3 Opening => Deck.Antechamber + new Vector3(0f, 0f, Size / 2f);
}
