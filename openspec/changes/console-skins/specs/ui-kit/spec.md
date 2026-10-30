## ADDED Requirements

### Requirement: Screens draw only named kit pieces

Every screen SHALL draw its frames, bars, buttons, tabs, strips and hero elements only from the named pieces of the UI kit, so that swapping the active skin changes the look of every screen without changing any layout.

#### Scenario: Swapping the skin keeps the layout
- **WHEN** the active skin changes from Concord to Vos Syndicate
- **THEN** every panel keeps its position and size and shows the Vos pieces

### Requirement: Every skin supplies every piece

A skin SHALL supply an image for every piece and state in the kit, and a skin that lacks one SHALL fail validation naming the skin and the piece.

#### Scenario: A missing button state
- **WHEN** a skin has no pressed state for `button`
- **THEN** validation fails naming the skin and `button` pressed

### Requirement: 9-slice pieces keep their corners at any size

A 9-slice piece SHALL draw its corners at their native size and stretch or repeat only its edges and centre, using the margins recorded for it.

#### Scenario: A frame stretched three ways
- **WHEN** `frame.panel` is drawn at 96 x 96, 460 x 300 and 1200 x 140
- **THEN** its corners are identical in all three and no corner detail is stretched
