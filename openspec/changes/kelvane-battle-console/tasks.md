# Tasks: kelvane-battle-console

- [x] 1.1 Proposal, design and spec delta
- [x] 1.2 Kit pieces in Blender: `frame.screen`, `inset.screen`, `strip.*.screen`, `nameplate.console`, `plate.title`,
  `console.backing`, `key.*`, `keyw.*`, `cell.*` and `bar.cell.ok`. Fit-checked, with rule B verified on the renders.
- [x] 1.3 `lib/kelvane.js` with every field the battle console reads; the skins page module thinned onto it
- [x] 1.4 The Kelvane faction, and the Patience in the allied and swap scenes (key 4)
- [x] 1.5 The page written by `skin_page.py`; every press matches the Concord's (parity)
- [x] 1.6 Strip meters tied to hull integrity (top) and speed (bottom), drawn live (D8)
- [x] 1.6a Shots looked at: battle, expanded panels, hover card, menu, swap and allied hops
- [x] 1.6b The emblem: the hydrogen 3d z² eigen plot on the crest, the name plates and the live emblem (D9)
- [ ] 1.7 The owner's review; K1-K3
- [ ] 1.8 Re-render mockup 4's ten `icon.*` pieces from the icon guide's subjects as `lib/kelvane.js` draws them, so the
  Institute has one picture per subject everywhere (D6)

## 2. In the game (owner, 2026-11-06: "VERIFY ALL FACTION UI", "please add ALL systems that are IN the mockup")

- [x] 2.1 `src/Game/Ui/Kit/KelvaneKit.cs`, a port of `lib/kelvane.js`: every rendered piece from
  `assets/ui/kelvane/manifest.json` with the console's screened pieces standing in under the kit's names (`CONSOLE`), the
  colour roles, Share Tech Mono (`assets/fonts/`, its source in `SOURCES.md`), the 24 schematic icons, rounded drawn keys
  and round knobs, the key and tab fields, icon tabs on the window key, title placards raised 2 px, lit-cell bars, the
  72 px strips; registered in `Kits.cs`
- [x] 2.2 The strip meters (D8): needles drawn live over the printed scales, hull integrity on the top strip and speed on
  the bottom, settling at 4 per second and named on hover (`ConsolePanel.StripMeter`, `SkinStyle.Meter`)
- [x] 2.3 `console.backing` behind the column and an expanded panel
- [x] 2.4 The Kelvane faction with its skin (`data/battle/factions.json`); the Patience in the Four races scene on a
  stand-in class (`kelvane_rv`, no model), the Institute having no designed ships, as the mockup has it
- [x] 2.5 The skin parity check and the layout test walk every skin, the Institute's included
- [ ] 2.6 Stills of the game beside mockup 20's shots, looked at; differences into `docs/design/mockup-sync.md` (a desktop
  session)
- [ ] 2.7 Not yet in the game: the live readouts drawn by the mockup's painters (the scope trace of the ship, the radar's
  decaying trace, the scope cursors, STOP on the helm scope), the emblem (D9) where the console draws one, the hover name
  on the lit wide key (`tip`), the skin's own names (`player`)
