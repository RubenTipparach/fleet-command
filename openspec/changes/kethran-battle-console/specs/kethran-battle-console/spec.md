## ADDED Requirements

### Requirement: The Kethran Hegemony has a battle console in its own skin

The battle console SHALL be available in the Kethran Hegemony skin, with:
- the same rules, panels and controls as the Concord's;
- the Hegemony's rendered kit, its 24 icons, its type and its colour roles.

Every press SHALL change the console's state exactly as the same press does in the Concord skin.

#### Scenario: Parity
- **WHEN** the parity check presses every control of every named shot, both in the Kethran page and in the Concord page
- **THEN** every press leaves the same state in both pages

### Requirement: The Kethran keys are the rendered keycaps, lit by state

In the Kethran skin, every square key of the battle console SHALL be the helm pad's rendered keycap and every wide key
the rendered wedge key, with the state shown as the light of the cap's face and the cap's own shading kept.

#### Scenario: A key engaged
- **WHEN** a station key is chosen
- **THEN** its cap glows ember with the render's bevel and highlight still on it

### Requirement: The Kethran column stands on gunmetal

In the Kethran skin, the battle console's column and an expanded panel SHALL be drawn over a gunmetal backing plate,
never over black.

#### Scenario: The column
- **WHEN** the console wears the Kethran skin with no panel expanded
- **THEN** the space between the column's panels shows the bolted gunmetal plate
