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
