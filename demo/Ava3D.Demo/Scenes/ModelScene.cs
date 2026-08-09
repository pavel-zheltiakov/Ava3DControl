using System.Numerics;
using Avalonia.Media;
using Avalonia.Platform;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// A model somebody else made: a CC0 camera from Poly Haven, packed to <c>.glb</c> and loaded through
/// <see cref="GltfLoader"/> with nothing else done to it.
///
/// The neighbouring glTF scene authors its asset in memory, which proves the loader against something this
/// demo controls completely. This one proves the harder half — an asset modelled by an artist in Blender,
/// unwrapped by hand, and baked to maps that were never made with this renderer in mind.
/// </summary>
public sealed class ModelScene : DemoScene
{
    private string _detail = "";

    public override string Title => "A real model";

    public override string Summary => "A CC0 asset from Poly Haven, loaded as shipped";

    public override string Notes =>
        $"""
         Camera 01 by Rajil Jose Macatangay, from Poly Haven, CC0. Nothing about it was made for this
         renderer: it was modelled in Blender, unwrapped by hand, and baked to 1k maps years before this
         control existed.

         {_detail}

         This is the case the generated glTF scene cannot cover. An asset you author yourself agrees with
         your own conventions by construction. A real one arrives with whatever the artist's pipeline
         produced — packed metallic-roughness in one image with ambient occlusion in the red channel,
         OpenGL-convention normal maps, a node hierarchy that exists for rigging reasons, and separate
         materials for body, lens and strap. All of that is glTF's job to describe and the loader's job to
         map onto Material, and this scene is the evidence that it does.

         It shipped as a .gltf with a .bin and nine JPEGs beside it, and was repacked into one .glb — a
         container change and nothing else, since every buffer and image just moves into the BIN chunk. The
         loader reads binary glTF only, because resolving a dozen satellite URIs is awkward in a browser
         and pointless everywhere else.

         2.4 MB of the demo's download is this file, and it is the only asset here that came from outside.
         The board in the next scene is another 2.7 MB, but it is a build output rather than a download —
         tools/models/build-pcb.py produces it. Everything else in the demo is generated in code at
         startup, which is why the remaining scenes cost nothing to ship.
         """;

    public override SceneLook Look => SceneLook.Studio;

    public override bool Animates => true;

    public override bool DrivesCamera => true;

    public override void Frame(Camera camera)
    {
        // Angle only. AutoFit does the rest, which is the right division of labour for a model whose
        // extent nobody here chose — Poly Haven publishes in metres and the strap hangs well below the
        // body, so a hand-picked distance would be a guess about somebody else's pivot.
        camera.Yaw = 0.85f;
        camera.Pitch = 0.24f;
        camera.FieldOfView = 38f;
    }

    /// <summary>
    /// A three-point studio: key from the front left, fill from the right, and a rim from behind to pull
    /// the silhouette off the background.
    ///
    /// Three lights for one object is more than most of this folder spends, and it is spent here because
    /// this is the one asset nobody in this repository authored. The maps carry all the detail there is;
    /// the lights only have to be fair to them, and a single key would leave half of somebody else's work
    /// in the dark.
    /// </summary>
    public override void Stage(Scene scene)
    {
        scene.Background = Color.FromRgb(16, 17, 22);

        scene.Lights.Clear();
        scene.Lights.Add(new DirectionalLight
        {
            Direction = Vector3.Normalize(new Vector3(0.5f, -0.62f, -0.6f)),
            Color = new Vector3(1.00f, 0.97f, 0.92f),
            Intensity = 2.6f,
            Ambient = 0.06f,
            AmbientColor = new Vector3(0.55f, 0.60f, 0.72f)
        });

        scene.Lights.Add(new DirectionalLight
        {
            Direction = Vector3.Normalize(new Vector3(-0.75f, -0.25f, -0.35f)),
            Color = new Vector3(0.62f, 0.72f, 0.95f),
            Intensity = 0.9f
        });

        scene.Lights.Add(new DirectionalLight
        {
            Direction = Vector3.Normalize(new Vector3(0.1f, -0.15f, 0.95f)),
            Color = new Vector3(1.00f, 0.86f, 0.70f),
            Intensity = 1.1f
        });

        scene.Environment = new EnvironmentLight
        {
            SkyColor = new Vector3(0.30f, 0.34f, 0.44f),
            GroundColor = new Vector3(0.11f, 0.10f, 0.09f),
            Intensity = 1.1f
        };
    }

    public override Node BuildSubject()
    {
        var model = new Node { Name = "camera" };

        try
        {
            using var stream = AssetLoader.Open(new Uri("avares://Ava3D.Demo/Assets/camera.glb"));
            using var memory = new MemoryStream();
            stream.CopyTo(memory);

            var bytes = memory.ToArray();
            var loaded = GltfLoader.Load(bytes, "camera.glb");

            // The loaded hierarchy goes under one holder and is otherwise untouched — no re-pivot, no
            // re-scale, nothing moved. The holder is identity, so it changes neither the picture nor the
            // extent AutoFit measures; it exists because a subject has to be a single node, and it is the
            // only thing this scene does to somebody else's file.
            foreach (var root in loaded.Children.ToList())
                model.Children.Add(root);

            var meshes = Count(model);
            _detail =
                $"{bytes.Length / 1024:N0} KB of .glb, {meshes.Nodes} nodes, {meshes.Draws} primitives, " +
                $"{meshes.Triangles:N0} triangles, {meshes.Materials} materials.";
        }
        catch (Exception e)
        {
            _detail = $"The model failed to load: {e.GetType().Name}: {e.Message}";
        }

        return model;
    }

    public override void Update(Scene scene, Camera camera, double elapsed)
    {
        // The camera goes round the model rather than the model going round on a turntable. AutoFit chose
        // the target and the distance from the model's own extent on the first frame; turning the object
        // instead would swing it out of that framing, because its pivot is not its centre and nothing here
        // has any business assuming it would be.
        camera.Yaw = (float)elapsed * 0.32f;
        scene.Invalidate();
    }

    /// <summary>Walks what the loader produced, so the notes can quote it rather than claim it.</summary>
    private static (int Nodes, int Draws, int Triangles, int Materials) Count(Node root)
    {
        var nodes = 0;
        var draws = 0;
        var triangles = 0;
        var materials = new HashSet<Material>();

        Walk(root.Children);
        return (nodes, draws, triangles, materials.Count);

        void Walk(IEnumerable<Node> children)
        {
            foreach (var node in children)
            {
                nodes++;

                if (node is MeshNode { Mesh: { } geometry } mesh)
                {
                    draws++;
                    materials.Add(mesh.Material);
                    triangles += (geometry.Indices is { Length: > 0 } indices
                        ? indices.Length
                        : geometry.Positions.Length) / 3;
                }

                Walk(node.Children);
            }
        }
    }
}
