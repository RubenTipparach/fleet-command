# Design: krai-console-engine

The Krai Warhost's battle console (mockup 8, `docs/mockups/battle-console-krai.html`) in the game, on the console the
game already has (`battle-console-engine`). The mockup is mockup 2's page in another kit
(`tools/mockups/skin_page.py`), so the game does the same: one console, two kits, no panel duplicated (CLAUDE.md
section 6.1).

## 1. The kit

- **`KraiKit : IKit`** (`src/Game/Ui/Kit/KraiKit.cs`): the palette, inks, type sizes, blade-cut keys, hexagon knobs and the
  24 icons ported from `docs/mockups/lib/krai.js`; the icons' cut-outs by polygon clipping, worked out once at start.
- **`RenderedPieces`**: any rendered skin's `assets/ui/<skin>/manifest.json`, each piece nine-sliced at its recorded
  margins and stretch axes, or drawn whole (a hero), as the mockup's `kitdraw.js` does. The next Blender-rendered skin
  reuses it.
- **`SkinStyle`**: every per-skin value the panels used to hard-code (the mockup's skin API: padding, the name plate's
  field, the helm ring, key ink and centring, tab icon colour, glyphs on dark keys, the strips' heights and content rows,
  typefaces), and flags for the Krai's optional parts (the column plate, label and title placards, symbol bars, joined
  strips, rendered glass keys). `KitPieces.Verify` fails a skin that turns a part on without its pieces.
- **`Kits.For(id)`** is the one place a skin id becomes a kit; the console takes the skin of the scenario's side
  (`battle-scenarios`), and `--skin=id` overrides it for the checks.
- **Type**: Teko and Russo One (OFL, `assets/fonts/`, sources and SHA-256 in `assets/fonts/SOURCES.md`).
- **The column**: `ColumnBacking` draws the skin's cast plate (`console.column`) behind the docked panels, so no black
  seam shows between them; the Concord's stays black.
- **The strips**: the Krai's are 60 and 80 px (krai.js `stripTop`, `stripBottom`); `ConsoleLayout.Compute` takes a skin's
  strip heights and the view gives way, as the mockup's `rects()` does.

## 2. Parity (owner, 2026-11-04: "we need to make sure all UI elements are at parrity")

`-- --parity-test` (run by `scripts/run-tests.sh`), after `tools/mockups/skin_parity.mjs` for the pages: one battle played
the same in each skin, through 20 console states (every station, every ship page, each panel expanded, the menu, the
scene list). In each state both skins must register the same controls, and every control must lie inside the panel that
draws it and inside the window. First run: the control sets were identical; every Expand key's reach poked 3.5 px above
its clipped panel, in both skins (fixed: the reach is clipped to the panel). Now 2,576 controls, all in place.

## 3. What still differs from mockup 8

In `docs/design/mockup-sync.md`, with the tasks below.

## Open questions

| # | Question | Provisional answer |
|---|---|---|
| 1 | Should the column's channel of red light bloom in the game as in the mockup's experiment (v8)? | Yes, with the game's glow once the 3D view's post effects are set |
