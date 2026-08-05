using System.Numerics;
using Avalonia.Media;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// <see cref="LineNode"/> on its own: what a line can vary. Two charts — one holding colour still and
/// changing width, one holding width still and changing colour — under two boxes that differ only in
/// whether their edges are depth tested.
/// </summary>
public sealed class LinesScene : DemoScene
{
    private static readonly float[] Widths = [1f, 2f, 4f, 7f, 11f, 16f];

    private static readonly Vector3[] Hues =
    [
        new(1.00f, 0.30f, 0.22f),
        new(1.00f, 0.66f, 0.16f),
        new(0.95f, 0.94f, 0.30f),
        new(0.28f, 0.92f, 0.42f),
        new(0.30f, 0.78f, 1.00f),
        new(0.72f, 0.42f, 1.00f)
    ];

    private const float ChartTop = -0.30f, ChartStep = 0.32f;

    private Node _left = null!, _right = null!;

    public override string Title => "Lines";

    public override string Summary => "Width, colour, and edges over a hull";

    public override string Notes =>
        """
        Six lines down the left at widths 1, 2, 4, 7, 11 and 16, all the same colour. Six down the right
        all at width 7, each a different one. Width is one number per node and so is Color, which is why
        there are twelve nodes here rather than two: a line that changes weight halfway along is two
        lines.

        Width is in pixels on the screen, not units in the world, so a line does not get thinner as it
        goes away. Watch the boxes turn: their edges stay exactly as heavy on the far side as the near.

        Getting that on the GPU takes doing. Every mainstream GL driver clamps glLineWidth to 1 and Metal
        has no line-width control at all, so anything wider than one device pixel is drawn as two triangles
        per segment, expanded to width after the perspective divide and faded over the last pixel at each
        edge. A line that already lands on one device pixel skips all of that and stays a plain GL or Metal
        line, because six vertices per segment is a real cost and a hairline does not need it. Skia strokes
        the line itself and always did — and on this display all three agree to within a pixel at every one
        of the six widths.

        The boxes above show the other thing worth knowing. On the left the edges are depth tested, so
        only the ones you could actually see are drawn — panel seams on a solid hull. On the right
        DepthTest is off and all twelve show through, which is the blueprint look. The recipe for lines
        over a surface they belong to is DepthWrite = false and RenderOrder = 1: the first stops them
        fighting the surface underneath in the depth buffer, the second puts them after the hull
        regardless of how the two origins compare.

        Positions is an endpoint list — [0]–[1] is one segment, [2]–[3] the next. The renderers cache a GPU
        buffer against the array's identity, so a line that never changes uploads once. One that does moves
        its node when the whole thing moves together, and writes into the array and calls
        InvalidateGeometry() when the endpoints go their own ways — a wake, a waveform, a rope.

        Ends are square-cut on every backend, so two segments meeting at a sharp angle leave a notch at
        the joint. Overlap them rather than butting them together.
        """;

    public override SceneLook Look => SceneLook.Blueprint;

    public override bool Animates => true;

    public override bool FramesItself => true;

    public override void Frame(Camera camera)
    {
        camera.Target = new Vector3(0f, -0.25f, 0f);
        camera.Distance = 8.2f;
        camera.Yaw = 0.30f;
        camera.Pitch = 0.20f;
        camera.NearPlane = 0.5f;
        camera.FarPlane = 30f;
    }

    public override Scene Build()
    {
        var scene = new Scene { Background = Color.FromRgb(8, 9, 14) };

        _left = Cube(-1.5f, depthTest: true);
        _right = Cube(1.5f, depthTest: false);

        scene.Children.Add(_left);
        scene.Children.Add(_right);

        // Left: one colour, six widths. Right: one width, six colours. The same six rows on both sides,
        // so the eye compares across rather than hunting for what changed.
        for (var i = 0; i < Widths.Length; i++)
        {
            var y = ChartTop - i * ChartStep;
            scene.Children.Add(Rule(-2.90f, -0.65f, y, Widths[i], new Vector3(0.55f, 0.86f, 1.00f)));
            scene.Children.Add(Rule(0.65f, 2.90f, y, 7f, Hues[i]));
        }

        return scene;

        static LineNode Rule(float x0, float x1, float y, float width, Vector3 color) => new()
        {
            Positions = [new Vector3(x0, y, 0f), new Vector3(x1, y, 0f)],
            Color = color,
            Width = width,
            DepthWrite = false
        };

        static Node Cube(float x, bool depthTest)
        {
            var group = new Node { Position = new Vector3(x, 1.15f, 0f) };
            var box = Primitives.Box(1.4f, 1.4f, 1.4f);

            group.Children.Add(new MeshNode(box, new Material
            {
                // Dark, so a line at a fifth opacity has something to be seen against. Panel lines over a
                // pale hull need a dark line rather than a light one; the principle is the same either way.
                BaseColor = new Vector4(0.10f, 0.12f, 0.16f, 1f),
                Roughness = 0.6f,
                Metallic = 0.2f
            }));

            group.Children.Add(new LineNode
            {
                // The edges of the very box above, rather than a second list that has to be kept in step
                // with it. A cube's faces fold by 90°, so the default finds all twelve and nothing else.
                Positions = box.GetEdges(),
                Color = new Vector3(0.55f, 0.86f, 1.00f),
                // Wide enough that which edges are missing on the left is obvious rather than a squint,
                // which a hairline at this distance would not be.
                Width = 4f,
                Opacity = depthTest ? 0.85f : 0.55f,
                Blend = BlendMode.Alpha,
                DepthTest = depthTest,
                DepthWrite = false,
                RenderOrder = 1
            });

            return group;
        }
    }

    public override void Update(Scene scene, double elapsed)
    {
        var rotation = Quaternion.CreateFromYawPitchRoll(
            (float)elapsed * 0.5f, MathF.Sin((float)elapsed * 0.35f) * 0.4f, 0f);

        // The same rotation on both, so the only difference on screen is the one being demonstrated.
        _left.Rotation = rotation;
        _right.Rotation = rotation;

        scene.Invalidate();
    }

}
