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
- [x] 2.2a The weapons redone (owner, 2026-11-10: "Sfx are really goofy for the weapons. We need to fix that"): the
  Lance, Disruptor and Pulse torpedo rebuilt without the arcade lasers, zaps and phaser rises and the synthesised snap,
  from the doomsday laser cannon's blast and a ship's cannon (`tools/audio/cc0_cues.json`); the missile given a cannon's
  ignition thump; point defence slowed and darkened; an old-against-new audition on Drive for the owner's ears
- [ ] 2.2b The owner's verdict on the redone weapons
- [x] 2.2c The shield hit and the lock tone redone (owner, 2026-11-11: "Shield hit bad, target select bad. Fix those"):
  three candidates of each in an audition on Drive; candidate A of each in the game (a soft dark crack over a deep shield
  thump; a rising two-note pip from recorded interface tones, in place of our synthesised sweep)
- [ ] 2.2d The owner's pick from the shield and lock audition (A, B or C of each)

## 3. In the game (after approval for implementation)

- [x] 3.1 `data/audio/cues.json` and its validator in `src/Core` (`AudioCues`), with tests (2026-11-07: the console keys
  and the bridge bed; the battle's cues join it with 3.2-3.3)
- [x] 3.2 The event-to-cue mapping in `src/Core` (`BattleSounds`, `data/audio/events.json` read by `SoundMap` and checked
  against the cue table; `BattleSoundsTests`) (owner, 2026-11-10: "Implement sound fx in the game")
- [x] 3.3 A sound player in `src/Game` (`BattleAudio`): shots, hits, ships destroyed and lightning placed in 3D at their
  ship, quieter with distance; the commanded ship's lock and critical hull and the pause key through the console
  (placed at the ship, not yet at the named mount node)
- [x] 3.4 Buses and the Options volume boxes: Master and Effects; the boxes' curve made the amplitude's share (a squared
  curve put the defaults 20 dB down and the bed and the keys were not heard: owner, 2026-11-10, "I don't hear any clicks or
  bridge hums"); the cue levels set so a near sound peaks about 10 dB under full scale
- [x] 3.5 Skins' key sounds: each skin's style names its key cue (`SkinStyle.KeySound`, required), checked against the cue
  table as the skin is made; every console key plays it (`Imm.Pressed`, `ConsoleSounds.Key`) (2026-11-07)
- [x] 3.5a The bridge bed loops under a battle, silent on the menus and the map (`ConsoleSounds.Bed`) (2026-11-07)
- [x] 3.5b A playtest that hears the keys and the bed: in a scripted run every sound played prints `PLAYTEST_SOUND <cue>`;
  the Concord and Krai battles play the bed and their skin's key sound on every key press
  (`docs/playtests/2026-11-07-console-sounds.md`)
- [x] 3.6 The voice cap (24 placed voices, the longest-playing taken past it) and the pause (no battle event while paused;
  the pause key's own sound)
- [ ] 3.7 `PLAYTEST_SOUND` lines (done for the console, 2026-11-07) and a playtest beat that checks them for the battle's
  sounds
- [ ] 3.8 The game's credits carry every CC-BY pack's credit in use (`tools/audio/licence_exceptions.json`); none is in
  use since 2026-11-07 (the space ambience was replaced by the brown-noise bed)
