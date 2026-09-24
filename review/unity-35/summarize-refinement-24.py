import json,statistics
from pathlib import Path
root=Path(__file__).resolve().parent
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
out={'scope':'Geometry-only refinement; compare forecast and actual samples exactly, preserve physical error separately. Timing is hidden-probe evidence, not frame acceptance.','sets':{}}
for current,previous in (('approaches-24-fast','approaches-23-fast'),('paths-24-adaptive','paths-21-adaptive')):
    report=read(root/current/'analysis.json');cases={};costs=[]
    for name,s in report['summary'].items():
        now=read(root/current/name/'race.path-prediction.json');old=read(root/previous/name/'race.path-prediction.json')
        costs.extend(now['forecastCosts'][1:])
        cases[name]={'comparisonSamplesExact':now['samples']==old['samples'],
            'firstUncertain':now['envelopeFirstUncertain'],'previousFirstUncertain':old['envelopeFirstUncertain'],
            'actualDeparture':now['actualDeparture'],'predictedDeparture':now['predictedDeparture'],
            'queries':now['envelopeQueries'],'maximumBoundUnderestimate':now['envelopeMaximumUnderestimate']}
    out['sets'][current]={'passed':report['passed'],'total':report['total'],'cases':cases,
        'allSamplesExact':all(c['comparisonSamplesExact'] for c in cases.values()),
        'warmMeanMs':{key:statistics.mean(c[key] for c in costs) for key in costs[0]},
        'maximumPositionError':max(s['maxPositionError'] for s in report['summary'].values()),
        'maximumBodyUnderestimate':max(s['maxBodyUnderestimate'] for s in report['summary'].values())}
(root/'refinement-24-comparison.json').write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8')
print(json.dumps(out,indent=2))
raise SystemExit(0 if all(s['allSamplesExact'] for s in out['sets'].values()) else 1)
