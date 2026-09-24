"""Inspect actual Cedar laps; centreline adherence is diagnostic, not ideal-line acceptance."""
import argparse
import json
import statistics
import sys
from pathlib import Path

parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('folder',type=Path)
parser.add_argument('--plot',action='store_true')
args=parser.parse_args();folder=args.folder.resolve();checks=[];runs=[]
def check(name,passed): checks.append({'name':name,'passed':bool(passed)})
for path in sorted(folder.glob('solo-*/race.json')):
    race=json.loads(path.read_text(encoding='utf-8-sig'))
    audit=json.loads(path.with_suffix('.track.json').read_text(encoding='utf-8-sig'))
    name=path.parent.name;car=race['cars'][0];metric=audit['results'][0];samples=audit['samples'];length=race['trackLength']
    check(name+': completed every required ordered lap',race['finished']==1 and car['laps']==race['requiredLaps'] and car['finished'])
    check(name+': no impacts, assisted resets or missed gates',car['contacts']==car['recoveries']==car['missedGates']==0)
    check(name+': full body remains on asphalt at 10 Hz',metric['outsideBodySamples']==0)
    check(name+': same declared geometry and physics',race['trackId']=='cedar-technical' and race['trackVersion']==audit['version'] and abs(race['physicsStepSeconds']-1/120)<1e-7)
    check(name+': trace reaches last half-second of lap',samples[-1]['time']>=car['time']-.5)
    crossings=[]
    for gate in range(race['requiredLaps']):
        target=gate*length
        for a,b in zip(samples,samples[1:]):
            if a['progress']<=target<b['progress']:
                f=(target-a['progress'])/(b['progress']-a['progress']);crossings.append(a['time']+f*(b['time']-a['time']));break
    crossings.append(car['time'])
    laps=[b-a for a,b in zip(crossings,crossings[1:])]
    runs.append({'name':name,'setup':race['equipment'],'seed':race['seed'],'lapTimes':laps,'raceTime':car['time'],'bestLap':car['bestLap'],
                 'peakMph':max(s['speed'] for s in samples)*2.23694,'metric':metric,'source':str(path),'audit':audit})
check('Both stock and club baselines present',{'stock','club'}<={r['setup'] for r in runs})
passed=all(c['passed'] for c in checks)
result={'passed':passed,'checks':checks,'runs':[{k:v for k,v in r.items() if k!='audit'} for r in runs],
        'limits':'Legacy AI only. Centreline is not an ideal racing line; no skill, racecraft, physical-recovery, continuous boundary or GPU performance acceptance.'}
(folder/'technical-analysis.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
lines=['# Cedar technical circuit — initial solo baseline','',f"{sum(c['passed'] for c in checks)}/{len(checks)} checks pass.",'',
       'Times below are actual physical laps. Line statistics use centreline-v1, ±1 m, sampled at 10 Hz. They are diagnostic, not a score for an ideal racing line.','',
       '| Setup | Lap times (s) | Peak mph | Within ±1 m | Mean deviation | Worst body offset |','|---|---|---:|---:|---:|---:|']
for r in runs:
    m=r['metric'];lines.append(f"| {r['name']} | {', '.join(f'{v:.3f}' for v in r['lapTimes'])} | {r['peakMph']:.1f} | {m['withinOneMetrePercent']:.1f}% | {m['meanAbsError']:.2f} m | {m['maxBodyOffset']:.2f} m |")
lines+=['',result['limits'],'','Next: input-policy seam, validated fast reference line/speed profile, then physical recovery before traffic/skill variation.']
(folder/'technical-report.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
if args.plot and runs:
    sys.path.insert(0,str(Path(__file__).resolve().parent/'python-packages'))
    import matplotlib
    matplotlib.use('Agg')
    import matplotlib.pyplot as plt
    fig=plt.figure(figsize=(15,10),facecolor='#f5f3ec');grid=fig.add_gridspec(2,2,width_ratios=[1.2,1])
    overview=fig.add_subplot(grid[:,0]);speeds=fig.add_subplot(grid[0,1]);deviations=fig.add_subplot(grid[1,1])
    survey=runs[0]['audit']['survey'];x=[p['position']['x'] for p in survey];z=[p['position']['z'] for p in survey]
    overview.plot(x+[x[0]],z+[z[0]],color='#c7ccc5',lw=12,label='16 m road (schematic width)')
    overview.plot(x+[x[0]],z+[z[0]],color='#344e50',lw=.7,ls='--',label='Centreline baseline')
    for r,color in zip(runs,['#cd6c36','#087f8c','#695bb0','#999000']):
        rows=[s for s in r['audit']['samples'] if 0<=s['progress']<r['audit']['length']]
        overview.plot([s['position']['x'] for s in rows],[s['position']['z'] for s in rows],lw=1,color=color,label=r['name'])
        speeds.plot([s['progress'] for s in rows],[s['speed']*2.23694 for s in rows],color=color,label=r['name'])
        deviations.plot([s['progress'] for s in rows],[s['lineError'] for s in rows],color=color)
    overview.set_aspect('equal');overview.set_title('Cedar Technical · 2.068 km\nActual first-lap paths');overview.set_xlabel('East / west (m)');overview.set_ylabel('North / south (m)');overview.legend(loc='lower left',fontsize=8)
    speeds.set_title('Speed around the lap');speeds.set_ylabel('mph');speeds.legend(fontsize=8)
    deviations.axhspan(-1,1,color='#dae6d7');deviations.axhline(0,color='#637365',ls='--',lw=.8);deviations.set_title('Deviation from centreline · not an ideal racing line');deviations.set_ylabel('Lateral offset (m)')
    for ax in [speeds,deviations]:ax.set_xlabel('Distance from timing line (m)');ax.grid(alpha=.2)
    fig.suptitle('Driving baseline before AI changes',fontsize=19);fig.tight_layout(rect=(0,.03,1,.95));fig.text(.04,.018,'Legacy AI · dry tarmac · shared RoadCar physics · clear-track line statistics only',fontsize=10)
    fig.savefig(folder/'technical-baseline.png',dpi=160);plt.close(fig)
print(json.dumps({'passed':passed,'checks':len(checks),'runs':len(runs),'report':str(folder/'technical-report.md')}))
sys.exit(0 if passed else 1)
