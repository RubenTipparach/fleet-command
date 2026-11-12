# Proposal: nebula-shells

## Why

The owner, 2026-11-12, after the Raspberry Pi 5 low-spec runs ("Make a lowres simplified version of this game (nebula just
standard fog and billboards) and see how well the game runs on a pi5"): "for the nebulas, can you generate some sort of cube
marched geometry with transparency?", then "youre just generating statick unlit geomtery, so it shjouldnt freeze", and "the
top and bottom decks of clouds need to be lowpoly generated too".

On the Pi the heavy-fleet battle never draws a frame: its main thread spends minutes inside Mesa's V3D shader compiler
right after the planet and nebula shaders load (`docs/playtests/pi/README.md`). The marched nebula (`nebula-plumes`,
style `volume`) evaluates layered noise per pixel. A nebula made of geometry does that work once, on the CPU, when the
battle loads, and draws with a trivial shader. A test on the Pi drew three such shells (20,714 triangles) at 85-104 fps at
1280x720 (`docs/playtests/pi/simple-scene/nebula.gd`).

## What Changes

- **A fourth nebula style, `shells`**, beside `volume`, `meshes` and `billboards` (`data/space/nebula_decks.json`'s
  `style`): each plume's column and the two decks above and below the battle plane are polygonised once, when the battle
  loads, into a few nested see-through shells at rising density.
- **Same shapes, same data**: the columns and decks come from the plumes' own numbers and looks
  (`data/space/plumes.json`, `data/space/nebula_decks.json`), so the shells are a way of drawing the existing nebula, not a
  new nebula.
- **Static geometry, a trivial shader**: no noise per pixel, one draw per shell, sun shading per vertex, edges faded by how
  face-on they are.
- Mockup 40, `docs/mockups/nebula-shells.html`, shows it with its cost (triangles, build time, draws).

## Impact

- The view only. The simulation, the plume rules (half chance to hit, half range, lost locks) and the ion storms do not
  change.
- Game code waits for the mockup's two approvals (CLAUDE.md section 4).
