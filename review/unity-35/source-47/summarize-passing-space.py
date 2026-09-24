"""Passing-space experiment screen; retains unchanged controlled regression failures."""
import argparse
import json
import subprocess
import sys
from pathlib import Path

p=argparse.ArgumentParser(description=__doc__)
p.add_argument('folder',type=Path)
args=p.parse_args()
project=Path(__file__).resolve().parents[1]
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))

def run(script,folder,*extra):
    result=subprocess.run([sys.executable,str(project/'tools'/script),str(folder),*map(str,extra)],capture_output=True,text=True)
    (folder/(script+'.log')).write_text(result.stdout+result.stderr,encoding='utf-8')
    return result.returncode==0

rows=[]
for name in ('field-control','field-anticipation'):
    folder=args.folder/name
    field_ok=run('analyze-field-race.py',folder,'--baseline-root',project/'review/unity-34')
    flow_ok=run('analyze-checkpoint-flow.py',folder,'--baseline-root',project/'review/unity-34')
    if not run('analyze-preview-car.py',folder,'--solo-root',project/'review/unity-35/driver-braking-43'):
        raise RuntimeError('Preview-car report failed')
    r=read(folder/'race.json');t=read(folder/'race.track.json');a=read(folder/'preview-car-analysis.json');launch=read(folder/'launch.json')
    cars={c['id']:c for c in r['cars']}
    active=[s for s in t['contextSamples'] if s['time'] <= (cars[s['id']]['time'] if cars[s['id']]['finished'] else r['simSeconds'])]
    contacts={(min(e['id'],e['other']),max(e['id'],e['other']),e['time']) for e in r['impacts'] if e['other']>=0}
    rows.append(dict(name=name,finished=r['finished'],totalCarSeconds=sum(c['time'] for c in cars.values()) if r['finished']==r['carCount'] else None,
                     checkpoint1=a['checkpoints'][1]['crossingRankRange'],finishPlace=a['finalPlace'],car0Seconds=cars[0]['time'],
                     pairContacts=len(contacts),maximumImpact=max(c['maxImpact'] for c in cars.values()),
                     outside=sum(c['outsideBodySamples'] for c in t['results']),recoveries=sum(c['recoveries']+c['physicalRecoveries'] for c in cars.values()),
                     unresolvedActiveSamples=sum(s['roadUnresolved'] for s in active),fieldGates=field_ok,checkpointGates=flow_ok,
                     declaredConfiguration=launch['anticipatePassingSpace']==(name=='field-anticipation') and launch['trafficPathCheck'] and launch['noGapCentres'] and launch['driverRisk']==5))
parity=run('verify-context-race-parity.py',project/'review/unity-35/traffic-path-46/field-risk5',args.folder/'field-control')
regressions_ok=run('analyze-coasting-regressions.py',args.folder/'regressions')
reg=read(args.folder/'regressions/analysis.json');candidate=rows[1]
checks=dict(disabledExactParity=parity,fieldGates=candidate['fieldGates'],checkpointGates=candidate['checkpointGates'],
            declaredConfigurations=all(r['declaredConfiguration'] for r in rows),
            recoverHalfLostPace=candidate['totalCarSeconds'] is not None and candidate['totalCarSeconds']<2215,
            previewProgress=candidate['checkpoint1'] is not None and candidate['checkpoint1'][1]<=20 and candidate['finishPlace'] is not None and candidate['finishPlace']<=8,
            contactReviewThreshold=candidate['pairContacts']<=10 and candidate['maximumImpact']<=6,
            controlledRegressions=regressions_ok)
out=dict(scope='Seed1709 only; no exact crowded replay, rendered or player acceptance.',
         passedScreen=all(checks.values()),checks=checks,results=rows,controlled=dict(passed=reg['passed'],total=reg['total'],failures=[k for k,v in reg['checks'].items() if not v]))
(args.folder/'summary.json').write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8')
print(json.dumps(out,indent=2))
raise SystemExit(0 if out['passedScreen'] else 1)
