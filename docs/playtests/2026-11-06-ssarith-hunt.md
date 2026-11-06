# Playtest 2026-11-06: the Ssarith Hunt in the game

- **Commit:** the branch `claude/gallant-brown-1vddu9` at the commit adding the Hunt to the game (change
  `ssarith-ships-engine`, rubentipparach/fleet-command#38)
- **Machine:** a cloud session, Godot 4.7.1 .NET on Linux; headless at a fixed 30 fps for the suite, Xvfb with Mesa's
  software renderer for the stills and frames. No frame time measured (CLAUDE.md section 9)
- **Scripts:** `battle` on `concord_hunt` and `krai_hunt`, `wrecks` on `concord_hunt_wrecks` and `krai_hunt_wrecks`
- **Not recorded with OBS:** a cloud session cannot record. One rendered run of Hunt wrecks was grabbed from the Xvfb
  screen every 3 s with ffmpeg (`hunt-wrecks-contact.png`); software rendering is slow, so it ran 25 minutes and reached
  the carrier before the timeout. No Drive link

| Beat | Outcome |
|---|---|
| The Hunt, from the Concord's side | The Hunt's cruiser, two frigates and carrier drawn from their own baked maps; won at 299 s, 93 % hull |
| The Hunt, from the Krai's side | Still fighting at 609 s, one Hunt ship left, the Krai cruiser at 25 % (the placeholder figures are close) |
| Hunt wrecks, Concord | All six hulks broken up in 175 s: the Hooded lance, Forked prow and Trident come apart into burning pieces, each break closed, the skin kept on every piece |
| Hunt wrecks, Krai | All six broken up in 470 s |
| Every surface dressed | `SHIP_SKIN ssarith/<id>.glb: hull shader on 1 surfaces, 0 floodlight pools, 7-11 lamps` for each Hunt ship |

Suite: `PLAYTEST_SUITE 14/14 ok` (both ranges end `fighting`, as a range never ends).

Stills (`docs/screenshots/game/`): `hunt-cruiser.png`, `hunt-carrier.png`, `hunt-trident.png` (each class close in Target
view), `hunt-battle-late.png` (the cruiser at 76 % hull, closing), `hunt-wrecks.png`, `hunt-breakup-lance.png` and
`hunt-breakup-prow.png` (frames of the rendered run), `hunt-wrecks-contact.png` (twelve frames of it).

Still open (change `ssarith-ships-engine` section 2): rooms behind the windows and hangars behind the bays, the Hunt as a
side you command, its own weapons, strobe and beacon sprites.
