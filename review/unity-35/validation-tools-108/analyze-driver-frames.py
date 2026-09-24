"""Summarise full driver CPU/allocation observations without claiming batch rendering acceptance."""
import argparse,json,math
from pathlib import Path

parser=argparse.ArgumentParser()
parser.add_argument('folder',type=Path)
args=parser.parse_args()
root=args.folder
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
evidence=read(root/'race.driver-frames.json')
launch=read(root/'launch.json')
# The first observation can contain loading/setup. All remaining countdown and
# launch frames are included, rather than hiding the busy start behind warm-up.
rows=evidence['frames'][1:]
if not rows:raise SystemExit('No post-loading frames recorded.')
def distribution(values):
    a=sorted(values)
    return dict(mean=sum(a)/len(a),p50=a[math.ceil(.5*len(a))-1],p95=a[math.ceil(.95*len(a))-1],p99=a[math.ceil(.99*len(a))-1],maximum=a[-1])
real_time=bool(launch.get('visible')) and not evidence['batchMode'] and evidence['playbackRate']==1
cpu=distribution([r['decisionMs']+r['snapshotMs'] for r in rows])
frames=distribution([r['frameMs'] for r in rows])
warm=[r for r in rows if r['simTime']>=5]
allocating=[r for r in warm if r['decisionBytes']+r['snapshotBytes']+r.get('forecastWorkerBytes',0)>0]
# Real wall-clock windows, not simulated seconds (accelerated runs differ).
start=0;total=0;worst=None
for end,row in enumerate(rows):
    total+=row['frameMs']
    while start<end and row['realTime']-rows[start]['realTime']>1:
        total-=rows[start]['frameMs'];start+=1
    span=row['realTime']-rows[start]['realTime']
    if span>=.95:
        mean=total/(end-start+1)
        if worst is None or mean>worst['meanFrameMs']:
            worst=dict(startRealTime=rows[start]['realTime'],endRealTime=row['realTime'],startSimTime=rows[start]['simTime'],meanFrameMs=mean,frames=end-start+1)
sizes=sorted({(r.get('width',0),r.get('height',0)) for r in rows})
checks={
    'allFramesAt1920x1080':sizes==[(1920,1080)] if sizes!=[(0,0)] else None,
    'completeCapture':evidence['droppedFrames']==0,
    'allAiP95Within3ms':cpu['p95']<=3 if real_time else None,
    'frameP95Within16_7ms':frames['p95']<=16.7 if real_time else None,
    'frameP99Within33_3ms':frames['p99']<=33.3 if real_time else None,
    'noObservedDriverAllocationAfter5s':not allocating if warm else None,
}
out=dict(scope=evidence['scope'],realTimeRenderingEvidence=real_time,omittedInitialLoadingFrames=1,frames=len(rows),
    observedFrameResolutions=sizes,settings={k:evidence[k] for k in ['cpu','gpu','width','height','frameCap','vsync','playbackRate','batchMode','developmentBuild','droppedFrames']},
    allAiMsPerRenderedFrame=cpu,observedFrameMs=frames,worstOneSecondWindow=worst,
    totalDecisionCalls=sum(r['decisionCalls'] for r in rows),totalSnapshotCalls=sum(r['snapshotCalls'] for r in rows),
    driverBytesAfter5s=sum(r['decisionBytes']+r['snapshotBytes'] for r in warm),
    forecastWorkerBytesAfter5s=sum(r.get('forecastWorkerBytes',0) for r in warm),
    forecastJobs=sum(r.get('forecastJobs',0) for r in rows),forecastBatches=sum(r.get('forecastBatches',0) for r in rows),
    forecastBatchWallMs=distribution([r.get('forecastBatchMs',0) for r in rows]),
    summedForecastWorkerElapsedMs=sum(r.get('forecastWorkerMs',0) for r in rows),allocatingFramesAfter5s=len(allocating),checks=checks)
(root/'driver-frame-analysis.json').write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8')
print(json.dumps(out,indent=2))
