# Design: ssarith-ship-concepts

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

About 3,000 to 4,000 triangles a frigate before detail (measured on the page), inside a phone budget (survey A1, T3). The claws are the most
expensive part (about 300 triangles each).

## Not done yet

The other classes, the Concord-standard skin (detail and normal tiles in the shared `lib/ship-skin.js`, interior-mapped
windows, flood spotlights with shadows), damage and breakup on `lib/ship-fx.js` and `lib/ship-break.js`, and the hull
marking (waits on E11 and E12).

## Open questions

Survey section H. A blank answer accepts the recommendation; until then each is provisional here.

| # | Question | Recommendation (provisional) |
|---|---|---|
| H1 | Which direction: A Grasp, B Jaw or C Crouch? | A Grasp |
| H2 | Stalk and Strike: do the claws (or jaw) move between the two? | Yes: they move only when the ship strikes, and fold back when it breaks off |
| H3 | Lights: how dark does a stalking Hunt ship run? | Dim but readable: every bible light kept, hooded floods at a third, no strobes; full only in Strike |
| H4 | Kill notches: are kills counted on the ship? | A bone notch cut into a claw for each worthy kill, the emblem's notches said on the hull |
| H5 | How many classes, and how does a class grow? | Three: a stalker (frigate), a hunter (cruiser) and a great hunter (super capital); a class grows by a pair of claws and a deck, never by a swarm of small ships |
| H6 | The rooms the culture adds: a trophy hall, a hunters' ready room, dens for quarters, plants in the warm rooms? | Yes to all four, as placeholders, the names yours |
| H7 | Do Hunt ships cloak? The culture says "built to wait unseen"; the cloaking change gives cloaks to the Concord and the Kethran only | No cloak: they run dark instead (Stalk), seen but hard to lock at range. A gameplay rule, so it would be its own change |
| H8 | The scute paneling and the camouflage: the right paneling and colour for the Hunt? | Yes, as built |
