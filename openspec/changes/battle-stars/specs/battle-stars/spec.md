## ADDED Requirements

### Requirement: A battle with a planet has a star

A battle with any planet SHALL have a star, distant or in the map, and the battle's light SHALL come from it. A scenario
with a planet and no star SHALL fail validation naming the scenario. A campaign battle SHALL take its sector's star, in
the map when the sector holds a star and distant otherwise.

#### Scenario: A distant star
- **WHEN** a battle with a planet and a distant star is shown
- **THEN** the star's disc is in the sky in the direction the planets are lit from

#### Scenario: A sector with a star
- **WHEN** a campaign battle is fought in a sector that holds a star
- **THEN** the star stands on the map and the light comes from it
