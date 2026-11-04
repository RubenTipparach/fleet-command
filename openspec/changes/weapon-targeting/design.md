# Design: weapon-targeting

## Context

Before this change a shot rolled one chance to hit, and Aim at decided which subsystem a damaging hit landed on (always
the aimed one; 35 % of the time a random one when aimed at the hull), whatever the weapon and whatever side of the
target faced you. The owner describes the reference game's rule instead: most weapons hit what the receiving arc
exposes; a few aim (2026-11-04, quoted in the proposal). The Starfleet Command 2 manual's pages on firing and damage
(printed pages 108 onward, the battle console) are to be cited here when checked (open question W6); the rule below is
the owner's account, in our own numbers.

## Decisions

### 1. The chopping block

Each subsystem has a place in the hull in half-lengths, as the mounts do (`SYS_POS`: sensors at the bow, weapons
control forward, transporters starboard of centre, shields at the centre, reactor just aft, tractor aft, engines at the
stern). A hit through facing f reaches a subsystem or mount whose bearing from the centre is within 60 degrees of the
facing's centre (`reach_deg`), or which lies within 0.3 half-lengths of the centre (`internal_r`: the internals are
reachable from any side). That set is the chopping block.

| Weapon | `aim` | Where its damage lands |
|---|---|---|
| Lance, disruptor | precise | On the aimed subsystem 50 % of the time (`direct_p`) when it is on the block; otherwise a random block subsystem 60 % of the time (`spread_p`) |
| Torpedo, missile, mine, collision | arc | A random block subsystem 60 % of the time; the aim does not matter |
| Every hit | | An exposed mount 25 % of the time (`mount_p`), plus 25 % for a direct hit aimed at weapons |

Subsystem damage per point through the shields stays 0.02 of its health (`sys_per_dmg`); the reactor takes 0.012 of its
confinement (`reactor_per_dmg`). Aiming at a subsystem still costs 10 points of hit chance, as before.

### 2. Showing it

- **Aim at** is a row of eight icon keys (hull, weapons control, engines, sensors, shield generators, reactor, tractor,
  transporters) in the skin's icon set, the name on hover.
- **Brackets**: the target's systems display (the Target box, and anywhere a target's display is drawn) draws a small red
  bracket over every subsystem box and mount on the block from the facing turned toward you. Turn the target, and the
  brackets move.
- **Hit details**: shift+click an aim icon or a weapon mount. A sheet over the battle: the target's facing toward you and
  the block, the rule in one line, then each weapon (or the one clicked): precise or by arc, in arc, range, chance to hit,
  chance to put the hit on the aimed subsystem (to-hit x 50 % when it is exposed; "Not exposed" otherwise; "By arc" for
  by-arc weapons), and its pin. The hint "Shift+click: hit details" sits beside the Aim at label.

### 3. Pinned fire

A mount's `pin` is `{ to, aim }`. Fire fires each selected, ready mount at its pinned ship (with its pinned aim) when the
pin's ship lives, else at the locked target with the current aim; Fire's count counts the same set (`fireable()`). A pin
stops, with a Tactical message, when its ship is destroyed or the subsystem it aims at reaches 0 health (`pinsStep()`).
Ways to pin: Pin weapons on the Tactical station (the selected weapons, to the locked target, with the current aim); a
cell of the expanded Tactical grid (that weapon to that contact; click again to unpin); Unpin all. A pinned mount shows a
red flag on its box's corner, and its hover card says where it is pinned.

### 4. The expanded Tactical station

Expanded, the contacts take the left 40 % and a grid takes the rest: a row per weapon (with Prec or Arc), a column per
contact (six at most), each cell a key showing the chance to hit now, a dot when out of arc or range; viable cells lit,
pinned cells engaged. This is the owner's "expand button to view viable targets to show with various weapons".

### 5. Nebulae

A scene may place a nebula (a circle on the battle plane with a density). Inside, at its density (thinning to 70 % at
the edge): every shot loses 0.3 x density of hit chance; ECM and ECCM count (1 + 2 x density) times as much; and a ship
inside cannot be locked from beyond 6 km unless the locker's ECCM is greater than its ECM: it is an "Unknown contact" on
the Tactical list, and a lock attempt says why. It is drawn as violet glow in the battle and a tinted circle on the map.

### 6. The scenes

| Scene | Objective | Tests |
|---|---|---|
| Disable | Disable the Vorrak's engines and weapons without destroying it (each under 35 % health, the Disabled status); fails, with a message, if it is destroyed | Aiming precise weapons; manoeuvring so the aimed system is on the block |
| Pinned fire | Pin a weapon to each of three raiders closing from three sides and destroy them all | Pins, the expanded grid |
| Nebula | Find and destroy a raider jamming (ECM 3) from inside a nebula; no target at the start | ECM, ECCM, the lock rule |

## Recommendations (the owner: "if you think of anything recommend some stuff")

Mechanics from the reference games' family the mockup does not have yet, each a candidate for its own change, in rough
order of how much they add to a captain's choices. The owner's answers, 2026-11-04: 1, 2, 3, 4 and 10 built (changes
`damage-control`, `weapon-arming`, `cloaking`, `officers`, `boarding-defence`); "6 and 5 are good ideas too though
low.priority" (accepted, not started); 7, 8 and 9 not answered.

1. **Damage control priorities**: a repair queue the captain orders (which subsystem first), with crew shown working.
2. **Weapon arming choices per type**: proximity torpedoes (a wider block, lower damage), overloaded lances at short range only.
3. **Cloaking** for one faction: invisible and unfireable while cloaked, a fade-in on decloak, and ECCM or a scan to find it.
4. **Officers** (the SFC3 manual's crew, printed pages 28-29): skills that shift hit chance, repair speed or HET safety.
5. **Electronic warfare drones**: a deployable decoy that soaks seeking weapons and adds ECM.
6. **Shuttle and fighter recall, scatter-packs** (a shuttle loaded with missiles).
7. **Emergency power**: a one-off surge to shields or engines at a reactor heat cost.
8. **Surrender and hailing**: enemies that strike their colours when disabled, for capture without boarding.
9. **Terrain**: ion storms (shields down), black hole gravity wells (already in Warp), asteroid fields for cover.
10. **Morale and boarding defence**: crew quality that falls with damage and makes boarding easier.

## Open questions

| # | Question | Provisional answer |
|---|---|---|
| W1 | Is 50 % the right direct-hit chance for precise weapons? | 50 %, the owner's "50/50 spread" |
| W2 | Should a by-arc hit always damage something on the block, or 60 % of the time? | 60 %, so hull-only hits still happen |
| W3 | Internals (shields, reactor, transporters at the centre) reachable from every side? | Yes, as the reference game's internal boxes |
| W4 | Should the AI pin its weapons and aim at subsystems? | Not yet: it fires at its one target at the hull |
| W5 | Should a pinned weapon fire on its own (auto-fire) when ready? | No: it fires with Fire, so the captain keeps the trigger |
| W6 | Cite the SFC2 manual's pages on firing arcs and damage allocation | To be read and cited (CLAUDE.md section 1) |
