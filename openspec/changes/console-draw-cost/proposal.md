# Proposal: console-draw-cost

## Why

The owner, 2026-11-12, after the Raspberry Pi 5 profile: "so can we optimize the ui?", "Rounded rectangles? shouldnt that
just be texture-fied", "we talked about sliing stuff in the ui document", "why are we drawing stuff in godot?", "what else
can be optimized? draw calls down farther?", "I expect ui draw calls to be pretty small if we do this right", and "we will
need to test visuals extensively in main to make sure we didnt degrade any visuals by optimizing".

On the Pi the battle ran at 20 fps, and the console was half of every frame (`docs/playtests/pi/README.md`). Every panel
rebuilt its drawing every frame, as vector shapes. The Concord's frames and keys are drawn as live vector, a port of the
mockup's canvas code; every skin draws its icons, diagrams and cells that way too. The console was about 1,100 of the
frame's 1,280 draw calls. The UI document (CLAUDE.md section 7) has the console made of images: 9-slice pieces with
recorded margins, and hero pieces, with only the readouts drawn live.

## What Changes

1. **Capped redraws** (done, PR #84): a panel redraws from the battle's state at most `panel_redraw_hz` times a second,
   and at once when touched.
2. **The console is rendered only when it changes.** The panels and strips draw into one viewport that renders only on a
   frame where a panel redrew. Every other frame the console is one textured rectangle. The 3D view's overlay and the
   hover cards stay live, outside it.
3. **Rounded shapes and icons are textures.** Rounded rectangles become 9-slice draws from one atlas of white corner
   shapes, tinted. Each skin's icons are baked once into an atlas from the same vector code (one implementation), so a
   panel's drawing is a few long batches.
4. **The look does not change.** Every named still is rendered before and after on the same machine and compared pixel by
   pixel. A difference is fixed, or shown to the owner, before the step is merged.

## Impact

- The game's view only. The simulation, data rules and layout do not change. The kit's piece names and the rule that a
  missing piece is a build error still hold.
- The target: on a frame where nothing redraws, the console costs one draw call; on a frame where it re-renders, a few
  dozen.
