"""Opt-in integration diagnostics; never substitutes for field/contact/performance gates."""
import json,sys,statistics
from pathlib import Path
root=Path(sys.argv[1]);read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
expected=read(root/'road-expectations.json');race=read(root/'race.json');track=read(root/'race.track.json')
finishes={c['id']:c['time'] if c['finished'] else float('inf') for c in race['cars']}
rows=[s for s in track['contextSamples'] if 0<=s['time']<=finishes[s['id']]]
unresolved=[s for s in rows if s['roadUnresolved']]
checks={'normalExit':read(root/'exit.json')['exitCode']==0,
    'completeEnabledPopulation':{s['id'] for s in rows}==set(range(expected['requiredCars'])) and all(s['roadEnabled'] for s in rows),
    'noUnresolvedRaceSamples':len(unresolved)<=expected['maximumUnresolvedSamples']}
by_car={i:{'queries':max((s['roadCalls'] for s in rows if s['id']==i),default=0),
    'unresolvedSamples':sum(s['id']==i for s in unresolved),
    'firstUnresolved':next((s['time'] for s in unresolved if s['id']==i),None)} for i in range(race['carCount'])}
out={'scope':expected['scope'],'passed':sum(checks.values()),'total':len(checks),'checks':checks,
    'unresolvedSamples':len(unresolved),'unresolvedSampleCarSeconds':len(unresolved)*.1,
    'totalForecastCalls':sum(c['queries'] for c in by_car.values()),'cars':by_car,
    'firstUnresolvedSamples':unresolved[:24],
    'nominalPlanningCosts':track['contextDecisionCosts']}
(root/'road-analysis.json').write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8')
print(json.dumps({k:v for k,v in out.items() if k not in ('cars','firstUnresolvedSamples','nominalPlanningCosts')},indent=2))
raise SystemExit(0 if all(checks.values()) else 1)
