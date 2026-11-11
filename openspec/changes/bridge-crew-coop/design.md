# Design: bridge-crew-coop

The owner's ask (2026-11-11) is in the proposal. The owner waived the mockup step for this change and asked for the
build, proven by a run (2026-11-11: "Do the prototype implementation, prove it, and do the whole thing", "no need for my
approvals with milestones, I expect you to verify yourself, and ensure syncronization between the sessions"). The captain's
menu here is a plain sheet in the Concord's kit; its mockup follows (task 6.1).

## 1. Who is who

| Seat | Who | Opens | Orders |
|---|---|---|---|
| **Admiral** | the host, member 1, for the whole game ("whoeever is designated as captain remains captain the whole game") | every tab of the flagship | every order, and the fleet's (take command, stances, withdraw, end the battle) |
| **Captain of a ship** | a member the admiral gave a ship ("if you have more than one ship, you can asing whole ships to other players", "other players can be captains of their own ship, the main captain is technically admiral of the fleet") | every tab of that ship | every order on it but the admiral's |
| **Officer** | a member holding roles on a bridge | the tabs of the roles they hold; the rest are shown disabled | their roles' orders, and a lock |

The roles (data/coop/roles.json): Navigation (helm), Weapons (tactical, the craft page), Point defence, Engineering (power,
repair, the shields page), Science (science, the sensors page), Tractor (the tractor page), Transporters (transport). Comms,
the fleet and the ship's systems page stay with the captain. A role no one was given is the captain's ("by default -
captain has access to all roles"); one member may hold several ("some players can assume the roles of multiple stations").
The captain gives and takes back roles in the captain's menu; the admiral gives ships there.

## 2. Independent targets

Navigation, Weapons, Science, Tractor and Transporters keep their own target on a crewed ship (`Ship.RoleTargets`); every
system reads its role's (`Battle.TargetOf(s, role)`): the helm's manoeuvres, the weapons and the craft, the scan, the tractor
beam, the transporters and raids. The ship's one `Target` stays the captain's and is what comms, the fleet's "Mine" rule and
the cloak follow. A lock moves the targets of the targeting roles its sender holds; a captain locking from the tab of a
role another member holds moves that role's alone (stepping in). An AI ship, and every ship in single play, has no
`RoleTargets`: one lock is every role's target, and single play steps exactly as before (test
`A_battle_with_no_crew_steps_exactly_as_in_single_play`, and the 409 tests that were there).

Over the view, every ship one of your bridge's stations has locked carries that station's icon in a disc (your own in the
skin's light, a crewmate's in lilac, the holder named on hover).

## 3. Lockstep

The battle is deterministic (CLAUDE.md 6.3), so every peer steps its own battle and only orders cross the network:

- Every player order on `Battle` (90 of them, `Battle.RelayedOrders`) begins with `Sent(nameof(...), args)`: on a networked
  peer an order given from outside the battle (a key, a click, the scripted captain, the game flow's Win and Lose) is handed
  to the lockstep instead of applied. Orders the battle gives itself, inside a step or an order, are applied. No call site in
  the console changed.
- The host gathers its own orders and its crew's, refuses any the sender's roles do not allow (`Crew.Refuses`), and sends
  each step as a tick: crew changes, then orders with their senders and the tab they were given from, then the step. Every
  peer applies a tick the same way (`Lockstep.Apply`): each order as its sender (`Battle.ApplyAs`: on the sender's ship, for
  their lock's roles), then `Step`. The host applies its tick from the same text it sends, so every peer reads the same
  numbers.
- `Battle.Player` is the admiral's ship between ticks (what the AI leaves alone). Ships given to members are `Helmed`: the AI
  leaves them alone too, and each steers to its own helm order and fires by its own auto-fire. A crew peer's console shows
  its own ship (`Battle.ViewAs`), put back to the admiral's before every tick.
- Every 30 steps (1 s) the host adds the battle's checksum (`Battle.Checksum`, FNV-1a over the bits of every number that
  decides the battle) and each crew peer compares its own: a drift is logged (`COOP_DESYNC`) and reported to the host the step
  it happens. The captain's menu shows the count.
- Pause is the admiral's: while paused (or in the menu) the host sends only ticks of orders, no steps; orders still work
  (survey G2). A crew peer's pause key does nothing.
- A crew peer plays ticks at the step rate, and a backlog of more than 6 (a stall on the network) faster.

**Across machines.** Lockstep holds only if two machines step the same battle from the same orders bit for bit. The
determinism probe (`--checksum`, `Coop.Determinism`: every scenario 3000 steps with a fixed pattern of orders, three
checksums each) is run on the Mac (ARM) and the owner's Windows laptop (x64) and the lines compared (task 1.3).

## 4. Connection

Star Crew's pattern (the owner: "we can use our star crew system for connecting players together"; its `sc-net`: hello,
welcome, the roster, intents up and the host's word down, a seat back to automation when its player leaves), carried by
Godot's ENet (`CoopNet`) in place of Star Crew's WebRTC, which is Rust: Godot ships ENet on Windows and Android, and a LAN or a
forwarded port is enough for the first co-op. One reliable channel of JSON messages, one per packet. Port 7790.

| Message | From | Says |
|---|---|---|
| hello {name} | crew | joining |
| welcome {you} | host | your member id |
| roster {crew} | host | members, ships given, roles given |
| order {tab, name, args} | crew | an order given on that console |
| crew {op, role or ship, to} | either | give a role or a ship |
| tick {n, step, ops, orders, sum?} | host | what to apply, in order |
| start {kind, id or save and offer} | host | build this battle (a skirmish by id; a campaign mission from the admiral's save and its mission id, `MissionRun.Plan`) |
| stop {over} | host | the admiral's battle left the screen |
| pause {on} | host | the admiral paused |
| desync {n} | crew | my checksum differed at tick n |

Later (task 6.3): Star Crew's matchmaker (join by code over HTTP, `sc-matchmaker`) to find a host off the LAN; a crew
member joining mid-battle (today they wait for the next battle).

## 5. The campaign

The admiral plays the campaign as alone: the map, the ports, buying and refitting, the missions. When a mission's battle
starts, the crew's games build the same battle from the admiral's campaign save and the mission's id (planning is a pure
function of the campaign, so both get the same scenario), with the mission's goals on Comms. Between battles a crew member
sees the bridge crew sheet: the roster, their roles, the last battle's end. Repairs are paid for at a port (owner,
2026-11-11: "when ship gets damaged you dont get auto repair, you have to pay to repair your ship"); a ship bought can be
given to a crew member in the next battle.

## Open questions

| # | Question | Provisional |
|---|---|---|
| C1 | Should a crew member see the campaign map (read only) between battles? | No: the bridge crew sheet; the map stays the admiral's |
| C2 | Join off the LAN: Star Crew's matchmaker codes, or a direct address? | Direct address for now; the matchmaker next |
| C3 | Should a member given a ship keep it from battle to battle? | Yes while the ship lives (its id is the campaign ship's) |
| C4 | A crew member joining mid-battle | Waits for the next battle |
