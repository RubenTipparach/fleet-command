# Tasks: vos-ship-concepts

## 1. The board

- [x] 1.1 Proposal, design and spec delta
- [x] 1.2 `vsec`, the Syndicate's section layout, checked as it is built
- [x] 1.3 Twelve blockouts from five drafted traditions, every skin closed
- [x] 1.4 Mockup 37, `docs/mockups/vos-ship-concepts.html`; shots looked at; DOM fit passes
- [x] 1.5 Survey section Y (Y1-Y5)
- [x] 1.6 The owner's note on version 1 (2026-11-10): not sculpted shapes; modified freighters with scrapped and stolen parts

## 1b. Version 2: conversions

- [x] 1b.1 The parts bin: the game's glbs inlined, parts taken by piece or by a clean cut, never scaled
- [x] 1b.2 Vos-made joining pieces (collar, strut, prow, tub, deck) grown on `hull-grow.js`
- [x] 1b.3 Eight conversions, each from a freighter and a raider's job; shots looked at; DOM fit passes
- [x] 1b.4 Survey section Y rewritten
- [x] 1b.5 The owner's note on version 2 (2026-11-10): 5 and 6 kept; "create some unique battle freighter designs"

## 1c. Version 3: battle freighters

- [x] 1c.1 `base()`: a freighter's drive block and spine cut to length; guns, beams and single containers
- [x] 1c.2 Six battle freighters (9 to 14) in the Syndicate's own layouts, beside 5 and 6; shots looked at; DOM fit passes
- [x] 1c.3 Survey section Y updated
- [x] 1c.4 The owner's picks (2026-11-10): 9, 10, 11 and "15" (which is survey Y6), with 5 and 6

## 1d. Version 4

- [x] 1d.1 No pointed bows: every welded bow ends in a flat face; the Ram a hammer head
- [x] 1d.2 5 and 6 first on the board and in the list, marked kept
- [x] 1d.3 The owner's correction (2026-11-10): the kept ships are the Saucer runner and the Boarder, not 5 and 6

## 1e. Version 5: six specialists

- [x] 1e.1 3 Boarder and 8 Saucer runner restored; 5, 6, 13 and 14 withdrawn
- [x] 1e.2 Every ship cut to about cruiser size (122 to 185 m) and given one trade: boarding, mines, tractor beams, rock
  breaking, fighters, passing as a freighter
- [x] 1e.3 Survey section Y: Y5 decided; Y8 to Y10 added
- [ ] 1e.4 The owner's answers to Y7 to Y11

## 1f. Version 6: fighters

- [x] 1f.1 Three fighters of the Syndicate's own (21 Cutter, 22 Twin boom, 23 Stack), about 16 m, welded round salvaged engines
- [x] 1f.2 The Saucer runner carries six Cutters in place of the Institute fighters
- [x] 1f.3 Survey Y11

## 1g. Version 7: the fighters, detailed

- [x] 1g.1 The fighters' jobs recorded (D14): Cutter the standard fighter, Twin boom the shuttle stand-in and bomber; survey Y11 decided
- [x] 1g.2 The Breakers' recipe (D15) in `data/ships/vos/hull_detail.json`
- [x] 1g.3 Both fighters exported, unwrapped and baked onto their own UVs (2048 px), shown on the page in Painted and PBR
- [x] 1g.4 Survey Y12 (patched evenly or unevenly side to side)
- [ ] 1g.5 The owner's look at the detailed fighters, and Y12's answer

## 1h. Version 8: rooms, and the Saucer runner detailed

- [x] 1h.1 A room plan for each of the six specialists (`data/ships/vos/<ship>_plan.json`, D16), fitted inside their parts
- [x] 1h.2 The page's room check (`MOCKUP_ROOMS`) and the Rooms view with deck plans; all six pass
- [x] 1h.3 The Saucer runner exported, unwrapped and baked onto its own UVs at 2048 px, its taken parts in their makers' looks (D17)
- [x] 1h.3b The owner's note (2026-11-11): seams too harsh and crossing; narrower, lighter, square seams, rebaked
- [ ] 1h.4 The owner's look at the detailed specialist (now the Deck runner, 1i), before the other five are detailed
- [ ] 1h.5 If wanted: each taken part plated in its maker's own way, not only painted
- [ ] 1h.6 The other five specialists detailed the same way

## 1i. Version 9: freighter hulls only

- [x] 1i.1 The owner's culture (2026-11-11): a militia organised from various traders; no warship parts on any Vos ship (D18)
- [x] 1i.2 Saucer runner rebuilt as the Deck runner (two Hunt freighters joined); Boarder's prow a welded grapple fork; Ram's nacelles welded drive pods
- [x] 1i.3 The Deck runner's and Boarder's rooms redrawn and refitted; all six pass the room check
- [ ] 1i.4 The Deck runner baked with the softer, square seams and shown
- [ ] 1i.5 Survey Y15 (whose colours a ship wears)

## 1j. No face without detail

- [x] 1j.1 The rule in the Ship Design Bible (T9), the ship-detailing skill and CLAUDE.md; the references' lessons as T10 to T12
- [x] 1j.2 Machinery on bare faces; ordered machinery panels (`machinery`) on the bow face and inset along the hull
- [x] 1j.3 Masts as geometry from the bake's manifest (`antenna`, `baked-ship.js` masts)
- [ ] 1j.4 Colour fields and pale edge lines in the painter (T12)
- [ ] 1j.5 The game builds the masts from the manifest when the Vos ships are implemented

## 2. After the picks

- [x] 2.1 Each ship's rooms (done as 1h.1: fitted into the parts, since a conversion's hull comes first)
- [ ] 2.2 Detail, the ship's own unwrap and bake (`ship-detailing`): the fighters and the Saucer runner done; the other five in 1h.6
- [ ] 2.3 Rock breaking as its own change (asteroids that split, a blast that hurts every ship near), after
  `campaign-space` 2.3
- [ ] 2.4 The disguise as its own change (how a Vos ship reads as a freighter until it acts)
- [ ] 2.5 Once approved for implementation: export, six classes of about cruiser size with a weaker hull, and the
  placeholder Vos line (mockup 36) retired
