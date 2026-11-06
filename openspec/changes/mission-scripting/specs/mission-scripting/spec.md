## ADDED Requirements

### Requirement: Missions are files the game checks when it loads them

Every mission SHALL be a JSON file in `data/missions/`. It SHALL be loaded by the one loader in `src/Core/Campaign`, which
refuses an unknown key, a bad expression, or a name its scope does not provide, naming the file and the field.

#### Scenario: A misspelt name in a mission file
- **WHEN** a mission's `where` names `friendly`, which no sector scope provides
- **THEN** loading stops with an error naming `data/missions/<file>.json.where` and the unknown name

### Requirement: A mission's battle is graded from how it ended

A mission's objectives SHALL be checked against the battle as it ended. Its grade SHALL be the file's own rule, or else
the default: lost fleet crushing; every primary met a victory, and every bonus too outstanding; withdrawn short a draw;
otherwise a defeat. Its result SHALL be the file's first outcome rule that holds.

#### Scenario: Breaking out of an ambush
- **WHEN** the fleet withdraws from the Dispatch run's ambush
- **THEN** its primary objective, met by withdrawing, is met, and the grade is a win

#### Scenario: A prize struck
- **WHEN** the prize of Capture the prize is beaten to a quarter of its hull and its escorts are destroyed
- **THEN** it strikes, the battle is won, and Deliver the prize is offered at the home port

### Requirement: Missions are written outside the game

The mission editor SHALL open, check and preview mission files with the game's own code, on Windows and macOS.

#### Scenario: Previewing a mission
- **WHEN** an author opens a mission and previews it won
- **THEN** the editor shows the briefing, the battle as planned and the grade, credits and prestige the game would give
