## ADDED Requirements

### Requirement: The campaign map is a hex grid seen in perspective

The campaign map SHALL be a grid of hexes seen by a tilted perspective camera that pans and zooms, showing territory at far zoom, terrain, planets and stations at middle zoom, and ship standees at near zoom.

#### Scenario: Zooming in shows ships
- **WHEN** the player zooms from far to near
- **THEN** ship standees and their labels appear

### Requirement: Ships, planets and stations stand on the map as standees

Every fleet, planet and station on the map SHALL be drawn as a standee in its faction's colour that faces the camera.

#### Scenario: Turning the camera
- **WHEN** the player pans the map
- **THEN** every standee still faces the camera

### Requirement: The fleet moves by move points

The fleet SHALL move along hexes paying each hex's move cost from 3 move points a turn, SHALL NOT enter impassable hexes, and SHALL refill its move points at End turn.

#### Scenario: Nebula costs two
- **WHEN** the fleet with 3 move points moves into a nebula hex and then an open hex
- **THEN** it has 0 move points left and cannot move again until End turn

#### Scenario: Stars are impassable
- **WHEN** the player picks a star hex
- **THEN** no path is offered and the fleet stays where it is
