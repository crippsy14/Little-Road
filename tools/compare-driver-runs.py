"""Compare completed field evidence without confusing batch timing with rendering performance."""
import argparse
import importlib.util
import json
from pathlib import Path


def read(p):
    return json.loads(p.read_text(encoding='utf-8-sig'))


def load_tool(name):
    spec=importlib.util.spec_from_file_location(name,Path(__file__).with_name(name+'.py'))
    module=importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def compare(folders):
    duty=load_tool('analyze-driving-duty')
    flow=load_tool('analyze-checkpoint-flow')
    runs=[]
    for folder in folders:
        race=read(folder/'race.json')
        track=read(folder/'race.track.json')
        launch=read(folder/'launch.json') if (folder/'launch.json').exists() else {}
        if race['carCount']<24 or race['requiredLaps']!=1:
            raise ValueError('This comparison requires standing first-lap fields of at least 24 cars.')
        if (folder/'exit.json').exists() and read(folder/'exit.json')['exitCode']!=0:
            raise ValueError(f'Non-normal process exit: {folder}')
        signature={key:race[key] for key in ('trackId','trackVersion','trackLength','trackWidth','carCount','requiredLaps','gridMode','seed','collisionMode','physicsStepSeconds')}
        signature['equipmentById']=sorted((c['id'],c['equipment']) for c in race['cars'])
        signature['reference']=track['reference']
        if runs and signature!=runs[0]['comparisonSignature']:
            raise ValueError(f'Incomparable track, grid, equipment, reference or physics settings: {folder}')
        if launch.get('trafficCase') not in (None,'','field'):
            raise ValueError('Mid-course fixtures cannot be compared as standing races.')
        for key in ('physicalRecovery','sweepContacts','noImmediateSafety'):
            if runs and key in launch and key in runs[0]['launch'] and launch[key]!=runs[0]['launch'][key]:
                raise ValueError(f'Incomparable launch option {key}: {folder}')
        audit=duty.audit(folder)
        passages,precision=flow.passages(race,track)
        # Equal populations per sector: do not make a stalled candidate look
        # faster by silently averaging only its successful cars.
        sector=[]
        for gate in range(1,13):
            times=[v[gate]-v[gate-1] for v in passages.values() if gate in v and gate-1 in v]
            sector.append(dict(gate=gate,completeCars=len(times),meanSeconds=sum(times)/len(times) if len(times)==race['carCount'] else None))
        finished=[c['time'] for c in race['cars'] if c['finished']]
        live=launch.get('visible') is True and launch.get('playbackRate')==1
        runs.append(dict(folder=str(folder),launch=launch,comparisonSignature=signature,driver=race['driverPolicy'],
                         finished=len(finished),lastFinish=max(finished) if len(finished)==race['carCount'] else None,
                         totalCarSeconds=sum(finished) if len(finished)==race['carCount'] else None,
                         meanCarSeconds=sum(finished)/race['carCount'] if len(finished)==race['carCount'] else None,
                         perCarContactRecords=sum(c['contacts'] for c in race['cars']),
                         maxImpact=max(c['maxImpact'] for c in race['cars']),
                         physicalRecoveries=sum(c['physicalRecoveries'] for c in race['cars']),
                         departingCars=sum(c['outsideBodySamples']>0 for c in track['results']),
                         brakePercent=audit['aggregatePercent']['braking'],linePercent=audit['aggregatePercent']['onLine'],
                         fullThrottlePercent=audit['aggregatePercent']['fullThrottle'],
                         sectors=sector,passagePrecision=precision,
                         frameP95Ms=race['p95FrameMs'] if live else None,
                         performanceScope='Visible 1x run; check resolution, graphics mode and concurrent load before comparison.' if live else 'Batch/accelerated/unknown visibility: rendering comparison excluded.'))
    return dict(scope='Settings equality is necessary but cannot prove unchanged physics source. Verify build provenance and source changes. '
                'Lower braking and higher line adherence alone do not establish better racing. Review contacts, sector pace and moving behaviour together.',runs=runs)


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('folders',nargs='+',type=Path)
    parser.add_argument('--output',required=True,type=Path)
    args=parser.parse_args()
    if len(args.folders)<2:
        parser.error('Provide at least two evidence folders.')
    if args.output.exists():
        parser.error('Choose a new comparison directory to preserve earlier evidence.')
    result=compare([p.resolve() for p in args.folders])
    args.output.mkdir(parents=True)
    (args.output/'comparison.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
    lines=['# Driver comparison','',result['scope'],'',
           '| Run | Finished | Last finish | Brake | On line | Contacts¹ | Peak impact | Departing cars | Recoveries |',
           '|---|---:|---:|---:|---:|---:|---:|---:|---:|']
    for r in result['runs']:
        finish=f"{r['lastFinish']:.2f} s" if r['lastFinish'] is not None else 'Incomplete'
        lines.append(f"| {Path(r['folder']).name} | {r['finished']} | {finish} | {r['brakePercent']:.1f}% | {r['linePercent']:.1f}% | {r['perCarContactRecords']} | {r['maxImpact']:.2f} m/s | {r['departingCars']} | {r['physicalRecoveries']} |")
    lines+=['','¹ Contacts are per-car records, not unique incidents. Peak impact is reported relative speed.','',
            '## Collective completion time','',
            'Sum every entrant’s official first-lap finish time after green. Compare identical populations and starts; missing finishers make the total incomplete. Lower is better alongside clean racing.','',
            '| Run | Total car-time | Mean per car |','|---|---:|---:|']
    for r in result['runs']:
        total=f"{r['totalCarSeconds']:.2f} car-s" if r['totalCarSeconds'] is not None else 'Incomplete'
        mean=f"{r['meanCarSeconds']:.2f} s" if r['meanCarSeconds'] is not None else 'Incomplete'
        lines.append(f"| {Path(r['folder']).name} | {total} | {mean} |")
    lines+=['',
            '## Sector times','', '| Sector ending at gate | '+' | '.join(Path(r['folder']).name for r in result['runs'])+' |',
            '|---|'+'---:|'*len(result['runs'])]
    for i in range(12):
        values=[]
        for r in result['runs']:
            s=r['sectors'][i]
            values.append(f"{s['meanSeconds']:.2f} s" if s['meanSeconds'] is not None else f"Incomplete ({s['completeCars']})")
        lines.append(f"| {i+1} | "+' | '.join(values)+' |')
    lines+=['','## Rendering evidence','']
    for r in result['runs']:
        lines.append(f"- {Path(r['folder']).name}: {r['performanceScope']}"+(f" Recorded p95 {r['frameP95Ms']:.2f} ms." if r['frameP95Ms'] is not None else ''))
    (args.output/'comparison.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
    print(args.output/'comparison.md')
