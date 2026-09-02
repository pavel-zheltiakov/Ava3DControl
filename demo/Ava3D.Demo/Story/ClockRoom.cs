using System.Numerics;
using Ava3D.Demo.Scenes;

namespace Ava3D.Demo.Story;

/// <summary>
/// The clock room: the back of a tower dial, one window, and <b>the only room in this building that casts
/// a shadow.</b>
///
/// Every other room here is lit by lamps somebody could point at — see <see cref="Hall"/>, which clears the
/// scene's directional light in its constructor and never puts one back. That is the right default for the
/// inside of a building and it is the reason this room is a room rather than a plinth: a shadow needs a
/// light with a direction, a direction needs somewhere for the light to be coming from, and the only honest
/// somewhere is a hole in a wall. So this room's key light is a sun, it arrives through one opening, and
/// what it draws on the floor is the exhibit.
///
/// <b>It is a mounted scene and not a built room, and it is the largest thing the story mounts.</b> The
/// pattern shop stands a whole scene on a table because that scene is a floor with things on it; this is
/// the same argument one size up. <c>ClockTowerScene</c> is already a room — a floor, a ceiling, three
/// walls and a window — so building a second room around it would be building the same walls twice and
/// then hiding one set. What the story adds is the one thing the scene does not have: a fourth wall.
///
/// The scene is a stage set with its camera side missing, which is right for a scene you orbit and wrong
/// for a room you stand in and turn round in. So the open side is closed here, with the doorway in it, and
/// nothing else about the tower is touched.
///
/// <b>Turned a half turn, so the window faces east.</b> The scene builds its opening in the −X wall and
/// sends the sun through it travelling +X; a half turn about Y puts the glass in the east wall and the beam
/// running west across the floor, which is what the plan needs — see <see cref="Deck.ClockTower"/>. It also
/// buys the thing the plan cares about most: he comes in on the room's east side with the window behind
/// him, so the drawing on the floor is in front of him before the dial that casts it is anywhere in frame.
///
/// <b>What a mounted subject does not get is <c>Stage</c>.</b> That method is where the scene puts its sun,
/// its lantern and its sky, and the story never calls it — so all three are rebuilt here from the two
/// vectors the scene publishes for exactly this. The sun is copied and turned; the lantern is placed at the
/// bulb, at the scale's square, because a point light at a tenth the distance is a hundred times the light.
/// </summary>
internal sealed class ClockRoom
{
    /// <summary>
    /// How far down the tower is stood, and it is the dome that sets it.
    ///
    /// The scene is a room seven metres and a half across and five and a half high, which is a real tower
    /// and is taller than anything on this deck but the engine room. Four metres nine is the planetarium's
    /// crown and the planetarium is the tallest thing in the exhibition half on purpose — see
    /// <see cref="Planetarium"/>, which says so and means it. Eighty-two hundredths brings this ceiling to
    /// four metres four, which is under the dome and over everything else: the tower is still the second
    /// tallest room a visitor walks into, and the dome keeps what it was given.
    ///
    /// Nothing else about the scene moves with it. The dial comes to two metres two across instead of two
    /// metres seven, which is a tower clock either way, and the drawing it throws on the floor is three
    /// metres three instead of four.
    /// </summary>
    public const float Scale = 0.82f;

    /// <summary>Half the clear floor, and the ceiling, once the tower is stood down.</summary>
    public const float Half = ClockTowerScene.RoomHalf * Scale;

    public const float Height = ClockTowerScene.RoomHeight * Scale;

    /// <summary>
    /// The wall the story adds, and why it is six hundred millimetres of masonry rather than the quarter of
    /// a metre every other wall in the building is.
    ///
    /// It is centred on the link's north wall so that it contains it — the engine room's bulkhead trick,
    /// which this building has now paid for four times and which is always the same fault: two walls that
    /// meet face to face are two coplanar surfaces, the depth test has nothing to choose between them, and
    /// what it draws is a checkerboard that changes every time the camera moves. Two hundred millimetres of
    /// thick wall either side of the thin one is enough that no face of the corridor's wall is level with
    /// any face of this one.
    ///
    /// And it is the last wall in the film that is thick enough to be a reveal, which the engine room's
    /// bulkhead was the first of. You walk through six hundred millimetres of stone to get into a tower,
    /// which is what the outside of a tower is like.
    /// </summary>
    private const float Thickness = 0.6f;

    /// <summary>Where the doorway is, which is the link's centre line and not this room's.</summary>
    private static readonly float DoorX = Deck.Link.X - Deck.ClockTower.X;

    /// <summary>
    /// What the sun is worth, and it is the scene's own number because a directional light has no distance
    /// in it to scale.
    /// </summary>
    private const float SunFull = ClockTowerScene.SunIntensity;

    /// <summary>
    /// And what the lantern is worth, which is the scene's number times the square of the scale.
    ///
    /// A point light falls off with the square of the distance, so standing a room down by a factor moves
    /// everything in it that much closer to the lamp and the same intensity arrives that much brighter. The
    /// range goes with the scale linearly for the same reason — it is a distance — and the two together are
    /// what make a mounted lamp light a mounted room the way it lit the room it was tuned in.
    /// </summary>
    private static readonly float LanternFull = ClockTowerScene.LanternFull * Scale * Scale;

    public ClockRoom(Hall hall)
    {
        var root = hall.Add(Deck.ClockRoom, Deck.ClockTower);

        // The tower itself, turned a half turn and stood down. Its own floor is at y = 0 and the room's is
        // too, so it needs no seating: this is the one mounted thing in the building that is not sitting on
        // something.
        Movement = new ClockTowerScene();

        var tower = Movement.BuildSubject() ?? throw new InvalidOperationException(
            "the clock tower has stopped building a subject, and the room has nothing in it");

        tower.Name = "tower";
        tower.Scale = new Vector3(Scale);
        tower.RotationDegrees = new Vector3(0f, 180f, 0f);
        root.Children.Add(tower);

        // The fourth wall, with the doorway in it. The same ashlar the tower's own three walls are cut
        // from — see Finish.Masonry, which the scene draws its walls with — because what it is butted
        // against is a tower, and a fourth wall in plaster would be a fourth wall from a different
        // building. The courses are the building's pitch here and the tower's are the scene's, stood
        // down by the scale; the two never meet along a course, so nothing has to line up.
        var stone = Finish.Masonry();

        var south = Fabric.PiercedWall(
            Half * 2f + 0.8f, Height, Thickness, DoorX, Deck.DoorWidth, Deck.DoorHeight, stone);

        south.Position = new Vector3(0f, 0f, -(Half + Thickness / 2f));
        root.Children.Add(south);

        // And the wall is a bystander to the cloud. When the sun is in and the lantern holds the map,
        // nothing but the movement casts — see ClockTowerScene.Hand, which says why — and this is the one
        // caster in the room the tower did not build.
        Movement.Bystander(south);

        // The sun, which is the scene's own direction turned with the room it lights.
        //
        // It is the first light in the scene's list that casts, which is what Scene.ShadowCastingLight
        // means — so the order this is spent in matters and the chapter spends it first. The other two here
        // are point lights and could not cast anyway.
        var sun = ClockTowerScene.SunDirection;

        Sun = new DirectionalLight
        {
            Direction = new Vector3(-sun.X, sun.Y, -sun.Z),
            Color = new Vector3(1f, 0.92f, 0.76f),
            Intensity = SunFull,
            Ambient = 0.02f,
            CastsShadows = true
        };

        // The lantern over the movement, at the bulb the scene already draws. The glass is part of the
        // mounted subject and is unlit, so it glows whether or not this is on — which is why this is placed
        // at the bulb rather than anywhere convenient, and why moving the movement moves both.
        Lantern = new PointLight
        {
            Position = Deck.ClockTower + Mounted(ClockTowerScene.Lantern),
            Color = new Vector3(1f, 0.66f, 0.32f),
            Intensity = LanternFull,
            Range = ClockTowerScene.LanternRange * Scale,
            Decay = 2f
        };

        // And one small fitting inside the doorway, which is the same lamp the pattern shop hangs inside
        // its own entrance and is there for the same reason: he arrives through a hole in six hundred
        // millimetres of stone, and a hole with nothing lighting its inside faces is a black rectangle
        // rather than a way in. It is dim and it is low, because everything above it is a tower.
        Door = Fabric.Ceiling(
            Deck.ClockTower, new Vector3(DoorX, 2.45f, -Half + 0.85f), 1.5f, 3.4f);

        root.Children.Add(Door.Fixture);
        Door.Dim(0f);

        Daylight(0f);
    }

    /// <summary>The scene, kept so the chapter can run its own clock rather than a copy of it. A chapter
    /// reimplementing an exhibit is an exhibit that can drift from itself.</summary>
    public ClockTowerScene Movement { get; }

    /// <summary>The sun. The one light in this building with a direction, and the one that casts — except
    /// for the seconds the cloud is over it, when the lantern holds the map instead. See
    /// <see cref="Running"/>.</summary>
    public DirectionalLight Sun { get; }

    /// <summary>The lantern over the movement, which is what makes the dark half of the room legible.</summary>
    public PointLight Lantern { get; }

    /// <summary>The fitting inside the doorway.</summary>
    public Lamp Door { get; }

    /// <summary>
    /// How much sun there is, nought to one — the light and the sky outside the window together.
    ///
    /// Both, because they are one thing to look at. The window is an unlit plane and takes no notice of any
    /// light in the scene, so a fade that moved only the directional would empty the beam out of the room
    /// and leave the opening as bright as it ever was, which is a window onto a wall.
    /// </summary>
    public void Daylight(float fraction)
    {
        // The scene owns the sky and the dust in the beam and takes the sun's fraction from here, so
        // that all three go together — see ClockTowerScene.Daylight. What the light is set to is what
        // the scene says is left of that once the weather has had its say.
        Movement.Daylight = fraction;
        Light();
    }

    /// <summary>The sun and the map, from what the scene says the weather is.</summary>
    private void Light()
    {
        Sun.Intensity = SunFull * Movement.Sun;
        Sun.CastsShadows = !Movement.Overcast;
        Lantern.CastsShadows = Movement.Overcast;
    }

    /// <summary>
    /// The clock, running, at whatever second the chapter is at.
    ///
    /// The scene's own <c>Update</c> drives the pendulum, both hands and the escapement; the lantern's
    /// flicker is repeated here because the scene's lantern is the one <c>Stage</c> makes and there is no
    /// <c>Stage</c> on this path. Everything else in that method is a node it built itself.
    /// </summary>
    public void Running(Scene scene, float seconds)
    {
        Movement.Update(scene, seconds);

        Lantern.Intensity = LanternFull * (0.94f + 0.06f * MathF.Sin(seconds * 7.3f) * MathF.Sin(seconds * 2.9f));

        // The cloud. The scene decides when it is over and who holds the map while it is — see the cloud
        // constants in ClockTowerScene, which are timed to this chapter — and this repeats on the room's
        // own lights the lines the scene does on its own, because these are the lights the room is lit by.
        Light();
        scene.ShadowStrength = Movement.Overcast ? Movement.Handed : 1f;
    }

    /// <summary>A point in the scene's own coordinates, in the room's — turned and stood down.</summary>
    private static Vector3 Mounted(Vector3 inScene) =>
        new(-inScene.X * Scale, inScene.Y * Scale, -inScene.Z * Scale);

    /// <summary>The doorway, on the link's centre line, in world coordinates.</summary>
    public static Vector3 Doorway => Deck.ClockTower + new Vector3(DoorX, Deck.Eye, -Half);

    /// <summary>A point on the tower's floor at eye height, in the room's own coordinates.</summary>
    public static Vector3 At(float x, float z) => Deck.ClockTower + new Vector3(x, Deck.Eye, z);

    /// <summary>The middle of the patch of sun, on the floor.</summary>
    public static Vector3 Beam => Deck.ClockTower + new Vector3(-0.3f, 0.06f, -0.15f);

    /// <summary>The back of the dial, in the east wall.</summary>
    public static Vector3 Dial => Deck.ClockTower + new Vector3(Half, 2.9f * Scale, 0f);

    /// <summary>
    /// What the camera aims at for the one shot this room is for, which is not the middle of the dial.
    ///
    /// Aimed at the ring itself the lens comes up level and the floor drops out of the bottom of the
    /// frame — so the shot is a clock, and a clock on a wall is not what anybody came in here for. A metre
    /// off the ground is low enough that the drawing runs up the frame to meet the thing drawing it and
    /// high enough that the top of the ring is still in shot. Both halves or neither: that is the whole
    /// composition and it is worth a second constant.
    /// </summary>
    public static Vector3 Face => Deck.ClockTower + new Vector3(Half, 1.0f, 0f);

    /// <summary>The bob, at the bottom of its rod.</summary>
    public static Vector3 Bob => Deck.ClockTower + new Vector3(0.08f, 1.02f, 0.45f);

    /// <summary>The two wheels on their plinth, in the dark half of the room.</summary>
    public static Vector3 Wheels => Deck.ClockTower + new Vector3(-1.23f, 0.86f, 1.68f);

    /// <summary>The drive weight, hanging down the middle of the tower.</summary>
    public static Vector3 Weight => Deck.ClockTower + new Vector3(1.80f, 1.1f, -1.03f);
}
