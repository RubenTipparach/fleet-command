## ADDED Requirements

### Requirement: A skirmish is a preset scenario with a point budget

The skirmish screen SHALL list the preset scenarios, each with a briefing and a point budget, and SHALL let the player scale the budget to Small, Standard or Large.

#### Scenario: Picking a scenario sets the budget
- **WHEN** the player picks a scenario whose budget is 900 points at Standard and chooses Large
- **THEN** the fleet budget is 1350 points

### Requirement: A fleet stays within its budget and its size

A skirmish fleet SHALL hold one to four ships, the first being the flagship, and SHALL NOT launch while its spending exceeds the budget.

#### Scenario: Over budget cannot launch
- **WHEN** the fleet's hulls and loadouts cost more than the budget
- **THEN** Launch is refused and the budget bar shows by how much

#### Scenario: A fifth ship is refused
- **WHEN** the fleet holds four ships and the player adds another
- **THEN** the ship is not added and the fleet stays at four

### Requirement: A skirmish ends back at the main menu

Ending a skirmish battle SHALL show its results and then return to the main menu.

#### Scenario: Victory to menu
- **WHEN** the player wins a skirmish and continues from the results
- **THEN** the main menu shows
