# Tasks: ssarith-battle-console

- [x] 1.1 Proposal, design and spec delta
- [x] 1.2 Kit pieces in Blender: key.*, keyw.*, cell.*; fit-checked; rule B verified
- [x] 1.3 `lib/ssarith.js` with every field the battle console reads; the skins page module thinned onto it
- [x] 1.4 The Hunt stalker class and its geometric model; the ship display's outline
- [x] 1.5 The page written by `skin_page.py`; every press matches the Concord's
- [x] 1.6 Tabs without the tally, labels and icons inside the tube; `plate.title` placards; `console.backing` behind
  the column and expanded panels (and the Krai's backing); the rule in CLAUDE.md section 7
- [ ] 1.7 The owner's review; H1-H3

## 2. In the game (owner, 2026-11-06: "VERIFY ALL FACTION UI", "please add ALL systems that are IN the mockup")

- [x] 2.1 `src/Game/Ui/Kit/SsarithKit.cs`, a port of `lib/ssarith.js`: every rendered piece from
  `assets/ui/ssarith/manifest.json`, the claw-mark ok cell turned to bone, the colour roles, VT323 and Black Ops One
  (`assets/fonts/`, sources in `SOURCES.md`), the 24 icons in the Hunt's hand, cut-cornered drawn keys and octagon knobs,
  the tab field, icon tabs on the tube key, title placards, symbol bars, the 64 px strips; registered in `Kits.cs`
- [x] 2.2 `console.backing` behind the column and behind an expanded panel (`ColumnBacking`, `SkinStyle.Backing`), the
  Krai's backing behind its expanded panels too
- [x] 2.3 The Ssarith a side of their own (`data/battle/factions.json` skin `ssarith`): Battle, Duel and Arming as the Hunt
  in its approved ships (`ssarith_battle`, `ssarith_duel`, `ssarith_arming`), each under a sky of its own
- [x] 2.4 The skin parity check and the layout test walk every skin, the Hunt's included (`scripts/run-tests.sh`)
- [ ] 2.5 Stills of the game beside mockup 15's shots (`tools/compare/side_by_side.py`), looked at; differences into
  `docs/design/mockup-sync.md` (a desktop session: the stills need a window)
- [ ] 2.6 Not yet in the game, as for the Krai: the skin's live readouts drawn by the mockup's own painters (the ship
  display's phosphor outline, the radar sweep, the tracking brackets, the helm's arrows); the hover name on the dark tube
  (`tip`); the skin's own officer and ship names (`player`)
