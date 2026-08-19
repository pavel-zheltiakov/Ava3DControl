using System.Numerics;
using Ava3D.Demo.Textures;
using Avalonia.Media;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// One texture, tiled and slid, and an atlas played as a flipbook — the two things a UV transform is for.
/// </summary>
public sealed class UvTransformScene : DemoScene
{
    private Material _scroll = null!;
    private Material _tile = null!;
    private SpriteNode _flip = null!;

    public override string Title => "UV transform";

    public override string Summary => "Tile a wall, scroll a surface, play a sprite sheet";

    public override string Notes =>
        """
        Material.UvScale and Material.UvOffset transform a material's texture coordinates before the
        texture is sampled. One transform for the whole material rather than one per map, because the
        cases that exist want every map moving together — a base colour sliding out from under its own
        normal map is a bug rather than a feature.

        Left: UvScale at 3×2, so the checker repeats three times across and twice down. That needs the
        texture's wrap mode to be a repeating one, or the extra tiles clamp to the edge row.

        Middle: UvOffset animated. This is the cheapest animation in the library — one vector a frame,
        no geometry touched, nothing re-uploaded, no second texture. It is what a conveyor, a waterfall
        and a jet exhaust are.

        Right: UseFrame(4, 4, n) on a SpriteNode, stepping through a sixteen-cell atlas. A flipbook
        without a flipbook: one image holds every frame and the sprite names which to show, so sixteen
        frames cost one upload and one texture binding instead of sixteen of each.

        All four renderers apply it in the vertex shader, so it costs a vertex uniform rather than a
        fragment one and takes nothing from the light budget.
        """;

    public override bool FramesItself => true;

    /// <summary>Framed on the row itself: the staging floor is context, not the subject.</summary>
    public override void Frame(Camera camera)
    {
        camera.Target = new Vector3(0f, 0.0f, 0f);
        camera.Distance = 6.6f;
        camera.Yaw = 0f;
        camera.Pitch = 0.16f;
        camera.NearPlane = 0.3f;
        camera.FarPlane = 60f;
    }

    public override bool Animates => true;

    public override Node BuildSubject()
    {
        var row = new Node { Name = "uv transform" };
        var checker = Procedural.Checker();

        _tile = new Material { Name = "tiled", BaseColorTexture = checker, Roughness = 0.5f };
        _tile.UvScale = new Vector2(3f, 2f);

        _scroll = new Material { Name = "scrolling", BaseColorTexture = checker, Roughness = 0.5f };

        row.Children.Add(new MeshNode(Primitives.Box(1.5f, 1.5f, 1.5f), _tile)
        {
            Position = new Vector3(-2.1f, 0f, 0f),
            RotationDegrees = new Vector3(-14f, 28f, 0f)
        });

        row.Children.Add(new MeshNode(Primitives.Box(1.5f, 1.5f, 1.5f), _scroll)
        {
            RotationDegrees = new Vector3(-14f, 28f, 0f)
        });

        _flip = new SpriteNode
        {
            Texture = Atlas(),
            Position = new Vector3(2.2f, 0f, 0f),
            Size = new Vector2(1.6f, 1.6f),
            Blend = BlendMode.Alpha
        };
        _flip.UseFrame(4, 4, 0);
        row.Children.Add(_flip);

        return row;
    }

    public override void Update(Scene scene, double elapsed)
    {
        // Only the offset moves. The falloff of a real plume would be vertex colours, not this — see the
        // vertex-colour scene, which is the other half of that trick.
        _scroll.UvOffset = new Vector2((float)elapsed * 0.35f, 0f);
        _flip.UseFrame(4, 4, (int)(elapsed * 8));

        scene.Invalidate();
    }

    public override void Stage(Scene scene)
    {
        Staging.Neutral(scene);
        scene.Background = Color.FromRgb(14, 16, 22);
    }

    /// <summary>
    /// A four-by-four sheet of sixteen frames of one animation: a hand sweeping a dial, with the arc
    /// behind it filling as it goes.
    ///
    /// <b>It was a disc that grew, and that is why it was reported as a flashing circle.</b> Sixteen
    /// concentric discs played in a loop is a pulse — every frame is the same shape at a different size,
    /// so nothing in it says "these are frames". What a flipbook has to show is that the <i>content</i>
    /// changes: a hand at sixteen bearings is unmistakably a sequence, and the arc filling behind it says
    /// which way round the sequence runs and where in it you are. Neither costs anything the disc did not.
    ///
    /// Five hundred and twelve rather than two hundred and fifty-six, so a cell is a hundred and
    /// twenty-eight texels instead of sixty-four. The sprite is drawn a metre and a half across at three
    /// metres and fills a good part of the frame; sixty-four texels magnified that far is the blur the
    /// note about it was really objecting to.
    /// </summary>
    private static Texture Atlas(int size = 512)
    {
        const int cells = 4;
        const int frames = cells * cells;

        var pixels = new byte[size * size * 4];
        var cell = size / cells;

        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            var n = y / cell * cells + x / cell;

            var fx = (x % cell) / (float)cell - 0.5f;
            var fy = (y % cell) / (float)cell - 0.5f;

            var r = MathF.Sqrt(fx * fx + fy * fy);

            // Clockwise from twelve o'clock, which is the only direction a dial has ever run.
            var bearing = MathF.Atan2(fx, -fy);
            if (bearing < 0f) bearing += MathF.Tau;

            var reached = bearing / MathF.Tau * frames;

            // One texel of soft edge everywhere rather than a hard test. A binary shape at this
            // magnification is a staircase, and a staircase reads as the atlas being low resolution
            // rather than as the sprite being a shape.
            var soft = (float)cell;

            // The face: a dark disc with a rim, transparent outside it, so a cell reads as an object and
            // not as a tile.
            var face = Math.Clamp((0.44f - r) * soft, 0f, 1f);
            var rim = Math.Clamp((0.44f - r) * soft, 0f, 1f) * Math.Clamp((r - 0.385f) * soft, 0f, 1f);

            // The arc behind the hand: everything the sweep has already passed.
            var track = Math.Clamp((0.36f - r) * soft, 0f, 1f) * Math.Clamp((r - 0.27f) * soft, 0f, 1f);
            var filled = track * (reached <= n + 1f ? 1f : 0f);

            // And the hand itself, a wedge two cells wide about the current bearing.
            var off = MathF.Abs(reached - (n + 0.5f));
            var hand = Math.Clamp((0.30f - r) * soft, 0f, 1f)
                       * Math.Clamp((0.8f - off) * 3f, 0f, 1f);

            var lit = MathF.Min(1f, hand + filled * 0.75f + rim * 0.9f);

            var alpha = MathF.Min(1f, face * 0.55f + lit);

            var o = (y * size + x) * 4;

            pixels[o + 0] = (byte)((0.10f + 0.90f * lit) * 255f);
            pixels[o + 1] = (byte)((0.11f + 0.72f * lit) * 255f);
            pixels[o + 2] = (byte)((0.14f + 0.44f * lit) * 255f);
            pixels[o + 3] = (byte)(alpha * 255f + 0.5f);
        }

        return Texture.FromPixels(pixels, size, size, "atlas", TextureWrap.ClampToEdge);
    }
}
