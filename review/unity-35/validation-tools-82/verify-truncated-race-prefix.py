"""Exact overlapping early-run telemetry; never certifies completion or later behavior."""
import json,sys
from pathlib import Path
from runtime_log_errors import runtime_error_lines
base,capture=map(Path,sys.argv[1:3]);read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
a,b=[read(p/'race.json') for p in (base,capture)];ta,tb=[read(p/'race.track.json') for p in (base,capture)]
la,lb=[read(p/'launch.json') for p in (base,capture)]
end=min(max(s['time'] for s in ta['samples']),max(s['time'] for s in tb['samples']))
checks=dict(sameAssembly=la['assemblyHash']==lb['assemblyHash'],sameSetup=all(a[k]==b[k] for k in ['trackId','trackVersion','seed','carCount','requiredLaps','driverPolicy','equipment','gridMode']),sameEquipment=[c['equipment'] for c in a['cars']]==[c['equipment'] for c in b['cars']],coversRequestedCapture=end>=lb['duration']-.15,normalExit=read(capture/'exit.json')['exitCode']==0,noErrors=not runtime_error_lines(capture))
for key in ['motion','checkpointPassages','impacts','assistedRecoveries']:
    checks[key]=[s for s in a[key] if s['time']<=end]==[s for s in b[key] if s['time']<=end]
for key in ['samples','contextSamples']:
    checks[key]=[s for s in ta[key] if s['time']<=end]==[s for s in tb[key] if s['time']<=end]
out=dict(scope='Exact recorded prefix only; truncated diagnostic does not prove race completion or equivalence beyond its endpoint.',base=str(base),capture=str(capture),throughRaceSeconds=end,checks=checks,passed=all(checks.values()))
(capture/'prefix-parity.json').write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8');print(json.dumps(out,indent=2))
raise SystemExit(0 if out['passed'] else 1)
