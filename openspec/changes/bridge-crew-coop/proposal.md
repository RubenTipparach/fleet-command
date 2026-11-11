# Proposal: bridge-crew-coop

## Why

The owner, 2026-11-11: "lets start building multiplayer modes. the main on I want to focus on is coop in the campaign and
or scenarios. we would need to introduce the captain's menu. assigning players roles in the game. certain roles like
science/sensors, tractor beam, transporters , weapons targetting, and navigation would have acces to independent
targetting. and each station targeting various ships would have the station icon marked on the traget showing they have
targeted someone. This would allow players to each target a ship of their choosing", "players play roles of officers on
the bridge, because there are so many staitons some players can assume the roles of multiple stations. It is up to the
captain to assign these roles. The different tabs become disabled if your role doesnt have access to that system", "we
can use our star crew system for connecting players together".

Today one person plays every station of the commanded ship, and the ship has one target that every station acts on. A
crew of friends cannot share a bridge.

## What Changes

- **A crew on one bridge.** In a co-op campaign or scenario, several players crew the commanded ship. One is the
  captain (the host). The others join through the connection system of the owner's Star Crew Bridge Simulator.
- **Roles, assigned by the captain** in the captain's menu: Navigation, Weapons, Science, Tractor, Transporters,
  Engineering, Point defence, Flight, and the Captain's own (fleet, comms, the pause, taking command of another ship).
  A player may hold several roles; a role nobody holds stays with the captain.
- **Each player's console shows every station, but only their roles' tabs work**; the others are disabled (seen,
  not pressed).
- **Independent targeting.** Navigation, Weapons, Science, Tractor, Transporters (and Flight, provisionally) each keep
  their own target: one player may tractor a freighter while another fires on its escort and a third scans a cruiser.
  Every bracket shows the icon of each station that has that ship targeted.
- **One player stays as today**: holding every role, one lock sets every role's target, so single-player play does
  not change.

## Impact

- `src/Core/Sim`: a target per role on the ship (in place of the one `Ship.Target`), each station's orders reading its
  role's target; the order stream carries who sent each order, checked against their roles.
- `src/Game`: the captain's menu (the lobby and the role grid), disabled tabs, the role icons on the brackets, the
  connection (Star Crew's system), the campaign's screens shared with the crew.
- New screens: **mockup first** (mockup 39 in `mockup-work.md`), then the game.
