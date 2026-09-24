"""Freeze the complete110 pilot, including a rejected experiment's evidence."""
import hashlib,json,shutil
from pathlib import Path

root=Path(__file__).resolve().parents[1];review=root/'review/unity-35';study=review/'frame-cost-110'
read=lambda f:json.loads(f.read_text(encoding='utf-8-sig'))
fingerprint=lambda f:dict(file=f.relative_to(root).as_posix(),sha256=hashlib.sha256(f.read_bytes()).hexdigest())
comparisons={track:read(study/f'{track}-after/extra-candidate-comparison.json') for track in ('bayview','cedar')}
total=sum(c['collectiveGain'] for c in comparisons.values()) if all(c['collectiveGain'] is not None for c in comparisons.values()) else None
passed=all(c['passed'] for c in comparisons.values()) and total is not None and total>0
summary=dict(build=110,comparisons=comparisons,pooledCollectiveGain=total,pilotPassed=passed,
    decision='Eligible for broader matrix; not promoted' if passed else 'Reject broad third-candidate option; retain disabled baseline',
    interpretation='Two declared pilot pairs, not a six-seed acceptance matrix. All failures retained. Source and exact-replay diagnostic files belong to this milestone; diagnostic allocation runs do not establish frame performance.')
(study/'pilot-summary.json').write_text(json.dumps(summary,indent=2)+'\n',encoding='utf-8')
manifest=review/'prototype-110-manifest.json';m=read(manifest)
for entry in m['sourceFiles']:
    assert fingerprint(root/entry['file'])['sha256']==entry['sha256'],entry['file']
archive=review/'validation-tools-110';archive.mkdir(exist_ok=True)
for name in ('run-frame-cost.ps1','run-technical-trial.ps1','analyze-driver-frames.py','analyze-pedal-stages.py','analyze-pace-bottlenecks.py','verify-context-race-parity.py','compare-extra-candidate.py','runtime_log_errors.py','archive-pace-pilot.py'):
    shutil.copy2(root/'tools'/name,archive/name)
for name in ('pace-audit-110-plan.md','pace-audit-110-review.md'):
    shutil.copy2(review/name,archive/name)
for source in (root/'unity/Assets/Resources').rglob('*.json'):
    dest=archive/'resources'/source.relative_to(root/'unity/Assets/Resources');dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,dest)
m.update(reviewDocument='review/unity-35/pace-audit-110-review.md',productionPromoted=False,performanceAccepted=False,pilotPassed=passed,
    thirdCandidatePromoted=False,nextPriority='Investigate road-edge tracking and recovery lost time; the broad extra-candidate pilot is not a retained pace improvement.',
    validationTools=[fingerprint(f) for f in sorted(archive.rglob('*')) if f.is_file()],
    evidenceFiles=[fingerprint(f) for f in sorted(study.rglob('*')) if f.is_file()])
manifest.write_text(json.dumps(m,indent=2)+'\n',encoding='utf-8')
print(json.dumps(dict(pooledCollectiveGain=total,pilotPassed=passed,decision=summary['decision']),indent=2))
