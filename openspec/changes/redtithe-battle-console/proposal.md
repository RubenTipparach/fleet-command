# Proposal: redtithe-battle-console

## Why

The owner, 2026-11-07: "Id also like another pirate faction. One more brutal. Decorate their UI with sharp teeth, bullets
and blades. Blood stained buttons and crusty paper with burn marks and drops of blood and grime on them", and, on page
version 29, "More brown wood on the red tithe".

The Red Tithe has a console skin on the console skins page (mockup 4, page version 30): bounty sheets nailed to scarred
oak under strap iron, trap-jaw keys, a bandolier for every bar (`console-skins/design.md`). It has no battle console yet.
The Concord, Krai, Ssarith Hunt and Kelvane Institute have one, and the console follows the ship you command
(`fleet-skins`).

## What Changes

- **The battle console in the Red Tithe skin.** This is mockup 27, `docs/mockups/battle-console-redtithe.html`, written
  from the one source page by `tools/mockups/skin_page.py`. Every rule, panel and control is the same code as the
  Concord's.
- **The Tithe's library**, `docs/mockups/lib/redtithe.js`. It holds:
  - the materials (crusted paper, gun steel, the hall's oak) and the trophies (knives, a hatchet, teeth, cartridges);
  - mockup 4's kit painters, hero pieces and column, unchanged;
  - all 24 icons of the icon guide, cut with a blade (the ten mockup 4 had, and fourteen new), cut into paper in black or
    into the dark boards in a light ink;
  - the live readouts;
  - the battle console's fields and its own pieces: panels, insets and strips of the hall's boards tarred black for the
    readouts, slot keys in seven states, allocation notches, a charred title slat, a name plate with room for the Fleet
    key and the hall's plank wall behind the column.

  The skins page's Red Tithe module now registers this library; mockup 4 renders pixel for pixel as before.

## Impact

- New files: `docs/mockups/lib/redtithe.js`, `docs/mockups/battle-console-redtithe.html`.
- Changed files: `docs/mockups/battle-console.html` and every faction page written from it, `docs/mockups/console-skins.html`,
  `docs/mockups/skins/redtithe.js`, `tools/mockups/skin_page.py`.
- This is a mockup only. The game's Godot console gets the skin when the owner approves it for implementation.
