# Proposal: krai-battle-console

## Why

Mockup 6 in `mockup-work.md`: the battle console (mockup 2) played in the Krai Warhost skin (mockup 4). The owner,
2026-11-01, starting the battle console: "the first design system we will implement will be the concord, we will then
reuse this design once it has been fully approved across all other factions, and each faction's implementation will be
manually verified such that all of it is functional and readable." Then, 2026-11-03: "Krai Warhost - start work on
mocking up gameplay with this UI now", and "verify all functionality from concord works here".

On the first pass (2026-11-03): "Labels on the krai UI need better integration. They should have a metallic background.
The UI panels should be somewhat connected like they are part of the same console", then "Cool but you didn't render
the stitching elements in blender like you are supposed to" (CLAUDE.md section 7: everything with material and depth is
modelled and rendered in Blender).

## What Changes

- **One battle console, many skins.** `docs/mockups/battle-console.html` stays the one source of every rule, panel and
  control. It reads its skin from `<meta name="fc-skin">` (Concord when absent) out of `window.FCSkins`, and every
  colour, ink, type face, drawn key shape and optional kit piece goes through the skin. The Concord renders exactly as
  before (measured on all 40 named shots: pixel-identical apart from the version label and the parts that animate with the clock, the reactor's pulse, the mine rings and the seeker-lock blink).
- **`tools/mockups/skin_page.py`** writes a faction's page (`docs/mockups/battle-console-krai.html`) from the source,
  setting the skin, the title and the fonts and inlining the faction's library and kit. The written page is a build
  product with `--check`.
- **`docs/mockups/lib/krai.js`** (`window.FCKrai`): the one Krai implementation, used by the battle console and, through
  `docs/mockups/skins/krai.js`, by the console skins page. Its palette as the console's colour roles, its type (Teko
  sized to Antonio's width, Russo One for headings), its keys' ink, blade-cut shapes for drawn keys, hexagonal knobs, the
  player's side named as the Warhost, and the full icon subject list of the icon guide in the Krai's language.
- **New Krai kit pieces rendered in Blender** (`tools/mockups/krai_shapes.py`, `tools/blender/build_ui_krai.py`):
  the label and title placards, the column's cast backplate with its spine of red light, clamp bridges between stacked
  panels, gussets bracing the column to the strips, and the allocation cells (idle, lit, warning, ok, locked) with the
  green bar fill. Every shaped piece passes the fit rules and rule B on its render.
- **`tools/mockups/skin_parity.mjs`**: the functional check. Every control of every named shot is pressed, from a fresh
  replay of the shot, in the Concord and in the faction's page, and the console and battle state a second later must
  match exactly.
- **Bug fixes to the Concord page found on the way**: the Fleet key threw when no ship of the player's side was left
  (after a warp jump); named shots carried an explosion, the scene list, the log or a hover from the shot before.

## Capabilities

- `krai-battle-console` (new): the battle console in the Krai Warhost skin, and the rule that a faction's console is the
  Concord's code in another skin.

## Impact

- Pages: `docs/mockups/battle-console.html` (v66), `docs/mockups/battle-console-krai.html` (new, written),
  `docs/mockups/console-skins.html` (re-inlined; its Krai icons are now the full icon-guide set).
- Libraries: `docs/mockups/lib/concord.js` (skin fields), `docs/mockups/lib/krai.js` (new), `docs/mockups/skins/krai.js`
  (now a thin registration).
- Assets: `assets/ui/krai/` gains 11 rendered pieces; `shapes.json` gains 5 shaped pieces.
- Tools: `tools/mockups/skin_page.py`, `tools/mockups/skin_parity.mjs` (new).
- No game code (CLAUDE.md section 4).
