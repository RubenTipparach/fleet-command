# Proposal: ssarith-battle-console

## Why

The owner, 2026-11-04: "lets next work on: Ssarith Hunt UI, this time combine uis together, so we can hop between
different ships", then "use simple geometric ships for the Ssarith" and "we'll be developing their ships in another
thread".

The Ssarith Hunt has a console skin on the console skins page (mockup 4): worn olive gunmetal over CRT tubes, claw-mark
bars, phosphor readouts. It has no battle console yet.

## What Changes

- **The battle console in the Ssarith Hunt skin** (mockup 12, `docs/mockups/battle-console-ssarith.html`), written from
  the one source page by `tools/mockups/skin_page.py` like the Krai's. Every rule, panel and control is the same code.
- **The Hunt's library**, `docs/mockups/lib/ssarith.js`. It has the palette as the page's colour roles, all 24 icons of
  the icon guide as phosphor traces, the type, the live readouts, the cut-cornered key shape, and the Hunt's own names
  for ships, officers and ranks. The console skins page's Ssarith module now registers this library, so the look has one
  implementation.
- **New kit pieces rendered in Blender**: the square tube key and the wide tube key in seven lights each (dark at rest,
  lit on hover, flaring when engaged; phosphor, amber and kill red for status; an unlit slot), and the allocation cells.
  The shapes are fit-checked by `tools/mockups/ssarith_shapes.py`.
- **The Hunt's ship**: a Hunt stalker class (`shs`, cloaked) drawn as simple geometry until the Hunt's ships are
  designed in their own thread.

## Impact

- New files: `docs/mockups/lib/ssarith.js`, `docs/mockups/battle-console-ssarith.html`.
- Changed: `docs/mockups/skins/ssarith.js`, `tools/mockups/ssarith_shapes.py`, `tools/blender/build_ui_ssarith.py`,
  `assets/ui/ssarith/`, `tools/mockups/skin_page.py`.
- Mockup only.
