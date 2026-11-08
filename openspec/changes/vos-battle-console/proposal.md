# Proposal: vos-battle-console

## Why

The Vos Syndicate's skin is the owner's first favourite (2026-10-30, of the reference games' skins: "I really like
espeically the mirak ui design, its so playful and pirate like", then "design a hand drawn aesthetic of the mirak ui").
It has lived only on the console skins page (mockup 4), drawn inside that page's own script. The Krai (mockup 8), the
Ssarith Hunt (mockup 15) and the Kelvane Institute (mockup 20) already have a battle console, and the console follows the
ship you command (owner, 2026-11-04: "combine uis together, so we can hop between different ships"; `fleet-skins`).
The Syndicate needs its own battle console, and its look needs one home both pages share (CLAUDE.md section 6.1).

## What Changes

- **The battle console in the Vos Syndicate skin.** This is mockup 28, `docs/mockups/battle-console-vos.html`, written
  from the one source page by `tools/mockups/skin_page.py`. Every rule, panel and control is the same code as the
  Concord's.
- **The Syndicate's library**, `docs/mockups/lib/vos.js`. It holds:
  - the hand-drawn helpers (ink, wash, hatch, dry, spiral, the painted body), moved out of the skins page;
  - the kit's painters, moved out of the skins page, and the pieces only the battle console draws: square tag keys and
    wide claw-tag keys in seven lights, allocation cells, the title ticket and patched sailcloth behind the column;
  - the palette as the page's colour roles, all 24 icons of the icon guide in the crew's brush, the type, the live
    readouts;
  - placeholder names for ships, ranks and officers.

  The console skins page now takes its Vos entry and its `FCK` drawing helpers from this library, so the look has one
  implementation.
- **The battle console page's fonts** gain Permanent Marker and Patrick Hand.

## Impact

- New files:
  - `docs/mockups/lib/vos.js`
  - `docs/mockups/battle-console-vos.html`
- Changed files:
  - `docs/mockups/battle-console.html` and the faction pages written from it
  - `docs/mockups/console-skins.html` (the Vos code moved out; its icons are now the icon guide's subjects)
  - `tools/mockups/skin_page.py`
- This is a mockup only. The game's Godot console gets the skin when the owner approves it for implementation.
