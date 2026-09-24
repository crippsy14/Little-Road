"""Keep inherited screening plus72's stricter predeclared pace requirement."""
import json,subprocess,sys
from pathlib import Path
root=Path(sys.argv[1]);prefix=sys.argv[2] if len(sys.argv)>2 else ''
result=subprocess.run([sys.executable,str(Path(__file__).with_name('compare-open-space.py')),str(root),prefix],capture_output=True,text=True)
(root/(prefix+'comparison-output.txt')).write_text(result.stdout+result.stderr,encoding='utf-8')
p=root/(prefix+'comparison.json');d=json.loads(p.read_text(encoding='utf-8'))
a,b=d['results']['off'],d['results']['on']
d['scope']='Predeclared pace screen. Both aggregate and preview time must improve; physical checks, controlled cases and held-out robustness remain independent. Raw zero times mean DNF, never faster racing. Incomplete-field summed times are not comparable.'
fieldValid=a['finished']==b['finished']==24
previewValid=a['previewPlace']>0 and b['previewPlace']>0 and a['previewSeconds']>0 and b['previewSeconds']>0
d['paceComparisonValid']=dict(field=fieldValid,preview=previewValid)
d['checks'].update(collectiveImproves=fieldValid and b['combinedSeconds']<a['combinedSeconds'],previewImproves=previewValid and b['previewSeconds']<a['previewSeconds'],
                   collectiveWithin2Percent=fieldValid and b['combinedSeconds']<=a['combinedSeconds']*1.02,
                   previewWithin2Seconds=previewValid and b['previewSeconds']<=a['previewSeconds']+2)
d['passed']=all(d['checks'].values())
p.write_text(json.dumps(d,indent=2)+'\n',encoding='utf-8')
print(json.dumps(d,indent=2))
raise SystemExit(0 if d['passed'] else 1)
