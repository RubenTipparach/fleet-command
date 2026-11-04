## ADDED Requirements

### Requirement: Security teams move through the ship and fight boarders where they meet

The ship's marines SHALL be security teams that walk the deck plan's corridors, or are beamed to a room for a pad
charge, at the captain's order; a team and a boarding squad close on one deck SHALL fight in volleys, and a team that has
held its room for 6 s SHALL open with an ambush that the boarders cannot answer.

#### Scenario: An ambush on the bridge
- **WHEN** boarders walk into the bridge where a team has held for 6 s
- **THEN** the team fires first with the ambush bonus, the boarders get no shot in that volley, and the fight goes on
  every 3 s until one side is gone

#### Scenario: Unguarded sabotage
- **WHEN** a squad reaches the shield generator and no team is there
- **THEN** every 12 s it damages the shield generators, and the view shows them there with their goal lit
