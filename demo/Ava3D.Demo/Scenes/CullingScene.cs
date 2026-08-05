using System.Numerics;
using Avalonia.Media;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// Three identical rooms differing only in <see cref="Material.Cull"/>, with the camera flying in through
/// the middle one and back out — because which faces are drawn only matters once you are on the other
/// side of them.
/// </summary>
public sealed class CullingScene : DemoScene
{
    private const float Cycle = 16f;

    private string? _caption;

    public override string Title => "Culling";

    public override string Summary => "Which side of a face is drawn, from outside and from inside";

    public override string Notes =>
        """
        Three identical rooms. Same geometry, same material, same light. One property differs.

        Watch from outside first. None and Back look the same — a closed box hides its own inside either
        way, and Back is simply cheaper because half the fragments never happen. Front looks broken: its
        outward faces are gone and you are seeing the inside of its far walls. That is not a bug, it is a
        room you can look into.

        Then the camera flies into the middle one, and the comparison inverts. Front and None are solid
        around you. Back has vanished — from inside, every wall you would see is a back face, and back
        faces are what it culls. The crate and the lamp stay, because they are separate objects with their
        own material.

        That is the whole rule. A face is front-facing when its winding comes out counter-clockwise on
        screen, which flips the moment you cross the surface. Cull.Front is what a sky sphere seen from
        inside needs, and Cull.Back is what a closed hull wants for the half of the fragments it saves.

        Cull and DoubleSided are independent and both are needed. Cull decides which faces are drawn;
        DoubleSided decides how the ones that are drawn are lit — with it off, a face seen from behind is
        lit as though you were seeing its back, which is what the far walls here rely on.
        """;

    public override SceneLook Look => SceneLook.Blueprint;

    public override bool Animates => true;

    public override bool FramesItself => true;

    public override bool DrivesCamera => true;

    public override string? Caption => _caption;

    public override void Frame(Camera camera)
    {
        camera.FieldOfView = 60f;
        camera.NearPlane = 0.15f;
        camera.FarPlane = 60f;
    }

    public override Scene Build()
    {
        var scene = new Scene { Background = Color.FromRgb(8, 9, 14) };

        scene.Lights.Clear();
        scene.Lights.Add(new DirectionalLight
        {
            Direction = Vector3.Normalize(new Vector3(-0.4f, -0.7f, -0.55f)),
            Color = new Vector3(1.00f, 0.96f, 0.90f),
            Intensity = 1.9f,
            Ambient = 0.10f,
            AmbientColor = new Vector3(0.60f, 0.66f, 0.80f)
        });

        // A lamp in the middle room, so the interior the camera flies into has a source of its own. There
        // are no shadows, so the key light reaches inside every room regardless — this is warmth, not
        // visibility.
        scene.Lights.Add(new PointLight
        {
            Position = new Vector3(0f, 1.6f, 0f),
            Color = new Vector3(1.00f, 0.82f, 0.55f),
            Intensity = 22f,
            Range = 11f,
            Decay = 2f
        });

        scene.Environment = new EnvironmentLight
        {
            SkyColor = new Vector3(0.10f, 0.12f, 0.17f),
            GroundColor = new Vector3(0.05f, 0.05f, 0.06f)
        };

        Room(-9f, CullMode.None, new Vector3(0.62f, 0.64f, 0.70f));
        Room(0f, CullMode.Back, new Vector3(0.40f, 0.68f, 0.95f));
        Room(9f, CullMode.Front, new Vector3(0.95f, 0.62f, 0.38f));

        return scene;

        void Room(float x, CullMode cull, Vector3 tint)
        {
            var group = new Node { Position = new Vector3(x, 0f, 0f) };
            scene.Children.Add(group);

            // The shell. A closed box with no door: the only way in is through a wall, which is exactly
            // the question this scene is about.
            group.Children.Add(new MeshNode(Primitives.Box(6.6f, 4.6f, 6.6f), new Material
            {
                Name = $"room {cull}",
                BaseColor = new Vector4(tint.X, tint.Y, tint.Z, 1f),
                Roughness = 0.75f,
                Cull = cull,
                // Off, so a wall seen from behind is lit as its own back rather than as though it faced
                // the light. Without this the interiors would look flat and identical.
                DoubleSided = false
            }));

            // A floor plate just inside the shell, on the default CullMode.None. It is the reference: when
            // the walls go, the floor stays, so "this room lost its walls" reads instead of "the picture
            // went black".
            group.Children.Add(new MeshNode(Primitives.Plane(6.4f, 6.4f), new Material
            {
                BaseColor = new Vector4(tint.X * 0.5f, tint.Y * 0.5f, tint.Z * 0.5f, 1f),
                Roughness = 0.9f
            })
            {
                Position = new Vector3(0f, -2.24f, 0f)
            });

            // Furniture, also on CullMode.None, so it is unaffected and stays as a reference.
            group.Children.Add(new MeshNode(Primitives.Box(1.5f, 1.5f, 1.5f), new Material
            {
                BaseColor = new Vector4(0.55f, 0.45f, 0.32f, 1f),
                Roughness = 0.85f
            })
            {
                Position = new Vector3(-1.6f, -1.45f, -1.4f),
                RotationDegrees = new Vector3(0f, 22f, 0f)
            });

            group.Children.Add(new MeshNode(Primitives.Cylinder(0.36f, 0.5f, 2.0f, 12), new Material
            {
                BaseColor = new Vector4(0.30f, 0.32f, 0.36f, 1f),
                Roughness = 0.5f,
                Metallic = 0.6f
            })
            {
                Position = new Vector3(1.7f, -1.2f, -1.1f)
            });

            // The lamp itself: unlit geometry on a short stem, so it reads as the source rather than as a
            // lit ball, and so there is something at eye height to tell you which room you are in.
            group.Children.Add(new MeshNode(Primitives.Cylinder(0.09f, 0.09f, 0.7f, 8), new Material
            {
                BaseColor = new Vector4(0.22f, 0.23f, 0.26f, 1f),
                Roughness = 0.6f
            })
            {
                Position = new Vector3(0f, 1.95f, 0f)
            });

            group.Children.Add(new MeshNode(Primitives.Sphere(0.3f, 20, 14), new Material
            {
                BaseColor = new Vector4(1.00f, 0.88f, 0.62f, 1f),
                Unlit = true
            })
            {
                Position = new Vector3(0f, 1.6f, 0f)
            });
        }
    }

    public override void Update(Scene scene, Camera camera, double elapsed)
    {
        var cycle = (float)(elapsed % Cycle);

        // Out, in, look sideways, back out. The sideways look is the whole point of going in: from inside
        // a back-culled room there is nothing to see except what is beyond its missing walls, and what is
        // beyond them is the room next door, still solid.
        var push = cycle switch
        {
            < 4.5f => Ease.InOut(Ease.Ramp(0f, 4.5f, cycle)),        // approach and enter
            < 12f => 1f,                                             // inside
            < 15f => 1f - Ease.InOut(Ease.Ramp(12f, 15f, cycle)),    // back out
            _ => 0f
        };

        // Inside, the focus sweeps the whole row: past the left room, out through this room's missing
        // wall, and on to the right one. One continuous shot with all three states in it.
        var sweep = cycle switch
        {
            < 5.5f => 0.5f,
            < 8f => 0.5f - Ease.InOut(Ease.Ramp(5.5f, 8f, cycle)) * 0.5f,    // left
            < 11.5f => Ease.InOut(Ease.Ramp(8f, 11.5f, cycle)),              // across to the right
            _ => 1f
        };

        _caption = cycle switch
        {
            < 4f => "Outside — None and Back are solid, Front is open",
            < 6f => "Entering the middle room",
            < 12f => "Inside Back: its own walls are gone, and the two beside it still have theirs",
            < 15f => "Backing out",
            _ => null
        };

        // Straight in along Z into the middle room, then the focus pans across the row.
        var eye = new Vector3(0f, 0.4f, 22f - push * 20f);
        var focus = Vector3.Lerp(
            new Vector3(-9f, -0.9f, -2f),
            new Vector3(9f, -0.9f, -2f),
            sweep);

        camera.LookFrom(eye, focus);

        scene.Invalidate();
    }
}
