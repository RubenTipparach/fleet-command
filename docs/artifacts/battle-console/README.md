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
- Version 26 published 2026-11-02, version id `1793578903-e903`: a scene selector in the view's top left (Scene key and its list).
- Version 27 published 2026-11-02, version id `1793580688-9bf9`: the camera lock (locking a target yaws the chase camera to face it, smoothed by a critically damped spring; a drag frees it; Lock camera under the Target box), after Fargo's CameraSystem.
- Version 28 published 2026-11-02, version id `1793580890-bd74`: the panels' Expand pill replaced by a small round key with diagonal arrows (pointing in to return), names on hover.
- Version 29 published 2026-11-02, version id `1793581929-1c47`: the warp drive (Power, Warp mode): nacelles with draw and damage, banks, course plot, six risk factors and the jump risk, the safety override near a neutron star, the jump and its mishaps; a Warp test scene (change `warp`).
- Version 30 published 2026-11-02, version id `1793582380-60b2`: panel titles' black plate starts at the frame's top edge (no longer cutting the panel above); expand key 20 % smaller (21 px).
- Version 31 published 2026-11-02, version id `1793583166-34de`: the click mark (blue plus) removed; drag on or near the helm ring to steer; right drag turns the camera; one finger turns it on touch (or steers from the ring); the sky drawn behind every ship; space dust streaking with your velocity.
- Version 32 published 2026-11-02, version id `1793589279-a672`: a recharging weapon box fills from the bottom in its bright status colour over the dark shade as it charges; the seconds in white with a dark outline.
- Version 33 published 2026-11-02, version id `1793590141-f06b`: the helm rebuilt from the SFC2 manual's Helm Officer display (target orders, a HET pad, erratic, emergency stop, HET chance); a red target bracket with a double frame on a seeker lock; fresnel shield keys with centred names; the expanded weapons panel as the ship with Fire keys and a detail half; ready weapons outlined dashed white; ships four times tougher.
- Version 34 published 2026-11-02, version id `1793590854-3845`: hull bars in the view under the targeted enemy and your ship (docked and expanded); the helm ring steers in the expanded view.
- Version 35 published 2026-11-02, version id `1793591099-eefd`: helm icons redrawn like signage (road-sign turn arrows, U-turn, straight ahead, STOP); Approach and hold distance with its distance slider; Orbit with a CW/CCW switch.
- Version 36 published 2026-11-02, version id `1793595050-ad9c`: a big ship explosion (white-hot core, blue shockwave ring, plasma cloud, sparks, burning chunks; Destroy target in the menu); the helm's target orders in the left half, Hit and run with a two-handled range slider and Evasive manoeuvres (a toggle, out of the HET pad) in the right.
- Version 37 published 2026-11-02, version id `1793595346-a7ae`: the hit and run icon redrawn as a strafing pass (up toward a target, a tight turn short of it, back down and away).
- Version 38 published 2026-11-02, version id `1793601438-8fb3`: the evasive icon redrawn (a bold zigzag with its head aimed along the last stroke).
- Version 39 published 2026-11-02, version id `1793601524-e678`: a failed high energy turn damages the impulse engines and leaves the ship dead in space for 15 s (the owner's rule); the readouts renamed To hit, HET (counting a breakdown down), Heading, Speed.
- Version 40 published 2026-11-02, version id `1793601692-846d`: the mine placement preview (an animated bullseye of the mine's 1.2 km range under the pointer: yellow, red with a ship in range, grey and "Invalid position" outside the 4 km transporter reach).
- Version 41 published 2026-11-02, version id `1793601920-a5e5`: the scene selector as a drop-down (remembered across reloads) with the scene's objective under it (green, COMPLETE when met); an unarmed Resolute in every test scene; each scene's tool decisive (one mine, one shuttle, a rock, a barrage past the point defence).
