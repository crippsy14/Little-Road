"""Run existing field diagnostics and retain failed risk comparisons explicitly."""
import argparse
import json
import subprocess
import sys
from pathlib import Path

p=argparse.ArgumentParser(description=__doc__)
p.add_argument('folder',type=Path)
args=p.parse_args()
project=Path(__file__).resolve().parents[1]
read=lambda f:json.loads(f.read_text(encoding='utf-8-sig'))
rows=[]
for risk in (0,5):
    folder=args.folder/f'field-risk{risk}'
    diagnostics={}
    for script,extra in [('analyze-preview-car.py',['--solo-root',str(project/'review/unity-35/driver-braking-43')]),
                         ('analyze-field-race.py',['--baseline-root',str(project/'review/unity-34')]),
                         ('analyze-checkpoint-flow.py',['--baseline-root',str(project/'review/unity-34')])]:
        run=subprocess.run([sys.executable,str(project/'tools'/script),str(folder),*extra],capture_output=True,text=True)
        (folder/(script+'.log')).write_text(run.stdout+run.stderr,encoding='utf-8')
        diagnostics[script]=run.returncode==0
    race=read(folder/'race.json');track=read(folder/'race.track.json');preview=read(folder/'preview-car-analysis.json')
    cars={c['id']:c for c in race['cars']}
    incidents={(min(e['id'],e['other']),max(e['id'],e['other']),e['time']) for e in race['impacts'] if e['other']>=0}
    active=[s for s in track['contextSamples'] if s['time']<= (cars[s['id']]['time'] if cars[s['id']]['finished'] else race['simSeconds'])]
    rows.append(dict(risk=risk,finished=race['finished'],cars=race['carCount'],
                     totalCarSeconds=sum(c['time'] for c in cars.values()) if race['finished']==race['carCount'] else None,
                     previewCheckpoint1=preview['checkpoints'][1]['crossingRankRange'],previewFinalPlace=preview['finalPlace'],
                     pairContacts=len(incidents),contactRecords=len(race['impacts']),maximumImpact=max(c['maxImpact'] for c in cars.values()),
                     outsideBodySamples=sum(c['outsideBodySamples'] for c in track['results']),
                     assistedRecoveries=sum(c['recoveries'] for c in cars.values()),physicalRecoveries=sum(c['physicalRecoveries'] for c in cars.values()),
                     unresolvedActiveSamples=sum(s['roadUnresolved'] for s in active),diagnostics=diagnostics))
run=subprocess.run([sys.executable,str(project/'tools/verify-context-race-parity.py'),str(project/'review/unity-35/driver-braking-43/field-neutral'),str(args.folder/'field-risk0')],capture_output=True,text=True)
(args.folder/'parity.log').write_text(run.stdout+run.stderr,encoding='utf-8')
base,candidate=rows
checks={'risk0ExactParity':run.returncode==0,
        'earlierCheckpointProgress':candidate['previewCheckpoint1'] is not None and candidate['previewCheckpoint1'][1]<base['previewCheckpoint1'][0],
        'betterPreviewFinish':candidate['previewFinalPlace'] is not None and candidate['previewFinalPlace']<base['previewFinalPlace'],
        'fasterCompleteField':candidate['totalCarSeconds'] is not None and candidate['totalCarSeconds']<base['totalCarSeconds'],
        'fieldGates':candidate['diagnostics']['analyze-field-race.py'],
        'checkpointDeadlines':candidate['diagnostics']['analyze-checkpoint-flow.py'],
        'contactReviewThreshold':candidate['pairContacts']<=10 and candidate['maximumImpact']<=6}
out=dict(scope='One-seed 24-car experiment, no rendered-frame or player acceptance.',
         readyForControlledRegressions=all(checks.values()),checks=checks,results=rows,
         outstanding='Existing no-unresolved-path and rendered AI CPU/startup requirements remain open. A passing field run alone never promotes production.')
(args.folder/'summary.json').write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8')
print(json.dumps(out,indent=2))
raise SystemExit(0 if all(checks.values()) else 1)
