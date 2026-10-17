"""Fetch reference photos for Car Forge cars from Wikimedia Commons (freely licensed), with a sources list.

Usage (Blender's Python, from the project root):
  python tools/forge-refs.py <car> "<search>" ["<search>" ...] [--n 8]
Saves review/forge/refs/<car>/<nn>-<name>.jpg (1280 px wide) and appends title, licence, author and page to sources.txt.
"""
import json, os, re, sys, urllib.parse, urllib.request

UA = {'User-Agent': 'LittleRoadForge/1.0 (car reference research; personal game project)'}


def get(url):
    with urllib.request.urlopen(urllib.request.Request(url, headers=UA), timeout=60) as r:
        return r.read()


def search(q, n):
    api = 'https://commons.wikimedia.org/w/api.php?' + urllib.parse.urlencode(dict(
        action='query', generator='search', gsrnamespace=6, gsrlimit=n * 3, gsrsearch='intitle:"%s" filetype:bitmap' % q,
        prop='imageinfo', iiprop='url|size|extmetadata', iiurlwidth=1280, format='json'))
    pages = json.loads(get(api)).get('query', {}).get('pages', {}).values()
    out = []
    for p in sorted(pages, key=lambda p: p.get('index', 0)):
        ii = p['imageinfo'][0]
        if ii['width'] < 900 or ii['width'] < ii['height']: continue   # (landscape photos only: car views)
        meta = ii.get('extmetadata', {})
        lic = meta.get('LicenseShortName', {}).get('value', '?'); art = re.sub('<[^>]+>', '', meta.get('Artist', {}).get('value', '?'))
        out.append((p['title'], ii['thumburl'], lic, art.strip(), ii['descriptionurl']))
    return out[:n]


def main():
    a = sys.argv[1:]; n = 8
    if '--n' in a: i = a.index('--n'); n = int(a[i + 1]); del a[i:i + 2]
    car, queries = a[0], a[1:]
    folder = os.path.join('review', 'forge', 'refs', car); os.makedirs(folder, exist_ok=True)
    k = len([f for f in os.listdir(folder) if f.endswith('.jpg')])
    with open(os.path.join(folder, 'sources.txt'), 'a', encoding='utf-8') as src:
        for q in queries:
            for title, url, lic, art, page in search(q, n):
                k += 1; name = '%02d-%s.jpg' % (k, re.sub(r'[^A-Za-z0-9]+', '_', title[5:].rsplit('.', 1)[0])[:60])
                open(os.path.join(folder, name), 'wb').write(get(url))
                src.write('%s | %s | %s | %s | %s\n' % (name, title, lic, art, page)); print(name, '|', lic)


main()
