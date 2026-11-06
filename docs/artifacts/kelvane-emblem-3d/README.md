# Kelvane badge in 3D (mockup 21)

- Page: `docs/mockups/kelvane-emblem-3d.html`, its INLINE blocks filled by `tools/mockups/inline_data.py`.
- OpenSpec: `openspec/changes/faction-emblems`, `openspec/changes/kelvane-battle-console` (D9, D10).
- Geometry: `tools/mockups/kelvane_emblem.py` writes `assets/emblems/kelvane/ka.orbital.json`, `ka.satin.json` and `ka.grid.png`,
  and the 2D logo `assets/ui/kelvane/emblem.svg` (rasterised as `ka-logo.png`).
- Blender render: `tools/blender/emblem_kelvane.py` on `tools/blender/emblem_render.py` (`ka-clay.png`, `ka-face.png`, `ka-angle.png`).
- Live build: `docs/mockups/lib/emblem-builds.js`'s `kelvane()` on `docs/mockups/lib/emblem-badge.js`.
- Shots: `docs/screenshots/mockups/kelvane-emblem-3d-*.png`.

## Publishes
- Link: https://claude.ai/artifact/JWkaEkgW1aTvxU8s5p9sN3
- Version 1 (2026-11-06): KID on an oval coin: the orbital in iridescent green with its wireframe light green, over red satin draped in soft folds, in a champagne rim; the 2D hull logo beside the renders. Version id `1793934506-805f`.
