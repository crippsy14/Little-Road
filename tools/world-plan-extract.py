"""Extract the planned world (layout B, "Glasswater") from the world blueprint artifact's page into docs/data/world-plan.json.

The blueprint (https://claude.ai/artifact/L9zDqLxe29UbkXcLHmfPrb, docs/58) holds its data as JavaScript object literals.
Save the page first (Artifact read with path index.html), then:
    python tools/world-plan-extract.py <saved index.html>
The JSON is the repo's copy of the plan: tools/world-plan.py and the world-plan skill read it.
"""
import json, math, re, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
BAY = (6.0, 18.98)


def block(text, start):
    """The balanced {...} or [...] starting at text[start]."""
    open_, close = text[start], {'{': '}', '[': ']'}[text[start]]
    depth, i, quote = 0, start, None
    while i < len(text):
        c = text[i]
        if quote:
            if c == '\\': i += 1
            elif c == quote: quote = None
        elif c in '"\'`': quote = c
        elif c == open_: depth += 1
        elif c == close:
            depth -= 1
            if depth == 0: return text[start:i + 1]
        i += 1
    raise ValueError('unbalanced')


def to_json(js):
    """A JS object literal (keys unquoted, comments, BAY.x maths, trailing commas) as JSON. Strings are left alone."""
    out, i = [], 0
    for m in re.finditer(r'"(?:\\.|[^"\\])*"', js):
        out.append(('code', js[i:m.start()])); out.append(('str', m.group(0))); i = m.end()
    out.append(('code', js[i:]))
    res = []
    for kind, part in out:
        if kind == 'str': res.append(part); continue
        part = re.sub(r'//[^\n]*', '', part)
        part = part.replace('BAY.x', str(BAY[0])).replace('BAY.y', str(BAY[1]))
        part = re.sub(r'(?<![\w.])(-?\d*\.?\d+)\s*([+-])\s*(\d*\.?\d+)(?![\w.])', lambda m: repr(round(float(m.group(1)) + (1 if m.group(2) == '+' else -1) * float(m.group(3)), 4)), part)
        part = re.sub(r'([{,]\s*)([A-Za-z_]\w*)\s*:', r'\1"\2":', part)
        part = re.sub(r'(?<![\w"])\.(\d)', r'0.\1', part)
        part = re.sub(r',(\s*[}\]])', r'\1', part)
        res.append(part)
    return json.loads(''.join(res))


def main(page):
    html = Path(page).read_text(encoding='utf-8')
    b = html.index('\nB:{')
    places = to_json(block(html, html.index('places:{', b) + len('places:')))
    tours = to_json(block(html, html.index('tours:[', b) + len('tours:')))
    tracks = to_json(block(html, html.index('const TRACKS=') + len('const TRACKS=')))
    for pid, p in places.items():
        p['id'] = pid
        p['km_from_bayview'] = round(math.hypot(p['x'] - BAY[0], p['y'] - (BAY[1] - .15)), 1)
        p['kind'] = 'hub' if p.get('hub') else 'island' if p.get('island') else 'secret' if p.get('secret') else 'village' if p.get('village') else 'main'
    data = {'source': 'https://claude.ai/artifact/L9zDqLxe29UbkXcLHmfPrb (layout B, Glasswater; docs/58)',
            'note': 'Kilometres on the 20 x 20 km sheet, x east and y north; Bayview island near (6.0, 18.98). Main towns race their tracks (club layout for C and B, pro for A, S and E): one race per class on each layout, plus one more in the town\'s own class. Villages, the secret and the tours list their own races.',
            'places': places, 'tracks': tracks, 'tours': tours}
    out = ROOT / 'docs/data/world-plan.json'; out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(json.dumps(data, indent=1, ensure_ascii=False) + '\n', encoding='utf-8')
    print(f'{out}: {len(places)} places, {sum(len(v) for v in tracks.values())} tracks, {len(tours)} tours')


if __name__ == '__main__':
    main(sys.argv[1])
