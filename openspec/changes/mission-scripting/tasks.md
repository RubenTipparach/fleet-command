# Tasks: mission-scripting

## 1. This change (2026-11-05)

- [x] 1.1 `Expr.cs`: the expression language and templates, with tests
- [x] 1.2 `MissionScript.cs`: the loader, its checks of every name against `Scopes`; `docs/design/mission-files.md`
- [x] 1.3 The fifteen mockup missions as `data/missions/*.json`
- [x] 1.4 `MissionRun.cs`: plan a mission's battle, grade it, apply its result; the prize that strikes and the Withdraw order in the simulation
- [x] 1.5 Tests: every mission offered, briefed, fought and graded for both playable sides; capture; withdrawing from an ambush
- [x] 1.6 `tools/mission-editor` (Avalonia): files, form and JSON, live checks, preview won, withdrawn or lost (`MissionPreview.cs`)
- [ ] 1.7 The editor run by the owner on Windows and macOS (its window was drawn off screen on the Mac, 2026-11-05: the display link was refused)

## 2. Still to do

- [ ] 2.1 The campaign mockup reads `data/missions/` (inlined) through a JavaScript copy of the evaluator, and `campaign-missions.js` goes (open question S1)
- [ ] 2.2 Boarding with marines in the battle (change `boarding-party`), so "lose no marines" can be an objective again
- [ ] 2.3 Stations and planets in the battle, so station and planet assaults fight the base itself
