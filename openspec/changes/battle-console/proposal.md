# Proposal: battle-console

## Why

Mockup 2 in `mockup-work.md`, the in-game UI. The owner, 2026-11-01, starting this thread with a screenshot of the
reference game's battle screen: "we're gonna start a thread to handle the ui design mockup of the game, the first design
system we will implement will be the concord, we will then reuse this design once it has been fully approved across all
other factions, and each faction's implementation will be manually verified such that all of it is functional and
readable." And: "review this starfleet command ui implementation and my initial prompt on how we would go about this,
break down the elements, create a javascript three js mockup, 3d gameplay can be placeholder of a ship moving through
space for now, but the ui must be fully interactive. I'll walk you through and iterate the fine details, but lets get
something built so we can begin the iteration process."

The owner's earlier brief for this mockup (2026-10-30, `mockup-work.md` section 2):

- "Main panels: helm, repairs, science, transport, tactical, comms, point defense, map, fleet, power, in-game menu and
  settings."
- "Ship overview panels: shields and weapons (the primary display) ... shield array, sensor systems, transport and
  cargo (perhaps combined with the transporter system above), tractor beam, shuttles and fighters."
- "Ship 3D view: the player's ship, a target camera, the weapons capacitor, a speed slider, energy and reactor output,
  the enemy view, the enemy targeting system (point and right click), and a 3D display of the ship's shield array."

And from the survey: real time with pause, orders given while paused (G2); a small fleet with AI wingmen whose
behaviours are configurable (G3); an expanded panel puts the live battle in a strip a third of the width (U1); phones
play landscape and portrait (U2).

## What Changes

- **The battle console layout** (CLAUDE.md section 7) at the 1920 x 1080 reference: the left column (name plate with
  the fleet switch, the stations panel, the ship panel, the weapons panel, the radar row), the top strip, the bottom
  strip and the 3D view.
- **Every panel works** against a small stand-in battle: the helm pad and autopilot, energy allocation in boxes, the
  ship systems display with shield facings and weapon mounts, arming states and fire, targeting by right click, the
  target camera, the 3D shield display, and the stations: tactical, point defence, science, comms, repair, transport,
  fleet (wingman behaviours), plus the ship pages (shields, sensors, tractor, craft), the tactical map, the menu.
- **Every panel expands** to full screen with the live battle in a strip at the side (landscape) or along the bottom
  (portrait), at desktop, phone landscape and phone portrait.
- **Concord first**: the page draws only from the kit's named pieces in the Concord skin, from one shared module
  (`docs/mockups/lib/concord.js`) that the console skins page now also uses. Other factions follow once Concord is
  approved, each checked by hand for function and legibility.
- **Proposed kit additions**: the allocation cell (`cell.idle`, `cell.lit`, `cell.warn`, `cell.locked`) and eleven new
  icons; every skin will need them before the kit grows (survey B6).
- The 3D battle is a stand-in (mockup 3 decides the look). Nothing is built in the game until the mockup passes both
  approvals (CLAUDE.md section 4).
