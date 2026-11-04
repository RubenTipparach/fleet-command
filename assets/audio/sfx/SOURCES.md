# Sound effect sources

Recorded cues are cut from CC0 and public domain originals by `tools/audio/cut_cc0.py` to the spec in
`tools/audio/cc0_cues.json` (originals fetched by `tools/audio/fetch_cc0.py`, which refuses a pack
whose page does not say CC0 or public domain); the rest are synthesised by `tools/audio/gen_sfx.py`.
Rebuild all of them with `tools/audio/render_sfx.sh`. This file is written by `cut_cc0.py`: edit the
spec, not this table. CC0 asks for no credit; the people who recorded the originals are named anyway.
Candidates and what was looked at: `docs/analysis/audio-sources.md`.

| Cue | Made from | Edits |
| --- | --- | --- |
| core_breach | `tools/audio/gen_sfx.py` (synthesised, ours) | none |
| disruptor_fire | [63-digital-sound-effects-lasers-phasers-space-etc](https://opengameart.org/content/63-digital-sound-effects-lasers-phasers-space-etc) (Kenney) `zap1.mp3`, CC0; [electricity-game-sound-pack](https://opengameart.org/content/electricity-game-sound-pack) (faxcorp) `groundhit.wav`, CC0; our `sub_thump` synthesis | slowed to 0.85 speed, layered with a crack and our sub thump, saturated |
| engine_hum | [rocket-engine](https://opengameart.org/content/rocket-engine) (theMinesAreShakin) `rocket_engine.001.wav`, CC0; [generator-loop](https://opengameart.org/content/generator-loop) (YCbCr) `loop_generator_1.mp3`, CC0 | a rocket motor slowed to 0.7 and lowpassed, a generator under it, crossfaded into a loop |
| explosion_small | [explosions-4](https://opengameart.org/content/explosions-4) (EZduzziteh) `explosion3.ogg`, CC0; [explosions-4](https://opengameart.org/content/explosions-4) (EZduzziteh) `explosion2.ogg`, CC0 | layered, the second slowed to 0.8 |
| hail | `tools/audio/gen_sfx.py` (synthesised, ours) | none |
| hull_critical | `tools/audio/gen_sfx.py` (synthesised, ours) | none |
| hull_hit | [battle-at-sea](https://opengameart.org/content/battle-at-sea) (Thimras) `cannon_hit_ship_short.ogg`, CC0; [100-cc0-metal-and-wood-sfx](https://opengameart.org/content/100-cc0-metal-and-wood-sfx) (rubberduck) `metal_slam_01.ogg`, CC0; [explosions-4](https://opengameart.org/content/explosions-4) (EZduzziteh) `explosion2.ogg`, CC0 | a cannonball hitting a ship slowed to 0.8 and lowpassed, a metal slam at 0.6, saturated |
| lance_fire | [63-digital-sound-effects-lasers-phasers-space-etc](https://opengameart.org/content/63-digital-sound-effects-lasers-phasers-space-etc) (Kenney) `laser4.mp3`, CC0; our `beam_body` synthesis; [63-digital-sound-effects-lasers-phasers-space-etc](https://opengameart.org/content/63-digital-sound-effects-lasers-phasers-space-etc) (Kenney) `zap2.mp3`, CC0 | slowed to 0.7-0.8 speed, layered over our beam synthesis, lowpassed |
| missile_launch | [missile-sound](https://opengameart.org/content/missile-sound) (mikeask) `missile launch.ogg`, CC0; [rocket-launch](https://opengameart.org/content/rocket-launch) (qubodup) `launch.wav`, CC0 | slowed to 0.9, layered with a rocket's roar, lowpassed |
| overload_charge | [electricity-game-sound-pack](https://opengameart.org/content/electricity-game-sound-pack) (faxcorp) `charge.wav`, CC0 | trimmed, lowpassed, crossfaded into a loop |
| pause_off | `tools/audio/gen_sfx.py` (synthesised, ours) | none |
| pause_on | `tools/audio/gen_sfx.py` (synthesised, ours) | none |
| pd_burst | [laser-weapon-burst-fire](https://opengameart.org/content/laser-weapon-burst-fire) (celestialghost8) `burst fire.mp3`, CC0 | sped to 1.1, highpassed |
| reactor_hum | [sci-fi-ambience-sfx](https://opengameart.org/content/sci-fi-ambience-sfx) (Fun Gi Development) `SFX SCI FI Hum.mp3`, CC0 | trimmed, crossfaded into a loop |
| red_alert | `tools/audio/gen_sfx.py` (synthesised, ours) | none |
| shield_hit | [electricity-game-sound-pack](https://opengameart.org/content/electricity-game-sound-pack) (faxcorp) `shieldhit.wav`, CC0; [space-ship-shield-sounds](https://opengameart.org/content/space-ship-shield-sounds) (bart) `space shield sounds - 4.wav`, CC0; our `shield_shimmer` synthesis; [electricity-game-sound-pack](https://opengameart.org/content/electricity-game-sound-pack) (faxcorp) `groundhit.wav`, CC0 | two shield takes layered with our shimmer synthesis and a spark |
| shields_collapse | [electricity-game-sound-pack](https://opengameart.org/content/electricity-game-sound-pack) (faxcorp) `powerdown.wav`, CC0; [space-ship-shield-sounds](https://opengameart.org/content/space-ship-shield-sounds) (bart) `space shield sounds - 1.wav`, CC0 | layered |
| shields_failing | `tools/audio/gen_sfx.py` (synthesised, ours) | none |
| shields_raise | [electricity-game-sound-pack](https://opengameart.org/content/electricity-game-sound-pack) (faxcorp) `powerup.wav`, CC0; [seamless-energy-emission-loop](https://opengameart.org/content/seamless-energy-emission-loop) (zeroisnotnull) `movingshield_sound.ogg`, CC0 | layered with a shield hum |
| ship_destroyed | [chunky-explosion](https://opengameart.org/content/chunky-explosion) (Joth) `Chunky Explosion.mp3`, CC0; [electricity-game-sound-pack](https://opengameart.org/content/electricity-game-sound-pack) (faxcorp) `deathboom.wav`, CC0; [explosions-4](https://opengameart.org/content/explosions-4) (EZduzziteh) `explosion1_0.ogg`, CC0; [100-cc0-metal-and-wood-sfx](https://opengameart.org/content/100-cc0-metal-and-wood-sfx) (rubberduck) `metal_falling_01.ogg`, CC0; [low-rumbling](https://opengameart.org/content/low-rumbling) (Musheran) `low-rumbling-176033.mp3`, CC0 | four takes slowed to 0.5-0.8 and layered over a low rumble, the metal lowpassed |
| target_lock | `tools/audio/gen_sfx.py` (synthesised, ours) | none |
| thruster_burst | [boost-or-launch-or-thruster-sound-effect](https://opengameart.org/content/boost-or-launch-or-thruster-sound-effect) (EZduzziteh) `boost.mp3`, CC0; [rocket-launch](https://opengameart.org/content/rocket-launch) (qubodup) `launch.wav`, CC0 | a boost slowed to 0.6, a slice of rocket roar |
| torpedo_detonate | [explosions-4](https://opengameart.org/content/explosions-4) (EZduzziteh) `explosion1_0.ogg`, CC0; our `energy_bloom` synthesis; [50-cc0-sci-fi-sfx](https://opengameart.org/content/50-cc0-sci-fi-sfx) (rubberduck) `explosion_01.ogg`, CC0 | two blasts slowed to 0.7-0.8, layered with our energy bloom |
| torpedo_launch | [battle-at-sea](https://opengameart.org/content/battle-at-sea) (Thimras) `cannon_fire_1.ogg`, CC0; [63-digital-sound-effects-lasers-phasers-space-etc](https://opengameart.org/content/63-digital-sound-effects-lasers-phasers-space-etc) (Kenney) `phaserUp3.mp3`, CC0; [50-cc0-sci-fi-sfx](https://opengameart.org/content/50-cc0-sci-fi-sfx) (rubberduck) `teleport_02.ogg`, CC0; our `sub_thump` synthesis | a ship's cannon slowed and lowpassed to a thump, a phaser rise slowed to 0.6, our sub thump |
| ui_chirp | `tools/audio/gen_sfx.py` (synthesised, ours) | none |
| ui_clunk | [51-ui-sound-effects-buttons-switches-and-clicks](https://opengameart.org/content/51-ui-sound-effects-buttons-switches-and-clicks) (Kenney) `switch7.wav`, CC0; [100-cc0-metal-and-wood-sfx](https://opengameart.org/content/100-cc0-metal-and-wood-sfx) (rubberduck) `metal_hit_03.ogg`, CC0 | slowed to 0.7-0.75, layered, lowpassed |
| ui_confirm | `tools/audio/gen_sfx.py` (synthesised, ours) | none |
| ui_deny | `tools/audio/gen_sfx.py` (synthesised, ours) | none |
| weapon_charged | `tools/audio/gen_sfx.py` (synthesised, ours) | none |
