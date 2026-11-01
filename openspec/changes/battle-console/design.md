# Design: battle-console

Mockup 2: the battle console in the Concord skin, around a stand-in battle. Page: `docs/mockups/battle-console.html`.

## 1. The reference screen, broken down

The owner's screenshot is the reference game's battle screen at 1024 x 768. Its parts, in our words, with the manual
page that describes each (Starfleet Command 2 manual, printed page numbers from its contents: "Take the conn" 108,
"Data displays" 113-116, "Tactical Officer MFD" 116-148, "Tactical Ship Schematic & System MFD" 150-169). The
screenshot and the manual set the shape only; nothing of their text, names or art is used (CLAUDE.md section 1).

| Reference element | Where | What it does (manual page) | Fleet Command |
|---|---|---|---|
| Officer panel with a column of officer buttons | Left column, top | One panel whose page is picked by a column of icon buttons: helm, repair, science, marines, weapons, comms, defence, mini map, fleet control, energy (116-148) | **Stations panel**: one panel, nine stations on a rail of tabs (helm, power, tactical, point defence, science, comms, repair, transport, fleet) |
| Energy page: POWER and SYSTEMS bars, rows of system, level, needed and allotted, OTHER and EXCESS | Shown in the officer panel | Allocates the reactor's output to movement, weapons, shields, tractors, sensors, other systems; excess charges the batteries (144-146) | **Power station**: seven rows of ten boxes (weapons, shields, engines, sensors, tractor, transport, repair), output, used, spare, battery |
| Ship schematic with shield arcs and weapon boxes, a column of system icons | Left column, middle | The ship from above: shield indicators, weapon icons, firing arcs, firing status, selecting weapons; the system buttons open shields, EW, transporters, tractors, shuttles (150-169) | **Ship panel**: the systems display (own or target) plus pages for shields, sensors, tractor and craft. Transport and cargo are one station, as the owner suggested |
| MULTIPLE, FIRE, OFFLINE, NORMAL, OVERLOAD | Left column, below | Fire the selected weapons; set their arming state (116, weapon pages 181-199) | **Weapons panel**: Fire (with the count that would fire now), Multiple or Single, Offline, Normal, Overload; expanded, the full mount table |
| Data readout and a fleet or comms schematic | Left column, bottom | Mini map (138) | **Radar row**: a dial of contacts round own heading, the count, Map (the tactical map, expanded), Next target, the crest |
| Hull integrity, phaser capacitor, target hull integrity, two clocks | Top strip | Data displays and mission timers (113-114) | **Top strip**: hull, weapon capacitor (in cells), target hull with its name, the clock, Pause, Menu |
| Speed with a slider, POWER and SYSTEMS, target labels, alert lamps | Bottom strip | Speed, energy, alert indicators (115-116) | **Bottom strip**: speed in boxes (one engine box is 30 m/s), power used of output, systems health, Next target, the alert lamp |
| Weapon status lines, an officer's message, target names with ranges | The 3D view | Tactical information: class or name, range, speed (111-112) | **The view**: the target bracket with name, class, range and speed; a readout of range, weapons in arc, charged, chance to hit; the last officer message; the target camera inset; Shield view and Target view toggles |

## 2. Layout

Reference 1920 x 1080. The console scales to the window's height (and to 1500 px of width at most), so panels keep their
size in reference pixels and only the strips and the view stretch (CLAUDE.md section 7: fixed panels, clipped, text
trimmed with an ellipsis).

| Region | Rectangle (x, y, w, h) | Content box |
|---|---|---|
| Name plate | 10, 6, 460, 56 | Ship and captain; Fleet n/m cycles the ship you command |
| Stations panel | 6, 68, 468, 420 | Rail 92 wide, nine tabs of 40; body 304 x 376 |
| Ship panel | 6, 494, 468, 276 | Five tabs across; body 396 x 186 |
| Weapons panel | 6, 776, 468, 160 | Two rows: Fire and Multiple (48), arming tabs (40) |
| Radar row | 6, 942, 468, 132 | Dial 120, inset, crest |
| Top strip | 480, 0, rest, 56 | |
| Bottom strip | 480, bottom 56, rest, 56 | |
| 3D view | 480, 56, rest, rest | |

**Expanded** (any panel, any size; survey U1, U2): the panel takes the left two thirds and the battle runs in the right
third, full height; in portrait the panel takes the top two thirds and the battle the bottom third. A tap on the battle,
Return, or Esc brings the console back. The stations rail stays, so a captain can move between stations while expanded.

**Phones.** Below 1000 x 600 the left column does not fit at a legible size (scaled to 390 px tall it would put 40 px
tabs at 14 px). So phones get their own reference: landscape 844 x 390, portrait 390 x 844, at one CSS pixel each.

| | Phone landscape 844 x 390 | Phone portrait 390 x 844 |
|---|---|---|
| Top strip | 44 high: ship, hull, capacitor, target, clock, Pause, Menu | 44 high: hull, capacitor, target, clock, Pause, Menu |
| Panel keys | A rail of four (Stations, Ship, Weapons, Map) down the left, 84 wide | A row of four under the top strip, 62 high |
| Quick keys in the view | Next, Port, Fire, Stbd, 48 high | The same, across the bottom of the view |
| Bottom strip | Speed, minus, ten boxes, plus, power, alert | Speed, minus, ten boxes, plus, alert |
| A panel | Opens expanded only | Opens expanded only |

Touch targets on phones are 44 x 44 CSS px or larger, except the speed boxes in portrait (about 18 px wide), which have
the minus and plus keys beside them. Nothing is reachable by hover alone: right click to lock has a tap equivalent
(tap a ship, then Lock target).

## 3. The Concord skin on this layout

Everything is drawn from the kit's named pieces (`console-skins` design): `frame.panel` for every panel, `tab` for every
tab row and the stations rail, `button` for keys (a pill: Signal idle, Sky hover, Lilac pressed or on), `bar.track` and
`bar.fill` for bars, `strip.top` and `strip.bottom`, `frame.inset` for the target camera and the radar inset, `divider`,
and the hero pieces `nameplate`, `helm.pad`, `dial`, `lamp.alert.*` and `crest`. Titles sit in a black notch that breaks
the panel's top arm, right-aligned, with the panel's Expand key beside them (LCARS's habit of making a frame's end the
button).

**Proposed kit additions** (survey B6), drawn in `docs/mockups/lib/concord.js` and marked as proposals there:

| Piece | Kind | Slot | Used by |
|---|---|---|---|
| `cell.idle`, `cell.lit`, `cell.warn`, `cell.locked` | 9-slice, 4 states | any x 28 (16 in the strips) | Energy allocation, ECM and ECCM, speed, the capacitor (CLAUDE.md section 7: allocation is discrete boxes) |
| `icon.power`, `.tactical`, `.pd`, `.fleet`, `.ship`, `.sensors`, `.cargo`, `.craft`, `.weapons`, `.pause`, `.menu` | hero, 48 x 48 | 48 x 48 | The phone rail, the power rows |

## 4. The stand-in battle

The page carries a small simulation so every control does something. It is the mockup's, not the game's design of
combat (that is its own change), but it follows the rules the game will:

- **One implementation of every rule** (CLAUDE.md 6.1): the chance to hit that the helm, the weapons panel and the view
  show is the same `hitChance()` that rolls the shot; the shield facing the systems display marks is the same
  `facingFrom()` that takes the damage; wingmen, enemies and the player's autopilot steer with one `steer()`.
- **Fixed steps** of 1/30 s, seeded randomness forked per subsystem (hits, damage, AI), ships in a stable order; a named
  shot replays the battle from its seed for 600 steps, so shots are the same every run.
- **Every input is an action** (`A.*`), called by keys, buttons, taps and the shots alike. Keys: A and D turn 15
  degrees, Q and E 45, W and S speed, X all stop, Space fire, Tab next target, P pause, M multiple, Esc menu or return.
- **Real time with pause** (G2): Pause stops the steps; every panel still takes orders.
- **The fleet** (G3): three Concord ships. The Fleet station sets each wingman's stance (attack, escort, hold, hunt),
  target rule (my target, nearest, weakest), range (close 4 km, mid 8 km, long 13 km), retreat (never, 50%, 25%) and
  fire (free, hold). Take command moves the player to that ship and its console; the ship left behind follows its own
  orders.

Units: the scene is 1 unit = 100 m; ranges in km, speeds in m/s, energy in energy points (E). The numbers (ranges,
charge times, costs, shield strengths) are placeholders written in the page's `WEAPONS` and `CLASSES` tables, as they
will be in `data/`.

## 5. Costs

Measured in the page (`window.MOCKUP_INFO`) on the battle shot at 1920 x 1080, SwiftShader: see the table in the
change's mockup record (`docs/artifacts/battle-console/README.md`). The console itself is DOM: about 40 images (the kit
painted once at 2x, a few hundred kilobytes), redrawn readouts ten times a second, no layout changes after a panel is
built. The 3D view is a stand-in and costs nothing that the game will keep; mockup 3 sets the real budgets.

## Open questions

| Id | Question | Recommendation (built in the mockup) |
|---|---|---|
| B1 | Speed: discrete boxes (one per engine energy point, 30 m/s each) or a slider? The owner's brief said "speed slider"; CLAUDE.md section 7 prefers boxes for whole counts | **Boxes**, because speed is bought with engine energy in whole boxes; the menu has a Boxes or Slider switch to compare |
| B2 | Targeting with a mouse: left click focuses and right click locks, or left click locks? | **Right click locks** (the owner's "point and right click"); left click or a tap focuses and shows a Lock target key |
| B3 | Nine stations in one panel on a rail of tabs, or fewer stations merged? | **Nine on one rail**: helm, power, tactical, point defence, science, comms, repair, transport, fleet |
| B4 | The helm pad: fixed steps (turn 15, 45, 90 degrees; speed one box) or hold to keep turning? | **Fixed steps**, which work as orders while paused (G2), and autopilot orders (approach, keep range, circle, evade) with Engage |
| B5 | Phone landscape: a rail of four panel keys and quick keys (Next, Port, Fire, Stbd) in the view, panels opening expanded; or the desktop column scaled down? | **The rail and quick keys**: the desktop column scaled to a phone is too small to read or touch |
| B6 | Add allocation cells and eleven icons to the kit (so every skin must draw them)? | **Yes**, they are needed by every console; Concord's are drawn now |
