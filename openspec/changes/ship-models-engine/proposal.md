# Proposal: ship-models-engine

## Why

The owner, 2026-11-04: "we need to integrate proper ships in the game. I've approved ships to be built". The approved
ships are the Concord line (mockup 6, `docs/mockups/concord-hull-concepts.html`, "the standard for Concord ships") and
the Krai Warhost line (mockup 7, `docs/mockups/krai-ship-concepts.html`). The game drew every ship as a stand-in
primitive (`scenes/standins/`).

## What Changes

- **`tools/mockups/export_ships.mjs`** builds each approved ship intact with the mockup's own builder, in a headless
  browser, and exports it as a glb with three.js's GLTFExporter: one implementation of every hull (CLAUDE.md section
  6.1), so the game's ships are the approved ones and change when the mockup changes. Deterministic: the same page gives
  the same bytes.
- **`assets/ships/`**: the Concord line's eight variants and the Krai line's four classes, the factions' detail tiles,
  and `manifest.json` (lengths, triangles, node names, materials, and the hull shader's parameters).
- **Named nodes** for what the game will drive (CLAUDE.md section 10): mounts, engines, the bridge, the reactor and the
  lamps, as empties in each glb.
- **The battle draws them**: a class with a `model` is drawn as its ship, scaled to the class's battle length; the
  hull plating is the mockup's tri-planar detail and the Krai livery, ported to a Godot shader (`hull.gdshader`).
  Classes of factions not designed yet (the Kethran, the Vos) keep their stand-ins.
- Not yet: battle damage (holes, scorch, venting, nacelles shot off, the hull breaking apart), the lamps lit, windows
  with rooms behind. Each is a task here and a line in `docs/design/mockup-sync.md`.
