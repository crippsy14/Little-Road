"""Score ordinary leader braking separately from the deliberately blind command probe."""
import json
import sys
from pathlib import Path

root = Path(sys.argv[1])
manifest = json.loads((root / 'manifest.json').read_text(encoding='utf-8-sig'))
checks, results = [], {}

def check(name, passed, value=None):
    checks.append(dict(name=name, passed=bool(passed), value=value))

scenarios=manifest.get('scenarios',['brake-leader','brake-probe'])
if isinstance(scenarios,str):scenarios=[scenarios]
check('All declared paired runs completed', manifest['complete'] and len(manifest['runs']) == 4*len(scenarios))
for run in manifest['runs']:
    folder = Path(run['folder'])
    race = json.loads((folder / 'race.json').read_text())
    track = json.loads((folder / 'race.track.json').read_text())
    rows = [s for s in track['samples'] if s['id'] == 0]
    active = [s for s in rows if s['safetyActive']]
    after = [s for s in rows if active and s['time'] > active[-1]['time'] and s['pedal'] > .1]
    release = after[0]['time'] - active[-1]['time'] if after else None
    # The declared disturbance ends at 2 s; full planned throttle remains in
    # effect through 3 s. Later corner/finish braking is a separate encounter.
    probe_after=[s for s in rows if 2 <= s['time'] < 3 and not s['safetyActive'] and s['pedal'] > .1]
    probe_release=probe_after[0]['time']-2 if probe_after else None
    probe_late=[s for s in rows if 2.3 < s['time'] < 3 and s['safetyActive']]
    key = folder.name
    finish=[c['time'] for c in race['cars'] if c['finished']];complete=len(finish)==race['carCount']
    result = dict(followerTime=race['cars'][0]['time'], finished=race['finished'],expectedCars=race['carCount'],totalCarSeconds=sum(finish) if complete else None,meanCarSeconds=sum(finish)/len(finish) if complete else None,lastCarSeconds=max(finish) if complete else None,
                  contacts=sum(c['contacts'] for c in race['cars']),
                  outside=sum(c['outsideBodySamples'] for c in track['results']),
                  safetySamples=len(active), firstIntervention=active[0]['time'] if active else None,
                  lastIntervention=active[-1]['time'] if active else None,
                  positivePedalAfterLastIntervention=release,
                  probeReleaseAfterLeaderAccelerates=probe_release if run['scenario']=='brake-probe' else None,
                  probeLateGuardSamples=len(probe_late) if run['scenario']=='brake-probe' else None,
                  maximumImpact=max(c['maxImpact'] for c in race['cars']))
    results[key] = result
    check(key + ': normal process exit', run['exitCode'] == 0)
    # The deliberately unsafe control demonstrates contact; failure to finish
    # after that incident is recorded, not required to be a clean-control pass.
    if run['mode'] == 'safety' or run['scenario'] != 'brake-probe':
        check(key + ': remaining course completed', race['finished'] == 2)
    if run['mode'] != 'safety':
        continue
    check(key + ': no contacts', result['contacts'] == 0, result['contacts'])
    check(key + ': no sampled road departure', result['outside'] == 0)
    check(key + ': no recovery or missed gates', all(c['recoveries'] == c['physicalRecoveries'] == c['missedGates'] == 0 for c in race['cars']))
    check(key + ': no pose overlap over 2 cm', max(race['maxCarPoseOverlap'], race['maxEnvironmentPoseOverlap']) < .02)
    check(key + ': no collision query saturation', race['overlapQuerySaturations'] == 0)
    check(key + ': no intervention before disturbance', not any(s['time'] < 1 and s['safetyActive'] for s in rows))
    if run['scenario'] == 'brake-probe':
        check(key + ': safety response exercised', len(active) > 0)
        check(key + ': positive pedal resumes within 0.3 s of declared leader acceleration', probe_release is not None and probe_release <= .3, probe_release)
        check(key + ': no renewed guard hold during remaining straight command probe', not probe_late,len(probe_late))

for setup in ('street', 'club'):
    if 'brake-leader' in scenarios:
        baseline, candidate = (results[f'brake-leader-{setup}-{mode}'] for mode in ('baseline', 'safety'))
        check(setup + ': ordinary braking pace retained within 0.5 s', candidate['followerTime'] <= baseline['followerTime'] + .5)
    if 'brake-probe' in scenarios:
        check(setup + ': controlled probe actually exposes contact without guard', results[f'brake-probe-{setup}-baseline']['contacts'] > 0)

out = dict(analyzerVersion=3,scope='Two equal-equipped cars, 12 m initial gap, street 18 m/s or club 26 m/s. Leader physically brakes from 1 to 2 s. Ordinary case uses the planner. Probe holds the follower straight at full planned throttle for 3 s; only the guard may restrict it, then normal planning resumes. Release is scored against the known end of leader braking at 2 s, while the follower still requests full throttle through 3 s; later course interventions remain diagnostic. This is a safety-component probe, not evidence the ordinary planner would collide. Unsafe probe controls must expose contact, but their post-contact completion is diagnostic. Safety candidates still must finish the remaining course within the unchanged 45 s timeout. Partial-course completion is not a full-lap pace benchmark.',
           scenarios=scenarios,passed=sum(c['passed'] for c in checks), total=len(checks), results=results, checks=checks)
(root / 'analysis.json').write_text(json.dumps(out, indent=2) + '\n')
print(json.dumps(dict(passed=out['passed'], total=out['total'], results=results,
                      failures=[c for c in checks if not c['passed']]), indent=2))
raise SystemExit(0 if all(c['passed'] for c in checks) else 1)
