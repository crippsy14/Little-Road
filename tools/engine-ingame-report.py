# One table of the latest in-game scores (docs/55): python tools/engine-ingame-report.py [--offline] [measure ...]
# Each engine's overall and every measure; name measures (pitch tone chipmunk garble clicks liftoff shift rise) to see
# their notes as well.
import json, os, sys
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
tag = '-offline' if '--offline' in sys.argv else ''; want = [a for a in sys.argv[1:] if not a.startswith('--')]
KEYS = ['pitch', 'tone', 'chipmunk', 'garble', 'clicks', 'liftoff', 'shift', 'rise']
order = ['engine_stock', 'engine_workshop', 'engine_club', 'engine_sport16', 'engine_valley', 'engine_b_twincam', 'engine_b_highcomp', 'engine_b_v6',
         'engine_a_touring', 'engine_a_evo', 'engine_a_works', 'engine_s_v6', 'engine_s_v8', 'engine_s_v10', 'engine_e_v6', 'engine_e_v8', 'engine_e_12h', 'engine_e_economy']
print(f"{'engine':24} {'all':>4} " + ' '.join(f'{k[:6]:>6}' for k in KEYS)); vals = []
for e in order:
    p = os.path.join(ROOT, 'review', 'engine-ingame', e, f'ingame{tag}.json')
    if not os.path.exists(p): continue
    d = json.load(open(p)); g = {r['key']: r for r in d['rows']}; vals.append(d['overall'])
    print(f"{d['name'][:24]:24} {d['overall']:4.1f} " + ' '.join(f"{g[k]['score']:6.1f}" if k in g else f"{'-':>6}" for k in KEYS))
    for k in want:
        if k in g: print(f"{'':29}{k}: {g[k]['note']}")
if vals: print(f"mean {sum(vals)/len(vals):.2f}, min {min(vals):.1f}, at or above 8: {sum(v >= 8 for v in vals)} of {len(vals)}")
