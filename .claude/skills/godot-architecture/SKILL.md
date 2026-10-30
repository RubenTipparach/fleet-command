---
name: godot-architecture
description: Decide where Fleet Command code goes and how a Godot feature is structured - the folders (a headless simulation library, thin scenes and scripts, data, tools), the composition root, fixed-step simulation and determinism, input actions shared by players and the scripted captain, the console layout with fixed-size panels that expand to full screen on phones, and the extension points. Use before starting any new feature or system (movement, energy, shields, weapons, damage, AI, a panel, the 3D view, a mission), when adding a scene, autoload or folder, or when asking "where does this go" or "how should this be structured in Godot".
metadata:
  author: Fleet Command (Claude Code), after the-federation's simulation library and CLAUDE.md, and Crab Breeze's bevy-architecture
  version: "1.0"
---

# Godot architecture

The stack is Godot 4.7.1-stable with statically typed GDScript (owner, 2026-10-30: "I want to do
godot for this"), pinned in `build.config`. Read the 4.7 class reference and migration notes
before writing engine calls; never trust remembered APIs across versions. the-federation
(`RubenTipparach/the-federation`) is the reference project built this way: read its `src/sim/`,
`tests/run_tests.gd` and `tests/layout_test.gd` before inventing a pattern it already has.

## Folders (dependencies point downward only)

```
scenes/        .tscn files: the battle, the console, each panel, the 3D view, ships, effects
src/ui/        thin scripts for UI scenes: wire controls to the sim and to input actions
src/view/      thin scripts for the 3D view: place models, play effects, follow the camera
src/game/      the composition root (battle bootstrap), autoloads, input, the scripted captain
src/sim/       the simulation: plain RefCounted GDScript, no Node, no scene tree
data/          JSON: ships, weapons, factions, missions, input bindings, playtests (units in keys)
assets/        glb models, textures, audio, fonts (what Godot imports)
tools/         Blender, Material Maker, music and mockup scripts (Python, shell, Node)
tests/         headless test runner and the layout test
scripts/       install, test, build and export entry points shared by CI and people
```

- **`src/sim/` never touches a `Node`, the scene tree, rendering, audio, input or `Time`.** It runs
  under `godot --headless --script` with no scene and is tested that way (the-federation 5.2). It
  owns movement, energy and allocation, shields, weapons and arcs, the hit roll, damage, AI and
  stable ids.
- **The simulation is authoritative.** Panels and the 3D view read its state. They never keep
  their own copies of a number they display.
- **Scripts on nodes are thin.** They forward input actions to the simulation and copy sim state
  into controls and transforms. If a scene script grows a rule, the rule moves into `src/sim/`.

## The battle loop

- **One composition root** (`src/game/battle.gd`) builds the simulation from data and a seed,
  hands it to the view and the console, and owns the step loop. Nothing else constructs sim objects.
- **Fixed steps in seconds.** The sim advances by a fixed `step_s` (data) from `_physics_process`
  or an accumulator, never by frame time. Rendering interpolates between steps.
- **Determinism** (CLAUDE.md 6.3): one seeded `RandomNumberGenerator` per subsystem, forked from
  the battle seed and a purpose name, never the global `randf()`. Iterate ships and mounts sorted
  by stable id. `is_finite()` guards at every entry. A battle replays from `(seed, inputs)` exactly,
  and a golden-seed test pins it.
- **Events** go out as signals from the root (`ship_destroyed`, `weapon_fired`, `shield_hit`) so the
  view, sound and console react without reaching into one another.
- **Input is actions** (`InputMap`, bindings in `data/input.json`): `helm_turn_left`,
  `fire_group_1`, `arm_overload`, `target_next`. Keyboard, mouse, touch and pad map to the same
  actions. The scripted captain (`playtest`) injects the same actions through
  `Input.parse_input_event`, so it is checked exactly as a player is.

## The console (CLAUDE.md section 7)

- **Layout is authored**: one console scene at a fixed reference resolution (1920 x 1080 on desktop,
  844 x 390 on a phone, provisional) with a
  left column, a top strip, a bottom strip and a `SubViewportContainer` for the 3D view. Every panel
  is a fixed rectangle: `custom_minimum_size` equal to `custom_maximum_size`,
  `propagate_maximum_size`, `clip_contents`, runtime text with `clip_text` and trim with an ellipsis
  (the-federation 6.4, which has the reasons and the two-frame test).
- **Expanding a panel** is a state of the console scene, not a second copy of the panel: the
  same panel instance moves to a full-screen slot, and the 3D view's container moves to the side
  strip. The slots are authored; the state switch moves nodes between them. The view keeps
  rendering.
- **The ship systems display is one scene** (`scenes/ui/ssd.tscn`), parameterised, painted from sim
  state. Painting a live data chart in `_draw()` is allowed (the-federation 7 lists the same kind of
  exception); each painter is listed in CLAUDE.md section 14 before it is built.
- **Theme in one place**: a generated `Theme` resource (colours from palette roles, one typeface,
  sizes owned by the theme). No per-node colour or font size overrides in scenes.

## Extension points (prefer these over new code paths)

| To add | Do this |
| --- | --- |
| a ship class | a data entry in `data/ships/` plus its glb from `blender-hard-surface`; no code unless a new part kind |
| a weapon | a data entry; a new behaviour is one class extending the weapon behaviour base, registered once |
| a panel readout | a control in the panel's scene bound to one sim facet; the sim computes the value |
| an effect | a scene in `scenes/fx/` listening to one signal from the battle root |
| a mission | a data file in `data/missions/` |
| a playtest | a script in `data/playtests/` (`playtest`) |

## Feature checklist

1. Write the OpenSpec change (`/opsx:propose`): proposal, design with numbers, spec deltas, tasks.
2. Mock it up in three.js if the player will see it (`threejs-mockups`). Stop until asked to build.
3. Rules in `src/sim/` with tests, tuning in `data/`, wiring in thin scripts, structure in `.tscn`.
4. Run `scripts/run-tests.sh` (headless sim tests, then the layout test under Xvfb).
5. Play it (`playtest`), at desktop and phone sizes.
