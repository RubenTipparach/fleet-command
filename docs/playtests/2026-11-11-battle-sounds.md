# Playtest 2026-11-11: the battle's sounds

**Superseded on merge (PR #78):** main already played the battle's sounds (`BattleSounds`, `BattleAudio`,
`data/audio/events.json`, PR #75), so the second implementation this report counts (`BattleSoundRules`,
`BattleView.Sounds.cs`, `data/audio/battle.json`) was dropped at the merge (CLAUDE.md 6.1). Kept as the record of the run.

- Commit: the battle sounds change (committed with this report), on the merge of main after
  RubenTipparach/fleet-command#46.
- Machine: cloud session, Linux, Godot 4.7.1 .NET headless (`--headless --fixed-fps 30 --resolution 1920x1080`). A cloud
  session cannot record or listen: this counts the sounds played (`PLAYTEST_SOUND <cue>`), not how they sound.
- What: the owner's "ok implment it" after the shield hit's fix: every weapon firing and every hit landing, placed in 3D
  (`src/Game/View/BattleView.Sounds.cs`; which event plays which: `data/audio/battle.json`).

| Run | Result | Sounds played |
| --- | --- | --- |
| `battle` as `concord_battle` | ok | lance_fire 209, pd_burst 140, hull_hit 58 and hull_hit_short 94, disruptor_fire 52, torpedo_launch 36, shield_hit 32 and shield_hit_short 9, missile_launch 26, torpedo_detonate 7 and torpedo_detonate_short 7, ship_destroyed 3; ui_chirp 4; amb_bridge (the bed) |
| `battle` as `krai_battle` | ok | lance_fire 210, hull_hit 96 and hull_hit_short 24, disruptor_fire 90, pd_burst 84, shield_hit 44 and shield_hit_short 7, missile_launch 26, torpedo_launch 13, torpedo_detonate 4 and its short 1, ship_destroyed 4; ui_clunk 6; amb_bridge |

Every weapon kind on both sides fires with its sound, every hit lands with one by what it struck, the short alternatives
take over when a ship is struck again within half a second, and each destroyed ship plays its full explosion. Not
covered: how loud a fleet battle is in the owner's ears (209 lance shots in one battle is busy; the 24-voice cap holds
it), and the engines, thrusters, reactor, shields rising and failing, and the alerts, which are not wired yet (task 3.9).

The whole suite after the change (`bash scripts/playtest.sh`): `PLAYTEST_SUITE 127/127 ok`; `scripts/run-tests.sh`: 399 Core
tests, the layout test and skin parity (47 states, 12876 controls) pass.
