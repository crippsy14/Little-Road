# Needs matplotlib for its plots: py -m pip install matplotlib (no longer vendored in tools/).
"""Validate actual graded-road runs, gravity controls and conditional upgrades."""
import argparse
import json
import os
import statistics as st
import sys
from collections import defaultdict
from pathlib import Path

p=argparse.ArgumentParser(description=__doc__);p.add_argument('folder',type=Path);args=p.parse_args();folder=args.folder.resolve()
batch=json.loads((folder/'batch.json').read_text(encoding='utf-8-sig'));runs=batch['runs'];groups=defaultdict(list)
for r in runs: groups[r['course'],r['loadout']].append(r)
checks=[]
def check(name,passed,detail=''):checks.append(dict(name=name,passed=bool(passed),detail=detail))
courses=['coast-flat','coast-up','coast-down','hill','combined']
loadouts=['stock','engine_workshop','transmission_long','chassis_light','club']
check('Complete 75-run graded-road matrix',batch.get('complete') and len(runs)==75 and all(
    len(groups[c,l])==3 and {r['repeat'] for r in groups[c,l]}=={1,2,3} for c in courses for l in loadouts))
check('Every attempted run is valid',all(r['valid'] and r['completed'] for r in runs))
check('One runtime assembly at 120 Hz',len({r['assemblyVersion'] for r in runs})==1 and all(abs(r['physicsStep']-1/120)<1e-7 for r in runs))
check('No contacts, recovery, missing gates or saturated support queries',all(r['contacts']==r['recoveries']==r['missedGates']==r['groundQuerySaturations']==0 for r in runs))
check('All graded runs remain physically supported',all(r['airborneSteps']==0 and r['maxAirClearance']<.12 for r in runs),
      'Local physical support clearance, not height above sea level; no unsupported completed physics step.')
check('Chassis tracks road normal within five degrees',all(r['maxAlignmentDegrees']<5 for r in runs))
check('Every car remains within the measured carriageway',all(r['maxAbsOffset']<8 for r in runs))
rows=[]
for (course,loadout),items in groups.items():
    times=[r['time'] for r in items];check(f'{course}/{loadout}: repeatability',len(items)==3 and max(times)-min(times)<max(.05,st.median(times)*.002))
    row=dict(course=course,loadout=loadout,seconds=st.median(times),timeRange=max(times)-min(times),
        peakMph=st.median(r['peakSpeed'] for r in items)*2.237,finalMph=st.median(r['finalSpeed'] for r in items)*2.237,
        maxClearance=max(r['maxAirClearance'] for r in items),maxAlignment=max(r['maxAlignmentDegrees'] for r in items))
    if course.startswith('coast-'):
        errors=[]
        for r in items:
            steady=[s for s in r['samples'] if .3<s['time']<r['time']-.1]
            check(f"{r['id']}: matched unpowered coasting",r['entrySpeed']==12 and all(abs(s['throttle'])<.001 for s in steady) and len(steady)>=5)
            error=st.mean(s['longitudinalAcceleration']-s['expectedCoastAcceleration'] for s in steady) if steady else 999
            check(f"{r['id']}: one gravity component plus measured drag",abs(error)<.20,
                  f'Mean acceleration residual {error:.5f} m/s²; tolerance 0.20, versus ~1.55 m/s² for one 16% grade gravity component.')
            errors.append(error)
        row['meanAccelerationResidual']=st.mean(errors)
    else:
        climb=[next(s for s in r['sectors'] if s['name']=='climb') for r in items]
        row['climbSeconds']=st.median(s['time'] for s in climb);row['crestMph']=st.median(s['speed'] for s in climb)*2.237
        check(f'{course}/{loadout}: surveyed summit reached and descent completed',all(abs(r['peakHeight']-35.2)<.03 and abs(r['endHeight'])<.03 for r in items))
    rows.append(row)

def value(course,loadout,field):return next(r[field] for r in rows if r['course']==course and r['loadout']==loadout)
try:
    for loadout in loadouts:
        check(loadout+': downhill coast faster than flat, flat faster than uphill',
              value('coast-down',loadout,'finalMph')>value('coast-flat',loadout,'finalMph')+1 and
              value('coast-flat',loadout,'finalMph')>value('coast-up',loadout,'finalMph')+1)
    for course in ['coast-flat','coast-up','coast-down']:
        check(course+': unfuelled engine upgrade cannot propel a coasting car',
              abs(value(course,'stock','seconds')-value(course,'engine_workshop','seconds'))<.01 and
              abs(value(course,'stock','finalMph')-value(course,'engine_workshop','finalMph'))<.02)
    for course in ['hill','combined']:
        check(course+': workshop engine improves climb time',value(course,'engine_workshop','climbSeconds')<value(course,'stock','climbSeconds')-.05)
        check(course+': lighter chassis improves powered climb time',value(course,'chassis_light','climbSeconds')<value(course,'stock','climbSeconds')-.05)
        check(course+': full club setup improves total time',value(course,'club','seconds')<value(course,'stock','seconds')-1)
    check('Club car sheds downhill overspeed on the flat exit',all(r['finalSpeed']<r['tuning']['gearCeiling']+.10 for r in groups['hill','club']),
          'The engine must not cancel drag indefinitely above its gear ceiling after gravity has accelerated the car downhill.')
except (StopIteration,KeyError):check('Required upgrade comparisons exist',False)

result=dict(passed=all(c['passed'] for c in checks),runs=len(runs),checks=checks,rows=rows,
    scope='Actual car on dry surveyed hill/combined/coasting fixtures; not campaign track, fuel, wet/off-road, natural AI or human acceptance.')
(folder/'grade-analysis.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
lines=['# Physical hill and coasting measurements','',f"{len(runs)} actual-car runs; **{'PASS' if result['passed'] else 'ATTENTION REQUIRED'}**.",'',
    'The hill has a 35.2 m summit, 16% sustained grades, 60 m smooth transitions and a 100 m plateau. '
    'Coasting controls start at 12 m/s and travel 20 m on the same physical support system. '
    'Gravity comes from PhysX once; the expected acceleration is gravity along the ground plus drag, with no engine or brake input.','',
    '| Course | Setup | Time (s) | Crest / final mph | Maximum local clearance (m) | Maximum alignment error (°) |',
    '| --- | --- | ---: | ---: | ---: | ---: |']
for r in rows:lines.append(f"| {r['course']} | {r['loadout']} | {r['seconds']:.3f} | {r.get('crestMph',r['finalMph']):.2f} | {r['maxClearance']:.4f} | {r['maxAlignment']:.3f} |")
lines+=['','## Checks','']+[f"- {'PASS' if c['passed'] else 'FAIL'} — {c['name']}"+(f": {c['detail']}" if c['detail'] else '') for c in checks]
lines+=['','## Limits','',result['scope'],
    'Ground support aligns an aggregate car collider with sampled collision geometry through torque. It is not a wheel suspension or tyre slip-ratio simulation. '
    'Do not infer banked-road, airborne jump, mixed traffic on hills or production-controller acceptance from these isolated runs. '
    'Retain the flat equipment and 24-car Bayview vertical regressions before promoting a build.','',
    '![Measured grade comparisons](grade-comparison.png)','']
(folder/'grade-report.md').write_text('\n'.join(lines),encoding='utf-8')
local=Path(__file__).resolve().parent/'python-packages'
if local.is_dir():sys.path.insert(0,str(local))
os.environ.setdefault('MPLCONFIGDIR',str(folder/'.matplotlib-cache'))
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
fig,axes=plt.subplots(2,2,figsize=(15,10),constrained_layout=True)
names={'stock':'Stock','engine_workshop':'Workshop engine','transmission_long':'Long gears','chassis_light':'Light chassis','club':'Full club'}
for course,axis in [('hill',axes[0,0]),('combined',axes[1,0])]:
    for loadout in loadouts:
        items=groups.get((course,loadout))
        if items:
            samples=items[0]['samples'];axis.plot([s['routeDistance'] for s in samples],[s['speed']*2.237 for s in samples],label=names[loadout])
    axis.set(title=course.title()+': actual speed along the route',xlabel='Distance from start (m)',ylabel='Speed (mph)');axis.legend(fontsize=8);axis.grid(alpha=.2)
profile=folder/'hill-profile.json'
if profile.exists():
    survey=json.loads(profile.read_text())['samples'];axes[0,1].plot([s['distance'] for s in survey],[s['height'] for s in survey],color='forestgreen')
axes[0,1].set(title='Surveyed hill elevation',xlabel='Distance from start (m)',ylabel='Road elevation (m)');axes[0,1].grid(alpha=.2)
for course in courses[:3]:
    items=groups.get((course,'stock'))
    if items:
        samples=items[0]['samples'];axes[1,1].plot([s['routeDistance'] for s in samples],[s['speed']*2.237 for s in samples],label=course)
axes[1,1].set(title='Unpowered stock car: gravity controls',xlabel='Distance from start (m)',ylabel='Speed (mph)');axes[1,1].legend();axes[1,1].grid(alpha=.2)
fig.suptitle(f'Little Road / physical grade tests / {len(runs)} runs',fontsize=18);fig.savefig(folder/'grade-comparison.png',dpi=160);plt.close(fig)
print(json.dumps(dict(runs=len(runs),checks=len(checks),passed=result['passed'],failed=[c for c in checks if not c['passed']]),indent=2))
raise SystemExit(0 if result['passed'] else 2)
