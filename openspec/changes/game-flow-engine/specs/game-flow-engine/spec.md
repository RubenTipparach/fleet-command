## ADDED Requirements

### Requirement: The game opens on the main menu and plays a campaign

The game SHALL open on the main menu. From it, the player SHALL be able to:
- pick a faction;
- start or continue a campaign;
- move on the hex map;
- end turns while the factions move;
- take a mission's briefing, fight its battle and see its debrief;
- return to the map.

#### Scenario: A first mission
- **WHEN** a new Concord campaign starts and the fleet flies to a patrol and launches it
- **THEN** the battle console plays the patrol's battle with its objectives and Withdraw key, and at its end the debrief
  shows the grade, credits, prestige and reputation, and Continue returns to the map

### Requirement: The campaign draws the mockup's map

A new campaign SHALL grow its territory and terrain from `data/campaign/campaign.json` with the mockup's random generator,
so the same seed gives the page's map.

#### Scenario: The same territory
- **WHEN** a new campaign is built with seed 7
- **THEN** every sector of row 7 that is not a port or a star has the holder and terrain the page's generator gives it

### Requirement: A saved campaign continues exactly

A campaign SHALL save its whole state, its random generator's state included, and a loaded save SHALL play on exactly
as the unsaved one would.

#### Scenario: Save, load, end turn
- **WHEN** a campaign is saved and loaded, and both copies end a turn
- **THEN** their saves are identical
