# Tasks: paper-map

## 1. Mockup (now)

- [x] 1.1 Proposal, design and spec delta
- [x] 1.2 Material Maker graphs `map_paper`, `carpet_red`, `wall_green_rustic` (the rose wallpaper replaced), rendered headless; contact sheet looked at
- [x] 1.3 One walnut table builder for the faction screen and the map room (`walnutTable`)
- [x] 1.4 The chart (`paperSheet`), the sheet (`paperMesh`) and the room (`buildRoom`) in mockup 1 v21; `?look=space` keeps the glass board
- [x] 1.5 Shots `map`, `map_room`, `map_edge`, `map_close`, `map_krai`, `map_route_multi`, `map_space` at desktop and phone sizes, looked at
- [x] 1.6 Published; tracker, `mockup-work.md` and the sync record updated

- [x] 1.7 The owner's notes of 2026-11-07: the print a star map with a nebula background; rustic dark green walls with
  walnut trim and sconces; the room and the print darker; main merged and the fog of war drawn on the chart (`campaign-space` 2.2) (mockup 1 v22)
- [x] 1.8 The chart as its own artifact (owner, 2026-11-07: "Our space chart mockup is not available to me as an artifact, please
  make it"): `docs/mockups/space-chart.html` (mockup 34, first numbered 24), written from mockup 1 by `tools/mockups/chart_page.py` and opening straight
  onto the map through mockup 1's `startAt()`
- [x] 1.9 The chart on phones (owner, 2026-11-07: "Space chart not working on mobile"): the nebula's noise from a seeded lattice
  texture so it compiles on WebGL 1; the sheet at 32 px a unit on a touch screen; `softDraw()` for browsers with no canvas filter
- [x] 1.10 Panels on a phone (owner, 2026-11-07: "Panels on my phone stuck open, I can't see"): the sector panel's Hide key, a
  Sector map key to bring it back, the portrait top bar in two rows (mockup 1 v25, mockup 34 v3; the game's sync task `game-flow-engine` 2.11)
- [x] 1.11 The drawn pieces (owner, 2026-11-08: "hand drawn billboard puffs of cloud", "very pirate and very space"):
  `docs/mockups/lib/drawn.js` (ink, wash, hatching; clouds, storms, planets, suns, rocks, the compass rose), billboards on the
  chart, hand-lettered names, rhumb lines (mockup 1 v26, mockup 34 v4)
- [x] 1.12 The planets drawn live by our planet shader (`docs/mockups/lib/planet-shader.js`, after Francisco Cidade's CC0 Pixel
  Planet: six kinds, spinning, clouds, caps, rings, finer pixels, cross-hatched night sides), and the clouds, rocks and sun
  cross-hatched in three zones (owner, 2026-11-08: "More cross hatch", "less pixelated") (mockup 1 v27, mockup 34 v5)
- [x] 1.13 Planets half size and whole, clouds smaller with hatched shadows over their sectors (owner, 2026-11-08: "planet rings
  and the bottom of the planet are getting clipped. they should be 1/2 size", "can the clouds be smaller? and cast a shadow") (mockup 1
  v28, mockup 34 v6)
- [x] 1.14 Twice the cross-hatch density, the printed nebulae and belts removed (their billboards stand instead), planets on
  hexagon flight stands (owner, 2026-11-08: "2x cross hatch density", "nebula and asteroid drawings dont need to appear as part of
  map texture", "give planets those hexagon standees as well") (mockup 1 v29, mockup 34 v7)

- [x] 1.15 The lamp dimmer and the room darker, so the chart reads more dramatic (owner, 2026-11-08: "tone down lighting to
  make it a bit more dramatic"): hemisphere 0.12, sun 0.08, the spot 1.25 with a 0.55 penumbra, sconces 0.45 (mockup 1 v30,
  mockup 34 v8)

## 2. After the owner's notes

- [x] 2.1 Fold in the owner's notes
- [x] 2.2 The game's map in the paper look (`game-flow-engine` 2.10) (owner, 2026-11-08: "implement space chart in game, it looks
  great!"): `tools/mockups/export_chart.mjs` exports the page's still sheet layers, the room and the sheet as glbs with their
  Material Maker graphs named, the bank of drawn pieces and a manifest into `assets/scenes/chart/`; `ChartRoom.cs` builds the
  room, the lamps, the sheet (`chart_sheet.gdshader`: the holders' outlines and the fog drawn live on it) and stands the drawn
  pieces on the game's own sectors by the page's rules; `planet.gdshader` is the planet shader's port; the names hand-lettered in
  IM Fell English SC (OFL). `ChartTests` checks the bank against the game's palette
- [ ] 2.3 Phones: the sheet's base layer at 2048 px and the table's shadow baked (the game loads the 2,800 px sheet everywhere today)
