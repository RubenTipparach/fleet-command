# Tasks: nebula-shells

## 1. Mockup

- [x] 1.1 Proposal, design and spec delta
- [x] 1.2 Mockup 40, `docs/mockups/nebula-shells.html`: columns and decks from `data/space/plumes.json` and
  `data/space/nebula_decks.json` (inlined), surface nets, 1-4 shells, grid, shading, blend, edge fade, wireframe, cost
  readout
- [x] 1.3 Shots at desktop, phone and portrait sizes in `docs/screenshots/mockups/nebula-shells/`, looked at; dom fit passes
- [x] 1.4 Published, and recorded in `mockup-work.md`, the tracker and `docs/artifacts/nebula-shells/`
- [ ] 1.5 Survey questions NS1-NS4
- [ ] 1.6 The owner's review (approval for build, then for implementation)

## 2. In the game (once approved for implementation)

- [ ] 2.1 The column and deck density fields in Core, with tests that pin them at fixed points
- [ ] 2.2 Surface nets in Core, tested on a sphere (a closed, watertight mesh within half a cell of the true surface)
- [ ] 2.3 `NebulaShells` in the view, built at load on a worker thread; `style: "shells"`; `--lowspec` picks it
- [ ] 2.4 Heavy-fleet on the Pi 5 with `--lowspec`: it plays, with its FPS recorded
