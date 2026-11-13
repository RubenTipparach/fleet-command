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

### Requirement: Skills are learned on a tree, by hand or by auto level

A skill SHALL be learned with one point only once its prerequisites are met: every skill in its `all` list and at least one
in its `any` list. An officer on auto level SHALL spend each point along their station's path as it is earned.

#### Scenario: A crossover
- **WHEN** an officer has learned the left lane's first two skills and nothing in the centre lane
- **THEN** the crossover between the left and centre lanes is open

#### Scenario: The keystone needs both crossovers
- **WHEN** an officer has learned one of the two crossovers
- **THEN** the keystone stays closed until the other is learned too

#### Scenario: Auto level
- **WHEN** an officer on auto level is promoted
- **THEN** the new point is spent on the next skill of their station's path

### Requirement: Every skill shows what it does

Every skill SHALL show an icon of what it changes, and hovering, focusing or tapping it SHALL show what it changes, what it
needs and whether it can be learned.

#### Scenario: A locked keystone
- **WHEN** the player hovers over a station's keystone with one crossover learned
- **THEN** the tooltip names both crossovers it needs and says it is locked

### Requirement: The flagship's slots show the campaign crew
In a campaign battle the flagship's station slots SHALL hold the campaign officers, each in the battle stations their
station covers, and hovering over a slot SHALL show that officer's level, rank, experience against the next level and
their learned skills summed into one short bonus line per ability.

#### Scenario: An engineering officer covers power and repair
- **WHEN** the crew has a level 4 engineering officer with one skill learned and no helm officer
- **THEN** the flagship's power and repair slots hold that officer, whose card shows level 4, their experience and that skill's bonus
- **AND** the helm slot is vacant
