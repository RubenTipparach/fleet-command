# Proposal: special-weapons

## Why

The owner, 2026-11-04: "we need to develop special weapons for each faction, currently only concord and krai are fully
done, so start with those two, we also ened high end fx + shaders for both of those things too".

Every ship today fires the same five weapons (lance, pulse torpedo, disruptor, missile, point defence). The reference
games gave each empire heavy weapons of its own, each with its own rule (SFC2 manual, printed pages 22-25; SFC3 manual,
printed pages 12-13), and that is much of what made a race feel like itself. This change gives the two finished factions,
the Concord and the Krai Warhost, two special weapons each, drawn from how each faction thinks. The look (effects and
shaders) is its sister change, `weapon-fx`.

## What Changes

- **Concord: Chorus lance.** The Albatross-class cruiser's deflector fires one long beam. Every Concord ship within 8 km
  with a lance ready may join it, spending that lance; each one that joins makes the beam 35 % stronger, up to three.
- **Concord: Quell torpedo.** A load for any torpedo tube. It does no damage: it drops the shield facing it strikes and
  stuns every mount on that half of the target for 8 s.
- **Krai: Forge driver.** The spinal cannon throws a white-hot spike: 40 % of its damage goes straight through the shield,
  and the hull where it lands burns for 6 s.
- **Krai: Blood charge.** An order from the helm on every Krai ship: a 5 s drive surge to 2.2 times top speed, then a ram
  with the bayonet, which ignores the struck facing's shield. Damage grows with closing speed; the rammer takes 30 % back.
- Their numbers in one data file, `data/battle/special_weapons.json`, read by the mockup now and by the game later.
- **Mockup 12, the special weapons range** (`docs/mockups/special-weapons.html`): the four weapons fired on the approved
  ships, with every readout computed from the data.

## Impact

- New: `data/battle/special_weapons.json`, `docs/mockups/special-weapons.html`, this change.
- Later, once the mockup is approved for implementation (CLAUDE.md section 4): `src/Core` (the data loader, `Rules`, the
  battle), the weapons panel and helm in both skins, the AI, the icons, the battle console mockup, the playtest suite.
- Mockup only for now. The game is unchanged by this change.
