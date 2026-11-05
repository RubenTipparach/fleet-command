# Tasks: sound-effects

## 1. The pipeline and the first set (done 2026-11-04)

- [x] 1.1 Port `tools/audio` from Crab Breeze `0e569e3` (fetch, cut, synthesis building blocks, sources list, render, page)
- [x] 1.2 Write the `sound-effects` skill and record its provenance
- [x] 1.3 Fetch 28 CC0 packs from OpenGameArt, licence-checked (`docs/analysis/audio-sources.md`)
- [x] 1.4 Build the 30 cues (18 recorded, 12 synthesised), check them (`inspect_sfx.py check`) and look at the sheet
- [x] 1.5 Write the Fleet Command Sounds page and screenshot it at desktop and phone widths
- [ ] 1.6 Publish the Sounds page from the desktop session; add it to the tracker and record its link

## 2. The owner's ears

- [ ] 2.1 The owner approves the set or notes cues to redo (the tracker)
- [ ] 2.2 Redo the noted cues

## 3. In the game (after approval for implementation)

- [ ] 3.1 `data/audio/sfx.json` and its validator in `src/Core`, with tests
- [ ] 3.2 The event-to-cue mapping in `src/Core` (pure, tested on golden-seed battles)
- [ ] 3.3 A sound player in `src/Game` reading `Battle.Hits` and the shots, placing 3D players at the named nodes
- [ ] 3.4 Buses and the Options volume boxes
- [ ] 3.5 Skins' key sounds through the kit manifest
- [ ] 3.6 The voice cap and pause behaviour
- [ ] 3.7 `PLAYTEST_BEAT sound` lines and a playtest that checks them
