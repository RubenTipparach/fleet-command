## ADDED Requirements

### Requirement: An ensign brings nothing until promoted

An officer SHALL have as many skill points as their rank's index, and an ensign SHALL add nothing to any ability.

#### Scenario: A new ensign at the helm
- **WHEN** an ensign is the helm officer in a battle
- **THEN** the helm's abilities are those of an empty slot

### Requirement: Officers gain experience and are promoted for skill points

Every officer of the crew SHALL gain experience from each battle by its grade and from each campaign turn, and a promotion
SHALL spend the next rank's experience (any shortfall paid in prestige at the data's rate) and give one skill point.

#### Scenario: Three victories
- **WHEN** an ensign with no experience serves in three victories
- **THEN** they can be promoted to Lieutenant without prestige, and have one skill point to place

### Requirement: Crew is traded and hired for prestige

Transferring an officer out SHALL give their rank's worth plus their banked experience in prestige, and hiring from the pool
SHALL cost prestige by rank.

#### Scenario: An ensign traded in
- **WHEN** an ensign with 300 experience banked is transferred out
- **THEN** the side gains 6 prestige and the station is vacant

### Requirement: Skills are learned on a tree, by hand or by auto level

A skill SHALL be learned with one point only once the skill above it in its branch is learned; an officer on auto level
SHALL spend each point along their station's path as it is earned.

#### Scenario: Auto level
- **WHEN** an officer on auto level is promoted
- **THEN** the new point is spent on the next skill of their station's path
