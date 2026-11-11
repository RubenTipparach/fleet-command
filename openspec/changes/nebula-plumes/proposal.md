# Proposal: nebula-plumes

## Why

The owner, 2026-11-10, while asking for recorded heavy-fleet battles: "add in our planets, and also write up some shaders
for nebula too", then "something like a 3d noise texture would be cool? something like how Ace Combat 7 does clouds,
entering a column of nebula plume can hide you from enemies. weapons have difficulty targetting, missiles dont quite work,
energy weapons 1/2 range", "and 1/2 acuracy to hit", "oh also some nebulas are ion storms, which will have cool
lightning", "which can strike ships, and cause a temprorary shut down of the reactor core, the ship loosing power for a
few seconds completely".

Until now a battle's nebula was one flat billboard over the whole field with the mockup's rules (a 30 % penalty to hit, a
6 km lock range, doubled electronic warfare). The owner asks for cloud a captain can see, fly into and hide in, and for
storms that are a hazard of their own.

## What Changes

- **A nebula is a column of cloud standing in the battle**, drawn by marching a 3D noise texture: billowing towers lit
  by the battle's sun, dark in their own shadow, glowing at their thin edges. A scenario places any number of them, each
  with a place, a radius, a density and a look (seven looks).
- **Inside a plume** (the circle the cloud stands on): a shot keeps half its chance to hit, beam and bolt weapons reach
  half as far, a missile loses its target with a chance every second, electronic warfare counts for more, and a ship in
  the plume cannot be locked from more than 60 km away unless the locker's ECCM beats its ECM. Each effect thins with the
  plume's density toward its edge.
- **An ion storm** is a plume a scenario marks as one: lightning flickers in its cloud, and finds each ship inside about
  three times a minute. A strike does no damage; it trips the reactor core for 5 seconds (no drive, no charging, no fire,
  no shields, no cloak, no tractor, no repair), and no bolt finds that ship again for 15 seconds after its core restarts.
- **What the captain sees**: the bolt from the cloud's head onto the ship, the cloud lit by it, the ship's lamps dark
  while its core is down, a line in the log, and the plumes on the radar dial and the tactical map (a storm in the
  caution colour, named).
- **Four heavy-fleet scenes** (`concord_heavy`, `krai_heavy`, `kelvane_heavy`, `ssarith_heavy`) with two planets, a plume
  and an ion storm each; the four Nebula scenes now stand under one plume in place of the billboard.

## Impact

- `src/Core/Sim`: `Rules.Cloak.cs` (the plume's rules), `Battle.Nebula.cs` and `Ship.Nebula.cs` (storms), every reader of
  a weapon's range (`Rules.RangeKm`); `data/battle/systems/cloak.json` (`nebula`); `NebulaTests`.
- `src/Game/View`: `nebula_plume.gdshader`, `NebulaPlume.cs`, `PlumeLooks.cs`, `BattleView.Nebula.cs`;
  `data/space/plumes.json`; `assets/space/plume_noise.bin` (`tools/space/plume_noise.py`).
- `src/Game/Ui`: the radar dial and the tactical map draw the plumes.
- Built in the game at the owner's direct ask, as the planets were; the battle console mockup's flat nebula is behind
  (tasks 3.x).
