# Design: reactor

A system of our own: the reference game's energy page (SFC2 manual, printed pages 144-146: power, systems, excess
energy, batteries) had a fixed reactor. Here the reactor can be damaged, steered and lost.

## The model (mockup's numbers, in the page's `REACTOR` table)

| Quantity | Value | Unit |
|---|---|---|
| Coils | 3, alignment -1 to 1 (0 is aligned) | |
| Output | rating x (1 - 0.6 x mean coil misalignment) | E (energy points per second) |
| Heat in | 4 x output / rating + 12 x mean misalignment | % of core limit per s |
| Heat out | 0.04 x cooling x core temperature | % per s |
| Cooling | sum over loops of flow x loop health | flow boxes |
| Coolant loops | 2, flow 0-5 boxes, 1 E per box | |
| Core floor, limit | 20 %, 100 % | % |
| Meltdown | 20 s with cooling 0 or core at its limit; counts back at half speed when clear | s |
| Blast | 45 damage at the centre, falling to 0 at 3 km | |
| Nudge | 0.05 per press | alignment |
| Drift | random walk, 0.5 x (1 - confinement) per s | alignment |
| A hit | 30 % a coil is knocked up to 0.4 and confinement loses 1.2 % per hull point; 18 % a loop loses 35-70 % health | |

At full output, aligned, with both loops at 1 box, the core settles at 50 %. One loop lost doubles that toward the
limit; both lost starts the meltdown clock. Checked headless in the page (`window.MOCKUP_SIM`): equilibrium 50 %;
pumps off, alive at 19 s, gone at 21 s; draw past output off caps the allocation; on, the battery empties and systems
brown out.

## Energy

- **Spare** (output above the draw) charges the battery at half rate. **Deficit** drains it one for one. With the
  battery empty, every system's share is scaled by output / draw (the brownout): capacitor charging, shield regen,
  speed limit, repair, transporter, scan.
- **Draw past output** (off by default) only decides whether the captain may allocate past the output. A damaged
  reactor can fall below the allocation with it off; the battery covers that, then the brownout does.

## The screen (Concord skin)

- **The reactor diagram** (owner, 2026-11-01: "I want like an actual diagram of the nuclear reactor and be able to like
  interact with it", then "please improve that diagram", "the loop a and loop b should look like loops", and "simpler
  please!"). Only what the captain acts on:
  - **The core**: the vessel's two walls and the plasma ring between them, coloured by temperature (Sky, amber from
    65 %, red from 85 %; pulsing while the meltdown clock runs) and pushed off centre by the coils' misalignment; CORE
    and the temperature in the middle.
  - **Three coils** (1 top, 2 left, 3 bottom), numbered, each with a green ring where it belongs: **drag a coil onto its
    ring**. Its value shows while it is pointed at or dragged.
  - **Two coolant loops**, each a closed circuit through the core: out past the pump, round the radiator, back. Hot
    (core-coloured) out, Sky back, a few beads moving at the flow. The pump shows its flow, with **minus and plus**
    beside it. Amber when damaged, a broken red line when offline.
  - **Warnings** in the corner: COOLING LOW, or MELTDOWN IN n S flashing.
  - Docked 348 x 276 px; expanded, drawn large beside the same controls as numbers. Checked headless: dragging coil 1
    from +0.56 onto its ring sets it to 0.00; plus on loop A raises its flow from 1 to 2.
- **Reactor mode**: output and efficiency; the core temperature bar (warning from 85 %, and the meltdown countdown in
  its label); three coil gauges (a track, a centre mark, a green safe band of 0.1, a dot green, amber or red by
  misalignment) with minus and plus keys and the value; two coolant loops as five flow boxes each with the loop's
  health or OFFLINE; the Draw past output switch. Expanded, it adds confinement, cooling, battery and brownout.
- **Allocation mode** shows the coolant pumps' draw ("set in Reactor") and the battery.
- **Bottom strip**: Power blue over Systems green (proposed kit piece `bar.fill.accord`), Battery beside them.

## Open questions

| Id | Question | Recommendation (built in the mockup) |
|---|---|---|
| R1 | Is 20 s from coolant loss to meltdown right? | **Decided** (survey, 2026-11-01): "Depends on how much output the reactor generates and heat build up. This should be temperature based and the amount of heat tolerance the materials into the reactor can withstand." *Queued*: meltdown from a material temperature limit, not a fixed time |
| R2 | An emergency shutdown (scram): drop the output to zero to stop a meltdown? | **Decided** (survey, 2026-11-01): yes, a scram now. *Queued* |
| R3 | Can enemy and wingman reactors melt down too? | **Decided** (survey, 2026-11-01): one rule for every ship, as built |
| R4 | Coil alignment by minus and plus keys, or by dragging the dot? | **Decided** (survey, 2026-11-01): drag the dot. *Queued* |
