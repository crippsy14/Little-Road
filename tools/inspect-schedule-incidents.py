"""Inspect sampled responses near recorded108 contacts; never relabel failed gates."""
import argparse,json
from pathlib import Path

p=argparse.ArgumentParser();p.add_argument('study',type=Path);p.add_argument('output',type=Path);a=p.parse_args()
read=lambda f:json.loads(f.read_text(encoding='utf-8-sig'))
cases=[]
for folder in sorted(a.study.glob('*-after')):
    race=read(folder/'race.json');track=read(folder/'race.track.json')
    events=[]
    for event in race['impacts']:
        rows=[s for s in track['samples'] if s['id']==event['id'] and event['time']-.12<=s['time']<=event['time']+.31]
        events.append(dict(event=event,nearbySamples=[{k:s[k] for k in ('time','speed','pedal','recoveryState','contactDeltaV','contactImpulse','contactSurface')} for s in rows]))
    recovery=[]
    for car in race['cars']:
        rows=[s for s in track['samples'] if s['id']==car['id'] and s['recoveryState'] not in ('Racing','Disabled')]
        if rows:
            recovery.append(dict(id=car['id'],sampleCount=len(rows),first=rows[0],last=rows[-1],finish=car['time']))
    cases.append(dict(name=folder.name,events=events,recoveries=recovery))
out=dict(scope='Existing10Hz samples around each contact, including the maximum collision impulse/mass observed since the preceding sample. That value may belong to another contact/surface; it is not an event-specific signed speed loss. Do not add paired per-car events or call relative velocity an impact impulse. Recovery samples are retained for diagnosis; this does not change108 gates or establish causality.',cases=cases)
a.output.parent.mkdir(parents=True,exist_ok=True)
a.output.write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8')
print(json.dumps([dict(name=c['name'],events=len(c['events']),recoveringCars=[r['id'] for r in c['recoveries']],
    peakNearbySampledDeltaV=max((s['contactDeltaV'] for e in c['events'] for s in e['nearbySamples']),default=0)) for c in cases],indent=2))
