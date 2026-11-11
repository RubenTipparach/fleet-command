# Tasks: bridge-crew-coop

## 1. The battle shared
- [x] 1.1 `Battle.Checksum`, and the determinism probe (`--checksum`, `Coop.Determinism`)
- [x] 1.2 Every player order relays (`Sent`, `Battle.RelayedOrders`), applied by name (`ApplyCall`) and as a sender (`ApplyAs`)
- [x] 1.3 The probe on the Mac and the Windows laptop, lines compared (byte-identical once `Sim.DMath` took every transcendental call)
- [x] 1.4 Lockstep: ticks, the host's refusals, checksums every second, the crew's playback (`Coop.Lockstep`, `CoopTests`)

## 2. Roles and targets
- [x] 2.1 `data/coop/roles.json` and `Coop.Roles` (validated against the relayed orders)
- [x] 2.2 `Coop.Crew`: the admiral, ship captains, roles given and taken back
- [x] 2.3 A target per targeting role on a crewed ship (`Ship.RoleTargets`, `TargetOf(s, role)`), every system reading its own
- [x] 2.4 Ships given to members (`Battle.Helmed`): left by the AI, steered and fired by their own orders

## 3. The console
- [x] 3.1 Disabled tabs for roles not held; a station shut opens the first held
- [x] 3.2 The station icons over each ship a station has locked
- [x] 3.3 The captain's menu (`CrewCard`): roles grid, ships, the sync count; the Crew key on the plate and in the menu
- [x] 3.4 A crew member's waiting screen between battles

## 4. The connection and the flow
- [x] 4.1 `CoopNet` over ENet: host, join, hello, welcome, roster, orders, ticks
- [x] 4.2 Battles started on the crew's games (skirmish by id, mission from the save and its id), stopped, paused
- [x] 4.3 A co-op skirmish played on one machine (two games, localhost), and on the Mac and the laptop (stage 1)
- [x] 4.4 A co-op campaign of 10 missions, the Mac hosting and the laptop crewing, recorded (stage 2; a ship given in stage 3)

## 5. The bot
- [x] 5.1 Beats: wait_crew, crew_role, crew_ship, dock (repair paid for, a ship bought), crew_session
- [x] 5.2 `data/playtests/coop-campaign.json`, the admiral's 10 missions

## 6. The crew layer (owner, 2026-11-11: "someway for players to see and interact with crew members in game")
- [x] 6.0 Player names in the log, chat, pings, the admiral's directives with Aye, crew notes, the fleet manifest
- [x] 6.1 Change bridge and Lock for over the Fleet and Tactical stations (owner, 2026-11-11: "There's the fleet manager and a targeting manager tap both should let you change bridge and change targets"): `docs/screenshots/game/coop-crew-strip.png`

## 7. Later
- [ ] 7.1 The captain's menu mockup in each skin (the Concord first), and the console's sync with it
- [ ] 7.2 A crew member joining mid-battle
- [ ] 7.3 Star Crew's matchmaker codes for play off the LAN
- [ ] 7.4 The owner's call on reactor fragility: a frigate's coolant loops go at about 78 % hull (`docs/playtests/2026-11-11-coop-stage2-3.md`)
