# Skill provenance

Where each skill in this directory came from, so a later copy from the source can be compared
(CLAUDE.md sections 6.5 and 14). The owner's repositories are under `RubenTipparach/`, read on
2026-10-30. Every skill copied from one of them keeps the source's text below an "In Fleet Command"
note, and the note wins where they differ.

| Skill | Source | Revision | Copied | Changes here |
| --- | --- | --- | --- | --- |
| `openspec-*`, `../commands/opsx/*` | OpenSpec CLI 1.14.0 (`openspec init --tools claude`); `openspec-verify-change` and `opsx/verify.md` from Crab Breeze, the same CLI version | Crab Breeze `69ce196` | 2026-10-30 | None. Tool-owned: `openspec update` rewrites them. |
| `html-survey` (with `template.html`) | Crab Breeze, from rubens-solitare-collection `0ef4ade` | `69ce196` | 2026-10-30 | An "In Fleet Command" note (survey path, id prefixes). |
| `obs-record` (with `scripts/obs_record.py`) | Crab Breeze, from godot-sandbox (Mining Mike) `3336b88` and Pale-Blue-Dot | `69ce196` | 2026-10-30 | Note with Fleet Command's recipes. |
| `blender-hard-surface` | Crab Breeze, from Star-Crew-Bridge-Simulator `8b0bbf1` | `69ce196` | 2026-10-30 | Description retargeted to ships; note (named nodes, LODs, mobile budgets, Godot import). Star Crew's `hs_kit.py` is not copied yet: the first ship change ports it. |
| `material-maker` | Crab Breeze, from Star-Crew-Bridge-Simulator `8b0bbf1` | `69ce196` | 2026-10-30 | Description retargeted; note (full PBR for Godot, the pipeline here). Tools ported: `tools/materials/ptex_graph.py` (Crab Breeze `build_ptex.py` helper and `build_house_ptex.py`'s `finish`), `postprocess.py` (its `lit` and contact-sheet cells), `export_materials.sh` (after `export_house_materials.sh`). |
| `material-maker-headless` | Crab Breeze (shared by the owner's repositories) | `69ce196` | 2026-10-30 | Note on paths and the game's own Godot. |
| `panel-textures` | Crab Breeze, from Star-Crew-Bridge-Simulator `8b0bbf1` | `69ce196` | 2026-10-30 | Description retargeted to hull plating and console faces; note. Its kit is copied: `tools/blender/panel_kit.py` (Crab Breeze `tools/blender/cockpit_kit.py`), `tools/fonts/`. |
| `light-baking` | Crab Breeze, from Star-Crew-Bridge-Simulator `8b0bbf1` | `69ce196` | 2026-10-30 | Retargeted to hull lights baked into textures that travel with the ship. |
| `threejs-mockups` | Crab Breeze, from Star-Crew-Bridge-Simulator `8b0bbf1` | `69ce196` | 2026-10-30 | Note with Fleet Command's kit: `docs/mockups/lib/template.html` (new), `docs/mockups/lib/gltf_json_loader.js` (Crab Breeze `tools/mockups/`), `tools/mockups/shoot.mjs` (new, after Crab Breeze's). |
| `music-authoring` | Crab Breeze, after tom-lander `dea3d50` | `69ce196` | 2026-10-30 | Note (direction open, survey `S1`). Tools copied: `tools/music/` (titles renamed), `music/instruments.json`. |
| `game-ui-frontend` (with `references/`) | openai/plugins `plugins/game-studio/skills/game-ui-frontend` and three files of `plugins/game-studio/references/`, MIT | `0722921` | 2026-10-30 | Note: CLAUDE.md section 7's console overrides the playfield budget; reference paths moved beside it. The Codex `agents/openai.yaml` left out. |
| `ui-ux-pro-max` | nextlevelbuilder/ui-ux-pro-max-skill `.claude/skills/ui-ux-pro-max` v2.15.0, MIT (`LICENSE` beside it) | `28b3b8d` | 2026-10-30 | Note; `scripts/tests/` left out. |
| `mockup-tracker` | New, after `html-survey` (the owner, 2026-10-30: "create another html survey skill to track all mockup urls") | | 2026-10-30 | Page `docs/mockups/tracker.html`, written new on the survey's look. |
| `clean-code` | New, after Crab Breeze's `clean-code` and the-federation's CLAUDE.md sections 4 and 5 | Crab Breeze `69ce196`, the-federation `02791b6` | 2026-10-30 | Rewritten for GDScript and Godot. |
| `godot-architecture` | New, after the-federation's `src/sim/` split and CLAUDE.md sections 5 and 6, and Crab Breeze's `bevy-architecture` | the-federation `02791b6`, Crab Breeze `69ce196` | 2026-10-30 | |
| `testing-practices` | New, after the-federation's `tests/` and `scripts/run-tests.sh`, and Crab Breeze's `testing-practices` | the-federation `02791b6`, Crab Breeze `69ce196` | 2026-10-30 | |
| `playtest` | New, after Crab Breeze's `playtest` (itself after Star Crew's `--playtest`, Mining Mike's five-minute checks and Tom Lander's `bot-run`) | Crab Breeze `69ce196` | 2026-10-30 | Rewritten for Godot input events and two screen shapes. |
| `blender-ui-panels` | New, from this session's lighting study after a 1999 music-player skin (the study itself is not committed) | | 2026-10-30 | Its companion guide: `docs/design/ui-bible.html` (the Analog Panel Bible), with the study's step renders in `docs/design/ui-bible/`. |

Data and code borrowed for the mockups (not skills, recorded here so they are found):
- The puff nebulae in `docs/mockups/game-flow.html` (camera-facing billboards, noise-eroded edges, lit as spheres,
  sorted back to front): Crab-breeze `69ce196`, `docs/mockups/island.html` (its cloud-weather puffs), adapted to glow from
  within and take a colour per nebula.
- `data/space/` (nebula skies `quiet`, `verdigris`, `ember` and `starfield.json`): Star-Crew-Bridge-Simulator `9a35f08`,
  unchanged (`data/space/SOURCE.md`). The sky and streak shaders in `docs/mockups/game-flow.html` are ported from its
  `crates/sc-render/shaders/sky.glsl` and `docs/mockups/nebula-skies.html`.

Not copied, and why:
- Crab Breeze's `bevy-architecture`, `deterministic-simulation` and `perf-measure`: Bevy and Rust
  specific. Determinism is in `godot-architecture`; a Godot `perf-measure` comes with the first
  performance question.
- `blender-humanoid-characters`, `blender-csg-levels` and the low-poly car skills: Fleet Command has
  no people, levels or cars yet.
- Tactical Airsuite's `balatro-pixel-art`, `blender-card-art`, `hand-drawn-illustration`: 2D styles
  for other games.
- OpenAI's other game-studio skills (`web-game-foundations`, `three-webgl-game`, `game-playtest` and
  the rest): browser-game engines. `threejs-mockups` covers the prototypes, and `playtest` covers the
  game.
