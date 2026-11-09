## ADDED Requirements

### Requirement: The Tarsine Remnant has a battle console in its own skin

The battle console SHALL be available in the Tarsine Remnant skin, with:
- the same rules, panels and controls as the Concord's;
- the Remnant's rendered kit, its 24 icons, its type and its colour roles.

Every press SHALL change the console's state exactly as the same press does in the Concord skin.

#### Scenario: Parity
- **WHEN** the parity check presses every control of every named shot, both in the Tarsine page and in the Concord page
- **THEN** every press leaves the same state in both pages

### Requirement: The Remnant's keys keep their dirt in every state

In the Tarsine skin, every square key and wide key of the battle console SHALL show the same rendered dirt in every
state, and only the backlight behind the glass SHALL change with the state.

#### Scenario: A key lit
- **WHEN** a station key goes from at rest to chosen
- **THEN** the dirt on its glass is unchanged and the light behind it goes from dim to full gold

### Requirement: The Remnant's column stands on iron

In the Tarsine skin, the battle console's column and an expanded panel SHALL be drawn over a riveted iron backing
plate of worked iron (rendered in Blender, not painted), never over black.

#### Scenario: The column
- **WHEN** the console wears the Tarsine skin with no panel expanded
- **THEN** the space between the column's panels shows the riveted iron plate

### Requirement: The Remnant's panel faces are worked iron, never a stretched texture

In the Tarsine skin, the face of every battle console panel SHALL be worked iron rendered in Blender (grain, hammer
facets, a sheen, soot and rust in its crevices), never a pane of glass or a painted smear. The pieces the console draws
large (the column with its three docked panels, its backing, the expanded panel's frame and the strips) SHALL be
rendered at the size the console draws them at the 1920 x 1080 reference, and every other nine-sliced piece SHALL carry
no texture in a band the console stretches.

#### Scenario: An expanded panel
- **WHEN** the Weapons panel is expanded on a 1920 x 1080 screen
- **THEN** its frame is drawn from a render of 1268 x 1040 reference px, at one image px per reference px times the
  render scale, and its face shows the iron's grain at its rendered size

#### Scenario: The docked column
- **WHEN** the console wears the Tarsine skin with no panel expanded
- **THEN** the column and its three panels are one rendered plate of 480 x 1080 reference px, drawn whole
