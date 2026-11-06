## ADDED Requirements

### Requirement: A beam's ends stay on the firing mount and on the target

A beam SHALL be drawn, every frame it lasts, from its mount on the firing ship as that ship now stands to its target as
that target now stands; a direct-fire bolt SHALL fly along that live line; a seeker (missile or torpedo) MAY fly free and
leave its trail.

#### Scenario: Both ships move while a lance fires
- **WHEN** a lance beam is drawn and, a step later, both the firing ship and its target have moved
- **THEN** the beam's start is the lance's mount where the firing ship now is, and its end is the target where it now is

#### Scenario: A disruptor bolt between two moving ships
- **WHEN** a disruptor bolt is in flight while both ships move
- **THEN** it lies on the line from its mount to its target as they now stand, between the two

### Requirement: Each faction's special weapons have their own light

The Concord's specials SHALL be drawn in clean light (Sky with Lilac strands, round flares, a hex shield lattice) and
the Krai's in heat (blackbody to forge orange, triangle flares and lattice), with a phone quality that keeps every effect
but drops the heat haze, the aberration and half the bloom's resolution.

#### Scenario: A forge spike on a Concord hull
- **WHEN** a forge spike strikes the Albatross
- **THEN** the hull itself shows a white-hot pool that cools through orange to a dark crust over its burn, with glowing
  fissures round it, riding the ship as it moves
