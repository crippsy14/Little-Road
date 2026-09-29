import json,glob,os
# One line per Hillcrest race run: policy, finishers, collective time, assisted resets, contacts.
for f in sorted(glob.glob('review/hillcrest-race/*/race.json')):
    d=json.load(open(f));cs=d['cars'];fin=[c for c in cs if c['finished']]
    print(os.path.basename(os.path.dirname(f)),d['driverPolicy'],'fin %d/24'%len(fin),'collective',round(sum(c['time'] for c in fin),1),
          'resets',sum(c['recoveries'] for c in cs),'contacts',sum(c['contacts'] for c in cs),
          'bestLap',round(min(c['bestLap'] for c in fin),2) if fin else '-','win',round(min(c['time'] for c in fin),1) if fin else '-','len',round(d['trackLength'],1))
