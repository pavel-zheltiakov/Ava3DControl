using System;

namespace Ava3D.Demo.Scenes.Arcade;

/// <summary>
/// A grid of pixels to draw a game into, and the RGBA array <see cref="Texture.FromPixels"/> wants, which
/// are the same object.
///
/// There is no intermediate representation on purpose. A canvas is <c>width × height × 4</c> bytes in
/// exactly the layout a texture is defined as — row-major, top-left origin, tightly packed — so
/// <see cref="Freeze"/> is a handover rather than a conversion, and a game that plots a pixel has written
/// into the texture it is about to hand over.
///
/// Colours are <c>0xRRGGBB</c>, which is how a palette is written down everywhere else and therefore how it
/// should be written down here. Alpha is not a colour channel in this world: the screen is opaque, and the
/// only transparency is a sprite's, which is decided per texel by <see cref="Art"/> before anything is
/// plotted.
/// </summary>
public sealed class PixelCanvas(int width, int height)
{
    public int Width { get; } = width;

    public int Height { get; } = height;

    /// <summary>The pixels, RGBA, ready to hand to <see cref="Texture.FromPixels"/>.</summary>
    public byte[] Bytes { get; } = new byte[width * height * 4];

    public void Clear(uint rgb)
    {
        var r = (byte)(rgb >> 16);
        var g = (byte)(rgb >> 8);
        var b = (byte)rgb;

        for (var i = 0; i < Bytes.Length; i += 4)
        {
            Bytes[i] = r;
            Bytes[i + 1] = g;
            Bytes[i + 2] = b;
            Bytes[i + 3] = 255;
        }
    }

    /// <summary>One pixel. Off-canvas coordinates are dropped rather than wrapped or thrown.</summary>
    /// <remarks>
    /// Clipping here rather than at every call site is the whole reason this class earns its place: a
    /// side-scroller draws a column of the level at a negative x for as long as it takes to leave, and a
    /// game that had to check would say so on every line and read as bounds arithmetic rather than as a
    /// level.
    /// </remarks>
    public void Plot(int x, int y, uint rgb)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
            return;

        var at = (y * Width + x) * 4;
        Bytes[at] = (byte)(rgb >> 16);
        Bytes[at + 1] = (byte)(rgb >> 8);
        Bytes[at + 2] = (byte)rgb;
        Bytes[at + 3] = 255;
    }

    /// <summary>A filled rectangle, clipped to the canvas.</summary>
    public void Fill(int x, int y, int w, int h, uint rgb)
    {
        var x1 = Math.Min(Width, x + w);
        var y1 = Math.Min(Height, y + h);

        for (var py = Math.Max(0, y); py < y1; py++)
        for (var px = Math.Max(0, x); px < x1; px++)
            Plot(px, py, rgb);
    }

    /// <summary>A one-pixel outline, for a window frame or the lip of a well.</summary>
    public void Outline(int x, int y, int w, int h, uint rgb)
    {
        Fill(x, y, w, 1, rgb);
        Fill(x, y + h - 1, w, 1, rgb);
        Fill(x, y, 1, h, rgb);
        Fill(x + w - 1, y, 1, h, rgb);
    }

    /// <summary>A sprite, with its transparent texels left alone.</summary>
    /// <param name="art">What to draw.</param>
    /// <param name="x">Left edge, in canvas pixels.</param>
    /// <param name="y">Top edge, in canvas pixels.</param>
    /// <param name="flip">True to mirror it horizontally — which is how a character faces the other way.</param>
    public void Draw(Art art, int x, int y, bool flip = false)
    {
        for (var row = 0; row < art.Height; row++)
        for (var col = 0; col < art.Width; col++)
        {
            if (art.At(flip ? art.Width - 1 - col : col, row) is not { } colour)
                continue;

            Plot(x + col, y + row, colour);
        }
    }

    /// <summary>
    /// The canvas as a texture, with its texels intact.
    ///
    /// <see cref="TextureFilter.Nearest"/> is the entire point. A hundred-and-something pixels magnified
    /// across a screen four hundred pixels wide under a bilinear filter is a smear of averages — every hard
    /// edge that the art is made of, gone. The picture drawn and the picture shown have to be the same one.
    /// </summary>
    public Texture Freeze(string name) =>
        Texture.FromPixels(Bytes, Width, Height, name, TextureWrap.ClampToEdge, TextureFilter.Nearest);
}

/// <summary>
/// A sprite, written out in the source as the picture it is.
///
/// Digits index a palette, and a dot or a space is transparent:
///
/// <code>
/// Art.Parse(
///     """
///     .1111.
///     122221
///     .3..3.
///     """,
///     0x3A2C1E, 0xD8642F, 0x1B1B1B);
/// </code>
///
/// This is the readable form and it is also the compact one: nine colours is more than any of these games
/// uses, the shape is visible at a glance, and an artist changing a pixel changes a character in a string.
/// A byte array of indices would be neither.
/// </summary>
public sealed class Art
{
    private readonly byte[] _indices;
    private readonly uint[] _palette;

    private Art(byte[] indices, int width, int height, uint[] palette)
    {
        _indices = indices;
        _palette = palette;
        Width = width;
        Height = height;
    }

    public int Width { get; }

    public int Height { get; }

    /// <summary>The colour at a texel, or null where the sprite is transparent.</summary>
    public uint? At(int x, int y)
    {
        var index = _indices[y * Width + x];
        return index == 0 ? null : _palette[index - 1];
    }

    /// <summary>
    /// The sprite on its own, as a texture with its surround transparent.
    ///
    /// This is the one place in this folder where alpha is a channel. A screen is opaque and
    /// <see cref="PixelCanvas"/> says so: a sprite drawn into one has already had its transparent texels
    /// decided and dropped, and what lands in the buffer is colour. Lifted off the screen and stood up as a
    /// billboard there is no longer a sky behind it to have been drawn over, so the surround has to survive
    /// as alpha instead.
    ///
    /// Transparent texels are left at zero in all four channels rather than at the colour of anything. Under
    /// <see cref="TextureFilter.Nearest"/> that is invisible either way — no filter ever averages two texels
    /// — and it is the honest value: there is no colour there.
    /// </summary>
    public Texture Sheet(string name)
    {
        var rgba = new byte[Width * Height * 4];

        for (var y = 0; y < Height; y++)
        for (var x = 0; x < Width; x++)
        {
            if (At(x, y) is not { } colour)
                continue;

            var at = (y * Width + x) * 4;
            rgba[at] = (byte)(colour >> 16);
            rgba[at + 1] = (byte)(colour >> 8);
            rgba[at + 2] = (byte)colour;
            rgba[at + 3] = 255;
        }

        return Texture.FromPixels(rgba, Width, Height, name, TextureWrap.ClampToEdge, TextureFilter.Nearest);
    }

    /// <param name="rows">
    /// The picture, one line per row. Lines are padded to the longest, so a row that ends in transparent
    /// texels does not need them typed out.
    /// </param>
    /// <param name="palette">The colours <c>1</c>..<c>9</c> stand for, in order.</param>
    /// <exception cref="ArgumentException">
    /// When a digit has no colour behind it. That is a typo in a picture, and the alternative to throwing
    /// is a sprite with a silently wrong pixel in it, which is the sort of thing nobody finds.
    /// </exception>
    public static Art Parse(string rows, params uint[] palette)
    {
        var lines = rows.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var width = 0;

        foreach (var line in lines)
            width = Math.Max(width, line.TrimEnd('\r').Length);

        var indices = new byte[width * lines.Length];

        for (var y = 0; y < lines.Length; y++)
        {
            var line = lines[y].TrimEnd('\r');

            for (var x = 0; x < line.Length; x++)
            {
                if (line[x] is '.' or ' ' or '0')
                    continue;

                var index = line[x] - '0';
                if (index < 1 || index > palette.Length)
                    throw new ArgumentException(
                        $"'{line[x]}' at row {y}, column {x} has no colour — the palette has {palette.Length}",
                        nameof(rows));

                indices[y * width + x] = (byte)index;
            }
        }

        return new Art(indices, width, lines.Length, palette);
    }
}
