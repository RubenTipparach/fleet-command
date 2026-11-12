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

## The pyramids (version 2)

The owner, of version 1's three separate branches: "I wish crew leveling was kind of like a pyramid tree or something where
some nodes can cross over, and some nodes have 2 or more prereqs". Each of the six stations has fifteen skills on five
tiers, laid out as a pyramid with the apex on top:

| Tier | Skills | Opens when |
|---|---|---|
| 1 (base) | 5 | Always |
| 2 | 4 | Either of the two below it (any) |
| 3 | 3 | The sides: either of the two below; the centre keystone: both below it (all) |
| 4 | 2 | Either of the two below it |
| 5 (apex) | 1 | Both below it |

Because most skills open from either neighbour, a path starts on one side and crosses to the other. The cheapest way to
the apex is up the centre: one base skill feeds both centre tier-2 skills, which open the keystone, which opens both tier-4
skills, then the apex: seven points. A level 10 officer has nine, so the apex leaves two to spend elsewhere, and a pyramid of
fifteen is never full: every officer has chosen.

The base runs across the station's three themes left to right (for the helm: piloting, emergency turns, evasion), so
crossing over mixes them; the keystone and the apex give two or three abilities at once. Every skill's changes are its
`fx` (ability keys the battle rules read, as version 1 listed, with the same new keys); `data/campaign/crew.json` has every
name and number.

Auto level spends up the centre to the apex (the station's `auto`, Auto 1 to 7 on the tree), then the first open skill.
Picking a skill by hand turns auto level off for that officer.

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
| CL9 | The pyramid: either-below for most skills, both-below for the keystone and the apex | Yes, as drawn |
| CL7 | Six campaign stations covering the battle's nine | Yes, as mapped above |
| CL8 | Wounds and deaths in battle (the reference game's) | Not yet |
