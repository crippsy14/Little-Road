"""Check connected race evidence, including retries and a new-process reload."""
import argparse
import collections
import json
import re
from pathlib import Path

p = argparse.ArgumentParser()
p.add_argument('root', type=Path)
a = p.parse_args()
read = lambda f: json.loads(f.read_text(encoding='utf-8-sig'))
checks = {}
summary = []
folder = a.root / 'adventure'
reports = [folder/'race.json', folder/'race-race-2.json']
reward = lambda place: 500 if place == 1 else 400 if place == 2 else 330 if place == 3 else 280 if place == 4 else 220 if place <= 8 else 170 if place <= 12 else 130 if place <= 18 else 100
for serial, path in enumerate(reports, 1):
    r = read(path)
    roster = read(path.with_suffix('.roster.json'))
    entrants = roster['entries']
    player = next(c for c in r['cars'] if c['id'] == 0)
    key = f'race{serial}'
    checks[key+'ConnectedModern'] = r['stage'] == 'Connected adventure race' and r['driverPolicy'] == 'context-v1' and roster['modern'] and r['seed'] == 1709+serial*11
    checks[key+'FullField'] = r['carCount'] == r['finished'] == len(entrants) == 24 and r['requiredLaps'] == 3 and all(c['finished'] and c['laps'] == 3 for c in r['cars'])
    checks[key+'RearSlotAndOwnedEngine'] = entrants[0]['slot'] == 23 and {v['slot']: v['id'] for v in entrants[0]['parts']}['engine'] == 'engine_workshop'
    passages = collections.defaultdict(list)
    for v in r['checkpointPassages']:
        passages[v['id']].append(v)
    checks[key+'OrderedPhysicalGates'] = len(passages) == 24 and all(len(rows) == 37 and all(v['gate'] == i % 12 and abs(v['requiredProgress']-i*r['trackLength']/12) < .01 for i,v in enumerate(rows)) for rows in passages.values())
    checks[key+'NoTeleportRecovery'] = not r['assistedRecoveries'] and all(c['recoveries'] == 0 for c in r['cars'])
    summary.append(dict(serial=serial,seed=r['seed'],finished=r['finished'],playerPlace=player['place'],playerSeconds=player['time'],reward=reward(player['place']),combinedSeconds=sum(c['time'] for c in r['cars']),participantContactRecords=sum(c['contacts'] for c in r['cars']),physicalRecoveries=sum(c['physicalRecoveries'] for c in r['cars']),maxImpact=max(c['maxImpact'] for c in r['cars'])))
for name in ('adventure','reload'):
    f = a.root/name
    log = (f/'player.log').read_text(encoding='utf-8-sig', errors='replace')
    checks[name+'NormalCleanExit'] = read(f/'exit.json')['exitCode'] == 0 and not re.findall(r'(?m)^(?:[\w.]*Exception:.*|Assertion failed\b.*)',log)
    checks[name+'SmokePassed'] = 'LITTLE_ROAD_ADVENTURE_SMOKE_PASSED' in log
    if name == 'adventure':
        checks['RetryCancelAndCommitPassed'] = 'LITTLE_ROAD_ADVENTURE_RETRY_PASSED' in log
save = read(folder/'save.json')
checks['TwoRewardsPersistExactly'] = save['raceSerial'] == save['settledSerial'] == 2 and save['credits'] == sum(r['reward'] for r in summary)
checks['ReloadMatchesCompletedSession'] = save == read(folder/'adventure.json') == read(a.root/'reload/adventure.json')
checks['QuestAndFittedUpgradePersist'] = save['questStage'] == 3 and len(save['collected']) == 2 and {v['slot']:v['id'] for v in save['fitted']}['engine'] == 'engine_workshop'
out = dict(passed=all(checks.values()),checks=checks,races=summary,savedCredits=save['credits'],limitation='Accelerated unattended integration, not rendered performance, physical DualSense or player acceptance. Physical recovery events remain diagnostic; no zero-contact requirement.')
(a.root/'adventure-analysis.json').write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8')
print(json.dumps(out,indent=2))
raise SystemExit(0 if out['passed'] else 1)
