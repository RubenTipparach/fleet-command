## ADDED Requirements

### Requirement: Windows are person sized on every ship

Every window on every ship SHALL be the same person-sized window (1.1 m by 0.6 m, observation windows 3 m), set in rows one deck (3.4 m) apart, so the number of windows and rows tells the size of the ship.

#### Scenario: Counting decks
- **WHEN** a corvette and a cruiser are seen side by side
- **THEN** their windows are the same size, and the cruiser shows more rows and more windows per row

### Requirement: Concord ships read as a head and legs

A Concord ship SHALL have a disc (the head) carrying a bridge crown on top, and nacelles carried on pylons with a knee bend (the legs).

#### Scenario: The silhouette from above
- **WHEN** a Concord ship is seen from directly above or from the side
- **THEN** the disc, the crown and two nacelles on bent pylons are each distinct in the outline

### Requirement: Marking density is set per class

Each ship class SHALL carry a marking density (ceremonial, working or minimal) in its data, and its markings SHALL follow that density.

#### Scenario: A working frigate carries service markings
- **WHEN** the Lantern frigate and the Swift corvette are compared
- **THEN** the frigate shows hatch numbers, pod rings, docking targets and walkway lines, and the corvette shows only its stripe and registry

### Requirement: Registry and features read in low light

The registry, the name, the crest and the hull plating SHALL be lit by floodlights baked into the ship's emission, so they read with the sun behind the ship or with no sun.

#### Scenario: Shadow side
- **WHEN** the ship is lit only from behind
- **THEN** the registry, name and crest are legible in their light pools, the windows are lit and the navigation lights show
