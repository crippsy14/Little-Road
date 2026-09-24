"""Assert causal controls using live trajectories, not just assigned labels."""
import json
from pathlib import Path
root=Path(__file__).resolve().parents[1]/'review/unity-35/driver-identities-86'
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
results=read(root/'analysis.json')['results']
names=['pass-cautious','pass-assertive','pass-daring']
passes=[results[n] for n in names]
tracks=[read(root/n/'race.track.json') for n in names]
launches=[read(root/n/'launch.json') for n in names]
a,b=[results[n] for n in ('solo-neutral','solo-early')]
checks=dict(
    passingRunsFinish=all(all(p['checks'].values()) for p in passes),
    passingClean=all(p['contactParticipantRecords']==0 and p['outsideSamples']==0 and p['physicalRecoveries']==0 and p['assistedRecoveries']==0 for p in passes),
    allPassBeforeExit=all(p['passing']['beforeCorridorExit'] for p in passes),
    increasingRiskPassesEarlier=passes[0]['passing']['sustained']>passes[1]['passing']['sustained']>passes[2]['passing']['sustained']>0,
    equalLeaderFinish=len({p['cars'][1]['time'] for p in passes})==1,
    equalBrakingEstimate=all(p['cars'][0]['profile']['effectiveBraking']==passes[0]['cars'][0]['profile']['effectiveBraking'] for p in passes),
    sameVehicle=all(t['tuning']==tracks[0]['tuning'] for t in tracks),
    matchedPassingSetup=all(all(l[k]==launches[0][k] for k in ('seed','track','loadout','cars','opponents','racingLine','trafficCase')) for l in launches),
    soloClean=all(all(p['checks'].values()) and p['contactParticipantRecords']==0 and p['outsideSamples']==0 and p['physicalRecoveries']==0 for p in (a,b)),
    earlierBrakeEntry=b['firstBrakeAfter150m']['progress']<a['firstBrakeAfter150m']['progress'],
    earlierBrakingCostsLapTime=b['cars'][0]['time']>a['cars'][0]['time'],
    clearOpeningPedalUnchanged=a['openingAveragePedal']==b['openingAveragePedal'] and a['openingFullPedalFraction']==b['openingFullPedalFraction'],
    allControlsZeroWarmAllocation=all(p['warmBytes']==0 for p in passes+[a,b]))
out=dict(checks=checks,passed=all(checks.values()),scope='One-seed clean opportunity and braking controls, not proof of universal risk ordering or personality enjoyment.')
(root/'trait-controls.json').write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8')
print(json.dumps(out,indent=2))
