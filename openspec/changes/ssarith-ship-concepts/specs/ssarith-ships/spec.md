## ADDED Requirements

### Requirement: Hunt ships are designed from their rooms

Every Ssarith Hunt class SHALL have a plan of its rooms in `data/ships/ssarith/`, written by
`tools/mockups/ssarith_plans.py` on the same plan layer as the Krai's, and its hull SHALL be fitted round them by the
shared fitter, with its character added on top and never cut into a room.

#### Scenario: The frigate's plan
- **WHEN** the Hunt page fits a hull round `data/ships/ssarith/frigate_plan.json`
- **THEN** no two rooms on a deck overlap, every room touches its corridor, and every room corner lies at least 1.05 m inside the hull

### Requirement: Hunt ships come from the Hunt's culture, never its body

A Hunt ship SHALL take no shape, paneling, texture or marking from what the species looks like, and each of its design
choices SHALL name its source in the Hunt's culture as the owner sets it (Ship Design Bible rules C1 to C5).

#### Scenario: The stereotype test
- **WHEN** a Hunt design is described without naming the species
- **THEN** the description is not a picture of a reptile's body

### Requirement: Hunt ships are shown at true scale beside the Concord line

The Hunt page SHALL show each direction beside the Concord Petrel built from `docs/mockups/lib/concord-line.js`, with
its length, beam and height measured from the built geometry, and each direction as a 40 px silhouette, top and side.

#### Scenario: The yardstick
- **WHEN** the line-up is shown
- **THEN** each Hunt direction stands beside the Petrel at the same scale, each with its measured size
