# Playtest 2026-11-07: console keys and the bridge bed

- Commit: d063cc4e plus the PLAYTEST_SOUND logging, committed with this report
- Machine: cloud session, Linux, Godot 4.7.1 .NET headless (`--headless --fixed-fps 30 --resolution 1920x1080`).
  A cloud session cannot record or listen: this checks that the sounds are played, not how they sound.
- What: the owner's "Console keys - this should be implemented in the game" and "use amb_bridge_brown", "put in game now"
  (`src/Game/Ui/ConsoleSounds.cs`, `data/audio/cues.json`). In a scripted run every sound played prints
  `PLAYTEST_SOUND <cue>`.

| Run | Result | Sounds heard |
| --- | --- | --- |
| `battle` as `concord_battle` | ok, won at 145 s, 14 beats | `amb_bridge` (the bed, once, at the battle's start); `ui_chirp` x4, one for each console key the captain pressed |
| `battle` as `krai_battle` | ok | `amb_bridge` (the bed); `ui_clunk` x5, the Krai skin's key sound |

The key sound follows the skin of the ship commanded (the Concord's chirp, the Krai's clunk), every press on a console
control that acts plays one (drags do not), and the bed starts under the battle. No audio errors in the engine log.
Not covered: how they sound in the game (the owner's ears, on Windows), and the menus' and map's silence of the bed
(the flow runs are in the suite).

The whole suite after the change (`bash scripts/playtest.sh`, every run in `data/playtests/suite.json` from both sides and
the campaign): `PLAYTEST_SUITE 58/58 ok`, so the click hook changed nothing the captain does.
