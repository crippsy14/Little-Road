import json
from pathlib import Path
root=Path('review/unity-35/driver-braking-43')
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
m=read(root/'manifest.json');rows=[]
for run in m['runs']:
 f=Path(run['folder']);r=read(f/'race.json');t=read(f/'race.track.json');c=r['cars'][0]
 gates=[e['time'] for e in r['checkpointPassages'] if e['id']==0 and e['gate']==0 and e['requiredProgress']>0]
 rows.append(dict(name=run['name'],finished=r['finished'],cars=r['carCount'],totalTime=c['time'],flyingLaps=[b-a for a,b in zip(gates,gates[1:])],contacts=sum(x['contacts'] for x in r['cars']),outside=sum(x['outsideBodySamples'] for x in t['results']),recoveries=sum(x['recoveries']+x['physicalRecoveries'] for x in r['cars']),missedGates=sum(x['missedGates'] for x in r['cars']),planning=t['driverPlanning']))
(root/'summary.json').write_text(json.dumps(dict(complete=m['complete'],scope='Measured results only; remaining ability/straight-power/variation acceptance not claimed.',results=rows),indent=2)+'\n')
print(json.dumps(rows,indent=2))
