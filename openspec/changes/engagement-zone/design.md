# Design: engagement-zone

## The zone

A square on the battle plane, centred on the middle of the two fleets' starts, `side_km` across (`data/battle/systems/
zone.json`); a scenario may set its own centre and side (`zone`: `x_m`, `z_m`, `side_km`). The square is the rule; the
radar dial, the tactical map and the 3D view draw that same square (CLAUDE.md 6.1: `Rules.InZone` is the one reader).

| Number | Provisional value | Why |
|---|---|---|
| `side_km` | 300 km | The heavy scenes start their fleets about 105 km apart and their plumes reach about 130 km from the middle; 300 km leaves room to manoeuvre round the field without the border being part of every fight |
| `return_s` | 30 s | The owner's number |
| `warn_km` | 20 km | A quiet caution on the radar once the commanded ship is this close to the border, before any countdown |

## Leaving

- The countdown runs while the **commanded ship** is outside the square. It shows on screen, over the 3D view, in the
  caution colour, with the seconds left; the log says so once when it starts and once when it is called off.
- Flying back inside calls it off. Leaving again starts it from the full 30 s (provisional, survey EZ3).
- Taking command of another ship that is inside the zone calls it off; one outside starts its own.
- The other ships of the fleet do not trigger it (they follow their own orders and the AI keeps inside: task 1.5).

## Abandoning

At zero the mission is abandoned:
- **The warp drive works** (`Battle.WarpFunctional`: at least one nacelle working and the coils not wrecked; the same
  test `Battle.Warp.cs` uses to call off a Withdraw): the fleet warps out at once, with no charge or plot needed (the
  retreat is the order); the battle ends `abandoned`.
- **It does not**: the ship self-destructs (destroyed, as by its own reactor's meltdown, `Battle.Reactor.cs`). From the
  moment the countdown starts with the drive down, the warning reads as a self-destruct countdown (owner: "have the
  warning for self destruct show up on screen"), in the alarm colour. If another ship of the fleet is left, command
  passes to it and the battle goes on; if none, it ends `lost`.

## The screen

- The warning banner: centred near the top of the 3D view, under the top strip; one line of the warning and the seconds
  in large type. Two states: retreat (caution) and self-destruct (alarm, pulsing). It never covers the console's panels
  and is drawn by each skin in its own language (the Concord master first, CLAUDE.md 7).
- The radar dial and the tactical map: the square in the caution colour, brighter within `warn_km`.
- The 3D view: a faint line where the square meets the plane, brighter as the camera's ship nears it.

## Open questions

| # | Question | Provisional answer |
|---|---|---|
| EZ1 | How big is the zone? | 300 km a side, centred between the fleets' starts |
| EZ2 | What does abandoning cost in the campaign? | The mission failed, as a withdrawal (`campaign-missions`) |
| EZ3 | Does a ship that comes back and leaves again get the full 30 s again? | Yes |
| EZ4 | Do the other ships of the fleet warp out with the commanded one, and what of a wingman without a working drive? | They warp out with it; a wingman without a working drive is left behind (lost to the fleet) |
| EZ5 | Do enemy ships respect the zone? | They keep inside it; an enemy that leaves has fled (counts as gone for the objectives) |
