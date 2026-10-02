# In-game engine captures for the scorecard (docs/55). Usage (project root, Blender's Python):
#   python tools/engine-ingame-capture.py [engine_id ...] [--parallel 3]
# Each game engine, fitted with its class's parts, is driven round the test loop by --sound-drive (the AI steers, a fixed
# script works the pedal: a pull through the gears, a lift at high revs in 3rd, top speed, lifts, a tip-in, a stop and
# idle), in a small minimised window with the speakers silent. The game writes the engine voice alone and its revs,
# gear and throttle every ~10 ms to review/engine-ingame/<engine>/ (player-engine.wav, player.csv, player.log).
import argparse, json, os, subprocess, sys, time
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
EXE = os.path.abspath(os.path.join(ROOT, '..', 'little-road-dev-build', 'Little Road Unity.exe'))

def kits():
    data = json.load(open(os.path.join(ROOT, 'unity', 'Assets', 'Resources', 'Data', 'parts.json'), encoding='utf-8'))
    parts = data['parts']; out = {}
    for e in (p for p in parts if p['slot'] == 'engine'):
        tier = e.get('tier', 'C'); fit = {'engine': e['id']}
        if tier != 'C':   # class C: the stock car with this engine (how it is first heard); above it, the class's first part in each slot
            for slot in ('turbo', 'tyres', 'transmission', 'brakes', 'steering', 'chassis'):
                first = next((p for p in parts if p['slot'] == slot and p.get('tier', 'C') == tier), None)
                if first: fit[slot] = first['id']
        out[e['id']] = ','.join(f'{k}={v}' for k, v in fit.items())
    return out

def launch(engine, kit):
    folder = os.path.join(ROOT, 'review', 'engine-ingame', engine); os.makedirs(folder, exist_ok=True)
    # (--start-windowed: otherwise the player's saved display setting opens each at full screen, 4K here.)
    args = [EXE, '-monitor', '1', '--start-windowed', '-screen-width', '640', '-screen-height', '360', '-screen-fullscreen', '0',
            '--trial-track', 'test-loop', '--trial-setup', kit, '--trial-cars', '1', '--trial-laps', '10', '--trial-duration', '150',
            '--trial-auto', '--sound-drive', '--audio-capture', os.path.join(folder, 'player.wav'), '--audio-capture-seconds', '150',
            '--audio-capture-silent', '-logFile', os.path.join(folder, 'player.log')]
    si = subprocess.STARTUPINFO(); si.dwFlags |= subprocess.STARTF_USESHOWWINDOW; si.wShowWindow = 7   # minimised, not activated
    return subprocess.Popen(args, startupinfo=si)

def main():
    ap = argparse.ArgumentParser(); ap.add_argument('engines', nargs='*'); ap.add_argument('--parallel', type=int, default=2)
    a = ap.parse_args(); k = kits(); todo = a.engines or list(k)
    running = []
    while todo or running:
        while todo and len(running) < a.parallel:
            e = todo.pop(0); running.append((e, launch(e, k[e]), time.time())); time.sleep(4)
        for item in list(running):
            e, p, t0 = item
            if p.poll() is not None or time.time() - t0 > 240:
                if p.poll() is None: p.kill()
                running.remove(item)
                ok = os.path.exists(os.path.join(ROOT, 'review', 'engine-ingame', e, 'player-engine.wav'))
                print(f'{"ok  " if ok else "FAIL"} {e} ({time.time() - t0:.0f} s) kit {k[e]}', flush=True)
        time.sleep(2)

if __name__ == '__main__': main()
