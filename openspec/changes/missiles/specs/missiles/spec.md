## ADDED Requirements

### Requirement: Every faction can fit missile racks

A ship of any faction SHALL be able to carry missile racks; a rack SHALL hold a number of rounds and reload them on a
cycle without reactor energy, and a missile SHALL fly free, home on its target for its fuel time, and be stoppable by
point defence.

#### Scenario: A Krai cruiser answers a Concord salvo
- **WHEN** an Albatross fires four missiles at a Krai cruiser and the cruiser fires four back
- **THEN** each missile flies in its grade's colour (red, orange, blue, green), each ship's point defence fires on the
  missiles closing on it, a blue or green one takes two hits to stop, and each that gets through does its grade's damage
  at any range

#### Scenario: A missile runs out of fuel
- **WHEN** a missile has flown 20 s without reaching its target
- **THEN** it drifts dead and can no longer hit anything
