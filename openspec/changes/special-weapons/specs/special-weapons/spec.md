## ADDED Requirements

### Requirement: Each finished faction fields two special weapons of its own

The Concord SHALL field the Chorus lance and the Quell torpedo, and the Krai Warhost the Forge driver and the Blood
charge, with their numbers read from `data/battle/special_weapons.json`; no other faction SHALL field them.

#### Scenario: The chorus grows with every ship that joins
- **WHEN** an Albatross fires its chorus lance with two Concord ships within 8 km holding ready lances
- **THEN** both may join, each spends its lance, and the beam deals 7 x (1 + 0.35 x 2) = 11.9 a second for 2.5 s

#### Scenario: A quell silences one half of a ship
- **WHEN** a quell torpedo strikes a ship's port facing
- **THEN** that facing's shield drops to 0, no damage is dealt, and every mount within 90 degrees of the facing cannot
  fire for 8 s

#### Scenario: A forge spike goes through a shield
- **WHEN** a forge driver hits a facing whose shield holds 28
- **THEN** 7.2 goes to the hull at once, the shield takes 10.8, and the hull burns for 1 a second for 6 s

#### Scenario: A blood charge hits harder the faster it closes
- **WHEN** a Krai frigate rams at 660 m/s closing
- **THEN** its bayonet deals 33 to the hull whatever the struck facing's shield, and 9.9 comes back on the frigate's bow
