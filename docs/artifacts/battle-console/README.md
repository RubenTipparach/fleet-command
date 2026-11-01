# Mockup 2: Battle Console

- Link: https://claude.ai/artifact/9yertq3bYQRmFiDE7N9eBs (slug `48b217b4-8632-4ff8-bd62-913e6dadd23a`), on the owner's current account.
- Shows: the battle console in the Concord skin around a stand-in battle. The left column (name plate with the fleet
  switch; the stations panel with helm, power, tactical, point defence, science, comms, repair, transport and fleet on
  a rail; the ship panel with the systems display, shields, sensors, tractor and craft; the weapons panel; the radar row
  with the tactical map), the top and bottom strips, and the 3D view with the target bracket, right-click targeting,
  the target camera, the 3D shield display, officer messages and the readout. Every control works against a seeded
  battle (three Concord ships against two Kethran and a Vos raider, wingmen with configurable behaviours, pause with
  orders); every panel expands with the battle beside it. Desktop only for now (the owner: no mobile mockup yet).
- Made by: `docs/mockups/battle-console.html` (the page source, one copy), written up in
  `openspec/changes/battle-console/`. Shared code inlined by `tools/mockups/inline_data.py`:
  `docs/mockups/lib/kitdraw.js`, `docs/mockups/lib/concord.js` (the Concord skin, also used by mockup 4) and
  `docs/mockups/lib/space.js` (the sky and stand-in ship, also used by mockup 1). Shots:
  `docs/screenshots/mockups/battle-console-*.png`, from `tools/mockups/shoot.mjs` then `tools/mockups/shrink_png.py`.
- Cost, measured on the battle shot at 1920 x 1080 (SwiftShader, `window.MOCKUP_INFO`): 48,668 triangles and 106
  draw calls a frame, main view and target camera together. The stand-in ships and sky are mockup 1's; mockup 3 sets
  the real budgets.
- Stage: drafted.
- Published: version 1 not published; version 2 published 2026-11-01 from the cloud session, version id `1793521024-5061` (2026-11-01: icon tabs in narrow columns named on
  hover, the ship below the centre, a speed handle, Power over Systems, desktop only).
- Version 3 (2026-11-01, not published: the owner approves each publish by hand, so changes are shown as screenshots
  until asked): no seams between 9-slice pieces (each element composed whole at its pixel size); Power and Systems
  labels tightened.
- Version 5 published 2026-11-01 from the cloud session at the owner's request ("publish it so I can play with this"), version id `1793525175-7698`: no slice seams, the Reactor mode with its diagram (owner's "simpler please!" version), Power blue over Systems green with the battery, the link back to the tracker.
- Version 6 published 2026-11-01, version id `1793526785-4023`: Power and Systems as ticks, alert lamps, the Target box showing the target's systems display.
- Version 7 published 2026-11-01, version id `1793527854-936c`: the console drawn in the engine (one canvas, kit pieces composed at pixel size, fixed text, grow-only number slots, immediate-mode input); no HTML layout left to twitch.
- Version 8 published 2026-11-01, version id `1793528480-1a5b`: weapon glyphs, subsystem columns and status cards on hover on the systems display.
- Version 9 published 2026-11-01, version id `1793557329-7d68`: white arc over the facing toward the enemy, square weapon boxes that never overlap, shield strength as 1-3 layers, the 3D shields a flat hexagon.
- Version 10 published 2026-11-01, version id `1793558522-5cfa`: the shield page as a hexagon diagram (click a side to reinforce, colour-coded strengths) with the power station's shield row.
