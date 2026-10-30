## ADDED Requirements

### Requirement: Port facilities open only at a friendly port

The shipyard, outfitter and recruiting center SHALL be available only while the fleet is on a hex holding a friendly station or planet, and SHALL be shown disabled elsewhere.

#### Scenario: Away from port
- **WHEN** the fleet is on an open-space hex
- **THEN** the shipyard, outfitter and recruiting center buttons are disabled

#### Scenario: At a friendly station
- **WHEN** the fleet ends its move on a hex with a friendly station
- **THEN** all three facilities can be opened

### Requirement: Purchases are paid in credits and refused when short

Every purchase at a port SHALL be charged in credits and SHALL be refused, with nothing charged, when the credits do not cover it.

#### Scenario: Not enough credits
- **WHEN** the player has 400 credits and buys a hull priced 2300
- **THEN** the purchase is refused and the credits stay 400
