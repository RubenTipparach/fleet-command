# Fonts: where they came from

Every font here is under the SIL Open Font License 1.1; each one's licence text is beside it, unmodified.

| File | Family | Used by | Source | Licence |
|---|---|---|---|---|
| `Antonio-VariableFont_wght.ttf` | Antonio | the Concord skin (`src/Game/Ui/Kit/ConcordKit.cs`) | added with the first battle console slice (commit `c213456`), its download not recorded then; its licence names the Antonio Project Authors (https://github.com/googlefonts/antonioFont) | `Antonio-OFL.txt` |
| `Teko-VariableFont_wght.ttf` | Teko (variable, weights 300-700) | the Krai Warhost skin's body text (`src/Game/Ui/Kit/KraiKit.cs`) | https://github.com/google/fonts, `ofl/teko/Teko[wght].ttf` at commit `2fe1e8c6b137fb13041a7cfca6fdcd8a910ea4dc`, downloaded 2026-11-04; SHA-256 `d1321889f262bbbff632e7976349853399cd097b6f382d4b19790c915c13c1ae` | `Teko-OFL.txt` (`ofl/teko/OFL.txt`, same commit) |
| `RussoOne-Regular.ttf` | Russo One | the Krai Warhost skin's titles and name plate | https://github.com/google/fonts, `ofl/russoone/RussoOne-Regular.ttf` at commit `2fe1e8c6b137fb13041a7cfca6fdcd8a910ea4dc`, downloaded 2026-11-04; SHA-256 `bc0abcc660bd8b7ad3000ecb2898a27c58a29a50f7ec81652fa12e75148d09df` | `RussoOne-OFL.txt` (`ofl/russoone/OFL.txt`, same commit; Reserved Font Name "Russo", so the file is used as published, never modified) |

The Krai pair is the one the Krai battle console mockup loads (`docs/mockups/lib/krai.js`, `font()`); its sizes are
mapped from the console's Antonio sizes as that mockup maps them.
