# Proposal: fleet-skins

## Why

The owner, 2026-11-04: "this time combine uis together, so we can hop between different ships".

## What Changes

- **The console follows the ship you command.** Every skin's kit, library and type are in the one page. Taking a ship
  of another faction (the Fleet key on the name plate, or Take command) swaps the whole console to that faction's skin.
  No rule and no layout changes.
- **Allied factions**: the Krai Warhost and the Ssarith Hunt can now fly on your side, each in its own skin.
- **The Allied fleet scene**: a Concord cruiser, a Krai cruiser and a Ssarith stalker against the Kethran and Vos ships.
- **The Three races scene** (owner, 2026-11-04: "create a new test scenario for me to swap between three ships of
  different races all under my control"): the same three ships at rest, a Kethran outpost far out of range, nothing
  attacking, to hop between the skins and try every panel in each. Keys 1, 2 and 3 take your first, second and third ship.
- **Every ship you can command has its own officers**, named and ranked in its own faction's way.
- A faction's page (Krai, Ssarith) still opens in its own skin. Its player side wears that skin's names in the
  single-faction scenes, and the allied scene shows every ship as itself.

## Impact

- `docs/mockups/battle-console.html` (and the faction pages written from it):
  - `SKINS`, `bindSkin`, `applySkin`, `skinOf`, one kit per skin (`KITS`);
  - `FACTION.krai` and `FACTION.ssarith`, faction marker colours as role names;
  - per-ship officers, rank names from the skin worn;
  - the Allied fleet and Three races scenes and their shots; keys 1, 2 and 3 for the ships of your side.
- `tools/mockups/skin_page.py`: a faction's page now changes only its meta, title and header.
- Mockup only.
