# Design: commander-rank

## Rank from career prestige

Prestige already rises and falls with each mission's grade (`rules.prestige`: crushing -5, defeat 0, draw 2, victory 5,
outstanding 10; story missions times `story_prestige_x`). A rank that fell after a bad mission would make the next missions
easier, which rewards losing, so rank is read from **career prestige**: the sum of every prestige gain, losses ignored. It is
a new field of the campaign save (`CampaignState.CareerPrestige`), added to wherever prestige is added.

| Rank | Title (provisional, Concord) | Career prestige | Strength weights Light / Moderate / Heavy | Enemy tier ceiling |
|---|---|---|---|---|
| 1 | Lieutenant Commander | 0 | 70 / 30 / 0 | 1 |
| 2 | Commander | 30 | 45 / 45 / 10 | 2 |
| 3 | Captain | 80 | 25 / 45 / 30 | 3 |
| 4 | Commodore | 160 | 10 / 40 / 50 | 4 |
| 5 | Rear Admiral | 300 | 0 / 35 / 65 | 5 |

At about 6 prestige a mission (a victory with the odd outstanding), rank 2 comes after 5 missions, rank 3 after about 13,
rank 4 after about 27, rank 5 after about 50.

All of it is data: `rules.ranks` in `data/campaign/campaign.json`, one entry per rank with `title`, `career_prestige`,
`strength_weights` (one per entry of `rules.strengths`) and `tier_max`. Checked on load: thresholds rising, weights
non-negative and not all zero, one weight per strength, `tier_max` 1 or more.

## Strength

`MakeOffer` rolls strength from the rank's weights instead of evenly (the campaign's own seeded stream, so a save replays).
The strength is fixed when the mission is offered: a mission taken before a promotion keeps its terms.

## Tier ceiling

Where a mission group names no `tier`, its hulls today alternate tier 1 and 2. They become `min(1 + i % 2, tier_max)`, and a
group that names a tier is capped at `tier_max` too, for general missions only (`story: true` missions keep their written
forces, which are balanced by hand). `HullOfTier` already falls back to the nearest tier a faction has.

## Reward

The reward rolled today (`reward` plus up to `reward_steps` steps of `reward_step_cr`) is multiplied by
`strength_reward_x` for the strength: Light 1, Moderate 1.25, Heavy 1.5 (`rules.strength_reward_x`). Prestige stays by grade.

## Where it shows

- **Top bar**: a Rank stat after Prestige, the title (trimmed to fit), its tip naming the career prestige and the next rank's
  threshold.
- **Debrief**: "Promoted to Commander" with the new title, when a mission's prestige crosses a threshold.
- **Briefing**: the strength already shows; its tip says the rank set it.

The same in mockup 1 (`docs/mockups/game-flow.html`): the Rank stat, a promotion line in the debrief, and a shot
`map_rank` (a rank 3 captain).

## Cost

One integer in the save, one table in data, two lines changed in offer rolling and group building. No new screen.

## Open questions

| # | Question | Recommendation (provisional until answered) |
|---|---|---|
| R1 | Rank from career prestige, or its own experience points? | Career prestige: one number the player already sees, and losses never demote |
| R2 | Five ranks with these thresholds? | Yes, tuned after a playtest sweep at each rank |
| R3 | Titles per faction (the Krai, Ssarith and Kelvane would not say Commodore) | Yes, each faction's own titles, from its written culture; the Concord's above until then |
| R4 | Should the difficulty also follow your fleet (its size and tiers), not just rank? | Not yet: rank only, so a player can tell why missions got harder |
| R5 | Should rank unlock bigger hulls at your shipyard? | Later, its own change |
| R6 | Heavy rewards 1.5 times Light's? | Yes |
