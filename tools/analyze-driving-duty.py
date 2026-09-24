"""Time-weighted pedal/line audit, optionally compared with matched solo runs.

The comparison flags braking while below solo pace; it does not prove that
traffic braking or leaving the reference line was unnecessary.
"""
import argparse
from bisect import bisect_left
from collections import defaultdict
import json
from pathlib import Path


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def within_fraction(a, b, limit=1):
    if abs(b-a) < 1e-9:
        return float(abs(a) <= limit)
    lo, hi = sorted(((-limit-a)/(b-a), (limit-a)/(b-a)))
    return max(0, min(1, hi)-max(0, lo))


def intervals(rows):
    """Common post-start-line denominator; no invented final or missing samples."""
    for a, b in zip(rows, rows[1:]):
        dt = b['time']-a['time']
        if not 0 < dt <= .25 or b['progress'] < 0:
            continue
        fraction = 0
        if a['progress'] < 0:
            fraction = -a['progress']/(b['progress']-a['progress'])
        error = a['lineError']+(b['lineError']-a['lineError'])*fraction
        yield a, dt*(1-fraction), within_fraction(error, b['lineError'])


class Solo:
    def __init__(self, folder, field):
        self.folder = folder
        self.race = read(folder/'race.json')
        track = read(folder/'race.track.json')
        if self.race['trackId'] != field['trackId'] or self.race['trackVersion'] != field['trackVersion']:
            raise ValueError('Solo and field must use the same track/version')
        if self.race['carCount'] != 1 or not self.race['cars'][0]['finished']:
            raise ValueError('Solo must be one completed car')
        self.equipment = self.race['cars'][0]['equipment']
        # Discard grid and any movement beyond the first reference lap.
        self.rows = sorted((s for s in track['samples'] if 0 <= s['progress'] <= self.race['trackLength']), key=lambda s:s['route'])
        self.distance = [s['route'] for s in self.rows]

    def at(self, route):
        i = bisect_left(self.distance, route)
        if i == 0 or i == len(self.rows):
            return self.rows[min(i, len(self.rows)-1)]
        a, b = self.rows[i-1:i+1]
        u = (route-a['route'])/max(1e-9, b['route']-a['route'])
        return {k:a[k]+(b[k]-a[k])*u for k in ('speed', 'pedal')}


def audit(folder, solo_folders=()):
    race = read(folder/'race.json')
    track = read(folder/'race.track.json')
    solos = [Solo(p, race) for p in solo_folders]
    solos = {s.equipment:s for s in solos}
    grouped = defaultdict(list)
    for sample in track['samples']:
        grouped[sample['id']].append(sample)
    results = []
    for car in race['cars']:
        totals = defaultdict(float)
        regions = defaultdict(lambda:defaultdict(float))
        causes = defaultdict(float)
        solo = solos.get(car['equipment'])
        for a, dt, adherence in intervals(grouped[car['id']]):
            brake = a['pedal'] < -.05
            throttle = a['pedal'] > .05
            recovery = a.get('recoveryState', 'Disabled') not in ('Disabled', 'Racing')
            cause = ('recovery' if recovery else 'immediate guard' if a.get('safetyActive') else
                     'planner following/stop' if a.get('trafficIntent') in ('Follow','Brake / hold') else
                     'reference driving' if a.get('trafficIntent') == 'RaceLine' else 'other path tracking')
            reference = solo.at(a['route']) if solo else None
            below = bool(reference and reference['pedal'] >= -.05 and a['speed'] < reference['speed']-2)
            for bucket in (totals, regions[a['region']]):
                bucket['seconds'] += dt
                bucket['brakingSeconds'] += dt*brake
                bucket['acceleratingSeconds'] += dt*throttle
                bucket['coastingSeconds'] += dt*(not brake and not throttle)
                bucket['fullThrottleSeconds'] += dt*(a['pedal'] >= .95)
                bucket['onLineSeconds'] += dt*adherence
                bucket['speedIntegral'] += dt*a['speed']
                bucket['brakingBelowNonBrakingSoloSeconds'] += dt*(brake and below)
            if brake:
                causes[cause] += dt
        for bucket in (totals, *regions.values()):
            sec = bucket['seconds']
            for name in ('braking','accelerating','coasting','fullThrottle','onLine'):
                bucket[name+'Percent'] = 100*bucket[name+'Seconds']/max(1e-9, sec)
            bucket['meanSpeed'] = bucket.pop('speedIntegral')/max(1e-9, sec)
        results.append(dict(id=car['id'], equipment=car['equipment'], place=car['place'], finished=car['finished'],
                            finishSeconds=car['time'], contacts=car['contacts'], totals=dict(totals),
                            brakingContextSeconds=dict(causes), regions={k:dict(v) for k,v in regions.items()}))
    seconds = sum(c['totals']['seconds'] for c in results)
    aggregate = {k:100*sum(c['totals'][k+'Seconds'] for c in results)/max(1e-9, seconds)
                 for k in ('braking','accelerating','coasting','fullThrottle','onLine')}
    out = dict(scope='10 Hz sampled controls held to next sample; line error interpolated within each interval. '
               'All percentages share post-start-line time to last recorded unfinished-car sample. '
               'Intervals over 0.25 s are excluded; no extrapolated finish tail. Context is controller state, not causal proof. '
               'Below-solo flag means braking at least 2 m/s below a non-braking matched solo at that road location. '
               'Traffic, lateral path and vehicle state can justify it. Samples cannot establish sub-frame control smoothness.',
               folder=str(folder), soloFolders={k:str(v.folder) for k,v in solos.items()},
               eligibleCarSeconds=seconds, aggregatePercent=aggregate, cars=results)
    (folder/'driving-duty.json').write_text(json.dumps(out,indent=2)+'\n')
    lines=['# Driving duty audit','',out['scope'],'',
           '| Car | Parts | Finish | Brake | On line | Full throttle | Below-solo braking |',
           '|---|---|---:|---:|---:|---:|---:|']
    for c in sorted(results,key=lambda c:c['place']):
        t=c['totals']
        lines.append(f"| {c['id']} | {c['equipment']} | {c['finishSeconds']:.2f} s | {t['brakingPercent']:.1f}% | {t['onLinePercent']:.1f}% | {t['fullThrottlePercent']:.1f}% | {t['brakingBelowNonBrakingSoloSeconds']:.2f} s |")
    (folder/'driving-duty.md').write_text('\n'.join(lines)+'\n')
    return out


if __name__ == '__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('folder', type=Path)
    parser.add_argument('--solo', type=Path, action='append', default=[])
    args=parser.parse_args()
    result=audit(args.folder.resolve(), [p.resolve() for p in args.solo])
    print(json.dumps(result['aggregatePercent'],indent=2))
