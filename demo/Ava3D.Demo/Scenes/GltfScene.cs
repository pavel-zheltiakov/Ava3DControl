using System.Numerics;
using Ava3D.Demo.Textures;
using Avalonia.Media;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Memory;
using SharpGLTF.Scenes;

namespace Ava3D.Demo.Scenes;

/// <summary>
/// The glTF loader, end to end: a binary <c>.glb</c> is authored in memory, then read back through
/// <see cref="GltfLoader"/> exactly as a file from disk would be.
///
/// Authored rather than shipped for a licensing reason. The well-known glTF sample models are variously
/// CC BY-NC or attribution-carrying, and this demo's source is licensed for reuse without attribution inside
/// a product licensed for commercial use — so the two do not mix. Generating the asset sidesteps that
/// entirely, keeps the download small, and exercises more of the loader than a fixed file would: the model
/// below deliberately carries a full metallic-roughness material with four texture channels, a node
/// hierarchy, and no TANGENT attribute, so the loader has to generate tangents before the normal map can be
/// applied.
/// </summary>
public sealed class GltfScene : DemoScene
{
    private string _detail = "";

    public override string Title => "glTF loading";

    public override string Summary => "A .glb built in memory, then loaded back through GltfLoader";

    public override string Notes =>
        $"""
         GltfLoader.Load(bytes) takes binary glTF and returns a Scene: node hierarchy preserved, one draw call
         per primitive, materials mapped onto Ava3D's own, and textures left encoded until a renderer needs
         them. SharpGLTF does the parsing — fully managed, which is the only reason a browser build is
         possible at all, since Assimp cannot load under WebAssembly.

         The model here is generated at startup rather than shipped as a file, so the demo carries no
         third-party assets. It has a base colour, metallic-roughness and normal map, an emissive factor, and
         no tangents — which the loader notices and generates, because exporters omit the TANGENT attribute
         unless asked and most are never asked.

         {_detail}

         Only .glb is read. A multi-file .gltf needs every satellite fetched through a URI resolver, which the
         browser makes awkward; pack it first.
         """;

    public override SceneLook Look => SceneLook.Studio;

    public override Scene Build()
    {
        try
        {
            var bytes = Author();
            var scene = GltfLoader.Load(bytes, "generated.glb");
            scene.Background = Color.FromRgb(18, 20, 26);
            scene.Environment = new EnvironmentLight
            {
                SkyColor = new Vector3(0.40f, 0.45f, 0.56f),
                GroundColor = new Vector3(0.16f, 0.14f, 0.12f)
            };

            var meshes = scene.EnumerateMeshes().ToList();
            var triangles = meshes.Sum(m => m.Node.Mesh!.TriangleCount);
            var tangents = meshes.Count(m => m.Node.Mesh!.Tangents is { Length: > 0 });

            _detail =
                $"Loaded {bytes.Length / 1024.0:F0} KB of glb: {meshes.Count} primitives, " +
                $"{triangles:N0} triangles, tangents generated for {tangents} of them.";

            return scene;
        }
        catch (Exception e)
        {
            _detail = $"The round trip failed: {e.GetType().Name}: {e.Message}";

            var fallback = new Scene();
            fallback.Children.Add(new MeshNode(Primitives.Box(1f, 1f, 1f),
                Material.FromColor(0.8f, 0.3f, 0.3f)));
            return fallback;
        }
    }

    /// <summary>
    /// Writes a small textured PBR model as binary glTF.
    ///
    /// Everything here is SharpGLTF's authoring API rather than Ava3D's — that is the point, since the
    /// purpose is to produce bytes a third party would recognise as a valid glTF 2.0 file.
    /// </summary>
    private static byte[] Author()
    {
        var maps = Procedural.Panels(size: 256, cells: 2);

        var material = new MaterialBuilder("panels")
            .WithMetallicRoughnessShader()
            .WithBaseColor(ImageOf(maps.BaseColor), new Vector4(1f, 1f, 1f, 1f))
            .WithMetallicRoughness(ImageOf(maps.MetallicRoughness), 1f, 1f)
            .WithNormal(ImageOf(maps.Normal), 1f)
            // The map, not a flat factor. An emissive *factor* of 0.9 with no texture would add 0.9 to every
            // fragment on the model and wash the whole thing out — which is exactly what it did the first
            // time, and a good demonstration of why emissive is the one channel worth checking twice.
            .WithEmissive(ImageOf(maps.Emissive), new Vector3(1f, 1f, 1f))
            .WithDoubleSide(true);

        // Position + normal + texture coordinate, and deliberately no tangent.
        var mesh = new MeshBuilder<VertexPositionNormal, VertexTexture1>("panelled");
        var primitive = mesh.UsePrimitive(material);

        AddBox(primitive, new Vector3(1.4f, 1.4f, 1.4f));

        var builder = new SceneBuilder("generated");
        builder.AddRigidMesh(mesh, Matrix4x4.CreateTranslation(0f, 0.75f, 0f));
        builder.AddRigidMesh(mesh, Matrix4x4.CreateScale(0.55f) * Matrix4x4.CreateTranslation(-1.5f, -0.5f, 0f));
        builder.AddRigidMesh(mesh, Matrix4x4.CreateScale(0.55f) * Matrix4x4.CreateTranslation(1.5f, -0.5f, 0f));

        using var stream = new MemoryStream();
        builder.ToGltf2().WriteGLB(stream);
        return stream.ToArray();
    }

    private static MemoryImage ImageOf(Texture texture) => new(texture.Encoded);

    /// <summary>
    /// A box with per-face vertices, so each face gets flat normals and its own copy of the 0..1 UV square.
    /// </summary>
    private static void AddBox(
        PrimitiveBuilder<MaterialBuilder, VertexPositionNormal, VertexTexture1, VertexEmpty> primitive,
        Vector3 size)
    {
        var h = size * 0.5f;

        (Vector3 Normal, Vector3 A, Vector3 B, Vector3 C, Vector3 D)[] faces =
        [
            (new(0, 0, 1), new(-h.X, -h.Y, h.Z), new(h.X, -h.Y, h.Z), new(h.X, h.Y, h.Z), new(-h.X, h.Y, h.Z)),
            (new(0, 0, -1), new(h.X, -h.Y, -h.Z), new(-h.X, -h.Y, -h.Z), new(-h.X, h.Y, -h.Z), new(h.X, h.Y, -h.Z)),
            (new(1, 0, 0), new(h.X, -h.Y, h.Z), new(h.X, -h.Y, -h.Z), new(h.X, h.Y, -h.Z), new(h.X, h.Y, h.Z)),
            (new(-1, 0, 0), new(-h.X, -h.Y, -h.Z), new(-h.X, -h.Y, h.Z), new(-h.X, h.Y, h.Z), new(-h.X, h.Y, -h.Z)),
            (new(0, 1, 0), new(-h.X, h.Y, h.Z), new(h.X, h.Y, h.Z), new(h.X, h.Y, -h.Z), new(-h.X, h.Y, -h.Z)),
            (new(0, -1, 0), new(-h.X, -h.Y, -h.Z), new(h.X, -h.Y, -h.Z), new(h.X, -h.Y, h.Z), new(-h.X, -h.Y, h.Z))
        ];

        foreach (var (normal, a, b, c, d) in faces)
        {
            primitive.AddQuadrangle(
                (new VertexPositionNormal(a, normal), new VertexTexture1(new Vector2(0, 1))),
                (new VertexPositionNormal(b, normal), new VertexTexture1(new Vector2(1, 1))),
                (new VertexPositionNormal(c, normal), new VertexTexture1(new Vector2(1, 0))),
                (new VertexPositionNormal(d, normal), new VertexTexture1(new Vector2(0, 0))));
        }
    }
}
