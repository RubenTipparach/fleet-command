## ADDED Requirements

### Requirement: A campaign starts by choosing a faction from its emblem

Choosing Campaign on the main menu SHALL show every playable faction's emblem floating over a walnut table, each over its own
brass ring, and picking one SHALL light its lamp full and offer Continue campaign and New campaign for that faction.

#### Scenario: Picking an emblem
- **WHEN** the player clicks the Krai emblem
- **THEN** the Krai lamp shines full on it with its beam and dust, and a card in the Krai skin offers Continue campaign and New campaign

#### Scenario: No save to continue
- **WHEN** the player picks a faction with no saved campaign
- **THEN** Continue campaign is disabled and New campaign is offered

### Requirement: The emblems float and turn with the pointer

Every emblem SHALL bob gently over the table and turn toward the pointer, the one under the pointer by up to 0.5 rad and the
others by a little; the one under the pointer SHALL be half lit by its lamp, with dust shining in the beam.

#### Scenario: Hover and leave
- **WHEN** the pointer moves onto the Concord emblem and then away
- **THEN** its lamp comes up to half while it is under the pointer and goes down after, and the emblem eases back to a slight turn

#### Scenario: Moving the pointer
- **WHEN** the pointer moves across the screen with no emblem under it
- **THEN** every emblem turns a little toward it, and keeps bobbing

### Requirement: The campaign wears the chosen faction's skin

Every campaign screen SHALL be drawn in the console skin of the faction the campaign is played as, from that skin's own kit
pieces.

#### Scenario: A Krai campaign
- **WHEN** the player begins a Krai campaign
- **THEN** the map's top bar, panels, keys and tabs are the Krai kit's pieces
