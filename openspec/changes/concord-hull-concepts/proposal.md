# Proposal: concord-hull-concepts

## Why

The owner, 2026-11-03, on mockup 5's Concord ships: "ship designs suck, I want somethign more original ,check out star
crew and adventure con repos for how ships are designed".

Mockup 5 started from the reference game's saucer and nacelles and decorated them. Both of the owner's other ship
projects work the other way round, and their method is what this change adopts:

- **Star Crew** (`RubenTipparach/Star-Crew-Bridge-Simulator`, `openspec/changes/federation-ship-concepts/` and
  `grabon-ship-concepts/`): design from the inside out. Mission, crew, decks, rooms and circulation first; then a hull
  wrapped around the occupied volume; then the rooms refitted to the hull. Windows only where a real room is. Three
  alternatives per brief, reviewed in those three stages, with measured checks.
- **Adventure** (`RubenTipparach/adventure-2dcon-26`, CLAUDE.md 13.2 and 13.2b, `mockups/fleet/`): each faction has
  one shape language, drawn from how it thinks and fights, that survives at 40 pixels. The player's faction is the one
  with negative space you can see through. A class is countable by a repeated element, and each class has one mark no
  other class carries.

## What Changes

- Three original Concord directions, each built inside out for the frigate and grown to a corvette and a cruiser:
  - **A, Halo**: the crew live in a ring you can see through; the bridge is a raised block on the hub at the ring's
    centre; drive pods hang outboard on short pylons. A class is counted in rings.
  - **B, Lantern spine**: an open lattice keel strung with lit habitat lanterns, each pair a different shape (every
    body in the Concord gets a home shaped for it). A class is counted in lantern pairs.
  - **C, Light sail**: a slim keel between outboard booms, with glowing shield sails stretched between them. A class
    is counted in sail pairs.
- Each shown in three stages (zones, hull, fitted), at true scale with a 1.8 m figure, with measured length, beam,
  decks, zones contained in the hull, and window count; 40 px silhouettes from above and the side.
- Mockup 6, `docs/mockups/concord-hull-concepts.html`. Mockup 5's damage system stays and takes the chosen hull later.
- Survey A11 (which direction) and A12 (the Concord's sizes).

## Impact

- Supersedes the silhouette and class shapes in `concord-ship-look` (its livery, markings, windows-as-scale and
  lighting rules stand). No game code.
