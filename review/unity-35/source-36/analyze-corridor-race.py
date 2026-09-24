"""Check the predeclared narrow-corridor contract; preserve every failure."""
import argparse
import json
from pathlib import Path

p=argparse.ArgumentParser()
p.add_argument('folder',type=Path)
args=p.parse_args();root=args.folder
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
e=read(root/'expectations.json');race=read(root/'race.json');track=read(root/'race.track.json');launch=read(root/'launch.json')
finish={c['id']:c['time'] if c['finished'] else float('inf') for c in race['cars']}
rows=[s for s in track['contextSamples'] if 0<=s['time']<=finish[s['id']]]
unresolved=[s for s in rows if s['roadUnresolved']]
totals={k:sum(c[k] for c in race['cars']) for k in ('contacts','recoveries','physicalRecoveries','missedGates')}
totals['outsideBodySamples']=sum(c['outsideBodySamples'] for c in track['results'])
totals['unresolvedSamples']=len(unresolved)
totals['guideAttempts']=sum(c.get('guideAttempts',0) for c in track['roadCosts'])
totals['guideAccepted']=sum(c.get('guideAccepted',0) for c in track['roadCosts'])
checks={
    'normalExit':read(root/'exit.json')['exitCode']==0,
    'declaredScenario':launch['trafficCase']=='no-room' and launch['cars']==e['requiredCars'],
    'checkedGuideEnabled':launch.get('guidePathFallback',False) and launch['roadFeasibility'] and all(s['roadEnabled'] for s in rows),
    'guideActuallyAccepted':totals['guideAccepted']>0,
    'allFinishWithinIndependentWitnessAllowance':race['finished']==e['requiredCars'] and max(finish.values())<=e['completionDeadline'],
    'zeroContacts':totals['contacts']<=e['maximumContacts'],
    'zeroAssistedResets':totals['recoveries']<=e['maximumAssistedRecoveries'],
    'zeroPhysicalRecoveries':totals['physicalRecoveries']<=e['maximumPhysicalRecoveries'],
    'zeroMissedGates':totals['missedGates']==0,
    'zeroSampledRoadDepartures':totals['outsideBodySamples']<=e['maximumOutsideBodySamples'],
    'noUnresolvedActivePlans':len(unresolved)<=e['maximumUnresolvedActiveSamples'],
    'overlapBelowExistingThreshold':max(race['maxCarPoseOverlap'],race['maxEnvironmentPoseOverlap'])<e['maximumOverlapMetres'] and race['overlapQuerySaturations']==0,
}
out=dict(scope=e['scope'],passed=sum(checks.values()),total=len(checks),checks=checks,totals=totals,finishes=finish,deadline=e['completionDeadline'],firstUnresolved=unresolved[:3])
(root/'corridor-analysis.json').write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8')
print(json.dumps({k:v for k,v in out.items() if k!='firstUnresolved'},indent=2))
raise SystemExit(0 if all(checks.values()) else 1)
