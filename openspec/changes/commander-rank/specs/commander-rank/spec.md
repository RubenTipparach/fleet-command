## ADDED Requirements

### Requirement: The captain's rank rises with career prestige and never falls

The campaign SHALL keep the captain's career prestige, the sum of every prestige gained, and SHALL NOT lower it for prestige
lost. The rank SHALL be the highest rank in `rules.ranks` whose `career_prestige` it has reached. The campaign map's top bar
SHALL show the rank's title, and the debrief SHALL announce a promotion when a mission raises the rank.

#### Scenario: Promoted
- **WHEN** a rank 1 captain with career prestige 26 wins a mission graded outstanding (10 prestige)
- **THEN** the career prestige is 36, the rank is 2 and the debrief says "Promoted to Commander"

#### Scenario: Never demoted
- **WHEN** a rank 2 captain loses a mission graded crushing (-5 prestige)
- **THEN** the prestige falls by 5 and the rank and career prestige are unchanged

### Requirement: Rank sets the strength and enemy tiers of general missions

A general mission's strength SHALL be rolled from the captain's rank's `strength_weights` when it is offered, and its enemy
hulls SHALL be at most the rank's `tier_max`. Story missions SHALL keep their written forces. The reward SHALL be multiplied
by the strength's `strength_reward_x`.

#### Scenario: A new captain
- **WHEN** 1,000 general missions are offered to a rank 1 captain
- **THEN** none is Heavy, about 70 % are Light, and no enemy hull in their battles is above tier 1

#### Scenario: A senior captain
- **WHEN** 1,000 general missions are offered to a rank 5 captain
- **THEN** none is Light and about 65 % are Heavy
