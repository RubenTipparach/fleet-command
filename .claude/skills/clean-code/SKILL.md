---
name: clean-code
description: Write and review Fleet Command GDScript the repository's way - one implementation of every rule, SOLID applied to Godot (a scene-free simulation, thin node scripts, small classes, registries and data over growing match statements, dependencies passed in), static typing, tuning in validated data with units, documented contracts, guarded edges, warnings as errors. Use when writing a new class, scene script, panel or system, adding a feature to an existing system, refactoring, or reviewing a diff ("clean this up", "review this", "where should this go", "is this SOLID", "refactor").
metadata:
  author: Fleet Command (Claude Code), after Crab Breeze's clean-code, the-federation's CLAUDE.md sections 4 and 5, and Falchion's solid-principles
  version: "1.0"
---

# Clean code in Fleet Command

Fleet Command will grow by ships, weapons, factions and panels. Write code so that adding one means
**adding** a data entry or a class, not editing ten files. CLAUDE.md section 6 holds the rules; this
is the working checklist. `godot-architecture` says where code goes; `testing-practices` how it is
proven.

## One implementation of every rule

The owner's highest-priority rule in every repository.
- **Search first.** Before writing a resolver, a panel, a generator or a math helper, find the one
  that exists. Extend or parameterise it: an argument, a strategy, a data field.
- **Never rebuild an existing feature from scratch.** If it cannot be adapted, say why before
  replacing it.
- **What a panel shows is computed by the code that resolves it.** The chance to hit calls the
  same hit roll's probability function the shot uses; the capacitor bar reads the energy model;
  the target's hull bar reads the damage model. A preview that disagrees with the outcome is the
  bug this rule prevents.
- **The ship systems display is one scene**, configured by parameters (read only or editable,
  damage shown, size), instanced wherever a ship's state is shown (the-federation 6.1).
- **Report duplication** when you find it. Never add a third copy.

## SOLID, in GDScript and Godot

- **Single responsibility.** One reason to change per class and per scene. A rule lives in
  `src/sim/`; a scene script only wires its nodes to the simulation and to input. If a class's
  header comment needs "and", split it.
- **Open/closed.** A new weapon is a data entry; a new firing behaviour is one class extending the
  weapon behaviour base, registered in one registry. Never a new arm in a `match` spread over
  files.
- **Liskov substitution.** Every subclass honours its base's whole contract: no
  `push_error("not implemented")` stubs, no hidden preconditions.
- **Interface segregation.** Small classes, narrow signals. A panel subscribes to the facets it
  shows (hull, shields, capacitor), not to "the ship changed".
- **Dependency inversion.** Simulation classes take what they need in `_init` or as parameters:
  the seeded `RandomNumberGenerator`, the step in seconds, the data tables. One composition root
  (the battle's bootstrap script) wires them. No simulation class reads an autoload, the scene
  tree, `Time` or `Engine`.

## GDScript conventions

- **Static typing everywhere**: `var speed_u_s: float = 0.0`, `func fire(mount: Mount) -> FireResult:`.
  Typed arrays and dictionaries (`Array[Ship]`, `Dictionary[StringName, Weapon]`). The project sets
  untyped declarations, unsafe calls and unused variables as errors.
- **Naming**: `snake_case` files, functions and variables; `PascalCase` `class_name`s and nodes;
  `CONSTANT_CASE` constants; signals in the past tense (`hull_damaged`). Units in the name
  (`range_u`, `charge_e`, `cooldown_s`).
- **Header comment** on every script: what it owns and why it lives there. `##` doc comments on
  every public function and exported property.
- **No inline tuning.** Numbers come from `data/` (JSON, units in keys), validated on load
  (CLAUDE.md 6.4). Code defaults are for missing fields only.
- **Guard the edges.** `is_finite()` on numbers from data, input and saves; bounds checks on
  indices; an action that does not fit (not enough energy, mount offline) returns a reason and
  changes nothing. Player mistakes return a result; programming errors `assert` or `push_error`.
- **Nodes are found by exported `NodePath`s or `%UniqueName`**, never long `get_node("../../a/b")`
  chains.
- **Signals up, calls down.** A child signals; its owner calls into children. No sideways reaching
  into a sibling's internals.
- **Scenes are authored files**: build node trees in `.tscn`, not in code (the-federation 5.1).
  Instancing and configuring a prebuilt scene is fine; assembling one node by node is not.
- **Borrowed code keeps its provenance** (repository and revision) in its header.
- **No em or en dashes** anywhere (CLAUDE.md section 5).

## Review checklist

- [ ] Does this already exist? Was it extended rather than copied?
- [ ] Does `src/sim/` stay free of `Node`, the scene tree, rendering, audio and input?
- [ ] Does every readout call the code that resolves the thing it shows?
- [ ] Can a new variant be added by data or registration alone?
- [ ] Are tuning numbers in data, with units in the keys?
- [ ] Is everything statically typed, every script headed, every public function documented?
- [ ] Is anything random seeded and forked, and anything ordered sorted by a stable id?
- [ ] Does every rule have a test that reads as a sentence (`testing-practices`)?
- [ ] Do panels keep their authored size whatever their text (CLAUDE.md section 7)?
