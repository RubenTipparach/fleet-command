# Tasks: music

## 1. The motifs and the first cues (done 2026-11-04)

- [x] 1.1 Write the motifs in `docs/design/music.md` and the MOTIF LIBRARY of `music/motifs.strudel`
- [x] 1.2 Score and render the Main Title, Battle Stations, Victory, Ship Lost and the motif sheet
- [x] 1.3 `// @loop` in `render.sh` (`tools/music/fold_loop.py`); Battle Stations loops at 43.64 s
- [x] 1.4 Check each render (clipping, loudness profile, spectrogram) and lift the leads
- [x] 1.5 Write the Fleet Command Music page and screenshot it
- [ ] 1.6 Publish the Music page from the desktop session; add it to the tracker and record its link

## 1b. Faction music (owner, 2026-11-07)

- [x] 1b.1 The Krai, Ssarith and Kelvane motifs in the MOTIF LIBRARY and `docs/design/music.md`, each with its sources
- [x] 1b.2 A battle loop and an ambient loop for each, and the Concord's ambient loop; seven new instruments calibrated
- [x] 1b.3 Rendered, checked (clipping, loops, loudness profiles, spectrograms) and put on the Music page
- [ ] 1b.4 The owner hears them and answers S5-S7

## 2. The owner's ears

- [ ] 2.1 The owner approves the score or notes what to change (the tracker)
- [ ] 2.2 Answer S2 and S3 (survey) and fold them in

## 3. In the game (after approval for implementation)

- [ ] 3.1 `data/audio/music.json` and its validator, with tests
- [ ] 3.2 The contact rule in `src/Core`, shared with the alerts, tested
- [ ] 3.3 The music player in `src/Game`: states, crossfades, loop points, pause ducking
- [ ] 3.4 A playtest beat for each state change (`PLAYTEST_BEAT music <cue>`)
