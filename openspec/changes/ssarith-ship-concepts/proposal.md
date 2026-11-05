# Proposal: ssarith-ship-concepts

## Why

The owner, 2026-11-04: "using current bibles and mockups on ship design, lets consider the Ssarith Hunt culture and how
their ships will look." Then, 2026-11-05, on version 8: "Bro why are you stuck on this. Delete this design. It sucks, look
at the inspiration. Block out more stuff. We need a variety of designs. Not just 3 lame ones that look like coffins. Look
at my examples - gorn from Starfleet command".

Versions 1 to 8 are deleted: version 1 read the species' body into the ships (withdrawn, owner: "you are over
stereotyping an alien species"), and versions 3 to 8 refined one long fitted hull into three ships that read as coffins.
What the owner asks for now is breadth before depth: many massings to choose from, then detail on the chosen ones.

## What Changes

- **Mockup 12 version 9**, `docs/mockups/ssarith-ship-concepts.html`: a blockout board of twelve frigate-sized massings in
  grey clay, each its own silhouette (a flattened ovoid with high pods, a sphere on a neck, a catamaran, a ring ship,
  stepped terraces, a T-bar, a crescent, a bundle of pods, a teardrop with an underslung hull, a delta, an asymmetric
  offset ship, a disc over a drum), at true scale with the Concord Petrel to hand, each orbitable on its own.
- **Deleted** with versions 1 to 8: the frigate's plans (`data/ships/ssarith/`, `tools/mockups/ssarith_plans.py`), the
  baked plating tiles and their generator (`assets/textures/ssarith/`, `tools/blender/build_hull_tiles_ssarith.py`), the
  old shots. Git history keeps them (the tile generator at commit 6f7a22c) for when a chosen blockout is detailed.
- **Survey H4**: which blockouts go on.
- **Kept**: the Ship Design Bible's rules apply to whatever is chosen (rooms first, culture not anatomy, C1-C5), and the
  plan layer's tapering rooms in `tools/mockups/krai_plans.py`.

## Impact

- Changed: `docs/mockups/ssarith-ship-concepts.html` (replaced), `docs/design/survey.html` (section H), `mockup-work.md`,
  `docs/artifacts/ssarith-ship-concepts/`, this change.
- No game code.
