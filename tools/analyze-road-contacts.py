"""Summarize sampled solver impulses separately from scored racing incidents."""
import argparse
import json
import math
from pathlib import Path

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('folder', type=Path)
parser.add_argument('--compare', type=Path)
args = parser.parse_args()
track = json.loads((args.folder / 'race.track.json').read_text())
rows = track['samples']
if not rows or 'contactImpulse' not in rows[0]:
    raise SystemExit('This run predates solver-contact telemetry.')

events = []
maximum_road_impulse = 0
for s in rows:
    p = s['contactImpulse']
    planar = math.hypot(p['x'], p['z'])
    if s.get('contactSurface') != 'Continuous drivable support':
        continue
    maximum_road_impulse = max(maximum_road_impulse, planar)
    if planar > 1:
        events.append(dict(id=s['id'], time=s['time'], progress=s['progress'],
                           speed=s['speed'], planarDeltaV=planar,
                           offset=s['roadOffset'], impulse=p))

departures = []
for car in track['results']:
    if not car['outsideBodySamples']:
        continue
    outside = [s for s in rows if s['id'] == car['id'] and s['maxBodyOffset'] > track['width'] / 2]
    first = outside[0]
    preceding = [e for e in events if e['id'] == car['id'] and 0 <= first['time'] - e['time'] <= 2]
    departures.append(dict(id=car['id'], firstTime=first['time'],
                           samples=len(outside), maximum=car['maxBodyOffset'],
                           precedingRoadImpulses=preceding))

comparison = None
if args.compare:
    previous = json.loads((args.compare / 'race.track.json').read_text())['samples']
    keys = ('id', 'time', 'position', 'speed', 'progress', 'pedal', 'steer', 'trafficIntent')
    mismatch = next((i for i, (a, b) in enumerate(zip(previous, rows)) if any(a[k] != b[k] for k in keys)), None)
    comparison = dict(folder=str(args.compare), fields=keys,
                      previousSamples=len(previous), currentSamples=len(rows),
                      firstMismatch=mismatch, exact=mismatch is None and len(previous) == len(rows))

out = dict(scope='10 Hz observations of the strongest single solver impulse per sampling interval, divided by mass. Not summed impulses or complete contact history. The 1 m/s planar threshold flags investigation, not a pass/fail gate.',
           comparison=comparison, strongRoadImpulseSamples=len(events),
           maximumPlanarRoadImpulse=maximum_road_impulse,
           roadImpulseEvents=events, bodyDepartures=departures)
(args.folder / 'road-contact-analysis.json').write_text(json.dumps(out, indent=2) + '\n')
print(json.dumps({k: v for k, v in out.items() if k not in ('roadImpulseEvents', 'bodyDepartures')}, indent=2))
print('Body departures:', [(d['id'], d['samples'], len(d['precedingRoadImpulses'])) for d in departures])
