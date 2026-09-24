"""Validate the extracted legacy policy against the frozen physical baseline."""
import json
import sys
from pathlib import Path

folder=Path(sys.argv[1]).resolve();baseline=Path(sys.argv[2]).resolve();checks=[];comparisons=[]
def load(path):return json.loads(path.read_text(encoding='utf-8-sig'))
def check(name,passed,detail=None):checks.append(dict(name=name,passed=bool(passed),detail=detail))
for name,before,after in [('Cedar stock',baseline/'solo-stock/race.json',folder/'stock-fields/race.json'),
                         ('Cedar club',baseline/'solo-club/race.json',folder/'cedar-fields/race.json'),
                         ('Bayview 24 run 1',folder/'bayview-before/race.json',folder/'bayview-fields/race.json'),
                         ('Bayview 24 run 2',folder/'bayview-before-r2/race.json',folder/'bayview-fields-r2/race.json'),
                         ('Bayview 24 run 3',folder/'bayview-before-r3/race.json',folder/'bayview-fields-r3/race.json')]:
    a,b=load(before),load(after)
    check(name+': matched track/equipment/seed/physics',all(a[k]==b[k] for k in ['trackId','trackVersion','equipment','seed','carCount','requiredLaps','physicsStepSeconds','gridMode','collisionMode']))
    check(name+': whole field finishes',a['finished']==b['finished']==b['carCount'])
    # Finishers circulate until the render-frame report runs. Their terminal
    # offset/progress vary with report latency; the timestamped motion trace is
    # the matched positional evidence. Finish times and gate/incident outcomes
    # remain exact comparisons, not widened tolerances after a failure.
    result_fields=['id','laps','nextGate','place','contacts','recoveries','missedGates','equipment','finished','time','bestLap','maxImpact','maxPenetration']
    maximum_time=max(abs(x['time']-y['time']) for x,y in zip(a['cars'],b['cars']))
    exact=all(all(x[k]==y[k] for k in result_fields) for x,y in zip(a['cars'],b['cars']))
    check(name+': exact frozen-run car outcomes',exact,{'maxTimeDifference':maximum_time})
    check(name+': exact sampled commands and motion',a['motion']==b['motion'])
    check(name+': no introduced recovery/gate failure',all(c['recoveries']==c['missedGates']==0 for c in b['cars']))
    check(name+': contact depth stays below 2 cm',b['maxCarPoseOverlap']<.02 and b['maxEnvironmentPoseOverlap']<.02)
    comparisons.append(dict(name=name,before=str(before),after=str(after),maxTimeDifference=maximum_time,exact=exact))
for name in ['snapshot-fields']:
    r=load(folder/name/'race.json')
    check(name+': explicitly selected common pre-controller observations',r['observationMode']=='shared pre-controller snapshot')
    check(name+': all ordered laps finish without assisted resets/missing gates',r['finished']==r['carCount'] and all(c['recoveries']==c['missedGates']==0 and c['laps']==r['requiredLaps'] for c in r['cars']))
    check(name+': contact depth stays below 2 cm',r['maxCarPoseOverlap']<.02 and r['maxEnvironmentPoseOverlap']<.02)
for name in ['stock-fields','cedar-fields']:
    audit=load(folder/name/'race.track.json');check(name+': full body remains in bounds at 10 Hz',audit['results'][0]['outsideBodySamples']==0)
out={'passed':all(c['passed'] for c in checks),'checks':checks,'comparisons':comparisons,
     'limits':'Frozen legacy compatibility retains cached observation order. Shared snapshots are separate smoke tests, not full racecraft/CPU acceptance. Exact results apply only to these matched runs on this machine. Terminal post-finish positions are excluded because render-frame report latency varies; timestamped motion is compared exactly.'}
(folder/'policy-comparison.json').write_text(json.dumps(out,indent=2),encoding='utf-8')
print(json.dumps({'passed':out['passed'],'checks':len(checks),'failed':[c for c in checks if not c['passed']],'comparisons':comparisons},indent=2))
sys.exit(0 if out['passed'] else 1)
