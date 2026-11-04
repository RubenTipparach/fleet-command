# Proposal: boarding-defence

## Why

The owner, 2026-11-04, answering the weapon-targeting change's recommendation "Morale and boarding defence: crew quality
that falls with damage and makes boarding easier": "10.. boarding def sounds cool. Put a high security officer,
transport units around the ship to setup ambushes. We need to mock up units moving about the ship.in 3d space"

## What Changes

- **A Security page** on the Transport station: the ship's marines as security teams (T1, T2, ...), Walk or Beam, and
  the ship's deck plan in 3D (an exploded view of its three decks, orbit by dragging) with the teams and boarders as
  figures, one for each marine, walking the corridors and ladders.
- **Orders**: pick a team, click a room; it walks there along the corridors, or is beamed there by the internal
  transporter for one pad charge.
- **Ambushes**: a team that holds a room for 6 s has an ambush set; when boarders reach it, its first volley lands harder
  and they get no shot back.
- **Boarders**: an enemy alongside with the facing toward it down beams squads into empty rooms; each walks to its goal
  (the bridge, the shield generator, the reactor, engineering) and, unopposed, sabotages it; holding the bridge 30 s
  takes the ship.
- **The security chief** is the Transport station's officer (change `officers`): ambush, hit aboard and pad recharge.
- **The Repel boarders scene**: the Meridian (a Petrel-class destroyer, the plan's own ship) with its shields down and a
  Vos corsair alongside sending three squads.

## Impact

- `docs/mockups/battle-console.html` (both skins): the Petrel's plan inlined (`data/ships/concord/petrel_plan.json`),
  `SECURITY`, `ROOM_SYSTEM`, `INTERIOR`, `route`, `secStep`, `boardShip`, `volley` (now also the boarding party's
  volley), a second small renderer for the interior, the Security page, shots `scene_repel`, `repel_expanded`.
- Mockup only.
