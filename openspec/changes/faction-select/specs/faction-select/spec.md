## ADDED Requirements

### Requirement: A campaign starts by choosing a faction from its emblem

Choosing Campaign on the main menu SHALL show every playable faction's emblem on its own pedestal, and picking one SHALL
offer Continue campaign and New campaign for that faction.

#### Scenario: Picking an emblem
- **WHEN** the player clicks the Krai emblem
- **THEN** a card in the Krai skin offers Continue campaign and New campaign

#### Scenario: No save to continue
- **WHEN** the player picks a faction with no saved campaign
- **THEN** Continue campaign is disabled and New campaign is offered

### Requirement: The emblem under the pointer is lit and turns toward it

The emblem under the pointer SHALL be lit by its lamp, with dust shining in the beam, and SHALL turn toward the pointer by at
most 0.45 rad, settling back when the pointer leaves.

#### Scenario: Hover and leave
- **WHEN** the pointer moves onto the Concord emblem and then away
- **THEN** its lamp comes on while it is under the pointer and goes down after, and the emblem returns to face the front

### Requirement: The campaign wears the chosen faction's skin

Every campaign screen SHALL be drawn in the console skin of the faction the campaign is played as, from that skin's own kit
pieces.

#### Scenario: A Krai campaign
- **WHEN** the player begins a Krai campaign
- **THEN** the map's top bar, panels, keys and tabs are the Krai kit's pieces
