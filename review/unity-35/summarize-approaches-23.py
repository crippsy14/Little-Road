import json,math
from pathlib import Path
root=Path(__file__).resolve().parent/'approaches-23'
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
analysis=read(root/'analysis.json');out={}
for name,s in analysis['summary'].items():
    data=read(root/name/'race.path-prediction.json');rows=data['samples']
    distance=lambda a,b:math.hypot(a['x']-b['x'],a['z']-b['z'])
    travel=sum(distance(a['actualPosition'],b['actualPosition']) for a,b in zip(rows,rows[1:]))
    out[name]={'sourceTime':data['sourceApproach']['time'],'entrySpeed':data['sourceApproach']['speed'],
        'sourcePedal':data['sourceApproach']['pedal'],'firstWitnessPedal':rows[0]['actualPedal'],
        'firstHeadingError':data['restoredHeadingError'],'travelledMetresInTwoSeconds':travel,
        'estimatedFinalPlanarSpeed':distance(rows[-1]['actualPosition'],rows[-2]['actualPosition'])/(rows[-1]['time']-rows[-2]['time']),
        'brakeFraction':sum(r['actualPedal']<-.05 for r in rows[:-1])/(len(rows)-1),
        'peakBodyOffset':s['maxActualBodyOffset'],'departureAt':s['actualDeparture'],'maxPredictionError':s['maxPositionError']}
result={'scope':'Two-second physical witnesses only. Travel is planar path length, not lap pace or route gain; final speed estimated from final native-step displacement. Opponent occupancy is absent.', 'summary':out}
(root/'witness-comparison.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print(json.dumps(result,indent=2))
