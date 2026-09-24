"""Compare displayed lap history with the player's physical start/finish passages."""
import argparse
import json
import re
from pathlib import Path
from runtime_log_errors import runtime_error_lines

p=argparse.ArgumentParser();p.add_argument('folder',type=Path);a=p.parse_args()
read=lambda path:json.loads(path.read_text(encoding='utf-8-sig'))
race=read(a.folder/'race.json');view=read(a.folder/'hud/lap-history.json')
gates=[v for v in race['checkpointPassages'] if v['id']==0 and v['gate']==0]
completed=view['completed']
durations=[gates[i+1]['time']-gates[i]['time'] for i in range(len(gates)-1)]
displayed=[]
for row in view['rows']:
    if not row:continue
    m=re.fullmatch(r'L(\d+)\s+(\d+):(\d+\.\d{3})',row)
    if not m:raise ValueError('Unrecognised displayed lap: '+row)
    displayed.append((int(m[1]),int(m[2])*60+float(m[3])))
checks={
    'normalExit':read(a.folder/'exit.json')['exitCode']==0,
    'noRuntimeErrors':not runtime_error_lines(a.folder),
    'fullFieldFinished':race['carCount']==race['finished']==24,
    'actualSmallResolution':(view['width'],view['height'])==(1280,720),
    'finalLapNotice':view['notice']=='FINAL LAP' and completed==race['requiredLaps']-1,
    'lastLapMatchesPhysicalPassages':abs(view['lastLap']-durations[completed-1])<.001,
    'newestCompletedLapsOnly':[i for i,t in displayed]==list(range(completed,max(0,completed-3),-1)),
    'displayedTimesMatchPassages':all(abs(t-durations[i-1])<=.001 for i,t in displayed),
    'runtimeInputAndLapChecks':all(token in (a.folder/'run.log').read_text(encoding='utf-8-sig') for token in ['LITTLE_ROAD_HUD_REVIEW_CAPTURED','LITTLE_ROAD_HUD_LAP_HISTORY_PASSED'])
}
out=dict(passed=all(checks.values()),checks=checks,physicalPlayerLapSeconds=durations,displayed=displayed,
         limitation='Automated data/input and small-window capture; map readability needs image inspection and physical-controller acceptance is separate.')
(a.folder/'hud-lap-analysis.json').write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8')
print(json.dumps(out,indent=2));raise SystemExit(0 if out['passed'] else 1)
