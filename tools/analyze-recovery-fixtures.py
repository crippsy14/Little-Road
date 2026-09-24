"""Check actual recovery outcomes; retain and explain failures."""
import json
import sys
from pathlib import Path

root=Path(sys.argv[1])
def read(path):return json.loads(Path(path).read_text(encoding='utf-8-sig'))
manifest=read(root/'manifest.json')
if not manifest['complete']:raise SystemExit('Recovery batch is still running.')
checks=[];rows=[]
def check(name,passed):checks.append(dict(name=name,passed=bool(passed)))
for run in manifest['runs']:
    race=read(run['report']);recovery=read(Path(run['report']).with_suffix('.recovery.json'));car=race['cars'][0]
    if run['fixture']=='permanent-block':
        resets=race.get('assistedRecoveries',[])
        check(run['name']+': trapped entrant finishes with one explicitly counted assisted reset',race['finished']==race['carCount'] and car['recoveries']==recovery['assistedResets']==recovery['observedResetJumps']==len(resets)==1)
        # The report clock is a separately accumulated float. Allow two physics
        # ticks at the sixty-second boundary, not an arbitrary time concession.
        earliest=60-2*race['physicsStepSeconds']
        check(run['name']+': fallback retains delay and loses progress without advancing gate',len(resets)==1 and earliest<=resets[0]['time']<90 and resets[0]['progressAfter']<=resets[0]['progressBefore']-4 and resets[0]['gateBefore']==resets[0]['gateAfter'] and resets[0]['reason']=='Persistent geometry or stationary traffic')
        check(run['name']+': blocked fallback destination is rejected',bool(resets) and resets[0]['rejectedDestinations']>=1)
    else:
        check(run['name']+': all entrants finish, no assisted reset',race['finished']==race['carCount'] and all(c['recoveries']==0 for c in race['cars']))
        check(run['name']+': at least one physical recovery completed',recovery['physicalRecoveries']>=1)
    check(run['name']+': no contacts or sustained body overlap',all(c['contacts']==0 for c in race['cars']) and race['maxCarPoseOverlap']<.02 and race['maxEnvironmentPoseOverlap']<.02)
    if run['fixture']=='permanent-block':check(run['name']+': physical movement remains continuous outside the reported reset',recovery['maxPhysicalStepMetres']<.5 and recovery['maxStepMetres']>.5)
    else:check(run['name']+': no position jump over 0.5 m per 120 Hz tick',recovery['maxStepMetres']<.5)
    if run['fixture']=='missed-gate':
        check(run['name']+': unearned gate requires a physical return',car['missedGates']>=1 and recovery['returnedBehindMissedGate'] and not recovery['unearnedGateAdvance'])
    else:check(run['name']+': no missed checkpoints',all(c['missedGates']==0 for c in race['cars']))
    if run['fixture'] in ('rolling-left','rolling-right'):
        rolling=[e for e in recovery['events'] if e['state']=='Rolling rejoin']
        resumed=[e for e in recovery['events'] if e['state']=='Racing']
        check(run['name']+': moving return completes within five recovery seconds',bool(rolling) and bool(resumed) and recovery['activeSeconds']<5 and resumed[0]['speed']>5.5)
        track=read(Path(run['report']).with_suffix('.track.json'))
        rolling_samples=[s for s in track['samples'] if s['id']==0 and s['recoveryState']=='Rolling rejoin']
        check(run['name']+': rolling commands coast or brake while physically moving',bool(rolling_samples) and all(s['pedal']<=0 and s['speed']>1.5 for s in rolling_samples))
        check(run['name']+': clear return does not reverse or wait',recovery['reverseSeconds']==0 and recovery['waitingSeconds']==0)
    if run['fixture']=='rolling-blocked':
        check(run['name']+': blocked high-speed return first stabilises',any(e['state']=='Stabilise' and e['speed']>5.5 for e in recovery['events']))
    if run['fixture']=='blocked-wait':check(run['name']+': purposeful wait survives the old six-second limit',recovery['waitingSeconds']>7.5)
    if run['fixture']=='long-blocked-wait':check(run['name']+': temporary geometry clears before the separate recovery deadline',recovery['waitingSeconds']>39)
    if run['fixture']=='traffic':
        track=read(Path(run['report']).with_suffix('.track.json'))
        actor=[s for s in track['samples'] if s['id']==0];other=[s for s in track['samples'] if s['id']==1]
        passed=[a['time'] for a,b in zip(actor,other) if abs(a['time']-b['time'])<.001 and b['progress']>a['progress']+6]
        resumed=[e['time'] for e in recovery['events'] if e['state']=='Racing']
        check(run['name']+': approaching traffic passes before racing resumes',recovery['trafficPassed'] and bool(passed) and bool(resumed) and passed[0]<resumed[0])
        check(run['name']+': recovery yields for the approaching car',recovery['waitingSeconds']>.25)
    if run['fixture']=='wall':check(run['name']+': front obstruction provokes physical reversing',recovery['reverseSeconds']>.2)
    check(run['name']+': player exits normally',run['exitCode']==0)
    rows.append(dict(name=run['name'],finished=car['finished'],time=car['time'],activeSeconds=recovery['activeSeconds'],waitingSeconds=recovery['waitingSeconds'],reverseSeconds=recovery['reverseSeconds'],resets=car['recoveries'],contacts=car['contacts'],missedGates=car['missedGates'],maxStep=recovery['maxStepMetres'],events=recovery['events']))
result=dict(scope='Declared straight-section recovery fixtures; broader corners, obstacles and traffic remain separate acceptance work',assemblyHash=manifest['assemblyHash'],passed=sum(c['passed'] for c in checks),total=len(checks),checks=checks,runs=rows)
(root/'analysis.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps(dict(passed=result['passed'],total=result['total'],failed=[c['name'] for c in checks if not c['passed']]),indent=2))
for row in rows:print(f"{row['name']:25s} finish={row['finished']} recovery={row['activeSeconds']:.1f}s reverse={row['reverseSeconds']:.1f}s wait={row['waitingSeconds']:.1f}s resets={row['resets']} contacts={row['contacts']}")
if result['passed']!=result['total']:raise SystemExit(1)
