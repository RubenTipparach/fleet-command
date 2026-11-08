## ADDED Requirements

### Requirement: The Vhorr Brood has a battle console in its own skin

The battle console SHALL be available in the Vhorr Brood skin, with:
- the same rules, panels and controls as the Concord's;
- the Brood's kit sculpted on the Organic Panel Bible at the battle console's own slot sizes, its 24 icons, its type and
  its colour roles.

Every press SHALL change the console's state exactly as the same press does in the Concord skin.

#### Scenario: Parity
- **WHEN** the parity check presses every control of every named shot, both in the Vhorr page and in the Concord page
- **THEN** every press leaves the same state in both pages

### Requirement: The Vhorr column is one grown body at the console's sizes

In the Vhorr skin, the battle console's column (the name plate, the station, ship and weapons panels, the radar's pocket
and the crest, on the tissue backing) SHALL be sculpted at the console's own rectangles from a blockout written from the
console's rectangles, with growth kept at least 2 mm clear of every screen and title (the bible's rule G1), and drawn
whole, never nine-sliced.

#### Scenario: The fit of the sculpt
- **WHEN** the sculptors run over the battle blockout with `--fit-only`
- **THEN** every protected rectangle's clearance is at least 2 mm
