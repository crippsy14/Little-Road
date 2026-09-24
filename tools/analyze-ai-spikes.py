"""Attribute captured AI wall-time spikes without changing the simulation."""
import argparse
import json
import math
from pathlib import Path

p = argparse.ArgumentParser()
p.add_argument('root', type=Path)
args = p.parse_args()
read = lambda path: json.loads(path.read_text(encoding='utf-8-sig'))
evidence = read(args.root / 'race.driver-frames.json')
track = read(args.root / 'race.track.json')
rows = evidence['frames'][1:]  # Same single loading-frame exclusion as frame analysis.
ai = lambda row: row['decisionMs'] + row['snapshotMs']

def stats(items):
    values = sorted(items)
    if not values:
        return None
    return dict(count=len(values), mean=sum(values)/len(values),
                p95=values[math.ceil(.95*len(values))-1],
                maximum=values[-1])

tail = sorted(rows, key=ai, reverse=True)[:max(1, math.ceil(.05*len(rows)))]
total = sum(map(ai, tail))
indices = {row['frame']: i for i, row in enumerate(rows)}
examples = []
for row in tail[:12]:
    i = indices[row['frame']]
    examples.append(dict(frame=row['frame'], simTime=row['simTime'],
                         allAiMs=ai(row), decisionMs=row['decisionMs'],
                         snapshotMs=row['snapshotMs'], forecastBatchMs=row['forecastBatchMs'],
                         snapshotCalls=row['snapshotCalls'], forecastJobs=row['forecastJobs'],
                         nextCapturedFrameMs=rows[i+1]['frameMs'] if i+1<len(rows) else None))

keys = ('forecastMs', 'corridorMs', 'sceneryMs', 'trafficMs', 'trafficProgressMs',
        'trafficProgressRivalMs', 'trafficProgressProposalMs', 'trafficProgressContinuationMs')
out = dict(
    scope='Descriptive attribution, not a new acceptance gate. First loading frame excluded; countdown retained. '
          'Main-thread decision and snapshot regions are disjoint. Forecast batch is nested in snapshot; '
          'traffic-progress component timers are nested in its total. Never add nested timers together. '
          'Unity delta time measures an elapsed frame interval; nextCapturedFrameMs is shown as a diagnostic '
          'adjacent interval, not asserted as exact GPU/presentation attribution. No causal timing claim from correlation.',
    planningBatchFrames=stats(ai(r) for r in rows if r['forecastBatches']),
    otherFrames=stats(ai(r) for r in rows if not r['forecastBatches']),
    topFivePercent=dict(frames=len(tail), decisionWallShare=sum(r['decisionMs'] for r in tail)/total,
                        snapshotWallShare=sum(r['snapshotMs'] for r in tail)/total,
                        withForecastBatch=sum(r['forecastBatches']>0 for r in tail)),
    roadStageTotalsMs={key:sum(r[key] for r in track['roadCosts']) for key in keys},
    examples=examples)
(args.root / 'ai-spike-analysis.json').write_text(json.dumps(out, indent=2)+'\n', encoding='utf-8')
print(json.dumps(out, indent=2))
