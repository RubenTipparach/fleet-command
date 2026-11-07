# Tasks: freighters

## 1. The write-up and the board (this change)

- [x] 1.1 Proposal, design and spec delta, citing the SFC3 manual's printed pages 37 and 42
- [x] 1.2 Mockup 24, the freighter board (`docs/mockups/freighter-concepts.html`): eighteen grown hulls, three a faction,
      each naming its tradition, at true scale beside the Petrel and a person, measured; on `mockup-work.md` and the tracker
- [x] 1.3 Survey section V (V1-V6) in `docs/design/survey.html`
- [ ] 1.4 The owner's picks (survey V2) and the traditions kept (V1)

## 2. The picks (after the owner picks: approved for build)

- [ ] 2.1 Each pick grown again at its size on its faction's own section (the Institute's `ksec`, the Hunt's `hsec`, the
      Concord's octagonal loft); new sections written for the Kethran and the Vos
- [ ] 2.2 Detailed and baked onto its own UVs (`ship-detailing`, `painted-bake`): plates, windows a person tall on the
      lived-in decks, the faction emblem, hull lights
- [ ] 2.3 Exported by `tools/mockups/export_ships.mjs` with named nodes (drive, point defence, holds as damage zones)
- [ ] 2.4 One cradle: the board's `cradle` and the Institute page's `loopOf` become one function in
      `docs/mockups/lib/hull-grow.js`, inlined by both pages (design, "One copy")

## 3. The game (after approval for implementation, a separate request)

- [ ] 3.1 A freighter battle class for each faction in `data/battle/classes.json`, with its model; the campaign's
      `freighter` hull takes its faction's class in place of `kff`
- [ ] 3.2 The campaign map draws each faction's freighter in place of the placeholder (`MapPieces.cs`, `game-flow.html`)
- [ ] 3.3 Convoy defense, Convoy ambush and Distress field the faction's own freighters; a playtest of each from both sides
