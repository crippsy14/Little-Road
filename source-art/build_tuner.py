"""Class B, the Tuner series (doc 65; Richard, 6 Oct: "classic Japanese 1990s / early 2000s feel, with Skyline and 350Z
kind of feel"). Each body is a signed-distance sculpture like the Regale's (build_prototype.py): a slab body swept along
the car, a greenhouse with a trapezoid section, fender flares or haunches blended in, carved wheel wells and dark pockets
(intakes, lamp pockets), polygonised with surface nets. Real panel gaps are cut as thin dark bands between pairs of
planes; glass is classified by face. Lamps, wing, mirrors, exhausts and the wheels are modelled parts.

Proportions: the real car's dimensions scaled by 0.87 into the class B size band (3.75-4.0 m), then the toy treatment the
game uses everywhere: wheels about 18% bigger, the greenhouse about 6% taller, the track out to the flares.

Run: blender --background --python source-art/build_tuner.py -- <car> [--render DIR] [--no-export] [--quick]
Cars: hayate (R34-spirited GT-4: AWD twin-turbo straight-six coupe), kaze (Z33-spirited: RWD V6 fastback).
Blender +Y is the nose, Z up, ground at z=0, metres.
"""
from pathlib import Path
import math, sys, json
import numpy as np
import bpy, bmesh
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
# The Regale's machinery: its signed-distance helpers, surface nets, materials and the studio review render (everything
# before its build entry point; it also brings in the family builder's modelling helpers).
_proto=(ROOT/'source-art/build_prototype.py').read_text(encoding='utf-8')
exec(_proto[:_proto.index("if __name__=='__main__':")],globals())

args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
CAR=next((a for a in args if not a.startswith('--') and a in ('hayate','kaze')),'hayate')
CID=CAR

# ---------------------------------------------------------------- the cars
# Every length in game metres. Axles (y), wheel radius, wheel centre x, tyre width factor, and the paint.
SPECS={
 # R34-spirited: 4600 x 1785 x 1360 mm, wheelbase 2665, overhangs ~1000 / 935, 245/40 R18 (0.65 m).
 'hayate':dict(name='Hayate GT-4',FA=1.13,RA=-1.19,R=.335,T=.765,W=1.00,front=2.00,rear=-2.00,
               paint=(.035,.20,.62),wheel='twin5',rimfinish='gunmetal',caliper=(.78,.62,.12)),
 # Z33-spirited: 4310 x 1815 x 1315 mm, wheelbase 2650, overhangs ~900 / 760, 225-245/45 R18 (0.66 m).
 'kaze':dict(name='Kaze Z',FA=1.095,RA=-1.215,R=.345,T=.765,W=1.04,front=1.875,rear=-1.875,
             paint=(.92,.36,.04),wheel='split7',rimfinish='bronze',caliper=(.72,.07,.05)),
}
S=SPECS[CAR];FA,RA,RF,RR=S['FA'],S['RA'],S['R'],S['R'];TF=TR=S['T'];WF=WR=S['W']

def tilt_box(p,c,b,r,pitch=0.0,yaw=0.0):
    """A rounded box turned by `yaw` about z then `pitch` about x (radians), centred at c."""
    q=p-np.array(c);x,y,z=q[...,0],q[...,1],q[...,2]
    cy,sy=math.cos(yaw),math.sin(yaw);x,y=cy*x+sy*y,-sy*x+cy*y
    cp,sp=math.cos(pitch),math.sin(pitch);y,z=cp*y+sp*z,-sp*y+cp*z
    return rbox(np.stack([x,y,z],-1),(0,0,0),b,r)
def disc_pocket(p,c,radius,depth,r=.006):
    """A round pocket facing -y (the tail): a short cylinder along y."""
    q=p-np.array(c);d=np.hypot(q[...,0],q[...,2])-radius;return np.maximum(d,np.abs(q[...,1])-depth)-0*r
def greenhouse(p,ys,zt,r1,r2,z0,rr=.06):
    """The cabin: head-on a rounded trapezoid (r1 at its foot on the belt, r2 at the roof), roof height zt along y."""
    x,y,z=p[...,0],p[...,1],p[...,2]
    top=spline(y,ys,zt);a=spline(y,ys,r1);b=spline(y,ys,r2);he=np.maximum(.03,(top-z0)*.5)
    d=trapezoid(x,z-(z0+he),np.maximum(.012,a-rr),np.maximum(.006,b-rr),np.maximum(.012,he-rr))-rr
    return smax(d,np.maximum(y-ys[-1],ys[0]-y),.04)

# ---- Hayate (R34-spirited): upright and square-shouldered, boxed flares, a long bonnet with a centre duct, a notchback
# greenhouse, a tall square tail with four round lamps, and the big wing on two uprights.
HAYATE_POCKETS=[((0,2.00,.30),(.44,.06,.11),.02),       # the big lower intake
                ((.70,1.99,.29),(.11,.06,.08),.015),    # brake ducts at the bumper corners
                ((.58,1.96,.59),(.24,.07,.045),.012),   # headlamp pockets: long, narrow, across the corners
                ((0,1.99,.56),(.15,.05,.035),.012),     # the small upper grille between the lamps
                ((0,1.30,.80),(.11,.20,.012),.008),     # the bonnet's centre duct
                ((0,-2.00,.27),(.52,.05,.07),.015)]     # the diffuser under the tail
def hayate_sdf(P):
    p=P.copy();p[...,0]=np.abs(p[...,0]);x,y,z=p[...,0],p[...,1],p[...,2]
    # The main slab: deck, belt and bonnet heights along the car; flat sides with a slight tumblehome.
    ys=[-2.00,-1.80,-1.25,-.50,.50,1.20,1.75,2.00]
    body=pod(p,ys,[-.9]*8,[.80,.84,.85,.85,.85,.85,.84,.80],[.86,.88,.88,.85,.83,.78,.70,.64],[.17]*8,.055,crown=.02,crown_x=0)
    body=smax(body,(y-2.00)+np.maximum(z-.40,0)*.55,.04)   # (v3) a vertical bumper, the nose raked back above it
    body=smax(body,(-2.00-y)-(z-.30)*.06,.03)             # the tail: nearly vertical
    d=body
    # Boxed flares: square-topped blisters standing proud of the flat sides over each wheel.
    # (v2: they stood above the bonnet; now they're arch lips below its line, standing out from the sides.)
    # (v3: subtle, blended wide: head-on they made shelves at the top corners.)
    for ax,top in ((FA,.60),(RA,.66)):
        fl=pod(p,[ax-.52,ax-.40,ax+.40,ax+.52],[.70]*4,[.85,.895,.895,.85],[top-.10,top,top,top-.10],[.17]*4,.05,crown=.01)
        d=smin(d,fl,.09)
    # Side skirts between the arches.
    d=smin(d,pod(p,[RA+.30,RA+.45,FA-.45,FA-.30],[.5]*4,[.85,.89,.89,.85],[.31]*4,[.16]*4,.02),.02)
    # The greenhouse: screen base at y .52, roof 1.25 from y -.05 to -.70, a steep rear screen down to the deck.
    gh=greenhouse(p,[-1.24,-1.02,-.72,-.40,-.05,.25,.54],[.88,1.06,1.23,1.25,1.24,1.05,.84],
                  [.72,.75,.77,.78,.78,.76,.72],[.50,.53,.55,.56,.55,.52,.46],.80)
    d=smin(d,gh,.035)
    # A raised lip along the front of the bonnet and a power bulge.
    d=smin(d,pod(p,[.70,1.10,1.50],[-.3]*3,[.30,.30,.28],[.865,.835,.775],[.70]*3,.06,crown=.012,crown_x=0),.05)
    # Cuts: wheel wells, pockets, a flat floor.
    d=smax(d,-well(p,FA,RF,RF+.045),.03);d=smax(d,-well(p,RA,RR,RR+.045),.03)
    for cen,hs,r in HAYATE_POCKETS:d=smax(d,-rbox(p,cen,hs,r),.008)
    # Four round tail-lamp pockets (two each side) on the tail panel.
    for cx in (.40,.66):d=smax(d,-disc_pocket(p,(cx,-2.02,.72),.105,.05),.006)
    d=smax(d,.16-z,.015)
    return d

# ---- Kaze (Z33-spirited): low and round, wheels pushed to the corners, a single arc of roof from the screen to a short
# high tail, big rounded haunches, headlamps swept up the front fenders, boomerang tail lamps.
KAZE_POCKETS=[((0,1.86,.32),(.40,.06,.075),.03),         # the wide low mouth
              ((.68,1.80,.25),(.10,.07,.05),.015),       # fog/brake vents
              ((0,-1.86,.30),(.46,.05,.06),.015)]        # the rear valance
def kaze_sdf(P):
    p=P.copy();p[...,0]=np.abs(p[...,0]);x,y,z=p[...,0],p[...,1],p[...,2]
    ys=[-1.875,-1.70,-1.20,-.40,.40,1.10,1.60,1.875]
    body=pod(p,ys,[-.9]*8,[.76,.80,.81,.80,.80,.80,.78,.72],[.82,.84,.82,.78,.78,.73,.64,.56],[.15]*8,.10,crown=.04,crown_x=0)
    body=smax(body,(y-1.875)+(z-.36)*.45,.06)               # a rounded, raked nose
    body=smax(body,(-1.875-y)-(z-.36)*.10,.04)
    d=body
    # Haunches: big rounded shoulders over the wheels, the rear ones fuller.
    d=smin(d,ellipsoid(p,(.66,FA,.50),(.27,.60,.27)),.10)
    d=smin(d,ellipsoid(p,(.66,RA,.55),(.30,.78,.31)),.12)
    # The greenhouse: one arc from the screen base (y .42) over the roof (1.21 at y -.25) down to the tail.
    gh=greenhouse(p,[-1.72,-1.40,-1.00,-.62,-.25,.08,.42],[.85,.96,1.09,1.18,1.21,1.12,.79],
                  [.58,.66,.72,.74,.75,.73,.68],[.26,.38,.47,.51,.52,.48,.40],.76,.08)
    d=smin(d,gh,.06)
    # A small spoiler lip at the top of the hatch.
    d=smin(d,pod(p,[-1.80,-1.74,-1.66],[-.5]*3,[.56,.58,.56],[.885,.89,.875],[.84]*3,.012),.02)
    d=smax(d,-well(p,FA,RF,RF+.045),.035);d=smax(d,-well(p,RA,RR,RR+.045),.035)
    for cen,hs,r in KAZE_POCKETS:d=smax(d,-rbox(p,cen,hs,r),.01)
    # Headlamps: long pockets from the nose corners swept back and up the fenders.
    d=smax(d,-tilt_box(p,(.57,1.60,.615),(.17,.30,.045),.03,pitch=.30,yaw=-.22),.01)
    # Boomerang tail lamps: across the corner, then up and forward along the haunch.
    d=smax(d,-tilt_box(p,(.58,-1.86,.72),(.17,.05,.05),.02),.008)
    d=smax(d,-tilt_box(p,(.72,-1.72,.79),(.05,.15,.045),.02,pitch=-.20),.008)
    d=smax(d,.13-z,.02)
    return d

SDF={'hayate':hayate_sdf,'kaze':kaze_sdf}[CAR]
def kaze_lamps(c):
    q=np.array([[abs(c[0]),c[1],c[2]]])
    if tilt_box(q,(.57,1.60,.615),(.17,.30,.045),.03,pitch=.30,yaw=-.22)[0]<.012:return 4
    if tilt_box(q,(.58,-1.86,.72),(.17,.05,.05),.02)[0]<.010 or tilt_box(q,(.72,-1.72,.79),(.05,.15,.045),.02,pitch=-.20)[0]<.010:return 5
    return 0
LAMPS={'hayate':lambda c:0,'kaze':kaze_lamps}[CAR]
POCKETS={'hayate':HAYATE_POCKETS,'kaze':KAZE_POCKETS}[CAR]

# Glass and seams per car: tests on a face's centre c and normal n; positive inside.
def glass_hayate(c,n):
    if c.z<.86:return -1
    screen=min(c.y-(-.04),.50-c.y,.49-abs(c.x)) if n.y>.30 else -1
    rear=min(-.73-c.y,c.y+1.20,.47-abs(c.x)) if n.y<-.30 else -1
    side=min(c.z-.87,1.17-c.z,c.y+.92,.44-c.y-(c.z-.87)*.9,abs(c.y+.46)-.04) if abs(n.x)>.35 else -1
    return max(screen,rear,side)
def glass_kaze(c,n):
    if c.z<.82:return -1
    screen=min(c.y-(-.12),.38-c.y,.48-abs(c.x)) if n.y>.25 else -1
    rear=min(-.95-c.y,c.y+1.62,.42-abs(c.x)) if n.y<-.20 and c.z>.88 else -1
    # The side light: a curved-top triangle, its rear edge chasing the roof's arc down.
    side=min(c.z-.83,1.10-c.z-(c.y+.25)**2*.55,c.y+1.05,.30-c.y-(c.z-.83)*.95) if abs(n.x)>.35 else -1
    return max(screen,rear,side)
GLASS={'hayate':glass_hayate,'kaze':glass_kaze}[CAR]
# Window edges cut exactly (the glass test's own lines; then the seal 1.8 cm outside), within the greenhouse.
def _wp(lines,lo,hi):return [(co,no,lo,hi) for co,no in lines]
WINDOW_PLANES={'hayate':_wp([((0,-.04,0),(0,1,0)),((0,.50,0),(0,1,0)),((.49,0,0),(1,0,0)),((-.49,0,0),(1,0,0)),((0,-.73,0),(0,1,0)),((0,-1.20,0),(0,1,0)),
                             ((.47,0,0),(1,0,0)),((-.47,0,0),(1,0,0)),((0,0,.87),(0,0,1)),((0,0,1.17),(0,0,1)),((0,-.92,0),(0,1,0)),((0,-.42,0),(0,1,0)),((0,-.50,0),(0,1,0))],
                            (-1,-1.4,.84),(1,.75,1.4)),
               'kaze':_wp([((0,-.12,0),(0,1,0)),((0,.38,0),(0,1,0)),((.48,0,0),(1,0,0)),((-.48,0,0),(1,0,0)),((0,-.95,0),(0,1,0)),((.42,0,0),(1,0,0)),((-.42,0,0),(1,0,0)),
                           ((0,0,.83),(0,0,1)),((0,-1.05,0),(0,1,0))],(-1,-1.8,.80),(1,.6,1.4))}[CAR]
# Panel gaps: (point, normal, region lo, region hi) pairs of planes 5 mm apart; faces between them turn to seam.
SEAMS={'hayate':[((0,.53,0),(0,1,0),(-.9,.48,.14),(.9,.58,.86)),          # door front shut line (and the bonnet's back edge)
                 ((0,-.66,0),(0,1,0),(-.9,-.72,.14),(.9,-.60,.84)),        # door rear shut line
                 ((.33,0,0),(1,0,0),(.28,.55,.60),(.38,1.98,.90)),         # bonnet edges
                 ((-.33,0,0),(1,0,0),(-.38,.55,.60),(-.28,1.98,.90)),
                 ((0,-1.27,0),(0,1,0),(-.70,-1.33,.80),(.70,-1.21,.92))],  # boot lid front edge
       'kaze':[((0,.40,0),(0,1,0),(-.9,.35,.15),(.9,.45,.80)),
               ((0,-.78,0),(0,1,0),(-.9,-.84,.15),(.9,-.72,.80)),
               ((.36,0,0),(1,0,0),(.31,.45,.55),(.41,1.75,.82)),
               ((-.36,0,0),(1,0,0),(-.41,.45,.55),(-.31,1.75,.82))]}[CAR]

def in_pocket(c,margin=.002):
    q=(abs(c[0]),c[1],c[2])
    return any(all(abs(q[i]-cen[i])<=hs[i]+margin for i in range(3)) for cen,hs,_ in POCKETS)

def build_tuner_body(M):
    h=.022 if QUICK else .014
    v,q=surface_nets(SDF,(-1.05,S['rear']-.12,0.0),(1.05,S['front']+.12,1.40),h)
    o=mesh(S['name']+' body',[Vector(p) for p in v],[tuple(int(i) for i in f) for f in q],M['paint'])
    for k in ['trim','canopy','seam','lamp','red']:o.data.materials.append(M[k])
    dec=o.modifiers.new('Even reduction','DECIMATE');dec.ratio=.45 if not QUICK else .6;dec.use_collapse_triangulate=True
    bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=dec.name)
    bm=bmesh.new();bm.from_mesh(o.data)
    def cut(co,no,lo,hi):
        faces=[f for f in bm.faces if (lambda c:all(lo[i]<c[i]<hi[i] for i in range(3)))(f.calc_center_median())]
        edges=list({e for f in faces for e in f.edges});verts=list({v for f in faces for v in f.verts})
        bmesh.ops.bisect_plane(bm,geom=verts+edges+faces,plane_co=Vector(co),plane_no=Vector(no).normalized())
    for co,no,lo,hi in WINDOW_PLANES:cut(Vector(co),Vector(no).normalized(),lo,hi)
    seam_bands=[]
    for co,no,lo,hi in SEAMS:
        nv=Vector(no).normalized()
        for off in (-.0025,.0025):cut(Vector(co)+nv*off,nv,lo,hi)
        seam_bands.append((Vector(co),nv,lo,hi))
    bm.normal_update()
    for f in bm.faces:
        c=f.calc_center_median();n=f.normal;g=GLASS(c,n)
        lamp=LAMPS(c)
        if lamp:f.material_index=lamp
        elif g>0:f.material_index=2
        elif g>-.018:f.material_index=1
        elif c.z<.15:f.material_index=1
        elif in_pocket(c):f.material_index=1
        elif any(abs((c-co).dot(nv))<.0026 and all(lo[i]<c[i]<hi[i] for i in range(3)) for co,nv,lo,hi in seam_bands):f.material_index=3
        f.smooth=True
    bm.to_mesh(o.data);bm.free()
    return o

# ---------------------------------------------------------------- parts
def lamp_disc(name,c,radius,M,ring=True,inner=True):
    """A round tail lamp facing -y: red lens, a chrome bezel, a smaller clear ring inside (the GT-R's signature)."""
    cylinder(name+' lens',c,radius,.03,M['red'],axis='Y',vertices=32,r=.006)
    if ring:
        rv=[];rf=[];n=32
        for k in range(n):
            a=k*math.tau/n
            for rr in (radius,radius+.012):rv.append((c[0]+rr*math.cos(a),c[1]-.016,c[2]+rr*math.sin(a)))
        for k in range(n):rf.append((2*k,2*k+1,2*((k+1)%n)+1,2*((k+1)%n)))
        mesh(name+' bezel',rv,rf,M['chrome'],False)
    if inner:cylinder(name+' inner ring',(c[0],c[1]-.018,c[2]),radius*.45,.004,M['reverse'],axis='Y',vertices=24,r=0)

def pocket_fill(name,cen,hs,mat,inset=.012,sides=(1,-1)):
    """A lamp or grille block filling a pocket, set back `inset` behind the surface."""
    for sx in (sides if cen[0]>0 else (1,)):
        box(name,(sx*cen[0],cen[1]-math.copysign(inset,cen[1]),cen[2]),(hs[0]*2-.01,hs[1]*2,hs[2]*2-.01),mat,.008)

def mirrors(M,y,z,x):
    for sx in (-1,1):
        box('Mirror arm',(sx*(x-.03),y,z-.02),(.07,.05,.03),M['trim'],.01)
        box('Mirror head',(sx*(x+.05),y-.02,z+.02),(.13,.07,.08),M['paint'],.025)
        box('Mirror glass',(sx*(x+.05),y-.06,z+.02),(.10,.01,.06),M['chrome'],.01)

def gt_wing(M,y,z,span,chord,stand):
    """A rear wing on two uprights: a cambered blade with end plates (the Tuner's hallmark)."""
    pts=aerofoil(chord,.035)
    for sx in (-1,1):box('Wing upright',(sx*span*.32,y+.02,z-stand*.5),(.03,.10,stand),M['trim'],.008)
    v=[];f=[];n=len(pts)
    for sx in (-span*.5,span*.5):
        for (u,w) in pts:v.append((sx,y+u,z+w+u*.10))
    for i in range(n):f.append((i,(i+1)%n,n+(i+1)%n,n+i))
    f.append(tuple(range(n))[::-1]);f.append(tuple(range(n,2*n)))
    mesh('Wing blade',v,f,M['paint'],False)
    for sx in (-1,1):box('Wing end plate',(sx*span*.5,y,z+.02),(.012,chord*1.25,.13),M['trim'],.006)

def exhaust(M,x,y,z,r):
    cylinder('Exhaust tip',(x,y,z),r,.12,M['chrome'],axis='Y',vertices=24,r=.004)
    cylinder('Exhaust bore',(x,y-.061,z),r*.75,.01,M['trim'],axis='Y',vertices=24,r=0)

def plate(M,y,z,face):
    box('Number plate',(0,y,z),(.30,.012,.075),M['plate'],.006)

def details(M):
    if CAR=='hayate':
        # Headlamps: projector clusters in the long pockets; amber indicators at the outer ends.
        cen,hs,_=HAYATE_POCKETS[2]
        for sx in (-1,1):
            box('Headlamp housing',(sx*cen[0],cen[1]-.03,cen[2]),(hs[0]*2-.006,.04,hs[2]*2-.006),M['steel'],.006)
            for dx in (-.11,.0):
                cylinder('Projector bowl',(sx*(cen[0]+dx),cen[1]+.0,cen[2]),.036,.012,M['chrome'],axis='Y',vertices=24,r=0)
                cylinder('Projector lens',(sx*(cen[0]+dx),cen[1]+.008,cen[2]),.024,.006,M['lamp'],axis='Y',vertices=20,r=0)
            box('Indicator',(sx*(cen[0]+.17),cen[1]+.005,cen[2]),(.06,.012,.06),M['amber'],.006)
            box('Headlamp lens',(sx*cen[0],cen[1]+.022,cen[2]),(hs[0]*2-.01,.006,hs[2]*2-.01),M['glass'],.004)
        pocket_fill('Intake mesh',HAYATE_POCKETS[0][0],HAYATE_POCKETS[0][1],M['trim'],.04);pocket_fill('Upper grille',HAYATE_POCKETS[3][0],HAYATE_POCKETS[3][1],M['trim'],.03)
        box('Front lip',(0,1.99,.155),(1.40,.12,.025),M['trim'],.008)
        for cx in (.40,.66):
            for sx in (-1,1):lamp_disc('Tail lamp',(sx*cx,-2.005,.72),.095,M)
        gt_wing(M,-1.82,1.00,1.42,.26,.11)
        box('Diffuser fins',(0,-1.99,.25),(.80,.06,.10),M['trim'],.004)
        exhaust(M,.55,-2.01,.27,.055)
        mirrors(M,.42,.95,.78)
        for sx in (-1,1):box('Door handle',(sx*.858,-.42,.79),(.012,.12,.025),M['trim'],.004)
        plate(M,2.015,.40,1);plate(M,-2.012,.52,-1)
        box('Roof antenna base',(0,-.62,1.255),(.04,.08,.02),M['trim'],.006)
    else:
        for sx in (-1,1):
            box('Door handle',(sx*.80,-.30,.80),(.014,.025,.10),M['chrome'],.004)   # vertical, as the Z's
        pocket_fill('Mouth mesh',KAZE_POCKETS[0][0],KAZE_POCKETS[0][1],M['trim'],.04)
        box('Front lip',(0,1.82,.17),(1.20,.10,.022),M['trim'],.008)
        exhaust(M,.30,-1.88,.27,.05);exhaust(M,-.30,-1.88,.27,.05)
        mirrors(M,.32,.92,.74)
        plate(M,1.88,.36,1);plate(M,-1.882,.50,-1)

def tuner_wheel(M,side,cy,r,track,width,name):
    """A tuner wheel: a low-profile tyre with sidewall stripes, a deep-dish rim (twin five-spoke or split seven-spoke),
    a drilled disc and a coloured caliper."""
    s=r/.425;cx=side*track;cz=r;before=set(bpy.context.scene.objects)
    prof=[(-.144,.312),(-.164,.340),(-.166,.380),(-.150,.410),(-.112,.423),(-.075,.425),(.075,.425),(.112,.423),(.150,.410),(.166,.380),(.164,.340),(.144,.312)]
    wv=[];wf=[];n=48
    for u,rr in prof:
        for k in range(n):a=k*math.tau/n;wv.append((cx+u*s*.95*width,cy+rr*s*math.sin(a),cz+rr*s*math.cos(a)))
    for j in range(len(prof)):
        for k in range(n):wf.append((j*n+k,j*n+(k+1)%n,((j+1)%len(prof))*n+(k+1)%n,((j+1)%len(prof))*n+k))
    mesh('Tyre',wv,wf,M['rubber'])
    finish={'gunmetal':material('RimGunmetal',(.13,.14,.15),.85,.30),'bronze':material('RimBronze',(.42,.28,.10),.85,.30)}[S['rimfinish']]
    caliper=material('Caliper '+CAR,S['caliper'],.2,.35)
    outer=cx+side*.148*s*width
    bv=[];bf=[];nb=40;x0b=cx-side*.06;x1b=outer-side*.008
    for xx in (x0b,x1b):
        for k in range(nb):a=k*math.tau/nb;bv.append((xx,cy+.31*s*math.sin(a),cz+.31*s*math.cos(a)))
    for k in range(nb):bf.append((k,(k+1)%nb,nb+(k+1)%nb,nb+k))
    mesh('Rim barrel',bv,bf,finish)
    cylinder('Rim back',(x0b,cy,cz),.31*s,.01,finish,vertices=40,r=0)
    cylinder('Brake disc',(cx+side*.02*s,cy,cz),.25*s,.026,M['disc'],vertices=40,r=0)
    for k in range(16):a=k*math.tau/16;cylinder('Disc drilling',(cx+side*.034*s,cy+math.sin(a)*.19*s,cz+math.cos(a)*.19*s),.007,.006,M['trim'],vertices=8,r=0)
    # The polished lip.
    tv=[];tf=[];seg=48
    for a in range(seg):
        th=a*math.tau/seg
        for b in range(8):ph=b*math.tau/8;rr=.305*s+.011*math.cos(ph);tv.append((outer+.011*math.sin(ph),cy+rr*math.sin(th),cz+rr*math.cos(th)))
    for a in range(seg):
        for b in range(8):tf.append((a*8+b,((a+1)%seg)*8+b,((a+1)%seg)*8+(b+1)%8,a*8+(b+1)%8))
    mesh('Polished lip',tv,tf,M['chrome'])
    hubx=outer-side*.09;tipx=outer-side*.012
    def X(rad):return hubx+(tipx-hubx)*(rad-.08)/(.30-.08)
    def P(a,rad):return Vector((X(rad),cy+math.sin(a)*rad*s,cz+math.cos(a)*rad*s))
    def bar(nm,a_,b_,w,t,mat):
        d_=(b_-a_);perp=d_.cross(Vector((1,0,0))).normalized()*w*.5;th=Vector((t*.5,0,0))
        v=[a_-perp-th,a_+perp-th,b_+perp-th,b_-perp-th,a_-perp+th,a_+perp+th,b_+perp+th,b_-perp+th]
        return mesh(nm,v,[(0,1,2,3),(7,6,5,4),(0,4,5,1),(1,5,6,2),(2,6,7,3),(3,7,4,0)],mat,False)
    if S['wheel']=='twin5':
        for i in range(5):
            a=i*math.tau/5
            for da in (-.11,.11):bar('Twin spoke',P(a+da*.35,.08),P(a+da,.30),.034,.024,finish)
    else:
        for i in range(7):
            a=i*math.tau/7;bar('Spoke',P(a,.08),P(a,.17),.040,.024,finish)
            for da in (-.075,.075):bar('Split spoke',P(a,.17),P(a+da,.30),.022,.020,finish)
    cylinder('Hub',(hubx,cy,cz),.075*s,.05,finish,vertices=24)
    for k in range(5):a=k*math.tau/5;cylinder('Lug nut',(hubx+side*.028,cy+math.sin(a)*.045*s,cz+math.cos(a)*.045*s),.011,.02,M['chrome'],vertices=6,r=0)
    cylinder('Centre cap',(hubx+side*.03,cy,cz),.028*s,.012,M['trim'],vertices=20,r=0)
    objs=list(set(bpy.context.scene.objects)-before)
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:o.select_set(True)
    bpy.context.view_layer.objects.active=objs[0];bpy.ops.object.convert(target='MESH');bpy.ops.object.join();w=bpy.context.object;w.name=name+'_Mesh'
    pivot=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(pivot);pivot.location=(cx,cy,cz)
    bpy.context.view_layer.update();mw=w.matrix_world.copy();w.parent=pivot;w.matrix_world=mw
    box('Brake caliper',(side*(track-.02),cy-.12*s,cz+.16*s),(.07,.12,.10),caliper,.02,rot=(math.radians(-35),0,0))

# Review views scaled to the car (the Regale's angles; the cars here are shorter).
VIEWS_T=[('ref1-front-low',(-4.2,5.6,1.0),(.1,.2,.5),42),('ref2-front-high',(-4.4,5.0,3.8),(0,.1,.45),36),('ref3-head-on',(0,8.0,.95),(0,0,.55),62),
         ('ref4-side',(-9,0,.62),(0,0,.62),4.9),('ref5-rear',(4.2,-5.6,1.7),(0,-.3,.55),40),('ref6-rear-low',(-3.0,-5.6,.75),(0,-.6,.6),40)]

if __name__=='__main__':
    bpy.ops.wm.read_factory_settings(use_empty=True)
    M=proto_materials();M['paint']=material('BodyPaint '+CAR,S['paint'],.35,.22)
    p=M['paint'].node_tree.nodes['Principled BSDF'];p.inputs['Coat Weight'].default_value=.9;p.inputs['Coat Roughness'].default_value=.03
    body=build_tuner_body(M)
    print('TUNER_BODY',CAR,len(body.data.vertices),'verts')
    details(M)
    bpy.ops.object.select_all(action='DESELECT')
    for o in bpy.context.scene.objects:
        if o.type in {'MESH','CURVE'} and o.parent is None:o.select_set(True)
    bpy.context.view_layer.objects.active=body;bpy.ops.object.convert(target='MESH');bpy.ops.object.join();body=bpy.context.object;body.name='Body'
    for side,letter in [(-1,'L'),(1,'R')]:
        tuner_wheel(M,side,FA,RF,TF,WF,'Wheel_F'+letter);tuner_wheel(M,side,RA,RR,TR,WR,'Wheel_R'+letter)
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    pts=np.array([tuple(o.matrix_world@v.co) for o in meshes for v in o.data.vertices])
    info={'car':CAR,'triangles':sum(sum(len(f.vertices)-2 for f in o.data.polygons) for o in meshes),
          'width':round(float(pts[:,0].max()-pts[:,0].min()),3),'length':round(float(pts[:,1].max()-pts[:,1].min()),3),'height':round(float(pts[:,2].max()),3)}
    print('TUNER_STATS',json.dumps(info))
    if RENDER:render(RENDER,VIEWS_T)
