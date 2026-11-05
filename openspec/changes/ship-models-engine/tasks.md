# Tasks: ship-models-engine

## 1. This change (2026-11-04)

- [x] 1.1 `tools/mockups/export_ships.mjs` with the vendored GLTFExporter and GLTFLoader; `assets/ships/README.md`
- [ ] 1.2 The full export against the Concord page and the Krai page v19, run twice for byte-identical output, the contact sheet looked at
- [x] 1.3 `model` on the classes, validated; `BattleView` draws the glb at the class's battle length
- [x] 1.4 `hull.gdshader`, the mockups' tri-planar plating and the Krai livery
- [ ] 1.5 The hull shader applied from the manifest (detail tiles, origin offset, livery values)
- [ ] 1.6 A test that every class's model is in the manifest and its export is not stale (the page's sha256)
- [ ] 1.7 Stills of both lines in battle, looked at beside the mockups

## 2. Still different from mockups 6 and 7

- [ ] 2.1 Battle damage: holes into the rooms, scorch marks, venting, nacelles shot off, the hull breaking apart
- [ ] 2.2 Lamps lit from the named empties (floods, navigation lights, strobes, beacons), baked where they can be
- [ ] 2.3 Windows with a room behind them
- [ ] 2.4 Triangle budgets once mockup 3 sets them

## 4. Own unwrap and baked AO (owner, 2026-11-05)

The owner: "every ship needs custom uv Unwraped textures. From NOW on . ALL SHIPS MUST AVOID REPETTIVE TILE FOR MAIN COLOR
MAP", "tile textures are for DETAIL only, we'll probably need a detail mask in the future, same goes for Concord and Krai
ships which will need more work done as well", "dont forget to bake AO as well" (CLAUDE.md section 10; Ship Design Bible
chapter 15). Not started: the Ssarith Hunt goes first.

- [ ] 4.1 Each class unwrapped on its own and its main colour map painted onto its own UVs (`painted-bake`, ship mode), replacing the repeating plating tile as the main colour
- [ ] 4.2 AO baked onto each class's own UVs and kept as its own map
- [ ] 4.3 The plating tile kept as detail only, under a detail mask (to be designed)
- [ ] 4.4 Every hull checked against Ship Design Bible chapter 14 (one grown structure, no placed primitive)
