# Faction skin modules

Each file here is one faction skin for mockup 4 (`docs/mockups/console-skins.html`, OpenSpec change `console-skins`).
`tools/mockups/inline_data.py` inlines every module into the page after its main script, between
`<!-- INLINE docs/mockups/skins/<id>.js -->` markers, so the published page stays one self-contained file.

A module is plain JavaScript in an IIFE that calls `window.FCK.register(id, def)`. It draws everything in canvas,
deterministically (seeded randomness only: `FCK.rng(FCK.hash('<id>.<piece>'))`), and fetches nothing.

## What `def` must have

The page refuses a skin missing any of these (see `REQUIRED` in the page):

| Field | Meaning |
|---|---|
| `name`, `kind` | Display name; one line on the method ("Hologram light in sculpted armour") |
| `bg` | The console's background colour |
| `css` | The skin's CSS tokens (optional ones: `--btn-shadow`, `--btn-align`, `--btn-pt`, `--bt-rep` and `--bf-rep` set to `round` to tile the bar images instead of stretching them, and `--stt-pt`, `--stt-pb`, `--stb-pt`, `--stb-pb`, the top and bottom strips' content padding, so a strip's readouts stay inside the open part of its art): `--s-ink`, `--s-dim`, `--s-num`, `--s-body`, `--btn-ink`, `--btn-justify`, `--btn-font`, `--btn-track`, `--btn-case`, `--btn-motion`, `--btn-press`, `--tab-font`, `--tab-ink`, `--tab-ink-on`, `--title-font`, `--title-ink`, `--lbl-font`, `--num-font`; optional `--btn-align`, `--btn-pt` |
| `accent`, `warn` | Accent and warning colours |
| `title(t, m)` | Places a panel's title element `t` (CSS text) given the panel frame's margins `m` (top, right, bottom, left) |
| `plateText` | `{ left, top, width, height, font, color, sub }`, optional `shadow` (a CSS text-shadow, for letters over a busy ground): the name plate's live text field and text |
| `helmKeys` | `'ring'` with `helmR` (radius of eight keys round the centre of a 300 x 300 pad) or an array of eight `[x, y]` key centres, clockwise from north-west or north |
| `shield` | Three colours for shield strength: strong, middling, weak |
| `fonts` | CSS font strings to load before drawing, e.g. `'400 20px "Exo 2"'` (Google Fonts; the page's `<link>` must include them) |
| `view` | `{ title, body, upper }`: canvas fonts for the 3D view's target label and readouts, and whether they are upper case; optional `ink` and `dim` colours for them when the skin's own ink is dark |
| `build()` | Returns `FCK.fromPainters(nine, hero, icon, false)`: see below |
| `live` | `{ arrow(x, a), ssd(x, cx, cy, shields, col), dial(x, c, contacts), bracket(x, bx, by, bw, bh) }`, optional `helmCentre(x, cx, cy)` |
| `world` | Four strings: world view, bodies, technology, ships |
| `shapes`, `light`, `motion`, `type` | One line each |
| `palette` | Six `[name, hex, role, ideology]` rows |

## The kit pieces (`build`)

`FCK.fromPainters(nine, hero, icon, false)`:

- `nine[piece] = { size: [w, h], m: [top, right, bottom, left], draw(x, w, h), inset?: [x, y] }` for every name in
  `FCK.NINE`: `frame.panel`, `frame.inset`, `bar.track` (height 16; `inset` is where the fill sits inside it),
  `bar.fill` (height 10), `bar.fill.red` (the warning fill), `button.idle`, `button.hover`, `button.pressed`,
  `tab.idle`, `tab.active`, `strip.top`, `strip.bottom` (height 72, since 2026-10-31: a label stands over its bar inside the strip's open band), `divider` (height 10). Sizes and margins are at
  the 1920 x 1080 reference; `FCK.paint` draws at 2x for you. Detail lives in the margins (corners and the edges'
  ends); edges must stretch cleanly and the centre must be plain. Buttons are scaled uniformly to 48 px high and tabs
  to 40, so draw them at any height with sensible proportions.
- **Symbol bars (optional)**: `nine['bar.cell.off']`, `nine['bar.cell.on']`, `nine['bar.cell.warn']`, each
  `{ size: [w, h], gap, draw(x, w, h) }` (a rendered kit: pieces of the same names in its manifest, with `gap`). Supply all
  three and every bar becomes a row of the skin's own symbol in the track's groove: as many whole cells as fit at the
  groove's height, centred, the first ones lit (owner, 2026-10-31: "have different repeated symbols for them ... instead
  of the same boring progress bar"). The track may then be taller than 16 (its own image height is its slot height);
  the groove is its `inset`. Draw a lit and an unlit cell from the same seed, so lighting a cell changes only its light.
  Concord, Kethran and the Vos keep the plain fill.
- `hero[piece] = draw(x, w, h)` for every key of `FCK.HERO`: `nameplate` 460 x 56 (leave a field for live text),
  `helm.pad` 300 x 300 (eight direction keys and a centre key; arrows are drawn live by `live.arrow`), `ssd.bezel`
  460 x 300 (a frame round an empty field for `live.ssd`), `dial` 120 x 120 (a housing; `live.dial` draws the
  reading), `lamp.alert.green`, `.yellow`, `.red` 64 x 64, `crest` 96 x 96 (an original faction mark).
- `icon(name)` returns a canvas (use `FCK.paint(48, 48, draw)`) for every name in `FCK.ICONS`: helm, fire,
  shields, tractor, transport, science, comms, repair, map, settings.

Helpers on `FCK`: `P` (pi), `rng`, `hash`, `paint`, `png`, `rr` (round rect path), `noise1`, `wob`, `arcPts`,
`rrPts`, `trace`, and the Vos hand-drawn kit `ink`, `wash`, `hatch`, `dry`, `spiral`, `body`. A module may add its own
helpers inside its IIFE; it must not touch other skins.

## The rules every skin follows (owner, 2026-10-30; CLAUDE.md section 7)

1. No two skins share a button shape.
2. The palette comes from the faction's ideology.
3. Shapes, lighting, shading and icons come from how the faction thinks: its world view, bodies, technology, ships.

The faction write-ups are in `openspec/changes/console-skins/design.md`. Original designs only: nothing traced from
another game.
