"""Class-sort overtaking fixture: python tools/analyze-class-sort.py <run folder or race.json> [...]

Grid (tools/run-class-sort.ps1, --traffic-case class-sort), rows front to back, 1 = fastest:
    3-3-3 / 2-2-2 / 3-3-3 / 1-1-1 / (empty) / 2-2-2
Expected: the field sorts itself by class (all 1s, then 2s, then 3s).

Per run:
  - class order on the grid, at the end of each lap, and at the finish (e.g. 111 222222 333333);
  - inversions: pairs where a slower class is ahead of a faster one (grid 36 -> 0 when sorted);
    'sorted from' is the first checkpoint after which the running order never has one again;
  - passes between consecutive checkpoints, split into faster-class-past-slower (expected),
    same-class and slower-past-faster (should be rare: a quick car went off, stalled or was blocked);
  - PASS when every car finishes and the finishing order has no inversions.
"""
import json,os,sys,itertools

CLASS={'club':1,'street':2,'stock':3}

def load(path):
    if os.path.isdir(path):path=os.path.join(path,'race.json')
    return path,json.load(open(path))

def inversions(order,cls):return sum(1 for a,b in itertools.combinations(order,2) if cls[a]>cls[b])

def text(order,cls):
    s=''.join(str(cls[i]) for i in order);out=[];prev=None
    for ch in s:
        if prev is not None and ch!=prev:out.append(' ')
        out.append(ch);prev=ch
    return ''.join(out)

def analyse(path,data):
    cars={c['id']:c for c in data['cars']};cls={i:CLASS.get(c['equipment'],9) for i,c in cars.items()}
    roster=path.replace('race.json','race.roster.json');grid=None
    if os.path.exists(roster):
        entries=json.load(open(roster))['entries'];grid=[e['id'] for e in sorted(entries,key=lambda e:e['slot'])]
    passages={}
    for p in data.get('checkpointPassages') or []:passages.setdefault(round(p['requiredProgress'],1),{})[p['id']]=p['time']
    gates=sorted(passages)
    orders=[]
    for g in gates:
        seen=passages[g]
        if len(seen)<len(cars):continue   # a checkpoint not every car reached (race ended)
        orders.append((g,sorted(seen,key=lambda i:seen[i])))
    fin=[c for c in data['cars'] if c['finished']]
    final=[c['id'] for c in sorted(fin,key=lambda c:c['place'])]+[c['id'] for c in sorted((c for c in data['cars'] if not c['finished']),key=lambda c:-c['progress'])]
    good=same=bad=0
    for (_,a),(_,b) in zip(orders,orders[1:]):
        pa={i:k for k,i in enumerate(a)};pb={i:k for k,i in enumerate(b)}
        for x,y in itertools.combinations(a,2):
            if (pa[x]<pa[y])!=(pb[x]<pb[y]):
                passer,passed=(y,x) if pa[x]<pa[y] else (x,y)
                if cls[passer]<cls[passed]:good+=1
                elif cls[passer]==cls[passed]:same+=1
                else:bad+=1
    sortedFrom=None
    for k in range(len(orders)):
        if all(inversions(o,cls)==0 for _,o in orders[k:]):sortedFrom=orders[k][0];break
    length=data['trackLength'];laps=data['requiredLaps']
    lapEnds=[(g,o) for g,o in orders if g>0 and abs(g/length-round(g/length))<1e-3]
    best={}
    for i,c in cars.items():best.setdefault(cls[i],[]).append(c['bestLap'])
    return dict(name=os.path.basename(os.path.dirname(path)),track=data['trackId'],seed=data['seed'],cls=cls,grid=grid,orders=orders,lapEnds=lapEnds,final=final,
        finished=len(fin),cars=len(cars),good=good,same=same,bad=bad,sortedFrom=sortedFrom,length=length,
        best={k:min(v) for k,v in best.items()},resets=sum(c['recoveries'] for c in data['cars']),recoveries=sum(c.get('physicalRecoveries',0) for c in data['cars']),
        contacts=sum(c['contacts'] for c in data['cars'])//2,winner=min((c['time'] for c in fin),default=None))

def main():
    results=[]
    for r in sys.argv[1:]:
        path,data=load(r)
        if not data.get('checkpointPassages'):print(f'{r}: no checkpoint passages');continue
        a=analyse(path,data);results.append(a);cls=a['cls']
        inv=inversions(a['final'],cls);ok=a['finished']==a['cars'] and inv==0
        print(f"{a['name']}: {'PASS' if ok else 'FAIL'} | finished {a['finished']}/{a['cars']} | final inversions {inv} | "
              f"sorted from {('%.0f m (lap %.2f)'%(a['sortedFrom'],a['sortedFrom']/a['length'])) if a['sortedFrom'] is not None else 'never'} | "
              f"passes: faster-past-slower {a['good']}, same class {a['same']}, slower-past-faster {a['bad']} | contacts {a['contacts']} | physical recoveries {a['recoveries']} | assisted resets {a['resets']}")
        if a['grid']:print(f"    grid      {text(a['grid'],cls):<22} inversions {inversions(a['grid'],cls)}")
        for g,o in a['lapEnds']:print(f"    lap {round(g/a['length'])}     {text(o,cls):<22} inversions {inversions(o,cls)}")
        print(f"    finish    {text(a['final'],cls):<22} inversions {inv}")
        print('    best lap by class: '+', '.join(f'{k}: {v:.2f}s' for k,v in sorted(a['best'].items())))
    if len(results)>1:
        passed=sum(1 for a in results if a['finished']==a['cars'] and inversions(a['final'],a['cls'])==0)
        print(f"SUMMARY {passed}/{len(results)} runs sorted by class | slower-past-faster passes {sum(a['bad'] for a in results)} | contacts {sum(a['contacts'] for a in results)}")

if __name__=='__main__':main()
