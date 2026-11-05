## ADDED Requirements

### Requirement: Every mission is a script of one shape

Every campaign mission SHALL be defined by a script naming where it may be offered, its briefing and objective, what it
needs, its battle or none, and its result, and the campaign SHALL apply the result the script returns without the script
changing the campaign itself.

#### Scenario: A mission without a battle
- **WHEN** the fleet reaches an accepted Shakedown cruise and the player chooses Complete
- **THEN** no battle starts, 250 credits are added and First blood is offered

### Requirement: A mission is accepted, flown to and then deployed

A mission SHALL be accepted from its briefing into the mission log, and Deploy or Complete SHALL be available only while the
fleet is in the mission's sector.

#### Scenario: Deploy from afar
- **WHEN** the player opens an accepted mission's briefing two sectors away
- **THEN** Deploy is disabled

#### Scenario: Accepted contracts wait
- **WHEN** a contract has been accepted and its three turns pass
- **THEN** it stays on the map and in the mission log

### Requirement: A new campaign starts with one ship of the smallest tier

A new campaign SHALL start with one ship of the chosen side's smallest tier, at that side's home port.

#### Scenario: A new Krai campaign
- **WHEN** the player begins a Krai campaign
- **THEN** the fleet is one Krai corvette at Vahr

### Requirement: The fleet flies its route

Moving SHALL fly the fleet's miniature along the route sector by sector, and SHALL NOT move the camera.

#### Scenario: A three-sector move
- **WHEN** the player moves the fleet three sectors
- **THEN** the miniature flies through each sector in turn and the view stays where the player left it

### Requirement: The game's scenarios are skirmish test maps

The skirmish SHALL list every scenario in the game's scenario data as a test map that launches with its own fleets.

#### Scenario: Launching a test map
- **WHEN** the player picks Krai Battle and launches
- **THEN** the battle starts with the Krai side's three ships against three Concord ships
