"""Compare an observation-only forecast experiment against its exact-mode control."""
import argparse
import json
from pathlib import Path

p = argparse.ArgumentParser()
p.add_argument('baseline', type=Path)
p.add_argument('candidate', type=Path)
p.add_argument('--output', type=Path, required=True)
args = p.parse_args()
read = lambda f: json.loads(f.read_text(encoding='utf-8-sig'))
am, bm = (read(root / 'manifest.json') for root in (args.baseline, args.candidate))
ae, be = (read(root / 'expectations.json') for root in (args.baseline, args.candidate))
names = [r['name'] for r in am['runs']]
checks = dict(completeManifests=am['complete'] and bm['complete'], sameCases=names == [r['name'] for r in bm['runs']])
checks['matchedExpectationsExceptPreparedMode'] = {k:v for k,v in ae.items() if k != 'preparedPath'} == {k:v for k,v in be.items() if k != 'preparedPath'}
for key, root in [('baseline', args.baseline), ('candidate', args.candidate)]:
    analysis = read(root / 'analysis.json')
    checks[key + 'PhysicalChecksPass'] = analysis['passed'] == analysis['total']
cases = {}
actual_keys = ('time', 'actualPosition', 'actualPedal', 'actualSteering', 'actualBodyOffset', 'flatTarmacDomain')
for name in names:
    a, b = (read(root / name / 'race.path-prediction.json') for root in (args.baseline, args.candidate))
    al, bl = (read(root / name / 'launch.json') for root in (args.baseline, args.candidate))
    actual = lambda data: [{k:row[k] for k in actual_keys} for row in data['samples']]
    checks[name + ': actual physics and controls identical'] = actual(a) == actual(b) and all(a[k] == b[k] for k in ('contactsBefore', 'contactsAfter', 'actualDeparture'))
    checks[name + ': same assembly'] = bool(al['assemblyHash']) and al['assemblyHash'] == bl['assemblyHash']
    mean = lambda data, key: sum(c.get(key, 0) for c in data['forecastCosts'][1:]) / 3
    cases[name] = dict(baselineWarmMs=mean(a, 'totalMs'), candidateWarmMs=mean(b, 'totalMs'),
                       candidatePreparationMs=mean(b, 'preparationMs'), baselineFollowerMs=mean(a, 'followerMs'), candidateFollowerMs=mean(b, 'followerMs'),
                       preparedHits=b.get('preparedHits'), preparedMisses=b.get('preparedMisses'))
old = sum(c['baselineWarmMs'] for c in cases.values()) / len(cases)
new = sum(c['candidateWarmMs'] for c in cases.values()) / len(cases)
out = dict(scope='Paired hidden fixture forecasts; total includes preparation, native projection/physics and envelope. Not rendered-frame acceptance or live selection safety.',
           checks=checks, passed=all(checks.values()), cases=cases, baselineWarmMeanMs=old, candidateWarmMeanMs=new, warmReductionPercent=100*(1-new/old))
args.output.write_text(json.dumps(out, indent=2)+'\n', encoding='utf-8')
print(json.dumps(out, indent=2))
raise SystemExit(0 if out['passed'] else 1)
