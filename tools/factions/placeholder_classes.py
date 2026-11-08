#!/usr/bin/env python3
"""Put the placeholder lines into the game's data (owner, 2026-11-08, on mockup 36: "cool start implementation"): for each
of the nine factions in data/ships/placeholders.json, its battle classes (data/battle/classes.json), its stand-in colours
(data/battle/factions.json) and its campaign faction and hulls (data/campaign/campaign.json). Only the entries this script
owns are written (each class id from placeholders.json, the factions named there); every other entry is left as it is.

The battle numbers are a role's: each class copies the stats and mounts of the approved class that fills the same role
(TEMPLATE below), since the placeholders' armament is a sketch and each faction's combat change sets its own. A class's
battle length is its true length times BATTLE_X, the framing every approved class uses (classes.json _doc). The older
Kethran and Vos stand-in classes (kff, kdd, vcr) get the placeholder glbs of their roles.

    python3 tools/factions/placeholder_classes.py [--check]
"""
import json, pathlib, sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
PH = json.loads((ROOT / 'data/ships/placeholders.json').read_text())['factions']
CLASSES, BFAC, CAMP = (ROOT / p for p in ('data/battle/classes.json', 'data/battle/factions.json', 'data/campaign/campaign.json'))
COMMS, CLOAK, SCEN = ROOT / 'data/battle/systems/comms.json', ROOT / 'data/battle/systems/cloak.json', ROOT / 'data/battle/scenarios.json'
# the damage ranges that stand every class with a model (RangeTests): a row of each placeholder line behind the approved rows
RANGES = ('concord_range', 'krai_range', 'kelvane_range')

BATTLE_X = 2.7
TEMPLATE = {'Frigate': 'ff', 'Destroyer': 'dd', 'Cruiser': 'ca', 'Carrier': 'kelvane_cv'}   # anything else is a capital ship
CAPITAL = 'kelvane_bc'
CODE = {'Frigate': 'FF', 'Destroyer': 'DD', 'Cruiser': 'CA', 'Carrier': 'CV', 'Battleship': 'BB', 'Dreadnought': 'DN', 'Flagship': 'FL', 'Capital': 'CC'}
SHAPE = {'axiom': 'wedge', 'exponent': 'saucer', 'redtithe': 'claw', 'vos': 'claw', 'kethran': 'wedge', 'tarsine': 'saucer',
         'aelith': 'saucer', 'ombrai': 'saucer', 'vhorr': 'claw'}
OLD = {'kff': 'kethran_ff', 'kdd': 'kethran_dd', 'vcr': 'vos_dd'}
# Campaign names: short name and ship names, ours and provisional (survey U8, N1-N6), from each written culture.
CAMPAIGN = {
    'axiom': ('Axiom', ['Fixed Point', 'Invariant', 'Limit', 'Root', 'Bound', 'Fold']),
    'exponent': ('Exponent', ['Accretion', 'Holdfast', 'Tidewrack', 'Unit Forty', 'Ballast', 'Undertow Yard']),
    'redtithe': ('Tithe', ['Red Ledger', 'Bonetally', 'Seizure', 'Last Rites', 'Nail and Rope', 'Debt Collector']),
    'vos': ('Vos', ['Varn', 'Grist', 'Kettle']),
    'kethran': ('Kethran', ['Iron Oath', 'Ascendant', 'Forgeborn', 'Hammerfall', 'Ninth Rank', 'Thane\'s Due']),
    'tarsine': ('Tarsine', ['First Light', 'Unbent', 'Elder Lamp', 'Long Watch', 'Remembrance', 'Old Throne']),
    'aelith': ('Aelith', ['Quiet Answer', 'Long Thought', 'Measure', 'Clear Sight', 'Seventh Silence', 'Patient Star']),
    'ombrai': ('Ombrai', ['Lidless', 'Hush', 'Half Light', 'Pale Witness', 'Unseen', 'Undertow']),
    'vhorr': ('Vhorr', ['Bloom', 'Hunger Root', 'Swell', 'Gravid', 'Spore Tide', 'Deep Brood']),
}

# The campaign map's colour for each (territory, labels): from its palette, picked to stay apart from the others' on the map.
MAPCOL = {'axiom': '#6a5cf0', 'exponent': '#8be6dc', 'redtithe': '#c4141c', 'vos': '#e3a33a', 'kethran': '#e86a3a', 'tarsine': '#b0602e',
          'aelith': '#6ff3ff', 'ombrai': '#ff4fb0', 'vhorr': '#a6e34a'}

def dump(p, d): return json.dumps(d, indent=1) + '\n'

SPECIAL = {k for k in json.loads((ROOT / 'data/battle/special_weapons.json').read_text()) if k != '_doc'}

def build():
    classes, bfac, camp = (json.loads(p.read_text()) for p in (CLASSES, BFAC, CAMP))
    for fid, F in PH.items():
        col = F['colours']
        bfac[fid] = {**{k: v for k, v in bfac.get(fid, {}).items() if k == 'skin'}, 'name': F['name'], 'hull': col['hull'], 'trim': col['trim']}
        short, names = CAMPAIGN[fid]
        old = camp['factions'].get(fid, {})
        camp['factions'][fid] = {'name': F['name'], 'short': short, 'color': old.get('color', MAPCOL[fid]),
                                 'rival': old.get('rival', ''), 'playable': old.get('playable', False), 'prefix': old.get('prefix', ''), 'home': old.get('home', ''),
                                 'names': old.get('names', names), **({'freighter': old['freighter']} if 'freighter' in old else {})}
        for tier, C in enumerate(F['classes'], 1):
            t = classes[TEMPLATE.get(C['role'], CAPITAL)]
            classes[C['id']] = {'name': F['name'].replace('The ', '') + ' ' + C['role'].lower() + ' (' + C['name'] + ')', 'code': CODE[C['role']], 'shape': SHAPE[fid],
                                'model': f"assets/ships/{fid}/{C['id']}.glb", 'length_m': round(C['length_m'] * BATTLE_X),
                                **{k: v for k, v in t.items() if k not in ('name', 'code', 'shape', 'model', 'length_m', 'mounts')},
                                'mounts': [m for m in t['mounts'] if m['weapon'] not in SPECIAL]}   # a special weapon is its own faction's
            camp['hulls'][C['id']] = {'side': fid, 'tier': tier, 'name': C['name'], 'cls': C['role'], 'battle_class': C['id'], 'schematic': f"{fid}/{C['id']}"}
    for old_id, new_id in OLD.items():
        fid = new_id.split('_')[0]
        classes[old_id]['model'] = f'assets/ships/{fid}/{new_id}.glb'
    # each class's comms and its boarding numbers (marines, deck plan) are its template's; comms.json is written whole, the
    # hand-laid cloak.json has the new keys put after its last entry in each block, so the rest of the file keeps its layout
    comms = json.loads(COMMS.read_text())
    ids = [(C['id'], TEMPLATE.get(C['role'], CAPITAL)) for F in PH.values() for C in F['classes']]
    for cid, tid in ids:
        comms['classes'][cid] = comms['classes'][tid]
    cloak_t = CLOAK.read_text(); cloak = json.loads(cloak_t)
    for block, sec, anchor in (('marines', cloak['security']['marines'], '"kelvane_cv": 10\n  }'), ('deck_plans', cloak['deck_plans']['classes'], '"kelvane_cv": "petrel"\n  }')):
        missing = [(cid, tid) for cid, tid in ids if cid not in sec and tid in sec]
        if not missing: continue
        if cloak_t.count(anchor) != 1: raise SystemExit(f'cloak.json: the {block} block changed; update its anchor here')
        add = ''.join(f',\n   "{cid}": {json.dumps(sec[tid])}' for cid, tid in missing)
        cloak_t = cloak_t.replace(anchor, anchor.split('\n')[0] + add + '\n  }')
    scen = json.loads(SCEN.read_text())
    for sc in scen['scenarios']:
        if sc['id'] not in RANGES: continue
        sc['ships'] = [sh for sh in sc['ships'] if not sh['id'].startswith('ph_')]
        for row, (fid, F) in enumerate(PH.items()):
            line = [C['id'] for C in F['classes']] + [o for o, n in OLD.items() if n.startswith(fid + '_')]
            for k, cid in enumerate(line):
                sc['ships'].append({'id': 'ph_' + cid, 'name': classes[cid]['name'], 'captain': CAMPAIGN[fid][0] + ' captain', 'class': cid, 'faction': fid, 'side': 1,
                                    'x_m': round((k - (len(line) - 1) / 2) * 20000), 'z_m': -150000 - 22000 * row, 'heading_deg': 90, 'speed_m_s': 0, 'stance': 'hold', 'hold_fire': True})
    return {CLASSES: dump(CLASSES, classes), BFAC: dump(BFAC, bfac), CAMP: dump(CAMP, camp), COMMS: dump(COMMS, comms), CLOAK: cloak_t, SCEN: dump(SCEN, scen)}

out = build()
if '--check' in sys.argv:
    stale = [str(p.relative_to(ROOT)) for p, t in out.items() if p.read_text() != t]
    print('stale: ' + ', '.join(stale) if stale else 'placeholder classes current'); sys.exit(1 if stale else 0)
for p, t in out.items(): p.write_text(t)
print('wrote', ', '.join(str(p.relative_to(ROOT)) for p in out))
