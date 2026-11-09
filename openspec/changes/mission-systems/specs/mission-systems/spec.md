## ADDED Requirements

### Requirement: Sensors identify contacts within their reach

A ship's sensor reach SHALL be `field.json` base_km plus per_e_km for each energy box in sensors, times the sensors' health,
times passive_x on passive sensors. A contact SHALL be identified (named, with its class) when it is of your side, within any
friendly ship's reach, or scanned; otherwise it SHALL be shown as an unknown contact. Every finished scan SHALL be remembered
for the mission.

#### Scenario: Out of reach
- **WHEN** an enemy is 20 km beyond the reach of every friendly ship
- **THEN** it is not identified, and it is once it comes 5 km inside the reach

### Requirement: A planet is surveyed and its sites visited by away teams

A planet SHALL be scanned only from within planet_scan_km of its surface; a finished scan SHALL make its sites known. An away
team SHALL go only to a known site not yet held, within the transporter's reach, with a pad ready and a marine aboard; after
away_s it SHALL hold the site unless a garrison beats it, and SHALL beam back up once its ship is in reach.

#### Scenario: A quiet ruin
- **WHEN** a team of marines beams down to a known, quiet ruin beside the ship
- **THEN** after away_s the ruin is held, and the team is back aboard with every marine

#### Scenario: A strong garrison
- **WHEN** a team beams down to a site with a garrison of 30
- **THEN** the site is not held and is marked failed

### Requirement: Mission battles field stations, planets, monsters and timed events

A mission battle SHALL be able to field placeholder stations, yards, bases, outposts, batteries, pods and the monster, place
planets with sites, bring groups in late, keep them asleep, run them for home, hold them to a truce, and end at a time limit.
A fixed installation SHALL never move.

#### Scenario: A station stands still
- **WHEN** a station assault is fought for 30 s
- **THEN** the station has not moved

#### Scenario: A late wave
- **WHEN** a holding action starts
- **THEN** its third wave is unseen until its arrival time, then on the field at its place

#### Scenario: Held to the time
- **WHEN** a holding action reaches its time limit with the post standing
- **THEN** the battle ends and the mission is won

### Requirement: Every catalogue mission type is a mission file

Every type of the Missions guide's catalogue SHALL be played by a mission file, and every mission file SHALL be offered,
briefed, fought and graded on a new campaign for each playable side that has a sector it fits.

#### Scenario: All of them
- **WHEN** the tests offer every mission file on new Concord, Krai and Hunt campaigns
- **THEN** each is offered somewhere, briefed, fought and graded

### Requirement: No battle locks in a standoff

A withdrawal SHALL jump by itself only at or under `warp.withdraw_max_risk`, SHALL be called off by Withdraw pressed again,
and SHALL be called off by itself when its drive can no longer charge. A battle that cannot be withdrawn from SHALL end as a
stalemate, graded as a withdrawal, once no ship has been hurt for `stalemate_s`. An AI captain whose fire has not landed for
`ai_close_after_s` SHALL close to half its fighting range, and half again for each further span, to an eighth.

#### Scenario: A wrecked drive
- **WHEN** a withdrawal is under way and every nacelle is wrecked with the banks short
- **THEN** the withdrawal is called off and the battle goes on

#### Scenario: A story stalemate
- **WHEN** no ship is hurt for `stalemate_s` in a story mission's battle
- **THEN** the battle ends as a stalemate, graded a draw

#### Scenario: An intercept keeps its target in arc
- **WHEN** a ship intercepts a target crossing at its own speed
- **THEN** its ordered heading is never more than `intercept_max_lead_deg` off the target

#### Scenario: A kiting raider
- **WHEN** a corsair's fire lands on nothing for two minutes
- **THEN** it has closed to within 20 km of its target
