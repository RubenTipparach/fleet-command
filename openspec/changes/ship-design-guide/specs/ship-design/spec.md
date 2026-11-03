## ADDED Requirements

### Requirement: Ships are designed from their rooms

Every ship class SHALL have a plan of its rooms as blocks in `data/ships/`, and its hull SHALL clear every room by the
plan's clearance plus structure, measured from the data.

#### Scenario: The Petrel's plan
- **WHEN** the Ship Design Bible fits a hull round `data/ships/concord/petrel_plan.json`
- **THEN** no two rooms on a deck overlap, every room touches its deck's corridor, and every room corner lies at least 1.05 m inside the fitted hull

### Requirement: Windows sit on lived-in rooms

A window SHALL be placed only where a command, crew or systems room meets the hull's flank, one row a deck 1.5 m above
the floor, person-sized.

#### Scenario: Engineering has no windows
- **WHEN** windows are placed from the Petrel's plan
- **THEN** none falls on engineering, the reactor, the magazine, the shuttle bay or a corridor

### Requirement: A ship's exterior complements its console without sharing its materials or colours

A faction's hull SHALL NOT use a material, texture, shader or colour swatch of that faction's console skin, and SHALL
be distinguished by its own features, colour scheme and paneling.

#### Scenario: A new faction's hull
- **WHEN** a faction's ship exterior is designed
- **THEN** its palette and materials are listed beside its console skin's and none is shared
