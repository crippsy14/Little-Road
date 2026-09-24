"""Coasting's controlled physical interactions; full-field contacts are separate."""
import json
from pathlib import Path
import subprocess
import sys

root=Path(sys.argv[1]);read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
expected=read(root/'expectations.json');manifest=read(root/'manifest.json')
baseline=Path(__file__).resolve().parents[1]/expected['referenceRoot']
checks={};results={}
wanted={c['name'] for c in expected['cases']}|{f'solo-{s}-{v}' for s in ('street','club') for v in ('control','coast')}
names=[r['name'] for r in manifest['runs']]
checks['Every declared run completed exactly once']=manifest['complete'] and set(names)==wanted and len(names)==len(wanted)
for run in manifest['runs']:
    name=run['name'];folder=root/name
    race=read(folder/'race.json');track=read(folder/'race.track.json');launch=read(folder/'launch.json')
    cars=race['cars'];clean=race['finished']==run['cars'] and all(c['contacts']==c['recoveries']==c['physicalRecoveries']==c['missedGates']==0 for c in cars)
    clean=clean and all(c['outsideBodySamples']==0 for c in track['results']) and max(race['maxCarPoseOverlap'],race['maxEnvironmentPoseOverlap'])<.02 and race['overlapQuerySaturations']==0
    checks[name+': normal exit and clean completion']=run['exitCode']==0 and clean
    checks[name+': declared coast/road/guide/worker configuration']=launch['coastWithinEnvelope']==run['coast'] and not launch['timedRearCorridors'] and launch['roadFeasibility'] and launch['guidePathFallback'] and launch['parallelForecastWorkers']==8
    if 'driverRisk' in expected:
        checks[name+': declared risk and gap-candidate configuration']=launch['driverRisk']==expected['driverRisk'] and launch['noGapCentres']==expected['noGapCentres']
    if 'trafficPathCheck' in expected:
        checks[name+': declared traffic-path check']=launch['trafficPathCheck']==expected['trafficPathCheck']
    if 'anticipatePassingSpace' in expected:
        checks[name+': declared passing-space anticipation']=launch['anticipatePassingSpace']==expected['anticipatePassingSpace']
    if 'anticipateBlockingCar' in expected:
        checks[name+': declared blocking-car anticipation']=launch['anticipateBlockingCar']==expected['anticipateBlockingCar']
    if 'trafficCoastContinuation' in expected:
        checks[name+': declared coast continuation']=launch['trafficCoastContinuation']==expected['trafficCoastContinuation']
    results[name]=dict(finished=race['finished'],totalCarSeconds=sum(c['time'] for c in cars) if race['finished']==run['cars'] else None,
                      contacts=sum(c['contacts'] for c in cars),outsideBodySamples=sum(c['outsideBodySamples'] for c in track['results']),
                      physicalRecoveries=sum(c['physicalRecoveries'] for c in cars),times=[c['time'] for c in cars])
    if name.endswith('-control'):continue
    reference=root/name.replace('-coast','-control') if name.startswith('solo-') else baseline/name
    old=read(reference/'race.json');old_launch=read(reference/'launch.json')
    checks[name+': same physical comparison']=all(race[k]==old[k] for k in ('trackId','trackVersion','trackLength','trackWidth','physicsStepSeconds','collisionMode','carCount','requiredLaps')) and [c['equipment'] for c in cars]==[c['equipment'] for c in old['cars']] and all(launch[k]==old_launch[k] for k in ('seed','racingLine','trafficCase','loadout','opponents','physicalRecovery'))
    allowance=expected['completionAllowance'];ratio=expected['maximumCompletionRatio']
    checks[name+': no excessive completion delay']=all(c['finished'] and c['time']<=b['time']*ratio+allowance for c,b in zip(cars,old['cars']))
    traffic=read(folder/'race.traffic.json')
    if name=='queue':
        passes=[p for p in traffic['passes'] if p['follower']==0]
        checks[name+': faster rear car passes both']=len(passes)==2 and all(p['sustainedAt']>0 and p['onStraight'] for p in passes)
    if name.startswith('merge-'):
        side=name.removeprefix('merge-')
        witness=read(baseline.parent/'merges-13'/f'{side}-12-witness'/'race.traffic.json')
        deadline=witness['passes'][0]['sustainedAt']*1.25+1
        actual=traffic['passes'][0]
        checks[name+': independent witness pass deadline']=0<actual['sustainedAt']<=deadline and actual['onStraight']
        results[name]['passDeadline']=deadline;results[name]['sustainedPass']=actual['sustainedAt']
    if name=='corner-three':
        exits={c['id']:next((s['time'] for s in track['samples'] if s['id']==c['id'] and s['progress']>=traffic['corridorEnd']),None) for c in cars}
        checks[name+': all earn corner checkpoint before exit']=all(t is not None and any(e['id']==i and e['gate']==2 and e['time']<=t for e in race['checkpointPassages']) for i,t in exits.items())
    if name=='no-room':
        p=subprocess.run([sys.executable,str(Path(__file__).with_name('analyze-corridor-race.py')),str(folder)],capture_output=True,text=True)
        (folder/'corridor-output.txt').write_text(p.stdout+p.stderr)
        checks[name+': existing independent corridor contract']=p.returncode==0
out=dict(scope=manifest['scope'],passed=sum(checks.values()),total=len(checks),checks=checks,results=results)
(root/'analysis.json').write_text(json.dumps(out,indent=2)+'\n')
print(json.dumps(dict(passed=out['passed'],total=out['total'],failures=[k for k,v in checks.items() if not v],results=results),indent=2))
raise SystemExit(0 if all(checks.values()) else 1)
