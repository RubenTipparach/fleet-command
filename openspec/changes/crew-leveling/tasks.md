# Tasks: crew-leveling

## 1. Mockup (2026-11-12)

- [x] 1.1 `data/campaign/crew.json`: ranks, experience, prestige rates, pool, trees, auto paths
- [x] 1.2 Mockup 39, `docs/mockups/crew-leveling.html`: the crew screen, playable (battles, turns, promote, transfer, hire, auto level, pick, retrain), desktop and phone
- [x] 1.3 Survey section CL (CL1-CL8)
- [x] 1.3a Version 2 (2026-11-12): ten levels, and a pyramid of fifteen skills a station with crossovers and two-prerequisite skills
- [x] 1.3b Version 3 (2026-11-12): one skill tree a station (fourteen skills, crossovers, a keystone and tips with two prerequisites), drawn icons (`docs/mockups/lib/skill-icons.js`), tooltips, four styles
- [x] 1.3c Version 4 (2026-11-13): Branches grows down from the roots, its lanes closer; Constellation and Radial dropped
- [x] 1.3d Version 5 (2026-11-13): Branches only (CL10), taller where it fans out; the stage in `crew.json` `layout`, read by the page and the game
- [x] 1.4 The owner's review: approved for implementation 2026-11-13 ("lets do branches, implement that", "implement the tree in the game")

## 2. Into the game (after approval for implementation)

- [x] 2.1 `CrewData` loading `crew.json` (validated on load), the campaign officer's level, experience, skills and auto flag in the save
- [x] 2.2 Experience from battles and turns, promotion, transfer, hiring for prestige, retraining (`Campaign`), with tests
- [x] 2.3 The game's crew screen from mockup 39 (`PortPanel.Crew.cs`, still `--flow=crew`)
- [ ] 2.3a Sync task: mockup 1's Crew tab still shows the old officers (skill pips, credits); redraw it from mockup 39's screen
- [x] 2.4a The flagship's slots seat the campaign crew (`Battle.SeatCrew`, `crew.json` covers), each slot's hover card: rank, level, XP and their bonuses summed by ability (`crew.json` abilities) (owner, 2026-11-13: "in battle,hovering over crew members should show their skills , lvl and xp"); still `--flow=battle-crew`
- [x] 2.4 The flagship's battle slots take the campaign crew (officers 2.5); an officer's ability is the sum of their skills
- [x] 2.5 The new abilities' rules: turn, accel, speed, het_safe, evade, pd_double, charge, reactor_e, repair_crews, shield_rate, anticloak, standing (and the rest of the 25 in `CrewDef.Abilities`)
- [ ] 2.5a scan_weak: needs a scan readout of the target's weakest shield facing to show it in
- [ ] 2.7 The skill icons in every skin (the icon guide's subject list), and in the game's kits (the Concord's drawing is in the game: `assets/ui/skills/`, exported by `tools/mockups/export_skill_icons.mjs`)
- [ ] 2.6 A playtest: a campaign's officers promoted through a few battles
