"""Find brake/accelerate/brake episodes for review, without inferring necessity."""
import argparse
from collections import defaultdict
import json
from pathlib import Path


def cycles(rows):
    episodes=[];current=None;segment=0
    for a,b in zip(rows,rows[1:]):
        dt=b['time']-a['time']
        if not 0<dt<=.25 or a['progress']<0:
            current=None
            segment+=1
            continue
        kind='brake' if a['pedal']<-.05 else 'accelerate' if a['pedal']>.2 else 'neutral'
        if current is None or current['kind']!=kind:
            current=dict(kind=kind,start=a['time'],end=b['time'],progress=a['progress'],reason=a.get('trafficReason'),region=a['region'],segment=segment)
            episodes.append(current)
        else:current['end']=b['time']
    qualified=[e for e in episodes if e['kind']!='neutral' and e['end']-e['start']>=.199]
    found=[]
    for a,b,c in zip(qualified,qualified[1:],qualified[2:]):
        if a['segment']==b['segment']==c['segment'] and (a['kind'],b['kind'],c['kind'])==('brake','accelerate','brake') and 0<=c['start']-a['end']<=2:
            found.append(dict(start=a['start'],end=c['end'],progress=a['progress'],firstReason=a['reason'],secondReason=c['reason'],firstRegion=a['region'],secondRegion=c['region'],classification='unreviewed'))
    return found


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('folder',type=Path);parser.add_argument('--output',required=True,type=Path)
    args=parser.parse_args()
    if args.output.exists():parser.error('Choose a new output to preserve prior evidence.')
    track=json.loads((args.folder/'race.track.json').read_text(encoding='utf-8-sig'))
    grouped=defaultdict(list)
    for row in track['samples']:grouped[row['id']].append(row)
    results=[]
    for car,rows in grouped.items():
        events=cycles(rows)
        kilometres=sum(max(0,b['progress']-max(0,a['progress'])) for a,b in zip(rows,rows[1:]) if 0<b['time']-a['time']<=.25 and b['progress']>=0)/1000
        results.append(dict(id=car,carKilometres=kilometres,cycleCount=len(events),cycles=events))
    distance=sum(r['carKilometres'] for r in results);count=sum(r['cycleCount'] for r in results)
    out=dict(scope='Raw cycle candidates, not unexplained-braking verdicts. Corner transitions and real traffic can justify cycles. Review limiting evidence before applying the 25% reduction gate.',folder=str(args.folder.resolve()),count=count,carKilometres=distance,cyclesPerCarKm=count/distance if distance else None,cars=results)
    args.output.parent.mkdir(parents=True,exist_ok=True);args.output.write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({k:v for k,v in out.items() if k!='cars'},indent=2))
