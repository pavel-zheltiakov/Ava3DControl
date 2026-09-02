namespace Ava3D.Demo.Scenes;

/// <summary>
/// Every scene, in the order they build on each other: one cube, then geometry, then the transform tree,
/// then materials and lighting, then textures and the surface detail that goes on top of them, then the
/// things that are drawn but are not lit triangles — sprites, lines, points, blending — then the things a
/// viewer control is actually asked for, picking and animation and a scene big enough to hurt, then the
/// board and the four ways of looking at it, then the two that exist to show the shading model, and last
/// the one that exists to be watched.
///
/// Each of the middle group shows exactly one feature, with it on and off in the same frame wherever that
/// is possible. A scene that demonstrates six things demonstrates none of them.
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
        () => new FlatShadingScene(),
        () => new TransformsScene(),
        () => new MaterialsScene(),
        () => new LightingScene(),
        () => new EnvironmentScene(),
        () => new TexturesScene(),
        () => new NormalMappingScene(),
        () => new BumpScene(),
        () => new UvTransformScene(),
        () => new VertexColorScene(),
        () => new ToonScene(),
        () => new MatcapScene(),
        () => new FogScene(),
        () => new ShadowsScene(),
        () => new ClockTowerScene(),
        () => new ShadowProbeScene(),
        () => new BloomScene(),
        () => new TextureKitScene(),
        () => new RenderToTextureScene(),
        () => new SoftParticlesScene(),
        () => new PixelTextureScene(),
        () => new Arcade.GrassBricksScene(),
        () => new Arcade.CoinMazeScene(),
        () => new Arcade.RunnerScene(),
        () => new Arcade.BlocksScene(),
        () => new UnlitScene(),
        () => new CullingScene(),
        () => new BlendingScene(),
        () => new SpritesScene(),
        () => new LinesScene(),
        () => new DepthBiasScene(),
        () => new PointsScene(),
        () => new RimScene(),
        () => new FourLightsScene(),
        () => new LampTreeScene(),
        () => new PickingScene(),
        () => new AnimationScene(),
        () => new StressScene(),
        () => new GltfScene(),
        () => new ModelScene(),
        () => new MotherboardScene(),
        () => new Board.InspectorScene(),
        () => new Board.IndicatorsScene(),
        () => new Board.DraftsmanScene(),
        () => new Board.WireframeScene(),
        () => new PbrChartScene(),
        () => new PbrShowcaseScene(),
        () => new Contact.ContactScene()
    ];

    /// <summary>Titles and summaries without building anything, for the list.</summary>
    public static IReadOnlyList<DemoScene> Describe() => [.. Scenes.Select(f => f())];
}
