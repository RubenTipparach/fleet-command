## ADDED Requirements

### Requirement: The battle draws the Ssarith Hunt's approved ships

A Hunt class with a `model` SHALL be drawn in the battle as its glb, exported from mockup 12, dressed from the export
manifest with its own baked maps by the baked hull shader, scaled to the class's battle length.

#### Scenario: The Hunt from the Concord's side
- **WHEN** the Concord's scenario The Hunt starts
- **THEN** the Hunt's cruiser, two frigates and carrier are drawn as their baked ships, and no ship is a stand-in

### Requirement: A grown hull comes apart closed

A hull with no loft stations SHALL have its breaks closed by cut faces clipped to its signed distance field, so a piece
blown off leaves no open hole.

#### Scenario: A piece off a Hunt hull
- **WHEN** a Hunt ship on the damage range loses a piece
- **THEN** the piece and the hull it left are closed where they broke, by faces showing decks and rooms
