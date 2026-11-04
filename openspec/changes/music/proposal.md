# Proposal: music

## Why

The owner, 2026-11-04: "pull in audio and music skills from crab breeze, lets get some audio for this game!" The
`music-authoring` skill and `tools/music/` came over from Crab Breeze on 2026-10-30 and were proven on a two-bar test;
the game has no music. Survey S1 set the direction: orchestral space opera, brass, strings and timpani, over a synth bed.

## What Changes

- **The motifs, written down first** (`docs/design/music.md`, the MOTIF LIBRARY in `music/motifs.strudel`): the Concord
  (the player's fleet, an 8-bar sentence in E flat major with a lift to F major), the wonder chord, the Threat (a C minor
  ostinato and horn call for whoever the fleet fights) and the Lost (the Concord's idea in E flat minor).
- **The first cues**, scored in Strudel and rendered headless in LMMS: the Main Title (55 s), Battle Stations (a 43.6 s
  loop), Victory (12.5 s), Ship Lost (18 s) and the motif sheet (`assets/audio/music/*.ogg`, -16 LUFS).
- **Seamless loops**: `// @loop` in a score makes `tools/music/render.sh` cut the render at its scored length and fold
  the tail onto the start (`tools/music/fold_loop.py`).
- **The Fleet Command Music page** (`docs/mockups/music.html`, `tools/music/music_page.py`, on the same page code as
  the Sounds page).
- **The design for playing them in the game** (below), for after the owner approves the score.

## Impact

- New: `docs/design/music.md`, `music/*.strudel` with their `.mid` and `.mmp`, `assets/audio/music/`,
  `tools/music/fold_loop.py`, `tools/music/music_page.py`, `docs/mockups/music.html`.
- Changed: `tools/music/render.sh` (the loop step), the `music-authoring` skill's Fleet Command note.
- Survey questions S2 (faction themes) and S3 (battle layers).
- Game code waits for the owner's approval of the score (CLAUDE.md section 4).
