using System.Numerics;
using Avalonia.Media;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// A scene drawn into a texture, and that texture hung on a surface inside another scene.
/// </summary>
public sealed class RenderToTextureScene : DemoScene
{
    public override string Title => "Render to texture";

    public override string Summary => "A scene inside a scene, and a thumbnail of anything";

    public override string Notes =>
        """
        OfflineRenderer.Render turns a Scene and a Camera into a Texture. The panel on the left is showing
        one: a second scene — three shapes and a light of its own — drawn at 512×288 and put on a material
        as an ordinary base-colour map. The right-hand panel is the same second scene from a different
        camera, which is all the difference between them.

        This is render-to-texture as something a caller does, rather than as a second pass inside a frame,
        and that is the whole design. A live nested render needs a second target on three graphics
        backends and a rule about a scene sampling a texture the same frame drew it — a week in the frame
        path and a class of ordering bug that shows up on one driver and not the others. Handing back an
        image the caller owns needs neither, and it covers what desktop applications actually ask for: a
        model thumbnail generated once and cached, a screen inside a scene refreshed when something
        changes, a picture to save or print at a size that has nothing to do with the window, and a render
        from a test or a command line where there is no window at all.

        It runs on the CPU renderer on every platform, which is the point rather than a limitation: that
        is the one backend needing no graphics context, so this works headless, in a unit test and on a
        background thread, and gives the same pixels everywhere. What it costs is speed and the things
        that renderer cannot do — per-pixel lighting, most of the maps, anti-aliasing. The renderer panel
        lists them.

        The size is honoured exactly or refused: asking for more than four megapixels throws rather than
        quietly handing back something smaller, because a caller asking for a size usually needs it.
        """;

    public override bool FramesItself => true;

    public override void Frame(Camera camera)
    {
        camera.Target = new Vector3(0f, 0f, 0f);
        camera.Distance = 7.2f;
        camera.Yaw = 0f;
        camera.Pitch = 0.14f;
        camera.NearPlane = 0.3f;
        camera.FarPlane = 40f;
    }

    public override Node BuildSubject()
    {
        var wall = new Node { Name = "two screens" };
        var inner = InnerScene();

        // <b>The subject itself, standing between the two panels.</b>
        //
        // The note was that this scene is not clear what it is showing, and it is right: two dark
        // rectangles with a sphere and a box in them say nothing at all about where those pictures came
        // from. What was missing is the thing being photographed. Put the same three objects in the room,
        // as ordinary geometry, between two renders of them from two angles, and the whole claim is one
        // glance — <i>that</i> is the scene, and <i>these</i> are pictures of it.
        //
        // It has to be built a second time rather than shared: a node has one parent, and the copy in the
        // offline scene is inside a Scene the renderer owns.
        var live = Pieces();

        live.Name = "the scene itself";
        // Below the panels and well forward of them, so it is plainly a thing in the room rather than a
        // third picture — and low enough to leave the label under it readable.
        live.Scale = new Vector3(0.34f);
        live.Position = new Vector3(0f, -1.35f, 1.35f);

        wall.Children.Add(live);

        // Two cameras on one scene, so the pair reads as "this is a render" rather than "this is a
        // picture somebody supplied".
        (string Name, float Yaw, float Pitch, float X)[] shots =
        [
            ("front", 0.35f, 0.24f, -1.85f),
            ("side", 1.05f, 0.40f, 1.85f)
        ];

        foreach (var shot in shots)
        {
            var camera = new Camera();
            camera.Fit(inner.WorldBounds);
            camera.Yaw = shot.Yaw;
            camera.Pitch = shot.Pitch;
            // Fit leaves room for a scene to be orbited; a fixed shot does not need it, and the panel is
            // small enough that the margin is the difference between reading the scene and not.
            camera.Distance *= 0.72f;

            var screen = OfflineRenderer.Render(inner, camera, 512, 288);

            // Unlit, because the panel is showing an image rather than being a lit surface — the same
            // reasoning a monitor gets in any scene.
            wall.Children.Add(new MeshNode(Primitives.Plane(3.2f, 1.8f), new Material
            {
                Name = shot.Name,
                BaseColorTexture = screen,
                Unlit = true
            })
            {
                // Primitives.Plane lies in XZ — a floor. A screen is that stood up to face the camera.
                Position = new Vector3(shot.X, 0.15f, 0f),
                RotationDegrees = new Vector3(90f, 0f, 0f)
            });

            // A bezel, so the panel reads as an object rather than as a floating rectangle.
            wall.Children.Add(new MeshNode(Primitives.Box(3.5f, 2.1f, 0.12f), new Material
            {
                Name = "bezel",
                BaseColor = new Vector4(0.14f, 0.15f, 0.18f, 1f),
                Roughness = 0.55f
            })
            {
                Position = new Vector3(shot.X, 0.15f, -0.09f)
            });
        }

        return wall;
    }

    public override void Stage(Scene scene)
    {
        Staging.Neutral(scene, floor: false);
        scene.Background = Color.FromRgb(11, 12, 17);
    }

    /// <summary>
    /// The three objects the panels are pictures of.
    ///
    /// A method rather than a field, because it is built twice — once into the scene the offline renderer
    /// is handed, and once as ordinary geometry standing in the room between the two panels. A node has
    /// one parent, so "the same objects" here means the same arithmetic rather than the same instances.
    /// </summary>
    private static Node Pieces()
    {
        var pieces = new Node();

        pieces.Children.Add(new MeshNode(Primitives.Sphere(0.9f, 44, 30), new Material
        {
            BaseColor = new Vector4(0.90f, 0.48f, 0.22f, 1f),
            Roughness = 0.35f
        })
        {
            Position = new Vector3(-1.15f, 0f, 0f)
        });

        pieces.Children.Add(new MeshNode(Primitives.Box(1.3f, 1.3f, 1.3f), new Material
        {
            BaseColor = new Vector4(0.32f, 0.58f, 0.88f, 1f),
            Roughness = 0.5f
        })
        {
            Position = new Vector3(1.1f, 0f, 0.2f),
            RotationDegrees = new Vector3(0f, 24f, 0f)
        });

        return pieces;
    }

    /// <summary>The scene being rendered into the panels. Built here so nothing outside this file sees it.</summary>
    private static Scene InnerScene()
    {
        var scene = new Scene { Background = Color.FromRgb(16, 20, 30) };

        scene.Children.Add(Pieces());

        scene.Children.Add(new MeshNode(Primitives.Plane(7f, 7f), new Material
        {
            BaseColor = new Vector4(0.24f, 0.26f, 0.30f, 1f),
            Roughness = 0.9f
        })
        {
            Position = new Vector3(0f, -0.95f, 0f),
            RotationDegrees = new Vector3(-90f, 0f, 0f)
        });

        scene.Light.Direction = Vector3.Normalize(new Vector3(-0.5f, -0.75f, -0.4f));

        return scene;
    }
}
