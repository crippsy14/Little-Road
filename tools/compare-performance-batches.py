"""Compare matched actual-car fixtures across builds; preserve incompatible cases.

Use repeated --baseline folders to combine disjoint earlier loadouts. This is
for deterministic equipment/reference-policy regressions, not AI confidence.
"""
import argparse
import json
import statistics
from collections import defaultdict
from pathlib import Path

parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('current',type=Path)
parser.add_argument('--baseline',type=Path,action='append',required=True)
args=parser.parse_args()

def read(folder):
    batch=json.loads((folder/'batch.json').read_text(encoding='utf-8-sig'))
    if not batch.get('complete') or not batch.get('runs'):
        raise ValueError(f'Incomplete or empty batch: {folder}')
    return batch['runs']

def key(run):
    return run['course'],run['loadout'],run['repeat']

def index(runs):
    result={}
    for run in runs:
        if key(run) in result: raise ValueError(f'Duplicate comparison case: {key(run)}')
        result[key(run)]=run
    return result

current=index(read(args.current))
baseline=index([run for folder in args.baseline for run in read(folder)])
checks=[]
def check(name,passed): checks.append(dict(name=name,passed=bool(passed)))
check('Every supplied baseline run is retained in the current batch',set(baseline)<=set(current))
groups=defaultdict(list)
for case,new in current.items():
    old=baseline.get(case)
    if old is None: continue
    comparable=all(new[field]==old[field] for field in
        ['fixtureVersion','surface','body','physicsStep','routeLength','straightLength','turnRadius','roadWidth','tuning','seed'])
    comparable &= {p['slot']:p['id'] for p in new['fitted']}=={p['slot']:p['id'] for p in old['fitted']}
    comparable &= new['valid'] and old['valid'] and new['completed'] and old['completed']
    check('/'.join(map(str,case))+': matched valid physical configuration',comparable)
    if not comparable: continue
    if case[0] in ('straight','brake'):
        check('/'.join(map(str,case))+': unchanged control measurement',
              abs(new['time']-old['time'])<=new['physicsStep'] and
              abs(new['peakSpeed']-old['peakSpeed'])<.02 and
              abs(new['travelledDistance']-old['travelledDistance'])<.05)
    groups[case[:2]].append(dict(beforeSeconds=old['time'],afterSeconds=new['time'],
        savedSeconds=old['time']-new['time'],beforeOffset=old['maxAbsOffset'],afterOffset=new['maxAbsOffset'],
        beforeDriver=old['driver'],afterDriver=new['driver']))

rows=[]
for (course,loadout),runs in groups.items():
    rows.append(dict(course=course,loadout=loadout,repeats=len(runs),
        beforeSeconds=statistics.median(r['beforeSeconds'] for r in runs),
        afterSeconds=statistics.median(r['afterSeconds'] for r in runs),
        savedSeconds=statistics.median(r['savedSeconds'] for r in runs),
        beforeMaxOffset=max(r['beforeOffset'] for r in runs),afterMaxOffset=max(r['afterOffset'] for r in runs),
        beforeDriver=runs[0]['beforeDriver'],afterDriver=runs[0]['afterDriver']))

result=dict(passed=all(c['passed'] for c in checks),current=str(args.current.resolve()),
    baselines=[str(p.resolve()) for p in args.baseline],matchedRuns=sum(len(v) for v in groups.values()),
    newCasesWithoutBaseline=[list(case) for case in current if case not in baseline],checks=checks,rows=rows)
(args.current/'baseline-comparison.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
lines=['# Matched comparison with earlier build','',
    f"Validation: **{'PASS' if result['passed'] else 'ATTENTION REQUIRED'}**; {result['matchedRuns']} matched runs. "
    f"{len(result['newCasesWithoutBaseline'])} additional current cases have no supplied baseline and receive no before/after claim.",'',
    'Positive savings mean the current reference driver finished sooner. No improvement in total time is required for every setup. '
    'Straight and brake controls must retain their earlier measurements; changed U-turn policy is assessed separately. '
    'This does not establish natural AI variance, human acceptance or whole-game balance.','',
    '| Course | Loadout | Earlier seconds | Current seconds | Saved seconds | Maximum offset, earlier → current |',
    '| --- | --- | ---: | ---: | ---: | ---: |']
for row in rows:
    lines.append(f"| {row['course']} | {row['loadout']} | {row['beforeSeconds']:.4f} | {row['afterSeconds']:.4f} | {row['savedSeconds']:.4f} | {row['beforeMaxOffset']:.3f} → {row['afterMaxOffset']:.3f} m |")
lines+=['','## Failed checks','']+[c['name'] for c in checks if not c['passed']]
if result['passed']: lines+=['None.']
(args.current/'baseline-comparison.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
print(json.dumps(dict(passed=result['passed'],matchedRuns=result['matchedRuns'],checks=len(checks),failed=[c for c in checks if not c['passed']]),indent=2))
raise SystemExit(0 if result['passed'] else 2)
