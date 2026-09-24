import json,subprocess,sys
from pathlib import Path
from runtime_log_errors import runtime_error_lines
root=Path(sys.argv[1]);prefix=sys.argv[2] if len(sys.argv)>2 else '';tools=Path(__file__).resolve().parent
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
results={}
for mode in ['off','on']:
 folder=root/(prefix+mode)
 outputs={}
 for script,extra in [('analyze-field-race.py',['--baseline-root','review/unity-34']),('analyze-checkpoint-flow.py',['--baseline-root','review/unity-34']),('analyze-preview-car.py',['--solo-root','review/unity-35/driver-braking-43']),('analyze-driver-frames.py',[])]:
  p=subprocess.run([sys.executable,str(tools/script),str(folder),*extra],capture_output=True,text=True)
  (folder/(script.removesuffix('.py')+'-output.txt')).write_text(p.stdout+p.stderr,encoding='utf-8');outputs[script]=p.returncode
 a=read(folder/'race.json');t=read(folder/'race.track.json');f=read(folder/'race.driver-frames.json');preview=read(folder/'preview-car-analysis.json');active=[s for s in t['samples'] if s['time']>0]
 results[mode]=dict(analyzerExitCodes=outputs,normalExit=read(folder/'exit.json')['exitCode']==0,runtimeErrors=runtime_error_lines(folder),finished=a['finished'],combinedSeconds=sum(c['time'] for c in a['cars']),previewSeconds=a['cars'][0]['time'],previewPlace=a['cars'][0]['place'],previewCP1=preview['checkpoints'][1]['crossingRankRange'],previewPasses=len(preview['sustainedPasses']),possibleHeldBehindSeconds=preview['possibleHeldBehindSlowerSeconds'],boundarySamples=sum(x['outsideBodySamples'] for x in t['results']),maximumBodyOffset=max(x['maxBodyOffset'] for x in t['results']),contacts=len(a['impacts']),peakImpact=max((x['relativeSpeed'] for x in a['impacts']),default=0),unresolved=sum(x['roadUnresolved'] for x in t['contextSamples']),physicalRecoveries=sum(c['physicalRecoveries'] for c in a['cars']),brakingSamplePercent=100*sum(s['pedal']<-.01 for s in active)/len(active),meanAiMsPerSnapshot=sum(x['decisionMs']+x['snapshotMs'] for x in f['frames'][1:])/sum(x['snapshotCalls'] for x in f['frames'][1:]),warmAllocatedBytes=sum(x['decisionBytes']+x['snapshotBytes']+x.get('forecastWorkerBytes',0) for x in f['frames'] if x['simTime']>=5))
a,b=results['off'],results['on']
checks=dict(noErrors=all(x['normalExit'] and not x['runtimeErrors'] for x in results.values()),complete=b['finished']==24,noRecovery=b['physicalRecoveries']==0,noAdditionalBoundarySamples=b['boundarySamples']<=a['boundarySamples'],collectiveWithin2Percent=b['combinedSeconds']<=a['combinedSeconds']*1.02,previewWithin2Seconds=b['previewSeconds']<=a['previewSeconds']+2,zeroWarmDriverAllocation=b['warmAllocatedBytes']==0)
out=dict(scope='Predeclared open-space first-lap screening; controlled cases, exact disabled parity, held-out field and rendering remain separate.',checks=checks,passed=all(checks.values()),results=results)
(root/(prefix+'comparison.json')).write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8');print(json.dumps(out,indent=2))
raise SystemExit(0 if out['passed'] else 1)
