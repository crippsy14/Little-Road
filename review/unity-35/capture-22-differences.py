import json
from pathlib import Path
root=Path(__file__).resolve().parent
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
a=read(root/'field-14-visible/race.json');b=read(root/'field-22-approach-capture/race.json')
differences=[]
for old,new in zip(a['cars'],b['cars']):
    changed={k:{'before':v,'after':new[k]} for k,v in old.items() if v!=new[k]}
    if changed:differences.append({'id':old['id'],'changes':changed})
out={'scope':'Exact field parity FAILED; preserve that result. This isolates the differences without relabelling the full field as equivalent.',
    'additionalImpacts':[i for i in b['impacts'] if i not in a['impacts']],
    'missingImpacts':[i for i in a['impacts'] if i not in b['impacts']],
    'carResultDifferences':differences,
    'targetCar16ResultExact':a['cars'][16]==b['cars'][16],
    'finishTimesExact':[c['time'] for c in a['cars']]==[c['time'] for c in b['cars']]}
(root/'field-22-approach-capture/capture-differences.json').write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8')
print(json.dumps(out,indent=2))
