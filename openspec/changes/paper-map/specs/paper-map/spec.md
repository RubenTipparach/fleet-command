## ADDED Requirements

### Requirement: The campaign map is a printed chart on a table

The campaign map SHALL be drawn as an old printed star map, a nebula and stars printed in dark ink, lying on a wooden table in
a room with a red carpet and dark green walls with wood trim, its hexes outlined in the colours of the factions that hold them, with a heavier rule along every border between
holders, and the map's pieces standing on it.

#### Scenario: Who holds a hex
- **WHEN** the player looks at a hex held by the Krai beside one held by the Concord
- **THEN** each is outlined in its holder's colour, and the edge between them carries a heavier rule on each side in each holder's colour

#### Scenario: Zoomed out
- **WHEN** the player zooms the map all the way out
- **THEN** the whole chart shows on its table, with the carpet and the green walls round it

### Requirement: The chart shows the fog of war

The chart SHALL print unexplored sectors as flat black with nothing charted on them, revealed sectors darker in the holder last
seen, and visible sectors as they are now; a faction's fleets SHALL stand on the chart only where seen now.

#### Scenario: A new campaign
- **WHEN** a Concord campaign starts
- **THEN** the Concord's own space and the sectors round its fleet are charted, each offered mission's sector is charted darker, and the rest of the print is black

### Requirement: The chart's edge is worn

The chart SHALL have a white border round its print whose outer edge is frayed, browned and torn in places, with at least one
tear running into the border and one corner torn away.

#### Scenario: The edge close up
- **WHEN** the player zooms in on the chart's edge
- **THEN** the white border shows a ragged, browned edge with tears, and the table's wood shows where the paper is torn away

### Requirement: What stands on the chart is drawn by hand

On the paper chart the nebulae, storms, planets, suns and asteroids SHALL be hand-drawn pieces (ink and watercolour, shaded
away from the lamp) standing on the chart as billboards, the names SHALL be hand-lettered, and the chart SHALL carry a compass
rose and rhumb lines; the ships and stations SHALL stay as their 3D models.

#### Scenario: A nebula close up
- **WHEN** the player zooms in on a nebula on the paper chart
- **THEN** it stands as inked, washed puffs of cloud in its colours, facing the camera, with the ships among them still models

#### Scenario: A storm
- **WHEN** a cloud is one of the storms
- **THEN** it is drawn in slate with a forked bolt of lightning out of its base, and moving through it costs what any nebula costs
