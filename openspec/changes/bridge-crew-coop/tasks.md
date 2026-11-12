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
- [x] 6.2 The Fleet station is the one fleet and crew manager: a Fleet | Crew toggle; Fleet: each ship's captain, its bridge crew, the admiral's directives; Crew: Bridge, Lock for and the roles grid (owner, 2026-11-11: "I would opt for crew manager to be in the fleet panel", "You can toggle between fleet and crew panels there"); `docs/screenshots/game/coop-fleet-crew.png`
- [x] 6.5 Roles are grants, not exclusive: several members may hold one role; a member sent alone to a ship holds every role there, one joining a crewed ship none until granted; the admiral grants on any ship's bridge (a tab per crewed bridge), a ship's captain on their own (owner, 2026-11-11: "Remember. Roles are not exclusive", "It's whatever the admiral sets up")
- [x] 6.6 Assignments and grants carry from mission to mission until the ship is sold or destroyed; a lost ship's crew goes back to the flagship (owner: "it persists from mission to mission until a ship is sold or destroyed", "any dangling crew members default to admiral ship"); the crew manager on the campaign map's crew sheet too (owner: "This can be set up in the campaign map before missions too at any point")
- [x] 6.7 The admiral steps onto a ship another player is on; the AI flies the flagship while no one stands on its bridge (owner: "Just have the admiral swap ships to a ship that another player is on")
- [ ] 6.8 Record the shared-ship game (`coop-host-shared.json`), and a three-machine game once the desktop is back online
- [x] 6.4 The admiral assigns crew to ships in the Crew view (a key per ship for each member: an AI ship makes them its captain, a crewed one puts them in its crew, the flagship brings them back); the Fleet view reads each captain out (owner, 2026-11-11: "I don't see how you are assigning crew members to a specific ship"); `docs/screenshots/game/coop-crew-assign.png`
- [x] 6.3 The admiral opens every role and steps onto any ship's bridge; one player alone on a ship holds every role there (owner: "The admiral has access to all roles and all ships at all times"); up to eight players (owner: "can be up to 8 players")
- [x] 6.1 Change bridge and Lock for over the Fleet and Tactical stations (owner, 2026-11-11: "There's the fleet manager and a targeting manager tap both should let you change bridge and change targets"): `docs/screenshots/game/coop-crew-strip.png`

## 7. Later
- [ ] 7.1 The captain's menu mockup in each skin (the Concord first), and the console's sync with it
- [ ] 7.2 A crew member joining mid-battle
- [ ] 7.3 Star Crew's matchmaker codes for play off the LAN
- [ ] 7.4 The owner's call on reactor fragility: a frigate's coolant loops go at about 78 % hull (`docs/playtests/2026-11-11-coop-stage2-3.md`)
- [x] 7.4b The comms box in every battle, alone or in co-op: every comms message there, the lone last line gone; no Chat key alone (owner, 2026-11-11: "All comms messages should be in the chat box", "Standardize this across all the game")
- [ ] 7.5 The battle console mockup (and every faction's console page) draws the comms box (`docs/design/mockup-sync.md`)
- [ ] 7.6 Dialogue option keys in the comms box for campaign missions (owner: "some campaign missions will have buttons for dialogue options")
