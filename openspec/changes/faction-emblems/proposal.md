# Proposal: faction-emblems

Mockup 9. Status: **Drafted** (2026-11-03). Page `docs/mockups/faction-emblems.html`; shapes in
`docs/mockups/lib/emblems.js`.

## Why

The owner asked, 2026-11-03, for a session dedicated to designing the emblems (faction crests and insignia) for every
faction in the game. Three places already need them and none has one that holds together:

- Each console skin has a `crest` hero piece (`assets/ui/<faction>/crest.png`, 96 x 96 px), drawn or rendered on its
  own, with nothing saying what it means or how it looks small.
- The ship bible (chapter 10, markings and liveries) puts "the faction's crest forward of the bridge, one per ship", and
  the Krai frigate (mockup 7) carries a placeholder crimson marking on each flank "until the Krai's emblem exists".
- The battle console shows contacts, targets and fleet rows where a faction must be told apart at a glance, beside the
  icons of the icon standard (`docs/design/icon-guide.html`, owner, 2026-11-03: "these icons need a similar standard").

What a people puts on its flag is a cultural decision, so it is the owner's (survey U7: "no cultural or story decision
without the owner"). This change draws one emblem per faction on the recommendations, shows each at every size it will
be seen, and asks the meanings in the survey (section E).

## What Changes

- **One emblem per faction**, ten: Concord, Kethran Hegemony, Vos Syndicate, Vhorr Brood, Aelith Conclave, Tarsine
  Remnant, Krai Warhost, Ombrai, Kelvane Institute, Ssarith Hunt. Each is a field (the outer shape) and a device (the
  picture inside), from the faction's world view in `openspec/changes/console-skins/design.md`.
- **Eight rules for an emblem** (E1 to E8 in the design), the emblem's counterpart to the icon standard.
- **One source, three renderings**: the console crest (the skin's material), the mark (flat, 40 px and 20 px) and the
  hull marking (paint), all drawn from the same shapes.
- **Sizes**: 96 x 96 px crest, 40 px in lists, 20 px in tags and rows, and on a hull at least 6 % of the ship's length.
- **A mockup** showing every emblem at every size and painted on a stand-in hull flank in 3D at true scale.
- **Survey section E** (E1 to E12): what each emblem means, whether sub-marks exist, and where on a hull it goes.

Not in this change: re-rendering each skin's `crest` piece from the emblem (a task here, done after approval for build
in each skin's generator), any faction's livery or ship (each faction's ship change), and the Krai frigate's marking
(the Krai ships thread replaces its placeholder once the Krai emblem is approved).

## Impact

- New capability `faction-emblems`.
- `docs/mockups/lib/emblems.js`: the shapes, shared by any page that shows a faction.
- Later: `assets/ui/<faction>/crest.png` re-rendered from the shapes; the ship bible's chapter 10 if E12 changes where
  the emblem goes; the Krai frigate's flank marking.
