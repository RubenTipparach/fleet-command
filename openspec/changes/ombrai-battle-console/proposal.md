# Proposal: ombrai-battle-console

## Why

The owner, 2026-10-30: "an enigmatic alien race with cool dark purple colors and pink buttons, progress bars are
curcular, almost organic like eyes"; 2026-10-31: "we need to update krai, onbrai, and hunt to use the new guildine
standards ... Please make them look nice".

The Ombrai have a console skin on the console skins page (mockup 4): anodised plum metal rendered in Blender, wells lit
from below, every gauge an eye. They have no battle console. The Concord, the Krai, the Ssarith Hunt and the Kelvane
Institute have one, and the console follows the ship you command (`fleet-skins`).

## What Changes

- **The battle console in the Ombrai skin**: mockup 32, `docs/mockups/battle-console-ombrai.html`, written from the one
  source page by `tools/mockups/skin_page.py`. Every rule, panel and control is the same code as the Concord's.
- **The Ombrai's library**, `docs/mockups/lib/ombrai.js`: the palette as the page's colour roles, all 24 icons of the
  icon guide in the Ombrai's hand (eyes, moons, rings, petals, dots), the type, the live readouts (moved from the skins
  page's module, so the look has one implementation) and placeholder names. The skins page's Ombrai module now registers
  this library.
- **The console's own pieces, composed from the existing renders** with the Ombrai's light drawn into their wells: the
  square key is the dial's plum ring, the wide key the tab's capsule, the cells the inset's small plate, each lit from
  below in the state's colour; the title placard is the tab's capsule; the name plate is drawn shorter for the Fleet key;
  the panels', insets' and strips' wells are veiled from above for the console's readouts.
- **The column's backing, rendered in Blender**: one plum plate the height of the screen with a pink eye jewel wherever
  two panels part (CLAUDE.md section 7: no black behind a metal skin's panels). The skins page shows it behind its column.

## Impact

- New files: `docs/mockups/lib/ombrai.js`, `docs/mockups/battle-console-ombrai.html`,
  `assets/ui/ombrai/console.backing.png` (and its `.import`).
- Changed files: `docs/mockups/battle-console.html` and the faction pages written from it;
  `docs/mockups/skins/ombrai.js` and `docs/mockups/console-skins.html`; `tools/mockups/ombrai_shapes.py`,
  `assets/ui/ombrai/shapes.json`, `tools/blender/build_ui_ombrai.py`, `assets/ui/ombrai/manifest.json`;
  `tools/mockups/skin_page.py`.
- This is a mockup only. The game's Godot console gets the skin when the owner approves it for implementation.
