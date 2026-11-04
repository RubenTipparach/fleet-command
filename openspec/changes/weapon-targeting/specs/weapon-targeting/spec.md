## ADDED Requirements

### Requirement: A hit lands on the chopping block of the facing that takes it

A hit SHALL damage only the subsystems and mounts exposed by the target's facing that takes it: those within 60 degrees
of the facing's centre, or near the ship's centre. A precise weapon SHALL put the hit on the aimed subsystem half of the
time when that subsystem is exposed; every other hit, and every hit from a by-arc weapon, SHALL land on a random exposed
subsystem or none.

#### Scenario: A torpedo ignores the aim
- **WHEN** a torpedo hits a target aimed at its sensors through its aft facing
- **THEN** the damage lands on the aft facing's exposed subsystems, never on the bow sensors

#### Scenario: The block is shown
- **WHEN** a target is locked
- **THEN** its systems display brackets every subsystem and mount its facing toward you exposes

### Requirement: A pinned weapon keeps its target

A weapon pinned to a ship SHALL fire at that ship with its pinned aim whatever target is locked, and the pin SHALL stop
when that ship, or the subsystem the pin aims at, is destroyed.

#### Scenario: Changing the lock does not move a pinned weapon
- **WHEN** W1 is pinned to the Ashkar and the captain locks the Rattlejaw and fires
- **THEN** W1 fires at the Ashkar and the unpinned weapons fire at the Rattlejaw

### Requirement: A nebula hides and jams

Inside a nebula every shot SHALL lose hit chance with its density, ECM and ECCM SHALL count for more, and a ship inside
SHALL not be locked from beyond 6 km unless the locker's ECCM exceeds its ECM.

#### Scenario: A jamming raider in the nebula
- **WHEN** the captain tries to lock a raider with ECM 3 inside the nebula at 9 km with ECCM 2
- **THEN** the lock is refused with the reason, and the contact reads Unknown until ECCM is 4 or the range is 6 km
