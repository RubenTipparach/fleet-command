# Proposal: vos-ship-concepts

## Why

The Vos Syndicate has a console (mockup 28), a placeholder emblem and a placeholder line (mockup 36), but no ships of its
own. The owner, 2026-11-10: "lets desgin some vos pirate ships!".

## What Changes

- **Mockup 37, Vos Syndicate Ships** (`docs/mockups/vos-ship-concepts.html`), version 2: eight conversions, after the
  owner's note on version 1 ("they are usualy modified freighters", "out of scrapped parts or stolen parts from other
  ships"). Each starts as one of the game's four freighters and carries parts taken from other factions' approved ships,
  at true size. Version 1's twelve sculpted hulls and five drafted schools are withdrawn.
- **The parts bin**: the page inlines the game's own glbs and copies their triangles, cutting cleanly where a crew cut.
- **The Syndicate's own metal**: collars, struts, a welded prow and gun tubs in flat welded plate, grown on the shared
  `docs/mockups/lib/hull-grow.js`.
- **Survey section Y**, rewritten for the conversions.

## Impact

- No game code. The placeholder Vos line (mockup 36) stays in the game until a line from this mockup is approved for
  implementation.
- `docs/mockups/lib/hull-grow.js` is used as it is, unchanged.
