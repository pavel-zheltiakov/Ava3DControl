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
    /// carried the screen room's east door, which is where the screen room put it. A room between two fixed
    /// openings does not get to choose its own centre, and rounding this to (9, 0, 18) would move both
    /// doorways off the walls they are cut in — which does not read as a slightly wrong number, it reads as
    /// a gap of daylight down the side of a door.
    ///
    /// The west one is a blank wall now. The lounge's way in moved to its own north wall when the studio and
    /// the pattern shop went in between the two rooms, and the lounge itself has since moved seven metres
    /// north — see <see cref="Screens"/>. This centre is still what it was, because the rotunda's door has
    /// not moved and one fixed opening is enough to fix a room.
    /// </summary>
    public static readonly Vector3 Materials = new(8.75f, 0f, 17.875f);

    /// <summary>
    /// The lounge, and <b>it moved seven metres north to shorten a corridor.</b>
    ///
    /// It was at z 24 for one reason: its east wall met the material gallery's west one, back to back, and
    /// the route ran straight through between them. That door has been gone since the studio and the
    /// pattern shop landed — the route goes east out of the gallery now and comes back down the link — and
    /// once a room's only fixed edge stops being fixed, the room is free.
    ///
    /// What it buys is the link. The planetarium is pinned at z 40.2 by its own radius against the pattern
    /// shop's north wall, and the corridor between the two is an L, so its length is the Manhattan distance
    /// between two points and nothing else. Moving this room's north wall from 27.5 to 34.5 takes seven
    /// metres straight off it: the link was fifteen metres nine and is eight metres nine. The alarm
    /// corridor, the engine room and the illuminator all go with it, because all three are measured off
    /// this wall rather than placed — which is what makes a seven-metre move four numbers instead of a
    /// rebuild.
    ///
    /// It costs the plan a void. There are now seven metres of empty deck between the gallery and this
    /// room where there used to be a shared wall, and nothing will ever be built in it. That is the right
    /// trade: a viewer cannot see an empty region of a plan, and they can very much see fifteen metres of
    /// corridor.
    /// </summary>
    public static readonly Vector3 Screens = new(1f, 0f, 31f);

    /// <summary>
    /// The studio, east of the gallery's north half — and the first room in the building that is not on
    /// the way to anywhere the plan already went.
    ///
    /// <b>It is here because the gallery and the lounge share a wall.</b> The route ran west out of the
    /// gallery straight into the lounge, back to back with a quarter of a metre of plaster between them,
    /// and there is no gap between two rooms like that to put a third one in. Moving the lounge north runs
    /// the gallery's exit off the end of its own wall inside two metres; moving it west takes the corridor,
    /// the engine room and the illuminator with it. So the gallery's exit turned round instead: it is in
    /// the east wall now, and the whole deck east of x = 12 was empty.
    ///
    /// Its centre is not a round number for the same reason <see cref="Materials"/>'s is not. The west wall
    /// has to overlap the gallery's east one — every wall in this building interpenetrates the wall it
    /// meets rather than abutting it — and the doorway in it has to be the doorway the gallery cut, so
    /// neither coordinate is free. Two and a half centimetres of overlap is what puts no two faces level.
    /// </summary>
    public static readonly Vector3 Studio = new(15.65f, 0f, 22.475f);

    /// <summary>
    /// The pattern shop, north of the studio and reaching back west over the top of the gallery.
    ///
    /// Same arithmetic, one room along: its south wall overlaps the studio's north one, so its centre is
    /// four metres north of that overlap and not of anything else. The width is chosen — it is the widest
    /// room in the first half of the building, because it is the only one with four exhibits in it — and
    /// the west face lands at x = 6.4, which is as far back toward the lounge as it can come without
    /// standing on the gallery's roof.
    /// </summary>
    public static readonly Vector3 Patterns = new(12.5f, 0f, 31f);

    /// <summary>
    /// The planetarium, north of the pattern shop, and the second round room on the deck.
    ///
    /// It is the only room in the building placed by <i>subtraction</i>. Everything else was fitted against
    /// a doorway that already existed; this one was fitted against the two things it must not touch — the
    /// pattern shop's north wall, which its own wall has to overlap by a hundred millimetres, and the
    /// engine room's south bulkhead three metres further on, which nothing may come within reach of. The
    /// twelve metres between them is the room, and the radius is what was left.
    ///
    /// Round, because the ceiling is a dome and a dome over a square room leaves four corners with no roof
    /// on them. That is not a preference: a spherical cap springs from a circle, and the only square that
    /// circle covers is the one inscribed in it — so a twelve-metre square would want an eighteen-metre
    /// dome, which at this ceiling height is a curve so shallow it reads as a slightly bent lid. The wall
    /// folds instead, exactly as <see cref="Rotunda"/>'s does and by the same twelve sectors of thirty
    /// degrees, which is what lets the dome be a dome.
    ///
    /// Its two doorways are a quarter turn apart — due south to the pattern shop, due west to the link —
    /// which is the rotunda's arrangement again and enforces rule 2 the way the rotunda does: from either
    /// opening the other is ninety degrees off the axis you are walking, so neither shows you what is
    /// through it.
    /// </summary>
    public static readonly Vector3 Planetarium = new(12.2f, 0f, 40.2f);

    /// <summary>
    /// The link: the L that brings him back down out of the planetarium into the lounge.
    ///
    /// It is <see cref="Threshold"/> a second time and it is here for the same reason — the two rooms it
    /// joins are on different axes and a straight run between them would line their doorways up, which is
    /// the one thing the plan never does.
    ///
    /// <b>It used to be four and a half metres and it is now seventeen</b>, and every one of the extra ones
    /// is the planetarium. The lounge's north doorway is at z 27.5 and the pattern shop stands across the
    /// whole deck from x 6.15 north of it, so anything further north than the shop can only be reached up
    /// the strip west of it — and the planetarium's own west doorway is thirteen metres up that strip. The
    /// corridor is what that costs and there is no shorter answer: a room north of a room has to be walked
    /// back past it.
    ///
    /// <b>It was fifteen metres nine and it is eight metres nine</b>, and the difference is not this room:
    /// it is <see cref="Screens"/>, which moved seven metres north. An L between two fixed doorways is the
    /// Manhattan distance between them and there is nothing to be done about it from inside the corridor —
    /// so the only way to shorten this was to move one of the two rooms it joins, and the lounge turned out
    /// to be free. Five metres seven up and three metres two across, with the turn at the top.
    ///
    /// Its origin is the middle of the lounge's new north doorway, on the wall's own centre plane, and
    /// local +Z runs north away from the lounge — so this room, like the passage, can talk in metres from
    /// the door rather than in deck coordinates.
    /// </summary>
    public static readonly Vector3 Link = new(4.2f, 0f, 34.5f);

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
    public static readonly Vector3 Corridor = new(-2.3f, 0f, 34.75f);

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
    public static readonly Vector3 Engine = new(-2.3f, 0f, 56.2f);

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
    public static readonly Vector3 Illuminator = new(-2.3f, 0f, 69.6f);

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
    public const string StudioRoom = "studio";
    public const string PatternRoom = "patterns";
    public const string PlanetariumRoom = "planetarium";
    public const string LinkRoom = "link";
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
