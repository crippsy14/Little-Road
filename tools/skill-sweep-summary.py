# Summarise a skill sweep (run-pace-parallel arms labelled sk<skill>): per skill and seed, finishers, assisted recoveries
# (with where), lap times, contacts and time off the ground. Usage: python tools/skill-sweep-summary.py <study> [track]
import json,glob,os,sys,collections,statistics,re
study=sys.argv[1];track=sys.argv[2] if len(sys.argv)>2 else '*'
rows=[]
for f in sorted(glob.glob(f'review/unity-35/frame-cost-{study}/{track}-s*-sk*/race.json')):
    d=json.load(open(f,encoding='utf-8'));name=os.path.basename(os.path.dirname(f));t,seed,arm=name.rsplit('-',2)
    fin=[c for c in d['cars'] if c['finished']];times=sorted(c['time'] for c in fin)
    rec=d['assistedRecoveries'];why=collections.Counter(r['reason'][:22] for r in rec)
    where=collections.Counter(round(r['progressBefore']/50)*50 for r in rec)
    m=re.match(r'sk(\d+)(.*)',arm);skill,variant=int(m.group(1)),m.group(2)
    rows.append((skill,t+('/'+variant if variant else ''),seed,len(fin),len(d['cars']),len(rec),dict(why),dict(where.most_common(4)),times[0] if times else 0,statistics.mean(times) if times else 0,sum(c['contacts'] for c in d['cars']),max(c['unsupportedSeconds'] for c in d['cars'])))
rows.sort()
print('skill track     seed  finished  recov  winner  mean   contacts  max-airborne  recovery reasons / where (lap metres)')
for r in rows:print(f'{r[0]:>4}  {r[1]:<9} {r[2]:<5} {r[3]:>2}/{r[4]:<2}    {r[5]:>4}  {r[8]:6.1f} {r[9]:6.1f}  {r[10]:>6}  {r[11]:8.1f}      {r[6]} {r[7]}')
