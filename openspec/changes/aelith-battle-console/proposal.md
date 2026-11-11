# Proposal: aelith-battle-console

## Why

The owner, 2026-10-30: "an wise and advanced alien race. holographic buttons, with organic shaped metal armor peices
surrounding the edges"; "the conclave is one exception. They have a hybrid of both vector graphics for holographic
buttons and icons, but 3d rendered shell around the UI panels and elements".

The Aelith Conclave has a console skin on the console skins page (mockup 4): satin gold and pearl shells rendered in
Blender, holograms drawn as vectors into their wells. It has no battle console. The Concord (mockup 2), the Krai
(mockup 8), the Ssarith Hunt (mockup 15) and the Kelvane Institute (mockup 20) have one, and the console follows the
ship you command (`fleet-skins`).

## What Changes

- **The battle console in the Aelith Conclave skin**: mockup 31, `docs/mockups/battle-console-aelith.html`, written from
  the one source page by `tools/mockups/skin_page.py`. Every rule, panel and control is the same code as the Concord's.
- **The Conclave's library**, `docs/mockups/lib/aelith.js`. It holds:
  - the palette, as the page's colour roles;
  - the holograms and their layering over each rendered shell (moved from the skins page's module, so the look has one
    implementation);
  - all 24 icons of the icon guide, in the Conclave's sacred geometry;
  - the type, the live readouts and placeholder names for ships, officers and ranks.

  The console skins page's Aelith module now registers this library.
- **The console's own pieces, composed from the existing renders and holograms** (no new shell but one): the square key
  is the dial's gold ring round a disc of light; the wide key is the key's gold claws round a lozenge of light; the
  allocation cells are lozenges of light; the title placard is the tab's capsule; the name plate is drawn shorter for the
  Fleet key; the panels', insets' and strips' wells are veiled for the console's readouts.
- **The column's backing, rendered in Blender**: one gold shell the height of the screen, with a pearl wherever two
  panels part (CLAUDE.md section 7: no black behind a metal skin's panels). The skins page shows it behind its column too.

## Impact

- New files: `docs/mockups/lib/aelith.js`, `docs/mockups/battle-console-aelith.html`,
  `assets/ui/aelith/console.backing.png` (and its `.import`).
- Changed files: `docs/mockups/battle-console.html` (the library, its manifest and shapes inlined, the fonts, the label)
  and the faction pages written from it; `docs/mockups/skins/aelith.js` and `docs/mockups/console-skins.html`;
  `tools/mockups/aelith_shapes.py`, `assets/ui/aelith/shapes.json`, `tools/blender/build_ui_aelith.py`,
  `assets/ui/aelith/manifest.json`; `tools/mockups/skin_page.py`.
- This is a mockup only. The game's Godot console gets the skin when the owner approves it for implementation.
