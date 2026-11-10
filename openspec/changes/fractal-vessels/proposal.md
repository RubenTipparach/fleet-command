# Proposal: fractal-vessels

## Why

The owner, 2026-11-10: "a faction is designed to be a fractal type of life form. and interdimensional being. what would
that look like in our game? can we build raymarched fractal ships?" Then: "do the mockup yo", "dont worry about phone",
"I dont really need this stuff to work on a phone", "three js mockup lets go".

Every ship in the game so far is a mesh. A fractal has detail at every scale, and a mesh of one would need millions of
triangles or would lose what makes it a fractal. Raymarching draws the shape straight from a formula. Each pixel steps
along its ray by the distance the formula says is safe, until it reaches the surface. That also suits an
interdimensional being: a 4D fractal can be sliced into our three dimensions, so the vessel changes shape as the slice
moves, without moving through our space.

## What Changes

- **Mockup 37, Fractal vessels** (`docs/mockups/fractal-vessels.html`), desktop only. It has three raymarched vessels, one
  for each school of the Axiom's draft culture (survey N1):
  - a 4D quaternion Julia set sliced into 3D (the escape-time school);
  - a chain of folded Menger blocks (the tilers);
  - a branching tree (the branchers).
- The Concord's Petrel is beside them at true scale. It sorts in depth against each vessel, as a mesh ship would in the
  game.
- **Controls**:
  - shape: the slice through the 4th dimension (or the fold turn, or the branch angle), drift, damage, glow and halo;
  - cost: fractal iterations, march steps and render scale.
  - A frame-time readout goes with them, so the owner can judge the cost on the RTX 3070.
- **Damage** is drawn the way a distance field allows. The vessel loses fold levels as it is hurt, so it grows simpler,
  and holes are cut out of the field, with the being's light leaking round their edges.
- **The questions** go on the survey (section FV): whose vessels these are, how the culture frames them, which forms to
  keep, and how phones draw them.

## Impact

- Nothing in the game changes until the owner approves the mockup for implementation.
- If it is built, it brings three things:
  - a raymarched ship material in Godot (a box proxy writing depth);
  - the same distance functions in C#, for hits;
  - documented exceptions to the mesh, UV and bake rules (CLAUDE.md section 10), which a field has no use for.
- The Axiom's placeholder line (mockup 36, in the game) stays until the owner chooses. Fractal vessels could replace it,
  or could belong to a new faction (survey FV1).
