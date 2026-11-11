## ADDED Requirements

### Requirement: Ships do not mend on their own

A ship's hull SHALL stay as its last battle left it until it is repaired at your own port or with a repair kit.

#### Scenario: Turns pass
- **WHEN** a ship at 40 % hull sees five turns end
- **THEN** its hull is still 40 %

### Requirement: A repair kit mends a share of a hull anywhere

Using a repair kit SHALL spend one kit from the fleet and mend `rules.kit_repair_pct` of a ship's hull, never past whole; with
no kit aboard it SHALL mend nothing and say why.

#### Scenario: One kit
- **WHEN** a ship at 40 % uses its one kit, then tries again
- **THEN** it is at 65 %, the fleet has no kits, and the second try is refused
