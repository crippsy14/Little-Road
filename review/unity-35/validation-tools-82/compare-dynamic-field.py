"""Exploratory paired field evidence; incomplete fields never get a collective total."""
import json, subprocess, sys
from pathlib import Path
from runtime_log_errors import runtime_error_lines

root=Path(sys.argv[1]); tools=Path(__file__).resolve().parent
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
results={}
for mode in ['off','on']:
    folder=root/mode; exits={}
    for script,extra in [('analyze-field-race.py',['--baseline-root','review/unity-34']),('analyze-checkpoint-flow.py',['--baseline-root','review/unity-34']),('analyze-preview-car.py',['--solo-root','review/unity-35/driver-braking-43']),('analyze-driver-frames.py',[]),('analyze-context-braking.py',[])]:
        p=subprocess.run([sys.executable,str(tools/script),str(folder),*extra],capture_output=True,text=True)
        (folder/(script.removesuffix('.py')+'-output.txt')).write_text(p.stdout+p.stderr,encoding='utf-8'); exits[script]=p.returncode
    a=read(folder/'race.json'); t=read(folder/'race.track.json'); f=read(folder/'race.driver-frames.json'); field=read(folder/'field-analysis.json'); preview=read(folder/'preview-car-analysis.json')
    active=[s for s in t['samples'] if s['time']>0]; frames=f['frames'][1:]
    results[mode]=dict(analyzerExitCodes=exits,normalExit=read(folder/'exit.json')['exitCode']==0,runtimeErrors=runtime_error_lines(folder),finished=a['finished'],combinedSeconds=field['totalCarSeconds'],previewSeconds=a['cars'][0]['time'] if a['cars'][0]['finished'] else None,previewPlace=a['cars'][0]['place'],previewCP1=preview['checkpoints'][1]['crossingRankRange'],previewPasses=len(preview['sustainedPasses']),possibleHeldBehindSeconds=preview['possibleHeldBehindSlowerSeconds'],boundarySamples=sum(x['outsideBodySamples'] for x in t['results']),contacts=len(a['impacts']),peakImpact=max((x['relativeSpeed'] for x in a['impacts']),default=0),unresolved=sum(x['roadUnresolved'] for x in t['contextSamples']),physicalRecoveries=sum(c['physicalRecoveries'] for c in a['cars']),brakingSamplePercent=100*sum(s['pedal']<-.01 for s in active)/len(active),meanAiMsPerSnapshot=sum(x['decisionMs']+x['snapshotMs'] for x in frames)/sum(x['snapshotCalls'] for x in frames),warmAllocatedBytes=sum(x['decisionBytes']+x['snapshotBytes']+x.get('forecastWorkerBytes',0) for x in frames if x['simTime']>=5))
a,b=results['off'],results['on']
checks=dict(noErrors=all(x['normalExit'] and not x['runtimeErrors'] for x in results.values()),complete=b['finished']==24 and b['combinedSeconds'] is not None,fieldFlow=b['analyzerExitCodes']['analyze-field-race.py']==0,checkpointFlow=b['analyzerExitCodes']['analyze-checkpoint-flow.py']==0,noRecovery=b['physicalRecoveries']==0,noAdditionalBoundarySamples=b['boundarySamples']<=a['boundarySamples'],collectiveImproves=a['combinedSeconds'] is not None and b['combinedSeconds'] is not None and b['combinedSeconds']<a['combinedSeconds'],previewImproves=b['previewSeconds'] is not None and b['previewSeconds']<a['previewSeconds'],noHigherPeakImpact=b['peakImpact']<=a['peakImpact'],zeroWarmDriverAllocation=b['warmAllocatedBytes']==0)
out=dict(scope='Exploratory unchanged-grid candidate/reference field comparison; flags are recorded in each launch. Controlled failures, held-out grid and rendered performance remain separate; this is not promotion.',checks=checks,passed=all(checks.values()),results=results)
(root/'comparison.json').write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8');print(json.dumps(out,indent=2))
raise SystemExit(0 if out['passed'] else 1)
