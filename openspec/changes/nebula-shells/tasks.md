# Tasks: nebula-shells

## 1. Mockup

- [x] 1.1 Proposal, design and spec delta
- [x] 1.2 Mockup 40, `docs/mockups/nebula-shells.html`: columns and decks from `data/space/plumes.json` and
  `data/space/nebula_decks.json` (inlined), surface nets, 1-4 shells, grid, shading, blend, edge fade, wireframe, cost
  readout
- [x] 1.3 Shots at desktop, phone and portrait sizes in `docs/screenshots/mockups/nebula-shells/`, looked at; dom fit passes
- [x] 1.4 Published, and recorded in `mockup-work.md`, the tracker and `docs/artifacts/nebula-shells/`
- [ ] 1.5 Survey questions NS1-NS4
- [x] 1.6 The owner's review: approved for implementation, 2026-11-12 ("Nebula shells. The cube marched, generate once static mesh")

## 2. In the game (once approved for implementation)

- [ ] 2.1 The column and deck density fields in Core, with tests that pin them at fixed points (for now they are the view's: NebulaPlume.Cover and DeckField, which the puffs share)
- [x] 2.2 Surface nets in Core, tested on a sphere (a closed, watertight mesh within half a cell of the true surface) (PR #84)
- [x] 2.3 `NebulaShells` in the view, built at load (0.28 s on the Pi, on the main thread for now); `style: "shells"`; `--lowspec` picks it (PR #84)
- [x] 2.4 Heavy-fleet on the Pi 5 with `--lowspec`: it plays, 16 fps (no nebula 27, billboards 5) (PR #84)
