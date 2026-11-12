# Tasks: crew-leveling

## 1. Mockup (2026-11-12)

- [x] 1.1 `data/campaign/crew.json`: ranks, experience, prestige rates, pool, trees, auto paths
- [x] 1.2 Mockup 39, `docs/mockups/crew-leveling.html`: the crew screen, playable (battles, turns, promote, transfer, hire, auto level, pick, retrain), desktop and phone
- [x] 1.3 Survey section CL (CL1-CL8)
- [x] 1.3a Version 2 (2026-11-12): ten levels, and a pyramid of fifteen skills a station with crossovers and two-prerequisite skills
- [x] 1.3b Version 3 (2026-11-12): one skill tree a station (fourteen skills, crossovers, a keystone and tips with two prerequisites), drawn icons (`docs/mockups/lib/skill-icons.js`), tooltips, four styles
- [ ] 1.4 The owner's review (and survey CL10, the style): approved for build, then for implementation

## 2. Into the game (after approval for implementation)

- [ ] 2.1 `CrewData` loading `crew.json` (validated on load), the campaign officer's level, experience, skills and auto flag in the save
- [ ] 2.2 Experience from battles and turns, promotion, transfer, hiring for prestige, retraining (`Campaign`), with tests
- [ ] 2.3 Mockup 1's Crew tab and the game's crew screen from mockup 39
- [ ] 2.4 The flagship's battle slots take the campaign crew (officers 2.5); an officer's ability is the sum of their skills
- [ ] 2.5 The new abilities' rules: turn, accel, speed, het_safe, evade, pd_double, charge, reactor_e, repair_crews, shield_rate, scan_weak, anticloak, standing
- [ ] 2.7 The skill icons in every skin (the icon guide's subject list), and in the game's kits
- [ ] 2.6 A playtest: a campaign's officers promoted through a few battles
