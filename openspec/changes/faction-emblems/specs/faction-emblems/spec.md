## ADDED Requirements

### Requirement: Every faction has one emblem drawn from one source
Each faction SHALL have exactly one emblem, made of a field, at most one device colour and cuts, and its console crest,
its small mark and its hull marking SHALL all be drawn from the same shapes.

#### Scenario: The crest and the mark agree
- **WHEN** the Krai Warhost's emblem shapes change
- **THEN** the Krai console crest, the Krai mark in the contacts list and the Krai hull marking all change with them, and none is edited by hand

### Requirement: An emblem reads in one colour
Every device SHALL sit in a window cut through the field at least 5 units wider than itself on the emblem's 100 unit
grid, so the emblem reads painted in a single colour.

#### Scenario: A one-colour mark at 20 px
- **WHEN** any emblem is drawn in one colour at 20 px
- **THEN** its device is separated from its field by at least 1 px of ground

### Requirement: No two factions share an outline
No two emblems SHALL share an outer shape class, so a faction is known by its outline alone at 20 px.

#### Scenario: The ten side by side
- **WHEN** all ten emblems are shown in one colour at 20 px
- **THEN** each outline (ring, heater shield, bitten coin, egg in ribs, drop, stepped throne, triangle, crescent and eye, gauge arch, target brackets) is different

### Requirement: Emblems are sized to be read
An emblem SHALL be drawn at 96 x 96 px as the console crest, at 40 px in lists and at 20 px in tags and rows, and a
hull marking SHALL be at least 6 % of the ship's length tall.

#### Scenario: A frigate at battle distance on a phone
- **WHEN** an 88 m frigate fills 40 % of an 844 px wide phone screen
- **THEN** its 5.3 m hull marking is at least 20 px tall

### Requirement: An emblem is not an icon
An emblem SHALL name a people and SHALL never be used as the icon for a station, system or action.

#### Scenario: A target's tag beside the systems icons
- **WHEN** a hostile Krai target's tag sits in a row of icons at 20 px
- **THEN** the Krai mark is shown as the target's faction and no icon in the row uses an emblem's picture
