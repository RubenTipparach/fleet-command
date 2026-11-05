## ADDED Requirements

### Requirement: Hunt ships come from the Hunt's culture, never its body

A Hunt ship SHALL take no shape, paneling, texture or marking from what the species looks like, and each of its design
choices SHALL name its source in the Hunt's culture as the owner sets it (Ship Design Bible rules C1 to C5).

#### Scenario: The stereotype test
- **WHEN** a Hunt design is described without naming the species
- **THEN** the description is not a picture of a reptile's body

### Requirement: Hunt ship designs start from a board of varied blockouts

Before any Hunt ship is designed in detail, the Hunt page SHALL show at least twelve frigate-sized massings, each a
different silhouette, in plain clay at true scale with the Concord Petrel to hand, with each massing's length, beam and
height measured from its built geometry.

#### Scenario: The board
- **WHEN** the board is shown
- **THEN** twelve massings stand on it, every one with its measured size, and each can be viewed on its own

### Requirement: A picked blockout is designed rooms first

A blockout the owner picks SHALL be designed on the Ship Design Bible's method: its rooms as blocks in `data/ships/`, its
hull fitted round them, then its parts and skin.

#### Scenario: From massing to ship
- **WHEN** a blockout is picked
- **THEN** its plan is written before its hull is drawn in detail
