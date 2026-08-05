using Avalonia.Media;

namespace Ava3D.Demo.Scenes;

/// <summary>The smallest thing that is still a 3D view: one cube, one light, no configuration.</summary>
public sealed class HelloCubeScene : DemoScene
{
    public override string Title => "Hello cube";

    public override string Summary => "One box, default everything";

    public override string Notes =>
        """
        Four lines of scene-building. A Mesh from Primitives, a Material, a MeshNode, done — the control
        frames it, lights it and would hand you orbit, pan and zoom without being asked.

        There is no camera setup here because AutoFit is on by default: the view measures whatever it is
        given and positions itself. The input is on by default too, and this demo is the odd case that
        turns it off — every scene in the list is either framed for one angle or flown by a script, so
        the shell sets IsOrbitEnabled, IsPanEnabled and IsZoomEnabled to false once and leaves them there.
        In an application, delete those three lines.
        """;

    public override Scene Build()
    {
        var scene = new Scene();

        scene.Children.Add(new MeshNode(
            Primitives.Box(1f, 1f, 1f),
            Material.FromColor(0.55f, 0.62f, 0.78f)));

        return scene;
    }
}
