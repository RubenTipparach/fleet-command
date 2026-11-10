# Proposal: engagement-zone

## Why

The owner, 2026-11-10: "important rule: zone of engagement, a big square, leaving means you are leaving engagement zone
and retreating. give players 30 seconds to turn around before abandoning a mission", "note: abandoning a mision without
functional warp drive means you are self destructing", "have the warning for self destruct show up on screen lol".

Today a battle has no edge: a ship can fly away for ever, and the only way out is the Withdraw key (charge the drive, plot
a course, jump; the battle ends `withdrawn`, `Battle.Warp.cs`). The owner wants a field with a border, where flying out is
itself a retreat, with time to change one's mind, and a real price for a ship that cannot warp away.

## What Changes

- **Every battle has an engagement zone**: a square on the battle plane, drawn on the radar dial, the tactical map and
  faintly in the 3D view where it meets the plane.
- **Leaving it starts a retreat**: when the commanded ship crosses the border, an on-screen warning counts down 30 s
  ("Leaving the engagement zone: return within 30 s or abandon the mission"). Flying back inside cancels it.
- **At zero the mission is abandoned**: a ship whose warp drive works warps out (the battle ends `abandoned`, a retreat).
- **Without a working warp drive, abandoning is self-destruction**: the countdown's warning says so on screen
  ("Warp drive down: leaving the zone will self-destruct the ship"), and at zero the ship is destroyed (the battle ends
  `lost`, or command passes to another ship of the fleet if one is left).

## Impact

- `src/Core/Sim`: the zone (`Battle.Zone.cs`), its countdown and outcome; `Over` gains `abandoned`.
- `data/battle/systems/zone.json` (size, countdown), a scenario's own zone where it names one.
- The console: the warning banner, the zone on the radar and the map, the border in the 3D view. **New UI: mockup first**
  (mockup 37 in `mockup-work.md`), then the game.
- The campaign: an abandoned mission's consequences (`campaign-missions`).
