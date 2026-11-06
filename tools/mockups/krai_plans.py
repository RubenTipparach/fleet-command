#!/usr/bin/env python3
"""Write the Krai ship classes' floor plans (data/ships/krai/<class>_plan.json), rooms first, as the Ship Design Bible
says (chapters 2 and 3): a class is a list of decks, and each deck a list of rooms laid along a corridor. The frigate's
plan (frigate_plan.json) was laid by hand and is the pattern; this script lays the corvette, the cruiser and the
dreadnought the same way so a change to a class is a change to its list, never to hand-placed numbers.

    python3 tools/mockups/krai_plans.py          # write the plans
    python3 tools/mockups/krai_plans.py --check  # fail (exit 1) when a written plan differs from what the lists make

A deck: its floor height, the corridor's half width, and from the bow aft:
  bow     a room across the centreline at the deck's front (the corridor ends at its aft face)
  port, starboard   rooms laid aft along each side of the corridor, from the corridor out to the deck's half width
                    (or to the room's own, when its tuple has a fourth number)
  keel    a weapon band down the centreline (the spinal guns); the deck then has a corridor either side of it
  stern   a room across the centreline at the deck's back (the corridor ends at its front face)
Every room touches its corridor and none overlaps another, so each plan passes the guide's check P1 (the page's
FCShipPlan.check runs the same rule). Functional rooms only: what the Krai call them, and any room their culture adds,
is the owner's to decide (U7). Axes as data/ships/concord/petrel_plan.json: x to port, y up, z to the bow, metres.
"""
import json
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = ROOT / "data" / "ships" / "krai"
CW = 1.2  # half a corridor, metres (a corridor is 2.4 m wide)


def cycle(pattern, length):
    """Rooms from a repeating pattern of (name, kind, length) until the side is `length` metres long."""
    out, z, i = [], 0.0, 0
    while z < length - 1e-6:
        name, kind, ln = pattern[i % len(pattern)]
        ln = min(ln, length - z)
        if ln < 3:  # never a sliver: the last room takes what is left
            out[-1] = (out[-1][0], out[-1][1], out[-1][2] + ln)
            break
        out.append((name, kind, ln))
        z += ln
        i += 1
    return out


def slug(s):
    return "".join(c if c.isalnum() else "_" for c in s.lower()).strip("_")


def lay(spec):
    """One class's plan from its decks."""
    rooms, used = [], set()

    def add(name, kind, d, x0, x1, z0, z1):
        rid = slug(name)
        k = 2
        while rid in used:
            rid = f"{slug(name)}_{k}"
            k += 1
        used.add(rid)
        rooms.append({"id": rid, "name": name, "kind": kind, "decks": [d], "x_m": [round(x0, 2), round(x1, 2)], "z_m": [round(z0, 2), round(z1, 2)]})

    for d, deck in enumerate(spec["decks"]):
        half, front = deck["half"], deck["front"]
        z = front
        if deck.get("bow"):
            name, kind, ln = deck["bow"]
            bw = deck.get("bow_half", half)
            add(name, kind, d, -bw, bw, z - ln, z)
            z -= ln
        keel = deck.get("keel")
        inner = keel["half"] + 2 * CW if keel else CW  # where the side rooms start
        sides = {}
        for side, sx in (("port", 1), ("starboard", -1)):
            zz = z
            for name, kind, ln, *own in deck.get(side, []):
                out = own[0] if own else half  # a room may name its own outer half width (a hull that tapers)
                x0, x1 = (inner, out) if sx > 0 else (-out, -inner)
                add(name, kind, d, x0, x1, zz - ln, zz)
                zz -= ln
            sides[side] = zz
        aft = min(sides.values()) if deck.get("port") or deck.get("starboard") else z - deck.get("corridor", 10)
        if keel:
            name, kind = keel["name"], keel["kind"]
            add(name, kind, d, -keel["half"], keel["half"], aft, z)
            add(f"Deck {chr(65 + d)} corridor, port", "corridor", d, keel["half"], keel["half"] + 2 * CW, aft, z)
            add(f"Deck {chr(65 + d)} corridor, starboard", "corridor", d, -keel["half"] - 2 * CW, -keel["half"], aft, z)
        else:
            add(f"Deck {chr(65 + d)} corridor", "corridor", d, -CW, CW, aft, z)
        if deck.get("stern"):
            name, kind, ln = deck["stern"]
            sw = deck.get("stern_half", half)
            add(name, kind, d, -sw, sw, aft - ln, aft)
    plan = {"schema": "fleetcommand.ship-plan/1", "_doc": spec["doc"], "ship": spec["ship"], "crew": spec["crew"],
            "deck_floor_m": [deck["floor"] for deck in spec["decks"]], "headroom_m": 2.8, "clearance_m": 0.45, "structure_m": 0.6,
            "nose_m": spec["nose_m"], "tail_m": spec["tail_m"], "rooms": rooms}
    check(plan)
    return plan


def check(plan):
    """P1: no two rooms on a deck overlap, and every room touches a corridor on one of its decks."""
    R = plan["rooms"]
    def hit(a, b, e=0.0):
        return (a["x_m"][0] < b["x_m"][1] + e and b["x_m"][0] < a["x_m"][1] + e and a["z_m"][0] < b["z_m"][1] + e and b["z_m"][0] < a["z_m"][1] + e)
    bad = []
    for i, a in enumerate(R):
        for b in R[i + 1:]:
            if set(a["decks"]) & set(b["decks"]) and hit(a, b):
                bad.append(f"{a['id']}/{b['id']} overlap")
    for r in R:
        if r["kind"] != "corridor" and not any(c["kind"] == "corridor" and set(c["decks"]) & set(r["decks"]) and hit(r, c, 0.01) for c in R):
            bad.append(f"{r['id']} off its corridor")
    if bad:
        raise SystemExit(f"{plan['ship']}: " + ", ".join(bad))


# The classes. Decks are top to bottom, 3.5 m apart; the wide decks are in the middle so each hull fits round its rooms as
# the Krai's blade on its edge, as the frigate's does.
CREW = ("Barracks", "crew", 10)
CLASSES = {
    "corvette": {
        "ship": "krai_corvette", "crew": 14, "nose_m": 6, "tail_m": 3,
        "doc": "The Krai corvette's rooms as blocks, laid by tools/mockups/krai_plans.py: three decks, the smallest Krai warship; twin bow guns, no spinal cannon. Functional rooms only (U7).",
        "decks": [
            {"floor": 3.5, "half": 3.2, "front": 16, "bow": ("Bridge", "command", 7), "port": [("Captain", "command", 6), ("Officers", "crew", 7)],
             "starboard": [("Tactical", "command", 6), ("Comms", "systems", 7)]},
            {"floor": 0.0, "half": 6.0, "front": 19, "bow": ("Bow guns", "weapons", 6),
             "port": [("Barracks", "crew", 10), ("Mess", "crew", 8), ("Armoury", "weapons", 6)],
             "starboard": [("Barracks", "crew", 10), ("Medbay", "crew", 8), ("Stores", "bay", 6)], "stern": ("Engineering", "engineering", 8)},
            {"floor": -3.5, "half": 4.4, "front": 14, "bow": ("Magazine", "weapons", 5),
             "port": [("Shield room", "systems", 8), ("Life support", "systems", 8)],
             "starboard": [("Boarding bay", "bay", 10), ("Damage control", "systems", 6)], "stern": ("Reactor", "engineering", 8)},
        ],
    },
    "cruiser": {
        "ship": "krai_cruiser", "crew": 120, "nose_m": 12, "tail_m": 4,
        "doc": "The Krai cruiser's rooms as blocks, laid by tools/mockups/krai_plans.py: five decks, two spinal cannons side by side on the keel deck. Functional rooms only (U7).",
        "decks": [
            {"floor": 7.0, "half": 4.0, "front": 40, "bow": ("Bridge", "command", 9),
             "port": [("Captain", "command", 8), ("War room", "command", 10)] + cycle([("Officers", "crew", 9)], 27),
             "starboard": [("Tactical", "command", 8), ("Comms", "systems", 10)] + cycle([("Officers", "crew", 9)], 27)},
            {"floor": 3.5, "half": 7.0, "front": 48, "bow": ("Forward battery", "weapons", 8),
             "port": cycle([CREW, ("Mess", "crew", 9), ("Armoury", "weapons", 7)], 80), "starboard": cycle([CREW, ("Medbay", "crew", 9), ("Stores", "bay", 7)], 80)},
            {"floor": 0.0, "half": 8.5, "front": 50, "bow": ("Sensors", "systems", 8),
             "port": cycle([("Barracks", "crew", 12), ("Training hall", "crew", 10), ("Life support", "systems", 9)], 88),
             "starboard": cycle([("Barracks", "crew", 12), ("Shield room", "systems", 10), ("Damage control", "systems", 9)], 88),
             "stern": ("Engineering", "engineering", 14)},
            {"floor": -3.5, "half": 7.0, "front": 46, "keel": {"name": "Spinal cannons", "kind": "weapons", "half": 2.4},
             "port": cycle([("Magazine", "weapons", 10), ("Boarding bay", "bay", 14), ("Stores", "bay", 9)], 78),
             "starboard": cycle([("Magazine", "weapons", 10), ("Shuttle bay", "bay", 14), ("Fabricators", "engineering", 9)], 78),
             "stern": ("Reactor", "engineering", 14)},
            {"floor": -7.0, "half": 4.5, "front": 34, "port": cycle([("Stores", "bay", 10), ("Ballast", "systems", 9)], 56),
             "starboard": cycle([("Stores", "bay", 10), ("Ballast", "systems", 9)], 56)},
        ],
    },
    "dreadnought": {
        "ship": "krai_dreadnought", "crew": 420, "nose_m": 18, "tail_m": 5,
        "doc": "The Krai dreadnought's rooms as blocks, laid by tools/mockups/krai_plans.py: seven decks, three spinal cannons on the keel deck, and a command block raised on the spine (a super capital ship, the guide's rule B1). Functional rooms only (U7).",
        "decks": [
            {"floor": 10.5, "half": 3.5, "front": 26, "bow": ("Bridge", "command", 10),
             "port": [("Captain", "command", 8), ("War room", "command", 10)], "starboard": [("Tactical", "command", 8), ("Fleet command", "command", 10)]},
            {"floor": 7.0, "half": 6.0, "front": 70, "bow": ("Forward battery", "weapons", 10),
             "port": cycle([("Officers", "crew", 9), ("Wardroom", "crew", 9)], 120), "starboard": cycle([("Officers", "crew", 9), ("Comms", "systems", 9)], 120)},
            {"floor": 3.5, "half": 10.0, "front": 82, "bow": ("Sensors", "systems", 10),
             "port": cycle([("Barracks", "crew", 12), ("Mess", "crew", 10), ("Armoury", "weapons", 8)], 150),
             "starboard": cycle([("Barracks", "crew", 12), ("Medbay", "crew", 10), ("Stores", "bay", 8)], 150), "stern": ("Engineering", "engineering", 16)},
            {"floor": 0.0, "half": 12.0, "front": 86, "bow": ("Broadside battery", "weapons", 10),
             "port": cycle([("Barracks", "crew", 12), ("Training hall", "crew", 12), ("Life support", "systems", 10), ("Broadside battery", "weapons", 10)], 160),
             "starboard": cycle([("Barracks", "crew", 12), ("Shield room", "systems", 12), ("Damage control", "systems", 10), ("Broadside battery", "weapons", 10)], 160),
             "stern": ("Engineering", "engineering", 18)},
            {"floor": -3.5, "half": 10.0, "front": 80, "keel": {"name": "Spinal cannons", "kind": "weapons", "half": 3.6},
             "port": cycle([("Magazine", "weapons", 12), ("Boarding bay", "bay", 16), ("Fabricators", "engineering", 10)], 146),
             "starboard": cycle([("Magazine", "weapons", 12), ("Shuttle bay", "bay", 16), ("Fabricators", "engineering", 10)], 146),
             "stern": ("Reactor", "engineering", 20)},
            {"floor": -7.0, "half": 7.0, "front": 66, "port": cycle([("Stores", "bay", 12), ("Ballast", "systems", 10)], 120),
             "starboard": cycle([("Stores", "bay", 12), ("Ballast", "systems", 10)], 120)},
            {"floor": -10.5, "half": 4.0, "front": 44, "port": cycle([("Ballast", "systems", 12)], 72), "starboard": cycle([("Ballast", "systems", 12)], 72)},
        ],
    },
}


def write(check_only=False, classes=None, out=None):
    """Write each class's plan to out/<class>_plan.json (the Krai's by default; another faction passes its own classes and
    folder), or with check_only count the plans that differ from their lists."""
    classes, out = classes or CLASSES, out or OUT
    out.mkdir(parents=True, exist_ok=True)
    bad = 0
    for name, spec in classes.items():
        path = out / f"{name}_plan.json"
        plan = lay(spec)
        head = {k: v for k, v in plan.items() if k != "rooms"}
        text = json.dumps(head, indent=2)[:-2] + ',\n  "rooms": [\n' + ",\n".join("    " + json.dumps(r) for r in plan["rooms"]) + "\n  ]\n}\n"
        if check_only:
            if not path.exists() or path.read_text() != text:
                print(f"{path.relative_to(ROOT)} differs from its class list"); bad += 1
        else:
            path.write_text(text)
            print(f"wrote {path.relative_to(ROOT)}: {len(plan['rooms'])} rooms on {len(plan['deck_floor_m'])} decks")
    return bad


if __name__ == "__main__":
    sys.exit(1 if write("--check" in sys.argv) else 0)
