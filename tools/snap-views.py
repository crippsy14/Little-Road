# Turns snap records (the snap tool, docs/73 §4) into --view-shots for tools/capture-map.ps1, so a snap's exact view can be
# captured again before and after a fix. Usage:
#   python tools/snap-views.py [snap folder or .json files...]   (default: today's snaps folder)
# Prints the --view-shots argument: one shot per snap, named after it, from the snap's camera position along its yaw/pitch.
# Example: .\tools\capture-map.ps1 -Name snaps-before -Flags @('--view-shots', (python tools/snap-views.py))
import datetime, glob, json, math, os, sys

def shots(paths):
    out = []
    for p in paths:
        r = json.load(open(p, encoding='utf-8'))
        c = r['cameraPosition']; yaw = math.radians(r['cameraYaw']); pitch = math.radians(r['cameraPitch'])
        f = (math.sin(yaw) * math.cos(pitch), -math.sin(pitch), math.cos(yaw) * math.cos(pitch))
        look = (c['x'] + f[0] * 20, c['y'] + f[1] * 20, c['z'] + f[2] * 20)
        name = r['name'][:60]
        out.append(f"{name}:{c['x']:.2f},{c['y']:.2f},{c['z']:.2f}>{look[0]:.2f},{look[1]:.2f},{look[2]:.2f}")
    return ';'.join(out)

if __name__ == '__main__':
    args = sys.argv[1:]
    if not args:
        root = os.path.join(os.environ['USERPROFILE'], 'AppData', 'LocalLow', 'Little Road Project', 'Little Road Unity Trial', 'snaps')
        args = [os.path.join(root, datetime.date.today().isoformat())]
    files = []
    for a in args:
        files += sorted(glob.glob(os.path.join(a, '*.json'))) if os.path.isdir(a) else [a]
    print(shots(files))
