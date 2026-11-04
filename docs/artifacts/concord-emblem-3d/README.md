# Concord Badge 3D (mockup 10)

- Link: https://claude.ai/artifact/CJSVJxXYLn7cdKF6TDtvDN
- Shows: the Concord's emblem B-E23 (the rounded peak) as a badge 100 mm across, orbitable: a polished platinum frame,
  an iridescent pearl ship with the star pressed into its saucer, and a holographic night field (nebula and stars at
  three depths that shift with the view); the Blender renders beside it.
- Made by: `docs/mockups/concord-emblem-3d.html` with the parts from `assets/emblems/concord/b-e23.parts.json`
  (`tools/emblems/export_parts.mjs`), the Material Maker maps listed in `docs/mockups/lib/emblem-materials.json`, and
  the Blender renders listed in `assets/emblems/concord/b-e23.renders.json` (`tools/blender/emblem_concord.py`),
  inlined by `tools/mockups/inline_data.py`.

## Publishes

- Version 1 published 2026-11-04 (cloud session, faction emblems thread), version id `1793765019-c6ec`.
- 2026-11-04, version 2 (`1793776101-60fc`): B-E25 (the pointed peak), the satin blue star, and the owner's small layer of detail: a two-tier saucer (upper tier inset 1.6 mm) and four segments on each leg (grooves 0.5 mm deep).
- 2026-11-04, version 3 (`1793776690-edf5`): corners fixed (owner: "fix the corners here"): the saucer's upper tier is an exact inset with one clean point at each tip, and the frame's inner peak no longer spikes, because every shape is now sampled with each corner as a vertex.
- 2026-11-04, version 4 (`1793777293-400e`): the full-quality Blender render (96 samples, 1200 px) of the corrected shapes beside the badge.
- 2026-11-04, version 5 (`1793787574-bb93`): rebuilt on B-E27, the smooth keyhole (accepted), with a satin cyan star (owner: "Also render the 3d version too"); Blender test render beside it, full render to follow.
- 2026-11-04, version 6 (`1793788354-4ac8`): the full-quality Blender render of B-E27 (96 samples, 1200 px) beside the badge.
