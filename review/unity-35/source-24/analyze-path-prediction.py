"""Prospective commands from a frozen path; physical car is independent truth."""
import json,sys
from pathlib import Path
root=Path(sys.argv[1]);read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
m=read(root/'manifest.json');e=read(root/'expectations.json');checks=[];summary={};unsafe=0
def check(name,ok):checks.append(dict(name=name,passed=bool(ok)))
check('Every declared probe completed once',m['complete'] and {r['name'] for r in m['runs']}==set(e['cases']) and len(m['runs'])==len(e['cases']))
for run in m['runs']:
    name=run['name'];d=read(root/name/'race.path-prediction.json');rows=d['samples'];eligible=[s for s in rows if s['flatTarmacDomain']]
    check(name+': normal process exit',run['exitCode']==0)
    if 'sourceSha256' in e:
        check(name+': first observation matches captured state',bool(d.get('sourceApproach',{}).get('bodyAsset')) and 'restoredHeadingError' in d and d['restoredPositionError']<=.001 and d['restoredHeadingError']<=.05 and d['restoredVelocityError']<=.001 and d['restoredSteeringError']<=.00001)
    if 'forecastControlSteps' in e:
        check(name+': declared forecast cadence',d.get('forecastControlSteps')==e['forecastControlSteps'])
    if 'forecastCosts' in d:
        check(name+': warm forecasts exactly match the cold forecast',d['warmForecastParity'] and len(d['forecastCosts'])==4)
    check(name+': complete declared forecast window',d['completed'] and len(rows)==round(e['horizon']/d['physicsStep'])+1 and abs(rows[-1]['time']-e['horizon'])<.001)
    check(name+': sufficient supported calibration samples',len(eligible)>=e['minimumEligibleSamples'])
    pos=max((s['positionError'] for s in eligible),default=float('inf'))
    under=max((max(0,s['actualBodyOffset']-s['predictedBodyOffset']) for s in eligible),default=float('inf'))
    check(name+': bounded prospective position error',pos<=e['positionMaximumMetres'])
    check(name+': bounded body-clearance underestimate',under<=e['bodyUnderestimateMaximumMetres'])
    pred,actual=d['predictedDeparture'],d['actualDeparture']
    if e.get('requireContactFree'):
        check(name+': contact-free physical witness',d['contactsAfter']==d['contactsBefore'])
    if 'adaptiveEnvelope' in e:
        check(name+': declared envelope mode',d.get('adaptiveEnvelope')==e['adaptiveEnvelope'])
    if e.get('adaptiveEnvelope'):
        check(name+': swept bounds cover dense reference substeps',d['envelopeAuditSamples']==(len(rows)-1)*9 and d['envelopeMaximumUnderestimate']<=e['envelopeUnderestimateTolerance'])
        uncertain=d['envelopeFirstUncertain']
        check(name+': envelope flags departure without rejecting normal path',uncertain<0 and pred<0 or uncertain>=0 and pred>=0 and -.001<=pred-uncertain<=e['departureTimingErrorSeconds'])
    check(name+': predicts actual departure or containment',pred<0 and actual<0 or pred>=0 and actual>=0 and abs(pred-actual)<=e['departureTimingErrorSeconds'])
    if not name.startswith('overspeed') and e.get('requireNormalPathsContained',True):
        check(name+': normal path remains contained and contact-free',pred<0 and actual<0 and d['contactsAfter']==d['contactsBefore'])
    elif actual>=0:unsafe+=1
    summary[name]=dict(eligibleSamples=len(eligible),totalSamples=len(rows),maxPositionError=pos,maxBodyUnderestimate=under,predictedDeparture=pred,actualDeparture=actual,
        maxPredictedBodyOffset=max(s['predictedBodyOffset'] for s in rows),maxActualBodyOffset=max(s['actualBodyOffset'] for s in rows),forecastMilliseconds=d['forecastMilliseconds'])
    if 'forecastCosts' in d:summary[name]['forecastCosts']=d['forecastCosts']
    if e.get('adaptiveEnvelope'):
        summary[name]['envelope']={k:d[k] for k in ('envelopeQueries','envelopeAuditSamples','envelopeMaximumUnderestimate','envelopeFirstUncertain','envelopeMaximumBound')}
if e.get('requireAtLeastOneOverspeedDeparture',True):check('Overspeed set demonstrates at least one real road-envelope failure',unsafe>=1)
out=dict(scope=e['scope'],passed=sum(c['passed'] for c in checks),total=len(checks),checks=checks,summary=summary)
(root/'analysis.json').write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8')
print(json.dumps(dict(passed=out['passed'],total=out['total'],summary=summary,failures=[c for c in checks if not c['passed']]),indent=2))
raise SystemExit(0 if out['passed']==out['total'] else 1)
