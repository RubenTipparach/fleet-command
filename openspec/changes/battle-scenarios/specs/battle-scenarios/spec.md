## ADDED Requirements

### Requirement: Scenarios for both sides

The game SHALL offer scenarios from `data/battle/scenarios.json` played from the Concord's side and from the Krai
Warhost's side, each naming the side the player commands, and SHALL start the player in the first ship of the scenario,
which belongs to that side.

#### Scenario: Both sides are playable
- **WHEN** the scenarios load
- **THEN** there are Concord and Krai scenarios with the same names, and every scenario's first ship is on side 0 and of
  the scenario's side

#### Scenario: A side without a console cannot be played
- **WHEN** a scenario names the Kethran as its side
- **THEN** loading fails, naming the scenario's `side` field and saying the faction has no console skin

### Requirement: Standing orders come from the scenario

An AI captain SHALL fly with the stance, fighting range, erratic manoeuvring and hold-fire order its scenario gives, and
SHALL start with the scenario's fraction of hull and shields.

#### Scenario: A captain told to hold fire never shoots
- **WHEN** an enemy with `hold_fire` closes on the player for a minute
- **THEN** it fires no shot and the player's hull is untouched

### Requirement: The scene selector swaps the side

The view SHALL show a scene selector naming the scenario in play; picking another scenario SHALL start it from its seed
and SHALL draw the console in the skin of the side it is played from.

#### Scenario: Picking a Krai scenario
- **WHEN** the player opens the selector and picks the Krai's Battle
- **THEN** the battle restarts with the player in the Kharz and the console in the Krai Warhost skin
