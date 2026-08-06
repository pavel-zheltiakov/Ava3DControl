using System.Numerics;
using Avalonia.Media;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// Two identical panels with their own wireframe drawn on top of them, one biased and one not. The scene
/// that only makes sense in motion.
/// </summary>
public sealed class DepthBiasScene : DemoScene
{
    private Node? _pair;

    public override string Title => "Depth bias";

    public override string Summary => "Coplanar lines that hold, against coplanar lines that crawl";

    public override string Notes =>
        """
        Both panels are the same mesh with the same grid of lines over them, and the lines are made from
        the panel's own vertices — so every line lies exactly in the surface it is drawn on.

        Exactly is the problem. The depth test is being asked which of two surfaces at the same depth is in
        front, and it has no answer: the result comes down to how each pixel's depth was rounded, so it
        changes with the angle, with the distance, and from frame to frame. On the left that is what you
        see — the grid breaks into dashes and the dashes crawl as the panels turn.

        The right-hand panel sets DepthBias = 1 and DepthBiasSlope = 1 on its material, which pushes its
        triangles one step further from the camera. The lines win everywhere, at every angle, near end and
        far end alike.

        Both numbers earn their place. The panels lean away from you, so the far end of one covers many
        more depth values per pixel than the near end does, and a constant offset large enough for the far
        end would visibly lift the lines off the near end. The slope-scaled term measures the lean; the
        constant term covers the rest. It is the same pair of numbers three.js spells polygonOffsetFactor
        and polygonOffsetUnits, and the same pair every GPU implements.

        The obvious alternatives are worse. Turning the lines' DepthTest off draws them through the back of
        whatever they are on. Nudging the line vertices along the normal works at one distance and comes
        adrift at another.

        Two renderers cannot do this, for opposite reasons. The CPU fallback paints back to front with no
        depth buffer, so it has nothing to fight over and both panels look like the right-hand one. The
        browser has a depth buffer and no way to bias it — glPolygonOffset terminates the WebAssembly
        runtime, so it is never called — and both panels look like the left-hand one. The features list in
        the statistics panel says which you are looking at.
        """;

    public override SceneLook Look => SceneLook.Blueprint;

    public override bool Animates => true;

    public override bool FramesItself => true;

    public override void Frame(Camera camera)
    {
        camera.Target = new Vector3(0f, 0f, 0.4f);
        camera.Distance = 11.6f;
        camera.Yaw = 0f;
        camera.Pitch = 0.70f;
        camera.NearPlane = 0.5f;
        camera.FarPlane = 40f;
    }

    public override Scene Build()
    {
        var scene = new Scene { Background = Color.FromRgb(16, 20, 28) };

        scene.Light.Direction = Vector3.Normalize(new Vector3(-0.35f, -0.85f, -0.4f));
        scene.Environment = EnvironmentLight.Studio(0.22f);

        // Long in Z so the panel recedes steeply: the near end and the far end of one surface are at very
        // different depths, which is the case a constant offset alone cannot cover.
        var panel = Primitives.Plane(2.6f, 7f, 6, 16);
        var edges = panel.GetEdges(0f);

        _pair = new Node();
        scene.Children.Add(_pair);

        _pair.Children.Add(Panel(-1.65f, bias: 0f, "no bias"));
        _pair.Children.Add(Panel(1.65f, bias: 1f, "DepthBias 1, slope 1"));

        return scene;

        Node Panel(float x, float bias, string name)
        {
            var node = new MeshNode(panel, new Material
            {
                Name = name,
                BaseColor = new Vector4(0.15f, 0.19f, 0.26f, 1f),
                Roughness = 0.85f,
                Cull = CullMode.Back,
                DepthBias = bias,
                DepthBiasSlope = bias
            })
            {
                Name = name,
                Position = new Vector3(x, 0f, 0f)
            };

            // A child, so it shares the panel's transform exactly. Anything else — a copy of the position,
            // a sibling kept in step by hand — is a second chance to be a fraction of a unit out, and a
            // fraction of a unit is the whole subject of this scene.
            node.Children.Add(new LineNode
            {
                Name = $"{name} grid",
                Positions = edges,
                Color = new Vector3(0.95f, 0.97f, 1f),
                Width = 1.6f,
                // Ordinary depth testing, because that is the thing being demonstrated. What is turned off
                // is the write: an overlay that writes depth occludes the next line that crosses it.
                DepthWrite = false,
                RenderOrder = 1
            });

            return node;
        }
    }

    public override void Update(Scene scene, double elapsed)
    {
        if (_pair is not { } pair)
            return;

        // Turning is not decoration. A still frame of the left-hand panel is a plausible dashed grid;
        // it is the crawling as the angle changes that says the depth test is guessing.
        pair.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.Sin((float)elapsed * 0.35f) * 0.55f);
        scene.Invalidate();
    }
}
