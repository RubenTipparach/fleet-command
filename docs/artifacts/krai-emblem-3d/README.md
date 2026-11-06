# Krai Badge 3D (mockup 14)

- Link: https://claude.ai/artifact/PTvvbWnntKFDsiWEyuXD48
- Shows: the Krai Warhost's emblem KC+ (the fang torc) as a badge 100 mm across, orbitable: a blade steel ring round a
  blackened steel field, the faceted torc with its thin outer crescent and trophy fangs, the founder's fang in blood-red
  enamel, and the forge star struck proud; the Blender renders beside it.
- Made by: `docs/mockups/krai-emblem-3d.html` on the shared badge library `docs/mockups/lib/emblem-badge.js`, with the
  parts from `assets/emblems/krai/kc.parts.json` (`tools/emblems/export_parts.mjs krai`), the Material Maker maps listed in
  `docs/mockups/lib/emblem-materials.json`, and the Blender renders listed in `assets/emblems/krai/kc.renders.json`
  (`tools/blender/emblem_krai.py`, 96 samples, 1200 px), inlined by `tools/mockups/inline_data.py`.

## Publishes

- 2026-11-04, version 1 (`1793803223-e96d`): first publish, with the full-quality Blender render (owner: "make the 3d
  version of the fang torc").
- 2026-11-05, version 2 (`1793863643-5461`), desktop session: the badge's parts and materials now come from the shared builder `docs/mockups/lib/emblem-builds.js` (shared with the game flow's faction screen, change `faction-select`); no visible change.
