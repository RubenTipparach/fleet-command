# Proposal: crew-leveling

## Why

The owner, 2026-11-12: "I want to work on crew leveling system next: ensigns provide no bonus but gain xp overtime, and
can be traded for prestige so you can get more experienced crew, or just level up your crew member", "need crew skill
tree", then "skill tree, each level gets a skill point, the player can auto level officers or pick paths on the tree to go
down and specialize". It answers the officers change's open question O1 (whether officers gain experience).

Today an officer's skill is fixed: the campaign hires officers at five skill levels for credits
(`data/campaign/campaign.json` `officers`), the battle's roster gives each a rank and a skill from 0 to 4
(`data/battle/systems/reactor.json` `officers`), and nothing a captain does changes either.

## What Changes

- **Ranks are levels.** Ensign, Lieutenant, Lt. Commander, Commander, Captain. Each rank above ensign is one skill point,
  so an ensign has none and brings nothing to a battle.
- **Experience over time**: every officer of the crew gains experience from each battle (by its grade) and from each
  campaign turn served.
- **Promotion** spends the experience for the next rank and gives a skill point; a shortfall may be paid in prestige.
- **Transfer for prestige**: an officer leaves for their rank's worth plus their banked experience, in prestige.
- **Hiring costs prestige**, by rank, from a pool at each station that changes every turn (ensigns free).
- **A skill tree per station**: three branches of three skills, each skill one point, a skill open once the one above it in
  its branch is learned; every skill changes an ability a battle rule reads.
- **Auto level or pick**: an officer on auto level spends points along their station's own path as they are earned; off,
  the player picks skills and specialises. Retraining takes the points back for prestige.
- **Data**: `data/campaign/crew.json` holds every number (ranks, experience, prestige rates, the trees, the auto paths).
- **Mockup 39**, `docs/mockups/crew-leveling.html`: the campaign's crew screen with all of it, playable. Once approved it
  becomes mockup 1's Crew tab.
- Mockup only until approved for implementation.

After the Starfleet Command 3 manual (printed page 36, officers in the campaign; page 29, recruiting officers in skirmish
setup), in our own words and numbers.
