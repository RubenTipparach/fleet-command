## ADDED Requirements

### Requirement: The Vos Syndicate's ships start from a board of blockouts

Mockup 37 SHALL show twelve frigate-sized blockouts, each naming the drafted tradition it comes from, for the owner to
pick from before any is detailed.

#### Scenario: Looking at one
- **WHEN** the owner picks a blockout in the list
- **THEN** it is shown alone, orbitable, with its size, triangle count and tradition

### Requirement: Every Vos hull is one closed grown skin

Every Vos blockout SHALL be grown on `docs/mockups/lib/hull-grow.js` from the Syndicate's own section layout, with no
placed primitive and no open edge.

#### Scenario: Closure
- **WHEN** `window.MOCKUP_MEASURE()` runs on the page
- **THEN** every blockout reports zero open edges

### Requirement: No Vos hull is drawn from the species' body

Every Vos blockout SHALL be sourced from a tradition inside the Syndicate and SHALL NOT be shaped after the Vos's bodies,
nor be a picture of a sailing ship, a skull or a hook.

#### Scenario: The stereotype test
- **WHEN** a blockout's description is read without the species' name
- **THEN** it describes a working ship's shape
