## ADDED Requirements

### Requirement: The Ssarith Hunt console in the game

The game SHALL draw the battle console in the Ssarith Hunt skin from its rendered kit and its own icons, type and colour
roles, from the same panels as every other skin.

#### Scenario: The skin supplies every piece
- **WHEN** the Ssarith Hunt skin is worn
- **THEN** `KitPieces.Verify` finds every piece, hero piece, icon and feature piece the kit lists

#### Scenario: The parity check
- **WHEN** the parity check plays one battle in every skin through every station, ship page, expanded panel, the menu and
  the scene list
- **THEN** the Ssarith Hunt skin registers the same control ids as the Concord's in every state, each inside its panel
