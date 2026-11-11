# Fleet Command Sounds (mockup 17; drafted as 16, renumbered on merging main, where 16 is the special weapons range)

- Link: https://claude.ai/artifact/7zWthg14SmsRfaFZpj1vAE (published 2026-11-10 from the desktop session's account; versions 1-10 below were at https://claude.ai/artifact/P2PEz2NG6Yv8NnKXT2SrNW, made from the cloud session's account, which this account cannot update)
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
- Version 3 published 2026-11-06, version id `1793924827-1a74`: the owner's notes (shorter, punchier weapons; short
  alternatives of the hits; a recorded battle stations klaxon and two alternatives; a minute-long bridge bed with
  bleeps, boops and scans). 36 cues.
- Version 4 published 2026-11-07, version id `1793952072-f1ca`: the bridge bed quieted (owner: "bridge loop is no longer
  backgrouind noise, it has a really annoying whirling noise ... get rid of the sweep sound, just subtle bleeps and
  airduct noise"): no sensor sweeps or scan pings, no swelling computer-room layer, the air duct twice over a low hum,
  the bleeps about 10 dB quieter.
- Version 5 published 2026-11-07, version id `1793953907-28eb`: the bridge and the weapons softened on Crab Breeze's
  low-pass rules (the bed at 3.5 kHz and -14 dB peak, bleeps included; every weapon at 4 kHz, 24 dB/octave, 6 dB down).
- Version 6 published 2026-11-07, version id `1793954421-9912`: the bridge bed without its low rumble (owner: "low rumble
  bridge ambient, it hurts my ears a bit", "make it sound pleaasent like proper ambience"): a soft room tone over the
  air duct, nothing under 180 Hz.
- Version 7 published 2026-11-07, version id `1793955211-d761`: the bridge bed as a hum (owner: "no, it got worse, now
  sounds like water!", "I want it to sound like a hum"): a warm 152 Hz tone and a recorded hum an octave up, no noise wash.
- Version 8 published 2026-11-07, version id `1793955877-84e3`: the bridge bed from the owner's pack (Little Robot Sound
  Factory's space ambience, CC-BY 3.0, credited on the page) and a brown-noise alternative; the synthesised hums removed
  (owner: "it sounds like electrical buzz which is wrong!").
- Version 9 published 2026-11-07, version id `1793955908-18e1`: every sound's link fingerprinted (`?v=` the file's
  hash), so a changed sound is never played from the viewer's cache.
- Version 10 published 2026-11-07, version id `1793956237-fd25`: the brown-noise bed is the bridge (owner: "delete
  amb_bridge, use amb_bridge_brown"); the space ambience removed, and with it the only CC-BY pack.
- Version 11 ready to publish 2026-11-10, not yet published (the artifact tools stopped working in the desktop session that
  made it): the weapons redone without the arcade lasers (owner: "Sfx are really goofy for the weapons. We need to fix
  that"), the doomsday laser cannon and a ship's cannon in the Lance, Disruptor and Pulse torpedo, the missile's ignition
  thump, point defence darkened. The page was rebuilt on the Mac by `tools/audio/render_sfx.sh` (sources in a scratch
  folder, `SFX_SRC`); its MP3s are in `output/sfx` there. The account that published versions 1-10 is the cloud session's,
  so this goes out as a new artifact from the desktop account; record its link here and in the tracker.
- 2026-11-10, desktop session (owner: "Can you update and publish the sfx artifact? There's some changes I want to suggest", "Publish new artifact"): published as a NEW artifact, https://claude.ai/artifact/7zWthg14SmsRfaFZpj1vAE, version 1, version id `1794367132-8556`, with its 36 MP3s beside it (`docs/mockups/sounds.files.json`). The page's text brought up to date: the weapons redone without the arcade lasers, every sound now in the game, no CC-BY pack in use. The tracker, `docs/design/README.md` and `mockup-work.md` point at the new link.
- 2026-11-11, desktop session (owner: "Shield hit bad, target select bad. Fix those"): version 2, version id `1794377172-fe48`: `shield_hit_short` and `target_lock` redone (commit `27ad65de`), their MP3s replaced at their served paths, the page's fingerprints updated; the other 34 sounds unchanged.

- 2026-11-11, desktop session (owner: "Shield hit still has an artificial retro game ding that's very out of place. It needs to sound like a low thump hit"): version 3, version id `1794377757-1d58`: `shield_hit_short` redone as a low thump with no ring (commit `8d41a92c`), its MP3 replaced at its served path, the page's fingerprint updated; the other 35 sounds unchanged.
