# Design: field-repair

| Way | Where | Cost | Mends |
|---|---|---|---|
| Repair | your own port (PortPanel, each ship) | tier cost x credit rate x missing share x 0.5 | to whole |
| Repair kit | anywhere, between battles (sector panel) | one kit (4 points, 40 cr; at most one per 20 hull points a ship) | 25 % of the hull, never past whole |

`Campaign.FieldRepair(ship)` spends one kit (the ship's own, else the first ship's that has one) and mends
`rules.kit_repair_pct`. The sector panel's Use a kit mends the ship with the least hull (`GameFlow.FieldRepair`). Nothing in
`EndTurn` or `FactionsMove` touches a hull; a test runs five turns and checks it.

## Open questions

| # | Question | Provisional answer |
|---|---|---|
| R1 | Should kits mend systems too, as the old line said? | Hull only: the campaign tracks no system damage between battles |
| R2 | Should a kit be usable in battle? | No: battle damage control is the repair crews' (`damage-control`) |
