# Story missions: ideas for the Concord campaign

**Status: draft ideas, nothing here is built.** The owner asked (2026-11-11): "draft up some ideas for more story
missions please, and put it in to a file. and some overarching plot points for the concord". This page is a pool to
pick from, not a plan. Once the owner picks, the chosen missions become an OpenSpec change (`campaign-story`) and mission
files in `data/missions/` (format: `docs/design/mission-files.md`).

What the campaign already has: the **Shakedown cruise** (optional, 5 turns) leads to **First blood** (drive off a raider
watching Halcyon), and the **prize** contract leads to **Bring the prize home**. Everything below follows on from those.

The cultures used here are only what is set: the Concord is a federation of species, classic space opera (survey G1),
home Halcyon; the Krai Warhost is its rival, home Vahr. Every other faction's culture is open or a draft (the Tarsine,
the Exponent, the Vos, the Red Tithe), so where the plot leans on one, it says so and the owner has the last word
(CLAUDE.md section 1). No faction's part in the story comes from what its people look like.

Questions for the owner are in the survey, section ST.

---

## The plot: "The Quiet Line"

### The premise

The Concord is a union of member worlds that agreed to settle their quarrels by vote and to defend each other. It has
kept an uneasy peace with the Krai Warhost for a generation, along a border both sides call the Quiet Line: a string
of buoys, a few listening posts, and an agreement that neither fleet crosses it armed.

The captain takes a new ship out of Halcyon's yard as that peace starts to fail, and nobody can say who is breaking it.

### The four threads

1. **Someone is selling the war.** Raiders on the Concord side are working from stolen patrol routes, and Krai convoys
   are hit by ships that fly no flag. A broker in the Vos Syndicate is selling each side's movements to the other's
   hard-liners. The Vos are not the enemy. The broker is one person with a price, and some Vos captains want them gone
   as much as the Concord does.
2. **Each side has people who want the war.** In the Concord, an admiralty bloc (the "Forward Council") holds that a
   strike on the Krai yards now would end the threat for good. In the Warhost, some clans say the same of Halcyon. Both
   blocs take the broker's reports at face value, because the reports tell them what they already believe.
3. **The war feeds something else.** The wrecks left by every skirmish on the Line are not staying wrecks. Something is
   growing on them, taking in hulls and crews, and growing faster with every battle. This is the Exponent (its draft
   culture: a collective that grows by taking ships in, survey N2). Neither government believes it at first, because
   each side thinks the missing ships were the other side's doing.
4. **What sleeps beyond the Line.** Past the Line, deep in unclaimed space, lies a fleet of dormant capital ships from a
   power that fell long ago (proposed: the Tarsine Remnant's ancestors; their culture is open). The Exponent is growing
   toward them. If it takes them in, neither the Concord nor the Warhost can stop it alone.

The story turns on one choice made over many missions: **does the captain help the Concord make a truce with the Krai,
or follow the Forward Council into war?** Both paths reach the finale. Which allies arrive for it depends on what the
captain chose.

### The acts

The acts are gated by the prestige tiers that already exist (`data/campaign/campaign.json`), so the story keeps pace
with the ship the captain can buy. At 80 cr a contract step, ten missions reach a heavy cruiser.

| Act | Prestige | Ship tier | What happens |
|---|---|---|---|
| 1. Fresh from the yard | 0-25 | Frigate | Raiders near home; the stolen routes; the first incident on the Line |
| 2. The Quiet Line | 25-50 | Destroyer | Border incidents escalate; the first overgrown wreck; the broker's trail |
| 3. The Reef | 50-90 | Heavy cruiser | The Exponent shows itself; a vote at Halcyon; truce or strike |
| 4. Old Lamps | 90+ | Carrier, battleship | The race for the sleeping fleet; the defence of Halcyon |

---

## The missions

Each mission names what it reuses. **Built** means the mission file format already has it. **New** means it needs work
first, listed at the end.

### Act 1: Fresh from the yard

**1.1 Shakedown cruise** (exists). Optional, 5 turns. Leads to First blood.

**1.2 First blood** (exists). Drive off the raider watching Halcyon.
*Proposed change:* on a win, the raider's wreck leaves a data core, and the next mission is offered.

**1.3 The raider's log**
- Type: salvage, then scout.
- The raider from First blood was carrying a Concord patrol schedule, a real one, two weeks old. Beam its data core
  aboard from the debris field before a second raider comes back for it.
- Built: salvage pods (`salvage.json`), a late arrival.
- Leads to: 1.4.

**1.4 Market at Grist**
- Type: diplomat run, but no shots fired first.
- The schedule was bought at a Vos market station. Fly in under the Syndicate's rules (no weapons armed inside the
  marker ring) and scan the traders until one of them is carrying more Concord schedules.
- Built: `find_ship.json` (scan the traders), the "without firing first" rule from `diplomat_run.json`.
- New: a **comms choice** on arrival (comms box dialogue options, bridge-crew-coop task 7.6). Pay the market's dock
  fee (credits), or bluff it (with an officer at Comms, a skill check).
- Leads to: 1.5.

**1.5 Lights on the Quiet Line**
- Type: crossroads.
- A Concord freighter has strayed across the Line. A Krai patrol of two frigates has its guns on her. Get her back
  across without starting a war.
- The Krai hold fire unless fired on. The captain wins by escorting the freighter to the marker; firing first fails
  the mission, and Krai standing drops.
- Built: crossroads (harm no traffic), escort, the `truce` tag, standing.
- New: a comms choice (hail the Krai commander and apologise, or order them off).
- Act 1 ends here.

### Act 2: The Quiet Line

**2.1 Silent buoy**
- Type: survey, then holding action.
- A buoy on the Line has stopped transmitting. Survey it, and hold while its last recording downloads. The recording
  shows a ship with no flag cutting it open. Something arrives before the download ends.
- Built: `survey.json`, `holding_action.json`.

**2.2 Wreck at Dunmore**
- Type: scout.
- A Krai and a Concord frigate fought here a week ago, and both were lost. Scan both wrecks. The wrecks have moved
  since the battle, and something is growing on their hulls.
- This is the Exponent's first appearance, as targets that do not answer hails and do not fire until they are fired on.
- Built: scan objectives (`scout.json`).
- New: a wreck that moves (drifting hulks), and Exponent ships in the campaign (placeholder line).

**2.3 The member's vote** (proposed member world: Sennet)
- Type: diplomat run, then escort.
- Sennet, a member world near the Line, is voting on whether to leave the Concord, because the Concord cannot keep the
  Line safe. Carry its envoy home from Halcyon. Raiders know the route.
- Built: `diplomat_run.json`, `ambushed.json`.
- Result: on a win, Sennet stays and its yard sells the captain one hull tier early. On a loss, Sennet leaves and its
  sector turns neutral.
- New: a story result that changes a sector's owner.

**2.4 The broker's trail**
- Type: stop scout.
- A courier is running the broker's latest reports toward Krai space. Catch it within two and a half minutes. Disable
  it, do not destroy it: its records are the evidence.
- Built: `stop_scout.json`, boarding (`capture.json`, needs marines).

**2.5 Fire on the Line**
- Type: fleet action. Act 2 ends here.
- A Krai battle group crosses the Line in force, sure that the Concord raided its convoys. A comms choice comes first:
  - **Send them the courier's records.** A hail; the Krai commander demands proof under fire. Hold out for three
    minutes without destroying their flagship, then they break off. Krai standing rises.
  - **Fight them back across the Line.** A plain fleet action. Krai standing falls, and the Forward Council's favour
    rises.
- New: a comms choice that picks the battle, and a story flag (`truce_path` or `war_path`) the later missions read.

### Act 3: The Reef

**3.1 Something growing**
- Type: monster hunt, against a structure.
- The Dunmore wrecks have grown into a station-sized reef that sends out new hulls. Destroy its core before it spawns a
  full group. The Exponent's ships fight to the end.
- Built: `monster_hunt.json` and `asteroid_base.json` together (a base to break, with defenders).
- New: a group that grows during the battle (spawning on a timer).

**3.2 The vote at Halcyon**
- Type: defence; the Act 3 turning point.
- The Concord's assembly meets at Halcyon to vote on the Forward Council's strike. Raiders in Krai colours attack the
  assembly station during the session, an attack timed to win the vote.
- Win and capture one raider: its crew are the broker's hire, the vote fails and the truce path opens. Win without a
  capture: the vote passes, and the strike goes ahead.
- On the war path, the captain's choice at 2.5 already tilted the vote. The defence still matters, because the
  station must survive either way.
- Built: `defend_base.json`, capture.

**3.3a (truce path) Truce at Vahr's edge**
- Type: escort, with Krai ships as allies.
- Escort the Concord's envoy to a meeting with the Krai on the Line. An Exponent group attacks the meeting. Concord
  and Krai ships fight on the same side for the first time.
- Built: allied groups (`role: ally`), the `truce` tag.
- New: allied ships of a rival faction, flown by AI, in the campaign.

**3.3b (war path) The Forward strike**
- Type: shipyard strike.
- Ordered to strike the Krai yard at the Line. Destroy the yard before the ships in dock get under way.
- A comms choice partway through: a Krai hail reports Exponent ships attacking the yard's far side. Finish the strike,
  or turn and fight the reef beside the Krai you came to destroy. The second choice moves the captain back toward the
  truce path.
- Built: `shipyard_strike.json`.

**3.4 The broker**
- Type: assassinate, or capture.
- The broker's own ship, cornered in a Vos nebula. Capture it (bring the broker to Halcyon) or destroy it.
- Vos captains who also want the broker gone join as allies if the captain was straight with them at Grist (1.4).
- Built: `assassinate.json`, `capture.json`, nebula plumes.
- Result: captured, the broker names the Forward Council's contact, and the war path can be left even this late.

### Act 4: Old Lamps

**4.1 Sleeping giants** (proposed: a Tarsine dormant fleet; the culture is open)
- Type: sleeping giants.
- The reef is growing toward the dormant capital ships beyond the Line. Reach them first. Disable them before they
  wake, or before the reef takes them in.
- Built: `sleeping_giants.json` exactly.
- New: one giant awake and taken in by the reef, fought in 4.3.

**4.2 Every ship we have**
- Type: a run of three short battles in one turn; the co-op centrepiece.
- The reef moves on Halcyon. The admiral's fleet splits up: hold a listening post, escort the evacuation freighters,
  and screen the main body.
- In co-op, each crew-captained ship can be sent to a different one of the three, the admiral choosing who goes where.
- Built: holding action, escort, defence.
- New: a turn with several battles at once (one per ship group), only in co-op or with more than one ship.

**4.3 Halcyon holds**
- Type: overwhelming fleet; the finale.
- The reef's main body, the taken giant at its heart, comes for Halcyon. Who stands beside the captain depends on the
  campaign:
  - The truce path brings a Krai battle group.
  - Grist (1.4) and the broker (3.4) bring Vos ships.
  - The vote at Sennet (2.3) brings Sennet's yard ships.
  - The war path brings the Forward Council's fleet alone.
- Destroy the taken giant to win.
- Built: `overwhelming.json`, allies.
- New: allies chosen by story flags.

### Side stories (story missions off the main line)

Each is offered once, lapses after 5 turns like the shakedown, and leaves a mark on the campaign:

- **The long patrol.** A Concord frigate missing for a year sends a distress call from deep inside a nebula. Its crew
  are alive, and have been fighting the reef alone. Rescuing them adds an officer with a story.
  Built: `distress.json`, nebula.
- **Dr. Hale's ruin.** The scientist from the extraction contract finds Tarsine records on a dead world, telling how
  the old power fell and how its dormant fleet is woken. Reading them makes 4.1 easier.
  Built: `away_team.json`, `secret_below.json`.
- **The Red Tithe's due.** The Red Tithe (draft culture, survey N3) claims a Concord frontier colony owes it. Pay, or
  defend the colony. Paying makes the next Red Tithe encounter friendly; defending makes them hunt the captain.
  Built: `pursuit.json` (hunters), `defend_base.json`.
- **A Krai captain's word.** A Krai captain the player spared at 1.5 or 2.5 asks for help against a clan of hard-liners
  in their own fleet. The Warhost is not one mind, and this shows it.
  Built: fleet action with Krai allies against Krai enemies.

---

## Co-op in the story

- The admiral (the host) makes the comms choices. A crew captain can **propose** one: it shows in the comms box for the
  admiral to accept, so the crew argue about the story together.
- Missions with split objectives (2.1, 4.2) give each crew-captained ship its own job.
- A crew member's ship lost in a story mission is a loss to the whole fleet, and its captain returns to the flagship's
  stations (as now).

## What the story needs that the game lacks

| Need | Used by | Note |
|---|---|---|
| Comms choices: dialogue options in the comms box | 1.4, 1.5, 2.5, 3.3b | bridge-crew-coop task 7.6 |
| Story flags that later missions read | 2.5, 3.2, 3.3, 4.3 | a campaign variable set by a result, read in `where` and `setup` |
| A result that changes a sector's owner | 2.3 | |
| Allies of another faction, flown by AI | 3.3a, 3.4, 4.3 | `role: ally` exists; groups of a second faction may need work |
| A group that grows during a battle | 3.1, 4.3 | spawn on a timer |
| Drifting hulks | 2.2 | |
| Exponent ships in the campaign | Act 2 onward | its placeholder line, or its own ships once approved |
| Several battles in one turn | 4.2 | co-op first |
