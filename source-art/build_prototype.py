"""Regale: Duke Rolo's prototype racer (Richard, 1 Oct 2026: "a super detailed prototype model with a really
detailed livery... the owner of the castle and a teaser as to the type of cars in the later game").

Run: blender --background --python source-art/build_prototype.py -- [--render DIR] [--no-export] [--quick]
Writes source-art/family/regale.blend, unity/Assets/Resources/Cars/regale.fbx (+ two LODs) and the livery
texture regale_livery.png beside it. Blender +Y is the nose, Z up, ground at z=0, metres.

The body is one signed-distance sculpture: a low tub, sidepods, a teardrop canopy, a spine to the tail and four
fender pods, smooth-blended (polynomial smooth minimum) and carved by the wheel wells, polygonised with surface
nets on a 1.6 cm grid and projected back onto the exact surface. Windows are bands of the canopy cut by planes. The
livery is a 2048 px texture drawn as flat artwork in a second scene (Workbench, the game's own fonts) in three
views: right side, left side (both read front to back) and top, projected onto the body by face normal.
"""
from pathlib import Path
import math, sys, json
import numpy as np
import bpy, bmesh
from mathutils import Vector, Matrix

ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'source-art'))
# The family builder's modelling helpers (material, box, mesh, cylinder, sphere, curve, sweep, rrect...): run only its
# helper section, not its build loop.
_src=(ROOT/'source-art/build_family_cars.py').read_text(encoding='utf-8')
exec(_src[_src.index('# ---------------------------------------------------------------- helpers'):_src.index('# ---------------------------------------------------------------- car')],globals())
from compact_lods import export_lods
from regale_livery import paint as paint_livery, livery_uvs

args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
RENDER=Path(args[args.index('--render')+1]).resolve() if '--render' in args else None
EXPORT='--no-export' not in args
QUICK='--quick' in args
TARGET=ROOT/'unity/Assets/Resources/Cars'
SOURCE=ROOT/'source-art/family'
FONTS=ROOT/'unity/Assets/Resources/Fonts'
CID='regale'

# Version 2 (Richard, 1 Oct: "a bit chunky and not that kind of le mans aesthetic", with photos of a current
# hydrogen Le Mans prototype concept to work to). Real proportions scaled into the game's 4.8 m: about 5.1 m long,
# 2.0 m wide and 1.06 m tall with 0.72 m wheels on a 3.15 m wheelbase becomes 4.76 m, 2.0 m, 0.99 m, 0.69 m wheels
# and 2.93 m between the axles; a long front overhang and a short tail.
# Wheels: front axle, rear axle, radii, track (wheel centre x), tyre width factor.
# Version 3 (Richard: "the wheels on the real model are further apart... the front looks far too front heavy"):
# measured off the side photo by the wheel (149 px = 0.69 m): wheelbase 3.43 m (70% of the length), overhangs 0.75 m
# front and 0.73 m rear, roof 1.10 m, snorkel 1.19 m, fender tops 0.74 m just over the tyres, tail top 0.79 m.
FA,RA=1.70,-1.73
RF,RR=.345,.345
TF,TR=.82,.82
WF,WR=1.15,1.25

# ---------------------------------------------------------------- signed distances (numpy, any shape (...,3))
def smin(a,b,k):
    h=np.clip(.5+.5*(b-a)/k,0,1);return b*(1-h)+a*h-k*h*(1-h)
def smax(a,b,k):return -smin(-a,-b,k)
def rbox(p,c,b,r):
    q=np.abs(p-np.array(c))-(np.array(b)-r)
    return np.linalg.norm(np.maximum(q,0),axis=-1)+np.minimum(q.max(-1),0)-r
def ellipsoid(p,c,r):
    q=(p-np.array(c))/np.array(r);k0=np.linalg.norm(q,axis=-1);k1=np.linalg.norm(q/np.array(r),axis=-1)
    return k0*(k0-1)/np.maximum(k1,1e-9)
def roundcone(p,a,b,ra,rb):
    a=np.array(a);b=np.array(b);ba=b-a;t=np.clip(((p-a)@ba)/(ba@ba),0,1)
    return np.linalg.norm(p-(a+t[...,None]*ba),axis=-1)-(ra+(rb-ra)*t)
def spline(t,ts,vs):
    """Catmull-Rom through (ts, vs), flat beyond the ends (straight segments put creases in the pods)."""
    ts=np.asarray(ts,float);vs=np.asarray(vs,float);t=np.clip(t,ts[0],ts[-1])
    i=np.clip(np.searchsorted(ts,t)-1,0,len(ts)-2);t0=ts[i];t1=ts[i+1];u=(t-t0)/(t1-t0)
    m=np.gradient(vs,ts);m0=m[i]*(t1-t0);m1=m[i+1]*(t1-t0);v0=vs[i];v1=vs[i+1]
    return (2*u**3-3*u*u+1)*v0+(u**3-2*u*u+u)*m0+(-2*u**3+3*u*u)*v1+(u**3-u*u)*m1
def pod(p,ys,xin,xout,top,bot,r,crown=0.0,crown_x=None):
    """A swept rounded box: its inner and outer sides and its top follow profiles along y (fender pods). `crown`
    lifts the middle of the top (at crown_x, else the centre) so the surface curves between crisp edges."""
    y=p[...,1];xi=spline(y,ys,xin);xo=spline(y,ys,xout);t=spline(y,ys,top);b=spline(y,ys,bot)
    if crown:
        cx=(xi+xo)*.5 if crown_x is None else crown_x;hw=np.maximum(1e-3,(xo-xi)*.5 if crown_x is None else xo-crown_x)
        u=np.clip((p[...,0]-cx)/hw,-1,1);t=t+crown*(1-u*u)
    c=np.stack([(xi+xo)*.5,np.full_like(y,(ys[0]+ys[-1])*.5),(t+b)*.5],-1)
    h=np.stack([(xo-xi)*.5,np.full_like(y,(ys[-1]-ys[0])*.5),(t-b)*.5],-1)
    q=np.abs(p-c)-(h-r);return np.linalg.norm(np.maximum(q,0),axis=-1)+np.minimum(q.max(-1),0)-r
def well(p,cy,cz,r):
    """A wheel well: a cylinder across the car (x from .62 to 1.54 each side), closed inboard by the body."""
    ax=np.abs(p[...,0]);rad=np.hypot(p[...,1]-cy,p[...,2]-cz)-r;return np.maximum(rad,np.abs(ax-1.08)-.46)

def trapezoid(px,pz,r1,r2,he):
    """Distance to an isosceles trapezoid in (x, z), centred: half-width r1 at its foot (z = -he), r2 at its top."""
    px=np.abs(px);k2x=r2-r1;k2z=2*he
    cax=px-np.minimum(px,np.where(pz<0,r1,r2));caz=np.abs(pz)-he
    t=np.clip(((r2-px)*k2x+(he-pz)*k2z)/(k2x*k2x+k2z*k2z),0,1)
    cbx=px-r2+k2x*t;cbz=pz-he+k2z*t
    sg=np.where((cbx<0)&(caz<0),-1.0,1.0)
    return sg*np.sqrt(np.minimum(cax*cax+caz*caz,cbx*cbx+cbz*cbz))

def canopy_sdf(p):
    """Version 5 (Richard: get "the drivers seating area shape nailed"): head-on a rounded trapezoid, narrowing to
    its roof as the reference's does (the ellipsoid was an egg); from the side the same line as before, which
    Richard called perfect. Wide at the foot where it meets the brow, tapering in plan to the screen and the spine."""
    x,y,z=p[...,0],p[...,1],p[...,2]
    ys=[-1.00,-.60,-.20,.35,.80,1.20,1.45,1.66]
    zt=.62+.48*np.sqrt(np.clip(1-((y-.35)/1.25)**2,0,1))
    r1=spline(y,ys,[.18,.30,.37,.39,.38,.33,.25,.10]);r2=spline(y,ys,[.10,.16,.19,.20,.19,.16,.11,.05])
    z0=.56;he=np.maximum(.03,(zt-z0)*.5);rr=.075
    d=trapezoid(x,z-(z0+he),np.maximum(.012,r1-rr),np.maximum(.006,r2-rr),np.maximum(.012,he-rr))-rr
    return smax(d,np.maximum(y-1.66,-1.00-y),.05)

# Dark pockets carved into the body (x >= 0, mirrored): centre, half size, edge radius. Their faces turn carbon.
RECESSES=[((0,2.32,.265),(.50,.32,.135),.015),          # the mouth under the nose panel
          ((.80,2.33,.60),(.17,.07,.032),.012),         # headlamp pocket across each fender's front
          ((.99,2.20,.63),(.05,.14,.028),.010),         # ...wrapping back along its outer shoulder
          ((.95,2.40,.30),(.07,.14,.13),.02),           # the corner intake below the headlamp
          ((.78,FA-.02,.82),(.13,.24,.045),.01)]        # the vent over the front wheel
def in_recess(c,margin=.0015):
    q=(abs(c[0]),c[1],c[2])
    return any(all(abs(q[i]-cen[i])<=hs[i]+margin for i in range(3)) for cen,hs,_ in RECESSES)

def body_sdf(P):
    """Version 4 (Richard: "the top of the car from front to back is flat... the reference car has a lot of
    overlapping body pieces throughout with ups and downs", and "a lot more angular"; photos of the GR010 and the
    Peugeot 9X8 from Wikimedia Commons for the layering and the open rear): separate crisp-edged pieces, blended
    with 2-3 cm fillets instead of 6-9 cm ones, each at its own height.
    Version 5 (Richard: the front "looks like a box with a hole in it"; all the detail stopped at the front arch):
    a domed nose panel over a deep mouth, channels down either side of it, raked fender fronts with a headlamp pocket
    wrapping their top outer corner and an intake below it, a vent over each front wheel; crowned (not flat) tops."""
    p=P.copy();p[...,0]=np.abs(p[...,0])   # symmetric
    x,y,z=p[...,0],p[...,1],p[...,2]
    # The floor plank.
    d=rbox(p,(0,0,.17),(.80,2.30,.07),.02)
    # The lower nose: the floor of the channels either side of the nose panel.
    low=pod(p,[1.10,1.60,2.00,2.38],[-.6]*4,[.55]*4,[.42,.41,.38,.32],[.10]*4,.02)
    # The nose panel: domed across, from the screen's foot down to a sharp lip that overhangs the mouth, narrowing
    # backwards so the channels beside it open up towards the cockpit.
    nose=pod(p,[1.30,1.60,1.90,2.20,2.44],[-.6]*5,[.36,.38,.42,.46,.48],[.70,.64,.57,.50,.44],[.40]*5,.018,crown=.035,crown_x=0)
    # Front fenders: crowned tops with crisp shoulders, upright inner walls, fronts raked back.
    ffend=pod(p,[.95,1.20,1.45,1.70,1.95,2.20,2.44],[.52]*7,[.96,.99,1.0,1.0,1.0,.99,.97],[.69,.77,.78,.78,.77,.72,.65],[.10]*7,.03,crown=.02)
    ffend=smax(ffend,(y-2.44)+(z-.12)*.28,.03)
    d=smin(d,low,.02);d=smin(d,nose,.02);d=smin(d,ffend,.02)
    # The cockpit tub, its door shoulder over the cavity, and a raised brow round the canopy's foot.
    tub=pod(p,[-1.20,-.6,0,.6,1.0,1.30],[-.5]*6,[.80,.82,.84,.86,.82,.60],[.60,.62,.64,.66,.68,.68],[.10]*6,.03)
    brow=pod(p,[-.60,0,.60,1.10,1.40],[-.5]*5,[.42,.46,.46,.42,.34],[.74,.76,.76,.74,.70],[.10]*5,.025)
    d=smin(d,tub,.02);d=smin(d,brow,.02)
    d=smin(d,canopy_sdf(p),.03)
    # Low sidepods rising to the rear wheels.
    pods=pod(p,[-1.40,-1.05,-.5,0,.40],[-.2]*5,[.98,.97,.95,.92,.86],[.56,.48,.42,.38,.35],[.10]*5,.03)
    d=smin(d,pods,.02)
    # The engine cover: a narrow spine over a low rear deck, deep gullies either side of it.
    spine=roundcone(p,(0,-.45,.74),(0,-2.15,.66),.24,.12)
    deck=pod(p,[-2.20,-1.60,-1.00],[-.5]*3,[.40,.44,.46],[.56,.58,.60],[.10]*3,.03)
    d=smin(d,spine,.04);d=smin(d,deck,.02)
    # Rear fenders: crisp blocks over the rear wheels, cut off square just behind them, open underneath at the back.
    rfend=pod(p,[-2.18,-2.00,-1.73,-1.40,-1.10,-.85],[.52]*6,[.97,1.0,1.0,1.0,.97,.90],[.75,.79,.80,.79,.73,.59],[.40,.30,.10,.10,.10,.10],.035,crown=.02)
    d=smin(d,rfend,.02)
    # The gearbox block in the open rear (dark in the livery), under the end of the spine.
    d=smin(d,rbox(p,(0,-2.16,.33),(.28,.14,.19),.02),.02)
    # Cut-outs: the wheel wells; the deep cavity behind each front wheel; the pockets; the flat floor; the fenders'
    # rolled outer shoulders.
    d=smax(d,-well(p,FA,RF,RF+.04),.03);d=smax(d,-well(p,RA,RR,RR+.04),.03)
    d=smax(d,-rbox(p,(1.00,FA-.80,.31),(.38,.42,.23),.03),.02)
    for cen,hs,r in RECESSES:d=smax(d,-rbox(p,cen,hs,r),.008)
    d=smax(d,.095-z,.02)
    d=smax(d,.57*x+.82*z-1.13,.02)
    return d

def surface_nets(sdf,lo,hi,h):
    xs=np.arange(lo[0],hi[0]+h,h);ys=np.arange(lo[1],hi[1]+h,h);zs=np.arange(lo[2],hi[2]+h,h)
    G=np.stack(np.meshgrid(xs,ys,zs,indexing='ij'),-1).astype(np.float64)
    F=sdf(G).astype(np.float32);nx,ny,nz=F.shape;s=F<0
    offs=[(0,0,0),(1,0,0),(0,1,0),(1,1,0),(0,0,1),(1,0,1),(0,1,1),(1,1,1)]
    C=[F[i:nx-1+i,j:ny-1+j,k:nz-1+k] for i,j,k in offs]
    anyin=np.zeros(C[0].shape,bool);allin=np.ones(C[0].shape,bool)
    for c in C:anyin|=c<0;allin&=c<0
    active=anyin&~allin
    acc=np.zeros(C[0].shape+(3,),np.float32);cnt=np.zeros(C[0].shape,np.float32)
    for a,b in [(0,1),(2,3),(4,5),(6,7),(0,2),(1,3),(4,6),(5,7),(0,4),(1,5),(2,6),(3,7)]:
        fa,fb=C[a],C[b];m=(fa<0)!=(fb<0);t=np.where(m,fa/np.where(m,fa-fb,1),0)
        pa=np.array(offs[a],np.float32);pb=np.array(offs[b],np.float32)
        acc+=np.where(m[...,None],pa+t[...,None]*(pb-pa),0);cnt+=m
    idx=-np.ones(active.shape,np.int64);idx[active]=np.arange(active.sum())
    cells=np.argwhere(active);verts=(cells+acc[active]/cnt[active][:,None])*h+np.array(lo)
    quads=[]
    # x edges: cells round the edge counter-clockwise seen from +x; kept when the lower point is inside.
    lo_in=s[:-1,1:-1,1:-1];m=lo_in!=s[1:,1:-1,1:-1];I,J,K=np.nonzero(m);J+=1;K+=1
    q=np.stack([idx[I,J-1,K-1],idx[I,J,K-1],idx[I,J,K],idx[I,J-1,K]],1);f=~lo_in[m];q[f]=q[f][:,::-1];quads.append(q)
    lo_in=s[1:-1,:-1,1:-1];m=lo_in!=s[1:-1,1:,1:-1];I,J,K=np.nonzero(m);I+=1;K+=1
    q=np.stack([idx[I-1,J,K-1],idx[I-1,J,K],idx[I,J,K],idx[I,J,K-1]],1);f=~lo_in[m];q[f]=q[f][:,::-1];quads.append(q)
    lo_in=s[1:-1,1:-1,:-1];m=lo_in!=s[1:-1,1:-1,1:];I,J,K=np.nonzero(m);I+=1;J+=1
    q=np.stack([idx[I-1,J-1,K],idx[I,J-1,K],idx[I,J,K],idx[I-1,J,K]],1);f=~lo_in[m];q[f]=q[f][:,::-1];quads.append(q)
    quads=np.concatenate(quads)
    # Onto the exact surface: two Newton steps along the numerical gradient.
    e=.002
    for _ in range(3):
        d=sdf(verts);g=np.stack([(sdf(verts+np.eye(3)[i]*e)-sdf(verts-np.eye(3)[i]*e))/(2*e) for i in range(3)],-1)
        verts=verts-(d/np.maximum((g*g).sum(-1),1e-6))[:,None]*g
    return verts,quads

def surface_z(x,y,top=2.0):
    """Height of the body's top surface over (x, y) (bisection down a vertical line)."""
    lo,hi=0.0,top
    if body_sdf(np.array([[x,y,lo+.15]]))[0]>0 and body_sdf(np.array([[x,y,.5]]))[0]>0:
        # Nothing at mid-height: search down from the top for the first inside point.
        pass
    zs=np.linspace(top,0,400);d=body_sdf(np.stack([np.full_like(zs,x),np.full_like(zs,y),zs],-1))
    inside=np.nonzero(d<0)[0]
    if len(inside)==0:return None
    i=inside[0];a,b=zs[i-1],zs[i]
    for _ in range(30):
        m=(a+b)*.5
        if body_sdf(np.array([[x,y,m]]))[0]<0:b=m
        else:a=m
    return (a+b)*.5
def surface_y(x,z,front=True):
    ys=np.linspace(2.6,-.5,500) if front else np.linspace(-2.6,.5,500)
    d=body_sdf(np.stack([np.full_like(ys,x),ys,np.full_like(ys,z)],-1));inside=np.nonzero(d<0)[0]
    if len(inside)==0:return None
    i=inside[0];a,b=ys[i-1],ys[i]
    for _ in range(30):
        m=(a+b)*.5
        if body_sdf(np.array([[x,m,z]]))[0]<0:b=m
        else:a=m
    return (a+b)*.5
def surface_x(y,z):
    xs=np.linspace(1.6,0,400);d=body_sdf(np.stack([xs,np.full_like(xs,y),np.full_like(xs,z)],-1));inside=np.nonzero(d<0)[0]
    if len(inside)==0:return None
    i=inside[0];a,b=xs[i-1],xs[i]
    for _ in range(30):
        m=(a+b)*.5
        if body_sdf(np.array([[m,y,z]]))[0]<0:b=m
        else:a=m
    return (a+b)*.5

# ---------------------------------------------------------------- materials
def textured(name,image_path=None,metal=.05,rough=.16):
    m=bpy.data.materials.get(name) or bpy.data.materials.new(name);m.use_nodes=True
    nt=m.node_tree;p=nt.nodes.get('Principled BSDF');p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
    p.inputs['Coat Weight'].default_value=.8;p.inputs['Coat Roughness'].default_value=.04
    if image_path:
        tex=nt.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(image_path),check_existing=True);tex.interpolation='Cubic'
        nt.links.new(tex.outputs['Color'],p.inputs['Base Color'])
    return m
def proto_materials():
    M=materials()
    M['livery']=textured('LiveryMap')
    M['carbon']=material('CarbonFibre',(.030,.033,.036),.25,.35)
    M['gold']=material('GoldTrim',(.83,.60,.20),.9,.25)
    M['caliper']=material('CaliperRed',(.72,.07,.05),.2,.35)
    M['canopy']=material('CanopyGlass',(.008,.010,.014),.1,.04)
    return M

# ---------------------------------------------------------------- body
# Window tests: margin in metres inside the window (>0 inside), from the face centre and normal.
def windscreen(c,n):return min(c.y-.60,1.42-c.y,.26-abs(c.x),c.z-.80) if n.z>.15 else -1
def side_window(c,n):
    # A long side light: its front edge raked with the screen pillar, its rear edge leaning back at the roof's end.
    if abs(n.x)<.35:return -1
    return min(c.z-.80,1.00-c.z,(c.y+.05)-(c.z-.80)*.6,(1.24-c.y)-(c.z-.80)*1.8)
def build_body(M):
    h=.022 if QUICK else .015
    v,q=surface_nets(body_sdf,(-1.2,-2.55,0.0),(1.2,2.55,1.25),h)
    o=mesh('Regale body',[Vector(p) for p in v],[tuple(int(i) for i in f) for f in q],M['livery'])
    for k in ['carbon','canopy']:o.data.materials.append(M[k])
    # Reduce before the window cuts (so their outlines stay exact): surface nets put even quads everywhere,
    # where a third as many triangles hold the curves (the whole car then sits near 100k triangles).
    dec=o.modifiers.new('Even reduction','DECIMATE');dec.ratio=.32 if not QUICK else .5;dec.use_collapse_triangulate=True
    bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=dec.name)
    # Window outlines are cut exactly (planes), so the glass and its seal follow straight lines.
    bm=bmesh.new();bm.from_mesh(o.data)
    def cut(co,no,lo,hi):
        # A plane cut through only the faces inside a box (planes through the whole car left slivers: a crinkle on
        # the front arch lips).
        faces=[f for f in bm.faces if (lambda c:all(lo[i]<c[i]<hi[i] for i in range(3)))(f.calc_center_median())]
        edges=list({e for f in faces for e in f.edges});verts=list({v for f in faces for v in f.verts})
        bmesh.ops.bisect_plane(bm,geom=verts+edges+faces,plane_co=co,plane_no=no)
    def bisect(co,no):cut(co,no,(-.52,-1.2,.62),(.52,1.75,9))
    # The pockets' edges cut exactly too, so their dark interiors have straight edges (by face they came out ragged).
    for cen,hs,_ in RECESSES:
        for sx in ((1,-1) if cen[0]>0 else (1,)):
            c0=Vector((sx*cen[0],cen[1],cen[2]))
            lo=[c0[i]-hs[i]-.06 for i in range(3)];hi=[c0[i]+hs[i]+.06 for i in range(3)]
            for ax in range(3):
                for sg in (-1,1):
                    co=c0.copy();co[ax]+=sg*hs[ax];no=Vector((0,0,0));no[ax]=1;cut(co,no,lo,hi)
    cut(Vector((0,0,.15)),Vector((0,0,1)),(-9,-9,-1),(9,9,.4))   # the carbon floor's edge
    planes=[]
    for e in (0,.02):   # each window edge and its seal's outer edge
        planes+=[((0,.60-e,0),(0,1,0)),((0,1.42+e,0),(0,1,0)),((.26+e,0,0),(1,0,0)),((-.26-e,0,0),(1,0,0)),((0,0,.80-e),(0,0,1)),((0,0,1.00+e),(0,0,1)),
                 ((0,-.05-e,.80),(0,1,-.6)),((0,1.24+e,.80),(0,-1,-1.8))]
    for co,no in planes:
        bisect(Vector(co),Vector(no).normalized())
    bm.normal_update()
    for f in bm.faces:
        c=f.calc_center_median();n=f.normal
        g=max(windscreen(c,n),side_window(c,n))
        if g>0:f.material_index=2          # glass
        elif g>-.02:f.material_index=1     # its carbon seal
        elif c.z<.15:f.material_index=1   # the carbon floor and its skirt
        elif in_recess(c):f.material_index=1   # the dark pockets: mouth, headlamps, intakes, vents
        f.smooth=True
    bm.to_mesh(o.data);bm.free()
    return o

# ---------------------------------------------------------------- parts
def plate(name,outline,z0,z1,mat,r=.008):
    """A flat part from a plan outline (x, y), extruded from z0 to z1."""
    n=len(outline);v=[Vector((x,y,z0)) for x,y in outline]+[Vector((x,y,z1)) for x,y in outline]
    f=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(k,(k+1)%n,n+(k+1)%n,n+k) for k in range(n)]
    o=mesh(name,v,f,mat,False);return bevel(o,r,2) if r else o
def round_outline(x0,x1,y0,y1,r,steps=5):
    pts=[]
    for cx,cy,a0 in [(x1-r,y1-r,0),(x0+r,y1-r,90),(x0+r,y0+r,180),(x1-r,y0+r,270)]:
        for k in range(steps+1):a=math.radians(a0+90*k/steps);pts.append((cx+r*math.cos(a),cy+r*math.sin(a)))
    return pts
def side_plate(name,outline_yz,x,thick,mat,r=.006):
    """A vertical plate (fin, endplate, strake) from a side outline (y, z), centred on x."""
    n=len(outline_yz);v=[Vector((x-thick/2,y,z)) for y,z in outline_yz]+[Vector((x+thick/2,y,z)) for y,z in outline_yz]
    f=[tuple(range(n)),tuple(range(2*n-1,n-1,-1))]+[(k,n+k,n+(k+1)%n,(k+1)%n) for k in range(n)]
    o=mesh(name,v,f,mat,False);return bevel(o,r,2) if r else o
def aerofoil(chord,thick,n=14):
    """A cambered section (a: along y, leading edge at +a; b: up), closed loop."""
    up=[];lo=[]
    for i in range(n+1):
        t=(1-math.cos(math.pi*i/n))/2;y=.5*chord-t*chord
        th=5*thick*chord*(.2969*math.sqrt(t)-.126*t-.3516*t*t+.2843*t**3-.1036*t**4);camber=-.05*chord*math.sin(math.pi*t)
        up.append((y,camber+th));lo.append((y,camber-th))
    return up+lo[::-1][1:-1]
def wing_element(name,x0,x1,y,z,chord,thick,angle,mat):
    prof=[(a*math.cos(angle)-b*math.sin(angle),a*math.sin(angle)+b*math.cos(angle)) for a,b in aerofoil(chord,thick)]
    path=[Vector((x,y,z)) for x in np.linspace(x0,x1,24)]
    return sweep(name,path,[Vector((0,1,0))]*len(path),prof,mat)
def smooth_path(P,steps=8):
    pts=[]
    for i in range(len(P)-1):
        for k in range(steps):pts.append(P[i].lerp(P[i+1],k/steps))
    pts.append(P[-1])
    for _ in range(3):pts=[pts[0]]+[(pts[i-1]+pts[i]*2+pts[i+1])/4 for i in range(1,len(pts)-1)]+[pts[-1]]
    return pts

def lights(M):
    # The nose: a thin LED line along the nose panel's lip.
    path=[]
    for x in np.linspace(-.46,.46,31):
        yy=surface_y(abs(x)+1e-4,.425)
        if yy is not None:path.append(Vector((x,yy+.002,.425)))
    sweep('Nose light line',path,[Vector((0,1,0))]*len(path),[(-.006,-.005),(.006,-.005),(.006,.005),(-.006,.005)],M['lamp'])
    # Headlamps in their pockets: two LED blades across the fender's front, one back along its shoulder, an amber end.
    for side in (-1,1):
        for dz,w in ((.012,.30),(-.014,.24)):box('LED headlamp blade',(side*(.80+(.30-w)*.5),2.27,.60+dz),(w,.02,.012),M['lamp'],.004)
        box('LED shoulder blade',(side*.945,2.20,.635),(.02,.26,.012),M['lamp'],.004)
        box('Amber indicator',(side*.955,2.27,.585),(.035,.02,.016),M['amber'],.004)
    # The open rear (as the reference cars'): a flat carbon light frame across the back and down its sides, the red
    # light along it, linked to the rear fenders' cut-off ends; the gearbox block, rain light and tow strap inside it.
    yf=-2.30;pts=[Vector((-.94,yf,.24)),Vector((-.94,yf,.56))]
    for a_ in np.linspace(math.pi,math.pi/2,6):pts.append(Vector((-.88+.06*math.cos(a_),yf,.56+.06*math.sin(a_))))
    for a_ in np.linspace(math.pi/2,0,6):pts.append(Vector((.88+.06*math.cos(a_),yf,.56+.06*math.sin(a_))))
    pts+=[Vector((.94,yf,.56)),Vector((.94,yf,.24))]
    frame=[];[frame.append(q) for q in pts if not frame or (q-frame[-1]).length>1e-4]
    ups=[]
    for i in range(len(frame)):
        t=(frame[min(len(frame)-1,i+1)]-frame[max(0,i-1)]).normalized();ups.append(Vector((0,-1,0)).cross(t))
    # (A flat blade, 7 cm across and 2 cm deep, as the reference cars' light frames; the round tube read as a bumper.)
    sweep('Rear light frame',frame,[Vector((0,-1,0))]*len(frame),[(-.012,-.035),(.012,-.035),(.012,.035),(-.012,.035)],M['carbon'],ups)
    sweep('Rear light',frame,[Vector((0,-1,0))]*len(frame),[(.011,-.012),(.019,-.012),(.019,.012),(.011,.012)],M['red'],ups)
    for side in (-1,1):
        box('Light frame link',(side*.90,-2.24,.50),(.05,.14,.05),M['carbon'],.01)
        box('Reversing lamp',(side*.55,yf-.02,.62),(.10,.02,.022),M['reverse'],.006)
    y=surface_y(.001,.40,front=False);box('Rain light',(0,y-.01,.40),(.08,.02,.05),M['red'],.008)

def aero(M):
    # Splitter; in the mouth a dark back wall, a centre pillar, two cream blades in a V (as the reference's silver
    # ones) and a vane across; cream fences at the outer corners framing the intakes; two dive planes a side.
    plate('Front splitter',round_outline(-1.00,1.00,1.98,2.50,.06),.07,.092,M['carbon'],.005)
    box('Mouth back wall',(0,2.02,.265),(1.0,.02,.27),M['carbon'],0)
    side_plate('Mouth pillar',[(2.04,.13),(2.46,.13),(2.42,.40),(2.06,.40)],0,.03,M['cream'],.006)
    for side in (-1,1):
        x0,z0,x1,z1=.47,.15,.08,.385;dx=(x1-x0)*side;dz=z1-z0
        box('Mouth V blade',(side*(x0+x1)*.5,2.25,(z0+z1)*.5),(math.hypot(dx,dz),.38,.016),M['cream'],.004,rot=(0,math.atan2(-dz,dx),0))
        side_plate('Corner fence',[(2.24,.09),(2.52,.09),(2.50,.18),(2.43,.38),(2.30,.40)],side*1.005,.012,M['cream'],.004)
        for zz,ang in ((.40,.30),(.52,.24)):box('Dive plane',(side*1.05,2.18,zz),(.10,.18,.010),M['carbon'],.003,rot=(-ang,0,0))
    wing_element('Mouth vane',-.44,.44,2.38,.27,.14,.10,math.radians(-10),M['carbon'])
    # Diffuser: a ramp out of the cave under the tail, six tall strakes.
    ys=np.linspace(-1.95,-2.48,8);zs=np.linspace(.10,.36,8)
    v=[Vector((sx*.84,y,z)) for y,z in zip(ys,zs) for sx in (-1,1)];f=[(2*i,2*i+1,2*i+3,2*i+2) for i in range(7)]
    d=mesh('Diffuser ramp',v,f,M['carbon']);sol=d.modifiers.new('Ramp','SOLIDIFY');sol.thickness=.012
    for x in np.linspace(-.78,.78,6):side_plate('Diffuser strake',[(-1.95,.09),(-2.48,.09),(-2.48,.40),(-2.02,.12)],x,.012,M['carbon'],.003)
    # Rear wing over the tail on swan-neck pylons, tall liveried endplates (0.66 to 1.12 m, as the reference's).
    wing_element('Rear wing main plane',-.91,.91,-2.22,.98,.34,.11,math.radians(-6),M['carbon'])
    wing_element('Rear wing flap',-.91,.91,-2.36,1.05,.16,.10,math.radians(-26),M['carbon'])
    box('Gurney flap',(0,-2.44,1.09),(1.82,.006,.022),M['gold'],0)
    for side in (-1,1):
        side_plate('Wing endplate',[(-2.48,.66),(-2.04,.66),(-1.94,.78),(-1.98,1.11),(-2.48,1.13)],side*.925,.014,M['livery'],.004)
        zs=surface_z(.18,-1.86)
        sm=smooth_path([Vector((side*.18,-1.86,zs-.02)),Vector((side*.18,-1.93,1.10)),Vector((side*.18,-2.08,1.14)),Vector((side*.18,-2.20,1.03))])
        ups=[]
        for i in range(len(sm)):
            a=sm[max(0,i-1)];b=sm[min(len(sm)-1,i+1)];t=(b-a).normalized();ups.append(Vector((1,0,0)).cross(t))
        sweep('Swan-neck wing pylon',sm,[Vector((1,0,0))]*len(sm),[(a*.011,b*.042) for a,b in [(-1,-1),(1,-1),(1,1),(-1,1)]],M['carbon'],ups)
    # Shark fin: it grows out of the roof (no height where it starts), runs back level and meets the wing in a
    # rounded corner (a flat slab before).
    ys=np.linspace(.05,-2.02,22)
    def topz(y):
        roof=surface_z(0,y) or .8;t=np.clip((.05-y)/.55,0,1);t=t*t*(3-2*t)
        return roof+(max(roof,1.075+.015*np.clip(-y/2,0,1))-roof)*t
    top=[(y,topz(y)) for y in ys]
    corner=[(-2.02-.04*math.sin(a),top[-1][1]-.04*(1-math.cos(a))) for a in np.linspace(.3,math.pi/2,4)]
    bottom=[(y,(surface_z(0,y) or .7)-.04) for y in np.linspace(-2.06,.05,16)]
    side_plate('Shark fin',top+corner+bottom,0,.016,M['livery'],.003)

def fittings(M):
    # Carbon lips round the wheel arches' outer edges (as the reference's; they also finish the cut edge cleanly).
    for cy,r in ((FA,RF),(RA,RR)):
        for side in (-1,1):
            path=[]
            for a in np.linspace(-1.25,1.25,40):   # from behind the wheel, over the top, to in front of it
                yy=cy+math.sin(a)*(r+.055);zz=r+math.cos(a)*(r+.055)
                if zz<.14:continue
                xx=surface_x(yy,zz)
                if xx is not None and xx>.80:path.append(Vector((side*(xx+.004),yy,zz)))
            if len(path)<4:continue
            nrm=[Vector((side,0,0))]*len(path);ups=[]
            for i in range(len(path)):
                t=(path[min(len(path)-1,i+1)]-path[max(0,i-1)]).normalized();ups.append(Vector((side,0,0)).cross(t)*side)
            sweep('Arch lip',path,nrm,[(-.012,-.016),(.006,-.016),(.006,.016),(-.012,.016)],M['carbon'],ups)
    # The vents over the front wheels: carbon slats standing in their pockets.
    for side in (-1,1):
        for y in np.linspace(FA-.22,FA+.18,9):box('Fender vent slat',(side*.78,y,.80),(.24,.016,.05),M['carbon'],.004,rot=(math.radians(30),0,0))
    # Mirrors on stalks, inboard on the front fenders beside the screen.
    for side in (-1,1):
        x=side*.70;y=FA-.50;z=surface_z(x,y)
        cylinder('Mirror stalk',(x,y,z+.045),.010,.10,M['carbon'],'Z',10,0)
        box('Mirror pod',(x+side*.02,y,z+.11),(.15,.06,.065),M['livery'],.026)
        box('Mirror glass',(x+side*.02,y-.031,z+.11),(.12,.005,.048),M['chrome'],.010)
    # Roof snorkel (its top at 1.19 m, as the reference's) and camera, antenna.
    z=surface_z(0,.15);box('Roof snorkel',(0,.12,z+.045),(.18,.38,.10),M['livery'],.04)
    box('Roof snorkel inlet',(0,.31,z+.065),(.14,.02,.055),M['carbon'],.008)
    z=surface_z(0,.62);box('Roof camera pod',(0,.62,z+.016),(.07,.08,.04),M['carbon'],.012)
    cylinder('Antenna',(.10,-.45,surface_z(.10,-.45)+.08),.004,.18,M['carbon'],'Z',8,0)
    # Windscreen wiper, quick-fill caps, tow straps.
    zs=surface_z(0,1.36);box('Wiper arm',(-.09,1.36,zs+.010),(.38,.010,.007),M['carbon'],0,rot=(0,0,math.radians(-8)))
    for side in (-1,1):
        z=surface_z(side*.80,RA+.55);cylinder('Quick-fill cap',(side*.80,RA+.55,z+.004),.045,.012,M['chrome'],'Z',24,.003)
        cylinder('Quick-fill ring',(side*.80,RA+.55,z),.058,.009,M['gold'],'Z',24,0)
    bpy.ops.mesh.primitive_torus_add(major_radius=.04,minor_radius=.010,location=(0,2.47,.14),rotation=(0,math.pi/2,0));finish(bpy.context.object,'Front tow loop',M['amber'])
    y=surface_y(.001,.26,front=False)
    if y:
        bpy.ops.mesh.primitive_torus_add(major_radius=.05,minor_radius=.012,location=(0,y-.03,.26),rotation=(math.pi/2,0,0));finish(bpy.context.object,'Rear tow strap',M['amber'])

def wheel(M,side,cy,r,track,width,name):
    """Prototype wheel: low-profile slick with sidewall stripes, dark twelve-spoke rim with a gold lip and centre lock,
    drilled disc; red caliper on the upright."""
    s=r/.425;cx=side*track;cz=r;before=set(bpy.context.scene.objects)
    prof=[(-.144,.320),(-.164,.345),(-.166,.382),(-.150,.410),(-.112,.423),(-.075,.425),(.075,.425),(.112,.423),(.150,.410),(.166,.382),(.164,.345),(.144,.320)]
    wv=[];wf=[];n=48
    for u,rr in prof:
        for k in range(n):a=k*math.tau/n;wv.append((cx+u*s*.95*width,cy+rr*s*math.sin(a),cz+rr*s*math.cos(a)))
    for j in range(len(prof)):
        for k in range(n):wf.append((j*n+k,j*n+(k+1)%n,((j+1)%len(prof))*n+(k+1)%n,((j+1)%len(prof))*n+k))
    mesh('Low-profile slick',wv,wf,M['rubber'])
    outer=cx+side*.150*s*width
    def torus(label,rad,minor,x,mat,seg=48,a0=0,a1=math.tau):
        tv=[];tf=[];closed=a1-a0>=math.tau-1e-6;cnt=seg if closed else seg+1
        for a in range(cnt):
            th=a0+(a1-a0)*a/seg
            for b in range(8):ph=b*math.tau/8;rr=rad+minor*math.cos(ph);tv.append((x+minor*math.sin(ph),cy+rr*math.sin(th),cz+rr*math.cos(th)))
        for a in range(seg if closed else seg):
            for b in range(8):tf.append((a*8+b,((a+1)%cnt)*8+b,((a+1)%cnt)*8+(b+1)%8,a*8+(b+1)%8))
        return mesh(label,tv,tf,mat)
    # Sidewall lettering stripes (unbranded): four arcs on the outer wall.
    for k in range(4):
        a0=k*math.tau/4+.35;torus('Sidewall stripe',.372*s,.006,cx+side*.158*s*width,M['cream'],16,a0,a0+.85)
    # The rim barrel: an open tube (a capped cylinder hid the deep-dish spokes inside it), closed far inboard.
    bv=[];bf=[];nb=40;x0b=cx-side*.06;x1b=outer-side*.008
    for xx in (x0b,x1b):
        for k in range(nb):a=k*math.tau/nb;bv.append((xx,cy+.32*s*math.sin(a),cz+.32*s*math.cos(a)))
    for k in range(nb):bf.append((k,(k+1)%nb,nb+(k+1)%nb,nb+k))
    mesh('Rim barrel',bv,bf,M['trim'])
    cylinder('Rim back',(x0b,cy,cz),.32*s,.01,M['trim'],vertices=40,r=0)
    cylinder('Carbon-ceramic disc',(cx+side*.02*s,cy,cz),.26*s,.03,M['disc'],vertices=40,r=0)
    for k in range(18):
        a=k*math.tau/18;cylinder('Disc drilling',(cx+side*.036*s,cy+math.sin(a)*.20*s,cz+math.cos(a)*.20*s),.008,.006,M['carbon'],vertices=8,r=0)
    torus('Gold rim lip',.312*s,.010,outer,M['gold'])
    torus('Dark rim face',.30*s,.016,outer-side*.006,M['carbon'])
    # A deep dish: ten Y spokes from a recessed hub out to the lip, the disc and caliper showing between them.
    hubx=outer-side*.11;tipx=outer-side*.012
    def X(r):return hubx+(tipx-hubx)*(r-.085)/(.30-.085)
    def P(a,r):return Vector((X(r),cy+math.sin(a)*r*s,cz+math.cos(a)*r*s))
    def bar(name,a_,b_,w,t,mat):
        d_=(b_-a_);perp=d_.cross(Vector((1,0,0))).normalized()*w*.5;th=Vector((t*.5,0,0))
        v=[a_-perp-th,a_+perp-th,b_+perp-th,b_-perp-th,a_-perp+th,a_+perp+th,b_+perp+th,b_-perp+th]
        return mesh(name,v,[(0,1,2,3),(7,6,5,4),(0,4,5,1),(1,5,6,2),(2,6,7,3),(3,7,4,0)],mat,False)
    for i in range(10):
        a=i*math.tau/10
        bar('Spoke',P(a,.085),P(a,.20),.030,.022,M['carbon'])
        for da in (-.12,.12):bar('Spoke arm',P(a,.20),P(a+da,.30),.018,.018,M['carbon'])
    cylinder('Centre-lock hub',(hubx,cy,cz),.09*s,.05,M['carbon'],vertices=24)
    cylinder('Centre-lock nut',(hubx+side*.03,cy,cz),.05*s,.03,M['gold'],vertices=6,r=.004)
    cylinder('Nut retainer',(hubx+side*.047,cy,cz),.02*s,.012,M['caliper'],vertices=16,r=0)
    objs=list(set(bpy.context.scene.objects)-before)
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:o.select_set(True)
    bpy.context.view_layer.objects.active=objs[0];bpy.ops.object.convert(target='MESH');bpy.ops.object.join();w=bpy.context.object;w.name=name+'_Mesh'
    pivot=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(pivot);pivot.location=(cx,cy,cz)
    bpy.context.view_layer.update();mw=w.matrix_world.copy();w.parent=pivot;w.matrix_world=mw
    # The caliper stays on the upright (the body), at the top rear of the disc.
    box('Brake caliper',(side*(track+.0),cy-.13*s,cz+.17*s),(.06,.12,.11),M['caliper'],.02,rot=(math.radians(-35),0,0))

# ---------------------------------------------------------------- review render
def render(out,views):
    """Review renders in a dark studio like the reference photos': a dark, slightly glossy floor, long strip lights
    overhead and to the sides (their reflections show the body's shape and gloss), a warm key and a cool rim."""
    scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=24 if QUICK else 96;scene.cycles.use_denoising=True
    scene.render.resolution_x=1600;scene.render.resolution_y=1000
    if scene.world is None:scene.world=bpy.data.worlds.new('World')
    scene.world.use_nodes=True
    bg=scene.world.node_tree.nodes['Background'];bg.inputs[0].default_value=(.030,.033,.038,1);bg.inputs[1].default_value=1
    floor=material('Studio floor',(.045,.046,.05),0,.42);box('Studio floor',(0,0,-.05),(60,60,.1),floor,0)
    def light(name,loc,power,size,color,shape='DISK',aim=(0,0,.5),size_y=None):
        d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape=shape;d.size=size;d.color=color
        if size_y:d.size_y=size_y
        o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector(aim)-o.location).to_track_quat('-Z','Y').to_euler()
    # Overhead strips along the car, side strips at belt height, a key softbox, a cool rim from behind.
    for x in (-1.6,0,1.6):light('Overhead strip',(x,0,4.2),260,.35,(1,.98,.95),'RECTANGLE',(x,0,0),7.5)
    for x in (-4.5,4.5):light('Side strip',(x,0,1.1),260,.30,(1,.98,.95),'RECTANGLE',(0,0,1.1),7.0)
    light('Key softbox',(-3.5,5.0,3.0),420,3.0,(1,.93,.84))
    light('Rim',(2.5,-5.5,2.4),500,2.5,(.80,.88,1))
    d=bpy.data.cameras.new('Cam');cam=bpy.data.objects.new('Cam',d);scene.collection.objects.link(cam);scene.camera=cam
    scene.view_settings.view_transform='AgX';scene.view_settings.look='AgX - Medium High Contrast'
    out.mkdir(parents=True,exist_ok=True)
    for label,loc,aim,scale in views:
        # A scale under 10 is an orthographic width in metres; above, a perspective lens in millimetres.
        d.type='ORTHO' if scale<10 else 'PERSP'
        if scale<10:d.ortho_scale=scale
        else:d.lens=scale
        cam.location=loc;cam.rotation_euler=(Vector(aim)-cam.location).to_track_quat('-Z','Y').to_euler()
        scene.render.filepath=str(out/f'{CID}-{label}.png');bpy.ops.render.render(write_still=True)
# The reference photos' angles: low front three-quarter from the left, high front three-quarter, head-on, the left
# side, and the rear three-quarter from the right.
VIEWS=[('ref1-front-low',(-4.4,6.4,1.05),(.1,.2,.42),42),('ref2-front-high',(-4.6,5.6,4.2),(0,.1,.35),36),('ref3-head-on',(0,8.5,.95),(0,0,.42),62),
       ('ref4-side',(-9,0,.58),(0,0,.58),5.6),('ref5-rear',(4.4,-6.2,1.9),(0,-.3,.45),40)]
CLOSEUPS=[('front-detail',(1.9,4.6,.62),(0,2.15,.36),35),('cockpit',(-2.5,2.9,1.75),(0,.45,.85),45),('nose',(2.2,4.6,1.3),(0,1.6,.4),2.2),('wheel',(3.6,2.4,.75),(.8,1.37,.36),1.3),('tail',(-2.2,-4.8,1.5),(0,-2.0,.6),2.6),('top',(0,0,8),(0,0,0),5.2)]

if __name__=='__main__':
    bpy.ops.wm.read_factory_settings(use_empty=True)
    M=proto_materials()
    body=build_body(M)
    print('REGALE_BODY',len(body.data.vertices),'verts',len(body.data.polygons),'faces')
    if '--body-only' in args:   # (shape iterations: the sculpture alone, in white)
        render(RENDER,VIEWS);raise SystemExit
    lights(M);aero(M);fittings(M)
    # Join everything stationary into the body, then add the wheels (pivots with their meshes) and calipers.
    bpy.ops.object.select_all(action='DESELECT')
    for o in bpy.context.scene.objects:
        if o.type in {'MESH','CURVE'} and o.parent is None:o.select_set(True)
    bpy.context.view_layer.objects.active=body;bpy.ops.object.convert(target='MESH');bpy.ops.object.join();body=bpy.context.object;body.name='Body'
    for side,letter in [(-1,'L'),(1,'R')]:
        wheel(M,side,FA,RF,TF,WF,'Wheel_F'+letter);wheel(M,side,RA,RR,TR,WR,'Wheel_R'+letter)
    bpy.ops.object.select_all(action='DESELECT')
    for o in bpy.context.scene.objects:
        if o.type=='MESH' and o.parent is None:o.select_set(True)
    bpy.context.view_layer.objects.active=body;bpy.ops.object.join()
    # The livery: painted flat, graded, then projected onto every LiveryMap face.
    art=TARGET/(CID+'_livery.png') if EXPORT else (RENDER or SOURCE)/(CID+'_livery.png')
    art.parent.mkdir(parents=True,exist_ok=True);paint_livery(art,FONTS);textured('LiveryMap',art);livery_uvs(body)
    # Stats: triangles, and the numbers CarBodies keeps (shoulder width: 99.5th percentile of |x| + 2 cm; length + 1 cm).
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    pts=np.array([tuple(o.matrix_world@v.co) for o in meshes for v in o.data.vertices])
    info={'triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes),
          'shoulders':round(2*float(np.percentile(np.abs(pts[:,0]),99.5))+.02,3),'length':round(float(pts[:,1].max()-pts[:,1].min())+.01,3),
          'height':round(float(pts[:,2].max()),3)}
    if EXPORT:
        SOURCE.mkdir(parents=True,exist_ok=True);bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/(CID+'.blend')))
        kw=dict(use_selection=False,object_types={'MESH','EMPTY'},apply_unit_scale=True,bake_space_transform=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,use_mesh_modifiers=True,bake_anim=False)
        bpy.ops.export_scene.fbx(filepath=str(TARGET/(CID+'.fbx')),**kw)
        info['lods']=export_lods(meshes,TARGET,CID)
    print('REGALE_STATS',json.dumps(info))
    if RENDER:render(RENDER,VIEWS+([] if QUICK else CLOSEUPS))
