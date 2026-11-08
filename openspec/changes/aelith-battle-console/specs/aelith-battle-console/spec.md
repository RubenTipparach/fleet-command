## ADDED Requirements

### Requirement: The Aelith Conclave has a battle console in its own skin

The battle console SHALL be available in the Aelith Conclave skin, with:
- the same rules, panels and controls as the Concord's;
- the Conclave's rendered gold shells with their holograms drawn as vectors in the wells, its 24 icons, its type and
  its colour roles.

Every press SHALL change the console's state exactly as the same press does in the Concord skin.

#### Scenario: Parity
- **WHEN** the parity check presses every control of every named shot, both in the Aelith page and in the Concord page
- **THEN** every press leaves the same state in both pages

### Requirement: The Aelith console's readouts are lit on a veiled void

In the Aelith skin, every panel, inset and strip of the battle console SHALL hold its readouts in its shell's well,
the void veiled so that every colour role measures a WCAG contrast ratio of at least 4.5 against it, except the
receding roles (ghost and deep).

#### Scenario: A panel's readouts
- **WHEN** the console wears the Aelith skin
- **THEN** each panel's labels and values sit inside the veiled well of `frame.panel`, inside the panel's padding

### Requirement: No black behind the Aelith column

In the Aelith skin, the battle console's column and an expanded panel SHALL stand on the rendered gold backing
(`console.backing`), never on black.

#### Scenario: The column's ground
- **WHEN** the console wears the Aelith skin with no panel expanded
- **THEN** the gaps between the column's panels show the gold backing
