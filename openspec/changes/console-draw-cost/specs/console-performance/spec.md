## ADDED Requirements

### Requirement: A console panel redraws at a capped rate and at once when touched

A console panel SHALL redraw from the battle's state at most `panel_redraw_hz` times a second
(`panel_redraw_hz_lowspec` in the low-spec mode), read from `data/console/layout.json`. It SHALL redraw on the next frame
after a press, a hover, the pointer leaving it, a resize or a change of visibility.

#### Scenario: A key is pressed
- **WHEN** the player presses a key on a panel
- **THEN** the panel shows the key pressed on the next frame, whatever its redraw rate

### Requirement: The console costs one draw call on a frame where nothing in it changed

On a frame where no console panel redrew, the console's panels SHALL be drawn as one textured rectangle. The 3D view's
overlay and the hover cards SHALL still be drawn every frame.

#### Scenario: A quiet frame
- **WHEN** no panel redrew this frame
- **THEN** the console's panels add one draw call to the frame

### Requirement: Optimising the console does not change how it looks

Every named still of the game SHALL render the same, before and after a change to how the console is drawn, on the same
machine and renderer, within rounding. Any other difference SHALL be fixed or shown to the owner before the change is
merged.

#### Scenario: The stills are compared
- **WHEN** a change to the console's drawing is ready to merge
- **THEN** every named still has been rendered on main and on the branch and compared, and its result is in the PR
