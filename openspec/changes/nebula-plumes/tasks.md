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
- [x] 2.13 A lock needs a clear sight line (`Rules.NebulaOnLine`, `Battle.CanLock`, `lock_km` 20); `NebulaTests`
- [x] 2.14 The decks (`NebulaDeck`, `nebula_deck.gdshader`, `data/space/nebula_decks.json`), the shared cloud include
  (`nebula_cloud.gdshaderinc`), columns that reach them, coloured by column placement, fading at their edges and into
  the sky
- [x] 2.15 Four forms (`NebulaDef.Forms`) and each column's own proportions; the four scenes' extra columns and puffs
- [x] 2.16 The nebula battles' skies in their plumes' colours (`tools/space/nebula_recipes.py`)
- [ ] 2.17 The deck's grain where a long ray crosses it at a low angle (40 steps over up to 300 km): blue-noise jitter or
  a step that grows with distance
- [ ] 2.18 The frame time with the decks and eleven plumes, on the Mac and on the phone, for each style
- [x] 2.19 Varying density inside the cloud (`contrast`), the nebula's fog, and the three styles (`volume`, `meshes`,
  `billboards`) on one switch, side by side in `nebula-styles.png`
- [ ] 2.20 Keep the volume as the high setting and the billboards as the low one (owner, 2026-11-10: "geometry version
  sucks lol", "we could save puffs for a lower end setting device like phones"); delete the meshes; a graphics setting
  picks the style, the billboards on phones
- [x] 2.21 The volume cheaper: 64 longer steps, the decks' field baked (`DeckField`); the chase view 21.7 to 10.4 ms
- [x] 2.22 Each column's shadow baked toward the battle's sun (`NebulaPlume.ShadowTexture`); the chase view 10.4 to 8.9 ms
- [ ] 2.23 The march at half the pixels with 128 steps (owner: "try the half resolution trick with 128")
- [ ] 2.24 Fewer steps for a far or small column (owner: "we can come back to 2 if we reeeealllly need it")

## 3. Mockup sync (CLAUDE.md section 4)

- [ ] 3.1 `docs/mockups/battle-console.html`: the nebula as plumes (several, each with a place, radius, density, look and
  storm) in place of the one disc; `NEBULA` read from `data/battle/systems/cloak.json` by `inline_data.py` (it carries its
  own `hit_penalty: 0.3, ew_gain: 2, lock_km: 6` today)
- [ ] 3.2 The mockup's rules: half accuracy, half energy range, lost missiles, storms and the tripped core
- [ ] 3.3 The plumes on the mockup's radar and map; the Heavy fleets scene; shots beside the game's stills
- [ ] 3.4 Campaign: a sector the chart draws as nebula puts plumes in its battles (`campaign-space`)

## 4. The owner's review

- [ ] 4.1 Survey NP1-NP11; the looks approved or changed
