"""Report the first context-policy screen without claiming field acceptance."""
import argparse
import json
from pathlib import Path


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def summarize(root):
    manifest=read(root/'manifest.json')
    if not manifest['complete']:
        raise ValueError('Wait for the declared runner to complete; partial evidence is not a completed screen.')
    rows=[]
    for run in manifest['runs']:
        folder=Path(run['report']).parent
        race=read(folder/'race.json');track=read(folder/'race.track.json');fixture=read(folder/'race.traffic.json')
        car=race['cars'][0];body=next(r for r in track['results'] if r['id']==0)
        samples=[r for r in track['samples'] if r['id']==0]
        seconds=braking=0
        for a,b in zip(samples,samples[1:]):
            dt=b['time']-a['time']
            if 0<dt<=.25:
                seconds+=dt
                braking+=dt*(a['pedal']<-.05)
        finishes=[c['time'] for c in race['cars'] if c['finished']];complete=len(finishes)==race['carCount']
        rows.append(dict(name=run['name'],exitCode=run['exitCode'],finished=car['finished'],time=car['time'],
                         totalCarSeconds=sum(finishes) if complete else None,meanCarSeconds=sum(finishes)/len(finishes) if complete else None,lastCarSeconds=max(finishes) if complete else None,completedCars=len(finishes),expectedCars=race['carCount'],
                         brakePercent=100*braking/seconds if seconds else None,
                         clean=all(c['finished'] and not(c['contacts'] or c['recoveries'] or c['physicalRecoveries'] or c['missedGates']) for c in race['cars']) and all(r['outsideBodySamples']==0 for r in track['results']),
                         allCars=[dict(id=c['id'],contacts=c['contacts'],recoveries=c['recoveries'],physicalRecoveries=c['physicalRecoveries'],missedGates=c['missedGates']) for c in race['cars']],
                         departures=body['outsideBodySamples'],sustainedPass=fixture['sustainedPassAt'],
                         passOnStraight=fixture['passOnStraight'],maxOverlap=max(race['maxCarPoseOverlap'],race['maxEnvironmentPoseOverlap']),
                         querySaturations=race['overlapQuerySaturations']))
    lookup={r['name']:r for r in rows}
    pairs=[]
    for key in ['solo-stock','solo-street','solo-club','rear']:
        a=lookup[key+'-reference'];b=lookup[key+'-context']
        pairs.append(dict(case=key,timeDelta=b['time']-a['time'],brakePointDelta=b['brakePercent']-a['brakePercent'],bothClean=a['clean'] and b['clean']))
    report=dict(scope=manifest['scope'],rows=rows,matchedPairs=pairs,
                limits='Single-run screen, mid-course start. Solo runs include one full flying lap. No witness manoeuvre/deadline, repeated or held-out cases, full-field, moving-review or rendering acceptance is established.')
    (root/'summary.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    lines=['# Context prototype screen','',report['limits'],'',
           '| Case | Finish time, car 0 | Total car-time | Brake duty | All cars clean | Sustained pass |',
           '|---|---:|---:|---:|---|---:|']
    for r in rows:
        total=f"{r['totalCarSeconds']:.3f} car-s" if r['totalCarSeconds'] is not None else 'Incomplete'
        lines.append(f"| {r['name']} | {r['time']:.3f} s | {total} | {r['brakePercent']:.2f}% | {r['clean']} | {r['sustainedPass']:.3f} s |")
    lines+=['','A pass time of −1 means no sustained pass. Clean requires every car finished, zero recorded contacts, recoveries, missed gates and sampled departures. Brake duty uses the recorded mid-course fixture interval, not the standing-field denominator.','',
            '| Matched case | Context − reference time | Brake percentage-point delta | Both clean |',
            '|---|---:|---:|---|']
    for r in pairs:lines.append(f"| {r['case']} | {r['timeDelta']:.4f} s | {r['brakePointDelta']:.4f} | {r['bothClean']} |")
    (root/'summary.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
    return report


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('folder',type=Path)
    args=parser.parse_args();result=summarize(args.folder)
    print(json.dumps(dict(matchedPairs=result['matchedPairs'],passCase=next(r for r in result['rows'] if r['name']=='pass-context')),indent=2))
