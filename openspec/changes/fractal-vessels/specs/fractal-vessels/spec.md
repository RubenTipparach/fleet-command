## ADDED Requirements

### Requirement: A fractal vessel is drawn by raymarching a distance field

A fractal vessel SHALL be drawn by marching each pixel's ray through a distance function. Its only geometry SHALL be its
bounding box. It SHALL write the depth of the surface it reaches, so meshes sort against it correctly.

#### Scenario: A mesh ship passes behind a fractal vessel
- **WHEN** the Petrel sits behind a fractal vessel in mockup 37
- **THEN** the vessel's surface hides the part of the Petrel behind it and nothing else, because the box writes the
  hit point's depth

### Requirement: Each form comes from a school of its makers' culture

Each fractal form SHALL name the tradition it comes from in the culture the owner sets (survey N1 and FV2). It SHALL
never come from the species' body.

#### Scenario: The forms are named
- **WHEN** a form is chosen in mockup 37
- **THEN** its description names its school (the escape-time school, the tilers or the branchers)

### Requirement: Damage simplifies the field and opens holes in it

A damaged fractal vessel SHALL lose fold levels in proportion to its damage. It SHALL have holes cut from its field at
named points, each with an edge that glows.

#### Scenario: Damage at 80 percent
- **WHEN** damage is set to 0.8 in mockup 37
- **THEN** the vessel draws fewer fold levels and shows three holes with glowing rims
