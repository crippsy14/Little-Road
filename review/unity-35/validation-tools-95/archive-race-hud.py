"""Freeze terminal race-HUD review evidence without promoting a build."""
import argparse,hashlib,json,shutil
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('version',type=int);a=p.parse_args()
root=Path(__file__).resolve().parents[1];review=root/'review/unity-35';evidence=review/f'race-hud-{a.version}'
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
fingerprint=lambda p:dict(file=p.relative_to(root).as_posix(),sha256=hashlib.sha256(p.read_bytes()).hexdigest())
for name in ['bayview-hud','cedar-hud','bayview-laps-small','view-off','view-on','view-off-1080p','view-on-1080p']:
    assert read(evidence/name/'exit.json')['exitCode']==0,name
for name in ['bayview-hud','cedar-hud','view-on','view-on-1080p']:
    assert read(evidence/name/'motion-parity.json')['passed'],name
assert read(evidence/'bayview-laps-small/hud-lap-analysis.json')['passed']
for name in ['view-off','view-on','view-off-1080p','view-on-1080p']:
    assert (evidence/name/'driver-frame-analysis.json').exists()
    if name.endswith('-1080p'):assert read(evidence/name/'driver-frame-analysis.json')['checks']['allFramesAt1920x1080']
target=review/f'validation-tools-{a.version}';target.mkdir(exist_ok=True)
for name in ['run-race-hud.ps1','run-technical-trial.ps1','verify-context-race-parity.py','analyze-race-hud-laps.py','analyze-driver-frames.py','runtime_log_errors.py','archive-race-hud.py']:
    shutil.copy2(root/'tools'/name,target/name)
for name in [f'race-hud-{a.version}-expectations.md',f'race-hud-{a.version}-review.md']:
    shutil.copy2(review/name,target/name)
for source in (root/'unity/Assets/Resources').rglob('*.json'):
    dest=target/'resources'/source.relative_to(root/'unity/Assets/Resources');dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,dest)
path=review/f'prototype-{a.version}-manifest.json';m=read(path)
for item in m['sourceFiles']:assert fingerprint(root/item['file'])['sha256']==item['sha256'],item['file']
m['validationTools']=[fingerprint(p) for p in sorted(target.rglob('*')) if p.is_file()]
m['evidenceFiles']=[fingerprint(p) for p in sorted(evidence.rglob('*')) if p.is_file()]
m['retainedForNextMilestone']=True;m['productionPromoted']=False
m['reviewDocument']=f'review/unity-35/race-hud-{a.version}-review.md'
m['limitations']=['Frame budgets and physical-controller acceptance remain open; inspect full review.', 'Campaign scope remains active; this is a presentation milestone only.']
path.write_text(json.dumps(m,indent=2)+'\n',encoding='utf-8')
print(f"Archived {len(m['validationTools'])} tools/resources and {len(m['evidenceFiles'])} evidence files; frozen sources match.")
