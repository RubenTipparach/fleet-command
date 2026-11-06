# Tasks: battle-console-engine

## 1. First slice (this change, 2026-11-03)

- [x] 1.1 C# toolchain: Godot 4.7.1 .NET and the .NET 8 SDK pinned and installed by scripts; CLAUDE.md section 2
- [x] 1.2 Data in `data/battle/` and `data/console/`, validated on load (unknown key, missing field, non-finite, bad reference)
- [x] 1.3 `src/Core`: the stand-in battle, rules, orders and layout model, with headless tests and a golden seed
- [x] 1.4 The Concord kit in vector, verified against the piece list at startup
- [x] 1.5 Panels: name plate, stations (Helm, Power), ship (systems display, shields), weapons, radar, both strips, menu, hover cards
- [x] 1.6 The 3D view: stand-in ships, the ship held 74 % down the view, orbit and zoom, helm by pointing and the helm ring, locking
- [x] 1.7 Every panel expands with the battle in the right third; Esc or a click on the battle returns
- [x] 1.8 Keys through the InputMap (A D Q E turn, W S speed, X stop, F fire, Tab next target, M multiple, Space or P pause, Esc)
- [x] 1.9 Layout test in the engine at 1920, 2560 and 1500 wide, collapsed and expanded; stills in `docs/screenshots/game/`
- [x] 1.10 Side by side with the mockup (owner, 2026-11-03: "Be sure to conpare mockup side by side"): `tools/compare/side_by_side.py`, sheets and the remaining differences in `docs/screenshots/compare/`
- [x] 1.11 From the comparison: the Helm page as the mockup has it (orders toward the target, hold and run sliders, evasive, the 3 x 3 high energy turns, emergency stop), the expanded weapons panel (groups, the systems display, the picked mount's details), the radar row, the view's readouts, the 3D shield hexagon, the Target box, the menu sheet and the camera

## 2. Next slices (each its own request)

- [ ] 2.1 The other stations: tactical, point defence, science, comms, repair, transport, fleet (wingman behaviours)
- [ ] 2.2 Ship pages: sensors, tractor (the contest), craft; the Target box with the target's systems display
- [x] 2.3 Power: Reactor and Warp modes, the warp key on the speed slider
- [ ] 2.5 The tactical map, the event log, the 3D shield hexagon, the transporter effect
- [ ] 2.6 The scripted captain and a recorded desktop playtest on the owner's PC (CLAUDE.md section 9)
- [ ] 2.7 Phone layouts, once the mockup has them
