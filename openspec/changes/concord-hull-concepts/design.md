# Design: concord-hull-concepts

## The method, from the owner's two ship projects

| Rule | From | Here |
|---|---|---|
| Inside out: zones first, hull wrapped round them, then fitted | Star Crew, federation-ship-concepts design ("Design sequence") | Every concept is a list of zones (rooms and spaces with a kind, a size in metres and a deck count); every hull piece is the convex wrap of its zones plus 0.7 m clearance, chamfered; the page measures that every zone corner lies inside its hull |
| Windows only on real rooms | Star Crew, ship-exteriors ("real apertures ... from the same portal coordinates as the furnished interiors") | Windows are placed by casting from inside crew, commons, medical and command zones to the hull, one row per 3.4 m deck; no painted windows |
| Three alternatives, three stages | Star Crew, both concept changes | Halo, Lantern spine, Light sail; stages Zones, Hull, Fitted |
| One shape language per faction, from how it thinks | Adventure CLAUDE.md 13.2 | The Concord decides by consent and houses every kind of body: shapes that gather (a ring), that house many kinds side by side (lanterns), or that are made of light (sails) |
| The player's faction has negative space | Adventure 13.2b ("You can see through a federation ship") | Each direction has an opening you see stars through: the ring's centre, the lattice, the sails |
| Countable classes | Adventure 13.2b ("A class grows by adding nacelles in PAIRS, never by fattening them") | Rings, lantern pairs or sail pairs; the generator grows a class only by count |
| Measured, not intended | Adventure 13.2b ("Lengths above are MEASURED off the committed .glb") | Lengths, beams, clearances and containment are measured from the built geometry |
| Read at 40 px | Adventure 13.2 | Top and side silhouettes rendered at 40 px on the page |

Take the shape, not the text: the method is borrowed; the ships, names and words are Fleet Command's own.

## Zones

Deck height 3.4 m. Zone kinds and their colours on the page: command (Signal blue), crew (sand), commons (lilac),
medical (pale green), machinery (amber), weapons (red), hangar (steel blue), corridors (slate). Engines, pylons, booms,
lattice and sails are uninhabited structure (Star Crew's Grabon rule: "thin wing tips and external engines are
uninhabited equipment").

## The three directions (frigate)

| | A Halo | B Lantern spine | C Light sail |
|---|---|---|---|
| Tell (what reads at 40 px) | A ring with stars through it | A see-through lattice strung with lit pods | Two glowing sails beside a slim keel |
| Command | Bridge raised on the hub, at the centre of the ring | Bridge block on top of the bow | Bridge on top of the keel's bow |
| Crew | Twelve ring segments: quarters, a council chamber, mess, medbay | Lantern pairs, each pair its own shape (sphere, hexagonal drum, cylinder, octahedron) | Two decks of quarters in the keel |
| Machinery | Reactor in the hub; engineering, hangar and drive on the aft spine | Reactor inside the lattice aft, drive bells and radiator fins | Reactor and engineering aft in the keel |
| Engines | Two pods hung outboard of the ring on short pylons | Three bells at the end of the lattice | At the aft ends of the booms |
| Class counted by | Rings: corvette 1 small, frigate 1, cruiser 2 in tandem | Lantern pairs: 1, 2, 3 | Sail pairs: 1, 1 (longer), 2 |

The page's Facts panel gives the measured length, beam, height, decks, zones, windows and containment for each.
Recommended: **A, Halo**: its tell is the strongest at 40 px (a circle with a hole is unlike any wedge or blade),
the ring is the Concord's idea of itself (many species round one table), and classes count cleanly by rings.

## Cost

A hull piece is a convex wrap (tens of triangles each); a frigate is 2,000-6,000 triangles and 40-90 draws on the page,
under the 12,000-triangle concept ceiling Star Crew uses per ship. Windows are two instanced meshes. In the game each
chosen hull is a Blender generator (`blender-hard-surface`) from the same zone data.

## Open questions

| Id | Question | Recommendation (built in the mockup) |
|---|---|---|
| A11 | Which direction for the Concord: Halo, Lantern spine, Light sail, or a mix? | Halo |
| A12 | Concord sizes: keep mockup 5's 118-312 m, or the concept sizes (corvette 60 m, frigate 120 m, cruiser 220 m)? | The concept sizes |
