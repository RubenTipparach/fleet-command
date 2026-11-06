# Tasks: fleet-skins

- [x] 1.1 Proposal, design and spec delta
- [x] 1.2 Every skin inlined in the one source page; `bindSkin`, `applySkin`, `skinOf`, a kit per skin
- [x] 1.3 Allied factions, the Allied fleet scene and its shots
- [x] 1.4 Officers for every ship you can command; rank names from the skin worn
- [x] 1.5 `skin_page.py` simplified; the Krai and Ssarith pages written; parity holds
- [x] 1.6 The Three races scene (owner, 2026-11-04: "create a new test scenario for me to swap between three ships of
  different races all under my control") and keys 1, 2 and 3 for the ships of your side; its shots
- [ ] 1.7 The owner's review; F1-F3

## 2. In the game (owner, 2026-11-06: "VERIFY ALL FACTION UI", "NEEED ALL THE DEMO MISSIONS IN HERE")

- [x] 2.1 The console follows the ship you command: the skin of the commanded ship's faction (or the scenario side's, for a
  faction without one), swapped with its column and strips when command passes (`Main.SkinOf`, `UseKit`, `FollowCommand`)
- [x] 2.2 Keys 1 to 9 take the n-th ship of your fleet (`command_1` to `command_9` in `project.godot`,
  `GameActions.Command`), beside the Fleet key on the name plate
- [x] 2.3 The Allied fleet (a Concord, a Krai and a Ssarith ship against the mockup's Kethran and Vos) and Four races (the
  same three and the Kelvane Patience, a far Kethran picket holding fire) for each side, in `data/battle/scenarios.json`,
  each under its own sky; the Kelvane ship is not in the Allied fleet (no stand-in you can be sent to fight in)
- [x] 2.4 The parity check proves it: in every scenario whose fleet holds more than one faction, keys 1 to n give command
  of each ship and the console and its column wear that ship's skin (`FOLLOW` lines)
- [x] 2.5 Playtest scripts `allied` (hop, then fight) and `hop` (every ship, no battle); their runs to add to
  `data/playtests/suite.json`
- [ ] 2.6 The officers of every ship you can command and their ranks in the skin worn (1.4) are not in the game yet
