// The release being prepared right now, shown on the Releases page before the GitHub release
// exists — it is created when the version tag is pushed. An entry whose tag is already on GitHub
// is ignored, so this never duplicates or overrides a published release.
//
// Generated from RELEASE_NOTES.md by tools/release-feed.py. Edit the notes, not this file.
window.LOCAL_RELEASES = [{
  "tag_name": "v12.1.0-preview.9",
  "name": "Ava3DControl 12.1.0-preview.9",
  "published_at": "2026-09-02T00:00:00Z",
  "html_url": "https://github.com/pavel-zheltiakov/Ava3DControl/releases",
  "body": "# Added shadow support\n\nEverything built against `12.1.0-preview.8` still compiles. Nothing draws differently unless you ask.\n\n## Added\n\n- **Shadows.** Set `CastsShadows` on a light. Geometry now blocks it, on every renderer.\n- **Shadow controls.** `Scene.ShadowMapSize`, `ShadowBias` and `ShadowStrength` tune the map. `Scene.ShadowsEnabled` switches it off and on.\n- **Per-object opt-out.** `MeshNode.CastsShadow` keeps a mesh out of the map.\n- **Bloom.** `Scene.BloomThreshold`, `BloomIntensity` and `BloomRadius`. A bright thing spills light around it.\n- **Vignette.** `Scene.Vignette` darkens the corners of the frame.\n- **Subsurface scattering.** `Material.SubsurfaceColor` and `SubsurfaceWrap`, for skin, wax and leaves.\n- **Morph targets.** `Mesh.MorphTargets` and `MeshNode.MorphWeights` blend shapes. A face can blink.\n- **Mesh simplification.** `Mesh.Simplified` cuts a mesh to a triangle budget. Creases survive.\n- **Vertex alpha.** `Material.VertexAlpha` reads transparency from vertex colours.\n- **Draw only on change.** `Ava3DView.RenderTrigger` stops the loop on a still scene. `InvalidateScene` asks for the next frame.\n\n## Worth knowing\n\n- One light casts. The first that asks wins. `Scene.ShadowCastingLight` says which.\n- A point light shadows a cone aimed at the scene. Under the lamp, not behind it.\n- The CPU renderer's shadows are coarser, as its lighting is.\n- Still missing: animation, skinning, per-triangle transparency sorting. The full list is on the docs site.\n\n**This is a beta.** `12.1.0-preview.9` needs `--prerelease` to install.\n\nDocs: https://pavel-zheltiakov.github.io/Ava3DControl/"
}];
