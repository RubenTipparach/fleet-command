## ADDED Requirements

### Requirement: Space is star systems and the deep between them

A new campaign SHALL lay out its terrain from the map's star systems: each star with its asteroid belt and its nebula
beside it, and dust clouds and asteroid fields with no star in the deep, clear of every star. The same seed SHALL lay out
the same space.

#### Scenario: Systems and the deep
- **WHEN** a new campaign is built
- **THEN** every system's star is a star sector, a system with a belt has asteroids on its ring, a system with a nebula has
  one within four sectors, there are nebulae and asteroid fields four or more sectors from every star, and Ouro is a rogue
  planet

### Requirement: The fog of war has three states

Every sector SHALL be unexplored, revealed or visible. Your empire's sectors and those within your fleet's and friendly
ships' sight SHALL be visible; whatever is visible SHALL be revealed with the empire that holds it; an unexplored sector
SHALL be drawn black with nothing on it, and other fleets SHALL be shown only where visible.

#### Scenario: Flying out reveals
- **WHEN** the fleet flies to an unexplored sector
- **THEN** that sector is revealed and visible, and an offered mission's sector is always revealed

### Requirement: Patrols are offered often and around planets, nebulae and asteroids

Contracts SHALL be offered by their weight, and the patrols SHALL include one in a nebula, one in an asteroid field and
one over a planet.

#### Scenario: Patrol supply
- **WHEN** 300 contracts are offered
- **THEN** between 35 and 65 percent are patrols, each in its own terrain
