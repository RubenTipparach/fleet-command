## ADDED Requirements

### Requirement: One refit screen serves skirmish and campaign

Skirmish setup and the campaign outfitter SHALL use the same refit rules and screen, differing only in currency (points or credits) and in the refund given for a removed system.

#### Scenario: Same hull, same hardpoints
- **WHEN** the same hull is refitted in skirmish setup and at a campaign outfitter
- **THEN** both show the same hardpoint groups, hardpoints and systems

### Requirement: Systems fit only hardpoints of their group

A system SHALL be fitted only to a hardpoint of its own group (shields, primary weapons, heavy weapons, hull systems, bridge systems, engines), and fitting it to an occupied hardpoint SHALL trade out the old system with its refund.

#### Scenario: Trading a weapon
- **WHEN** a primary hardpoint holds a pulse cannon and the player fits a beam lance there in skirmish setup
- **THEN** the pulse cannon's full price is refunded, the beam lance's price is spent, and the hardpoint holds the beam lance

#### Scenario: Wrong group refused
- **WHEN** the player tries to fit a torpedo tube to a primary hardpoint
- **THEN** the fit is refused and nothing is spent

### Requirement: Campaign sales return half

Removing or selling a system or a ship in the campaign SHALL refund half its price, rounded down.

#### Scenario: Selling a torpedo tube
- **WHEN** the player removes a torpedo tube priced 450 credits at an outfitter
- **THEN** 225 credits are refunded

### Requirement: Supplies stay within the hull's maximum

Each supply (marines, shuttles, mines, missile reloads, spare parts) SHALL be bought only up to the ship's maximum for it, and a supply the ship's systems cannot carry SHALL have a maximum of zero.

#### Scenario: No shuttle bay, no shuttles
- **WHEN** a ship without a shuttle bay opens its supplies
- **THEN** shuttles show a maximum of 0 and cannot be bought

### Requirement: Every wingman has an AI behaviour

Every ship in a fleet except the flagship SHALL have one AI behaviour (Escort, Attack my target, Hold at range or Defensive), chosen on the loadout screen.

#### Scenario: Default behaviour
- **WHEN** a wingman joins a fleet
- **THEN** its behaviour is Escort until the player changes it
