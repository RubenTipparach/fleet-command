# Proposal: krai-console-engine

## Why

The owner, 2026-11-04: "make scenarios for both sides (concord and Krai), and keep the UI updated witht he mockups". The
Krai Warhost's battle console is mockup 8 (`docs/mockups/battle-console-krai.html`): the battle console of mockup 2 in the
Krai skin, with its kit rendered in Blender (`assets/ui/krai/`, `tools/blender/build_ui_krai.py`). The game drew only
the Concord kit.

## What Changes

- **`KraiKit`**, a second `IKit`: the Blender-rendered pieces from `assets/ui/krai/manifest.json` nine-sliced at their
  recorded margins, the hero pieces whole, the Krai icons in the Krai's own shapes, its palette and the pieces the render
  lacks drawn as `docs/mockups/lib/krai.js` draws them. It passes `KitPieces.Verify`.
- **The skin's own parameters** (the mockup's skin API: content padding, the name plate's field, the helm ring's colours,
  centred key labels, the tab icons' colour, glyphs on dark keys, the strips' heights and rows, its typefaces Teko and
  Russo One) move from hard-coded Concord values into the kit, so every panel reads them; the Concord looks as before.
- **`Kits.For(skin)`** picks the kit; the console takes the skin of the side the scenario is played from
  (`battle-scenarios`), and swaps it when the player picks a scenario of the other side.
- Not yet: whatever the game's console does not have in either skin (the list in `docs/design/mockup-sync.md`).
