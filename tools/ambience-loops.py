# Ambience loops from field recordings (docs/47 stage 4): the steadiest stretch of each recording (no sudden
# close calls), made seamless with a crossfade, levelled, written as a stereo 48 kHz WAV into
# unity/Assets/Resources/Ambience (Unity compresses it in the build; about 0.5 MB each in the game).
# Sources: Sonniss GDC 2021-2023 bundle (royalty-free for games, no attribution), fetched with fetch-sonniss.py
# into ../little-road-audio-sources. Usage: python tools/ambience-loops.py
import os, struct, math, importlib.util
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
spec = importlib.util.spec_from_file_location('grains', os.path.join(HERE, 'engine-grains.py'))
grains = importlib.util.module_from_spec(spec); spec.loader.exec_module(grains)
FS = 48000
SOURCES = os.path.join(HERE, '..', '..', 'little-road-audio-sources', 'Sonniss-GDC2023')
OUT = os.path.join(HERE, '..', 'unity', 'Assets', 'Resources', 'Ambience')
LOOPS = {
    'forest-birds': ('344 Audio- UK Canals, Parks & Forests/Forest, Daytime, Birds, Outdoors, Nature, Spring.wav', 30),
    'river-birds': ('Federica Vairani - Italian ambience/09_River close shot, nature, birds, no people.wav', 30),
}
TARGET_RMS = 0.06   # about -24 dBFS: a bed, set far quieter still in the game

def read_stereo(path):
    with open(grains.lp(path), 'rb') as f: data = f.read()
    pos = 12; fmt = None; pcm = None
    while pos + 8 <= len(data):
        cid = data[pos:pos + 4]; size = struct.unpack('<I', data[pos + 4:pos + 8])[0]; body = data[pos + 8:pos + 8 + size]
        if cid == b'fmt ': fmt = struct.unpack('<HHIIHH', body[:16])
        elif cid == b'data': pcm = body
        pos += 8 + size + (size & 1)
    tag, channels, rate, _, _, bits = fmt
    if bits == 16: x = np.frombuffer(pcm, dtype='<i2') / 32768.0
    elif bits == 24:
        b = np.frombuffer(pcm[:len(pcm) // 3 * 3], dtype=np.uint8).reshape(-1, 3)
        v = (b[:, 0].astype(np.int32) | (b[:, 1].astype(np.int32) << 8) | (b[:, 2].astype(np.int32) << 16)); x = np.where(v >= 1 << 23, v - (1 << 24), v) / float(1 << 23)
    elif bits == 32 and tag in (3, 0xFFFE): x = np.frombuffer(pcm, dtype='<f4').astype(np.float64)
    else: x = np.frombuffer(pcm, dtype='<i4') / 2147483648.0
    x = x[:len(x) // channels * channels].reshape(-1, channels)
    if channels == 1: x = np.repeat(x, 2, axis=1)
    return np.stack([grains.resample(x[:, 0], rate), grains.resample(x[:, 1], rate)], axis=1)

def steadiest(x, seconds):
    # 0.5 s loudness; the window whose loudest moment stands least above its median (no close calls or bumps).
    hop = FS // 2; n = len(x) // hop; lvl = np.array([math.sqrt(np.mean(x[i * hop:(i + 1) * hop] ** 2)) + 1e-9 for i in range(n)])
    win = int(seconds * 2) + 5; best = 0; score = 1e9
    for s in range(0, n - win):
        w = lvl[s:s + win]; sc = w.max() / np.median(w)
        if sc < score: score, best = sc, s
    return x[best * hop:best * hop + int((seconds + 2.5) * FS)], score

def main():
    os.makedirs(OUT, exist_ok=True)
    for name, (file, seconds) in LOOPS.items():
        x = read_stereo(os.path.join(SOURCES, file)); seg, score = steadiest(x, seconds)
        fade = int(2.5 * FS); body = seg[:-fade].copy(); tail = seg[-fade:]
        ramp = np.sin(np.linspace(0, math.pi / 2, fade))[:, None]
        body[:fade] = body[:fade] * ramp + tail * np.cos(np.linspace(0, math.pi / 2, fade))[:, None]   # seamless loop
        body *= TARGET_RMS / (math.sqrt(np.mean(body ** 2)) + 1e-9); body = np.clip(body, -0.99, 0.99)
        pcm = (body * 32767).astype('<i2').tobytes(); path = os.path.join(OUT, name + '.wav')
        with open(path, 'wb') as f:
            f.write(b'RIFF' + struct.pack('<I', 36 + len(pcm)) + b'WAVEfmt ' + struct.pack('<IHHIIHH', 16, 1, 2, FS, FS * 4, 4, 16) + b'data' + struct.pack('<I', len(pcm)) + pcm)
        print(f'AMBIENCE_LOOP {name}: {len(body) / FS:.1f} s, steadiness {score:.2f} (peak/median loudness), {os.path.getsize(path) / 1e6:.1f} MB source file')

if __name__ == '__main__': main()
