# Engine grain bank builder (docs/47 stage 4, granular engines).
# Usage: python tools/engine-grains.py <recording.wav> <out.bytes> --cylinders 4 --idle 850 --redline 6000
#        [--rpm-csv known.csv] [--start s] [--end s] [--report out.png]
# Reads an engine recording (a rev sweep: idle to redline under load, and ideally back down), tracks the engine's
# revs from its firing frequency, cuts it into one-cycle grains (two crank turns, so uneven firing keeps its rhythm)
# and writes a bank the game's GrainSynth plays: grains tagged with rpm and load (rising revs = on load, falling =
# off load). Needs only numpy (Blender's Python has it).
import argparse, struct, wave, sys, math, os, json
import numpy as np

FS = 48000

def lp(path):
    # Recording names run past Windows' 260-character limit: use the long-path form.
    return '\\\\?\\' + os.path.abspath(path) if os.name == 'nt' else path

def read_wav(path):
    with open(lp(path), 'rb') as f: data = f.read()
    if data[:4] != b'RIFF' or data[8:12] != b'WAVE': raise SystemExit('not a WAV file: ' + path)
    pos = 12; fmt = None; pcm = None
    while pos + 8 <= len(data):
        cid = data[pos:pos+4]; size = struct.unpack('<I', data[pos+4:pos+8])[0]; body = data[pos+8:pos+8+size]
        if cid == b'fmt ': fmt = struct.unpack('<HHIIHH', body[:16])
        elif cid == b'data': pcm = body
        pos += 8 + size + (size & 1)
    tag, channels, rate, _, _, bits = fmt
    if tag == 0xFFFE: tag = 3 if bits == 32 and b'\x03\x00\x00\x00\x00\x00\x10\x00' in data[:200] else 1
    if tag == 3: x = np.frombuffer(pcm, dtype='<f4' if bits == 32 else '<f8').astype(np.float64)
    elif bits == 16: x = np.frombuffer(pcm, dtype='<i2') / 32768.0
    elif bits == 24:
        b = np.frombuffer(pcm[:len(pcm)//3*3], dtype=np.uint8).reshape(-1, 3)
        v = (b[:, 0].astype(np.int32) | (b[:, 1].astype(np.int32) << 8) | (b[:, 2].astype(np.int32) << 16))
        x = np.where(v >= 1 << 23, v - (1 << 24), v) / float(1 << 23)
    elif bits == 32: x = np.frombuffer(pcm, dtype='<i4') / 2147483648.0
    else: raise SystemExit(f'unsupported WAV: {bits} bits')
    x = x[:len(x)//channels*channels].reshape(-1, channels).mean(axis=1)
    return x, rate

def resample(x, rate):
    if rate == FS: return x
    if rate > FS:   # anti-alias: windowed-sinc low-pass at 0.45 of the new rate
        n = 127; k = np.arange(n) - n // 2; fc = 0.45 * FS / rate
        h = np.sinc(2 * fc * k) * np.hanning(n); h /= h.sum(); x = np.convolve(x, h, mode='same')
    n = int(round(len(x) * FS / rate)); t = np.arange(n) * rate / FS
    return np.interp(t, np.arange(len(x)), x)

def track_rpm(x, cylinders, idle, redline, hop=0.01):
    # Harmonic-sum pitch tracking of the firing frequency, then a Viterbi path over a fine log-frequency grid.
    # Score(f) = sum over harmonics 1-6 of the log spectrum at h*f: a candidate an octave too low misses half the
    # engine's strong harmonics (and one too high skips the true note), so octave errors lose. The path may move a
    # few percent per 10 ms frame (a gearshift drop), not jump. Engine off or far away (quiet) is untrusted.
    fmin = idle / 60 * cylinders / 2 * 0.7; fmax = redline / 60 * cylinders / 2 * 1.08
    n = 8192; pad = 32768; h = int(hop * FS); win = np.hanning(n)
    bins = np.fft.rfftfreq(pad, 1 / FS); df = bins[1]
    step = 0.005; grid = fmin * np.exp(np.arange(0, math.log(fmax / fmin), step)); G = len(grid)
    H = 6; weights = np.array([1 / (k ** 0.3) for k in range(1, H + 1)])
    idx = np.array([[min(len(bins) - 2, int(round(k * f / df))) for f in grid] for k in range(1, H + 1)])
    frames = list(range(0, max(1, len(x) - n), h)); T = len(frames)
    score = np.empty((T, G), np.float32); levels = np.empty(T); times = np.empty(T)
    for t, start in enumerate(frames):
        seg = x[start:start + n]
        if len(seg) < n: seg = np.pad(seg, (0, n - len(seg)))
        S = np.abs(np.fft.rfft(seg * win, pad)); levels[t] = math.sqrt(np.mean(seg ** 2)); times[t] = (start + n / 2) / FS
        L = np.log1p(S / (np.median(S) + 1e-12))
        Lm = np.maximum(np.maximum(L[:-2], L[1:-1]), L[2:]); Lm = np.concatenate([[L[0]], Lm, [L[-1]]])   # +-1 bin tolerance
        sc = (Lm[idx] * weights[:, None]).sum(axis=0)
        score[t] = sc / (sc.max() + 1e-9)
    # Viterbi: cost = -score + a smooth-motion penalty; up to 4 grid steps (2%) per frame move freely.
    reach = 12; move = np.arange(-reach, reach + 1); pen = np.where(np.abs(move) <= 4, 0.01 * np.abs(move), 0.05 + 0.08 * (np.abs(move) - 4))
    cost = -score[0].astype(np.float64); back = np.zeros((T, G), np.int16)
    for t in range(1, T):
        best = np.full(G, np.inf); arg = np.zeros(G, np.int16)
        for m, p in zip(move, pen):
            shifted = np.full(G, np.inf)
            if m >= 0: shifted[m:] = cost[:G - m] if m > 0 else cost
            else: shifted[:m] = cost[-m:]
            c = shifted + p; better = c < best; best[better] = c[better]; arg[better] = m
        back[t] = arg; cost = best - score[t]
    path = np.zeros(T, int); path[-1] = int(np.argmin(cost))
    for t in range(T - 1, 0, -1): path[t - 1] = path[t] - back[t][path[t]]
    freqs = grid[path]; conf = score[np.arange(T), path]
    # Trust: the path's harmonic score against the frame's best, and a real engine level.
    conf = np.where(levels < 0.1 * np.percentile(levels, 90), 0, conf)
    return times, freqs * 120 / cylinders, conf, levels

def report(path, x, times, rpm, conf, cylinders):
    # Spectrogram (40 Hz - 4 kHz, log) with the tracked firing frequency drawn over it (white where trusted).
    import zlib
    W, H = 1400, 360; n = 4096; hop = max(1, (len(x) - n) // W); win = np.hanning(n); cols = []
    for i in range(W):
        seg = x[i*hop:i*hop+n]
        if len(seg) < n: seg = np.pad(seg, (0, n - len(seg)))
        cols.append(np.abs(np.fft.rfft(seg * win)))
    S = np.array(cols).T; fr = np.fft.rfftfreq(n, 1 / FS); rows = np.geomspace(40, 4000, H)[::-1]
    img = 20 * np.log10(S[np.searchsorted(fr, rows)] / S.max() + 1e-9); v = np.clip((img + 70) / 70, 0, 1)
    rgb = (np.stack([v ** 1.5, v ** 0.9 * 0.8, 0.35 + 0.4 * v], -1) * 255).astype(np.uint8)
    f = rpm / 120 * cylinders
    for i in range(W):
        t = (i * hop + n / 2) / FS; fi = np.interp(t, times, f); c = np.interp(t, times, conf)
        y = int(np.searchsorted(-rows, -fi)); 
        if 0 <= y < H: rgb[max(0, y-1):y+2, i] = (255, 255, 255) if c > 0.6 else (255, 60, 60)
    raw = b''.join(b'\x00' + rgb[y].tobytes() for y in range(H))
    def chunk(t, d): return struct.pack('>I', len(d)) + t + d + struct.pack('>I', zlib.crc32(t + d) & 0xffffffff)
    open(path, 'wb').write(b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', W, H, 8, 2, 0, 0, 0)) + chunk(b'IDAT', zlib.compress(raw, 6)) + chunk(b'IEND', b''))

def cut_grains(x, times, rpm, conf):
    # Cycle phase = integral of the cycle rate (rpm/120); a grain boundary at every whole cycle.
    t = np.arange(len(x)) / FS; r = np.interp(t, times, rpm)
    phase = np.cumsum(r / 120 / FS)
    edges = np.searchsorted(phase, np.arange(math.ceil(phase[0]) + 1, phase[-1]))
    grains = []
    for a, b in zip(edges[:-1], edges[1:]):
        g = x[a:b]; mid = (a + b) // 2; slope = np.interp(mid / FS + 0.1, times, rpm) - np.interp(mid / FS - 0.1, times, rpm)
        # The tail: the start of the next cycle as recorded, so the game's crossfade into the true next cycle is the
        # recording itself (overlapping two different stretches of audio sounded like two engines at once).
        tail = x[b:b + int(0.3 * (b - a))]
        grains.append((float(np.mean(r[a:b])), 1 if slope >= -50 else 0, g, float(np.interp(mid / FS, times, conf)), int(a), tail))
    return grains

def pull_rise(x, times, rpm, conf):
    # How much louder the recording gets per doubling of revs while pulling (docs/55): 20 ms levels on the frames
    # where the revs climb (over 120 rpm/s) and the engine is within 30 dB of its loudest, against log2 revs. (The
    # cycles' own levels, the old measure, mixed in gentle part-throttle rises and read the Skyline at 3.7 dB, its
    # pulls at 7.4.) Returns (log2 rpm, dB) points.
    T = int(len(x) / FS / 0.01); t = np.arange(T) * 0.01; good = conf >= 0.5
    if good.sum() < 10: return []
    r = np.interp(t, times[good], rpm[good]); c = np.interp(t, times, conf); k = 6; slope = np.zeros(T); slope[k:-k] = (r[2*k:] - r[:-2*k]) / (2 * k * 0.01)
    lv = np.array([10 * math.log10(np.mean(x[max(0, int(ti * FS) - 480):int(ti * FS) + 480] ** 2) + 1e-12) for ti in t])
    m = (slope > 120) & (slope < 2200) & (c >= 0.5) & (lv > np.percentile(lv, 90) - 30)
    # (And the coasting frames: revs falling 120-2200 rpm/s, engine braking. Real overrun loudness barely follows the
    # revs; levelled like pulling, every in-game lift-off kept fading as the revs fell: -5 to -6 dB after 1 s against
    # the recordings' -1 to -2.)
    k2 = (slope < -120) & (slope > -2200) & (c >= 0.5) & (lv > np.percentile(lv, 90) - 30)
    COAST.extend(zip(np.log2(np.maximum(r[k2], 1)), lv[k2]))
    return list(zip(np.log2(np.maximum(r[m], 1)), lv[m]))
COAST = []

def shift_dips(x, times, rpm, conf):
    # Gearchanges in the recording, read exactly as the scorecard reads them (tools/engine-compare.py: its driving
    # states and events), so playback and scoring agree on what a shift is. For each, the 20 ms level from the shift's
    # start for 0.6 s in 50 ms steps, against the median of the 0.15 s before, less what the revs alone explain (the
    # cycles already follow the revs; RISE is filled in by build()).
    ec = __import__('engine-compare')
    t, r, c, thr, sh, up, state = ec.read_inputs(times, rpm, conf, x); lifts, shifts = ec.events(state)
    lv = np.array([10 * math.log10(np.mean(x[max(0, int(ti * FS) - 480):int(ti * FS) + 480] ** 2) + 1e-12) for ti in t])
    out = []
    for i in shifts:
        if i < 20 or i + 60 >= len(t) or c[i] < 0.5: continue
        base = np.median(lv[i - 15:i]); r0 = np.median(r[i - 15:i])
        out.append([float(np.mean(lv[i + s * 5:i + s * 5 + 5]) - base - RISE[0] * math.log2(max(1, np.mean(r[i + s * 5:i + s * 5 + 5])) / max(1, r0))) for s in range(12)])
    return out
RISE = [6.0]

def build(sources, cylinders, idle, redline, out, report_base='', min_confidence=0.6, per_bin=4, track_cylinders=0):
    # track_cylinders: on some vees each bank's pulse train is the strongest line (half the firing rate); the
    # tracker then follows that line as if the engine had half the cylinders, and the revs come out right.
    # sources: [(wav path, start s, end s)]; every file's grains go into one bank.
    grains = []; tracked = []; shift_src = []; rise_pts = []; COAST.clear()
    for n, (path, start, end) in enumerate(sources):
        x, rate = read_wav(path); x = resample(x, rate)
        if end > 0: x = x[:int(end * FS)]
        x = x[int(start * FS):]; x = x / (np.max(np.abs(x)) + 1e-9) * 0.9
        times, rpm, conf, level = track_rpm(x, track_cylinders or cylinders, idle, redline); tracked.append((rpm.min(), rpm.max(), np.median(conf)))
        rise_pts += pull_rise(x, times, rpm, conf) if n == 0 else []; shift_src.append((x, times, rpm, conf))
        if report_base: report(f'{report_base}-{n + 1}.png', x, times, rpm, conf, track_cylinders or cylinders)
        grains += [g for g in cut_grains(x, times, rpm, conf) if g[3] >= min_confidence]
    # Sweeps, not scattered cycles (Richard, 1 Oct: revving up flickered between rev noises). Consecutive trusted
    # cycles whose revs keep rising form a pulling sweep; falling, a coasting sweep. The widest sweeps are kept whole
    # and in order, so the game can step through real consecutive cycles; others only fill revs not yet covered.
    grains = [g for g in grains if abs(len(g[2]) / (FS * 120 / g[0]) - 1) <= 0.08 and idle * 0.6 <= g[0] <= redline * 1.1]
    runs = []; cur = []
    def close():
        if len(cur) >= 12:
            lo_r = min(c[0] for c in cur); hi_r = max(c[0] for c in cur)
            direction = 1 if cur[-1][0] >= cur[0][0] else 0
            if hi_r / lo_r > 1.12: runs.append((direction, lo_r, hi_r, list(cur)))
            # Steady idle is not a sweep but is needed: keep it with the coasting sweeps.
            elif lo_r < idle * 1.5 and len(cur) >= 24: runs.append((0, lo_r, hi_r, list(cur)))
    for g in grains:
        if cur:
            last = cur[-1]; gap = g[4] - (last[4] + len(last[2])); step = g[0] / last[0] - 1
            rising = cur[-1][0] >= cur[0][0] if len(cur) > 3 else step >= 0
            # Same sweep: next cycle in time, revs moving the same way (small wobble allowed).
            if abs(gap) < 64 and (step >= -0.015 if rising else step <= 0.015) and abs(step) < 0.08:
                cur.append(g); continue
            close(); cur = []
        cur.append(g)
    close()
    kept = []; run_id = 0; coverage = {0: set(), 1: set()}
    for direction in (1, 0):
        for d, lo_r, hi_r, run in sorted([r for r in runs if r[0] == direction], key=lambda r: -(r[2] / r[1])):
            bins = {int(c[0] // 40) for c in run}
            if len(bins - coverage[direction]) < 0.25 * len(bins): continue   # adds little
            coverage[direction] |= bins; run_id += 1
            for c in run: kept.append((c[0], direction, c[2], run_id, c[5]))
    if not kept: raise SystemExit(f'{out}: no usable sweeps: check cylinders/idle/redline or the recording')
    # Level: the recordings' loudness wanders with microphone and distance (the Escort's pulls ran 19x quieter at
    # low revs than at the top; Richard, 1 Oct: mostly quiet, one rev range at a good level). Each sweep follows a
    # smooth target instead, pulling rising 2x from bottom to top revs and coasting at 65% of it. The correction
    # is smoothed over nine cycles so each cycle keeps its own texture.
    lo_r = min(k[0] for k in kept); hi_r = max(k[0] for k in kept); levelled = []
    # What the levelling must keep (docs/55, the engine scorecard): how much louder the engine gets as the revs rise on
    # the throttle, and how much quieter it is lifted than pulling at the same revs, both measured from the recording.
    # (A fixed 6 dB rise and lifted at 65% of pulling dropped every lift-off 6-8 dB at once; real ones drop 1-4 dB.)
    def db(g): return 20 * math.log10(math.sqrt(np.mean(g ** 2)) + 1e-6)
    pull_bins, coast_bins = {}, {}
    for r, load, g, rid, tail in kept: (pull_bins if load == 1 else coast_bins).setdefault(int(r // 250), []).append(db(g))
    gaps = [np.median(pull_bins[k]) - np.median(coast_bins[k]) for k in pull_bins if k in coast_bins and len(pull_bins[k]) >= 4 and len(coast_bins[k]) >= 4]
    # Floors: the game is heard from behind the car, by the exhaust. Exhaust microphones lift 4-10 dB (the Skyline 9.8,
    # the sprint car 5.0) and gain 4-8 dB per doubling of revs; engine-bay microphones mostly hear the valve train,
    # which barely changes with load (0 dB), and would leave the throttle inaudible.
    contrast = float(np.clip(np.median(gaps), 3, 12)) if gaps else 4.0
    pts = [(math.log2((k + 0.5) * 250), np.median(v)) for k, v in pull_bins.items() if len(v) >= 4]
    rise = float(np.clip(np.polyfit([p[0] for p in rise_pts], [p[1] for p in rise_pts], 1)[0], 3, 12)) if len(rise_pts) >= 30 else         float(np.clip(np.polyfit([p[0] for p in pts], [p[1] for p in pts], 1)[0], 3, 12)) if len(pts) >= 3 else 6.0
    # Both kinds are levelled alike; playback applies the lift-off drop as a gain from the header (coasting sweeps
    # rarely cover every rev range, and pulling cycles stand in for them).
    coast_rise = float(np.clip(np.polyfit([p[0] for p in COAST], [p[1] for p in COAST], 1)[0], 0, rise)) if len(COAST) >= 30 and np.ptp([p[0] for p in COAST]) > .3 else rise
    def target(r, load): return max(0.02, 0.16 * 10 ** ((rise if load == 1 else coast_rise) * math.log2(r / hi_r) / 20))
    # The gearchange dip, measured like the lift-off (median of the recording's shifts). Deeper than 15 dB is not a
    # gearchange (the Niro hybrid's "shifts" are its engine stopping): then, and without three shifts, 4 dB.
    # The gearchange (docs/55): the recording's median level curve through its shifts, 0.6 s in 50 ms steps, beyond
    # the revs' own effect. Deeper than 15 dB is not a gearchange (the Niro hybrid's engine stopping); that, or fewer
    # than three shifts: a gentle generic dip (-3 dB for 0.2 s).
    RISE[0] = rise; shift_all = [c for src in shift_src for c in shift_dips(*src)]
    curve = np.median(np.array(shift_all), 0) if len(shift_all) >= 3 else None
    if curve is None or curve.min() < -15: curve = np.array([0, -1.5, -3, -3, -3, -1.5, 0, 0, 0, 0, 0, 0], float)
    curve = np.clip(curve - max(0, curve[0]), -15, 3); shift_db = float(-curve.min())
    for run in sorted({k[3] for k in kept}):
        idx = [i for i, k in enumerate(kept) if k[3] == run]
        rms = np.array([math.sqrt(np.mean(kept[i][2] ** 2)) + 1e-6 for i in idx]); lg = np.log(rms)
        smooth = np.array([np.median(lg[max(0, j - 4):j + 5]) for j in range(len(lg))])
        for j, i in enumerate(idx):
            r, load, g, rid, tail = kept[i]
            gain = min(6, max(0.25, target(r, load) / math.exp(smooth[j])))
            levelled.append((r, load, g * gain, rid, tail * gain))
    kept = levelled
    # Headroom per cycle: a bang (the Fabia's anti-lag) is limited on its own, not by turning the whole bank down.
    def limit(k):
        r, l, g, rid, t = k; peak = max(np.max(np.abs(g)), np.max(np.abs(t)) if len(t) else 0)
        return (r, l, g * 0.98 / peak, rid, t * 0.98 / peak) if peak > 0.98 else k
    kept = [limit(k) for k in kept]
    os.makedirs(os.path.dirname(out) or '.', exist_ok=True)
    with open(out, 'wb') as f:
        # Version 4: the header also carries the measured lift-off drop and rev-rise (dB), which playback uses.
        f.write(b'LRGB'); f.write(struct.pack('<iiiffi', 6, FS, cylinders, idle, redline, len(kept))); f.write(struct.pack('<fff', contrast, rise, shift_db))
        f.write(struct.pack('<12f', *curve))   # version 6: the gearchange curve
        for r, load, g, run, tail in kept:
            pcm = np.clip(np.round(np.concatenate([g, tail]) * 32767), -32768, 32767).astype('<i2')
            f.write(struct.pack('<fBHii', r, load, run, len(g), len(pcm))); f.write(pcm.tobytes())
    on = [g[0] for g in kept if g[1] == 1]; off = [g[0] for g in kept if g[1] == 0]
    print(f'ENGINE_GRAINS {os.path.basename(out)}: {len(kept)} grains in {run_id} sweeps ({os.path.getsize(out) / 1e6:.1f} MB) from {len(grains)} trusted cycles; '
          f'on load {len(on)} ({min(on) if on else 0:.0f}-{max(on) if on else 0:.0f} rpm), '
          f'off load {len(off)} ({min(off) if off else 0:.0f}-{max(off) if off else 0:.0f} rpm); rise {rise:.1f} dB per doubling (coasting {coast_rise:.1f}), lifted {contrast:.1f} dB under, shifts dip {shift_db:.1f} dB ({len(shift_all)} found: {' '.join(f'{v:+.0f}' for v in curve)}); tracked '
          + ', '.join(f'{a:.0f}-{b:.0f} rpm (confidence {c:.2f})' for a, b, c in tracked))

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('wav', nargs='?'); ap.add_argument('out', nargs='?')
    ap.add_argument('--config', default='', help='tools/engine-banks.json: build every bank (or those named with --only)')
    ap.add_argument('--only', nargs='*', default=[])
    ap.add_argument('--cylinders', type=int); ap.add_argument('--idle', type=float); ap.add_argument('--redline', type=float)
    ap.add_argument('--start', type=float, default=0); ap.add_argument('--end', type=float, default=0)
    ap.add_argument('--report', default=''); ap.add_argument('--min-confidence', type=float, default=0.6); ap.add_argument('--per-bin', type=int, default=4)
    a = ap.parse_args()
    if a.config:
        here = os.path.dirname(os.path.abspath(a.config)); cfg = json.load(open(a.config, encoding='utf-8')); root = os.path.join(here, cfg['sources'])
        for name, b in cfg['banks'].items():
            if a.only and name not in a.only: continue
            srcs = [(os.path.join(root, f['file']), f.get('start', 0), f.get('end', 0)) for f in b['files']]
            missing = [p for p, _, _ in srcs if not os.path.exists(lp(p))]
            if missing: print(f'skip {name}: not downloaded yet ({os.path.basename(missing[0])})'); continue
            build(srcs, b['cylinders'], b['idle'], b['redline'], os.path.join(here, cfg['output'], name + '.bytes'),
                  os.path.join(here, '..', 'review', 'grains', name), b.get('minConfidence', a.min_confidence), b.get('perBin', a.per_bin), b.get('trackCylinders', 0))
        return
    build([(a.wav, a.start, a.end)], a.cylinders, a.idle, a.redline, a.out, a.report[:-4] if a.report.endswith('.png') else a.report, a.min_confidence, a.per_bin)

if __name__ == '__main__': main()
