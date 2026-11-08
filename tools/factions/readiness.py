#!/usr/bin/env python3
"""Faction readiness: what each faction has in the game, checked off (owner, 2026-11-07: "Are you keeping track of faction
readiness on md or survey somewhere? It would be nice to have an artifact check off what's been done for a faction").

Measures every item it can from the game's own files, so the page cannot drift from the game: the console kit
(src/Game/Ui/Kit/Kits.cs, assets/ui/<f>/), the emblem (the faction screen's badge and the boarding portrait), the ship
classes and their models (data/battle/classes.json), their schematics (assets/ships/schematics/manifest.json) and baked
textures, the scenes it plays as a side (data/battle/scenarios.json and data/battle/scenes/), its bot runs
(data/playtests/suite.json), its campaign (data/campaign/campaign.json), its officers (data/battle/systems/reactor.json)
and its music (assets/audio/music/, and whether src/Game plays it). What it cannot measure, the owner's approvals and
whether a culture is set, comes from docs/design/faction-readiness.json. Writes docs/design/faction-readiness.html.

    python3 tools/factions/readiness.py           # write the page
    python3 tools/factions/readiness.py --check   # exit 1 if the committed page is not what the game says (run-tests.sh)
"""
import html
import json
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = ROOT / "docs" / "design" / "faction-readiness.html"
MANUAL = ROOT / "docs" / "design" / "faction-readiness.json"
TRACKER_URL = "https://claude.ai/artifact/8VqA9YjhCrgoiY2CGR1Qxu"
STAGES = {"none": ("todo", "No mockup"), "drafted": ("part", "Drafted"), "build": ("part", "Approved for build"),
          "game": ("done", "Approved for the game"), "retired": ("na", "Retired")}


def load(rel):
    return json.loads((ROOT / rel).read_text(encoding="utf-8"))


def scenarios():
    """Every scenario: the menu's and the scene files'."""
    out = list(load("data/battle/scenarios.json")["scenarios"])
    for p in sorted((ROOT / "data/battle/scenes").glob("*.json")):
        out += json.loads(p.read_text(encoding="utf-8")).get("scenarios", [])
    return out


def measure():
    factions = {k: v for k, v in load("data/battle/factions.json").items() if k not in ("_doc", "neutral")}
    classes = {k: v for k, v in load("data/battle/classes.json").items() if isinstance(v, dict)}
    schem = {f"{s['faction']}/{s['id']}" for s in load("assets/ships/schematics/manifest.json")["ships"]}
    kits = (ROOT / "src/Game/Ui/Kit/Kits.cs").read_text(encoding="utf-8")
    flow = (ROOT / "src/Game/Flow/FlowScreens.cs").read_text(encoding="utf-8")
    game_src = "\n".join(p.read_text(encoding="utf-8") for p in (ROOT / "src/Game").rglob("*.cs"))
    camp = load("data/campaign/campaign.json")
    officers = load("data/battle/systems/reactor.json")["officers"]["factions"]
    scen = scenarios()
    by_id = {s["id"]: s for s in scen}
    runs = load("data/playtests/suite.json")["runs"]
    concord = [s for s in scen if s.get("side") == "concord"]
    general = sorted({s["name"] for s in concord})
    music = ROOT / "assets/audio/music"
    rows = {}
    for f in factions:
        r = {}
        # the console
        kit = f'("{f}",' in kits
        r["console"] = ("done", "In the game") if kit else ("todo", "No kit in the game")
        # the emblem: the faction screen's badge and the boarding portrait
        badge = (ROOT / f"assets/scenes/faction/{f}.glb").exists()
        portrait = f'["{f}"]' in flow
        r["emblem"] = ("done", "Faction screen and boarding portrait") if badge and portrait else \
            ("part", "Faction screen" if badge else "Boarding portrait") if badge or portrait else ("todo", "Not in the game")
        # the ships: classes flown on the faction's own models
        own = sorted(k for k, c in classes.items() if (c.get("model") or "").startswith(f"assets/ships/{f}/")
                     and (ROOT / c["model"]).exists())
        names = ", ".join(classes[k].get("name", k) for k in own)
        r["ships"] = ("done", f"{len(own)} classes: {names}") if own else ("todo", "Stand-in hulls")
        keys = [classes[k]["model"][len("assets/ships/"):-len(".glb")] for k in own]
        have = [k for k in keys if k in schem]
        r["schematics"] = ("na", "No ships") if not own else ("done", f"All {len(own)}") if len(have) == len(own) else \
            ("part", f"{len(have)} of {len(own)}") if have else ("todo", "None: a stand-in outline is drawn")
        baked = [k for k in keys if (ROOT / f"assets/ships/{k}_albedo.png").exists()]
        r["textures"] = ("na", "No ships") if not own else ("done", "Every ship on its own UVs, AO baked") if len(baked) == len(own) else \
            ("part", f"{len(baked)} of {len(own)}") if baked else ("todo", "Tiled, to move onto its own UVs (CLAUDE.md section 10)")
        # the scenes it plays as a side, against the Concord's general scenes
        # (a scene where the faction is the foe in every side's version, The Hunt for the Hunt, is not one it plays)
        mine = {s["name"] for s in scen if s.get("side") == f}
        foe_of = {n for n in general if n not in mine and all(any(sh.get("faction") == f and sh.get("side") == 1 for sh in s.get("ships", []))
                                                             for s in scen if s["name"] == n)}
        apply = [n for n in general if f == "concord" or n not in foe_of]
        miss = [n for n in apply if n not in mine]
        r["scenes"] = ("todo", "Not a side") if not mine else ("done", f"All {len(apply)} scenes it can play") if not miss else \
            ("part", f"{len(apply) - len(miss)} of {len(apply)}; not yet: {', '.join(miss)}")
        n_runs = sum(1 for x in runs if by_id.get(x["play"], {}).get("side") == f)
        r["bot"] = ("done", f"{n_runs} runs") if n_runs >= max(1, len(mine)) else ("part", f"{n_runs} runs for {len(mine)} scenes") \
            if n_runs else ("todo", "No runs")
        # the campaign
        cf = camp["factions"].get(f)
        hulls = [h for h in camp["hulls"].values() if h["side"] == f]
        drawn = [h for h in hulls if h.get("schematic")]
        if cf and cf.get("playable"):
            r["campaign"] = ("done", f"Playable, {len(hulls)} hulls") if len(drawn) == len(hulls) else \
                ("part", f"Playable, {len(drawn)} of {len(hulls)} hulls drawn")
        else:
            r["campaign"] = ("part", "On the map, not playable") if cf else ("todo", "Not in the campaign")
        # its freighter: the campaign's freighter hull, flown on its approved model, on a route, and played by the bot
        fh = camp["hulls"].get((cf or {}).get("freighter") or "")
        fmodel = classes.get(fh["battle_class"], {}).get("model", "") if fh else ""
        routed = any(rt[0] == f for rt in camp["map"].get("freight_routes", []))
        played = any(x["script"] == f"freighters-{f}" for x in runs)
        if not fh or not fmodel.startswith("assets/ships/freighters/"):
            r["freighter"] = ("todo", "The stand-in freighter")
        else:
            got = [w for w, ok in (("on a route", routed), ("a raid, cash out and escort in the bot suite", played)) if ok]
            r["freighter"] = ("done" if len(got) == 2 else "part", f"The {fh['name']}" + (f", {', '.join(got)}" if got else ", no route"))
        # officers' names and ranks: the Concord's are the default
        r["officers"] = ("done", "The default names (the Concord's)") if f == "concord" else \
            ("done", "Its own names and ranks") if f in officers else ("todo", "The Concord's names")
        # music: written, and played in the game
        written = (music / f"{f}_ambient.ogg").exists() and ((music / f"{f}_battle.ogg").exists() or f == "concord")
        played = re.search(r'_ambient|_battle"', game_src) is not None
        r["music"] = ("done", "Battle and ambient loops, played") if written and played else \
            ("part", "Battle and ambient loops written, not played in the game yet") if written else ("todo", "None")
        rows[f] = (factions[f].get("name", f), r)
    return rows


ROWS = [  # (key, label, from)
    ("culture", "Culture set by the owner", "manual"),
    ("console_mockup", "Console mockup", "manual"),
    ("console", "Console skin in the game", "game"),
    ("emblem_mockup", "Emblem mockup", "manual"),
    ("emblem", "Emblem in the game", "game"),
    ("ships_mockup", "Ships mockup", "manual"),
    ("ships", "Ships in the game", "game"),
    ("schematics", "Ship schematics", "game"),
    ("textures", "Own UVs and baked textures", "game"),
    ("weapons", "Its own weapons", "manual"),
    ("scenes", "Playable as a side", "game"),
    ("bot", "Bot suite runs", "game"),
    ("campaign", "Campaign", "game"),
    ("freighter", "Freighter and its missions", "game"),
    ("officers", "Officers' names", "game"),
    ("music", "Music", "game"),
]


def manual_cell(key, m):
    if key == "culture":
        return ("done" if m["status"] == "set" else "todo", ("Set: " if m["status"] == "set" else "Open: ") + m["note"])
    if key == "weapons":
        return ("done" if m["status"] == "own" else "part", m["note"])
    st, word = STAGES[m["stage"]]
    num = f"Mockup {m['mockup']}: " if m.get("mockup") else ""
    return (st, f"{word}. {num}{m['note']}")


def page(rows, manual):
    mark = {"done": "&#10003;", "part": "&#9680;", "todo": "&#9675;", "na": "&#8211;"}
    facs = list(rows)
    head = "".join(f"<th>{html.escape(rows[f][0])}</th>" for f in facs)
    body = []
    for key, label, src in ROWS:
        cells = []
        for f in facs:
            m = manual.get(f, {})
            st, note = manual_cell(key, m[key]) if src == "manual" and key in m else rows[f][1].get(key, ("na", "Not tracked"))
            cells.append(f'<td class="{st}"><b>{mark[st]}</b> {html.escape(note)}</td>')
        body.append(f'<tr><th class="row">{label}<small>{"the owner" if src == "manual" else "measured"}</small></th>{"".join(cells)}</tr>')
    score = []
    for f in facs:
        sts = [(manual_cell(k, manual[f][k])[0] if s == "manual" and k in manual.get(f, {}) else rows[f][1].get(k, ("na",))[0])
               for k, _, s in ROWS]
        counted = [x for x in sts if x != "na"]
        score.append(f"<td>{sum(x == 'done' for x in counted)} of {len(counted)}</td>")
    return f"""<!DOCTYPE html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>Fleet Command: faction readiness</title>
<style>
body {{ margin: 0; background: #0b0e14; color: #d8dde6; font: 14px/1.4 system-ui, sans-serif; }}
header {{ padding: 18px 20px 6px; }} h1 {{ margin: 0 0 4px; font-size: 22px; letter-spacing: .04em; }}
.master {{ color: #8fc3ff; }} p {{ color: #9aa4b2; margin: 6px 0; max-width: 980px; }}
.wrap {{ overflow-x: auto; padding: 0 20px 24px; }}
table {{ border-collapse: collapse; min-width: 900px; }}
th, td {{ border: 1px solid #222a36; padding: 8px 10px; vertical-align: top; text-align: left; }}
thead th {{ background: #151b25; position: sticky; top: 0; font-size: 15px; }}
th.row {{ background: #121821; width: 190px; }} th.row small {{ display: block; color: #6c7686; font-weight: 400; }}
td b {{ font-size: 16px; margin-right: 4px; }}
td.done b {{ color: #5fd07a; }} td.part b {{ color: #ffc865; }} td.todo b {{ color: #ff6a5a; }} td.na {{ color: #6c7686; }}
tfoot td {{ background: #151b25; font-weight: 600; }}
</style></head><body>
<header>
<a class="master" href="{TRACKER_URL}" target="_blank" rel="noopener">All Fleet Command mockups (the tracker)</a>
<h1>Faction readiness</h1>
<p>What each faction has in the game. Rows marked measured are read from the game's own files by
<code>tools/factions/readiness.py</code>, so they cannot drift; the test run fails when this page is out of date. Rows marked
the owner are your approvals and your culture decisions (<code>docs/design/faction-readiness.json</code>).</p>
<p>&#10003; done &nbsp; &#9680; part way &nbsp; &#9675; not yet &nbsp; &#8211; does not apply</p>
</header>
<div class="wrap"><table>
<thead><tr><th></th>{head}</tr></thead>
<tbody>
{chr(10).join(body)}
</tbody>
<tfoot><tr><th class="row">Done</th>{"".join(score)}</tr></tfoot>
</table></div>
</body></html>
"""


def main():
    manual = json.loads(MANUAL.read_text(encoding="utf-8"))["factions"]
    text = page(measure(), manual)
    if "--check" in sys.argv:
        if not OUT.exists() or OUT.read_text(encoding="utf-8") != text:
            print("FAIL: docs/design/faction-readiness.html is not what the game says; run python3 tools/factions/readiness.py and commit it")
            return 1
        print("READINESS current")
        return 0
    OUT.write_text(text, encoding="utf-8")
    print(f"wrote {OUT.relative_to(ROOT)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
