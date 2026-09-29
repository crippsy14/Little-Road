"""Stops and queues in a race: python tools/analyze-stalls.py <race.json or run folder> [...]

From the race report's motion samples (one per car per second):
  - stall: a racing car (started, not finished) below --slow m/s (default 3) for at least
    --seconds s (default 2). Reported with where (track metres), how long, and a cause guess:
      recovery   the car's own physical recovery had taken over (target speed 0 / reversing)
      contact    a contact involving the car within 2 s before the stop
      queued     another car was stalled within 40 m ahead of it (a queue)
      planner    none of the above: the driver chose to stop
  - hard stop: speed falls from above 15 m/s to below 3 m/s within 3 s.
Summary per race: stall count, total car-seconds stalled, hard stops, worst spots, and each
car's class and finish. Compare runs on the same track and seeds; the count is a red flag.
"""
import json,sys,os,argparse,collections

def load(path):
    if os.path.isdir(path):path=os.path.join(path,'race.json')
    return path,json.load(open(path))

def analyse(path,data,slow,seconds):
    length=data['trackLength'];motion=data.get('motion') or []
    if not motion:return None
    by=collections.defaultdict(list)
    for m in motion:by[m['id']].append(m)
    cars={c['id']:c for c in data['cars']}
    contacts=collections.defaultdict(list)
    for i in data.get('impacts') or []:contacts[i['id']].append(i['time'])
    # stalled samples (for queue detection)
    stalledAt=collections.defaultdict(list)
    for cid,ms in by.items():
        for m in ms:
            if m['time']>3 and m['speed']<slow and not(cars[cid]['finished'] and m['laps']>=data['requiredLaps']):
                stalledAt[round(m['time'])].append((cid,m['progress']))
    stalls=[];hard=[]
    for cid,ms in by.items():
        ms.sort(key=lambda m:m['time']);run=[]
        def close():
            if run and run[-1]['time']-run[0]['time']+1>=seconds:
                first=run[0];t=first['time'];prog=first['progress']
                if any(m['targetSpeed']==0 or m['speed']<0 for m in run[:3]):cause='recovery'
                elif any(t-2<=c<=t+.5 for c in contacts[cid]):cause='contact'
                elif any(o!=cid and 0<(op-prog)%length<40 for o,op in stalledAt.get(round(t),[])):cause='queued'
                else:cause='planner'
                stalls.append(dict(car=cid,equipment=cars[cid]['equipment'],start=round(t,1),seconds=round(run[-1]['time']-t+1,1),at=round(prog%length),cause=cause))
        for m in ms:
            racing=m['time']>3 and not(m['laps']>=data['requiredLaps'])
            if racing and m['speed']<slow:run.append(m)
            else:close();run=[]
        close()
        for a,b in zip(ms,ms[3:]):
            if a['time']>3 and a['speed']>15 and min(x['speed'] for x in ms[ms.index(a):ms.index(b)+1])<3:
                hard.append(dict(car=cid,time=round(a['time'],1),at=round(a['progress']%length),**{'from':round(a['speed'],1)}));break
    return stalls,hard

def main():
    ap=argparse.ArgumentParser();ap.add_argument('runs',nargs='+');ap.add_argument('--slow',type=float,default=3);ap.add_argument('--seconds',type=float,default=2);ap.add_argument('--detail',action='store_true')
    a=ap.parse_args()
    for r in a.runs:
        path,data=load(r);res=analyse(path,data,a.slow,a.seconds)
        name=os.path.basename(os.path.dirname(path))
        if res is None:print(f'{name}: no motion samples');continue
        stalls,hard=res;fin=[c for c in data['cars'] if c['finished']]
        causes=collections.Counter(s['cause'] for s in stalls)
        spots=collections.Counter(round(s['at'],-1) for s in stalls)
        print(f"{name}: finished {len(fin)}/{len(data['cars'])} · stalls {len(stalls)} ({sum(s['seconds'] for s in stalls):.0f} car-s) · causes {dict(causes)} · hard stops {len(hard)} · spots {dict(spots.most_common(4))}")
        if a.detail:
            for s in sorted(stalls,key=lambda s:s['start']):print('   ',s)
            for h in hard:print('    hard',h)

if __name__=='__main__':main()
