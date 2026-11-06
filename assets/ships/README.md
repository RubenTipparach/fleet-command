# Ships

The approved ship designs as glTF binary files for the game: the Concord line (mockup 6,
`docs/mockups/concord-hull-concepts.html`), the Krai Warhost line (mockup 7,
`docs/mockups/krai-ship-concepts.html`) and the Ssarith Hunt (mockup 12, `docs/mockups/ssarith-ship-concepts.html`).

| File | What it is |
|---|---|
| `concord/<id>.glb` | Every Concord line variant except the Tern (Star Crew's yardstick): `swift_below`, `swift_updown`, `swift_level`, `petrel_updown`, `petrel_level`, `albatross`, `condor`, `pelican` |
| `krai/<id>.glb` | Every Krai class: `corvette`, `frigate`, `cruiser`, `dreadnought` |
| `ssarith/<id>.glb` | The Ssarith Hunt (mockup 12, `docs/mockups/ssarith-ship-concepts.html`): the frigates `hooded_lance`, `forked_prow`, `harpoon`, `trident`, the `cruiser` and the `carrier`. Geometry, UVs and empties only: each hull's look is its own baked maps beside it (`ssarith/<id>_base.png`, `_orm`, `_normal`, `_emit`, `_light`, `_albedo`; `tools/blender/painted_bake.py --ship`, recorded in `ssarith/manifest.json`), named in the ship's `shader_params.maps` and drawn by `src/Game/View/baked_hull.gdshader` |
| `ssarith/<id>_sdf.bin` | Each Hunt hull's signed distance field, the inside its cut faces clip to (one byte a cell, x fastest; the manifest's `break.bodies[].sdf` gives its grid; `tools/blender/hull_sdf.py`) |
| `<faction>/detail_albedo.png`, `detail_normal.png` | The hull shader's detail tile, 1024 px square: grime and wear in the red channel, and a tangent-space normal map (xy). The mockups project it tri-planar in ship space, with no UVs |
| `manifest.json` | For each ship: size, triangles, node names, materials, and the values its hull shader read. For each faction: what the hull shader did, its constants, and the detail tile's size in metres |

## What made them

`tools/mockups/export_ships.mjs` made every file here. It loads each mockup page headless and lets the page build
its own ships, intact. It copies their meshes out as they stand and exports them with three.js r147's GLTFExporter
(vendored in `tools/mockups/vendor/`). No geometry rule has a second copy: change a ship in its mockup, then re-run.

```sh
NODE_PATH=$(npm root -g) node tools/mockups/export_ships.mjs
```

Re-running is the only step needed when a mockup changes. The current files were exported on 2026-11-04 on the Mac
from the Krai page v21 and the Concord page v20 (two runs, byte-identical). The `<ship>_<n>.png` files beside a glb are
Godot's import of the textures embedded in it (the registry decals), committed with their `.import` files. Each ship's `source` there names the page, its published version
(the newest in `docs/artifacts/<page>/README.md`) and the sha256 of the page file it came from, so a check can tell a
stale export: the page's sha256 differs. The exporter stops if a page changes while it runs.

The run is deterministic: a second run gives byte-identical glb, PNG and manifest files, and it prints each file's
sha256. It also renders every glb, loaded back with GLTFLoader, onto `docs/screenshots/ships/export-contact.png`.
Look at that sheet before you commit a change. A test (`tests/Core.Tests/ScenarioTests.cs`) fails when a page's sha256
no longer matches the manifest: the export is stale, re-run it.

## In the game

`src/Game/View/ShipSkin.cs` dresses each ship from the manifest: `src/Game/View/hull.gdshader` on every material marked
`hull_shader` (its base colour, vertex colours, metalness, the faction's detail tile, the ship's `origin_offset_m` as
`mockup_offset = (-o.x, o.y, -o.z)`, and the Krai livery's `uLiv` and `uLiv2`), and a lamp on every `light_*` empty from
its extras (a flood is a `SpotLight3D`, the rest small `OmniLight3D`s; strobes double flash). Not yet ported: the
Concord's own paint rules (the service band, livery stripes, windows and light pools of its `hullMat`), so a Concord
hull shows the armour white with the detail tile only.

## Axes and units

- Metres. +Y up. **Bow toward -Z, port toward -X** (Godot's forward is -Z). The mockups build with +Z bow and +X
  port, so every point is turned half round Y.
- The origin is the centre of the ship's bounding box on all three axes.
- The hull shaders work in the mockup's own space. For a glb point (x, y, z),
  `p_mockup = (-(x + o.x), y + o.y, -(z + o.z))`, where `o` is the ship's `origin_offset_m` in the manifest.

## Nodes

- **Root**: the ship's id.
- **Pieces**: one node per piece of the mockup, at the piece's own pivot (so a piece that breaks away tumbles about
  itself): `hull`, `secondary`, `neck`, `command`, `wing`, `pod_<side>`, `pylon_<side>`, `nacelle_<side>`, `blade`,
  `crest`. A side is `port`, `starboard`, `dorsal` or `ventral`, with `_2` for a second pair.
- **Meshes**: under its piece, merged by zone and material so draw calls stay low:
  `<piece>[_<zone>]_<material>`. The zone (`weapons`, `drive`, `deflector`, `reactor`, `bridge`) is left out where it
  is the piece's own. Each mesh's extras give its piece, zones and material.
- **Empties**, for what the game aims from, fires from, lights and hits (CLAUDE.md section 10), under the piece that
  carries them, with their facts in glTF extras:
  - `mount_turret_<i>` (Concord: turret, point defence, main gun, sponson), `mount_torpedo_<i>`, `mount_gun_<i>`
    (Krai gun muzzles). A mount's +Y is the way it faces out of the hull.
  - `engine_<i>` (with `radius_m`), `deflector`, `reactor`, `bridge`, `bay_<i>` (the Pelican's hangar doors).
  - `light_nav_port`, `light_nav_starboard`, `light_nav_aft`, `light_strobe_<side>`, `light_beacon_dorsal`,
    `light_beacon_ventral`, `light_flood_<i>`. Each has its colour (linear RGB and hex). A flood's -Z points the way it
    shines, as a Godot `SpotLight3D` does, with its range and strength.

Not exported: live lights, plumes, particles, wires, damage, shields and broken pieces.

## Materials

Glows, metals, vertex colours and textures export as glTF PBR (a glow brighter than 1 uses
`KHR_materials_emissive_strength`). Some looks exist only in a mockup's shader, so they cannot export. Those
materials carry a PBR stand-in, and the manifest records what the shader did:

- **Hull paint, detail and livery.** The Concord's paint, plate seams, service band, stripes, windows and floodlight
  pools, and the Krai's detail, bronze seams, war stripe, pin lines and tallies. The manifest has the shader's rules,
  its constants, and each ship's uniforms (`shader_params`, and each material's `uniforms`). A Godot shader can paint
  them the same way from `detail_albedo.png` and `detail_normal.png`.
- **Thruster bells.** The inside of a bell glows with heat in the mockup. The glb has the outer shell.
- **Krai windows.** Each pane shows a room by interior mapping in the mockup. The glb has a warm lit pane.
