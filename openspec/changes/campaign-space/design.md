# Design: campaign-space

## Space

Laid out once, when a campaign starts, after the territory (`Campaign.MakeSpace`), from the campaign's seeded generator
(Mulberry32, the mockup's), so a seed always gives the same map:

1. Each system's star is placed.
2. Its belt: each sector exactly `belt_r` sectors from the star is taken with chance `belt_share`, unless it is a port or a
   star.
3. Its nebula: a start picked among the free sectors two or three from the star, grown to `nebula` sectors, each new one a
   random free neighbour of the patch so far (neighbours in the grid's fixed order).
4. The deep: `dust_clouds` nebulae of `dust_size` sectors and `asteroid_fields` fields of `field_size` sectors, each grown
   the same way from a sector at least `clear_of_stars` (4) from every star.
5. The ports are placed over it (each port sector is open space).

| System (star) | Near | Belt | Nebula |
|---|---|---|---|
| 2,9 | Halcyon | ring 3, half | none |
| 5,2 | Aster | none | 5 |
| 8,15 | Meridian | ring 3, 45 % | 3 |
| 12,12 | Selene | ring 2, half | none |
| 16,1 | Calder | ring 2, half | 4 |
| 17,12 | Tessaly | none | 6 |
| 22,10 | Vahr | ring 3, half | none |
| 28,2 | Kesh Prime | ring 2, 60 % | 3 |
| 25,15 | (Redoubt) | ring 2, 70 % | none |
| 20,20 | (Rook's Nest) | none | 5 |

Each star was placed two sectors from its world, as far as it could be from the other ports and stars.

## The fog of war

| State | When | Drawn |
|---|---|---|
| Unexplored | never seen | flat black, unlit, nothing on it, no borders |
| Revealed | seen once (`Sector.Known`) | its terrain, star, clouds, rocks and port; glass and borders in the empire it had when last seen (`Sector.KnownOwner`), darker; no fleets |
| Visible | seen now (`Campaign.Visible`) | everything, other fleets included |

Seen now: every sector your empire holds, every sector within 2 of your fleet, within 1 of your faction's other fleets and
freighters, and of your allies' (standing 25 or more). `Reveal` marks what is seen as known, with its holder, when a
campaign starts or loads, after every sector the fleet flies, and after the factions' turn. An offered mission's sector is
revealed (its intel). A route may still be planned through the unexplored: the fleet explores by flying.

## Patrols

A contract is picked by weight: Patrol 4, Nebula sweep, Belt patrol and Orbital patrol 2 each, the other eight contracts 1,
so about half of what is offered is a patrol of some kind. Each is the Patrol's battle (raiders by the offer's strength)
with its own place and briefing.

## Open questions

| # | Question | Provisional |
|---|---|---|
| F1 | Should revealed sectors show a fleet's last known position (a ghost), as some strategy games do? | No: fleets only where seen now |
| F2 | Do asteroid and nebula sectors change the battle (rocks on the field, sensors short in a cloud)? | The sky only, as today; rocks to follow (task 2.3) |
| F3 | Should stars be seen from afar through the fog, as a lit beacon? | No: "black fully" |
