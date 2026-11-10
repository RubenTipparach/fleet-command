## ADDED Requirements

### Requirement: A Vos ship is a conversion, not a new hull

Every Vos ship on mockup 37 SHALL start as one of the game's freighters or a captured warship, and SHALL carry parts
taken from other factions' approved ships. No Vos hull SHALL be sculpted new.

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
