"""Matched component fields; isolate interventions without assuming additive traffic effects."""
import json
import sys
from pathlib import Path
from runtime_log_errors import runtime_error_lines

root=Path(sys.argv[1]);read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
checks={};arms={};baseline_launch=None;baseline_race=None
physical_keys=('trackId','trackVersion','trackLength','trackWidth','physicsStepSeconds','carCount','requiredLaps','collisionMode')
launch_keys=('seed','grid','track','loadout','policy','racingLine','physicalRecovery','roadFeasibility','preparedPath',
             'parallelForecastWorkers','driverRisk','noGapCentres','trafficPathCheck','anticipatePassingSpace',
             'compareContinuation','trafficAwareProgress','comparePassCompletion','retainPassTrajectory','trafficCoastContinuation')
for name,scope,enabled in [('off','all',False),('all','all',True),('car0','car0',True),('rivals','rivals',True)]:
    folder=root/name;race=read(folder/'race.json');track=read(folder/'race.track.json');launch=read(folder/'launch.json')
    entries=sorted((e for b in track['followingPlanAudits'] for e in b['entries']),key=lambda e:(e['timestamp'],e['self']['id']))
    costs=track['roadCosts'];accepted=sum(c['followingFallbackAccepted'] for c in costs);attempts=sum(c['followingFallbackAttempts'] for c in costs)
    def allowed(i):return enabled and (scope=='all' or scope=='car0' and i==0 or scope=='rivals' and i!=0)
    checks[name+': declared component']=launch['followingFeasibilityFallback']==enabled and launch['followingFallbackScope']==scope and launch['followingFallbackAudit']
    checks[name+': scope excludes other drivers']=all(allowed(c['id']) or c['followingFallbackAttempts']==c['followingFallbackAccepted']==0 for c in costs)
    checks[name+': acceptance audit complete']=len(entries)==accepted and all(b['dropped']==0 for b in track['followingPlanAudits'])
    checks[name+': model and path identity']=all(e['captureReason']=='following-fallback-accepted' and len(e['candidates'])==1 and e['candidates'][0]['reason']=='accepted' and e['candidates'][0]['forecastModel']=='moving-leader' and e['candidates'][0]['path']==e['selectedPath'] for e in entries)
    checks[name+': normal exit without runtime errors']=read(folder/'exit.json')['exitCode']==0 and not runtime_error_lines(folder)
    checks[name+': everyone finishes']=race['finished']==24 and all(c['missedGates']==0 for c in race['cars'])
    checks[name+': bodies contained without recovery']=all(s['outsideBodySamples']==0 for s in track['results']) and all(c['physicalRecoveries']==0 for c in race['cars'])
    if baseline_launch is None:baseline_launch=launch;baseline_race=race
    checks[name+': same physical and policy configuration']=all(launch[k]==baseline_launch[k] for k in launch_keys) and all(race[k]==baseline_race[k] for k in physical_keys) and [c['equipment'] for c in race['cars']]==[c['equipment'] for c in baseline_race['cars']] and launch['assemblyHash']==baseline_launch['assemblyHash']
    frames=read(folder/'race.driver-frames.json')['frames'][1:]
    origin=track['samples'][0]['clock']-track['samples'][0]['time']
    events=[dict(id=e['self']['id'],timestamp=e['timestamp'],raceTime=e['timestamp']-origin,ownSpeed=e['self']['speed'],
                 selectedOffset=e['selectedPath']['selectedOffset'],gain=e['candidates'][0]['predictedRouteGain'],endSpeed=e['candidates'][0]['predictedEndSpeed']) for e in entries]
    arms[name]=dict(totalCarSeconds=sum(c['time'] for c in race['cars']),previewTime=race['cars'][0]['time'],previewPlace=race['cars'][0]['place'],
                    impactRecords=len(race['impacts']),peakImpact=max((e['relativeSpeed'] for e in race['impacts']),default=0),
                    unresolvedSamples=sum(s['roadUnresolved'] for s in track['contextSamples']),attempts=attempts,accepted=accepted,
                    meanAllAiMsPerSnapshot=sum(f['snapshotMs']+f['decisionMs'] for f in frames)/sum(f['snapshotCalls'] for f in frames),events=events)
out=dict(scope='Single deterministic matched field, component intervention only. Own-car/rival effects need not sum to the all-car effect because each run changes subsequent interactions. Event timestamps are pre-controller snapshots; audit-enabled timing is not rendered acceptance.',
         passed=sum(checks.values()),total=len(checks),checks=checks,arms=arms)
(root/'analysis.json').write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8')
print(json.dumps(dict(passed=out['passed'],total=out['total'],failures=[k for k,v in checks.items() if not v],arms={name:{k:v for k,v in arm.items() if k!='events'} for name,arm in arms.items()}),indent=2))
raise SystemExit(0 if all(checks.values()) else 1)
