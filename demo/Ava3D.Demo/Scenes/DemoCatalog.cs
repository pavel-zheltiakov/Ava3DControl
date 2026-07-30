namespace Ava3D.Demo.Scenes;

/// <summary>
/// Every scene, in the order they build on each other: one cube, then geometry, then the transform tree,
/// then materials and lighting, then textures, then the things a viewer control is actually asked for —
/// picking, animation, a scene big enough to hurt — and finally the two that exist to show the shading
/// model, a metallic-roughness chart and a fully textured PBR surface.
///
/// Factories rather than instances, because a scene is rebuilt from scratch each time it is selected. That
/// keeps every scene's state its own and means the tour can run for an hour without accumulating anything.
/// </summary>
public static class DemoCatalog
{
    public static IReadOnlyList<Func<DemoScene>> Scenes { get; } =
    [
        () => new HelloCubeScene(),
        () => new PrimitivesScene(),
        () => new TransformsScene(),
        () => new MaterialsScene(),
        () => new LightingScene(),
        () => new TexturesScene(),
        () => new NormalMappingScene(),
        () => new PickingScene(),
        () => new AnimationScene(),
        () => new StressScene(),
        () => new GltfScene(),
        () => new PbrChartScene(),
        () => new PbrShowcaseScene()
    ];

    /// <summary>Titles and summaries without building anything, for the list.</summary>
    public static IReadOnlyList<DemoScene> Describe() => [.. Scenes.Select(f => f())];
}
