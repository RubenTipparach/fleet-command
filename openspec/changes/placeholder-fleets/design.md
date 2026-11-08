# Design: placeholder-fleets

## D1. What a placeholder is

A placeholder gives a faction something to fly and a mark to show before its ships are designed. It is not a design.
Each faction's ship mockup and emblem still follow their own process (the Ship Design Bible, the Emblem Design Guide),
and they replace the placeholders. So the placeholders follow three rules:

- They are cheap: one generator and one data file.
- They are still read from the faction's culture, never from the species' body (CLAUDE.md section 1).
- They keep the hull rule: one grown structure, no stretched primitives (CLAUDE.md section 10).

## D2. The classes (our discretion, from the owner's brief)

Every faction gets the same four-step ladder: frigate, destroyer, cruiser and a capital ship. The capital is whatever
its culture would build at the top: a battleship, carrier, dreadnought or flagship. The lengths sit on the scale of the
approved lines, where the Concord runs from 52 m to 188 m:

- frigates are 50-72 m;
- destroyers are 78-110 m;
- cruisers are 118-170 m;
- capitals are 168-300 m.

The Tarsine Remnant is the largest at every step, because it does not shrink what it built in the first age.

The armament is a role sketch for the class card, not tuning. The battle numbers come with each faction's combat change.

| Faction | Tradition the shape comes from | Frigate | Destroyer | Cruiser | Capital |
|---|---|---|---|---|---|
| The Axiom | The tilers (diamond sections at golden-ratio stations) and the branchers (fins iterated one level deeper per class) | Lemma 58 m | Corollary 86 m | Theorem 128 m | Proof 190 m (battleship) |
| The Exponent | The assimilators (a modular near-box hull, taken hulls kept as pods) and the conduit-keepers (a lumen line) | Tender 64 m | Salvor 96 m | Hulk 150 m | Reef 230 m (carrier) |
| Red Tithe | Boarding: a ram keel, boarding tubes, the flagship carrying the Hall where debts are posted | Cutter 54 m | Reaver 82 m | Bloodship 124 m | Tithe Hall 176 m (flagship) |
| Vos Syndicate | Clans that take ships whole: boarding forks, salvaged outrigger hulls | Skiff 50 m | Corsair 78 m | Privateer 118 m | Galleon 168 m |
| Kethran Hegemony | Rank and the foundry: faceted plate, a blade prow, a rank crest that grows with standing | Lancer 60 m | Warden 92 m | Bastion 140 m | Throne 210 m (dreadnought) |
| Tarsine Remnant | Unchanged since the first age: one monolith plan at every size, buttresses, lamps in arched windows | Votary 72 m | Custodian 110 m | Monument 170 m | Dominion 300 m (dreadnought) |
| Aelith Conclave | Shells round a core of light, one more shell for each age of service | Seeker 56 m | Envoy 84 m | Archive 130 m | Sanctum 196 m |
| Ombrai | The watchers: flat dark hulls, petals closed forward, a lit eye under the bow | Veil 54 m | Shade 82 m | Vigil 126 m | Eclipse 186 m |
| Vhorr Brood | Grown, not built: hulls grown in chambers, carapace bands, feeding tendrils | Drone 52 m | Ravager 80 m | Broodship 130 m | Hive 220 m (carrier) |

## D3. The generator

`docs/mockups/lib/placeholder-hulls.js` reads one faction and one class and returns a mesh.

- **The body** is a superellipse section with exponent `n`, from 1 (a diamond) to 5 (near a box). It is swept from stern
  to bow through seven-station profiles: `plan` (the half-beam), `rise` (the half-height) and `centre`.
  - A faceted faction (the Axiom, the Kethran) interpolates linearly with flat normals.
  - Every other faction uses Catmull-Rom with smooth normals.
- **A limb** is a loft along a three-point Bezier path, rooted inside the body.
  - Its section is sized in fractions of the half-beam, and its frame follows the path.
  - `mirror` copies it to port.
  - `iterate` repeats it smaller and further aft. This is how the Axiom's rule goes one level deeper per class.
- **The stern** is a drive recess: a dark lip to an inner ring set back, with a glowing floor.
- **Colour**: stripes over a range along the length and a range of angles round the section give trim, accent and glow
  faces. Limbs carry the body's trim, not its glow.
- **Cost**: 3,000-16,000 triangles and 4-5 draw calls a line. That is within the approved ships' budgets.

## D4. The emblems

Each emblem is a 100 x 100 vector roundel drawn from one tradition:

- **The Axiom**: a Koch snowflake two levels deep, converging on a gold point.
- **The Exponent**: seven unlike plates in a ring round a branching conduit.
- **Red Tithe**: crossed boarding blades over the drop of the tithe.
- **Vos Syndicate**: the crews' spiral cut into a coin.
- **Kethran Hegemony**: a forged prow between blade chevrons of rank.
- **Tarsine Remnant**: the first age's arched tablet with its lamp.
- **Aelith Conclave**: light held between two shells.
- **Ombrai**: rings round a watching point under a veil.
- **Vhorr Brood**: six chambers round a lit heart.

These are SVG, so Godot can import them for menus as they stand.

## D5. The page

Mockup 36 has:
- a tab for each faction;
- the line in a row at true scale, with a 100 m bar and a 10 m grid;
- line, top and side views;
- a card with the emblem, the faction's tradition and the class table.

It is checked at desktop, phone landscape and phone portrait sizes.

## Open questions

| # | Question | Provisional answer |
|---|---|---|
| PF1 | Are four classes per faction enough as placeholders, and is the ladder (frigate to capital) right for each? | Yes, four each; the capital follows the culture |
| PF2 | Should the placeholders go into the game (glb export and classes) now, or wait for each faction's ship mockup? | Into the game once approved for implementation, so the new factions can fight |
| PF3 | Do the placeholder emblems stand until each faction goes through the Emblem Design Guide? | Yes |
