# Mockup 24: Space Chart

- Link: https://claude.ai/artifact/A4E9dHbWA5qdvi1Z77sAjX
- Shows: the campaign's space chart, opened straight onto the paper map. It is a printed star map on old paper (nebula,
  stars, every hex ruled in its holder's ink, torn white border), the fog of war with a soft edge, on a walnut table in a
  room with a red carpet and rustic green walls with wood trim. The whole campaign plays from it: it continues the saved
  Concord campaign if there is one, else starts a new one.
- Made by: `tools/mockups/chart_page.py`, which writes `docs/mockups/space-chart.html` from mockup 1
  (`docs/mockups/game-flow.html`), so the chart runs mockup 1's own code. Its only differences are the title, a
  `<meta name="fc-start" content="chart">` and the corner badge linking back to the tracker. Written up in
  `openspec/changes/paper-map/`. Shot: `docs/screenshots/mockups/space-chart-open-desktop.png`.
- Stage: drafted. Approvals are given in the mockup tracker.
- Version 1 published 2026-11-07, version id `1794057082-da7c` (owner: "Our space chart mockup is not available to me as
  an artifact, please make it").
- Version 2 published 2026-11-07, version id `1794092239-3938`: phones (owner, 2026-11-07: "Space chart not working on mobile"): the nebula print's noise read from a seeded lattice texture instead of integer maths, so it renders on WebGL 1 (it failed to compile there and printed black); the chart sheet drawn at 32 px a unit on a touch screen (1603 x 1394, 9 MB) instead of 56 (27 MB); the fog's soft edge drawn without the canvas blur filter where the browser lacks it (Safari).

- Version 3 published 2026-11-08, version id `1794096305-9138`: phones (owner, 2026-11-07, with a screenshot of a phone in portrait: "Panels on my phone stuck open, I can't see"): the sector panel has a Hide key, the map keys a Sector key to bring it back, and picking a sector opens it again; in portrait the top bar is two rows (credits, turn, moves and Missions; then prestige and the standing chips, unlabelled), not four; the corner badge sits under the bar on the left. `dom_fit.mjs` passes at desktop, phone and portrait.
- Version 4 published 2026-11-08, version id `1794098592-e375`: the drawn pieces (owner, 2026-11-08: "make the nebula or storm clouds like hand drawn billboard puffs of cloud. And make the planets have that hand drawn quality too", "very pirate and very space"): `docs/mockups/lib/drawn.js` draws in sepia ink and watercolour, shaded away from the lamp; on the paper chart nebulae stand as cartoon cloud billboards (one cloud in three a storm with a bolt), planets as banded worlds ringed in the holder's colour, stars as rayed suns, belts as drawn rocks; names hand-lettered in IM Fell English SC; a compass rose and rhumb lines on the print. Ships and stations stay as models. Shots `space-chart-room.png`, `space-chart-close.png`, `space-chart-drawn-pieces.png`.
