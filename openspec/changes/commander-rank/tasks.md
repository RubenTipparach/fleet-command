# Tasks: commander-rank

Design stage: game code waits for the owner's approval of the mockup (CLAUDE.md section 4).

- [x] 0.1 Proposal, design, spec delta; open questions R1-R6 in the survey
- [x] 0.2 Mockup 1 (`docs/mockups/game-flow.html`): the Rank stat, the promotion in the debrief, the `map_rank` shot
- [ ] 0.3 Owner approves the mockup for build, then for implementation
- [ ] 1.1 `rules.ranks`, `rules.strength_reward_x` in `data/campaign/campaign.json`, read and checked (CampaignData)
- [ ] 1.2 `CampaignState.CareerPrestige`, added wherever prestige is gained; `Campaign.Rank`
- [ ] 1.3 `MakeOffer` rolls strength by the rank's weights and multiplies the reward
- [ ] 1.4 `MissionRun` caps general missions' enemy tiers at `tier_max`
- [ ] 1.5 Top bar Rank stat, debrief promotion, briefing tip
- [ ] 1.6 Tests: the scenarios in the spec delta, the save round trip, data refusals
- [ ] 1.7 Playtest: the mission sweep at rank 1 and at rank 5 from all four sides
