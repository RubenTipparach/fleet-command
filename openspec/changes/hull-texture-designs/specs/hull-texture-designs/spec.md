## ADDED Requirements

### Requirement: Concord and Krai hulls wear a texture design from their faction's culture

Each Concord and Krai ship SHALL wear one texture design picked by the owner from the culture-led designs of mockup 35,
unwrapped on its own UVs with its main colour map painted onto them (never a repeating tile), its plates, seams,
liveries and markings laid out by that design, and the design SHALL name the tradition of its faction it comes from.

#### Scenario: A Concord frigate in the battle
- **WHEN** the Petrel is drawn in the battle
- **THEN** its hull shows the picked Concord design's plating, bands and markings from its own baked maps, with one
  emblem on the port side

#### Scenario: A Krai frigate in the battle
- **WHEN** the Krai frigate is drawn in the battle
- **THEN** its hull shows the picked Krai design's plating and markings from its own baked maps, its tallies in fives aft
