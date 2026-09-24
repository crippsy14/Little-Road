"""Compare physical passing outcomes with an independently driven witness."""
import argparse
import json
from pathlib import Path


def read(path):return json.loads(path.read_text(encoding='utf-8-sig'))


def result(run):
    folder=Path(run['folder']);r=read(folder/'race.json');t=read(folder/'race.track.json');f=read(folder/'race.traffic.json')
    clean=run['exitCode']==0 and all(c['finished'] and not(c['contacts'] or c['recoveries'] or c['physicalRecoveries'] or c['missedGates']) for c in r['cars']) and all(s['outsideBodySamples']==0 for s in t['results']) and max(r['maxCarPoseOverlap'],r['maxEnvironmentPoseOverlap'])<=.02 and r['overlapQuerySaturations']==0
    signature={k:r[k] for k in ('trackId','trackVersion','trackLength','trackWidth','physicsStepSeconds','collisionMode','requiredLaps')}
    signature.update(equipment=[c['equipment'] for c in r['cars']],start=f['start'],gap=f['startingGap'],speed=f['startingSpeed'],reference=t['reference'],tuning=t['tuning'])
    finish=[c['time'] for c in r['cars'] if c['finished']];complete=len(finish)==r['carCount']
    return dict(case=run['case'],repeat=run['repeat'],clean=clean,time=r['cars'][0]['time'],passTime=f['sustainedPassAt'],onStraight=f['passOnStraight'],signature=signature,
                completedCars=len(finish),expectedCars=r['carCount'],totalCarSeconds=sum(finish) if complete else None,meanCarSeconds=sum(finish)/len(finish) if complete else None,lastCarSeconds=max(finish) if complete else None,
                collectiveTimeBasis='Official remaining-course finish times since fixture release, summed across every entrant; incomplete if anyone does not finish.')


if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('folder',type=Path);p.add_argument('--witness-root',type=Path);args=p.parse_args()
    m=read(args.folder/'manifest.json');w=read((args.witness_root or args.folder)/'manifest.json')
    if not m['complete'] or not w['complete']:raise ValueError('Both declared batches must be complete.')
    witnesses={r['case']:result(r) for r in w['runs'] if r['policy']=='traffic' and r['repeat']==1}
    rows=[]
    for run in m['runs']:
        if run['policy']!='context':continue
        r=result(run);witness=witnesses[r['case']]
        if r['signature']!=witness['signature']:raise ValueError('Incompatible witness settings: '+r['case'])
        feasible=witness['clean'] and witness['passTime']>=0 and witness['onStraight']
        deadline=1.25*witness['passTime']+1 if feasible else None
        r.update(witnessTime=witness['passTime'],deadline=deadline,feasibilityDemonstrated=feasible,passed=feasible and r['clean'] and r['onStraight'] and 0<=r['passTime']<=deadline)
        rows.append(r)
    standard=[r for r in rows if r['case']=='standard'];times=sorted(r['time'] for r in standard)
    repeatability=len(times)==3 and (times[-1]-times[0])/times[1]<=.005
    out=dict(scope='Physical passing witness, not an optimal trajectory or full-field/feel acceptance. Matching runtime settings do not independently prove identical physics source.',rows=rows,repeatabilityPassed=repeatability,passed=sum(r['passed'] for r in rows)+int(repeatability),total=len(rows)+1)
    (args.folder/'analysis.json').write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(dict(passed=out['passed'],total=out['total'],results=[{k:v for k,v in r.items() if k!='signature'} for r in rows]),indent=2))
    raise SystemExit(0 if out['passed']==out['total'] else 1)
