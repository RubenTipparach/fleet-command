## ADDED Requirements

### Requirement: The Axiom has a battle console in its own skin

The battle console SHALL be available in the Axiom skin, with:
- the same rules, panels and controls as the Concord's;
- the Axiom's kit drawn by its rules, its 24 icons, its type and its colour roles.

Every press SHALL change the console's state exactly as the same press does in the Concord skin.

#### Scenario: Parity
- **WHEN** the parity check presses every control of every named shot, both in the Axiom page and in the Concord page
- **THEN** every press leaves the same state in both pages

### Requirement: One implementation of the Axiom's look

The Axiom's painters, icons and live readouts SHALL live in one library that both the console skins page and the battle
console draw from, and moving them there SHALL leave the console skins page's Axiom unchanged.

#### Scenario: The skins page after the move
- **WHEN** the console skins page shows the Axiom console and its kit sheet
- **THEN** each shot is pixel-identical to the shot taken before the library was made

### Requirement: The Axiom's strips leave the readouts their room

In the Axiom skin, the art at each end of the battle console's strips SHALL stay inside the 92 px the console leaves
before its readouts start.

#### Scenario: The top strip
- **WHEN** the console wears the Axiom skin
- **THEN** the depth ladders of the top and bottom strips end before 92 px from each end
