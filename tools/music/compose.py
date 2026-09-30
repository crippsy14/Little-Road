# Little Road music (docs/49): themes per place, written as notes here, rendered through FluidSynth with the
# GeneralUser GS instruments (both in ../little-road-tools, outside the repo), cut into seamless loops.
# Usage: python tools/music/compose.py [name ...]
# Writes unity/Assets/Resources/Music/<name>.wav (the game's loops) and review/music/<name>-preview.wav.
# A loop is rendered twice through and the second pass kept, so its start carries the reverb tail of its end
# exactly as it will when it loops.
import os, sys, struct, subprocess, math
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
TOOLS = os.path.abspath(os.path.join(ROOT, '..', 'little-road-tools'))
FLUID = os.path.join(TOOLS, 'fluidsynth', 'fluidsynth-v2.6.1-win10-x64-cpp11', 'bin', 'fluidsynth.exe')
SF2 = os.path.join(TOOLS, 'soundfonts', 'GeneralUser-GS', 'GeneralUser-GS.sf2')
OUT = os.path.join(ROOT, 'unity', 'Assets', 'Resources', 'Music'); PREVIEW = os.path.join(ROOT, 'review', 'music')
FS = 48000; TPQ = 480

# ---------------------------------------------------------------- MIDI writing
def vlq(n):
    out = [n & 0x7F]; n >>= 7
    while n: out.append((n & 0x7F) | 0x80); n >>= 7
    return bytes(reversed(out))
def track(events):
    # events: (tick, bytes); sorted, delta-encoded, end-of-track appended
    data = b''; last = 0
    for tick, msg in sorted(events, key=lambda e: (e[0], e[1][0] & 0xF0 == 0x90)):
        data += vlq(int(tick - last)) + msg; last = tick
    data += vlq(0) + b'\xff\x2f\x00'
    return b'MTrk' + struct.pack('>I', len(data)) + data
def write_midi(path, bpm, parts, repeat, loop_beats):
    tempo = [(0, b'\xff\x51\x03' + struct.pack('>I', int(60_000_000 / bpm))[1:])]
    tracks = [track(tempo)]
    for p in parts:
        ch = p['channel']; ev = []
        if 'bank' in p: ev.append((0, bytes([0xB0 | ch, 0, p['bank']])))
        ev.append((0, bytes([0xC0 | ch, p['program']])))
        ev += [(0, bytes([0xB0 | ch, 7, p.get('volume', 100)])), (0, bytes([0xB0 | ch, 10, p.get('pan', 64)])),
               (0, bytes([0xB0 | ch, 91, p.get('reverb', 50)])), (0, bytes([0xB0 | ch, 93, p.get('chorus', 0)]))]
        for r in range(repeat):
            off = r * loop_beats
            for beat, dur, note, vel in p['notes']:
                t0 = int(round((beat + off) * TPQ)); t1 = int(round((beat + off + dur) * TPQ)) - 4
                ev.append((t0, bytes([0x90 | ch, note, max(1, min(127, int(vel)))]))); ev.append((max(t0 + 1, t1), bytes([0x80 | ch, note, 0])))
        tracks.append(track(ev))
    with open(path, 'wb') as f:
        f.write(b'MThd' + struct.pack('>IHHH', 6, 1, len(tracks), TPQ)); [f.write(t) for t in tracks]

# ---------------------------------------------------------------- theory helpers
NAMES = {'C': 0, 'C#': 1, 'Db': 1, 'D': 2, 'D#': 3, 'Eb': 3, 'E': 4, 'F': 5, 'F#': 6, 'Gb': 6, 'G': 7, 'G#': 8, 'Ab': 8, 'A': 9, 'A#': 10, 'Bb': 10, 'B': 11}
def n(name):   # 'F#4' -> midi
    pitch = name[:-1]; octave = int(name[-1]); return 12 * (octave + 1) + NAMES[pitch]
def chord(sym):   # 'D/F#', 'Bm', 'Am', 'G/B' -> (root pc, [intervals], bass pc)
    bass = None
    if '/' in sym: sym, b = sym.split('/'); bass = NAMES[b]
    minor = sym.endswith('m'); root = NAMES[sym[:-1] if minor else sym]
    return root, ([0, 3, 7] if minor else [0, 4, 7]), (root if bass is None else bass)
def voicing(sym, low):   # chord tones at or above `low` (midi), close position, 4 notes (root doubled)
    root, iv, _ = chord(sym); pcs = [(root + i) % 12 for i in iv] + [root % 12]; out = []; x = low
    for pc in pcs:
        while x % 12 != pc: x += 1
        out.append(x); x += 1
    return out
def melody(bars, beats_per_bar, lines, vel=80, shift=0):
    notes = []
    for b, line in enumerate(lines):
        for start, dur, name in line: notes.append((b * beats_per_bar + start, dur, n(name) + shift, vel))
    return notes
DRUM = dict(kick=36, side=37, snare=38, clap=39, brush=40, hat=42, pedal=44, open=46, crash=49, ride=51, tamb=54, shaker=82, maracas=70)

# ---------------------------------------------------------------- Bayview: sunny coastal G major, 96 bpm
BAY_CHORDS = ['G', 'D/F#', 'Em', 'C', 'G', 'Bm', 'C', 'D', 'Em', 'C', 'G', 'D', 'C', 'G/B', 'Am', 'D']
BAY_MELODY = [
    [(0, 1, 'D5'), (1, .5, 'B4'), (1.5, .5, 'D5'), (2, 1.5, 'G5'), (3.5, .5, 'E5')],
    [(0, 1.5, 'D5'), (1.5, .5, 'A4'), (2, 1, 'F#5'), (3, 1, 'E5')],
    [(0, .5, 'E5'), (.5, .5, 'G5'), (1, 1, 'B5'), (2, .5, 'A5'), (2.5, .5, 'G5'), (3, 1, 'E5')],
    [(0, 1.5, 'G5'), (1.5, .5, 'E5'), (2, 2, 'C5')],
    [(0, .5, 'B4'), (.5, .5, 'D5'), (1, 1, 'G5'), (2, 1, 'B5'), (3, 1, 'A5')],
    [(0, 1.5, 'F#5'), (1.5, .5, 'D5'), (2, 1, 'B4'), (3, 1, 'D5')],
    [(0, 1, 'E5'), (1, .5, 'G5'), (1.5, .5, 'E5'), (2, 1, 'C5'), (3, .5, 'E5'), (3.5, .5, 'D5')],
    [(0, 2, 'D5'), (2, .5, 'F#5'), (2.5, .5, 'A5'), (3, .5, 'C6'), (3.5, .5, 'A5')],
    [(0, 1.5, 'B5'), (1.5, .5, 'G5'), (2, 2, 'E5')],
    [(0, .5, 'G5'), (.5, .5, 'E5'), (1, 1, 'C5'), (2, 1, 'E5'), (3, 1, 'G5')],
    [(0, 1.5, 'D6'), (1.5, .5, 'B5'), (2, 1, 'G5'), (3, 1, 'B5')],
    [(0, 2, 'A5'), (2, 1, 'F#5'), (3, 1, 'D5')],
    [(0, .5, 'E5'), (.5, .5, 'G5'), (1, 1.5, 'C6'), (2.5, .5, 'B5'), (3, 1, 'A5')],
    [(0, 1, 'G5'), (1, 1, 'D5'), (2, 1, 'B4'), (3, 1, 'D5')],
    [(0, .5, 'C5'), (.5, .5, 'E5'), (1, 1, 'A5'), (2, .5, 'G5'), (2.5, .5, 'E5'), (3, 1, 'C5')],
    [(0, 1, 'D5'), (1, 1, 'F#5'), (2, 1, 'A5')],
]
def strum(chords, bpb, low, pattern, vel=60, spread=.02):
    out = []
    for b, sym in enumerate(chords):
        v = voicing(sym, low)
        for beat, dur, accent, up in pattern:
            order = list(reversed(v)) if up else v
            for i, note in enumerate(order): out.append((b * bpb + beat + i * spread, dur, note, vel * accent))
    return out
def bassline(chords, bpb, pattern, low=n('E2'), vel=84):
    out = []
    for b, sym in enumerate(chords):
        root, iv, bass = chord(sym); r = low + (bass - low) % 12; fifth = low + (root + 7 - low) % 12
        for beat, dur, which in pattern: out.append((b * bpb + beat, dur, r if which == 'r' else fifth if which == 'f' else r + 12, vel))
    return out
def drums(bars, bpb, hits):
    return [(b * bpb + beat, .1, DRUM[name], vel) for b in range(bars) for beat, name, vel in hits]

def bayview(race=False, lift=False):
    bpb = 4; bars = 16; parts = []
    if not race:
        parts.append(dict(channel=0, program=24, volume=92, pan=48, reverb=45, notes=strum(BAY_CHORDS, bpb, n('G3'), [(0, .9, 1, 0), (1.5, .45, .7, 1), (2, .45, .85, 0), (2.5, .45, .7, 1), (3.5, .45, .75, 1)], 58)))
        parts.append(dict(channel=1, program=12, volume=104, pan=70, reverb=55, notes=melody(bars, bpb, BAY_MELODY, 86)))
        parts.append(dict(channel=2, program=32, volume=96, pan=64, reverb=25, notes=bassline(BAY_CHORDS, bpb, [(0, 1.4, 'r'), (2, .9, 'f'), (3.5, .45, 'o')])))
        parts.append(dict(channel=3, program=9, volume=60, pan=80, reverb=70, notes=[x for x in melody(bars, bpb, BAY_MELODY, 50, 12) if x[0] >= 8 * bpb]))
        parts.append(dict(channel=9, program=0, volume=88, reverb=30, notes=drums(bars, bpb, [(b * .5, 'shaker', 52 if b % 2 else 70) for b in range(8)] + [(0, 'kick', 58), (2, 'kick', 50), (1, 'side', 46), (3, 'side', 50)])))
        return 96, bars * bpb, parts
    # Race: 124 bpm, driving but under the engines; the lift stem is the melody and brighter hats (last lap).
    if lift:
        parts.append(dict(channel=1, program=12, volume=100, pan=70, reverb=45, notes=melody(bars, bpb, BAY_MELODY, 88)))
        parts.append(dict(channel=4, program=80, volume=54, pan=58, reverb=40, notes=melody(bars, bpb, BAY_MELODY, 70)))
        parts.append(dict(channel=9, program=0, volume=80, reverb=20, notes=drums(bars, bpb, [(b * .5 + .5, 'open', 58) for b in range(0, 8, 2)] + [(b * .25, 'tamb', 40 + 14 * (b % 2 == 0)) for b in range(16)])))
        return 124, bars * bpb, parts
    parts.append(dict(channel=0, program=89, volume=70, pan=64, reverb=60, notes=[(b * bpb, bpb, x, 52) for b, s in enumerate(BAY_CHORDS) for x in voicing(s, n('G3'))]))
    parts.append(dict(channel=2, program=34, volume=100, pan=64, reverb=15, notes=bassline(BAY_CHORDS, bpb, [(i * .5, .42, 'o' if i in (3, 7) else 'r') for i in range(8)], vel=90)))
    parts.append(dict(channel=5, program=24, volume=70, pan=40, reverb=35, notes=strum(BAY_CHORDS, bpb, n('G3'), [(i * .5, .3, 1 if i % 2 == 0 else .7, i % 2) for i in range(8)], 46, .012)))
    hits = [(0, 'kick', 92), (1, 'kick', 84), (2, 'kick', 90), (3, 'kick', 84), (1, 'snare', 70), (3, 'snare', 76)] + [(b * .5, 'hat', 48 if b % 2 else 60) for b in range(8)]
    d = drums(bars, bpb, hits); d += [(b * bpb, .1, DRUM['crash'], 70) for b in (0, 8)]
    d += [(b * bpb + 3.5, .1, DRUM['snare'], 60) for b in (3, 7, 11, 15)] + [(b * bpb + 3.75, .1, DRUM['snare'], 66) for b in (7, 15)]
    parts.append(dict(channel=9, program=0, volume=92, reverb=25, notes=d))
    return 124, bars * bpb, parts

# ---------------------------------------------------------------- Hillcrest: warm folk D major, 6/8 (eighth = 1 beat)
HILL_CHORDS = ['D', 'A/C#', 'Bm', 'G', 'D', 'G', 'A', 'A', 'Em', 'A', 'D', 'Bm', 'G', 'A', 'D', 'D']
HILL_MELODY = [
    [(0, 2, 'A4'), (2, 1, 'F#4'), (3, 2, 'A4'), (5, 1, 'D5')],
    [(0, 3, 'E5'), (3, 2, 'C#5'), (5, 1, 'A4')],
    [(0, 2, 'D5'), (2, 1, 'B4'), (3, 2, 'F#4'), (5, 1, 'B4')],
    [(0, 3, 'G4'), (3, 2, 'B4'), (5, 1, 'D5')],
    [(0, 2, 'F#5'), (2, 1, 'E5'), (3, 2, 'D5'), (5, 1, 'F#5')],
    [(0, 2, 'G5'), (2, 1, 'F#5'), (3, 2, 'E5'), (5, 1, 'D5')],
    [(0, 3, 'E5'), (3, 1, 'C#5'), (4, 1, 'D5'), (5, 1, 'E5')],
    [(0, 6, 'A4')],
    [(0, 2, 'B4'), (2, 1, 'E5'), (3, 2, 'G5'), (5, 1, 'F#5')],
    [(0, 2, 'E5'), (2, 1, 'C#5'), (3, 3, 'A4')],
    [(0, 2, 'D5'), (2, 1, 'F#5'), (3, 2, 'A5'), (5, 1, 'F#5')],
    [(0, 3, 'B5'), (3, 2, 'A5'), (5, 1, 'F#5')],
    [(0, 2, 'G5'), (2, 1, 'E5'), (3, 2, 'D5'), (5, 1, 'B4')],
    [(0, 2, 'C#5'), (2, 1, 'E5'), (3, 2, 'A5'), (5, 1, 'G5')],
    [(0, 3, 'F#5'), (3, 2, 'E5'), (5, 1, 'D5')],
    [(0, 6, 'D5')],
]
def arpeggio(chords, low, vel=62):
    out = []
    for b, sym in enumerate(chords):
        root, iv, bass = chord(sym); r = low + (bass - low) % 12; v = voicing(sym, r + 7)
        seq = [r, v[0], v[1], v[2], v[1], v[0]]
        for i, x in enumerate(seq): out.append((b * 6 + i, 1.4, x, vel * (1 if i in (0, 3) else .8)))
    return out

def hillcrest(race=False, lift=False):
    bars = 16; parts = []
    if not race:
        # 6/8 at a dotted-quarter of 66: 198 eighths a minute.
        parts.append(dict(channel=0, program=25, volume=96, pan=44, reverb=50, notes=arpeggio(HILL_CHORDS, n('D3'))))
        parts.append(dict(channel=1, program=21, volume=92, pan=72, reverb=60, chorus=20, notes=melody(bars, 6, HILL_MELODY, 78)))
        parts.append(dict(channel=2, program=32, volume=96, pan=64, reverb=30, notes=bassline(HILL_CHORDS, 6, [(0, 2.8, 'r'), (3, 2.8, 'f')], low=n('D2'))))
        parts.append(dict(channel=3, program=48, volume=58, pan=64, reverb=80, notes=[(b * 6, 6, x, 40) for b, s in enumerate(HILL_CHORDS) if b >= 8 for x in voicing(s, n('F#3'))]))
        parts.append(dict(channel=9, program=40, bank=128, volume=80, reverb=45, notes=drums(bars, 6, [(0, 'kick', 54), (3, 'brush', 52), (0, 'pedal', 40), (3, 'pedal', 36), (5, 'brush', 34)])))
        return 198, bars * 6, parts
    # Race: straight 4/4 at 132 bpm, the same tune (6/8 melody spread over the bar), strummed steel guitar.
    bpb = 4
    def to44(lines): return [[(s * 4 / 6, d * 4 / 6, name) for s, d, name in line] for line in lines]
    if lift:
        parts.append(dict(channel=1, program=21, volume=96, pan=70, reverb=45, chorus=20, notes=melody(bars, bpb, to44(HILL_MELODY), 84)))
        parts.append(dict(channel=4, program=73, volume=60, pan=56, reverb=50, notes=melody(bars, bpb, to44(HILL_MELODY), 64, 12)))
        parts.append(dict(channel=9, program=0, volume=80, reverb=20, notes=drums(bars, bpb, [(b * .5, 'ride', 50 + 12 * (b % 2 == 0)) for b in range(8)])))
        return 132, bars * bpb, parts
    parts.append(dict(channel=0, program=48, volume=66, pan=64, reverb=65, notes=[(b * bpb, bpb, x, 50) for b, s in enumerate(HILL_CHORDS) for x in voicing(s, n('F#3'))]))
    parts.append(dict(channel=2, program=33, volume=86, pan=64, reverb=15, notes=bassline(HILL_CHORDS, bpb, [(i * .5, .42, 'f' if i in (2, 6) else 'r') for i in range(8)], low=n('D2'), vel=86)))
    parts.append(dict(channel=5, program=25, volume=74, pan=38, reverb=35, notes=strum(HILL_CHORDS, bpb, n('D3'), [(i * .5, .3, 1 if i % 2 == 0 else .75, i % 2) for i in range(8)], 50, .012)))
    hits = [(0, 'kick', 84), (1.5, 'kick', 70), (2, 'kick', 80), (1, 'snare', 74), (3, 'snare', 80)] + [(b * .5, 'hat', 50 if b % 2 else 62) for b in range(8)]
    d = drums(bars, bpb, hits); d += [(b * bpb, .1, DRUM['crash'], 72) for b in (0, 8)]
    for b in (7, 15): d += [(b * bpb + 3 + i * .25, .1, 45 + (i % 2) * 5, 64 + i * 4) for i in range(4)]   # tom fill
    parts.append(dict(channel=9, program=0, volume=92, reverb=25, notes=d))
    return 132, bars * bpb, parts

SONGS = {
    'bayview-explore': lambda: bayview(), 'bayview-race': lambda: bayview(True), 'bayview-race-lift': lambda: bayview(True, True),
    'hillcrest-explore': lambda: hillcrest(), 'hillcrest-race': lambda: hillcrest(True), 'hillcrest-race-lift': lambda: hillcrest(True, True),
}
# The race's lift stem is levelled with its base (same gain), so the two layer as mixed.
PAIRS = {'bayview-race-lift': 'bayview-race', 'hillcrest-race-lift': 'hillcrest-race'}
TARGET_RMS = 0.1

def render(name):
    bpm, beats, parts = SONGS[name](); loop = beats * 60 / bpm
    tmp = os.path.join(ROOT, 'review', 'tmp', 'music'); os.makedirs(tmp, exist_ok=True)
    mid = os.path.join(tmp, name + '.mid'); wav = os.path.join(tmp, name + '-raw.wav')
    write_midi(mid, bpm, parts, 3, beats)
    subprocess.run([FLUID, '-ni', '-q', '-g', '0.5', '-r', str(FS), '-F', wav, SF2, mid], check=True, capture_output=True)
    import wave
    w = wave.open(wav); ch = w.getnchannels(); x = np.frombuffer(w.readframes(w.getnframes()), dtype='<i2').astype(np.float64).reshape(-1, ch) / 32768
    a = int(round(loop * FS)); seg = x[a:2 * a].copy()   # the second pass: its start holds the first pass's tails
    # Seam: the first 10 ms blend in from what truly follows the loop's end (the third pass), so the wrap is
    # continuous even where the synth's chorus doesn't repeat exactly.
    k = int(.01 * FS); r = np.linspace(0, 1, k)[:, None]; seg[:k] = x[2 * a:2 * a + k] * (1 - r) + seg[:k] * r
    return seg, loop

def save(path, x):
    pcm = (np.clip(x, -.99, .99) * 32767).astype('<i2').tobytes()
    with open(path, 'wb') as f:
        f.write(b'RIFF' + struct.pack('<I', 36 + len(pcm)) + b'WAVEfmt ' + struct.pack('<IHHIIHH', 16, 1, 2, FS, FS * 4, 4, 16) + b'data' + struct.pack('<I', len(pcm)) + pcm)

def main():
    for path in (FLUID, SF2):
        if not os.path.exists(path): raise SystemExit('missing ' + path)
    os.makedirs(OUT, exist_ok=True); os.makedirs(PREVIEW, exist_ok=True)
    names = sys.argv[1:] or list(SONGS)
    rendered = {}
    for name in names: rendered[name] = render(name)
    gains = {}
    for name, (seg, loop) in rendered.items():
        base = PAIRS.get(name)
        if base and base in rendered: continue
        gains[name] = TARGET_RMS / (math.sqrt(np.mean(seg ** 2)) + 1e-9)
    for name, (seg, loop) in rendered.items():
        g = gains.get(name) or gains.get(PAIRS.get(name, ''), TARGET_RMS / (math.sqrt(np.mean(seg ** 2)) + 1e-9))
        out = seg * g; save(os.path.join(OUT, name + '.wav'), out)
        print(f'MUSIC {name}: {loop:.1f} s loop, rms {math.sqrt(np.mean(out ** 2)):.3f}, peak {np.max(np.abs(out)):.2f}')
    # Previews: explore twice through; race base + lift layered.
    for name, (seg, loop) in rendered.items():
        if name.endswith('-lift'): continue
        mix = seg * (gains.get(name, 1))
        lift = name + '-lift'
        if lift in rendered: mix = np.concatenate([mix, mix + rendered[lift][0] * gains.get(name, 1)])
        else: mix = np.concatenate([mix, mix])
        save(os.path.join(PREVIEW, name + '-preview.wav'), mix * .9)

if __name__ == '__main__': main()
