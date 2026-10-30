---
name: testing-practices
description: How to test Fleet Command - headless GDScript tests for the simulation named as sentences, a failing test before every bug fix, golden seeds, data validation from the real files, the fixed-size layout test and HUD staleness test under Xvfb, headless stills, and the full check before a push. Use when adding or changing logic, fixing a bug, adding data or a panel, or before committing or pushing.
metadata:
  author: Fleet Command (Claude Code), after the-federation's tests/ and scripts/run-tests.sh, and Crab Breeze's testing-practices
  version: "1.0"
---

# Testing practices

the-federation's test setup is the model (`tests/run_tests.gd`, `tests/layout_test.gd`,
`tests/hud_test.gd`, `scripts/run-tests.sh` at `02791b6`). Copy its patterns when the project is
created; do not invent a framework. No addon (GUT, gdUnit) unless a change argues for one.

## The simulation tests

- **Headless, no scene.** `godot --headless --path . --script res://tests/run_tests.gd` runs every
  `tests/sim/test_*.gd`. A test builds sim objects from data and a seed, steps them, and asserts.
  The runner prints `ALL TESTS PASSED` on success and exits non-zero otherwise; any engine error or
  script warning in the log fails the run.
- **Tests read as sentences**: `func test_a_shield_facing_at_zero_lets_the_next_hit_through_to_the_hull()`.
  One behaviour per test. A non-obvious assertion carries a message saying why it matters.
- **Every public behaviour of `src/sim/` has a test.** Inputs are a seeded RNG, a fixed `step_s` and
  data; no mocks needed beyond those.
- **Property checks for maths**: arcs are symmetric, energy in equals energy spent plus stored, a
  hit chance stays in [0, 1], damage never goes negative.
- **Golden seeds**: "seed 7, this script of inputs, ends with the frigate at 41 hull after 90 s".
  An unexpected change is a determinism bug until proven otherwise.
- **A bug fix brings its regression test**, failing first, committed with the fix.
- **Readouts are tested against outcomes**: the chance to hit a panel shows equals the observed
  hit rate over many seeded rolls within its tolerance (CLAUDE.md 6.1).

## Data

Validate by loading the real files (`data/`) with the game's own loader: unknown keys, missing
required fields, out-of-range and non-finite values all fail with the path and field. Never compare
against a hand-written copy of the expected value.

## Layout and panels (CLAUDE.md section 7)

These need a rendering context, so they run under `xvfb-run` with `--rendering-driver opengl3`
rather than `--headless`:
- **Fixed size**: instance the console at each reference size (desktop and phone), write an overlong
  string into every runtime label, wait **two** process frames, and assert every panel's `size` is
  its authored size (not `get_combined_minimum_size()`, which ignores the maximum). the-federation's
  `layout_test.gd` has the reasons.
- **Expand**: expand each panel in turn and assert it fills the screen while the 3D view's container
  sits in the side strip at its authored size and is still visible.
- **Staleness**: drive a seeded battle and compare every panel's text with the sim state it claims
  to show (the-federation's `hud_test.gd`).
- **Touch targets**: every interactive control at the phone size is at least 44 x 44 CSS px.

## What it looks like

A capture, looked at by eye: headless stills under Xvfb in a cloud session, a playtest recording on
the owner's machine (`playtest`). A green test is not a visual sign-off.

## Before every push

```sh
scripts/run-tests.sh                 # once the Godot project exists
openspec validate --all
LC_ALL=C.UTF-8 grep -rnIP '\x{2014}|\x{2013}' --exclude-dir=.git --exclude-dir=.claude \
  --exclude-dir=.godot --exclude-dir=.tools --exclude-dir=node_modules . && echo FAIL
```

Every OpenSpec scenario maps to a test or a playtest beat, named in its `tasks.md`.
