# Design: special-weapons

## Where the shape comes from

The reference manuals, read for how the systems worked, never for their names or text (CLAUDE.md section 1):

| Manual, printed page | What it shows | What we take |
|---|---|---|
| SFC2, 22-25 ("Heavy Weapons") | Each race carries heavy weapons of its own beside common beams; there are two classes, direct fire (hits or misses at once) and seeking (flies, and can be degraded or shot down on the way); each has an energy cost to arm, a cost to hold armed, and its own fall of damage with range; one empire's weapon is neither kind (a sphere round the firing ship) | Two specials a faction, each with a rule no other faction has; the direct fire and seeker split, kept in our rules (the chorus and the forge are direct fire, the quell is a seeker); a special that is neither (the blood charge) |
| SFC2, 25 ("Weapon Status") | A weapon can be stunned by enemy fire, unable to fire until the stun passes | The quell's stun, on mounts |
| SFC3, 12-13 ("Weapon Selection/Control") | Primary weapons charge fast and hit light, heavy weapons charge slowly and hit hard; which a ship carries depends on its empire and its loadout | Specials charge slowly (10-16 s against a lance's 3.5 s) and are fitted by loadout |
| SFC3, 28-29 (refit by hardpoint) | A ship's weapons are bought into hardpoint groups at a dock | The quell is a load any torpedo tube can take; the forge goes in the spinal cannon's hardpoint |

## From each faction's world view

The two factions as the console skins and the ship concepts wrote them (`openspec/changes/console-skins/design.md`,
`openspec/changes/krai-ship-concepts/design.md`):

| Faction | As written | Chorus lance / Forge driver (signature) | Quell torpedo / Blood charge (tactical) |
|---|---|---|---|
| Concord | "A federation of many species that decides by consent; explores, mediates, defends"; clean energy and light; saucers and slender nacelles | Consent made into a weapon: ships choose to join, and the beam is stronger for every voice in it. Fired from the deflector, the Concord's own clean light | The mediator's weapon: it ends a fight without killing, the moment to board, capture or talk (the boarding party, `boarding-party`) |
| Krai Warhost | "War is the only honest test; rank is won in blood"; lean predators, forged steel, edged everything; volcanic gunmetal, forge orange as light | The forge itself: a spike of their own white-hot metal down the spinal cannon, which leaves the enemy's hull burning like the Krai's own fissured plate | Face to face, edge first ("every direction is a weapon you charge with"): the bayonet every Krai hull already carries, driven home |

## The numbers (provisional, `data/battle/special_weapons.json`)

| | Chorus lance | Quell torpedo | Forge driver | Blood charge |
|---|---|---|---|---|
| Kind | Beam, precise | Torpedo (a seeker), by arc | Slug (direct fire), precise | Ram, by arc |
| Fits | Albatross-class cruiser (CA), on its deflector | Any torpedo tube, as a load | The spinal cannon (Krai FF, CA, DN) | Every Krai ship, from the helm |
| Range | 14 km | 12 km at 900 m/s | 10 km at 4,500 m/s | Contact |
| Charge, cost | 14 s, 8 E | 16 s, 6 E | 10 s, 7 E | 8 E from the battery, 30 s to ready again |
| Damage | 7 a second for 2.5 s (17.5), +35 % for each ship that joins, up to 3 (35.9) | None | 18, 40 % through the shield; then 1 a second for 6 s | 5 for every 100 m/s closing, by the rammer's battle length over 230 m |
| Else | A joiner must be within 8 km and spends its ready lance | The struck facing's shield to 0; every mount within 90 degrees of the facing stunned 8 s; point defence can shoot it down | Overload: 5 km, 14 s, 11 E, 30, 60 % through | Surge 5 s: 2.2 x top speed, 3 x thrust, 0.5 x turn, bow shield x 1.5; the bayonet ignores the struck facing's shield; 30 % back on the rammer's bow |

A Krai frigate at 340 m/s that surges from 1.25 km out closes at about 660 m/s: 33 to the hull, 9.9 back on its own bow
(the range shows this run). Against a fresh 32 side facing the chorus breaks through only with all three joined (35.9);
with two (29.8) it needs a facing already worn down. One cruiser alone cannot burn through a fresh shield, which is the
point: the Concord fight together.

## The rules, one copy

A hit through facing k with pierce share p: `p x damage` goes straight to the hull, the shield takes the rest until it is
spent, and what it cannot take goes to the hull too. The chorus's damage a second is `dmg_per_s x (1 + per_joiner_x x
joined)`. The ram's damage is `closing / 100 x dmg_per_100_m_s x length_m / mass_ref_m`. In the mockup these are the
page's `RULE` (the card's readouts call it, so a readout cannot disagree with a hit, CLAUDE.md 6.1); in the game they
become `Rules` in `src/Core/Sim/Rules.cs`, and the battle console mockup and the range then inline one module.

## On the console (to mock up next, in the battle console mockup)

- The weapons panel lists a special as a mount row of its own, with its charge pips and arming tabs; the chorus shows
  how many ships stand ready to join, the quell shows the load in its tube.
- The Krai helm gets a Charge key (the Krai skin's blade key), lit forge orange while the surge runs.
- Four new icon subjects on the icon guide (`docs/design/icon-guide.html`): a beam with strands (chorus), a lattice ball
  (quell), a spike (forge), a bayonet's point with a bow wave (charge), drawn by every skin.
- Wingmen: a behaviour "Join the chorus" (always, when ordered, never: survey X2), and the AI fires a quell before a
  boarding and charges when it can close faster than 500 m/s (provisional).

## Cost

Rules: a few multiplications a hit; the chorus checks up to three joiners a step. No new state beyond a burn timer and a
stun timer a ship and a surge timer a Krai ship. The look's cost is `weapon-fx`'s.

## Open questions

| # | Question | Provisional answer |
|---|---|---|
| X1 | Two specials a faction (a signature and a tactical one)? | Two |
| X2 | Does a wingman join the chorus by itself, or by a behaviour you set? | A behaviour, Always by default |
| X3 | Does the quell stun subsystems on the struck half as well as mounts? | Mounts only |
| X4 | Does damage control put out the forge's burn? | Yes: a repair crew on it halves what is left |
| X5 | Does a blood charge lock the two ships together for a boarding? | No |
| X8 | The names Chorus lance, Quell torpedo, Forge driver, Blood charge | Keep them |
