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

Every nacelle SHALL be held on a pylon with at least 3.0 m of daylight between it and every hull along its whole length, in every layout (one below, above and below, level), and the page SHALL show the measured daylight.

#### Scenario: A nacelle under the hull
- **WHEN** the Swift is shown with one nacelle below
- **THEN** the nacelle's top is at least 3.0 m below the lowest hull point above it

### Requirement: The cruiser's pylons come out of a secondary hull

The Albatross SHALL be two hulls, a primary hull holding the decks and a secondary hull under it joined by a neck, and its nacelle pylons SHALL root in the secondary hull.

#### Scenario: Where the pylons start
- **WHEN** the Albatross is built
- **THEN** each pylon's root lies inside the secondary hull's beam and its tip at the nacelle
