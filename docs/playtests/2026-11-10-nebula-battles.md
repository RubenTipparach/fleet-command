# Playtest 2026-11-10: heavy fleets in nebula, three a side, the new clouds

Owner, 2026-11-10: "Can you record some battle videos in nebula for me? Remove the old ones if you have those. I just
wanna see new stuff". The first takes of the day (before the decks, the columns of every form, the fog, the sight-line
lock and the optimised march) were deleted from Drive at the owner's ask; these replace them.

- **Commit:** `7bb9bcd4` for the Concord, Krai and Ssarith takes; the Kelvane take with the scripted captain's fix below
  on top (committed with this report).
- **Machine:** Apple M4 Max MacBook Pro, 3456 x 2234 Retina display at 120 Hz; the game in a 1920 x 1080 window (Forward+),
  the nebula in the volume style (marched columns and decks, 128 samples shared by each 2 x 2 block, baked shadows and
  deck field, fog); recorded by OBS window capture at 60 fps (1920 x 1136: the window with its title bar).
- **Script:** `data/playtests/heavy-fleet.json`: `wait_ready`, `pick_scene`, `all_out hop`.
- **Recorded:** in real time between `PLAYTEST_START` and `PLAYTEST_END`, nothing cut; one OBS, closed after each take.

## The takes

| Side | Against | Result | Drive |
|---|---|---|---|
| Concord | Krai: dreadnought and two cruisers, under a garnet nebula with a cinder storm | `PLAYTEST_RESULT ok concord_heavy side=concord skin=Concord outcome=won t=148s foes_left=0 hull=98%` | [concord/fleet-command-2026-11-10-nebula-battle-concord.mp4](https://drive.google.com/file/d/1tm63DtaMptq6ebadqS1IAmrzN4GLLOxL/view?usp=drivesdk) |
| Krai | Kelvane: research vessel, carrier, battlecruiser, in brass cloud with a slate storm | `PLAYTEST_RESULT ok krai_heavy side=krai skin=Krai Warhost outcome=won t=287s foes_left=0 hull=84%` | [krai/fleet-command-2026-11-10-nebula-battle-krai.mp4](https://drive.google.com/file/d/17-EhqkjEP_vKaGaZlgT9zVYCqdWVtS36/view?usp=drivesdk) |
| Kelvane | Ssarith Hunt: two cruisers and a carrier, in teal cloud with a slate storm | `PLAYTEST_RESULT ok kelvane_heavy side=kelvane skin=Kelvane Institute outcome=won t=173s foes_left=0 hull=100%` | [kelvane/fleet-command-2026-11-10-nebula-battle-kelvane.mp4](https://drive.google.com/file/d/1KoocSXb9OcyXKvOBZpcawC1WsO-u6JMA/view?usp=drivesdk) |
| Ssarith Hunt | Concord: three heavy cruisers, in orchid cloud with a cinder storm | `PLAYTEST_RESULT ok ssarith_heavy side=ssarith skin=Ssarith Hunt outcome=won t=295s foes_left=0 hull=100%` | [ssarith/fleet-command-2026-11-10-nebula-battle-ssarith.mp4](https://drive.google.com/file/d/16yIYGP8tIzRAbovyXud5e9IxIRG5iWbY/view?usp=drivesdk) |

Every beat ended. What the captain used (`PLAYTEST_SYSTEMS`):

| Side | Fighters | Barrages | Shuttles | Tractor | Mines | Raids | Special weapon | Ships taken | Power pages |
|---|---|---|---|---|---|---|---|---|---|
| Concord | 12 | 3 | 3 | 3 | 6 | 1 | Chorus lance 7 | 3 | 3 |
| Krai | 14 | 4 | 3 | 3 | 7 | 2 | Forge driver 10 | 7 | 3 |
| Kelvane | 20 | 5 | 2 | 2 | 6 | 3 | none fitted | 4 | 3 |
| Ssarith Hunt | 20 | 4 | 3 | 2 | 4 | 1 | none fitted | 7 | 2 |

Lightning tripped cores in the Concord take (Kharz at 116 s, Resolute at 118 s) and twice in the Kelvane take (142 s,
167 s). Contact sheets: [Concord](2026-11-10-nebula-battles-concord_frames.png),
[Krai](2026-11-10-nebula-battles-krai_frames.png), [Kelvane](2026-11-10-nebula-battles-kelvane_frames.png),
[Ssarith](2026-11-10-nebula-battles-ssarith_frames.png): the decks, columns and fog in every frame, no dialog, no black frame.

## What went wrong, and the fix

- **The first Kelvane take stuck** (`PLAYTEST_RESULT stuck beat 2 all_out: no hull changed for 60 s (t=164 s; no
  target)`, the last line "No lock: the nebula is between us"): with a plume on every sight line beyond 20 km, the
  scripted captain kept pressing Next target and never flew toward an enemy it could not lock. It now does what a player
  does: with enemies seen and none lockable, it clicks the battle plane toward the nearest one every few seconds (the
  helm by pointing), and closing on it counts as progress (`ScriptedCaptain.Fight`, `ViewOverlay.ScreenPointAt`). The
  four heavy battles headless after the fix: all won (concord 178 s, krai 198 s, kelvane 164 s, ssarith 305 s); the
  Kelvane take was recorded again with it.
- The Krai and Ssarith fights run long (287 s and 295 s): much of each is hunting through the cloud, the sight-line rule
  at work.
- The Concord, Krai and Ssarith takes were recorded before the captain's fix; they did not need it.
- AI captains still do not steer round a storm or use craft, tractors or mines (`nebula-plumes` task 1.4).
