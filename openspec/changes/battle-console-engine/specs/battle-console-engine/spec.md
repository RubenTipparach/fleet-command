## ADDED Requirements

### Requirement: The game loads only validated data

The game SHALL load weapons, classes, factions, the skirmish, tuning and the console layout from `data/`, and SHALL
refuse to start on an unknown key, a missing required field, a non-finite number or a reference to something that does
not exist, naming the file and the field.

#### Scenario: A misspelt key stops startup
- **WHEN** `data/battle/weapons.json` holds `rnage_km` in the lance
- **THEN** loading fails with an error naming `data/battle/weapons.json.lance.rnage_km` as an unknown key

### Requirement: The battle replays from its seed

The battle SHALL advance in fixed steps with randomness forked per subsystem from the scenario's seed, so the same seed
and orders give the same battle.

#### Scenario: Two runs of the golden seed agree
- **WHEN** the skirmish is played twice for 1,800 steps with the same orders
- **THEN** every ship's position, heading, hull and shields are the same in both runs and match the pinned values

### Requirement: The console keeps its layout in the engine

The game's console SHALL place every panel at the rectangle `ConsoleLayout` gives for the window, clipped, at any window
width, and SHALL expand any panel to the left two thirds with the live battle in the right third.

#### Scenario: Layout test across window sizes
- **WHEN** the layout test runs at 1920 x 1080, 2560 x 1080 and 1500 x 1080, collapsed and with each panel expanded
- **THEN** every panel is at its designed rectangle and clipped, and the left column's panels never change size

### Requirement: Readouts and outcomes share one rule

The chance to hit shown on a mount's hover card, in the expanded weapons table and in the view's readout SHALL be the
value the shot rolls against.

#### Scenario: The hover card shows the rolled chance
- **WHEN** the player points at a lance on the systems display with a target locked
- **THEN** the card's chance to hit is `Rules.HitChance` for that mount and target, the same call `Battle.Fire` rolls
