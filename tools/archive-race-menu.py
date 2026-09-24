"""Archive a final menu milestone and its earlier iterations without promotion."""
import argparse,hashlib,json,shutil
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('version',type=int);a=p.parse_args()
root=Path(__file__).resolve().parents[1];review=root/'review/unity-35';evidence=review/f'race-preview-{a.version}'
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
fingerprint=lambda p:dict(file=p.relative_to(root).as_posix(),sha256=hashlib.sha256(p.read_bytes()).hexdigest())
for run in ['bayview-preview','adventure','reload']:assert read(evidence/run/'exit.json')['exitCode']==0,run
assert read(evidence/'bayview-preview/motion-parity.json')['passed']
assert len(read(evidence/'bayview-preview/preview/review.json')['checks'])>=212
assert (evidence/'adventure-analysis.json').exists()
target=review/f'validation-tools-{a.version}';target.mkdir(exist_ok=True)
for name in ['run-race-menu.ps1','run-technical-trial.ps1','run-adventure-integration.ps1','analyze-adventure-integration.py','verify-context-race-parity.py','runtime_log_errors.py','archive-race-menu.py']:
    shutil.copy2(root/'tools'/name,target/name)
for name in [f'race-menu-{a.version}-expectations.md',f'race-menu-{a.version}-review.md']:
    shutil.copy2(review/name,target/name)
for source in list((root/'unity/Assets/Resources').rglob('*.json'))+list((root/'unity/Assets/Resources/UI').glob('*')):
    dest=target/'resources'/source.relative_to(root/'unity/Assets/Resources');dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,dest)
shutil.copy2(root/'references/ui/coastal-race-postcard-v1.md',target/'coastal-race-postcard-v1.md')
path=review/f'prototype-{a.version}-manifest.json';m=read(path)
for item in m['sourceFiles']:assert fingerprint(root/item['file'])['sha256']==item['sha256'],item['file']
m['validationTools']=[fingerprint(p) for p in sorted(target.rglob('*')) if p.is_file()]
m['evidenceFiles']=[fingerprint(p) for p in sorted(evidence.rglob('*')) if p.is_file()]
m['retainedForNextMilestone']=True;m['productionPromoted']=False;m['nextPriority']='Measured performance, then remaining UI/campaign scope.'
m['reviewDocument']=f'review/unity-35/race-menu-{a.version}-review.md'
m['limitations']=['Physical controller/mouse and small-window acceptance open.', 'Connected recovery defect retained in analysis.', 'Rendered performance budgets remain unmet; hidden UI tests are not performance acceptance.']
path.write_text(json.dumps(m,indent=2)+'\n',encoding='utf-8')
history={96:'Plain cream/teal menu superseded after user requested more illustration; 200 preview checks and connected17/18.',97:'Illustrated preview passed200 checks and recorded parity; no connected run before interactive expansion.',98:'Build passed, runtime preview failed at missing turbo_club display asset; process explicitly stopped. Corrected in99.'}
if a.version>=101:
    history.update({99:'212 preview checks, recorded parity and connected17/18. User requested an actual3D showroom and further playfulness; superseded by101.',100:'498 editor checks; runtime failed with exit2 because the new Unlit shader was stripped. Corrected in101 using the retained Lit/emission variant.'})
for v,status in history.items():
    path=review/f'prototype-{v}-manifest.json';m=read(path);m['reviewStatus']=status;m['productionPromoted']=False
    m['reviewDocument']=f'review/unity-35/race-menu-{a.version}-review.md'
    m['evidenceFiles']=[fingerprint(p) for p in sorted((review/f'race-preview-{v}').rglob('*')) if p.is_file()]
    path.write_text(json.dumps(m,indent=2)+'\n',encoding='utf-8')
print('Archived menu evidence, tools, generated art and prompt; current source hashes match frozen build.')
