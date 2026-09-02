using System.Numerics;
using Avalonia.Media;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// Three solids on a floor with one lamp above them, and the thing every one of the other scenes is
/// missing: something standing between the light and the ground.
/// </summary>
public sealed class ShadowsScene : DemoScene
{
    private Node? _turntable;

    public override string Title => "Shadows";

    public override string Summary => "One casting light, one depth map, and geometry that sits on the floor";

    public override string Notes =>
        """
        Two lines of scene code: CastsShadows on the light, ShadowMapSize on the scene. Everything else
        here is ordinary geometry.

        What it buys is not darkness. It is the relationship between two objects — a figure with no shadow
        sits *in front of* a floor rather than *on* it, and no amount of work on the figure or the floor
        fixes that, because the fault is in the pair. Watch the ring as it turns: the shadow is what tells
        you which solid is resting and which is hovering.

        How it works is a second pass. Before the frame is drawn, the scene is rendered again from the
        light's own point of view, keeping only depth — no colours, no textures, no lighting. Shading then
        asks each pixel a single question: is anything nearer to the light than I am? That map is 2048
        pixels square here, which is four megabytes and about a fifth of a millisecond.

        Two numbers exist because that question is asked at one resolution and answered at another.
        ShadowBias pushes a surface a little towards the light before comparing, and the comparison also
        offsets along the surface normal by a texel's worth of world. Without them a lit floor fails its
        own test in bands — acne, which crawls as the camera moves. With too much of them a shadow slides
        away from the thing casting it, and a solid floats above its own shadow. The defaults here are
        tuned for a room-sized scene; a world measured in kilometres wants more.

        ShadowStrength is the one control that is not physical. A shadow at 1 loses all of the casting
        light and keeps everything else — the ambient floor, the environment, and any other lamp — which is
        why it comes out a dark grey rather than black. Below 1 it keeps some of the casting light too,
        which reads as a room with bounce in it and costs nothing.

        The scope is one light and one map. A second casting light would double the pass, the memory and
        the sampler budget; a point light shadows the cone between it and the middle of the scene rather
        than a full sphere, because a sphere needs six maps. The lamp here is directional, which has no
        such limit.

        The floor sets CastsShadow = false. It receives, but a large flat plane spread across the light's
        frustum would push everything else into a corner of the map and leave a ring's shadow four texels
        wide.

        The Shadows switch in the toolbar is Scene.ShadowsEnabled, and it is worth using here with the
        panel of numbers open. Off is not the shadow multiplied by zero — no map is allocated and the
        second pass over the casting geometry does not happen — so what moves is ms/render, by about a
        fifth of a millisecond on OpenGL at this map size and by less than this measurement can resolve on
        Metal. That is the honest answer to what the feature costs, and it is a smaller number than most
        people expect.
        """;

    public override bool Animates => true;

    public override bool FramesItself => true;

    public override void Frame(Camera camera)
    {
        camera.Target = new Vector3(0f, 0.7f, 0f);
        camera.Distance = 9.5f;
        camera.Yaw = 0.75f;
        camera.Pitch = 0.42f;
        camera.NearPlane = 0.5f;
        camera.FarPlane = 60f;
    }

    /// <summary>
    /// One lamp, steep enough that the shadows land beside their casters rather than under them, and a
    /// dim environment so the shadowed floor still reads as floor rather than as a hole.
    /// </summary>
    public override void Stage(Scene scene)
    {
        scene.Background = Color.FromRgb(14, 16, 22);

        scene.Light.Direction = Vector3.Normalize(new Vector3(-0.42f, -0.86f, -0.30f));
        scene.Light.Color = new Vector3(1f, 0.97f, 0.90f);
        scene.Light.Intensity = 2.2f;
        scene.Light.Ambient = 0.05f;

        // The two lines this whole scene is about.
        scene.Light.CastsShadows = true;
        scene.ShadowMapSize = 2048;

        scene.Environment = EnvironmentLight.Studio(0.30f);
    }

    public override Node BuildSubject()
    {
        var root = new Node { Name = "shadows" };

        var floorMaterial = new Material
        {
            BaseColor = new Vector4(0.42f, 0.41f, 0.38f, 1f),
            Roughness = 0.92f
        };

        // Forty-eight segments each way rather than the one a flat floor needs to be flat.
        //
        // The software renderer shades per vertex, so a shadow it computes lands at vertex resolution — and
        // at one segment this floor has four vertices, sixteen units apart, which interpolates every shadow
        // in the scene away to nothing. That is exactly what it did: on Skia this scene drew three solids
        // sitting on a clean floor and demonstrated the opposite of what it is here to demonstrate.
        //
        // The GPU paths shade per pixel and do not care either way, so this is a cost paid entirely for the
        // renderer that needs it, and it is the scene's job to be legible on all four.
        root.Children.Add(new MeshNode(Primitives.Plane(16f, 16f, 48, 48), floorMaterial)
        {
            Name = "floor",
            Position = new Vector3(0f, -0.9f, 0f),
            // Receives, does not cast. A plane this large would spread the light's frustum across the
            // whole floor and leave the ring's shadow a few texels wide.
            CastsShadow = false
        });

        _turntable = new Node { Name = "solids" };

        // Resting on the floor: the shadow meets the object, which is what "on" looks like.
        _turntable.Children.Add(new MeshNode(
            Primitives.Box(1.5f, 1.5f, 1.5f),
            new Material { BaseColor = new Vector4(0.78f, 0.34f, 0.28f, 1f), Roughness = 0.55f })
        {
            Name = "resting",
            Position = new Vector3(-2.2f, -0.15f, 0f)
        });

        // Hovering: the shadow detaches, and that is the whole signal that it is off the ground.
        _turntable.Children.Add(new MeshNode(
            Primitives.Sphere(0.85f, 40, 28),
            new Material { BaseColor = new Vector4(0.36f, 0.58f, 0.80f, 1f), Roughness = 0.35f })
        {
            Name = "hovering",
            Position = new Vector3(0.4f, 1.35f, 0.3f)
        });

        // A shape whose shadow is not its silhouette, so the map is visibly doing more than drawing a
        // blob under things.
        _turntable.Children.Add(new MeshNode(
            Primitives.Torus(0.85f, 0.26f, 40, 20),
            new Material { BaseColor = new Vector4(0.86f, 0.74f, 0.32f, 1f), Metallic = 0.9f, Roughness = 0.28f })
        {
            Name = "ring",
            Position = new Vector3(2.4f, 0.15f, -0.4f),
            RotationDegrees = new Vector3(72f, 0f, 18f)
        });

        root.Children.Add(_turntable);

        return root;
    }

    public override void Update(Scene scene, double elapsed)
    {
        if (_turntable is null)
            return;

        _turntable.RotationDegrees = new Vector3(0f, (float)(elapsed * 22.0), 0f);
    }
}
