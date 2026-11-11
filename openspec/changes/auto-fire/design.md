# Design: auto-fire

## What Auto fires

"All currently active weapons" is read as every mount that **Fire would loose with Multiple on**: selected (in the
volley), armed (not Offline), ready, in arc and in range of its target, its target being its pinned ship or else the
locked enemy (`Battle.Fireable`, the mockup's `fireable`). Auto fire calls that one list and the same `FireMount` the
Fire key does, so the Fire key's count ("Fire · 3") is exactly what Auto looses on the next step (CLAUDE.md 6.1).

- **Single is ignored.** Single limits a press of Fire to one weapon; Auto fires every active weapon, as the owner
  asked. A captain who wants a weapon held takes it out of the volley or sets it Offline.
- **Never while cloaked, never at a friendly, never from a struck ship.** The same guards as Fire.
- **Silent.** Fire says "No weapon ready in arc and range" on an empty press; Auto, firing every step, says nothing
  but its own on and off ("Auto fire: every selected weapon fires as it bears.", "Auto fire off.").
- **Per ship.** `AutoFire` is on the ship, like `Volley`: taking command of another ship shows that ship's setting.
  A ship you do not command is fought by its captain (the AI), whose fire rule is unchanged.

## Layout

The weapons panel's first row keeps its height (48 px) and its two shares: the Fire share (62 % of the row) is split
into Auto (40 % of it) and Fire (the rest), 6 px apart as every other pair of keys, then Multiple or Single. Auto is a
toggle drawn pressed while on (the kit's `button.pressed`), its label centred. No new kit piece: every skin draws it
from its own button pieces, so the split reaches every skin.

## Open questions

| # | Question | Provisional answer |
|---|---|---|
| F1 | Should Auto hold a weapon whose chance to hit is very low (the AI's `ai_fire_min_chance`)? | No: Auto fires what Fire would; the captain sets what is in the volley |
| F2 | Should Auto be saved between battles? | No: it starts off in every battle |
