"""Recorded opening-straight field spread, ranks and commands; no inferred pass viability."""
import argparse,bisect,json,math
from pathlib import Path

def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def interpolate(rows,time):
    times=[s['time'] for s in rows]; j=bisect.bisect_right(times,time)
    a,b=rows[max(0,j-1)],rows[min(j,len(rows)-1)]
    u=(time-a['time'])/(b['time']-a['time']) if b['time']>a['time'] else 0
    return {k:a[k]+(b[k]-a[k])*u for k in ('progress','speed','roadOffset')}
def crossing(rows,gate):
    for a,b in zip(rows,rows[1:]):
        if a['progress']<gate<=b['progress']:
            return a['time']+(b['time']-a['time'])*(gate-a['progress'])/(b['progress']-a['progress'])
    raise ValueError(f'Missing crossing {gate}')
def frame_at(rows,time):
    cars=[dict(id=i,**interpolate(s,time)) for i,s in rows.items()]
    cars.sort(key=lambda s:-s['progress'])
    for i,c in enumerate(cars):
        c['rankRange']=[1+sum(x['progress']>c['progress']+.01 for x in cars),sum(x['progress']>=c['progress']-.01 for x in cars)]
        c['centreGapAhead']=None if i==0 else cars[i-1]['progress']-c['progress']
    return dict(time=time,spreadMetres=cars[0]['progress']-cars[-1]['progress'],cars=cars)
def analyze(root):
    asset=Path(__file__).resolve().parents[1]/'unity/Assets/Resources/Tracks/cedar-technical.json'
    knots=read(asset)['knots'];a,b=knots[:2]
    # This fixture declares a straight segment; refuse a changed curved layout.
    assert a['name']=='Home straight' and b['name']=='First braking zone'
    assert a['z']==b['z'] and a['dz']==b['dz']==0 and a['dx']==b['dx']==1
    gate=b['x']-a['x'];assert gate>0
    results={}
    for mode in ('off','on'):
        f=root/mode;t=read(f/'race.track.json');r=read(f/'race.json')
        assert r['carCount']==24 and r['trackVersion']=='cedar-v1' and r['requiredLaps']==1
        rows={i:[s for s in t['samples'] if s['id']==i and s['time']>=0] for i in range(24)}
        assert all(len(s)>1 for s in rows.values())
        gates={str(g):[dict(id=i,time=crossing(s,g)) for i,s in rows.items()] for g in (0,100,200,gate)}
        for values in gates.values():values.sort(key=lambda c:c['time'])
        first=min(x['time'] for x in gates[str(gate)]);last=max(x['time'] for x in gates[str(gate)])
        commonStart=max(s[0]['time'] for s in rows.values())
        frames=[frame_at(rows,commonStart)]+[frame_at(rows,float(x)) for x in range(2,math.floor(first)+1,2)]+[frame_at(rows,first)]
        endStates=frame_at(rows,first)
        equipment={c['id']:c['equipment'] for c in r['cars']}
        own=[s for s in rows[0] if s['progress']<=gate and s['speed']>2]
        contexts={s['time']:s for s in t['contextSamples'] if s['id']==0}
        reasons={}
        for s in own:
            reason=contexts.get(s['time'],{}).get('reason','unavailable');reasons[reason]=reasons.get(reason,0)+1
        results[mode]=dict(firstCarAtBrakingZone=first,lastCarAtBrakingZone=last,passageWindow=last-first,collectiveArrivalSeconds={g:sum(c['time'] for c in entries) for g,entries in gates.items()},fieldAtFirstArrival=endStates,frames=frames,gateCrossings=gates,
            perCar=[dict(id=i,equipment=equipment[i],gateTime=crossing(s,gate),gateRankRange=[1+sum(x['time']<crossing(s,gate)-.001 for x in gates[str(gate)]),sum(x['time']<=crossing(s,gate)+.001 for x in gates[str(gate)])]) for i,s in rows.items()],
            previewMovingSamples=len(own),previewPedalDutyPercent={k:100*sum(test(s['pedal']) for s in own)/len(own) for k,test in [('powered',lambda p:p>.05),('coasting',lambda p:-.01<=p<=.05),('braking',lambda p:p<-.01)]},previewContextReasonSamples=reasons)
    out=dict(scope='Opening straight only, unchanged 24-car Cedar grid. Spatial field spread is a diagnostic, not a target to minimise: a stretched or compact field can both race well.',brakingZoneRouteMetres=gate,boundaryBasis='Authored start of First braking zone, 280 m from the start line; initial grid is behind the line. This is geometry, not each car\'s actual brake onset.',precision='Linearly interpolated 10 Hz car-centre progress/offset/speed; rank ties retained within 1 cm spatially and 1 ms at gates. Centre gaps ignore lateral separation and are not clearance or proof that passing is possible.',results=results)
    (root/'opening-straight-analysis.json').write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8')
    lines=['# Opening straight: reference versus dynamic','',out['scope'],'',out['boundaryBasis'],'',out['precision'],'','| Measure | Reference | Dynamic |','|---|---:|---:|']
    a,b=results['off'],results['on']
    for label,va,vb in [('First car at braking zone (s)',a['firstCarAtBrakingZone'],b['firstCarAtBrakingZone']),('Last car at braking zone (s)',a['lastCarAtBrakingZone'],b['lastCarAtBrakingZone']),('All-car passage window (s)',a['passageWindow'],b['passageWindow']),('First-to-last distance when leader arrives (m)',a['fieldAtFirstArrival']['spreadMetres'],b['fieldAtFirstArrival']['spreadMetres']),('Preview arrival (s)',a['perCar'][0]['gateTime'],b['perCar'][0]['gateTime'])]:lines.append(f'| {label} | {va:.3f} | {vb:.3f} |')
    lines+=['','## Per-car arrival at 280 m','','| Car | Equipment | Reference time / rank | Dynamic time / rank |','|---|---|---:|---:|']
    for x,y in zip(a['perCar'],b['perCar']):lines.append(f"| {x['id']} | {x['equipment']} | {x['gateTime']:.3f} / {x['gateRankRange']} | {y['gateTime']:.3f} / {y['gateRankRange']} |")
    lines+=['','Full position, lateral offset, speed, gaps and tied ranks at each snapshot are stored in the JSON. Pedal/context labels are observational and cannot establish why an alternative was rejected.','']
    (root/'opening-straight-analysis.md').write_text('\n'.join(lines),encoding='utf-8');return out

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('root',type=Path);args=p.parse_args();o=analyze(args.root)
    print((args.root/'opening-straight-analysis.md').read_text(encoding='utf-8').split('## Per-car')[0])
