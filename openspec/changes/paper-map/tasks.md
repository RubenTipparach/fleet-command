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
  make it"): `docs/mockups/space-chart.html` (mockup 24), written from mockup 1 by `tools/mockups/chart_page.py` and opening straight
  onto the map through mockup 1's `startAt()`
- [x] 1.9 The chart on phones (owner, 2026-11-07: "Space chart not working on mobile"): the nebula's noise from a seeded lattice
  texture so it compiles on WebGL 1; the sheet at 32 px a unit on a touch screen; `softDraw()` for browsers with no canvas filter
- [x] 1.10 Panels on a phone (owner, 2026-11-07: "Panels on my phone stuck open, I can't see"): the sector panel's Hide key, a
  Sector map key to bring it back, the portrait top bar in two rows (mockup 1 v25, mockup 24 v3; the game's sync task `game-flow-engine` 2.11)

## 2. After the owner's notes

- [ ] 2.1 Fold in the owner's notes
- [ ] 2.2 The game's map in the paper look (`game-flow-engine` 2.10), when the owner asks for it
