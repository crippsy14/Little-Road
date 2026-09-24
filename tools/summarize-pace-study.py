import json,sys,glob,os,statistics as st
root=sys.argv[1];rows={}
for f in sorted(glob.glob(os.path.join(root,'*','race.json'))):
    name=os.path.basename(os.path.dirname(f));r=json.load(open(f,encoding='utf-8-sig'))
    cars=r['cars'];fin=sum(c['finished'] for c in cars)
    coll=sum(c['time'] for c in cars) if fin==len(cars) else None
    c0=cars[0];imp=r.get('impacts',[])
    ar=r['assistedRecoveries'];ar=len(ar) if isinstance(ar,list) else ar
    rows[name]=dict(fin=fin,n=len(cars),coll=coll,c0=c0['time'] if c0['finished'] else None,c0p=c0['place'],contacts=len(imp),
      maxrel=max([i['relativeSpeed'] for i in imp],default=0),assist=ar,phys=sum(c['physicalRecoveries'] for c in cars),
      physs=sum(c['physicalRecoverySeconds'] for c in cars),missed=sum(c['missedGates'] for c in cars))
    # Opening: field-mean time to gates 1 and 2 (Cedar 172/345 m, Bayview 57/114 m).
    for g in (1,2):
        ts=[x['time'] for x in r.get('checkpointPassages',[]) if x['gate']==g]
        rows[name][f'g{g}']=st.mean(ts) if len(ts)==len(cars) else None
    # Racecraft (builds from 118 on): plan reversals per car-minute and pass commitments.
    minutes=sum(c['time'] if c['finished'] else r.get('simSeconds',0) for c in cars)/60
    rows[name]['rev']=sum(c.get('planReversals',0) for c in cars)/minutes if 'planReversals' in c0 and minutes>0 else None
    rows[name]['commits']=(sum(c.get('passCommits',0) for c in cars),sum(c.get('passCompleted',0) for c in cars),sum(c.get('passAborted',0) for c in cars)) if 'passCommits' in c0 else None
fmt=lambda v:'-' if v is None else (f'{v:.3f}' if isinstance(v,float) else str(v))
print(f"{'run':28} fin  collective  car0(t/pl)   contacts maxrel assist phys(s) missed  gate1   gate2  rev/min  commits(done/abort)")
for k,v in rows.items():
    print(f"{k:28} {v['fin']:2}/{v['n']} {fmt(v['coll']):>10}  {fmt(v['c0']):>7}/{v['c0p']:<2}  {v['contacts']:4} {v['maxrel']:6.2f} {v['assist']:4} {v['phys']:2}({v['physs']:.1f}) {v['missed']:6} {fmt(v['g1']):>7} {fmt(v['g2']):>7}  {('-' if v['rev'] is None else f"{v['rev']:.1f}"):>6}  {'-' if v['commits'] is None else '%d(%d/%d)'%v['commits']}")
# paired deltas
arms=sorted({k.rsplit('-',1)[1] for k in rows}-{'off'})
for arm in arms:
    for track in ('cedar','bayview'):
        ds=[];c0=[];g1=[];g2=[]
        for k,v in rows.items():
            if k.startswith(track) and k.endswith('-off'):
                o=rows.get(k[:-3]+arm)
                if o and v['coll'] and o['coll']:ds.append(o['coll']-v['coll'])
                if o and v['c0'] and o['c0']:c0.append(o['c0']-v['c0'])
                if o and v['g1'] and o['g1']:g1.append(o['g1']-v['g1'])
                if o and v['g2'] and o['g2']:g2.append(o['g2']-v['g2'])
        if ds:print(f"{arm} {track}: collective delta per seed {[round(x,2) for x in ds]} mean {st.mean(ds):+.2f}s | car0 {[round(x,2) for x in c0]}")
        if g1:print(f"   opening (field-mean) gate1 {[round(x,2) for x in g1]} mean {st.mean(g1):+.2f}s | gate2 {[round(x,2) for x in g2]} mean {st.mean(g2):+.2f}s")
