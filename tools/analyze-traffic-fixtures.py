"""Declared fixture acceptance, preserving failures and paired clear-track controls."""
import json
import sys
from pathlib import Path

root = Path(sys.argv[1]).resolve()
def read(path): return json.loads(path.read_text(encoding='utf-8-sig'))
manifest = read(root / 'manifest.json')
if not manifest['complete']: raise SystemExit('Matrix is still running.')
checks, summaries, reports = [], {}, {}
policy=manifest.get('policy','traffic')
def check(name, passed, detail=None): checks.append(dict(name=name, passed=bool(passed), detail=detail))
if 'scenarios' in manifest:
    scenarios=manifest['scenarios']
    if isinstance(scenarios,str):scenarios=[scenarios]
    expected=set(scenarios)|{f'solo-{loadout}-{p}' for loadout in ('stock','club') for p in ('reference',policy)}
    if manifest.get('includeQueue'):expected.add('queue')
    names=[r['name'] for r in manifest['runs']]
    check('Every declared fixture appears exactly once',set(names)==expected and len(names)==len(expected),names)
for run in manifest['runs']:
    name = run['name']; folder = root / name
    race = read(folder / 'race.json'); track = read(folder / 'race.track.json'); fixture = read(folder / 'race.traffic.json')
    reports[name] = race
    check(name + ': process exits normally', run['exitCode'] == 0, run['exitCode'])
    check(name + ': every car finishes', race['finished'] == run['cars'])
    for field in ['contacts', 'recoveries', 'physicalRecoveries', 'missedGates']:
        check(name + ': no ' + field, all(c[field] == 0 for c in race['cars']), [c[field] for c in race['cars']])
    check(name + ': no sampled body departure', all(c['outsideBodySamples'] == 0 for c in track['results']))
    check(name + ': overlap below 2 cm', max(race['maxCarPoseOverlap'], race['maxEnvironmentPoseOverlap']) < .02)
    check(name + ': collision query not saturated', race['overlapQuerySaturations'] == 0)
    straight = [s for s in track['samples'] if s['progress'] < fixture['straightEnd'] - 80 and s['time'] > .2]
    minimum_speed = min(s['speed'] for s in straight)
    braking = [s for s in straight if s['id'] == run['cars'] - 1 and s['pedal'] < -.1]
    check(name + ': keeps moving on initial straight', minimum_speed > 3, minimum_speed)
    stopped = sum(s['speed'] < 1 and s['time'] > .2 for s in track['samples'])
    check(name + ': no unexplained stop on remaining course', stopped == 0, stopped)
    # The leading car starts with clear road; stopping it would manufacture a pass.
    check(name + ': no unnecessary lead-car braking on initial straight', len(braking) == 0, len(braking))
    finish=[c['time'] for c in race['cars'] if c['finished']]
    complete=len(finish)==run['cars']
    summaries[name] = dict(completedCars=len(finish),expectedCars=run['cars'],totalCarSeconds=sum(finish) if complete else None,meanCarSeconds=sum(finish)/len(finish) if complete else None,lastCarSeconds=max(finish) if complete else None,times=[c['time'] for c in race['cars']], minimumStraightSpeed=minimum_speed,
        leaderBrakingSamples=len(braking), firstClearPass=fixture['firstClearPass'], sustainedPassAt=fixture['sustainedPassAt'])
    if name == 'pass':
        check(name + ': sustained complete pass on straight', fixture['sustainedPassAt'] > 0 and fixture['passOnStraight'])
    if name == 'follow':
        check(name + ': equal equipment maintains order', fixture['firstClearPass'] < 0)
        # 24 m grid spacing is roughly 1 s; allow 4 s total for a stable following gap.
        delta = race['cars'][0]['time'] - race['cars'][1]['time']
        check(name + ': follows without excessive delay', 0 < delta < 4, delta)
    if name == 'no-room':
        check(name + ': no pass inside single-car corridor', not fixture['passBeforeCorridorExit'])
    if name == 'queue':
        rear_passes = [p for p in fixture['passes'] if p['follower'] == 0]
        summaries[name]['passes'] = fixture['passes']
        check(name + ': faster rear car clears both opponents sustainably', len(rear_passes) == 2 and all(p['sustainedAt'] > 0 for p in rear_passes))

for loadout in ['stock', 'club']:
    a, b = (reports[f'solo-{loadout}-{p}'] for p in ['reference', policy])
    check(loadout + ': exact solo motion and inputs', a['motion'] == b['motion'])
    fields = ['time', 'bestLap', 'laps', 'contacts', 'recoveries', 'physicalRecoveries', 'missedGates']
    check(loadout + ': exact solo result', all(a['cars'][0][f] == b['cars'][0][f] for f in fields))

out = dict(scope='Deterministic mid-lap fixtures, not general racecraft, full-lap pace, rendering performance or player acceptance.',
    assemblyHash=manifest['assemblyHash'], passed=sum(c['passed'] for c in checks), total=len(checks), summaries=summaries, checks=checks)
(root / 'analysis.json').write_text(json.dumps(out, indent=2) + '\n')
print(json.dumps(dict(passed=out['passed'], total=out['total'], summaries=summaries, failures=[c for c in checks if not c['passed']]), indent=2))
raise SystemExit(0 if out['passed'] == out['total'] else 1)
