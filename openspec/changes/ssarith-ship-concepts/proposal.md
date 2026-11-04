# Proposal: ssarith-ship-concepts

## Why

The owner, 2026-11-04: "using current bibles and mockups on ship design, lets consider the Ssarith Hunt culture and how
their ships will look."

The Ssarith Hunt has a console skin (`console-skins`, version 13 onward) and an emblem draft (`faction-emblems`, E11), but
no ships. Its culture is already written and approved (survey U7, 2026-11-01: "yes, as written"): "The hunt is the only
honest sport: the quarry must be worthy, the stalk patient, the kill earned and counted"; "Big-boned reptiles with heavy
clawed hands, slow and silent until they strike"; "Squat armoured hunting ships with broad prows, dark and mottled, built
to wait unseen". The owner set their silhouette in the ships thread (2026-11-02: "gorn emphasizes claw like features,
since they enjoy hunting"), and survey A8 left blank gives the Hunt claws.

The Krai taught the order to work in (owner on Krai version 1: "Those are fighter jets, not spaceships. Think about how
we defined culture and story"): the culture first, then the Ship Design Bible's method, rooms first.

## What Changes

- **The language**: each line of the Hunt's culture turned into a part of a spaceship (design, "From the culture").
- **Rules for Hunt ships**, provisional: claws are the distinct feature; squat and broad; scute paneling; camouflage
  over dark gunmetal; a ship that can run dark (Stalk) and light up only when it strikes (Strike).
- **One frigate plan**, rooms first (`data/ships/ssarith/frigate_plan.json`, written by `tools/mockups/ssarith_plans.py`
  on the Krai plan layer), three decks, squat and broad.
- **Three directions on that one plan**, the bible's rule H2 (character added on top, never cut into a room):
  - **A Grasp**: four hooked claws at the broad prow, closed in front of the bow while stalking, spread open when it strikes.
  - **B Jaw**: a broad prow split into two jaws lined with fangs, the nacelles splayed low like a reptile's legs with
    talons at their tips.
  - **C Crouch**: a hump of armour scutes over a low dipped prow, the nacelles folded under the belly like forelegs, a
    sickle claw at each.
- **Mockup 12**, `docs/mockups/ssarith-ship-concepts.html` version 1: the three at true scale beside the Concord Petrel
  (the yardstick, from `docs/mockups/lib/concord-line.js`), Stalk and Strike, the deck plans, the 40 px silhouettes, the
  bible's checks measured on the page, the console palette beside the hull's (X4).
- **Survey section H** (H1-H8): the direction, and every culture and story call this design needs from the owner (U7).
- Not in this change: the other classes, the Concord-standard skin (detail maps, interior-mapped windows, hull floods
  with shadows), damage and breakup. Those follow once a direction is chosen, on the shared modules the Krai already use.

## Impact

- New: `openspec/changes/ssarith-ship-concepts/`, `data/ships/ssarith/frigate_plan.json`, `tools/mockups/ssarith_plans.py`,
  `docs/mockups/ssarith-ship-concepts.html`, `docs/artifacts/ssarith-ship-concepts/`, shots in
  `docs/screenshots/mockups/ssarith-ship-concepts/`.
- Changed: `tools/mockups/krai_plans.py` (its writer takes the classes and folder, and a room may name its own outer
  width, so the Hunt's plans use the same layer; the Krai plans are unchanged, `--check` passes), `mockup-work.md`, `docs/design/survey.html` (section H), the tracker.
- No game code.
