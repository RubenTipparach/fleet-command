## ADDED Requirements

### Requirement: The Kelvane Institute has a battle console in its own skin

The battle console SHALL be available in the Kelvane Institute skin, with:
- the same rules, panels and controls as the Concord's;
- the Institute's rendered kit, its 24 icons, its type and its colour roles.

Every press SHALL change the console's state exactly as the same press does in the Concord skin.

#### Scenario: Parity
- **WHEN** the parity check presses every control of every named shot, both in the Kelvane page and in the Concord page
- **THEN** every press leaves the same state in both pages

### Requirement: The Kelvane console's readouts are lit on glass

In the Kelvane skin, every panel, inset and strip of the battle console SHALL hold its readouts in a well of dark glass
inside its champagne plate. Every colour role SHALL measure a WCAG contrast ratio of at least 4.5 against that glass,
except the receding roles (ghost and deep).

#### Scenario: A panel's readouts
- **WHEN** the console wears the Kelvane skin
- **THEN** each panel's labels and values sit inside the glass of `frame.screen`, inside the panel's padding

### Requirement: The console follows the ship to a Kelvane ship

In a scene with allied ships, taking command of a Kelvane ship SHALL dress the whole console in the Kelvane skin, and
taking command of another faction's ship SHALL dress it in that faction's skin.

#### Scenario: Hopping in the swap scene
- **WHEN** the captain presses key 4 in the "Four races" scene
- **THEN** the Patience is commanded and the console wears the Kelvane skin

### Requirement: The Kelvane strips' meters read the ship

In the Kelvane skin, the needle meter on the top strip SHALL read the commanded ship's hull integrity, and the one on the
bottom strip its speed out of the speed slider's full scale. Each SHALL be computed from the same values the strip's bar
and number show.

#### Scenario: Speed on the meter
- **WHEN** the ship makes 60 m/s against a full scale of 300 m/s
- **THEN** the bottom meter's needle stands a fifth of the way along its sweep

