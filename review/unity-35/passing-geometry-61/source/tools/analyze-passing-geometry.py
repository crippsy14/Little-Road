"""Describe counterfactual forecasts, keeping stationary and moving cases separate."""
import json
import sys
from collections import Counter
from pathlib import Path

path = Path(sys.argv[1])
report = json.loads(path.read_text(encoding='utf-8-sig'))
if not report['observations'] or report['originalMatches'] != report['observations']:
    raise SystemExit('Original forecasts must all reproduce before interpreting alternatives.')
groups = {}
for group, rows in (
    ('all', report['rows']),
    ('speed_below_2_mps', [r for r in report['rows'] if r['speed'] < 2]),
    ('moving_at_least_2_mps', [r for r in report['rows'] if r['speed'] >= 2]),
):
    summary = []
    for name in [a['name'] for a in report['rows'][0]['alternatives']]:
        gains = []
        rejected = Counter()
        for row in rows:
            scores = {a['name']: a for a in row['alternatives']}
            candidate, baseline = scores[name], scores['fresh-1.00']
            if candidate['nominalAccepted'] and candidate['trafficRoadContained']:
                gains.append(candidate['routeGain'] - baseline['routeGain'])
            else:
                rejected[candidate['nominalReason'] if not candidate['nominalAccepted'] else 'traffic-road'] += 1
        summary.append(dict(name=name, feasible=len(gains), observations=len(rows),
                            meanMetresVsFresh=sum(gains)/len(gains) if gains else None,
                            minimumMetresVsFresh=min(gains) if gains else None,
                            maximumMetresVsFresh=max(gains) if gains else None,
                            rejections=dict(rejected)))
    groups[group] = summary
result = dict(scope='Fixed two-second counterfactuals with constant observed rival speed; capped selected captures. Feasibility is not live safety certification and small score differences are not lap-time improvements.',
              observations=report['observations'], originalMatches=report['originalMatches'],
              timestampRange=[min(r['timestamp'] for r in report['rows']), max(r['timestamp'] for r in report['rows'])],
              groups=groups)
(path.parent/'summary.json').write_text(json.dumps(result, indent=2)+'\n', encoding='utf-8')
print(json.dumps({k:v for k,v in result.items() if k!='groups'}, indent=2))
print(json.dumps(groups['moving_at_least_2_mps'], indent=2))
