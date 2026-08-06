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

## `board.glb`

The ATX motherboard and its indicator panel, shared by the `Motherboard`, `Board inspector`,
`Indicators`, `Draftsman` and `Wireframe` scenes. **Ours**, and generated — no third-party licence, no
attribution, nothing to check before reusing it.

It was built this way because there is no alternative that could be shipped here. There is no CC0
motherboard model: Poly Haven has none, ambientCG publishes circuit-board *materials* but no geometry, the
good models on Sketchfab are CC-BY, and KiCad's parts library requires attribution and forbids
redistribution as a collection. Every one of those would put an attribution obligation on anyone who reused
this demo, which is the one promise these assets make.

Four files build it, and they take about five seconds between them:

```
python3 tools/models/build-pcb-texture.py
/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup \
    --python tools/models/build-pcb.py
python3 tools/models/build-pcb-manifest.py
python3 tools/models/check-pcb.py
```

`pcb_layout.py` holds every position in millimetres and is the only place any of them appear — the ATX
outline, the nine mounting holes at their specified coordinates, the socket, the slots, the connectors and
420 scattered passives. It also routes the copper: a maze router over a one-millimetre grid, one net at a
time, every cell a net uses taken out of the grid before the next one starts, so two traces physically
cannot cross. Nets that cannot get through drop to an inner layer — a via at each end and nothing drawn
between — which is what a real board does and what every gap in the copper here is.

The same table also holds the second board. Six 5 mm indicators sit on a panel of their own, thirty
millimetres off the motherboard's right-hand edge with nothing but air between them, joined to it by a
flex jumper: a strip of polyimide swept along a path, ten conductors printed on top of it, translucent
because polyimide is. That is what a flex is for — it connects two boards that are not in the same place
— and it is why the motherboard's copper stops dead at J1 and begins again at J2. The path runs straight
and level for five millimetres out of each connector before it is allowed to bend at all, because that is
the part the connector is gripping; the arch and the sideways wander are both in the middle, where
nothing is holding it.

`build-pcb-texture.py` draws the solder mask, the pour, the copper and the pads from that same table,
which is what makes the traces run *to* the parts rather than near them. `build-pcb.py` builds the
geometry and packs both maps into the `.glb`. `build-pcb-manifest.py` writes `board.txt` beside it — the
half of a board that geometry cannot carry: what each part is called, what it is, and which copper
reaches it. `check-pcb.py` fails the build if two footprints overlap, if a mounting hole ends up under a
component, if a part is on neither board, if any closed solid came out wound inside out, if two
designators collide, if any two copper runs cross, if a flex bends before it is clear of its connector or
lies over a component while it is still low, or if the manifest has drifted from the layout. The first
time it ran it found five collisions and a screw hole beneath the USB stack; the crossing test found
twenty more the day the router replaced the elbows, and one more the day the flex connector arrived.

2.7 MB, 610 nodes over 194 meshes, 32,620 triangles of unique geometry drawn 47,596 times, 859 routed
copper runs and 744 vias, plus seven hand-laid nets on the panel. 1.1 MB of that is the two texture maps
and 71 KB is the manifest. The four hundred small parts share four meshes between them, which is why the
file is that size and why the scene has something to say about draw calls.

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
`Textures/Space.cs` and `Textures/Billboards.cs` — as is every mesh outside the six `.glb` files above.
The board is the one exception on the texture side: its two maps are drawn by a build script and packed
into the model, because a 2048-pixel board surface is not something to generate on every startup.
That was a deliberate choice and it stands: generated maps carry no licence, add nothing to the download,
and make the PBR scenes legible, because a roughness map that is four lines of arithmetic is one you can
read.
