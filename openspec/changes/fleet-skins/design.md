# Design: fleet-skins

## One page, every skin

`SKINS` holds every skin library in the page (Concord, Krai, Ssarith). `bindSkin(id)` binds the fields the page reads:

- `CON` and the colour roles `CC`;
- `INK` and `KINK`, the inks on fills and on keys;
- `FNT`, the type;
- `RR` and `KNOB`, the shapes of drawn keys and knobs;
- `MARK`, the marks the Concord draws in white;
- `STT` and `STB`, the strips' heights;
- `FP`, the panel padding.

`applySkin(id)` does the rest:

- binds the skin and its kit (built once per skin and kept in `KITS`);
- re-derives the status colours and the explosion ring's light;
- clears the drawn-piece cache and the interior view's meshes;
- recolours the link to the tracker;
- lays the console out again, because strips differ in height.

The console calls `applySkin(skinOf(W.player))` at the start of every frame, so any change of command, by any route,
swaps the skin.

`skinOf(s)` is one rule. In an allied scene it is the ship's own faction's skin. Otherwise it is the page's skin, which
the player's side wears under that skin's names (the Krai and Ssarith pages, as before).

## Scenes and keys

- **Allied fleet**: the three ships against two Kethran ships and a Vos cruiser, to try the hop under fire.
- **Three races**: the three ships at rest 12 units apart, and the Kethran Varn Outpost 260 units ahead, well beyond
  every weapon's range. Nothing attacks, so the captain can open every panel in every skin at leisure.
- **Keys 1, 2 and 3** take command of the first, second and third ship of your side, in the scene's order. They go
  through `A.command`, the Fleet key's own route, and both read one list of your ships, `ownFleet()`.

## Officers

Every ship on your side that you can command has its own officers, from the roster, with ids `<ship>.<officer>`. They
are named from the skin that ship wears, and a second ship of the same faction takes the names in a different order.
Rank names come from the skin being worn. A slot's Next officer cycles only that ship's own officers.

## Checks

The parity check compares each faction page with the Concord page, press for press, including the Allied fleet shots.
Both pages start the allied scene on the Concord cruiser and swap skins at the same hops, so their states match.

## Open questions

| # | Question | Provisional answer |
|---|---|---|
| F1 | Should hopping be instant, or take a moment (a transfer of command)? | Instant |
| F2 | Should a hop keep the station and panels you had open? | Yes: the console's layout state is kept |
| F3 | Should the Krai ships in the allied fleet use the Krai line's models (mockup 7)? | Not yet: the stand-in in Krai colours |
