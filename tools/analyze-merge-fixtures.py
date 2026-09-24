"""Compare mirrored merges against declared physical hold-course witnesses."""
import json,sys
from pathlib import Path

root=Path(sys.argv[1]);read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
manifest=read(root/'manifest.json');expect=read(root/'expectations.json')
checks=[];results={};signatures={}
def check(name,passed,value=None):checks.append(dict(name=name,passed=bool(passed),value=value))
expected={f'{side}-{gap}-{mode}' for side in ('left','right') for gap in (12,24) for mode in ('witness','candidate')}
names=[r['name'] for r in manifest['runs']]
check('Every declared run completed exactly once',manifest['complete'] and set(names)==expected and len(names)==len(expected))
for run in manifest['runs']:
    name=run['name'];folder=root/name
    race=read(folder/'race.json');track=read(folder/'race.track.json');fixture=read(folder/'race.traffic.json');launch=read(folder/'launch.json')
    cars=race['cars'];finished=[c['time'] for c in cars if c['finished']]
    complete=len(finished)==2;passed=fixture['passes'][0]
    clean=complete and all(c['contacts']==c['recoveries']==c['physicalRecoveries']==c['missedGates']==0 for c in cars) and all(c['outsideBodySamples']==0 for c in track['results']) and max(race['maxCarPoseOverlap'],race['maxEnvironmentPoseOverlap'])<.02 and race['overlapQuerySaturations']==0
    early=[s for s in track['samples'] if s['id']==1 and 0<=s['time']<5]
    results[name]=dict(clean=clean,finished=len(finished),expectedCars=2,totalCarSeconds=sum(finished) if complete else None,
        fastCarFinish=cars[1]['time'] if cars[1]['finished'] else None,slowCarFinish=cars[0]['time'] if cars[0]['finished'] else None,
        sustainedPass=passed['sustainedAt'],passOnStraight=passed['onStraight'],witnessRelease=fixture['witnessRelease'],
        contacts=sum(c['contacts'] for c in cars),outsideSamples=sum(c['outsideBodySamples'] for c in track['results']),
        earlyFastGuardSamples=sum(s['safetyActive'] for s in early),earlyFastMinimumSpeed=min(s['speed'] for s in early))
    signatures[name]={k:race[k] for k in ('trackId','trackVersion','trackLength','trackWidth','physicsStepSeconds','collisionMode','carCount','requiredLaps')}
    signatures[name].update(equipment=[c['equipment'] for c in cars],reference=track['reference'],start=fixture['start'],gap=fixture['startingGap'],frontSpeed=fixture['startingSpeed'],rearSpeed=fixture['rearStartingSpeed'],assemblyHash=launch['assemblyHash'])
    check(name+': normal process exit',run['exitCode']==0)
    check(name+': both cars finish cleanly without departures or recovery',clean,results[name])
    if run['mode']=='witness':
        check(name+': witness releases after a body-clear pass and preserves it for two seconds',fixture['witnessRelease']>0 and passed['sustainedAt']>0 and passed['onStraight'])
for gap in (12,24):
    for side in ('left','right'):
        key=f'{side}-{gap}';w=results[key+'-witness'];c=results[key+'-candidate']
        check(key+': same physical setup for witness and candidate',signatures[key+'-witness']==signatures[key+'-candidate'])
        feasible=w['clean'] and w['sustainedPass']>0 and w['passOnStraight']
        deadline=w['sustainedPass']*expect['passDeadlineMultiplier']+expect['passDeadlineAllowance'] if feasible else None
        c['witnessDeadline']=deadline
        c['collectiveDeltaFromWitness']=c['totalCarSeconds']-w['totalCarSeconds'] if c['totalCarSeconds'] is not None and w['totalCarSeconds'] is not None else None
        check(key+': sustained clean pass within independent witness deadline',feasible and c['clean'] and 0<c['sustainedPass']<=deadline and c['passOnStraight'],dict(actual=c['sustainedPass'],deadline=deadline))
out=dict(scope='First mirrored rear-approach merge cases, not all merges, skill/personality acceptance or a physical-reachability proof. Emergency intervention samples are diagnostic, not automatically a failure.',
    passed=sum(c['passed'] for c in checks),total=len(checks),results=results,checks=checks)
(root/'analysis.json').write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8')
print(json.dumps(dict(passed=out['passed'],total=out['total'],results=results,failures=[c for c in checks if not c['passed']]),indent=2))
raise SystemExit(0 if out['passed']==out['total'] else 1)
