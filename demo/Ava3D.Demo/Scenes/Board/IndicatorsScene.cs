using System.Numerics;
using Avalonia.Media;
using Ava3D.Demo.Textures;

namespace Ava3D.Demo.Scenes.Board;

/// <summary>
/// The indicator panel on a timer: translucent lenses, emissive dies, bloom, and light on the copper.
/// </summary>
public sealed class IndicatorsScene : DemoScene
{
    private const float Step = 1.1f;
    private const int Steps = 8;

    /// <summary>How long a lamp takes to come up, as a fraction of a step.</summary>
    private const float Rise = 0.25f;

    private sealed record Lamp(
        Material Lens,
        Material Die,
        Vector3 Color,
        Vector3 At,
        SpriteNode Core,
        SpriteNode Halo,
        string Designator);

    private readonly List<Lamp> _lamps = [];
    private readonly List<PointLight> _lights = [];
    private Node? _root;
    private string _detail = "";
    private string _caption = "";

    public override string Title => "Indicators";

    public override string Summary => "Six LEDs on a flex-linked panel: glass, emission and bloom";

    public override string Notes =>
        $"""
         {_detail}

         The lamps are on a board of their own. That is what the flex jumper is for — a printed circuit
         that is also a cable, joining two rigid boards that are not in the same place — and it is why
         the motherboard's copper stops dead at J1 and starts again at J2 thirty millimetres away.
         The film is polyimide at alpha 0.62 with its ten conductors printed on top of it, and it is the
         other transparent thing in this scene.

         Each indicator is four nodes: a lens, the reflector cup, the die in it, and the two legs it
         stands on. It takes four separate things to make one read as lit.

         The lens is transparent, and that is a property of the file rather than of this scene. It is
         glTF alphaMode BLEND with an alpha of 0.74 in baseColorFactor, which the loader turns into
         Material.Blend = Alpha with DepthWrite off. Without the depth-write half you get a lens that
         hides its own far side and everything inside it, which on a lamp is the entire subject.

         The die is what emits. Material.EmissiveColor is added after lighting and unaffected by it, so
         a chip 0.44 mm across goes white-hot while the board keeps its own shading. The lens carries
         the same colour at a fifth of the strength, which is what fills the dome — a lit indicator is
         not a bright point behind a coloured window, it is a body of glowing epoxy with a core in it.

         The bloom is two additive SpriteNodes per lamp, a tight one and a wide one, at RenderOrder 1 so
         they draw after everything else. A renderer with no post-processing has no glare, and glare is
         most of what a photograph of a lit LED actually shows.

         And the light on the board is a real light. Emission illuminates nothing — an emissive surface
         is bright and the surface beside it is exactly as dark as it was — so each lit lamp gets a
         PointLight at its die, in its own colour, Range 34 mm. Three of them, so three can cast and all
         six can glow. Watch the row during the fill: the pools of colour follow the newest three and the
         older lamps keep their glow with no light under them.

         Three was the renderer's number once and is the scene's own now. Nothing stops six lamps having
         six lights — and the reason this one still has three is that it is also the board on the bench in
         chapter 6, where the row lighting itself three lamps at a time is a shot somebody framed. Which
         is the better argument for a number than a shader ever was.
         """;

    public override SceneLook Look => SceneLook.Studio;

    public override bool Animates => true;

    public override bool FramesItself => true;

    public override TimeSpan TourDuration => TimeSpan.FromSeconds(18);

    public override string? Caption => _caption;

    public override void Frame(Camera camera)
    {
        // On the panel, with the jumper and the motherboard's edge running away to the left.
        camera.Target = new Vector3(1.930f, 0.062f, -0.050f);
        camera.Distance = 0.62f;
        camera.Yaw = 0.45f;
        camera.Pitch = 0.34f;
        camera.FieldOfView = 32f;
        camera.NearPlane = 0.02f;
        camera.FarPlane = 20f;
    }

    /// <summary>
    /// The three lights the panel casts on the board around it, in the order they are handed to the
    /// newest lamps.
    ///
    /// Empty until the panel has been wired — see <see cref="MountOn"/>. They are exposed because a room
    /// has to be able to spend its own slots on them: the engine room's power-up gives three of its four
    /// to these and keeps one on the bench, which is the arithmetic this scene's notes describe, done by
    /// a room rather than by a scene.
    /// </summary>
    public IReadOnlyList<PointLight> Lights => _lights;

    public override Scene Build()
    {
        var scene = new Scene();
        Stage(scene);

        if (BuildSubject() is { } board)
            scene.Children.Add(board);

        foreach (var light in _lights)
            scene.Lights.Add(light);

        return scene;
    }

    /// <summary>
    /// Dimmer than the other board scenes, but not as dim as it first was.
    ///
    /// A lamp is only as bright as what is around it, so the room has to come down for the coloured pools
    /// to read at all — and at 0.55 it came down too far and took the metal with it. Every leg, pad and
    /// flex conductor here is metallic 1, which has no diffuse term whatever: with nothing to reflect they
    /// are black, and a row of indicators standing on two black wires each is not what any photograph of
    /// one shows. The key is what is turned down instead.
    /// </summary>
    public override void Stage(Scene scene)
    {
        scene.Background = Color.FromRgb(8, 9, 12);
        scene.Environment = EnvironmentLight.FromTexture(Environments.Studio(), 0.80f);
        scene.Light.Direction = Vector3.Normalize(new Vector3(-0.40f, -0.86f, -0.32f));
        scene.Light.Color = new Vector3(0.94f, 0.96f, 1.00f);
        scene.Light.Intensity = 0.55f;
        scene.Light.Ambient = 0.015f;
    }

    public override Node? BuildSubject()
    {
        Node root;
        BoardData board;

        try
        {
            (_, root, _detail) = BoardModel.Load();
            board = BoardData.Read();
        }
        catch (Exception e)
        {
            _detail = $"The board failed to load: {e.GetType().Name}: {e.Message}";
            return null;
        }

        foreach (var node in root.Descendants)
        {
            if (node is MeshNode { Material.Name: "board.silk" } silk)
            {
                silk.Material.DepthBias = 1f;
                silk.Material.DepthBiasSlope = 1.4f;
            }
        }

        MountOn(root, board);

        return root;
    }

    /// <summary>
    /// Wires the six lamps, their glows and their three lights onto a board that already exists.
    ///
    /// <see cref="BuildSubject"/> loads one and calls this, which is every path this scene takes on its
    /// own. It is public because the names it hands out are load-bearing outside this file: the engine
    /// room fits the indicator card to the board by hand and gathers the card by name, and the two bloom
    /// sprites per lamp are named after the lamp so they travel with it.
    ///
    /// Call it before the board is put anywhere. It reads the lenses' bounds to find where a glow goes,
    /// and it converts them into the board's own space through <see cref="Node.WorldTransform"/> — which
    /// is exact while the board is still standing at the origin and a conservative box once it is not.
    /// </summary>
    /// <param name="root">The board's root node.</param>
    /// <param name="board">Its manifest, for the designators and the lamp positions.</param>
    public void MountOn(Node root, BoardData board)
    {
        _root = root;
        Collect(root, board);

        // Three, which used to be everything left after the key light and is now a choice — see the
        // notes. They are made once and never added or removed, because a light that comes and goes
        // changes what Scene.Light resolves to and rebuilds the snapshot's light block every time it does.
        for (var i = _lights.Count; i < 3; i++)
            _lights.Add(new PointLight { Intensity = 0f, Range = 0.34f, Decay = 1.5f });
    }

    /// <summary>
    /// Finds the six indicators, gives each its own pair of materials, and hangs two glows over it.
    ///
    /// The lenses already differ — one material per colour, six colours — but the dies share a single
    /// <c>part.led.die</c>, which is right in the file and wrong the moment one of them is lit on its
    /// own. Materials are shared by identity, so setting emissive on that one would light all six at
    /// once, and the fault would look like the sequence being broken rather than like a material being
    /// shared.
    ///
    /// Where the glow goes is read off the lens's own bounds rather than worked out from the layout.
    /// The alternative is for this file to know that a 5 mm LED stands 4 mm clear of the board and is
    /// 8.35 mm tall, which is three constants that live in the build script and would have to be kept
    /// in step with it by hand.
    /// </summary>
    private void Collect(Node root, BoardData board)
    {
        var glow = Space.Glow();

        // Where a glow goes is worked out from a lens's bounds, and the bounds a node reports are in
        // world space — so they have to come back through the root's own transform before they can be
        // used as a position under it. On its own that inverse is the identity and this line is free;
        // mounted in a room it is the difference between six glows on the lamps and six glows at the
        // origin of the building.
        if (!Matrix4x4.Invert(root.WorldTransform, out var intoRoot))
            intoRoot = Matrix4x4.Identity;

        foreach (var part in board.Parts.Where(part => part.Node.StartsWith("led.", StringComparison.Ordinal)))
        {
            if (root.Find<MeshNode>($"{part.Node}.lens") is not { } lens ||
                root.Find<MeshNode>($"{part.Node}.die") is not { } die)
            {
                continue;
            }

            lens.Material = lens.Material.Clone();
            die.Material = die.Material.Clone();

            // The lens's own base colour is the lamp's colour, and taking it from there rather than
            // from a table here is what keeps the light, the glow and the lens the same colour when
            // one of them is changed in the build script.
            var color = new Vector3(lens.Material.BaseColor.X, lens.Material.BaseColor.Y,
                lens.Material.BaseColor.Z);

            var bounds = lens.WorldBounds.Transform(intoRoot);
            var centre = new Vector3((bounds.Min.X + bounds.Max.X) / 2f, 0f,
                (bounds.Min.Z + bounds.Max.Z) / 2f);

            // Named after the lamp they belong to, and the name is load-bearing outside this file: the
            // engine room draws the whole indicator card out of its guides by gathering every node called
            // led.something, and a bloom that stayed behind would be two glows hanging in the air where
            // the card used to be.
            var halo = Glow(glow, color, centre with { Y = Up(bounds, 0.62f) }, $"{part.Node}.halo");
            var core = Glow(glow, color, centre with { Y = Up(bounds, 0.82f) }, $"{part.Node}.core");

            root.Children.Add(halo);
            root.Children.Add(core);

            _lamps.Add(new Lamp(
                lens.Material,
                die.Material,
                color,
                board.At(part.X + part.W / 2f, part.Y + part.D / 2f, board.Thickness + 7.4f),
                core,
                halo,
                part.Designator));
        }
    }

    /// <summary>A height a given fraction of the way up a lens, from its flange to the top of its dome.</summary>
    private static float Up(BoundingBox bounds, float fraction) =>
        bounds.Min.Y + (bounds.Max.Y - bounds.Min.Y) * fraction;

    private static SpriteNode Glow(Texture texture, Vector3 color, Vector3 at, string name) => new()
    {
        Texture = texture,
        Color = color,
        Opacity = 0f,
        Size = Vector2.Zero,
        Blend = BlendMode.Additive,
        Position = at,
        Name = name,

        // After everything, because a bloom is glare and glare is not behind anything. Left at the
        // default it sorts against the lenses by node origin — and every node in this file has its
        // origin at the model's, so that sort is a coin toss and the dome ends up drawn over its own
        // glow about half the time.
        RenderOrder = 1
    };

    public override void Update(Scene scene, double elapsed)
    {
        if (_lamps.Count == 0)
            return;

        var t = (float)(elapsed / Step);
        var step = (int)t % Steps;
        var frac = t - MathF.Floor(t);

        var lit = 0;
        var placed = 0;

        // A light lives on the Scene rather than in the node tree, so nothing transforms it and the
        // board's own transform has to be applied by hand. Read every frame rather than kept, because the
        // story's board is put on a bench after this scene has been wired — and a lamp position baked at
        // build time would be six hundred nodes of board with its lights left behind at the origin of the
        // building. It is the identity when this scene stands on its own.
        var onto = _root?.WorldTransform ?? Matrix4x4.Identity;

        // And how big it is, because three of the numbers below are in world units rather than in the
        // model's. The story mounts this board at fifteen hundredths, which is what makes it a component
        // lying on a bench instead of a wall panel, and at that size a range of thirty-four millimetres is
        // five, a bloom a hundred and fifteen across is seventeen — and the intensity is the one that
        // bites, because a point light falls off as the square of the distance. Six and two thirds closer
        // is forty-four times brighter. Scale it or the row of indicators renders as one white blob.
        var scale = new Vector3(onto.M11, onto.M12, onto.M13).Length();
        var squared = scale * scale;

        // Back to front, so the three lights land on the newest lamps — which is where the eye is
        // during a fill, and the only ordering under which "the row runs out of lights" reads as the
        // budget rather than as a glitch.
        for (var i = _lamps.Count - 1; i >= 0; i--)
        {
            var lamp = _lamps[i];
            var level = Level(i, step, frac);

            lamp.Die.EmissiveColor = lamp.Color * (level * 9.0f);
            lamp.Lens.EmissiveColor = lamp.Color * (level * 1.9f);

            lamp.Halo.Opacity = level * 0.50f;
            lamp.Halo.Size = new Vector2(0.115f, 0.115f) * ((0.55f + level * 0.45f) * scale);
            lamp.Core.Opacity = level * 0.85f;
            lamp.Core.Size = new Vector2(0.048f, 0.048f) * ((0.55f + level * 0.45f) * scale);

            if (level <= 0.02f)
                continue;

            lit++;
            if (placed >= _lights.Count)
                continue;

            var light = _lights[placed++];
            light.Position = Vector3.Transform(lamp.At, onto);
            light.Color = lamp.Color;
            light.Intensity = level * 0.030f * squared;
            light.Range = 0.34f * scale;
        }

        for (var i = placed; i < _lights.Count; i++)
            _lights[i].Intensity = 0f;

        _caption = lit == 0
            ? "all clear"
            : $"{lit} of {_lamps.Count} lit · {placed} point light{(placed == 1 ? "" : "s")}, " +
              $"and the key light makes four";

        scene.Invalidate();
    }

    /// <summary>
    /// How brightly lamp <paramref name="index"/> burns at this point in the sequence, 0 to 1.
    ///
    /// A power-on self test: the lamps fill in one at a time, hold together for a step, and go out
    /// together. Only the one just switched on ramps — the rest are already up — so the sequence reads
    /// as a row filling rather than as a row breathing.
    /// </summary>
    private static float Level(int index, int step, float frac)
    {
        var ramp = Smooth(MathF.Min(frac / Rise, 1f));

        if (step == Steps - 1)
            return 1f - ramp;

        if (index > step)
            return 0f;

        return index == step ? ramp : 1f;

        static float Smooth(float x) => x * x * (3f - 2f * x);
    }
}
