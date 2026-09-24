"""Describe manual field attempts against the same-car AI reference, without grading human driving."""
import argparse
import json
from pathlib import Path
from runtime_log_errors import runtime_error_lines


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def summarize(folder):
    race, track, launch = (read(folder / name) for name in ('race.json', 'race.track.json', 'launch.json'))
    own = next(c for c in race['cars'] if c['id'] == 0)
    end = own['time'] if own['finished'] else race['simSeconds']
    samples = sorted((s for s in track['samples'] if s['id'] == 0 and 0 <= s['time'] <= end), key=lambda s: s['time'])
    durations = dict(observed=0., braking=0., coasting=0., accelerating=0., outside=0.)
    speed_time = 0.
    for a, b in zip(samples, samples[1:]):
        dt = b['time'] - a['time']
        if not 0 < dt <= .16:
            continue
        durations['observed'] += dt
        durations['braking' if a['pedal'] < -.05 else 'accelerating' if a['pedal'] > .05 else 'coasting'] += dt
        durations['outside'] += dt * (a['maxBodyOffset'] > race['trackWidth'] / 2)
        speed_time += a['speed'] * dt
    passages = [e for e in race['checkpointPassages'] if e['id'] == 0]
    checkpoints = {round(e['requiredProgress'] / (race['trackLength'] / 12)): e['time'] for e in passages}
    impacts = [e for e in race['impacts'] if e['id'] == 0]
    return dict(folder=str(folder), car=own, autonomous=race['autonomous'], sessionType=track['sessionType'],
                inputOwners=sorted({s['inputOwner'] for s in samples}),
                finishersAtReport=race['finished'], fieldSize=race['carCount'],
                fullFieldResultsAvailable=race['finished'] == race['carCount'],
                checkpointTimes=checkpoints, measuredSeconds=durations['observed'],
                dutyPercent={k: 100*v/durations['observed'] if durations['observed'] else None for k, v in durations.items() if k != 'observed'},
                meanSampledSpeedMps=speed_time/durations['observed'] if durations['observed'] else None,
                peakSampledSpeedMps=max((s['speed'] for s in samples), default=None),
                maximumSampledBodyOffset=max((s['maxBodyOffset'] for s in samples), default=None),
                playerContactEvents=len(impacts), contactedCarIds=sorted({e['other'] for e in impacts if e['other'] >= 0}),
                runtimeErrors=runtime_error_lines(folder),
                configuration=dict(trackId=race['trackId'], trackVersion=race['trackVersion'],
                    physicsStep=race['physicsStepSeconds'], carCount=race['carCount'], requiredLaps=race['requiredLaps'],
                    equipment=[c['equipment'] for c in race['cars']], tuning=track['tuning'],
                    **{k: launch[k] for k in ('seed','grid','racingLine','trafficCase','driverRisk','driverBraking',
                        'physicalRecovery','trafficPathCheck','anticipatePassingSpace','trafficCoastContinuation',
                        'compareContinuation','trafficAwareProgress')}))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('attempts', nargs='+', type=Path)
    parser.add_argument('--baseline', required=True, type=Path)
    parser.add_argument('--output', required=True, type=Path)
    args = parser.parse_args()
    baseline = summarize(args.baseline)
    attempts = []
    for folder in args.attempts:
        attempt = summarize(folder)
        attempt['sameConfigurationAsBaseline'] = attempt['configuration'] == baseline['configuration']
        attempt['secondsFasterThanAiReference'] = (baseline['car']['time'] - attempt['car']['time']) if baseline['car']['finished'] and attempt['car']['finished'] else None
        attempt['checkpointSecondsAheadOfAiReference'] = {k: baseline['checkpointTimes'][k]-v for k, v in attempt['checkpointTimes'].items() if k in baseline['checkpointTimes']}
        attempts.append(attempt)
    result = dict(scope='Observational same-car field comparison. Human traffic interactions change rivals. A single attempt does not establish campaign balance or optimal pace. Contact events can repeat during one encounter. Outside duty is sampled body-envelope occupancy, not an automatic invalid-lap judgement. Reports saved at human finish omit later rival finish times.',
                  baseline=baseline, attempts=attempts)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    print(json.dumps([dict(folder=a['folder'], place=a['car']['place'], time=a['car']['time'],
                          faster=a['secondsFasterThanAiReference'], sameConfiguration=a['sameConfigurationAsBaseline'],
                          inputOwners=a['inputOwners'], dutyPercent=a['dutyPercent']) for a in attempts], indent=2))


if __name__ == '__main__':
    main()
