# Proposal: freighter-missions

## Why

The owner, 2026-11-07, approving mockup 24 (the freighters) for implementation: "implement in game, also implment on campaign
map, and implement freighter raid/escort missions, no mision flag needed. Just whenever you are on the same tile as freighter,
you get a mission option". Then: "capturing enemy freighter: bring it to a station to cash out".

Until now the campaign's freighters were a placeholder miniature, drawn only for your own faction, and the convoy missions
fought tier 1 warships labelled "freighters". Nothing happened when your fleet met one.

## What changes

- **Each faction flies its own freighter.** The campaign's factions name their freighter hull (`factions.<id>.freighter`): the
  Concord's Gauge carrier, the Krai's Spear train, the Hunt's Tally carrier, each fighting as its battle class (`fr`, `krai_fr`,
  `hunt_fr`; the freighters change, task 3.1). A faction with none (the Vos) keeps the neutral stand-in.
- **On the map, the real ship.** Every freighter is drawn as her faction's approved freighter with her containers, your own
  always and anyone's where your ships see; routes stay your own and your allies' only.
- **Meet a freighter, get a mission.** With the fleet in a freighter's sector, the sector panel lists her with an Escort key
  (your own or an ally's) or a Raid key (anyone else's). The key opens the encounter's briefing, launched at once. An
  encounter is a mission file of a new kind, `encounter`: never offered at random, gone at the end of the turn.
- **Take her, cash her out.** A raided freighter beaten to her capture line strikes (or is boarded) and joins the fleet with
  her cargo. She sails with you but stays out of your battles. At a port that deals with you (never her own faction's) Cash out
  sells her cargo for its haul and hands her over. A freighter destroyed or taken is off her route for six turns.
- **The convoy missions field freighters.** Convoy defense, Convoy ambush and Distress fight real freighters, which keep
  their course and never close to fight.

## Impact

- `data/campaign/campaign.json` (freighter hulls, `factions.<id>.freighter`, `rules.freighter_refit_turns`),
  `data/missions/escort.json` and `raid.json` (new), `defend.json`, `ambush.json`, `distress.json` (`hull: "freighter"`).
- `src/Core/Campaign/` (Campaign, CampaignData, CampaignState, MissionRun, MissionScript), `src/Game/Flow/` (CampaignMap,
  MapHud, MapPieces, GameFlow, MapScreen), `docs/design/mission-files.md`, the mission editor's kind field.
- Mockup 1 (game-flow) still draws its own freighter placeholder and has no encounters: sync tasks in this change and in
  `docs/design/mockup-sync.md`.
