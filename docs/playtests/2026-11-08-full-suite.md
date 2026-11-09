# Playtest: the whole suite, every scenario for the four races (2026-11-08)

The owner asked: "has every scenario for all 4 races been tested". The suite had only ever been run in parts (58 runs at
most), so this is the first run of all 116 together.

| | |
|---|---|
| Commit | `claude/auto-fire` at `9ef233a0` (main with PR #60 merged, plus auto fire), then the fix below |
| Machine | the owner's Mac (Apple silicon), Godot 4.7.1 .NET |
| How | `scripts/playtest.sh`: every run in `data/playtests/suite.json`, headless at a fixed 30 fps, 1920 x 1080 |

## Result

`PLAYTEST_SUITE 108/116 ok` on the first run, then **116 of 116** with the fix. No run crashed.

## Found

1. **Every campaign script stuck on the faction screen** (8 runs: campaign, the four freighter runs, paper chart, both
   standing runs): "no control 'fac.concord.new' on screen". PR #60 made the screen choose its phone layout from the
   real window's size, and a headless run reports a 64 x 64 window (and a 64 x 64 root viewport), so the bot got the
   phone key grid sized for that and its click picked nothing. A player's real window was never affected. Fixed in
   `src/Game/Flow/FlowScreens.cs`: a headless run takes the canvas's own size, the desktop layout the bot plays. All
   eight then played through.
2. **Losses that repeat** (balance, not a stall): the Krai lose battle, duel, hunt, institute, pinned, fleet and disable;
   the Kelvane and Ssarith lose battle and fleet; the three non-Concord sides lose The Institute; the Concord loses
   repel. In disable, three sides destroyed the ship they were meant to disable.
3. **Not in the suite**: no Kelvane allied fleet scenario, and no standing campaign script for the Ssarith or Kelvane.
   The Hunt has no Ssarith side and The Institute no Kelvane side, as each is fought against that race.

## Runs

| Kind | Concord | Krai | Kelvane | Ssarith |
|---|---|---|---|---|
| battle, fleet | won | lost | lost | lost |
| duel | won | lost | won | won |
| auto fire (duel) | won | | | |
| arming, wrecks, tractor, mines, shuttle, cloak, boarding, assault, distress, nebula, relief, VIP | ok | ok | ok | ok |
| range (never ends) | fighting | fighting | fighting | fighting |
| fighters | fighting | ok | fighting | fighting |
| disable | lost | lost | lost | lost |
| pinned | ok | lost | ok | ok |
| hunt, hunt wrecks | won | lost, won | lost, won | |
| institute, institute wrecks | lost, won | lost, won | | lost, won |
| allied | ok | ok | | ok |
| four races (hop) | fighting | fighting | fighting | fighting |
| warp | jumped | jumped | jumped | jumped |
| escape | jumped, hull 35 % | jumped, hull 35 % | jumped, hull 35 % | jumped, hull 35 % |
| repel | lost | held | held | held |
| freighters (campaign) | raid won, cash out, escort won | raid lost, escort won | both lost | both lost |
| standing (campaign) | Hunted won | Hunted won | | |
| campaign, paper chart | Shakedown and Stop the scout won, Patrol and Convoy lost; chart ok | | | |

No take was recorded: a headless run proves the game plays through, not how it looks.
