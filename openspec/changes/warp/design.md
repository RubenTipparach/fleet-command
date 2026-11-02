# Design: warp

The owner's words are in the proposal. Everything here is the mockup's (`docs/mockups/battle-console.html`, the `WARP`
table); every number is data and provisional (open questions below).

## The model

| Part | Rule | Number |
|---|---|---|
| Nacelles | Two, port and starboard. The energy put into warp (a 0-10 E row, counted in Systems) is split between the nacelles still working; each charges the banks at 0.5 of what it draws times its health. A hit damages a nacelle 1 time in 7, by 25 % | `charge_k` 0.5 |
| Banks | Hold 60 E; a jump needs 40 E (a white line on the gauge) and spends it | `bank_e` 60, `jump_e` 40 |
| Course plot | Plot course takes the ordered heading and builds over 20 s (faster with healthy sensors). If a new heading more than 15 degrees off is ordered, the plot degrades at 20 % a second until the heading is back | `plot_s` 20, `plot_drift_deg` 15 |
| Coolant reserve | The reactor's coolant reserve, 0 to 100 %; damaged loops leak it (1 % a second per lost loop's worth of health); a jump spends 25 % | `leak_per_s` 0.01, `coolant_jump` 0.25 |
| Reactor stability | Confinement times coil alignment times the core's distance from its limit, 0 to 1 | from the reactor |
| Q-G field | Each gravity well has a strength at its centre, falling to 0 at its reach; the field at the ship is the strongest. Above 0.5 the jump needs the safety override | `qg_block` 0.5 |

**Jump risk.** Each factor gives a risk, 0 to 1, and the jump risk is 1 minus the product of (1 minus each):

| Factor | Risk |
|---|---|
| Nacelles | (1 - average health) x 0.8; 95 % if a nacelle is out |
| Coolant | (0.5 - reserve) x 1.2 below a 50 % reserve, else 0 |
| Banks | (1 - banks / 60) x 0.3 |
| Navigation | (1 - plot) x 0.6 |
| Reactor | (1 - stability) x 0.7 |
| Q-G field | field x 0.3, or x 0.9 above the threshold (with the override) |

**The jump** is refused, with the reason shown, while the banks are under 40 E or the field is over the threshold
without the override. Otherwise it rolls once against the jump risk (its own seeded stream). A success: the warp field
comes up, the ship streaks forward for 1.2 s and leaves the battle ("Jumped"). A failure: the nacelles, coolant and
hull are damaged in proportion to the risk, the banks drain and the plot resets; with a risk over 80 % and a bad roll,
the warp core breaches (the reactor's meltdown).

## The screen (Power, Warp)

Left, the two nacelles as rounded bars filled from the bottom by health in their status colour (Active green, Damaged
amber, Disabled red), ringed in Sky while drawing, with "Port 1.9 E" and "Stbd 1.9 E" under them; between them the banks
gauge (green once over the threshold) with its threshold line and "23/60". Right, the six factors, each a label, a thin
bar of its risk (green under 10 %, amber under 30 %, red above) and its percentage, then **Jump risk** and the total.
Under them three keys: **Plot course** (showing the plot's progress, lit when complete), **Override** (lit when on) and
**Jump** (disabled with its reason under it). The warp energy boxes sit at the foot, the same row as the power station's.
Hovering a nacelle shows its health, status and draw. The Allocation page lists the warp draw beside the coolant pumps.

In the view, a gravity well is a small white-hot star with its name and the field strength at your ship over it, and an
amber ring where the field crosses the override threshold.

## The Warp scene

The Resolute 3 km from a neutron star (strength 0.9, reach 8 km: 56 % at the start, over the threshold), banks at 12 E,
the port nacelle at 70 %, coolant at 80 %, 4 E in warp; two Kethran frigates closing from 12 km astern. Played through:
turn about, plot, run clear of the star at 180 m/s, and jump at 30 s with a 14 % risk: "Resolute jumped to warp". Forced
by the star with full banks and no plot, the jump is refused ("Q-G field: override needed"); with the override, a risk
of 83 % fails and breaches the core.

## Open questions

| Id | Question | Recommendation (built in the mockup) |
|---|---|---|
| J1 | Banks 60 E, a jump needs 40 E, a 20 s plot: right scale? | **As built** |
| J2 | The six risk weights (nacelles 0.8, coolant 1.2 under 50 %, banks 0.3, navigation 0.6, reactor 0.7, field 0.3 or 0.9 over the threshold), combined as independent risks? | **As built** |
| J3 | A failed jump: damage scaled by the risk, a core breach over 80 %? | **As built** |
| J4 | Where the warp page lives: a mode of Power, or its own station on the rail? | **A mode of Power** (nine stations stay, survey B3) |
| J5 | Do enemy ships jump out too (to escape when beaten)? | **Yes, later**: the same rule, an AI crew charging and plotting when its retreat threshold is reached |
