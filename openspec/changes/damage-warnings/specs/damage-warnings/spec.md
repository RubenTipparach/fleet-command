## ADDED Requirements

### Requirement: A ship low on hull smokes, burns and sheds scraps

Every ship in a battle, the player's and every other, SHALL show its hull state in the view between hits: below 50 % of
its hull it SHALL trail smoke from its worst-hit place; below 35 % it SHALL also burn at two places, throw sparks and
shed glowing scraps that tumble away; below 20 % all of it SHALL come at twice the rate from a third place. The places
SHALL be the ship's own hits where it has them, and seeded places on its hull otherwise. The smoke and scraps SHALL be
left behind in space as the ship moves. All of it SHALL come from one pool of at most `low_hull.max_particles` particles.

#### Scenario: A ship at a quarter of its hull
- **WHEN** a ship's hull is at 25 % of its class's hull
- **THEN** it trails dark smoke, burns at two places, throws sparks and sheds scraps

#### Scenario: A ship at 60 %
- **WHEN** a ship's hull is at 60 %
- **THEN** it shows no smoke, fire, sparks or scraps beyond its hit marks

#### Scenario: Reduced damage effects
- **WHEN** Options sets Damage effects to Reduced and a ship is at 25 % of its hull
- **THEN** it smokes as on Full, and its fire, sparks and scraps come at a quarter of Full's rate

### Requirement: The console flashes Low hull integrity

Below `warnings.hull_below_frac` of its hull, the ship you command SHALL show a warning band at the top of the live view
reading Low hull integrity with the hull's share as a percentage, flashing at `warnings.period_s`, and the top strip's hull
bar SHALL be labelled Low hull integrity. A tap on the band SHALL open Damage control. Every skin SHALL show the band in
the same place, drawn in its own inset frame, alert colour and type.

#### Scenario: The hull falls below 35 %
- **WHEN** the hull of the ship you command falls from 36 % to 34 %
- **THEN** the band reading Low hull integrity 34 % appears and flashes, the red alert cue plays once, and the hull bar
  is labelled Low hull integrity

#### Scenario: Taking command of a healthy ship
- **WHEN** you take command of another ship at 80 % of its hull
- **THEN** the band is gone

### Requirement: The console flashes Reactor unstable with the seconds to meltdown

While the reactor of the ship you command counts toward a meltdown (coolant offline, or the core at its limit), the
warning band SHALL show Reactor unstable and the seconds left before the meltdown, computed from the same count the
simulation runs (`MeltdownS` less the count so far), flashing at `warnings.fast_period_s` once fewer than
`warnings.fast_below_s` seconds are left. As the count runs back it SHALL show Reactor cooling in Caution with the seconds
until it is safe, and SHALL be gone once the count is back to 0. A tap SHALL open the Reactor page. With Low hull integrity
also showing, the two SHALL share the one band component, side by side where they fit.

#### Scenario: Coolant lost
- **WHEN** both coolant loops of the ship you command are knocked out and the core has counted 13 s of a 20 s meltdown
- **THEN** the band reads Reactor unstable, Meltdown 7 s, flashing at the fast rate

#### Scenario: Coolant restored
- **WHEN** a loop is repaired and the core stops counting up with 8 s counted
- **THEN** the band reads Reactor cooling, Safe in 16 s, in Caution, and is gone 16 s later

#### Scenario: Both at once
- **WHEN** the hull is at 22 % and the reactor is counting down
- **THEN** Low hull integrity and Reactor unstable show side by side
