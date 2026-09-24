"""Same-tick pedal attribution, with per-driver and launch/region breakdowns."""
import json
import sys
from collections import Counter, defaultdict
from pathlib import Path

root=Path(sys.argv[1])
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
track=read(root/'race.track.json');race=read(root/'race.json')
samples={(s['id'],s['time']):s for s in track['samples']}
contexts={(s['id'],s['time']):s for s in track['contextSamples']}
finish={c['id']:c['time'] for c in race['cars']}
totals=defaultdict(Counter);regions=defaultdict(lambda:defaultdict(Counter));launch=[];recovery=Counter()
for p in track['pedalSamples']:
    key=(p['id'],p['time']);s=samples[key];c=contexts[key]
    if p['time']<=0 or p['time']>=finish[p['id']] or s['inputOwner']!='AI':continue
    # Recovery owns its own command and intentionally stops calling the context
    # policy. Report these samples separately; stale context values cannot be
    # attributed to follower/leader/safety decisions during recovery.
    if s['recoveryState'] not in ('Racing','Disabled'):
        recovery[p['id']]+=1;continue
    assert abs(p['decisionTimestamp']-p['sampleTimestamp'])<1e-6
    assert abs(p['applied']-s['pedal'])<1e-6
    source='unresolvedPath' if p['trafficUnresolved'] else 'coastContinuation' if p['coastContinuation'] else 'follower'
    for a,b,name in [('follower','leader','movingLeader'),('leader','scenery','scenery'),('scenery','applied','immediateSafety')]:
        if p[b]<p[a]-1e-6:source=name
    for bucket in [totals[p['id']],regions[p['id']][s['region']]]:
        bucket['samples']+=1
        bucket['braking']+=p['applied']<-.01
        bucket['coasting']+=abs(p['applied'])<=.01
        bucket['coastPath']+=p['coastContinuation']
        bucket['trafficUnresolved']+=p['trafficUnresolved']
        if p['applied']<.95:bucket['belowFull_'+source]+=1
    if p['id']==0 and p['time']<15:
        rival=samples.get((c['leader'],p['time']))
        launch.append(dict(time=p['time'],speed=s['speed'],offset=s['roadOffset'],targetOffset=c['offset'],
                           follower=p['follower'],followerTarget=p['followerTarget'],afterLeader=p['leader'],applied=p['applied'],
                           leader=c['leader'],leaderLimit=c['leaderLimit'],
                           lateralSeparation=abs(s['roadOffset']-rival['roadOffset']) if rival else None,
                           coastPath=p['coastContinuation'],source=source))
out=dict(scope='10Hz pre-finish same-tick command-stage attribution. Counts indicate restrictions, not that the restrictions were unnecessary; no elapsed-time integration or hidden speed entitlement. Geometry and future traffic still matter.',
         driverSampleCounts=totals,regionSampleCounts=regions,previewOpening=launch,recoverySampleCounts=recovery)
(root/'pace-bottleneck-analysis.json').write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8')
print(json.dumps(dict(previewCounts=totals[0],openingRecords=len(launch)),indent=2))
