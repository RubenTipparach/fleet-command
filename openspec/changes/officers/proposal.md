# Proposal: officers

## Why

The owner, 2026-11-04, answering the weapon-targeting change's recommendation "Officers (the SFC3 manual's crew, printed
pages 28-29): skills that shift hit chance, repair speed or HET safety": "We need officer slots for stations. Then when
you hover over officer. Slots it shows their rank and abilities."

## What Changes

- **An officer slot on every station**, on the panel's top edge left of its title: the officer's rank as pips and name.
- **Hover**: a card with the officer's rank and name, the station, their specialty, their skill, and what they bring to
  this station, in numbers.
- **Click**: the next officer in reserve takes the slot. An officer outside their specialty counts half their skill.
- **Abilities that the rules read**: high energy turns, reactor drift, chance to hit, intercepts, scan speed and sweep
  reach, enemy ECM countered, repair crews, ambush and fighting aboard and pad recharge, wingmen's chance to hit.
- The Krai skin names its officers and ranks its own way.

## Impact

- `docs/mockups/battle-console.html`: `OFFICER`, `RANKS`, `SKILLS`, `ABILITIES`, `ROSTER`, `officerSkill`, `hetOdds`,
  `padPower`, the slot; `docs/mockups/lib/krai.js` (`player.ranks`, `player.officers`). Shots `officer_tip`, `officer_repair`.
- Mockup only. After the Starfleet Command 3 manual, printed pages 28-29 (officers), in our own words and numbers.
