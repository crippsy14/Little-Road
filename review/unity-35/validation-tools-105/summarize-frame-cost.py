"""Retain the matched rendered comparisons and their open acceptance gates."""
import argparse,hashlib,json,shutil
from pathlib import Path

p=argparse.ArgumentParser();p.add_argument('--study',type=int,required=True);p.add_argument('--baseline',type=int,required=True);p.add_argument('--candidate',type=int,required=True);args=p.parse_args()
root=Path(__file__).resolve().parents[1]
review=root/'review/unity-35';evidence=review/f'frame-cost-{args.study}'
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
fingerprint=lambda p:dict(file=p.relative_to(root).as_posix(),sha256=hashlib.sha256(p.read_bytes()).hexdigest())
summary={'baselineBuild':args.baseline,'candidateBuild':args.candidate,'comparisons':{},'performanceAccepted':False,
         'scope':'One sequential normal-speed 1080p rendered pair per track; no project editor, competing race, or screenshot capture during measurement. Recorded parity is fixture-scoped; no subjective smoothness claim.'}
for track in ['bayview','cedar']:
    a,b=[read(evidence/f'{track}-{arm}'/'driver-frame-analysis.json') for arm in ['before','after']]
    parity=read(evidence/f'{track}-after'/'motion-parity.json')
    assert parity['passed']
    for arm,data in zip(['before','after'],[a,b]):
        folder=evidence/f'{track}-{arm}'
        assert read(folder/'exit.json')['exitCode']==0
        assert data['realTimeRenderingEvidence'] and data['checks']['allFramesAt1920x1080'] and data['checks']['completeCapture']
        assert data['checks']['noObservedDriverAllocationAfter5s']
        assert not data['settings']['developmentBuild']
        race=read(folder/'race.json');assert race['finished']==race['carCount']==24
    assert a['settings']==b['settings']
    summary['comparisons'][track]={
        'recordedParity':True,'wholeAiMsPerTick':parity['meanAllAiMsPerSnapshotTick'],
        'wholeAiReductionPercent':parity['allAiWallCostReductionPercent'],
        'frameMs':{'before':a['observedFrameMs'],'after':b['observedFrameMs']},
        'worstOneSecondWindow':{'before':a['worstOneSecondWindow'],'after':b['worstOneSecondWindow']},
        'aiPerFrameMs':{'before':a['allAiMsPerRenderedFrame'],'after':b['allAiMsPerRenderedFrame']},
        'candidateChecks':b['checks']}
(evidence/'summary.json').write_text(json.dumps(summary,indent=2)+'\n',encoding='utf-8')
archive=review/f'validation-tools-{args.candidate}';archive.mkdir(exist_ok=True)
for name in ['run-frame-cost.ps1','run-technical-trial.ps1','analyze-driver-frames.py','verify-context-race-parity.py','runtime_log_errors.py','summarize-frame-cost.py']:
    shutil.copy2(root/'tools'/name,archive/name)
for name in [f'frame-cost-{args.study}-expectations.md',f'frame-cost-{args.candidate}-review.md']:
    shutil.copy2(review/name,archive/name)
for source in (root/'unity/Assets/Resources').rglob('*.json'):
    dest=archive/'resources'/source.relative_to(root/'unity/Assets/Resources');dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,dest)
path=review/f'prototype-{args.candidate}-manifest.json';m=read(path)
for item in m['sourceFiles']:assert fingerprint(root/item['file'])['sha256']==item['sha256'],item['file']
m.update(reviewDocument=f'review/unity-35/frame-cost-{args.candidate}-review.md',productionPromoted=False,retainedForNextMilestone=True,
         performanceAccepted=False,nextPriority='Continue measured planning/frame-stall reduction while preserving the accepted racing and UI.',
         validationTools=[fingerprint(p) for p in sorted(archive.rglob('*')) if p.is_file()],
         evidenceFiles=[fingerprint(p) for p in sorted(evidence.rglob('*')) if p.is_file()])
path.write_text(json.dumps(m,indent=2)+'\n',encoding='utf-8')
print(json.dumps({track:row['wholeAiReductionPercent'] for track,row in summary['comparisons'].items()},indent=2))
print('Archived frozen-source-matched evidence. Performance budgets remain open.')


