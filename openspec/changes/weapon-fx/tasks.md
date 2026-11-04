# Tasks: weapon-fx

## 1. The write-up and the mockup

- [x] 1.1 Proposal, design and spec delta
- [x] 1.2 `docs/mockups/lib/weapon-fx.js`: the pipeline, the phone quality and every effect in the design's table
- [x] 1.3 The weapons range (mockup 16) uses it; named shots at the three sizes, looked at
- [ ] 1.4 The owner's review of mockup 16, and survey X6, X7
- [ ] 1.5 The battle console mockup's lances, disruptors and torpedoes drawn with `ribbon`, `flare` and `trail` (both
      skins), once the look is approved

## 2. Beams and bolts keep their ends on their ships (the owner's rule, done now)

- [x] 2.1 Battle console mockup: `shotEnds` and `boltStep`; beams drawn from it; point defence beams from their mount to
      the shot (v76; the Krai page v15 written from it by `tools/mockups/skin_page.py`)
- [x] 2.2 The game: `Battle.Ends`, the bolt on the live line, the overlay drawing beams from it
- [x] 2.3 Headless tests for both; the golden seed re-pinned (disruptor bolts land at other moments)
- [ ] 2.4 A playtest beat that watches a beam while both ships turn (desktop session, OBS)

## 3. The game (after approval for implementation, a separate request)

- [ ] 3.1 The effects as Godot shaders and meshes (the design's table), the phone quality on the Compatibility renderer
- [ ] 3.2 Scars in `hull.gdshader`
- [ ] 3.3 Frame time measured on the RTX 3070 and the owner's phone (T3: 60 fps at the phone layout)
