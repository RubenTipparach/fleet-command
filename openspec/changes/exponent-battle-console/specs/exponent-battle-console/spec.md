## ADDED Requirements

### Requirement: The Exponent has a battle console in its own skin

The battle console SHALL be available in the Exponent skin, with:
- the same rules, panels and controls as the Concord's;
- the Exponent's painted kit, its 24 icons, its type and its colour roles.

Every press SHALL change the console's state exactly as the same press does in the Concord skin.

#### Scenario: Parity
- **WHEN** the parity check presses every control of every named shot, both in the Exponent page and in the Concord page
- **THEN** every press leaves the same state in both pages

### Requirement: One implementation of the Exponent's look

The Exponent's painters, icons and live readouts SHALL live in one library that both the console skins page and the
battle console draw from, and moving them there SHALL leave the console skins page's Exponent unchanged.

#### Scenario: The skins page after the move
- **WHEN** the console skins page shows the Exponent console and its kit sheet
- **THEN** each shot is pixel-identical to the shot taken before the library was made

### Requirement: The Exponent's keys show their state as light

In the Exponent skin, every square key, wide key and status box of the battle console SHALL be a steel inspection port
whose window is lit in the colour of its state, and the plate round the window SHALL be the same in every state.

#### Scenario: A system's status box
- **WHEN** a subsystem is damaged
- **THEN** its status box's window is lit amber and its glyph is drawn dark on the light

### Requirement: No black behind the Exponent's panels

The battle console in the Exponent skin SHALL draw a painted ground behind the column and behind an expanded panel
(CLAUDE.md section 7), and the part of it an expanded panel repeats SHALL be free of the reef's masses.

#### Scenario: An expanded panel
- **WHEN** a panel is expanded to full screen
- **THEN** the ground round it is riveted plate and veins, with no reef repeated across it
