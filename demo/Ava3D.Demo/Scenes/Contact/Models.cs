using System.Numerics;
using Avalonia.Platform;

namespace Ava3D.Demo.Scenes.Contact;

/// <summary>
/// A loaded <c>.glb</c>, plus the two things the film needs out of it that the file does not hand over
/// by itself: its materials by name, and its panel lines.
///
/// The four models — <c>kestrel</c>, <c>harrier</c>, <c>raider</c> and <c>relay</c> — are built by
/// <c>tools/models/build-models.py</c>, which is a Blender script that runs headless and takes about a
/// second. They are the demo's own work, so there is no licence on them and nothing to attribute; the
/// script is checked in beside them, so the models are a build output rather than a binary somebody has
/// to take on trust.
/// </summary>
internal sealed class Model
{
    /// <summary>The whole model under one node, already scaled to the size the scene wants.</summary>
    public required Node Root { get; init; }

    /// <summary>Its materials, keyed by the name they were given in Blender.</summary>
    public required Dictionary<string, Material> Materials { get; init; }

    /// <summary>
    /// Panel lines: the creased edges of the geometry, in the model's own space.
    ///
    /// A ship the demo built for itself could carry a hand-written edge list. A ship that came out of a
    /// modelling package cannot, so the lines are found rather than authored, by
    /// <see cref="Mesh.GetEdges"/> at <see cref="Models.CreaseDegrees"/>.
    /// </summary>
    public required Vector3[] Edges { get; init; }

    public Material Material(string name) =>
        Materials.TryGetValue(name, out var material)
            ? material
            : throw new KeyNotFoundException(
                $"no material '{name}' in the model — it has {string.Join(", ", Materials.Keys)}");

    /// <summary>Every material whose name starts with <paramref name="prefix"/>, in name order.</summary>
    public Material[] Group(string prefix) =>
    [
        .. Materials
            .Where(pair => pair.Key.StartsWith(prefix, StringComparison.Ordinal))
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => pair.Value)
    ];
}

internal static class Models
{
    /// <summary>
    /// How sharp a fold has to be before <see cref="Mesh.GetEdges"/> calls it a panel line, in degrees.
    ///
    /// Well above the 30° default, and chosen against this geometry rather than by eye: a hull section
    /// here is a six- or eight-sided prism, whose facets meet at 60° and 45°, and a threshold below
    /// those would trace every one of them and turn the ship into a wireframe. Above them, what is left
    /// is where two parts of the ship actually meet — a wing root, a nacelle against a fuselage, the
    /// corner of a bridge — which is what a panel line is.
    /// </summary>
    private const float CreaseDegrees = 65f;

    public static Model Load(string file, float scale)
    {
        using var stream = AssetLoader.Open(new Uri($"avares://Ava3D.Demo/Assets/{file}"));
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);

        var loaded = GltfLoader.Load(buffer.ToArray(), file);

        // The loader parents what it read to a scene of its own; the film wants it in its own graph.
        var root = loaded.Children[0];
        loaded.Children.Remove(root);
        root.Scale = new Vector3(scale);

        var materials = new Dictionary<string, Material>(StringComparer.Ordinal);
        var edges = new List<Vector3>();

        foreach (var child in root.Children)
        {
            if (child is not MeshNode { Mesh: { } geometry } node)
                continue;

            if (node.Material.Name is { } name)
                materials[name] = node.Material;

            // Every material was authored with backface culling on except the inside of the docking
            // bay, and glTF carries that across as doubleSided. Turning it into a cull mode is what
            // makes it mean anything: a closed hull drops its far side, and the one surface that is a
            // tube you look down keeps both of its.
            node.Material.Cull = node.Material.DoubleSided ? CullMode.None : CullMode.Back;

            // Into the root's space, since that is where the LineNode carrying them will hang.
            foreach (var end in geometry.GetEdges(CreaseDegrees))
                edges.Add(Vector3.Transform(end, node.LocalTransform));
        }

        return new Model { Root = root, Materials = materials, Edges = [.. edges] };
    }
}
