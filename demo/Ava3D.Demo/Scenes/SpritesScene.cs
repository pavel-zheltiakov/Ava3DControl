using System.Numerics;
using Ava3D.Demo.Textures;
using Avalonia.Media;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// <see cref="SpriteNode"/> doing the job billboards were invented for: a field of grass, a sky of cloud,
/// and one lamp behind a wall to show the property no geometry has.
/// </summary>
public sealed class SpritesScene : DemoScene
{
    private const float Cycle = 22f;

    private SpriteNode[] _clouds = [];
    private SpriteNode _lamp = null!, _through = null!;

    public override string Title => "Sprites";

    public override string Summary => "Grass, cloud, and the one thing geometry cannot do";

    public override string Notes =>
        """
        Two hundred and forty tufts of grass, eight clouds and a wall. Every tuft and every cloud is one
        SpriteNode — a quad that turns to face the camera, with a texture that has a real silhouette and a
        transparent surround.

        This is what billboards were invented for. A field of grass modelled as geometry is millions of
        triangles for something you only ever read as a shape; as sprites it is 240 four-vertex draws and
        looks better at a distance, because the alpha edge stays sharp where geometry that thin would be
        eaten by the depth buffer. The clouds are the same argument at the other end of the scale.

        Size is in world units, so a tuft near the camera is large and one at the back of the field is
        small — they shrink with distance like everything else. Screen-space sizing would make a cloud
        impossible to place.

        Watch the camera circle. The quads never foreshorten and never turn edge-on, which is the whole
        contract: a sprite node's own rotation and scale are ignored, because a quad that is always
        square-on to the camera has nowhere to put them.

        Two lamps stand behind the wall. The red one is depth tested, so the wall hides it and it reappears
        as the camera comes round the end. The green one has DepthTest = false and never goes away: it
        draws over the wall, which is the marker that says something is there when the thing itself is
        behind cover or too far off to be more than a pixel. Emissive geometry cannot do that — emissive
        geometry is still geometry, and geometry gets occluded.
        """;

    public override SceneLook Look => SceneLook.Blueprint;

    public override bool Animates => true;

    public override bool FramesItself => true;

    public override bool DrivesCamera => true;

    public override void Frame(Camera camera)
    {
        camera.FieldOfView = 48f;
        camera.NearPlane = 0.2f;
        camera.FarPlane = 220f;
    }

    public override Scene Build()
    {
        var scene = new Scene { Background = Color.FromRgb(126, 156, 196) };

        scene.Lights.Clear();
        scene.Lights.Add(new DirectionalLight
        {
            Direction = Vector3.Normalize(new Vector3(-0.45f, -0.72f, -0.52f)),
            Color = new Vector3(1.00f, 0.96f, 0.86f),
            Intensity = 2.3f,
            Ambient = 0.22f,
            AmbientColor = new Vector3(0.62f, 0.72f, 0.92f)
        });

        scene.Environment = new EnvironmentLight
        {
            SkyColor = new Vector3(0.42f, 0.55f, 0.78f),
            GroundColor = new Vector3(0.16f, 0.18f, 0.12f),
            Intensity = 1f
        };

        // The ground, with the turf texture tiled rather than stretched across the whole field.
        scene.Children.Add(new MeshNode(TiledGround(70f, 8f), new Material
        {
            BaseColorTexture = Billboards.Turf(),
            Roughness = 0.95f
        })
        {
            Position = new Vector3(0f, -0.02f, 0f),
            IsPickable = false
        });

        var grass = Billboards.Grass();
        var random = new Random(17);

        // The field. Sprites are anchored at their centre, so a tuft standing on the ground sits at half
        // its own height — the sort of detail that is obvious once and invisible forever after.
        for (var i = 0; i < 240; i++)
        {
            var angle = (float)random.NextDouble() * MathF.Tau;
            var radius = 2.2f + MathF.Sqrt((float)random.NextDouble()) * 24f;
            var height = 0.9f + (float)random.NextDouble() * 0.7f;

            scene.Children.Add(new SpriteNode
            {
                Texture = grass,
                Position = new Vector3(MathF.Cos(angle) * radius, height * 0.5f, MathF.Sin(angle) * radius),
                Size = new Vector2(height * 0.85f, height),
                Color = new Vector3(0.86f + (float)random.NextDouble() * 0.28f),
                Blend = BlendMode.Alpha,
                DepthWrite = false,
                RenderOrder = 1
            });
        }

        // The wall the lamps hide behind.
        scene.Children.Add(new MeshNode(Primitives.Box(7.2f, 3.4f, 0.55f), new Material
        {
            BaseColorTexture = Billboards.Brick(),
            // Knocked back, so an additive glow drawn on top of it has somewhere to go.
            BaseColor = new Vector4(0.62f, 0.62f, 0.62f, 1f),
            Roughness = 0.9f
        })
        {
            Position = new Vector3(0f, 1.7f, -6f)
        });

        var glow = Space.Glow();

        _lamp = Lamp(glow, new Vector3(-2.4f, 1.9f, -7.4f), depthTest: true, new Vector3(1.00f, 0.20f, 0.08f));
        _through = Lamp(glow, new Vector3(2.4f, 1.9f, -7.4f), depthTest: false, new Vector3(0.12f, 1.00f, 0.30f));

        scene.Children.Add(_lamp);
        scene.Children.Add(_through);

        // Cloud, high and wide. Same node type as a blade of grass, three orders of magnitude bigger.
        var cloud = Billboards.Cloud();
        _clouds = new SpriteNode[8];

        for (var i = 0; i < _clouds.Length; i++)
        {
            var angle = i / (float)_clouds.Length * MathF.Tau + 0.4f;
            var radius = 34f + (float)random.NextDouble() * 22f;
            var width = 22f + (float)random.NextDouble() * 20f;

            _clouds[i] = new SpriteNode
            {
                Texture = cloud,
                Position = new Vector3(
                    MathF.Cos(angle) * radius,
                    16f + (float)random.NextDouble() * 9f,
                    MathF.Sin(angle) * radius),
                Size = new Vector2(width, width * 0.5f),
                Opacity = 0.88f,
                Blend = BlendMode.Alpha,
                DepthWrite = false,
                RenderOrder = -1
            };

            scene.Children.Add(_clouds[i]);
        }

        return scene;

        static SpriteNode Lamp(Texture glow, Vector3 position, bool depthTest, Vector3 color) => new()
        {
            Texture = glow,
            Position = position,
            Color = color,
            // Kept well below saturation. An additive glow at full strength over a lit wall clips to
            // white, and a white core is a core with no colour left in it — the tint is still being
            // applied, it is just being clipped away, which is the sort of thing that reads as a bug.
            Size = new Vector2(1.35f),
            Opacity = 0.24f,
            Blend = BlendMode.Additive,
            DepthTest = depthTest,
            DepthWrite = false,
            RenderOrder = 2
        };
    }

    /// <summary>
    /// A ground quad whose texture repeats instead of stretching.
    ///
    /// <see cref="Primitives.Plane"/> runs its UVs 0..1, which is right for a single image and wrong for a
    /// field — one 256-pixel turf tile smeared over seventy metres is a blur. Four vertices with bigger
    /// texture coordinates is the whole fix, and it needs no library feature: wrapping is the texture's
    /// default.
    /// </summary>
    private static Mesh TiledGround(float size, float repeats)
    {
        var h = size * 0.5f;

        return new Mesh
        {
            Positions = [new(-h, 0, -h), new(h, 0, -h), new(h, 0, h), new(-h, 0, h)],
            Normals = [Vector3.UnitY, Vector3.UnitY, Vector3.UnitY, Vector3.UnitY],
            TexCoords = [new(0, 0), new(repeats, 0), new(repeats, repeats), new(0, repeats)],
            Indices = [0, 2, 1, 0, 3, 2],
            Name = "ground"
        };
    }

    public override void Update(Scene scene, Camera camera, double elapsed)
    {
        var t = (float)elapsed;

        // Clouds drift, slowly, and breathe. Nothing here rotates: they cannot.
        for (var i = 0; i < _clouds.Length; i++)
        {
            var cloud = _clouds[i];
            cloud.Position = cloud.Position with { X = cloud.Position.X + 0.35f * (float)(1.0 / 60.0) };
            cloud.Opacity = 0.82f + 0.10f * MathF.Sin(t * 0.3f + i);
        }

        _lamp.Opacity = 0.18f + 0.14f * (0.5f + 0.5f * MathF.Sin(t * 2.1f));
        _through.Opacity = 0.18f + 0.14f * (0.5f + 0.5f * MathF.Sin(t * 2.1f + 1.6f));

        // A slow circle at head height. Circling is what proves the billboards are re-oriented every
        // frame — a static camera cannot tell a facing quad from a painted one.
        var angle = t / Cycle * MathF.Tau;
        var eye = new Vector3(MathF.Sin(angle) * 13.5f, 2.6f, 9f + MathF.Cos(angle) * 4.5f);
        var focus = new Vector3(0f, 1.5f, -3f);

        var offset = eye - focus;
        var distance = offset.Length();

        camera.Target = focus;
        camera.Distance = distance;
        camera.Pitch = MathF.Asin(Math.Clamp(offset.Y / distance, -1f, 1f));
        camera.Yaw = MathF.Atan2(offset.X, offset.Z);

        scene.Invalidate();
    }
}
