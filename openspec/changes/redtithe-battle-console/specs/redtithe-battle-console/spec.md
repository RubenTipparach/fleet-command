## ADDED Requirements

### Requirement: The Red Tithe has a battle console in its own skin

The battle console SHALL be available in the Red Tithe skin, with:
- the same rules, panels and controls as the Concord's;
- the Tithe's painted kit, its 24 icons, its type and its colour roles.

Every press SHALL change the console's state exactly as the same press does in the Concord skin.

#### Scenario: Parity
- **WHEN** the parity check presses every control of every named shot, both in the Red Tithe page and in the Concord page
- **THEN** every press leaves the same state in both pages

### Requirement: One implementation of the Red Tithe's look

The Tithe's painters, icons and live readouts SHALL live in one library that both the console skins page and the battle
console draw from, and moving them there SHALL leave the console skins page's Red Tithe unchanged.

#### Scenario: The skins page after the move
- **WHEN** the console skins page shows the Red Tithe console and its kit sheet
- **THEN** each shot is pixel-identical to the shot taken before the library was made

### Requirement: The Red Tithe's readouts are chalked on tar

In the Red Tithe skin, every panel, inset and strip of the battle console SHALL hold its readouts on a field of tarred
board, and the trophies, scraps, stains and scars of each piece SHALL stay in its margins, clear of every readout.

#### Scenario: A panel's readouts
- **WHEN** the console wears the Red Tithe skin
- **THEN** each panel's labels and values sit on the tarred field inside the panel's padding, and the knife, the scrap of
  sheet and the corner straps lie outside it

### Requirement: The Red Tithe's keys show their state by what is in their slot

In the Red Tithe skin, every square key, wide key and status box SHALL be an oak block with a slot, empty at rest and
holding a slip of paper tinted by the key's state when engaged, and barred with iron when off.

#### Scenario: A destroyed system
- **WHEN** a subsystem is down
- **THEN** its status box holds a blood-soaked slip and its glyph is drawn black on it
