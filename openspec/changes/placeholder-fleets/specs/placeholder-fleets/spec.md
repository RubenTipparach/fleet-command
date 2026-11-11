## ADDED Requirements

### Requirement: Every faction without designed ships has a placeholder line

Each of the nine factions without designed ships SHALL have four placeholder classes, frigate to capital, in
`data/ships/placeholders.json`. Each class SHALL have:
- a name, role, code and length in metres;
- an armament sketch;
- the tradition its shape comes from.

#### Scenario: The data is current
- **WHEN** `python3 tools/factions/placeholder_fleets.py --check` runs
- **THEN** it exits 0, because the committed file matches the generator

### Requirement: Placeholder hulls are grown lofts from one generator

Every placeholder hull SHALL be built by `docs/mockups/lib/placeholder-hulls.js` from the data:
- a body lofted through the faction's profiles;
- limbs lofted from inside the body;
- no stretched primitive as a part.

#### Scenario: One builder
- **WHEN** mockup 36 shows any faction's line
- **THEN** every ship in it is a mesh from `PlaceholderHulls.build`

### Requirement: Every faction without an emblem has a placeholder emblem

Each of the nine factions SHALL have a vector placeholder emblem, `assets/emblems/<faction>/placeholder.svg`, drawn from
a tradition in its culture and never from its species' body.

#### Scenario: The emblems are current
- **WHEN** `python3 tools/factions/placeholder_emblems.py --check` runs
- **THEN** it exits 0
