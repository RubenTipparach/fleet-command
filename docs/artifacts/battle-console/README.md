# Mockup 2: Battle Console

- Link: not yet published. A cloud session cannot publish without a manual approval, so the desktop session publishes
  it (CLAUDE.md section 8) and records the link here, in the mockup tracker and in `mockup-work.md`.
- Shows: the battle console in the Concord skin around a stand-in battle. The left column (name plate with the fleet
  switch; the stations panel with helm, power, tactical, point defence, science, comms, repair, transport and fleet on
  a rail; the ship panel with the systems display, shields, sensors, tractor and craft; the weapons panel; the radar row
  with the tactical map), the top and bottom strips, and the 3D view with the target bracket, right-click targeting,
  the target camera, the 3D shield display, officer messages and the readout. Every control works against a seeded
  battle (three Concord ships against two Kethran and a Vos raider, wingmen with configurable behaviours, pause with
  orders); every panel expands with the battle beside it, at desktop, phone landscape and phone portrait.
- Made by: `docs/mockups/battle-console.html` (the page source, one copy), written up in
  `openspec/changes/battle-console/`. Shared code inlined by `tools/mockups/inline_data.py`:
  `docs/mockups/lib/kitdraw.js`, `docs/mockups/lib/concord.js` (the Concord skin, also used by mockup 4) and
  `docs/mockups/lib/space.js` (the sky and stand-in ship, also used by mockup 1). Shots:
  `docs/screenshots/mockups/battle-console-*.png`, from `tools/mockups/shoot.mjs` then `tools/mockups/shrink_png.py`.
- Cost, measured on the battle shot at 1920 x 1080 (SwiftShader, `window.MOCKUP_INFO`): 48,668 triangles and 106
  draw calls a frame, main view and target camera together. The stand-in ships and sky are mockup 1's; mockup 3 sets
  the real budgets.
- Stage: drafted.
- Published: version 1 to publish (2026-11-01).
