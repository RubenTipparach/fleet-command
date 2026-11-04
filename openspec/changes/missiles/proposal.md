# Proposal: missiles

## Why

The owner, 2026-11-04, on the weapons range (mockup 16): "all factions can have missiles too, so develop missiels (in
starfleet command they are refered to as drones)".

Today a missile is one weapon on one class (the Vos corsair) and the barrage scene: it flies at 1,100 m/s, homes, does
11, and point defence can stop it. It costs reactor energy like a beam, has no magazine and never runs out of fuel. The
reference game made its missiles a system of their own (SFC2 manual, printed pages 24 and 190-192): racks with a number
of rounds and a reload, no energy to fire, two warhead sizes, a flight time, a limit on how many one ship can guide, and
fire one or fire all.

## What Changes

- **Every faction can fit missile racks**, as a loadout choice (SFC3 manual, printed pages 28-29), not only the Vos.
- **Racks and rounds, not energy**: a rack holds a magazine of rounds and reloads on a cycle; firing costs no reactor
  energy (the battle console mockup already powers missiles on their own).
- **Four grades, by colour** (owner: "red, orange, blue, green ... red being lowest grade, green being highest"): faster,
  harder-hitting and harder to stop as the grade rises; today's missile is the orange grade (`data/battle/missiles.json`).
- **Fuel**: a missile flies for a set time, then drifts dead.
- **Guidance**: a ship guides at most six missiles at once; a seventh launch drops the oldest. A launcher whose sensors
  are out loses its missiles' lock.
- **Fire one or fire all** on the weapons panel.
- **The look** (change `weapon-fx`): a plain dart with a big ball of drive light and a long tail in its grade's colour,
  weaving as it homes; shown now on the weapons range and in the battle console mockup.

## Impact

- Now: the weapons range (mockup 16) fires missile salvos both ways with point defence; the battle console mockup draws
  missiles with the new look. Survey section D.
- Later (after approval for implementation): `data/battle/weapons.json` (rounds, reload, fuel, the heavy warhead) and
  `classes.json` (racks for every faction), `src/Core` (`DataLoader`, `Rules`, the battle), the weapons panel.
