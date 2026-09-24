"""Keep whole-AI cost, frame tails and behavioural parity separate."""
import json
from pathlib import Path
root=Path(__file__).resolve().parents[1]/'review/unity-35'
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
field=root/'dynamic-field-85'
hidden=read(field/'on/motion-parity.json')
render=read(field/'view-on/motion-parity.json')
a,b=[read(field/m/'driver-frame-analysis.json') for m in ('view-off','view-on')]
bay={case:read(root/'bayview-transfer-85'/(case+'-latest')/'motion-parity.json') for case in ('solo','field')}
parities={mode:read(field/mode/'motion-parity.json')['passed'] for mode in ('off','on','view-off','view-on')}
parities.update({f'bayview-{case}':r['passed'] for case,r in bay.items()})
out=dict(parities=parities,hidden=hidden,rendered=render,bayview=bay,
    frameP95Ms=[a['observedFrameMs']['p95'],b['observedFrameMs']['p95']],
    frameP99Ms=[a['observedFrameMs']['p99'],b['observedFrameMs']['p99']],
    aiFrameP95Ms=[a['allAiMsPerRenderedFrame']['p95'],b['allAiMsPerRenderedFrame']['p95']],
    aiFrameP99Ms=[a['allAiMsPerRenderedFrame']['p99'],b['allAiMsPerRenderedFrame']['p99']],
    fixed1080p=all(x['checks']['allFramesAt1920x1080'] and x['realTimeRenderingEvidence'] for x in (a,b)),
    warmAllocations={m:read(field/m/'driver-frame-analysis.json')['driverBytesAfter5s']+read(field/m/'driver-frame-analysis.json')['forecastWorkerBytesAfter5s'] for m in ('on','view-off','view-on')},
    renderedChecks=b['checks'])
out['retainForNextMilestone']=all(parities.values()) and out['fixed1080p'] and not any(out['warmAllocations'].values()) and render['allAiWallCostReductionPercent']>0 and out['frameP99Ms'][1]<out['frameP99Ms'][0]
(field/'performance-summary.json').write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8')
print(json.dumps({k:v for k,v in out.items() if k not in ('hidden','rendered','bayview')},indent=2))
