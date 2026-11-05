## ADDED Requirements

### Requirement: Every designed ship has a two-layer schematic

Every exported ship glb SHALL have a top-down schematic of two white layers, the hull shaded darker than the quad
wireframe, made by the committed generator from the glb itself.

#### Scenario: Recombined in a skin
- **WHEN** the systems display draws a Concord frigate in the Concord skin
- **THEN** it draws the Swift's hull layer in the skin's Signal colour and its wire layer in Sky, nose up

### Requirement: The shipyard previews each hull by its schematic

The campaign's shipyard, fleet and outfitter SHALL show each hull's schematic, or say it is a stand-in hull when it has none.

#### Scenario: Buying a ship
- **WHEN** the player opens the shipyard at their home port
- **THEN** every hull for sale is listed with its schematic, its class, tier, hull and hardpoints, and its price
