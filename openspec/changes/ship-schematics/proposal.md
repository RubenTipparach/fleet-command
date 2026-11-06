# Proposal: ship-schematics

## Why

The owner, 2026-11-05: "we also need to generate schematics for each ship, top down view - avoid triangles, use quads to
render wireframe schematic, shade hull darker than wireframes. make sure to use 2 differnt layers both white so we can
recombine them for each ui skin. I want these to be used in game, and in store to preview the kinds of ships you are buying
our outfitting". The reference is the systems display of the 1999 game: the ship from above as a glowing wireframe over a
darker hull. The battle's systems display drew a stand-in outline by hull shape.

## What Changes

- **`tools/mockups/ship_schematics.py`** renders every exported ship glb (`assets/ships/`) from directly above, nose up:
  - triangles are paired into quads, and the diagonal of a pair is never drawn;
  - it writes two layers per ship, both white with the drawing in alpha: `<id>.hull.png` (the hull, shaded by a top
    light, alpha 0.15 to 0.55) and `<id>.wire.png` (the quad edges seen from above, full alpha);
  - it writes `manifest.json`, with each picture's scale, origin and source glb with its sha256.

  Deterministic: a second run gives the same bytes.
- **`Schematics.cs`** (the game) tints the hull layer with the skin's dark colour and the wire with its bright one, so one
  picture serves every skin. It draws them in:
  - the battle's systems display and Target box;
  - the campaign's shipyard (every hull for sale);
  - the fleet and the outfitter.
