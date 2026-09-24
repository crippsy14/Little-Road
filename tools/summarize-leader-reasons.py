import json,sys,collections,os
for folder in sys.argv[1:]:
    t=json.load(open(os.path.join(folder,'race.track.json'),encoding='utf-8-sig'))
    main={(x['id'],round(x['time'],3)):x for x in t['samples']}
    cs=[c for c in t['contextSamples'] if (c['id'],round(c['time'],3)) in main and main[(c['id'],round(c['time'],3))]['progress']>0 and main[(c['id'],round(c['time'],3))]['recoveryState']=='Racing']
    n=len(cs);R=collections.Counter(c['reason'] for c in cs);I=collections.Counter(c['intent'] for c in cs)
    pedal=[main[(c['id'],round(c['time'],3))]['pedal'] for c in cs]
    brake=sum(p<-.05 for p in pedal)/n
    lim=[c for c in cs if c['reason'].startswith('Moving leader')]
    lb=sum(main[(c['id'],round(c['time'],3))]['pedal']<-.05 for c in lim)
    print(f"== {os.path.basename(folder)} n={n} brake {brake:.1%} unresolved {sum(c['roadUnresolved'] for c in cs)/n:.2%} safety {sum(c['safetyActive'] for c in cs)/n:.1%} leader-limited {len(lim)/n:.1%} (braking within: {lb/max(1,len(lim)):.0%})")
    for r,c in R.most_common(7):print(f"   R {c/n:6.1%}  {r}")
    for r,c in I.most_common(5):print(f"   I {c/n:6.1%}  {r}")
