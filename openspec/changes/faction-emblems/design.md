# Design: faction-emblems

Mockup 9. Every faction's emblem, its rules, its sizes, and where it goes. Our own designs (CLAUDE.md section 1: take
the shape, not the art); no shape is traced from another game, a film, or a real nation's or faith's symbol.

## Sources

- Each faction's world view, body, technology, ships and palette: `openspec/changes/console-skins/design.md` (the
  three languages, "Five more races", the Kelvane Institute and the Ssarith Hunt).
- The icon standard: `docs/design/icon-guide.html` (owner, 2026-11-03). An emblem is not an icon, but it sits beside
  icons at 20 px, so it follows the same discipline: silhouette first, read at 20 px, colour for state.
- Where an emblem goes on a hull: the ship bible, chapter 10 (markings and liveries: "the faction's crest forward of
  the bridge, one per ship"), and chapter 11 (the exterior complements the console, a guide, not a hard rule; owner,
  2026-11-03: "I said compliment, so not a hard rule"). The bible is `docs/design/ship-bible.html` on the Concord
  ships thread's branch.
- The Krai frigate's placeholder marking on each flank (mockup 7, `docs/mockups/krai-ship-concepts.html`, on the
  Concord ships thread's branch). This change does not edit that page.
- Each skin's `crest` hero piece today: `assets/ui/<faction>/crest.png` (96 x 96 px slot), Concord's in
  `docs/mockups/lib/concord.js`, the Vos's in `docs/mockups/console-skins.html`.

The reference manuals (SFC2, SFC3) show the reference games' empire symbols only as art on their screens; nothing of
them is taken here.

## Superseded in part (owner, 2026-11-03)

The owner rejected the first Concord render ("I'm not a fan of the circle design here", "its too simple and easy to
make") and set a method: each faction's emblem is sketched flat in SVG with every choice labelled, picked by the owner,
and only then sculpted in 3D. That method, sixteen rules compiled from the owner's notes, the reference badges and the
STARFLEET Visual Standards Manual 2026, and the Concord's four sketches live on the Emblem Design Guide
(`docs/design/emblem-guide.html`). The ten flat emblems below are placeholders until each faction's sketch is picked.

## The Concord's badge in 3D (2026-11-04)

B-E23, the rounded peak, built as a badge 100 mm across from the guide's own shapes. Heights (mm), so no two faces
share a plane: field 0.3 to 1.0, star floor 0.5 to 1.9 (a pocket 0.9 mm deep in a hole through the saucer), nacelles
and tail 0.5 to 2.4, saucer 0.6 to 2.8, frame 0 to 3.6. Frame polished platinum; ship iridescent pearl enamel (a thin
film and an angle-dependent pastel shift); field a holographic night (nebula and stars at three depths that move with
the viewing angle, under a colour-shifting sheen). Materials are Material Maker graphs with albedo, normal, ORM and a
specular map derived from the graph's own roughness and occlusion. The emblem light rig was scaled for a badge this
size: at 3 W the key lamp clipped white enamel to flat white (measured, 255 everywhere); at 0.55 W the pearl reads
about 239 of 255 and every bevel shows.

Since then (owner, 2026-11-04): the outline is B-E25, the pointed peak (B-E23's sketch with an arrow foot and a pointed
top), the star is satin blue, and the ship carries a small layer of detail ("a double bevel on the saucer", "segment
marks on the legs"). Heights now (mm): saucer 0.6 to 2.3 with an upper tier inset 1.6 mm rising 1.9 to 2.9, so its
rim steps twice; each leg body 0.5 to 1.9 under four segments 0.5 to 2.4, so the joins read as grooves 0.5 mm deep;
tail 0.5 to 2.4. The upper tier and the segments come from the guide's own shapes (`lensTop`, `legSegments` in
`window.EMBLEM_PARTS`), so the sketch, the render and the three.js badge stay one source.

## On a Concord hull (2026-11-04)

Mockup 11 paints B-E25 on the Concord line's Petrel frigate (88 m) in the hull's colour by a planar projection in its
shader (no decal plane). The owner set the marking (2026-11-04: "too many logos on the hull there", "1/2 size on the left
side", "only one logo"): **one emblem, on the port side, at half of E7's size**: 3 % of the ship's length (2.6 m on the
Petrel). This replaces E7's 6 % and E12's three marks for Concord hulls. Measured placement: aft of the pylon root the
swept pylon hides the side from view, so the emblem sits on the bow's flat side just ahead of the last windows (z 33 m).
The paint is the solid version (pearl on a night field; owner: "on the ship hull I like the solid shape more"). At battle distance on desktop it measures about 19 px tall (phones set aside for now, owner: "dont care about phone right
now").

**The Concord's emblem is B-E27, the smooth keyhole** (owner, 2026-11-04: "Keyhole but smoother", then "Yea ok. I'll
accept that"): B-E15's keyhole drawn as one smooth curve, its head a flattened oval over the saucer, measured as one
closed outline with the ship 5.3 units clear. On the hull it is the solid version with a cyan star (`#2fd0f0`; owner:
"Solid color variant with a cyan colored star in the middle"). The 3D badge (mockup 10) is rebuilt on B-E27 (owner: "Also render the 3d version too"),
its star satin cyan: the near-white pearl enamel's maps tinted `#2fd0f0`, since Signal blue cannot be tinted up to cyan.

**On the hull, in the livery** (owner, 2026-11-04: "Have it use the same color scheme as the liveries and integrate it into
one of the stripes", then "See how this works? Although don't rotate our logo", with a reference of an emblem heading a
pair of pinstripes): the emblem's field is the livery's stripe colour and its frame the seam colour, both read from the
Concord line's livery table, with a pearl ship and the cyan star. It stands upright on the port bow and heads two
pinstripes (broad stripe blue, 0.28 m, over thin seam navy, 0.12 m) that run aft along the flank and end in the forward
livery band. Taken from the reference: only the idea of an emblem heading pinstripes; no shape or art.

## The rules for an emblem

| Rule | Says | Why |
|---|---|---|
| **E1 One mark per people** | One emblem per faction. It names who they are, never a job, so it is never a station or system icon | The icon standard's rule 2, one subject one picture, kept apart from it |
| **E2 A field and a device** | An outer shape (the field) and one picture inside (the device), two colours at most, plus cuts through to the ground | Two colours paint as a hull marking and stay legible at 40 px |
| **E3 Reads in one colour** | Every device sits in a window cut 5 units wider than itself on a 100 unit grid (1 px at 20 px, 2 px at 40 px); every rim left is at least 5 units | A stencil, a single-colour state tag and a 20 px mark all still read |
| **E4 No two outlines alike** | Each faction has its own outer shape class (table below) | A captain tells factions apart by outline alone, as no two skins share a button shape (console skins rule 1) |
| **E5 From how the people think** | Shape and colour from the faction's world view, body and ships, as the skin's buttons and icons are (console skins rules 2 and 3) | The emblem, the console and the ship read as one people |
| **E6 Three renderings, one source** | Crest, mark and hull marking all drawn from `docs/mockups/lib/emblems.js` (later `assets/emblems/<faction>.svg`) | CLAUDE.md section 6.1: one implementation; nobody redraws an emblem |
| **E7 Sized to be read** | At least 20 px wherever it is on screen; on a hull at least 6 % of the ship's length | Measured below |
| **E8 Our own** | No shape taken from another game, a film, or a real state's or faith's symbol | CLAUDE.md sections 1 and 10 |

## The ten emblems (provisional: survey E2-E11)

Grid 100 x 100 units, centre 50, 50. "Field" and "device" colours are the mark's (from the skin's palette); the hull
colours are stand-ins until each faction's livery is designed with its ships.

| Faction | Outline | Field | Device | Cuts | Meaning (recommended) | Mark colours | Crest today |
|---|---|---|---|---|---|---|---|
| Concord | Ring | A ring, radii 38-46 | Five worlds (radius 7 at 24 from the centre) and an open centre ring | none | Five founding worlds held as equals in one ring, round an open centre any people may join | Signal `#2f62e0`, Sky `#3fa9f0` | Five worlds round an open circle, crossed by an orbit: the emblem keeps it and drops the orbit, which blurs at 20 px |
| Kethran Hegemony | Heater shield | A shield with chamfered top corners | The prow (a lozenge) between two blades | none | The empire's prow leads, its great houses flank it | Steel `#8a9096`, Signal red `#d9483b` | A prow between two blade chevrons on a shield: kept |
| Vos Syndicate | Bitten coin | A coin, radius 44 | The crew's spiral (stroke 6) | A bite at the upper right; three claw rakes | A stolen coin with a bite out of it, the crew's spiral struck on it, raked by three claws | Brass `#d6a043`, Verdigris `#3fb7a3` | A coin with a spiral, raked by three claws: kept, with the bite added so its outline is not Concord's ring |
| Vhorr Brood | Egg in ribs | Two pairs of ribs (strokes 7 and 6) | The egg (19 x 27) | none | The brood's egg cradled in its ribs: one body, always growing | Bone `#e2d6bb`, Bile `#a6e34a` | A glowing egg in chitin horns: kept |
| Aelith Conclave | Drop | A gold drop, pointed below, as a band (inner 80 %) | A triangle of light, point down | A crystal through the triangle | Knowledge (the light) kept safe in something that lasts (the gold) | Gold `#c9a646`, Holo `#6ff3ff` | A gold drop round a hologram: kept |
| Tarsine Remnant | Stepped throne | Three steps, 88 wide | The first star, eight points | An arched gate in the base | The stepped throne under the first star: they were first, and remember it | Gold `#a88a46`, Light `#d6c76a` | A stepped ziggurat on a rust disc: kept, without the disc so its outline is its own |
| Krai Warhost | Triangle | A triangle, apex up | The bayonet: a serrated blade, hooked point down | Two fangs up from the base | The bayonet driven into the war host's triangle between two fangs: rank won in blood | Blood `#b3121b`, Edge `#c9d0d8` | A triangle with a red blade and two fangs: kept; the blade is now the frigate's alien bayonet (mockup 7) |
| Ombrai | Crescent and eye | A crescent, horns up | An eye | A slit pupil | An eye held in a crescent: they watch from the dark, and are seldom seen | Lilac `#c58cff`, Pink `#ff4fb0` | An eye in a crescent cradle: kept |
| Kelvane Institute | Gauge arch | A gauge's arch, radii 34-46 | A tuning fork in the gauge, the wave it sounds below | Five ticks through the arch | Truth measured, never assumed | Brass `#b0915a`, Phosphor `#4dff88` | A tuning fork over a sine wave in a ring: kept, the ring opened into a gauge so its outline is not a ring |
| Ssarith Hunt | Target brackets | Four bracket corners (stroke 8) | A fang, point down | Three kill notches across the fang | A worthy quarry's fang in the hunter's brackets, notched once for each kill | Phosphor `#6dff8a`, Bone `#d8cfb4` | A fang and track marks on an octagonal camouflage plate: kept, the plate opened into brackets (their icons' closing brackets) |

Every emblem keeps the idea its skin's crest already had, so the owner's reviewed consoles stay recognisable. Where a
crest's outline matched another's (three were circles: Concord, the Vos and the Kelvane; the Tarsine sat on a disc), the
emblem changes the outline to meet E4.

## Three renderings, one source

| Rendering | Size | How | Where it is used |
|---|---|---|---|
| **Crest** | 96 x 96 px slot (the kit's `crest` hero piece) | The skin's own method on the shapes: flat vector for Concord, inked for the Vos, the Blender skins modelled from the outline and rendered by their generators (`tools/blender/ui_render.py`). The page shows a quick stand-in (a lit gradient, a dark rim, a glowing device) beside today's crest | The console's left column |
| **Mark** | 40 px and 20 px | Flat: the field and device colours, or one colour (the state colour at 20 px, as the icons take it: hostile red, friendly, neutral) | Contacts, fleet list, hover cards and the campaign map's standees (40 px); a target's tag, Power and Systems rows, the radar's legend (20 px) |
| **Hull marking** | At least 6 % of ship length tall | Two-colour hull paint baked into the albedo (no decal plane, so no z-fighting), floodlit (ship bible chapter 9); a faction whose ships grow or set light (Vhorr, Aelith, Ombrai, provisional) has the device in the emission map too | Each flank and forward of the bridge (E12) |

## Sizes, measured

- **Hull marking, E7.** In the chase view on a phone (844 px landscape), a ship framed to about 40 % of the width:
  an 88 m frigate is 338 px, 3.84 px a metre. A 20 px mark needs 5.2 m, so the rule is 6 % of ship length
  (5.3 m on the frigate, 3.1 m on a 51 m corvette, 11 m on a 184 m battleship), each still 20 px when that ship is framed
  the same way. On desktop (1920 px) the same frigate's mark is 46 px. The page's "battle distance" view puts a camera
  where the frigate fills 40 % of the view.
- **E3 in pixels.** 5 units is 1 px at 20 px and 2 px at 40 px. The device windows and rims on the page meet 5 units by
  construction (the window is drawn as the device grown by 5); rims are set by hand in the shapes and checked by eye in
  the 20 px rows. A measuring check is task 2.2.

## Placement on a hull

The ship bible's chapter 10 puts one crest forward of the bridge, on the dorsal plating; the Krai frigate carries one on
each flank. The chase view sees the dorsal; a target is often seen side on. Recommended (E12, provisional): the dorsal
crest and one on each flank, all at the E7 size, on plain plating clear of windows and grilles (chapter 10's rule).
The ship bible changes only if the owner accepts it.

## Open questions

| Id | Question | Recommendation (built, provisional) |
|---|---|---|
| E1 | One emblem per faction, or a faction emblem plus sub-marks (a Vos clan, a Krai war band, a Kethran house)? | One emblem per faction now; sub-marks later, for the factions whose culture has them |
| E2 | Concord: what does the mark say? | Five founding worlds in one ring, round an open centre |
| E3 | Kethran Hegemony | The prow between two blades: the empire leads, its houses flank it |
| E4 | Vos Syndicate | A bitten coin with the crew's spiral and three claw rakes |
| E5 | Vhorr Brood | The egg in its ribs |
| E6 | Aelith Conclave | A triangle of light in a drop of gold |
| E7 | Tarsine Remnant | The stepped throne under the first star |
| E8 | Krai Warhost | The bayonet in the triangle, between two fangs |
| E9 | Ombrai | An eye in a crescent, painted on their hulls |
| E10 | Kelvane Institute | A tuning fork in a gauge over its wave |
| E11 | Ssarith Hunt | A fang in brackets, notched for each kill |
| E12 | Where on a hull? | Decided for the Concord (owner, 2026-11-04): one emblem on the port side, 3 % of ship length |
