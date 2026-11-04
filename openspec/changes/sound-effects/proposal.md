# Proposal: sound-effects

## Why

The owner, 2026-11-04: "pull in audio and music skills from crab breeze, lets get some audio for this game!" CLAUDE.md
section 11 already set the rules (recorded CC0 sounds where possible, every original named, battle sounds placed in 3D at
their ship or mount); nothing was built. Crab Breeze has the working pipeline: CC0 packs fetched and licence-checked,
cuts layered to a spec, synthesis where nothing recorded is better, and an audition page.

## What Changes

- **The pipeline, ported from Crab Breeze `0e569e3`**: `tools/audio/` (`fetch_cc0.py`, `cut_cc0.py`, `gen_sfx.py`'s
  building blocks, `list_sources.py`, `render_sfx.sh`, the page), with `inspect_sfx.py` new here (measuring sounds a
  cloud session cannot hear) and the page's look moved to one shared module, `audio_page.py`.
- **The `sound-effects` skill**: the method written down (Crab Breeze keeps it only in its CLAUDE.md).
- **The first set, 30 cues** (`assets/audio/sfx/`, `assets/audio/ambience/`): every weapon in `data/battle/weapons.json`
  firing (the Lance, the Disruptor, the Pulse torpedo, the missile, point defence, an overload charge), shield and hull
  hits, a torpedo detonating, small blasts and a ship destroyed, the engines, the reactor, thrusters, shields rising and
  failing, the console keys (the Concord's chirp, the metal skins' clunk), confirm and deny, a weapon charged, a target
  locked, a hail, pause and resume, the alerts (battle stations, shields failing, hull critical, a core breach) and a
  stereo bridge bed. 18 are cut from 19 CC0 packs on OpenGameArt, 12 synthesised.
- **The Fleet Command Sounds page** (`docs/mockups/sounds.html`): every cue with a play button, for the owner's ears.
- **The design for playing them in the game** (below), for after the owner approves the set.

## Impact

- New: `tools/audio/`, `.claude/skills/sound-effects/`, `assets/audio/sfx/`, `assets/audio/ambience/`,
  `AUDIO_SOURCES.md`, `docs/analysis/audio-sources.md`, `docs/mockups/sounds.html`, `docs/screenshots/audio/`.
- Game code waits for the owner's approval of the set (CLAUDE.md section 4): `src/Game` gains a sound player reading
  the battle's own events, `data/audio/sfx.json` the cue table.
