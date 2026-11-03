# Design: ship-design-guide

## The method

Star Crew Bridge Simulator's `federation-ship-concepts` design sequence, adopted:
1. Rooms and circulation as data.
2. A hull wrapped round them.
3. The room edges adjusted to the hull's usable inner envelope, recording what moved.

Fleet Command keeps the same order and adds parts, skin and damage after it. The guide states it as rule M1: never draw
a hull without its rooms.

## Numbers

| Quantity | Value | Source |
|---|---|---|
| Deck spacing | 3.5 m | `DECKS` in the Concord line (mockup 6) |
| Headroom | 2.8 m | Star Crew minimum deck headroom |
| Corridor | 2.4 m wide, on the centreline | Star Crew main passage |
| Door | 1.2 by 2.1 m | Star Crew |
| Air clearance, room to inner skin | 0.45 m | Star Crew hull-to-air clearance |
| Structure (frames, plating) | 0.6 m | Ours, provisional |
| Window | 1.1 by 0.6 m, 1.5 m above the floor, one row a deck | Concord line |
| Nacelle daylight | 3.0 m at least | Concord line (`concord-hull-concepts`) |
| Bow and stern past the rooms | 6 m tapered, 3 m square | Ours, provisional |

## The fit

The guide samples the plan every 2 m along the ship, with a station on the first room's front face.
- **Half beam, top and bottom.** At each station these clear the rooms there by 1.05 m.
- **Chamfer.** It is limited so that no room's outer corner comes within 1.05 m of the diagonal faces.
- **Smoothing.** A three-station running maximum smooths the steps.
- **Bow.** It tapers over 6 m.
- **The check.** It measures every room's eight corners with the Concord line's own `loftDist`. The fitted Petrel: 23 of
  23 rooms clear, least 1.05 m. Mockup 6's Petrel: 16 of 23 clear and none outside, so it is drawn too tight in places,
  and its stations should be refitted from the plan (rule H3).

## The bridge (survey A18)

Recommended: yes. The bridge is the room a player looks for and the target that ends a fight, and in the battle the
bracket, the bridge-fire signature and the crown blown off all land on it. A bridge inside the hull's outline cannot be
read, aimed at or broken. Every faction gives it a block of its own, on the dorsal centreline, forward of mid-ship, one
deck proud. Each faction shapes that block its own way.

## Exterior against the console (owner, 2026-11-03)

- **X1.** A hull never shares a material, texture or shader with its faction's console skin.
- **X2.** It never shares a colour swatch either.
- **X3.** Each faction is set apart by three things: distinct features, its colour scheme and unique paneling.
- **X4.** The console and the ship are tested side by side.
- Measured against X2, mockup 6's band blue is too close to the Concord console's Signal (#2f62e0), and is to move to a
  hull blue of its own. Mockup 7 (Krai) shares its console's material and colours outright.

## Open questions

| # | Question | Recommendation (provisional until answered) |
|---|---|---|
| A18 | Does the bridge need a shape of its own? | Yes: a raised bridge block on the dorsal centreline, forward of mid-ship, shaped per faction |
