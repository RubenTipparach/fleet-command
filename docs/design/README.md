# Design

## Faction readiness

What each faction has in the game, checked off: `docs/design/faction-readiness.html`, written by
`tools/factions/readiness.py` from the game's files and `docs/design/faction-readiness.json` (record in
`docs/artifacts/faction-readiness/`; CLAUDE.md section 12).

## Survey

Open questions for the owner live in one survey page (CLAUDE.md section 8):

- Page: https://claude.ai/artifact/81ey21Ngrr8WPTdhce7y8a (cloud session account copy: https://claude.ai/artifact/YHX4u5fPn5hQ1CMr3HhEbt)
- Source: `docs/design/survey.html` (record in `docs/artifacts/survey/`)
- Answers: the artifact's `answers` collection, read with `ArtifactData`.

Question id prefixes: `G` game and feel, `U` the console and screens, `T` tech and platforms, `A` art,
`S` sound and music, `P` playtesting, `F` the game flow (mockup 1), `B` the battle console (mockup 2), `R` the reactor (mockup 2), `E` faction emblems (mockup 9), `X` special weapons (mockup 16), `D` missiles (mockup 16), `I` the Kelvane Institute's ships (mockup 23), `V` freighters (mockup 24). Ids are
never reused.

## Mockups

Every mockup and its approval stage (CLAUDE.md section 4, the `mockup-tracker` skill):

- Tracker: https://claude.ai/artifact/8VqA9YjhCrgoiY2CGR1Qxu (source `docs/mockups/tracker.html`). It is the
  master artifact: every published artifact, in three sections (game mockups, guides, misc).
- The rolling list: [`mockup-work.md`](../../mockup-work.md)

## Guides

- Fleet Command Missions: https://claude.ai/artifact/LRgd2iRm8gdTZ84i7htXhH (source `docs/design/mission-guide.html`, record in
  `docs/artifacts/mission-guide/`): mission types, outcome grades, scenarios and story chains.
- Emblem Design Guide: https://claude.ai/artifact/1ELwhue4vzFPjySnVc475V (cloud session account copy: https://claude.ai/artifact/AewTrvPYf4gyF1kP1gxkDk) (source `docs/design/emblem-guide.html`, record in
  `docs/artifacts/emblem-guide/`). How faction emblems are designed: sketch first with every choice explained, the
  owner picks, then 3D; the rules; the owner's notes, kept on the page.
- Fleet Command Icons: https://claude.ai/artifact/RWE6nzNQpPHE2LGTGCYCMb (cloud session account copy: https://claude.ai/artifact/2Ce2Nw5BbWZsPz1NSWG4Jj) (source `docs/design/icon-guide.html`, record in
  `docs/artifacts/icon-guide/`). The icon standard for every skin and the sheet of every icon.
- Analog Panel Bible: https://claude.ai/artifact/Gzo38Dh7o1EqmfzfpJr8rF (cloud session account copy: https://claude.ai/artifact/Hf4o8xLrZ9WzXaMAwTX4PS) (source `docs/design/ui-bible.html`, record in
  `docs/artifacts/ui-bible/`). Building 1990s-style instrument panels in Blender; goes with the `blender-ui-panels`
  skill.
- Organic Panel Bible: https://claude.ai/artifact/RNyM5ka91uporfETHjAQcD (cloud session account copy: https://claude.ai/artifact/2PceqpsqnjZeLorkvxYGdU) (source `docs/design/organic-bible.html`, record in
  `docs/artifacts/organic-bible/`). Building grown, organic panels in Blender (the Vhorr Brood first); goes with the
  `blender-organic-panels` skill.
- Ship Design Bible: https://claude.ai/artifact/QxFV2qGabbN2chE3zd8km7 (cloud session account copy: https://claude.ai/artifact/4nhxxoGusHfkXX63Xt25bm) (source `docs/design/ship-bible.html`, record in
  `docs/artifacts/ship-bible/`). How every ship is designed, worked on the Concord line: rooms first as block floor
  plans (`data/ships/`), the hull fitted round them, then parts, windows, lights, markings, material and damage.

## Sound and music

- Fleet Command Sounds (mockup 17): https://claude.ai/artifact/7zWthg14SmsRfaFZpj1vAE (source `docs/mockups/sounds.html`, record in `docs/artifacts/sounds/`). Every sound effect
  with a play button; the method is the `sound-effects` skill, every original is in `AUDIO_SOURCES.md`.
- Fleet Command Music (mockup 18): https://claude.ai/artifact/NRSwJgnSQkt3ZiKZtG9Lov (source `docs/mockups/music.html`, record in `docs/artifacts/music/`). The score; its motifs
  are [`music.md`](music.md), the method the `music-authoring` skill.

Both were published from the cloud session's account (2026-11-05); the tracker lists them once it is republished from its own account.

## The screen

The console layout the game keeps from the owner's reference is described in CLAUDE.md section 7.
