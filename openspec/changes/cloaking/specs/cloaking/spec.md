## ADDED Requirements

### Requirement: A cloaked ship is hidden until a sweep finds it

A fully cloaked ship SHALL be hidden from the other side (no marker, contact, radar or map mark, model or lock) unless it
is within 1.5 km or a sweep by that side has revealed it in the last 8 s; while cloaking or cloaked it SHALL have no
shields and SHALL not fire.

#### Scenario: Sweep with ECCM
- **WHEN** the captain sets ECCM 2 and sweeps with a cloaked destroyer 9 km away
- **THEN** the sweep reaches 10 km, the destroyer is revealed and can be locked, at a penalty to hit

#### Scenario: Cloak to break a lock
- **WHEN** the player's ship cloaks while an enemy holds a lock on it
- **THEN** the enemy loses its lock once the cloak is complete, and the player cannot fire until decloaked
