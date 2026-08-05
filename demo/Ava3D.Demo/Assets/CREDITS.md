# Third-party assets in the demo

## `camera.glb`

**Camera 01**, by Rajil Jose Macatangay, from [Poly Haven](https://polyhaven.com/a/Camera_01).

Licensed **CC0 1.0 Universal** — public domain dedication. No attribution is required, no permission is
needed, and it may be used commercially. Everything on Poly Haven is published under that licence.

This credit exists anyway, because knowing where an asset came from is worth more than the licence
requires and costs nothing.

### What was done to it

Downloaded as the 1k glTF package — `Camera_01_1k.gltf`, `Camera_01.bin`, and nine JPEG maps — and
repacked into a single binary `.glb`. That is a container change only: every buffer and every image was
appended to one BIN chunk and the JSON rewritten to reference bufferViews instead of URIs. The geometry,
the UVs and the material parameters are exactly what the author published; nothing was re-exported.

`GltfLoader` reads binary glTF only, because a multi-file `.gltf` needs every satellite file fetched
through a URI resolver, and a browser build makes that awkward. Packing first is the answer, and this
model is the demonstration that it works on something a person actually modelled rather than on something
the demo generated for itself.

The 1k texture set was chosen over 2k/4k/8k to keep the WebAssembly download honest: 2.4 MB embedded,
against roughly 9 MB for 2k.

## `kestrel.glb`, `harrier.glb`, `raider.glb`, `relay.glb`

The freighter, the escort, the raider and Relay Nine, from the `Contact` scene. **Ours.** Modelled from
scratch for this demo, so there is no third-party licence on them, nothing to attribute, and nothing to
check before reusing them.

They are a build output rather than a binary you have to take on trust. `tools/models/build-models.py` is
the Blender script that produces all four, and it runs headless in about a second:

```
/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup \
    --python tools/models/build-models.py
```

Four files, 105 KB between them, 1,600 triangles in total. No textures — the detail is in the facets, in
the panel lines the scene derives from the geometry, and in the emissive materials, which is what keeps
them this small.

The material names are an interface, not decoration. `Fleet` looks up `relay.lamp.0` … `relay.lamp.5` to
chase the docking lamps, `kestrel.engine` to throttle the engines, and `raider.hull` to light a raider
from inside while it is being shot at. Rename one in the script and the scene will tell you, loudly.

### Everything else

Every texture in this demo is generated in code at startup — see `Textures/Procedural.cs`,
`Textures/Space.cs` and `Textures/Billboards.cs` — as is every mesh outside the five `.glb` files above.
That was a deliberate choice and it stands: generated maps carry no licence, add nothing to the download,
and make the PBR scenes legible, because a roughness map that is four lines of arithmetic is one you can
read.
