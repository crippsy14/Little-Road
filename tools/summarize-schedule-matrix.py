"""Summarise all paired seeds without hiding failed gates or slower fields."""
import argparse,json,statistics
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('study',type=Path);a=p.parse_args()
rows=[]
for path in sorted(a.study.glob('*-after/schedule-comparison.json')):
    r=json.loads(path.read_text(encoding='utf-8-sig'))
    name=path.parent.name
    rows.append(dict(name=name,track=name.split('-')[0],passed=r['passed'],
        failedChecks=[k for k,v in r['checks'].items() if not v],
        collectiveGain=r['collectiveReductionSeconds'],playerGain=r['playerReductionSeconds'],
        before={k:r['before'][k] for k in ('collectiveSeconds','playerSeconds','playerPlace','impactEvents','maxImpactSpeed','physicalRecoveries','assistedRecoveries','maxCarPoseOverlap','maxEnvironmentPoseOverlap','wholeAiMsPerTick')},
        after={k:r['after'][k] for k in ('collectiveSeconds','playerSeconds','playerPlace','impactEvents','maxImpactSpeed','physicalRecoveries','assistedRecoveries','maxCarPoseOverlap','maxEnvironmentPoseOverlap','wholeAiMsPerTick')},
        frameBefore=r['before']['performance']['observedFrameMs'],frameAfter=r['after']['performance']['observedFrameMs']))
out={'pairs':rows,'tracks':{}}
for track in ('bayview','cedar'):
    subset=[r for r in rows if r['track']==track]
    assert len(subset)==3,'Expected the declared three paired seeds per course'
    out['tracks'][track]=dict(pairs=len(subset),passed=sum(r['passed'] for r in subset),
        collectiveGainMedian=statistics.median(r['collectiveGain'] for r in subset),
        collectiveGainMean=statistics.mean(r['collectiveGain'] for r in subset),
        playerGainMedian=statistics.median(r['playerGain'] for r in subset),
        p95BeforeRange=[min(r['frameBefore']['p95'] for r in subset),max(r['frameBefore']['p95'] for r in subset)],
        p95AfterRange=[min(r['frameAfter']['p95'] for r in subset),max(r['frameAfter']['p95'] for r in subset)],
        p99AfterRange=[min(r['frameAfter']['p99'] for r in subset),max(r['frameAfter']['p99'] for r in subset)],
        wholeAiWallIncreasePercentRange=[min(100*(r['after']['wholeAiMsPerTick']/r['before']['wholeAiMsPerTick']-1) for r in subset),max(100*(r['after']['wholeAiMsPerTick']/r['before']['wholeAiMsPerTick']-1) for r in subset)])
out['interpretation']='Three paired seeds on one machine, not a confidence interval or moving/player acceptance. Keep failed gates and per-seed regressions visible. Whole AI is joined main-thread wall cost, not summed worker CPU utilisation.'
(a.study/'matrix-summary.json').write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8')
print(json.dumps(out['tracks'],indent=2))
