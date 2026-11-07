## ADDED Requirements

### Requirement: Every approved faction has a freighter, from its culture

Each approved faction (Concord, Krai Warhost, Ssarith Hunt, Kelvane Institute: its ships approved and in the game) SHALL
have a freighter. A freighter SHALL take no shape, paneling, texture or marking from what its species looks like. Each of
its design choices SHALL name the tradition inside its faction it comes from (Ship Design Bible C1 to C5). A tradition
not written by the owner SHALL be marked provisional.

#### Scenario: The stereotype test
- **WHEN** a freighter concept is described without naming the species
- **THEN** the description is a working ship's shape, never a creature's body

#### Scenario: Every concept names its tradition
- **WHEN** a concept is picked on the board
- **THEN** the page names the tradition it comes from, says whether it is drafted, and gives its way of carrying cargo

### Requirement: The freighter board is a board to pick from

The freighter page SHALL show eight freighter blockouts, two for each approved faction, at true scale with the Concord
Petrel and a person to hand. Each SHALL show its length, beam, height and triangle count, measured from its built geometry. Each
SHALL be viewable on its own, and each faction's three together.

#### Scenario: The board
- **WHEN** the board is shown
- **THEN** all eight stand on it in four columns, one a faction, each labelled with its number and name

#### Scenario: One faction
- **WHEN** a faction is chosen
- **THEN** only its two concepts stand side by side, with the Petrel beside them

#### Scenario: One at a time
- **WHEN** a concept is picked from the list
- **THEN** only it is shown, from its bow quarter, with its measured size, its tradition and its way of carrying

### Requirement: Every freighter carries the same container, in sight

Every freighter SHALL carry its cargo as containers of one size, 6 by 6 by 12 m, the same for every faction, and its
containers SHALL be seen from its bow quarter and from above.

#### Scenario: One container
- **WHEN** the page measures a freighter's cargo
- **THEN** every container is 6 by 6 by 12 m, and the count is shown with the concept

### Requirement: Every freighter hull is one closed grown structure; only its cargo is placed

Every freighter hull SHALL be grown on the shared `docs/mockups/lib/hull-grow.js`: one body lofted through the board's
section layout set in its faction's way, every branch split from the body's own section. Its skin SHALL be closed, every
edge shared by exactly two triangles. Its containers SHALL be the only placed pieces.

#### Scenario: Closed skins
- **WHEN** the page measures its hulls
- **THEN** every hull reports zero open edges
