# Playtest 2026-11-07: the Kelvane Institute in the game

- **Commit:** the branch `claude/vigilant-keller-s93pth` at the commits adding the Institute to the game (change
  `kelvane-ships-engine`)
- **Machine:** a cloud session, Godot 4.7.1 .NET on Linux; headless at a fixed 30 fps for the suite, Xvfb with Mesa's
  software renderer for the stills. No frame time measured (CLAUDE.md section 9)
- **Not recorded with OBS:** a cloud session cannot record. No Drive link

| Beat | Outcome |
|---|---|
| The Institute, from the Concord's side | The Institute's battlecruiser, frigate, destroyer and carrier drawn from their own baked maps; lost at 178 s, three Institute ships left (the placeholder figures favour the Institute's squadron) |
| The Institute, from the Krai's side | Lost at 168 s, four left |
| Institute wrecks, Concord | All five hulks broken up in 124 s |
| Institute wrecks, Krai | All five broken up in 192 s |
| The Institute's Battle, in the Patience | Won at 195 s, the Institute's own console |
| Its Duel and Arming | Won at 66 s and 216 s |
| Its Four races | Hops ship to ship (the `hop` script), fighting at 5 s as it ends |
| The damage ranges | 17 targets with the Institute's row, fighting (a range never ends) |
| Every surface dressed | `SHIP_SKIN kelvane/<id>.glb: hull shader on 1 surfaces`, 7 to 11 lamps with a glow each, and the frigate 188, destroyer 154, cruiser 122, battlecruiser 242 and carrier 336 windows and bays; the fighter 8 lamps, no windows |
| The carrier's fighters | On its pads in the well, one a fighter in the bay (12 of 24), upside down on the ceilings (`institute-carrier-pads.png`) |

Suite: `PLAYTEST_SUITE 58/58 ok` for the scenes before this change, then `8/8 ok` for the Institute's eight; 66 runs in
`data/playtests/suite.json`. `scripts/run-tests.sh`: 301 of 301 tests, the build, the import check, the layout test and
skin parity (45 states, 12450 controls) pass.

Stills (`docs/screenshots/game/`): `institute-battlecruiser.png`, `institute-carrier.png` (The Institute, Target view),
`institute-range.png` (the Patience), `institute-frigate.png`, `institute-carrier-pads.png` (the damage range),
`institute-wrecks.png`, `kelvane-patience.png` (the Institute's Battle, its console). Beside the mockup's shots in
`docs/screenshots/compare/institute-*.png` (`tools/compare/side_by_side.py`): the same hulls, maps, windows and craft; the
game's battle light is brighter than the page's studio, as for every line.

Still open (change `kelvane-ships-engine` section 2): the Institute's own weapons and figures, its ships' names and
captains, every pad filled, its campaign.
