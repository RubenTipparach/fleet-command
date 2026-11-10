# Tasks: nebula-plumes

## 1. The rules (src/Core)

- [x] 1.1 Plumes in a scenario (`nebulae`: place, radius, density, look, storm), validated; `Rules.NebulaAt`, `StormAt`
- [x] 1.2 Half the chance to hit (`NebulaHitX`), half an energy weapon's range (`Rules.RangeKm`, one reader for every
  range), lost missiles (`MissileLostPerS`), the lock range; `NebulaTests`
- [x] 1.3 Ion storms: `Battle.NebulaStep`, `LightningStrike`, a core down (`Ship.CoreDown`, `ShieldsHold`, `HoldsFire`),
  the grace after it; seeded from the nebula's own fork; `NebulaTests`
- [ ] 1.4 AI captains that know a storm from a plume (steer round one, lure a foe into one); survey NP6

## 2. The view (src/Game)

- [x] 2.1 The shared noise (`tools/space/plume_noise.py`, `assets/space/plume_noise.bin`, `--check` in the tests)
- [x] 2.2 The looks (`data/space/plumes.json`, `PlumeLooks`), one for every `NebulaDef.Looks`
- [x] 2.3 The plume (`NebulaPlume`, `nebula_plume.gdshader`): the column's shape baked to a 3D texture, the march, the
  light march, the view from inside
- [x] 2.4 Lightning (`BattleView.Nebula`): the flicker in a storm, the bolt onto a struck ship, dark lamps on a tripped core
- [x] 2.5 The plumes on the radar dial and the tactical map
- [x] 2.6 The heavy-fleet scenes and the Nebula scenes under plumes; stills (`nebula-plume`, `nebula-storm`,
  `nebula-inside` in `scripts/game-stills.sh`)
- [x] 2.7 The scripted captain plays the heavy scenes (`data/playtests/heavy-fleet.json`, the `all_out` beat) and the
  Nebula scenes under the new rules (`scripts/playtest.sh`)
- [ ] 2.8 A thunder cue for a strike and a rumble inside a storm (`sound-effects`), placed in 3D at the struck ship
- [ ] 2.9 A readout for a tripped core on the ship systems display (the seconds left), beyond the log line and the dark lamps
- [ ] 2.10 The phone's frame time (Compatibility: 44 steps, 2 light steps, no wisps) measured on the owner's phone (survey T3)
- [ ] 2.11 `assets/space/*.bin` in the export presets' include filter, when the Windows and Android exports are set up
- [ ] 2.12 A planet's air or rings in front of a plume draw under it (the render order's known limit, design): sort
  plumes against planets by distance if a scene ever needs it

## 3. Mockup sync (CLAUDE.md section 4)

- [ ] 3.1 `docs/mockups/battle-console.html`: the nebula as plumes (several, each with a place, radius, density, look and
  storm) in place of the one disc; `NEBULA` read from `data/battle/systems/cloak.json` by `inline_data.py` (it carries its
  own `hit_penalty: 0.3, ew_gain: 2, lock_km: 6` today)
- [ ] 3.2 The mockup's rules: half accuracy, half energy range, lost missiles, storms and the tripped core
- [ ] 3.3 The plumes on the mockup's radar and map; the Heavy fleets scene; shots beside the game's stills
- [ ] 3.4 Campaign: a sector the chart draws as nebula puts plumes in its battles (`campaign-space`)

## 4. The owner's review

- [ ] 4.1 Survey NP1-NP7; the looks approved or changed
