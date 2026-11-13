## ADDED Requirements

### Requirement: A co-op crew steps one battle

In a co-op game every peer SHALL step its own copy of the battle from the same orders, applied in the order the host sends
them, and SHALL compare the battle's checksum with the host's every 30 steps, reporting any difference.

#### Scenario: Two peers fight a battle
- **WHEN** the host and one crew member fight a battle, each giving orders
- **THEN** both battles' checksums are equal at every comparison

### Requirement: Roles decide what a crew member opens and orders

The admiral SHALL hold every role on the flagship until they give one away; a crew member's console SHALL open only the
tabs of the roles they hold, showing the others disabled; and the host SHALL refuse any order the sender's roles do not
allow.

#### Scenario: An order from a station not held
- **WHEN** a crew member holding only Science orders a turn
- **THEN** the host refuses it and the ship's heading order is unchanged on every peer

### Requirement: Targeting roles keep their own targets

On a crewed ship Navigation, Weapons, Science, Tractor and Transporters SHALL each keep their own target, each system acting
on its own role's, and every ship a station has locked SHALL be marked with that station's icon; a ship with no player
crew SHALL have one target for every role, as in single play.

#### Scenario: Science locks another ship
- **WHEN** the admiral (holding Weapons) locks one enemy and the Science officer another
- **THEN** the weapons fire on the first, the scan runs on the second, and each carries its station's icon

### Requirement: The admiral gives ships to the crew

The admiral SHALL be able to give any other ship of the fleet to a crew member, who becomes its captain: the AI SHALL leave
it alone and it SHALL answer to that member's orders.

#### Scenario: A wingman given to a crew member
- **WHEN** the admiral gives the second ship to a crew member and they order all stop
- **THEN** that ship stops on every peer and the AI does not move it
