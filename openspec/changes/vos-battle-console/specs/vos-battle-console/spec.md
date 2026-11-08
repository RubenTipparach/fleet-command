## ADDED Requirements

### Requirement: The Vos Syndicate has a battle console in its own skin

The battle console SHALL be available in the Vos Syndicate skin, with:
- the same rules, panels and controls as the Concord's;
- the Syndicate's hand-drawn kit, its 24 icons, its type and its colour roles.

Every press SHALL change the console's state exactly as the same press does in the Concord skin.

#### Scenario: Parity
- **WHEN** the parity check presses every control of every named shot, both in the Vos page and in the Concord page
- **THEN** every press leaves the same state in both pages

### Requirement: The Vos look has one implementation

The console skins page and the battle console SHALL draw the Vos Syndicate's kit, icons and live readouts from the one
library, `docs/mockups/lib/vos.js`, and every Vos piece SHALL be drawn from a seeded generator, the same every time.

#### Scenario: A piece on both pages
- **WHEN** the console skins page and the battle console each draw the Vos name plate
- **THEN** both call the same painter with the same seed

### Requirement: Vos keys show their state as the wash inside the ink

In the Vos skin, every square key, wide key and allocation cell of the battle console SHALL draw the same ink strokes in
every state, and only the wash inside them SHALL change with the state.

#### Scenario: A key lit
- **WHEN** a station key goes from at rest to chosen
- **THEN** its tag's outline is unchanged and its wash goes from dark wine to verdigris
