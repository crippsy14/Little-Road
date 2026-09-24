"""Archive source and assembly identity for an already completed Unity build."""
import argparse,hashlib,json,shutil
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('version',type=int);a=p.parse_args()
root=Path(__file__).resolve().parents[1];v=a.version
review=root/'review/unity-35';target=review/f'source-{v}'
assert not target.exists(),'Choose a new source archive; do not overwrite.'
exe=(root/f'../little-road-context-{v}-review/Little Road Unity.exe').resolve()
assembly=exe.parent/'Little Road Unity_Data/Managed/Assembly-CSharp.dll'
log=review/f'build-context-{v}.log'
check=next(x for x in log.read_text(encoding='utf-8-sig').splitlines() if x.startswith('LITTLE_ROAD_BUILD_OK'))
hashfile=lambda f:hashlib.sha256(f.read_bytes()).hexdigest()
files=sorted((root/'unity/Assets/Scripts').glob('*.cs'))+sorted((root/'unity/Assets/Editor').glob('*.cs'))
files += [root/'tools'/n for n in ['run-technical-trial.ps1','run-coasting-regressions.ps1','run-drafting-70.ps1']]
rows=[]
for f in files:
 rel=f.relative_to(root);out=target/rel;out.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(f,out)
 rows.append(dict(file=rel.as_posix(),sha256=hashfile(f)))
shutil.copy2(root/'review/unity-01/editor-checks.json',review/f'build-context-{v}-checks.json')
(review/f'prototype-{v}-manifest.json').write_text(json.dumps(dict(version=v,export=str(exe),assemblyHash=hashfile(assembly),buildCheckLine=check,sourceFiles=rows),indent=2)+'\n',encoding='utf-8')
print(check);print('Archived',len(rows),'files')
