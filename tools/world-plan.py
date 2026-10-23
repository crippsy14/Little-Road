"""The planned world (docs/world.md, layout B "Glasswater"), from docs/data/world-plan.json (tools/world-plan-extract.py).

    python tools/world-plan.py                  # summary: places and races per class
    python tools/world-plan.py class B          # every place of a class: kind, distance, features, races, tracks
    python tools/world-plan.py races B          # every race a class runs anywhere (the catalogue), with planned stars
    python tools/world-plan.py place harbour    # one place in full (id or name)
    python tools/world-plan.py places           # every place, nearest Bayview first

Race counts follow the blueprint: a main town runs one race per class on each of its tracks, plus one more in the
town's own class; villages, the secret and the Grand Tours list their own. Planned stars come from
docs/data/race-stars.json.
"""
import json, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
DATA = json.loads((ROOT / 'docs/data/world-plan.json').read_text(encoding='utf-8'))
STARS_FILE = ROOT / 'docs/data/race-stars.json'
STARS = json.loads(STARS_FILE.read_text(encoding='utf-8')) if STARS_FILE.exists() else {}
CLASSES = ['C', 'B', 'A', 'S', 'E']
P = DATA['places']; T = DATA['tracks']


def track_races(place, track):
    post = track.get('post', [])
    return {c: (track.get('count') or {}).get(c, 1 + (c == place['cls'])) for c in track['classes'] if c not in post}


def catalogue():
    rows = []
    for pid, p in sorted(P.items(), key=lambda kv: kv[1]['km_from_bayview']):
        if pid in T:
            for t in T[pid]:
                rows.append(dict(place=p['name'], id=pid, track=t['name'], layout=t['layout'], km=t.get('km'), races=track_races(p, t), note=t.get('note', '')))
                if t.get('post'): rows.append(dict(place=p['name'], id=pid, track=t['name'] + ' (post-game)', layout='post-game', km=t.get('km'), races={c: 1 for c in t['post']}, note='After beating Duke Rolo.'))
        elif p.get('races'):
            rows.append(dict(place=p['name'], id=pid, track='Lake-bed streets' if p['kind'] == 'secret' else p['name'] + ' course', layout=p['kind'], km=None, races=p['races'], note=p.get('what', '')))
    for tour in DATA.get('tours', []):
        rows.append(dict(place='Around the map', id=None, track=tour['name'], layout='tour', km=None, races={tour['cls']: len(tour['stops']) - 1}, note=f"{len(tour['stops']) - 1} stages, {tour['dir']}"))
    return rows


def summary():
    rows = catalogue(); total = {c: sum(r['races'].get(c, 0) for r in rows if r['layout'] != 'post-game') for c in CLASSES}
    print('Planned world: layout B, Glasswater (docs/world.md). Blueprint: ' + DATA['source'].split(' ')[0])
    for c in CLASSES:
        home = [p['name'] for p in sorted(P.values(), key=lambda p: p['km_from_bayview']) if p['cls'] == c]
        print(f"\nClass {c}: {total[c]} races before the credits; home places ({len(home)}): {', '.join(home)}")
    post = sum(r['races'].get('E', 0) for r in rows if r['layout'] == 'post-game')
    print(f"\nPost-game: +{post} E races. All: {sum(total.values())} + {post} = {sum(total.values()) + post}")


def show_place(p):
    feats = ', '.join(p.get('features', [])) or '-'
    races = ', '.join(f'{c}:{n}' for c, n in (p.get('races') or {}).items())
    print(f"{p['name']} ({p['id']}) · class {p['cls']} {p['kind']} · {p['km_from_bayview']} km from Bayview · at ({p['x']}, {p['y']}) km")
    if p.get('disc'): print(f"  discipline: {p['disc']}")
    print(f"  features: {feats}   races: {races or '-'}")
    for t in T.get(p['id'], []): print(f"  track: {t['name']} ({t['layout']}, {t.get('km', '?')} km, classes {'/'.join(t['classes'])}) - {t.get('note', '')}")
    if p.get('what'): print(f"  {p['what']}")


def races(cls):
    rows = [r for r in catalogue() if r['races'].get(cls) and r['layout'] != 'post-game']
    n = 0
    print(f"Class {cls} races, nearest Bayview first (stars from docs/data/race-stars.json where planned):")
    for r in rows:
        k = r['races'][cls]; n += k
        stars = [e['stars'] for e in STARS.get(cls, []) if e['place_id'] == r['id'] and e.get('track') == r['track']]
        dist = P[r['id']]['km_from_bayview'] if r['id'] else '-'
        print(f"  {k} x {r['track']} · {r['place']} · {r['layout']} · {dist} km" + (f" · stars {stars}" if stars else ''))
    extra = [e for e in STARS.get(cls, []) if e.get('extra')]
    for e in extra: print(f"  + {e['name']} · {P[e['place_id']]['name']} · {e['stars']}★ (beyond the blueprint: {e['extra']})")
    print(f"{n} races in the blueprint" + (f"; {len(extra)} more planned on top" if extra else '') + (f"; {sum(e['stars'] for e in STARS.get(cls, []))}★ planned in all" if STARS.get(cls) else ''))


def main(args):
    if not args: summary()
    elif args[0] == 'class': [show_place(p) or print() for p in sorted(P.values(), key=lambda p: p['km_from_bayview']) if p['cls'] == args[1].upper()]
    elif args[0] == 'races': races(args[1].upper())
    elif args[0] == 'places': [print(f"{p['km_from_bayview']:5.1f} km  {p['cls']}  {p['kind']:8} {p['name']} ({p['id']})") for p in sorted(P.values(), key=lambda p: p['km_from_bayview'])]
    elif args[0] == 'place':
        q = ' '.join(args[1:]).lower(); hits = [p for p in P.values() if q in (p['id'], p['name'].lower()) or q in p['name'].lower()]
        [show_place(p) for p in hits] if hits else print('No place matches ' + q)
    else: print(__doc__)


if __name__ == '__main__':
    sys.stdout.reconfigure(encoding='utf-8'); main(sys.argv[1:])
