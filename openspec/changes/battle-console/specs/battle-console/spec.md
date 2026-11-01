## ADDED Requirements

### Requirement: The battle console keeps the reference layout

The battle screen SHALL show, at the 1920 x 1080 reference, a left column (name plate, stations panel, ship panel,
weapons panel, radar row), a top strip (own hull, weapon capacitor, target hull, mission clock, pause, menu), a bottom
strip (speed, power, systems, next target, alert lamp) and the 3D view filling the rest. Panels SHALL be fixed
rectangles whose content is clipped and never changes their size.

#### Scenario: Long names do not resize a panel
- **WHEN** the locked target has a name longer than its readout field
- **THEN** the name is trimmed with an ellipsis and every panel keeps its rectangle

#### Scenario: A wider window stretches only the strips and the view
- **WHEN** the window is resized from 1920 x 1080 to 2560 x 1080
- **THEN** the left column's panels keep their reference sizes and the strips and the view widen

### Requirement: Every panel expands to full screen with the battle beside it

Every panel SHALL have an Expand key. Expanded, the panel SHALL take the left two thirds of the screen and the live
battle the right third (in portrait, the top two thirds and the bottom third). The battle SHALL keep running, and a tap
on it, Return or Esc SHALL bring the console back.

#### Scenario: Expanding the power station on the desktop
- **WHEN** the player opens Power and presses Expand at 1920 x 1080
- **THEN** the power station fills the left two thirds, the battle plays in the right third, and a click on the battle returns to the console

### Requirement: Energy is allocated in boxes

The power station SHALL allocate the reactor's output to weapons, shields, engines, sensors, tractor, transport and
repair in whole boxes, never more in total than the output, and SHALL show output, used, spare and the battery.

#### Scenario: A box past the output is locked
- **WHEN** all of the reactor's output is allocated
- **THEN** every unlit box is shown locked, and tapping it changes nothing

#### Scenario: Engine boxes set the speed limit
- **WHEN** the player lowers engines from 6 boxes to 4
- **THEN** the speed limit falls from 180 m/s to 120 m/s, the bottom strip locks the speed boxes above it, and the speed handle cannot be dragged past them

### Requirement: Readouts call the rule that resolves the outcome

The chance to hit shown by the helm, the weapons panel and the view SHALL come from the same function that rolls the
shot, and the shield facing marked on the systems display SHALL come from the same function that applies the damage.

#### Scenario: The shown chance is the rolled chance
- **WHEN** a shot is fired at a target the helm shows at 66%
- **THEN** the shot hits with probability 0.66 under the battle's seed

### Requirement: The captain locks a target by pointing

A right click on a ship in the view SHALL lock it as the target. A left click or a tap SHALL focus it and show a Lock
target key. Next target SHALL cycle hostile ships by range.

#### Scenario: Locking on a touch screen
- **WHEN** the player taps an enemy ship, then Lock target
- **THEN** the ship is the target: it is bracketed in the view, named in the top strip and shown in the target camera

### Requirement: Orders are taken while paused

Pause SHALL stop the battle, and every panel SHALL still take orders, which take effect when the battle resumes.

#### Scenario: Turning while paused
- **WHEN** the game is paused and the player presses the helm's 45 degree starboard key
- **THEN** the ordered heading changes at once and the ship turns when the game resumes

### Requirement: Wingmen follow configurable behaviours

The fleet station SHALL set each wingman's stance, target rule, range, retreat and fire orders, and SHALL let the player
take command of any ship in the fleet, the ship left behind then following its own orders.

#### Scenario: Taking command of a wingman
- **WHEN** the player chooses Tallis in the fleet station and presses Take command
- **THEN** the console shows Tallis's systems, the camera follows Tallis, and Resolute fights under its orders

### Requirement: Panel tabs are icons named on hover

The stations and ship panels SHALL pick their pages from a narrow column of icon tabs. Pointing at a tab SHALL show its
name above it, and the panel's title SHALL name the open page.

#### Scenario: Pointing at a station tab
- **WHEN** the pointer rests on the science icon in the stations column
- **THEN** "Science" shows in a pill above the icon, and disappears when the pointer leaves

### Requirement: The commanded ship sits below the centre of the view

The ship under the player's command SHALL be drawn at 74% of the view's height from the top at every zoom and orbit,
without the game moving the camera.

#### Scenario: Zooming out keeps the ship's height
- **WHEN** the player zooms the chase view from its nearest to its farthest
- **THEN** the ship stays at 74% of the view's height

### Requirement: Power and systems draw are compared on one scale

The bottom strip SHALL show the reactor's output (Power) above the allocation's draw (Systems), on one scale.

#### Scenario: Spare energy shows as a gap
- **WHEN** 21 E of a 22 E output is allocated
- **THEN** the Systems bar is one twenty-second shorter than the Power bar

### Requirement: Mounts and subsystems show their type and status

The ship systems display SHALL draw each weapon mount as its weapon type's glyph and SHALL show the subsystems beside the
ship. Pointing at a mount or a subsystem SHALL show its name, health and status, one of Active (on and working), Damaged
(hit, still working, disabled if hit again), Disabled (damaged past working, repairable), Off (turned off, or without
power to run) and Destroyed (off line for good, cannot be restored).

#### Scenario: A disabled lance
- **WHEN** mount 3, a lance, is at 20 % health and the captain points at it
- **THEN** a card shows "3 · Lance", health 20 %, status Disabled in red

#### Scenario: A subsystem turned off
- **WHEN** the tractor beam has no power and the captain points at its box
- **THEN** the box is dimmed and the card shows status Off

### Requirement: A cooling weapon shows the seconds until it is ready
While a weapon mount charges, its box SHALL turn a darker shade of its status colour and SHALL show the seconds until
it is ready in place of its glyph, computed from the
same charge rate that charges the mount: one decimal under 10 s, whole seconds from 10 s, scaled down to fit the box
when it runs to three figures. Each mount SHALL be drawn as one group (hover outline, selection ring, status colour,
glyph or seconds) above the ship schematic, and pointing at a mount SHALL outline it in white.

#### Scenario: A lance cooling down
- **WHEN** a lance (3.5 s to charge) fired 0.1 s ago
- **THEN** its box shows 3.4 until the next tenth passes, then its glyph once it is ready

#### Scenario: A long cooldown
- **WHEN** a mount has 12.3 s left
- **THEN** its box shows 13

### Requirement: Mines are dropped no faster than one every 3 s
After a mine is dropped, the next SHALL wait the mine cooldown (3 s), and the Drop mine key SHALL show the seconds left in
the weapons' cooldown format and be disabled until then.

#### Scenario: Two mines in a row
- **WHEN** the captain drops a mine and presses Drop mine again 0.6 s later
- **THEN** no second mine is dropped and the key reads "Next mine in 2.4 s"

### Requirement: The tractor beam is a contest of rounds
The tractor station SHALL offer two keys, Hold and Break. Hold SHALL power the beam up for 10 s, after which it SHALL take
hold of the target inside reach at a grip of 50 %. Every round (10 s) SHALL end with each ship's beam value (the energy
in its tractor, times its tractor's health) compared, and the grip SHALL move toward the higher value by 10 % for each
energy point of difference, between 0 and 100 %. A grip of 0, or the holder's tractor going down, SHALL free the held
ship; if the holder's tractor is down or its value is under half the breaker's, the breaker SHALL take hold of the
holder at 50 %. The holder SHALL be able to Break at any time. The panel SHALL show the power-up, the grip (on the
target, or on the player when held), the seconds left in the round and the last round's two values, all from the rule
that resolves the round.

#### Scenario: Powering up
- **WHEN** the captain presses Hold with energy in the tractor
- **THEN** the key reads "Powering" with the seconds left, and no beam takes hold for 10 s

#### Scenario: A round won
- **WHEN** the holder puts 5 E in its tractor and the held ship resists with 4.3 E when the round ends
- **THEN** the grip moves from 50 % to 57 % and the panel shows "5.0 vs 4.3 · +7%"

#### Scenario: Breaking free
- **WHEN** the grip reaches 0 with the holder at 3 E and the held ship at 5 E
- **THEN** the held ship is free, and the same holder cannot take it again for 10 s

#### Scenario: The beam turns
- **WHEN** the grip reaches 0 with the holder at 2 E and the held ship at 5 E, or the holder's tractor is disabled
- **THEN** the ship that broke free holds the former holder at 50 %, and the former holder's panel shows "Held by"

#### Scenario: Break
- **WHEN** the holder clicks Break during the power-up or while holding
- **THEN** the beam lets go at once
