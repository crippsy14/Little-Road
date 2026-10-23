# Held-up seconds (docs/94; Richard, 23 Oct: "when a car is faster but behind a car in front, they coast at < 100% throttle
# for a while"). From the 1 Hz motion samples: a car is "held" when traffic binds its speed (Follow or Guard) and its own
# plan is at least `surplus` m/s faster than it is going. A lane is "free" when some lane across the road (centre, or the
# car's half-width plus 1.3 m inside either edge of the usable corridor) has no other car within 25 m ahead or 6 m behind
# covering it. Reports per race: held seconds per car, the share of them with a free lane beside (the time a pass was there
# for the taking), and how many of those seconds the car was in the Attack state.
# Usage: python tools/racer-held.py review/unity-35/frame-cost-260/*/race.json [--surplus 3]
import json, sys, glob, os, collections

args = [a for a in sys.argv[1:] if not a.startswith('--')]
surplus = float(sys.argv[sys.argv.index('--surplus') + 1]) if '--surplus' in sys.argv else 3.0
HALF_W = 0.9; CAR_LEN = 4.4

def summary(path):
    r = json.load(open(path, encoding='utf-8')); cars = r['cars']
    half_road = r.get('trackWidth', 16) * .5; edge = min(half_road - HALF_W - .25, 5.6 if half_road >= 8 else half_road - 1.6)
    lanes = [-edge + 1.3, 0, edge - 1.3]
    finish = {c['id']: c['time'] for c in cars}
    by_time = collections.defaultdict(list)
    for s in r['motion']: by_time[round(s['time'])].append(s)
    held = free = free_attack = free_straight = free_straight_race = 0
    for t, samples in by_time.items():
        for s in samples:
            if s['time'] < 2 or s['time'] >= finish[s['id']] or s['reason'] not in (1, 2): continue
            if s['targetSpeed'] - s['speed'] < surplus: continue
            held += 1
            near = [o for o in samples if o['id'] != s['id'] and -6 < o['progress'] - s['progress'] < 25]
            if any(all(abs(o['offset'] - lane) >= 2 * HALF_W + .3 for o in near) for lane in lanes):
                free += 1
                if s['intent'] == 2: free_attack += 1
                # Straight enough to move across (steering under a quarter lock), and not already attacking: the seconds a
                # pass was there and nothing was being done about it.
                if abs(s['steering']) < .25:
                    free_straight += 1
                    if s['intent'] != 2: free_straight_race += 1
    n = len(cars)
    return {'run': os.path.basename(os.path.dirname(path)), 'held/car': held / n, 'free/car': free / n,
            'free%': 100 * free / max(1, held), 'attacking%': 100 * free_attack / max(1, free),
            'straight/car': free_straight / n, 'idle/car': free_straight_race / n}

rows = [summary(p) for a in args for p in sorted(glob.glob(a))]
cols = ['run', 'held/car', 'free/car', 'free%', 'attacking%', 'straight/car', 'idle/car']
print('\t'.join(cols))
for row in rows: print('\t'.join(f"{row[c]:.1f}" if isinstance(row[c], float) else str(row[c]) for c in cols))
