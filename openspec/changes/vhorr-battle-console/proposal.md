# Proposal: vhorr-battle-console

## Why

The owner, 2026-10-30: "an organic alien race, lots of leather shining through light, slimy carapace, tendrils"; then
2026-10-31: "due to the nature of this ui, nine slice is not possible and the UI will have to be bocked out using
placeholders then a full detail agent will go in and sculpt components manually", and "If you structured your sculpting
tools well. Then we should be able to sculpt panels, buttons and elements of any size".

The Vhorr Brood has a console skin on the console skins page (mockup 4), sculpted slot by slot at that page's sizes on
the Organic Panel Bible. It has no battle console. The battle console's panels are not the skins page's slots, so the
Brood's kit has to be sculpted again at the battle console's own sizes.

## What Changes

- **The battle console in the Vhorr Brood skin**: mockup 33, `docs/mockups/battle-console-vhorr.html`, written from the
  one source page by `tools/mockups/skin_page.py`. Every rule, panel and control is the same code as the Concord's.
- **A blockout of the battle console's own slots**, `assets/ui/vhorr/organic/battle/blockout.json`, written by
  `tools/mockups/vhorr_battle_blockout.py` from the console's rectangles, with its styles
  (`assets/ui/vhorr/organic/battle/styles.json`, the owner's survey W picks carried over), sculpted and rendered by the
  existing parametric sculptors (`tools/blender/build_ui_vhorr_organic.py`, run by `scripts/render-vhorr-battle.sh`):
  - **the column** as one grown body: the tissue backing, the name plate, the station, ship and weapons panels each round
    one black screen, the radar's pocket and the crest, at their exact sizes;
  - **the strips**, each a spine round one glass channel, at 1440 x 72;
  - **the expanded panel's frame**, at 1268 x 1040;
  - **a tray of the controls** the console draws at many sizes: a key in three states, a square key, a tab, an inset, a
    divider cord and the alert lamp.
- **The Brood's library**, `docs/mockups/lib/vhorr.js`: the palette as colour roles, all 24 icons in the brood's hand,
  the type, the live arrow and target bracket, the kit composed from the renders, placeholder names. The skins page's
  module takes its palette, icons, arrow, bracket and tendril from it.
- **Two small fixes in the build script** so a layout with no name plate or no strips (the expanded frame, the tray)
  builds: no portrait copied where there is no portrait window, no column backing where there is no column.

## Impact

- New files: `docs/mockups/lib/vhorr.js`, `docs/mockups/battle-console-vhorr.html`, `tools/mockups/vhorr_battle_blockout.py`,
  `scripts/render-vhorr-battle.sh`, `assets/ui/vhorr/organic/battle/` (blockout, styles, three renders with manifests).
- Changed files: `docs/mockups/battle-console.html` and the pages written from it, `docs/mockups/skins/vhorr.js`,
  `docs/mockups/console-skins.html`, `tools/blender/build_ui_vhorr_organic.py`, `tools/mockups/skin_page.py`.
- This is a mockup only.
