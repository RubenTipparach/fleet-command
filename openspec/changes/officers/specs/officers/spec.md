## ADDED Requirements

### Requirement: Each station has an officer slot whose officer's abilities the rules use

Every station SHALL show its officer's rank and name, a hover card with their rank, specialty, skill and abilities at
this station, and the rules SHALL apply those abilities: the whole skill in the officer's specialty, half elsewhere.

#### Scenario: The tactical officer's eye
- **WHEN** a Veteran tactical officer is in the Tactical slot
- **THEN** the hover card shows "Chance to hit +6 %" and every chance to hit the console shows is 6 points higher

#### Scenario: Out of specialty
- **WHEN** the captain clicks the Tactical slot and a science officer from reserve takes it
- **THEN** the card names their specialty, says "half skill", and the bonus falls to half their skill
