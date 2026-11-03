# Proposal: warp

## Why

The owner, 2026-11-02, on mockup 2: "we should also include a system for warp systems as well. the generator powers
nacelles, with anti-matter reactions. nacelles need to show how much power they draw and how much damage theyve taken.
Jump risk depends on a few factors:

- damage on nacelles
- coolant remaining in system
- power stored in banks (need lots to initate jump)
- navigation preparedness
- reactor stability
- quantum-gravitational stability - requires safety override if environment is highly unstable. this usually happens
  around neutron stars, black holes, and anomalies. usually not easily produced by known civilizations"

## What Changes

- **A Warp mode on the power station**, beside Allocation and Reactor (the reactor's generator powers the nacelles, and
  the survey kept nine stations on one rail, B3).
- **Two nacelles**, port and starboard, each drawn with its health (status colour) and its power draw in E; hits damage
  them.
- **Warp banks** charged by the nacelles from the energy put into warp (a new energy row, counted in Systems); a jump
  needs most of the banks.
- **A course plot** that builds over time and holds while the ordered heading stays on it.
- **Six risk factors** (nacelles, coolant reserve, banks, navigation, reactor stability, quantum-gravitational field),
  combined into one jump risk, which is the number the jump rolls against.
- **Quantum-gravitational instability** from the surroundings (gravity wells: neutron stars, black holes, anomalies);
  above a threshold the jump needs the **safety override**.
- **The jump**: a success takes the ship out of the battle; a failure is a mishap scaled by the risk, and a very bad one
  breaches the core.
- **A Warp test scene**: near a neutron star, frigates closing.

## Impact

- Mockup 2 (`docs/mockups/battle-console.html`), version 29.
- The reactor change gains a coolant reserve (leaking from damaged loops, spent by jumps) and a stability measure the
  warp drive reads.
- No game code: the mockup waits for the owner's approvals (CLAUDE.md section 4).
