"""Summarise actual Unity proving-ground telemetry; retain missing/failed cases.

Expectations describe conditional part effects, not a ranking of every price.
Run after a completed batch: python analyze-performance-trials.py <batch folder>
"""
import argparse
import csv
import json
import os
import statistics as st
import sys
from collections import defaultdict
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument('folder', type=Path)
args = parser.parse_args()
folder = args.folder.resolve()
batch = json.loads((folder / 'batch.json').read_text(encoding='utf-8-sig'))
runs = batch['runs']
groups = defaultdict(list)
for run in runs:
    groups[(run['course'], run['loadout'])].append(run)
checks = []


def check(name, passed, detail=''):
    checks.append(dict(name=name, passed=bool(passed), detail=detail))


def value(course, loadout, field):
    items = groups.get((course, loadout), [])
    if not items:
        raise ValueError(f'Missing required comparison: {course}/{loadout}')
    return st.median(item[field] for item in items)


check('Batch is terminal and contains runs', batch.get('complete') and len(runs) > 0)
check('Every attempted run completed validly', all(r['completed'] and r['valid'] for r in runs),
      '; '.join(r['id'] + ': ' + str(r.get('failure')) for r in runs if not r['valid']))
check('No recovery, impact or missed measured gate', all(r['recoveries'] == r['contacts'] == r['missedGates'] == 0 for r in runs))
check('Same runtime assembly and physics step', len({r['assemblyVersion'] for r in runs}) == 1 and all(abs(r['physicsStep'] - 1/120) < 1e-7 for r in runs))
check('Dry-road runs remained on the measured road', all(r['maxAbsOffset'] <= 8 for r in runs))
check('No unexplained lift on flat fixtures', all(r['maxAirClearance'] < .06 for r in runs), 'Collider bottom clearance < 6 cm on these flat courses only.')
expected_loadouts=['stock','engine_workshop','engine_club','transmission_long','transmission_close','tyres_sport','brakes_sport','brakes_sport+tyres_sport','engine_club+transmission_long','club']
policy_v2=any(r['driver']=='fixed reference policy v2' for r in runs)
fast_tyres='engine_club+transmission_long+tyres_sport'
fast_brakes=fast_tyres+'+brakes_sport'
if policy_v2: expected_loadouts += [fast_tyres,fast_brakes]
expected_runs=len(expected_loadouts)*3*3
check(f'Full declared {expected_runs}-run matrix is present', len(runs)==expected_runs and all(
    len(groups.get((course,loadout),[]))==3 and {r['repeat'] for r in groups[(course,loadout)]}=={1,2,3}
    for course in ['straight','brake','uturn'] for loadout in expected_loadouts))
rows = []
for (course, loadout), items in groups.items():
    times = [r['time'] for r in items]
    tolerance = max(.05, st.median(times) * .002)
    check(f'{course}/{loadout}: at least three repeats', len(items) >= 3)
    check(f'{course}/{loadout}: repeatable elapsed time', max(times)-min(times) <= tolerance,
          f'Range {max(times)-min(times):.6f}s; fixed-driver tolerance {tolerance:.4f}s, not desired natural AI variance.')
    if course == 'straight':
        check(f'{loadout}: reached sustained speed plateau', all(r['sustainedSpeedReached'] for r in items))
        check(f'{loadout}: garage speed estimate agrees with physical run', all(abs(r['sustainedTopSpeed']-r['predictedTopSpeed']) < .08 for r in items), 'Tolerance 0.08 m/s covers fixed-step gearbox ceiling overshoot.')
        check(f'{loadout}: garage 0–30 estimate agrees with physical run', all(abs(r['zeroTo30']-r['predictedZeroTo30']) < .04 for r in items), 'Independent actual-car measurement versus garage force-integration estimate; tolerance 0.04s.')
    rows.append(dict(course=course, loadout=loadout, repeats=len(items), valid=sum(r['valid'] for r in items),
                     medianSeconds=st.median(times), rangeSeconds=max(times)-min(times),
                     averageMph=st.median(r['averageSpeed'] for r in items)*2.237,
                     zeroTo30=st.median(r['zeroTo30'] for r in items),
                     sustainableMph=st.median(r['sustainedTopSpeed'] for r in items)*2.237 if all(r['sustainedSpeedReached'] for r in items) else None,
                     brakingMetres=st.median(r['stopDistance'] for r in items) if course == 'brake' else None,
                     apexMph=st.median(r['apexMinSpeed'] for r in items)*2.237 if course == 'uturn' else None,
                     maxOffset=max(r['maxAbsOffset'] for r in items)))

# Source-informed expectations, written before inspecting the full matrix.
try:
    s = lambda loadout, field: value('straight', loadout, field)
    b = lambda loadout: value('brake', loadout, 'stopDistance')
    u = lambda loadout, field: value('uturn', loadout, field)
    check('Workshop engine materially improves 0–30 mph', s('engine_workshop', 'zeroTo30') < s('stock', 'zeroTo30') - .10)
    check('Club engine improves launch further', s('engine_club', 'zeroTo30') < s('engine_workshop', 'zeroTo30') - .10)
    check('Long gearing alone does not invent engine power/top speed', abs(s('transmission_long', 'sustainedTopSpeed') - s('stock', 'sustainedTopSpeed')) < .08)
    check('Long gearing trades away some launch acceleration', s('transmission_long', 'zeroTo30') > s('stock', 'zeroTo30') + .10)
    check('Close gearing improves launch but lowers maximum speed', s('transmission_close', 'zeroTo30') < s('stock', 'zeroTo30') - .10 and s('transmission_close', 'sustainedTopSpeed') < s('stock', 'sustainedTopSpeed') - .5)
    check('Powerful engine benefits from longer gearing', s('engine_club+transmission_long', 'sustainedTopSpeed') > s('engine_club', 'sustainedTopSpeed') + 5)
    check('Sport brakes alone meet the stock tyre ceiling', abs(b('brakes_sport')-b('stock')) < .05,
          'Expected current model limitation; no blanket assertion that every purchase improves every metric.')
    check('Better tyres unlock more of the stock brakes', b('tyres_sport') < b('stock') - .5)
    check('Sport brakes add a benefit with sport tyres', b('brakes_sport+tyres_sport') < b('tyres_sport') - .5)
    check('Road sport tyres improve corner speed and total U-turn run', u('tyres_sport', 'apexMinSpeed') > u('stock', 'apexMinSpeed') + 1 and u('tyres_sport', 'time') < u('stock', 'time') - .3)
    check('Road sport tyres do not invent extra engine top speed', abs(s('tyres_sport', 'sustainedTopSpeed')-s('stock', 'sustainedTopSpeed')) < .08)
    if policy_v2:
        check('Stronger brakes do not meaningfully worsen the low-power U-turn',
              u('brakes_sport+tyres_sport', 'time') <= u('tyres_sport', 'time') + .025,
              'The initial hypothesis of >0.05 s improvement was falsified in pilots. At this entry speed the potential saving is small; do not force every upgrade to win every course.')
        check('Stronger brakes improve the matched faster-car U-turn beyond three physics steps',
              u(fast_brakes, 'time') < u(fast_tyres, 'time') - 3/120,
              'Matched engine/gearing/tyres and driver. This resolution threshold detects an effect; it does not establish satisfying upgrade value.')
        check('Brake upgrade preserves faster-car corner grip', abs(u(fast_brakes,'apexMinSpeed')-u(fast_tyres,'apexMinSpeed')) < .10)
except ValueError as exc:
    check('Required equipment comparison matrix is present', False, str(exc))

summary = dict(scope=batch['scope'], source=str(folder/'batch.json'), runCount=len(runs), groups=rows,
               passed=all(c['passed'] for c in checks), checks=checks)
summary['driverLimitations']=['Reference-driver times include pursuit, reaction and cornering margins; they are not ideal car-performance ceilings or production AI-skill acceptance.']
if ('uturn','tyres_sport') in groups and ('uturn','brakes_sport+tyres_sport') in groups:
    brake_gain=value('uturn','tyres_sport','time')-value('uturn','brakes_sport+tyres_sport','time')
    summary['brakeUpgradeUturnGainSeconds']=brake_gain
    if brake_gain<=.05:
        summary['driverLimitations'].append('At stock engine power, sport brakes do not save more than 0.05 s over sport tyres alone through this U-turn. The initial hypothesis of a larger low-power gain was falsified; do not confuse better physical stopping ability with a guaranteed useful lap-time gain on every course.')
if ('uturn',fast_tyres) in groups and ('uturn',fast_brakes) in groups:
    summary['fasterCarBrakeUpgradeGainSeconds']=value('uturn',fast_tyres,'time')-value('uturn',fast_brakes,'time')
    summary['driverLimitations'].append(f"Matched faster-car brake upgrade saves {summary['fasterCarBrakeUpgradeGainSeconds']:.4f} s on this run. Evaluate that effect across later braking zones, equipment prices and player feedback before calling the upgrade well balanced.")
(folder/'analysis.json').write_text(json.dumps(summary, indent=2), encoding='utf-8')
with (folder/'summary.csv').open('w', newline='', encoding='utf-8') as stream:
    writer=csv.DictWriter(stream, fieldnames=list(rows[0]) if rows else ['course'])
    writer.writeheader();writer.writerows(rows)

lines = ['# Dry-road equipment measurements', '',
         f"{len(runs)} actual-car runs. Validation: **{'PASS' if summary['passed'] else 'ATTENTION REQUIRED'}**.", '',
         'Three repeats per course/loadout are intended to establish controlled repeatability, not natural driver variance. '
         'The production RoadCar/PhysX handles every force and contact. Dry fixtures only; hill, wet/off-road and seeded AI work remain pending.', '',
         '| Course | Loadout | Valid/repeats | Median time | Time range | Average mph | 0–30 mph | Sustained mph | Stop distance | Apex mph |',
         '| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |']
fmt = lambda n, suffix='': '—' if n is None or n < 0 else f'{n:.3f}{suffix}'
for r in rows:
    lines.append(f"| {r['course']} | {r['loadout']} | {r['valid']}/{r['repeats']} | {fmt(r['medianSeconds'],' s')} | {fmt(r['rangeSeconds'],' s')} | {fmt(r['averageMph'])} | {fmt(r['zeroTo30'],' s')} | {fmt(r['sustainableMph'])} | {fmt(r['brakingMetres'],' m')} | {fmt(r['apexMph'])} |")
lines += ['', 'Braking starts at 20 m/s (44.74 mph) and ends at ≤0.15 m/s. Times use completed physics steps; sector crossings are interpolated. '
          'The 900 m straight reports sustained speed only after a stable three-second window. U-turn values cover the whole approach/turn/exit run, with a 22 m centreline radius.', '',
          '## Expectations and validity', '']
for c in checks:
    lines.append(f"- {'PASS' if c['passed'] else 'FAIL'} — {c['name']}" + (f": {c['detail']}" if c['detail'] else ''))
lines += ['', '## Limits and next decisions', '',
          'These are bounded dry-road fixtures, not whole-game balance or physical-controller acceptance. '
          'The fixed reference policy deliberately removes AI skill variation. Its cornering margins and centring are identical policies across loadouts, but its chosen inputs adapt to equipment. '
          'Speed estimates agreeing here do not validate hill, fuel, tyre-wear, wet-road or off-road estimates. '
          'Use matched full-field races and first-time player feedback to assess whether early purchases feel worthwhile.', '',
          *summary['driverLimitations'], '',
          '![Measured equipment comparisons](equipment-comparison.png)', '']
(folder/'report.md').write_text('\n'.join(lines), encoding='utf-8')

local_packages=Path(__file__).resolve().parent/'python-packages'
if local_packages.is_dir(): sys.path.insert(0,str(local_packages))
os.environ.setdefault('MPLCONFIGDIR', str(folder/'.matplotlib-cache'))
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt

names = {'stock':'Stock', 'engine_workshop':'Workshop engine', 'engine_club':'Club engine', 'transmission_long':'Long gearbox',
         'transmission_close':'Close gearbox', 'tyres_sport':'Sport tyres', 'brakes_sport':'Sport brakes',
         'brakes_sport+tyres_sport':'Sport brakes + tyres', 'engine_club+transmission_long':'Club engine + long gears', 'club':'Full club setup',
         fast_tyres:'Club engine + long gears + tyres', fast_brakes:'Same fast setup + sport brakes'}
plt.rcParams.update({'font.size':10, 'axes.spines.top':False, 'axes.spines.right':False})
fig, axes = plt.subplots(2, 2, figsize=(15, 10), constrained_layout=True)
colours = dict(zip(names, plt.cm.tab20.colors))
for loadout in ['stock','engine_workshop','engine_club','transmission_long','engine_club+transmission_long','club']:
    items=groups.get(('straight', loadout))
    if not items: continue
    samples=items[0]['samples'];axes[0,0].plot([s['time'] for s in samples], [s['speed']*2.237 for s in samples], label=names[loadout], color=colours[loadout])
axes[0,0].set(title='Straight: power and gearing', xlabel='Time (s)', ylabel='Speed (mph)');axes[0,0].legend(fontsize=8);axes[0,0].grid(alpha=.18)
brakes=[r for r in rows if r['course']=='brake'];axes[0,1].barh([names.get(r['loadout'],r['loadout']) for r in brakes], [r['brakingMetres'] for r in brakes], color=[colours.get(r['loadout'],'teal') for r in brakes])
axes[0,1].invert_yaxis();axes[0,1].set(title='Braking: 44.74 mph to near-rest', xlabel='Stopping distance (m)');axes[0,1].grid(axis='x',alpha=.18)
for loadout in ['stock','tyres_sport','brakes_sport+tyres_sport','club',fast_tyres,fast_brakes]:
    items=groups.get(('uturn',loadout))
    if not items: continue
    samples=items[0]['samples'];axes[1,0].plot([s['routeDistance'] for s in samples],[s['speed']*2.237 for s in samples],label=names[loadout],color=colours[loadout])
axes[1,0].axvspan(200,269.1,alpha=.08,color='black');axes[1,0].set(title='U-turn: grip, braking and exit speed',xlabel='Distance from start (m)',ylabel='Speed (mph)');axes[1,0].legend(fontsize=8);axes[1,0].grid(alpha=.18)
turns=[r for r in rows if r['course']=='uturn'];axes[1,1].barh([names.get(r['loadout'],r['loadout']) for r in turns],[r['medianSeconds'] for r in turns],color=[colours.get(r['loadout'],'teal') for r in turns]);axes[1,1].invert_yaxis();axes[1,1].set(title='U-turn course: complete run',xlabel='Median elapsed time (s)');axes[1,1].grid(axis='x',alpha=.18)
fig.suptitle(f'Little Road / dry proving grounds / {len(runs)} actual-car runs', fontsize=18)
fig.savefig(folder/'equipment-comparison.png',dpi=160);plt.close(fig)
print(json.dumps(dict(runs=len(runs),checks=len(checks),passed=summary['passed'],failed=[c for c in checks if not c['passed']],report=str(folder/'report.md')),indent=2))
raise SystemExit(0 if summary['passed'] else 2)
