# Audio sources: what was looked at

The candidates for Fleet Command's first sound effects (2026-11-04, the `sound-effects` skill), how
they were found and which are used. What each cue is cut from is `assets/audio/sfx/SOURCES.md`; every
pack in use is `AUDIO_SOURCES.md`.

## How they were found

- OpenGameArt's search with the CC0 licence filter, for: laser, shield, explosion, spaceship, sci-fi,
  engine, alarm, torpedo, ui click, computer, beep, forcefield, thruster.
- Crab Breeze's packs (`tools/audio/cc0_packs.json` there) where they fit a starship: its explosions,
  rocket motors, the ship's cannon from *battle at sea*, the electricity pack, the generator and the
  air vent.
- kenney.nl answered 403 to a cloud session on 2026-11-04, so Kenney's own site was not used; his
  digital laser and phaser pack is his upload on OpenGameArt.
- Freesound needs an account and an API key for downloads, and was not tried.

Every pack was fetched by `tools/audio/fetch_cc0.py`, which read its page and found CC0 on every one.

## The packs

| Pack | By | Licence on its page | Status |
| --- | --- | --- | --- |
| [10-impactshield-blocks](https://opengameart.org/content/10-impactshield-blocks) | StarNinjas | CC0 | fetched, not used yet |
| [100-cc0-metal-and-wood-sfx](https://opengameart.org/content/100-cc0-metal-and-wood-sfx) | rubberduck | CC0 | used |
| [50-cc0-sci-fi-sfx](https://opengameart.org/content/50-cc0-sci-fi-sfx) | rubberduck | CC0 | used |
| [51-ui-sound-effects-buttons-switches-and-clicks](https://opengameart.org/content/51-ui-sound-effects-buttons-switches-and-clicks) | Kenney | CC0 | used |
| [60-cc0-sci-fi-sfx](https://opengameart.org/content/60-cc0-sci-fi-sfx) | rubberduck | CC0 | fetched, not used yet |
| [63-digital-sound-effects-lasers-phasers-space-etc](https://opengameart.org/content/63-digital-sound-effects-lasers-phasers-space-etc) | Kenney | CC0 | used |
| [7-space-sounds](https://opengameart.org/content/7-space-sounds) | Joth | CC0 | fetched, not used yet |
| [75-cc0-breaking-falling-hit-sfx](https://opengameart.org/content/75-cc0-breaking-falling-hit-sfx) | rubberduck | CC0 | fetched, not used yet |
| [airvent-loop](https://opengameart.org/content/airvent-loop) | Longor | CC0 | used |
| [atmospheric-interaction-sound-pack](https://opengameart.org/content/atmospheric-interaction-sound-pack) | qubodup | CC0 | fetched, not used yet |
| [battle-at-sea](https://opengameart.org/content/battle-at-sea) | Thimras | CC0 | used |
| [boost-or-launch-or-thruster-sound-effect](https://opengameart.org/content/boost-or-launch-or-thruster-sound-effect) | EZduzziteh | CC0 | used |
| [chunky-explosion](https://opengameart.org/content/chunky-explosion) | Joth | CC0 | used |
| [doomsday-laser-cannon-sound-effect](https://opengameart.org/content/doomsday-laser-cannon-sound-effect) | TAD | CC0 | fetched, not used yet |
| [electricity-game-sound-pack](https://opengameart.org/content/electricity-game-sound-pack) | faxcorp | CC0 | used |
| [electronic-device-loop](https://opengameart.org/content/electronic-device-loop) | qubodup | CC0 | fetched, not used yet |
| [explosion-somewhere-far](https://opengameart.org/content/explosion-somewhere-far) | pauliuw | CC0 | fetched, not used yet |
| [explosions-4](https://opengameart.org/content/explosions-4) | EZduzziteh | CC0 | used |
| [generator-loop](https://opengameart.org/content/generator-loop) | YCbCr | CC0 | used |
| [interface-beeps](https://opengameart.org/content/interface-beeps) | bart | CC0 | fetched, not used yet |
| [laser-weapon-burst-fire](https://opengameart.org/content/laser-weapon-burst-fire) | celestialghost8 | CC0 | used |
| [low-rumbling](https://opengameart.org/content/low-rumbling) | Musheran | CC0 | used |
| [missile-sound](https://opengameart.org/content/missile-sound) | mikeask | CC0 | used |
| [rocket-engine](https://opengameart.org/content/rocket-engine) | theMinesAreShakin | CC0 | used |
| [rocket-launch](https://opengameart.org/content/rocket-launch) | qubodup | CC0 | used |
| [sci-fi-ambience-sfx](https://opengameart.org/content/sci-fi-ambience-sfx) | Fun Gi Development | CC0 | used |
| [seamless-energy-emission-loop](https://opengameart.org/content/seamless-energy-emission-loop) | zeroisnotnull | CC0 | used |
| [space-ship-shield-sounds](https://opengameart.org/content/space-ship-shield-sounds) | bart | CC0 | used |

## Chosen without listening

A cloud session cannot listen. Files were shortlisted by name, length and spectral centroid
(`tools/audio/inspect_sfx.py inventory`), each candidate's loudness strip and low and high energy
read (`inspect_sfx.py envelope`), and the built set judged on a spectrogram contact sheet
(`docs/screenshots/audio/sfx-sheet.jpg`). The owner's ears are the real test: the Fleet Command
Sounds page is where each cue is heard and approved.
