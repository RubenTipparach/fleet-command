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
| Ranks | Ensign, Lieutenant, Lt. Commander, Commander, Captain; skill points = rank index (0 to 4) |
| Experience to promote (spent on promotion) | 300, 600, 1000, 1500 |
| Experience per battle, every crew officer | 100 x grade: crushing 0.25, defeat 0.5, draw 0.75, victory 1, outstanding 1.5 |
| Experience per campaign turn | 10 |
| Experience to prestige | 50 experience = 1 prestige, both ways |
| Transfer out | rank worth (0, 6, 14, 26, 42) + banked experience / 50, in prestige |
| Hire from the pool | 0, 10, 24, 44, 70 prestige by rank; each station offers 2 ensigns and one each of Lieutenant to Commander, new every turn |
| Retrain | 10 prestige: every point back, auto level off |

A victory is 5 prestige (`campaign.json` rules), so an ensign is promoted after three victories, and a Captain is about
twelve more; an ensign with 300 experience banked transfers out for 6 prestige, about one victory.

An officer hired above ensign arrives with their points unspent, so the player shapes them (survey CL4).

## The trees

Six stations (the campaign's), three branches each, three tiers each; tier 3 is a capstone. A Captain has four points, so
an officer finishes one branch and starts a second: a specialist. Every node names the ability key its rule reads; the
battle's existing abilities (`reactor.json` `officers.ability`: het, drift, hit, pd, scan, sweep_km, jam, repair_rate,
ambush, aboard, pad_rate, wing_hit) carry over with the same per-step values, and the new keys need rules of their own
(tasks 2.x).

| Station | Branch 1 | Branch 2 | Branch 3 |
|---|---|---|---|
| Helm | Piloting: turn 4 %, accel 5 %, top speed 3 % (new) | Emergency turns: HET 4 %, 4 %, a failed HET never breaks down (new) | Evasion: enemies hit 1 %, 1 %, 2 % less (new) |
| Tactical | Targeting: hit 2 %, 2 %, 3 % | Point defence: intercept 5 %, 5 %, two missiles a burst (new) | Gunnery: charge 3 %, 3 %, 6 % faster (new) |
| Engineering | Power: drift -15 %, -15 %, +1 E reactor (new) | Repair: crews 20 %, 20 %, one more crew (new) | Shields: recharge 5 %, 5 %, 10 % (new) |
| Science | Sensors: scan 25 %, 25 %, shows the weakest system (new) | Sweep: 10, 10, 20 km | Cloak watch: 10 %, 10 %, 15 % (new) |
| Comms | Counter-jamming: 0.25, 0.25, 0.5 of ECM | Diplomacy: standing +1, +1, +2 (new) | Coordination: wingmen hit 1.5 %, 1.5 %, 3 % |
| Security | Boarding: aboard 4 %, 4 %, 6 % | Ship's defence: ambush 10 %, 10 %, 15 % | Transporters: pads 10 %, 10 %, 20 % |

Auto level spends along each station's `auto` path (four skills, shown on the tree as Auto 1 to 4), then the first open
skill. Picking a skill by hand turns auto level off for that officer.

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
| CL6 | Five ranks, Ensign to Captain | Yes |
| CL7 | Six campaign stations covering the battle's nine | Yes, as mapped above |
| CL8 | Wounds and deaths in battle (the reference game's) | Not yet |
