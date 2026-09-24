"""Preview-car checkpoint rank and equipment use; observational, never a forced finish target."""
import argparse
import json
from collections import Counter
from pathlib import Path


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def rank_range(values, own):
    """Equal physics-tick crossings remain tied; ID/order must not break ties."""
    return [1 + sum(v < own for v in values), sum(v <= own for v in values)]


def analyze(folder, car_id=0, solo_root=None):
    race = read(folder / 'race.json')
    track = read(folder / 'race.track.json')
    if race['requiredLaps'] != 1:
        raise ValueError('Use a one-lap field recording; later laps require separate interval accounting.')
    cars = {c['id']: c for c in race['cars']}
    car = cars[car_id]
    frames = {}
    for sample in track['samples']:
        frames.setdefault(sample['time'], {})[sample['id']] = sample
    frames = sorted(frames.items())
    initial = next(frame for _, frame in frames if len(frame) == len(cars))
    start_rank = rank_range([-s['progress'] for s in initial.values()], -initial[car_id]['progress'])
    passages = {}
    for event in race['checkpointPassages']:
        ordinal = round(event['requiredProgress'] / (race['trackLength'] / 12))
        if 0 <= ordinal <= 12:
            passages.setdefault(ordinal, {}).setdefault(event['id'], event['time'])
    rows = []
    for ordinal in range(13):
        events = passages.get(ordinal, {})
        time = events.get(car_id)
        rank = None if time is None else rank_range(events.values(), time)
        rows.append(dict(ordinal=ordinal, time=time, crossingRankRange=rank,
                         positionsGainedRange=None if rank is None else
                         [start_rank[0]-rank[1], start_rank[1]-rank[0]],
                         recordedEntrants=len(events), expectedEntrants=len(cars)))

    # Club/street comparison is deliberately specific to this development field.
    # Arbitrary equipment names require matched solo evidence, not a guessed hierarchy.
    slower = [i for i, c in cars.items() if car['equipment'] == 'club' and c['equipment'] == 'street']
    eligible = [i for i in slower if initial[i]['progress'] - initial[car_id]['progress'] > 7]
    clear_since = {i: None for i in eligible}
    passes = {}
    suspect_seconds = 0.0
    contexts = {s['time']: s for s in track.get('contextSamples', []) if s['id'] == car_id}
    last_time = None
    for index, (time, frame) in enumerate(frames):
        if car_id not in frame:
            continue
        own = frame[car_id]
        if car['finished'] and time > car['time']:
            break
        continuous = last_time is None or time-last_time <= .16
        for other in eligible:
            if other in passes:
                continue
            clear = (continuous and other in frame and own['progress']-frame[other]['progress'] > 7
                     and own['recoveryState'] in ('Racing', 'Disabled')
                     and frame[other]['recoveryState'] in ('Racing', 'Disabled')
                     and not cars[other]['recoveries'] and not car['recoveries'])
            clear_since[other] = (time if clear_since[other] is None else clear_since[other]) if clear else None
            if clear_since[other] is not None and time-clear_since[other] >= 2:
                passes[other] = time
        context = contexts.get(time)
        leader_id = context['leader'] if context else -1
        if leader_id in slower and leader_id in frame:
            leader = frame[leader_id]
            gap = leader['progress'] - own['progress']
            if (0 < gap < 30 and own['speed'] > 2 and abs(own['speed']-leader['speed']) <= 1.5
                    and own['pedal'] <= .05 and own['recoveryState'] in ('Racing', 'Disabled')):
                next_time = frames[index+1][0] if index+1 < len(frames) else time
                suspect_seconds += max(0, min(.1, next_time-time, car['time']-time if car['finished'] else .1))
        last_time = time
    solo = {}
    if solo_root:
        for equipment in ('street', 'club'):
            sr = read(solo_root / (equipment+'-neutral') / 'race.json')
            if (sr['trackId'], sr['trackVersion'], sr['driverPolicy']) != (race['trackId'], race['trackVersion'], race['driverPolicy']):
                raise ValueError('Solo track or driver policy mismatch')
            if sr['carCount'] != 1 or sr['cars'][0]['equipment'] != equipment:
                raise ValueError('Solo equipment/entrant mismatch')
            gates = sorted(e['time'] for e in sr['checkpointPassages'] if e['id'] == 0 and e['gate'] == 0 and e['requiredProgress'] > 0)
            laps = [b-a for a,b in zip(gates, gates[1:])]
            if not laps:
                raise ValueError('No complete flying laps in solo evidence')
            solo[equipment] = dict(flyingLaps=laps, meanSeconds=sum(laps)/len(laps))
    out = dict(carId=car_id, equipment=car['equipment'], fieldEquipment=dict(Counter(c['equipment'] for c in cars.values())),
               startingRankRange=start_rank, finalPlace=car['place'] if car['finished'] else None,
               checkpoints=rows, slowerEquipmentIds=slower,
               sustainedPasses=[dict(other=i, confirmedAt=t) for i,t in sorted(passes.items(), key=lambda p:p[1])],
               possibleHeldBehindSlowerSeconds=suspect_seconds, soloEvidence=solo,
               limits='Checkpoint crossing order, not interpolated live HUD position; same-tick ties retained. First lap only. '
               'Pass = initially >7m behind, then >7m ahead for 2s at 10Hz without reset; contact not excluded. '
               'Held-behind proxy = named street leader <30m ahead, speeds within 1.5m/s, own speed >2m/s and pedal <=0.05. '
               'This does not prove a usable gap or unnecessary braking; cornering may justify it. '
               'Solo flying laps establish equipment potential, not an expected rear-grid checkpoint rank. No hard acceptance threshold yet.')
    (folder / 'preview-car-analysis.json').write_text(json.dumps(out, indent=2)+'\n', encoding='utf-8')
    lines = ['# Preview-car progress', '', f"Car {car_id}: {car['equipment']}; starts {start_rank}, finishes {out['finalPlace']}.", '',
             '| Checkpoint | Seconds after green | Rank (ties retained) |', '|---|---:|---:|']
    for row in rows:
        lines.append(f"| {row['ordinal']} | {row['time']} | {row['crossingRankRange']} |")
    lines += ['', f"Sustained passes of initially leading street cars: {len(passes)}. Possible held-behind time: {suspect_seconds:.2f}s.", '', out['limits']]
    (folder / 'preview-car-analysis.md').write_text('\n'.join(lines)+'\n', encoding='utf-8')
    return out


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('folder', type=Path)
    parser.add_argument('--car-id', type=int, default=0)
    parser.add_argument('--solo-root', type=Path)
    args = parser.parse_args()
    result = analyze(args.folder, args.car_id, args.solo_root)
    print(json.dumps(result, indent=2))
