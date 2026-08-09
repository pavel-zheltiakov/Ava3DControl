using System;
using System.Collections.Generic;
using System.Linq;

namespace Ava3D.Demo.Scenes.Arcade;

/// <summary>
/// Falling blocks, played once at startup and replayed off the recording.
/// </summary>
public sealed class BlocksScene : ArcadeScene
{
    private const int Wide = 10;
    private const int Deep = 12;
    private const int Size = 6;

    private const int WellX = 8;
    private const int WellY = 11;

    /// <summary>How long one piece takes from the top of the well to its landing.</summary>
    private const float Fall = 0.55f;

    private const uint Backdrop = 0x0A0A14;
    private const uint Edge = 0x3C4468;
    private const uint Grid = 0x14162A;

    /// <summary>One colour a shape, so the stack says what it is made of.</summary>
    private static readonly uint[] Palette =
        [0x46C8E8, 0xF2C14E, 0xB668D8, 0xE86A54, 0x62C46A];

    /// <summary>
    /// The five shapes, each written as the cells of its first rotation in a four-by-four box. The other
    /// rotations are turned out of these rather than typed, which is both shorter and impossible to get
    /// subtly wrong in one of the twenty entries a table would have.
    /// </summary>
    private static readonly (int X, int Y)[][] Bases =
    [
        [(0, 1), (1, 1), (2, 1), (3, 1)], // a bar
        [(1, 0), (2, 0), (1, 1), (2, 1)], // a square
        [(1, 0), (0, 1), (1, 1), (2, 1)], // a tee
        [(2, 0), (0, 1), (1, 1), (2, 1)], // an ell
        [(1, 0), (2, 0), (0, 1), (1, 1)]  // a step
    ];

    /// <summary>Every shape in every rotation, worked out once.</summary>
    private static readonly (int X, int Y)[][][] Shapes = [.. Bases.Select(Rotations)];

    /// <summary>The whole game, played through before the first frame is drawn. See <see cref="Play"/>.</summary>
    private static readonly Turn[] Recording = Play(26);

    public override string Title => "Falling blocks";

    public override string Summary => "Played once at startup, replayed off the recording";

    public override string Notes =>
        """
        The other two games in this folder compute their picture from the clock with arithmetic. This one
        cannot: where a piece lands depends on every piece before it, and no expression in t is going to
        say what the eleventh one did.

        So it is played first and drawn afterwards. The whole game runs once when the class is loaded —
        twenty-six pieces chosen, placed by a greedy rule that prefers a low landing and punishes buried
        gaps, rows cleared as they fill — and each turn is recorded as the well before the piece, where it
        landed, and the well after. Drawing at time t is then a lookup: which turn, and how far into it.

        That is the general answer whenever the rule "the picture depends on t and nothing else" meets
        something that genuinely has history. Record, then index. It costs a few kilobytes, it makes the
        game seekable, and it makes it identical on every machine and in every run — which is worth more
        here than it sounds, because a screen in a film has to show the same thing at the same moment
        however you arrived at it.

        The five shapes are written once each and turned by code. A rotation table is twenty entries and
        exactly one of them is going to be wrong.
        """;

    protected override float Loop => Recording.Length * Fall;

    /// <summary>
    /// A piece landing every <see cref="Fall"/>, and a row going when the recording says one did.
    ///
    /// The landing is the end of a turn, which is the same instant the next turn begins — so one mark serves
    /// both and there is no arithmetic about where in the well the piece got to. The row is read straight out
    /// of the recording: a turn whose score is higher than the one before it cleared that many rows, at that
    /// turn's landing, and the weight is how many. Four at once is the loudest thing this screen does and it
    /// should be.
    ///
    /// This is the game the folder's own notes call the one that cannot be computed from t, and it is the one
    /// whose sound is the most certain of all four — because "record, then index" makes the history a table,
    /// and a table can be read for what happened as easily as for what is on screen.
    /// </summary>
    public override void Moves(float from, float to, Action<Move, float> made)
    {
        foreach (var turn in Ticks(MathF.Max(0f, from) / Fall, to / Fall, 1f))
        {
            made(Move.Drop, 1f);

            // The turn that just ended against the one before it. Across the loop's own join this comes out
            // negative — the score starts again — and a negative row count is no row, which is the right
            // answer for a game that has gone back to its first piece.
            var rows = Played(turn - 1).Score - Played(turn - 2).Score;

            if (rows > 0)
                made(Move.Clear, rows);
        }

        return;

        Turn Played(long n) => Recording[(int)(((n % Recording.Length) + Recording.Length) % Recording.Length)];
    }

    protected override void Paint(PixelCanvas screen, float seconds)
    {
        var elapsed = seconds / Fall;
        var index = (int)MathF.Floor(elapsed) % Recording.Length;
        if (index < 0)
            index += Recording.Length;

        var into = elapsed - MathF.Floor(elapsed);
        var turn = Recording[index];

        screen.Clear(Backdrop);
        Well(screen, turn.Before);

        // The piece, dropping a whole row at a time. Smooth would be wrong: the well is a grid, and a
        // block halfway between two rows of it is a block that is not in the well.
        var travel = turn.Landing + 4;
        var row = (int)MathF.Round(into * travel) - 4;

        foreach (var (x, y) in Shapes[turn.Shape][turn.Rotation])
            Cell(screen, turn.Column + x, row + y, Palette[turn.Shape]);

        Panel(screen, Recording[(index + 1) % Recording.Length], turn.Score);
    }

    private static void Well(PixelCanvas screen, byte[] board)
    {
        screen.Fill(WellX - 1, WellY - 1, Wide * Size + 2, Deep * Size + 2, Edge);
        screen.Fill(WellX, WellY, Wide * Size, Deep * Size, Grid);

        // A dot at every cell corner. The well is a grid and a block lands on it; without something to land
        // on, an empty well is a rectangle and a stack of three blocks is floating in it.
        for (var y = 0; y <= Deep; y++)
        for (var x = 0; x <= Wide; x++)
            screen.Plot(WellX + x * Size - 1, WellY + y * Size - 1, 0x1E2140);

        for (var y = 0; y < Deep; y++)
        for (var x = 0; x < Wide; x++)
            if (board[y * Wide + x] is var filled and > 0)
                Cell(screen, x, y, Palette[filled - 1]);
    }

    /// <summary>One block, with a lit top-left edge so a stack of them has depth rather than being a mass.</summary>
    private static void Cell(PixelCanvas screen, int column, int row, uint colour)
    {
        if (row < 0)
            return;

        var x = WellX + column * Size;
        var y = WellY + row * Size;

        screen.Fill(x, y, Size - 1, Size - 1, colour);
        screen.Fill(x, y, Size - 1, 1, Lighten(colour));
        screen.Fill(x, y, 1, Size - 1, Lighten(colour));
    }

    /// <summary>What is coming, and how many rows have gone — the two things a player watches.</summary>
    private static void Panel(PixelCanvas screen, Turn next, int score)
    {
        const int left = 78;

        screen.Outline(left, WellY, 34, 30, Edge);

        foreach (var (x, y) in Shapes[next.Shape][next.Rotation])
            screen.Fill(left + 5 + x * Size, WellY + 9 + y * Size, Size - 1, Size - 1, Palette[next.Shape]);

        // The score as a bar rather than as a number, because there is no font in this folder and a bar
        // that grows says the same thing in four lines of code.
        for (var i = 0; i < score && i < 40; i++)
            screen.Fill(left + i % 10 * 3, WellY + 38 + i / 10 * 5, 2, 4, Palette[i % Palette.Length]);
    }

    private static uint Lighten(uint colour)
    {
        var r = Math.Min(255, (int)(colour >> 16) + 46);
        var g = Math.Min(255, (int)((colour >> 8) & 0xFF) + 46);
        var b = Math.Min(255, (int)(colour & 0xFF) + 46);

        return (uint)((r << 16) | (g << 8) | b);
    }

    /// <summary>One piece: the well it fell into, where it went, and the well it left behind.</summary>
    private sealed record Turn(byte[] Before, int Shape, int Rotation, int Column, int Landing, int Score);

    /// <summary>
    /// Plays a whole game and writes down every turn of it.
    ///
    /// The chooser is greedy and one line of arithmetic: land as low as possible, and pay four rows' worth
    /// of penalty for every square it seals over. That is enough to keep the stack flat and to clear rows
    /// at a believable rate, which is all a screen in the background of a room has to do. A well-played
    /// game and a plausible-looking one are not the same problem, and this is the second one.
    /// </summary>
    private static Turn[] Play(int turns)
    {
        var random = new Random(4021);
        var board = new byte[Wide * Deep];
        var score = 0;
        var recorded = new List<Turn>();

        for (var i = 0; i < turns; i++)
        {
            var shape = random.Next(Shapes.Length);
            var best = Choose(board, shape);

            if (best is not var (rotation, column, landing))
            {
                // Topped out. Sweep it and start again, which is what the machine in the corner does.
                board = new byte[Wide * Deep];
                score = 0;
                best = Choose(board, shape);
                (rotation, column, landing) = best!.Value;
            }

            var before = (byte[])board.Clone();

            foreach (var (x, y) in Shapes[shape][rotation])
                board[(landing + y) * Wide + column + x] = (byte)(shape + 1);

            score += Sweep(board);
            recorded.Add(new Turn(before, shape, rotation, column, landing, score));
        }

        return [.. recorded];
    }

    /// <summary>The best rotation, column and landing row for a shape, or null when nothing fits.</summary>
    private static (int Rotation, int Column, int Landing)? Choose(byte[] board, int shape)
    {
        (int Rotation, int Column, int Landing)? best = null;
        var bestScore = float.NegativeInfinity;

        for (var rotation = 0; rotation < Shapes[shape].Length; rotation++)
        {
            var cells = Shapes[shape][rotation];
            var width = cells.Max(c => c.X) + 1;

            for (var column = 0; column + width <= Wide; column++)
            {
                if (Land(board, cells, column) is not { } landing)
                    continue;

                var score = landing - 4f * Buried(board, cells, column, landing);
                if (score <= bestScore)
                    continue;

                bestScore = score;
                best = (rotation, column, landing);
            }
        }

        return best;
    }

    /// <summary>The lowest row the piece can rest at in this column, or null when it does not fit at all.</summary>
    private static int? Land(byte[] board, (int X, int Y)[] cells, int column)
    {
        var height = cells.Max(c => c.Y) + 1;
        int? resting = null;

        for (var top = 0; top + height <= Deep; top++)
        {
            if (cells.Any(c => board[(top + c.Y) * Wide + column + c.X] != 0))
                break;

            resting = top;
        }

        return resting;
    }

    /// <summary>How many empty squares this placement seals under itself.</summary>
    private static int Buried(byte[] board, (int X, int Y)[] cells, int column, int landing)
    {
        var count = 0;

        foreach (var group in cells.GroupBy(c => c.X))
        {
            var lowest = landing + group.Max(c => c.Y);

            for (var y = lowest + 1; y < Deep && board[y * Wide + column + group.Key] == 0; y++)
                count++;
        }

        return count;
    }

    /// <summary>Removes every full row, dropping what was above it. Returns how many went.</summary>
    private static int Sweep(byte[] board)
    {
        var cleared = 0;

        for (var y = Deep - 1; y >= 0; y--)
        {
            var full = true;
            for (var x = 0; x < Wide && full; x++)
                full = board[y * Wide + x] != 0;

            if (!full)
                continue;

            Array.Copy(board, 0, board, Wide, y * Wide);
            Array.Clear(board, 0, Wide);

            cleared++;
            y++; // the row that dropped into this one has not been looked at
        }

        return cleared;
    }

    /// <summary>
    /// A shape's distinct rotations, turned a quarter at a time inside its box and shifted back into the
    /// corner, stopping when it comes round to one already seen — which is how a square gets one and a bar
    /// gets two without either being told so.
    /// </summary>
    private static (int X, int Y)[][] Rotations((int X, int Y)[] cells)
    {
        var all = new List<(int X, int Y)[]>();
        var current = Normalise(cells);

        for (var i = 0; i < 4; i++)
        {
            if (all.Any(seen => seen.SequenceEqual(current)))
                break;

            all.Add(current);
            current = Normalise([.. current.Select(c => (3 - c.Y, c.X))]);
        }

        return [.. all];
    }

    /// <summary>The same cells pushed against the top-left corner, in a fixed order so two can be compared.</summary>
    private static (int X, int Y)[] Normalise((int X, int Y)[] cells)
    {
        var left = cells.Min(c => c.X);
        var top = cells.Min(c => c.Y);

        return [.. cells.Select(c => (X: c.X - left, Y: c.Y - top)).OrderBy(c => c.Y).ThenBy(c => c.X)];
    }
}
