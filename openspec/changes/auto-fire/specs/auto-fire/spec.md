## ADDED Requirements

### Requirement: The Fire key is split into Auto and Fire

The weapons panel SHALL show, in the place of the one Fire key, two keys side by side: Auto, a toggle, and Fire, which
fires on a press. Every skin SHALL draw the same two keys in the same place, and R SHALL toggle Auto.

#### Scenario: Auto on
- **WHEN** the captain presses Auto
- **THEN** the key stays lit and the log says "Auto fire: every selected weapon fires as it bears."

### Requirement: Auto fires every active weapon as it bears

While Auto is on, the ship you command SHALL fire, each step and without a press, every mount Fire would loose with
Multiple on: selected, armed, ready, in arc and in range of its pinned ship or the locked enemy. It SHALL fire none while
cloaked, none at a friendly ship, and SHALL use the same shot as Fire.

#### Scenario: Every bearing weapon fires on Single
- **WHEN** Auto is on with Single selected, the enemy dead ahead at 50 km and every weapon charged
- **THEN** every selected forward-bearing weapon fires on the next step, the one left out of the volley holds, and the
  aft lance, out of arc, holds

#### Scenario: Auto off
- **WHEN** Auto is off and every weapon is charged and bearing
- **THEN** nothing fires until Fire is pressed
