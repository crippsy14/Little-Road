"""Mechanical tow evidence and matched-field screening; no rendering acceptance."""
import json,sys
from pathlib import Path
from runtime_log_errors import runtime_error_lines
root=Path(sys.argv[1]);read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
checks={};physics={}
for scenario in ['draft-straight','draft-pullout']:
 pair={m:read(root/f'{scenario}-{m}'/'race.json') for m in ['off','on']}
 rows={m:[s for s in a['motion'] if s['id']==0 and s['time']<3] for m,a in pair.items()}
 at=lambda values,time:min(values,key=lambda x:abs(x['time']-time))
 times={m:next(x['time'] for x in samples if x['speed']>=32) for m,samples in rows.items()}
 drafts=[s for s in pair['on']['draftingSamples'] if s['id']==0]
 checks[scenario+' logs/exits']=all(read(root/f'{scenario}-{m}'/'exit.json')['exitCode']==0 and not runtime_error_lines(root/f'{scenario}-{m}') for m in pair)
 checks[scenario+' declared equal loadouts']=all(a['cars'][0]['equipment']==a['cars'][1]['equipment']=='club' for a in pair.values())
 checks[scenario+' no boost for leader']=all(x['reduction']==0 and x['leader']==-1 for x in pair['on']['draftingSamples'] if x['id']==1 and x['time']<3)
 checks[scenario+' bounded physical wake']=all(0<=x['reduction']<=.080001 and (x['reduction']==0 or x['leader']==1) for x in drafts)
 checks[scenario+' no contact during commands']=not any(x['time']<3 for a in pair.values() for x in a['impacts'])
 if scenario=='draft-straight':
  checks['physical 26–32m/s benefit']=.01<times['off']-times['on']<.25
  checks['physical distance benefit']=at(rows['on'],2.99)['progress']>at(rows['off'],2.99)['progress']
 else:
  checks['pull-out removes wake']=at(drafts,1)['reduction']>0 and at(drafts,2)['reduction']==0
 physics[scenario]=dict(timeTo32mps=times,speedAtOneSecond={m:at(x,1)['speed'] for m,x in rows.items()},progressAtThreeSeconds={m:at(x,2.99)['progress'] for m,x in rows.items()},draftAtOneSecond=at(drafts,1)['reduction'],draftAtTwoSeconds=at(drafts,2)['reduction'],scope='First three seconds of declared commands; fixture resumes AI after that. Partial-course final time is not a race metric.')
field={}
for m in ['off','on']:
 folder=root/m;a=read(folder/'race.json');t=read(folder/'race.track.json');frame=read(folder/'race.driver-frames.json');samples=[x for x in t['samples'] if x['time']>0]
 field[m]=dict(combinedSeconds=sum(c['time'] for c in a['cars']),previewSeconds=a['cars'][0]['time'],previewPlace=a['cars'][0]['place'],finished=a['finished'],physicalRecoveries=sum(c['physicalRecoveries'] for c in a['cars']),boundarySamples=sum(c['outsideBodySamples'] for c in t['results']),impactParticipantRecords=len(a['impacts']),peakImpact=max((i['relativeSpeed'] for i in a['impacts']),default=0),unresolvedSamples=sum(x['roadUnresolved'] for x in t['contextSamples']),brakingSamplePercent=100*sum(x['pedal']<-.01 for x in samples)/len(samples) if samples and 'pedal' in samples[0] else None,meanAiMsPerSnapshot=sum(x['decisionMs']+x['snapshotMs'] for x in frame['frames'][1:])/sum(x['snapshotCalls'] for x in frame['frames'][1:]),warmAllocatedBytes=sum(x['decisionBytes']+x['snapshotBytes']+x.get('forecastWorkerBytes',0) for x in frame['frames'] if x['simTime']>=5),draftNonzeroSamples=sum(x['reduction']>0 for x in a['draftingSamples']))
 checks[m+' field normal exit/no errors']=read(folder/'exit.json')['exitCode']==0 and not runtime_error_lines(folder)
a,b=field['off'],field['on']
checks.update({'disabled exact parity':read(root/'off/motion-parity.json')['passed'],'24 finish':b['finished']==24,'no physical recovery':b['physicalRecoveries']==0,'no increase in boundary samples':b['boundarySamples']<=a['boundarySamples'],'collective time within 2 percent':b['combinedSeconds']<=a['combinedSeconds']*1.02,'preview no more than 2 seconds slower':b['previewSeconds']<=a['previewSeconds']+2,'zero warm driver allocation':b['warmAllocatedBytes']==0,'actual field tow recorded':b['draftNonzeroSamples']>0})
out=dict(scope='Mechanical/first-seed screen only. Controlled regression, held-out seed and fixed-resolution rendering are separate gates.',checks=checks,passed=all(checks.values()),physics=physics,field=field)
(root/'analysis.json').write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8');print(json.dumps(out,indent=2))
raise SystemExit(0 if out['passed'] else 1)
