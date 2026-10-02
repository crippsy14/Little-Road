# In-game engine scorecard (docs/55). Usage (project root, Blender's Python), after tools/engine-ingame-capture.py:
#   python tools/engine-ingame.py [engine_id ...]
# Scores what the game actually plays: each engine's capture from the scripted test drive (its own gearbox, revs and
# lifts, including revs its recording never reached) against how its real recording behaves. There is no matching
# recording of the game's drive, so each measure compares a property, not a waveform:
#   pitch     the note follows the game's revs (tracked from the capture, in cents);
#   chipmunk  how fast brightness climbs with revs while pulling (octaves of spectral centroid per octave of revs),
#             above the recording's top revs, against the real engine's own pulls (squeezed grains drag the exhaust's
#             resonances up with the note: a slope near 1; real engines' resonances stay put);
#   garble    noise between the harmonics while pulling, and clicks (sudden bursts above 2 kHz) per second, against
#             the recording's;
#   lift-off and gearchanges: the level curves at the game's own lifts and upshifts against the recording's;
#   rise      how much louder it gets per doubling of revs, pulling.
# Coverage (the share of the pull above the recording's top revs, and the furthest stretch) is reported, not scored.
# Writes review/engine-ingame/<engine>/ingame.json and prints a table.
import json, math, os, struct, subprocess, sys
import numpy as np
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
ec = __import__('engine-compare'); eg = ec.eg
FS = eg.FS; HOP = 0.01; ROOT = ec.ROOT
CFG = json.load(open(os.path.join(ROOT, 'tools', 'engine-banks.json'), encoding='utf-8'))
PARTS = {p['id']: p for p in json.load(open(os.path.join(ROOT, 'unity', 'Assets', 'Resources', 'Data', 'parts.json'), encoding='utf-8'))['parts']}
# SOUND_OVERRIDE="engine_s_v8=ferrari-458,...": try other recordings for engines without changing the game data
# (offline only: the in-game capture plays whatever parts.json says).
for pair in filter(None, os.environ.get('SOUND_OVERRIDE', '').split(',')):
    k, v = pair.split('='); PARTS[k] = dict(PARTS[k], sound=v)

def bank_top(bank):
    # The bank's highest pulling cycle (the game stretches above 97% of it).
    with open(os.path.join(ROOT, 'unity', 'Assets', 'Resources', 'Engines', bank + '.bytes'), 'rb') as f:
        assert f.read(4) == b'LRGB'; version, fs, cyl, idle, red, n = struct.unpack('<iiiffi', f.read(24))
        if version >= 4: f.read(12 if version >= 5 else 8)
        if version >= 6: f.read(48)
        top = 0
        for _ in range(n):
            r, load, run = struct.unpack('<fBH', f.read(7)); cyc, ln = struct.unpack('<ii', f.read(8)) if version >= 3 else (0, struct.unpack('<i', f.read(4))[0])
            f.seek(ln * 2, 1)
            if load == 1: top = max(top, r)
    return top * 0.97

def brightness(bands):
    # Spectral centroid (log2 Hz) over the third-octave bands from 100 Hz to 8 kHz.
    k = (ec.BANDS >= 100) & (ec.BANDS <= 8000); e = 10 ** (bands[:, k] / 10)
    return (e * np.log2(ec.BANDS[k])).sum(1) / (e.sum(1) + 1e-18)

def clicks(x, mask_t, t):
    # Sudden bursts above 2 kHz: 5 ms frames whose energy jumps 10 dB over the median of the 100 ms around them.
    n = 240; hp = np.diff(x, prepend=0); hp = np.diff(hp, prepend=0)   # second difference: a cheap high-pass (~+12 dB/oct)
    e = np.array([np.mean(hp[i:i + n] ** 2) + 1e-14 for i in range(0, len(x) - n, n)]); db = 10 * np.log10(e)
    med = np.array([np.median(db[max(0, i - 10):i + 10]) for i in range(len(db))])
    spikes = (db - med > 10)
    ft = (np.arange(len(db)) * n + n / 2) / FS; use = np.interp(ft, t, mask_t.astype(float)) > .5
    secs = use.sum() * n / FS
    return float(spikes[use].sum() / secs) if secs > 1 else float('nan')

def periodicity(x, t, r, mask, emphasis):
    # How closely each engine cycle repeats the one before: the best normalised match within +-5% of the cycle length
    # the revs give (real engines wander a little; logged revs are not sample-exact). A real engine on full throttle
    # repeats almost exactly (0.8-0.97 over the full band); unrelated cycles crossfaded together, or two streams at once,
    # do not. emphasis: a first difference, which weights the mid band (where the V6s lost their repeat).
    h = np.diff(x, prepend=0) if emphasis else x.astype(float); out = []
    e2 = np.concatenate([[0], np.cumsum(h * h)])
    for i in np.where(mask)[0][::3]:
        L = int(round(FS * 120 / max(r[i], 300))); c = int(t[i] * FS); lo = int(L * 1.05); hi = int(L * .95)
        if c - lo < 0 or c + L > len(h): continue
        a = h[c:c + L]; na = math.sqrt(float(np.dot(a, a)))
        if na == 0: continue
        dots = np.correlate(h[c - lo:c - hi + L], a, 'valid'); starts = np.arange(c - lo, c - hi + 1)
        out.append(float(np.max(dots / (na * np.sqrt(e2[starts + L] - e2[starts]) + 1e-12))))
    return float(np.median(out)) if len(out) >= 10 else float('nan')

def slope(xs, ys):
    if len(xs) < 30 or np.ptp(xs) < .2: return float('nan')
    return float(np.polyfit(xs, ys, 1)[0])

def curves(level, events, span):
    out = []
    for i in events:
        a, b = i + span[0], i + span[1]
        if a < 5 or b >= len(level): continue
        out.append(level[a:b] - np.median(level[a - 5:i]))
    return np.median(out, 0) if out else None

def stats(x, t, r, state, cyl, lifts, shifts, top=None):
    # The properties scored, from any signal with its revs and driving state on the 10 ms grid.
    A = ec.analyse(x, t, r, cyl); loud = np.percentile(A['level'], 90)
    pull = (state == 1) & (A['level'] > loud - 30) & (r > 0)
    br = brightness(A['bands']); lr = np.log2(np.maximum(r, 1))
    s = dict(level=A['level'])
    # Tone colour by rev band (500 rpm): the mean third-octave balance while pulling, for band-by-band comparison.
    # (Full throttle only, within 12 dB of the loudest, on both sides: the game's test drive is flat out, and the
    # recording's gentle part-throttle rises have another tone altogether.)
    after_shift = np.convolve((state == 2).astype(float), np.ones(30), 'full')[:len(state)] > 0
    full = pull & (A['level'] > loud - 12) & ~after_shift   # (and steady: from 0.3 s after a gearchange)
    s['tone'] = {int(lo): np.mean(A['bands'][full & (r >= lo) & (r < lo + 500)], 0) for lo in range(0, 16000, 500) if (full & (r >= lo) & (r < lo + 500)).sum() >= 15}
    s['bright_slope'] = slope(lr[pull], br[pull])
    if top:
        above = pull & (r > top * 1.05); within = pull & (r <= top)
        s['bright_slope_above'] = slope(lr[above], br[above]); s['bright_slope_within'] = slope(lr[within], br[within])
        s['noise_above'] = float(np.median(A['noise'][above])) if above.sum() > 20 else float('nan')
        s['above_share'] = float(above.sum() / max(1, pull.sum())); s['stretch'] = float(r[pull].max() / top) if pull.sum() else 1
    s['noise'] = float(np.median(A['noise'][pull])) if pull.sum() > 20 else float('nan')
    hard = pull & (A['level'] > loud - 12)
    s['period'] = periodicity(x, t, r, hard, False); s['period_mid'] = periodicity(x, t, r, hard, True)
    s['clicks'] = clicks(x, pull, t)
    # (Steady pulling only: from 0.3 s after a gearchange. The recovery after a shift is the gearchange measure's, and
    # mixed in it read the steep rise within each gear as almost none across them.)
    after = np.convolve((state == 2).astype(float), np.ones(30), 'full')[:len(state)] > 0
    steady = pull & ~after
    s['rise'] = slope(lr[steady], A['level'][steady])
    s['lift'] = curves(A['level'], lifts, (-20, 100)); s['shift'] = curves(A['level'], shifts, (-10, 50))
    return s

REF_VERSION = 5   # bump when stats() changes, to rebuild the cached references
def reference(bank):
    # The real recording, read the way tools/engine-compare.py reads it, but whole (the bank is built from the whole
    # file: a 90 s cut left some references without the top revs the chipmunk measure compares against). Cached in
    # review/engine-ingame/ref-<bank>.pkl (long recordings take minutes).
    import pickle
    b = CFG['banks'][bank]; src = b['files'][0]; path = os.path.join(ROOT, 'tools', CFG['sources'], src['file'])
    cache = os.path.join(ROOT, 'review', 'engine-ingame', f'ref-{bank}.pkl'); stamp = (REF_VERSION, os.path.getmtime(eg.lp(path)), src.get('start', 0), src.get('end', 0))
    if os.path.exists(cache):
        try:
            got = pickle.load(open(cache, 'rb'))
            if got['stamp'] == stamp: return got['stats']
        except Exception: pass
    x, rate = eg.read_wav(path); x = eg.resample(x, rate)
    if src.get('end', 0) > 0: x = x[:int(src['end'] * FS)]
    x = x[int(src.get('start', 0) * FS):]; x = x / (np.max(np.abs(x)) + 1e-9) * 0.9
    cyl = b.get('trackCylinders', 0) or b['cylinders']
    times, rpm, conf, _ = eg.track_rpm(x, cyl, b['idle'], b['redline'])
    t, r, c, thr, sh, up, state = ec.read_inputs(times, rpm, conf, x); lifts, shifts = ec.events(state)
    state = np.where(c >= .5, state, -1)
    st = stats(x, t, r, state, cyl, lifts, shifts); st.pop('level', None)
    os.makedirs(os.path.dirname(cache), exist_ok=True); pickle.dump({'stamp': stamp, 'stats': st}, open(cache, 'wb'))
    return st

def capture(engine, wav='player-engine.wav'):
    folder = os.path.join(ROOT, 'review', 'engine-ingame', engine)
    x, _ = eg.read_wav(os.path.join(folder, wav))
    log = np.genfromtxt(os.path.join(folder, 'player.csv'), delimiter=',', names=True)
    T = int(len(x) / FS / HOP); t = np.arange(T) * HOP
    near = lambda col: log[col][np.clip(np.searchsorted(log['time'], t), 0, len(log) - 1)]
    r = np.interp(t, log['time'], log['rpm']); thr = np.interp(t, log['time'], log['throttle'])
    shifting = near('shifting') > .5; gear = near('gear'); speed = np.interp(t, log['time'], log['speed'])
    state = np.full(T, -1)
    # (Bouncing on the limiter is not pulling: its cuts would be judged as the engine's tone and level. The 75 ms cut
    # and the following 150 ms are left out.)
    lim = near('limiter') > .5; lim = np.convolve(lim.astype(float), np.ones(15), 'full')[:T] > 0
    state[(thr >= .9) & ~shifting & ~lim & (speed > 1)] = 1
    state[(thr <= .05) & (thr >= -.05) & ~shifting & (speed > 3)] = 0
    state[shifting] = 2
    lifts = [i for i in range(30, T - 100) if thr[i - 1] > .05 and thr[i] <= .05 and (thr[i - 30:i] >= .9).all() and (np.abs(thr[i:i + 100]) <= .05).all()]
    ups = [i for i in range(10, T - 50) if shifting[i] and not shifting[i - 1] and gear[min(T - 1, i + 40)] > gear[i - 5]]
    return x, t, r, state, lifts, ups

def render_offline(engine, opts=''):
    # The in-game drive re-rendered by the sound harness from the capture's own log (revs, throttle with braking,
    # shifting, upshifts, limiter, gear): the same sound as the game's (docs/55), in seconds and without the game,
    # so a sound change can be scored on every engine's real in-game drive before confirming with a fresh capture.
    folder = os.path.join(ROOT, 'review', 'engine-ingame', engine); e = PARTS[engine]
    log = np.genfromtxt(os.path.join(folder, 'player.csv'), delimiter=',', names=True)
    x, _ = eg.read_wav(os.path.join(folder, 'player-engine.wav')); T = int(len(x) / FS / HOP); t = np.arange(T) * HOP
    near = lambda col: log[col][np.clip(np.searchsorted(log['time'], t), 0, len(log) - 1)]
    r = np.maximum(np.interp(t, log['time'], log['rpm']), e.get('idleRpm', 800)); thr = np.interp(t, log['time'], log['throttle'])
    sh = near('shifting'); lim = near('limiter'); gear = near('gear'); up = np.cumsum(np.diff(np.concatenate([[0], sh])) > 0)
    tag = os.environ.get('OFFLINE_TAG', '')   # (separate files for runs side by side on one engine)
    trace = os.path.join(folder, f'offline-trace{tag}.csv')
    with open(trace, 'w') as f:
        f.write('t,rpm,throttle,shifting,upshifts,limiter,gear' + chr(10))
        for i in range(T): f.write(f'{t[i]:.2f},{r[i]:.1f},{thr[i]:.3f},{int(sh[i])},{up[i]},{int(lim[i])},{int(max(0, gear[i] - 1))}' + chr(10))
    out = os.path.join(folder, f'offline-engine{tag}.wav')
    ec.PREBUILT = True; ec.run_harness(f'follow:{engine}|{e["sound"]}|{trace}|{out}' + (('|' + opts.replace(',', '|')) if opts else ''))
    return f'offline-engine{tag}.wav'

def score_engine(engine, refs, wav='player-engine.wav', tag=''):
    e = PARTS[engine]; bank = e['sound']; b = CFG['banks'][bank]; cyl = b.get('trackCylinders', 0) or b['cylinders']
    if bank not in refs: refs[bank] = reference(bank)
    R = refs[bank]; top = bank_top(bank)
    x, t, r, state, lifts, ups = capture(engine, wav)
    G = stats(x, t, r, state, cyl, lifts, ups, top)
    rows = []
    def add(key, label, err, tol, unit, note=''):
        if err is None or (isinstance(err, float) and math.isnan(err)): return
        rows.append(dict(key=key, label=label, err=float(err), tol=tol, unit=unit, score=ec.score(err, tol), note=note))
    # Pitch: the capture's tracked revs against the game's.
    tt, rr, cc, _ = eg.track_rpm(x, cyl, e.get('idleRpm', 800) * .7, e.get('redline', 7000) * 1.1)
    ro = np.interp(t, tt, rr); ok = (state >= 0) & (np.interp(t, tt, cc) > .5) & (r > e.get('idleRpm', 800) * 1.2)
    if ok.sum() > 50:
        # (Whole-number fractions folded, down to the cycle rate: real engines often sound strongest at half the firing
        # rate or at the cycle rate, and the tracker follows the strongest line. The fraction found is reported.)
        ratio = np.maximum(ro[ok], 1) / np.maximum(r[ok], 1); best = None
        for k in range(1, max(2, cyl) + 1):
            c = 1200 * np.log2(ratio * k); cents = np.abs(c - 1200 * np.round(c / 1200))
            if best is None or np.median(cents) < np.median(best[1]) - 5: best = (k, cents)
        k, cents = best
        add('pitch', 'Pitch follows the revs', float(np.median(cents)), 50, 'cents (median)',
            f'{100 * np.mean(cents < 100):.0f}% of the drive within a semitone' + (f'; strongest line 1/{k} of the expected note' + (' (the cycle rate: a once-per-cycle buzz)' if k == cyl else '') if k > 1 else ''))
    # Tone colour, rev band by rev band (third-octave balance, dB rms, weighted by the real engine's energy). Within the
    # recording, against the real engine at the same revs; above it ("chipmunk"), against the real engine at its highest
    # well-recorded revs: real exhausts' resonances stay put as the revs climb, so the top of the recording is the best
    # guide to the tone further up, and squeezed grains, which drag the resonances up with the note, land far from it.
    def tone_gap(a, b):
        w = 10 ** (b / 10); w = w / w.sum(); d = a - b; return float(math.sqrt(np.sum(w * d ** 2)))
    # (Wherever the real recording has the revs, even past the bank's top: some trusted cycles were left out of the bank,
    # but the recording itself still shows how the engine sounds there.)
    within = [tone_gap(G['tone'][k], R['tone'][k]) for k in G['tone'] if k in R['tone']]
    if within: add('tone', 'Tone colour at the same revs, pulling', float(np.mean(within)), 6, 'dB rms', f'{len(within)} rev bands compared with the real engine')
    # Chipmunk: beyond the revs the real recording covers, the spectral envelope (the exhaust's resonances) against the
    # real engine's at its highest revs. Only the shape from 3x the firing frequency (at least 400 Hz) to 6 kHz, every
    # band equal and the overall level removed: there the harmonics are dense, so the bands show resonances, not single
    # harmonics hopping from band to band as the revs move (which a real engine does too). Squeezed grains drag the
    # resonances up with the note. Allowed: the drift the real engine's own envelope shows per octave of revs.
    firing = lambda k: (k + 250) / 60 * e.get('cylinders', 4) / 2
    def envelope_gap(a, b, k):
        m = (ec.BANDS >= max(400, 3 * firing(k))) & (ec.BANDS <= 6000)
        if m.sum() < 4: return float('nan')
        da = a[m] - a[m].mean(); db = b[m] - b[m].mean(); return float(np.sqrt(np.mean((da - db) ** 2)))
    rtop = sorted(R['tone'])[-2:]; rmax = max(R['tone']) + 500 if R['tone'] else top
    above = [k for k in G['tone'] if k >= rmax]
    if rtop and above:
        ref_top = np.mean([R['tone'][k] for k in rtop], 0); topc = np.mean(rtop) + 250
        pts = [(math.log2(topc / (k + 250)), envelope_gap(R['tone'][k], ref_top, max(rtop))) for k in R['tone'] if k + 250 < topc * .95]
        pts = [p for p in pts if not math.isnan(p[1])]
        drift = float(np.clip(sum(d * g for d, g in pts) / max(1e-6, sum(d * d for d, g in pts)), 0, 12)) if len(pts) >= 2 else 4.0
        excess = [max(0.0, envelope_gap(G['tone'][k], ref_top, k) - drift * math.log2((k + 250) / topc)) for k in above]
        excess = [v for v in excess if not math.isnan(v)]
        if excess:
            add('chipmunk', 'No chipmunk: resonances above the recording', float(np.mean(excess)), 4, 'dB beyond natural drift',
                f'{len(above)} rev bands above the real recording ({above[0]}-{above[-1] + 500} rpm); real envelope drifts {drift:.1f} dB per octave of revs; worst excess {max(excess):.1f} dB')
    # Garble: the cycles fail to repeat the way the real engine's do (full band, and the mid band), pulling.
    gaps = [max(0.0, R[k] - G[k]) for k in ('period', 'period_mid') if not math.isnan(R[k]) and not math.isnan(G[k])]
    if gaps: add('garble', 'One clean engine (cycles repeat like the real one), pulling', max(gaps), .3, 'short of the real (0-1)',
        f'full band real {R["period"]:.2f}, game {G["period"]:.2f}; mid band real {R["period_mid"]:.2f}, game {G["period_mid"]:.2f}')
    if not math.isnan(G['clicks']) and not math.isnan(R['clicks']):
        add('clicks', 'No clicks (bursts per second), pulling', max(0.0, G['clicks'] - R['clicks']), 3, 'per second over the real', f'real {R["clicks"]:.1f}/s, game {G["clicks"]:.1f}/s')
    if G['lift'] is not None and R['lift'] is not None:
        add('liftoff', 'Lifting off (level over 1.2 s)', float(np.sqrt(np.mean((G['lift'] - R['lift']) ** 2))), 6, 'dB (rms over the curve)',
            f'{len(lifts)} lifts; after 0.5 s real {R["lift"][69]:+.0f} dB, game {G["lift"][69]:+.0f} dB; after 1 s real {R["lift"][119]:+.0f}, game {G["lift"][119]:+.0f}')
    # (A reference "shift" deeper than 15 dB is not a gearchange: the Niro hybrid's engine stopping. No measure then.)
    if G['shift'] is not None and R['shift'] is not None and R['shift'].min() > -15:
        add('shift', 'Gearchanges (level over 0.6 s)', float(np.sqrt(np.mean((G['shift'] - R['shift']) ** 2))), 6, 'dB (rms over the curve)',
            f'{len(ups)} upshifts; real dips to {R["shift"].min():.0f} dB, game to {G["shift"].min():.0f} dB')
    add('rise', 'Louder as the revs climb, pulling', abs(G['rise'] - R['rise']), 6, 'dB per doubling', f'real {R["rise"]:+.1f}, game {G["rise"]:+.1f}')
    result = dict(engine=engine, name=e['name'], tier=e.get('tier', 'C'), bank=bank, redline=e.get('redline'), recording_top=round(top),
                  above_share=G['above_share'], stretch=G['stretch'], rows=rows, overall=float(np.mean([q['score'] for q in rows])) if rows else 0,
                  lifts=len(lifts), upshifts=len(ups))
    json.dump(result, open(os.path.join(ROOT, 'review', 'engine-ingame', engine, f'ingame{tag}.json'), 'w'), indent=1)
    return result

def main():
    # --offline[=harness options]: score the harness's re-render of each engine's in-game drive instead of the capture.
    args = [a for a in sys.argv[1:] if not a.startswith('--offline')]; off = [a for a in sys.argv[1:] if a.startswith('--offline')]
    offline = off[0][10:] if off and off[0].startswith('--offline=') else ('' if off else None)
    engines = args or [p for p in PARTS if PARTS[p]['slot'] == 'engine' and os.path.exists(os.path.join(ROOT, 'review', 'engine-ingame', p, 'player-engine.wav'))]
    refs = {}; out = []
    if offline is not None and not os.environ.get("NOBUILD"): subprocess.run(['powershell', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', os.path.join(ROOT, 'tools', 'sound-harness.ps1'), '-Filter', 'none'], capture_output=True)
    for e in engines:
        res = score_engine(e, refs, render_offline(e, offline), '-offline') if offline is not None else score_engine(e, refs); out.append(res)
        print(f"{res['name'][:24]:24} {res['tier']} {res['bank']:10} in game {res['overall']:4.1f} | above recording {100 * res['above_share']:3.0f}% (x{res['stretch']:.2f}) | " +
              ', '.join(f"{q['key']} {q['score']:.1f}" for q in res['rows']), flush=True)
    return out

if __name__ == '__main__': main()
