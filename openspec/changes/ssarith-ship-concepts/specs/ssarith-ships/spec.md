## ADDED Requirements

### Requirement: Hunt ships are designed from their rooms

Every Ssarith Hunt class SHALL have a plan of its rooms in `data/ships/ssarith/`, written by
`tools/mockups/ssarith_plans.py` on the same plan layer as the Krai's, and its hull SHALL be fitted round them by the
shared fitter, with its character added on top and never cut into a room.

#### Scenario: The frigate's plan
- **WHEN** the Hunt page fits a hull round `data/ships/ssarith/frigate_plan.json`
- **THEN** no two rooms on a deck overlap, every room touches its corridor, and every room corner lies at least 1.05 m inside the hull

### Requirement: Hunt ships come from the Hunt's culture, never its body

A Hunt ship SHALL take no shape, paneling, texture or marking from what the species looks like, and each of its design
choices SHALL name its source in the Hunt's culture as the owner sets it (Ship Design Bible rules C1 to C5).

#### Scenario: The stereotype test
- **WHEN** a Hunt design is described without naming the species
- **THEN** the description is not a picture of a reptile's body, and the page shows that description for each direction

### Requirement: Hunt ships show more than one tradition

The Hunt page SHALL show at least two traditions of the Hunt as separate directions on the same rooms, each with its own
plating, colours and distinct features, and each naming the source of every choice (Ship Design Bible C2, C3).

#### Scenario: The traditions differ
- **WHEN** the directions are compared
- **THEN** no two share a plating style or a palette

### Requirement: Hunt ships are shown at true scale beside the Concord line

The Hunt page SHALL show each direction beside the Concord Petrel built from `docs/mockups/lib/concord-line.js`, with
its length, beam and height measured from the built geometry, and each direction as a 40 px silhouette, top and side.

#### Scenario: The yardstick
- **WHEN** the line-up is shown
- **THEN** each Hunt direction stands beside the Petrel at the same scale, each with its measured size

### Requirement: A Hunt hull's edges run smooth

A Hunt hull SHALL carry one chamfer along its body, no deeper than its tightest station's rooms allow, and its beam, top
and bottom SHALL follow smooth curves that only add material to the fitted hull, with no polygons added.

#### Scenario: No notched edges
- **WHEN** a Hunt hull is fitted round its rooms
- **THEN** every body station has the same chamfer, the triangle count is the fitted hull's, and every room is still at least 1.05 m inside

### Requirement: A secondary hull is designed from its own rooms

A Hunt direction that carries a secondary hull SHALL fit it round its own plan in `data/ships/ssarith/`, join it to the
frigate by a neck at least a corridor's width plus clearance, and keep at least 3 m of daylight between the two hulls,
measured on the page.

#### Scenario: The field hull
- **WHEN** direction C is built in any layout
- **THEN** every field hull room is at least 1.05 m inside its hull and the page shows the daylight between the hulls at 3 m or more

### Requirement: Hunt plating is modelled and rendered into maps

Each Hunt tradition's plating SHALL be a tile modelled out of real pieces and rendered face-on by
`tools/blender/build_hull_tiles_ssarith.py` into albedo, tangent space normal and occlusion-roughness-metalness maps in
`assets/textures/ssarith/`, with a manifest the page reads, and a fine metal tile SHALL be laid over every hull face.

#### Scenario: The page and the maps agree
- **WHEN** the page is built
- **THEN** it carries the maps from the manifest by `tools/mockups/inline_data.py`, and `--check` finds the inlined maps equal to the files
