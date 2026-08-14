// The release being prepared right now, shown on the Releases page before the GitHub release
// exists — it is created when the version tag is pushed. An entry whose tag is already on GitHub
// is ignored, so this never duplicates or overrides a published release.
//
// Generated from RELEASE_NOTES.md by tools/release-feed.py. Edit the notes, not this file.
window.LOCAL_RELEASES = [{
  "tag_name": "v12.1.0-preview.6",
  "name": "Ava3DControl 12.1.0-preview.6",
  "published_at": "2026-08-13T00:00:00Z",
  "html_url": "https://github.com/pavel-zheltiakov/Ava3DControl/releases",
  "body": "Vulkan draws on a Mac. Additive — code built against `12.1.0-preview.5` still compiles and renders the\nsame.\n\n## API\n\n### Renderers\n\n- (Changed) `RenderBackendKind.Vulkan` can be selected on macOS, and takes effect on the next frame.\n- (Added) `BackendOption.MissingComponents` lists what a renderer needs that the machine has not got.\n- (Added) `MissingComponent` carries a name, a package, a command and a link, so a UI can offer them.\n\n## Improvements\n\n- Vulkan is a renderer you can pick on a Mac, rather than a row saying why you cannot.\n- A new `Ava3DControl.MoltenVK` package carries MoltenVK, for applications that would rather not ask.\n\n## Known limits\n\n- No shadows, no skinning, no transparency sorting; binary `.glb` only.\n- Vulkan on a Mac needs MoltenVK, and is slower than Metal because it runs on it.\n- Vulkan is not available in the browser, which offers only WebGL 2.\n- Vulkan samples textures without mipmaps or anisotropic filtering, so fine repeating detail at a glancing\n  angle shimmers.\n- Lights are not free. The software renderer costs a pass over every vertex per light, and OpenGL draws\n  sixteen at worst.\n- Environment maps are eight bits a channel: put the sun in a light, not in the image.\n- The software renderer lights per vertex, samples only the base-colour map, and does not anti-alias.\n- Browser builds must be published rather than assembled, and need `WasmBuildNative`; trimmed ones need\n  five `TrimmerRootAssembly` entries.\n\n**This is a beta.** `12.1.0-preview.6` is a prerelease and needs `--prerelease` to install.\n\nDocs: https://pavel-zheltiakov.github.io/Ava3DControl/"
}];
