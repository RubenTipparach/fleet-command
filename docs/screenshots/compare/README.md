# Mockup and game, side by side

Each sheet puts the battle console mockup (v65, `docs/mockups/battle-console.html`, its named shot at 1920 x 1080) on
the left and the game (Godot 4.7 C#, `scripts/game-stills.sh`) on the right. Both play the skirmish from the same seed
for 600 steps with the target locked and the ship intercepting, then fire. The game's stills are rendered headless with
Mesa's software renderer under Xvfb.

Remake them:

```bash
SHOOT_SIZES=desktop NODE_PATH=$(npm root -g) node tools/mockups/shoot.mjs docs/mockups/battle-console.html /tmp/mock battle,power,shields,power_expanded,ship_expanded,weapons_expanded,menu
```

```bash
scripts/game-stills.sh && python3 tools/compare/side_by_side.py /tmp/mock docs/screenshots/game docs/screenshots/compare
```

## Differences that remain (2026-11-03)

| Where | Mockup | Game |
|---|---|---|
| Ships | Stand-in ship models of the mockup | Stand-in primitives (`scenes/standins/`); mockups 3 and 5 set the real ones |
| Backdrop | Nebula sky with streaking dust | A simpler warm haze and stars |
| Power | Reactor and Warp pages, coolant pumps (2 E), a Warp row | Reactor and Warp tabs shown disabled; no coolant draw, so Used reads 19, not 21 |
| Bottom strip | The warp key charges and jumps | The warp key is drawn offline and does nothing |
| Radar row | Map opens the tactical map | Map is disabled (the tactical map is not built) |
| Target box | Holder's emblem, boarding tug of war, Target view and Camera lock keys | The target's systems display and the release X only |
| View | Scene picker, Reset, the mockup badge (mockup tools) | Left out: they are tools of the mockup, not the game |
| Other stations and ship pages | Tactical, point defence, science, comms, repair, transport, fleet; sensors, tractor, craft | Show "Not yet in the game" |
| Battle state | The mockup's own stand-in battle | The game's simulation: same seed and rules, so positions are close but not identical (for example Ashkar's distance) |

## The Ssarith Hunt and the Kelvane Institute (2026-11-06)

`ssarith-*.png` put mockup 15 beside the Hunt's Battle (`ssarith_battle`); `kelvane-*.png` put mockup 20 beside the
Concord's Battle worn in the Kelvane skin (`--skin=kelvane`: the Institute has no ships of its own yet). What still
differs is listed in each change's tasks (`openspec/changes/ssarith-console-engine/tasks.md`,
`openspec/changes/kelvane-console-engine/tasks.md`, section 2).
