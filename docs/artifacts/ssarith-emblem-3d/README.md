# Ssarith Badge 3D (mockup 19)

- Link: https://claude.ai/artifact/NLspB15dVMS7dhnbM8PvCB
- Shows: the Ssarith Hunt's emblem SD (the Bow Rites) as a badge 100 mm across, orbitable: three laminated wooden bows,
  overlapping and aimed right, each a step prouder than the one behind it; arrows in Watch green lacquer, each head its own
  shape (the learner's leaf, the hunter's barb, the elder's tally) under dried blood; a dry forest of beige, dull brown and
  dry green in the field with its canopy hanging over it as holographic leaves at three depths; a bone-pale metal rim; the
  Blender renders beside it.
- Made by: `docs/mockups/ssarith-emblem-3d.html` on the shared badge library `docs/mockups/lib/emblem-badge.js`, with the
  parts from `assets/emblems/ssarith/sd.parts.json` (`tools/emblems/export_parts.mjs ssarith`), the leaves from
  `assets/emblems/ssarith/sd.leaves.json` (`tools/emblems/ssarith_leaves.py`), the Material Maker maps listed in
  `docs/mockups/lib/emblem-materials-ssarith.json` (new graphs `emblem_wood_laminate`, `emblem_lacquer_watch`,
  `emblem_blood_watch`, `emblem_enamel_forest` in `tools/materials/build_ptex.py`), and the Blender renders listed in
  `assets/emblems/ssarith/sd.renders.json` (`tools/blender/emblem_ssarith.py`, 96 samples, 1200 px), inlined by
  `tools/mockups/inline_data.py`.

## Publishes

- 2026-11-05, version 1 (`1793839261-17d1`): first publish (owner: "shade the bow parts with like wood and the arrows green
  with blood", "Background should be like a beige, dull brown and dry green forest", "make each arrowhead unique", "with
  like holographic leaves", "Should be 3d leaves shapes").
