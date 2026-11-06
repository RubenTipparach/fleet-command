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
  - [x] 2.1a Tactical (aim keys, contacts, pins; expanded: target weapons as aims, the weapons-against-contacts grid),
    Point defence (modes, mounts, intercepts) and Fleet (wingman standing orders, Take command); the fleet orders' keys
    belong to Comms and call `Battle.FleetOrder` (weapon-targeting package, 2026-11-06)
  - [x] 2.1.1 Comms (Battle.Comms, StationPanel.Comms): hail the locked ship (most enemies do not answer; a friendly says what it needs), fleet orders (All attack, Form on me, All hold; Hunt waits for the Fleet station's stance), the Event log key and the latest lines
  - [x] 2.1.2 Transport (Battle.Transport, StationPanel.Transport, data/battle/systems/comms.json): the pads, the 1.5 km reach through a downed facing, the boarding queue, Capture (volleys, medics, hackers, the envoy, the deck held, the capture changing sides, pilot needed), Hit and run (a subsystem or one weapon, shoot or bomb), troops to a friendly ship, Cargo (supplies, goods and the envoy to a friendly ship, station or planet), the expanded party panel; the test scenes Boarding, Relief, VIP, Distress and Assault from both sides (data/battle/scenes/comms.json) with scripted runs
  - [ ] 2.1.3 The Transport station's Mines and Security tabs (hooks in StationPanel.Transport, for the mines and boarding defence systems)
- [ ] 2.2 Ship pages: sensors, tractor (the contest), craft; the Target box with the target's systems display
  - [x] 2.2a Tractor (the contest of rounds, the tow, reel, asteroids) and Craft (fighters on Attack, Defend and Defences,
    the disguised shuttle, the missile barrage), mines (dropped astern, placed by transporter with the bullseye over the
    view; their keys on the Craft page until the Transport station's Mines tab is built), the beam, craft, mines and rocks
    in 3D, and the mockup's Tractor, Mines, Shuttle and Fighters scenes from both sides with their playtests
  - [ ] 2.2b Mines spend a transporter pad and move to the Transport station's Mines tab (with the transport package)
  - [x] 2.1a Science (scan, active or passive, ECM and ECCM, cloak and sweep), with cloaking and nebulae in the battle (changes `cloaking`, `weapon-targeting`)
- [ ] 2.2 Ship pages: sensors, tractor (the contest), craft; the Target box with the target's systems display
  - [x] 2.2a Sensors (health, mode, ECM and ECCM, contacts, the target's ECM), and Security on its own entry with the deck plan in 3D (change `boarding-defence`)
- [ ] 2.3 Power: Reactor and Warp modes, the warp key on the speed slider
- [x] 2.3 Power: Reactor and Warp modes, the warp key on the speed slider
- [ ] 2.5 The tactical map, the event log, the 3D shield hexagon, the transporter effect
  - [x] 2.5a The tactical map (the radar row expanded: Map key and dial, 10/20/40 km) and the event log sheet with its X;
    its key is the Comms station's (`ConsoleState.OpenLog`) (weapon-targeting package, 2026-11-06)
  - [x] 2.5.1 The event log over the view (from Comms, its X), the transporter effect (columns, rings, flashes, the stream) and the dashed transport range ring
- [ ] 2.6 The scripted captain and a recorded desktop playtest on the owner's PC (CLAUDE.md section 9)
- [ ] 2.7 Phone layouts, once the mockup has them
