"""Diagnose captured rejection states without treating opponent extrapolation as truth."""
import argparse
from collections import Counter
import json
import math
from pathlib import Path

p=argparse.ArgumentParser();p.add_argument('folder',type=Path);args=p.parse_args()
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
track=read(args.folder/'race.track.json')
entries=[e for audit in track['failedPlanAudits'] for e in audit['entries'] if not e['finished']]
v=lambda d:(d['x'],d['z'])
dot=lambda a,b:sum(x*y for x,y in zip(a,b))
add=lambda a,b:tuple(x+y for x,y in zip(a,b))
sub=lambda a,b:tuple(x-y for x,y in zip(a,b))
scale=lambda a,s:tuple(x*s for x in a)
def axes(car):
    f=v(car['forward']);length=math.hypot(*f);f=scale(f,1/length)
    return f,(f[1],-f[0])
def centre(car):
    f,r=axes(car);c=car['colliderCentre']
    return add(v(car['position']),add(scale(f,c['z']),scale(r,c['x'])))
def separation(a,b):
    af,ar=axes(a);bf,br=axes(b);delta=sub(centre(b),centre(a))
    radius=lambda car,f,r,axis:abs(dot(f,axis))*car['halfLength']+abs(dot(r,axis))*car['halfWidth']
    return max(abs(dot(delta,axis))-radius(a,af,ar,axis)-radius(b,bf,br,axis) for axis in (af,ar,bf,br))

rows=[];reasons=Counter();instant=0
for e in sorted(entries,key=lambda e:e['timestamp']):
    own=e['self'];contributors=[]
    for other in e['traffic']:
        if other['id']==own['id']:continue
        width=own['halfWidth']+other['halfWidth']+.4
        sides=[]
        if abs(other['offset']+width-e['left'])<.0001:sides.append('left')
        if abs(other['offset']-width-e['right'])<.0001:sides.append('right')
        if sides:contributors.append((other,sides))
    for c in e['candidates']:
        reasons[c['reason']]+=1
        if c['reason']=='corridor' and c['afterSeconds']==0:instant+=1
        projected={**own,'position':c['position'],'forward':c['forward']}
        neighbours=[]
        for other,sides in contributors:
            # Explicitly limited diagnostic: keep measured velocity and heading
            # constant. This is not an accepted opponent-motion predictor.
            position={axis:other['position'][axis]+other['velocity'][axis]*c['afterSeconds'] for axis in ('x','y','z')}
            moving={**other,'position':position}
            neighbours.append(dict(id=other['id'],bounds=sides,currentSeparatingAxisGap=separation(own,other),
                predictedOwnVsFrozenOtherGap=separation(projected,other),predictedOwnVsConstantVelocityOtherGap=separation(projected,moving)))
        rows.append(dict(id=own['id'],timestamp=e['timestamp'],reason=c['reason'],afterSeconds=c['afterSeconds'],
                         initialOffset=own['offset'],left=e['left'],right=e['right'],rejectedOffset=c['offset'],
                         destination=c['path']['endOffset'],contributors=neighbours))
out=dict(scope='Exact failed-plan snapshot/rejection capture. Box separation uses current collider geometry. Positive separating-axis gap means disjoint planar boxes; not Euclidean clearance. Frozen and constant-velocity opponent comparisons are illustrative counterfactuals, not future-state truth or acceptance of a replacement constraint.',
         failedPlans=len(entries),dropped=sum(a['dropped'] for a in track['failedPlanAudits']),candidateReasons=dict(reasons),
         actualStartOutsideInterval=sum(not e['left']<=e['self']['offset']<=e['right'] for e in entries),instantCorridorRejections=instant,rows=rows)
(args.folder/'failed-plan-analysis.json').write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8')
print(json.dumps({k:v for k,v in out.items() if k!='rows'},indent=2))
for row in rows[::6]:print(json.dumps(row))
