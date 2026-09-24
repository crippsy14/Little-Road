"""Recorded-behaviour parity for the declared serial/parallel integration cases."""
import argparse
import json
from pathlib import Path
import subprocess
import sys

p = argparse.ArgumentParser()
p.add_argument('folder', type=Path)
args = p.parse_args()
root = args.folder
read = lambda p: json.loads(p.read_text(encoding='utf-8-sig'))
expected = read(root/'expectations.json')
manifest = read(root/'manifest.json')
names = [r['name'] for r in manifest['runs']]
wanted = {c['name']+'-'+mode for c in expected['cases'] for mode in ('serial', 'parallel')}
checks = {'all declared runs completed exactly once': manifest['complete'] and set(names) == wanted and len(names) == len(wanted)}
cases = {}
for case in expected['cases']:
    name = case['name']
    serial, parallel = (root/(name+'-'+mode) for mode in ('serial', 'parallel'))
    result = subprocess.run([sys.executable, str(Path(__file__).with_name('verify-context-race-parity.py')), str(serial), str(parallel)], capture_output=True, text=True)
    (parallel/'parity-output.txt').write_text(result.stdout+result.stderr, encoding='utf-8')
    checks[name+': exact recorded decisions, motion and race events/results'] = result.returncode == 0
    a, b = read(serial/'launch.json'), read(parallel/'launch.json')
    metadata = ('assemblyHash', 'track', 'loadout', 'grid', 'cars', 'laps', 'seed', 'policy', 'racingLine', 'physicalRecovery', 'trafficCase', 'opponents', 'roadFeasibility', 'preparedPath')
    checks[name+': paired configuration and declared workers'] = all(a[k] == b[k] for k in metadata) and a['parallelForecastWorkers'] == 0 and b['parallelForecastWorkers'] == expected['workers']
    checks[name+': both processes exited normally'] = read(serial/'exit.json')['exitCode'] == read(parallel/'exit.json')['exitCode'] == 0
    checks[name+': traffic evidence unchanged'] = read(serial/'race.traffic.json') == read(parallel/'race.traffic.json')
    track = read(parallel/'race.track.json')
    forecast = {k: sum(r[k] for r in track['roadCosts']) for k in ('calls', 'preparedRequests', 'prefetchedCalls', 'synchronousCalls')}
    checks[name+': joined results used and execution counters reconcile'] = forecast['prefetchedCalls'] > 0 and forecast['calls'] == forecast['prefetchedCalls']+forecast['synchronousCalls'] and forecast['preparedRequests'] >= forecast['prefetchedCalls']
    race = read(parallel/'race.json')
    cases[name] = dict(forecast=forecast, finished=race['finished'], cars=race['carCount'],
                       contacts=sum(c['contacts'] for c in race['cars']), recoveries=sum(c['recoveries'] for c in race['cars']),
                       physicalRecoveries=sum(c['physicalRecoveries'] for c in race['cars']),
                       outsideBodySamples=sum(c['outsideBodySamples'] for c in track['results']))
out = dict(scope=manifest['scope'], passed=sum(checks.values()), total=len(checks), checks=checks, cases=cases)
(root/'analysis.json').write_text(json.dumps(out, indent=2)+'\n', encoding='utf-8')
print(json.dumps(dict(passed=out['passed'], total=out['total'], failures=[k for k,v in checks.items() if not v], cases=cases), indent=2))
raise SystemExit(0 if all(checks.values()) else 1)
