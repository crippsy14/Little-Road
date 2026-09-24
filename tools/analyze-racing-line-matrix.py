"""Compare physical solo runs. Stdlib only; raw failures are retained in output."""
import argparse
import bisect
import csv
import json
import math
from pathlib import Path
from statistics import mean


def read(path):
    return json.loads(Path(path).read_text(encoding='utf-8-sig'))


def crossing(samples, progress):
    coordinates = [s['progress'] for s in samples]
    i = bisect.bisect_left(coordinates, progress)
    if not 0 < i < len(samples):
        return None
    a, b = samples[i - 1:i + 1]
    f = (progress - a['progress']) / (b['progress'] - a['progress'])
    return {key: a[key] + f * (b[key] - a[key]) for key in ('time', 'speed')}


def summarize(run):
    race = read(run['report'])
    track = read(Path(run['report']).with_suffix('.track.json'))
    car, metric = race['cars'][0], track['results'][0]
    samples = [s for s in track['samples'] if s['id'] == 0]
    monotonic = all(b['progress'] >= a['progress'] - .01 for a, b in zip(samples, samples[1:]))
    # Full flying laps only; the terminal crossing can be interpolated with the
    # exact finish event time and its reported progress (at most one physics step).
    terminal = dict(samples[-1], time=car['time'], progress=car['progress'])
    if car['finished'] and terminal['time'] > samples[-1]['time']:
        samples.append(terminal)
    length = track['length']
    laps, sectors = [], []
    region_starts = []
    previous = None
    for sample in samples:
        if sample['progress'] < length or sample['progress'] >= 2 * length:
            continue
        if sample['region'] != previous:
            region_starts.append((sample['progress'] % length, sample['region']))
            previous = sample['region']
    # Fixed 12 gate sectors are exactly comparable across candidate trajectories.
    # Named regions provide context only; their first observed sample is not an
    # authoritative timing boundary.
    if monotonic:
        for lap in range(1, race['requiredLaps']):
            begin, end = crossing(samples, lap * length), crossing(samples, (lap + 1) * length)
            if begin and end:
                laps.append(end['time'] - begin['time'])
            for sector in range(12):
                begin = crossing(samples, (lap + sector / 12) * length)
                end = crossing(samples, (lap + (sector + 1) / 12) * length)
                if begin and end:
                    sector_samples = [s for s in samples if begin['time'] <= s['time'] <= end['time']]
                    sectors.append(dict(lap=lap + 1, sector=sector + 1,
                                        seconds=end['time'] - begin['time'], entrySpeed=begin['speed'], exitSpeed=end['speed'],
                                        minSpeed=min((s['speed'] for s in sector_samples), default=begin['speed'])))
    flying = [s for s in samples if length <= s['progress'] < race['requiredLaps'] * length]
    row = dict(name=run['name'], loadout=run['loadout'], variant=run['variant'], repeat=run['repeat'],
               finished=car['finished'], bestLap=car['bestLap'], meanFlying=mean(laps) if laps else None,
               lapSpread=max(laps)-min(laps) if laps else None, predicted=track['predictedLapTime'],
               contacts=car['contacts'], recoveries=car['recoveries'], missed=car['missedGates'],
               outsideBodySamples=metric['outsideBodySamples'], maxBodyOffset=metric['maxBodyOffset'],
               adherenceTime=metric['withinOneMetrePercent'], adherenceDistance=metric['distanceWithinOneMetrePercent'],
               flyingMeanAbsoluteError=mean(abs(s['lineError']) for s in flying) if flying else None,
               flyingMaxAbsoluteError=max((abs(s['lineError']) for s in flying), default=None),
               flyingTopSpeed=max((s['speed'] for s in flying), default=None),
               monotonicProgress=monotonic, laps=laps, sectors=sectors, report=run['report'])
    row['exitCode']=run['exitCode']
    row['clean'] = bool(row['finished'] and monotonic and run['exitCode'] in (None,0) and all(row[k] == 0 for k in ('contacts','recoveries','missed','outsideBodySamples')))
    return row


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('folder', type=Path)
    args = parser.parse_args()
    manifest = read(args.folder / 'manifest.json')
    if not manifest['complete']:
        raise SystemExit('Matrix is still running; do not publish a partial ranking.')
    rows = [summarize(run) for run in manifest['runs']]
    checks = []
    for row in rows:
        checks.append(dict(name=row['name'] + ': clean complete physical run', passed=row['clean']))
        checks.append(dict(name=row['name'] + ': expected flying lap count', passed=len(row['laps']) == manifest['laps'] - 1))
        checks.append(dict(name=row['name'] + ': finite speed and timing', passed=all(math.isfinite(row[k]) and row[k] > 0 for k in ('bestLap', 'predicted', 'flyingTopSpeed'))))
    summary = dict(assemblyHash=manifest['assemblyHash'], status='candidate comparison; reference acceptance pending held-out validation',
                   measurement='Lap/sector splits interpolated from 10 Hz route progress; exact bestLap comes from the race finish/gate controller. Boundary checks are sampled. Human laps excluded.',
                   passed=sum(c['passed'] for c in checks), total=len(checks), checks=checks, runs=rows)
    (args.folder / 'comparison.json').write_text(json.dumps(summary, indent=2) + '\n')
    columns = [key for key in rows[0] if key not in ('laps', 'sectors')]
    with (args.folder / 'comparison.csv').open('w', newline='') as stream:
        writer = csv.DictWriter(stream, fieldnames=columns, extrasaction='ignore')
        writer.writeheader(); writer.writerows(sorted(rows, key=lambda r: (r['loadout'], r['bestLap'])))
    print(json.dumps({k: summary[k] for k in ('assemblyHash','passed','total')}))
    for row in sorted(rows, key=lambda r: (r['loadout'], r['bestLap'])):
        print(f"{row['name']:26s} best={row['bestLap']:.3f}s predicted={row['predicted']:.3f}s clean={row['clean']} line={row['adherenceTime']:.1f}% body={row['maxBodyOffset']:.2f}m")


if __name__ == '__main__':
    main()
