"""Review a volunteered human run separately from AI reference generation."""
import argparse
import bisect
import json
import math
import sys
from pathlib import Path


def read(path):
    return json.loads(Path(path).read_text(encoding='utf-8-sig'))


def at_progress(samples, progress):
    # Scan rather than bisect: an assisted recovery may move progress backwards.
    for a, b in zip(samples, samples[1:]):
        if a['progress'] <= progress < b['progress']:
            f = (progress-a['progress'])/(b['progress']-a['progress'])
            return {k: a[k]+f*(b[k]-a[k]) for k in ('time','speed')}
    return None


def laps(race, track):
    samples = track['samples']
    length = track['length']
    crossings = [at_progress(samples, i*length) for i in range(race['requiredLaps'])]
    crossings.append(dict(time=race['cars'][0]['time'],speed=samples[-1]['speed']))
    result=[]
    for i, (a,b) in enumerate(zip(crossings,crossings[1:])):
        if not a or not b:
            raise ValueError(f'Missing timed lap boundary: {i+1}')
        rows=[s for s in samples if a['time']<=s['time']<b['time']]
        result.append(dict(lap=i+1,seconds=b['time']-a['time'],start=a['time'],end=b['time'],
                           bodyOutsideSamples=sum(s['maxBodyOffset']>8 for s in rows),
                           centreOutsideSamples=sum(abs(s['roadOffset'])>8 for s in rows),
                           maxBodyOffset=max(s['maxBodyOffset'] for s in rows),
                           maxCentreOffset=max(abs(s['roadOffset']) for s in rows),
                           humanSamples=sum(s['inputOwner']=='human' for s in rows),samples=len(rows),
                           backwardsProgressSteps=sum(y['progress']<x['progress']-.1 for x,y in zip(rows,rows[1:])),
                           minSpeed=min(s['speed'] for s in rows),topSpeed=max(s['speed'] for s in rows)))
    return result


def sector_times(track, lap, final_time=None):
    times=[]
    for gate in range(13):
        p=(lap-1+gate/12)*track['length']
        item=at_progress(track['samples'],p)
        times.append(final_time if gate==12 and final_time is not None else item['time'] if item else None)
    return [b-a if a is not None and b is not None else None for a,b in zip(times,times[1:])]


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('human',type=Path)
    parser.add_argument('ai',type=Path)
    parser.add_argument('--exclude',type=int,nargs='*',default=[2])
    parser.add_argument('--plot',action='store_true')
    parser.add_argument('--output',type=Path,help='Keep additional comparisons separate from the original human review.')
    args=parser.parse_args()
    output=args.output or args.human;output.mkdir(parents=True,exist_ok=True)
    human=read(args.human/'race.json');ht=read(args.human/'race.track.json')
    ai=read(args.ai/'race.json');at=read(args.ai/'race.track.json')
    if human['autonomous'] or ht['sessionType']!='human comparison; never optimiser input':
        raise ValueError('Not a declared human comparison run')
    same_tuning=ht['tuning']==at['tuning']
    same_track=(human['trackId'],human['trackVersion'],human['trackLength'])==(ai['trackId'],ai['trackVersion'],ai['trackLength'])
    if not same_tuning or not same_track:
        raise ValueError('Equipment or geometry mismatch: cannot use a matched pace comparison')
    hl=laps(human,ht);al=laps(ai,at)
    for row in hl:
        row['selectedByUser']=row['lap'] not in args.exclude
        row['note']='Excluded: user reported off-track attempt' if not row['selectedByUser'] else 'User-selected comparison lap; boundary usage reported separately'
    selected=[r for r in hl if r['selectedByUser']]
    flying=[r for r in selected if r['lap']>1]
    best=min(flying,key=lambda r:r['seconds'])
    ai_best=min(al[1:],key=lambda r:r['seconds'])
    gap=ai['cars'][0]['bestLap']-best['seconds']
    hs=sector_times(ht,best['lap'],human['cars'][0]['time'] if best['lap']==human['requiredLaps'] else None)
    ass=sector_times(at,ai_best['lap'],ai['cars'][0]['time'] if ai_best['lap']==ai['requiredLaps'] else None)
    sectors=[dict(sector=i+1,humanSeconds=h,aiSeconds=a,aiLostSeconds=None if h is None or a is None else a-h) for i,(h,a) in enumerate(zip(hs,ass))]
    review=dict(status='Human comparison only; no training data or accepted reference changed',humanRun=str(args.human.resolve()),aiRun=str(args.ai.resolve()),
                sameTuning=same_tuning,sameTrack=same_track,laps=hl,aiLaps=al,
                bestSelectedFlyingLap=best['lap'],humanBestApproxSeconds=best['seconds'],humanOfficialBestSeconds=human['cars'][0]['bestLap'],
                aiOfficialBestSeconds=ai['cars'][0]['bestLap'],aiLostSeconds=gap,
                aiGapPercent=100*gap/best['seconds'],selectedFlyingSpread=max(r['seconds'] for r in flying)-min(r['seconds'] for r in flying),
                sectors=sectors,incidents=dict(contacts=human['cars'][0]['contacts'],recoveries=human['cars'][0]['recoveries'],missedGates=human['cars'][0]['missedGates']),
                limitations=['Individual lap and sector splits interpolate 10 Hz telemetry; displayed to 0.1 s. Official best lap is timed by ordered gates.',
                             'Lap 1 begins at the timing-line crossing after grid acceleration; compare flying laps 3 and 4 for steady pace.',
                             'The road/body boundary audit is stricter than the permissive checkpoint corridor. User-selected laps are not automatically full-body-on-asphalt laps.',
                             'Centreline adherence in the raw human report is not an ideal-line score.','Builds may differ; same reported tuning and geometry are verified, but inspect the corresponding build manifests before attributing changes.'])
    (output/'human-review.json').write_text(json.dumps(review,indent=2)+'\n')
    lines=['# Human Cedar comparison laps','',review['status'],'','Lap 2 is retained in raw evidence but excluded at the user’s request.','',
           '| Lap | Time (s, approx.) | Used | Centre outside asphalt samples | Body outside asphalt samples |','|---|---:|---|---:|---:|']
    for row in hl:
        lines.append(f"| {row['lap']} | {row['seconds']:.1f} | {'Yes' if row['selectedByUser'] else 'No: off-track attempt'} | {row['centreOutsideSamples']} | {row['bodyOutsideSamples']} |")
    lines+=['',f"Selected flying laps differ by {review['selectedFlyingSpread']:.2f} s. Human official best: {human['cars'][0]['bestLap']:.3f} s; candidate AI: {ai['cars'][0]['bestLap']:.3f} s. The candidate is {gap:.2f} s ({review['aiGapPercent']:.1f}%) slower.",
            '',f"Whole run: {review['incidents']}. All retained samples are labelled by input owner. Off-road observations are kept, not silently classed as clean.",'','## Interpretation','',
            'Compare measured pace, speed/position plots and legal road usage together: candidate adherence alone is not proof of optimal pace. Do not automatically copy the human line or train on these samples.','',
            '## Measurement limits','']+['- '+s for s in review['limitations']]
    (output/'human-review.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
    print(json.dumps({k:v for k,v in review.items() if k not in ('limitations','sectors','aiLaps')},indent=2))
    if args.plot:
        sys.path.insert(0,str(Path(__file__).resolve().parent/'python-packages'))
        import matplotlib
        matplotlib.use('Agg')
        import matplotlib.pyplot as plt
        fig=plt.figure(figsize=(16,10),facecolor='#f5f3ec')
        grid=fig.add_gridspec(3,2,width_ratios=[1,1.35],height_ratios=[1,1,.8])
        mapax=fig.add_subplot(grid[:2,0]);timeax=fig.add_subplot(grid[2,0]);speedax=fig.add_subplot(grid[0,1]);offsetax=fig.add_subplot(grid[1,1]);gainax=fig.add_subplot(grid[2,1])
        road=ht['survey'];x=[p['position']['x'] for p in road];z=[p['position']['z'] for p in road]
        # Boundaries use local road tangents and actual width rather than thick schematic strokes.
        for sign in (-1,1):
            bx=[];bz=[]
            for i in range(len(road)):
                dx=x[(i+1)%len(x)]-x[(i-1)%len(x)];dz=z[(i+1)%len(z)]-z[(i-1)%len(z)];mag=math.hypot(dx,dz)
                bx.append(x[i]+sign*8*dz/mag);bz.append(z[i]-sign*8*dx/mag)
            mapax.plot(bx+[bx[0]],bz+[bz[0]],color='#b9c2c0',lw=.7)
        mapax.plot(x+[x[0]],z+[z[0]],color='#acb3b0',lw=.5,ls='--')
        traces=[(ht,r['lap'],f"You · lap {r['lap']}",c) for r,c in zip(selected,['#c8843c','#07847d','#5176b8'])]
        traces.append((at,ai_best['lap'],'Candidate AI','#b14f69'))
        for track,lap,label,color in traces:
            rows=[s for s in track['samples'] if (lap-1)*track['length']<=s['progress']<lap*track['length']]
            route=[s['progress']-(lap-1)*track['length'] for s in rows]
            mapax.plot([s['position']['x'] for s in rows],[s['position']['z'] for s in rows],color=color,lw=1,alpha=.85,label=label)
            speedax.plot(route,[s['speed']*2.23694 for s in rows],color=color,lw=1.3,label=label)
            offsetax.plot(route,[s['roadOffset'] for s in rows],color=color,lw=1)
        mapax.set_aspect('equal');mapax.set_title('Recorded paths · actual asphalt edges');mapax.legend(fontsize=8,loc='upper left');mapax.set_xlabel('East / west (m)');mapax.set_ylabel('North / south (m)')
        timeax.bar(['You 1','You 2*','You 3','You 4','AI'],[r['seconds'] for r in hl]+[ai['cars'][0]['bestLap']],color=['#c8843c','#bcbcbc','#07847d','#5176b8','#b14f69'])
        timeax.set_ylim(50,max(r['seconds'] for r in hl)+4);timeax.set_ylabel('Lap time (s)');timeax.set_title('* Lap 2 excluded from comparison')
        for i,v in enumerate([r['seconds'] for r in hl]+[ai['cars'][0]['bestLap']]):timeax.text(i,v+.5,f'{v:.1f}',ha='center',fontsize=9)
        speedax.set_title('Speed · same club equipment');speedax.set_ylabel('mph');speedax.legend(fontsize=8,ncol=2)
        offsetax.axhline(8,color='#666',ls='--',lw=.7);offsetax.axhline(-8,color='#666',ls='--',lw=.7);offsetax.set_title('Car-centre position across the road · not an ideal-line score');offsetax.set_ylabel('Offset from centre (m)')
        vals=[s['aiLostSeconds'] for s in sectors]
        gainax.bar(range(1,13),[v or 0 for v in vals],color=['#07847d' if v is not None and v>=0 else '#b14f69' for v in vals]);gainax.axhline(0,color='#777',lw=.8)
        gainax.set_title(f'AI time lost per gate sector vs your lap {best["lap"]} · positive = you faster');gainax.set_xlabel('Sector between ordered gates');gainax.set_ylabel('Seconds');gainax.set_xticks(range(1,13))
        for ax in (speedax,offsetax):ax.set_xlabel('Road distance from timing line (m)');ax.set_xlim(0,ht['length']);ax.grid(alpha=.2)
        fig.suptitle('Cedar pace comparison · human laps and candidate AI',fontsize=19)
        fig.text(.04,.018,'10 Hz telemetry · lap 2 excluded · human data stays separate · line legality and player acceptance remain distinct from speed',fontsize=10)
        fig.tight_layout(rect=(0,.045,1,.95));fig.savefig(output/'human-comparison.png',dpi=145);plt.close(fig)


if __name__=='__main__':main()
