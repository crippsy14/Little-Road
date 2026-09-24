"""Compare identical predicted/actual trajectories; report measured costs separately."""
import json,statistics
from pathlib import Path
root=Path(__file__).resolve().parent
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
out={'scope':'Initial flat-road interpolated corner envelopes, not full footprint/physical uncertainty acceptance. Paired hidden process timings, not rendered-frame performance.','modes':{},'parity':{}}
for mode in ('dense','adaptive'):
    report=read(root/f'paths-21-{mode}/analysis.json')
    costs=[c for s in report['summary'].values() for c in s['forecastCosts'][1:]]
    normal=[c for name,s in report['summary'].items() if not name.startswith('overspeed') for c in s['forecastCosts'][1:]]
    out['modes'][mode]={'passed':report['passed'],'total':report['total'],
        'warmMeanMs':{k:statistics.mean(c[k] for c in costs) for k in costs[0]},
        'normalWarmMeanMs':{k:statistics.mean(c[k] for c in normal) for k in normal[0]}}
for case in read(root/'paths-21-dense/expectations.json')['cases']:
    old=read(root/f'paths-20-control-3/{case}/race.path-prediction.json')
    dense=read(root/f'paths-21-dense/{case}/race.path-prediction.json')
    adaptive=read(root/f'paths-21-adaptive/{case}/race.path-prediction.json')
    out['parity'][case]={'denseSamplesEqual20':dense['samples']==old['samples'],
        'adaptiveSamplesEqualDense':adaptive['samples']==dense['samples'],
        'adaptiveQueries':adaptive['envelopeQueries'],'denseQueries':len(dense['samples'])*4,
        'substepAuditSamples':adaptive['envelopeAuditSamples'],'maxBoundUnderestimate':adaptive['envelopeMaximumUnderestimate'],
        'firstUncertain':adaptive['envelopeFirstUncertain'],'predictedDeparture':adaptive['predictedDeparture'],'actualDeparture':adaptive['actualDeparture']}
out['allParityPassed']=all(v['denseSamplesEqual20'] and v['adaptiveSamplesEqualDense'] for v in out['parity'].values())
(root/'envelope-21-comparison.json').write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8')
print(json.dumps(out,indent=2))
raise SystemExit(0 if out['allParityPassed'] else 1)
