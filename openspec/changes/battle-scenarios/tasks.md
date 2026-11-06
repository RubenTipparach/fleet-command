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

## 3. The damage range (owner, 2026-11-05: "make testing scenes for ship breaking tests from weapons fire like the artifacts", "and batle scars")

- [x] 3.1 `data/battle/range.json` (from_km, the programs) loaded and validated with the battle data; `damage_range` on a scenario
- [x] 3.2 `Battle.Range`: a shot from a bearing by no ship through `Damage`, the programs on the battle's step from the range's own seed, shields up or down, point defence off, a range never ends; tests (`RangeTests`)
- [x] 3.3 `concord_range` and `krai_range`: every class with an approved ship on the range, both sides
- [x] 3.4 The range's keys over the view (weapon, overload, Fire at it, Shields, Slow motion, the programs, Repair all, Next ship); a click on a ship's plating fires at the point clicked (`ShipDamage.PlatingAt`, `Aim`); the camera to 120 m and the ship centred on the range
- [x] 3.5 The scripted captain's `range` script with `snap` stills (`docs/screenshots/game/range/`), in the suite from both sides
- [ ] 3.6 The range in the mockups: the battle console mockup's scene list (the ship mockups' Damage tab is its source)
- [ ] 3.7 Lance and bolt scars read faintly on the range at 120 m (the torpedo holes and breaks read well): the owner to judge whether the scorch marks (ShipDamage.Scorch, damage.gdshaderinc) should be larger or darker, then the mockups and the game together

## 2. The mockup's other scenes (each when its system is in the game)

- [ ] 2.1 Mines, Tractor, Boarding (transporter, tractor, boarding)
- [ ] 2.2 Warp, Escape (warp, damage control)
- [ ] 2.3 Relief, VIP, Distress, Assault, Shuttle, Fighters (transport, cargo, craft)
- [ ] 2.4 Disable, Pinned fire, Nebula, Cloak (weapon targeting, cloaking, ECM)
- [ ] 2.5 Repel boarders (boarding defence)
