"""Matched clear-road and merge controls; not general racecraft acceptance."""
import json
import sys
from pathlib import Path

root = Path(sys.argv[1]).resolve()
def read(path): return json.loads(path.read_text(encoding='utf-8-sig'))
manifest = read(root / 'manifest.json')
if not manifest['complete']: raise SystemExit('Matrix still running')
checks, summaries, rows = [], {}, {}
def check(name, passed, detail=None): checks.append(dict(name=name, passed=bool(passed), detail=detail))
check('All ten declared cases', len(manifest['runs']) == 10)
for run in manifest['runs']:
    name = run['scenario'] + '-' + run['loadout']
    race = read(root / name / 'race.json')
    track = read(root / name / 'race.track.json')
    samples = [s for s in track['samples'] if s['id'] == 0]
    rows[name] = samples
    if run['scenario'].startswith('rejoin-'):
        check(name + ': genuinely displaced initial position', abs(samples[0]['lineError']) > 3)
    check(name + ': normal exit and every car completes', run['exitCode'] == 0 and race['finished'] == len(race['cars']))
    check(name + ': no incident or recovery', all(c[k] == 0 for c in race['cars'] for k in ('contacts', 'recoveries', 'physicalRecoveries', 'missedGates')))
    check(name + ': all bodies remain on road', all(r['outsideBodySamples'] == 0 for r in track['results']))
    # These are predeclared engineering bounds for a 4 m offset on the long
    # straight: reach the line within 5 s and stay close through the next 3 s.
    # Do not change them retrospectively to make a failed rejoin pass.
    settled = [s for s in samples if 5 <= s['time'] <= 8]
    check(name + ': stable line after initial transition', bool(settled) and max(abs(s['lineError']) for s in settled) < 1)
    summaries[name] = dict(time=race['cars'][0]['time'], firstWithin07=next((s['time'] for s in samples if abs(s['lineError']) < .7), None),
        minimumSpeed=min(s['speed'] for s in samples), plannedRejoinSamples=sum(s['trafficIntent'] == 'Rejoin line' for s in samples))
for setup in ('street','club'):
    solo, rear = rows['rear-reference-'+setup], rows['rear-control-'+setup]
    fields = ('time','position','speed','pedal','steer','progress')
    check(setup + ': harmless rear car matches same-scene reference driving', len(solo) == len(rear) and all(all(a[k] == b[k] for k in fields) for a,b in zip(solo,rear)))
    baseline = summaries['solo-'+setup]['time']
    for side in ('left','right'):
        name = 'rejoin-'+side+'-'+setup
        delta = summaries[name]['time'] - baseline
        check(name + ': rejoins without excessive pace loss', delta < 2, delta)
out = dict(scope=__doc__, assemblyHash=manifest['assemblyHash'], passed=sum(c['passed'] for c in checks), total=len(checks), summaries=summaries, checks=checks)
(root / 'analysis.json').write_text(json.dumps(out, indent=2)+'\n')
print(json.dumps(dict(passed=out['passed'],total=len(checks),summaries=summaries,failures=[c for c in checks if not c['passed']]),indent=2))
raise SystemExit(0 if out['passed']==len(checks) else 1)
