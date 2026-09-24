"""Supplement mechanical evidence with held-out field and fixed-resolution capture."""
import json,subprocess,sys
from pathlib import Path
from runtime_log_errors import runtime_error_lines
root=Path(sys.argv[1]);tools=Path(__file__).resolve().parent
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
summaries={}
for name in ['heldout-off','heldout-on','rendered-on','rendered-off']:
 folder=root/name
 if not (folder/'exit.json').exists():raise SystemExit('Capture unfinished: '+name)
 outputs={}
 for script,extra in [('analyze-field-race.py',['--baseline-root','review/unity-34']),('analyze-checkpoint-flow.py',['--baseline-root','review/unity-34']),('analyze-driver-frames.py',[])]:
  p=subprocess.run([sys.executable,str(tools/script),str(folder),*extra],capture_output=True,text=True)
  (folder/(script.removesuffix('.py')+'-output.txt')).write_text(p.stdout+p.stderr,encoding='utf-8');outputs[script]=p.returncode
 a=read(folder/'race.json');t=read(folder/'race.track.json');f=read(folder/'race.driver-frames.json');active=[s for s in t['samples'] if s['time']>0]
 summaries[name]=dict(analyzerExitCodes=outputs,normalExit=read(folder/'exit.json')['exitCode']==0,runtimeErrors=runtime_error_lines(folder),finished=a['finished'],combinedSeconds=sum(c['time'] for c in a['cars']),previewSeconds=a['cars'][0]['time'],previewPlace=a['cars'][0]['place'],boundarySamples=sum(x['outsideBodySamples'] for x in t['results']),maximumBodyOffset=max(x['maxBodyOffset'] for x in t['results']),contacts=len(a['impacts']),peakImpact=max((x['relativeSpeed'] for x in a['impacts']),default=0),unresolved=sum(x['roadUnresolved'] for x in t['contextSamples']),physicalRecoveries=sum(c['physicalRecoveries'] for c in a['cars']),brakingSamplePercent=100*sum(s['pedal']<-.01 for s in active)/len(active),meanAiMsPerSnapshot=sum(x['decisionMs']+x['snapshotMs'] for x in f['frames'][1:])/sum(x['snapshotCalls'] for x in f['frames'][1:]))
 if name.startswith('rendered'):
  mode=name.split('-')[1]
  p=subprocess.run([sys.executable,str(tools/'verify-context-race-parity.py'),str(root/mode),str(folder)],capture_output=True,text=True)
  (folder/'parity-output.txt').write_text(p.stdout+p.stderr,encoding='utf-8');summaries[name]['exactHiddenRaceParity']=p.returncode==0
  # Do not substitute a completed-race-tail interpretation for raw parity.
  summaries[name]['frameSummary']=read(folder/'driver-frame-analysis.json') if (folder/'driver-frame-analysis.json').exists() else None
a,b=summaries['heldout-off'],summaries['heldout-on']
checks=dict(complete=a['finished']==b['finished']==24,noRecovery=b['physicalRecoveries']==0,noAdditionalBoundarySamples=b['boundarySamples']<=a['boundarySamples'],collectiveWithin2Percent=b['combinedSeconds']<=a['combinedSeconds']*1.02,previewWithin2Seconds=b['previewSeconds']<=a['previewSeconds']+2)
out=dict(scope='Held-out equipment arrangement and normal-speed captures. Existing absolute field and performance gates remain separate and unchanged.',heldoutScreen=checks,summaries=summaries)
(root/'validation-summary.json').write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'heldoutScreen':checks,'summaries':{k:{kk:vv for kk,vv in v.items() if kk!='frameSummary'} for k,v in summaries.items()}},indent=2))
