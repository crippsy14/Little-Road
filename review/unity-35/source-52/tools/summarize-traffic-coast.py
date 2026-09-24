"""Compare the declared coast-only and combined arms without hiding failed gates."""
import argparse
import json
import subprocess
import sys
from pathlib import Path

p = argparse.ArgumentParser(description=__doc__)
p.add_argument('folder', type=Path)
p.add_argument('--continuation', action='store_true', help='Compare the reviewed combined50 control against checked continuation52.')
args = p.parse_args()
project = Path(__file__).resolve().parents[1]
read = lambda p: json.loads(p.read_text(encoding='utf-8-sig'))

def run(script, folder, *extra):
    result = subprocess.run([sys.executable, str(project/'tools'/script), str(folder), *map(str, extra)], capture_output=True, text=True)
    (folder/(script+'.log')).write_text(result.stdout+result.stderr, encoding='utf-8')
    return result.returncode == 0

rows = []
for name in (('field-control', 'field-continuation') if args.continuation else ('field-control', 'field-coast', 'field-combined')):
    folder = args.folder/name
    field_ok = run('analyze-field-race.py', folder, '--baseline-root', project/'review/unity-34')
    flow_ok = run('analyze-checkpoint-flow.py', folder, '--baseline-root', project/'review/unity-34')
    if not run('analyze-preview-car.py', folder, '--solo-root', project/'review/unity-35/driver-braking-43'):
        raise RuntimeError('Preview-car analysis failed')
    pedal_ok = run('analyze-pedal-stages.py', folder)
    r=read(folder/'race.json'); t=read(folder/'race.track.json'); a=read(folder/'preview-car-analysis.json')
    launch=read(folder/'launch.json'); pedal=read(folder/'pedal-stage-analysis.json')
    cars={c['id']:c for c in r['cars']}
    active=[s for s in t['contextSamples'] if s['time'] <= (cars[s['id']]['time'] if cars[s['id']]['finished'] else r['simSeconds'])]
    contacts={(min(e['id'],e['other']),max(e['id'],e['other']),e['time']) for e in r['impacts'] if e['other']>=0}
    matched=pedal['counts']['matched']
    row=dict(name=name, finished=r['finished'], totalCarSeconds=sum(c['time'] for c in cars.values()) if r['finished']==r['carCount'] else None,
             lastCarSeconds=max(c['time'] for c in cars.values()) if r['finished']==r['carCount'] else None,
             checkpoint1=a['checkpoints'][1]['crossingRankRange'], finishPlace=a['finalPlace'], car0Seconds=cars[0]['time'],
             pairContacts=len(contacts), maximumImpact=max(c['maxImpact'] for c in cars.values()),
             outside=sum(c['outsideBodySamples'] for c in t['results']), recoveries=sum(c['recoveries']+c['physicalRecoveries'] for c in cars.values()),
             unresolvedActiveSamples=sum(s['roadUnresolved'] for s in active), fieldGates=field_ok, checkpointGates=flow_ok,
             coastAttempts=sum(c['coastAttempts'] for c in t['roadCosts']), coastAccepted=sum(c['coastAccepted'] for c in t['roadCosts']),
             coastActiveSamples=sum(s['coastContinuation'] for s in t['pedalSamples']),
             continuationAttempts=sum(c.get('continuationAttempts',0) for c in t['roadCosts']),
             continuationAccepted=sum(c.get('continuationAccepted',0) for c in t['roadCosts']),
             capturedTargetSwitches=sum(len(a['entries']) for a in t.get('switchPlanAudits',[])),
             droppedTargetSwitches=sum(a['dropped'] for a in t.get('switchPlanAudits',[])),
             pedalAuditValid=pedal_ok, brakingPercent=100*pedal['counts']['braking']/matched,
             brakingPercentByStage={k:100*v['braking']/matched for k,v in pedal['stages'].items()},
             declaredConfiguration=launch['trafficCoastContinuation']==(args.continuation or name!='field-control') and launch['anticipatePassingSpace']==(args.continuation or name=='field-combined') and launch.get('compareContinuation',False)==(name=='field-continuation') and not launch['anticipateBlockingCar'] and launch['trafficPathCheck'] and launch['noGapCentres'] and launch['driverRisk']==5)
    row['checks']=dict(field=field_ok, checkpoints=flow_ok, declaredConfiguration=row['declaredConfiguration'],
                       normalExit=read(folder/'exit.json')['exitCode']==0, pedalAudit=pedal_ok,
                       collectivePace=row['totalCarSeconds'] is not None and row['totalCarSeconds']<2215,
                       previewProgress=row['checkpoint1'][1]<=20 and row['finishPlace'] is not None and row['finishPlace']<=8,
                       contacts=row['pairContacts']<=10 and row['maximumImpact']<=6,
                       physicalCompletion=row['outside']==0 and row['recoveries']==0)
    rows.append(row)

baseline=project/('review/unity-35/traffic-coast-50/field-combined' if args.continuation else 'review/unity-35/traffic-path-46/field-risk5')
control=args.folder/'field-control'
result=subprocess.run([sys.executable,str(project/'tools/verify-context-race-parity.py'),str(baseline),str(control)],capture_output=True,text=True)
(args.folder/'disabled-parity.log').write_text(result.stdout+result.stderr,encoding='utf-8')
parity=read(control/'motion-parity.json')
# Keep the original raw comparison result. Separately distinguish post-finish tails.
old=read(baseline/'race.json'); new=read(control/'race.json')
end=max(c['time'] for c in old['cars']) if all(c['finished'] for c in old['cars']) else None
window=dict(lastFinishSeconds=end, oldRows=len(old['motion']), newRows=len(new['motion']),
            scope='Supplement only; raw parity above remains authoritative for full stored arrays.')
if end is not None:
    aa=[s for s in old['motion'] if s['time']<=end]
    bb=[s for s in new['motion'] if s['time']<=end]
    window.update(raceActiveMotionEqual=aa==bb, oldRaceActiveRows=len(aa), newRaceActiveRows=len(bb))
(args.folder/'control-motion-window.json').write_text(json.dumps(window,indent=2)+'\n',encoding='utf-8')
reg_folder=args.folder/('regressions' if args.continuation else 'regressions-combined')
reg_ok=run('analyze-coasting-regressions.py',reg_folder)
reg=read(reg_folder/'analysis.json')
out=dict(scope='Seed1709 comparison. Pedal percentages are diagnostic attribution, not proof of unnecessary braking or rendered performance. Only the final candidate arm has this controlled regression suite. Switch counts require the audit flag and disclose bounded-capture drops.',
         results=rows, disabledRawParity=parity['passed'], disabledParityChecks=parity['checks'], motionWindow=window,
         controlled=dict(passed=reg['passed'],total=reg['total'],failures=[k for k,v in reg['checks'].items() if not v]),
         candidatePassesDeclaredScreen=all(rows[-1]['checks'].values()) and reg_ok and parity['passed'])
(args.folder/'summary.json').write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8')
print(json.dumps(out,indent=2))
raise SystemExit(0 if out['candidatePassesDeclaredScreen'] else 1)
