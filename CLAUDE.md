# Fleet Command: rules for Claude

Fleet Command is a real-time tactical starship combat game in Godot. The captain commands a small
fleet from a bridge console: a column of control panels down the left, status bars along the top
and bottom, and the 3D view of the battle filling the rest. It plays on desktop and on phones.

These rules are binding. When a rule here conflicts with a general habit, a design doc or a
tool's default, this file wins. Most are adapted from the owner's other repositories (Crab Breeze,
the-federation, Tactical Airsuite, Star Crew Bridge Simulator), and each one says where it came from.

**This file is the only copy of these rules.** `AGENTS.md` points here and must stay a pointer.
`openspec/config.yaml` points here too. If two files have to agree, sooner or later they won't
(Star Crew, Pale Blue Dot).

Contents:

1. Owner direction
2. The stack: Godot
3. Spec-driven work: OpenSpec
4. Write it up and mock it up before code
5. Writing style
6. Architecture and code quality
7. The screen: the console layout
8. Working with the owner
9. Playtesting, recording and Google Drive
10. Art: models, materials, panels and hull lights
11. Sound and music
12. Verification
13. Skills in this repository
14. Documented exceptions

---

## 1. Owner direction

From the owner's brief, 2026-10-30. Quotes are the owner's words.

- **The game.** "I want to remake old starfleet command. we're gonna stick to this UI layout as much
  as possible." The owner's reference is a screenshot of the 1999 game's battle screen (section 7
  describes it in words). Fleet Command keeps the **shape** of that screen: a captain fighting one
  ship from a console of panels beside a 3D view. Its ships, factions, names, rules text and art
  are our own. That follows the-federation's rule for Federation Commander: "Take the shape, not
  the text."
- **Mobile friendly.** "I want to make this game mobile friendly as well, so panels can be expanded
  to full screen if needed, with the rendering screen of the action shoved off to the side." Every
  panel can be opened to fill the screen. The 3D view then moves to the side and keeps playing; it
  is never hidden or paused (section 7).
- **Godot** (owner, 2026-10-30: "I want to do godot for this"). Section 2.
- **The reference manuals** (owner, 2026-10-30: "also link this in claude md for refrence", "sfc 3 is
  where alot of the ship load out and configuration was, so add this too"):
  - Starfleet Command 2: https://gaming.trekcore.com/starfleetcommand2/downloads/sfc2.pdf (139 PDF
    pages). Menus at printed pages 74-75; skirmish setup 76-80; campaign, hex map and missions
    81-87; supply dock and shipyard 88-89; the battle console ("Take the conn") 108 onward.
  - Starfleet Command 3: https://gaming.trekcore.com/starfleetcommand3/downloads/SFC3manual.pdf (41
    PDF pages). **Ship loadout and configuration**: refit by hardpoint groups, resupply and officers
    at printed pages 28-29; skirmish setup with a points budget 26-27; the campaign hex map, its
    facilities (supply dock, shipyard, officers) and missions 32-37.

  They show how the reference games' screens, systems and campaigns worked. Two rules for using them,
  after the-federation's rules for Federation Commander:
  - **Cite the page, do not trust recall.** When a design rests on how the reference game did
    something, name the manual and its printed page in the OpenSpec design, not a memory of it.
  - **Take the shape, not the text.** Its wording, names, ships, races, art and screenshots belong to
    their owners. What we take is how a system works and how a screen is laid out. Never copy its
    text, names or images into this repository; notes about it go in our own words.
- **Survey answers** (owner, 2026-10-30; the decided list in `docs/design/survey.html`). Windows
  desktop plus native Android (T1). Forward+ on desktop, Compatibility on phones (T2). A mid-range
  phone from about 2022 must hold 60 fps at the phone layout (T3). An expanded panel puts the live
  battle in a strip down the right side, about a third of the width (U1). Phones play landscape and
  portrait; portrait stacks the console above the view (U2). Console panels are textured: modelled
  metal frames, real buttons and placards, rendered to images (U3). Classic space opera: bright
  hulls, named starships, a federation of species against rival empires (G1). Real time with pause,
  orders given while paused (G2). **A small fleet**: the player commands several ships and switches
  between them, "with ai wingmen, and behaviors configuratble for AI" (G3). Mid-detail ships: clear
  silhouettes, panel-line textures, baked hull lights, sized for phones (A1). Orchestral space opera
  with a synth bed (S1). Playtests on the Windows PC with the RTX 3070 and the owner's phone (P1).
- **Mockups first, in three stages** (owner, 2026-10-30: "we'll begin with a rolling list of
  mockups needed", "all these mockups need to be approved for build, and then need to be approved
  for actual game implementation after", "All mockups are to be checked into repo especially when
  they are html claude artifacts"). `mockup-work.md` is the rolling list; section 4 has the rules.
- **Ground rules and skills first** (owner, 2026-10-30: "lets establish ground rules, we need skills
  like open spec, 3d modeling, game ui, audio/music, light baking (hull lights), material maker,
  panel textures, automated playtesting, threejs mockups, obs recording, questions html survey,
  clean code", "just install all this stuff first, then I will dive into what I want for three js
  prototypes"). Section 13 lists the skills. The game itself starts with three.js prototypes
  (section 4).

## 2. The stack: Godot

> **Under review: Godot or Bevy** (owner, 2026-10-30: "I'm notr convinced with godot actually because I
> really want high fidelity rendering... soooo maybe bevy if it supports android?"). Bevy does build
> for Android (its `mobile` example, `cargo-ndk`, GameActivity on API 31 and up), and Crab Breeze is
> already a Bevy 0.20 workspace with its own skills. Survey `T4` decides; Bevy is the recommendation.
> Until it is answered, write no engine code and nothing new that only one engine can use. The
> mockups are three.js and carry over either way. When `T4` lands on Bevy, this section, the Godot
> skills (`godot-architecture`, `clean-code`, `testing-practices`, `playtest`) and `build.config` are
> rewritten from Crab Breeze's Bevy versions in one change.

- **Godot 4.7.1-stable, standard (GDScript) build**, pinned in `build.config`, the one place a
  toolchain version is written. `scripts/install-godot.sh` installs that editor into `.tools/godot/`.
  The SessionStart hook runs it in cloud sessions. Upgrading Godot is its own change: read the
  migration notes, and run a playtest afterwards.
- **GDScript, statically typed.** Every variable, parameter and return carries a type, and the
  project's untyped-declaration warnings are errors. The .NET build is not used, because it cannot
  export to the web (the-federation, measured on 4.7.1).
- **Read the docs for the pinned version.** The class reference and migration guide for 4.7 are
  the source; never write Godot API calls from memory of another version.
- **the-federation is the reference Godot project** (`RubenTipparach/the-federation`). It has a
  headless simulation library, thin scene scripts, a headless test runner, a layout test under
  Xvfb and `build.config`. Fleet Command follows the same split (section 6).
- **Assets are glTF (`.glb`)** built by committed Blender generators (section 10). They are
  imported by Godot, and the import is a build product.
- **Platforms**: Windows desktop is the owner's playtest machine, Linux runs the headless tests,
  and phones are a target from the first screen. The builds are Windows desktop and native Android
  (survey T1). The renderer is Forward+ on desktop and Compatibility on Android, set per platform
  in `project.godot` (`rendering/renderer/rendering_method.mobile`) (survey T2).

## 3. Spec-driven work: OpenSpec

Requirements and designs live in `openspec/`, driven by the OpenSpec CLI
(`npm install -g @fission-ai/openspec@1.14.0`; https://openspec.dev). The SessionStart hook
installs it. The split is the point (Star Crew, Pale Blue Dot, Crab Breeze):

- **`openspec/specs/<capability>/spec.md` is what the game does today.** Every requirement in it
  is true now, and a passing check pins it: a unit test, a data validation, a layout test or a
  playtest.
- **`openspec/changes/<name>/` is what is designed but not built.** It holds `proposal.md` (why,
  quoting the owner), `design.md` (how, with numbers, costs and an `## Open questions` table),
  `tasks.md`, and spec deltas under `specs/<capability>/spec.md`.
- **`openspec/changes/archive/`** takes a change once its work is real and its deltas are merged.
- **Small, single-purpose changes.** One per capability, never an umbrella.
- **`openspec validate --all` passes before every push.**

| Command | Does |
|---|---|
| `/opsx:explore` | Think the problem through |
| `/opsx:propose` | Write the planning artifacts, then stop |
| `/opsx:apply` | Implement (a separate request) |
| `/opsx:verify` | Check the implementation against the change |
| `/opsx:archive` | Close the change when it lands |

## 4. Write it up and mock it up before code

Adapted from Crab Breeze and Star Crew:
1. Investigate and measure.
2. Write the finding and the plan in `openspec/`: a proposal, a design and the spec deltas.
3. **Show it as a three.js mockup** if a player will see it (a ship, a panel, the console layout,
   an effect, a weapon's look). The mockup is an interactive page in `docs/mockups/` (the
   `threejs-mockups` skill), published as an artifact and linked to the owner. A 3D subject is shown
   orbitable and at true scale.
4. Stop there. Game code waits until the owner has seen the mockup and asks for it.

**Every mockup passes two approvals** (owner, 2026-10-30: "all these mockups need to be approved for
build, and then need to be approved for actual game implementation after"):

| Stage | Means | What may follow |
|---|---|---|
| **Drafted** | The write-up and the page exist and are published | Revisions from the owner's notes |
| **Approved for build** | The owner approves the mockup as the design to build | The mockup is finished out (every screen, state and shot) and the OpenSpec change is completed with its tasks |
| **Approved for implementation** | The owner approves putting it in the game | Game code (`/opsx:apply`), in a separate request |

- **`mockup-work.md` is the rolling list of mockups needed** (owner, 2026-10-30), in the owner's words,
  with each mockup's stage, change, page and link. A new mockup goes on the list before work starts.
- **The mockup tracker** (`mockup-tracker` skill) is an HTML artifact listing every mockup's link and
  stage, where the owner approves each stage and leaves notes. Its link is in `mockup-work.md` and
  `docs/design/README.md`. An approval counts only when it is in the tracker or in the owner's own
  words in the conversation; record it in `mockup-work.md` either way.
- **Every mockup is checked into the repository** (owner, 2026-10-30: "All mockups are to be checked
  into repo especially when they are html claude artifacts"): the page in `docs/mockups/`, its shots in
  `docs/screenshots/mockups/`, and a record in `docs/artifacts/<name>/` with its link, committed in the
  same step as every publish.
- **If the owner changes accounts, republish every mockup, the tracker and the survey from the new
  account** (owner, 2026-10-30), then update every link: the tracker's list, `mockup-work.md`,
  `docs/design/README.md` and each `docs/artifacts/<name>/README.md`. An artifact made from another
  account cannot be read or updated from here.

- **Any new screen or panel needs the owner's approval of its mockup first** (the-federation 6).
  Bug fixes and small changes to approved UI do not.
- **Mockups and documentation tooling are not game code**, and are built alongside the write-up.
- **A measurement instrument may come first**: code whose only job is to produce a number the
  write-up needs, said so in the write-up and kept to that job.

## 5. Writing style

**Never use em dashes (U+2014) or en dashes (U+2013), anywhere.** That covers docs, comments,
commit messages, PR bodies, in-game text, UI strings, logs and replies (every owner repository).
Use a colon, commas, parentheses, a period, or a plain hyphen for a range (`10-25 s`).

```sh
LC_ALL=C.UTF-8 grep -rnIP '\x{2014}|\x{2013}' --exclude-dir=.git --exclude-dir=.claude \
  --exclude-dir=.godot --exclude-dir=.tools --exclude-dir=node_modules . && echo FAIL
```

- **Numbers carry units.** SI units, unless a design defines game units and says so (a game
  distance unit, an energy point).
- **Plain words, from the player's side** in anything the owner reads: what they will see or do,
  then how it works.
- **Menus name things; they do not explain them** (the-federation 6.5). A row is a label and a
  control. Reasons belong in `docs/`.

## 6. Architecture and code quality

The `clean-code`, `godot-architecture` and `testing-practices` skills are the working detail.

### 6.1 One implementation of every rule

**Avoid divergent code paths that share functionality at all costs.** This is the highest-priority
engineering rule in every owner repository.
- Same behaviour, same code. Parameterise; never clone. Search before writing anything new.
- **What a panel shows is computed by the code that resolves it.** The chance-to-hit readout, the
  capacitor bar and the projected damage all call the simulation the shot itself uses. A readout
  that disagrees with the outcome is exactly the bug this rule prevents.
- **The ship systems display is one component** (the-federation 6.1). The console, the
  full-screen panel, the target readout and the after-battle report all instance it with
  parameters.
- Report duplication when you find it. Never add a third copy.

### 6.2 SOLID, and a simulation that runs without a scene

- **The simulation is plain GDScript** (`src/sim/`, `RefCounted` classes, no `Node`). It covers
  movement, energy, shields, weapons, damage, AI and stable ids. It runs and is tested headless,
  without instantiating a scene (the-federation 5.2).
- **Scenes are structure; code is behaviour.** A `.tscn` says what exists and how it is arranged.
  Scripts on nodes are thin: they wire a node to the simulation and to input. No rules live in
  scene data or inspector values.
- **SOLID**: one reason to change per class; extension by registries and data, not growing `match`
  statements; subtypes that honour their base; small interfaces; dependencies (RNG, time step,
  data) passed in and wired in one composition root.
- **Input is actions** (Godot's `InputMap`): keyboard, mouse, touch and pad map to the same
  actions. The scripted captain sends those actions too (section 9).

### 6.3 Determinism

The simulation advances in fixed steps (`_physics_process` or its own fixed loop, with the step
in seconds as data). Randomness is seeded and forked per subsystem, never `randf()` on the global
generator. Order is stable (sorted by stable id, never by node or dictionary order). Non-finite
numbers are rejected where they enter. Golden-seed tests pin all of this. A battle replays exactly
from its seed and inputs.

### 6.4 Tuning lives in data

No inline tuning. Ship classes, weapons, energy costs and every tuning number live in `data/`
(JSON), with the unit in the key (`max_speed_u_s`, `capacitor_e`), validated on load. A missing
field takes the code default, a present zero is zero, an unknown key is an error, and a file that
fails validation stops startup naming its path and field.

### 6.5 Conventions

- Every script opens with a comment block saying what it owns and why it lives there. Every public
  function is documented. Units go in names or docs.
- Tests sit beside the rule they test and read as sentences. A bug fix brings its regression test.
- Godot's script warnings are errors in `project.godot`, and the test run fails on any warning.
- Guard the edges: reject non-finite input, check indices, and an operation that does not fit
  changes nothing.
- When two things must agree, validate the real artifacts: a check loads both, never a hand-written
  copy of the expected value.
- Borrowed code keeps its provenance (source repository and revision) beside it.
- **CI stays thin**: build logic lives in `scripts/`, which CI and developers both run
  (the-federation 8).

## 7. The screen: the console layout

The owner's reference is the 1999 battle screen, and the layout is kept "as much as possible".
Fleet Command's version, in words (the image itself is not committed, because it is someone
else's artwork):

| Region | What it holds |
|---|---|
| **Left column** (about a quarter of the width, full height) | Stacked panels, top to bottom: the captain's name plate; the helm (a 3 x 3 pad of turn and manoeuvre buttons, the chance to hit, a big engage button); the **ship systems display** (the ship from above, its shield facings and weapon mounts); the weapons panel (fire, multiple fire, and each mount's arming state: offline, normal, overload, with charge pips); and a small radar or minimap at the bottom |
| **Top strip** | Own hull integrity bar, the weapon capacitor bar, target hull integrity bar, the mission clock |
| **Bottom strip** | Speed, power and systems bars |
| **The 3D view** (everything else) | The battle: own ship in a chase view, the target bracketed with its name, class and speed, a readout of the target's range and own weapons in arc and charged, the pointer for picking targets |

- **The console is the game's cockpit.** A captain learns where each readout is and reaches for it
  without looking. **Panels are a fixed size, and their content never changes it** (the-federation
  6.4): fixed rectangles on a reference resolution, clipped, with text that is trimmed with an
  ellipsis and never resizes the panel. A layout test checks this.
- **Mobile: every panel can be expanded to full screen.** An expanded panel takes the screen, and
  the 3D view moves into a strip down the right side, about a third of the width, still live, still
  showing the battle (survey U1). Tapping it brings the console back. **Phones play landscape and
  portrait** (U2): in portrait the console stacks above the view, and an expanded panel leaves the
  live view in a strip along the bottom. Touch targets are at least 44 x 44 CSS px (Tactical Airsuite
  5), and nothing is reachable by hover alone.
- **The console panels are textured** (U3): modelled metal frames, real buttons and placards,
  rendered to images by `panel-textures`, with readouts drawn live inside them.
- **The OpenAI `game-ui-frontend` skill's "protect the playfield" defaults** (one HUD cluster,
  20-25% of the screen at most) **do not apply** to the console: the owner chose a dense,
  panel-heavy layout. Its other rules do apply: readable over motion, themed (not a SaaS
  dashboard), restrained motion, input gated under menus.
- **Allocation is discrete boxes, not sliders**, wherever a value is a whole count of energy
  points (the-federation 6.2). The test for anything on the battle screen is: *would a captain
  reach for this under fire?*

## 8. Working with the owner

- **Ask with the survey, never in chat** (every owner repository). Open questions go into
  `docs/design/survey.html`, published as an HTML artifact whose database holds the answers, with
  options, a recommendation and a place to answer (the `html-survey` skill). Its link is in
  `docs/design/README.md`. Each reply that adds questions gives the link and reminds the owner to
  fill it in.
- **Surveys are always HTML artifacts, never Claude Docs** (owner, 2026-10-21, in Tactical
  Airsuite: "format surveys as html artifacts please, I actually really like those").
- **A blank answer means the recommendation is accepted.** Build on recommendations while a question
  is open, and mark them provisional in the change's `## Open questions`.
- **End every reply with the artifact links** (owner rule in every repository): each artifact made
  or changed since the owner's last message, with one line on what is new, or a line saying there
  were none.
- **Every published artifact is checked into the repo** (Crab Breeze, owner 2026-10-28). Its source
  goes in `docs/artifacts/<name>/` with a `README.md` giving its link and what made it, committed
  in the same step as the publish.
- **Every pushed branch has a pull request** (Crab Breeze, owner 2026-10-25), and **a session
  produces one combined pull request** (Crab Breeze, owner 2026-10-26). Every task in a session
  goes onto one branch as new commits. The PR body says which session owns it.
- **No scheduled PR check-ins** (owner rule in Pale Blue Dot, Star Crew, Mining Mike, the-federation).
  The one exception is the five-minute check on a running desktop playtest (section 9).
- **When the owner is using the computer, do not playtest** (Crab Breeze, owner 2026-10-24) on
  their machine unless they have said, in this conversation, that Claude may playtest now.
- **Never take camera control away from the player** (Crab Breeze, owner 2026-10-24). The chase
  view orbits by the player's drag only. No snapping, no scripted turns outside a cinematic the
  owner asked for.

## 9. Playtesting, recording and Google Drive

The `playtest` and `obs-record` skills are the method (Crab Breeze 9, Mining Mike, Star Crew).
- **A feature is done when a playtest shows it in the running game**, not when a test passes or a
  mockup looks right.
- **The scripted captain plays fair.** It sends the same input actions a player does (taps,
  clicks and keys through `InputMap`), following beats in `data/playtests/<name>.json`, and prints
  `GAME_READY`, `PLAYTEST_START`, `PLAYTEST_BEAT`, `PLAYTEST_RESULT` and `PLAYTEST_END`.
- **A playtest covers both screen shapes.** It plays at the desktop reference resolution and at a
  phone's (landscape and portrait, survey U2), including a panel expanded to full
  screen with the view at the side.
- **Record with OBS** between `PLAYTEST_START` and `PLAYTEST_END`, in real time, and **look at
  the contact sheet** before calling a take done.
- **Upload every take to Google Drive.** Make a small H.264 share copy (`-crf 26 -preset slow`)
  into `G:/My Drive/dungeon-crawler-2026/` (the folder the owner's other games use), named
  `fleet-command-<YYYY-MM-DD>-<what>.mp4`, confirm it with the Google Drive connector, and give
  the owner the link.
- **One game per device**, and **a desktop run is checked every five minutes** while it plays
  (Mining Mike).
- **Report each playtest** in `docs/playtests/<date>-<name>.md` with the commit, the machine, each
  beat's outcome, the Drive link and the contact sheet.
- **Cloud sessions cannot record.** They render headless stills under Xvfb (Mesa's software
  renderer) and say so. A cloud session measures no frame time.

## 10. Art: models, materials, panels and hull lights

The skills `blender-hard-surface`, `material-maker`, `material-maker-headless`, `panel-textures`,
`light-baking` and `threejs-mockups` are the method. Each has an "In Fleet Command" note at its top
saying what changes here. Where a note and the copied text below it disagree, the note wins.
- **Meshes are files built by committed generators** (Star Crew, Crab Breeze, the-federation 2). A
  Blender script writes each ship deterministically and exports a byte-reproducible `.glb` with
  a manifest (dimensions, triangles against budget, named nodes). No meshes are generated at
  runtime.
- **Hard surface the CSG way**: primitives, named cutters through Exact booleans, bevels where light
  catches, cleaned, checked, exported (`blender-hard-surface`).
- **Named nodes for what the game drives**: every weapon mount, shield emitter, engine, hull light
  and damage zone is a named node or empty in the glb, so the game can aim from it, light it and
  hit it.
- **Hull lights are baked** (owner, 2026-10-30: "light baking (hull lights)"). Running lights,
  window rows, spotlit registry and deflector glow are baked into the ship's emission and light
  maps with occlusion. A few live lights (weapon flashes, damage fires) play over the bake.
  `light-baking` says how.
- **Ships are lit like aircraft at night** (owner, 2026-10-30, with an airliner lighting guide: "logo
  lights will be cool, but we'll need additional hull lighting too, as that highlights details on the
  ship in dark lighting conditions"). Logo floodlights on insignia and registry, inspection floods on
  nacelles and wings, white strobes at the extremities, red beacons top and bottom, position lights
  (port red, starboard green, white aft), docking and manoeuvring lamps, and **hull floodlighting that
  grazes the plating** so its detail reads in shadow. The full table is mockup 3 in `mockup-work.md`.
- **Materials are Material Maker graphs** (Star Crew): a graph is the one implementation of how a
  material looks, one data file lists every material, and a contact sheet is looked at before it
  is done. PBR maps: base colour, normal, ORM, emission.
- **Panels are modelled and rendered** (owner, 2026-10-30: "panel textures"). Hull plating panels
  and the console's own panel faces (bevelled frames, buttons, placards) are modelled in Blender
  and rendered face-on into maps (`panel-textures`).
- **Mid detail, mobile budgets** (survey A1, T3). Ships have clear silhouettes, panel-line textures
  and baked hull lights. Every asset has a triangle and texture budget per LOD in its manifest, and
  the build refuses an asset over budget; the numbers are set by the look-dev mockup (mockup 3) so a
  mid-range 2022 phone holds 60 fps at the phone layout.
- **Original designs.** Ships, factions, insignia and markings are our own. Nothing is traced,
  sampled or modelled from another game's assets.
- **No z-fighting, ever** (Undercity, Star Crew).
- Blender for cloud sessions: `scripts/install-blender.sh` (4.4.3 into `/opt/blender`, about a
  minute).

## 11. Sound and music

- **Music is written motifs first, Tom Lander's way** (`music-authoring`): motifs as notes in
  `docs/design/music.md` and `music/motifs.strudel`, cues as Strudel scores, exported by
  `tools/music/` to multitrack MIDI and LMMS projects, and rendered headless in LMMS into the final
  comp. The direction is orchestral space opera, brass, strings and timpani, over a synth bed
  (survey S1).
- **Sound effects are recorded CC0 sounds where possible** (Crab Breeze), every original named
  in `SOURCES.md`. They are synthesised only where nothing recorded is better.
- **Sounds from the battle are placed in 3D** at their ship or mount. Interface clicks, music and
  ambience are not.

## 12. Verification

Before claiming anything is done, run what applies:

| Check | Command |
|---|---|
| Specs | `openspec validate --all` |
| Dash check | section 5 |
| Tests | `scripts/run-tests.sh` (headless simulation tests, then the layout test under Xvfb), once the project exists |
| Data | the data validator, once it exists |
| Mockups | screenshot each changed page and look at the shots (`threejs-mockups`) |
| In the game | a playtest (section 9), or headless stills in a cloud session |

- **Know what is proven.** Keep implemented, validated and proposed work distinct in docs, PRs and
  replies. A design is not a feature, and a green test is not a visual sign-off.
- **Every major step ends in something the owner can check**: a mockup link, a playtest video on
  Drive, or captures in `docs/screenshots/`.

## 13. Skills in this repository

| Skill | Use |
|---|---|
| `openspec-*` and `/opsx:*` | The OpenSpec workflow (section 3). Tool-owned: `openspec update` rewrites them. |
| `html-survey` | The owner's question system (section 8). |
| `mockup-tracker` | The page listing every mockup's link and approval stage; republishing after an account change (section 4). |
| `clean-code` | One implementation, SOLID in GDScript, conventions, review checklist (section 6). |
| `godot-architecture` | Where code goes: simulation, scenes, autoloads, input, the console, determinism (sections 2, 6, 7). |
| `testing-practices` | Headless GDScript tests, the layout test, golden seeds, the checks before a push (sections 6, 12). |
| `game-ui-frontend` | OpenAI's game UI skill: visual direction, hierarchy, readable overlays, motion (section 7 overrides its playfield budget). |
| `ui-ux-pro-max` | UI/UX design intelligence: styles, palettes, type pairings, UX and accessibility rules, searchable (`scripts/search.py`). |
| `threejs-mockups` | Interactive three.js mockups as artifact pages, before game code (section 4). |
| `blender-hard-surface` | Ships, stations and props in Blender, the CSG way, exported as glb (section 10). |
| `material-maker` | Making and judging materials from Material Maker graphs (section 10). |
| `material-maker-headless` | Running Material Maker with no GPU in a cloud session (section 10). |
| `panel-textures` | Hull plating and console panels modelled in Blender and rendered to PBR maps (section 10). |
| `light-baking` | Baking hull lights and occlusion into the ships' maps (section 10). |
| `music-authoring` | Motifs in notes, Strudel scores, multitrack MIDI, LMMS headless (section 11). |
| `playtest` | Running, watching, recording, uploading and reporting a playtest (section 9). |
| `obs-record` | Recording a window with OBS and copying it to Google Drive (section 9). |

Provenance for every skill is in `.claude/skills/PROVENANCE.md`.

## 14. Documented exceptions

Nothing is an exception until it is listed here with its reason.
- **Tool-owned files keep their tool's text.** The OpenSpec CLI writes `.claude/skills/openspec-*`
  and `.claude/commands/opsx/*`; don't hand-edit them. The dash check excludes `.claude/`.
- **Copied skills keep their source's text** below their "In Fleet Command" note, so a later copy
  from the source can be diffed. Where the source and the note disagree, the note and this file
  win. The two external skills (`game-ui-frontend` from OpenAI, `ui-ux-pro-max` from
  NextLevelBuilder) are MIT licensed, and their licence files are kept with them.
