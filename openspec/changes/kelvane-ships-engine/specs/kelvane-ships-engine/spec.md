## ADDED Requirements

### Requirement: The battle draws the Kelvane Institute's approved ships

A Kelvane class with a `model` SHALL be drawn in the battle as its glb, exported from mockup 23, dressed from the export
manifest with its own baked maps by the baked hull shader, scaled to the class's battle length, and it SHALL come apart
as the Hunt's ships do.

#### Scenario: The Institute from the Concord's side
- **WHEN** the Concord's scenario The Institute starts
- **THEN** the Institute's battlecruiser, frigate, destroyer and carrier are drawn as their baked ships, and no ship is a stand-in

#### Scenario: The Patience in Four races
- **WHEN** a Four races scene starts
- **THEN** the Patience is drawn as the Kelvane cruiser, not a stand-in

### Requirement: A carrier's craft wait on its landing pads

A ship whose glb has landing pads naming a craft SHALL show that craft's own approved model on its pads, one for each
craft in its bay, standing up each pad's normal, so a craft on a ceiling hangs upside down; launched craft leave their
pads empty, and a ship that has come apart shows none.

#### Scenario: The Kelvane carrier launches its fighters
- **WHEN** the Kelvane carrier, with 12 fighters in its bay, launches them
- **THEN** 12 of its 24 pads hold a fighter before the launch and none after

### Requirement: The Kelvane Institute is a side the player commands

The Institute SHALL be playable as its own side in its approved ships, its console its own skin.

#### Scenario: The Institute's scenarios
- **WHEN** the skirmish list is read
- **THEN** Battle, Duel, Arming and Four races can each be played as the Kelvane Institute, from the Patience
