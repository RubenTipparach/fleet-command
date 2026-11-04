# Proposal: boarding-party

## Why

The owner, 2026-11-04, of the Transport station's Capture and Hit & run queues:

- "by default marines get transported, so show darkend marines in the queue if nothing is queued"
- "thiss is both truye for capture and hit and run"
- "capture - medics heal marines -- int he expanded view show marine health and battle log for when medics heal, hackers
  speed up the process of capturing, but are easy to get shot and die if there are less than 5 marines. pilots arent
  needed right away, but are required to activate and fly the ship, if no pilot show a "pilot needed" after capturing the
  ship"

## What Changes

- **The default load**: with nothing queued, the pads beam marines, one a pad. The queue shows them darkened, and a pad
  or Energize all sends them. This applies to Capture and Hit & run.
- **Marine health**: each marine aboard an enemy ship has health. A hit wounds a marine, and a second hit kills it.
- **Medics heal**: each volley, every medic treats the most wounded marine. Medics no longer save a casualty outright.
- **Hackers** shorten each volley (15 % each, to half at most) and still lock out the guard. With fewer than 5 marines
  covering them, a hit finds a hacker half the time.
- **Pilots**: taking a ship needs an engineer and a hacker (or the envoy). A prize with no pilot aboard shows **Pilot
  needed**: in the view, the Fleet station and a notice. It holds still and does not fight, and command of it is refused,
  until a pilot beams over.
- **Expanded Capture**: the party's marines as health bars and the specialists with what they do now, next to a
  boarding log that names the volleys, every casualty, the medics' treatment and the hackers shot.

## Impact

- `docs/mockups/battle-console.html` (both skins): `defaultLoad`, `beamQueued`, the queue grid, `BOARD` (`wound`, `heal`,
  `hacker_speed`, `hacker_cover`, `hacker_exposed_p`), `partyHp`, `woundParty`, `canTake`, `pilotNeeded`, `partyPanel`.
  Shots: `queue_default`, `queue_default_capture`, `capture_party`, `pilot_needed`.
- This is a mockup only; game code waits for approval.
