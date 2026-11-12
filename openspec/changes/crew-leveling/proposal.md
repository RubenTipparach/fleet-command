# Proposal: crew-leveling

## Why

The owner, 2026-11-12: "I want to work on crew leveling system next: ensigns provide no bonus but gain xp overtime, and
can be traded for prestige so you can get more experienced crew, or just level up your crew member", "need crew skill
tree", then "skill tree, each level gets a skill point, the player can auto level officers or pick paths on the tree to go
down and specialize", and of version 1: "I wish crew leveling was kind of like a pyramid tree or something where some
nodes can cross over, and some nodes have 2 or more prereqs", and of version 2: "it doesnt have to be like an actual
pyramid... just a skill tree. mockup a few different versions and make it look nice. use art drawing skills to make skill
icons, and show tooltips on hover overs". It answers the officers change's open question O1 (whether officers gain experience).

Today an officer's skill is fixed: the campaign hires officers at five skill levels for credits
(`data/campaign/campaign.json` `officers`), the battle's roster gives each a rank and a skill from 0 to 4
(`data/battle/systems/reactor.json` `officers`), and nothing a captain does changes either.

## What Changes

- **Levels 1 to 10**, a skill point for each level after the first, so a level 1 ensign has none and brings nothing to a
  battle. Ranks come with levels: Ensign 1, Lieutenant 2, Lt. Commander 4, Commander 6, Captain 8.
- **Experience over time**: every officer of the crew gains experience from each battle (by its grade) and from each
  campaign turn served.
- **Promotion** spends the experience for the next level and gives a skill point; a shortfall may be paid in prestige.
- **Transfer for prestige**: an officer leaves for their level's worth plus their banked experience, in prestige.
- **Hiring costs prestige**, by level, from a pool at each station that changes every turn (ensigns free).
- **A skill tree per station**: fourteen skills in three lanes (the station's three themes). A crossover between two lanes
  opens from either; the keystone needs both crossovers; each lane's tip needs its lane and the crossover beside it; the
  capstone sits on the keystone. Every skill changes abilities a battle rule reads, and has a drawn icon of what it
  changes (`docs/mockups/lib/skill-icons.js`, to the icon standard) and a tooltip with what it does and needs.
- **Four styles of the same tree** on the mockup to choose from: Branches, Constellation, Radial and Lanes.
- **Auto level or pick**: an officer on auto level spends points up the centre of their station's tree to its capstone as they are earned; off,
  the player picks skills and specialises. Retraining takes the points back for prestige.
- **Data**: `data/campaign/crew.json` holds every number (ranks, experience, prestige rates, the trees, the auto paths).
- **Mockup 39**, `docs/mockups/crew-leveling.html`: the campaign's crew screen with all of it, playable. Once approved it
  becomes mockup 1's Crew tab.
- Mockup only until approved for implementation.

After the Starfleet Command 3 manual (printed page 36, officers in the campaign; page 29, recruiting officers in skirmish
setup), in our own words and numbers.
