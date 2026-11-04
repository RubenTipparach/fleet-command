## ADDED Requirements

### Requirement: The console takes the skin of the side played

The game SHALL draw the battle console in the console skin of the faction the player commands, the Krai Warhost's
rendered kit for the Krai and the Concord's for the Concord, from one set of panels.

#### Scenario: A Krai scenario in the Krai kit
- **WHEN** the player starts the Krai's Battle
- **THEN** the console is drawn by `KraiKit`, which supplies every piece, hero piece and icon the kit lists

### Requirement: Every skin has every control

In every console state, every skin SHALL present the same controls, each inside the panel that draws it and inside the
window.

#### Scenario: The parity check
- **WHEN** the parity check plays one battle in the Concord and the Krai skins through every station, ship page,
  expanded panel, the menu and the scene list
- **THEN** both skins register the same control ids in every state and no control lies outside its panel
