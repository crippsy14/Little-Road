"""Preserve cadence experiment cost/error and independent actual-car parity."""
import hashlib,json,statistics
from pathlib import Path
root=Path(__file__).resolve().parent
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
cases=read(root/'paths-20-control-1/expectations.json')['cases']
actual_keys=('time','actualPosition','actualBodyOffset','actualPedal','actualSteering','flatTarmacDomain')
out={'scope':'Six initial two-second club-car forecasts; paired hidden timing, not runtime frame acceptance. Actual driving remains native-rate.','cadences':{}}
for cadence in (1,3,6):
    folder=root/f'paths-20-control-{cadence}'
    report=read(folder/'analysis.json')
    costs=[c for s in report['summary'].values() for c in s['forecastCosts'][1:]]
    parity={}
    for case in cases:
        reference=read(root/f'paths-19-profile/{case}/race.path-prediction.json')
        current=read(folder/case/'race.path-prediction.json')
        parity[case]={
            'actualMotionInputsEqual19':len(reference['samples'])==len(current['samples']) and all(all(a[k]==b[k] for k in actual_keys) for a,b in zip(reference['samples'],current['samples'])),
            'allComparisonSamplesEqual19':reference['samples']==current['samples'],
        }
    out['cadences'][str(cadence)]={
        'passed':report['passed'],'total':report['total'],
        'meanWarmCost':{key:statistics.mean(c[key] for c in costs) for key in costs[0]},
        'maxPositionError':max(s['maxPositionError'] for s in report['summary'].values()),
        'maxBodyUnderestimate':max(s['maxBodyUnderestimate'] for s in report['summary'].values()),
        'failures':[c['name'] for c in report['checks'] if not c['passed']],
        'parity':parity,
    }
out['warmReduction1to3Percent']=100*(1-out['cadences']['3']['meanWarmCost']['totalMs']/out['cadences']['1']['meanWarmCost']['totalMs'])
(root/'cadence-20-comparison.json').write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8')
print(json.dumps(out,indent=2))
