import json,collections,sys
from pathlib import Path
root=Path(sys.argv[1]) if len(sys.argv)>1 else Path(__file__).resolve().parent/'field-25-road-feasibility'
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
race=read(root/'race.json');track=read(root/'race.track.json')
finishes={c['id']:c['time'] if c['finished'] else float('inf') for c in race['cars']}
poses={(s['id'],s['time']):s for s in track['samples']}
counts=collections.Counter();regions=collections.Counter();examples=[]
for s in track['contextSamples']:
    if not s['roadUnresolved'] or not 0<=s['time']<=finishes[s['id']]:continue
    p=poses[(s['id'],s['time'])];regions[p['region']]+=1
    if s['left']>s['right']:kind='emptyReachableInterval'
    elif not s['left']<=p['roadOffset']<=s['right']:kind='currentCentreExcludedByReachableInterval'
    else:kind='currentCentreInsideButNoAcceptedCandidate'
    counts[kind]+=1
    if len(examples)<12:examples.append({'id':s['id'],'time':s['time'],'kind':kind,'actualOffset':p['roadOffset'],
        'left':s['left'],'right':s['right'],'roadRejected':s['roadRejected'],'region':p['region']})
out={'scope':'Classification of failed no-unresolved gate; these are current interval diagnostics, not proof that all proposed manoeuvres were physically safe.',
    'counts':dict(counts),'regions':dict(regions),'examples':examples,
    'historicalSourceFindingIn25':'The old side reservation activates for abs(route gap) < summed half-lengths + 3m, including same-corridor cars in adjacent grid rows (5.5m spacing). An ahead car then forces an arbitrary left/right centre bound although following is physically available. Prototype26 changes this classification only; residual failures require their own diagnosis.'}
(root/'road-diagnosis.json').write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8')
print(json.dumps(out,indent=2))
