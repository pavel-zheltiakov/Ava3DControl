using System.Numerics;

namespace Ava3D.Demo.Scenes;

/// <summary>The built-in meshes, on a ground plane, so their proportions can be compared.</summary>
public sealed class PrimitivesScene : DemoScene
{
    public override string Title => "Primitives";

    public override string Summary => "Sphere, box, cylinder, cone, torus and plane";

    public override string Notes =>
        """
        Primitives.Sphere, Box, Cylinder, Torus and Plane are the whole set. They exist so a scene can be
        built without an asset pipeline, and because a viewer needs a ground plane more often than it needs
        anything else.

        Cylinder takes two radii. Equal ones give a tube, a zero top gives a cone, and anything between
        gives the tapered section most hand-built spacecraft and architecture are made of — the side
        normals are tilted by the taper, so a cone shades as a cone rather than as a pointy cylinder. Its
        segment count is the difference between a hex pipe and a smooth one; six is a deliberate look, not
        a low-quality setting.

        Torus lies in the XZ plane, so its axis is Y. Three.js builds its torus in XY and everyone rotates
        it a quarter turn; flat is what a ring of windows around a hub or a docking collar actually wants.

        All of them carry normals and texture coordinates, so every material feature in the later scenes
        works on them, and all of them are wound counter-clockwise seen from outside — which only becomes
        visible the first time you cull one.
        """;

    public override bool Animates => true;

    public override bool FramesItself => true;

    public override void Frame(Camera camera)
    {
        camera.Target = new Vector3(0f, -0.15f, 0f);
        camera.Distance = 7.6f;
        camera.Yaw = 0.42f;
        camera.Pitch = 0.30f;
        camera.NearPlane = 0.4f;
        camera.FarPlane = 40f;
    }

    private Node _turntable = null!;

    public override Scene Build()
    {
        var scene = new Scene();

        var ground = new MeshNode(Primitives.Plane(9f, 6f), Material.FromColor(0.16f, 0.17f, 0.19f))
        {
            Position = new Vector3(0f, -0.85f, 0f),
            // A backdrop should not be in the way of clicks aimed at the subject.
            IsPickable = false
        };
        ground.Material.Roughness = 0.9f;
        scene.Children.Add(ground);

        _turntable = new Node();
        scene.Children.Add(_turntable);

        Add("Sphere", Primitives.Sphere(0.5f), new Vector4(0.85f, 0.35f, 0.30f, 1f), -2.9f, 0f);
        Add("Box", Primitives.Box(0.9f, 0.9f, 0.9f), new Vector4(0.35f, 0.62f, 0.85f, 1f), -1.7f, -0.4f);
        Add("Cylinder", Primitives.Cylinder(0.42f, 0.42f, 1.0f), new Vector4(0.55f, 0.80f, 0.55f, 1f), -0.5f, -0.35f);

        // Six segments rather than thirty-two: a hex tube is a look, and the faceting is the point.
        Add("Hex tube", Primitives.Cylinder(0.40f, 0.40f, 1.0f, segments: 6),
            new Vector4(0.80f, 0.72f, 0.40f, 1f), 0.7f, -0.35f);

        Add("Cone", Primitives.Cylinder(0f, 0.48f, 1.0f), new Vector4(0.85f, 0.55f, 0.35f, 1f), 1.9f, -0.35f);
        Add("Torus", Primitives.Torus(0.44f, 0.16f), new Vector4(0.72f, 0.50f, 0.85f, 1f), 3.1f, -0.2f);

        return scene;

        void Add(string name, Mesh mesh, Vector4 color, float x, float y)
        {
            _turntable.Children.Add(new MeshNode(mesh, new Material
            {
                BaseColor = color,
                Roughness = 0.4f,
                // Back faces culled, which is only safe because every one of these is a closed solid wound
                // the same way. It is also the check: a primitive wound backwards would vanish here.
                Cull = CullMode.Back,
                DoubleSided = false
            })
            {
                Name = name,
                Position = new Vector3(x, y, 0f)
            });
        }
    }

    public override void Update(Scene scene, double elapsed)
    {
        // Turned rather than static: a cone and a hex tube are shapes you have to see move to read.
        _turntable.Rotation = Quaternion.CreateFromYawPitchRoll(MathF.Sin((float)elapsed * 0.35f) * 0.55f, 0f, 0f);
        scene.Invalidate();
    }
}
