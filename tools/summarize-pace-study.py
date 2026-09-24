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
fmt=lambda v:'-' if v is None else (f'{v:.3f}' if isinstance(v,float) else str(v))
print(f"{'run':28} fin  collective  car0(t/pl)   contacts maxrel assist phys(s) missed")
for k,v in rows.items():
    print(f"{k:28} {v['fin']:2}/{v['n']} {fmt(v['coll']):>10}  {fmt(v['c0']):>7}/{v['c0p']:<2}  {v['contacts']:4} {v['maxrel']:6.2f} {v['assist']:4} {v['phys']:2}({v['physs']:.1f}) {v['missed']}")
# paired deltas
arms=sorted({k.rsplit('-',1)[1] for k in rows}-{'off'})
for arm in arms:
    for track in ('cedar','bayview'):
        ds=[];c0=[]
        for k,v in rows.items():
            if k.startswith(track) and k.endswith('-off'):
                o=rows.get(k[:-3]+arm)
                if o and v['coll'] and o['coll']:ds.append(o['coll']-v['coll'])
                if o and v['c0'] and o['c0']:c0.append(o['c0']-v['c0'])
        if ds:print(f"{arm} {track}: collective delta per seed {[round(x,2) for x in ds]} mean {st.mean(ds):+.2f}s | car0 {[round(x,2) for x in c0]}")
