## ADDED Requirements

### Requirement: Standing opens a friendly faction's ports grant by grant

A port of another faction SHALL give parts at standing 0, supplies at 15, weapons at 30, ships at 50 and officers at 70
(`rules.privileges`), and nothing while that faction is your rival or below 0. Your own ports SHALL give everything. Every
purchase SHALL be refused, naming the standing needed, where its grant is not given.

#### Scenario: Earned in order
- **WHEN** the fleet is at a friendly faction's port at standings 0, 15, 30, 50 and 70
- **THEN** it is given parts, then supplies, weapons, ships and officers, one more each time

#### Scenario: Not yet a friend
- **WHEN** the fleet is at a port of a faction at standing 10
- **THEN** repairs are given and buying a ship or hiring an officer is refused

### Requirement: Hostile factions hunt a fleet in their space with growing fleets

Each turn the fleet ends in a hostile faction's sector SHALL raise that faction's alarm, more the lower the standing; each
alarm level reached SHALL send hunters or reinforce them, larger at each level; hunters SHALL close on the fleet each turn and,
in its sector, SHALL offer the Hunted fight. Away from hostile space the alarm SHALL cool and hunters with no alarm SHALL go home.

#### Scenario: Lingering
- **WHEN** a Concord fleet ends 14 turns in Krai space
- **THEN** Krai hunters of 2 or more ships have caught it and the Hunted fight is on offer

#### Scenario: Leaving
- **WHEN** a fleet with alarm 3 stays out of hostile space for 30 turns
- **THEN** the alarm is 0 and no hunters are out
