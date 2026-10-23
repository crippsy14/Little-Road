# Traffic and racecraft summary of race.json files (docs/94). Per race, from the 1 Hz motion samples and the per-car
# results: how much of the race traffic bound the speed and by how much the car was below its own plan, how much of it
# was spent attacking, passes against attacks, seconds lost behind traffic, and how far the finishing order moved from the
# grid (Spearman rank correlation: 1 = the grid order, 0 = unrelated). --slots adds a per-grid-slot table (mean over the
# given races) of time behind the winner, traffic seconds and passes.
# Usage: python tools/racer-traffic.py review/unity-35/frame-cost-257/*-off/race.json [--slots]
import json, sys, glob, os, statistics, collections

def spearman(a, b):
    n = len(a)
    if n < 3: return 0
    ra = {v: i for i, v in enumerate(sorted(a))}; rb = {v: i for i, v in enumerate(sorted(b))}
    d2 = sum((ra[x] - rb[y]) ** 2 for x, y in zip(a, b))
    return 1 - 6 * d2 / (n * (n * n - 1))

def summary(path):
    r = json.load(open(path, encoding='utf-8')); cars = r['cars']
    roster = os.path.join(os.path.dirname(path), 'race.roster.json')
    slot = {e['id']: e['slot'] for e in json.load(open(roster, encoding='utf-8'))['entries']} if os.path.exists(roster) else {c['id']: c['id'] for c in cars}
    finish = {c['id']: c['time'] for c in cars}
    m = [s for s in r.get('motion', []) if s['time'] > 1 and s['speed'] > 1 and s['time'] < finish[s['id']]]
    follow = [s for s in m if s['reason'] in (1, 2)]
    early = [s for s in m if s['time'] < 15]; early_follow = [s for s in early if s['reason'] in (1, 2)]
    win = min(finish.values())
    order = sorted(cars, key=lambda c: c['time'])
    row = {
        'run': os.path.basename(os.path.dirname(path)),
        'total': sum(finish.values()) if all(c['finished'] for c in cars) else None,
        'behind': statistics.mean(c['time'] - win for c in cars),
        'follow%': 100 * len(follow) / max(1, len(m)),
        'deficit': statistics.mean(s['targetSpeed'] - s['speed'] for s in follow) if follow else 0,
        'early%': 100 * len(early_follow) / max(1, len(early)),
        'attack%': 100 * sum(s['intent'] == 2 for s in m) / max(1, len(m)),
        'loss': statistics.mean(c.get('trafficLossSeconds', 0) for c in cars),
        'lanes': sum(c.get('laneChanges', 0) for c in cars),
        'reversals': sum(c.get('planReversals', 0) for c in cars),
        'passes': sum(c.get('passCompleted', 0) for c in cars), 'attacks': sum(c.get('passCommits', 0) for c in cars),
        'gridRho': spearman([slot[c['id']] for c in order], list(range(len(order)))),
        'contacts': sum(c['contacts'] for c in cars) // 2,
        'hard': len([i for i in r.get('impacts', []) if i['other'] >= 0 and i['relativeSpeed'] > 6]) // 2,
        'resets': len(r.get('assistedRecoveries', [])),
    }
    per_slot = {slot[c['id']]: (c['time'] - win, c.get('trafficSeconds', 0), c.get('passCompleted', 0), c.get('passCommits', 0)) for c in cars}
    return row, per_slot

args = [a for a in sys.argv[1:] if not a.startswith('--')]
paths = sorted(p for a in args for p in glob.glob(a))
rows = []; slots = collections.defaultdict(list)
for p in paths:
    row, per = summary(p); rows.append(row)
    for s, v in per.items(): slots[s].append(v)
cols = ['run', 'total', 'behind', 'follow%', 'deficit', 'early%', 'attack%', 'loss', 'lanes', 'reversals', 'passes', 'attacks', 'gridRho', 'contacts', 'hard', 'resets']
fmt = lambda v: '-' if v is None else (f"{v:.2f}" if isinstance(v, float) and abs(v) < 2 else f"{v:.1f}" if isinstance(v, float) else str(v))
print('\t'.join(cols))
for row in rows: print('\t'.join(fmt(row[c]) for c in cols))
if '--slots' in sys.argv:
    print('\nslot\tbehind\ttraffic\tpasses\tattacks')
    for s in sorted(slots):
        v = slots[s]; print(f"{s}\t{statistics.mean(x[0] for x in v):.1f}\t{statistics.mean(x[1] for x in v):.1f}\t{statistics.mean(x[2] for x in v):.1f}\t{statistics.mean(x[3] for x in v):.1f}")
