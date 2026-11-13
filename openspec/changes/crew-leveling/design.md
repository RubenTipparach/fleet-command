# Design: crew-leveling

## What the reference did (cited)

The Starfleet Command 3 manual, printed page 36 (single-player campaign, Officers): officers are recruited and traded;
they gain experience through the campaign, and their worth and skills increase with it; they can be swapped at any time by
spending prestige on more experienced officers from a pool. Each station's officer has three skills of its own (for
example the helm's piloting, evasive manoeuvres and emergency turns). Page 29 (skirmish setup, Select Officers): the crew
of six stations with each officer's worth in prestige, and a pool to recruit from, rated from Skilled up. Page 24
(combat): officers can be wounded or killed in battle.

We take the shape: experience over a campaign, prestige as the price of crew, three skills to a station. Our ranks, numbers,
trees and names are our own.

## The rules

| Rule | Number (`data/campaign/crew.json`) |
|---|---|
| Levels | 1 to 10; skill points = level - 1 (0 to 9) |
| Ranks, by level | Ensign 1, Lieutenant 2, Lt. Commander 4, Commander 6, Captain 8 |
| Experience to the next level (spent on promotion) | 300, 400, 500, 650, 800, 1000, 1200, 1450, 1700 |
| Experience per battle, every crew officer | 100 x grade: crushing 0.25, defeat 0.5, draw 0.75, victory 1, outstanding 1.5 |
| Experience per campaign turn | 10 |
| Experience to prestige | 50 experience = 1 prestige, both ways |
| Transfer out | 5 prestige a level above the first + banked experience / 50 |
| Hire from the pool | 8 prestige a level above the first; each station offers levels 1, 1, 3, 5 and 7, new every turn |
| Retrain | 10 prestige: every point back, auto level off |

A victory is 5 prestige and 100 experience, so an ensign reaches level 2 after three victories, Captain (level 8) after
about 38 and level 10 after about 80, turns helping; a level 1 ensign with 300 experience banked transfers out for 6
prestige, about one victory.

An officer hired above level 1 arrives with their points unspent, so the player shapes them (survey CL4).

## The trees (version 3)

The owner, of version 2's pyramid: "it doesnt have to be like an actual pyramid... just a skill tree. mockup a few
different versions and make it look nice. use art drawing skills to make skill icons, and show tooltips on hover overs".
Each of the six stations has one tree of fourteen skills, the same shape for every station, its three lanes the station's
three themes (the helm: piloting, emergency turns, evasion):

| Skill | Lane, depth | Opens when |
|---|---|---|
| Three roots | each lane, 1 | Always |
| A step in each lane | each lane, 2 | The root before it |
| A third step in the outer lanes | left and right, 3 | The step before it |
| Two crossovers | between two lanes, 3 | Either centre-lane step or its outer neighbour (any) |
| The keystone | centre, 4 | Both crossovers (all) |
| Two lane tips | left and right, 4 | Their lane's third step and the crossover beside them (all) |
| The capstone | centre, 5 | The keystone |

A path can change lanes at a crossover; the keystone and the tips are the skills with two prerequisites. The cheapest way to
the capstone is up the centre (root, step, both crossovers, keystone, capstone): six points of a level 10 officer's nine,
leaving three for one outer lane or two lane steps and a tip's way; fourteen skills are never all learned.

Every layout reads each skill's lane and depth (`data/campaign/crew.json`) and places it its own way; the lines between
skills come from the prerequisites: dashed for either, solid for every one, lit once the earlier skill is learned.

| Style | Look |
|---|---|
| Branches | Grows down the screen from the three roots across the top to the capstone at the foot, curved limbs, the themes named over the roots, the lanes 210 px apart (owner, 2026-11-13: "branches should be vertiacally inverted, starting from the top with the 3, squish them so theres not so much space between columns") |
| Lanes | Left to right in three bands like the console's, every skill a pill key with its icon and name |

Version 3 also drew a Constellation (the skills as stars) and a Radial fan; the owner kept Branches and Lanes ("branches
are good, lanes are good").

**Icons** (`docs/mockups/lib/skill-icons.js`, the Concord's): one picture for each ability (twenty-five), drawn on the
icon guide's 48 px grid with a 3 px line, solid for the thing and lines for what it gives off, one ink, read at 20 px; a
skill shows its first ability's. Each skin draws the same subject list its own way (task 2.7).

**Tooltips**: hovering or focusing a skill shows its station and role, each change with its icon, what it needs (either or
both, by name), and whether it is learned, open or locked; a click or tap pins it, and a pinned open skill carries Learn,
so nothing needs hover alone (CLAUDE.md 7).

## The two officer systems

The campaign has six stations and the battle nine (helm, power, tactical, point defence, science, comms, repair,
transport, fleet). The trees are the campaign's six, each covering the battle stations it holds: Engineering is power and
repair, Tactical is tactical and point defence, Security is transport, Comms is comms and fleet. In battle the flagship's
slots take the campaign crew (officers task 2.5); every other ship keeps the roster's officers.

## Open questions

| # | Question | Provisional answer |
|---|---|---|
| CL1 | Hiring officers: prestige or credits? | Prestige, as you asked; credits stay for ships and parts |
| CL2 | Where experience comes from | Each battle by its grade, and a little each turn |
| CL3 | Trading an officer for prestige: they leave, or only their experience is cashed? | They leave (transfer out) |
| CL4 | Officers hired above ensign: points unspent, or already placed? | Unspent, for the player to place or auto level |
| CL5 | Retraining | 10 prestige, every point back |
| CL6 | Ten levels, ranks every two (Ensign to Captain) | Yes |
| CL9 | The tree: crossovers open from either lane, the keystone and the tips need two | Yes, as drawn |
| CL10 | Branches or Lanes | Branches, provisionally |
| CL7 | Six campaign stations covering the battle's nine | Yes, as mapped above |
| CL8 | Wounds and deaths in battle (the reference game's) | Not yet |
