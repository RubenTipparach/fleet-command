# Tasks: tarsine-battle-console

- [x] 1.1 Proposal, design and spec delta
- [x] 1.2 `lib/tarsine.js` with every field the battle console reads; `skins/tarsine.js` thinned onto it
- [x] 1.3 The console's own pieces composed from the render and drawn in its materials: `key.*`, `keyw.*`, `cell.*`,
  `bar.cell.ok`, `bar.fill.accord`, `plate.title`, `console.backing`, the dimmed `nameplate`
- [x] 1.4 The 24 icons of the icon guide as worn carved glyphs, shared with mockup 4
- [x] 1.5 The page written by `skin_page.py`; every press matches the Concord's (parity); DOM fit and mockup 4's console fit pass
- [x] 1.6 Shots looked at: battle, expanded panels, hover card, menu, at desktop, phone and portrait
- [ ] 1.7 The owner's review; T1-T4
- [ ] 1.8 A Remnant ship in the allied and swap scenes, so the console can hop to the Remnant from another skin's page

## 1b. Version 2: worked iron, nothing stretched (owner, 2026-11-08; design D7)

- [x] 1b.1 Measure the smear on the version 1 page: which pieces, rendered at what size, drawn at what size
- [x] 1b.2 `worked()` iron in `build_ui_tarsine.py` (grain, hammer facets, worn edges, rust and soot in the crevices) on
  every plate; a frame's well floored with darker iron (`floor`)
- [x] 1b.3 `console.column` (the three docked panels as framed wells in one plate), `console.backing`, `plate.title` and
  `cell.*` rendered in Blender and fit-checked in `tarsine_shapes.py`; the canvas backing, lintel and cells removed from
  `lib/tarsine.js`
- [x] 1b.4 `frame.panel` rendered at the expanded panel's 1268 x 1040 and the strips at 1440 px; every other nine-sliced
  piece's texture folded in its stretched bands (`nine_co()`); the inset's dirt kept to its rim, its lamp removed
- [x] 1b.5 Shots before and after (battle, weapons and power expanded); parity, DOM fit and console fit
- [ ] 1b.6 The parent session re-inlines `battle-console.html`, regenerates `battle-console-tarsine.html` and
  `console-skins.html`, and republishes mockups 4 and 30
- [ ] 1b.7 The owner's review of version 2

## 2. In the game

- [ ] 2.1 A port of `lib/tarsine.js` to the game's kit (`src/Game/Ui/Kit/`), once the owner approves the mockup for
  implementation
