# Proposal: battle-console-engine

## Why

The owner approved the battle console mockup (mockup 2, `battle-console`, version 65) for implementation and chose the
language, 2026-11-03: "based on the ui mockup start implmenting this ui stuff in game, we have chosen Godot 4.7 with c#
so begin integration now."

The mockup is a three.js page with its own stand-in battle. This change builds the same console in the game, in Godot
4.7 with C#, on a simulation that runs and is tested without a scene (CLAUDE.md section 6.2).

## What Changes

- **The stack moves to C#**: the Godot .NET build and the .NET 8 SDK, pinned in `build.config`, installed by
  `scripts/install-godot.sh` and `scripts/install-dotnet.sh`. CLAUDE.md section 2 records the owner's decision.
- **`src/Core`** (plain .NET, no Godot): the data loader with strict validation, the stand-in battle (movement, energy
  allocation, shields, weapons with one hit-chance rule, damage, point defence, AI wingmen and enemies, the captain's
  orders) and the console's layout model. Tested headless with xUnit (`tests/Core.Tests`), with a golden seed.
- **`data/`**: weapons, classes, factions, the skirmish, tuning and the console layout, ported from the mockup's tables.
- **`src/Game`** (Godot): the composition root, the Concord kit drawn in vector from the kit's named pieces, the console
  panels (name plate, stations with Helm and Power built, ship panel with the systems display and shield keys, weapons,
  radar, both strips, menu, hover cards), the 3D view with stand-in ships, the helm by pointing, targeting, and every
  panel expanding with the live battle in the right third.
- **Checks**: `scripts/run-tests.sh` (headless tests, the build, the layout test in the engine) and
  `scripts/game-stills.sh` (named stills under Xvfb).
- Not in this slice: the other seven stations, the sensors, tractor and craft pages, the reactor, warp, transport,
  boarding, the tactical map, the Target box, phone layouts, real ship models and sound. Each follows as tasks here.
