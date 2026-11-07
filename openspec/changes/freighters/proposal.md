# Proposal: freighters

## Why

The owner, 2026-11-07: "we need to design freighter for every faction in the game currently".

Freighters already have work to do, and no ships to do it with:
- **The campaign map** runs every faction's freighters along its trade routes (`game-flow` design, "Freighters"). The
  piece it draws is a placeholder: a box spine, three box pods, a bridge and a drive (`freighterModel()` in
  `docs/mockups/game-flow.html`, `src/Game/Flow/MapPieces.cs`), "a placeholder until the factions' freighters are
  designed".
- **Missions**:
  - Convoy defense (keep two of three alive) and Convoy ambush (destroy two of three), in `data/missions/defend.json`
    and `data/missions/ambush.json`.
  - Distress, where a freighter calls for help against Vos raiders (`data/missions/distress.json`).
  - The mission guide's Cold Furnace (three Concord freighters running for a jump point) and The Long Haul (escort the
    Krai ore barges), in `docs/design/mission-guide.html`.
- **In battle**: the campaign's `freighter` hull fights as the Kethran frigate stand-in (`battle_class: "kff"` in
  `data/campaign/campaign.json`).

The reference game had the same jobs. A campaign mission could be an attack on an enemy base or convoy (SFC3 manual,
printed page 37). Missions to protect a convoy or raid an enemy supply route appeared in some hexes, and their outcome
moved the economy (SFC3, printed page 42). What we take is the role: a slow, weak ship worth guarding and worth
raiding. Its ships, names and text stay the reference game's (CLAUDE.md section 1).

Then, the same day: "Freighter should only focus on approved factions", "Narrow down freighter options to ones where cargo
is visible. And make sure all cargo is the same size", "The containers I mean", "The cargo hull shape it self can be the
way it was in the draft".

Two lessons from the Hunt and the Institute shape how this starts:
- **Breadth before depth.** A board of massings to pick from, before anything is detailed (mockups 12 and 23).
- **Culture, not anatomy, and the culture is the owner's** (CLAUDE.md section 1; Ship Design Bible C1-C5). No faction
  has a merchant or civilian tradition written down, so every concept here comes from a tradition drafted inside its
  faction, marked provisional, and the survey asks the owner to keep, change or reject each (survey section V).

## What Changes

- **Mockup 24 version 4**, `docs/mockups/freighter-concepts.html`: a board of eight freighter blockouts, two for each
  approved faction (the Concord, the Krai Warhost, the Ssarith Hunt, the Kelvane Institute).
  - About 150-180 m long, a cruiser to a battleship of their own faction.
  - Every one carries its cargo in sight, and every container is the same, 6 by 6 by 12 m.
  - Each is one grown hull on the shared `docs/mockups/lib/hull-grow.js`, in the faction's own cross-section and
    colours; the containers are the only placed pieces.
  - Each names the tradition it comes from.
  - Shown at true scale beside the Concord Petrel and a person, in faction colours or plain clay.
  - Each can be viewed on its own, and one faction's two at once.
- **Ways of carrying the containers in sight**: a spine (racked outside along a spine), a cradle (the hull parts round
  them and closes again) and, for the Institute's Twin ledger, between two hulls open at the bow.
- **Survey section V**: the drafted traditions, the picks, one class or two, how a freighter is armed, its size.
- Nothing in the game changes until the owner approves the picks for implementation.

## Impact

- New: `openspec/changes/freighters/`, `docs/mockups/freighter-concepts.html`, its shots, `docs/artifacts/freighter-concepts/`.
- Changed:
  - `mockup-work.md` (row 24 and its section).
  - The tracker's source (`docs/mockups/tracker.html`, M24).
  - The survey's source (`docs/design/survey.html`, section V).
- Later, after approval for implementation (tasks section 3):
  - A battle class per approved faction replaces the `kff` stand-in.
  - The campaign map's placeholder becomes each faction's freighter.
  - The convoy missions field them.
