# Proposal: exponent-battle-console

## Why

The owner, 2026-11-07: "Exponent is not a mix of two races. It is its own thing. Inspired by Borg. It's ship ui is a
metalic utilitarian look. With veins a and tentacles and barnacles growing. Like am abandoned rusted ship at sea. Take
over by reefs and coral".

The Exponent has a console skin on the console skins page (mockup 4, page version 30): painted steel overgrown like a
wreck on a reef, hatch keys, barnacle bars (`console-skins/design.md`, "The Exponent its own collective"). It has no
battle console yet. The Concord, Krai, Ssarith Hunt and Kelvane Institute have one, and the console follows the ship you
command (`fleet-skins`). We take the idea of an assimilating collective, never its names, symbols or look (CLAUDE.md
section 1: take the shape, not the text).

## What Changes

- **The battle console in the Exponent skin.** This is mockup 26, `docs/mockups/battle-console-exponent.html`, written
  from the one source page by `tools/mockups/skin_page.py`. Every rule, panel and control is the same code as the
  Concord's.
- **The Exponent's library**, `docs/mockups/lib/exponent.js`. It holds:
  - the materials (painted steel with rust, salt and verdigris) and the reef (barnacles, coral, sponge, anemones, tube
    worms, tendrils, veins);
  - mockup 4's kit painters, hero pieces, column and overgrowth, unchanged;
  - all 24 icons of the icon guide as stencil marks (the ten mockup 4 had, and fourteen new);
  - the live readouts;
  - the battle console's fields and its own pieces: window keys in seven lights, wide window keys, allocation ports, a
    stencil title plate, a name plate with room for the Fleet key, a narrower panel frame and the column's backing.

  The skins page's Exponent module now registers this library; mockup 4 renders pixel for pixel as before.

## Impact

- New files: `docs/mockups/lib/exponent.js`, `docs/mockups/battle-console-exponent.html`.
- Changed files: `docs/mockups/battle-console.html` and every faction page written from it, `docs/mockups/console-skins.html`,
  `docs/mockups/skins/exponent.js`, `tools/mockups/skin_page.py`.
- This is a mockup only. The game's Godot console gets the skin when the owner approves it for implementation.
