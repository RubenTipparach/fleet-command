## ADDED Requirements

### Requirement: Hunt ships come from the Hunt's culture, never its body

A Hunt ship SHALL take no shape, paneling, texture or marking from what the species looks like, and each of its design
choices SHALL name its source in the Hunt's culture as the owner sets it (Ship Design Bible rules C1 to C5).

#### Scenario: The stereotype test
- **WHEN** a Hunt design is described without naming the species
- **THEN** the description is not a picture of a reptile's body

### Requirement: The Hunt page shows the owner's picks

The Hunt page SHALL show only the blockouts the owner picked (1 Hooded lance, 3 Forked prow, 7 Harpoon, 9 Trident), at
true scale with the Concord Petrel to hand, each with its length, beam and height measured from its built geometry.

#### Scenario: The board
- **WHEN** the board is shown
- **THEN** the four picks stand on it, every one with its measured size, and each can be viewed on its own

### Requirement: A picked blockout is designed rooms first

A blockout the owner picks SHALL be designed on the Ship Design Bible's method: its rooms as blocks in `data/ships/`, its
hull fitted round them, then its parts and skin.

#### Scenario: From massing to ship
- **WHEN** a blockout is picked
- **THEN** its plan is written before its hull is drawn in detail

### Requirement: Hunt hulls can be shown painted

The Hunt page SHALL show its ships with their own painted bakes from `tools/blender/painted_bake.py --ship`
(`assets/ships/ssarith/`, inlined from its manifest), and SHALL let the viewer switch back to plain clay.

#### Scenario: Clay underneath
- **WHEN** the Painted switch is turned off
- **THEN** every blockout shows in plain clay, and turning it on paints them again

### Requirement: A picked Hunt ship is one grown structure

A picked Hunt ship SHALL be one closed skin: a body lofted through cross-sections of one layout, with every branch (prong,
lance, hood, blade, rail) lofted on from a region of the body's own section, and no placed box, cylinder, cone or plate
visible (Ship Design Bible rules F1 to F5).

#### Scenario: Nothing stuck on
- **WHEN** a picked ship is built on the Hunt page
- **THEN** each of its parts is a named branch of its body, and the meshes it is made of are only its skin, its light strips and its drive recess

### Requirement: A picked Hunt ship has its own unwrapped texture and baked AO

A picked Hunt ship SHALL be shown with its own UV mesh and its own painted colour, AO and glow maps from
`tools/blender/painted_bake.py --ship` (`assets/ships/ssarith/`, inlined from its manifest), and any tile on it SHALL only
add detail over that map (Ship Design Bible rules T1 to T4).

#### Scenario: No repeating main colour
- **WHEN** a picked ship is shown painted
- **THEN** its colour comes from its own map on its own UVs, and its AO map is in its manifest

### Requirement: A picked Hunt ship's hull detail is layered, its own, mirrored and shaded deep

A picked Hunt ship's texture SHALL be baked from a modelled detail layer (`tools/blender/hull_detail.py`, laid out in
`data/ships/ssarith/hull_detail.json`): primary armour plates each cut to its own outline over a sub-armour layer and the
machinery of the under layer, with domes, fans, shaped vents, lamps and liveries; mirrored about the centreline; no
element repeated; lit by a baked low sun with its cast shadows and AO (Ship Design Bible rules T5 to T8).

#### Scenario: Layers and features
- **WHEN** a picked ship is shown painted, close up
- **THEN** its armour plates differ in outline and stand at different heights, trenches between them show machinery, and its dome, fan vent, shaped vents and lamps can be found

#### Scenario: One axis of symmetry
- **WHEN** the ship is seen from above
- **THEN** its port and starboard detail mirror each other, plate for plate and colour for colour
