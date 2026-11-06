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

### Requirement: The factions move their own fleets

Every fleet but the player's SHALL be moved by its faction at End turn on its own cadence of 1 to 3 turns, SHALL face the
direction of its last move, and SHALL NOT be ordered by the player.

#### Scenario: Three turns on
- **WHEN** the player ends three turns without moving
- **THEN** every faction fleet whose cadence came round has moved, and each faces along its last leg

### Requirement: Pieces show missions, the player's fleet and strength

Each mission SHALL stand in its sector as a standee with its letter, the player's fleet SHALL stand on gold, and every
fleet SHALL have a strength flag beside it showing 1 to 5 pips.

#### Scenario: A new campaign's home sector
- **WHEN** a new Concord campaign opens
- **THEN** the fleet at Halcyon stands on a gold base with a one-pip flag, and the Shakedown cruise stands in its sector

### Requirement: Freighters and routes for your side and your allies

Each faction SHALL run freighters along routes between its own ports, and the map SHALL show the player's side's
freighters and routes and the routes of factions at standing 25 or more, and SHALL NOT show any other faction's.

#### Scenario: An ally's route
- **WHEN** the player's standing with the Ssarith Hunt reaches 30
- **THEN** the Calder to Pell Reach route shows, dimmer, without its freighter
