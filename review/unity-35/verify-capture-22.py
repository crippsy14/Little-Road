import json
from pathlib import Path
root=Path(__file__).resolve().parent
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
baseline=root/'field-14-visible';candidate=root/'field-22-approach-capture'
a=read(baseline/'race.json');b=read(candidate/'race.json')
at=read(baseline/'race.track.json');bt=read(candidate/'race.track.json')
caps=read(candidate/'race.approaches.json')['captures']
checks={key:a[key]==b[key] for key in ('motion','checkpointPassages','impacts','assistedRecoveries','overlapPeaks')}
checks.update({key:at[key]==bt[key] for key in ('samples','contextSamples')})
result=lambda car:{k:v for k,v in car.items() if k not in ('progress','offset')}
checks['carRaceResults']=[result(c) for c in a['cars']]==[result(c) for c in b['cars']]
checks['normalExit']=read(candidate/'exit.json')['exitCode']==0
checks['allFiveDeclaredCaptures']=len(caps)==5 and all(c['id']==16 and c['active'] and c['bodyAsset']=='coupe_car' and c['equipment']=='club' and abs(c['time']-(51+i*.5))<.01 for i,c in enumerate(caps))
out={'scope':'Observation-only capture parity against failed field 14. Final export positions are separate from race completion results.',
    'checks':checks,'passed':all(checks.values()),'baselineSimSeconds':a['simSeconds'],'candidateSimSeconds':b['simSeconds'],
    'captures':[{'time':c['time'],'speed':c['speed'],'offset':c['offset'],'path':c['path']} for c in caps]}
(candidate/'capture-parity.json').write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8')
print(json.dumps(out,indent=2))
raise SystemExit(0 if out['passed'] else 1)
