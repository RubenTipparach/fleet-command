## ADDED Requirements

### Requirement: Stars come from a seeded generator

Every star field the game draws SHALL be placed by a seeded generator with full avalanche (SplitMix64, or Mulberry32 where
a mockup's numbers must match), in a distribution chosen on purpose, with the classes and colours of
data/space/starfield.json, and SHALL NOT use a modular or sine hash.

#### Scenario: The menu's stars replay and show no lattice
- **WHEN** the menu's starfield is made twice for the same seed and window size
- **THEN** the stars are the same, and successive stars' steps are uncorrelated

#### Scenario: The field keeps its shares
- **WHEN** a 1920 by 1080 field is made
- **THEN** each class's share of the stars is within 4 points of starfield.json's
