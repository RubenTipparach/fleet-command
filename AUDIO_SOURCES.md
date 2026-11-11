# Audio sources

Every recording Fleet Command's sounds are cut from. 24 packs, every one CC0 or public domain by its own page when `tools/audio/fetch_cc0.py` read it (a pack whose page does not say so is refused), except the few the owner asked for under another licence (`tools/audio/licence_exceptions.json`), each printed with the credit its page asks for, which the game's credits must carry. CC0 asks for no credit; the people who recorded them are named anyway.

This file is written by `python3 tools/audio/list_sources.py` (run by `tools/audio/render_sfx.sh`) from `tools/audio/cc0_cues.json` and `tools/audio/cc0_licences.json`: edit those, not this. Each cue's exact files and edits are in `assets/audio/sfx/SOURCES.md` and `assets/audio/ambience/SOURCES.md`; candidates and what was looked at are in `docs/analysis/audio-sources.md`.

| Pack | Site | Recorded or uploaded by | Licence | Cues cut from it |
| --- | --- | --- | --- | --- |
| [100 cc0 metal and wood sfx](https://opengameart.org/content/100-cc0-metal-and-wood-sfx) | OpenGameArt | rubberduck | CC0 | `hull_hit`, `hull_hit_short`, `ship_destroyed`, `ui_clunk` |
| [50 cc0 sci fi sfx](https://opengameart.org/content/50-cc0-sci-fi-sfx) | OpenGameArt | rubberduck | CC0 | `amb_bridge`, `torpedo_detonate`, `torpedo_detonate_short` |
| [51 ui sound effects buttons switches and clicks](https://opengameart.org/content/51-ui-sound-effects-buttons-switches-and-clicks) | OpenGameArt | Kenney | CC0 | `ui_clunk` |
| [60 cc0 sci fi sfx](https://opengameart.org/content/60-cc0-sci-fi-sfx) | OpenGameArt | rubberduck | CC0 | `amb_bridge` |
| [alarm 1](https://opengameart.org/content/alarm-1) | OpenGameArt | EZduzziteh | CC0 | `red_alert_twotone` |
| [alarm 2](https://opengameart.org/content/alarm-2) | OpenGameArt | Frenchyboy | CC0 | `red_alert` |
| [battle at sea](https://opengameart.org/content/battle-at-sea) | OpenGameArt | Thimras | CC0 | `disruptor_fire`, `hull_hit`, `hull_hit_short`, `lance_fire`, `missile_launch`, `shield_hit_short`, `shield_hit_short_3`, `torpedo_launch` |
| [boost or launch or thruster sound effect](https://opengameart.org/content/boost-or-launch-or-thruster-sound-effect) | OpenGameArt | EZduzziteh | CC0 | `thruster_burst`, `torpedo_launch` |
| [chunky explosion](https://opengameart.org/content/chunky-explosion) | OpenGameArt | Joth | CC0 | `ship_destroyed` |
| [doomsday laser cannon sound effect](https://opengameart.org/content/doomsday-laser-cannon-sound-effect) | OpenGameArt | TAD | CC0 | `disruptor_fire`, `lance_fire` |
| [electricity game sound pack](https://opengameart.org/content/electricity-game-sound-pack) | OpenGameArt | faxcorp | CC0 | `disruptor_fire`, `overload_charge`, `shield_hit`, `shields_collapse`, `shields_raise`, `ship_destroyed` |
| [explosion somewhere far](https://opengameart.org/content/explosion-somewhere-far) | OpenGameArt | pauliuw | CC0 | `shield_hit_short`, `shield_hit_short_2` |
| [explosions 4](https://opengameart.org/content/explosions-4) | OpenGameArt | EZduzziteh | CC0 | `explosion_small`, `explosion_small_short`, `hull_hit`, `hull_hit_short`, `ship_destroyed`, `torpedo_detonate`, `torpedo_detonate_short` |
| [generator loop](https://opengameart.org/content/generator-loop) | OpenGameArt | YCbCr | CC0 | `engine_hum` |
| [interface beeps](https://opengameart.org/content/interface-beeps) | OpenGameArt | bart | CC0 | `amb_bridge`, `target_lock` |
| [laser weapon burst fire](https://opengameart.org/content/laser-weapon-burst-fire) | OpenGameArt | celestialghost8 | CC0 | `pd_burst` |
| [low rumbling](https://opengameart.org/content/low-rumbling) | OpenGameArt | Musheran | CC0 | `ship_destroyed` |
| [missile sound](https://opengameart.org/content/missile-sound) | OpenGameArt | mikeask | CC0 | `missile_launch` |
| [rocket engine](https://opengameart.org/content/rocket-engine) | OpenGameArt | theMinesAreShakin | CC0 | `engine_hum` |
| [rocket launch](https://opengameart.org/content/rocket-launch) | OpenGameArt | qubodup | CC0 | `thruster_burst` |
| [sci fi ambience sfx](https://opengameart.org/content/sci-fi-ambience-sfx) | OpenGameArt | Fun Gi Development | CC0 | `reactor_hum` |
| [seamless energy emission loop](https://opengameart.org/content/seamless-energy-emission-loop) | OpenGameArt | zeroisnotnull | CC0 | `shields_raise` |
| [sirens and alarm noise](https://opengameart.org/content/sirens-and-alarm-noise) | OpenGameArt | aquinn | CC0 | `red_alert_wail` |
| [space ship shield sounds](https://opengameart.org/content/space-ship-shield-sounds) | OpenGameArt | bart | CC0 | `shield_hit_short_3` |

## Not recorded

- Layered with our own synthesis (`tools/audio/gen_sfx.py`): `amb_bridge`, `shield_hit`, `shields_collapse`, `torpedo_detonate`, `torpedo_detonate_short`.
- Synthesised outright: `core_breach`, `hail`, `hull_critical`, `pause_off`, `pause_on`, `shields_failing`, `ui_chirp`, `ui_confirm`, `ui_deny`, `weapon_charged`.
- Music: LMMS's own synths and the GeneralUser GS soundfont (S. Christian Collins, GeneralUser GS License v2.0: free for music creation, private or commercial), rendered from the scores in `music/` (`music-authoring` skill).
