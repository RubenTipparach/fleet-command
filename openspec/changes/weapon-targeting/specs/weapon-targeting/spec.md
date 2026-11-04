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

### Requirement: A weapon can be aimed at, and weapons control feeds only the mounts it powers

The captain SHALL be able to aim at one of the target's weapon mounts, and a precise hit so aimed SHALL land on that
mount half of the time when it is on the block. Mounts fed by the capacitor SHALL stop charging once weapons control is
disabled; independently powered weapons (point defence, missiles) SHALL charge and fire whatever weapons control's state.

#### Scenario: Knocking out one gun
- **WHEN** the captain aims at the Vorrak's W3 and fires lances through the facing that exposes it
- **THEN** about half the hits land on W3, and the aim key and the Target box show W3's status falling

#### Scenario: Point defence outlives weapons control
- **WHEN** a ship's weapons control is disabled
- **THEN** its lances and torpedoes stop charging, and its point defence still charges and intercepts
