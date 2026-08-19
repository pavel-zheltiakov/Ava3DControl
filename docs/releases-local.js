// The release being prepared right now, shown on the Releases page before the GitHub release
// exists — it is created when the version tag is pushed. An entry whose tag is already on GitHub
// is ignored, so this never duplicates or overrides a published release.
//
// Generated from RELEASE_NOTES.md by tools/release-feed.py. Edit the notes, not this file.
window.LOCAL_RELEASES = [{
  "tag_name": "v12.1.0-preview.8",
  "name": "Ava3DControl 12.1.0-preview.8",
  "published_at": "2026-08-19T00:00:00Z",
  "html_url": "https://github.com/pavel-zheltiakov/Ava3DControl/releases",
  "body": "# Eight new ways to change how things look — without writing a shader\n\nEverything built against `12.1.0-preview.7` still compiles, and nothing that did not ask for these draws\nany differently than before.\n\n## Added\n\n- **Texture transforms and sprite sheets** — slide, tile and animate a texture, or show one frame of a sheet.\n- **Vertex colours** — colour a mesh with no texture at all.\n- **Cel shading** — flat cartoon bands of light instead of a smooth falloff.\n- **Matcaps** — a whole metal, clay or wax look from one picture of a lit ball, with no lights in the scene.\n- **Fog and exposure** — fade distance into a colour, and brighten or dim a whole scene with one number.\n- **Soft particles** — a glow meets a wall as smoke, not as a sticker with a visible edge.\n- **Render to texture** — draw a scene into an image, with no window at all.\n- **Glow and noise textures** — the two images every 3D scene ends up writing for itself, ready made.\n\n## Fixed\n\n- glTF files carrying vertex colours now load them. They had been dropped on import.\n- The OpenGL renderer asks the driver how many textures it may use, instead of assuming seven.\n\n## Worth knowing\n\n- Soft particles need OpenGL. Everywhere else a sprite keeps the hard edge it always had.\n- Cel shading, fog and matcaps are simplified on the software renderer.\n- Rendering into a texture uses the software renderer, so it is slower and stops at four megapixels.\n- What the library does not do at all — shadows, skinning, transparency sorting — is unchanged, and listed\n  in full on the docs site.\n\n**This is a beta.** `12.1.0-preview.8` is a prerelease and needs `--prerelease` to install.\n\nDocs: https://pavel-zheltiakov.github.io/Ava3DControl/"
}];
