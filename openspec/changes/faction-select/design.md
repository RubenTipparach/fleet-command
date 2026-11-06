# Design: faction-select

## The screen

Main menu, Campaign, then this screen. In metres, seen from 8.8 m back and 1.95 m up:

| Piece | What it is |
|---|---|
| Backdrop | The Star Crew nebula sky `verdigris` with its parallax stars (`docs/mockups/lib/space.js`), a far planet up left |
| Platform | A cast concrete slab 13 m x 5 m, 0.4 m deep, chamfered lip; form-tie holes on a 1.2 m grid and pour lines |
| Pedestals | Three turned pedestals 1.12 m tall, 1.0 m across the foot, in laminated wood (the Material Maker graph `emblem_wood_laminate`, darkened to walnut, plies about 10 mm at this size), 3.3 m apart |
| Emblems | Each faction's badge from `docs/mockups/lib/emblem-builds.js`, the same builder as its badge page, cast at 1.15 m across (11.5x the 100 mm badge) on a blackened steel post, leaning back 7 degrees |
| Light | A dim hall: hemisphere fill 0.12, a cold rim from behind 0.35, the badge studio's environment map (`FCEmblemStudio`) at 35 % until lit |

Hovering an emblem (pointer over its pedestal or casting):
- its lamp, a spot 4.2 m above and 2 m in front, rises from 0.25 to 5.75 (three.js 0.147 units) over about 0.2 s, with soft
  shadows (1024 px map) of the casting on the pedestal and the slab;
- a cone of light from the lamp (an additive shell, brightest along its middle and toward the lamp) and 420 dust motes in it,
  drifting on slow currents, each glinting on its own beat; the motes are lit only inside the cone and only while the lamp is on;
- the emblem's environment reflections rise to full;
- the emblem turns toward the pointer, up to 0.45 rad of yaw and 0.25 rad of pitch, easing at 6 per second, and settles back
  to a slow idle sway when the pointer leaves.

Clicking an emblem keeps its lamp on and opens a card in that faction's skin: the faction's name and emblem, the saved
campaign (captain, turn, credits, ships) or "No campaign saved", **Continue campaign** (disabled without a save), **New
campaign** (the captain and difficulty sheet, then the map), and Back. Clicking empty space closes the card. On a phone in
landscape the card sits at the right; in portrait along the bottom (survey U2).

The Ssarith Hunt is playable with stand-in ships and the card says so: its ships wait on the owner setting its culture
(survey H1; CLAUDE.md section 1).

## Skins on the campaign

`docs/mockups/lib/kitcss.js` turns a skin's 9-slice kit pieces into CSS border-images with the pieces' own slice margins:
frame.panel and frame.inset (panels), button.idle, hover and pressed (keys), tab.idle and tab.active, bar.track and bar.fill
(red when over). The pieces are drawn by the skin's own painters (`lib/concord.js`, `lib/krai.js`, `lib/ssarith.js`), the
same kit the battle console draws, so nothing is drawn twice. Large rendered frames are scaled so no margin passes 26 px;
keys and tabs are drawn at their element's height. Each skin's colour roles and type come with it.

Which skin a screen wears: the faction screen the faction picked (Concord before a pick), the campaign its side's, the
menus and every skirmish the Concord's (owner, 2026-11-05: "stick to Concord skin for skirmish, its kind of awkward with
skins changing when I select different skirmish maps").

The Concord's panels sit on one black box, borders and all, its corners following the sweep's outer curve with a 6 px band of
black round the outside (owner, 2026-11-05: "can we get a whole box around this? include the border too, the black background
shoule wrap and curve aorund the outer border"). The metal skins keep their frames as their own ground (CLAUDE.md section 7).

## Saves

One campaign per faction, kept in the browser (localStorage) where the host allows it, otherwise for the visit. The game's
own save is designed with the campaign's implementation.

## What it costs

The page carries the three badges' Material Maker maps (about 3.5 MB inlined) and the fern bed (2.2 MB), so the page is
about 8.9 MB of its 16 MiB limit. The scene is about 1.2 million triangles (most of it the fern bed) and 3 shadowed spots:
fine on a desktop; for phones the game would bake the casting's shadows and use one live spot.

## Open questions

| # | Question | Provisional |
|---|---|---|
| 1 | Concrete: a Material Maker graph `plinth_concrete` (Material Maker headless runs only in a Linux cloud session) | A canvas stand-in on the page until rendered |
| 2 | Does the Ssarith Hunt stay playable before its culture and ships are set? | Yes, with stand-in hulls, said on its card |
| 3 | Menus before a faction is chosen: Concord skin, or a neutral one? | Concord |
| 4 | Sounds for the lamp and the pick | None yet (the Sounds page has no cue for it) |
