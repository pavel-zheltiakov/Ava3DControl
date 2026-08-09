using System;
using System.Collections.Generic;

namespace Ava3D.Demo.Scenes.Arcade;

/// <summary>
/// A maze seen from above, a collector clearing it, and two drones going the other way round.
/// </summary>
public sealed class CoinMazeScene : ArcadeScene
{
    private const int Cell = 8;
    private const int Across = 16;
    private const int Down = 12;

    /// <summary>Cells a second. Seven is brisk without smearing at ten pictures a second.</summary>
    private const float Pace = 7f;

    private const uint Floor = 0x07070F;
    private const uint WallFace = 0x2438C4;
    private const uint WallLit = 0x4E63EE;
    private const uint CoinGold = 0xF5C542;

    /// <summary>
    /// The corridor, as the corners it turns at. Everything else on the grid is wall.
    ///
    /// Written as a route rather than as a picture of a maze, which is the other way round from how a maze
    /// is usually authored and much the better way here. A drawn maze has to be checked for whether a
    /// closed circuit through it exists, and it silently does not the moment a wall is nudged; a circuit
    /// drawn first cannot fail to be one, and the maze that comes out of it is whatever is left over.
    /// </summary>
    private static readonly (int X, int Y)[] Turns =
    [
        (1, 1), (7, 1), (7, 4), (10, 4), (10, 1), (14, 1),
        (14, 10), (8, 10), (8, 7), (5, 7), (5, 10), (1, 10)
    ];

    /// <summary>Every cell of the circuit in order, which is also the order the coins go.</summary>
    private static readonly (int X, int Y)[] Route = Expand();

    private static readonly bool[,] Wall = Walls();

    // Treads, a body, and a lamp on the front. Two frames that differ only in the treads, so it reads as
    // something being driven rather than something wobbling.
    private static readonly Art HunterA = Art.Parse(
        """
        ..3333..
        .333333.
        3.2222.3
        3.2112.3
        .333333.
        .3.33.3.
        """,
        0xFFF2A8, 0xE8B33A, 0xC0C6D8);

    private static readonly Art HunterB = Art.Parse(
        """
        ..3333..
        .333333.
        .32222.3
        3.2112.3
        .333333.
        3.33.33.
        """,
        0xFFF2A8, 0xE8B33A, 0xC0C6D8);

    /// <summary>A drone: one lens under a shell, in two colourways. Same picture, different palette.</summary>
    private const string DroneShape =
        """
        ..1111..
        .111111.
        11122111
        11122111
        .111111.
        .1.11.1.
        """;

    private static readonly Art[] Drones =
    [
        Art.Parse(DroneShape, 0xE0468C, 0xFFFFFF),
        Art.Parse(DroneShape, 0x3AD2D2, 0xFFFFFF)
    ];

    public override string Title => "Coin maze";

    public override string Summary => "A circuit cleared, a lap at a time";

    public override string Notes =>
        """
        The same hundred and twenty-eight by ninety-six buffer as the platformer, drawn a different way:
        a grid of eight-pixel cells, walls where the circuit does not go, and a coin on every cell that it
        does.

        The interesting part is what makes the coins disappear, because nothing here has any state. A coin
        is the nth cell of the circuit, and the collector is at cell number clock × pace; the coin is drawn
        when its number is ahead of the collector's and not drawn when it is behind. There is no eating and
        nothing is removed from anything. Wind the clock back and the maze fills in again.

        That is the discipline the whole folder is under — the picture at time t depends on t and nothing
        else — and it is why the maze can hang on a wall in a film you are allowed to scrub. A game that
        remembered which coins it had eaten would show the wrong maze the moment somebody jumped the
        timeline, and would have to be played from the beginning to be shown at all.

        The maze itself is authored as the route rather than as the walls. A drawn maze has to be checked
        for whether a closed circuit through it exists; a circuit cannot fail to be one, and the walls are
        whatever cells it does not visit.
        """;

    protected override float Loop => Route.Length / Pace;

    /// <summary>
    /// A coin every cell, and a note when a drone gets close.
    ///
    /// The coin is exact and costs nothing to be exact about. A coin <i>is</i> the nth cell of the circuit and
    /// the collector is at cell <c>clock × pace</c> — the same sentence the drawing is built on — so a coin
    /// goes each time that number passes a whole one, seven a second. The picture stops drawing precisely
    /// those coins. Nothing counts anything.
    ///
    /// The near miss is the one thing in this folder that is measured rather than derived: the gap is
    /// evaluated at both ends of the window and reported when it has just closed. Two constant speeds round
    /// one circuit could be solved for the crossing instead, and it would be four lines of modular arithmetic
    /// to save nothing — the frames are a sixtieth of a second and the drones close over a third of one.
    /// </summary>
    public override void Moves(float from, float to, Action<Move, float> made)
    {
        var a = MathF.Max(0f, from);

        foreach (var _ in Ticks(a * Pace, to * Pace, 1f))
            made(Move.Coin, 1f);

        for (var drone = 0; drone < Drones.Length; drone++)
        {
            var was = Gap(a, drone);
            var now = Gap(to, drone);

            // Closing, and just inside two cells. Only on the way in, so one pass is one note rather than
            // two — a drone that has gone by is not a near miss any more.
            if (now < 2f && was >= 2f)
                made(Move.Near, 1f);
        }
    }

    /// <summary>
    /// How far round the circuit a drone is.
    ///
    /// Backwards, and offset, so the two of them sweep towards the collector rather than trailing it.
    /// Slightly different speeds, so the near-misses land somewhere different every lap.
    ///
    /// A method rather than a line inside <see cref="Paint"/> because the sound reads it too, and a drone
    /// drawn by one expression and heard through a second copy of it is two drones the day somebody changes
    /// a speed.
    /// </summary>
    private static float Along(float seconds, int drone) =>
        Wrap(-seconds * (Pace * (0.72f + drone * 0.16f)) + drone * 19f, Route.Length);

    /// <summary>How many cells apart the collector and a drone are, the short way round the circuit.</summary>
    private static float Gap(float seconds, int drone)
    {
        var apart = MathF.Abs(Wrap(seconds * Pace, Route.Length) - Along(seconds, drone));
        return MathF.Min(apart, Route.Length - apart);
    }

    protected override void Paint(PixelCanvas screen, float seconds)
    {
        screen.Clear(Floor);

        for (var y = 0; y < Down; y++)
        for (var x = 0; x < Across; x++)
            if (Wall[x, y])
                Block(screen, x, y);

        // How far round the circuit the collector is, in cells. Everything else on screen is derived from
        // this one number.
        var progress = Wrap(seconds * Pace, Route.Length);

        for (var i = (int)MathF.Ceiling(progress); i < Route.Length; i++)
        {
            var (x, y) = Route[i];
            screen.Fill(x * Cell + 3, y * Cell + 3, 2, 2, CoinGold);
        }

        for (var drone = 0; drone < Drones.Length; drone++)
        {
            var (dx, dy) = Between(Along(seconds, drone));
            screen.Draw(Drones[drone], dx, dy + 1);
        }

        var (hx, hy) = Between(progress);
        screen.Draw(progress % 1f < 0.5f ? HunterA : HunterB, hx, hy + 1);
    }

    /// <summary>The pixel position part-way round the circuit, from one cell to the next.</summary>
    private static (int X, int Y) Between(float along)
    {
        var index = (int)along;
        var into = along - index;

        var (ax, ay) = Route[index % Route.Length];
        var (bx, by) = Route[(index + 1) % Route.Length];

        return (
            (int)MathF.Round((ax + (bx - ax) * into) * Cell),
            (int)MathF.Round((ay + (by - ay) * into) * Cell));
    }

    private static void Block(PixelCanvas screen, int x, int y)
    {
        screen.Fill(x * Cell, y * Cell, Cell, Cell, WallFace);
        screen.Fill(x * Cell, y * Cell, Cell, 1, WallLit);
        screen.Fill(x * Cell, y * Cell, 1, Cell, WallLit);
    }

    /// <summary>The corners expanded into every cell between them, once round and back to the start.</summary>
    private static (int X, int Y)[] Expand()
    {
        var cells = new List<(int X, int Y)>();

        for (var i = 0; i < Turns.Length; i++)
        {
            var (fromX, fromY) = Turns[i];
            var (toX, toY) = Turns[(i + 1) % Turns.Length];

            var stepX = Math.Sign(toX - fromX);
            var stepY = Math.Sign(toY - fromY);

            // The corner itself belongs to this leg and the cell it turns into belongs to the next, so
            // stopping one short is what keeps the circuit free of repeated cells — and a repeated cell
            // would be a coin that is eaten twice a lap.
            for (int x = fromX, y = fromY; x != toX || y != toY; x += stepX, y += stepY)
                cells.Add((x, y));
        }

        return [.. cells];
    }

    /// <summary>Everywhere the circuit does not go, which is the maze.</summary>
    private static bool[,] Walls()
    {
        var wall = new bool[Across, Down];

        for (var y = 0; y < Down; y++)
        for (var x = 0; x < Across; x++)
            wall[x, y] = true;

        foreach (var (x, y) in Route)
            wall[x, y] = false;

        return wall;
    }

    private static float Wrap(float value, float period) => value - period * MathF.Floor(value / period);
}
