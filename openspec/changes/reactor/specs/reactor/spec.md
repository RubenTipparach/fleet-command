## ADDED Requirements

### Requirement: Misaligned confinement cuts the output

The reactor's output SHALL be its rating less 60% of the mean misalignment of its three confinement coils, and a hit
SHALL be able to knock a coil out of alignment. The captain SHALL be able to nudge each coil.

#### Scenario: Realigning restores the output
- **WHEN** coil 1 is at +0.30 and the others aligned on a 22 E reactor, and the captain nudges coil 1 back to 0
- **THEN** the output rises from 20.7 E to 22 E

### Requirement: Coolant offline melts the reactor down

Cooling SHALL be the sum of each loop's flow times its health. Cooling at zero, or the core at its limit, for 20 s
SHALL explode the reactor, destroying the ship and damaging ships within 3 km.

#### Scenario: Pumps off
- **WHEN** both coolant loops' flow is set to 0
- **THEN** the ship survives 19 s and its reactor explodes before 21 s

### Requirement: Drawing past the output is a setting, off by default

With the setting off, no allocation SHALL exceed the output. With it on, it may; the deficit SHALL drain the battery,
and with the battery empty every system SHALL brown out to output divided by draw.

#### Scenario: Off caps the allocation
- **WHEN** the setting is off and the captain sets weapons to 10 boxes
- **THEN** weapons take only what the output leaves

#### Scenario: On drains the battery
- **WHEN** the setting is on and the draw exceeds the output
- **THEN** the battery falls to zero, and then every system runs at output divided by draw

### Requirement: Power and Systems read on one scale

The bottom strip SHALL show the output (Power, blue) above the draw (Systems, green) on the reactor's rating, Systems red
when it exceeds Power, and the battery beside them.

#### Scenario: A damaged reactor shows its deficit
- **WHEN** the output falls to 18 E under a 23 E draw
- **THEN** Power reads 18.0 E, Systems reads 23.0 E in red, and the battery bar falls

### Requirement: The reactor is worked from a diagram

The Reactor mode SHALL show the core, the three coils and the two coolant loops as a diagram. Dragging a coil SHALL set
its alignment, and the minus and plus keys on a loop's pump SHALL set its flow.

#### Scenario: Dragging a coil home
- **WHEN** coil 1 is at +0.56 and the captain drags it onto its ring
- **THEN** its alignment is 0 and it shows green
