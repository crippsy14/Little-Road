"""Measure car models' proportions with one ruler (docs/75 §10, stage A2): the legacy class C cars are the toy benchmark,
the Forge cars are checked against them.

Run (from the project root):
  blender --background --python tools/forge-measure.py -- <id> [<id> ...] [--json review/forge/measure.json]
Ids are model names in unity/Assets/Resources/Cars (bramble, dover, forge_test_banger, ...). Prints a table and, with
--json, writes every number.

Measures (metres, then ratios):
  L, W (body sides below the glass, mirrors excluded), H; wheel radius front/rear; wheelbase; overhangs; belt (lowest
  glass) and roof; lamp size (one headlamp's larger side).
  L/W, H/W, wheel diameter/H, overhangs/L, wheelbase/L, glasshouse (H - belt)/H, lamp/W.
"""
import bpy, sys, json, math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]; CARS = ROOT / 'unity/Assets/Resources/Cars'
args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
out_json = None
if '--json' in args: i = args.index('--json'); out_json = args[i + 1]; del args[i:i + 2]


def world_verts(o):
    m = o.matrix_world; return [m @ v.co for v in o.data.vertices]


def faces_with(o, key):
    """World positions of vertices on faces whose material name contains key."""
    me = o.data; m = o.matrix_world; idx = [i for i, s in enumerate(o.material_slots) if s.material and key in s.material.name]
    vs = set()
    for p in me.polygons:
        if p.material_index in idx: vs.update(p.vertices)
    return [m @ me.vertices[i].co for i in vs]


def measure(cid):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(CARS / (cid + '.fbx')))
    bpy.context.view_layer.update()
    pivots = {o.name.split('.')[0]: o for o in bpy.data.objects if o.name.startswith('Wheel_')}
    def under(o, root):
        while o.parent:
            if o.parent == root: return True
            o = o.parent
        return False
    wheel_meshes = [o for o in bpy.data.objects if o.type == 'MESH' and any(under(o, p) for p in pivots.values())]
    body = [o for o in bpy.data.objects if o.type == 'MESH' and o not in wheel_meshes]
    pts = [v for o in body for v in world_verts(o)]
    xs = [p.x for p in pts]; ys = [p.y for p in pts]; zs = [p.z for p in pts]
    # The long axis is the car's length; the models face +y or -y: the headlamps say which.
    long_y = (max(ys) - min(ys)) >= (max(xs) - min(xs))
    A = (lambda p: p.y) if long_y else (lambda p: p.x); S = (lambda p: p.x) if long_y else (lambda p: p.y)
    lamps = [v for o in body for v in faces_with(o, 'Headlamp')]
    sign = 1
    if lamps: sign = 1 if sum(A(p) for p in lamps) / len(lamps) > (max(map(A, pts)) + min(map(A, pts))) / 2 else -1
    f = lambda p: A(p) * sign   # forward
    nose = max(map(f, pts)); tail = min(map(f, pts)); L = nose - tail
    glass = [v for o in body for v in faces_with(o, 'Glass')]
    # Heights from the ground: the bottom of the wheels.
    zmin = min(p.z for o in wheel_meshes for p in world_verts(o)) if wheel_meshes else min(zs); H = max(zs) - zmin
    glass_z = [p.z for p in glass]
    belt = (min(glass_z) - zmin) if glass else None; roof = (max(glass_z) - zmin) if glass else None
    cx = (max(map(S, pts)) + min(map(S, pts))) / 2
    lowband = [abs(S(p) - cx) for p in pts if belt is None or p.z - zmin < belt - .02]
    lowband.sort(); W = 2 * lowband[int(len(lowband) * .995) - 1]
    wh = {}
    for name, piv in pivots.items():
        ws = [v for o in wheel_meshes if under(o, piv) for v in world_verts(o)]
        if ws: wh[name] = dict(r=(max(p.z for p in ws) - min(p.z for p in ws)) / 2, a=f(piv.matrix_world.translation))
    fr = [w for n, w in wh.items() if n[-2] == 'F'] or list(wh.values())[:2]; rr = [w for n, w in wh.items() if n[-2] == 'R'] or list(wh.values())[2:]
    fa = sum(w['a'] for w in fr) / len(fr); ra = sum(w['a'] for w in rr) / len(rr)
    rf = sum(w['r'] for w in fr) / len(fr); rrr = sum(w['r'] for w in rr) / len(rr)
    lamp = None
    if lamps:
        one = [p for p in lamps if (S(p) - cx) > 0] or lamps
        lamp = max(max(S(p) for p in one) - min(S(p) for p in one), max(p.z for p in one) - min(p.z for p in one))
    d = dict(id=cid, L=L, W=W, H=H, wheel_rf=rf, wheel_rr=rrr, wheelbase=fa - ra, over_f=nose - fa, over_r=ra - tail, belt=belt, roof=roof, lamp=lamp,
             clearance=(min(p.z for p in pts) - zmin))
    d.update(beyond_f=d['over_f'] - rf, beyond_r=d['over_r'] - rrr, beyond_L=(d['over_f'] - rf + d['over_r'] - rrr) / L, LW=L / W, HW=H / W, wheelD_H=2 * max(rf, rrr) / H, overhangs_L=(d['over_f'] + d['over_r']) / L, wheelbase_L=d['wheelbase'] / L,
             glasshouse=((H - belt) / H) if belt is not None else None, lamp_W=(lamp / W) if lamp else None)
    return d


rows = [measure(c) for c in args]
cols = [('id', '%-18s'), ('L', '%5.2f'), ('W', '%5.2f'), ('H', '%5.2f'), ('wheel_rf', '%5.2f'), ('wheel_rr', '%5.2f'), ('LW', '%5.2f'), ('HW', '%5.2f'),
        ('wheelD_H', '%5.2f'), ('beyond_L', '%5.2f'), ('wheelbase_L', '%5.2f'), ('glasshouse', '%5.2f'), ('lamp_W', '%5.3f')]
print('MEASURE ' + ' '.join(('%-18s' if c == 'id' else '%5s') % c[:5] for c, _ in cols))
for r in rows:
    print('MEASURE ' + ' '.join((fmt % r[c]) if r.get(c) is not None else '    -' for c, fmt in cols))
if out_json:
    Path(out_json).parent.mkdir(parents=True, exist_ok=True); Path(out_json).write_text(json.dumps(rows, indent=1))
