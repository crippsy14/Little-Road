"""Paired solo corner/gate analysis; no claim of full-field dynamic acceptance."""
import json,statistics,sys
from pathlib import Path
from runtime_log_errors import runtime_error_lines

root=Path(sys.argv[1])
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
def analyze(folder):
    race=read(folder/'race.json');track=read(folder/'race.track.json');fixture=read(folder/'race.traffic.json')
    frames=read(folder/'race.driver-frames.json')['frames'];car=race['cars'][0]
    rows=[s for s in track['samples'] if s['id']==0 and s['time']>=0]
    gate=fixture['corridorEnd'];crossing=None
    for before,after in zip(rows,rows[1:]):
        if before['progress']<gate<=after['progress']:
            f=(gate-before['progress'])/(after['progress']-before['progress'])
            crossing=dict(time=before['time']+f*(after['time']-before['time']),speed=before['speed']+f*(after['speed']-before['speed']),sampleGap=after['time']-before['time'])
            break
    earnedCorner=bool(crossing) and any(e['id']==0 and e['gate']==2 and e['time']<=crossing['time']+.12 for e in race['checkpointPassages'])
    return dict(finished=race['finished']==1 and car['time']>0,completion=car['time'] if car['time']>0 else None,
                earnedCornerCheckpoint=earnedCorner,
                cornerGate=gate,gateCrossing=crossing,contacts=len(race['impacts']),
                outside=sum(x['outsideBodySamples'] for x in track['results']),maximumBodyOffset=max(x['maxBodyOffset'] for x in track['results']),
                physicalRecoveries=car['physicalRecoveries'],unresolved=sum(x['roadUnresolved'] for x in track['contextSamples']),
                noErrors=read(folder/'exit.json')['exitCode']==0 and not runtime_error_lines(folder),
                dynamic=track.get('dynamicRejoin',[]),
                warmDriverBytes=sum(x['decisionBytes']+x['snapshotBytes']+x.get('forecastWorkerBytes',0) for x in frames if x['simTime']>=5),
                meanAiMsPerSnapshot=sum(x['decisionMs']+x['snapshotMs'] for x in frames[1:])/max(1,sum(x['snapshotCalls'] for x in frames[1:])))
cases={};checks={};deltas=[];offTimes=[];onTimes=[]
for loadout in ['street','club']:
    for entry in ['left','middle','right','deep']:
        name=f'{loadout}-{entry}'
        a,b=(analyze(root/f'{name}-{mode}') for mode in ['off','on'])
        valid=a['gateCrossing'] is not None and b['gateCrossing'] is not None
        delta=b['gateCrossing']['time']-a['gateCrossing']['time'] if valid else None
        cases[name]=dict(off=a,on=b,cornerSecondsDelta=delta)
        checks[name]=dict(normalExit=a['noErrors'] and b['noErrors'],finish=b['finished'],
                         noContact=b['contacts']==0,noDeparture=b['outside']==0,noRecovery=b['physicalRecoveries']==0,
                         noNewUnresolved=b['unresolved']<=a['unresolved'],gateMeasured=valid,
                         earnedCornerCheckpoint=b['earnedCornerCheckpoint'],
                         noHalfSecondLoss=valid and delta<=.5,zeroWarmDriverAllocation=b['warmDriverBytes']==0,
                         dynamicExercised=bool(b['dynamic']) and b['dynamic'][0]['selections']>0)
        if valid:
            deltas.append(delta);offTimes.append(a['gateCrossing']['time']);onTimes.append(b['gateCrossing']['time'])
complete=len(deltas)==8
medianImproves=complete and statistics.median(onTimes)<statistics.median(offTimes)
out=dict(scope='Actual solo first-corner gate crossings interpolated from10Hz samples; remaining-course finish and whole-run incidents are independent. Deep entry is a declared later start, not a measured human overshoot. Hidden CPU timings are not rendered acceptance.',
         cases=cases,checks=checks,medianCornerTimeImproves=medianImproves,
         medianPairedCornerDelta=statistics.median(deltas) if complete else None,
         passed=complete and medianImproves and all(all(c.values()) for c in checks.values()))
(root/'analysis.json').write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8')
print(json.dumps(dict(passed=out['passed'],medianPairedCornerDelta=out['medianPairedCornerDelta'],
                     failures={k:[n for n,ok in v.items() if not ok] for k,v in checks.items() if not all(v.values())},
                     cases={k:dict(delta=v['cornerSecondsDelta'],offFinish=v['off']['completion'],onFinish=v['on']['completion'],
                                   onOutside=v['on']['outside'],onUnresolved=v['on']['unresolved'],dynamic=v['on']['dynamic']) for k,v in cases.items()}),indent=2))
raise SystemExit(0 if out['passed'] else 1)
