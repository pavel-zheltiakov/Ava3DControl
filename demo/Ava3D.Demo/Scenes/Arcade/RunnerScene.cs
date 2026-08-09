using System;

namespace Ava3D.Demo.Scenes.Arcade;

/// <summary>
/// A night run: one silhouette, two skylines at different speeds, and obstacles to clear or duck.
/// </summary>
public sealed class RunnerScene : ArcadeScene
{
    private const int Period = 240;
    private const float Speed = 40f;
    private const int HeroX = 28;
    private const int GroundTop = 80;

    private const uint Night = 0x0E1230;
    private const uint Far = 0x2A3568;
    private const uint Near = 0x161C42;
    private const uint Underfoot = 0x141A38;
    private const uint Kerb = 0x4E5CA8;
    private const uint Neon = 0x8FE3FF;
    private const uint Hazard = 0xE0507A;

    /// <summary>Where each obstacle is and whether it is jumped over or ducked under.</summary>
    private static readonly (int X, bool Low)[] Hazards =
        [(58, true), (112, false), (156, true), (198, false)];

    /// <summary>Stars, fixed. They twinkle by turning off, which is one bit and reads as a sky.</summary>
    private static readonly (int X, int Y)[] Stars =
    [
        (9, 12), (23, 30), (37, 8), (52, 22), (68, 15), (81, 34),
        (95, 10), (104, 26), (117, 18), (14, 44), (60, 42), (123, 40)
    ];

    // A one-pixel neck. Without it the head and the shoulders are one blob eight pixels wide, and a
    // silhouette with no neck reads as a bollard however good the legs are.
    private static readonly Art RunA = Art.Parse(
        """
        ..1111..
        ..1111..
        ..1111..
        ...11...
        .111111.
        1111111.
        .111111.
        ..1..1..
        ..1..1..
        .11..11.
        """,
        Neon);

    private static readonly Art RunB = Art.Parse(
        """
        ..1111..
        ..1111..
        ..1111..
        ...11...
        .111111.
        .1111111
        .111111.
        ..1111..
        .11...1.
        11....11
        """,
        Neon);

    private static readonly Art Leap = Art.Parse(
        """
        ..1111..
        ..1111..
        ..1111..
        ...11...
        1111111.
        .111111.
        .111111.
        .11..11.
        11....11
        """,
        Neon);

    private static readonly Art Duck = Art.Parse(
        """
        ..111111..
        .11111111.
        1111111111
        .11111111.
        ..11..11..
        .11....11.
        """,
        Neon);

    public override string Title => "Night runner";

    public override string Summary => "Two skylines, one silhouette, no state";

    public override string Notes =>
        """
        The same pixel buffer as the other two, spent on depth instead of on colour. Four layers move at
        four speeds — stars fixed, far skyline at a fifth, near skyline at a half, ground and obstacles at
        full — and that alone is enough to read as distance, which is the whole trick a side-scroller has.

        A silhouette is a good use of a small palette. There are seven colours on screen and the runner is
        one of them, so it is legible at any size without a single pixel spent on detail.

        The run cycle is keyed to distance travelled rather than to the clock, so the feet move because the
        ground does. It is one line and it is the difference between a character running and a character
        doing a running animation while sliding.

        Whether the runner is in the air or crouched is a question about how far away the next obstacle is,
        not about anything that happened earlier — same as the platformer, and for the same reason: the
        picture at any moment has to be computable from that moment alone.
        """;

    protected override float Loop => Period / Speed;

    /// <summary>
    /// Four hazards, two of them jumped and two of them gone under.
    ///
    /// Read off the same table and the same two marks <see cref="Silhouette"/> uses — from twenty-four world
    /// pixels short to ten past — so what is heard is what is drawn rather than a copy of it timed to match.
    ///
    /// A duck reports its start and no end, and that is not laziness. Coming up out of a crouch makes no
    /// noise; leaving the ground and hitting it again both do, which is why the jump has two marks and this
    /// has one.
    /// </summary>
    public override void Moves(float from, float to, Action<Move, float> made)
    {
        var a = MathF.Max(0f, from) * Speed;
        var b = to * Speed;

        foreach (var (x, low) in Hazards)
        {
            if (!low)
            {
                if (Passed(a, b, x - 24f, Period))
                    made(Move.Duck, 1f);

                continue;
            }

            if (Passed(a, b, x - 24f, Period))
                made(Move.Jump, 1f);

            if (Passed(a, b, x + 10f, Period))
                made(Move.Land, 1f);
        }
    }

    protected override void Paint(PixelCanvas screen, float seconds)
    {
        var travelled = seconds * Speed;
        var scroll = (int)MathF.Round(Wrap(travelled, Period));

        screen.Clear(Night);
        Sky(screen, scroll);

        // Far first, and the far layer is the lighter one. That is the way round haze works and the way
        // round it has to be drawn: a near silhouette lighter than the one behind it reads as a hole.
        Skyline(screen, travelled * 0.2f, 210, Far, 9, 17);
        Skyline(screen, travelled * 0.55f, 187, Near, 15, 26);

        screen.Fill(0, GroundTop, screen.Width, screen.Height - GroundTop, Underfoot);
        screen.Fill(0, GroundTop, screen.Width, 1, Kerb);

        // Dashes on the road, keyed to world position so they travel with it.
        for (var x = 0; x < screen.Width; x++)
            if ((x + scroll) % 12 < 5)
                screen.Plot(x, GroundTop + 5, Kerb);

        for (var copy = -1; copy <= 1; copy++)
        foreach (var (x, low) in Hazards)
        {
            var at = x + copy * Period - scroll + HeroX;

            if (low)
            {
                screen.Fill(at, GroundTop - 8, 6, 8, Hazard);
                screen.Fill(at, GroundTop - 8, 6, 1, 0xFFA8C0);
            }
            else
            {
                // A bar hanging with a gap beneath it: the obstacle you go under rather than over.
                screen.Fill(at, GroundTop - 26, 5, 12, Hazard);
                screen.Fill(at, GroundTop - 26, 5, 1, 0xFFA8C0);
            }
        }

        Silhouette(screen, travelled, scroll);
    }

    private static void Silhouette(PixelCanvas screen, float travelled, int scroll)
    {
        var lift = 0f;
        var ducking = false;

        foreach (var (x, low) in Hazards)
        {
            var ahead = Wrap(x - travelled, Period);
            if (ahead > Period / 2f)
                ahead -= Period;

            if (ahead is <= -10f or >= 24f)
                continue;

            if (low)
            {
                var through = (24f - ahead) / 34f;
                lift = MathF.Max(lift, 4f * through * (1f - through) * 24f);
            }
            else
            {
                ducking = true;
            }
        }

        var height = (int)MathF.Round(lift);

        if (ducking && height == 0)
        {
            screen.Draw(Duck, HeroX - 1, GroundTop - Duck.Height);
            return;
        }

        var art = height > 0 ? Leap : scroll / 4 % 2 == 0 ? RunA : RunB;
        screen.Draw(art, HeroX, GroundTop - art.Height - height);
    }

    private static void Sky(PixelCanvas screen, int scroll)
    {
        // The moon does not move. Nothing that far away does, and a moon drifting past at skyline speed is
        // the first thing that makes a parallax look wrong.
        for (var y = -6; y <= 6; y++)
        for (var x = -6; x <= 6; x++)
            if (x * x + y * y <= 38)
                screen.Plot(104 + x, 18 + y, 0xF0EAD0);

        screen.Fill(101, 14, 3, 3, 0xD8D0B0);
        screen.Fill(107, 20, 4, 3, 0xD8D0B0);

        foreach (var (x, y) in Stars)
            if ((x + y + scroll / 9) % 5 != 0)
                screen.Plot(x, y, (x + y) % 3 == 0 ? 0x8899CCu : 0xFFFFFFu);
    }

    /// <summary>
    /// A row of towers, at whatever speed and colour this layer runs at.
    ///
    /// Widths and heights come out of the position rather than out of a table, which keeps a skyline to
    /// four lines and means the two layers can be the same code with different numbers. It is not random —
    /// the same tower is the same height every lap, because the expression only depends on where it is.
    /// </summary>
    private static void Skyline(
        PixelCanvas screen, float travelled, int period, uint colour, int shortest, int tallest)
    {
        var drift = (int)MathF.Round(Wrap(travelled, period));

        for (var copy = -1; copy <= 1; copy++)
        for (var tower = 0; tower * 11 < period; tower++)
        {
            var world = tower * 11;
            var height = shortest + world * 7 % (tallest - shortest + 1);
            var left = world + copy * period - drift;

            screen.Fill(left, GroundTop - height, 9, height, colour);

            // Windows on the far layer only. Distance is being carried entirely by tone here, and a lit
            // window on the near silhouette would put the brightest thing on screen in front of the
            // darkest — which undoes the layering the two colours just bought.
            if (colour != Far)
                continue;

            screen.Plot(left + 2, GroundTop - height + 3, 0xFFE9A8u);
            screen.Plot(left + 6, GroundTop - height + 7, 0xFFE9A8u);
        }
    }

    private static float Wrap(float value, float period) => value - period * MathF.Floor(value / period);
}
