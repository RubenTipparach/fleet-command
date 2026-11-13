## ADDED Requirements

### Requirement: A nebula can be drawn as shells built once at load

The view SHALL be able to draw a battle's nebula plumes and their two decks as static meshes. The meshes SHALL be
polygonised from the plumes' and decks' density fields when the battle loads. No noise SHALL be evaluated per pixel when
the nebula is drawn.

#### Scenario: The shells style is chosen
- **WHEN** a battle with plumes starts with the nebula style `shells`
- **THEN** each plume's column and both decks are drawn as nested see-through meshes, built before the first frame

### Requirement: The shells keep the nebula's shapes and looks

The shells SHALL take their columns' places, sizes and looks from `data/space/plumes.json`. The decks' heights, depths,
funnels, margins and fades SHALL come from `data/space/nebula_decks.json`. The shells SHALL NOT keep a second copy of
any of these numbers.

#### Scenario: A deck's height changes
- **WHEN** `underside_km` in `data/space/nebula_decks.json` changes
- **THEN** the decks' shells move with it, and the columns still reach into them

### Requirement: Shell edges fade when seen edge-on

A shell's opacity SHALL fall with how far it is turned from the viewer, so no hard rim shows where a shell is seen
edge-on.

#### Scenario: A column's silhouette
- **WHEN** a column is seen from the side in mockup 40
- **THEN** its outline fades into space instead of ending in a hard line
