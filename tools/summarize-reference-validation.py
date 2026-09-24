"""Combine the frozen candidate matrix, repeats and held-out physical run."""
import hashlib
import json
from pathlib import Path
import sys

from importlib.util import spec_from_file_location, module_from_spec

spec=spec_from_file_location('matrix_analysis',Path(__file__).with_name('analyze-racing-line-matrix.py'))
matrix=module_from_spec(spec);spec.loader.exec_module(matrix)
root=Path(sys.argv[1]);read=matrix.read
old=read(root/'candidate-matrix-03/comparison.json')
validated=read(root/'smooth-validation/comparison.json')
if not read(root/'smooth-validation/manifest.json')['complete']:
    raise SystemExit('Repeat validation is still active.')
held=matrix.summarize(dict(name='heldout-street-line',loadout='street',variant='smooth-v2',repeat=1,
                           report=str((root/'heldout-street-line/race.json').resolve()),exitCode=None))
checks=list(validated['checks'])
checks.append(dict(name='Held-out street equipment and centre start finish cleanly',passed=held['clean']))
comparisons=[]
for setup in ('stock','club'):
    before=next(r for r in old['runs'] if r['loadout']==setup and r['variant']=='mincurve')
    runs=[r for r in validated['runs'] if r['loadout']==setup]
    best=[r['bestLap'] for r in runs]
    traces=[]
    for run in runs:
        trace=read(Path(run['report']).with_suffix('.track.json'))['samples']
        traces.append(hashlib.sha256(json.dumps(trace,sort_keys=True).encode()).hexdigest())
    checks.append(dict(name=setup+': three independent launches have identical recorded driving traces',passed=len(traces)==3 and len(set(traces))==1))
    checks.append(dict(name=setup+': every smooth run improves the previous best geometry candidate',passed=all(t<before['bestLap'] for t in best)))
    comparisons.append(dict(setup=setup,previousBestSeconds=before['bestLap'],smoothBestSeconds=min(best),gainSeconds=before['bestLap']-min(best),repeatSpreadSeconds=max(best)-min(best),tracesIdentical=len(set(traces))==1,
                            withinOneMetrePercent=runs[0]['adherenceTime'],maxBodyOffset=max(r['maxBodyOffset'] for r in runs)))
summary=dict(scope='Bounded solo Cedar validation, not completion of the overall racing-AI plan or campaign',
             assemblyHash=validated['assemblyHash'],candidate='cedar-smooth-v2',passed=sum(c['passed'] for c in checks),total=len(checks),checks=checks,comparisons=comparisons,heldOut=held,
             limitations=['Three repeated launches per equipment set, one different equipment/start case; rare failures and other tracks are not established.',
                          'Reference is deliberately deterministic. Driver form, ability, traffic, physical recovery and pit strategy remain later work.',
                          'Boundaries sampled at 10 Hz; no continuous no-clipping claim.',
                          'The original ten-run matrix has complete reports and terminated players, but its first harness lost process exit codes. The corrected repeat harness preserves and verifies exit code 0.',
                          'The independent held-out player wrote a complete report and shut down; its exit code was not retained by the launcher.'])
(root/'reference-validation.json').write_text(json.dumps(summary,indent=2)+'\n')
print(json.dumps({k:summary[k] for k in ('passed','total','comparisons')},indent=2))
if summary['passed']!=summary['total']:raise SystemExit(1)
