# Proposal: faction-select

## Why

The owner, 2026-11-05: "create a faction selection screen using 3d rendered emblems", and "Emblems are placed on a wooden
pedestal each on a concrete platform with a space backdrop. Hovering over each causes a light to shine on it with some dust
reflecting off the light. The emblem rotates slightly as you hover your mouse over it. Clicking on it gives you an option to
continue campaign or start a new one." Also: "Can we update campaign UI with new skins?"

Three factions now have a finished emblem (the Emblem Gallery: the Concord's B-E27, the Krai's KC+, the Ssarith Hunt's SD)
and a console skin. Choosing a side is the first thing a campaign asks, and the emblems are the way to show it.

## What changes

- A new screen between the main menu's Campaign and the campaign map, in mockup 1 (`docs/mockups/game-flow.html`): three
  emblems on wooden pedestals on a concrete platform under space, a lamp and lit dust on the one under the pointer, the
  emblem turning a little toward the pointer, and on a click a card offering Continue campaign or New campaign.
- The campaign's screens are drawn in the chosen faction's console skin, from that skin's own kit pieces.
- The emblems are built by one shared builder for the badge pages and this screen (`docs/mockups/lib/emblem-builds.js`).

## Impact

- Mockup 1 gains the screen and the skins; the badge pages (mockups 10, 14, 19) now call the shared builder, unchanged in
  look.
- New capability `faction-select`. Game code waits for the owner's approval (CLAUDE.md section 4).
