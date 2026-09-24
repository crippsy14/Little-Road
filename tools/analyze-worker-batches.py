"""Separate worker-batch latency/parity from physical and live-racing acceptance."""
import argparse
import json
import math
from pathlib import Path

p = argparse.ArgumentParser()
p.add_argument('folders', nargs='+', type=Path)
p.add_argument('--previous-root', type=Path, required=True)
p.add_argument('--output', type=Path, required=True)
args = p.parse_args()
read = lambda path: json.loads(path.read_text(encoding='utf-8-sig'))
checks = {}
cases = {}
serial = []
concurrent = []
worker = []
for root in args.folders:
    manifest = read(root / 'manifest.json')
    report = read(root / 'analysis.json')
    checks[root.name + ': all declared cases completed'] = manifest['complete']
    checks[root.name + ': physical and worker checks pass'] = report['passed'] == report['total']
    for run in manifest['runs']:
        name = run['name']
        data = read(root / name / 'race.path-prediction.json')
        launch = read(root / name / 'launch.json')
        batch = data['batchProbe']
        key = root.name + '/' + name
        if name.startswith('start-'):
            previous = 'approaches-30-prepared' if int(name.split('-')[1]) in (1, 2) else 'heldout-30-prepared'
        else:
            previous = 'probes-30-prepared'
        old = read(args.previous_root / previous / name / 'race.path-prediction.json')
        checks[key + ': physical and primary forecast samples unchanged'] = old['samples'] == data['samples'] and all(old[k] == data[k] for k in ('actualDeparture', 'predictedDeparture', 'contactsBefore', 'contactsAfter'))
        checks[key + ': declared worker request'] = launch.get('forecastWorkers') == batch['workers'] and batch['jobs'] == 24
        checks[key + ': exact concurrent forecast outputs'] = batch['exactParity'] is True
        warm_serial = batch['serialMs'][1:]
        warm_concurrent = batch['concurrentMs'][1:]
        warm_worker = batch['workerElapsedMs'][1:]
        serial.extend(warm_serial)
        concurrent.extend(warm_concurrent)
        worker.extend(warm_worker)
        cases[key] = dict(workers=batch['workers'], serialWarmMs=sum(warm_serial)/3, concurrentWarmMs=sum(warm_concurrent)/3,
                          summedWorkerElapsedMs=sum(warm_worker)/3, initializationMs=batch['initializationMs'],
                          serialWarmBytes=sum(batch['serialBytes'][1:]), mainWarmBytes=sum(batch['mainBytes'][1:]), workerWarmBytes=sum(batch['workerBytes'][1:]))

def distribution(values):
    values = sorted(values)
    return dict(mean=sum(values)/len(values), p95=values[math.ceil(.95*len(values))-1], maximum=values[-1])

out = dict(scope='Sequential hidden physical fixtures; each has serial/concurrent 24-request batches. One cold round retained in raw data, three warm rounds summarized. Summed worker elapsed time is not CPU utilization. No live planning dispatch/commit, scene queries or rendered-frame acceptance.',
           checks=checks, passed=all(checks.values()), cases=cases, warmBatches=len(serial),
           serialBatchMs=distribution(serial), concurrentBatchMs=distribution(concurrent), summedWorkerElapsedMs=distribution(worker),
           warmLatencyReductionPercent=100*(1-sum(concurrent)/sum(serial)))
args.output.write_text(json.dumps(out, indent=2)+'\n', encoding='utf-8')
print(json.dumps({k:v for k,v in out.items() if k not in ('cases', 'checks')}, indent=2))
print('Failures:', json.dumps([k for k,v in checks.items() if not v]))
raise SystemExit(0 if out['passed'] else 1)
