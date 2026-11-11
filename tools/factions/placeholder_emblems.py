#!/usr/bin/env python3
"""Write the placeholder emblems of the nine factions with no designed emblem (owner, 2026-11-08: "a place holder logo for
the factions too"): assets/emblems/<faction>/placeholder.svg for the game, and assets/emblems/placeholders.json (the same
SVG text, by faction) for mockup 36. Each is drawn from a tradition in the faction's written culture
(openspec/changes/console-skins/design.md), never from the species' body (CLAUDE.md section 1). Placeholders until each
faction's emblem goes through the Emblem Design Guide (docs/design/emblem-guide.html).

    python3 tools/factions/placeholder_emblems.py [--check]
"""
import json, math, pathlib, sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
P = math.pi

def f(v): return f'{v:.2f}'.rstrip('0').rstrip('.')
def pts(ps): return ' '.join(f'{f(x)},{f(y)}' for x, y in ps)
def poly(ps, fill, stroke='none', sw=0): return f'<polygon points="{pts(ps)}" fill="{fill}" stroke="{stroke}" stroke-width="{f(sw)}" stroke-linejoin="round"/>'
def line(ps, stroke, sw): return f'<polyline points="{pts(ps)}" fill="none" stroke="{stroke}" stroke-width="{f(sw)}" stroke-linecap="round" stroke-linejoin="round"/>'
def circ(cx, cy, r, fill, stroke='none', sw=0): return f'<circle cx="{f(cx)}" cy="{f(cy)}" r="{f(r)}" fill="{fill}" stroke="{stroke}" stroke-width="{f(sw)}"/>'
def path(d, fill, stroke='none', sw=0): return f'<path d="{d}" fill="{fill}" stroke="{stroke}" stroke-width="{f(sw)}" stroke-linejoin="round"/>'
def svg(body, bg): return ('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100" width="256" height="256">'
                           + circ(50, 50, 48, bg) + ''.join(body) + '</svg>')
def ngon(cx, cy, r, n, a0=-P / 2): return [(cx + r * math.cos(a0 + 2 * P * i / n), cy + r * math.sin(a0 + 2 * P * i / n)) for i in range(n)]

def koch(ps, depth):
    for _ in range(depth):
        out = []
        for i in range(len(ps)):
            (x0, y0), (x1, y1) = ps[i], ps[(i + 1) % len(ps)]
            dx, dy = (x1 - x0) / 3, (y1 - y0) / 3
            a, b = (x0 + dx, y0 + dy), (x0 + 2 * dx, y0 + 2 * dy)
            c = (a[0] + dx * 0.5 + dy * math.sqrt(3) / 2, a[1] + dy * 0.5 - dx * math.sqrt(3) / 2)
            out += [(x0, y0), a, c, b]
        ps = out
    return ps

E = {}
# The Axiom: the tilers' rule carried two levels deep (a Koch snowflake), gold at the point where it converges.
E['axiom'] = {'why': 'A rule and its iteration: a Koch snowflake carried two levels, converging on a gold point',
  'svg': svg([poly(koch(ngon(50, 50, 38, 3, P / 2), 2), '#05060c', '#4b3fd8', 2.4), poly(koch(ngon(50, 50, 19, 3, -P / 2), 1), 'none', '#3fe0e6', 1.6),
              circ(50, 50, 4.5, '#f2c14e')], '#e8ecf6')}
# The Exponent: seven unlike plates bolted into a ring round a branching conduit (the assimilators and the conduit-keepers).
ring = []
for i, w in enumerate([46, 58, 40, 52, 44, 60, 60]):
    a0 = sum([46, 58, 40, 52, 44, 60, 60][:i]) * P / 180 - P / 2; a1 = a0 + (w - 6) * P / 180
    r0, r1 = 26 + (i % 3) * 1.5, 40 - (i % 2) * 2
    ring.append(path(f'M{f(50 + r0 * math.cos(a0))},{f(50 + r0 * math.sin(a0))} A{f(r0)},{f(r0)} 0 0 1 {f(50 + r0 * math.cos(a1))},{f(50 + r0 * math.sin(a1))} '
                     f'L{f(50 + r1 * math.cos(a1))},{f(50 + r1 * math.sin(a1))} A{f(r1)},{f(r1)} 0 0 0 {f(50 + r1 * math.cos(a0))},{f(50 + r1 * math.sin(a0))} Z',
                     ['#5d686d', '#8c4522', '#5d686d', '#46634c', '#5d686d', '#8c4522', '#7d776a'][i], '#0b1113', 1.2))
E['exponent'] = {'why': 'Seven unlike plates bolted into one ring round a branching conduit: what is taken is kept',
  'svg': svg(ring + [line([(50, 70), (50, 50), (40, 38)], '#8be6dc', 3), line([(50, 50), (60, 36)], '#8be6dc', 3),
                     line([(50, 58), (38, 54)], '#8be6dc', 2.2), line([(50, 58), (62, 55)], '#8be6dc', 2.2), circ(50, 50, 3.4, '#e2fffa')], '#0d1315')}
# The Red Tithe: two crossed boarding blades over the drop of the tithe, on an oak roundel.
blade = lambda s: poly([(50 + s * 30, 78), (50 + s * 25, 82), (50 - s * 22, 24), (50 - s * 30, 16), (50 - s * 26, 28)], '#c9c2b4', '#141618', 1.4)
E['redtithe'] = {'why': 'Two crossed boarding blades over the drop of blood each boarded crew pays',
  'svg': svg([circ(50, 50, 44, '#4a301c', '#24160b', 2), blade(1), blade(-1),
              path('M50,34 C56,46 62,53 62,60 A12,12 0 0 1 38,60 C38,53 44,46 50,34 Z', '#c4141c', '#33100a', 1.6),
              circ(46, 59, 2.4, '#ff8a8a')], '#1a100a')}
# The Vos Syndicate: a spiral (the crews' knotwork) cut into a coin of loot and luck.
sp = [(50 + (3 + 3.1 * t) * math.cos(t), 50 + (3 + 3.1 * t) * math.sin(t)) for t in [i * 0.2 for i in range(0, 52)]]
E['vos'] = {'why': 'The crews\' spiral cut into a coin: loot and luck',
  'svg': svg([circ(50, 50, 40, '#d6a043', '#2a1410', 3), circ(50, 50, 33, 'none', '#2a1410', 1.4), line(sp, '#2a1410', 4),
              circ(50, 50, 3, '#3fb7a3')], '#efe0bd')}
# The Kethran Hegemony: the foundry's prow between two blade chevrons of rank, red enamel inlaid.
E['kethran'] = {'why': 'A forged prow between two blade chevrons of rank',
  'svg': svg([poly([(50, 14), (64, 46), (56, 82), (44, 82), (36, 46)], '#4a4f55', '#121417', 2), poly([(50, 26), (57, 46), (50, 70), (43, 46)], '#d9483b'),
              poly([(30, 30), (40, 52), (30, 80), (18, 80), (26, 52), (18, 30)], '#8a9096', '#121417', 1.6),
              poly([(70, 30), (60, 52), (70, 80), (82, 80), (74, 52), (82, 30)], '#8a9096', '#121417', 1.6)], '#2b2f33')}
# The Tarsine Remnant: the arched tablet of the first age, its lamp still lit behind the grime.
E['tarsine'] = {'why': 'The first age\'s arched tablet, its lamp still lit',
  'svg': svg([path('M26,84 L26,44 A24,24 0 0 1 74,44 L74,84 Z', '#3a332c', '#1c1814', 2.4), path('M33,78 L33,46 A17,17 0 0 1 67,46 L67,78 Z', '#1c1814'),
              circ(50, 48, 10, '#d6c76a'), circ(50, 48, 15, 'none', '#a88a46', 1.6), line([(30, 84), (70, 84)], '#a88a46', 3)], '#7a3b1c')}
# The Aelith Conclave: a lozenge of light held between two gold shells.
E['aelith'] = {'why': 'Light held between two shells: act rarely, act exactly',
  'svg': svg([path('M30,16 C12,34 12,66 30,84 C22,64 22,36 30,16 Z', '#c9a646', '#6a4a14', 1.4),
              path('M70,16 C88,34 88,66 70,84 C78,64 78,36 70,16 Z', '#c9a646', '#6a4a14', 1.4),
              poly([(50, 18), (62, 50), (50, 82), (38, 50)], '#6ff3ff', '#dffcff', 1.2), poly([(50, 34), (55, 50), (50, 66), (45, 50)], '#dffcff')], '#0b2a33')}
# The Ombrai: rings round a watching point, a crescent closing over it (they watch and are seldom seen).
E['ombrai'] = {'why': 'Rings round a watching point, a crescent veil closing over it',
  'svg': svg([circ(50, 50, 38, 'none', '#3b1763', 4), circ(50, 50, 28, 'none', '#c58cff', 1.6), circ(50, 50, 16, '#ff4fb0'), circ(50, 50, 6, '#f4e6ff'),
              path('M14,46 A36,36 0 0 1 86,46 A40,30 0 0 0 14,46 Z', '#120822', '#c58cff', 1)], '#3b2a5c')}
# The Vhorr Brood: one body growing: a ring of chambers round a lit heart.
cells = [circ(50 + 22 * math.cos(a), 50 + 22 * math.sin(a), 11, '#5a3424', '#2b1d2a', 2) for a in [-P / 2 + 2 * P * i / 6 for i in range(6)]]
E['vhorr'] = {'why': 'One body growing: six chambers round a lit heart',
  'svg': svg(cells + [circ(50, 50, 12, '#a6e34a', '#2b1d2a', 2), circ(50, 50, 5, '#e4ff9a')], '#2e1814')}

ok = True
for k, v in E.items():
    p = ROOT / 'assets/emblems' / k / 'placeholder.svg'
    if '--check' in sys.argv:
        ok &= p.exists() and p.read_text() == v['svg'] + '\n'
    else:
        p.parent.mkdir(parents=True, exist_ok=True); p.write_text(v['svg'] + '\n')
J = ROOT / 'assets/emblems/placeholders.json'
text = json.dumps({'_doc': 'Placeholder emblems, by faction: the SVG of assets/emblems/<faction>/placeholder.svg and why it is drawn so. '
                   'Written by tools/factions/placeholder_emblems.py.', 'emblems': E}, indent=1) + '\n'
if '--check' in sys.argv:
    sys.exit(0 if ok and J.read_text() == text else 1)
J.write_text(text); print('wrote', len(E), 'emblems')
