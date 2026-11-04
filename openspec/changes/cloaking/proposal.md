# Proposal: cloaking

## Why

The owner, 2026-11-04, answering the weapon-targeting change's recommendation "Cloaking for one faction: invisible and
unfireable while cloaked, a fade-in on decloak, and ECCM or a scan to find it": "Yes, we need both sides to cloak. And
scan with eccm".

## What Changes

- **A cloak** on the Concord's heavy cruiser and destroyer and the Kethran's destroyer and frigate: Cloak on the Science
  station fades the ship out; cloaked it draws reactor energy, has no shields and cannot fire, and enemies cannot see or
  lock it.
- **The sweep**: Sweep on the Science station spends battery to reveal every cloaked enemy within its reach, which ECCM
  extends, for a few seconds; a revealed ship can be locked and fired on, at a penalty.
- **The AI** cloaks and decloaks to strike, and sweeps when it cannot find you.
- **What the captain sees**: your own ship shimmers when cloaked; a cloaked enemy is gone from the view, the contacts,
  the radar and the map until it is revealed or comes within 1.5 km; a sweep is a ring spreading over the battle.
- **The Cloak scene**: a cloaked Kethran destroyer hunting the Resolute.

## Impact

- `docs/mockups/battle-console.html` (both skins): `CLOAK`, `AI_CLOAK`, `cloakStep`, `setCloak`, `sweep`, `sweepKm`,
  `detected`, `seenFoes`, the Science keys, the cloak look and sweep rings, the Cloak scene, shots `scene_cloak`, `cloak_own`.
- Mockup only.
