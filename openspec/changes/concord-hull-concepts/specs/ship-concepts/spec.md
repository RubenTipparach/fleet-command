## ADDED Requirements

### Requirement: Concord hulls belong to the Tern family

A Concord hull SHALL be an octagonal hull lofted through stations of `[z, half beam, top, bottom, chamfer]` in metres, the format of Star Crew Bridge Simulator's ship data, painted with white armour over a grey subhull, a grey service band at the waterline and grey dorsal channels.

#### Scenario: A station is read the family's way
- **WHEN** a Concord class's stations are lofted
- **THEN** each station is an octagon with its four corners chamfered by the station's chamfer, and the hull passes through every station

### Requirement: Windows only where crew live

A window SHALL be placed only along a class's lived-in length, 1.5 m above a deck floor, and never on a livery stripe or the service band.

#### Scenario: The stripe is clear
- **WHEN** a window would fall within 2.2 m of a livery stripe's station
- **THEN** no window is placed there

### Requirement: Nacelles stand clear of the hull

Every nacelle SHALL be held on a pylon with daylight between it and the hull, in every pylon arrangement (level, one upright, two upright), and the page SHALL show the measured daylight.

#### Scenario: An upright nacelle
- **WHEN** a class is shown with one upright pylon
- **THEN** its nacelle's underside is at least 3.0 m above the highest hull point beneath it

### Requirement: A class is counted by its nacelles

A larger Concord class SHALL carry more nacelles in pairs or the same count, never thicker nacelles than its shape rule gives.

#### Scenario: Corvette to cruiser
- **WHEN** the corvette, frigate and cruiser stand side by side in the level arrangement
- **THEN** the corvette and frigate carry two nacelles and the cruiser four
