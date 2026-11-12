# Design: console-draw-cost

## D1. Measured (Raspberry Pi 5, battle at 1280x720, `--lowspec --lowspec-scale=1.0`)

| State | FPS | Frame | Draw calls |
|---|---|---|---|
| main | 20 | 50 ms | about 1,280 |
| Console updates frozen | 32 | 31 ms | about 1,300 |
| Console hidden | 41 | 25 ms | about 190 |
| Capped redraws (D2) | 23 | | |
| plus cached rounded-rect meshes and fill triangles | 26 | | |

`dotnet-trace` (sampled thread time) after those steps:

| What | Share of the main thread |
|---|---|
| Console panels' drawing | 51% |
| The 3D view's overlay | 36% |
| The systems display | 33% |
| Icons | 25% |
| Text | 11% |
| Fills | 10% |


### D1b. Where the console's draw calls come from (Pi 5, `--scenario=concord_battle`, the cache forced to render every frame)

Each component was hidden in turn and the frame's median draw calls read:

| Hidden | Draw calls saved |
|---|---|
| Ship panel (the systems display) | about 276 |
| Station panel (comms tab) | about 169 |
| Radar | about 68 |
| Bottom strip | about 37 |
| Weapons panel | about 31 |
| The view's overlay (early in the battle) | about 19 |
| Top strip | about 17 |
| Plate and column | about 5 |
| Every console component | about 623 (the 3D scene alone: about 166) |

By kind of drawing:
- **Icons:** about 310 draw calls, half the console's. With every icon off, the frame fell from 786 to 476. Each icon is several vector fills, lines and arcs.
- **Fills:** about 95 draw calls, overlapping the icons.

**Why.** Godot 4's canvas renderer merges consecutive rectangles, nine-patches and text glyphs that share a texture into one draw call. Every polygon-type command is its own draw: `DrawColoredPolygon`, triangle arrays, `DrawArc`, `DrawCircle`, antialiased `DrawPolyline` and wide `DrawLine`. A batch also breaks at every texture change, for example between a kit piece and a run of text.

**What batches, and how:**
- **Icons:** baked into the kit's atlas once, then drawn as textured rectangles that merge with the 9-slices around them.
- **Rings and arcs** (shield arcs, the radar's rings, contact dots, lamps): one `MultiMesh` per panel. A unit quad, with a shader that draws a ring segment from each instance's custom data (inner and outer radius, start and end angle) and colour. That makes every arc in a panel one draw call.
- **Cell rows** (power, systems, capacitor and speed pips): already 9-slices from the atlas. They merge once text no longer interleaves with them.
- **Text:** drawn in a child canvas item of each panel, above its shapes, so a panel's glyphs are one or two batches.

**Also found.** The game's menu screen (the flow, before a battle) drew about 8,126 draw calls at 4 fps on the Pi. That's a separate change.

## D2. Capped redraws (done)

- `ConsolePanel._Process` queues a redraw every `1 / panel_redraw_hz` seconds (`data/console/layout.json`: 30, and 15
  with `--lowspec`).
- A panel also redraws at once on `_GuiInput`, the pointer leaving, a resize, a visibility change or a theme change.
- Panels are phased by region so their redraws spread over the interval.
- Godot keeps a canvas item's commands between redraws, and a panel's controls stay those of its last drawing, so hits
  and the scripted captain are unchanged.

## D3. The console in its own viewport

- The panels, strips and column backing move under a `SubViewport` (transparent, the window's size, the same scale).
  It is shown by a `SubViewportContainer` filling the console layer, which forwards input to the panels as before.
- The viewport's `RenderTargetUpdateMode` is `Disabled`. A panel that queues a redraw sets it to `Once` for that frame.
- `ViewOverlay` (brackets and labels that follow ships every frame) and `TooltipLayer` stay in the console layer above
  the container, drawn every frame as now.
- **Blending.** UI drawn into a transparent target and then composited can darken antialiased edges and translucent fills
  (straight alpha blended twice). The container's texture is drawn with premultiplied-alpha blending
  (`CanvasItemMaterial.BlendMode = PremultAlpha`), and the stills check (D5) is the proof.
- `ConsolePanel.Controls()` reports screen coordinates; with the viewport at the window's origin and scale they are
  unchanged. A test pins it.

## D4. Textures instead of live vector

- **Rounded rectangles.** One atlas of white rounded corners, one cell per radius, each feathered over 0.8 px as
  `StyleBoxFlat` was. A rectangle is drawn as a 9-slice (`RenderingServer.CanvasItemAddNinePatch`) from its corner cell,
  with the colour as the modulate. Outlines (`RoundRectLine`) use a ring cell per radius and width.
- **Icons.** At load, each skin's icon set is drawn once from its vector code (`IconGeom`, `ConcordKit.DefineIcons`) into an
  atlas, white, at the sizes the console uses, and drawn tinted. The vector code stays the one source.
- **The Concord's pieces.** Its frames, keys, pills and lamps become 9-slice and hero pieces like the rendered skins
  (`RenderedPieces`), baked at load from `ConcordKit`'s own drawing, with their margins recorded beside them.
- **Order.** Shapes and textures from one atlas batch together. Text is drawn in a child layer of each panel, above its
  shapes, so glyphs do not break the shapes' batches. Where a skin draws something over text, it stays in order and is
  listed.

## D5. Proving the look is unchanged

- `scripts/game-stills.sh` (78 named stills: every skin, expanded panels, menus, target views, ships) is rendered on main
  and on the branch, on the same machine and renderer, and every pair is diffed.
- A pair that differs by more than rounding is put side by side (`tools/compare/side_by_side.py`), looked at, and fixed
  or shown to the owner.
- What stills cannot show (animation smoothness at 30 Hz) is said in the PR, for the owner to watch.

## Open questions

| # | Question | Recommendation (provisional until answered) |
|---|---|---|
| CD1 | Is 30 redraws a second enough for the console's animations (the warp key's pulse, lamps)? | 30 on desktop and phones, 15 in low-spec; the owner watches them once |
| CD2 | Bake icons and pieces at load, or ship them as files from a generator? | At load, from the same code: no second copy to drift; a file generator later if load time matters |
