## ADDED Requirements

### Requirement: Each subsystem shows its state on the ship

Each ship subsystem (nacelles, reactor, bridge, weapons, shields, impulse engines, life support, hull) SHALL show each of its four states (healthy, damaged, critical, destroyed) on the 3D ship with an effect no other subsystem uses.

#### Scenario: A coolant leak means the reactor
- **WHEN** the reactor falls below 66% health
- **THEN** white-cyan coolant gas vents from the engineering hull's belly, and no other subsystem vents that gas

#### Scenario: A dark section means lost power
- **WHEN** a saucer section's life support is critical
- **THEN** that section's windows go dark and the rest stay lit

### Requirement: A ship can fly on one nacelle

Destroying a nacelle SHALL detach it from the ship as a separate piece, and the ship SHALL keep moving at reduced speed on the remaining nacelle.

#### Scenario: Losing a nacelle
- **WHEN** the port nacelle's health reaches zero
- **THEN** it snaps at the pylon's knee and drifts away burning, and the ship still moves

### Requirement: Broken edges are charred toward the break

A piece broken off a ship SHALL be charred more strongly the nearer it is to the broken edge, with a glowing line along the break that cools over time.

#### Scenario: The break-up
- **WHEN** the reactor is destroyed
- **THEN** the ship comes apart into irregular chunks of mixed size, each charred toward its broken edges

### Requirement: A broken piece is never hollow

Every face a break exposes SHALL show the ship's interior structure (skin, deck plates, bulkheads) and rooms with depth, never the inside of an empty shell.

#### Scenario: Looking into a chunk
- **WHEN** a saucer chunk tumbles and its broken face turns toward the camera
- **THEN** decks and bulkheads are visible in the face and rooms behind it shift with parallax as it turns

### Requirement: One damage function

Every source of damage (a weapon hit, a system button, a scenario) SHALL go through the same damage function, and the look SHALL be derived only from the resulting subsystem states.

#### Scenario: A click and a button agree
- **WHEN** the nacelle is reduced to 30% by clicks on it, or by the system button
- **THEN** it looks the same either way
