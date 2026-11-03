## ADDED Requirements

### Requirement: Concord hulls are built inside out

A Concord hull SHALL be the wrap of its zones (rooms and spaces with a kind, a size and a deck count) plus clearance, and every zone SHALL lie inside its hull, measured.

#### Scenario: A zone outside its hull is caught
- **WHEN** a zone's corner lies outside the hull piece that wraps it
- **THEN** the containment check reports it and names the zone

### Requirement: Windows only where a room is

A window SHALL be placed only where a crew, commons, medical or command zone meets the hull, one row per deck.

#### Scenario: Machinery has no windows
- **WHEN** a hull piece wraps only machinery
- **THEN** it carries no windows

### Requirement: The Concord reads at 40 pixels

A Concord hull SHALL show its faction tell (an opening the stars show through) in a 40 pixel silhouette from above.

#### Scenario: The silhouette test
- **WHEN** the frigate is rendered as a 40 pixel silhouette from above
- **THEN** the opening is visible as background inside the outline

### Requirement: A class is counted, not fattened

A larger Concord class SHALL differ from a smaller one by the count of its repeated element (rings, lantern pairs or sail pairs), not by thicker parts.

#### Scenario: Corvette to cruiser
- **WHEN** the corvette, frigate and cruiser stand side by side
- **THEN** the count of the repeated element rises with the class
