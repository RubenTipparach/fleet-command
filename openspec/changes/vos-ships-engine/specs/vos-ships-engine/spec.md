## ADDED Requirements

### Requirement: The battle draws the Vos Syndicate's approved ships

A Vos class with a `model` SHALL be drawn in the battle as its glb, exported from mockup 37, dressed from the export
manifest with its own baked maps by the baked hull shader, with its antennas, its lenses and its containers, scaled to the
class's battle length, and it SHALL come apart as the Hunt's ships do.

#### Scenario: The Syndicate from the Concord's side
- **WHEN** the Concord's scenario The Syndicate starts
- **THEN** the Drum, the Cross, the Boarder, the Broadside and the Deck runner are drawn as their baked ships, and no ship is a stand-in

### Requirement: A glowing mouth is a lens, not paint

A ship whose glb has `lens_*` empties SHALL draw a lens on each, facing out along its normal at its radius, glowing by its
shader, and a lens on a piece that breaks away SHALL go out with the piece.

#### Scenario: The Cross's emitters
- **WHEN** the Cross is drawn
- **THEN** each of its four tractor emitters and its drives carries a lens

### Requirement: The Vos line replaces its placeholder

The game SHALL carry the Vos as the five classes of `data/ships/vos/line.json` and no placeholder Vos class.

#### Scenario: The class list
- **WHEN** the classes are loaded
- **THEN** vos_drum, vos_cross, vos_boarder, vos_broadside and vos_deck_runner are there, and vos_ff, vos_dd, vos_ca and vos_cap are not
