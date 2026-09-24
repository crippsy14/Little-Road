"""Generate bounded geometric racing-line seeds; actual laps decide acceptance."""
import argparse
import hashlib
import json
from pathlib import Path
import numpy as np

p=argparse.ArgumentParser(description=__doc__)
p.add_argument('survey',type=Path);p.add_argument('definition',type=Path);p.add_argument('output',type=Path)
p.add_argument('--smooth-only',action='store_true',help='Bake a C2 Cartesian spline candidate without replacing v1 seeds.')
args=p.parse_args();data=json.loads(args.survey.read_text(encoding='utf-8-sig'));definition=json.loads(args.definition.read_text())
length=data['length'];source=data['survey'];s=np.array([r['distance'] for r in source]+[length]);points=np.array([[r['position']['x'],r['position']['z']] for r in source]+[[source[0]['position']['x'],source[0]['position']['z']]])
count=int(np.ceil(length/8));at=np.arange(count)*length/count
centre=np.column_stack([np.interp(at,s,points[:,i]) for i in range(2)])
forward=np.roll(centre,-1,axis=0)-np.roll(centre,1,axis=0);forward/=np.linalg.norm(forward,axis=1)[:,None]
normal=np.column_stack([forward[:,1],-forward[:,0]])
offset=np.zeros(count);bound=4.6 if args.smooth_only else 5.0
for iteration in range(24000):
    path=centre+normal*offset[:,None]
    second=np.roll(path,1,axis=0)-2*path+np.roll(path,-1,axis=0)
    gradient=2*np.sum(normal*(np.roll(second,1,axis=0)-2*second+np.roll(second,-1,axis=0)),axis=1)
    offset=np.clip(offset-.025*gradient,-bound,bound)

def interpolate(values,target):
    # Periodic shape-preserving Hermite interpolation avoids overshoot at the
    # corridor constraint and has continuous tangent at control knots.
    delta=np.roll(values,-1)-values;previous=np.roll(delta,1)
    slope=np.where(previous*delta>0,np.sign(delta)*np.minimum(np.abs((previous+delta)*.5),3*np.minimum(np.abs(previous),np.abs(delta))),0)
    coordinate=(target/length*len(values))%len(values);index=np.floor(coordinate).astype(int);t=coordinate-index;n=(index+1)%len(values)
    return (2*t**3-3*t*t+1)*values[index]+(t**3-2*t*t+t)*slope[index]+(-2*t**3+3*t*t)*values[n]+(t**3-t*t)*slope[n]

samples=int(np.ceil(length/2));positions=np.arange(samples)*length/samples
args.output.mkdir(parents=True,exist_ok=True)
variants=[] if args.smooth_only else [('centre',0,0),('mincurve',1,0),('half',.5,0),('early',1,6),('late',1,-6)]
for name,scale,shift in variants:
    values=interpolate(offset,positions+shift)*scale
    out={'schema':1,'id':'cedar-'+name+'-v1','trackId':definition['id'],'trackVersion':definition['version'],
         'trackHash':hashlib.sha256(args.definition.read_bytes()).hexdigest().upper(),'roadLength':length,
         'status':'candidate; not a validated ideal line','method':'bounded discrete bending-energy seed, periodic shape-preserving offset interpolation',
         'controlSpacing':length/count,'iterations':24000,'offsetLimit':bound,'phaseShift':shift,'scale':scale,
         'offsets':[round(float(v),6) for v in values]}
    (args.output/(out['id']+'.json')).write_text(json.dumps(out,indent=2),encoding='utf-8')
if args.smooth_only:
    # Interpolating a scalar offset on a merely C1 road reintroduces curvature
    # discontinuities. Keep the smoothed Cartesian path itself in schema 2.
    controls=centre+normal*offset[:,None]
    coordinate=positions/length*count;i=np.floor(coordinate).astype(int);t=(coordinate-i)[:,None]
    path=((1-t)**3*controls[(i-1)%count]+(3*t**3-6*t*t+4)*controls[i]+(-3*t**3+3*t*t+3*t+1)*controls[(i+1)%count]+t**3*controls[(i+2)%count])/6
    road=np.column_stack([np.interp(positions,s,points[:,j]) for j in range(2)])
    tangent=np.roll(road,-1,axis=0)-np.roll(road,1,axis=0);tangent/=np.linalg.norm(tangent,axis=1)[:,None]
    right=np.column_stack([tangent[:,1],-tangent[:,0]])
    offsets=np.sum((path-road)*right,axis=1)
    out={'schema':2,'id':'cedar-smooth-v2','trackId':definition['id'],'trackVersion':definition['version'],
         'trackHash':hashlib.sha256(args.definition.read_bytes()).hexdigest().upper(),'roadLength':length,
         'status':'candidate; not a validated ideal line','method':'bounded bending-energy control polygon with periodic C2 Cartesian cubic B-spline',
         'controlSpacing':length/count,'iterations':24000,'offsetLimit':bound,
         'offsets':[round(float(v),6) for v in offsets],
         'positions':[{'x':round(float(p[0]),6),'y':0,'z':round(float(p[1]),6)} for p in path]}
    (args.output/(out['id']+'.json')).write_text(json.dumps(out,indent=2),encoding='utf-8')
print(json.dumps({'candidates':1 if args.smooth_only else 5,'samples':samples,'controlPoints':count,'maxOffset':float(np.max(np.abs(offset))),'output':str(args.output)}))
