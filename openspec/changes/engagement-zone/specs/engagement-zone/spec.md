## ADDED Requirements

### Requirement: A battle has an engagement zone

Every battle SHALL have an engagement zone, a square on the battle plane, which the radar dial, the tactical map and the
3D view SHALL draw where the rules place it. When the commanded ship is outside it, a countdown of 30 seconds SHALL run
and SHALL show on screen; returning inside SHALL call it off.

#### Scenario: Leaving and coming back
- **WHEN** the commanded ship crosses the zone's border and returns inside within 30 seconds
- **THEN** an on-screen warning counts the seconds while it is outside, and the battle goes on once it is back

### Requirement: Leaving the zone abandons the mission

When the countdown reaches zero the mission SHALL be abandoned: a ship whose warp drive works SHALL warp out with its
fleet and the battle SHALL end as abandoned; a ship whose warp drive does not work SHALL self-destruct, and the on-screen
warning SHALL say so from the moment the countdown starts.

#### Scenario: Abandoning with a working drive
- **WHEN** the commanded ship stays outside the zone for 30 seconds with its warp drive working
- **THEN** the fleet warps out and the battle ends as abandoned

#### Scenario: Abandoning without a warp drive
- **WHEN** the commanded ship leaves the zone with its warp drive wrecked
- **THEN** the on-screen warning reads as a self-destruct countdown, and at zero the ship is destroyed
