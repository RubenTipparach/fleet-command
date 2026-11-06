# Space fly-through (mockup 22)

- Link: https://claude.ai/artifact/9JWXLSiYcoMFwyasYX6fLg
- Page: `docs/mockups/space-flythrough.html` (its `data/space/starfield.json` block inlined by `tools/mockups/inline_data.py`)
- Change: `openspec/changes/space-flythrough`
- Shots: `docs/screenshots/mockups/space-flythrough/` (`tools/mockups/shoot.mjs`, desktop)

Owner, 2026-11-05: "write up a beautiful fly through of space with ray marched nebula clouds and stars".

## Versions

- 2026-11-06, desktop session: version 1, version id `1793935768-bd51`, then `1793935880-738f` (renumbered mockup 22, the
  numbers 20 and 21 having been taken by another session's Kelvane mockups). Ray-marched clouds, 33,000 seeded stars, four
  palettes, Low, High and Best, free look.
- 2026-11-06, desktop session: version 2, version id `1793938013-1945`: the noise baked into a 3D texture (owner: "the performance is terrible!"). M4 Max, 1920 x 1080: High from 16 fps to 4.5 ms a frame, Best from 8 fps to 8.9 ms (`tools/mockups/flythrough_perf.mjs`).
