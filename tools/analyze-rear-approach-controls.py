"""Frozen unchanged-approach controls, separate from later genuine encounters."""
import json,sys
from pathlib import Path
root=Path(sys.argv[1]);read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
manifest=read(root/'manifest.json');expect=read(root/'expectations.json');checks=[];data={};summaries={}
def check(name,ok):checks.append(dict(name=name,passed=bool(ok)))
names={f"{case['name']}-{version}" for case in expect['cases'] for version in ('baseline','candidate')}
check('Every declared run completed exactly once',manifest['complete'] and len(manifest['runs'])==len(names) and {r['name'] for r in manifest['runs']}==names)
for run in manifest['runs']:
    name=run['name'];folder=root/name;r=read(folder/'race.json');t=read(folder/'race.track.json');f=read(folder/'race.traffic.json');launch=read(folder/'launch.json')
    check(name+': normal exit',run['exitCode']==0)
    check(name+': declared component switch',launch['noRearAnticipation']==(run['version']=='baseline'))
    clean=r['finished']==2 and all(c['contacts']==c['recoveries']==c['physicalRecoveries']==c['missedGates']==0 for c in r['cars']) and all(c['outsideBodySamples']==0 for c in t['results']) and max(r['maxCarPoseOverlap'],r['maxEnvironmentPoseOverlap'])<.02 and r['overlapQuerySaturations']==0
    check(name+': both cars finish cleanly',clean)
    early=[s for s in t['samples'] if s['id']==0 and 0<=s['time']<expect['unchangedWindowSeconds']]
    signature={k:r[k] for k in ('trackId','trackVersion','trackLength','trackWidth','physicsStepSeconds','collisionMode','carCount','requiredLaps')}
    signature.update(equipment=[c['equipment'] for c in r['cars']],reference=t['reference'],scenario=f['scenario'],start=f['start'],gap=f['startingGap'],speed=f['startingSpeed'],rearSpeed=f['rearStartingSpeed'],assemblyHash=launch['assemblyHash'])
    if run['version']=='candidate':
        check(name+': no anticipation constraint during harmless approach',all(s['rearApproaches']==0 for s in t['contextSamples'] if s['id']==0 and 0<=s['time']<expect['unchangedWindowSeconds']))
    data[name]=(early,signature)
    summaries[name]=dict(clean=clean,completedCars=r['finished'],totalCarSeconds=sum(c['time'] for c in r['cars']) if r['finished']==2 else None,earlySamples=len(early))
for case in expect['cases']:
    name=case['name'];a,sa=data[name+'-baseline'];b,sb=data[name+'-candidate']
    check(name+': physical setup matches',sa==sb)
    check(name+': lead car keeps exact inputs and motion during declared harmless approach',len(a)>=19 and a==b)
out=dict(scope=expect['scope'],passed=sum(c['passed'] for c in checks),total=len(checks),checks=checks,summaries=summaries)
(root/'analysis.json').write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8')
print(json.dumps(dict(passed=out['passed'],total=out['total'],summaries=summaries,failures=[c for c in checks if not c['passed']]),indent=2))
raise SystemExit(0 if out['passed']==out['total'] else 1)
