"""Matched Bayview driver-package comparison; incomplete fields never get a total."""
import json
from pathlib import Path
from runtime_log_errors import runtime_error_lines
ROOT=Path(__file__).resolve().parents[1]
root=ROOT/'review/unity-35/bayview-transfer-84'
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))

def summarize(folder):
    r=read(folder/'race.json');t=read(folder/'race.track.json')
    start={c['id']:next((g['time'] for g in r['checkpointPassages'] if g['id']==c['id'] and abs(g['requiredProgress'])<.01),None) for c in r['cars']}
    complete=all(c['finished'] for c in r['cars'])
    rows=[dict(id=c['id'],equipment=c['equipment'],finished=c['finished'],place=c['place'],finishSeconds=c['time'] if c['finished'] else None,
               startCrossing=start[c['id']],lineToLineSeconds=c['time']-start[c['id']] if c['finished'] and start[c['id']] is not None else None,
               physicalRecoveries=c['physicalRecoveries'],assistedRecoveries=c['recoveries']) for c in r['cars']]
    return dict(driver=r['driverPolicy'],track=r['trackId'],trackLength=r['trackLength'],finished=r['finished'],entrants=r['carCount'],complete=complete,
                collectiveSeconds=sum(c['time'] for c in r['cars']) if complete else None,
                lastFinisherSeconds=max(c['time'] for c in r['cars']) if complete else None,
                preview=rows[0],cars=rows,contactParticipantRecords=len(r['impacts']),peakImpact=max((c['relativeSpeed'] for c in r['impacts']),default=0),
                bodyOutsideSamples=sum(c['outsideBodySamples'] for c in t['results']),maxBodyOffset=max(c['maxBodyOffset'] for c in t['results']),
                physicalRecoveries=sum(c['physicalRecoveries'] for c in r['cars']),assistedRecoveries=len(r['assistedRecoveries']),
                runtimeErrors=runtime_error_lines(folder),exitCode=read(folder/'exit.json')['exitCode'])

out=dict(scope='One-seed full driver-package transfer. Standing finish times include rear-grid launch; line-to-line times reported separately. Latest includes light drafting and physical recovery. No rendered-performance or universal balance claim.',comparisons={})
for case in ('solo','field'):
    folders=[root/(case+'-'+mode) for mode in ('legacy','latest')]
    a,b=[summarize(f) for f in folders]
    ar,br=[read(f/'race.json') for f in folders]
    grids=[read(f/'starting-grid.json')['entries'] for f in folders]
    checks={key:ar[key]==br[key] for key in ('seed','equipment','carCount','requiredLaps','gridMode','trackLength','trackWidth','trackId','trackVersion','physicsStepSeconds','collisionMode')}
    checks['sameGridBodiesAndTuning']=grids[0]==grids[1]
    checks['sameBuild']=read(folders[0]/'launch.json')['assemblyHash']==read(folders[1]/'launch.json')['assemblyHash']
    checks['cleanExit']=all(x['exitCode']==0 and not x['runtimeErrors'] for x in (a,b))
    complete=a['complete'] and b['complete']
    out['comparisons'][case]=dict(setupChecks=checks,validMatchedSetup=all(checks.values()),legacy=a,latest=b,
        collectiveGainSeconds=a['collectiveSeconds']-b['collectiveSeconds'] if complete else None,
        previewGainSeconds=a['preview']['finishSeconds']-b['preview']['finishSeconds'] if a['preview']['finished'] and b['preview']['finished'] else None)
(root/'comparison.json').write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8')
lines=['# Bayview legacy/latest comparison84','',out['scope'],'','| Test | Original | Latest | Reduction |','|---|---:|---:|---:|']
fmt=lambda v:'incomplete' if v is None else f'{v:.3f}s'
for case,c in out['comparisons'].items():
    a,b=c['legacy'],c['latest']
    lines.append(f"| {case}: preview finish | {fmt(a['preview']['finishSeconds'])} | {fmt(b['preview']['finishSeconds'])} | {fmt(c['previewGainSeconds'])} |")
    if case=='field':lines.append(f"| field: all24 combined | {fmt(a['collectiveSeconds'])} | {fmt(b['collectiveSeconds'])} | {fmt(c['collectiveGainSeconds'])} |")
    print(case,json.dumps(dict(valid=c['validMatchedSetup'],finishes=[a['finished'],b['finished']],preview=[a['preview']['finishSeconds'],b['preview']['finishSeconds']],collective=[a['collectiveSeconds'],b['collectiveSeconds']],outside=[a['bodyOutsideSamples'],b['bodyOutsideSamples']],contacts=[a['contactParticipantRecords'],b['contactParticipantRecords']],peak=[a['peakImpact'],b['peakImpact']],recoveries=[a['physicalRecoveries']+a['assistedRecoveries'],b['physicalRecoveries']+b['assistedRecoveries']])) )
for case,c in out['comparisons'].items():
    lines+=['',f"## {case}",'',f"Matched setup checks: {c['setupChecks']}."]
    for mode in ('legacy','latest'):
        r=c[mode];lines.append(f"- {mode}: {r['finished']}/{r['entrants']} finish; preview rank{r['preview']['place']}; preview start-line-to-finish {fmt(r['preview']['lineToLineSeconds'])}; contacts{r['contactParticipantRecords']} participant records, peak{r['peakImpact']:.3f}m/s; body-edge samples{r['bodyOutsideSamples']}, max{r['maxBodyOffset']:.3f}m; physical recovery{r['physicalRecoveries']}, assisted{r['assistedRecoveries']}.")
(root/'comparison.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
