# Summarise race.json files for the racing AI comparison (docs/46).
# Usage: python tools/summarize-racer.py review/unity-35/frame-cost-201/*/race.json
# Per race: finishers, total finish time of all cars (null if anyone fails), assisted resets, contacts,
# hard impacts, and per-car means of braking/lifting/stopped seconds, plus v2 intent counts.
import json, sys, glob, os

def summary(path):
    r = json.load(open(path, encoding='utf-8'))
    cars = r['cars']
    finished = [c for c in cars if c['finished']]
    total = sum(c['time'] for c in cars) if len(finished) == len(cars) else None
    impacts = r.get('impacts', [])
    hard = [i for i in impacts if i['other'] >= 0 and i['relativeSpeed'] > 6]
    mean = lambda key: sum(c.get(key, 0) for c in cars) / len(cars)
    return {
        'run': os.path.basename(os.path.dirname(path)),
        'policy': r.get('driverPolicy'),
        'finished': f"{len(finished)}/{len(cars)}",
        'total': total,
        'winner': min((c['time'] for c in finished), default=None),
        'last': max((c['time'] for c in finished), default=None),
        'resets': len(r.get('assistedRecoveries', [])),
        'contacts': sum(c['contacts'] for c in cars) // 2,
        'hard': len(hard) // 2,
        'maxImpact': max((c['maxImpact'] for c in cars), default=0),
        'brake': mean('brakeSeconds'), 'lift': mean('liftSeconds'), 'stopped': mean('stoppedSeconds'),
        'traffic': mean('trafficSeconds'), 'guard': mean('guardSeconds'),
        # Racecraft v3 (docs/94): seconds lost behind traffic, seconds attacking (per car), lane changes (whole field).
        'loss': mean('trafficLossSeconds'), 'attackS': mean('attackSeconds'), 'lanes': sum(c.get('laneChanges', 0) for c in cars),
        'attacks': sum(c.get('passCommits', 0) for c in cars), 'passes': sum(c.get('passCompleted', 0) for c in cars),
        'aborts': sum(c.get('passAborted', 0) for c in cars), 'defends': sum(c.get('defendMoves', 0) for c in cars),
        'reversals': sum(c.get('planReversals', 0) for c in cars),
    }

paths = [p for a in sys.argv[1:] for p in glob.glob(a)]
rows = [summary(p) for p in sorted(paths)]
cols = ['run', 'policy', 'finished', 'total', 'winner', 'last', 'resets', 'contacts', 'hard', 'maxImpact', 'brake', 'lift', 'stopped', 'traffic', 'guard', 'loss', 'attackS', 'lanes', 'attacks', 'passes', 'aborts', 'defends', 'reversals']
fmt = lambda v: '-' if v is None else (f"{v:.1f}" if isinstance(v, float) else str(v))
print('\t'.join(cols))
for row in rows:
    print('\t'.join(fmt(row[c]) for c in cols))
