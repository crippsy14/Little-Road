"""Evaluate the declared scheduling experiment; racing parity is not expected."""
import argparse
import json
import math
from pathlib import Path
from runtime_log_errors import runtime_error_lines

p = argparse.ArgumentParser()
p.add_argument('baseline', type=Path)
p.add_argument('candidate', type=Path)
p.add_argument('--group-gates', action='store_true', help='Use the explicitly revised108 gates; preserve107 historical failures.')
args = p.parse_args()
read = lambda path: json.loads(path.read_text(encoding='utf-8-sig'))

def dist(values):
    a = sorted(values)
    return dict(mean=sum(a)/len(a), p95=a[math.ceil(.95*len(a))-1], maximum=a[-1])

def arm(folder):
    race = read(folder/'race.json')
    perf = read(folder/'driver-frame-analysis.json')
    frames = read(folder/'race.driver-frames.json')['frames'][1:]
    player = next(c for c in race['cars'] if c['id'] == 0)
    return dict(finished=race['finished'], carCount=race['carCount'],
        collectiveSeconds=sum(c['time'] for c in race['cars']), playerSeconds=player['time'],
        playerPlace=player['place'], impactEvents=len(race['impacts']),
        maxImpactSpeed=max((i['relativeSpeed'] for i in race['impacts']), default=0),
        assistedRecoveries=len(race['assistedRecoveries']),
        physicalRecoveries=sum(c['physicalRecoveries'] for c in race['cars']),
        maxCarPoseOverlap=race['maxCarPoseOverlap'], maxEnvironmentPoseOverlap=race['maxEnvironmentPoseOverlap'],
        missedGates=sum(c['missedGates'] for c in race['cars']),
        wholeAiMsPerTick=sum(r['decisionMs']+r['snapshotMs'] for r in frames)/sum(r['snapshotCalls'] for r in frames),
        plansPerFrame=dist([r['contextPlans'] for r in frames]),
        plansPerFrameAfter5s=dist([r['contextPlans'] for r in frames if r['simTime']>=5]),
        totalPlans=sum(r['contextPlans'] for r in frames),
        performance=perf, normalExit=read(folder/'exit.json')['exitCode']==0,
        runtimeErrors=runtime_error_lines(folder))

a,b = arm(args.baseline), arm(args.candidate)
checks = {
    'sameGraphicsSettings': a['performance']['settings'] == b['performance']['settings'],
    'normalRealTime1080p': all(r['normalExit'] and not r['runtimeErrors'] and r['performance']['realTimeRenderingEvidence']
        and r['performance']['checks']['allFramesAt1920x1080'] and r['performance']['checks']['completeCapture'] for r in (a,b)),
    'all24Finish': all(r['finished']==r['carCount']==24 for r in (a,b)),
    'noWarmMeasuredAllocation': all(r['performance']['checks']['noObservedDriverAllocationAfter5s'] for r in (a,b)),
    'collectiveWithin3Percent': b['collectiveSeconds'] <= a['collectiveSeconds']*1.03,
    'playerWithin3Percent': b['playerSeconds'] <= a['playerSeconds']*1.03,
    'noAdditionalAssistedRecovery': b['assistedRecoveries'] <= a['assistedRecoveries'],
    'impactEventsWithin25Percent': b['impactEvents'] <= a['impactEvents']*1.25,
}
for key,limit in (('p95',16.7),('p99',33.3)):
    checks[f'frame{key}ImprovesOrWithinBudget'] = b['performance']['observedFrameMs'][key] <= max(limit,a['performance']['observedFrameMs'][key])
if args.group_gates:
    del checks['impactEventsWithin25Percent']
    checks.update(
        wholeAiWallWithin10Percent=b['wholeAiMsPerTick']<=a['wholeAiMsPerTick']*1.1,
        frameP95Within16_7ms=b['performance']['observedFrameMs']['p95']<=16.7,
        frameP99Within33_3ms=b['performance']['observedFrameMs']['p99']<=33.3,
        boundedImpactCount=b['impactEvents']<=max(8,a['impactEvents']*1.25),
        boundedImpactSeverity=b['maxImpactSpeed']<=max(5,a['maxImpactSpeed']),
        noAdditionalPhysicalRecovery=b['physicalRecoveries']<=a['physicalRecoveries'],
        noAdditionalMissedGates=b['missedGates']<=a['missedGates'])
out = dict(scope='Same-build schedule-off/on normal-speed comparison. Observation times intentionally differ; this does not claim exact motion parity or multi-seed/subjective acceptance.',
    gateVersion=108 if args.group_gates else 107,
    baseline=str(args.baseline), candidate=str(args.candidate), before=a, after=b,
    collectiveReductionSeconds=a['collectiveSeconds']-b['collectiveSeconds'],
    playerReductionSeconds=a['playerSeconds']-b['playerSeconds'], checks=checks, passed=all(checks.values()))
(args.candidate/'schedule-comparison.json').write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8')
print(json.dumps({k:out[k] for k in ('collectiveReductionSeconds','playerReductionSeconds','checks','passed')},indent=2))
for label,row in [('before',a),('after',b)]:
    print(label,json.dumps({k:row[k] for k in ('wholeAiMsPerTick','impactEvents','maxImpactSpeed','physicalRecoveries','plansPerFrameAfter5s')}))
    print('frames',json.dumps(row['performance']['observedFrameMs']))
raise SystemExit(0 if out['passed'] else 1)
