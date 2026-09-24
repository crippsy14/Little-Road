"""First-corner passage and prediction evidence, not full-lap acceptance."""
import bisect
import collections
import json
import math
import sys
from pathlib import Path

root=Path(sys.argv[1]).resolve()
def read(p): return json.loads(p.read_text(encoding='utf-8-sig'))
manifest=read(root/'manifest.json')
if not manifest['complete']: raise SystemExit('Corner suite still running')
checks=[];results={}
def check(name,ok,detail=None): checks.append(dict(name=name,passed=bool(ok),detail=detail))
def percentile(values,p):
    return sorted(values)[min(len(values)-1,int(len(values)*p))] if values else None
check('All twelve declared cases',len(manifest['runs'])==12)
for run in manifest['runs']:
    name=run['scenario']+'-'+run['loadout'];folder=root/name
    race=read(folder/'race.json');track=read(folder/'race.track.json');fixture=read(folder/'race.traffic.json')
    by_id={c['id']:[s for s in track['samples'] if s['id']==c['id']] for c in race['cars']}
    end=fixture['corridorEnd']
    exits={i:next((s['time'] for s in rows if s['progress']>=end),None) for i,rows in by_id.items()}
    section=[s for i,rows in by_id.items() for s in rows if s['time'] <= (exits[i] if exits[i] is not None else race['simSeconds'])]
    incidents=[x for x in race['impacts'] if x['time'] <= (exits[x['id']] if exits[x['id']] is not None else race['simSeconds'])]
    assisted=[x for x in race['assistedRecoveries'] if x['time'] <= (exits[x['id']] if exits[x['id']] is not None else race['simSeconds'])]
    check(name+': normal process exit',run['exitCode']==0)
    check(name+': every car clears corner exit',all(t is not None for t in exits.values()),exits)
    check(name+': no contacts in corner',not incidents,len(incidents))
    check(name+': no recovery in corner',not assisted and all(s['recoveryState'] in ('Racing','Disabled') for s in section))
    check(name+': bodies remain on road in corner',all(s['maxBodyOffset']<=8 for s in section))
    # The declared start lies between gate 1 and gate 2, and the corner exit
    # lies after gate 2. Require its real physics crossing, not progress alone.
    check(name+': each car earns the corner checkpoint',all(any(e['id']==i and e['gate']==2 and e['time']<=t for e in race['checkpointPassages']) for i,t in exits.items() if t is not None) and all(t is not None for t in exits.values()))
    if race['driverPolicy']=='context-v1':
        context=track.get('contextSamples',[])
        check(name+': context decision capture covers every car',all(any(s['id']==i and s['time']<= (t if t is not None else race['simSeconds']) for s in context) for i,t in exits.items()))
    else:
        check(name+': complete bounded diagnostic capture',fixture['droppedPathDecisions']==0)
    errors=[];horizons=[];by_horizon=collections.defaultdict(list);rejections=collections.Counter();clocks={i:[s['clock'] for s in rows] for i,rows in by_id.items()}
    # One prediction per observer, opponent and horizon per planning timestamp.
    # Alternative paths reuse the same observation; do not count it nine times.
    seen=set()
    for p in fixture['pathDecisions']:
        owner=by_id[p['id']];j=bisect.bisect_left(clocks[p['id']],p['time']);j=min(j,len(owner)-1)
        if owner[j]['progress']>=end: continue
        if p['rejection']:rejections[str(p['rejection'])]+=1
        if p['blocker']<0:continue
        key=(p['id'],p['blocker'],p['time'],p['conflictAfter'])
        if key in seen:continue
        seen.add(key);rows=by_id[p['blocker']];times=clocks[p['blocker']];future=p['time']+p['conflictAfter']
        k=bisect.bisect_left(times,future)
        if k==0 or k>=len(rows):continue
        a,b=rows[k-1],rows[k];u=(future-a['clock'])/(b['clock']-a['clock'])
        actual={axis:a['position'][axis]+u*(b['position'][axis]-a['position'][axis]) for axis in ('x','z')}
        error=math.sqrt(sum((actual[axis]-p['predictedOther'][axis])**2 for axis in ('x','z')))
        errors.append(error);horizons.append(p['conflictAfter'])
        bucket='0–0.5s' if p['conflictAfter']<=.5 else '0.5–1s' if p['conflictAfter']<=1 else '1–2s' if p['conflictAfter']<=2 else '2–3s'
        by_horizon[bucket].append(error)
    complete=all(t is not None for t in exits.values())
    results[name]=dict(exits=exits,completedCars=sum(t is not None for t in exits.values()),expectedCars=len(exits),
        totalCarSeconds=sum(exits.values()) if complete else None,meanCarSeconds=sum(exits.values())/len(exits) if complete else None,lastCarSeconds=max(exits.values()) if complete else None,
        collectiveTimeBasis='Seconds after fixture release to scored corner exit, first 10 Hz sample at/beyond exit; total is incomplete if any car is missing.',
        contacts=len(incidents),slowSamples=sum(s['speed']<1 for s in section),
        emergencySamples=sum(s['trafficIntent']=='Brake / hold' for s in section),rejections=dict(rejections),
        comparedPredictions=len(errors),medianPredictionError=percentile(errors,.5),p95PredictionError=percentile(errors,.95),medianConflictHorizon=percentile(horizons,.5),
        predictionByHorizon={k:dict(count=len(v),median=percentile(v,.5),p95=percentile(v,.95)) for k,v in sorted(by_horizon.items())})
for setup in ('street','club'):
    times=[results[f'corner-solo-{side}-{setup}']['exits'][0] for side in ('left','middle','right')]
    # Fixed in docs/32 before this prototype. Do not make the target easier
    # merely because a new candidate becomes slower in the solo controls.
    cutoff=16 if setup=='street' else 14
    for scenario in ('corner-pair-left','corner-pair-right','corner-three'):
        name=scenario+'-'+setup;exits=results[name]['exits'];results[name]['cutoff']=cutoff
        check(name+': all exit within frozen corner deadline',all(t is not None and t<=cutoff for t in exits.values()),dict(cutoff=cutoff,exits=exits))
out=dict(scope=__doc__,predictionCaveat='Prediction error uses the subsequent observed trajectory; opponents can react to us, so it is diagnostic, not proof a rejected counterfactual path was safe.',assemblyHash=manifest['assemblyHash'],passed=sum(c['passed'] for c in checks),total=len(checks),results=results,checks=checks)
(root/'analysis.json').write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8')
print(json.dumps(dict(passed=out['passed'],total=len(checks),results=results,failures=[c for c in checks if not c['passed']]),indent=2))
raise SystemExit(0 if out['passed']==len(checks) else 1)
