# Fleet Command Sounds (mockup 17; drafted as 16, renumbered on merging main, where 16 is the special weapons range)

- Link: https://claude.ai/artifact/P2PEz2NG6Yv8NnKXT2SrNW (published from the cloud session's account, which cannot open the tracker or the survey)
- Shows: the first 30 sound effects with play buttons and waveform strips, grouped as the game uses them.
- Made by: `tools/audio/render_sfx.sh` (which runs `tools/audio/gen_sfx_page.py`) into `docs/mockups/sounds.html`; the
  page's look is `tools/audio/audio_page.py`. The page (40 KB) names each sound's MP3 share copy under `output/`, and the
  publish sends those files beside it at the same paths (`docs/mockups/sounds.files.json`, written with the page), as
  Crab Breeze's Music page serves its tracks. Publish with that map as `files`; the MP3s are rebuilt by the render step,
  not committed.
- Change: `openspec/changes/sound-effects`. Shots: `docs/screenshots/mockups/sounds-desktop.png`, `sounds-phone.png`,
  `sounds-rows.png`.

## Publishes

- Version 1 published 2026-11-05 from the cloud session at the owner's request ("need to pubhlish music artifacts and sound"), version id `1793914825-6d3a`.
- Version 2 published 2026-11-05, version id `1793916586-7ffb`: version 1 inlined every sound in the page and never loaded in
  the viewer (owner: "artifact not loading"); the audio is now served beside the page.
