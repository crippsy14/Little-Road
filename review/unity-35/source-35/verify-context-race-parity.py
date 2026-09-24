"""Verify an optimisation preserves recorded decisions, motion and race events."""
import argparse
import json
from pathlib import Path

p = argparse.ArgumentParser()
p.add_argument('baseline', type=Path)
p.add_argument('candidate', type=Path)
args = p.parse_args()
read = lambda f: json.loads(f.read_text(encoding='utf-8-sig'))
a, b = (read(root / 'race.json') for root in (args.baseline, args.candidate))
at, bt = (read(root / 'race.track.json') for root in (args.baseline, args.candidate))
checks = {key: a[key] == b[key] for key in ('motion', 'checkpointPassages', 'impacts', 'assistedRecoveries', 'overlapPeaks')}
checks.update({key: at[key] == bt[key] for key in ('samples', 'contextSamples')})
# Terminal export positions may follow a different number of final batch ticks.
race_result = lambda car: {k: v for k, v in car.items() if k not in ('progress', 'offset')}
checks['carRaceResults'] = [race_result(c) for c in a['cars']] == [race_result(c) for c in b['cars']]
checks['normalExit'] = read(args.candidate / 'exit.json')['exitCode'] == 0
cost = lambda track: sum(c['totalMs'] for c in track['contextDecisionCosts']) / max(c['calls'] for c in track['contextDecisionCosts'])
out = dict(scope='Exact equality of stored samples/events/results; terminal export positions and profiling costs are separate. Hidden timing is not rendered-frame acceptance.',
           baseline=str(args.baseline), candidate=str(args.candidate), checks=checks, passed=all(checks.values()),
           meanDecisionRegionMsPerPhysicsTick=dict(baseline=cost(at), candidate=cost(bt)),
           decisionRegionReductionPercent=100 * (1 - cost(bt) / cost(at)))
# Forecast batching lives in the shared snapshot. Include it before making
# any whole-driver cost comparison; decision-only savings are not overall gain.
def full_cost(root):
    rows=read(root/'race.driver-frames.json')['frames'][1:]
    return sum(r['decisionMs']+r['snapshotMs'] for r in rows)/max(1,sum(r['snapshotCalls'] for r in rows))
if all((root/'race.driver-frames.json').exists() for root in (args.baseline,args.candidate)):
    ca,cb=full_cost(args.baseline),full_cost(args.candidate)
    out['meanAllAiMsPerSnapshotTick']=dict(baseline=ca,candidate=cb)
    out['allAiWallCostReductionPercent']=100*(1-cb/ca)
(args.candidate / 'motion-parity.json').write_text(json.dumps(out, indent=2) + '\n', encoding='utf-8')
print(json.dumps(out, indent=2))
raise SystemExit(0 if out['passed'] else 1)
