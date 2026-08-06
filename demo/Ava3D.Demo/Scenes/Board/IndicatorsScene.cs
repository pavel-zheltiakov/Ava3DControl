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
         PointLight at its die, in its own colour, Range 34 mm. A scene holds four lights and the key is
         one of them, so three can cast and all six can glow. Watch the row during the fill: the pools
         of colour follow the newest three and the older lamps keep their glow with no light under them.
         That is the constraint, not a simplification, and the sequence is built around it.
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

    public override Scene Build()
    {
        Scene scene;
        Node root;
        BoardData board;

        try
        {
            (scene, root, _detail) = BoardModel.Load();
            board = BoardData.Read();
        }
        catch (Exception e)
        {
            scene = new Scene();
            _detail = $"The board failed to load: {e.GetType().Name}: {e.Message}";
            scene.Background = Color.FromRgb(14, 16, 20);
            return scene;
        }

        scene.Background = Color.FromRgb(8, 9, 12);

        // Dimmer than the other board scenes, but not as dim as it first was. A lamp is only as bright
        // as what is around it, so the room has to come down for the coloured pools to read at all —
        // and at 0.55 it came down too far and took the metal with it. Every leg, pad and flex
        // conductor here is metallic 1, which has no diffuse term whatever: with nothing to reflect
        // they are black, and a row of indicators standing on two black wires each is not what any of
        // those photographs show. The key is what is turned down instead.
        scene.Environment = EnvironmentLight.FromTexture(Environments.Studio(), 0.80f);
        scene.Light.Direction = Vector3.Normalize(new Vector3(-0.40f, -0.86f, -0.32f));
        scene.Light.Color = new Vector3(0.94f, 0.96f, 1.00f);
        scene.Light.Intensity = 0.55f;
        scene.Light.Ambient = 0.015f;

        foreach (var node in root.Descendants)
        {
            if (node is MeshNode { Material.Name: "board.silk" } silk)
            {
                silk.Material.DepthBias = 1f;
                silk.Material.DepthBiasSlope = 1.4f;
            }
        }

        Collect(root, board);

        // Three, and the collection is full at four. They stay in the scene with Intensity 0 rather
        // than being added and removed, because a light that comes and goes changes what Scene.Light
        // resolves to and rebuilds the snapshot's light block every time it does.
        for (var i = 0; i < 3; i++)
        {
            var light = new PointLight { Intensity = 0f, Range = 0.34f, Decay = 1.5f };
            _lights.Add(light);
            scene.Lights.Add(light);
        }

        return scene;
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

            var bounds = lens.WorldBounds;
            var centre = new Vector3((bounds.Min.X + bounds.Max.X) / 2f, 0f,
                (bounds.Min.Z + bounds.Max.Z) / 2f);

            var halo = Glow(glow, color, centre with { Y = Up(bounds, 0.62f) });
            var core = Glow(glow, color, centre with { Y = Up(bounds, 0.82f) });

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

    private static SpriteNode Glow(Texture texture, Vector3 color, Vector3 at) => new()
    {
        Texture = texture,
        Color = color,
        Opacity = 0f,
        Size = Vector2.Zero,
        Blend = BlendMode.Additive,
        Position = at,

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
            lamp.Halo.Size = new Vector2(0.115f, 0.115f) * (0.55f + level * 0.45f);
            lamp.Core.Opacity = level * 0.85f;
            lamp.Core.Size = new Vector2(0.048f, 0.048f) * (0.55f + level * 0.45f);

            if (level <= 0.02f)
                continue;

            lit++;
            if (placed >= _lights.Count)
                continue;

            var light = _lights[placed++];
            light.Position = lamp.At;
            light.Color = lamp.Color;
            light.Intensity = level * 0.030f;
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
