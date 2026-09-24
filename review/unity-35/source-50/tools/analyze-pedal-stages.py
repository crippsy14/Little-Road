"""Attribute actual staged commands; intent strings are not causal evidence."""
import json
from pathlib import Path
from collections import Counter, defaultdict
import sys

root=Path(sys.argv[1]);read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
track=read(root/'race.track.json')
samples={(s['id'],s['time']):s for s in track['samples']}
counts=Counter();stages=defaultdict(Counter);heavy=[];previous={};abrupt=[]
for p in track['pedalSamples']:
    s=samples[(p['id'],p['time'])]
    if s['inputOwner']!='AI' or s['speed']<2 or s['recoveryState'] not in ('Racing','Disabled'):continue
    counts['eligible']+=1
    if abs(p['decisionTimestamp']-p['sampleTimestamp'])>1e-6:
        counts['staleDecision']+=1;continue
    if abs(p['applied']-s['pedal'])>1e-6:
        counts['appliedMismatch']+=1;continue
    counts['matched']+=1
    values=[p[k] for k in ('follower','leader','scenery','applied')]
    if any(b>a+1e-6 for a,b in zip(values,values[1:])):counts['nonMonotone']+=1
    source='follower'
    for before,after,name in (('follower','leader','moving leader'),('leader','scenery','scenery'),('scenery','applied','immediate safety')):
        if p[after]<p[before]-1e-6:source=name
    if p['applied']<-.01:
        grade='heavy' if p['applied']<-.5 else 'medium' if p['applied']<-.15 else 'light'
        counts['braking']+=1;stages[source]['braking']+=1;stages[source][grade]+=1
        stages[source]['summedBrakeMagnitude']+=-p['applied']
        if grade=='heavy':
            heavy.append(dict(id=p['id'],time=p['time'],source=source,region=s['region'],speed=s['speed'],target=s['target'],
                              follower=p['follower'],leader=p['leader'],scenery=p['scenery'],applied=p['applied'],
                              threat=s['safetyThreat'],conflictAfter=s['safetyConflictAfter'],guardUnresolved=s['safetyUnresolved']))
    old=previous.get(p['id'])
    if old and p['time']-old['time']<.12 and p['applied']<-.5 and p['applied']-old['applied']<-.5:
        abrupt.append(dict(id=p['id'],time=p['time'],source=source,region=s['region'],before=old['applied'],after=p['applied']))
    previous[p['id']]=p
out=dict(scope='Actual command values at successive controller stages, paired with same-tick 10Hz moving AI samples. Attributes the final pedal, not whether braking was necessary. Abrupt changes are between sampled commands, not physics-step jerk. Diagnostic allocations are outside performance acceptance.',
         counts=dict(counts),stages=dict(stages),heavySamples=heavy,abruptSampleTransitions=abrupt,
         valid=counts['matched']>0 and counts['staleDecision']==counts['appliedMismatch']==counts['nonMonotone']==0)
(root/'pedal-stage-analysis.json').write_text(json.dumps(out,indent=2)+'\n')
print(json.dumps(dict(valid=out['valid'],counts=out['counts'],stages=out['stages'],heavyBySource=dict(Counter(x['source'] for x in heavy)),abruptBySource=dict(Counter(x['source'] for x in abrupt))),indent=2))
raise SystemExit(0 if out['valid'] else 1)
