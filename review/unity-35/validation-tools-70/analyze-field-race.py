"""First-lap field throughput and racecraft gate, using matched solo pace."""
import argparse, json, math, sys
from pathlib import Path
from runtime_log_errors import runtime_error_lines

parser=argparse.ArgumentParser()
parser.add_argument('folder',type=Path)
parser.add_argument('--baseline-root',type=Path,help='Frozen matched solo evidence; never derive a relaxed cutoff from the candidate.')
args=parser.parse_args()
folder=args.folder.resolve();root=args.baseline_root.resolve() if args.baseline_root else folder.parent
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
race=read(folder/'race.json');track=read(folder/'race.track.json')
solo={name:read(root/f'solo-{name}/race.json')['cars'][0]['time'] for name in ['street','club']}
# Explicit Cedar mixed-equipment gate. Keep fixed while iterating: 25% traffic
# allowance plus 8 seconds for rear-grid travel/launch, rounded up.
cutoff=math.ceil(max(solo.values())*1.25+8)
rows={i:[] for i in range(race['carCount'])};frames={}
for s in track['samples']:
    rows[s['id']].append(s);frames.setdefault(s['time'],{})[s['id']]=s
launch={i:next((s['time'] for s in samples if s['speed']>1),None) for i,samples in rows.items()}
first_move=min((t for t in launch.values() if t is not None),default=None)
lap_times={}
for c in race['cars']:
    if race['requiredLaps']==1 and c['finished']:lap_times[c['id']]=c['time']
    elif race.get('checkpointPassages'):
        lap_times[c['id']]=next((e['time'] for e in race['checkpointPassages'] if e['id']==c['id'] and e['gate']==0 and abs(e['requiredProgress']-race['trackLength'])<.1),None)
    else:
        # Route distance alone can cross the finish with a missed checkpoint.
        # Historical lap-counter samples are coarser but prove a valid lap.
        lap_times[c['id']]=next((s['time'] for s in race['motion'] if s['id']==c['id'] and s['laps']>=1),None)
complete=all(t is not None for t in lap_times.values())
window=max(lap_times.values())-first_move if complete and first_move is not None else None
checks=[]
def check(name,value,detail=None):checks.append(dict(name=name,passed=bool(value),detail=detail))
errors=runtime_error_lines(folder)
check('No runtime exceptions or assertions',not errors,dict(count=len(errors),examples=errors[:3]))
check('At least 24 entrants',race['carCount']>=24)
check('Every car completes lap one',complete,lap_times)
check('First movement within two seconds of green',first_move is not None and first_move<2,first_move)
check('Every car starts within eight seconds',all(t is not None and t<8 for t in launch.values()),launch)
check('First movement to last first-lap completion within cutoff',window is not None and window<=cutoff,dict(seconds=window,cutoff=cutoff))
for field in ['recoveries','missedGates']:
    check('No '+field,all(c[field]==0 for c in race['cars']),sum(c[field] for c in race['cars']))
check('No sampled body departure',all(r['outsideBodySamples']==0 for r in track['results']))
check('No pose overlap above 2 cm',max(race['maxCarPoseOverlap'],race['maxEnvironmentPoseOverlap'])<.02)
check('No recovery episodes in the first 15 seconds',not any(s['time']<15 and s['recoveryState'] not in ('Racing','Disabled') for s in track['samples']))
# Count only substantial initially trailing club-vs-street pairs. A fleeting
# ranking change, a reset or the side-by-side grid stagger is not a clean pass.
initial={i:samples[0]['progress'] for i,samples in rows.items()}
equipment={c['id']:c['equipment'] for c in race['cars']};resets={c['id']:c['recoveries'] for c in race['cars']}
pairs={(a,b):None for a in rows for b in rows if equipment[a]=='club' and equipment[b]=='street' and initial[b]-initial[a]>7 and not resets[a] and not resets[b]}
passes={}
for time,frame in sorted(frames.items()):
    for pair in pairs:
        a,b=pair
        if pair in passes or a not in frame or b not in frame:continue
        if frame[a]['progress']-frame[b]['progress']>7:
            if pairs[pair] is None:pairs[pair]=time
            if time-pairs[pair]>=2:passes[pair]=time
        else:pairs[pair]=None
check('Equipment advantage creates a sustained overtake',len(passes)>0,len(passes))
slow=sum(s['speed']<2 and s['time']>8 for s in track['samples'])
out=dict(scope='Cedar first-lap mixed street/club field; not general AI or player acceptance.',soloEvidenceRoot=str(root),soloStandingLapSeconds=solo,
    cutoffSeconds=cutoff,firstMovementSeconds=first_move,firstMovementToLastLapSeconds=window,firstLapTimes=lap_times,
    totalCarSeconds=sum(lap_times.values()) if complete else None,meanCarSeconds=sum(lap_times.values())/len(lap_times) if complete else None,lastCarSeconds=max(lap_times.values()) if complete else None,
    completedCars=sum(t is not None for t in lap_times.values()),expectedCars=race['carCount'],collectiveTimeBasis='Valid first-lap completion after green: official one-lap finishes or ordered checkpoint events; historical fallback is sampled lap count, never route progress alone. Incomplete if any car is missing.',
    contacts=sum(c['contacts'] for c in race['cars']),physicalRecoveries=sum(c['physicalRecoveries'] for c in race['cars']),
    slowCarSecondsAfterLaunch=slow*.1,sustainedEquipmentPasses=[dict(follower=a,leader=b,time=t) for (a,b),t in passes.items()],
    passed=sum(c['passed'] for c in checks),total=len(checks),checks=checks)
(folder/'field-analysis.json').write_text(json.dumps(out,indent=2)+'\n')
print(json.dumps({k:v for k,v in out.items() if k not in ('checks','firstLapTimes','sustainedEquipmentPasses')},indent=2))
print('Failures:',[c['name'] for c in checks if not c['passed']])
raise SystemExit(0 if all(c['passed'] for c in checks) else 1)
