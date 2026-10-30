## ADDED Requirements

### Requirement: Missions are offered on eligible hexes

The campaign SHALL offer missions of seven types (patrol, enemy station assault, planetary assault, convoy ambush, convoy defense, assassination, capture a ship), each only on a hex that suits it, and each SHALL expire after 3 turns.

#### Scenario: Station assault needs an enemy station
- **WHEN** missions are offered
- **THEN** every enemy station assault sits on a hex with an enemy station

#### Scenario: Missions expire
- **WHEN** a mission has been offered for 3 turns and the player ends the turn
- **THEN** the mission is removed from the map

### Requirement: A mission is deployed from its hex and returns to the map

Deploy SHALL be available only while the fleet is on the mission's hex; ending the battle SHALL show its results and return to the campaign map with the reward, the damage and any losses applied and the mission removed.

#### Scenario: Deploy from afar
- **WHEN** the fleet is two hexes from a mission and the player opens its briefing
- **THEN** Deploy is disabled

#### Scenario: Back to the map after a victory
- **WHEN** the player wins a patrol worth 300 credits and continues from the results
- **THEN** the campaign map shows, the credits have risen by 300 and the patrol is gone

#### Scenario: Capturing a ship adds it to the fleet
- **WHEN** the player wins a capture mission with fewer than four ships
- **THEN** the captured ship joins the fleet as a wingman
