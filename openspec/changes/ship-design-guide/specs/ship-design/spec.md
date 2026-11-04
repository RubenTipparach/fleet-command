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

### Requirement: A ship's exterior complements its console and has its own features

A faction's hull SHALL be distinguished by its own features, colour scheme and paneling, and SHALL be shown beside
its console skin; sharing a colour or material with the console is allowed (complementing is a guide, not a check).

#### Scenario: A new faction's hull
- **WHEN** a faction's ship exterior is designed
- **THEN** its page shows its palette beside its console skin's, and no colour-distance check is applied

### Requirement: A ship's design comes from its makers' culture, never from their bodies

A faction's ship SHALL take no shape, paneling, texture or marking from what its species looks like, SHALL name for
every design choice the part of the faction's culture it comes from, and its line SHALL show more than one of that
culture's design traditions, as the owner sets them (Ship Design Bible chapter 13, rules C1 to C5).

#### Scenario: The stereotype test
- **WHEN** a faction's ship design is described without naming the species
- **THEN** the description is not a picture of the species' body, and every choice in it names its source in the culture
