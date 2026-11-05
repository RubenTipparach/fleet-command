# Audio sources

Every recording Fleet Command's sounds are cut from. 19 packs, every one CC0 by its own page when `tools/audio/fetch_cc0.py` read it (a pack whose page does not say CC0 is refused). CC0 asks for no credit; the people who recorded them are named anyway.

This file is written by `python3 tools/audio/list_sources.py` (run by `tools/audio/render_sfx.sh`) from `tools/audio/cc0_cues.json` and `tools/audio/cc0_licences.json`: edit those, not this. Each cue's exact files and edits are in `assets/audio/sfx/SOURCES.md` and `assets/audio/ambience/SOURCES.md`; candidates and what was looked at are in `docs/analysis/audio-sources.md`.

| Pack | Site | Recorded or uploaded by | Licence | Cues cut from it |
| --- | --- | --- | --- | --- |
| [100 cc0 metal and wood sfx](https://opengameart.org/content/100-cc0-metal-and-wood-sfx) | OpenGameArt | rubberduck | CC0 | `hull_hit`, `ship_destroyed`, `ui_clunk` |
| [50 cc0 sci fi sfx](https://opengameart.org/content/50-cc0-sci-fi-sfx) | OpenGameArt | rubberduck | CC0 | `torpedo_detonate`, `torpedo_launch` |
| [51 ui sound effects buttons switches and clicks](https://opengameart.org/content/51-ui-sound-effects-buttons-switches-and-clicks) | OpenGameArt | Kenney | CC0 | `ui_clunk` |
| [63 digital sound effects lasers phasers space etc](https://opengameart.org/content/63-digital-sound-effects-lasers-phasers-space-etc) | OpenGameArt | Kenney | CC0 | `disruptor_fire`, `lance_fire`, `torpedo_launch` |
| [airvent loop](https://opengameart.org/content/airvent-loop) | OpenGameArt | Longor | CC0 | `amb_bridge` |
| [battle at sea](https://opengameart.org/content/battle-at-sea) | OpenGameArt | Thimras | CC0 | `hull_hit`, `torpedo_launch` |
| [boost or launch or thruster sound effect](https://opengameart.org/content/boost-or-launch-or-thruster-sound-effect) | OpenGameArt | EZduzziteh | CC0 | `thruster_burst` |
| [chunky explosion](https://opengameart.org/content/chunky-explosion) | OpenGameArt | Joth | CC0 | `ship_destroyed` |
| [electricity game sound pack](https://opengameart.org/content/electricity-game-sound-pack) | OpenGameArt | faxcorp | CC0 | `disruptor_fire`, `overload_charge`, `shield_hit`, `shields_collapse`, `shields_raise`, `ship_destroyed` |
| [explosions 4](https://opengameart.org/content/explosions-4) | OpenGameArt | EZduzziteh | CC0 | `explosion_small`, `hull_hit`, `ship_destroyed`, `torpedo_detonate` |
| [generator loop](https://opengameart.org/content/generator-loop) | OpenGameArt | YCbCr | CC0 | `amb_bridge`, `engine_hum` |
| [laser weapon burst fire](https://opengameart.org/content/laser-weapon-burst-fire) | OpenGameArt | celestialghost8 | CC0 | `pd_burst` |
| [low rumbling](https://opengameart.org/content/low-rumbling) | OpenGameArt | Musheran | CC0 | `ship_destroyed` |
| [missile sound](https://opengameart.org/content/missile-sound) | OpenGameArt | mikeask | CC0 | `missile_launch` |
| [rocket engine](https://opengameart.org/content/rocket-engine) | OpenGameArt | theMinesAreShakin | CC0 | `engine_hum` |
| [rocket launch](https://opengameart.org/content/rocket-launch) | OpenGameArt | qubodup | CC0 | `missile_launch`, `thruster_burst` |
| [sci fi ambience sfx](https://opengameart.org/content/sci-fi-ambience-sfx) | OpenGameArt | Fun Gi Development | CC0 | `amb_bridge`, `reactor_hum` |
| [seamless energy emission loop](https://opengameart.org/content/seamless-energy-emission-loop) | OpenGameArt | zeroisnotnull | CC0 | `shields_raise` |
| [space ship shield sounds](https://opengameart.org/content/space-ship-shield-sounds) | OpenGameArt | bart | CC0 | `shield_hit`, `shields_collapse` |

## Not recorded

- Layered with our own synthesis (`tools/audio/gen_sfx.py`): `disruptor_fire`, `lance_fire`, `shield_hit`, `torpedo_detonate`, `torpedo_launch`.
- Synthesised outright: `core_breach`, `hail`, `hull_critical`, `pause_off`, `pause_on`, `red_alert`, `shields_failing`, `target_lock`, `ui_chirp`, `ui_confirm`, `ui_deny`, `weapon_charged`.
- Music: LMMS's own synths and the GeneralUser GS soundfont (S. Christian Collins, GeneralUser GS License v2.0: free for music creation, private or commercial), rendered from the scores in `music/` (`music-authoring` skill).
