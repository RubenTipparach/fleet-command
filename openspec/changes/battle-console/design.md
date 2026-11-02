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
  `tools/mockups/ring_seam.mjs` measures it: 0 at 1x and 2x).
- A cooling mount shows the **seconds until ready** in its box in place of the glyph (owner, 2026-11-01: "When weapon
  cool do show x.x seconds to cool down. Target 2 decimal places so either xx or x.x ... we'll scale stuff down to fit if
  needed"): `(1 - charge) x charge_s`, the rate the simulation charges by, one decimal under 10 s (rounded up, so never
  0.0 while cooling), whole seconds from 10 s, three figures scaled to fit. It replaces the charge bar under the box.
  While it cools the box is a **dark shade of its status colour** (34 % brightness) with the figures in the bright
  status colour, **filled from the bottom in the bright colour as it charges** (owner, 2026-11-02: "show progress like
  shade 50% a bright color as it is recharging"), the figures white with a dark outline so they read on both; ready, it
  returns to the full colour with the glyph (owner, 2026-11-01: "When cool down the status
  color should be a darker shade to indicate that").
  The hover card adds "Ready in".
- **Pointing at a mount outlines it in white**, outside its selection ring (owner: "If mouse hover over highlight
  outline"). Each mount is **one group** (hover outline, ring, status colour, glyph or seconds) drawn last, above the
  schematic, grid and subsystems (owner: "Outline and icon and icon status color should be grouped together and always
  show above ship schematic layer"). Mounts sit on a **grid** (owner, 2026-11-01:
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

## 4b. The helm by pointing

Owner, 2026-11-01: "I need to be able to turn my ship, clicking left to turn ship left, click right of the ship to turn
it right, see how this is designed in Fargo Game jam repo, or in the federation repo".

Both reference games steer by pointing. The-federation (`src/ui/combat_screen.gd`, `_helm_click`) makes a left click in
the battle view the helm: the click is projected onto the battle plane and the ship is ordered onto the bearing from its
own position to that point; an earlier version only nudged the heading by a fixed step toward the side clicked, and was
replaced because pointing somewhere did not take the ship there. The right button is the camera and targeting (drag to
orbit, click to lock). Its ship rig draws a turn arc, the signed turn still to make (`TurnArc`). Fargo (`dark-neb`,
`main.lua`) does the same with a right click raycast to the ground plane (`MovementSystem.set_target_heading`), turning
at a fixed rate.

The mockup takes the-federation's way, on the buttons it already has:

| Input in the battle view | Does |
|---|---|
| Left click on empty space | **The helm**: the ship comes onto the bearing of the point clicked (left of the ship comes left, right comes right, as far as the point asks); the same heading order as the pad's turn keys, which also takes the ship off autopilot; the log says "Come to 307" |
| Left click on a ship | Focuses it (unchanged) |
| Right click on a ship | Locks it (unchanged) |
| Left drag on or near the helm ring (within 160 m of its 450 m radius) | **Steers**: the ordered heading follows the pointer round the ring (owner, 2026-11-02: "I want to be able to click and drag to rotate ship. clicking near the ring should rotate my ship"); the ring brightens under the pointer, with a grab cursor |
| Right drag | Turns the camera ("otherwise I can pan stuff around using right click"); a left drag elsewhere does nothing to the camera |
| One finger (touch) | On the ring, steers; anywhere else, turns the camera ("make sure that on mobile i can still rotate camera by regular touch"); two fingers zoom |
| A click on your own ship | Nothing: it has no direction |

On the ship a faint ring (450 m) carries the **turn arc** from the heading to the ordered heading, with a tick at the
order. The mark that showed where the helm was clicked is gone (owner, 2026-11-02: "should get rid of plus sign").

## 4a. Mines and the tractor contest

Owner, 2026-11-01: "Mine laying should have a cool down system of 3 seconds. Then let's review tractor beam system.
Initial tractor beam grip is at about 50%. Tractor beam will have an amount of time that ticks down.. let's say 10
seconds. By this time, each ship will have picked what the next beam value will be. A higher value from each ship will
result in grip increasing or decreasing. Grip decreasing to 0 or below the ship being restrained breaks free."

**Mines.** After a mine is dropped the next waits 3 s (`MINE.cooldown_s`). The transport station's Drop mine key shows
"Next mine in 2.4 s" in the weapons' cooldown format (x.x under 10 s) and is disabled until then. A mine still arms 3 s
after it is dropped and goes off within 1.2 km of an enemy.

**The tractor contest** (`TRACTOR` in the page, every number data). Owner, 2026-11-01, second pass: "Hold and break
should be only two options. If enemy tractor beam module is down or is less than .5x of the breaking ships strength.
The ship breaking the beam then becomes the holder. The holding ship can break at anytime by clicking break. Tractor
beam does take 10 seconds to power up".

| Step | What happens |
|---|---|
| Two keys | **Hold** and **Break**, nothing else (Off, Pull and Push are gone). The beam only holds: it damps the held ship's speed, 80 % a second at full grip |
| Power-up | Hold powers the beam up for **10 s** ("Powering 6.4 s" on the key); it pauses while the tractor has no energy. Powered, it takes the target as soon as it is inside 3 km, at a grip of **50 %** |
| A round | **10 s**, counted down on the panel ("Round 2 ends in 6.6 s") |
| The picks | Each ship's **beam value** is the energy in its tractor when the round ends, times its tractor's health (and the brownout share). The player picks with the tractor panel's energy boxes, to hold or to resist; an AI crew picks once a round, taking energy from its weapons when it has none spare. **Neither side sees the other's pick until the round ends** (B10) |
| The result | The grip moves toward the higher pick, **10 % per energy point of difference** (5.0 E against 4.3 E: +7 %), between 0 and 100 % (B8) |
| Breaking free | A grip at **0**, or the holder's tractor going down (no energy, disabled or destroyed), frees the held ship |
| The beam turns | When the held ship breaks free and the holder's tractor is **down**, or the holder's value is **under half** the breaker's, the breaker **takes hold of the holder** at once, at 50 %, with no power-up (B12). Otherwise it is simply free (3 E against 5 E: free; 2 E against 5 E: the beam turns) |
| Break | The holder can **Break at any time**, also during the power-up. A ship that broke free cannot be taken by the same holder again for one round, 10 s (B9) |
| Held | When an enemy holds you, the panel shows "Held by Vorrak · 50 %" in red with its round and the last picks from your side; Hold and Break are disabled, and you resist with the energy boxes |

The energy in the tractor is drawn every second like any other system, so holding someone, or resisting, costs weapons or
shields. In the 3D view the beam is a pale line from holder to held, brighter as the grip tightens. An enemy holds you
only by turning your beam; whether enemies should start a hold on their own is B11.

## 4l. The mine placement preview (version 40)

Owner, 2026-11-02: "when placing mines, need to draw radius of mine when mouse hovers over the battle field, mine
placement will be yellow, but turns red when it is in radius of any ship. It will grey if unable to be placed, and the
letters 'invalid position' will show. invalid position is simply outside transporter range. the mine range display is
like an animated bullseye radiating outwards", and "mine range is different than mine transport range just to be clear".
While placing a mine, two ranges show and are named apart: the **transporter's reach** (4 km, the dashed ring round your
ship: how far out a mine can be placed) and, under the pointer on the battle plane, the **mine's range** (1.2 km: how
close a ship must come to set it off) as a bullseye whose rings radiate outward from its centre every 1.4 s. **Yellow**
("Mine range 1.2 km") where it can go; **red** ("Ship in mine range") when any ship is inside that range; **grey**
("Invalid position") outside the transporter's reach, where a click places nothing.

## 4k. The explosion, and the helm's second half (version 36)

**A ship's death** (owner, 2026-11-02: "can we design a big cool looking explosion like this?", with a shot of a starship
blowing apart in the reference game; our own layers, not its art): a white-hot **core** that flashes to five ship lengths
and fades through pink in 1.6 s; a flat blue **shockwave ring** racing out across the battle plane to nine ship lengths in
2.4 s, easing out; a **plasma cloud** of six magenta, violet and blue puffs that swell and drift for 5 s; 60 **sparks**
flung out for 1.8 s; and the hull broken into nine dark **chunks** that tumble away, each burning for a while, over 8 s.
The light is kept out of tone mapping so its colours stay saturated. A meltdown uses the same explosion, larger. The
menu's **Destroy target** key shows it on demand; the shots `explosion` (0.7 s) and `explosion_late` (2.2 s) hold it.

**The helm's target section takes the left half** (owner: "shrink the target maneuvers so it takes up half the panel, that
way we can use the other half for more maneuver types like hit and run, to move attack, and move away. this will require
a slider with two handles to pick min and max range"). The right half, **Runs and evasion**: **Hit and run** runs in at
full speed to the minimum range, then away to the maximum, and again, its range set by a **two-handled slider** (the
handle nearest the pointer moves; at least 0.5 km apart); measured with 3-6 km: in from 9.3 km, out at 3.0, in at 6.0,
repeating every 45-50 s. Its icon (version 37, owner: "hit and run icons sucks") is a strafing pass: a path rising toward a
target, a tight turn just short of it, and back down and away. **Evasive manoeuvres** moved here from the HET pad (owner: "evasive maneuvers technically isn't
an HET ability btw, so clicking it is a toggle and doesn't consume HET ability"): a toggle; it never spends the HET
chance (it still makes a HET likelier to fail while it is on, after the reference game). The pad's bottom row keeps the
emergency stop (under free heading) and the free-angle HET.

## 4j. Hull bars in the view, and steering in the expanded view (version 34)

Owner, 2026-11-02: "can you show enemy ship health in this? and my ship health too? always show current targeted enemy
health in this please? (even in the un-expanded view)", and "I cant turn the ship in expanded view either". The 3D view
draws a **hull bar** (92 px, green above 60 %, amber above 30 %, red below, with the percentage) under the targeted
enemy's bracket and name, and under your own ship, both docked and in the expanded view's strip; the fraction is the
same one the top strip's hull bars show. In the **expanded view** the helm ring is drawn and steers by dragging it, as
docked; a click elsewhere in the strip still returns the panel.

## 4h. The helm, rebuilt (version 33)

Owner, survey B4: "The original game had free heading, intercept, follow+match speed, orbit (ccw/cw toggle). High energy
turns, hard left, hard right, 180. Please look at screenshots I submitted or manuals linked for nav iconography and
layout"; then, 2026-11-02: "I want buttons that express types of maneuvers towards the target or toggles for self, or
triggers for High Energy turns".

**The reference** (Starfleet Command 2 manual): normal turns are a left click in the 3D view, a click on an enemy sets a
course for it, and an arc shows the turn still to make (printed page 17); special movement lives on the Helm Officer
display (page 117): emergency deceleration (stop as fast as possible, then a delay before moving), erratic manoeuvres
(harder to hit and to hit with; no shuttles, fighters, missiles, mines or tractors; turns slower; high energy turns about
17 % likelier to fail; pages 19 and 118), six high energy turn keys (90 left and right, 135 hard left and right, 180,
and a free angle set by clicking the view; page 119), the HET chance of success shown and recovering slowly after each
use (page 120), orbit target, intercept target (the helm steers, the captain keeps the speed) and normal manoeuvring
(page 120). Taken in shape, not art: our own glyphs and layout.

**The panel**, three groups of icon keys (names on hover, the order in force lit green):

| Group | Keys |
|---|---|
| Toward the target (toggles; pressing the lit one returns to free heading) | **Intercept** (leads the target; the captain keeps the speed), **Trail and match speed** (sits 600 m astern, matching its speed), **Approach and hold distance** with its **distance slider** beside it (1-12 km in 0.5 km steps; closes, then holds within 10 %), **Orbit** (round it at 4 km; the captain keeps the speed) with its **CW / CCW switch** beside it (owner, 2026-11-02: "also need approach and maintain distance, add a config button with a slider to change distance. Combine orbit button with cw button, where cw button is a toggle slider") |
| High energy turns (triggers) | A 3 x 3 pad: hard left 135, 180, hard right 135; left 90, **Free heading** (the middle key, cancelling helm orders), right 90; the angle printed in each turn key's corner |
| Self (toggles and triggers) | **Erratic manoeuvres** (toggle), **Emergency stop**, **HET free angle** (then click the view) |

Beside the pad: **To hit** (the best chance among the selected weapons of hitting the locked target now, the number
the shot rolls), **HET** (the chance the next high energy turn succeeds; during a breakdown the seconds left), **Heading**
(the ship's heading in degrees, 000 up the map) and **Speed** (the ordered speed, m/s); "Hdg" and "Order" were renamed
in version 39 for being cryptic. The helm ring,
pointing and the speed strip still steer by hand (free heading).

**The rules** (`HET`, `HELM` in the page): a HET turns at 240 degrees a second and costs 3 E from the battery; its
chance starts at the class's best (0.95 less the hull's length over 2 km: 79 % for the Resolute, 87 % for a frigate),
drops 20 points each HET and recovers 1 point a second; erratic manoeuvres take 17 points off. A failure is a
**breakdown** (version 39, owner, 2026-11-02: "failure to do so damages impulse engines, and knocks the ship out for 15
seconds, unable to move or rotate"): the impulse engines lose 30 % of their health and the ship is dead in space for
15 s, no speed and no turning; the HET readout counts it down in red ("Out 12 s"). Erratic manoeuvres: hit chances to and
from the ship x 0.75, turn rate x 0.8, no launches, mines or tractors. Emergency stop: five times the deceleration, then
the engines hold for 4 s.

Measured (headless): intercept closed on the Vorrak; orbit settled near it; trail caught it up; a 90 left HET turned 90
degrees in under a second (79 % to 60 %); six back to back all broke down; the emergency stop went from 240 m/s to 0 in
a second and held; erratic manoeuvres refused a launch.

**Icons like signage** (version 35; owner, 2026-11-02: "please fix these icons to look like actual icons I'd see in an
airport or something, they are way too weird looking I dont even understand them"): thick strokes and filled triangular
heads; the turns are road-sign turn arrows (a stem, the bend, a short run, the head), 180 the U-turn sign, free heading
the straight-ahead arrow, intercept an arrow into a target, trail two ships in line, approach and hold an arrow stopped
at a bar before a target, orbit a circular arrow round a dot, erratic (evasive) a bold three-stroke zigzag climbing to a head aimed along its last stroke (version 38, owner:
"review evasive, i cant read the arrow"), emergency stop an octagon with
STOP, the free-angle turn a turn arrow to a crosshair. Measured: holding 3 km, the Resolute closed from 7.5 km and held
3.2-3.3 km.

## 4i. Target bracket, shields, weapons page, toughness (version 33)

- **The target bracket is bright red** (owner, 2026-11-02: "selected enemy target should be bright red, and if torpedo or
  missile lock is possible to have double frames like in ace combat"): four red corners; when a torpedo or missile mount
  is charged with the target in its arc and range (`seekerLock`, the rules the shot uses), a second, pulsing outer frame
  and SEEKER LOCK. The off-screen marker of the locked target is red too.
- **Shield keys** (owner: "use a inner gradient shading on shields, like a fresnel shading almost for each shield face,
  also center text, they should be middle of the shield object"): each face darkens toward its middle and brightens to
  its rim in its colour; the facing's name sits at the middle of the key.
- **The expanded weapons panel** (owner: "show weapons as they are on the ship in the expanded menu, then when you hover
  over them and click on them, detail menu appears on the lower half. Also add a fire button underneath each weapon, and
  also bold the weapon box with a dashed white outline when they are ready to be fired"): its upper half is the ship
  systems display (the one component), its grid rows taller so a **Fire** key sits under each mount; hovering a mount
  names it, clicking it fills the lower half with its details (status, health, arc, range, damage, charge, ready in,
  cost; its own Offline, Normal, Overload; the target, chance to hit, in arc, in range; in the volley; Fire). On every
  ship systems display a mount **ready to fire** (charged, the target in its arc and range) has a bold dashed white
  outline. The old table is gone.
- **Ships four times tougher** (owner: "make ships in this demo like 4x tankier, the battle loop is over too quickly"):
  every class's hull x 4 (`DEMO.tough_x`), the disguised shuttle's charge with it. The battle scene, fought on intercept
  with the guns, now lasts about 6 minutes.

## 4g. Stars behind ships, and space dust (version 31)

Owner, 2026-11-02: "stars should draw behind ship always btw, it doesnt make sense if they draw in front. we should also
add space dust as it helps show motion as the ship moves". The sky (nebula, far stars and the parallax stars) is drawn in
its own pass first and the depth cleared, so no star ever lies over a ship. **Space dust**: 700 faint motes in a box 6 km
across and 1.4 km deep round your ship, wrapping so the field never ends, each drawn as a short streak back along your
velocity (a quarter of a second of travel), so speed and heading read even against a still sky; the motes are in the
battle pass, so they pass in front of and behind ships as they lie. (Mockup 3 sets the final look.)

## 4f. The expand key (version 28)

Owner, 2026-11-02: "expand button should look like this, be a circle, small", with a square icon of two diagonal arrows
pointing out to its corners. Every panel's Expand pill becomes a small round key (21 px across after the owner's "it should be 20% smaller", a
29 px touch area)
beside the panel's title: two arrows pointing out to the top-right and bottom-left corners; on an expanded panel they
point in, and the key returns the panel. Its name (Expand, Return) shows on hover, as every icon key's does. The title's black plate starts at the
frame's top edge and stops at the content (22 px), so it breaks only its own frame's top arm, never the gap or the panel
above (owner, 2026-11-02: "whatever background is around 'systems - resolute' is overlapping the upper border").

## 4e. The camera lock (version 27)

Owner, survey B2: "Lock camera on target is automatic when you first right click. Clicking and dragging camera around
frees up the lock..you can lock again under the enemy ship systems view box in the upper right corner"; then, 2026-11-02:
"lock target camera is not working, camera needs to yaw to face the enemy target, this is how it works in my reference
repos" and "we'll want to smooth that rotation in this repo".

Fargo's `CameraSystem.update` (dark-neb `src/systems/camera_system.lua`) yaws the locked camera each frame toward the
bearing from the ship to the target at a fixed rate, keeping the pitch; a drag unlocks it. The mockup does the same with
a **smoothed** turn: the yaw is a critically damped spring (natural rate 2.5 per second, at most 90 degrees a second),
so it eases in and out, never overshoots, and follows a moving target without jitter. The chase camera ends behind the
ship with the target ahead, beyond it; pitch and zoom stay the player's. Locking a target (right click, or the Lock target
key) locks the camera too; a **drag** frees it (zoom does not); a **Lock camera** key under the Target box locks it
again, and reads "Camera locked" while it holds. The player always has the camera back with one drag (CLAUDE.md 8: the
camera is never taken from the player against their will; this lock is theirs to set).

## 4c. Test scenes

Owner, 2026-11-01: "I also want different test scenes, I want a mine laying test scene, the player can transport mines to
different locations with in a certain radius of the shipp. I tractor beam test scene, with asteroids nearby to smash
ships into. A boarding ship test scene, to test out the transporter ship boarding feature. You can have boarding parties
hit and run on various subssytems (bombinbing or shooting them) or have a scene where you board and capture a ship".

The menu has a **Test scene** row: Battle (the original), Mines, Tractor, Boarding. A **scene selector** sits in the
view's top left as well (owner, 2026-11-02: "I would like a scene selector"): a Scene key naming the scene in play,
opening the list of scenes; a pick starts it, a click elsewhere closes the list. Each is a seeded world in the page's
`SCENES` table (ships, asteroids, a setup); Restart replays the scene picked. Test scenes do not end when the last enemy
goes (a captured ship should be seen joining the fleet). The designs follow the-federation (docs/01 section 8,
docs/13 sections 3 and 6, `src/sim/boarding.gd`).

| Scene | Set-up | What it tests |
|---|---|---|
| **Mines** | The Resolute stopped, 8 mines; two Kethran frigates on lane runs (full speed straight past, back again from 12 km out), holding fire | **Mines placed by transporter**: Transport, Mines tab, Place mine, then a left click in the view puts a mine there if it is inside the transporter's reach (4 km, a dashed ring while placing). It spends the transporter's charge and the 3 s mine cooldown. Drop mine astern stays. A mine does 30 (was 20) when a ship comes within 1.2 km |
| **Tractor** | The Resolute facing away from the Vorrak, a rock between them, a field of eight asteroids; the Vorrak holds still and holds fire | **Asteroids and the tow**. A rock is solid: touching it costs 14 x the impact speed over 150 m/s (at least 35 % of that) on the facing that met it, cuts speed to 30 %, once per 1.5 s per ship. The held ship is **tethered** at the distance the beam took hold at, drawn back by the grip every step (rigid at 100 %), so running drags it, and a rock in the way is hit at the speed it is dragged (the Vorrak at 172 m/s: 16 damage) |
| **Boarding** | The Rattlejaw crippled 1.9 km off: shields down, engines dead, one gun left, 6 marines | **Raid**: pick one of the target's subsystems (its keys, coloured by status), Shoot or Bomb, Send raid: 2 marines for 6 s. Shoot takes 40 % off the system and loses each marine 1 time in 5; Bomb takes 80 % and loses each 1 time in 2. **Board**: every marine goes; a volley every 2 s, each marine one shot at 50 %, both sides at once; defenders are its marines plus 2 armed crew. All defenders down: the ship strikes its colours, joins your fleet on Escort with the survivors as prize crew, and can be commanded. Recall brings a party home |

Raids and boarding need what the-federation's `beam_check` needs: the target inside **transporter range (4 km)**, its
**facing toward you at 0**, the transporter **charged**, and marines to send; the panel shows range and that facing's
shield in green when met. The transporter recharges from its power (Power station), so energy spent there is the cost.

## 4d. Transporter modes, crews, and four more scenes (version 25)

Owner, 2026-11-01: "transport vip scene using inventory transporter instead of combat transporter. Another one to
transport goods onto a planet. A delivery mission from a station to a planet. The vip one is similar but you transport
him on a ship with Marines and he works to capture a station. Transporters ui have 3 modes - capture transport mode,
target hit and turn mode, and inventory transporter mode. Shuttle scene - a disguised shuttle leaves your ship and
suicide bombs a station. Fighters scene. Deploy fighters to take down another ships defenses, and fire a missile barage
to take it down." With the survey answers B14 (marine types), B15 (what a capture needs) and B16 (reeling).

**The transport station's three modes**, each a tab, with what the combat transporter needs (range, the facing's
shield) under the first two:

| Mode | What it does |
|---|---|
| **Capture** | A boarding party picked by role with count keys: combat marines, medics, engineers, hackers, pilots (each against how many are aboard). Board sends it; a party aboard can be **reinforced** or recalled. Volleys every 2 s: each combat marine fires at 50 %; a **medic** saves a casualty at 50 %; a **hacker** locks one of the guard out at 50 %; the **VIP** talks one defender down at 50 %. With the defenders down, the ship is taken only if an **engineer**, a **pilot** and a **hacker or the VIP** are aboard; otherwise the deck is held and the log names who is missing |
| **Hit & run** | The target's subsystems as keys, Shoot or Bomb, and a squad of **combat marines** and **demolition marines**. Shooting takes 20 % off the system per combat marine standing (at most 60 %); bombing takes 80 % if a demolition marine lives to set the charge. Demolition marines cannot defend themselves: each is lost at 60 % divided by (1 + escorts), so one alone sets the charge about 4 times in 10, and with two marines escorting about 8 in 10 (measured 3 and 7 in 10) |
| **Inventory** | The inventory transporter: a destination (friendly ships, stations and planets, nearest first; the nearest in reach is picked when the one chosen is out of reach), then **goods** (5 crates a beam, Take or Send), the **VIP** (Take or Send) and **mines** (Place mine by transporter, Drop astern). Friendly destinations need no downed shield |

Every beam spends the transporter's charge; it recharges from the transporter's power. A ship's specialists are a
`team` in its class (the Resolute: 12 marines, 4 demolition, 2 medics, 3 engineers, 2 hackers, 2 pilots).

**Reeling** (B16): while holding, Reel in and Let out move the tether 200 m a press; it changes at 150 m/s at full grip
times the holder's hull over the held ship's (at most 1), so a light ship holding a heavy one barely moves it.

**Stations and planets.** A station is a ship that does not move or turn (class `SB`: a ring on spokes, lances all round,
two point-defence mounts). A planet is a sphere below the battle plane, solid like a rock, its name and any delivery
shown over it; transporter reach to a planet is measured to its surface.

| Scene | Set-up | Played through (measured) |
|---|---|---|
| **Delivery** | Harbor Nine (friendly station, 20 crates) 1.4 km off; Tessaly 14 km away needs 10 | Take 5, Take 5, fly (67 s at 180 m/s), Send 5, Send 5: "Tessaly: delivery received in full" |
| **VIP** | Envoy Saros aboard; the troop ship Corrin (14 marines, engineers, a pilot, no hacker) 0.8 km off; Varn Outpost (Kethran station, weak shields that do not regenerate, most guns out) 7 km away | Send the VIP to the Corrin; it closes on the outpost and, once a facing is down (the Resolute's guns), boards with its company and the envoy; the envoy is the specialist and talks defenders down; the outpost strikes its colours with the envoy aboard |
| **Shuttle** | Three disguised shuttles; Varn Outpost 10 km away at full strength | Each flies at 500 m/s as civilian traffic, through the shields, and goes off against the hull for 110; inside 1.5 km the crew may see through it (25 % a second): two of three got through (hull 260 to 40). Shown in the view as "SHUTTLE · DISGUISED" |
| **Fighters** | Six fighters, 12 missiles; the Ironhold with three point-defence mounts 6 km away | A barrage first: all 6 missiles intercepted. Fighters on the **Defences** mission strafe point defence, then the guns, from inside the shields: all three mounts out in 25 s. A second barrage: 6 of 6 land (hull 105 to 66) |

Barrage: 6 missiles, one every 0.2 s, each hitting at 85 % unless point defence takes it. Craft panel: fighters
(Attack, Defend, Defences; Launch, Recall), Barrage and Shuttle.

## 5. Costs

Measured in the page (`window.MOCKUP_INFO`) on the battle shot at 1920 x 1080, SwiftShader: see the table in the
change's mockup record (`docs/artifacts/battle-console/README.md`). The console itself is DOM: about 40 images (the kit
painted once at 2x, a few hundred kilobytes), redrawn readouts ten times a second, no layout changes after a panel is
built. The 3D view is a stand-in and costs nothing that the game will keep; mockup 3 sets the real budgets.

## Open questions

| Id | Question | Recommendation (built in the mockup) |
|---|---|---|
| B1 | Speed: discrete boxes or a slider? | **Decided** (owner, 2026-11-01: "speed needs a slider handle"): a handle dragged along the engine boxes |
| B2 | Targeting with a mouse: left click focuses and right click locks, or left click locks? | **Decided** (survey, 2026-11-01): right click locks, and the first right click also locks the camera on the target; dragging the camera frees it; it locks again from the Target box (top right). Built in version 27 (section 4e) |
| B3 | Nine stations in one panel on a column of icon tabs, or fewer stations merged? | **Decided** (survey, 2026-11-01): nine on one rail |
| B4 | The helm pad: fixed steps (turn 15, 45, 90 degrees; speed one box) or hold to keep turning? | **Decided** (survey, 2026-11-01), the owner's note: the reference game's helm had free heading, intercept, follow and match speed, orbit (clockwise or anticlockwise), high-energy turns, hard left, hard right and 180; lay it out from the owner's screenshots and the manual's nav icons. Built in version 33 (section 4h) |
| B5 | (Deferred: no mobile mockup yet.) Phone landscape: a rail of four panel keys and quick keys (Next, Port, Fire, Stbd) in the view, panels opening expanded; or the desktop column scaled down? | **Decided** (survey, 2026-11-01): the rail and quick keys (mobile still deferred) |
| B6 | Add allocation cells and eleven icons to the kit (so every skin must draw them)? | **Decided** (survey, 2026-11-01): yes, but not started until the Concord master UI is final |
| B7 | Power and Systems ticks on reactors from 12 E to 60 E and more? | **Decided** (survey, 2026-11-01): a different number of ticks per ship, the width of a tick scaled so the row fits. *Queued* |
| B8 | Tractor: how far does the grip move after a round? | **Decided** (survey, 2026-11-01): 10 % per energy point of difference, as built |
| B9 | Tractor: after a ship breaks free, how soon can the same holder beam it again? | **Decided** (survey, 2026-11-01): one round, as built |
| B10 | Tractor: does either side see the other's pick before the round ends? | **Decided** (survey, 2026-11-01): hidden until the round ends, as built |
| B11 | Tractor: should enemy ships start a hold on their own (power up and take you), not only by turning your beam? | **Decided** (survey, 2026-11-01): yes, enemies start holds on their own. *Queued* |
| B12 | Tractor: when a beam turns, does the new holder start at 50 % at once, or power up for 10 s first? | **Decided** (survey, 2026-11-01): at once at 50 % ("This is if the tug of war is 2:1 in the breaking ships favor, this can happen!"), as built |
| B13 | Mines placed by transporter: how far out? | **Decided** (survey, 2026-11-01): 4 km, as built ("We'll play around with this range") |
| B14 | Raids: Shoot (40 % off, 1 in 5 marines lost) against Bomb (80 % off, 1 in 2 lost)? | **Decided** (survey, 2026-11-01): "It depends the type of marines you send. Demolition marines cannot defend themselves..so a boarding party must be well rounded to actually successfully bomb the ship." Built in version 25 (section 4d) |
| B15 | A captured ship: joins the fleet at once, or is a prize to hold and tow home? | **Decided** (survey, 2026-11-01): joins the fleet; a capture needs "combat marines, medics (optional), engineers, specialist (hackers), and a pilot (or nav officer)". Built in version 25 (section 4d) |
| B16 | The tow: a tether that holds the distance (dragging the held ship, rigid at full grip)? | **Decided** (survey, 2026-11-01): yes, "distance is adjustable at any time by the holder ... based on difference in ship mass". Built in version 25 (Reel in, Let out) |
| B20 | High energy turns: 240 degrees a second, 3 E, a chance that drops 20 points a turn and recovers 1 a second, a 6 s breakdown on failure? | **As built** (section 4h) |
| B21 | Erratic manoeuvres: hit chances x 0.75 both ways, turn rate x 0.8, no launches, mines or tractors? | **As built**, after the reference game's restrictions |
| B22 | Ships four times tougher for the demo: keep it as the game's scale, or a demo setting only? | **A demo setting** until combat is tuned in its own change |
