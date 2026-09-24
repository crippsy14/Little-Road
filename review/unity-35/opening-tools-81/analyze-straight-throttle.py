"""Time-weighted applied throttle demand on declared opening-straight sections."""
import json,sys
from pathlib import Path
import importlib.util
spec=importlib.util.spec_from_file_location('opening',Path(__file__).with_name('analyze-opening-straight.py'))
opening=importlib.util.module_from_spec(spec);spec.loader.exec_module(opening)
read=opening.read

def exposure(rows,start,end):
    values=dict(seconds=0.,throttleSeconds=0.,fullThrottleSeconds=0.,partialThrottleSeconds=0.,coastSeconds=0.,brakeSeconds=0.)
    assert end>start
    for a,b in zip(rows,rows[1:]):
        dt=max(0,min(end,b['time'])-max(start,a['time']))
        if dt==0:continue
        p=a['pedal'];assert -1<=p<=1
        values['seconds']+=dt;values['throttleSeconds']+=max(0,p)*dt
        values['fullThrottleSeconds' if p>=.95 else 'partialThrottleSeconds' if p>.05 else 'coastSeconds' if p>=-.01 else 'brakeSeconds']+=dt
    assert abs(values['seconds']-(end-start))<1e-5,'Incomplete telemetry interval'
    return values

def percentages(v):
    n=v['seconds'];assert n>0
    return dict(v,averageThrottlePercent=100*v['throttleSeconds']/n,fullThrottlePercent=100*v['fullThrottleSeconds']/n,partialThrottlePercent=100*v['partialThrottleSeconds']/n,coastPercent=100*v['coastSeconds']/n,brakePercent=100*v['brakeSeconds']/n)

def analyze(root):
    data={}
    for mode in ['off','on']:
        r=read(root/mode/'race.json');t=read(root/mode/'race.track.json');per=[]
        for car in r['cars']:
            rows=[s for s in t['samples'] if s['id']==car['id'] and s['time']>=0]
            first=rows[0]['time'];zero=opening.crossing(rows,0);early=opening.crossing(rows,200);end=opening.crossing(rows,280)
            for phase,a,b in [('grid-launch',first,zero),('straight-0-200m',zero,early),('approach-200-280m',early,end),('whole-opening',first,end)]:
                per.append(dict(id=car['id'],equipment=car['equipment'],phase=phase,**percentages(exposure(rows,a,b))))
        cohorts={}
        for phase in ['grid-launch','straight-0-200m','approach-200-280m','whole-opening']:
            cohorts[phase]={}
            for label,ids in [('preview',[0]),('club',[c['id'] for c in r['cars'] if c['equipment']=='club']),('street',[c['id'] for c in r['cars'] if c['equipment']=='street']),('field',[c['id'] for c in r['cars']])]:
                rows=[x for x in per if x['phase']==phase and x['id'] in ids]
                total={k:sum(x[k] for x in rows) for k in ['seconds','throttleSeconds','fullThrottleSeconds','partialThrottleSeconds','coastSeconds','brakeSeconds']}
                cohorts[phase][label]=dict(carIds=ids,**percentages(total))
        data[mode]=dict(perCar=per,cohorts=cohorts)
    out=dict(scope='Applied gas-pedal demand, not measured acceleration or percent engine power. Every car is measured over its own complete spatial section. Groups use time-weighted car-seconds; preview is also included in club and field.',precision='10 Hz observations held to the next sample, clipped at interpolated spatial gates. Approximate exposure, not physics-tick pedal integration. Full throttle >=95%, partial >5%, coast -1% to 5%, braking <-1%. Average throttle includes zero during coast/brake.',interpretation='No target of 100% everywhere: traffic occupancy, speed limits, grip and braking distance must still be respected. The last 80 m is reported separately because corner preparation may legitimately require braking.',results=data)
    (root/'straight-throttle-analysis.json').write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8')
    lines=['# Opening-straight throttle demand','',out['scope'],'',out['precision'],'',out['interpretation'],'','| Section / group | Average throttle ref / dynamic | Full-throttle time ref / dynamic | Brake time ref / dynamic |','|---|---:|---:|---:|']
    for phase in ['grid-launch','straight-0-200m','approach-200-280m','whole-opening']:
        for label in ['preview','club','street','field']:
            a,b=[data[m]['cohorts'][phase][label] for m in ['off','on']]
            values=[f"{a[k]:.1f}% / {b[k]:.1f}%" for k in ['averageThrottlePercent','fullThrottlePercent','brakePercent']]
            lines.append(f"| {phase} / {label} | {' | '.join(values)} |")
    (root/'straight-throttle-analysis.md').write_text('\n'.join(lines)+'\n',encoding='utf-8');return out

if __name__=='__main__':
    root=Path(sys.argv[1]);analyze(root);print((root/'straight-throttle-analysis.md').read_text(encoding='utf-8'))
