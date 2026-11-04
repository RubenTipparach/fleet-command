---
name: sound-effects
description: Make, change and judge Fleet Command's sound effects the repository's way, after Crab Breeze's audio pipeline. Recorded CC0 sounds first (packs fetched by tools/audio/fetch_cc0.py, which refuses any pack whose page does not say CC0 or public domain), cut, slowed, layered and filtered to a spec in tools/audio/cc0_cues.json by tools/audio/cut_cc0.py; synthesised in tools/audio/gen_sfx.py only where nothing recorded beats it, or as a layer a recording lacks; every original named in assets/audio/sfx/SOURCES.md and AUDIO_SOURCES.md; mono and placed in 3D except the console, alerts, ambience and music; judged by measurement and a spectrogram contact sheet because a cloud session cannot listen; auditioned on the Fleet Command Sounds page. Use whenever making or changing a sound effect, a weapon's sound, an impact, an explosion, an engine or reactor loop, a console click, an alert, an ambience bed, or asking where a sound came from or what licence it has ("make a sound", "sfx", "weapon sound", "explosion sound", "shield hit", "klaxon", "button click sound", "ambience", "CC0 audio", "audio sources").
metadata:
  author: Fleet Command (Claude Code), after Crab Breeze's tools/audio and its CLAUDE.md sound rule
  version: "1.0"
---

# Sound effects

The owner, 2026-11-04: "pull in audio and music skills from crab breeze, lets get some audio for this
game!" Crab Breeze keeps its sound method in its CLAUDE.md and `tools/audio/`, not in a skill; this
skill writes it down for Fleet Command. The tools are ported from Crab Breeze `0e569e3`
(`.claude/skills/PROVENANCE.md`). Music is the `music-authoring` skill.

## The rules

From CLAUDE.md section 11 and Crab Breeze's sound rule (owner, 2026-10-25: "all audio and sfx
approved", "verify all sounds are 3d based (except ambience and music and ui)"):

- **Recorded CC0 first.** A cue is cut from real recordings wherever one exists, slowed and layered
  to the size of the thing making it. It is synthesised only where nothing recorded beats it (console
  tones, alerts) or as a layer a recording lacks (a beam's held buzz, a shield's shimmer).
- **Every original is named.** `assets/audio/sfx/SOURCES.md` (one row per cue: each file and the
  edits) and `AUDIO_SOURCES.md` (one row per pack: its page, who recorded it, its licence) are
  written by the tools from the spec. Edit the spec, never the tables.
- **CC0 or public domain only**, checked on the pack's own page when it is fetched. A pack whose page
  does not say so is refused, and nothing is copied from another game.
- **Placed in 3D**: every battle sound (weapons, impacts, the ship) plays from its ship or its named
  mount node (CLAUDE.md section 10, named nodes), mono. Only the console keys, the alerts, the
  ambience beds and the music are unplaced, and only beds may be stereo.
- **A cue name is a contract.** The game's data names cues; a better recording replaces the file and
  keeps the name.
- **Deterministic.** Each synthesised cue seeds its generator from its own name; a rebuild writes the
  same sound.
- **A skin's console keys are its own** (survey U6, decided: a skin changes pictures, colours, type
  and button sounds). `ui_chirp` is the Concord's soft chirp, `ui_clunk` the modelled metal skins'.
  A new skin's key sound comes from how its makers build things, never from the species' body
  (CLAUDE.md section 1).

## The pipeline

```
tools/audio/cc0_packs.json --fetch_cc0.py--> /opt/sfx-src (originals, never committed) + licences.json
tools/audio/gen_sfx.py      ---------------> output/sfx/<cue>.wav + sfx.json   (synthesised cues and layers)
tools/audio/cc0_cues.json   --cut_cc0.py---> output/sfx/<cue>.wav               (recorded cues replace them)
                                             assets/audio/sfx/SOURCES.md, assets/audio/ambience/SOURCES.md
render_sfx.sh: ffmpeg -----> assets/audio/sfx/<cue>.ogg (Vorbis q5, mono), assets/audio/ambience/amb_*.ogg (stereo, q4)
                             output/sfx/<cue>.mp3 (share copies), AUDIO_SOURCES.md (list_sources.py)
gen_sfx_page.py -----------> docs/mockups/sounds.html (every cue with a play button, MP3s inlined)
inspect_sfx.py check ------> clipping, loop seams, output/sfx/sheet.png (look at it)
```

One command rebuilds everything: `tools/audio/render_sfx.sh` (or `tools/audio/render_sfx.sh <cue> ...`).

## Adding or changing a cue

1. **Name it and place it.** Group `Weapons`, `Impacts`, `Ship` (placed) or `Console`, `Alerts`,
   `Ambience` (not placed; a bed's name starts `amb_`). Write its `what` from the player's side:
   what they hear, then what makes it. A loop's `what` contains `(loops)`.
2. **Find recordings.** Search OpenGameArt with the CC0 filter
   (`https://opengameart.org/art-search-advanced?keys=<words>&field_art_type_tid[]=13&field_art_licenses_tid[]=4`),
   add the pack to `tools/audio/cc0_packs.json` as `oga/<slug>`, then
   `python3 tools/audio/fetch_cc0.py --dest /opt/sfx-src oga/<slug>`. Kenney packs (`kenney/<slug>`)
   and Internet Archive items (`archive/<item>:<file>,...`) work the same way where the network allows.
3. **Choose by measurement**, since a cloud session cannot listen:
   `python3 tools/audio/inspect_sfx.py inventory` lists every file with its length and brightness
   (spectral centroid); `python3 tools/audio/inspect_sfx.py envelope 'oga/<slug>:<file>' ...` prints a
   0.1 s loudness strip and the share of energy under 250 Hz and over 4 kHz, which shows where a take
   starts, how many shots it holds and how heavy it is. A heavy thing (a torpedo tube, a hull) wants
   low energy; a beam or a spark wants high.
4. **Write the spec** in `tools/audio/cc0_cues.json` (its `_about` lists every field): layers of
   `{"src": "pack:file"}` or `{"synth": "<layer>"}` with `start`, `dur`, `pitch` (tape speed: below
   1 is bigger and slower), `gain`, `lp`, `hp`, fades and `delay`; the cue's `len`, `drive`, `loop`,
   `stereo`, `distance` and `peak_db`. Say the edits in `edits`.
5. **Synthesise only what is missing.** A synthesised cue or layer is a function in
   `tools/audio/gen_sfx.py` registered with `@sound(name, group, what, peak_db)`; a layer's group is
   `_layer`, so it is mixed into recorded cues and never written alone. Use the building blocks
   already there (oscillators, `sweep`, `adsr`, the SVF `lp` and `hp`, `ring`, `reverb`, `chorus`,
   `tone`); never a second copy of one.
6. **Build and check**: `tools/audio/render_sfx.sh`, then `python3 tools/audio/inspect_sfx.py check`
   (fails on clipping or a loop seam bigger than a few ordinary sample steps) and **look at the
   contact sheet** `output/sfx/sheet.png`: a launch should rise, a hit should strike and decay, a loop
   should be even, a console tone should fade to nothing (a hard vertical edge at its end is a click).
   Copy the sheet to `docs/screenshots/audio/sfx-sheet.jpg` at half size.
7. **Audition and record**: the page is rewritten by the build; it is an artifact like a mockup
   (CLAUDE.md section 4): commit it with its shots, its record in `docs/artifacts/sounds/` and its row
   in `mockup-work.md` and the tracker, and publish it (from the desktop session).

## Levels

Cues are peak-normalised when written: `-1 dBFS` for weapons and blasts, about `-9 dBFS` for
console keys, `-4` to `-8` for alerts, `-6` for the bridge bed. The mix between cues is set by each
cue's volume in the game's data, not by re-cutting the file.

## Setup in a fresh cloud session

`apt-get install -y --no-install-recommends ffmpeg p7zip-full unzip` and Python with numpy (about a
minute); `python3 tools/audio/fetch_cc0.py` fetches every pack (about 110 MB) into `/opt/sfx-src`.
On 2026-11-04 OpenGameArt was reachable from a cloud session and kenney.nl answered 403; the Kenney
packs used here are Kenney's own uploads on OpenGameArt.
