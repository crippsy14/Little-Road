# Fetch individual files from Sonniss' official GDC Game Audio Bundle zips without downloading whole parts
# (each part is ~3.5 GB). Reads each zip's central directory with HTTP range requests, then downloads and
# inflates only the entries asked for. Licence: royalty-free for games, no attribution, no AI/ML training,
# no redistribution of the raw files (keep them outside the repo). docs/47 stage 4.
# Usage:
#   python tools/fetch-sonniss.py list  <pattern>          # search every part's index for names containing pattern
#   python tools/fetch-sonniss.py fetch <out dir> <name>...  # download exact entry names (as printed by list)
import sys, os, struct, zlib, subprocess, json

# Other years: SONNISS_YEAR=2024 SONNISS_PARTS=9 (each year has its own index cache).
YEAR = os.environ.get('SONNISS_YEAR', '2023'); PARTS = int(os.environ.get('SONNISS_PARTS', '14'))
BASE = 'https://downloads.sonniss.com/Sonniss.com-GDC' + YEAR + '-GameAudioBundle{}of' + str(PARTS) + '.zip'
HEADERS = {'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0 Safari/537.36',
           'Referer': 'https://sonniss.com/gameaudiogdc/'}
CACHE = os.path.join(os.path.dirname(__file__), '..', 'review', 'tmp', 'sonniss-index.json' if YEAR == '2023' else f'sonniss-index-{YEAR}.json')

# curl, not urllib: the download host (Cloudflare) refuses Python's client.
def curl(url, *extra):
    args = ['curl', '-sS', '--fail', '--retry', '3', '-A', HEADERS['User-Agent'], '-e', HEADERS['Referer'], *extra, url]
    return subprocess.run(args, capture_output=True, check=True).stdout

def get(url, start, end):
    # The host occasionally answers a range request with the wrong length: check, and retry.
    for attempt in range(5):
        data = curl(url, '-r', f'{start}-{end}')
        if len(data) == end - start + 1: return data
    raise SystemExit(f'range {start}-{end} of {url} keeps coming back {len(data)} bytes')

def size(url):
    for line in curl(url, '-I').decode('latin-1').splitlines():
        if line.lower().startswith('content-length:'): return int(line.split(':')[1])
    raise SystemExit('no size for ' + url)

def index(part):
    url = BASE.format(part); total = size(url)
    tail = get(url, max(0, total - 131072), total - 1); base = total - len(tail)
    e = tail.rfind(b'PK\x05\x06')
    if e < 0: raise SystemExit(f'part {part}: no end of central directory')
    count, cd_size, cd_off = struct.unpack('<HII', tail[e + 10:e + 20])
    z = e - 20   # the ZIP64 locator, when present, sits right before the end record
    if z >= 0 and tail[z:z + 4] == b'PK\x06\x07':
        eocd64 = struct.unpack('<Q', tail[z + 8:z + 16])[0]
        rec = get(url, eocd64, eocd64 + 55)
        count, cd_size, cd_off = struct.unpack('<QQQ', rec[32:56])
    cd = get(url, cd_off, cd_off + cd_size - 1); entries = []; p = 0
    while p + 46 <= len(cd) and cd[p:p + 4] == b'PK\x01\x02':
        method, = struct.unpack('<H', cd[p + 10:p + 12])
        csize, usize = struct.unpack('<II', cd[p + 20:p + 28])
        nlen, xlen, clen = struct.unpack('<HHH', cd[p + 28:p + 34])
        off, = struct.unpack('<I', cd[p + 42:p + 46])
        name = cd[p + 46:p + 46 + nlen].decode('utf-8', 'replace'); extra = cd[p + 46 + nlen:p + 46 + nlen + xlen]
        q = 0
        while q + 4 <= len(extra):   # ZIP64 extended sizes and offset
            hid, hlen = struct.unpack('<HH', extra[q:q + 4]); body = extra[q + 4:q + 4 + hlen]
            if hid == 1:
                vals = list(struct.unpack('<' + 'Q' * (hlen // 8), body[:hlen // 8 * 8])); k = 0
                if usize == 0xFFFFFFFF: usize = vals[k]; k += 1
                if csize == 0xFFFFFFFF: csize = vals[k]; k += 1
                if off == 0xFFFFFFFF: off = vals[k]; k += 1
            q += 4 + hlen
        entries.append({'part': part, 'name': name, 'method': method, 'csize': csize, 'usize': usize, 'offset': off})
        p += 46 + nlen + xlen + clen
    return entries

def all_entries():
    if os.path.exists(CACHE): return json.load(open(CACHE, encoding='utf-8'))
    entries = []
    for part in range(1, PARTS + 1):
        e = index(part); entries += e; print(f'part {part}: {len(e)} entries', file=sys.stderr)
    os.makedirs(os.path.dirname(CACHE), exist_ok=True); json.dump(entries, open(CACHE, 'w', encoding='utf-8'))
    return entries

def fetch(entry, out_dir):
    url = BASE.format(entry['part']); head = get(url, entry['offset'], entry['offset'] + 29)
    nlen, xlen = struct.unpack('<HH', head[26:30]); start = entry['offset'] + 30 + nlen + xlen
    path = os.path.join(out_dir, *entry['name'].split('/'))
    # Pack and file names run long: past Windows' 260-character limit without the long-path prefix.
    if os.name == 'nt': path = '\\\\?\\' + os.path.abspath(path)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    if os.path.exists(path) and os.path.getsize(path) == entry['usize']: print('have', path); return
    inflater = zlib.decompressobj(-15) if entry['method'] == 8 else None; crc = 0
    written = 0; chunk = 16 << 20; pos = start; end = start + entry['csize']
    with open(path + '.part', 'wb') as f:
        while pos < end:
            data = get(url, pos, min(end, pos + chunk) - 1); pos += len(data)
            out = inflater.decompress(data) if inflater else data; f.write(out); written += len(out)
        if inflater: tail = inflater.flush(); f.write(tail); written += len(tail)
    if written != entry['usize']: raise SystemExit(f'size mismatch for {entry["name"]}: {written} != {entry["usize"]}')
    os.replace(path + '.part', path); print(f'fetched {path} ({written / 1e6:.1f} MB)')

if __name__ == '__main__':
    cmd = sys.argv[1]
    entries = all_entries()
    if cmd == 'list':
        pat = sys.argv[2].lower()
        for e in entries:
            if pat in e['name'].lower(): print(f"{e['usize'] / 1e6:8.1f} MB  part {e['part']:2}  {e['name']}")
    elif cmd == 'fetch':
        out = sys.argv[2]; names = sys.argv[3:]; byname = {e['name']: e for e in entries}
        for n in names:
            if n not in byname: raise SystemExit('not in the bundle: ' + n)
            fetch(byname[n], out)
