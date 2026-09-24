"""Archive an experimental schedule study, including failed acceptance checks."""
import argparse,hashlib,json,shutil
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('version',type=int);a=p.parse_args()
root=Path(__file__).resolve().parents[1];review=root/'review/unity-35';study=review/f'frame-cost-{a.version}'
read=lambda path:json.loads(path.read_text(encoding='utf-8-sig'))
fp=lambda path:dict(file=path.relative_to(root).as_posix(),sha256=hashlib.sha256(path.read_bytes()).hexdigest())
manifest=review/f'prototype-{a.version}-manifest.json';m=read(manifest)
for row in m['sourceFiles']:assert fp(root/row['file'])['sha256']==row['sha256'],row['file']
summary={track:read(study/f'{track}-after'/'schedule-comparison.json') for track in ('bayview','cedar')}
summary['additionalComparisons']=[read(path) for path in sorted(study.glob('*-seed*-after/schedule-comparison.json'))]
summary['scope']='Experimental scheduling; preserve failed checks. Retention decision is in the linked review. No production/shortcut promotion or campaign acceptance.'
(study/'summary.json').write_text(json.dumps(summary,indent=2)+'\n',encoding='utf-8')
archive=review/f'validation-tools-{a.version}';archive.mkdir(exist_ok=True)
for name in ['run-frame-cost.ps1','run-technical-trial.ps1','run-schedule-validation.ps1','analyze-driver-frames.py','verify-context-race-parity.py','compare-planning-schedule.py','summarize-schedule-matrix.py','runtime_log_errors.py','archive-schedule-study.py']:
    shutil.copy2(root/'tools'/name,archive/name)
for name in [f'frame-cost-{a.version}-expectations.md',f'frame-cost-{a.version}-review.md']:
    shutil.copy2(review/name,archive/name)
for source in (root/'unity/Assets/Resources').rglob('*.json'):
    dest=archive/'resources'/source.relative_to(root/'unity/Assets/Resources');dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,dest)
m.update(reviewDocument=f'review/unity-35/frame-cost-{a.version}-review.md',productionPromoted=False,performanceAccepted=False,
    validationTools=[fp(p) for p in sorted(archive.rglob('*')) if p.is_file()],
    evidenceFiles=[fp(p) for p in sorted(study.rglob('*')) if p.is_file()])
manifest.write_text(json.dumps(m,indent=2)+'\n',encoding='utf-8')
print('Archived experiment, including all failed checks; no promotion.')
