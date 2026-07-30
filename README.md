# Ava3DControl · 12.1.0-preview

A 3D viewport control for [Avalonia](https://avaloniaui.net/) that works on every platform Avalonia
targets — including the browser.

📖 **[Documentation and API reference](https://pavel-zheltiakov.github.io/Ava3DControl/)**

Metallic-roughness PBR, glTF loading and triangle-accurate picking, in one control that renders through
Metal on a Mac, OpenGL on Windows and Linux, WebGL 2 in a browser tab, and the CPU when a host offers no GPU
at all — chosen at runtime, from the same binary.

**Freeware, commercial use included.** No fee, no seat count, no registration, and no attribution
requirement in your application.

> **This is a beta.** `12.1.0-preview` is a NuGet prerelease: it will not appear in a default package search
> and needs `--prerelease` to install. Everything documented is measured and working; the suffix is there
> because the API may still move in response to early use. Pin the exact version.

```
dotnet add package Ava3DControl --prerelease
```

📦 **[nuget.org/packages/Ava3DControl](https://www.nuget.org/packages/Ava3DControl)** — and every version's
`.nupkg` is attached to its [release](https://github.com/pavel-zheltiakov/Ava3DControl/releases) here, if you
would rather have the file than the feed.

The major and minor version track the Avalonia release this is built against — `12.1.x` is for Avalonia
12.1 — so the version answers the compatibility question directly.

## What is in this repository

| | |
| --- | --- |
| `docs/` | The documentation site: a landing page, three concept guides with diagrams, and the generated API reference. Served at the link above; open `docs/index.html` for a local copy. |
| `demo/` | The demo application's full source — thirteen scenes and five platform heads. It restores `Ava3DControl` from nuget.org, exactly as your own project would. |
| `LICENSE.md` | The licence. Read section 3 before you resell anything. |
| `THIRD-PARTY-NOTICES.md` | What this builds on, and the fact that it ships no art assets at all. |

The library's own source is not here. This repository is the released package, its documentation, and a demo
you can copy from.

## Running the demo

```bash
cd demo
dotnet run --project Ava3D.Demo.Desktop        # macOS, Windows, Linux
dotnet run --project Ava3D.Demo.Browser        # a browser tab
```

Thirteen scenes, in the order they build on each other: one cube, geometry, the transform tree, materials,
lighting, textures, normal mapping, picking, animation, a 128,002-triangle benchmark, a glTF round trip, the
metallic-roughness chart, and a fully textured PBR surface. Each is a single self-contained file under
`demo/Ava3D.Demo/Scenes/` that you can copy into your own project without untangling it from the demo shell.

Switch scenes from the toolbar, or turn on **Touring** to have them advance by themselves. The **Engine**
picker switches renderer: GPU to CPU takes effect on the next frame, and swapping between GPU APIs relaunches
the desktop demo — because Avalonia fixes its graphics API when the application is built, which a control
cannot change from inside a running process.

Useful switches, on every head:

| | |
| --- | --- |
| `AVA3D_SCENE=pbr` | open on a scene, by index or by name |
| `AVA3D_TOUR=1` | start touring |
| `AVA3D_GL=1` | desktop: ask Avalonia for OpenGL instead of Metal |
| `AVA3D_SOFTWARE=1` | desktop: ask for no GPU context at all |
| `AVA3D_PROBE=8` | render for eight seconds, print what the renderer did, exit |

In a browser the first two are query-string parameters instead: `?scene=stress&tour=1`.

## Using it in your own application

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:a3d="using:Ava3D">
    <a3d:Ava3DView x:Name="View" />
</Window>
```

```csharp
var scene = new Scene();

scene.Children.Add(new MeshNode(
    Primitives.Sphere(0.5f),
    new Material
    {
        BaseColor = new Vector4(1f, 0.77f, 0.34f, 1f),
        Metallic  = 1f,
        Roughness = 0.25f
    }));

View.Scene = scene;
```

The camera frames the scene by itself, orbit, pan and zoom are wired up, and the renderer is whichever one
the platform can actually provide. `View.Info` reports which one that turned out to be, at what frame rate,
and what the platform could not offer.

Browser builds need one extra property, or the WebAssembly runtime aborts with no message on the first GL
call:

```xml
<PropertyGroup>
    <WasmBuildNative>true</WasmBuildNative>
</PropertyGroup>
```

## Measured

One scene — 128,002 triangles in 126 draw calls, the demo's "Stress test" — on the same code:

| Platform | Renderer | fps |
| --- | --- | --- |
| macOS, Apple M3 Max | Metal | 119.9 |
| macOS, Apple M3 Max | OpenGL 4.1 | 120.4 |
| Chrome, WebAssembly | WebGL 2 | 120.0 |
| iOS 26.5 simulator | Metal | 60.1 (vsync-capped) |
| macOS, Apple M3 Max | Skia, CPU | 24.2 |

Open that scene in the demo and read the counter to reproduce any of them.

## What it does not do

No shadows. No image-based lighting — the environment is an analytic two-colour hemisphere, so a mirror
reflects a smooth gradient rather than a room. No animation or skinning. No transparency sorting; alpha
masking works, blending is not ordered. Binary `.glb` only. Android builds but has not been verified on a
device. OpenGL on iOS renders nothing, which is an upstream Avalonia defect rather than a device limit — the
control selects Metal there automatically.

## Feedback

A beta is only worth releasing if the people using it can say something back.

- **[GitHub Issues](https://github.com/pavel-zheltiakov/Ava3DControl/issues)** — a bug, a missing thing or a
  question, in the open, where the answer helps whoever hits it next.
- **[Telegram](https://t.me/avadevtools/199)** — the Ava3DControl topic in the AvaDevTools group, for
  anything shorter than an issue.

## Licence

Freeware. Use it for anything, including commercially, at no cost; ship it inside something you sell. What
you may not do is resell the library itself. The demo's source is yours to copy outright, without
attribution. See [`LICENSE.md`](LICENSE.md) for the exact terms.
