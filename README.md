# Ava3DControl · 12.1.0-preview.2

A 3D viewport control for [Avalonia](https://avaloniaui.net/). Metallic-roughness PBR, glTF loading and
triangle-accurate picking, in one binary that renders through Metal, OpenGL, WebGL 2 or the CPU — chosen at
runtime from whatever the host can offer.

[Live demo](https://pavel-zheltiakov.github.io/Ava3DControl/demo.html) ·
[3D Guide](https://pavel-zheltiakov.github.io/Ava3DControl/api/a-scene-from-nothing.html) ·
[API reference](https://pavel-zheltiakov.github.io/Ava3DControl/api/#reference) ·
[Releases](https://pavel-zheltiakov.github.io/Ava3DControl/releases.html)

Freeware, commercial use included. Preview release — pin the exact version.

```
dotnet add package Ava3DControl --prerelease
```

The major and minor version track the Avalonia release this is built against, so 12.1.x is for Avalonia 12.1.

## In this repository

- `docs/` — the documentation site, and the browser demo it hosts.
- `demo/` — the demo application in full: twenty-six scenes and five platform heads. It restores
  Ava3DControl from nuget.org, exactly as your own project would.
- `LICENSE.md`, `THIRD-PARTY-NOTICES.md`.

The library's own source is not here. This repository is the released package, its documentation, and a demo
you can copy from.

## Running the demo

```bash
cd demo
dotnet run --project Ava3D.Demo.Desktop        # macOS, Windows, Linux
dotnet run --project Ava3D.Demo.Browser        # a browser tab
```

Twenty-six scenes in the order they build on each other, each a single self-contained file under
`demo/Ava3D.Demo/Scenes/` that copies into your own project without untangling. Last is Contact — a
sixty-second film on a loop, with a story, a shot list and a camera that flies itself.

Switch scenes from the toolbar, or turn on Touring to have them advance by themselves. The Engine picker
switches renderer. Switches, on every head: AVA3D_SCENE, AVA3D_TOUR, AVA3D_GL, AVA3D_SOFTWARE, AVA3D_PROBE.
In a browser the first two are query-string parameters instead.

## Using it

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

The camera frames the scene by itself; orbit, pan and zoom are already wired up. View.Info reports which
renderer you got, at what frame rate, and what the platform could not offer.

Browser builds need one extra property, or the WebAssembly runtime aborts with no message on the first GL
call:

```xml
<PropertyGroup>
    <WasmBuildNative>true</WasmBuildNative>
</PropertyGroup>
```

## Limits

- No shadows, no image-based lighting, no animation or skinning.
- Transparency is ordered per object, not per triangle.
- Binary .glb only.
- Android builds but has not been run on a device.
- OpenGL on iOS renders nothing, which is an upstream Avalonia defect. Metal is used there instead.

## Licence

Freeware. Use it for anything, including commercially, at no cost, and ship it inside something you sell.
What you may not do is resell the library itself. The demo's source is yours to copy outright, without
attribution. See [LICENSE.md](LICENSE.md) for the exact terms.

## Feedback

[GitHub Issues](https://github.com/pavel-zheltiakov/Ava3DControl/issues), or the
[Telegram topic](https://t.me/avadevtools/199) for anything shorter.
