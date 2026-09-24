"""Compare cumulative planning work, keeping worker time separate from wall time."""
import argparse
import json
from pathlib import Path

p = argparse.ArgumentParser()
p.add_argument('study', type=Path)
args = p.parse_args()
read = lambda path: json.loads(path.read_text(encoding='utf-8-sig'))
out = {}
for course in ('bayview', 'cedar'):
    row = {}
    tracks = []
    for arm in ('before', 'after'):
        folder = args.study / f'{course}-{arm}'
        track = read(folder / 'race.track.json')
        tracks.append(track)
        phases = track['planningPhaseCosts']
        costs = track['roadCosts']
        frames = read(folder / 'race.driver-frames.json')['frames'][1:]
        row[arm] = {
            'planningPhases': {k: sum(r.get(k, 0) for r in phases)
                               for k in sorted({k for r in phases for k in r} - {'id'})},
            'roadCosts': {k: sum(r.get(k, 0) for r in costs)
                          for k in sorted({k for r in costs for k in r} - {'id'})},
            # Null means this older build did not instrument the distinction.
            'dispatchedJobs': sum(r['forecastJobs'] for r in frames),
            'completedForecastJobs': sum(r['completedForecastJobs'] for r in frames)
                if all('completedForecastJobs' in r for r in frames) else None,
            'rankingJobs': sum(r['rankingJobs'] for r in frames)
                if all('rankingJobs' in r for r in frames) else None,
        }
    row['dynamicDiagnosticsEqual'] = tracks[0]['dynamicRejoin'] == tracks[1]['dynamicRejoin']
    if not row['dynamicDiagnosticsEqual']:
        raise SystemExit(f'{course}: cumulative dynamic planner diagnostics changed')
    out[course] = row
out['interpretation'] = ('Cumulative phase timings are contained regions, not additive wall cost. '
    'Worker rank time is included in summed worker elapsed and joined snapshot wall time. '
    'dynamicForecasts counts constructed finite requests; completedForecastJobs counts successful '
    'physical forecasts. Job totals omit the same initial loading observation as the frame analyzer.')
(args.study / 'phase-summary.json').write_text(json.dumps(out, indent=2) + '\n', encoding='utf-8')
print(json.dumps(out, indent=2))
