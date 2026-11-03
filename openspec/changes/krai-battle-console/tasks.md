# Tasks: krai-battle-console

## 1. Mockup (now)

- [x] 1.1 Skin fields in `docs/mockups/lib/concord.js`; the battle console reads every colour, ink, font, shape and optional piece through the skin; the Concord pixel-identical on all 40 shots apart from the version label and clock-animated parts
- [x] 1.2 `docs/mockups/lib/krai.js`: palette roles, ink, type, blade shapes, the full icon subject list, live readouts, the kit loader; `docs/mockups/skins/krai.js` uses it
- [x] 1.3 `tools/mockups/skin_page.py` writes `docs/mockups/battle-console-krai.html`
- [x] 1.4 Placards, the joined column, the joined 80 px strips and allocation cells shaped (`krai_shapes.py`, all fit rules pass) and rendered in Blender (`build_ui_krai.py`, rule B passes)
- [x] 1.5 `tools/mockups/skin_parity.mjs`: every control of every shot pressed in both skins, identical outcomes
- [x] 1.5a Version 3: the column's gaps closed to a rib; icon keys and status boxes as rendered square keys in the rail tab's style
- [x] 1.5b Version 4: one continuous 20 px divider between panels, titles on it; the backlight red, the whole kit re-rendered
- [x] 1.5c Version 5: the Krai palette for states (no green), glyphs orange on dark glass, no white marks
- [x] 1.6 Concord fixes: the Fleet key with no ship left; clean named shots
- [x] 1.7 Shots of all 40 named views looked at; listed in `mockup-work.md` and `docs/artifacts/battle-console-krai/`
- [ ] 1.8 Published by the desktop session and added to the mockup tracker
- [ ] 1.9 The owner's review (approved for build)

## 2. Game (after approval for implementation, a separate request)

- [ ] 2.1 The Godot console's skin resource carries the same fields; the Krai skin is a set of images and colours for the kit
