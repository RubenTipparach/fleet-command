## ADDED Requirements

### Requirement: Port facilities open only at a friendly port

The shipyard, outfitter and recruiting center SHALL be available only while the fleet is on a hex holding a friendly station or planet, and SHALL be shown disabled elsewhere.

#### Scenario: Away from port
- **WHEN** the fleet is on an open-space hex
- **THEN** the shipyard, outfitter and recruiting center buttons are disabled

#### Scenario: At a friendly station
- **WHEN** the fleet ends its move on a hex with a friendly station
- **THEN** all three facilities can be opened

### Requirement: Other factions' ports open a market while standing allows

A station or planet of another faction SHALL open a market (selling only) while the player's standing with that faction is 0 or above, and SHALL be closed while it is below 0.

#### Scenario: Pirates will deal
- **WHEN** the player's standing with the Vos Syndicate is 0 and the fleet is at a Vos station
- **THEN** its market opens and the shipyard, outfitter and recruiting stay disabled

#### Scenario: Selling to pirates raises standing
- **WHEN** the player sells a ship at a Vos market
- **THEN** the Vos pay 0.6 times the usual quarter and the player's Vos standing rises by 5

### Requirement: Purchases are paid in credits and refused when short

Every purchase at a port SHALL be charged in credits and SHALL be refused, with nothing charged, when the credits do not cover it.

#### Scenario: Not enough credits
- **WHEN** the player has 400 credits and buys a hull priced 2300
- **THEN** the purchase is refused and the credits stay 400
