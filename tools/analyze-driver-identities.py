"""First personality evidence; do not mistake parameter spread for lap statistics."""
import json
from pathlib import Path
from runtime_log_errors import runtime_error_lines
root=Path(__file__).resolve().parents[1]/'review/unity-35/driver-identities-86'
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
names=['bayview-neutral','cedar-neutral','pass-cautious','pass-assertive','pass-daring','solo-neutral','solo-early','bayview-mixed','cedar-mixed']
results={}
for name in names:
    folder=root/name
    if not (folder/'exit.json').exists():continue
    race=read(folder/'race.json');track=read(folder/'race.track.json');identities=read(folder/'race.drivers.json')
    frames=read(folder/'race.driver-frames.json')['frames']
    profile={d['entrant']:d for d in identities['drivers']};planning={d['id']:d for d in track['driverPlanning']}
    checks=dict(cleanExit=read(folder/'exit.json')['exitCode']==0 and not runtime_error_lines(folder),allFinish=race['finished']==race['carCount'],
                oneIdentityPerEntrant=len(profile)==race['carCount'] and set(profile)==set(range(race['carCount'])),
                actualPlanningMatchesIdentity=all(planning[i]['risk']==d['risk'] and planning[i]['localBrakeFraction']==d['effectiveBraking'] for i,d in profile.items()))
    samples=[s for s in track['samples'] if s['id']==0 and s['time']>=0]
    firstBrake=next((dict(time=s['time'],progress=s['progress'],speed=s['speed']) for s in samples if s['progress']>150 and s['pedal']<-.05),None)
    # Exposure-weighted applied pedal, clipped to authored Cedar opening0..200m.
    seconds=throttle=full=0
    if race['trackId']!='bayview':
        for a,b in zip(samples,samples[1:]):
            if b['progress']<=a['progress']:continue
            lo=max(0,(0-a['progress'])/(b['progress']-a['progress']));hi=min(1,(200-a['progress'])/(b['progress']-a['progress']))
            if hi<=lo:continue
            dt=(b['time']-a['time'])*(hi-lo);seconds+=dt;throttle+=dt*max(0,a['pedal']);full+=dt*(a['pedal']>=.95)
    cars=[dict(id=c['id'],profile=profile[c['id']],equipment=c['equipment'],time=c['time'] if c['finished'] else None,place=c['place'],contacts=c['contacts']) for c in race['cars']]
    result=dict(checks=checks,cars=cars,finished=race['finished'],entrants=race['carCount'],
                collectiveSeconds=sum(c['time'] for c in race['cars']) if checks['allFinish'] else None,
                contactParticipantRecords=len(race['impacts']),peakImpact=max((i['relativeSpeed'] for i in race['impacts']),default=0),
                outsideSamples=sum(r['outsideBodySamples'] for r in track['results']),maxBodyOffset=max(r['maxBodyOffset'] for r in track['results']),
                physicalRecoveries=sum(c['physicalRecoveries'] for c in race['cars']),assistedRecoveries=len(race['assistedRecoveries']),
                unresolvedSamples=sum(s['roadUnresolved'] for s in track['contextSamples']),
                meanAiMs=sum(f['decisionMs']+f['snapshotMs'] for f in frames[1:])/max(1,sum(f['snapshotCalls'] for f in frames[1:])),
                warmBytes=sum(f['decisionBytes']+f['snapshotBytes']+f.get('forecastWorkerBytes',0) for f in frames if f['simTime']>=5),
                openingAveragePedal=throttle/seconds if seconds else None,openingFullPedalFraction=full/seconds if seconds else None,firstBrakeAfter150m=firstBrake)
    if (folder/'race.traffic.json').exists():
        t=read(folder/'race.traffic.json');result['passing']=dict(firstClear=t['firstClearPass'],sustained=t['sustainedPassAt'],beforeCorridorExit=t['passBeforeCorridorExit'],pairs=t['passes'])
    results[name]=result
    print(name,json.dumps(dict(checks=checks,finished=result['finished'],preview=cars[0]['time'],collective=result['collectiveSeconds'],contacts=result['contactParticipantRecords'],outside=result['outsideSamples'],recovery=result['physicalRecoveries'],passing=result.get('passing'))))
out=dict(scope='Single-seed prototype. Causal trait controls and mixed-roster feasibility; no statistical lap/position-variance or player-recognition claim.',results=results)
(root/'analysis.json').write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8')
