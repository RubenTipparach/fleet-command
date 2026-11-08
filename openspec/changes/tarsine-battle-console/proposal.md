# Proposal: tarsine-battle-console

## Why

The Tarsine Remnant's skin is the owner's ancient race (2026-10-30: "a powerful ancient race. rusty, with back lit
buttons clogged with dirt. their ships havent been upgraded in ages, rough around the edges"), rebuilt in Blender on the
analog panel bible (2026-10-31: "I love the Remnant ui, i htink it could use some polish from our guide, but please dont
remove the grimey industrial feel, I love that story telling so much"). It lives on the console skins page (mockup 4) as
`docs/mockups/skins/tarsine.js`. The Krai (mockup 8), the Ssarith Hunt (mockup 15) and the Kelvane Institute (mockup 20)
already have a battle console, and the console follows the ship you command (owner, 2026-11-04: "combine uis together,
so we can hop between different ships"; `fleet-skins`). The Remnant needs its own battle console, and its look needs
one home both pages share (CLAUDE.md section 6.1).

## What Changes

- **The battle console in the Tarsine Remnant skin.** This is mockup 30, `docs/mockups/battle-console-tarsine.html`,
  written from the one source page by `tools/mockups/skin_page.py`. Every rule, panel and control is the same code as
  the Concord's.
- **The Remnant's library**, `docs/mockups/lib/tarsine.js`. It holds:
  - the materials (corroded iron, rust streaks, rivets), the palette as the page's colour roles, all 24 icons of the
    icon guide as worn carved glyphs, the type and the live readouts (moved out of `skins/tarsine.js`);
  - the kit, loaded from the Blender render (`assets/ui/tarsine/manifest.json`), and the pieces only the battle console
    draws, composed from that render: the square key is the helm pad's own arched tablet, the wide key the rendered
    tablet key, each with the same caked dirt in every state and only the backlight changing; allocation cells, a title
    lintel and a riveted iron plate behind the column (CLAUDE.md section 7: no black behind a metallic skin's panels);
    the name plate with its lamps turned low so the captain's name reads;
  - placeholder names for ships, ranks and officers.

  `docs/mockups/skins/tarsine.js` is now a thin wrapper that registers this library with mockup 4.
- **The battle console page's fonts** gain Cinzel and Alegreya Sans SC.

## Impact

- New files:
  - `docs/mockups/lib/tarsine.js`
  - `docs/mockups/battle-console-tarsine.html`
- Changed files:
  - `docs/mockups/battle-console.html` and the faction pages written from it (the Tarsine kit is now inlined in it)
  - `docs/mockups/skins/tarsine.js` and `docs/mockups/console-skins.html` (its icons are now the icon guide's subjects)
  - `tools/mockups/skin_page.py`
- No new Blender render: the console's extra pieces are composed from the committed render.
- This is a mockup only. The game's Godot console gets the skin when the owner approves it for implementation.
