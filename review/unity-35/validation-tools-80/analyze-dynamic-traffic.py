"""Controlled local-trajectory integration; not full-field/campaign acceptance."""
import json, statistics, sys
from pathlib import Path
from runtime_log_errors import runtime_error_lines

root=Path(sys.argv[1])
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
names=['pass','queue','corner-pair-left','corner-pair-right','corner-three','rear-control-left','rear-control-right','no-room']

def measure(folder):
    race=read(folder/'race.json');track=read(folder/'race.track.json');fixture=read(folder/'race.traffic.json')
    frames=read(folder/'race.driver-frames.json')['frames'];gate=fixture['corridorEnd'];gates={}
    for car in race['cars']:
        rows=[s for s in track['samples'] if s['id']==car['id'] and s['time']>=0]
        crossing=None
        for a,b in zip(rows,rows[1:]):
            if a['progress']<gate<=b['progress']:
                u=(gate-a['progress'])/(b['progress']-a['progress'])
                crossing=a['time']+u*(b['time']-a['time']);break
        gates[str(car['id'])]=crossing
    complete=all(t is not None for t in gates.values())
    end=max(gates.values()) if complete else race['simSeconds']
    section=[s for s in track['samples'] if 0<=s['time']<=end]
    corner=fixture['scenario'].startswith('corner-')
    earned=not corner or (complete and all(any(e['id']==int(i) and e['gate']==2 and e['time']<=t+.12 for e in race['checkpointPassages']) for i,t in gates.items()))
    costs=[x['decisionMs']+x['snapshotMs'] for x in frames[1:]]
    return dict(cars=len(race['cars']),finished=race['finished'],normalExit=read(folder/'exit.json')['exitCode']==0 and not runtime_error_lines(folder),
        gates=gates,collectiveGate=sum(gates.values()) if complete else None,collectiveFinish=sum(c['time'] for c in race['cars']) if race['finished']==len(race['cars']) else None,
        earnedCorner=earned,contacts=len(race['impacts']),sceneryContacts=sum(e['other']<0 for e in race['impacts']),cornerContacts=sum(e['time']<=end for e in race['impacts']) if corner else None,
        outside=sum(x['outsideBodySamples'] for x in track['results']),recoveries=sum(c['physicalRecoveries'] for c in race['cars']),
        unresolved=sum(x['roadUnresolved'] for x in track['contextSamples']),brakeFraction=sum(s['pedal']<-.05 for s in section)/max(1,len(section)),
        sustainedPass=fixture['sustainedPassAt'],passBeforeExit=fixture['passBeforeCorridorExit'],passes=fixture['passes'],
        dynamic=track.get('dynamicRejoin',[]),
        warmBytes=sum(x['decisionBytes']+x['snapshotBytes']+x.get('forecastWorkerBytes',0) for x in frames if x['simTime']>=5),
        meanAiMs=sum(costs)/max(1,sum(x['snapshotCalls'] for x in frames[1:])))

cases={};checks={};deltas=[]
for name in names:
    a,b=[measure(root/f'{name}-{mode}') for mode in ['off','on']]
    delta=b['collectiveGate']-a['collectiveGate'] if a['collectiveGate'] is not None and b['collectiveGate'] is not None else None
    cases[name]=dict(off=a,on=b,collectiveGateDelta=delta)
    c=dict(normalExit=a['normalExit'] and b['normalExit'],allFinish=b['finished']==b['cars'],
        allGates=b['collectiveGate'] is not None,earnedCorner=b['earnedCorner'],
        noNewDeparture=b['outside']<=a['outside'],noNewRecovery=b['recoveries']<=a['recoveries'],
        noNewSceneryContact=b['sceneryContacts']<=a['sceneryContacts'],
        noNewUnresolved=b['unresolved']<=a['unresolved'],zeroWarmAllocation=b['warmBytes']==0,
        dynamicExercised=sum(d.get('trafficPlans',0) for d in b['dynamic'])>0,
        noExcessGateLoss=delta is not None and delta<=.5*b['cars'])
    if name.startswith('corner-'):c['cleanCorner']=b['cornerContacts']==0
    if name in ['pass','queue']:
        needed=[p for p in b['passes'] if p['follower']==0]
        c['sustainedPassesBeforeExit']=bool(needed) and all(p['sustainedAt']>=0 and b['gates']['0'] is not None and p['sustainedAt']<=b['gates']['0'] for p in needed)
    if name.startswith('rear-control-'):c['leaderNotDelayed']=b['gates']['0'] is not None and a['gates']['0'] is not None and b['gates']['0']<=a['gates']['0']+.5
    if name=='no-room':c['noForcedPass']=not b['passBeforeExit']
    else:
        if delta is not None:deltas.append(delta/b['cars'])
    checks[name]=c
median=statistics.median(deltas) if len(deltas)==len(names)-1 else None
out=dict(scope='Paired declared mid-lap fixtures. Gate times interpolated from recorded samples. Contact counts are participants, not deduplicated collisions. Rear gate time is only a screen; command attribution is required to diagnose yielding.',cases=cases,checks=checks,
    medianPerCarGateDelta=median,passed=all(all(c.values()) for c in checks.values()) and median is not None and median<0)
(root/'analysis.json').write_text(json.dumps(out,indent=2)+'\n')
print(json.dumps(dict(passed=out['passed'],medianPerCarGateDelta=median,failures={n:[k for k,v in c.items() if not v] for n,c in checks.items() if not all(c.values())},cases={n:dict(delta=c['collectiveGateDelta'],off=c['off']['collectiveFinish'],on=c['on']['collectiveFinish'],outside=c['on']['outside'],unresolved=c['on']['unresolved']) for n,c in cases.items()}),indent=2))
raise SystemExit(0 if out['passed'] else 1)
