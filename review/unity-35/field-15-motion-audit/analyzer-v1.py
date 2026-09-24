"""Evaluate known-input model calibration; never label it prospective AI safety."""
import json,sys,math
from pathlib import Path
root=Path(sys.argv[1]);read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
data=read(root/'race.motion-prediction.json');expect=read(root/'prediction-expectations.json')
def stats(values):
    values=sorted(values)
    if not values:return None
    return dict(n=len(values),mean=sum(values)/len(values),p95=values[math.ceil(.95*len(values))-1],max=values[-1])
def group(rows):
    return dict(samples=len(rows),positionMetres=stats([s['positionError'] for s in rows]),speedMps=stats([s['speedError'] for s in rows]),headingDegrees=stats([s['headingError'] for s in rows]),
        bodyUnderestimateMetres=stats([max(0,s['actualBodyOffset']-s['predictedBodyOffset']) for s in rows]))
rows=data['samples'];horizons=sorted({round(s['horizon'],2) for s in rows});summary={}
for h in horizons:
    subset=[s for s in rows if round(s['horizon'],2)==h]
    summary[str(h)]=dict(flatContactFree=group([s for s in subset if s['flatContactFree']]),otherWindows=group([s for s in subset if not s['flatContactFree']]))
one=[s for s in rows if s['flatContactFree'] and abs(s['horizon']-1)<.01];score=group(one)
checks={
    'normalProcessExit':read(root/'exit.json')['exitCode']==0,
    'noDroppedEvidence':data['dropped']==0,
    'allDeclaredCarsCaptured':set(data['selectedIds'])=={s['id'] for s in rows},
    'sufficientOneSecondCalibration':len(one)>=expect['minimumOneSecondWindows'],
    'positionP95':bool(one) and score['positionMetres']['p95']<=expect['positionP95Metres'],
    'positionMaximum':bool(one) and score['positionMetres']['max']<=expect['positionMaximumMetres'],
    'speedP95':bool(one) and score['speedMps']['p95']<=expect['speedP95Mps'],
    'headingP95':bool(one) and score['headingDegrees']['p95']<=expect['headingP95Degrees'],
    'bodyUnderestimateMaximum':bool(one) and score['bodyUnderestimateMetres']['max']<=expect['bodyUnderestimateMaximumMetres'],
}
critical=[s for s in one if s['id']==16 and 50<s['end']<55]
out=dict(scope=data['scope'],domain=data['domain'],expectations=expect,checks=checks,passed=all(checks.values()),byHorizon=summary,
    equipment={e:group([s for s in one if s['equipment']==e]) for e in sorted({s['equipment'] for s in one})},
    switchbackCar16=group(critical),worstOneSecond=sorted(one,key=lambda s:s['positionError'],reverse=True)[:10])
(root/'prediction-analysis.json').write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8')
print(json.dumps({k:out[k] for k in ('passed','checks','byHorizon','switchbackCar16')},indent=2))
raise SystemExit(0 if out['passed'] else 1)
