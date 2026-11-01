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
| Stations panel | 6, 68, 468, 420 | A narrow column of nine icon tabs (48 wide, 40 high); body 348 x 376 |
| Ship panel | 6, 494, 468, 276 | A narrow column of five icon tabs; body 340 x 232 |
| Weapons panel | 6, 776, 468, 160 | Two rows: Fire and Multiple (48), arming tabs (40) |
| Radar row | 6, 942, 468, 132 | Dial 120, inset, crest |
| Top strip | 480, 0, rest, 56 | |
| Bottom strip | 480, bottom 56, rest, 56 | Speed (boxes with a handle), Power over Systems, Next target, alert |
| 3D view | 480, 56, rest, rest | |

**Expanded** (any panel, any size; survey U1, U2): the panel takes the left two thirds and the battle runs in the right
third, full height; in portrait the panel takes the top two thirds and the battle the bottom third. A tap on the battle,
Return, or Esc brings the console back. The stations rail stays, so a captain can move between stations while expanded.

**Phones: not mocked up yet** (owner, 2026-11-01: "lets first only care about the desktop experience FIRST", "DO NOT
MOCKUP MOBILE YET"). Every window gets the desktop layout, scaled, and the page is shot at 1920 x 1080 only. Phone
layouts (a rail of panel keys, quick keys in the view, panels opening expanded) are sketched in the page's code behind
`MOBILE = false` and are not part of this version; they come back as their own step when the owner asks.

**Tabs are icons** (owner, 2026-11-01: "lets focus on iconography instead of text for the different tabs", "text will
show as you hover over above these tabs, group icons into a narrow column"). The stations panel and the ship panel each
have a narrow column of icon tabs down their left side, Sky ink idle and black on the lit (active) tab. Pointing at a tab
shows its name above it in a lit pill; the panel's title names the open page, so the name is never hover-only.

**The ship you command sits below the centre** (owner, 2026-11-01, with the reference screenshot: "the ship you are
controlling should always be at a certain Y height (in screen coords) despite the zoom level. It shouldnt be centered,
it should be below center"): at 74% of the view's height from the top, at every zoom and orbit. The projection is
shifted (`setViewOffset`); the camera is never moved for it, so the player still owns the camera (CLAUDE.md section 8).

**Speed has a handle** (owner, 2026-11-01: "speed needs a slider handle"): the ten engine boxes are the track (one box
30 m/s, lit to the ordered speed, locked past the engines' power) and a lit pill handle on them is dragged to order a
speed in 5 m/s steps; it stops at the engines' limit. Arrow keys step it a box. This answers survey B1.

**Power over Systems** (owner, 2026-11-01: "power and systems need to sit on top of each other so we can see how much
power our systems are draining"): two bars stacked on one scale, the reactor's rating. Power is the output; Systems is
what the allocation draws. The gap between them is the spare energy that charges the battery; a drain past the output
turns Systems red. Systems' health moved out of the strip; it is in the repair station.

**Power and Systems as ticks** (owner, 2026-11-01: "can you show this as discrete marks/ticks?"): one tick per energy
point of the reactor's rating (22 for the cruiser). Power's ticks lit blue to the output; Systems' lit green to the draw,
red past the output. Proposed kit piece `cell.accord`.

**Alert condition** (owner, 2026-11-01: "can I get red, yellow, green alerts?"; the reference game's alert indicators,
SFC2 manual printed page 116): three lamps in the bottom strip, the kit's `lamp.alert.*` hero pieces, the set one lit
and the others dim, each named on hover. The captain sets it. Yellow raises shields; red raises shields, arms every
offline weapon and turns the sensors active; green changes nothing on its own. The battle starts at yellow.

**The Target box** (owner, 2026-11-01: "change this box to 'target' that way we dont need to toggle between the two on
the ship systems display"): the box at the view's top right, which was a target camera, now shows the locked target's
systems display, drawn by the same `drawSSD` as the ship panel: its shields (numbers once scanned), its mounts and the
facing it shows you, marked red. The ship panel shows only your own ship; its Own and Target switch is gone. The view's
Target key shows or hides the box.

**Drawn in the engine** (owner, 2026-11-01: "can you render this ui in engine as opposed to html? there are some issues
with html rendering that causes components to twitch and change sizes. (by engine I mean js engine)"). From version 7
the console is not HTML: one canvas over the 3D view is redrawn every frame in reference pixels, as the game's console
will be.

- **Kit pieces** are composed whole at their device-pixel rectangle (corners at their margins, every slice on whole
  pixels) and cached by piece and size, so no seam shows and nothing is resampled differently from frame to frame.
- **Text** is drawn at fixed sizes and trimmed with an ellipsis to its room; it never reflows anything.
- **Numbers** sit in slots that only ever grow (the widest value seen), so a changing number never moves what follows.
- **Input is immediate mode**: every control registers its rectangle as it is drawn; a press, a drag or a click is
  matched against the rectangles of the last frame. A press on the console never reaches the 3D view; elsewhere the
  view keeps its orbit, left-click focus and right-click lock. Names show above icon tabs and alert lamps on hover.
- The page's HTML is now two canvases and the link back to the tracker. Checked headless with real mouse input: the
  helm's +15 key turns the ordered heading by 15 degrees, Stop zeroes the speed, the speed handle drags to the engines'
  limit (180 m/s), the red lamp sets red alert, a power box sets weapons to 3, a coil drags home, plus on loop A raises
  its flow, a right click locks Ashkar, Expand and a click on the battle swap the layout.
- The dormant phone layouts were removed with the HTML; mobile comes back as its own step.

**Weapon glyphs, subsystems and their status** (owner, 2026-11-01: "we need some iconography for each weapon type, on
the side, show subsystems, as well. Tooltips: when I hover over subsystem or weapon box: show what subsystem name is,
health, status"). On the ship systems display (own and in the Target box, one drawing):

- **A glyph per weapon type**, in Concord's open circles and arcs: lance, a dot with a short beam either side (fixed: no glyph turns with its mount's arc);
  pulse torpedo, a dot in a ring; disruptor, two dots; missile, a capsule; point defence, a dot in three arcs. Its colour
  is its state (offline deep, charging Signal with the charge drawn round it, ready Accord, overload Lilac, disabled or
  destroyed Alert); a selected mount is ringed in Sky.
- **Subsystems** in two columns beside the ship, each an icon in a box: weapons control, shield generators, sensors and
  the reactor on the left; engines, tractor beam and transporters on the right. Active boxes are plain; any other status
  is outlined in its colour, Off is dimmed, Destroyed is darkened. New Concord icons: `engines`, `reactor`.
- **Hovering a mount or a subsystem** shows a card: its name, its health, its status (and a mount's charge).

**Then** (owner, 2026-11-01): "the shield side we are facing should have a white arc above it. the weapons should also be
icons here, instead of circle icons I prefer square with iconography inside", "shield facing should be hexagons here" (the
3D view, with the reference game's flat glowing facets), "going from red 1 layer -> yellow 2 layers -> green 3 layers",
"make sure weapon icons dont overlap!":

- The facing turned toward the enemy (yours toward your target, the target's toward you) has a **white arc** above it.
- Mounts are **square boxes** with the weapon's glyph inside: glyph colour for its state, the box outlined in its status
  colour when not Active, the charge as a bar under it, a Sky ring when selected, drawn as a solid plate under the box with the box's corner radius so the box's
  edge blends into Sky and no seam shows (owner: "Still gaps here on the weapon box", "Remeasure precisely";
  `tools/mockups/ring_seam.mjs` measures it: 0 at 1x and 2x). Mounts sit on a **grid** (owner, 2026-11-01:
  "They don't seem to align to a grid"): one cell is a box, its selection ring and a 3 px gap, centred on the ship's
  spine. Each mount takes the cell nearest its hull position, rounded alike on both sides so pairs stay mirrored; a
  taken cell sends it to the nearest free one, its own side first. Boxes line up in rows and columns and never overlap. The grid is drawn faintly behind the ship, clipped to the shield ring (owner: "Grid").
- **Shield strength**: in the 3D view as layers, strong (over 66 %) three green bands, middling two amber, weak one
  red; on the console one band per facing in the same colour (owner: "to avoid clutter. The UI itself should display 1
  layer. Where the game should display multiple"). An empty or lowered facing is a single dark line.
- The **3D shield display is a flat hexagon** in the ship's plane, an edge toward the bow, each facing a stack of one to
  three glowing bands (its layers); a hit flashes the inner band.

**The shield page** (owner, 2026-11-01: "this should be a shield diagram that I can pick what sides to reinforce", "and
adjust how much energy I want to allocate - same energy slider as the one found in the power menu"): a hexagon of the six
facings round the ship, each side drawn as its layers with its name and strength beside it; a click on a side reinforces
it (again to clear), the reinforced side outlined in Lilac, the side toward the target marked white; Shields up or down,
the regeneration and the reinforced side beside it; and under it the shields' row of energy boxes, the same control the
power station draws (`powerRow`, one implementation).

**Shield keys** (owner, 2026-11-01: "make shields like bigger buttons like in starfleet command where they were like
trapezoids, with the base pointing outwards, and the sections getting highlighted when selected for reinforcement"):
on the shield page the six facings are big trapezoid keys making a hexagonal ring round the ship, the wide side outward,
each tinted by its strength with its name and strength inside; the reinforced key is lit Lilac with a white edge,
pointing at a key lightens it, and the side toward the target is marked white on its outer edge. A key is hit by its
exact outline (a control may give its polygon), so neighbouring keys never steal each other's clicks. Then (owner: "those shield numbers
need to be inside the shield buttons", "or instead you can have a fill amount to show how much of that shield is charged",
"and show numbers when you hover over it"): each key fills outward from its inner edge by its charge, in its strength's
colour, with only its facing's name inside; pointing at a key shows a card with its strength, how charged it is and
whether it is reinforced.

**Solid status boxes** (owner, 2026-11-01: "Fill the icon boxes fully. Please. And communicate icon system status with
different colors. Show that as the ship gets hit and takes damage"): every weapon and subsystem box is filled solid with
its status colour (the table below), its glyph or icon black on it (a destroyed box dark red with a red edge and a red
glyph); a selected weapon has a Sky edge instead of a separate ring; the charge stays the bar under a weapon box. The
boxes change as hits land. The menu's Mockup section has **Take a hit**, which puts three heavy hits on your ship from
the nearest enemy so the colours can be watched changing (a mockup tool, not a game control).

**The five statuses** (owner's definitions; they drive the combat change's rules, which are not built here):

| Status | Means | Colour |
|---|---|---|
| Active | On and working | Accord |
| Damaged | Hit, still working; will be disabled if hit again | Amber |
| Disabled | Damaged past working; usually can be repaired | Alert |
| Off | Turned off by the captain, or not enough power to run it | Lilac, dimmed |
| Destroyed | Completely off line; cannot be restored | Alert, darkened |

In the mockup a status is read from the stand-in battle's numbers (health 0 destroyed, under 35 % disabled, under 100 %
damaged; off when switched off or given no power). The stand-in still repairs a mount from 0, which the Destroyed rule
forbids; the combat change sets the real rules.

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

**No seams between slices** (owner, 2026-11-01, with a close-up of the tabs and keys: "see these slice lines?"). CSS
`border-image` drew each of a piece's nine slices separately, and at the console's fractional scale their edges were
smoothed on their own, leaving faint lines where slices meet. The page now composes each kit element whole at its
device-pixel size (`nineImage()`: corners at their margins, edges and centre stretched, every slice on whole pixels),
one image per state, cached by piece and size. The game's own 9-slice (Godot's `NinePatchRect`) must be checked for
the same fault at fractional scales; that check goes in the layout test.

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
| B1 | Speed: discrete boxes or a slider? | **Decided** (owner, 2026-11-01: "speed needs a slider handle"): a handle dragged along the engine boxes |
| B2 | Targeting with a mouse: left click focuses and right click locks, or left click locks? | **Right click locks** (the owner's "point and right click"); left click or a tap focuses and shows a Lock target key |
| B3 | Nine stations in one panel on a column of icon tabs, or fewer stations merged? | **Nine on one rail**: helm, power, tactical, point defence, science, comms, repair, transport, fleet |
| B4 | The helm pad: fixed steps (turn 15, 45, 90 degrees; speed one box) or hold to keep turning? | **Fixed steps**, which work as orders while paused (G2), and autopilot orders (approach, keep range, circle, evade) with Engage |
| B5 | (Deferred: no mobile mockup yet.) Phone landscape: a rail of four panel keys and quick keys (Next, Port, Fire, Stbd) in the view, panels opening expanded; or the desktop column scaled down? | **The rail and quick keys**: the desktop column scaled to a phone is too small to read or touch |
| B6 | Add allocation cells and eleven icons to the kit (so every skin must draw them)? | **Yes**, they are needed by every console; Concord's are drawn now |
| B7 | Power and Systems ticks on reactors from 12 E to 60 E and more? | **Fixed slots (32)**, 1 E a tick to 32 E, then 2 E, then 5 E; slots past the rating outlined and room for overdraw shown red (the mockup has one tick per E today) |
