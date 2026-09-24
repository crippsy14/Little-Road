"""Aggregate final live feasibility timing, separately from rendered frame cost."""
import argparse
import json
from pathlib import Path

p = argparse.ArgumentParser()
p.add_argument('folder', type=Path)
args = p.parse_args()
track = json.loads((args.folder / 'race.track.json').read_text(encoding='utf-8-sig'))
rows = track['roadCosts']
totals = {k: sum(r[k] for r in rows) for k in ('calls', 'forecastMs', 'corridorMs', 'sceneryMs', 'roadFailures', 'corridorFailures', 'sceneryFailures')}
decision = sum(r['totalMs'] for r in track['contextDecisionCosts'])
timed = sum(totals[k] for k in ('forecastMs', 'corridorMs', 'sceneryMs'))
out = dict(scope='Live cumulative main-thread feasibility stage timing, includes measurement overhead. Concurrent forecast worker time and dispatch/join are recorded in driver-frame-analysis.json, not in these decision-region percentages; not rendered-frame acceptance.', totals=totals,
           driverMs=decision, remainingDriverMs=decision-timed,
           percentDriver={k: totals[k]*100/decision for k in ('forecastMs', 'corridorMs', 'sceneryMs')},
           forecastMeanMs=totals['forecastMs']/max(1, totals['calls']))
out['forecastExecution']={k:sum(r.get(k,0) for r in rows) for k in ('preparedRequests','prefetchedCalls','synchronousCalls')}
(args.folder / 'road-cost-analysis.json').write_text(json.dumps(out, indent=2)+'\n', encoding='utf-8')
print(json.dumps(out, indent=2))
