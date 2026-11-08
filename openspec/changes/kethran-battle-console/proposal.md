# Proposal: kethran-battle-console

## Why

The Kethran Hegemony's skin was the owner's second method (2026-10-30: "build the other design language using 3d models
in blender like starcraft", then of version 2: "buttons should be more plain. BUT YES YOU ARE GETTINGIT"). It is rendered
in Blender and lives on the console skins page (mockup 4), whose own script drew its icons and live readouts. The Krai
(mockup 8), the Ssarith Hunt (mockup 15) and the Kelvane Institute (mockup 20) already have a battle console, and the
console follows the ship you command (owner, 2026-11-04: "combine uis together, so we can hop between different ships";
`fleet-skins`). The Hegemony needs its own battle console, and its look needs one home both pages share (CLAUDE.md
section 6.1).

## What Changes

- **The battle console in the Kethran Hegemony skin.** This is mockup 29, `docs/mockups/battle-console-kethran.html`,
  written from the one source page by `tools/mockups/skin_page.py`. Every rule, panel and control is the same code as
  the Concord's.
- **The Hegemony's library**, `docs/mockups/lib/kethran.js`. It holds:
  - the palette as the page's colour roles, all 24 icons of the icon guide as solid wedges and chevrons, the type and the
    live readouts (moved out of the skins page);
  - the kit, loaded from the Blender render (`assets/ui/kethran/manifest.json`), and the pieces only the battle console
    draws, composed from that render: the square key is the helm pad's own rendered keycap in its socket, the wide key
    the rendered wedge key, each lit by state; allocation cells, a title placard and a bolted gunmetal backing plate
    behind the column (CLAUDE.md section 7: no black behind a metallic skin's panels);
  - placeholder names for ships, ranks and officers.

  The console skins page now takes its Kethran icons and live readouts from this library.
- **The battle console page's fonts** gain Saira Stencil One and Saira Condensed.

## Impact

- New files:
  - `docs/mockups/lib/kethran.js`
  - `docs/mockups/battle-console-kethran.html`
- Changed files:
  - `docs/mockups/battle-console.html` and the faction pages written from it (the Kethran kit is now inlined in it)
  - `docs/mockups/console-skins.html` (the Kethran icons and live readouts moved out; its icons are now the icon guide's
    subjects)
  - `tools/mockups/skin_page.py`
- No new Blender render: the console's extra pieces are composed from the committed render.
- This is a mockup only. The game's Godot console gets the skin when the owner approves it for implementation.
