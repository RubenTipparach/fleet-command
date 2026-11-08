# Proposal: axiom-battle-console

## Why

The owner, 2026-11-07: "I have a couple more faction ideas. Id like a faction that is purely mathematical. A faction based
on fractal patterns and designs."

The Axiom has a console skin on the console skins page (mockup 4, page version 29): Koch keys, Sierpinski corners, Cantor
rules and an escape-time screen, drawn flat by the rules themselves (`console-skins/design.md`, "Three more factions").
It has no battle console yet. The Concord (mockup 2), the Krai (mockup 8), the Ssarith Hunt (mockup 15) and the Kelvane
Institute (mockup 20) have one, and the console follows the ship you command (`fleet-skins`).

## What Changes

- **The battle console in the Axiom skin.** This is mockup 25, `docs/mockups/battle-console-axiom.html`, written from the
  one source page by `tools/mockups/skin_page.py`. Every rule, panel and control is the same code as the Concord's.
- **The Axiom's library**, `docs/mockups/lib/axiom.js`. It holds:
  - the rules (Koch edges, Sierpinski triangles and carpets, Hilbert and dragon curves, Cantor sets, the Julia field);
  - mockup 4's kit painters and hero pieces, unchanged;
  - all 24 icons of the icon guide in the branchers' hand (the ten mockup 4 had, and fourteen new);
  - the live readouts;
  - the battle console's fields: colour roles, type, drawn key shape, cells, symbol bars, strips, name plate and
    placeholder names for ships, officers and ranks.

  The skins page's Axiom module now registers this library, so the look has one implementation; mockup 4 renders
  pixel for pixel as before.
- **Shared helpers.** `arcPts`, `rrPts` and `trace` move into `docs/mockups/lib/kitdraw.js`, so the skins page and the
  three new libraries use one copy.

## Impact

- New files: `docs/mockups/lib/axiom.js`, `docs/mockups/battle-console-axiom.html`.
- Changed files: `docs/mockups/battle-console.html` and every faction page written from it, `docs/mockups/console-skins.html`,
  `docs/mockups/skins/axiom.js`, `docs/mockups/lib/kitdraw.js`, `tools/mockups/skin_page.py`.
- This is a mockup only. The game's Godot console gets the skin when the owner approves it for implementation.
