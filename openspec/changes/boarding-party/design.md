# Design: boarding-party

| Number | Value | Meaning |
|---|---|---|
| default load | one marine a pad | Sent when the queue is empty, taken from the marines aboard as each beams |
| `wound` | 0.5 | Health a hit takes from a marine; at none the marine dies |
| `heal` | 0.35 | Health each medic restores to the most wounded marine, each volley, before the shots |
| `hacker_speed` | 0.15 | The volley interval shortens this much for each hacker (to half at most) |
| `hacker_cover`, `hacker_exposed_p` | 5, 0.5 | Below 5 marines, a hit the defenders land finds a hacker at 50 % |

A defenders' hit is resolved by `woundParty`: an exposed hacker first, otherwise a random member of the party, weighted by
count. The shots on each side are the boarding rule's `volley()` (one implementation, shared with fights aboard our own
ship). `canTake` and `missingRoles` are one rule for the take and for what the panel asks for. `pilotNeeded(s)` is one rule
for the prize's AI, the view's label, the Fleet station and Take command.

## Open questions

| # | Question | Provisional answer |
|---|---|---|
| P1 | Should medics heal more than one marine a volley, or revive a downed marine? | One a volley, no revival |
| P2 | Should engineers do anything during the fight (repair the prize as it is taken)? | Not yet |
| P3 | Should a prize without a pilot still man its point defence? | No: nothing works until a pilot is aboard |
