"""Checkpoint throughput with explicit isolated-delay versus field-delay evidence."""
import argparse, json, math, sys
from pathlib import Path

def read(path): return json.loads(path.read_text(encoding='utf-8-sig'))

def passages(race,track):
    """First valid passage only; resets/progress jumps cannot invent a crossing."""
    result={i:{} for i in range(race['carCount'])}
    if 'checkpointPassages' in race:
        for event in race['checkpointPassages']:
            ordinal=round(event['requiredProgress']/(race['trackLength']/12))
            if 0<=ordinal<=12:
                result[event['id']].setdefault(ordinal,event['time'])
        return result,'validated physics-tick checkpoint events'
    # Historical reports predate the exact event hook. Require the next-gate
    # transition, not just geometric progress, and disclose sampling precision.
    expected={i:0 for i in result}
    for sample in track['samples']:
        i=sample['id'];ordinal=expected[i]
        if ordinal<12 and sample['gate']==(ordinal+1)%12 and sample['progress']>=ordinal*race['trackLength']/12-.5:
            result[i][ordinal]=sample['time'];expected[i]+=1
    for car in race['cars']:
        if car['finished'] and race['requiredLaps']==1:result[car['id']][12]=car['time']
    return result,'historical 10 Hz gate transitions; official one-lap finish times'

def analyze(folder,baseline_root=None):
    race=read(folder/'race.json');track=read(folder/'race.track.json')
    if race['carCount']<24:raise ValueError('Field flow requires at least 24 entrants.')
    actual,precision=passages(race,track);solo=[]
    for setup in ['street','club']:
        base=(baseline_root or folder.parent)/f'solo-{setup}';sr=read(base/'race.json')
        if sr['trackId']!=race['trackId'] or sr['trackVersion']!=race['trackVersion']:raise ValueError('Mismatched solo track.')
        sp,_=passages(sr,read(base/'race.track.json'));solo.append(sp[0])
    rows=[]
    for ordinal in range(13):
        if not all(ordinal in p for p in solo):raise ValueError(f'Missing solo checkpoint {ordinal}')
        deadline=12 if ordinal==0 else math.ceil(max(p[ordinal] for p in solo)*1.25+8)
        ontime=[i for i,p in actual.items() if ordinal in p and p[ordinal]<=deadline]
        late=[i for i,p in actual.items() if ordinal in p and p[ordinal]>deadline]
        missing=[i for i,p in actual.items() if ordinal not in p]
        delayed=sorted(late+missing);elapsed=race['simSeconds']
        status='met' if not delayed else 'pending' if elapsed<deadline else 'isolated-delay-review' if len(delayed)<=2 else 'multiple-car-delay'
        details=[]
        for i in delayed:
            previous=actual[i].get(ordinal-1,0);end=min(elapsed,actual[i].get(ordinal,elapsed))
            impacts=[e for e in race['impacts'] if e['id']==i and previous<=e['time']<=end]
            recovery=[s for s in track['samples'] if s['id']==i and previous<=s['time']<=end and s['recoveryState'] not in ('Racing','Disabled')]
            reached=[k for k in actual[i] if actual[i][k]<=min(deadline,elapsed)]
            details.append(dict(id=i,lastCheckpointAtDeadline=max(reached,default=-1),contactEventsInSector=len(impacts),
                maximumImpactSpeed=max((e['relativeSpeed'] for e in impacts),default=0),recoveryObserved=bool(recovery)))
        times=sorted(p[ordinal] for p in actual.values() if ordinal in p)
        rows.append(dict(ordinal=ordinal,label='Start line' if ordinal==0 else 'Lap-one finish' if ordinal==12 else f'Checkpoint {ordinal}',
            deadlineSecondsAfterGreen=deadline,status=status,expected=race['carCount'],passedByDeadline=len(ontime),passedTotal=len(times),
            firstPass=times[0] if times else None,lastPass=times[-1] if len(times)==race['carCount'] else None,
            onTimeIds=ontime,lateIds=late,notPassedIds=missing,delayedCarEvidence=details))
    out=dict(scope='First standing lap, Cedar mixed street/club field. Deadlines are provisional fixed engineering targets.',
        clock='Simulation seconds after countdown/green; start-line passage is distinct from beginning to move.',precision=precision,
        interpretation='All-car deadlines are strict. One or two delayed cars are flagged for incident review, not called a traffic jam. Multiple delays flag throughput failure, not a proven cause. Contact correlation does not establish blame or cause.',
        soloEvidenceRoot=str(baseline_root or folder.parent),allCarDeadlinesMet=all(row['status']=='met' for row in rows),checkpoints=rows)
    (folder/'checkpoint-flow.json').write_text(json.dumps(out,indent=2)+'\n')
    lines=['# Checkpoint flow','',out['clock'],'',f"Timing: {precision}.",'',
        '| Passage | Deadline after green | On time | Passed by report | Result |','|---|---:|---:|---:|---|']
    for row in rows:lines.append(f"| {row['label']} | {row['deadlineSecondsAfterGreen']} s | {row['passedByDeadline']}/{row['expected']} | {row['passedTotal']}/{row['expected']} | {row['status']} |")
    lines+=['',out['interpretation'],'','See `checkpoint-flow.json` for per-car IDs, previous checkpoint and incident evidence.']
    (folder/'checkpoint-flow.md').write_text('\n'.join(lines)+'\n')
    return out

if __name__=='__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('folder',type=Path)
    parser.add_argument('--baseline-root',type=Path)
    args=parser.parse_args()
    result=analyze(args.folder.resolve(),args.baseline_root.resolve() if args.baseline_root else None)
    for row in result['checkpoints']:print(row['label'],f"{row['passedByDeadline']}/{row['expected']} by {row['deadlineSecondsAfterGreen']} s",row['status'])
    raise SystemExit(0 if result['allCarDeadlinesMet'] else 1)
