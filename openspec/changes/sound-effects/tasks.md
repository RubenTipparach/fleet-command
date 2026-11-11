# Tasks: sound-effects

## 1. The pipeline and the first set (done 2026-11-04)

- [x] 1.1 Port `tools/audio` from Crab Breeze `0e569e3` (fetch, cut, synthesis building blocks, sources list, render, page)
- [x] 1.2 Write the `sound-effects` skill and record its provenance
- [x] 1.3 Fetch 28 CC0 packs from OpenGameArt, licence-checked (`docs/analysis/audio-sources.md`)
- [x] 1.4 Build the 30 cues (18 recorded, 12 synthesised), check them (`inspect_sfx.py check`) and look at the sheet
- [x] 1.5 Write the Fleet Command Sounds page and screenshot it at desktop and phone widths
- [ ] 1.6 Publish the Sounds page from the desktop session; add it to the tracker and record its link

## 2. The owner's ears

- [ ] 2.1 The owner approves the set or notes cues to redo (the tracker). The console keys approved for implementation
  (owner, 2026-11-07: "Console keys - this should be implemented in the game")
- [ ] 2.2 Redo the noted cues

## 3. In the game (after approval for implementation)

- [x] 3.1 `data/audio/cues.json` and its validator in `src/Core` (`AudioCues`), with tests (2026-11-07: the console keys
  and the bridge bed; the battle's cues join it with 3.2-3.3)
- [x] 3.2 The event-to-cue mapping in `src/Core` (`BattleSoundRules`, `data/audio/battle.json`), tested on a real battle
  (2026-11-11, owner: "ok implment it")
- [x] 3.3 A sound player in `src/Game` reading `Battle.Hits` and the shots, placing 3D players where they happen
  (`BattleView.Sounds.cs`: a shot at its mount's position, a hit at the ship struck) (2026-11-11). Still to do: the
  exact mount node once every ship's glb carries named mounts
- [ ] 3.4 Buses and the Options volume boxes
- [x] 3.5 Skins' key sounds: each skin's style names its key cue (`SkinStyle.KeySound`, required), checked against the cue
  table as the skin is made; every console key plays it (`Imm.Pressed`, `ConsoleSounds.Key`) (2026-11-07)
- [x] 3.5a The bridge bed loops under a battle, silent on the menus and the map (`ConsoleSounds.Bed`) (2026-11-07)
- [x] 3.5b A playtest that hears the keys and the bed: in a scripted run every sound played prints `PLAYTEST_SOUND <cue>`;
  the Concord and Krai battles play the bed and their skin's key sound on every key press
  (`docs/playtests/2026-11-07-console-sounds.md`)
- [x] 3.6 The voice cap (`voices`, 24; a new sound takes the voice that started longest ago); a paused battle makes no new
  sounds because it makes no new events (2026-11-11)
- [x] 3.7 `PLAYTEST_SOUND` lines for the console and the battle (2026-11-07, 2026-11-11), counted in
  `docs/playtests/2026-11-11-battle-sounds.md`
- [ ] 3.8 The game's credits carry every CC-BY pack's credit in use (`tools/audio/licence_exceptions.json`); none is in
  use since 2026-11-07 (the space ambience was replaced by the brown-noise bed)
- [ ] 3.9 The ship sounds (engines, thrusters, reactor, shields rising and failing) and the alerts, with their events

