# Design: ssarith-ship-concepts

## Version 7: working detail (owner, 2026-11-04: "ships arent very complex")

A ship reads as built of real parts when its jobs show on its skin, so the detail is the parts the plan and the bible
already call for, placed from the data, plus each tradition's own:

| | Every direction | A Old Lodge | B Trackers | C Field Hull |
|---|---|---|---|---|
| Parts | Two gun houses over the forward battery and one under the keel (the plan's forward battery and armoury); bay doors where the keel deck's boarding and hunting craft bays meet the hull, each a dark recess in a frame, split down the middle; attitude thruster blocks on the upper edges fore and aft; a forward emitter at the bow (the bible's deflector) | Rafters over the hall's roof every 4 m and posts down its walls; a riveted belt strake down each flank at the middle deck's floor; 22 plates of newer steel patched over old ones (the refits); a lodge mast with a red beacon at its head; two bells astern, the refit's newer and smaller | Clamps and saddles on every tracking tube, a three-lens sensor head at the cradle's bow; cooling fins on the radiators; louvred hoods over the side vents; lower gun houses aft | Field hoops round the field hull, each with a glowing seam; conduits down the neck; a dish over the bridge; three bells on the frigate and one on the field hull |

The bells are the shared thruster (`docs/mockups/lib/ship-skin.js`, inlined), eight-sided to match the Hunt's section, their
heat phosphor green-white, banked while waiting and full while hunting. Triangles per frigate: A 6,228, B 5,934, C 5,114
(from 1,550 to 2,070), still a phone's budget for a mid-detail ship (survey A1, T3); the triangle budgets per class are set
by mockup 3 (look dev). Every check still passes.

## Version 6: plating laid on each face (owner, 2026-11-04: "oof now theres uv issues")

Version 5's one deep chamfer (3.29 m, at exactly 45 degrees) showed a fault in how the plating was laid: it was
projected along each face's main axis, and on a 45 degree face neither axis leads, so the plates there were stretched
about 1.4 times and flipped between neighbouring faces. The plating is now laid on each face itself, along the ship and
across the face in metres measured on its own surface, so a plate keeps its size on the chamfers, the roof and the flat
sides, and the rows line up along the length; only the bow and stern faces are projected face-on. One shader, so the
fix covers A's plates, B's louvres and C's cast sections alike. No geometry changed.

## Version 5: a fair hull (owner, 2026-11-04)

On version 4's side view, marked: "I'd like to see these contours smoothed out top down and sides stay flat", "oh I guess
the sdies would be smoothed too", "but without adding more polygons", "remove the wobble"; and on its top view, marked:
"yea if this could be smoothed thatd be nice".

Version 4's chamfers still changed along the ship (a lower envelope of each station's allowance), so the edge between the
flat side and the 45 degree faces wobbled in profile, and the plan's outline broke at corners. `fairHull` replaces it,
on the frigate and the field hull alike, and only ever adds material, so every room stays at least 1.05 m inside:

1. **Smooth outlines.** The beam, top and bottom are each raised (lowered, for the bottom) to the smallest curve above the
   eased stations that a five-station running mean leaves unchanged, so the plan and the profile curve instead of
   breaking at corners.
2. **One chamfer.** Every station takes the same chamfer (3.29 m on the frigate), the deepest its rooms allow at the
   tightest station, so the flat side band runs straight from bow to stern.
3. The bow stations (the frigate's broad prow, the field hull's taper) are added after the fairing, so it cannot square
   them off.

No polygons are added: the same stations and the same eight-sided section, only the corners move. Triangles per ship
change by 6 (A 1,828, B 2,068, C 1,550), three more person-sized windows now that the flat faces are longer (14 instead of
11).

## Version 4 (owner, 2026-11-04, on version 3)

"sillhoutte seems a bit rough along the spine. I would prefer something smooth. No go for C, A and B are good options I
will think about that more. Add new C that replaces nacelles with a secondary hull instead, adding some assuymetry or
lower or upper secondary hull".

- **Smooth edges, every direction.** The 45 degree faces had jumped between 0.6 m and 3.6 m from one station to the next,
  so every edge of the hull was notched. Each station's chamfer is now worked out from the rooms against the eased hull
  (as deep as the rooms allow, the fitter's own rule), then the deepest chamfer that changes by at most 0.25 m per metre
  along the ship is taken (`fairChamfers`), so the edges run smooth and no chamfer comes closer to a room than its own
  station allows; H1 still passes (every room at least 1.05 m inside).
- **A Old Lodge and B Trackers stay** as they are, for the owner to weigh.
- **C Course is withdrawn** ("No go for C").
- **C Field Hull, new.** No nacelles: the field generators they carry sit in a secondary hull of their own, rooms first
  (`data/ships/ssarith/frigate_field_hull_plan.json`, 17 rooms on two decks, crew 8, from `tools/mockups/ssarith_plans.py`),
  fitted the same way and joined to the frigate by a neck a corridor's width plus clearance (the bible's chapter 4). Its
  field grille runs down each flank and its emitter is at its bow, the nacelles' jobs. Three layouts, as the owner listed
  them: **below and to port** (asymmetric, offset 6 m), **below** and **above**, each with 3.2 m of daylight to the
  frigate, measured. Plating: large smooth cast sections with a deep seam every 8 m, dark above and pale below, the way
  field kit is painted ("camouflage"). The tradition is ours, provisional: yards that build the field drive as a hull
  apart, to shield it, change it or cut it loose after a long hunt.
- Stereotype test (C5) for C: "A broad armoured ship with no engine pods: a second, smaller hull carries its field drive,
  joined to it by a short neck, slung below it and to one side; smooth cast plating, dark above and pale below."

## Version 3: three traditions inside the Hunt (owner, 2026-11-04: "now update the damn ships!", "Ssarith ships", "not anyother ships thoo lol, SSarith needs work")

Built on survey H1's recommendation (a blank answer accepts it, and the owner asked for the ships): several traditions
inside the Hunt, drafted for the owner to change or replace, none from the species' body (Ship Design Bible C1-C5).
Each choice names its source: a line of the culture as written (the console-skins design's Ssarith Hunt table,
approved as written in U7) or, where it is ours, says so. The rooms, the fitted hull and its broad prow ("squat armoured
hunting ships with broad prows") are shared; the traditions differ in parts, plating, colour and what moves.

| | A Old Lodge | B Trackers | C Course |
|---|---|---|---|
| The tradition (ours, provisional) | The oldest: ships handed down and refitted for generations | Patience: the hunt is won by waiting and watching | A younger school that disagrees: the worthy hunt is the open chase |
| Plating (X3) | Plates of every age side by side, riveted, a few bronze repairs, worn field camouflage ("worn gunmetal, camouflage") | Horizontal louvres in 6 m bays, dark and mottled ("dark and mottled") | Clean 4 m by 2.5 m panels in pale paint, the hunt party's amber band (ours: it does not hide) |
| Distinct features | The top deck under a pitched roof (ours: as the lodges roof their halls); one original nacelle and one later, longer refit; two grapple emitters in the prow over the plan's grapple projector room ("the kill earned": the quarry held close) | A row of tracking tubes along the spine ("tracking tubes"); radiators folded down the flanks while it waits, holding its heat in, swung out level when it hunts ("built to wait unseen"); the drive vented sideways; nacelles tucked close under it | A rack of four small hunting craft along its back, launched when it hunts (the plan's hunting craft bay); a long drive section with three bells for the pursuit; long nacelles level on swept struts |
| Waiting to hunting ("the stalk patient") | Lights up; the grapple lenses lit kill red | The radiators swing out; the tubes and radiators glow | The craft lift off the rack and fan out |
| Stereotype test (C5) | A broad armoured ship whose top deck sits under a pitched roof, its plates patched over generations, two engines that do not match, two grapple emitters in its blunt prow | A low, dark ship with louvred plating, glowing glass tubes along its back, radiators that swing out from its flanks, engines tucked beneath | A pale ship banded in amber, four small craft racked along its back, a long three-bell drive, long engines on swept struts |

No recommendation among the three: which traditions stand, and what they are called, is the owner's (survey H2). Kill
red (the console's "the strike, given once") is lit only while hunting, and only on the Old Lodge's grapple lenses. The
plan's "Dens" are renamed Quarters (a word for animals' homes, so it is out under C1).

## Version 1 is withdrawn (owner, 2026-11-04)

"part of me loves that these are space aligators, but at last, you are over stereotyping an alien species which is
extremely insensitive. We've stated multiple times before, the look of an alien species is not their UI, not their
spaceship. Cultural ideologies are far more deep and complex than what someone's skin or features are. And that no
culture in the world follows a single line of thinking when it comes to desigining UI, space ships, logos, dressing etc,
and none of those reflect the way they look."

What went wrong: version 1 read the species' body straight into the ship (claws because they have clawed hands, scutes
because they have scales, a sprawl and a crouch because they are reptiles) and let one line of the culture (the hunt)
shape the whole line. Both now break the Ship Design Bible's chapter 13 (rules C1 to C5) and CLAUDE.md section 1. The
sections below are kept as the record of version 1 only; none of their readings, rules or directions stand.

What carries over: the method (rooms first, the shared fitter, the plan layer's tapering rooms, the page's checks), which
is species-neutral. What comes next waits for the owner: the Hunt's culture is too thin to design from (C4), so the
survey asks how it is to be set before any ship is redrawn (H1).


## Sources

| Source | What it gives the ships |
|---|---|
| The Hunt's culture, `openspec/changes/console-skins/design.md` (the Ssarith Hunt table), approved as written (survey U7, 2026-11-01) | World view, bodies, technology, "Squat armoured hunting ships with broad prows, dark and mottled, built to wait unseen" |
| Owner, 2026-11-02 (ships thread, recorded in `mockup-work.md`, mockup 5): "gorn emphasizes claw like features, since they enjoy hunting" | Claws are the Hunt's distinct feature |
| Survey A8, left blank, so its recommendation stands | "Ssarith Hunt: claws" |
| The Ship Design Bible, `docs/design/ship-bible.html` | The method (rooms, hull, parts, skin, damage) and its universal rules (M1, P1, H1-H3, B1-B4, W1-W5, L1-L4, X1-X4) |
| The Krai line, mockup 7 (`krai-ship-concepts`) | How a faction is drawn from its culture; the plan layer (`tools/mockups/krai_plans.py`); the shared fitter (`docs/mockups/lib/ship-plan.js`) |
| The Hunt console (mockup 4) and its emblem draft (E11) | Palette, the scute texture on its hero pieces, backlight only, the fang and its kill notches |

The reference games are not used here: no page of either manual describes a race's ship designs, and section 1 of
CLAUDE.md applies (the shape of an idea, never another game's ships, names or art).

## From the culture

Each line as written, and what it becomes in a ship. The world view is the owner's (U7); every reading below it is ours
and provisional until survey section H is answered.

| As written | In the ship |
|---|---|
| "The quarry must be worthy" | A hunter goes out alone or in a pair, never a swarm: a few heavy, well-armoured ships rather than many light ones (H5) |
| "The stalk patient" | A ship built to wait: a hull that holds still, dark and mottled, with its lights shuttered (Stalk) |
| "The kill earned and counted" | Kills are counted on the ship: a notch cut in bone white into a claw for each worthy kill, the emblem's notches said on the hull (H4) |
| "Big-boned reptiles with heavy clawed hands" | Heavy bones: a squat, broad hull, thick armour, short and strong parts; the claws are the ship's distinct feature (A8) |
| "Slow and silent until they strike" | Two states: Stalk (claws folded, lights shuttered, the drive banked) and Strike (claws open, kill red at the claws and guns, the drive lit) |
| "A hunter's field kit: worn gunmetal, camouflage, tracking tubes" | Dark worn gunmetal under a disruptive camouflage of canopy and moss; green phosphor behind the windows, the tracking tubes said a second way |
| "Grown over by the jungle it hides in" | Inside only: the rooms are warm and green-lit, and a few hold plants (H6). Nothing grows on a hull in space |
| "Squat armoured hunting ships with broad prows" | A broad flat prow that stays wide to its face, never a point; the opposite of the Krai's edge-first blade |
| "Built to wait unseen" | A matte, low-albedo hull; floods that are hooded and graze the plating rather than throw light into space (H3) |

## The rules, provisional

1. **Claws are the feature.** Every Hunt ship carries hooked claws: at the prow (A), at the feet of its nacelles (B) or
   under the belly (C). Claws are faceted horns that taper to a hooked point, laterally flattened and keeled like a
   reptile's claw, never a straight spike (the Krai's) or a blade (the Krai's bayonet).
2. **Squat and broad.** Beam about twice the height, length under four beams. A Hunt frigate is shorter and wider than
   the Concord's or the Krai's of its rank.
3. **A broad prow.** The bow keeps at least 0.6 of the hull's beam to its face; it is a shovel or a jaw, never a point.
4. **Scute paneling** (X3's unique paneling): the hull plating is laid as rows of keeled oval scutes, like a crocodile's
   back, overlapping aft so they shed like scales; a ridge of large osteoderm plates runs down the spine.
5. **Camouflage is the field colour.** Dark gunmetal under large disruptive blotches of canopy green and moss, with a
   dark umber; bone only for kill notches and the emblem; kill red only as light, and only in Strike.
6. **Stalk and Strike.** A Hunt ship has two looks. Stalk: claws folded, windows dim green, floods hooded and low, no
   strobes. Strike: claws open, kill red at the claw roots and the guns, the drive lit. The bible's lights stay (each
   with its job), dimmer in Stalk, so a player can still read the ship (H3).

## The frigate's rooms

`data/ships/ssarith/frigate_plan.json`, written by `tools/mockups/ssarith_plans.py` on the Krai plan layer (its `lay`,
`cycle` and writer, now taking the classes and folder as arguments: one implementation of the room layout; a room may
now name its own outer half width, so a deck can taper). Three decks 3.5 m apart (floors 3.5, 0 and -3.5 m), so the hull
is low; the middle deck is widest and the top and keel decks narrower, so the section is a wide low lozenge; and each
room aft is a little narrower than the one ahead of it, so the hull is broadest at its shoulders and tapers aft like an
animal toward its tail:

| Deck | Half width | Rooms, bow to stern |
|---|---|---|
| A (top, narrow) | 5.0 m, to 3.8 m aft | Bridge; hunt master and tactical; tracking room and sensors; officers; a trophy hall (H6) and comms |
| B (widest) | 9.5 m, to 6.8 m aft and 5.8 m at engineering | The forward battery across the whole prow (this makes it broad); dens, mess, medbay, a hunters' ready room, life support; armoury, shields, damage control, stores; engineering |
| C (keel) | 6.5 m, to 4.8 m aft and 4.4 m at the reactor | Grapple projectors across the prow (the claws' machinery); magazines; the boarding bay and the hunting craft bay; fabricators; the reactor |

Crew 40. Rooms are functional; the three whose names come from the culture (trophy hall, hunters' ready room, dens) are
provisional, and what the Hunt calls any room is the owner's (H6, U7).

The hull is fitted round them by the shared fitter (`FCShipPlan.fit`, section `blade`: chamfers as deep as the rooms
allow), then the Hunt's character is added on top of the fit (H2), never cut into it:

- **No steps.** The beam may change by at most 0.16 m per metre along the ship and the top and bottom by 0.32, so where a
  deck ends the hull slopes over it like a heavy animal's back (a dilation: stations only grow, and a chamfer grows only
  as far as the faces it joins moved out).
- **A shovel prow.** The two nose stations keep 0.9 and 0.7 of the front station's beam and drop their top.

Every room is measured 1.05 m inside (H1) on the page. The fitted hull is 74 m long, 21.1 m in the beam and 11.9 m tall.

## The three directions (frigate)

All three are built on the one fitted hull; only their parts differ, so the page compares features, not plans.

| | A Grasp | B Jaw | C Crouch |
|---|---|---|---|
| The idea | A clawed hand reaching for the quarry | A crocodile's ambush at the water's edge | A big cat or a raptor crouched to spring |
| Claws | Four hooked claws at the prow's corners, 17 m and 21 m, curving forward and in | Three 5.5 m talons at the bow of each nacelle | One 12 m sickle claw under each foreleg nacelle |
| Prow | The shovel prow, the claws' roots at its corners | Split into an upper and a lower jaw, fangs along both, a gun in the throat | Dipped low, under the hump |
| Nacelles | One pair, flat and low under the flanks on short struts | One pair, splayed wide and low on angled struts, like legs | One pair, folded under the belly on struts angled down and forward |
| Spine | A low ridge of osteoderm plates | A double row of scutes, like a crocodile's back | A raised hump of large scutes |
| Stalk to Strike | The claws, closed in front of the bow, spread open | The lower jaw drops open; the talons straighten | The sickle claws, tucked down, raise forward |

**A is recommended.** It says "claws" from every side and at 40 px, the claws are the ship's own weapon (the grapple
projectors are under them), and it grows by claws without changing shape (a larger class adds a pair; H5). B reads as a
jaw first and a claw second; C's claws are under the hull, where the chase view rarely sees them.

## Lights and windows

- **Windows**: the bible's person-sized pane (1.1 m by 0.6 m), one row a deck, only on lived-in rooms (W1, W2), lit a
  dim phosphor green from the rooms. Interior mapping comes with the Concord-standard skin, after a direction is chosen.
- **Floods**: the bible's grazing hull floods, but hooded: each lamp sits under a lip of armour and throws its pool
  along the plating, so the hull reads in shadow while the lamp itself is hidden from ahead. In Stalk they burn at a
  third. Their light is a pale green-white, so the camouflage reads as green and brown, not grey.
- **Position lights** port red, starboard green, white aft (the bible), small and dim; no white strobes in Stalk (H3).
- **Kill red** only as light and only in Strike: a lamp at each claw's root, the gun throat on B.
- **Drive**: a wide flat slot across the stern, banked dark in Stalk, pale phosphor-white in Strike.

## Exterior against the console (X1, X3, X4)

| | Console (mockup 4) | Hull |
|---|---|---|
| Material | Pillowed olive gunmetal, every edge rolled; hero pieces pressed with hide scales | Matte dark gunmetal laid in keeled scutes, worn lighter on the keels |
| Colour | Gunmetal `#3a4044`, canopy `#2f4a2a`, moss `#5b6340`, phosphor `#6dff8a`, bone `#d8cfb4`, kill red `#c2412b` | Field `#33382f`, canopy blotch `#2d4228`, moss blotch `#4f5638`, umber `#3a3026`, bone notches `#d8cfb4`, green-white light `#c8ffd6`, kill red light `#ff4a2e` |
| Light | Backlight only | Hooded floods, phosphor windows, red only in Strike |

The console and the hull share gunmetal, canopy, moss and bone (allowed: X1 is a guide). What sets the hull apart is
its features (the claws), its paneling (the scutes) and its camouflage, which the console only hints at.

## Measured on the page

P1 and H1 on the plan; the prow's width as a fraction of the beam (rule 3); the beam to height and length to beam
(rule 2); nacelle daylight (3 m at least, the bible); triangles; the length, beam and height of each direction against
the Petrel's 88 m.

### Found on version 1

- **At 40 px the claws nearly vanish.** In the silhouettes (`docs/screenshots/mockups/ssarith-ship-concepts/ssarith-ship-concepts-silhouettes.png`)
  A's claws show only as a thin outline at the bow, B's jaw is lost in the hull and C's top view is a plain lozenge; what
  reads at that size is the hull and the nacelles. So whichever direction is chosen, its claws need more mass at the
  root (about twice the 1.35 m radius) before rule 1 is met at contact size.
- **With no sun the hooded floods light only pools on the flanks**; the bow and claws stay dark. That suits Stalk; Strike
  needs a lamp on the claws themselves, which comes with the flood spotlights and shadows of task 2.1.
- The fitted minimum was a long box at first (version 1's own first renders): the plan's decks shared one width each, so
  the plan layer now lets a room name its own outer width, and the hull eases its steps.

## Cost

About 5,100 to 6,200 triangles a frigate with version 7's detail (measured on the page), inside a phone budget (survey A1, T3). The claws are the most
expensive part (about 300 triangles each).

## Not done yet

The other classes, the Concord-standard skin (detail and normal tiles in the shared `lib/ship-skin.js`, interior-mapped
windows, flood spotlights with shadows), damage and breakup on `lib/ship-fx.js` and `lib/ship-break.js`, and the hull
marking (waits on E11 and E12).

## Open questions

Version 1's H1-H8 are withdrawn with it.

| # | Question | Recommendation (provisional) |
|---|---|---|
| H1 | How is the Hunt's culture to be set before its ships are redrawn? | Accepted as recommended (owner, 2026-11-04: "now update the damn ships!"): we draft several traditions for the owner to pick and change |
| H2 | A Old Lodge or B Trackers (the owner: "A and B are good options I will think about that more"), or C Field Hull? | None recommended: the owner's to choose, rename or replace |
| H3 | Where does C's field hull ride: below and to port, below, or above? | Below and to port: the asymmetry the owner asked for first, and it leaves the keel's grapple line clear |
