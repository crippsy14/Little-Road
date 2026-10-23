"""The race-field reference (docs/design.md §8): the parts and PP of the class C and B field at every planned race.

    python tools/field-table.py            # both tables as Markdown (per race, then the part windows per star level)
    python tools/field-table.py races      # only the per-race table
    python tools/field-table.py windows    # only the part windows

Reads docs/data/field-recipes.json (written by FieldRecipeChecks on every build, from FieldRecipe and parts.json),
docs/data/race-stars.json (planned stars and surfaces) and docs/data/world-plan.json (places and distances).
Run with the Blender Python on Richard's PC.
"""
import json, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
FIELD = json.loads((ROOT / 'docs/data/field-recipes.json').read_text(encoding='utf-8'))
STARS = json.loads((ROOT / 'docs/data/race-stars.json').read_text(encoding='utf-8'))
PLACES = json.loads((ROOT / 'docs/data/world-plan.json').read_text(encoding='utf-8'))['places']
ROWS = {(r['class'], r['disc'], r['stars']): r for r in FIELD['rows']}
SLOTS = ['tyres', 'brakes', 'engine', 'turbo', 'transmission', 'steering', 'chassis']
SURFACE = {'road': 'Road', 'gravel': 'Gravel', 'ice': 'Ice*'}


def races():
    print('| Class | Stars | Race | Place | km from Bayview | Surface | Rival PP: min · mean · max | Floor kit PP → ceiling kit PP | Built as |')
    print('|---|---|---|---|---|---|---|---|---|')
    rows = []
    for cls in ('C', 'B'):
        for e in STARS[cls]:
            rows.append((cls, e['stars'], PLACES[e['place_id']]['km_from_bayview'], e))
    for cls, stars, km, e in sorted(rows, key=lambda r: (r[0] != 'C', r[1], r[2])):
        r = ROWS[(cls, e['disc'], stars)]
        built = e.get('event', '') + (' (reversed)' if e.get('extra') == 'reversed' else '')
        print(f"| {cls} | {stars}★ | {e['name']} | {PLACES[e['place_id']]['name']} | {km} | {SURFACE[e['disc']]} | "
              f"{r['ppMin']} · {r['ppMean']:.0f} · {r['ppMax']} | {r['ppFloor']} → {r['ppCeiling']} | {built or '-'} |")
    print(f"| B | 10★ | Tuner Championship (three rounds) | three towns with B races, to be chosen | - | Road | "
          f"{ROWS[('B', 'road', 10)]['ppMin']} · {ROWS[('B', 'road', 10)]['ppMean']:.0f} · {ROWS[('B', 'road', 10)]['ppMax']} | "
          f"{ROWS[('B', 'road', 10)]['ppFloor']} → {ROWS[('B', 'road', 10)]['ppCeiling']} | - |")


def windows():
    for cls in ('C', 'B'):
        for disc in ('road', 'gravel'):
            levels = [s for s in (range(1, 9) if cls == 'C' else list(range(1, 9)) + [10]) if (cls, disc, s) in ROWS]
            print(f"\n**Class {cls}, {disc}** (each cell: the floor part → the ceiling part; a single name means the whole field runs it)\n")
            print('| Stars | Mean PP | ' + ' | '.join(s.capitalize() for s in SLOTS) + ' |')
            print('|---|---|' + '---|' * len(SLOTS))
            for s in levels:
                r = ROWS[(cls, disc, s)]
                cells = [w[0] if w[0] == w[1] else f'{w[0]} → {w[1]}' for w in (r['window'][slot] for slot in SLOTS)]
                print(f"| {s}★ | {r['ppMean']:.0f} | " + ' | '.join(cells) + ' |')


if __name__ == '__main__':
    sys.stdout.reconfigure(encoding='utf-8')
    arg = sys.argv[1] if len(sys.argv) > 1 else ''
    if arg in ('', 'races'): races()
    if arg in ('', 'windows'): windows()
    print(f"\nYour car for comparison: the stock starter is PP {FIELD['playerStockPp']}; the free Tuner on its seven Good parts is PP {FIELD['playerFreeTunerPp']}.")
