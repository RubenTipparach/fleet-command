## ADDED Requirements

### Requirement: The main menu offers Campaign, Skirmish, Options and Quit

The main menu SHALL offer exactly Campaign, Skirmish, Options and Quit, and SHALL ask the player to confirm before quitting.

#### Scenario: Quitting asks first
- **WHEN** the player chooses Quit
- **THEN** the game asks for confirmation, and choosing No returns to the main menu with nothing changed

#### Scenario: Options come back to the menu
- **WHEN** the player opens Options, changes the music volume and closes it
- **THEN** the main menu shows again and the new volume is kept
