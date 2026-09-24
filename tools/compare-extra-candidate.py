"""Predeclared110 pilot regression gates; pooled pace and moving review are separate."""
import argparse,json
from pathlib import Path
from runtime_log_errors import runtime_error_lines

p=argparse.ArgumentParser();p.add_argument('baseline',type=Path);p.add_argument('candidate',type=Path);args=p.parse_args()
read=lambda f:json.loads(f.read_text(encoding='utf-8-sig'))
def arm(folder):
    race=read(folder/'race.json');track=read(folder/'race.track.json');perf=read(folder/'driver-frame-analysis.json')
    frames=read(folder/'race.driver-frames.json')['frames'][1:];car=next(c for c in race['cars'] if c['id']==0)
    complete=race['finished']==race['carCount'] and all(c['finished'] for c in race['cars'])
    return dict(finished=race['finished'],carCount=race['carCount'],collectiveSeconds=sum(c['time'] for c in race['cars']) if complete else None,
        playerSeconds=car['time'] if car['finished'] else None,playerPlace=car['place'],impactEvents=len(race['impacts']),
        maxRelativeContactSpeed=max((e['relativeSpeed'] for e in race['impacts']),default=0),
        assistedRecoveries=len(race['assistedRecoveries']),physicalRecoveries=sum(c['physicalRecoveries'] for c in race['cars']),
        physicalRecoverySeconds=sum(c['physicalRecoverySeconds'] for c in race['cars']),missedGates=sum(c['missedGates'] for c in race['cars']),
        maxCarPoseOverlap=race['maxCarPoseOverlap'],maxEnvironmentPoseOverlap=race['maxEnvironmentPoseOverlap'],
        thirdCalls=sum(x.get('thirdCandidateCalls',0) for x in track['planningPhaseCosts']),
        thirdAccepted=sum(x.get('thirdCandidateAccepted',0) for x in track['planningPhaseCosts']),
        wholeAiMsPerTick=sum(f['decisionMs']+f['snapshotMs'] for f in frames)/sum(f['snapshotCalls'] for f in frames),
        performance=perf,normalExit=read(folder/'exit.json')['exitCode']==0,runtimeErrors=runtime_error_lines(folder))
a,b=arm(args.baseline),arm(args.candidate)
checks=dict(sameGraphics=a['performance']['settings']==b['performance']['settings'],
    normalComplete1080p=all(r['normalExit'] and not r['runtimeErrors'] and r['performance']['realTimeRenderingEvidence'] and r['performance']['checks']['allFramesAt1920x1080'] and r['performance']['checks']['completeCapture'] for r in (a,b)),
    all24Finish=all(r['finished']==r['carCount']==24 for r in (a,b)),
    zeroWarmAllocation=all(r['performance']['checks']['noObservedDriverAllocationAfter5s'] for r in (a,b)),
    playerWithin3Percent=a['playerSeconds'] is not None and b['playerSeconds'] is not None and b['playerSeconds']<=a['playerSeconds']*1.03,
    noAdditionalAssistedRecovery=b['assistedRecoveries']<=a['assistedRecoveries'],
    noAdditionalPhysicalRecovery=b['physicalRecoveries']<=a['physicalRecoveries'],
    noAdditionalRecoveryTime=b['physicalRecoverySeconds']<=a['physicalRecoverySeconds'],
    noAdditionalMissedGates=b['missedGates']<=a['missedGates'],
    boundedContactCount=b['impactEvents']<=max(8,a['impactEvents']*1.25),
    carOverlapRegression=b['maxCarPoseOverlap']<=a['maxCarPoseOverlap']+.1,
    environmentOverlapRegression=b['maxEnvironmentPoseOverlap']<=a['maxEnvironmentPoseOverlap']+.1,
    wholeAiWallWithin10Percent=b['wholeAiMsPerTick']<=a['wholeAiMsPerTick']*1.1,
    baselineExtraPathDisabled=a['thirdCalls']==0,
    candidateExtraPathExercised=b['thirdCalls']>0)
for key,limit in (('p95',16.7),('p99',33.3)):
    checks['frame'+key+'Regression']=b['performance']['observedFrameMs'][key]<=max(limit,a['performance']['observedFrameMs'][key]*1.1)
out=dict(scope='Same-build110 optional third-candidate pilot, fixed three-group scheduling. No allocating pace diagnostics. Per-course regression checks do not establish pooled pace improvement, broader acceptance or statistical confidence. Relative contact speed is retained for review, not called impact impulse.',
    before=a,after=b,collectiveGain=a['collectiveSeconds']-b['collectiveSeconds'] if a['collectiveSeconds'] is not None and b['collectiveSeconds'] is not None else None,
    playerGain=a['playerSeconds']-b['playerSeconds'] if a['playerSeconds'] is not None and b['playerSeconds'] is not None else None,checks=checks,passed=all(checks.values()))
(args.candidate/'extra-candidate-comparison.json').write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8')
print(json.dumps(dict(collectiveGain=out['collectiveGain'],playerGain=out['playerGain'],checks=checks,
    thirdCalls=b['thirdCalls'],thirdAccepted=b['thirdAccepted'],wholeAiBefore=a['wholeAiMsPerTick'],wholeAiAfter=b['wholeAiMsPerTick']),indent=2))
raise SystemExit(0 if out['passed'] else 1)
