## ADDED Requirements

### Requirement: Every faction in the game has a freighter, from its culture

Each faction in the game (Concord, Kethran Hegemony, Vos Syndicate, Krai Warhost, Ssarith Hunt, Kelvane Institute) SHALL
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

The freighter page SHALL show eighteen freighter blockouts, three for each faction, at true scale with the Concord Petrel
and a person to hand. Each SHALL show its length, beam, height and triangle count, measured from its built geometry. Each
SHALL be viewable on its own, and each faction's three together.

#### Scenario: The board
- **WHEN** the board is shown
- **THEN** all eighteen stand on it in six columns, one a faction, each labelled with its number and name

#### Scenario: One faction
- **WHEN** a faction is chosen
- **THEN** only its three concepts stand side by side, with the Petrel beside them

#### Scenario: One at a time
- **WHEN** a concept is picked from the list
- **THEN** only it is shown, from its bow quarter, with its measured size, its tradition and its way of carrying

### Requirement: Every freighter hull is one closed grown structure; only its cargo is placed

Every freighter hull SHALL be grown on the shared `docs/mockups/lib/hull-grow.js`: one body lofted through the board's
section layout set in its faction's way, every branch split from the body's own section. Its skin SHALL be closed, every
edge shared by exactly two triangles. The cargo it carries (containers, cassettes, cases, lashed loads) SHALL be the only
placed pieces.

#### Scenario: Closed skins
- **WHEN** the page measures its hulls
- **THEN** every hull reports zero open edges
