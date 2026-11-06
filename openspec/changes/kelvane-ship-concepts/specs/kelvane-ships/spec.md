## ADDED Requirements

### Requirement: Institute ships come from the Institute's culture, never its body

An Institute ship SHALL take no shape, paneling, texture or marking from what the Kelvane look like. Each of its design
choices SHALL name the tradition inside the Institute it comes from (Ship Design Bible rules C1 to C5). Until the owner sets
the Institute's culture, every tradition SHALL be marked provisional.

#### Scenario: The stereotype test
- **WHEN** an Institute blockout is described without naming the species
- **THEN** the description is an instrument's or a laboratory's working shape, never a creature's body

#### Scenario: Every blockout names its tradition
- **WHEN** a blockout is picked on the page
- **THEN** the page names the tradition it comes from and where that tradition's way of building comes from

### Requirement: The Institute page is a blockout board to pick from

The Institute page SHALL show nineteen frigate-sized blockouts, seven of them a family of loop ships beside the Loop. They
SHALL be shown at true scale with the Concord Petrel to hand. Each SHALL show its length, beam, height and triangle count,
measured from its built geometry, and SHALL be viewable on its own.

#### Scenario: The board
- **WHEN** the board is shown
- **THEN** all nineteen stand on it with their numbers and names, grouped by tradition in the list

#### Scenario: The loop family
- **WHEN** Loop family is chosen
- **THEN** only the eight loop ships stand on the board, listed as horizontal, vertical and both, each with an opening seen from above, from the side or both

#### Scenario: One at a time
- **WHEN** a blockout is picked from the list
- **THEN** only it is shown, from its bow quarter, with its measured size and its tradition

### Requirement: Every Institute hull is one closed grown structure

Every blockout SHALL be grown on the shared `docs/mockups/lib/hull-grow.js` (Ship Design Bible chapter 14): one body lofted
through the Institute's own cross-section layout, every branch split from the body's own section, and no primitive placed.
Every blockout's skin SHALL be closed, with every edge shared by exactly two triangles.

#### Scenario: Closed skins
- **WHEN** the page measures its blockouts (`window.MOCKUP_MEASURE`)
- **THEN** every blockout reports zero open edges

#### Scenario: Simple sections
- **WHEN** a station's section is built
- **THEN** its port half never turns back on itself; a section that does is reported as a console error, which fails the page's shots

### Requirement: The grown hull has one implementation

The Ssarith Hunt page and the Institute page SHALL grow their hulls with the same code, `docs/mockups/lib/hull-grow.js`,
each passing its own section layout and materials.

#### Scenario: The Hunt unchanged by the lift
- **WHEN** the Hunt's ships are exported as OBJ (`window.MOCKUP_EXPORT_OBJ`) before and after the Hunt page inlines the shared code
- **THEN** every file is identical

### Requirement: The line is five classes from the owner's picks

The Institute page SHALL show the line: blockouts 5, 11, 17, 18 and 19, the more complex ones the larger ships, as the frigate,
destroyer, cruiser, battlecruiser and carrier. Each SHALL be grown at its class's size as one closed structure, and each SHALL
name its class, its scientific work and the blockout it comes from.

#### Scenario: One class at a time
- **WHEN** a class is picked in the line
- **THEN** it alone is shown, framed on its length, with its class, work, blockout, size and closure

#### Scenario: The line-up
- **WHEN** Line-up is chosen
- **THEN** the five classes stand side by side at true scale

### Requirement: Every class has its own baked textures

Every class SHALL be unwrapped on its own and baked from a modelled detail layer onto its own UVs. The bake SHALL write a
painted map, a PBR set (base colour, ORM, tangent normals, emission) and a hull-light map. Its detail SHALL be clean and
smooth (seams, smooth round pods and glass domes; no greebles, trenches or slatted vents). The detail and paint SHALL be in
the Institute's data, `data/ships/kelvane/hull_detail.json`. The page SHALL show each class Painted, PBR, Champagne or Clay,
under Studio or Night light.

#### Scenario: The looks
- **WHEN** Painted or PBR is chosen
- **THEN** each class shows its own maps; Champagne and Clay show its grown skin

#### Scenario: The Hunt unchanged
- **WHEN** the bake tools run for the Ssarith Hunt
- **THEN** they read the same paint and detail values as before the faction options, from the Hunt's own data

