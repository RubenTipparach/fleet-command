## ADDED Requirements

### Requirement: A Vos ship is made of freighter parts and taken parts

Every Vos ship on mockup 37 SHALL be built on a freighter's drive block and spine from the game, or on a captured
warship, with the game's containers and parts taken from other factions' approved ships. No Vos hull SHALL be sculpted
new, except its fighters. A battle freighter's layout SHALL be the Syndicate's own: no other faction's ship is arranged the same way.

#### Scenario: Reading a conversion
- **WHEN** the owner picks a conversion in the list
- **THEN** it is shown alone, orbitable, with what it starts as and what was taken

### Requirement: Taken parts are the game's own ships at true size

Every taken part SHALL be copied from the glb the game flies, never redrawn and never scaled.

#### Scenario: A nacelle on a freighter
- **WHEN** the Chaser carries a Concord Albatross's nacelles
- **THEN** each nacelle is the Albatross's own mesh at its own size

### Requirement: Only the joining metal is the Syndicate's

Everything the Syndicate makes itself SHALL be a closed skin grown on `docs/mockups/lib/hull-grow.js` in its own plate,
and SHALL cover each cut it closes.

#### Scenario: A cut bow
- **WHEN** a freighter's bow is cut away
- **THEN** a collar a little larger than the measured section covers the cut

### Requirement: No Vos-made bow ends in a point

Every bow the Syndicate welds SHALL end in a flat face.

#### Scenario: The Ram
- **WHEN** the Ram is seen from above
- **THEN** its head ends in a flat face 52 m wide, not a point

### Requirement: The Vos line is specialists of about cruiser size

The Vos line SHALL have no ladder of larger classes: every Vos ship SHALL be about cruiser size and SHALL have one
trade, among boarding, mine laying, tractor beams, rock breaking, fighter launching and passing as a freighter.

#### Scenario: The board
- **WHEN** `window.MOCKUP_MEASURE()` runs on mockup 37
- **THEN** it lists six ships, each between 110 m and 190 m long

### Requirement: The Vos fly fighters of their own

The Vos SHALL have fighters of their own design, each with wings, guns clear of the nose, a blunt glazed nose and at
least one engine, and SHALL NOT fly another faction's fighter as their own.

#### Scenario: The Saucer runner's deck
- **WHEN** the Saucer runner is shown
- **THEN** the six fighters on its deck are the Syndicate's own

### Requirement: The Cutter and the Twin boom are detailed on their own textures

The Cutter (the standard fighter) and the Twin boom (the shuttle stand-in and bomber) SHALL each be unwrapped on its own
and carry maps baked onto its own UVs (painted, base colour, ORM, tangent normals, emission and a hull-light map) in the
Breakers' recipe, with no repeating tile as the main colour map and no black under the plating.

#### Scenario: The baked fighters on the page
- **WHEN** 21 Cutter or 22 Twin boom is picked on mockup 37 and Painted or PBR is chosen
- **THEN** the craft is shown with its baked maps, and Night shows its baked hull lights

#### Scenario: The manifest
- **WHEN** `assets/ships/vos/baked/manifest.json` is read
- **THEN** it lists `vos_cutter` and `vos_twin_boom`, each with a 2048 px texture size

### Requirement: Every Vos specialist has a room plan that fits inside its parts

Each of the six specialists SHALL have a room plan (`data/ships/vos/<ship>_plan.json`) whose every room, grown by the
plan's clearance and structure, lies inside the parts the ship is built from, with every room on a corridor and no two
rooms of a deck overlapping.

#### Scenario: The page's room check
- **WHEN** `window.MOCKUP_ROOMS(n)` runs on mockup 37 for each of 3, 8, 9, 10, 11 and 12
- **THEN** it reports no room outside and no plan failure

### Requirement: Vos ships are built from freighter hulls only

Every Vos ship SHALL be built from the game's freighters (their drive blocks, spines, bows and containers) and from parts
the Syndicate welds itself; no Vos ship SHALL carry any part of a warship of any faction.

#### Scenario: The parts of every Vos ship
- **WHEN** `window.MOCKUP_PARTS(n)` runs on mockup 37 for each of 3, 8, 9, 10, 11 and 12
- **THEN** every part's maker is a freighter's, a container, a fighter or the Vos's own

### Requirement: A detailed Vos ship's freighter parts keep their makers' looks

When a Vos ship is detailed, each freighter part SHALL be painted in that freighter's look, dulled, and the Syndicate's
own welded metal in the Breakers' recipe, with its windows from its room plan.

#### Scenario: The Deck runner
- **WHEN** the Deck runner is shown in Painted or PBR on mockup 37
- **THEN** its two Hunt freighter parts wear the Hunt freighters' look, dulled, its collar and flight deck the Vos's rust plate, and its windows are where its crew and command rooms meet the hull
