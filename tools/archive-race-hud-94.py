"""Archive the reviewed HUD evidence after all sequential runners have exited."""
import hashlib
import json
import shutil
from pathlib import Path

root = Path(__file__).resolve().parents[1]
review = root / 'review/unity-35'
read = lambda p: json.loads(p.read_text(encoding='utf-8-sig'))
fingerprint = lambda p: {'file': p.relative_to(root).as_posix(), 'sha256': hashlib.sha256(p.read_bytes()).hexdigest()}
for folder in ['race-hud-94/bayview-hud', 'race-hud-94/cedar-hud', 'race-hud-94/view-off', 'race-hud-94/view-on', 'race-preview-94/adventure', 'race-preview-94/reload']:
    assert read(review / folder / 'exit.json')['exitCode'] == 0, folder
assert (review / 'race-preview-94/adventure-analysis.json').exists()
target = review / 'validation-tools-94'
target.mkdir(exist_ok=True)
files = [root/'tools'/name for name in ['run-race-hud.ps1', 'run-technical-trial.ps1', 'run-adventure-integration.ps1', 'analyze-adventure-integration.py', 'verify-context-race-parity.py', 'analyze-driver-frames.py', 'runtime_log_errors.py', 'archive-race-hud-94.py']]
files += [review/'race-hud-94-expectations.md', review/'race-hud-94-review.md']
for p in files:
    shutil.copy2(p, target/p.name)
for p in (root/'unity/Assets/Resources').rglob('*.json'):
    dest = target/'resources'/p.relative_to(root/'unity/Assets/Resources')
    dest.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(p, dest)
manifest_path = review/'prototype-94-manifest.json'
m = read(manifest_path)
for item in m['sourceFiles']:
    assert fingerprint(root/item['file'])['sha256'] == item['sha256'], item['file']
m['validationTools'] = [fingerprint(p) for p in sorted(target.rglob('*')) if p.is_file()]
m['evidenceFiles'] = [fingerprint(p) for base in ['race-hud-94','race-preview-94'] for p in sorted((review/base).rglob('*')) if p.is_file()]
m['retainedForNextMilestone'] = True
m['productionPromoted'] = False
m['userVisualDirectionApproved'] = True
m['openDefects'] = ['Frame budgets fail in both clean-view on/off runs.', 'Connected recovery failures retained in integration analysis.', 'Physical DualSense, smaller windows and additional lighting review open.']
m['nextScope'] = 'docs/40-race-hud-reference-map.md and docs/39-driving-hud-and-playful-ui.md'
manifest_path.write_text(json.dumps(m,indent=2)+'\n',encoding='utf-8')
statuses = {89:'Superseded cream HUD; exact recorded parity passed; coarse timing corrected later.',90:'Superseded; synthetic L3 passed; performance comparison confounded by overlapping editor91.',91:'Build-only; no runtime acceptance.',93:'Build-only; corrected float fixture; no runtime acceptance.'}
for version, status in statuses.items():
    p = review/f'prototype-{version}-manifest.json'
    old = read(p)
    old['reviewStatus'] = status
    old['reviewDocument'] = 'review/unity-35/race-hud-94-review.md'
    old['productionPromoted'] = False
    p.write_text(json.dumps(old,indent=2)+'\n',encoding='utf-8')
print(f"Archived {len(m['validationTools'])} tools/resources and {len(m['evidenceFiles'])} evidence files; source hashes match frozen94.")
