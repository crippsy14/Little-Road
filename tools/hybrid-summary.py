# The hybrid prototype's keep/reject races (docs/84): per track and seed, the field flag off ('off') against flag on
# (any other arm): finishers, assisted resets, contacts, total finish time of all cars, the winner's time, and for hybrid
# cars the battery: deploy seconds, seconds running flat, kJ deployed and harvested, charge at the flag (field means).
# Usage: python tools/hybrid-summary.py <study> [base arm, default off]
import json, glob, os, sys, statistics as st
study = sys.argv[1]; base = sys.argv[2] if len(sys.argv) > 2 else 'off'
rows = {}
for f in sorted(glob.glob(f'review/unity-35/frame-cost-{study}/*/race.json')):
    name = os.path.basename(os.path.dirname(f)); track, seed, arm = name.split('-', 2)
    d = json.load(open(f, encoding='utf-8')); cars = d['cars']; fin = [c for c in cars if c['finished']]
    total = sum(c['time'] for c in cars) if len(fin) == len(cars) else None
    hyb = [c for c in cars if c.get('deploySeconds', 0) > 0 or c.get('emptySeconds', 0) > 0]
    m = lambda k: st.mean(c.get(k, 0) for c in hyb) if hyb else 0
    rows[(track, seed, arm)] = dict(fin=len(fin), n=len(cars), resets=len(d['assistedRecoveries']), contacts=sum(c['contacts'] for c in cars),
        total=total, win=min(c['time'] for c in fin) if fin else 0, deploy=m('deploySeconds'), empty=m('emptySeconds'),
        out=m('deployKj'), back=m('regenKj'), charge=m('finalCharge'), hybrids=len(hyb))
print('track       seed  arm          fin    resets contacts  total s   winner   | deploy s  flat s  out kJ  back kJ  end charge')
gains = {}
for (track, seed, arm), r in sorted(rows.items()):
    print(f"{track:<11} {seed:<5} {arm:<12} {r['fin']:>2}/{r['n']:<2}  {r['resets']:>5}  {r['contacts']:>6}   {r['total'] or 0:8.1f}  {r['win']:7.1f}  | "
          + (f"{r['deploy']:7.1f}  {r['empty']:6.1f}  {r['out']:6.0f}  {r['back']:7.0f}  {r['charge']:8.2f}" if r['hybrids'] else '      -'))
    if arm != base and (track, seed, base) in rows and r['total'] and rows[(track, seed, base)]['total']:
        gains.setdefault((track, arm), []).append(rows[(track, seed, base)]['total'] - r['total'])
print()
for (track, arm), g in sorted(gains.items()):
    print(f'{track:<11} {arm} vs {base}, total finish time: {st.mean(g):+.1f} s a race faster on average over {len(g)} seeds (each: {", ".join(f"{x:+.1f}" for x in g)})')
