# Proposal: krai-ship-concepts

## Why

The owner, 2026-11-03: "Let's start mocking up krai ships".

The Krai Warhost has a console skin (`console-skins`, version 23) but no ships. The skin already fixes the faction's
design language in the owner's words: "combat focused ... triangular motifs, angry red ... orange lit buttons ... blade
like", then "something like a rough gun metal, maybe sort of akin to volcanic magma type metal", and "The krai ship
display should be a wire frame schematic". Survey A8, left blank so its recommendation stands, gives the Krai a bird's
head and wings.

## What Changes

- **Mockup 7**, `docs/mockups/krai-ship-concepts.html`: the first Krai concepts, at true scale beside the Concord line
  (mockup 6), which is the yardstick through `docs/mockups/lib/concord-line.js`.
  - Three directions at frigate size:
    - **A Stoop**: a raptor diving; a dagger head on a thin neck, wings swept back and angled down, a tail blade up.
    - **B Crossed blades**: four forward-swept blades in an X round a short body.
    - **C Spearhead**: one flat delta whose point is the head, with forward-raked talons.
  - Direction A built out as a line of four classes (corvette, frigate, cruiser, dreadnought), each beside the Concord
    ship of its rank.
  - The 40 px silhouettes, top and side, of every direction and class.
- **The look is the console's**: cast volcanic gunmetal with glowing fissures, a red-enamel war mask on the head, red on
  leading edges, forge orange for engines, gun tips and window slits.
- Survey questions A15 (which direction), A16 (sizes and how a class grows) and A17 (what the Krai call their ships).
- Not in this change: damage, hull lamps and textures. Those follow once a direction is chosen.

## Impact

- New page and records: `docs/mockups/krai-ship-concepts.html`, `docs/artifacts/krai-ship-concepts/`, the tracker,
  `mockup-work.md`, the survey.
- No game code.
