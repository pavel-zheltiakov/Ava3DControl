using System;

namespace Ava3D.Demo.Scenes.Arcade;

/// <summary>
/// Grass and bricks: a side-scrolling platformer, running itself, in a hundred and twenty-eight pixels.
/// </summary>
public sealed class GrassBricksScene : ArcadeScene
{
    /// <summary>How wide the level is before it comes round again, in world pixels.</summary>
    private const int Period = 320;

    /// <summary>World pixels a second. Ten a second at 3.2 pixels a step — visibly a step.</summary>
    private const float Speed = 32f;

    /// <summary>Where the runner stands on screen. He never leaves it; the level moves past him.</summary>
    private const int HeroX = 36;

    private const int GroundTop = 78;

    /// <summary>
    /// The sky, the grass and the earth under it.
    ///
    /// Public where the rest of the palette is not, because these three are the only ones anything outside
    /// this file could need: they are what the world <i>is</i> rather than what is standing in it, and the
    /// story's diorama builds a strip of real ground under a real sky out of them. A second copy of three
    /// hex numbers in another file would be three numbers that can drift out of agreement with the game
    /// they are supposed to be a model of.
    /// </summary>
    public const uint Sky = 0x6BA8F5;

    public const uint Turf = 0x3FA34D;

    public const uint Soil = 0x8B5A2B;

    private const uint TurfLit = 0x66C96B;
    private const uint SoilDark = 0x6B4220;
    private const uint BrickFace = 0xB25F35;
    private const uint Mortar = 0x6E3618;
    private const uint PipeBody = 0x2E9E58;
    private const uint PipeLit = 0x59D07F;
    private const uint PipeDark = 0x1B6437;

    /// <summary>Left edge and height of each pipe, in world pixels. The runner jumps these.</summary>
    private static readonly (int X, int Height)[] Pipes = [(74, 16), (168, 22), (256, 16)];

    /// <summary>A run of brick blocks: left edge, top edge, how many. Scenery, and cover for the coins.</summary>
    private static readonly (int X, int Y, int Count)[] Bricks =
        [(96, 44, 4), (152, 32, 2), (208, 46, 3), (288, 38, 3)];

    private static readonly int[] Coins = [104, 120, 216, 232, 296];

    /// <summary>Hills, spaced inside their own period. Two of them a period apart would be one hill.</summary>
    private static readonly int[] Hills = [16, 92, 158];

    private static readonly Art Cloud = Art.Parse(
        """
        ..111...
        11111111
        .111111.
        """,
        0xFFFFFF);

    // Eight wide and twelve tall, which is as small as a figure can be and still have a stride. The two
    // running frames differ only in the legs; the arms and body are the same texels in both, so the eye
    // reads one character moving rather than two characters alternating.
    private static readonly Art RunA = Art.Parse(
        """
        ..1111..
        .111111.
        .122221.
        ..2222..
        .333333.
        2333332.
        .333333.
        ..3333..
        ..4444..
        ..4..4..
        ..4..4..
        .55..55.
        """,
        0xF2C14E, 0xE8A87C, 0x2F8F5B, 0x27405E, 0x3A2A1E);

    private static readonly Art RunB = Art.Parse(
        """
        ..1111..
        .111111.
        .122221.
        ..2222..
        .333333.
        .3333332
        .333333.
        ..3333..
        ..4444..
        ..4444..
        .44...4.
        55....55
        """,
        0xF2C14E, 0xE8A87C, 0x2F8F5B, 0x27405E, 0x3A2A1E);

    private static readonly Art Jump = Art.Parse(
        """
        ..1111..
        .111111.
        .122221.
        ..2222..
        2333332.
        .333333.
        .333333.
        ..3333..
        ..4444..
        .44..44.
        .5....5.
        .55..55.
        """,
        0xF2C14E, 0xE8A87C, 0x2F8F5B, 0x27405E, 0x3A2A1E);

    /// <summary>
    /// Everything on this screen that is a sprite, in the order a stride goes.
    ///
    /// Four, and that is not a shortlist — it is all of them. Everything else up there is rectangles: the
    /// bricks, the pipes, the coins and the hills are <see cref="PixelCanvas.Fill"/> calls with numbers in
    /// them, which is what a tile is. A sprite is a picture with a silhouette and a hole around it, and this
    /// game has a runner and a cloud.
    ///
    /// It is here rather than wherever it is used because the art belongs to the game. The story stands
    /// these up in a case as billboards — see <c>Story/Diorama</c> — and what it stands up is these
    /// instances, not copies of them, so a pixel edited in the picture above is edited in both places.
    /// </summary>
    public static IReadOnlyList<(string Name, Art Art)> Cast { get; } =
        [("run", RunA), ("stride", RunB), ("jump", Jump), ("cloud", Cloud)];

    public override string Title => "Grass and bricks";

    public override string Summary => "A platformer, playing itself, in a texture";

    public override string Notes =>
        """
        A hundred and twenty-eight pixels by ninety-six, drawn a texel at a time into a byte array and
        handed to Texture.FromPixels every tenth of a second. There is no 3D here at all beyond the quad it
        lands on — the scene is one plane, one unlit material and one texture that is thrown away and
        rebuilt ten times a second.

        Two things make it look like what it looks like. The first is the filter: the texture asks for
        TextureFilter.Nearest, so a texel magnified across thirty screen pixels stays a hard square. Under
        the default bilinear filter the same array is a smear, and every edge the art is made of is gone.

        The second is the rate. Ten pictures a second is slower than anything renders, and motion landing
        on a grid of tenths is what separates this from a modern game wearing chunky pixels — the runner
        crosses the screen in steps you can count.

        Nothing here is played and nothing accumulates. The runner's position is the clock times a speed;
        his jump is a parabola of how far he is from the next pipe. Ask for the picture at four minutes in
        and you get it without having drawn the first one, which is what lets this same file hang on a
        television in the story and already be mid-game when you walk into the room.
        """;

    protected override float Loop => Period / Speed;

    /// <summary>
    /// Three pipes, and a jump and a landing at each.
    ///
    /// The marks are the same two numbers <see cref="Runner"/> draws the parabola between, read out of the
    /// same table of pipes: airborne from twenty-six world pixels short of a pipe to fourteen past it. So the
    /// sound is not synchronised to the picture, it is <i>derived from the same expression as the picture</i>,
    /// and the only way for the two to disagree is for somebody to change one of the two numbers here and not
    /// there — which is why they are named in the comment above rather than repeated as constants of their
    /// own that could quietly drift.
    ///
    /// <b>Nothing is reported for the coins</b>, deliberately, and it is the interesting decision in this
    /// override. They spin, they scroll past, and they are still there afterwards — the runner never touches
    /// one. A pickup sound over a coin that stays on the screen is the exact fault this whole change is
    /// about, and it would be the loudest one in the room: a noise for something that visibly did not happen.
    /// </summary>
    public override void Moves(float from, float to, Action<Move, float> made)
    {
        var a = MathF.Max(0f, from) * Speed;
        var b = to * Speed;

        foreach (var (x, _) in Pipes)
        {
            if (Passed(a, b, x - 26f, Period))
                made(Move.Jump, 1f);

            if (Passed(a, b, x + 14f, Period))
                made(Move.Land, 1f);
        }
    }

    protected override void Paint(PixelCanvas screen, float seconds)
    {
        // Where the level has got to. Rounded to a whole pixel, because the level is made of pixels and a
        // brick that lands on a fraction of one would shimmer along its edge as it scrolls.
        var travelled = seconds * Speed;
        var scroll = (int)MathF.Round(Wrap(travelled, Period));

        screen.Clear(Sky);
        Clouds(screen, travelled);
        Hillside(screen, travelled);

        // Three copies of the level: the one on screen, the one behind and the one ahead. Only two can ever
        // be visible at once — the period is wider than the screen — and drawing the third costs nothing
        // next to the arithmetic of working out which two.
        for (var copy = -1; copy <= 1; copy++)
        {
            var offset = copy * Period - scroll + HeroX;

            foreach (var (x, y, count) in Bricks)
                for (var i = 0; i < count; i++)
                    Brick(screen, x + offset + i * 8, y);

            foreach (var (x, height) in Pipes)
                Pipe(screen, x + offset, height);

            foreach (var x in Coins)
                Coin(screen, x + offset, 30, scroll);
        }

        Grass(screen, scroll);
        Runner(screen, travelled, scroll);
    }

    /// <summary>
    /// The runner, and the only decision in this file that is really about animation.
    ///
    /// He is airborne as a function of where the nearest pipe is rather than of when he last pressed
    /// anything, which is the whole trick: the jump is a shape laid over the gap between him and the
    /// obstacle, so it lands correctly whatever moment you ask for.
    /// </summary>
    private static void Runner(PixelCanvas screen, float travelled, int scroll)
    {
        var lift = 0f;

        foreach (var (x, _) in Pipes)
        {
            // Signed distance to the pipe, in world pixels, taken the short way round the loop.
            var ahead = Wrap(x - travelled, Period);
            if (ahead > Period / 2f)
                ahead -= Period;

            // Off the ground from twenty-six pixels out to fourteen past, peaking just before the pipe.
            if (ahead is <= -14f or >= 26f)
                continue;

            var through = (26f - ahead) / 40f;
            lift = MathF.Max(lift, 4f * through * (1f - through) * 30f);
        }

        var height = (int)MathF.Round(lift);
        var art = height > 0 ? Jump : scroll / 4 % 2 == 0 ? RunA : RunB;

        screen.Draw(art, HeroX, GroundTop - 12 - height);
    }

    /// <summary>Sky furniture, at a quarter speed. Parallax is one division and it is what makes it deep.</summary>
    private static void Clouds(PixelCanvas screen, float travelled)
    {
        var drift = (int)MathF.Round(Wrap(travelled * 0.25f, 176));

        for (var copy = -1; copy <= 1; copy++)
        {
            screen.Draw(Cloud, 18 + copy * 176 - drift, 14);
            screen.Draw(Cloud, 96 + copy * 176 - drift, 24);
            screen.Draw(Cloud, 140 + copy * 176 - drift, 10);
        }
    }

    /// <summary>Hills, at half speed, sitting on the horizon behind everything.</summary>
    private static void Hillside(PixelCanvas screen, float travelled)
    {
        var drift = (int)MathF.Round(Wrap(travelled * 0.5f, 220));

        for (var copy = -1; copy <= 1; copy++)
        foreach (var x in Hills)
        {
            var left = x + copy * 220 - drift;

            // A stack of widening bars, narrow at the top. Fourteen of them read as a dome at this size,
            // and a circle would cost trigonometry to say the same thing less clearly.
            for (var row = 0; row < 14; row++)
                screen.Fill(left + 13 - row, GroundTop - 14 + row, 8 + row * 2, 1, 0x2E7D46);
        }
    }

    private static void Grass(PixelCanvas screen, int scroll)
    {
        screen.Fill(0, GroundTop, screen.Width, screen.Height - GroundTop, Soil);
        screen.Fill(0, GroundTop, screen.Width, 4, Turf);
        screen.Fill(0, GroundTop, screen.Width, 1, TurfLit);

        // Blades on the turf and stones in the soil, keyed to their world position rather than their screen
        // position — so the speckle travels with the ground instead of crawling on top of it.
        for (var x = 0; x < screen.Width; x++)
        {
            var world = x + scroll;

            if (world % 7 == 0)
                screen.Plot(x, GroundTop - 1, Turf);

            if (world % 11 == 0)
                screen.Fill(x, GroundTop + 7 + world % 5, 2, 2, SoilDark);
        }
    }

    private static void Brick(PixelCanvas screen, int x, int y)
    {
        screen.Fill(x, y, 8, 8, BrickFace);
        screen.Fill(x, y, 8, 1, Mortar);
        screen.Fill(x, y + 4, 8, 1, Mortar);

        // The vertical joints, offset every other course, which is the whole of why brickwork reads as
        // brickwork rather than as a grid.
        screen.Fill(x + 4, y + 1, 1, 3, Mortar);
        screen.Fill(x, y + 5, 1, 3, Mortar);
    }

    private static void Pipe(PixelCanvas screen, int x, int height)
    {
        var top = GroundTop - height;

        screen.Fill(x + 1, top + 5, 12, height - 5, PipeBody);
        screen.Fill(x + 2, top + 5, 2, height - 5, PipeLit);
        screen.Fill(x + 11, top + 5, 2, height - 5, PipeDark);

        // The lip: wider than the shaft, which is the shape that says pipe.
        screen.Fill(x, top, 14, 5, PipeBody);
        screen.Fill(x + 1, top + 1, 2, 3, PipeLit);
        screen.Fill(x + 11, top + 1, 2, 3, PipeDark);
        screen.Outline(x, top, 14, 5, PipeDark);
    }

    /// <summary>
    /// A coin, spinning. Four frames, and the two narrow ones are the same picture — an edge-on disc has no
    /// front and no back, which is exactly what makes the cycle read as a rotation rather than a pulse.
    /// </summary>
    private static void Coin(PixelCanvas screen, int x, int y, int scroll)
    {
        var width = (scroll / 3 % 4) switch { 0 => 6, 1 => 4, 2 => 2, _ => 4 };
        var left = x + (6 - width) / 2;

        // Corners knocked off the top and bottom rows, which is the whole difference between a coin and a
        // yellow rectangle at this size. The edge-on frame is two pixels wide and loses them to the inset,
        // which is correct — a disc seen edge-on has no corners either.
        screen.Fill(left + 1, y, width - 2, 1, 0xFFE58A);
        screen.Fill(left, y + 1, width, 4, 0xF5C542);
        screen.Fill(left, y + 1, 1, 4, 0xFFE58A);
        screen.Fill(left + width - 1, y + 1, 1, 4, 0xB07C1C);
        screen.Fill(left + 1, y + 5, width - 2, 1, 0xB07C1C);
    }

    /// <summary>The positive remainder. C#'s <c>%</c> gives a negative one for a negative left-hand side,
    /// and a level that jumps a period wide when the clock goes past zero is not a level.</summary>
    private static float Wrap(float value, float period) => value - period * MathF.Floor(value / period);
}
