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

### Requirement: Test scenes are picked from the menu
The menu SHALL offer test scenes (Battle, Mines, Tractor, Boarding), each a seeded world that Restart replays.

#### Scenario: Picking the boarding scene
- **WHEN** the captain opens the menu and picks Boarding
- **THEN** the battle restarts with the crippled Rattlejaw 1.9 km off, shields down

### Requirement: Mines can be placed by transporter
A mine SHALL be placeable by transporter at a point the captain clicks inside the transporter's range, spending the
transporter's charge and the mine cooldown; a point out of range SHALL place nothing.

#### Scenario: Out of reach
- **WHEN** the captain places a mine 5 km away with a 4 km transporter
- **THEN** no mine is placed and the log says the point is out of transporter range

### Requirement: Asteroids are solid and a held ship is towed
A ship touching an asteroid SHALL take damage on the facing that met it, scaled by the impact speed, and lose speed. A
held ship SHALL be tethered at the distance the beam took hold at, drawn back by the grip, so the holder drags it.

#### Scenario: Dragged into a rock
- **WHEN** the Resolute holds the Vorrak at full grip and runs with a rock between them
- **THEN** the Vorrak is dragged into the rock and takes damage scaled by the speed it is dragged

### Requirement: Marines raid subsystems or board to capture
Through a downed facing inside transporter range, the captain SHALL be able to send a raid of two marines to one
subsystem, to shoot or bomb it, after which the survivors return; or board with every marine, trading volleys until one
side is down. A ship whose defenders are all down SHALL join the captain's fleet.

#### Scenario: A capture
- **WHEN** 12 marines board the Rattlejaw against 8 defenders and win the volleys
- **THEN** the Rattlejaw strikes its colours, joins the fleet on Escort, and can be commanded

### Requirement: The transporter has three modes
The transport station SHALL offer Capture (a boarding party queued by role), Hit & run (a squad queued for one subsystem) and
Inventory (goods, a VIP and mines to or from a friendly ship, station or planet in reach). A capture SHALL take a ship
only when, with its defenders down, an engineer, a pilot and a hacker or the VIP are aboard. Demolition marines SHALL be
lost more often the fewer combat marines escort them.

#### Scenario: No pilot aboard
- **WHEN** a party without a pilot puts the last defender down
- **THEN** the deck is held, the log says a pilot is needed, and the ship is taken once a pilot beams over

#### Scenario: A delivery
- **WHEN** the captain takes 10 crates from Harbor Nine, flies within 4 km of Tessaly's surface and sends them
- **THEN** Tessaly reports the delivery received in full

### Requirement: Boarding goes by queue through the transporter pads
A ship SHALL have a number of transporter pads set by its class (2 to 8), each beaming one person and then recharging,
the transporter's energy shared among the pads recharging. The captain SHALL queue boarders by role (the role keys
showing how many are still aboard), take any queued member off the queue, and energize one pad or all charged pads,
each sending the head of the queue. The transport panel and the Target box SHALL show the target's holder's emblem and a
tug of war of our boarders against its defenders, each side's members shown by role on hover.

#### Scenario: A deck held without a pilot
- **WHEN** marines alone clear the Rattlejaw's defenders
- **THEN** the panel says "To take it, send" with an engineer, a pilot and a hacker, and those role keys are ringed

#### Scenario: A raid on one weapon
- **WHEN** the captain picks W1 from the weapon list (or clicks W1 on the target's display) and a raid bombs it
- **THEN** W1 alone loses the bomb's damage and the log names W1

#### Scenario: Energize all with four pads
- **WHEN** the Resolute (four pads, all charged) has six marines queued and the captain presses Energize all
- **THEN** four marines beam aboard the target, two stay queued, and all four pads recharge together

#### Scenario: A pad on its own
- **WHEN** one pad is charged and the captain clicks it with a hacker at the head of the queue
- **THEN** the hacker alone beams over and the pad recharges

### Requirement: Craft strip defences and missiles finish the job
Fighters on the Defences mission SHALL damage the target's point defence first, then its guns; a missile barrage SHALL
be intercepted by working point defence and land when it is gone. A disguised shuttle SHALL pass the target's shields
and damage its hull, unless the crew sees through it on the way in.

#### Scenario: Fighters, then a barrage
- **WHEN** the Ironhold's three point-defence mounts are knocked out by fighters and a barrage is fired
- **THEN** the barrage's missiles land instead of being intercepted
