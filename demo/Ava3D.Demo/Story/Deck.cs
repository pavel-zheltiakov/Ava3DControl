using System.Numerics;

namespace Ava3D.Demo.Story;

/// <summary>
/// The floor plan, as numbers.
///
/// One deck, and a route that switchbacks so that no two doorways are ever collinear. That last
/// rule is the one everything else rests on: an enfilade — the grand museum axis with every door lined up
/// — shows you the whole building from the front door, and this film's entire structure is that you never
/// see the next exhibit until you reach it. Every room's exit is therefore on a different wall from its
/// entrance, and the turn happens inside the threshold where neither room is fully in frame.
///
/// The origins are here rather than in each room's builder so that the plan can be read in one place and
/// so a walk, which is written in world coordinates, can be checked against the room it is supposed to
/// stay inside.
///
/// Metres. Ceilings are low — 3.2 in most rooms — because the reveal in chapter 12 only lands if the nine
/// minutes before it were spent somewhere you could touch the walls of.
/// </summary>
internal static class Deck
{
    public const float Ceiling = 3.2f;
    public const float WallThickness = 0.25f;
    public const float DoorWidth = 1.2f;
    public const float DoorHeight = 2.4f;

    /// <summary>Eye height. A person, not a camera on a tripod.</summary>
    public const float Eye = 1.7f;

    public static readonly Vector3 Antechamber = new(0f, 0f, 0f);
    public static readonly Vector3 Rotunda = new(9f, 0f, 6f);

    /// <summary>
    /// The material gallery, and the one room origin in the building that is not a round number.
    ///
    /// It is where it is because it has to meet two doorways that already exist. Its south wall carries the
    /// rotunda's north door, which is four and a half metres from the rotunda's centre; its west wall
    /// carries the screen room's east door, which is where the screen room put it. A room between two fixed
    /// openings does not get to choose its own centre, and rounding this to (9, 0, 18) would move both
    /// doorways off the walls they are cut in — which does not read as a slightly wrong number, it reads as
    /// a gap of daylight down the side of a door.
    /// </summary>
    public static readonly Vector3 Materials = new(8.75f, 0f, 17.875f);

    public static readonly Vector3 Screens = new(1f, 0f, 24f);

    /// <summary>
    /// The alarm corridor, and like the gallery it does not get to choose where it is.
    ///
    /// Its origin is the outer face of the lounge's north wall, on the centre line of the lounge's exit —
    /// so it is the lounge's own numbers, added up: the room is at x 1, its door is 3.3 west of that, its
    /// north wall is 3.5 north of centre and a quarter of a metre thick. A corridor bolted to a doorway
    /// that already exists is one that has to be measured from that doorway, and the alternative is a
    /// round number with daylight down the side of it.
    ///
    /// Local +Z runs up the corridor, away from the lounge, which is why <see cref="Corridor"/> can talk in
    /// metres from the door he came in by rather than in deck coordinates.
    /// </summary>
    public static readonly Vector3 Corridor = new(-2.3f, 0f, 27.75f);

    /// <summary>
    /// The engine room, and the third room in a row that is measured off the one before it rather than
    /// placed.
    ///
    /// It is on the corridor's centre line, at the inner face of its own south bulkhead — which is the
    /// corridor's twenty-one metres plus the four hundred and fifty millimetres that put the corridor's end
    /// wall <i>inside</i> this room's. That overlap is deliberate and is the same fix the corridor uses at
    /// the other end of itself: this bulkhead is six hundred millimetres of ship rather than the quarter of
    /// a metre every other wall in the building is, and the corridor's end wall stands entirely within it
    /// with a hundred and fifty millimetres to spare either side. Two walls that met face to face would be
    /// two coplanar surfaces with nothing to choose between them.
    ///
    /// The thickness is not only a fix. It is the first wall in the film that is thick enough to be a
    /// <i>reveal</i> — you walk through six hundred millimetres of it — and after twenty-one metres of
    /// two-metre corridor that is most of what says the room on the other side is a different kind of
    /// space.
    ///
    /// Local +Z runs into the room, away from the corridor, and local X runs with the deck's.
    /// </summary>
    public static readonly Vector3 Engine = new(-2.3f, 0f, 49.2f);

    /// <summary>
    /// The illuminator gallery, north of the engine room and the last room on the deck.
    ///
    /// Its origin is the inboard face of its own back wall, which is thirteen metres four past the engine
    /// room's origin — a quarter of a metre north of that room's north wall, so that wall ends up buried
    /// inside this one. Same fix as the engine room's bulkhead and the corridor's two ends: a wall six
    /// hundred millimetres thick that <i>contains</i> the quarter-metre wall it meets has no face level
    /// with any of that wall's, and two walls that met flush would be two coplanar surfaces the depth test
    /// cannot choose between.
    ///
    /// Its X runs with the deck's, so this room's local x and the engine room's are the same number — which
    /// is what lets one doorway be named once, in <see cref="EngineRoom.Doorway"/>, and used by both.
    ///
    /// Local +Z runs outboard, toward the hull and the ports, which is the direction everything in the
    /// chapter is measured as a bearing from.
    /// </summary>
    public static readonly Vector3 Illuminator = new(-2.3f, 0f, 62.6f);

    /// <summary>
    /// The corner between the antechamber and the rotunda, and the origin the passage is built around.
    ///
    /// It exists because of the turn. The antechamber's only door faces north and the rotunda is east of
    /// it, so the two cannot share a wall without lining their doorways up — and a lined-up pair of
    /// doorways is an enfilade, which shows you the next room from inside the last one. Four metres of
    /// corridor with a right angle in it is what rule 2 costs here, and it is the cheapest place in the
    /// building to pay it.
    /// </summary>
    public static readonly Vector3 Threshold = new(0f, 0f, 3f);

    /// <summary>Room names, which are also the keys <see cref="Hall.Occupy"/> takes.</summary>
    public const string AntechamberRoom = "antechamber";
    public const string ThresholdRoom = "threshold";
    public const string RotundaRoom = "rotunda";
    public const string MaterialsRoom = "materials";
    public const string ScreensRoom = "screens";
    public const string CorridorRoom = "corridor";
    public const string EngineRoom = "engine";
    public const string IlluminatorRoom = "illuminator";

    /// <summary>
    /// The one room in the hall that is not a room and is not on the deck.
    ///
    /// It is where the film's own sixty seconds hang — a planet, a station and six ships, at their own
    /// coordinates and their own scale, mounted at the deck's origin because that is the only place a
    /// world can be put without moving it. It has no entrance and nothing walks into it; the film cuts,
    /// which is the only way anybody gets there. See <see cref="Cut"/>.
    /// </summary>
    public const string ContactRoom = "contact";
}
