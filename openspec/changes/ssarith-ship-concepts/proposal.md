# Proposal: ssarith-ship-concepts

## Why

The owner, 2026-11-04: "using current bibles and mockups on ship design, lets consider the Ssarith Hunt culture and how
their ships will look." Then, 2026-11-05, on version 8: "Bro why are you stuck on this. Delete this design. It sucks, look
at the inspiration. Block out more stuff. We need a variety of designs. Not just 3 lame ones that look like coffins. Look
at my examples - gorn from Starfleet command".

Versions 1 to 8 are deleted: version 1 read the species' body into the ships (withdrawn, owner: "you are over
stereotyping an alien species"), and versions 3 to 8 refined one long fitted hull into three ships that read as coffins.
What the owner asks for now is breadth before depth: many massings to choose from, then detail on the chosen ones.

> **Version 10** (2026-11-05, with the owner's examples: "they looked menacing!", "make ships like this, with this kind of
> texture"): twelve blockouts in the examples' shape language, painted by a new skill, `painted-bake` (design, "Version 10").

> **Version 12** (2026-11-05): the owner picked 1 Hooded lance, 3 Forked prow, 7 Harpoon and 9 Trident and withdrew the
> Spearhead; asked that "every ship needs custom uv Unwraped textures ... tile textures are for DETAIL only ... dont forget
> to bake AO as well"; and, on the picks, "these shapes need work, they arent connect together well, and violate our "no
> deformed primitive rules", shapes must be made with purpose and flow into the grater structures". Each pick is rebuilt
> as one grown structure and baked onto its own UVs (design, "Version 12"); both are now rules for every ship (CLAUDE.md
> section 10, Ship Design Bible chapters 14 and 15).

> **Version 13** (2026-11-05): "discard ships that I didnt want", and more on the hulls: "vents, lights, and sub armor
> pieces, and primary armor peices", "liveries, smooth sphereical portions of armor, and specifically shaped vents", "no
> repeating elemnts, single axis symmetry, and lots of shading to give the armor depth". Only the picks are kept; each
> one's hull detail is modelled and baked onto its own UVs (design, "Version 13").

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
- Version 12 also changes: `CLAUDE.md` (section 10), `docs/design/ship-bible.html` (chapters 14 and 15),
  `.claude/skills/painted-bake/SKILL.md` (ship mode), `tools/blender/painted_bake.py`, `tools/mockups/export_blockouts.mjs`,
  `assets/ships/ssarith/`, and a task in `ship-models-engine`, `concord-hull-concepts` and `krai-ship-concepts` to move
  those lines onto their own unwrap.
- Version 13 also adds `tools/blender/hull_detail.py` and `data/ships/ssarith/hull_detail.json`, and removes the unpicked
  blockouts' shots.
- No game code.
