# Design: ship-models-engine

The approved ships (mockup 6, the Concord line; mockup 7, the Krai Warhost line) in the game's battle.

## 1. One implementation: export from the mockups' own builders

The ships' geometry is defined by the mockup pages (`docs/mockups/lib/concord-line.js`, `ship-plan.js`, `ship-skin.js`
and the Krai page's `buildKrai`). A Blender port would be a second copy of every hull rule, sure to drift from the
approved pages (CLAUDE.md section 6.1). So `tools/mockups/export_ships.mjs` loads each page headless, lets it build its
ships intact, and exports them with three.js r147's GLTFExporter (vendored, unmodified). This is CLAUDE.md section 10's
"meshes are files built by committed generators", with the generator being the mockup itself; section 14 records it.

- Re-running the exporter is the only step when a mockup changes. Each ship's manifest entry records the page, its
  published version and the page file's sha256, so a test can tell a stale export.
- Deterministic: a second run gives the same bytes.
- Conventions (`assets/ships/README.md`): metres, +Y up, bow toward -Z, port toward -X, origin at the bounding box's
  centre; one node per mockup piece, meshes merged by zone and material; named empties for mounts, engines, the
  deflector, reactor, bridge, bays and every lamp, with their facts in glTF extras.
- Shader-only looks cannot export: the hull's tri-planar detail and the Krai livery, the windows' rooms, the bells'
  heat. Their materials get plain stand-ins, and the manifest records what the page's shader did and each ship's values.

## 2. In the battle

- A class's `model` (`data/battle/classes.json`) names its glb. `BattleView` loads it and scales it so its length is the
  class's battle length (the framing the battle console mockup uses for every ship).
- `hull.gdshader` ports the mockups' hull shader: the faction's detail tile projected from three sides in the mockup's
  ship frame (positions turned back through the manifest's origin offset), the seams darkened and tinted, flat shading,
  and the Krai livery from the same formula as the page. It is applied to the hull materials the manifest marks.
- Kethran and Vos classes have no approved ships and keep their stand-ins.

## 3. Budgets

The mobile triangle budgets come from the look-dev mockup (mockup 3), not written yet. The manifest records every ship's
triangles so the budget check can be added when the numbers exist.

## Open questions

| # | Question | Provisional answer |
|---|---|---|
| 1 | Which nacelle arrangement does each Concord class fly in battle? | The battle console mockup's: Swift level (FF), Petrel level (DD), Albatross (CA); all eight variants are exported |
| 2 | Do the Condor battleship and the Pelican carrier get battle classes? | Yes, when a scenario needs them |
