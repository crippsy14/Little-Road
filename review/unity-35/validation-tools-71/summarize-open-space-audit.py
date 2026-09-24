"""Summarise the observation-only71 failed-plan capture; no acceptance changes."""
import json
from collections import Counter
from pathlib import Path

root = Path('review/unity-35/open-space-71/no-room-audit')
read = lambda p: json.loads(p.read_text(encoding='utf-8-sig'))
track = read(root / 'race.track.json')
parity = read(root / 'motion-parity.json')
assert parity['passed'], 'Diagnostic must reproduce the original decisions and motion'
audits = track['failedPlanAudits']
entries = [e for a in audits for e in a['entries']]
rows = []
for e in entries:
    own = e['self']
    traffic = [dict(id=t['id'], routeGap=t['route']-own['route'], offset=t['offset'], speed=t['speed'])
               for t in e['traffic'] if t['id'] != own['id']]
    rows.append(dict(clock=e['timestamp'], id=own['id'], route=own['route'], offset=own['offset'],
                     left=e['left'], right=e['right'], traffic=traffic,
                     reasons=dict(Counter(c['reason'] for c in e['candidates'])),
                     rejected=[dict(reason=c['reason'], afterSeconds=c['afterSeconds'],
                                    offset=c['offset'], target=c['path']['selectedOffset'])
                               for c in e['candidates']]))
out = dict(scope='Failed selection events, not 10 Hz unresolved samples. Clock includes countdown; race sample time does not. Raw evidence retained. No proof that an alternative path would pass all checks.',
           exactDiagnosticParity=True, dropped=sum(a['dropped'] for a in audits),
           events=len(entries), candidateReasons=dict(Counter(c['reason'] for e in entries for c in e['candidates'])),
           observations=rows)
(root / 'rejection-summary.json').write_text(json.dumps(out, indent=2)+'\n', encoding='utf-8')
print(json.dumps({k:v for k,v in out.items() if k != 'observations'}, indent=2))
