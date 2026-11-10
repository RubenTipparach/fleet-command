## ADDED Requirements

### Requirement: A Vos ship is made of freighter parts and taken parts

Every Vos ship on mockup 37 SHALL be built on a freighter's drive block and spine from the game, or on a captured
warship, with the game's containers and parts taken from other factions' approved ships. No Vos hull SHALL be sculpted
new. A battle freighter's layout SHALL be the Syndicate's own: no other faction's ship is arranged the same way.

#### Scenario: Reading a conversion
- **WHEN** the owner picks a conversion in the list
- **THEN** it is shown alone, orbitable, with what it starts as and what was taken

### Requirement: Taken parts are the game's own ships at true size

Every taken part SHALL be copied from the glb the game flies, never redrawn and never scaled.

#### Scenario: A nacelle on a freighter
- **WHEN** the Chaser carries a Concord Albatross's nacelles
- **THEN** each nacelle is the Albatross's own mesh at its own size

### Requirement: Only the joining metal is the Syndicate's

Everything the Syndicate makes itself SHALL be a closed skin grown on `docs/mockups/lib/hull-grow.js` in its own plate,
and SHALL cover each cut it closes.

#### Scenario: A cut bow
- **WHEN** a freighter's bow is cut away
- **THEN** a collar a little larger than the measured section covers the cut
