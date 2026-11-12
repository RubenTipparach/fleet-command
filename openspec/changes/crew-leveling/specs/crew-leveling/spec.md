## ADDED Requirements

### Requirement: An ensign brings nothing until promoted

An officer SHALL have one skill point for each level above the first, and a level 1 ensign SHALL add nothing to any ability.

#### Scenario: A new ensign at the helm
- **WHEN** a level 1 ensign is the helm officer in a battle
- **THEN** the helm's abilities are those of an empty slot

### Requirement: Officers gain experience and are promoted for skill points

Every officer of the crew SHALL gain experience from each battle by its grade and from each campaign turn, and a promotion
SHALL spend the next level's experience (any shortfall paid in prestige at the data's rate) and give one skill point.

#### Scenario: Three victories
- **WHEN** an ensign with no experience serves in three victories
- **THEN** they can be promoted to level 2 (Lieutenant) without prestige, and have one skill point to place

### Requirement: Crew is traded and hired for prestige

Transferring an officer out SHALL give their level's worth plus their banked experience in prestige, and hiring from the
pool SHALL cost prestige by level.

#### Scenario: An ensign traded in
- **WHEN** a level 1 ensign with 300 experience banked is transferred out
- **THEN** the side gains 6 prestige and the station is vacant

### Requirement: Skills are learned on a pyramid, by hand or by auto level

A skill SHALL be learned with one point only once its prerequisites are met: every skill in its `all` list and at least one
in its `any` list. An officer on auto level SHALL spend each point up their station's path as it is earned.

#### Scenario: A skill that crosses over
- **WHEN** an officer has learned only the leftmost base skill of a station
- **THEN** the one tier-2 skill resting on it is open, and no other tier-2 skill is

#### Scenario: The apex needs both below it
- **WHEN** an officer has learned one of the two tier-4 skills
- **THEN** the apex stays closed until the other is learned too

#### Scenario: Auto level
- **WHEN** an officer on auto level is promoted
- **THEN** the new point is spent on the next skill of their station's path
