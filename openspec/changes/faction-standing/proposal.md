# Proposal: faction-standing

## Why

The owner, 2026-11-08: "Verify and update reputation systems if needed. Friendly factions: allows increasing privilege to use
friendly stations and outposts. Start with parts, supplies, weapons, then ships, and finally officers. Enemy factions:
depending on hostility, enemy factions will persue you with larger and larger fleets if you cross into hostile territory."

Until now standing did three things: a port of a faction at 0 or better bought your ships (the Market), 25 or better made it an
ally (its fleets and freighters seen, its freighters escorted), and below 0 made its space hostile. Nothing else changed with it.

## What changes

- **Privileges at friendly ports**: each grant opens at a standing (`rules.privileges`): repair kits and repairs (parts) at 0,
  the other supplies at 15, weapons and systems at 30, ships of that faction's yard at 50, officers at 70. Your own ports give
  everything; a rival's or a hostile faction's nothing. The port screen shows the Market and the tabs you have earned; the
  sector panel's keys open as each grant does, with the standing needed as the tip; the standing chips list what each
  faction's ports give.
- **Pursuit**: ending a turn in a hostile faction's space raises its alarm (1, plus 1 for every 25 of standing below 0); each
  alarm level (2, 4, 7, 11) sends hunters from its port nearest you, 1 ship at the first level and 1 more each level after, at
  most 6, moving 2 sectors a turn toward the fleet. Caught, the fleet is offered the Hunted fight (fight, or withdraw and keep
  moving); beaten hunters are broken up, and more come while you stay. Away from hostile space the alarm cools a point a turn
  and hunters with no alarm go home.

## Impact

`data/campaign/campaign.json` (rules.privileges, rules.pursuit), `src/Core/Campaign` (Campaign, CampaignData, CampaignState,
MissionScript's `event` kind and `disband` outcome, MissionRun), `data/missions/pursuit.json`, the port panel and sector panel
(`src/Game/Flow`). The port screen keeps its approved layout; only which tabs show changes.
