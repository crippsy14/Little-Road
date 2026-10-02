# Engine sound scorecard (docs/55): the game's engine voice against the real recording it was built from, on the
# recording's own drive. Usage (from the project root, Blender's Python has numpy):
#   python tools/engine-compare.py skyline [--engine engine_valley] [--file 0] [--start s] [--end s] [--name x]
# 1. Tracks the recording's revs (the same tracker as tools/engine-grains.py) and reads the driver's inputs from them:
#    revs rising = on the throttle, falling slowly = lifted (overrun), falling fast = a gearchange.
# 2. Has the sound harness drive the game's voice along that trace twice: "raw" (the grains alone, untouched: is the
#    playback faithful?) and "game" (the full in-game voice: exhaust shaping and modelled layers).
# 3. Measures all three the same way, per driving phase (pulling, lifted, gearchanges, the moments of lifting off),
#    and scores each measure 0-10 against a tolerance. Writes review/engine-compare/<name>/index.html: scores,
#    spectrograms with the tracked revs, and a listening panel (A/B and a blind "which one is real?" test).
import argparse, json, math, os, subprocess, sys, wave, struct
import numpy as np
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
eg = __import__('engine-grains')
FS = eg.FS; HOP = 0.01
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))

# ---------------------------------------------------------------- the driver's inputs, read from the revs
def read_inputs(times, rpm, conf, x):
    # A 10 ms grid; rpm from the tracker (untrusted frames bridged), its slope over +-60 ms, and the level.
    T = int(len(x) / FS / HOP); t = np.arange(T) * HOP
    good = conf >= 0.5
    r = np.interp(t, times[good], rpm[good]) if good.sum() > 10 else np.interp(t, times, rpm)
    c = np.interp(t, times, conf)
    k = 6; slope = np.zeros(T); slope[k:-k] = (r[2*k:] - r[:-2*k]) / (2 * k * HOP)
    state = np.zeros(T, int)   # 1 pulling, 0 lifted, 2 gearchange
    cur = 1 if slope[0] >= 0 else 0
    for i in range(T):
        s = slope[i]
        if s < -2200 and r[i] > 1500: state[i] = 2; continue          # revs collapsing: the clutch is in
        if cur == 1 and s < -120: cur = 0                               # lifted: revs falling under engine braking
        elif cur == 0 and s > 120: cur = 1                              # back on the throttle
        state[i] = cur
    # A gearchange is short: anything over 0.8 s of fast-falling revs is a hard lift, not a shift.
    i = 0
    while i < T:
        if state[i] == 2:
            j = i
            while j < T and state[j] == 2: j += 1
            if (j - i) * HOP > 0.8: state[i:j] = 0
            i = j
        else: i += 1
    shifting = (state == 2).astype(int); throttle = (state == 1).astype(float)
    up = np.cumsum(np.diff(np.concatenate([[0], shifting])) == 1)
    return t, r, c, throttle, shifting, up, state

def events(state):
    # Lift-offs (pulling >= 0.3 s, then lifted >= 0.3 s) and gearchanges (their start), as frame indices.
    lifts, shifts = [], []
    for i in range(30, len(state) - 30):
        if state[i - 1] == 1 and state[i] == 0 and (state[i-30:i] == 1).all() and (state[i:i+100] == 0).all(): lifts.append(i)
        if state[i - 1] != 2 and state[i] == 2: shifts.append(i)
    return lifts, shifts

# ---------------------------------------------------------------- measuring a signal along the trace
BANDS = 50 * 2 ** (np.arange(0, 24) / 3)   # third octaves, 50 Hz - 8 kHz
def analyse(x, t, r, cylinders):
    n = 4096; win = np.hanning(n); fr = np.fft.rfftfreq(n, 1 / FS)
    edges = np.concatenate([[BANDS[0] / 2 ** (1/6)], BANDS * 2 ** (1/6)]); bidx = np.searchsorted(fr, edges)
    T = len(t); level = np.full(T, -120.0); bands = np.zeros((T, len(BANDS))); orders = np.zeros((T, 2 * cylinders)); noise = np.zeros(T)
    for i in range(T):
        a = int(t[i] * FS) - n // 2
        seg = x[max(0, a):a + n]
        if len(seg) < n: seg = np.pad(seg, (0, n - len(seg)))
        P = np.abs(np.fft.rfft(seg * win)) ** 2 + 1e-18
        short = x[max(0, int(t[i] * FS) - 480):int(t[i] * FS) + 480]
        level[i] = 10 * math.log10(np.mean(short ** 2) + 1e-12) if len(short) else -120
        e = np.array([P[bidx[k]:max(bidx[k] + 1, bidx[k + 1])].sum() for k in range(len(BANDS))]); bands[i] = 10 * np.log10(e / e.sum())
        # Engine orders: the cycle rate (rpm/120) and its multiples up to twice the firing rate.
        fc = r[i] / 120
        tonal = 0; between = 0
        for k in range(1, 2 * cylinders + 1):
            f = k * fc; lo = np.searchsorted(fr, f * 0.97); hi = max(lo + 1, np.searchsorted(fr, f * 1.03))
            orders[i, k - 1] = 10 * math.log10(P[lo:hi].max())
        for k in range(1, 2 * cylinders):
            f = k * fc; lo = np.searchsorted(fr, f * 1.25); hi = max(lo + 1, np.searchsorted(fr, f * 1.75))
            between += P[lo:hi].mean(); tonal += 10 ** (orders[i, k - 1] / 10)
        noise[i] = 10 * math.log10(between / (tonal + 1e-18) + 1e-18)
        orders[i] -= orders[i].max()
    return dict(level=level, bands=bands, orders=orders, noise=noise)

def score(err, tol): return float(max(0.0, 10 * (1 - err / tol)))

def compare(ref, out, t, r, c, state, lifts, shifts, ref_x, out_x, cylinders, idle):
    # Frames worth judging: trusted revs and an engine actually heard (within 30 dB of the drive's loud parts).
    loud = np.percentile(ref['level'], 90)
    use = (c >= 0.5) & (ref['level'] > loud - 30)
    rows = []
    def add(key, label, err, tol, unit, note='', **extra): rows.append(dict(key=key, label=label, err=err, tol=tol, unit=unit, score=score(err, tol), note=note, **extra))
    # Level differences are judged after matching the two drives' overall loudness on the pulling frames.
    pull = use & (state == 1); lift = use & (state == 0); gear = use & (state == 2)
    off = np.median(ref['level'][pull]) - np.median(out['level'][pull]) if pull.sum() > 20 else 0
    ol = out['level'] + off
    # 1. Revs: the voice's tracked note against the recording's.
    tt, rr, cc, _ = eg.track_rpm(out_x, cylinders, idle, max(r) * 1.2)
    ro = np.interp(t, tt, rr); ok = use & (np.interp(t, tt, cc) > 0.5)
    cents = np.abs(1200 * np.log2(np.maximum(ro[ok], 1) / np.maximum(r[ok], 1))) if ok.sum() else np.array([999])
    add('pitch', 'Pitch follows the revs', float(np.median(cents)), 50, 'cents (median)', f'95th percentile {np.percentile(cents, 95):.0f} cents')
    # 2-3. Tone colour (third-octave balance) and engine character (order profile), pulling and lifted.
    for name, m in (('pulling', pull), ('lifted', lift)):
        if m.sum() < 20: continue
        d = np.mean(ref['bands'][m], 0) - np.mean(out['bands'][m], 0); w = np.mean(10 ** (ref['bands'][m] / 10), 0); w = w / w.sum()
        add('tone-' + name, f'Tone colour, {name}', float(math.sqrt(np.sum(w * d ** 2) / np.sum(w))), 6, 'dB (rms, weighted)',
            'largest gaps: ' + ', '.join(f'{BANDS[k]:.0f} Hz {d[k]:+.0f}' for k in np.argsort(-np.abs(d) * (w > 0.01))[:3]) + ' (real minus game)')
        d = np.mean(ref['orders'][m], 0) - np.mean(out['orders'][m], 0)
        add('orders-' + name, f'Engine character (orders), {name}', float(np.sqrt(np.mean(d ** 2))), 8, 'dB (rms)')
        add('noise-' + name, f'Clean note (noise between harmonics), {name}', float(abs(np.median(ref['noise'][m]) - np.median(out['noise'][m]))), 8, 'dB',
            f'real {np.median(ref["noise"][m]):.0f} dB, game {np.median(out["noise"][m]):.0f} dB')
        rs = ref['level'][m] - np.convolve(ref['level'][m], np.ones(15) / 15, 'same'); os_ = ol[m] - np.convolve(ol[m], np.ones(15) / 15, 'same')
        add('rough-' + name, f'Cycle-to-cycle life (level wobble), {name}', float(abs(np.std(rs[7:-7]) - np.std(os_[7:-7]))), 2, 'dB',
            f'real {np.std(rs[7:-7]):.1f} dB, game {np.std(os_[7:-7]):.1f} dB')
    # 3b. Rev-rise: how much louder pulling gets as the revs climb (dB per doubling of revs), the sound of a gear.
    def rise(level):
        m = pull & (r > 0)
        if m.sum() < 30: return None
        bins_ = {}
        for lv, rv in zip(level[m], r[m]): bins_.setdefault(int(rv // 250), []).append(lv)
        pts = [(math.log2((k + .5) * 250), np.median(v)) for k, v in bins_.items() if len(v) >= 5]
        return float(np.polyfit([p[0] for p in pts], [p[1] for p in pts], 1)[0]) if len(pts) >= 3 else None
    rr_, ro_ = rise(ref['level']), rise(ol)
    if rr_ is not None and ro_ is not None: add('rise', 'Louder as the revs climb (pulling)', abs(rr_ - ro_), 6, 'dB per doubling', f'real {rr_:+.1f}, game {ro_:+.1f} dB per doubling of revs')
    # 4. Lift-off contrast: how much quieter lifted is than pulling at the same revs.
    bins = np.arange(1000, 12000, 500); dr, do = [], []
    for lo in bins:
        a = pull & (r >= lo) & (r < lo + 500); b = lift & (r >= lo) & (r < lo + 500)
        if a.sum() > 5 and b.sum() > 5:
            dr.append(np.median(ref['level'][a]) - np.median(ref['level'][b])); do.append(np.median(ol[a]) - np.median(ol[b]))
    if dr: add('contrast', 'Lift-off drop (pulling vs lifted, same revs)', float(abs(np.mean(dr) - np.mean(do))), 6, 'dB', f'real {np.mean(dr):.1f} dB quieter, game {np.mean(do):.1f} dB')
    # 5. The moment of lifting off, and gearchanges: level curves around each event.
    for key, label, ev, span in (('liftoff', 'Lifting off (level over 1.2 s)', lifts, (-20, 100)), ('shift', 'Gearchanges (level over 0.6 s)', shifts, (-10, 50))):
        curves_r, curves_o = [], []
        for i in ev:
            a, b = i + span[0], i + span[1]
            if a < 5 or b >= len(t): continue
            base_r = np.median(ref['level'][a - 5:i]); base_o = np.median(ol[a - 5:i])
            curves_r.append(ref['level'][a:b] - base_r); curves_o.append(ol[a:b] - base_o)
        if curves_r:
            cr = np.median(curves_r, 0); co = np.median(curves_o, 0)
            add(key, label, float(np.sqrt(np.mean((cr - co) ** 2))), 6, 'dB (rms over the curve)', f'{len(curves_r)} events; real dips to {cr.min():.0f} dB, game to {co.min():.0f} dB',
                real=[round(float(v), 2) for v in cr], game=[round(float(v), 2) for v in co])
    # 6. Level tracking through the drive (the envelope's correlation): does it swell and fall where the real one does?
    m = use; a = ref['level'][m]; b = ol[m]
    corr = float(np.corrcoef(a, b)[0, 1]) if m.sum() > 50 else 0
    add('envelope', 'Swells and falls in the right places', 1 - corr, 0.6, '1 - correlation', f'correlation {corr:.2f}')
    return rows

# ---------------------------------------------------------------- report
def write_wav(path, x):
    x = np.clip(x, -1, 1); pcm = (x * 32767).astype('<i2')
    with wave.open(path, 'wb') as w: w.setnchannels(1); w.setsampwidth(2); w.setframerate(FS); w.writeframes(pcm.tobytes())

def spectrogram(path, x, times, rpm, conf, cylinders, t, state):
    # The builder's spectrogram (40 Hz - 4 kHz, revs line), plus a strip of the inputs read from the revs (green
    # pulling, red lifted, yellow gearchange) and a tick every 5 s along the top.
    import zlib
    eg.report(path, x, times, rpm, conf, cylinders)
    W, H = 1400, 360; data = open(path, 'rb').read(); idat = b''; pos = 8
    while pos < len(data):
        n = struct.unpack('>I', data[pos:pos+4])[0]; typ = data[pos+4:pos+8]
        if typ == b'IDAT': idat += data[pos+8:pos+8+n]
        pos += 12 + n
    raw = zlib.decompress(idat); rows = [bytearray(raw[y*(W*3+1)+1:(y+1)*(W*3+1)]) for y in range(H)]
    seconds = len(x) / FS; colours = {1: (60, 200, 90), 0: (230, 70, 60), 2: (240, 210, 40), -1: (110, 110, 110)}
    strip = [bytearray(W * 3) for _ in range(10)]
    for i in range(W):
        k = state[min(len(state) - 1, int(i / W * seconds / HOP))]
        for y in range(10): strip[y][i*3:i*3+3] = bytes(colours[int(k)])
    for s5 in np.arange(0, seconds, 5):
        i = int(s5 / seconds * W)
        for y in range(8): rows[y][i*3:i*3+3] = b'\xff\xff\xff'
    allrows = rows + strip
    raw = b''.join(b'\x00' + bytes(r) for r in allrows)
    def chunk(t_, d): return struct.pack('>I', len(d)) + t_ + d + struct.pack('>I', zlib.crc32(t_ + d) & 0xffffffff)
    open(path, 'wb').write(b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', W, len(allrows), 8, 2, 0, 0, 0)) + chunk(b'IDAT', zlib.compress(raw, 6)) + chunk(b'IEND', b''))

def html(name, cfg, results, files, seconds):
    def table(rows):
        out = ['<table><tr><th>Measure</th><th>Raw grains</th><th>In game</th><th>Tolerance</th><th>Notes (in game)</th></tr>']
        keys = [r['key'] for r in rows['game']]
        for k in keys:
            g = next(r for r in rows['game'] if r['key'] == k); w = next((r for r in rows['raw'] if r['key'] == k), None)
            def cell(rr): return f'<td class="s{int(rr["score"] // 2.5)}"><b>{rr["score"]:.1f}</b><span>{rr["err"]:.1f} {rr["unit"]}</span></td>' if rr else '<td></td>'
            out.append(f'<tr><td>{g["label"]}</td>{cell(w)}{cell(g)}<td>{g["tol"]} {g["unit"].split(" ")[0]}</td><td class="note">{g["note"]}</td></tr>')
        tot = {k: np.mean([r['score'] for r in rows[k]]) for k in rows}
        out.append(f'<tr class="total"><td>Overall</td><td><b>{tot["raw"]:.1f}</b></td><td><b>{tot["game"]:.1f}</b></td><td></td><td></td></tr></table>')
        return '\n'.join(out), tot
    t, tot = table(results)
    return f'''<!doctype html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Engine scorecard: {name}</title><style>
:root{{--bg:#f6f4ef;--fg:#1d1d1b;--mut:#6b6a66;--card:#fff;--line:#e2ded5;--s0:#f3d6d2;--s1:#f6e3c6;--s2:#e9edc9;--s3:#cfe8d3}}
@media (prefers-color-scheme:dark){{:root{{--bg:#1b1b1a;--fg:#ecebe6;--mut:#a3a19a;--card:#252523;--line:#3a3935;--s0:#5a2f2a;--s1:#5a4626;--s2:#454a26;--s3:#274a30}}}}
body{{background:var(--bg);color:var(--fg);font:15px/1.45 system-ui,sans-serif;margin:0;padding:16px}} main{{max-width:1100px;margin:auto}}
h1{{font-size:22px;margin:4px 0}} .mut{{color:var(--mut)}} table{{border-collapse:collapse;width:100%;background:var(--card);margin:12px 0}}
td,th{{border-bottom:1px solid var(--line);padding:6px 8px;text-align:left;vertical-align:top}} td span{{display:block;font-size:12px;color:var(--mut)}}
.s0{{background:var(--s0)}} .s1{{background:var(--s1)}} .s2{{background:var(--s2)}} .s3,.s4{{background:var(--s3)}} .note{{font-size:13px;color:var(--mut)}}
.total td{{font-size:17px}} img{{width:100%;image-rendering:auto;border-radius:4px;display:block}} .card{{background:var(--card);padding:12px;border-radius:8px;margin:12px 0}}
button{{font:inherit;padding:6px 12px;margin:4px;border-radius:6px;border:1px solid var(--line);background:var(--bg);color:var(--fg);cursor:pointer}}
audio{{width:100%}} .scroll{{overflow-x:auto}}</style></head><body><main>
<h1>Engine scorecard: {name}</h1>
<p class="mut">The game's voice driven along the real recording's own drive ({seconds:.0f} s of {cfg}). Raw grains = the recording's cycles played back untouched;
in game = what the player hears (exhaust shaping and modelled layers). Each measure scores 10 at a perfect match and 0 at its tolerance.</p>
<div class="scroll">{t}</div>
<div class="card"><h2>Blind test: which one is real?</h2><p class="mut">Two clips of the same stretch of driving, in a random order.</p>
<audio id="bA" controls preload="none"></audio><audio id="bB" controls preload="none"></audio>
<button onclick="guess(0)">A is real</button><button onclick="guess(1)">B is real</button><button onclick="deal()">New pair</button> <span id="res"></span></div>
<div class="card"><h2>Listen side by side</h2>
<p>Real recording</p><audio controls preload="none" src="{files["ref"]}"></audio>
<p>Raw grains along the same drive</p><audio controls preload="none" src="{files["raw"]}"></audio>
<p>In game along the same drive</p><audio controls preload="none" src="{files["game"]}"></audio></div>
<div class="card"><h2>Spectrograms</h2><p class="mut">40 Hz to 4 kHz, low at the bottom; the white line is the tracked revs (red where unsure), ticks every 5 s. The strip underneath is the driving read from the revs: green on the throttle, red lifted, yellow a gearchange.</p>
<p>Real</p><img src="{files["ref_png"]}" alt="real recording spectrogram"><p>Raw grains</p><img src="{files["raw_png"]}" alt="raw grains spectrogram"><p>In game</p><img src="{files["game_png"]}" alt="in-game spectrogram"></div>
<script>
let real=0,score=[0,0];const clips=["{files["ref"]}","{files["game"]}"];
function deal(){{real=Math.random()<.5?0:1;document.getElementById(real?"bB":"bA").src=clips[0];document.getElementById(real?"bA":"bB").src=clips[1];document.getElementById("res").textContent=score[1]?`${{score[0]}} of ${{score[1]}} right`:"";}}
function guess(k){{score[1]++;if(k===real)score[0]++;document.getElementById("res").textContent=(k===real?"Right: ":"Wrong: ")+`${{score[0]}} of ${{score[1]}} right (guessing gets half)`;}}
deal();</script></main></body></html>'''

PREBUILT = False   # --prebuilt: run the harness as last compiled (parallel runs would all recompile the same file)
DOTNET = os.path.join('C:' + os.sep, 'Program Files', 'Unity', 'Hub', 'Editor', '6000.6.1f1', 'Editor', 'Data', 'DotNetSdk', 'dotnet.exe')
def run_harness(arg):
    if PREBUILT:
        out = subprocess.run([DOTNET, os.path.join(ROOT, 'review', 'tmp', 'sound-harness', 'SoundHarness.dll'), ROOT, arg], capture_output=True, text=True)
        if out.returncode != 0: raise SystemExit(out.stdout + out.stderr)
        return out.stdout.strip().splitlines()[-1]
    ps = os.path.join(ROOT, 'tools', 'sound-harness.ps1')
    out = subprocess.run(['powershell', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', ps, '-Filter', arg], capture_output=True, text=True)
    if out.returncode != 0: raise SystemExit(out.stdout + out.stderr)
    return out.stdout.strip().splitlines()[-1]

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('bank'); ap.add_argument('--engine', default=''); ap.add_argument('--file', type=int, default=0)
    ap.add_argument('--start', type=float, default=-1); ap.add_argument('--end', type=float, default=-1); ap.add_argument('--name', default='')
    ap.add_argument('--max-seconds', type=float, default=90)
    ap.add_argument('--selftest', action='store_true', help='score the recording against a copy of itself (6 dB down, 3 ms late): every measure should be near 10')
    ap.add_argument('--prebuilt', action='store_true')
    a = ap.parse_args(); global PREBUILT; PREBUILT = a.prebuilt
    cfgall = json.load(open(os.path.join(ROOT, 'tools', 'engine-banks.json'), encoding='utf-8')); b = cfgall['banks'][a.bank]
    src = b['files'][a.file]; path = os.path.join(ROOT, 'tools', cfgall['sources'], src['file'])
    parts = json.load(open(os.path.join(ROOT, 'unity', 'Assets', 'Resources', 'Data', 'parts.json'), encoding='utf-8'))['parts']
    engine = a.engine or next(p['id'] for p in parts if p['slot'] == 'engine' and p.get('sound') == a.bank)
    name = a.name or a.bank; out = os.path.join(ROOT, 'review', 'engine-compare', name); os.makedirs(out, exist_ok=True)
    x, rate = eg.read_wav(path); x = eg.resample(x, rate)
    start = a.start if a.start >= 0 else src.get('start', 0); end = a.end if a.end >= 0 else src.get('end', 0)
    if end > 0: x = x[:int(end * FS)]
    x = x[int(start * FS):]; x = x[:int(a.max_seconds * FS)]; x = x / (np.max(np.abs(x)) + 1e-9) * 0.9
    cyl = b.get('trackCylinders', 0) or b['cylinders']
    times, rpm, conf, _ = eg.track_rpm(x, cyl, b['idle'], b['redline'])
    t, r, c, thr, shifting, up, state = read_inputs(times, rpm, conf, x)
    lifts, shifts = events(state)
    trace = os.path.join(out, 'trace.csv')
    with open(trace, 'w') as f:
        f.write('t,rpm,throttle,shifting,upshifts\n')
        for i in range(len(t)): f.write(f'{t[i]:.2f},{r[i]:.1f},{thr[i]:.0f},{shifting[i]},{up[i]}\n')
    renders = {}
    if a.selftest:
        y = np.concatenate([np.zeros(int(0.003 * FS)), x[:-int(0.003 * FS)]]) * 0.5
        rows = compare(analyse(x, t, r, cyl), analyse(y, t, r, cyl), t, r, c, state, lifts, shifts, x, y, cyl, b['idle'])
        print(f'SELFTEST {a.bank} overall {np.mean([q["score"] for q in rows]):.1f}: ' + ', '.join(f'{q["key"]} {q["score"]:.1f}' for q in rows)); return
    for mode in ('raw', 'game'):
        wav = os.path.join(out, mode + '.wav')
        print(run_harness(f'follow:{engine}|{a.bank}|{trace}|{wav}' + ('|raw' if mode == 'raw' else '')))
        y, _ = eg.read_wav(wav); y = y[:len(x)]
        if len(y) < len(x): y = np.pad(y, (0, len(x) - len(y)))
        renders[mode] = y
    ref = analyse(x, t, r, cyl); results = {}
    for mode, y in renders.items(): results[mode] = compare(ref, analyse(y, t, r, cyl), t, r, c, state, lifts, shifts, x, y, cyl, b['idle'])
    # Listening files at matched loudness (pulling frames), and spectrograms.
    pull = (state == 1) & (c >= .5)
    def norm(y):
        lv = np.array([np.sqrt(np.mean(y[int(ti*FS):int(ti*FS)+480] ** 2)) for ti in t[pull]]) if pull.sum() else np.array([np.sqrt(np.mean(y ** 2))])
        return y * (0.12 / (np.median(lv) + 1e-9))
    files = {}
    for k, y in (('ref', x), ('raw', renders['raw']), ('game', renders['game'])):
        write_wav(os.path.join(out, k + '-listen.wav'), norm(y)); files[k] = k + '-listen.wav'
        spectrogram(os.path.join(out, k + '.png'), y, times, rpm, conf if k == 'ref' else np.zeros_like(conf) + 1, cyl, t, state); files[k + '_png'] = k + '.png'
    for k in ('raw', 'game'):
        p = os.path.join(out, k + '.wav')
        if os.path.exists(p): os.remove(p)
    page = html(name, f'{os.path.basename(path)}, engine {engine}', results, files, len(x) / FS)
    open(os.path.join(out, 'index.html'), 'w', encoding='utf-8').write(page)
    json.dump(dict(bank=a.bank, engine=engine, seconds=len(x) / FS, lifts=len(lifts), shifts=len(shifts),
                   pulling=float((state == 1).mean()), lifted=float((state == 0).mean()), results=results),
              open(os.path.join(out, 'scores.json'), 'w'), indent=1)
    print(f'ENGINE_COMPARE {name}: {len(x)/FS:.0f} s, {len(lifts)} lift-offs, {len(shifts)} gearchanges, revs {r.min():.0f}-{r.max():.0f}')
    for mode in ('raw', 'game'):
        print(f'  {mode:4} overall {np.mean([q["score"] for q in results[mode]]):.1f}: ' + ', '.join(f'{q["key"]} {q["score"]:.1f}' for q in results[mode]))
    print('  ' + os.path.join(out, 'index.html'))

if __name__ == '__main__': main()
