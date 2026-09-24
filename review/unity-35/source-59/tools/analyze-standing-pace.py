"""Compare full standing laps with/without traffic from the same rear-grid pose."""
import argparse
import json
from collections import Counter
from pathlib import Path
from runtime_log_errors import runtime_error_lines

read = lambda p: json.loads(p.read_text(encoding='utf-8-sig'))


def inspect(folder):
    race, track, launch = (read(folder / n) for n in ('race.json', 'race.track.json', 'launch.json'))
    car = next(c for c in race['cars'] if c['id'] == 0)
    samples = [s for s in track['samples'] if s['id'] == 0 and 0 <= s['time'] <= car['time']]
    by_time = {s['time']: s for s in samples}
    gates = {round(e['requiredProgress']/(race['trackLength']/12)): e['time'] for e in race['checkpointPassages'] if e['id'] == 0}
    counts, brake_sources = Counter(), Counter()
    for p in track['pedalSamples']:
        if p['id'] != 0 or p['time'] not in by_time:
            continue
        s = by_time[p['time']]
        if s['speed'] < 2 or s['recoveryState'] not in ('Racing', 'Disabled'):
            continue
        counts['eligible'] += 1
        if abs(p['decisionTimestamp']-p['sampleTimestamp']) > 1e-6 or abs(p['applied']-s['pedal']) > 1e-6:
            counts['mismatched'] += 1
            continue
        counts['matched'] += 1
        source = 'follower'
        for before, after, name in [('follower','leader','moving leader'), ('leader','scenery','scenery'), ('scenery','applied','immediate safety')]:
            if p[after] < p[before]-1e-6:
                source = name
        if p['applied'] < -.01:
            counts['braking'] += 1
            brake_sources[source] += 1
            if p.get('trafficUnresolved'):
                counts['brakingWhilePathUnresolved'] += 1
    return dict(car=car, gates=gates, firstSample=samples[0],
                inputOwners=sorted({s['inputOwner'] for s in samples}),
                outsideBodySamples=sum(s['maxBodyOffset'] > race['trackWidth']/2 for s in samples),
                maximumBodyOffset=max(s['maxBodyOffset'] for s in samples),
                pedalCounts=dict(counts), brakeSources=dict(brake_sources),
                runtimeErrors=runtime_error_lines(folder), normalExit=read(folder/'exit.json')['exitCode'] == 0,
                tuning=track['tuning'], planning=next(p for p in track['driverPlanning'] if p['id'] == 0),
                configuration={k:launch[k] for k in ('seed','grid','racingLine','loadout','driverRisk','driverBraking','physicalRecovery',
                    'roadFeasibility','preparedPath','parallelForecastWorkers','guidePathFallback','coastWithinEnvelope',
                    'noGapCentres','trafficPathCheck','anticipatePassingSpace','trafficCoastContinuation','compareContinuation','trafficAwareProgress')},
                track={k:race[k] for k in ('trackId','trackVersion','trackLength','trackWidth','physicsStepSeconds','requiredLaps')})


parser = argparse.ArgumentParser()
parser.add_argument('root', type=Path)
args = parser.parse_args()
solo, field = (inspect(args.root/name) for name in ('standing-solo','field'))
checks = dict(sameConfiguration=solo['configuration'] == field['configuration'], sameTuning=solo['tuning'] == field['tuning'],
              sameTrack=solo['track'] == field['track'], sameDriverPlanning=solo['planning'] == field['planning'],
              sameInitialPose=all(solo['firstSample'][k] == field['firstSample'][k] for k in ('position','forward','progress','roadOffset')),
              soloRearGridSlot=read(args.root/'standing-solo/starting-grid.json')['entries'][0]['slot'] == 23)
for name, run in [('solo',solo),('field',field)]:
    checks[name+'FinishedAllGates'] = run['car']['finished'] and set(run['gates']) == set(range(13)) and run['car']['missedGates'] == 0
    checks[name+'Autonomous'] = run['inputOwners'] == ['AI']
    checks[name+'NormalExit'] = run['normalExit']
    checks[name+'NoRuntimeErrors'] = not run['runtimeErrors']
    checks[name+'NoRecovery'] = run['car']['recoveries'] == run['car']['physicalRecoveries'] == 0
    checks[name+'MatchedPedalStages'] = run['pedalCounts'].get('matched',0)>0 and run['pedalCounts'].get('mismatched',0)==0
checks['soloNoContactOrDeparture'] = solo['car']['contacts'] == solo['outsideBodySamples'] == 0
rows=[]
for gate in range(13):
    a,b=solo['gates'].get(gate),field['gates'].get(gate)
    cumulative=None if a is None or b is None else b-a
    prior=0 if gate==0 else rows[-1]['fieldDelaySeconds']
    rows.append(dict(gate=gate,soloSeconds=a,fieldSeconds=b,fieldDelaySeconds=cumulative,
                     segmentDelaySeconds=None if cumulative is None or prior is None else cumulative-prior))
out=dict(scope='Matched car0 with/without traffic from the same standing rear-grid pose. Delay localises opportunity cost; it does not prove individual braking actions unnecessary. Human edge use differs from AI road limits. Pedal counts use same-tick moving10Hz samples; they are not physics-tick duty percentages.',
         checks=checks, passed=all(checks.values()), solo=solo, field=field, checkpoints=rows,
         totalTrafficCostSeconds=field['car']['time']-solo['car']['time'])
(args.root/'analysis.json').write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8')
print(json.dumps(dict(passed=out['passed'],failed=[k for k,v in checks.items() if not v],soloSeconds=solo['car']['time'],fieldSeconds=field['car']['time'],trafficCost=out['totalTrafficCostSeconds'],checkpoints=rows,pedal={n:{k:r[k] for k in ('pedalCounts','brakeSources')} for n,r in [('solo',solo),('field',field)]}),indent=2))
raise SystemExit(0 if out['passed'] else 1)
