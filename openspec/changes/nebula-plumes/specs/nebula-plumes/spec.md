## ADDED Requirements

### Requirement: A nebula plume blunts weapons and hides ships

Inside a nebula plume at full density a shot SHALL keep half its chance to hit, a beam or bolt weapon SHALL reach half its
range, and a missile SHALL lose its target with a chance of 0.08 a second. No ship SHALL lock another through a plume (any
plume on the sight line between them, either ship's own included) from beyond 15 km unless its ECCM is above the other's
ECM, and a lock held when a plume comes between SHALL be lost. Each effect SHALL scale with the plume's density where the
ships are (on the sight line, for a lock), and SHALL be nothing outside every plume.

#### Scenario: A shot into a plume
- **WHEN** a ship outside every plume fires at a target in the middle of a plume of density 1
- **THEN** its chance to hit is half what it is against the same target in clear space, and the weapon's hover card
  shows that halved chance

#### Scenario: A beam's reach from inside a plume
- **WHEN** a ship in the middle of a plume of density 1 has a target beyond half its beam's range
- **THEN** the beam does not fire, the target is not shown in range, and a torpedo's range is unchanged

#### Scenario: A missile after a ship in a plume
- **WHEN** a missile follows a target into a plume
- **THEN** each second it may lose the target and fly on straight, and a battle with the same seed loses the same missiles

#### Scenario: Hiding in a plume
- **WHEN** a ship sits in a plume 80 km from an enemy whose ECCM does not beat its ECM
- **THEN** the enemy cannot lock it, and can once it closes inside 15 km

#### Scenario: A plume between two ships
- **WHEN** two ships in clear space 60 km apart have a plume on the sight line between them, and one has the other locked
- **THEN** the lock is lost and cannot be made again until they close inside 15 km, the line clears, or the locker's ECCM
  beats the other's ECM; a plume beside the line hides nothing

### Requirement: An ion storm's lightning trips a ship's core

A plume marked as an ion storm SHALL strike each ship inside it at 3 strikes a minute at full density. A strike SHALL do
no damage and SHALL leave the ship without power for 5 seconds: no drive, no charging, no fire, no point defence, no
shields, no cloak, no tractor, no repair. No strike SHALL find that ship again until 15 seconds after its core restarts.

#### Scenario: A strike on the player's ship
- **WHEN** lightning strikes the player's ship
- **THEN** the log names the strike and the seconds without power, the ship cannot fire and its shields do not hold for
  5 seconds, and the log says when power is back

#### Scenario: A storm replays
- **WHEN** a battle with an ion storm is played twice from the same seed and inputs
- **THEN** the same ships are struck at the same times

#### Scenario: No storm, no change
- **WHEN** a battle has no ion storm
- **THEN** it replays exactly as it did before storms existed

### Requirement: A plume is drawn where its rules are

A plume SHALL be drawn as cloud standing on the circle its rules test, lit by the battle's sun, in its form: a column
rising from a deck of cloud below the battle into a deck above it, a column rising from the deck below or hanging from the
deck above that stops short of the other, or a loose puff, each column with proportions of its own. A battle with plumes
SHALL have the two decks, coloured by the columns placed in them and fading out at their edges and into the sky, and a sky
in its plumes' colours. An ion storm's lightning SHALL show in the cloud with a bolt onto each struck ship. A captain
inside a plume SHALL still see their own ship.
The radar dial and the tactical map SHALL draw each plume's circle, an ion storm distinct from a plain plume.

#### Scenario: The cloud and the rule agree
- **WHEN** a ship crosses the edge of the cloud's foot on the battle plane
- **THEN** that is where its chance to hit and its range begin to fall

#### Scenario: Inside a plume
- **WHEN** the camera is inside a plume
- **THEN** the view is fogged in the plume's colours and the captain's own ship and its nearest neighbours are still seen

#### Scenario: The decks
- **WHEN** a battle with plumes is seen from the chase view
- **THEN** a deck of cloud lies below the battle and another above it, each column that reaches a deck runs up into a
  funnel of it in the same colour, and both thin into the sky toward their edges

#### Scenario: Zoomed out over the battle
- **WHEN** the captain zooms the camera out high above the battle
- **THEN** the deck above thins away and the battle is seen through where it was

#### Scenario: The map
- **WHEN** the tactical map is open in a scene with a plume and an ion storm
- **THEN** each is a circle of its radius at its place, the storm in the caution colour and named
