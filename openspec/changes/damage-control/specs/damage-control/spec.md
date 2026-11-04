## ADDED Requirements

### Requirement: Crews work down the captain's repair queue

The ship's crews, one for each point of repair energy, SHALL work the repair queue in its order, at most three crews on
one job and the rest on the next job that needs them, and a job SHALL be mended at its worst part first.

#### Scenario: The reactor first
- **WHEN** the captain puts the reactor core at the top with four crews and the reactor and the nacelles are damaged
- **THEN** three crews work the reactor core and one the warp nacelles, and the queue shows each job's time to whole

#### Scenario: Escape
- **WHEN** the captain plays the Escape scene and orders the reactor and the nacelles first
- **THEN** the reactor holds and the ship can jump before the pursuers are in range
