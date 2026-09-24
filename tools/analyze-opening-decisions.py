"""Opening decisions and observed leader motion; future samples are diagnostics only."""
import bisect
import json
import math
import sys
from collections import Counter
from pathlib import Path
from runtime_log_errors import runtime_error_lines

folder=Path(sys.argv[1])
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
track=read(folder/'race.track.json');race=read(folder/'race.json')
buckets=track['openingPlanAudits']
entries=sorted((e for b in buckets for e in b['entries']),key=lambda e:e['timestamp'])
if not entries:raise SystemExit('No opening decisions captured')
by_car={c['id']:[] for c in race['cars']}
for s in track['samples']:by_car[s['id']].append(s)
clocks={i:[s['clock'] for s in rows] for i,rows in by_car.items()}
def sample(car_id,clock):
    times=clocks[car_id];i=bisect.bisect_left(times,clock)
    candidates=[by_car[car_id][j] for j in (i-1,i) if 0<=j<len(times)]
    result=min(candidates,key=lambda s:abs(s['clock']-clock))
    return result if abs(result['clock']-clock)<=.06 else None

clock_origin=track['samples'][0]['clock']-track['samples'][0]['time']
rows=[];candidate_reasons=Counter()
for e in entries:
    own=e['self'];best=None;limit=999
    for other in e['traffic']:
        if other['id']==own['id']:continue
        gap=(other['route']-own['route']+track['length']/2)%track['length']-track['length']/2
        width=own['halfWidth']+other['halfWidth']+.35
        if gap<=0 or gap>100 or abs(other['offset']-own['offset'])>width:continue
        clearance=gap-own['halfLength']-other['halfLength']-1.5
        brake=min(e['tuning']['braking'],e['tuning']['roadGrip']*1.65)*.7
        value=math.sqrt(max(0,other['speed']**2+2*brake*max(0,clearance-own['speed']*.25)))
        if value<limit:best=other;limit=value
    counts=Counter(c['reason'] for c in e['candidates']);candidate_reasons.update(counts)
    row=dict(timestamp=e['timestamp'],raceTime=e['timestamp']-clock_origin,speed=own['speed'],offset=own['offset'],
             selected=e['selectedPath']['selectedOffset'],captureReason=e['captureReason'],candidateReasons=dict(counts),
             hasLeader=best is not None)
    if best is not None:
        width=own['halfWidth']+best['halfWidth']+.35
        row.update(leader=best['id'],leaderSpeed=best['speed'],leaderOffset=best['offset'],leaderLimit=limit,
                   destinationClearsCurrentLeader=abs(row['selected']-best['offset'])>width,
                   lateralDistanceStillNeeded=max(0,width-abs(own['offset']-best['offset'])))
        for horizon,label in [(-1,'previous1s'),(1,'next1s'),(2,'next2s')]:
            s=sample(best['id'],e['timestamp']+horizon)
            if s:
                row[label]=dict(actualClock=s['clock'],speed=s['speed'],differenceFromObserved=s['speed']-best['speed'],
                                physicalRecovery=s['recoveryState'])
    rows.append(row)
cp4=next(e['time'] for e in race['checkpointPassages'] if e['id']==0 and e['gate']==4)
checks=dict(normalExit=read(folder/'exit.json')['exitCode']==0,noRuntimeErrors=not runtime_error_lines(folder),
            allCarsFinish=race['finished']==race['carCount'],onlyOwnCar=all(e['self']['id']==0 for e in entries),
            firstCaptureMoving=entries[0]['self']['speed']>=2,noCountdown=rows[0]['raceTime']>=0,
            boundedCadence=all(b['timestamp']-a['timestamp']>=.25-1e-8 for a,b in zip(entries,entries[1:])),
            boundedWindow=entries[-1]['timestamp']-entries[0]['timestamp']<=40,
            noDrops=all(b['dropped']==0 for b in buckets),coversCheckpoint4=rows[-1]['raceTime']>=cp4,
            candidatesPresent=all(e['candidates'] for e in entries))
leaders=[r for r in rows if r['hasLeader']]
with_future=[r for r in leaders if 'next2s' in r]
summary=dict(captures=len(rows),raceTimeRange=[rows[0]['raceTime'],rows[-1]['raceTime']],checkpoint4=cp4,
             potentialLeaderObservations=len(leaders),destinationsNotClearOfCurrentLeader=sum(not r['destinationClearsCurrentLeader'] for r in leaders),
             futureSpeedComparisons=len(with_future),leaderSpeedRisesOver2mps=sum(r['next2s']['differenceFromObserved']>2 for r in with_future),
             leaderSpeedFallsOver2mps=sum(r['next2s']['differenceFromObserved']< -2 for r in with_future),candidateReasons=dict(candidate_reasons))
out=dict(scope='Captured live choices, not alternative-route acceptance. Leader formula uses recorded dimensions/offsets with Python float precision; identifies the potential lowest speed envelope, not final pedal attribution. Destination-clear test uses current leader position only, not future occupancy. Future telemetry is an observational comparator, never AI input; nearest samples must be within60ms. Selected capture cadence is not whole-race statistics.',
         checks=checks,passed=sum(checks.values()),total=len(checks),summary=summary,rows=rows)
(folder/'opening-analysis.json').write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8')
print(json.dumps(dict(passed=out['passed'],total=out['total'],failures=[k for k,v in checks.items() if not v],summary=summary),indent=2))
raise SystemExit(0 if all(checks.values()) else 1)
