## ADDED Requirements

### Requirement: The battle draws the approved ships

A ship class with a `model` SHALL be drawn in the battle as that glb, exported from its approved mockup, scaled to the
class's battle length with its bow along the ship's heading; a class without one SHALL be drawn as its stand-in.

#### Scenario: The Concord's Battle
- **WHEN** the Concord's Battle starts
- **THEN** the Resolute is the Albatross, the Tallis the Swift and the Meridian the Petrel, and the Krai ships are the
  Krai line's cruiser, frigate and corvette

### Requirement: Ship exports follow their mockups

Every exported ship SHALL record the mockup page it came from and that page's sha256, and the exporter SHALL be the only
step needed to bring the ships up to a changed mockup.

#### Scenario: A stale export is caught
- **WHEN** a ship mockup page changes and the exporter has not been re-run
- **THEN** the check comparing each ship's recorded sha256 with the page fails, naming the ship and the page
