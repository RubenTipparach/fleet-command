#!/usr/bin/env python3
"""Write the Ssarith Hunt ship classes' floor plans (data/ships/ssarith/<class>_plan.json), rooms first, as the Ship
Design Bible says (chapters 2 and 3). The rooms are laid by the Krai plan layer (tools/mockups/krai_plans.py: lay, cycle
and the writer), so there is one implementation of how a deck is laid and checked (P1); this file holds only the Hunt's
lists of rooms (OpenSpec change ssarith-ship-concepts).

    python3 tools/mockups/ssarith_plans.py          # write the plans
    python3 tools/mockups/ssarith_plans.py --check  # fail (exit 1) when a written plan differs from its list

A Hunt hull is squat and broad (the design's rule 2): three decks, the middle one widest and the top and keel decks
narrower, so the section is a wide low lozenge; each room aft a little narrower than the one ahead of it, so the hull is broadest at its
shoulders and tapers aft like an animal toward its tail; a room across the
whole prow on the two lower decks, so the fitted bow is broad (rule 3). Rooms are functional; the trophy hall and the hunters' ready room come
from the culture as written (the kill counted; the hunt) and are provisional, and what the Hunt calls any room is the owner's (survey H6, U7).
Axes as data/ships/concord/petrel_plan.json: x to port, y up, z to the bow, metres.
"""
import pathlib
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import krai_plans as layer  # noqa: E402  the shared plan layer

OUT = layer.ROOT / "data" / "ships" / "ssarith"
cycle = layer.cycle

CLASSES = {
    "frigate": {
        "ship": "ssarith_frigate", "crew": 40, "nose_m": 7, "tail_m": 3,
        "doc": "The Ssarith Hunt frigate's rooms as blocks, laid by tools/mockups/ssarith_plans.py on the Krai plan layer: three decks, squat and broad, a room across the whole prow on the two lower decks so the bow stays wide. Functional rooms; the trophy hall and the hunters' ready room are provisional, their names the owner's (U7).",
        "decks": [
            {"floor": 3.5, "half": 5.0, "front": 22, "bow": ("Bridge", "command", 8),
             "port": [("Hunt master", "command", 7), ("Tracking room", "systems", 9), ("Officers", "crew", 10, 4.4), ("Trophy hall", "crew", 10, 3.8)],
             "starboard": [("Tactical", "command", 7), ("Sensors", "systems", 9), ("Officers", "crew", 10, 4.4), ("Comms", "systems", 10, 3.8)]},
            {"floor": 0.0, "half": 9.5, "front": 30, "bow": ("Forward battery", "weapons", 7),
             "port": [("Quarters", "crew", 10), ("Mess", "crew", 9, 9.2), ("Medbay", "crew", 8, 8.4), ("Hunters' ready room", "crew", 9, 7.6), ("Life support", "systems", 10, 6.8)],
             "starboard": [("Quarters", "crew", 10), ("Armoury", "weapons", 9, 9.2), ("Shield room", "systems", 8, 8.4), ("Damage control", "systems", 9, 7.6), ("Stores", "bay", 10, 6.8)],
             "stern": ("Engineering", "engineering", 10), "stern_half": 5.8},
            {"floor": -3.5, "half": 6.5, "front": 28, "bow": ("Grapple projectors", "weapons", 6),
             "port": [("Magazine", "weapons", 9), ("Boarding bay", "bay", 14, 6.2), ("Fabricators", "engineering", 10, 5.4), ("Stores", "bay", 11, 4.8)],
             "starboard": [("Magazine", "weapons", 9), ("Hunting craft bay", "bay", 14, 6.2), ("Life support", "systems", 10, 5.4), ("Stores", "bay", 11, 4.8)],
             "stern": ("Reactor", "engineering", 12), "stern_half": 4.4},
        ],
    },
}

if __name__ == "__main__":
    sys.exit(1 if layer.write("--check" in sys.argv, CLASSES, OUT) else 0)
