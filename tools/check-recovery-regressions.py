"""Compare clean reference driving and the unchanged production policy with frozen runs."""
import json
import sys
from pathlib import Path

root=Path(sys.argv[1]).resolve();review=root.parent;build=sys.argv[2] if len(sys.argv)>2 else '08'
def read(path):return json.loads(path.read_text(encoding='utf-8-sig'))
checks=[]
def check(name,value,detail=None):checks.append(dict(name=name,passed=bool(value),detail=detail))
fields=['id','laps','nextGate','place','contacts','recoveries','missedGates','equipment','finished','time','bestLap','maxImpact','maxPenetration']
for name,before,after in [
    ('clean club reference',review/'unity-29/smooth-validation/club-smooth-v2-r1',root/('clean-club-'+build)),
    ('production 24-car race',review/'unity-28/bayview-fields',root/('legacy-field-'+build))]:
    a=read(before/'race.json');b=read(after/'race.json')
    check(name+': matched scenario',all(a[k]==b[k] for k in ['trackId','trackVersion','equipment','seed','carCount','requiredLaps','physicsStepSeconds','gridMode','collisionMode']))
    check(name+': whole field finishes',b['finished']==b['carCount'])
    check(name+': exact car outcomes',all(all(x[k]==y[k] for k in fields) for x,y in zip(a['cars'],b['cars'])))
    check(name+': exact timestamped motion and inputs',a['motion']==b['motion'])
    check(name+': no recovery or checkpoint regression',all(c['recoveries']==c['missedGates']==c['physicalRecoveries']==0 for c in b['cars']))
    check(name+': overlap below 2 cm',b['maxCarPoseOverlap']<.02 and b['maxEnvironmentPoseOverlap']<.02)
    log=(after/'run.log').read_text(encoding='utf-8-sig')
    check(name+': report and normal shutdown recorded','Input System module state changed to: Shutdown.' in log and 'Exception:' not in log)
    if 'reference' in name:
        track=read(after/'race.track.json')
        check(name+': no sampled body departure',track['results'][0]['outsideBodySamples']==0)
out=dict(passed=sum(c['passed'] for c in checks),total=len(checks),checks=checks,scope='Exact matched outcomes on this machine; no claim of general AI acceptance or rendering performance')
(root/('regressions-'+build+'.json')).write_text(json.dumps(out,indent=2)+'\n')
print(json.dumps(out,indent=2));raise SystemExit(0 if out['passed']==out['total'] else 1)
