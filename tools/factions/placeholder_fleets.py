#!/usr/bin/env python3
"""Write data/ships/placeholders.json: the placeholder ship lines for the nine factions that have no designed ships yet
(owner, 2026-11-08: "generate place holders for all of their ships, class design is your discretion, and a place holder
logo for the factions too"). Each faction gets four classes, frigate to capital, built by one hull generator
(docs/mockups/lib/placeholder-hulls.js) from the numbers here. Every shape choice names the tradition it comes from
(CLAUDE.md section 1: a species' looks are not its design), and the cultures are the provisional ones in
openspec/changes/console-skins/design.md.

Profiles run stern (t = 0) to bow (t = 1) at seven even stations: plan is the half-beam and rise the half-height, each a
fraction of the class's beam and height; n is the superellipse exponent of the cross-section (1 a diamond, 2 an
ellipse, 5 near a box) and segs its corner count. A limb is a second loft grown from inside the body, its path in
fractions of (half-beam, half-height, half-length); mirror copies it to port; iterate repeats it smaller (the Axiom's
rule carried deeper). Stripes colour a band of the skin: t0..t1 along the length, a0..a1 degrees round the section
from the top.

    python3 tools/factions/placeholder_fleets.py [--check]
"""
import json, pathlib, sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = ROOT / 'data/ships/placeholders.json'

def limb(path, w, h, n=2.0, segs=12, mirror=True, glow_tip=False, iterate=0, scale=0.618):
    return {'path': path, 'w': w, 'h': h, 'n': n, 'segs': segs, 'mirror': mirror, 'glow_tip': glow_tip,
            'iterate': iterate, 'scale': scale}

def stripe(role, t0, t1, a0, a1):
    return {'role': role, 't': [t0, t1], 'a_deg': [a0, a1]}

def cls(id, name, role, code, length_m, beam, height, armament, limbs=(), stripes=(), why=''):
    return {'id': id, 'name': name, 'role': role, 'code': code, 'length_m': length_m, 'beam_ratio': beam,
            'height_ratio': height, 'armament': armament, 'limbs': list(limbs), 'stripes': list(stripes), 'why': why}

F = {}

# The Axiom: every true thing is a rule and its iteration; a ship is honoured by how deep its rule is carried. The
# tilers' school sets the hull: exact diamond sections, golden-ratio stations; the branchers' school sets the fins,
# each class carrying the fin rule one iteration deeper.
fin = lambda d: limb([[0.3, 0, -0.1], [1.4, 0.05, -0.32], [2.1, 0.1, -0.55]], [0.5, 0.3, 0.04], [0.35, 0.2, 0.03],
                     n=1, segs=4, iterate=d)
F['axiom'] = {'name': 'The Axiom', 'colours': {'hull': '#e8ecf6', 'trim': '#4b3fd8', 'dark': '#2f2696', 'glow': '#3fe0e6'},
  'hull': {'n': 1.0, 'segs': 8, 'facet': True, 'plan': [0.62, 0.9, 1, 0.618, 0.382, 0.236, 0.02],
           'rise': [0.5, 0.8, 1, 0.8, 0.5, 0.3, 0.04], 'centre': [0, 0, 0, 0, 0, 0, 0]},
  'tradition': 'The tilers (exact sections at golden-ratio stations) and the branchers (fins iterated one level deeper per class)',
  'classes': [
    cls('axiom_ff', 'Lemma', 'Frigate', 'LM', 58, 0.38, 0.2, ['2 rule lances', '1 missile rack'], [fin(1)],
        [stripe('glow', 0.2, 0.8, -8, 8), stripe('trim', 0.4, 0.45, -180, 180)], 'A small truth, carried one step'),
    cls('axiom_dd', 'Corollary', 'Destroyer', 'CR', 86, 0.38, 0.2, ['4 rule lances', '2 missile racks'], [fin(2)],
        [stripe('glow', 0.15, 0.85, -8, 8), stripe('trim', 0.38, 0.44, -180, 180)], 'What follows from the Lemma'),
    cls('axiom_ca', 'Theorem', 'Cruiser', 'TH', 128, 0.38, 0.2, ['6 rule lances', '2 missile racks', 'iteration field'], [fin(3)],
        [stripe('glow', 0.1, 0.9, -8, 8), stripe('trim', 0.36, 0.42, -180, 180), stripe('trim', 0.6, 0.63, -180, 180)], 'A rule stated and proven'),
    cls('axiom_bb', 'Proof', 'Battleship', 'PR', 190, 0.38, 0.2, ['10 rule lances', '4 missile racks', 'iteration field'], [fin(4)],
        [stripe('glow', 0.06, 0.94, -8, 8), stripe('trim', 0.34, 0.4, -180, 180), stripe('trim', 0.58, 0.62, -180, 180)], 'The rule carried as deep as it goes'),
  ]}

# The Exponent: one collective that grows by taking in ships; nothing decorated, everything function. The assimilators
# set a near-box hull of modules; taken hulls stay on as pods of their own section; the conduit-keepers run a lumen line.
pod = lambda x, y, t0, t1, w, n=2.2: limb([[x * 0.4, y * 0.4, t0], [x, y, (t0 + t1) / 2], [x, y, t1]], [w * 0.6, w, w * 0.8],
                                       [w * 0.6, w, w * 0.8], n=n, segs=14)
F['exponent'] = {'name': 'The Exponent', 'colours': {'hull': '#5d686d', 'trim': '#8c4522', 'dark': '#262e31', 'glow': '#8be6dc', 'accent': '#d27a6a'},
  'hull': {'n': 5.0, 'segs': 20, 'facet': False, 'plan': [0.9, 1, 1, 1, 0.95, 0.85, 0.6],
           'rise': [0.85, 1, 1, 0.95, 0.9, 0.8, 0.55], 'centre': [0, 0, 0, 0, 0, 0, 0]},
  'tradition': 'The assimilators (a modular near-box hull, taken hulls kept on as pods) and the conduit-keepers (a lumen line)',
  'classes': [
    cls('exponent_ff', 'Tender', 'Frigate', 'TN', 64, 0.36, 0.26, ['2 cutting beams', '1 tractor'], [],
        [stripe('glow', 0.1, 0.9, -4, 4), stripe('trim', 0.0, 0.25, 60, 140)], 'Tends what the collective has taken'),
    cls('exponent_dd', 'Salvor', 'Destroyer', 'SV', 96, 0.36, 0.26, ['4 cutting beams', '2 tractors'],
        [limb([[0.5, -0.2, 0.7], [0.75, -0.25, 1.0], [0.55, -0.3, 1.25]], [0.18, 0.16, 0.1], [0.2, 0.18, 0.12], n=4, segs=12)],
        [stripe('glow', 0.1, 0.9, -4, 4), stripe('trim', 0.0, 0.3, 60, 140)], 'Clamp arms forward to take a wreck in'),
    cls('exponent_ca', 'Hulk', 'Cruiser', 'HK', 150, 0.4, 0.28, ['6 cutting beams', '2 missile racks', '2 tractors'],
        [pod(1.25, 0.1, -0.5, 0.2, 0.32), limb([[0, 0.6, -0.4], [0, 1.4, -0.1], [0, 1.4, 0.3]], [0.3, 0.38, 0.28], [0.3, 0.35, 0.25], n=3, segs=14, mirror=False)],
        [stripe('glow', 0.1, 0.9, -4, 4), stripe('accent', 0.55, 0.75, 30, 120)], 'Three hulls taken and grown into one'),
    cls('exponent_cv', 'Reef', 'Carrier', 'RF', 230, 0.44, 0.3, ['8 cutting beams', '4 tractors', '24 drone tenders'],
        [pod(1.3, 0.1, -0.6, 0.3, 0.34), pod(1.1, -0.6, -0.3, 0.5, 0.26, n=4),
         limb([[0, 0.6, -0.5], [0, 1.45, -0.2], [0, 1.45, 0.4]], [0.32, 0.4, 0.28], [0.3, 0.36, 0.26], n=3, segs=14, mirror=False)],
        [stripe('glow', 0.06, 0.94, -4, 4), stripe('accent', 0.3, 0.5, 40, 130), stripe('trim', 0.7, 0.85, -60, 60)], 'A drifting yard where the taken are rebuilt'),
  ]}

# The Red Tithe: raiders who take a tithe of blood from every crew they board. The boarding tradition sets everything:
# a ram keel to hold a hull, boarding tubes thrust forward, and the flagship carries the Hall where debts are posted.
ram = lambda: limb([[0, -0.5, 0.6], [0, -0.75, 1.0], [0, -0.8, 1.28]], [0.22, 0.14, 0.02], [0.35, 0.25, 0.04], n=1.2, segs=8, mirror=False)
tube = lambda y, t: limb([[0.6, y, t - 0.15], [1.05, y, t + 0.05], [1.15, y, t + 0.22]], [0.16, 0.14, 0.12], [0.16, 0.14, 0.12], n=2, segs=12)
F['redtithe'] = {'name': 'Red Tithe', 'colours': {'hull': '#2e3236', 'trim': '#5a1a12', 'dark': '#141618', 'glow': '#f0cf7a', 'accent': '#c8963a'},
  'hull': {'n': 1.5, 'segs': 16, 'facet': False, 'plan': [0.7, 0.95, 1, 0.85, 0.6, 0.35, 0.12],
           'rise': [0.7, 0.9, 1, 0.95, 0.8, 0.6, 0.3], 'centre': [0.1, 0.05, 0, 0, 0.05, 0.1, 0.2]},
  'tradition': 'Boarding: a ram keel to hold a prize, boarding tubes forward, the flagship carrying the Hall where debts are posted',
  'classes': [
    cls('redtithe_ff', 'Cutter', 'Frigate', 'CT', 54, 0.34, 0.24, ['2 cannons', '1 boarding tube'], [ram()],
        [stripe('trim', 0.55, 1.0, -30, 30), stripe('accent', 0.3, 0.33, -180, 180)], 'Runs a prize down and holds it'),
    cls('redtithe_dd', 'Reaver', 'Destroyer', 'RV', 82, 0.34, 0.24, ['4 cannons', '2 boarding tubes', '1 missile rack (red)'],
        [ram(), tube(0.0, 0.55)], [stripe('trim', 0.5, 1.0, -30, 30), stripe('accent', 0.28, 0.31, -180, 180)], 'Two tubes to put crews aboard'),
    cls('redtithe_ca', 'Bloodship', 'Cruiser', 'BS', 124, 0.36, 0.26, ['6 cannons', '4 boarding tubes', '2 missile racks (red, orange)'],
        [ram(), tube(0.2, 0.6), tube(-0.35, 0.45)], [stripe('trim', 0.45, 1.0, -30, 30), stripe('accent', 0.25, 0.28, -180, 180)],
        'Boards a cruiser from four sides at once'),
    cls('redtithe_fl', 'Tithe Hall', 'Flagship', 'TH', 176, 0.38, 0.26, ['8 cannons', '4 boarding tubes', '2 missile racks', 'the Hall'],
        [ram(), tube(0.2, 0.62), tube(-0.35, 0.47),
         limb([[0, 0.6, -0.55], [0, 1.5, -0.35], [0, 1.45, 0.05]], [0.4, 0.42, 0.3], [0.3, 0.32, 0.22], n=3.2, segs=14, mirror=False)],
        [stripe('trim', 0.45, 1.0, -30, 30), stripe('accent', 0.22, 0.25, -180, 180)], 'Where debts are posted and paid'),
  ]}

# The Vos Syndicate: free clans of raiders, salvage patched and grown over. Boarding forks forward (the clans take
# ships whole) and outrigger hulls bolted on from salvage.
forks = lambda: limb([[0.35, 0, 0.7], [0.55, 0, 1.05], [0.45, 0.05, 1.3]], [0.2, 0.14, 0.03], [0.22, 0.16, 0.04], n=1.6, segs=10)
outrig = lambda: limb([[0.6, 0, -0.4], [1.5, -0.1, -0.2], [1.6, -0.1, 0.35]], [0.14, 0.26, 0.2], [0.14, 0.24, 0.18], n=2.4, segs=14, glow_tip=False)
F['vos'] = {'name': 'Vos Syndicate', 'colours': {'hull': '#a8482a', 'trim': '#d6a043', 'dark': '#2a1410', 'glow': '#3fb7a3'},
  'hull': {'n': 2.4, 'segs': 20, 'facet': False, 'plan': [0.8, 1, 0.95, 0.8, 0.7, 0.6, 0.3],
           'rise': [0.8, 1, 0.95, 0.8, 0.65, 0.5, 0.3], 'centre': [0, 0, 0, 0, 0, 0, 0]},
  'tradition': 'Free clans who take ships whole: boarding forks forward, salvaged outrigger hulls',
  'classes': [
    cls('vos_ff', 'Skiff', 'Frigate', 'SK', 50, 0.4, 0.26, ['2 disruptors', '1 missile rack (red)'], [],
        [stripe('trim', 0.6, 0.9, -40, 40)], 'A clan\'s fast boat'),
    cls('vos_dd', 'Corsair', 'Destroyer', 'CS', 78, 0.4, 0.26, ['4 disruptors', '2 missile racks (red, orange)'], [forks()],
        [stripe('trim', 0.5, 0.85, -40, 40), stripe('glow', 0.0, 0.08, -180, 180)], 'The raider every clan starts from'),
    cls('vos_ca', 'Privateer', 'Cruiser', 'PV', 118, 0.42, 0.28, ['6 disruptors', '2 missile racks', '1 tractor'], [forks(), outrig()],
        [stripe('trim', 0.45, 0.85, -40, 40), stripe('glow', 0.0, 0.06, -180, 180)], 'Two salvaged hulls lashed to a third'),
    cls('vos_cap', 'Galleon', 'Capital', 'GL', 168, 0.44, 0.3, ['10 disruptors', '4 missile racks', '2 tractors'],
        [forks(), outrig(), limb([[0, 0.5, -0.5], [0, 1.3, -0.3], [0, 1.25, 0.1]], [0.28, 0.32, 0.24], [0.3, 0.32, 0.22], n=2.4, segs=14, mirror=False)],
        [stripe('trim', 0.4, 0.85, -40, 40), stripe('glow', 0.0, 0.05, -180, 180)], 'A clan\'s home and treasury'),
  ]}

# The Kethran Hegemony: an empire of rank; foundries and armour plate; machines proud of their mass. A wedge of
# faceted plate to a blade prow, blade wings aft, and a rank crest that grows with the ship's standing.
crest = lambda hgt: limb([[0, 0.6, -0.45], [0, 0.6 + hgt, -0.35], [0, 0.55 + hgt * 0.6, 0.2]], [0.12, 0.08, 0.02], [0.4, 0.3, 0.05], n=1, segs=4, mirror=False)
wing = lambda s: limb([[0.6, -0.1, -0.2], [1.2 * s + 0.5, -0.2, -0.45], [1.6 * s + 0.5, -0.25, -0.62]], [0.5, 0.35, 0.05], [0.12, 0.08, 0.02], n=1, segs=4)
F['kethran'] = {'name': 'Kethran Hegemony', 'colours': {'hull': '#4a4f55', 'trim': '#9e2a1e', 'dark': '#2b2f33', 'glow': '#ffb547', 'accent': '#d9483b'},
  'hull': {'n': 1.2, 'segs': 8, 'facet': True, 'plan': [0.95, 1, 0.95, 0.8, 0.55, 0.3, 0.04],
           'rise': [0.9, 1, 1, 0.85, 0.6, 0.35, 0.08], 'centre': [0, 0, 0, 0, -0.05, -0.1, -0.15]},
  'tradition': 'Rank and the foundry: faceted plate to a blade prow, blade wings, a rank crest that grows with standing',
  'classes': [
    cls('kethran_ff', 'Lancer', 'Frigate', 'LN', 60, 0.46, 0.22, ['2 heavy guns', '1 missile rack'], [wing(0.35)],
        [stripe('accent', 0.4, 0.43, -180, 180), stripe('trim', 0.0, 0.3, -20, 20)], 'The lowest rank that may fly alone'),
    cls('kethran_dd', 'Warden', 'Destroyer', 'WD', 92, 0.46, 0.22, ['4 heavy guns', '2 missile racks'], [wing(0.45), crest(0.3)],
        [stripe('accent', 0.38, 0.41, -180, 180), stripe('trim', 0.0, 0.35, -20, 20)], 'Holds a border world for its lord'),
    cls('kethran_ca', 'Bastion', 'Cruiser', 'BN', 140, 0.48, 0.24, ['6 heavy guns', '2 missile racks', 'spinal driver'], [wing(0.55), crest(0.55)],
        [stripe('accent', 0.36, 0.39, -180, 180), stripe('accent', 0.5, 0.52, -180, 180), stripe('trim', 0.0, 0.4, -20, 20)], 'A fortress that moves'),
    cls('kethran_dn', 'Throne', 'Dreadnought', 'TR', 210, 0.5, 0.25, ['10 heavy guns', '4 missile racks', 'spinal driver'], [wing(0.65), crest(0.9)],
        [stripe('accent', 0.34, 0.37, -180, 180), stripe('accent', 0.48, 0.5, -180, 180), stripe('trim', 0.0, 0.45, -20, 20)], 'A lord\'s seat in war'),
  ]}

# The Tarsine Remnant: a fallen empire that remembers being first and will not change; machines older than their crews,
# patched and never replaced. Monolithic hulls held by buttresses, the same plan at every size since the first age.
butt = lambda y: limb([[0, y * 0.5, -0.5], [0, y * 1.25, -0.3], [0, y * 1.2, 0.35]], [0.08, 0.06, 0.04], [0.4, 0.35, 0.15], n=1.4, segs=8, mirror=False)
F['tarsine'] = {'name': 'Tarsine Remnant', 'colours': {'hull': '#6a5e50', 'trim': '#7a3b1c', 'dark': '#1c1814', 'glow': '#d6c76a', 'accent': '#a88a46'},
  'hull': {'n': 3.2, 'segs': 16, 'facet': False, 'plan': [0.85, 1, 1, 1, 0.95, 0.85, 0.55],
           'rise': [0.9, 1, 1, 1, 0.95, 0.85, 0.6], 'centre': [0, 0, 0, 0, 0, 0, 0]},
  'tradition': 'Unchanged since the first age: one monolithic plan at every size, buttresses, lamps in arched windows',
  'classes': [
    cls('tarsine_ff', 'Votary', 'Frigate', 'VT', 72, 0.34, 0.3, ['2 ancient beams'], [butt(1)],
        [stripe('glow', 0.3, 0.7, 80, 100), stripe('accent', 0.9, 0.95, -180, 180)], 'Even the smallest is old and large'),
    cls('tarsine_dd', 'Custodian', 'Destroyer', 'CU', 110, 0.34, 0.3, ['4 ancient beams', '1 missile rack'], [butt(1), butt(-1)],
        [stripe('glow', 0.25, 0.75, 80, 100), stripe('accent', 0.9, 0.95, -180, 180)], 'Guards what the Remnant still holds'),
    cls('tarsine_ca', 'Monument', 'Cruiser', 'MN', 170, 0.34, 0.3, ['6 ancient beams', '2 missile racks'], [butt(1), butt(-1)],
        [stripe('glow', 0.2, 0.8, 80, 100), stripe('accent', 0.88, 0.93, -180, 180), stripe('trim', 0.0, 0.2, -180, 180)], 'Built to be seen from every world'),
    cls('tarsine_dn', 'Dominion', 'Dreadnought', 'DM', 270, 0.34, 0.3, ['12 ancient beams', '4 missile racks', 'the first-age cannon'], [butt(1), butt(-1)],
        [stripe('glow', 0.15, 0.85, 80, 100), stripe('accent', 0.86, 0.9, -180, 180), stripe('trim', 0.0, 0.25, -180, 180)], 'From when the Remnant ruled'),
  ]}

# The Aelith Conclave: patient knowledge kept over aeons; act rarely, act exactly; light shaped by thought in sculpted
# shells. A smooth core with shells that curve forward round it, more shells for older, larger ships.
shell = lambda r, y: limb([[0.4, y, -0.45], [r, y, -0.05], [r * 0.75, y, 0.45]], [0.06, 0.06, 0.03], [0.18, 0.22, 0.08], n=2, segs=12)
F['aelith'] = {'name': 'Aelith Conclave', 'colours': {'hull': '#e8e4da', 'trim': '#c9a646', 'dark': '#0f3a45', 'glow': '#6ff3ff'},
  'hull': {'n': 2.0, 'segs': 28, 'facet': False, 'plan': [0.5, 0.9, 1, 0.95, 0.8, 0.55, 0.15],
           'rise': [0.5, 0.8, 0.9, 0.85, 0.7, 0.5, 0.15], 'centre': [0, 0, 0, 0, 0, 0, 0]},
  'tradition': 'Shells round a core of light: a smooth core, gold shells curving forward, one more shell for each age of service',
  'classes': [
    cls('aelith_ff', 'Seeker', 'Frigate', 'SE', 56, 0.42, 0.24, ['2 light lances'], [shell(1.3, 0)],
        [stripe('glow', 0.35, 0.65, 70, 110), stripe('trim', 0.8, 0.85, -180, 180)], 'Sent to learn, rarely to fight'),
    cls('aelith_dd', 'Envoy', 'Destroyer', 'EN', 84, 0.42, 0.24, ['4 light lances', '1 missile rack'], [shell(1.35, 0.15)],
        [stripe('glow', 0.3, 0.7, 70, 110), stripe('trim', 0.8, 0.85, -180, 180)], 'Speaks for the Conclave'),
    cls('aelith_ca', 'Archive', 'Cruiser', 'AR', 130, 0.42, 0.24, ['6 light lances', '2 missile racks', 'mind field'], [shell(1.35, 0.25), shell(1.55, -0.25)],
        [stripe('glow', 0.25, 0.75, 70, 110), stripe('trim', 0.8, 0.85, -180, 180)], 'Carries what the Conclave knows'),
    cls('aelith_cap', 'Sanctum', 'Capital', 'SA', 196, 0.44, 0.26, ['10 light lances', '4 missile racks', 'mind field'], [shell(1.3, 0.35), shell(1.55, 0), shell(1.3, -0.35)],
        [stripe('glow', 0.2, 0.8, 70, 110), stripe('trim', 0.8, 0.85, -180, 180)], 'Where the eldest sit'),
  ]}

# The Ombrai: hidden purpose; they watch and are seldom seen; technology that looks back at you. Flat, dark, curved
# hulls, petals swept forward like a closed bloom, and a lit eye under the bow: the watchers' lamp.
petal = lambda s, y: limb([[0.6, y, -0.5], [1.3 * s + 0.3, y, -0.1], [0.9 * s + 0.3, y, 0.55]], [0.3, 0.45, 0.04], [0.08, 0.08, 0.02], n=2, segs=12)
F['ombrai'] = {'name': 'Ombrai', 'colours': {'hull': '#3b2a5c', 'trim': '#3b1763', 'dark': '#120822', 'glow': '#ff4fb0'},
  'hull': {'n': 2.2, 'segs': 24, 'facet': False, 'plan': [0.4, 0.85, 1, 1, 0.85, 0.55, 0.1],
           'rise': [0.35, 0.5, 0.55, 0.5, 0.4, 0.3, 0.1], 'centre': [0, 0, 0, 0, 0, 0, 0]},
  'tradition': 'The watchers: flat dark hulls, petals closed forward, a lit eye under the bow',
  'classes': [
    cls('ombrai_ff', 'Veil', 'Frigate', 'VL', 54, 0.5, 0.3, ['2 lances', 'cloak'], [petal(0.8, 0)],
        [stripe('glow', 0.82, 0.9, 150, 210), stripe('trim', 0.2, 0.6, -50, 50)], 'Seen once, then gone'),
    cls('ombrai_dd', 'Shade', 'Destroyer', 'SH', 82, 0.5, 0.3, ['4 lances', '1 missile rack', 'cloak'], [petal(1.0, 0)],
        [stripe('glow', 0.8, 0.9, 150, 210), stripe('trim', 0.2, 0.6, -50, 50)], 'Follows, and reports'),
    cls('ombrai_ca', 'Vigil', 'Cruiser', 'VG', 126, 0.52, 0.3, ['6 lances', '2 missile racks', 'cloak'], [petal(1.0, 0.05), petal(0.7, -0.1)],
        [stripe('glow', 0.78, 0.9, 150, 210), stripe('glow', 0.45, 0.5, 170, 190), stripe('trim', 0.2, 0.6, -50, 50)], 'Watches a whole system'),
    cls('ombrai_cap', 'Eclipse', 'Capital', 'EC', 186, 0.54, 0.3, ['10 lances', '4 missile racks', 'cloak'], [petal(1.1, 0.05), petal(0.8, -0.1)],
        [stripe('glow', 0.76, 0.9, 150, 210), stripe('glow', 0.42, 0.48, 170, 190), stripe('trim', 0.2, 0.6, -50, 50)], 'When it is seen, it is too late'),
  ]}

# The Vhorr Brood: the brood is one body; it grows, consumes and adapts; its ships are grown, not built. Hulls grown in
# chambers (the plan swells and narrows), carapace bands over lit tissue, feeding tendrils trailing aft.
tendril = lambda x, y, k: limb([[x * 0.5, y * 0.5, -0.4], [x, y, -0.9], [x * 1.3, y * 1.2, -1.2 - 0.15 * k]], [0.1, 0.07, 0.02], [0.1, 0.07, 0.02], n=2, segs=10, glow_tip=True)
F['vhorr'] = {'name': 'Vhorr Brood', 'colours': {'hull': '#5a3424', 'trim': '#2b1d2a', 'dark': '#2e1814', 'glow': '#a6e34a'},
  'hull': {'n': 2.0, 'segs': 24, 'facet': False, 'plan': [0.5, 0.9, 1, 0.75, 0.95, 0.7, 0.3],
           'rise': [0.5, 0.85, 1, 0.75, 0.9, 0.7, 0.35], 'centre': [0, 0, 0, 0, 0, 0, 0]},
  'tradition': 'Grown, not built: hulls grown in chambers, carapace bands over lit tissue, feeding tendrils aft',
  'classes': [
    cls('vhorr_ff', 'Drone', 'Frigate', 'DR', 52, 0.44, 0.34, ['2 bile spitters'], [tendril(0.5, 0, 0)],
        [stripe('trim', 0.38, 0.5, -60, 60), stripe('glow', 0.5, 0.55, -180, 180)], 'Spawned by the hundred'),
    cls('vhorr_dd', 'Ravager', 'Destroyer', 'RA', 80, 0.44, 0.34, ['4 bile spitters', '1 spore rack'], [tendril(0.5, 0.2, 0), tendril(0.4, -0.3, 1)],
        [stripe('trim', 0.38, 0.5, -60, 60), stripe('trim', 0.68, 0.8, -60, 60), stripe('glow', 0.5, 0.55, -180, 180)], 'Feeds on what it breaks'),
    cls('vhorr_ca', 'Broodship', 'Cruiser', 'BR', 130, 0.46, 0.34, ['6 bile spitters', '2 spore racks', '12 swarmers'],
        [tendril(0.5, 0.25, 0), tendril(0.45, -0.3, 1), tendril(0.25, 0.6, 2)],
        [stripe('trim', 0.36, 0.5, -60, 60), stripe('trim', 0.66, 0.8, -60, 60), stripe('glow', 0.5, 0.55, -180, 180)], 'Carries a brood to a new world'),
    cls('vhorr_cv', 'Hive', 'Carrier', 'HV', 220, 0.5, 0.36, ['10 bile spitters', '4 spore racks', '48 swarmers'],
        [tendril(0.5, 0.25, 0), tendril(0.45, -0.3, 1), tendril(0.25, 0.6, 2), tendril(0.3, -0.7, 3)],
        [stripe('trim', 0.34, 0.5, -60, 60), stripe('trim', 0.64, 0.8, -60, 60), stripe('glow', 0.5, 0.55, -180, 180), stripe('glow', 0.25, 0.3, -180, 180)], 'The body\'s heart in a system'),
  ]}

DOC = ('Placeholder ship lines for the nine factions with no designed ships (owner, 2026-11-08: "generate place holders for '
       'all of their ships, class design is your discretion"). Written by tools/factions/placeholder_fleets.py; built by '
       'docs/mockups/lib/placeholder-hulls.js; shown on mockup 36 (docs/mockups/placeholder-fleets.html). Profiles run stern '
       '(t 0) to bow (t 1); limb paths are in fractions of (half-beam, half-height, half-length). Placeholders until each '
       'faction\'s ship mockup replaces them; the armament is a role sketch, not tuning.')
data = {'_doc': DOC, 'factions': F}
text = json.dumps(data, indent=1) + '\n'
if '--check' in sys.argv:
    sys.exit(0 if OUT.read_text() == text else 1)
OUT.write_text(text)
print('wrote', OUT.relative_to(ROOT), sum(len(f['classes']) for f in F.values()), 'classes')
