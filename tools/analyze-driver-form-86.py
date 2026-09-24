"""Account for every predeclared seed; report physical lap dispersion honestly."""
import json,statistics
from pathlib import Path
from runtime_log_errors import runtime_error_lines
root=Path(__file__).resolve().parents[1]/'review/unity-35/driver-form-86'
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
rows=[]
for seed in range(2000,2020):
    pair=[]
    for mode in ('steady','variable'):
        folder=root/f'{seed}-{mode}'
        race=read(folder/'race.json');track=read(folder/'race.track.json');driver=read(folder/'race.drivers.json')['drivers'][0]
        row=dict(seed=seed,mode=mode,form=driver['formSample'],braking=driver['effectiveBraking'],driverId=driver['id'],
                 time=race['cars'][0]['time'] if race['finished']==1 else None,contacts=len(race['impacts']),outside=sum(r['outsideBodySamples'] for r in track['results']),
                 recovery=sum(c['physicalRecoveries'] for c in race['cars'])+len(race['assistedRecoveries']),normalExit=read(folder/'exit.json')['exitCode']==0 and not runtime_error_lines(folder))
        rows.append(row);pair.append((row,race,track))
    assert pair[0][0]['form']==pair[1][0]['form'] and pair[0][0]['driverId']==pair[1][0]['driverId']
    assert pair[0][2]['tuning']==pair[1][2]['tuning']
    assert all(pair[0][1][k]==pair[1][1][k] for k in ('seed','trackLength','trackId','carCount','requiredLaps','equipment','gridMode'))
summary={}
for mode in ('steady','variable'):
    subset=[r for r in rows if r['mode']==mode];times=[r['time'] for r in subset if r['time'] is not None]
    summary[mode]=dict(runs=len(subset),completed=len(times),meanSeconds=statistics.mean(times),sampleStdSeconds=statistics.stdev(times),minSeconds=min(times),maxSeconds=max(times),
        contacts=sum(r['contacts'] for r in subset),outside=sum(r['outside'] for r in subset),recovery=sum(r['recovery'] for r in subset))
checks=dict(all40Accounted=len(rows)==40,all40Finish=all(r['time'] is not None for r in rows),allNormal=all(r['normalExit'] for r in rows),
            allClean=all(r['contacts']==0 and r['outside']==0 and r['recovery']==0 for r in rows),
            variableHasWiderMeasuredSpread=summary['variable']['sampleStdSeconds']>summary['steady']['sampleStdSeconds'])
out=dict(scope='20 paired solo Bayview seeds2000..2019, same club car/grid. Exploratory physical lap dispersion; no claim of traffic finish-rank distributions or driver enjoyment.',checks=checks,summary=summary,rows=rows)
(root/'analysis.json').write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8')
lines=['# Physical race-form sample','',out['scope'],'','| Consistency | Finished | Mean | Sample SD | Range |','|---|---:|---:|---:|---:|']
for mode,s in summary.items():lines.append(f"| {mode} | {s['completed']}/20 | {s['meanSeconds']:.6f}s | {s['sampleStdSeconds']:.6f}s | {s['minSeconds']:.6f}–{s['maxSeconds']:.6f}s |")
lines+=['',f'Checks: {checks}.','', 'Race form changes only the braking estimate in this slice. Broader skill, corner-error and familiarity systems remain unimplemented.']
(root/'analysis.md').write_text('\n'.join(lines)+'\n',encoding='utf-8');print(json.dumps(dict(checks=checks,summary=summary),indent=2))
