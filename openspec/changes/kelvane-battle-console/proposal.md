# Proposal: kelvane-battle-console

## Why

The owner, 2026-11-06: "start work on the UI for the institute, get latest from main and maintain the highest possible
standard for UI design".

The Kelvane Institute has a console skin on the console skins page (mockup 4). It is built like a Nucleo NLog
instrument (owner, 2026-10-31: "They were inspired by the nucleo look"): champagne plates, amber readouts and phosphor
scopes. It has no battle console yet. The Krai (mockup 8) and the Ssarith Hunt (mockup 15) have one, and the console
follows the ship you command (`fleet-skins`).

## What Changes

- **The battle console in the Kelvane Institute skin**. This is mockup 20, `docs/mockups/battle-console-kelvane.html`,
  written from the one source page by `tools/mockups/skin_page.py`. Every rule, panel and control is the same code as
  the Concord's.
- **The Institute's library**, `docs/mockups/lib/kelvane.js`. It holds:
  - the palette, as the page's colour roles;
  - all 24 icons of the icon guide, drawn in the Institute's schematic hand;
  - the type and the live readouts;
  - placeholder names for ships, officers and ranks.

  The console skins page's Kelvane module now registers this library, so the look has one implementation.
- **The console's own kit pieces, rendered in Blender** and fit-checked by `tools/mockups/kelvane_shapes.py`:
  - **Panel frames and insets as instruments with a screen.** The console draws its readouts in light, both over its
    panels and over the battle, so each frame is a champagne bezel round a well of dark smoked glass.
  - **Strips** with a long screen between the needle-meter bay and the knob.
  - **Keys and cells.** The square and wide window keys come in seven lights. The allocation cells come in five.
  - **Title placards and the name plate.** The title placard has a lit pilot jewel. The name plate has a flat end for
    the Fleet key.
  - **The column's backing.** It is a bead-blasted champagne plate, engraved with a measuring scale down each edge.
- **A Kelvane ship in the allied fleet and the swap scene.** It is the research cruiser Patience. Key 4 takes it. Until
  the Institute's ships are designed, it is the stand-in hull in the Institute's colours.

## Impact

- New files:
  - `docs/mockups/lib/kelvane.js`
  - `docs/mockups/battle-console-kelvane.html`
  - the new kit renders in `assets/ui/kelvane/`
- Changed files:
  - `docs/mockups/battle-console.html` and the Krai and Ssarith pages written from it
  - `docs/mockups/skins/kelvane.js`
  - `tools/mockups/kelvane_shapes.py`, `tools/blender/build_ui_kelvane.py` and `tools/blender/ui_shapes.py` (satin gets
    optional roughness and coat)
  - `tools/mockups/skin_page.py`
- This is a mockup only. The game's Godot console gets the skin when the owner approves it for implementation.
