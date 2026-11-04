## ADDED Requirements

### Requirement: A weapon is armed in one of the modes it has

A weapon SHALL be armed Offline, Normal, or one of its own modes (a lance: Overload; a pulse torpedo: Overload or
Proximity), every rule and readout SHALL use the armed mode's numbers, and a mode the weapon lacks SHALL be refused.

#### Scenario: Overloaded lances only up close
- **WHEN** the captain overloads a lance and the target is 6 km away
- **THEN** the lance is out of range and its chance to hit reads "--" until the range closes to 4 km

#### Scenario: Proximity torpedoes on a jinking frigate
- **WHEN** a torpedo armed for proximity fires at a frigate making erratic manoeuvres
- **THEN** its chance to hit is higher than a normal torpedo's, its damage lower, and it can reach the subsystems of the
  facings beside the one it bursts on
