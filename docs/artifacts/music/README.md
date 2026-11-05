# Fleet Command Music (mockup 18; drafted as 17, renumbered on merging main)

- Link: https://claude.ai/artifact/NRSwJgnSQkt3ZiKZtG9Lov (published from the cloud session's account, which cannot open the tracker or the survey)
- Shows: the first score (the Main Title, Battle Stations, Victory, Ship Lost, the motif sheet) with play buttons, each
  cue's description read from its score.
- Made by: `tools/music/music_page.py` into `docs/mockups/music.html` (after `tools/music/render.sh` on each score); the
  page's look is `tools/audio/audio_page.py`. The page (16 KB) names each sound's MP3 share copy under `output/`, and the
  publish sends those files beside it at the same paths (`docs/mockups/music.files.json`, written with the page), as
  Crab Breeze's Music page serves its tracks. Publish with that map as `files`; the MP3s are rebuilt by the render step,
  not committed.
- Change: `openspec/changes/music`. Shots: `docs/screenshots/mockups/music-desktop.png`, `music-phone.png`.

## Publishes

- Version 1 published 2026-11-05 from the cloud session at the owner's request ("need to pubhlish music artifacts and sound"), version id `1793914827-bdd2`.
- Version 2 published 2026-11-05, version id `1793916574-caa5`: version 1 inlined every sound in the page and never loaded in
  the viewer (owner: "artifact not loading"); the audio is now served beside the page.
