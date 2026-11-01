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
- Version 11 published 2026-11-01, version id `1793558594-e747`: one band per shield facing on the console, layers only in the 3D view.
- Version 12 published 2026-11-01, version id `1793560162-8b98`: the shield page's facings as big trapezoid keys, the reinforced one lit.
- Version 13 published 2026-11-01 (version ids `1793560287-49e6`, then the overlap fix): shield keys fill with their charge, numbers on hover; weapon boxes spaced so even their selection rings never touch (checked headless: no two weapon rectangles overlap).
- Version 14 published 2026-11-01, version id `1793563480-96e9`: weapon and subsystem boxes filled solid in their status colour; Take a hit in the menu.
- Version 15 published 2026-11-01, version id `1793564103-a559`: weapon mounts snapped to a grid (rows and columns, mirrored pairs); the lance glyph fixed, no longer turned to its arc.
- Version 16 published 2026-11-01, version id `1793564157-a675`: the mount grid drawn faintly behind the ship, clipped to the shield ring.
- Version 17 published 2026-11-01, version id `1793566190-ab8e`: the selection ring flush on the weapon box, same corner radius, no gap.
- Version 18 published 2026-11-01, version id `1793566438-87a3`: the selection ring drawn as a solid plate under the weapon box, boxes on whole pixels; measured seam 0 at 1x and 2x (`tools/mockups/ring_seam.mjs`, 21-33 before).
- Version 19 published 2026-11-01, version id `1793568233-3bd6`: cooling weapons show seconds until ready (x.x under 10 s, xx above, three figures scaled to fit) in place of the glyph and the charge bar; white hover outline; each mount one group drawn above the schematic.
- Version 20 published 2026-11-01, version id `1793569706-f5d7`: a cooling weapon box turns a dark shade of its status colour with the seconds in the bright colour. `tools/mockups/ring_seam.mjs` rebuilt (fill = most common colour in the box, ring pixels including blended corners): 0 on every selected box at 1x and 2x, ready and cooling; v17 fails it (41-152).
- Version 21 published 2026-11-01, version id `1793570069-f5ba`: mines on a 3 s cooldown (the Drop mine key counts down); the tractor beam as a contest of rounds (grip from 50 %, 10 s rounds, blind picks with the tractor energy boxes, 10 % per point of difference, break free at 0 with a one-round lockout), a beam line in the 3D view.
- Version 22 published 2026-11-01, version id `1793571048-5ae4`: the tractor down to two keys, Hold and Break; a 10 s power-up; when the held ship breaks free and the holder's tractor is down or under half its value, the beam turns and it holds the holder; a "Held by" panel state; Break at any time.
- Version 23 published 2026-11-01, version id `1793573215-0c15`: the helm by pointing (a left click on empty space orders the ship onto the bearing of the point, after the-federation `_helm_click`), a turn arc on a faint ring round the ship, a mark where the helm was pointed.
- Version 24 published 2026-11-01, version id `1793573870-8c46`: test scenes in the menu (Battle, Mines, Tractor, Boarding); mines placed by transporter within 4 km; asteroids and the tractor tow; raids (shoot or bomb a subsystem) and boarding to capture.
- Version 25 published 2026-11-01, version id `1793577332-e905`: the transporter's three modes (Capture by role, Hit & run with marine types, Inventory), reeling, stations and planets, and scenes Delivery, VIP, Shuttle and Fighters (eight test scenes in the menu).
