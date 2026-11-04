# Tasks: battle-scenarios

## 1. This change (2026-11-04)

- [x] 1.1 `data/battle/scenarios.json` with the loader's validation (side with a skin, the player's ship first and of the side, unique ids, standing orders)
- [x] 1.2 The Krai Warhost faction with its skin, and the Krai line's four classes
- [x] 1.3 Six scenarios, all the Krai against the Concord in approved ships: Battle, Duel and Arming for each side; the old skirmish kept as the golden seed's fixture
- [x] 1.4 Standing orders applied by the simulation (stance, range, erratic, hold fire, hull and shields at the start); the first message from the scenario
- [x] 1.5 The scene selector in the view; `--scenario=<id>`
- [x] 1.6 Tests: both sides present, every scenario runs a minute, refusals, standing orders, hold fire
- [ ] 1.7 Stills of both sides beside mockups 2 and 8, looked at (CLAUDE.md section 12)
- [x] 1.8 The scripted captain (owner, 2026-11-04: "develop a bot to play through these test scenarios for each faction"): `ScriptedCaptain`, `data/playtests/` (battle, arming, the suite of six runs), `scripts/playtest.sh`
- [ ] 1.9 The whole suite run headless on both sides and reported in `docs/playtests/` (the golden-seed skirmish ran first: won at 173 s, hull 100 %)
- [ ] 1.10 A recorded take of one battle per side (`playtest.sh --watch` under OBS), on Drive

## 2. The mockup's other scenes (each when its system is in the game)

- [ ] 2.1 Mines, Tractor, Boarding (transporter, tractor, boarding)
- [ ] 2.2 Warp, Escape (warp, damage control)
- [ ] 2.3 Relief, VIP, Distress, Assault, Shuttle, Fighters (transport, cargo, craft)
- [ ] 2.4 Disable, Pinned fire, Nebula, Cloak (weapon targeting, cloaking, ECM)
- [ ] 2.5 Repel boarders (boarding defence)
