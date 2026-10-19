"""The new Bayview's plan (docs/79): the island's outline, harbour, roads, the Harbour Loop and the landmarks, as data in
game coordinates (x east, z north, metres; the island at the origin, the mainland to the south), drawn to a plan image
and measured. The same numbers seed the game's layout when the build starts (NewBayview), so the brief and the build
can't drift apart.

    "C:/Program Files/Blender Foundation/Blender 5.2/5.2/python/bin/python.exe" tools/bayview_plan.py [--out review/bayview-plan/plan.png]
"""
import argparse, math, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT.parent / 'car-forge' / 'vendor'))   # (PIL)

# The coast, clockwise from the causeway's foot (the mainland contract: the junction (68.3,-83.4), the Halt quay x 56-109,
# z -163..-86 and the rail loop at (124,-142) stay where the mainland expects them).
COAST = [(56, -86), (40, -74), (12, -66), (-12, -72), (-24, -62), (-46, -50), (-80, -46), (-112, -48), (-132, -60), (-142, -74),
         (-160, -58), (-178, -32), (-188, 0), (-190, 30), (-178, 62), (-150, 90), (-108, 114), (-55, 124), (0, 122), (50, 112),
         (95, 94), (135, 64), (158, 26), (162, -15), (150, -50), (125, -75), (109, -86)]
HARBOUR = [(-12, -72), (-24, -62), (-46, -50), (-80, -46), (-112, -48), (-132, -60), (-142, -74)]   # (the bay's shore, curving north)
ARMS = {'west arm': [(-142, -74), (-148, -104), (-130, -130), (-102, -140)], 'quay arm': [(-12, -72), (-22, -104), (-44, -128), (-70, -138)]}
LOOP = [(-10, -52), (40, -54), (82, -52), (112, -36), (128, -8),
        # (19 Oct: the north-east hairpin as a 30 m arc and a 30 m S-bend out of it; it was 10 m, and the 16 m road's inside
        #  edge folded over itself; cars jammed there in the adventure race. Points evenly spaced on the arcs.)
        (130.4, 34.1), (128.3, 40.9), (122.5, 46.6), (113.8, 50.4), (103.6, 51.8), (93.3, 50.4), (84.6, 46.6), (78.8, 40.9),
        (76.8, 34.1), (73.2, 25.3), (63.4, 18.8), (50.0, 16.5), (36.6, 18.8), (26.8, 25.3),
        (10, 54), (-18, 72), (-50, 94), (-92, 96), (-130, 78), (-158, 50), (-170, 18), (-164, -14),
        (-162.0, -25.4), (-156.7, -34.8), (-149.0, -40.6), (-140.1, -41.8), (-131.5, -38.2),   # (the south-west bend: a 28 m arc)
        (-112, -24), (-90, -16), (-58, -16), (-34, -32)]
GRAVEL = [(10, 54), (20, 84), (60, 96), (98, 84), (124, 60), (103.6, 51.8)]   # (ends at the hairpin's apex)
LANDMARKS = {'lighthouse': (-190, 30), 'windmill': (-26, 52), 'clock tower': (-62, -30), 'helter-skelter pier': (162, -20),
             'lifeboat slip': (-140, -96), 'Pit Stop': (30, -70), 'farm': (75, 76), 'beach huts': (140, -40), 'chapel': (-90, 60),
             'causeway to Hillcrest': (70, -150)}


def _grow(p):
    """The island grown north and west (the mainland fixes only its south-east corner: the causeway's foot and the Halt
    quay keep their places)."""
    x, z = p
    if z < -70 and x > 40: return (x, z)
    return (x * (1.3 if x < 0 else 1.12), z * (1.7 if z > 0 else 1.0))


COAST, LOOP, GRAVEL = [_grow(p) for p in COAST], [_grow(p) for p in LOOP], [_grow(p) for p in GRAVEL]
HARBOUR = [_grow(p) for p in HARBOUR]; ARMS = {k: [_grow(p) for p in v] for k, v in ARMS.items()}
LANDMARKS = {k: _grow(v) for k, v in LANDMARKS.items()}


def length(pts, closed=True):
    P = pts + ([pts[0]] if closed else [])
    return sum(math.dist(a, b) for a, b in zip(P, P[1:]))


def catmull(pts, step=1.2):
    """A closed centripetal Catmull-Rom through the points, sampled about every `step` metres (the game's island loop does
    the same: NewBayview.LoopPoints). Centripetal, not uniform: uneven spacing makes no cusps or kinks."""
    out = []; N = len(pts)
    for i in range(N):
        p0, p1, p2, p3 = pts[i - 1], pts[i], pts[(i + 1) % N], pts[(i + 2) % N]
        t0 = 0; t1 = t0 + max(math.dist(p0, p1), 1e-3) ** .5; t2 = t1 + max(math.dist(p1, p2), 1e-3) ** .5; t3 = t2 + max(math.dist(p2, p3), 1e-3) ** .5
        n = max(4, math.ceil(math.dist(p1, p2) / step))
        for k in range(n):
            t = t1 + (t2 - t1) * k / n
            def L(a, b, ta, tb): return tuple(((tb - t) * a[j] + (t - ta) * b[j]) / (tb - ta) for j in (0, 1))
            A1, A2, A3 = L(p0, p1, t0, t1), L(p1, p2, t1, t2), L(p2, p3, t2, t3)
            out.append(L(L(A1, A2, t0, t2), L(A2, A3, t1, t3), t1, t2))
    return out


def lane(pts):
    """An open lane through the points: the closed curve without its closing span."""
    return catmull(pts)[:-max(4, math.ceil(math.dist(pts[-1], pts[0]) / 1.2))]


def min_radius(path, k=3):
    best = 1e9; N = len(path)
    for i in range(N):
        a, b, c = path[i - k], path[i], path[(i + k) % N]
        ab, bc, ca = math.dist(a, b), math.dist(b, c), math.dist(c, a)
        area = abs((b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0])) / 2
        if area > 1e-6: best = min(best, ab * bc * ca / (4 * area))
    return best


def draw(out):
    from PIL import Image, ImageDraw
    S, ox, oz = 2.2, 250, 200
    W, H = int(470 * S), int(385 * S)
    def P(p): return (int((p[0] + ox) * S), int((oz - p[1]) * S))
    im = Image.new('RGB', (W, H), (74, 132, 168)); d = ImageDraw.Draw(im)
    d.polygon([P(p) for p in COAST], fill=(118, 160, 84), outline=(230, 214, 160))
    d.polygon([P(p) for p in [(56, -86), (109, -86), (109, -163), (56, -163)]], fill=(150, 146, 136))                 # (Halt quay)
    for name, arm in ARMS.items(): d.line([P(p) for p in arm], fill=(200, 196, 184), width=int(5 * S))
    sm = catmull(LOOP)
    d.line([P(p) for p in sm + sm[:1]], fill=(60, 62, 66), width=int(8 * S))
    d.line([P(p) for p in lane(GRAVEL)], fill=(176, 140, 96), width=int(5 * S))
    d.line([P((68.3, -83.4)), P((70, -94)), P((73.4, -160)), P((73.4, -170))], fill=(60, 62, 66), width=int(7 * S))
    d.line([P(sm[0]), P((sm[0][0], sm[0][1] + 1))], fill=(255, 255, 255), width=int(3 * S))
    s0 = sm[0]; d.rectangle([P((s0[0] - 1, s0[1] + 5)), P((s0[0] + 1, s0[1] - 5))], fill=(255, 255, 255))
    for name, p in LANDMARKS.items():
        x, y = P(p); d.ellipse([x - 7, y - 7, x + 7, y + 7], fill=(196, 40, 30), outline=(255, 255, 255)); d.text((x + 9, y - 6), name, fill=(255, 255, 255))
    d.text((12, 10), f'Bayview (new), plan v1. Harbour Loop {length(sm):.0f} m, tightest bend {min_radius(sm):.0f} m radius; gravel lane {length(lane(GRAVEL), False):.0f} m. North up, 100 m = {int(100 * S)} px', fill=(255, 255, 255))
    out.parent.mkdir(parents=True, exist_ok=True); im.save(out); return out


def emit_cs(out):
    """The plan as C# (unity/Assets/Scripts/NewBayviewPlan.cs, generated: edit the plan here, not there)."""
    def arr(pts): return 'new Vector2[]{' + ','.join(f'new({x:.2f}f,{z:.2f}f)' for x, z in pts) + '}'
    lines = ['// Generated by tools/bayview_plan.py from the new Bayview plan (docs/79). Edit the plan there and re-run it; not here.',
             'using UnityEngine;', '', 'namespace LittleRoad', '{', '    public static class NewBayviewPlan', '    {',
             f'        public static readonly Vector2[] Coast={arr(COAST)};',
             f'        public static readonly Vector2[] Harbour={arr(HARBOUR)};',
             f'        public static readonly Vector2[] WestArm={arr(ARMS["west arm"])};',
             f'        public static readonly Vector2[] QuayArm={arr(ARMS["quay arm"])};',
             f'        public static readonly Vector2[] Loop={arr(LOOP)};',
             f'        public static readonly Vector2[] Gravel={arr(GRAVEL)};']
    for name, (x, z) in LANDMARKS.items():
        ident = ''.join(w.capitalize() for w in name.replace('-', ' ').split())
        lines.append(f'        public static readonly Vector2 {ident}=new({x:.2f}f,{z:.2f}f);')
    lines += ['    }', '}', '']
    Path(out).write_text(''.join(l + chr(13) + chr(10) for l in lines), encoding='utf-8', newline=''); return out


if __name__ == '__main__':
    ap = argparse.ArgumentParser(); ap.add_argument('--out', default=str(ROOT / 'review' / 'bayview-plan' / 'plan.png')); a = ap.parse_args()
    sm = catmull(LOOP)
    print(f'loop {length(sm):.0f} m, tightest radius {min_radius(sm):.1f} m; island {max(p[0] for p in COAST) - min(p[0] for p in COAST):.0f} x {max(p[1] for p in COAST) - min(p[1] for p in COAST):.0f} m')
    print('plan:', draw(Path(a.out)))
    print('C#:', emit_cs(ROOT / 'unity' / 'Assets' / 'Scripts' / 'NewBayviewPlan.cs'))
