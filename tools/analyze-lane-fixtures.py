"""Isolate full-lap local corridor tracking before traffic tactics."""
import json, sys, math
from pathlib import Path
root=Path(sys.argv[1]).resolve()
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
manifest=read(root/'manifest.json');checks=[];results=[]
def check(case,name,value):checks.append(dict(case=case,name=name,passed=bool(value)))
check('suite','All six declared runs completed',manifest['complete'] and len(manifest['runs'])==6)
for run in manifest['runs']:
    folder=Path(run['report']).parent;race=read(folder/'race.json');track=read(folder/'race.track.json')
    side={'left':-4,'middle':0,'right':4}[run['side']];car=race['cars'][0]
    samples=[s for s in track['samples'] if s['progress']>=0]
    errors=sorted(abs(s['roadOffset']-side) for s in samples)
    result=dict(case=folder.name,finished=car['finished'],time=car['time'],progress=car['progress'],
        meanError=sum(errors)/max(1,len(errors)),p95Error=errors[math.floor((len(errors)-1)*.95)] if errors else None,
        maxError=max(errors,default=None),contacts=car['contacts'],physicalRecoveries=car['physicalRecoveries'],
        resets=car['recoveries'],missedGates=car['missedGates'],outsideBodySamples=track['results'][0]['outsideBodySamples'])
    results.append(result)
    check(folder.name,'Normal process exit',run['exitCode']==0)
    check(folder.name,'Full valid lap completed',car['finished'] and car['laps']==1)
    check(folder.name,'No contacts, recovery or missed gates',all(result[x]==0 for x in ['contacts','physicalRecoveries','resets','missedGates']))
    check(folder.name,'Body remains on road',result['outsideBodySamples']==0)
    check(folder.name,'Stable corridor tracking',bool(errors) and result['meanError']<.6 and result['p95Error']<1 and result['maxError']<1.8)
out=dict(scope='Fixed road corridors with street/club equipment; does not validate traffic choice or passing.',passed=sum(c['passed'] for c in checks),total=len(checks),results=results,checks=checks)
(root/'analysis.json').write_text(json.dumps(out,indent=2)+'\n')
print(json.dumps({k:v for k,v in out.items() if k!='checks'},indent=2))
raise SystemExit(0 if out['passed']==out['total'] else 1)
