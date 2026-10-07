"""Little Road family-car field: builds the recipes in family_recipes.py.

Run: blender --background --python source-art/build_family_cars.py -- [ids...] [--render DIR] [--no-export]
Per car: editable source-art/family/<id>.blend, Unity FBX + two LODs in
unity/Assets/Resources/Cars/<id>*.fbx. With --render, Cycles review shots go to DIR.

Techniques follow build_compact_master.py (the player's compact): one continuous
lofted superellipse shell with real boolean wheel arches and lamp apertures, an
open passenger well with interior, pressed window frames around curved glazing,
swept wrap-around bumpers and mouldings, profiled tyres. Blender +Y is the nose.
"""
from pathlib import Path
import json, math, sys
import bpy, bmesh
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'source-art'))
from family_recipes import RECIPES, ORDER
from compact_lods import export_lods

args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
RENDER=Path(args[args.index('--render')+1]).resolve() if '--render' in args else None
EXPORT='--no-export' not in args
LINEUP=Path(args[args.index('--lineup')+1]).resolve() if '--lineup' in args else None
ONLY=[a for a in args if a in RECIPES] or ORDER
TARGET=ROOT/'unity/Assets/Resources/Cars'
SOURCE=ROOT/'source-art/family'

# ---------------------------------------------------------------- helpers

def material(name,color,metal=0,rough=.4):
    m=bpy.data.materials.get(name)
    if m:return m
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,1)
    try:m.use_nodes=True
    except Exception:pass
    p=m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value=(*color,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
    return m

def materials():
    M={}
    M['paint']=material('BodyPaint',(.80,.20,.12),.22,.27)
    p=M['paint'].node_tree.nodes['Principled BSDF'];p.inputs['Coat Weight'].default_value=.35;p.inputs['Coat Roughness'].default_value=.22
    M['glass']=material('MasterGlass',(.055,.075,.09),0,.18);M['glass'].node_tree.nodes['Principled BSDF'].inputs['Alpha'].default_value=.72
    M['rubber']=material('TyreRubber',(.025,.032,.036),0,.82)
    M['trim']=material('CharcoalTrim',(.024,.033,.037),.05,.48)
    M['seam']=material('PanelSeam',(.024,.054,.051),0,.55)
    M['alloy']=material('WarmAlloy',(.57,.61,.61),.75,.24)
    M['chrome']=material('ChromeAlloy',(.80,.82,.83),.9,.12)
    M['steel']=material('PaintedSteel',(.05,.06,.065),.3,.45)
    M['disc']=material('BrakeMetal',(.080,.095,.10),.55,.52)
    M['cloth']=material('InteriorCloth',(.065,.088,.094),0,.9)
    M['canvas']=material('RoofCloth',(.05,.06,.065),0,.9)
    M['lamp']=material('WarmHeadlamp',(.86,.86,.69),.25,.16)
    M['red']=material('TailLight',(.60,.026,.015),.18,.22)
    M['reverse']=material('ReverseLight',(.70,.75,.76),.08,.22)
    M['reflector']=material('RearReflector',(.42,.012,.008),0,.3)
    M['insert']=material('BumperInsert',(.22,.25,.26),.08,.5)
    M['amber']=material('Amber',(.92,.31,.025),.12,.24)
    M['plate']=material('NumberPlate',(.84,.83,.68),0,.45)
    M['cream']=material('CreamTrim',(.93,.90,.80),.05,.35)
    M['wood']=material('WoodTrim',(.45,.27,.13),0,.55)
    M['livery']=material('LiveryRed',(.85,.10,.08),.1,.3)
    return M

def finish(o,name,mat,smooth=True):
    o.name=name;o.data.materials.append(mat)
    if o.type=='MESH':
        for p in o.data.polygons:p.use_smooth=smooth
    return o

def bevel(o,width=.03,segments=3):
    m=o.modifiers.new('Manufactured corner radius','BEVEL');m.width=width;m.segments=segments
    m=o.modifiers.new('Panel normals','WEIGHTED_NORMAL');m.keep_sharp=True;m.weight=40
    return o

def box(name,loc,size,mat,r=.025,rot=None):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.dimensions=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if rot:o.rotation_euler=rot
    finish(o,name,mat);return bevel(o,r,3) if r else o

def mesh(name,v,f,mat,smooth=True):
    data=bpy.data.meshes.new(name);data.from_pydata([tuple(p) for p in v],[],f);data.update()
    bm=bmesh.new();bm.from_mesh(data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(data);bm.free()
    o=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(o);return finish(o,name,mat,smooth)

def cylinder(name,loc,radius,depth,mat,axis='X',vertices=40,r=.008):
    rot=(0,math.pi/2,0) if axis=='X' else (math.pi/2,0,0) if axis=='Y' else (0,0,0)
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=radius,depth=depth,location=loc,rotation=rot)
    o=finish(bpy.context.object,name,mat);return bevel(o,r,2) if r else o

def sphere(name,loc,scale,mat,segments=24,rings=10):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,location=loc)
    o=finish(bpy.context.object,name,mat);o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return o

def curve(name,points,mat,r=.01,cyclic=False,poly=False):
    data=bpy.data.curves.new(name,'CURVE');data.dimensions='3D';data.resolution_u=4;data.bevel_depth=r;data.bevel_resolution=1 if r<.015 else 2
    if cyclic or poly:
        s=data.splines.new('POLY');s.points.add(len(points)-1)
        for p,co in zip(s.points,points):p.co=(*co,1)
    else:
        s=data.splines.new('BEZIER');s.bezier_points.add(len(points)-1)
        for p,co in zip(s.bezier_points,points):p.co=co;p.handle_left_type='AUTO';p.handle_right_type='AUTO'
    s.use_cyclic_u=cyclic;o=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(o);o.data.materials.append(mat);return o

def apply_all(o):
    bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
    for m in list(o.modifiers):bpy.ops.object.modifier_apply(modifier=m.name)

def cut(target,cutter,label='Boolean opening'):
    mod=target.modifiers.new(label,'BOOLEAN');mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cutter
    try:mod.material_mode='TRANSFER'
    except Exception:pass
    bpy.context.view_layer.objects.active=target;bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.data.objects.remove(cutter,do_unlink=True)

def rrect(width,height,radius,steps=3):
    """Rounded rectangle profile (n outward, u up), counter-clockwise."""
    pts=[];r=min(radius,width*.5,height*.5)
    for cx,cy,a0 in [(width-r,height*.5-r,0),(r,height*.5-r,90),(r,-height*.5+r,180),(width-r,-height*.5+r,270)]:
        for k in range(steps+1):
            a=math.radians(a0+90*k/steps);pts.append((cx+r*math.cos(a),cy+r*math.sin(a)))
    return pts

def sweep(name,path,normals,profile,mat,ups=None,caps=True):
    """Extrude a 2D profile (n,u) along path; n follows normals, u follows ups (default +Z)."""
    v=[];f=[];n=len(profile)
    for i,p in enumerate(path):
        N=Vector(normals[i]).normalized();U=Vector(ups[i]).normalized() if ups else Vector((0,0,1))
        for a,b in profile:v.append(Vector(p)+N*a+U*b)
    for i in range(len(path)-1):
        for k in range(n):f.append((i*n+k,i*n+(k+1)%n,(i+1)*n+(k+1)%n,(i+1)*n+k))
    if caps:f+= [tuple(range(n-1,-1,-1)),tuple((len(path)-1)*n+k for k in range(n))]
    return mesh(name,v,f,mat)

def plan_normals(points):
    out=[]
    for i in range(len(points)):
        a=Vector(points[max(0,i-1)]);b=Vector(points[min(len(points)-1,i+1)]);t=(b-a);t.z=0;t.normalize()
        out.append(Vector((t.y,-t.x,0)))
    return out

def rounded_loop(corners,fraction=.10):
    result=[]
    for k,p in enumerate(corners):
        p=Vector(p);a=p.lerp(Vector(corners[(k-1)%len(corners)]),fraction);b=p.lerp(Vector(corners[(k+1)%len(corners)]),fraction)
        for j in range(6):
            t=j/5;result.append((1-t)**2*a+2*(1-t)*t*p+t*t*b)
    return result

def inset_quad(corners,m):
    P=[Vector(c) for c in corners];n=(P[1]-P[0]).cross(P[2]-P[0]).normalized()
    e1=(P[1]-P[0]).normalized();e2=n.cross(e1)
    Q=[Vector(((p-P[0]).dot(e1),(p-P[0]).dot(e2))) for p in P]
    area=sum(Q[i].x*Q[(i+1)%4].y-Q[(i+1)%4].x*Q[i].y for i in range(4));sg=1 if area>0 else -1
    lines=[]
    for i in range(4):
        a,b=Q[i],Q[(i+1)%4];d=(b-a).normalized();lines.append((a+Vector((-d.y,d.x))*sg*m,d))
    out=[]
    for i in range(4):
        (p1,d1),(p2,d2)=lines[i-1],lines[i];den=d1.x*d2.y-d1.y*d2.x;w=p2-p1
        t=(w.x*d2.y-w.y*d2.x)/den;q=p1+d1*t;out.append(P[0]+e1*q.x+e2*q.y)
    return out

def pchip(pts,y):
    """Monotone cubic through (y,z) points: no overshoot, flat at a crest (rooflines, doc 66)."""
    xs=[q[0] for q in pts];zs=[q[1] for q in pts];n=len(pts)
    if y<=xs[0]:return zs[0]
    if y>=xs[-1]:return zs[-1]
    h=[xs[i+1]-xs[i] for i in range(n-1)];d=[(zs[i+1]-zs[i])/h[i] for i in range(n-1)];m=[d[0]]+[0]*(n-2)+[d[-1]]
    for i in range(1,n-1):
        if d[i-1]*d[i]>0:w1=2*h[i]+h[i-1];w2=h[i]+2*h[i-1];m[i]=(w1+w2)/(w1/d[i-1]+w2/d[i])
    i=max(k for k in range(n-1) if xs[k]<=y);t=(y-xs[i])/h[i]
    return (2*t**3-3*t*t+1)*zs[i]+(t**3-2*t*t+t)*h[i]*m[i]+(-2*t**3+3*t*t)*zs[i+1]+(t**3-t*t)*h[i]*m[i+1]

# ---------------------------------------------------------------- car

class Car:
    def __init__(self,cid,R):
        self.id=cid;self.R=R;self.P=R['shell'];self.px,self.pz=R['shape'];self.M=materials()
        self.fa,self.ra=R['axles'];W=R['wheel'];self.wr=W['r'];self.track=W['track']
        G=dict(R['glass']);self.G=G
        # Frame base sits just below the shell's shoulder at the greenhouse width.
        ys=[G['yr0']+(G['yf0']-G['yr0'])*i/20 for i in range(21)]
        G['belt']=min(self.surface_z(G['wb'],y) for y in ys)-.012
        self.roofmat=self.M['cream'] if G.get('two_tone') else self.M['paint']
        self.C=R.get('cabin');self.rings={}
        if self.C:
            # The roofline's ends never sit above the bonnet or deck they meet: they drop to just under it, so the screen
            # sweeps down into it (the Raijin's ended 11 cm above its bonnet, and a black cowl bridged the drop).
            C=self.C=dict(self.C);rl=[list(q) for q in C['roofline']]
            for k,yy in ((-1,G['yf0']),(0,G['yr0'])):
                lo=self.surface_z(0,yy)-.03
                if rl[k][1]>lo:rl[k][1]=lo
            C['roofline']=[tuple(q) for q in rl]
            G['roof']=max(z for _,z in C['roofline'])
            G['yf1']=C.get('screen',(G['yf1'],))[0]
            if C.get('rear_screen'):G['yr1']=C['rear_screen'][1]
        self.grooves=[]

    # Shell geometry --------------------------------------------------
    def shape_at(self,y):
        """The section's superellipse exponents at y (doc 66): per-station values where a station gives them, else the
        recipe's global shape."""
        P=self.P
        if len(P[0])<6:return self.px,self.pz
        for i in range(len(P)-1):
            if y<=P[i+1][0]:break
        t=max(0,min(1,(y-P[i][0])/max(1e-6,P[i+1][0]-P[i][0])));t=t*t*(3-2*t)
        return P[i][4]+(P[i+1][4]-P[i][4])*t,P[i][5]+(P[i+1][5]-P[i][5])*t
    # ---- 2. Fender pods (doc 66): the top surface rises over each axle towards the sides, so the wings can stand above
    # the bonnet (Z, RX-7) or box out (GT-R). fenders=dict(front=(rise,reach,length), rear=(...)): rise in metres, reach
    # = how far in from the side the pod starts, length = half-length along the car.
    def pod(self,x,y):
        F=self.R.get('fenders')
        if not F:return 0.0
        best=0.0;w=self.section(y)[0]
        for key,cy in (('front',self.fa),('rear',self.ra)):
            if key not in F:continue
            rise,reach,length=F[key];dy=abs(y-cy)
            if dy>=length:continue
            fy=.5+.5*math.cos(math.pi*dy/length)
            u=(abs(x)-(w-reach))/max(.05,reach*.55);fx=max(0,min(1,u));fx=fx*fx*(3-2*fx)
            best=max(best,rise*fx*fy)
        return best
    def section(self,y):
        P=self.P
        for i in range(len(P)-1):
            if y<=P[i+1][0]:break
        a=P[max(0,i-1)];b=P[i];c=P[i+1];d=P[min(len(P)-1,i+2)]
        t=max(0,min(1,(y-b[0])/(c[0]-b[0])));span=c[0]-b[0]
        def slope(p,q):return (q-p)
        return tuple((2*t**3-3*t*t+1)*b[k]+(t**3-2*t*t+t)*span*(c[k]-a[k])/max(1e-6,c[0]-a[0])+
                     (-2*t**3+3*t*t)*c[k]+(t**3-t*t)*span*(d[k]-b[k])/max(1e-6,d[0]-b[0]) for k in [1,2,3])
    def surface_x(self,y,z):
        w,lo,hi=self.section(y);px,pz=self.shape_at(y);u=min(.9999,abs((z-(lo+hi)*.5)/((hi-lo)*.5)))
        return w*max(1e-5,1-u**(2/pz))**(px/2)
    def surface_z(self,x,y):
        w,lo,hi=self.section(y);px,pz=self.shape_at(y);u=min(.9999,abs(x)/w)
        s_=max(0,1-u**(2/px))**(pz/2)
        return (lo+hi)*.5+(hi-lo)*.5*s_+self.pod(x,y)*s_
    def inside(self,x,y,z):
        if y<self.P[0][0] or y>self.P[-1][0]:return False
        w,lo,hi=self.section(y);px,pz=self.shape_at(y);mid=(lo+hi)*.5;half=(hi-lo)*.5
        if z>mid:half+=self.pod(x,y)
        return (abs(x)/w)**(2/px)+(abs(z-mid)/half)**(2/pz)<=1
    def end_y(self,x,z,front=True):
        """Outermost y of the shell at (x,z) at the nose (front) or tail."""
        edge=self.P[-1][0] if front else self.P[0][0];deep=edge-(1.2 if front else -1.2)
        if self.inside(x,edge,z):return edge
        if not self.inside(x,deep,z):return None
        lo,hi=deep,edge
        for _ in range(30):
            m=(lo+hi)*.5
            if self.inside(x,m,z):lo=m
            else:hi=m
        return lo
    def yaw(self,x,z,front=True):
        """Z rotation that points an object's +Y (or -Y at the tail) along the shell's plan normal."""
        e=.02;a=self.end_y(x-e,z,front);b=self.end_y(x+e,z,front)
        if a is None or b is None:return 0
        slope=(b-a)/(2*e);nx,ny=(-slope,1) if front else (slope,-1);l=math.hypot(nx,ny);nx/=l;ny/=l
        return math.atan2(-nx,ny) if front else math.atan2(nx,-ny)
    def place(self,o,pivot,yaw):
        """Rotate an object about a vertical axis through pivot."""
        from mathutils import Matrix
        o.matrix_world=Matrix.Translation(pivot)@Matrix.Rotation(yaw,4,'Z')@Matrix.Translation(-Vector(pivot))@o.matrix_world
        return o
    def outline(self,z,front=True,offset=0,span=100,n=41):
        """Plan outline around the nose/tail at height z, offset outward."""
        cy=self.P[-1][0]-.9 if front else self.P[0][0]+.9;pts=[]
        for i in range(n):
            a=math.radians(-span+2*span*i/(n-1));d=Vector((math.sin(a),math.cos(a) if front else -math.cos(a),0))
            lo,hi=0,3
            for _ in range(28):
                m=(lo+hi)*.5;p=Vector((0,cy,z))+d*m
                if self.inside(p.x,p.y,z):lo=m
                else:hi=m
            pts.append(Vector((0,cy,z))+d*lo)
        if not front:pts.reverse()
        # Stop wrap-around parts short of the wheel openings.
        limit=self.fa+self.wr+.1 if front else self.ra-self.wr-.1
        kept=[p for p in pts if (p.y>limit if front else p.y<limit)]
        pts=kept if len(kept)>=9 else pts[len(pts)//2-4:len(pts)//2+5]
        normals=plan_normals(pts)
        if normals[len(pts)//2].y*(1 if front else -1)<0:normals=[-v for v in normals]
        return [p+nm*offset for p,nm in zip(pts,normals)],normals

    ARCH_GAP,FENDER=.095,.13   # arch opening and fender radius beyond the tyre (3.5 cm apart: near-coincident surfaces break the exact boolean)
    def fender(self,p,clear=FENDER,soft=.03):
        """Wings bulge over the wheels: the outer shell above each axle is pushed out (a soft maximum,
        so the surface stays smooth) to at least wheel radius + clear from the wheel centre. Without it
        the arch cut sliced flat shelves through low bonnets and wings."""
        x,y,z=p;wx=max(0,min(1,(abs(x)-(self.track-.44))/.24));wx=wx*wx*(3-2*wx)
        if wx<=0:return p
        need=self.wr+clear
        for cy in (self.fa,self.ra):
            dy=y-cy;dz=z-self.wr;d=math.hypot(dy,dz)
            if d<1e-4 or d>need+.3:continue
            up=max(0,min(1,(dz+.02)/.14));up=up*up*(3-2*up)
            if up<=0:continue
            target=need+soft*math.log1p(math.exp((d-need)/soft))       # smooth max(d, need)
            nd=d+(target-d)*wx*up;y=cy+dy/d*nd;z=self.wr+dz/d*nd
        return (x,y,z)

    def shell(self):
        P=self.P;M=self.M;v=[];f=[];ny=150;nr=88   # dense enough that boolean arch/lamp edges stay smooth
        for j in range(ny):
            y=P[0][0]+(P[-1][0]-P[0][0])*j/(ny-1);w,lo,hi=self.section(y);px,pz=self.shape_at(y)
            for k in range(nr):
                a=k*math.tau/nr;c=math.cos(a);s=math.sin(a)
                x=w*math.copysign(abs(c)**px,c);sz=math.copysign(abs(s)**pz,s)
                v.append(self.fender((x,y,(lo+hi)*.5+(hi-lo)*.5*sz+(self.pod(x,y)*sz if sz>0 else 0))))
        for j in range(ny-1):
            for k in range(nr):f.append((j*nr+k,j*nr+(k+1)%nr,(j+1)*nr+(k+1)%nr,(j+1)*nr+k))
        f.extend([tuple(range(nr-1,-1,-1)),tuple((ny-1)*nr+k for k in range(nr))])
        body=mesh('Continuous formed body shell',v,f,M['paint'])
        body.data.materials.append(M['trim'])
        # Keep uncut normals so arch/lamp openings don't dent the highlights.
        ref=body.copy();ref.data=body.data.copy();bpy.context.collection.objects.link(ref)
        for side in [-1,1]:
            for y in [self.fa,self.ra]:
                bpy.ops.mesh.primitive_cylinder_add(vertices=96,radius=self.wr+self.ARCH_GAP,depth=.70,location=(side*(self.track+.16),y,self.wr),rotation=(0,math.pi/2,0))
                c=bpy.context.object;c.data.materials.append(M['trim']);cut(body,c,'Actual wheel arch opening')
        self.lamp_apertures(body)
        t=body.modifiers.new('Continuous panel reflections','DATA_TRANSFER');t.object=ref;t.use_loop_data=True
        t.data_types_loops={'CUSTOM_NORMAL'};t.loop_mapping='POLYINTERP_NEAREST'
        bpy.context.view_layer.objects.active=body;bpy.ops.object.modifier_apply(modifier=t.name)
        bpy.data.objects.remove(ref,do_unlink=True)
        # Open passenger well down to the floor inside the greenhouse.
        G=self.G
        y0=G['yr0']+.05;y1=G['yf0']-.03
        # A lofted cabin opens the deck only from just ahead of its rear-glass base (doc 66): further back the cabin
        # skin is still under the deck, and the cut's dark walls showed through the glass.
        if self.C:
            # Open the deck only where the cabin stands clear of it (doc 66): ahead of that the bonnet (and behind,
            # the deck) runs on into the glass. Cut further and a dark band of cut walls showed under the screen.
            y0=max(y0,self.cab_emerge(False)+.03);y1=min(y1,self.cab_emerge(True)-.03)
        if self.R['rear']=='boot':y0=max(y0,G['yr0']+.05)
        if self.C:
            # Under a lofted cabin the well follows the cabin's plan, 9 cm inside its belt (a straight box left
            # dark cut walls standing beside a cabin narrower than the old greenhouse).
            n=24;ys=[y0+(y1-y0)*k/n for k in range(n+1)];loop=[(self.cab_widths(y)[0]-.09,y) for y in ys]+[(-(self.cab_widths(y)[0]-.09),y) for y in reversed(ys)]
            m=len(loop);v=[(x,y,.54) for x,y in loop]+[(x,y,1.70) for x,y in loop]
            f=[tuple(range(m-1,-1,-1)),tuple(range(m,2*m))]+[(k,(k+1)%m,m+(k+1)%m,m+k) for k in range(m)]
            c=mesh('Passenger well cutter',v,f,M['trim'],False)
        else:c=box('Passenger well cutter',(0,(y0+y1)*.5,1.12),(2*(G['wb']-.09),y1-y0,1.16),M['trim'],.045)
        apply_all(c)
        cut(body,c,'Open passenger cabin')
        # Pickups: an open load bed behind the cab, down to a floor 0.3 m above the sill line.
        if 'bed' in self.R['extras']:
            yb0=self.P[0][0]+.13;yb1=G['yr0']-.12;w,lo,hi=self.section((yb0+yb1)*.5);fl=lo+.30
            c=box('Load bed cutter',(0,(yb0+yb1)*.5,fl+.8),(2*(w-.08),yb1-yb0,1.6),M['trim'],.03);apply_all(c)
            cut(body,c,'Open load bed')
            box('Load bed floor',(0,(yb0+yb1)*.5,fl+.01),(2*(w-.1),yb1-yb0-.02,.03),M['trim'],.01)
            for k in range(5):box('Bed floor rib',(-.5+k*.25,(yb0+yb1)*.5,fl+.03),(.04,yb1-yb0-.06,.02),M['insert'],.005)
        self.body=body

    def lamp_span(self,tree,side,x):
        """How far the real (fendered) shell sits behind/ahead of the lamp's plane round its rim, along
        the lamp axis: (back, front). Curved noses vary by several cm; the barrel and cut follow it."""
        L=self.R['lamps'];z=L['z'];y=self.end_y(x,z);yw=self.yaw(side*x,z);r=L['r']+.03
        d=Vector((-math.sin(yw),math.cos(yw),0));u=Vector((math.cos(yw),math.sin(yw),0));c=Vector((side*x,y,z));ts=[]
        for k in range(24):
            a=k*math.tau/24;p=c+u*(math.cos(a)*r)+Vector((0,0,math.sin(a)*r))
            hit,_,_,_=tree.ray_cast(p+d*.5,-d,1.0)
            if hit is not None:ts.append((hit-c).dot(d))
        return (min(ts),max(ts)) if ts else (-.02,.02)
    def lamp_apertures(self,body):
        L=self.R['lamps'];M=self.M;self.spans={}
        if L['style'] in ('round','quad'):
            from mathutils.bvhtree import BVHTree
            tree=BVHTree.FromObject(body,bpy.context.evaluated_depsgraph_get())
            for side in [-1,1]:
                for x in [L['x']]+([L['x2']] if L['style']=='quad' else []):self.spans[(side,x)]=self.lamp_span(tree,side,x)
        for side in [-1,1]:
            # Round lamps are not cut in: on curved noses the aperture's walls showed as shards and black
            # wedges. They sit in a cowl on the panel instead (headlamps()).
            if L['style']=='rect':
                y=self.end_y(L['x'],L['z']);c=box('Lamp cutter',(side*L['x'],y-.05,L['z']),(L['w']+.03,.18,L['h']+.03),M['trim'],.03);apply_all(c)
                self.place(c,(side*L['x'],y,L['z']),self.yaw(side*L['x'],L['z']));apply_all(c)
                cut(body,c,'Recessed headlamp aperture')

    # Greenhouse -------------------------------------------------------
    def glass_patch(self,name,corners,axis,bulge=.02,mat=None):
        M=self.M;boundary=rounded_loop(corners);center=sum(boundary,Vector())/len(boundary);n=len(boundary)
        verts=[center+Vector(axis)*bulge];faces=[]
        for ring in range(1,5):
            rad=ring/4;verts.extend([center.lerp(p,rad)+Vector(axis)*bulge*(1-rad*rad) for p in boundary])
        for k in range(n):faces.append((0,1+k,1+(k+1)%n))
        for ring in range(3):
            for k in range(n):a=1+ring*n+k;b=1+ring*n+(k+1)%n;faces.append((a,b,b+n,a+n))
        o=mesh(name,verts,faces,mat or M['glass'])
        if mat is None:curve(name+' recessed seal',boundary,M['trim'],.009,True)
        else:
            s=o.modifiers.new('Panel thickness','SOLIDIFY');s.thickness=.014
        return o
    def frame(self,name,outer,inner,mat):
        outside=rounded_loop(outer,.024);inside=rounded_loop(inner);n=len(outside)
        normal=(Vector(outer[1])-Vector(outer[0])).cross(Vector(outer[2])-Vector(outer[0])).normalized()
        centre=sum((Vector(p) for p in outer),Vector())/4
        if normal.dot(Vector((centre.x,centre.y,0)))<0:normal=-normal
        fv=[];ff=[]
        for ring in range(5):
            t=ring/4;fv.extend([a.lerp(b,t)+normal*(.012*math.sin(math.pi*t)) for a,b in zip(outside,inside)])
        for ring in range(4):
            for k in range(n):a=ring*n+k;b=ring*n+(k+1)%n;ff.append((a,b,b+n,a+n))
        o=mesh(name,fv,ff,mat);m=o.modifiers.new('Pressed frame thickness','SOLIDIFY');m.thickness=.018;bevel(o,.012,2);return o

    # Lofted cabin (doc 66, phase 2) -----------------------------------
    # recipe cabin=dict(
    #   roofline=[(y,z),...]   side-view centre line, rear base to windscreen base (monotone cubic, flat at a crest)
    #   plan=[(y,wb,wr),...]   belt and roof half-widths along the cabin (default glass wb/wr)
    #   section=(px,pz)        superellipse of the cabin section (small = boxy)
    #   edge=q                 where the side meets the roof, as a fraction q of the way round from belt (0) to roof centre (1)
    #   screen=(y_top,y_base), apillar=(q_base,width)        windscreen glass; the A-pillar runs from the roof edge at
    #                                                        y_top down to q_base at y_base (low q_base = wrap-around)
    #   rear_screen=(y_base,y_top), cpillar=(q_base,width)   the same for the rear glass (high q_base = narrow screen,
    #                                                        broad sail panels); omit for none
    #   side=[(y,lo,hi),...]   side daylight opening along the car (lo/hi in q; lo rising = a kick-up, lo meeting hi = a point)
    #   pillars=[y,...], pillar_w, blackout (satin black B-pillars), corner (window corner radius, m), seal ('trim'/'paint'))
    def cab_widths(self,y):
        C=self.C;G=self.G;P=C.get('plan') or [(G['yr0'],G['wb'],G['wr']),(G['yf0'],G['wb'],G['wr'])]
        if y<=P[0][0]:return P[0][1],P[0][2]
        for i in range(len(P)-1):
            if y<=P[i+1][0]:
                t=(y-P[i][0])/max(1e-6,P[i+1][0]-P[i][0]);t=t*t*(3-2*t)
                return P[i][1]+(P[i+1][1]-P[i][1])*t,P[i][2]+(P[i+1][2]-P[i][2])*t
        return P[-1][1],P[-1][2]
    def cab_ring(self,y):
        """The cabin section at y as a polyline from the right belt (t=0) over the roof to the left belt (t=1), with
        its cumulative arc length (the cabin is parametrised by arc fraction t, so windows are drawn in metres)."""
        key=round(y,6)
        if key in self.rings:return self.rings[key]
        C=self.C;b=self.G['belt'];T=pchip(C['roofline'],y)
        if abs(T-b)<.003:T=b+.003
        T=max(b-.4,T);wb,wr=self.cab_widths(y);px,pz=C.get('section',(.40,.34))   # (T below the belt at an end: the section dips into a low bonnet or deck, doc 66)
        pts=[];cum=[0.0];NA=160
        for k in range(NA+1):
            a=math.pi*k/NA;c=math.cos(a);sn=max(0.0,math.sin(a));h=sn**pz
            x=(wb+(wr-wb)*h)*math.copysign(abs(c)**px,c);z=b+(T-b)*h
            if C.get('bubble'):z+=C['bubble']*max(0,math.cos(math.pi*x/max(.05,wr)*1.6))*h**4*max(0,min(1,(T-b)/.35))
            pts.append(Vector((x,y,z)))
            if k:cum.append(cum[-1]+(pts[-1]-pts[-2]).length)
        self.rings[key]=(pts,cum);return pts,cum
    def cab_len(self,y):return self.cab_ring(y)[1][-1]
    def cab_emerge(self,front=True):
        """Where the cabin rises out of the shell at the front (or rear): scanning in from the cabin's end, the first y at
        which the cabin's surface stands above the shell's top across the middle three-quarters of the well."""
        G=self.G;key='_em'+('f' if front else 'r')
        if hasattr(self,key):return getattr(self,key)
        ya,yb=(G['yf0'],G['yr0']) if front else (G['yr0'],G['yf0']);n=int(abs(yb-ya)/.01);out=yb
        for k in range(n+1):
            y=ya+(yb-ya)*k/n;w=(self.cab_widths(y)[0]-.09)*.75
            if all(self.cab_z(w*i/4,y)>self.surface_z(w*i/4,y)+.005 for i in range(5)):out=y;break
        setattr(self,key,out);return out
    def cowl(self):
        """A satin-black cowl panel along the line where the screen rises out of the bonnet (doc 66): the glass meets
        the bonnet at a shallow angle, and the long thin wedge between them read as a dark gap with flicker."""
        G=self.G;M=self.M;ye=self.cab_emerge(True);n=24;v=[];f=[]
        ws=self.cab_windows()[0];yq=ws['y1']-.03;w=min(self.cab_widths(ye)[0]-.03,abs(self.cab_S(ws['rng'](yq)[0],yq).x)+.02)
        for i in range(n+1):
            x=-w+2*w*i/n;y=G['yf0']
            while y>G['yf1'] and self.cab_z(x,y)<=self.surface_z(x,y)+.003:y-=.005
            # (the glass runs on down under the bonnet line; the cowl lies over the joint, 4 cm up the glass)
            for dy,on_cab in ((.045,False),(.0,False),(min(-.03,ye-.01-y),True)):   # (back edge always past the glass's lower seal)
                # (the cabin skin is 12 mm thick about cab_z, so the cowl's back edge sits 14 mm up to clear its face)
                yy=y+dy;z=max(self.top_z(x,yy),self.surface_z(x,yy))+(.006 if dy>.02 else .016)   # (clears the glass's lower seal)
                if on_cab:z=max(z,self.cab_z(x,yy)+.014)
                v.append((x,yy,z))
        for i in range(n):
            for j in range(2):k=i*3+j;f.append((k,k+1,k+4,k+3))
        o=mesh('Satin cowl panel',v,f,M['trim']);m=o.modifiers.new('Cowl thickness','SOLIDIFY');m.thickness=.010;m.offset=-1;bevel(o,.004,2)
        for k in range(-3,4):   # vent slots
            x=k*w*.22;y=G['yf0']
            while y>G['yf1'] and self.cab_z(x,y)<=self.surface_z(x,y)+.003:y-=.005
            box('Cowl vent slot',(x,y+.03,max(self.top_z(x,y+.03),self.surface_z(x,y+.03))+.013),(w*.16,.018,.006),M['insert'],.003)
    def cab_z(self,x,y):
        """Height of the lofted cabin's outer surface above (x,y) (the belt outside its width)."""
        pts,_=self.cab_ring(y);x=abs(x);half=pts[:len(pts)//2+1]
        for a,b in zip(half,half[1:]):
            if a.x>=x>=b.x:return a.z+(b.z-a.z)*(a.x-x)/max(1e-9,a.x-b.x)
        return self.G['belt'] if x>half[0].x else half[-1].z
    def top_z(self,x,y):
        """Height of the built shell's top at (x,y), found by a ray (surface_z misses the fenders' push-out)."""
        if not hasattr(self,'_bvh'):
            from mathutils.bvhtree import BVHTree
            self._bvh=BVHTree.FromObject(self.body,bpy.context.evaluated_depsgraph_get())
        hit,_,_,_=self._bvh.ray_cast(Vector((x,y,3)),Vector((0,0,-1)),4)
        return hit.z if hit is not None else self.surface_z(x,y)
    def cab_S(self,t,y,off=0.0):
        pts,cum=self.cab_ring(y);L=cum[-1];d=max(0,min(1,t))*L;lo,hi=0,len(cum)-1
        while hi-lo>1:
            m=(lo+hi)//2
            if cum[m]<=d:lo=m
            else:hi=m
        seg=max(1e-9,cum[hi]-cum[lo]);p=pts[lo].lerp(pts[hi],(d-cum[lo])/seg)
        return p+self.cab_n(t,y)*off if off else p
    def cab_n(self,t,y):
        e=.004;ey=.004
        da=self.cab_S(min(1,t+e),y)-self.cab_S(max(0,t-e),y);dy=self.cab_S(t,y+ey)-self.cab_S(t,y-ey)
        n=dy.cross(da)
        if n.length<1e-9:return Vector((0,0,1))
        n.normalize();p=self.cab_S(t,y)
        if n.dot(p-Vector((0,y,self.G['belt']-.25)))<0:n=-n
        return n
    def cab_windows(self):
        """Every opening as a band in (t,y): y0..y1 and, per y, a t-range (corners rounded to a radius in metres)."""
        C=self.C;G=self.G;edge=C.get('edge',.42);rho=C.get('corner',.05)
        def q2m(y,m):return m/(self.cab_len(y)*.5)          # metres -> q at y (q runs over half the arc)
        yt,yb=C.get('screen',(G['yf1']+.02,G['yf0']-.02));qa0,wa=C.get('apillar',(.10,.07))
        RS=C.get('rear_screen');qc0,wc=C.get('cpillar',(.10,.09))
        def qA(y):return edge+(qa0-edge)*max(0,min(1,(y-yt)/max(1e-6,yb-yt)))
        def qC(y):
            if not RS:return edge
            return edge+(qc0-edge)*max(0,min(1,(RS[1]-y)/max(1e-6,RS[1]-RS[0])))
        W=[]
        W.append(dict(name='Curved front windscreen',y0=yt,y1=min(G['yf0']-.005,self.cab_emerge(True)+.02),rng=lambda y:((qA(y)+q2m(y,wa*.5))*.5,1-(qA(y)+q2m(y,wa*.5))*.5)))
        if RS:W.append(dict(name='Rear glazing',y0=RS[0],y1=RS[1],rng=lambda y:((qC(y)+q2m(y,wc*.5))*.5,1-(qC(y)+q2m(y,wc*.5))*.5)))
        S=C.get('side') or [(G['yr0']+.10,.07,edge-.04),(yb,.07,edge-.04)]
        def st(y,k):
            if y<=S[0][0]:return S[0][k]
            for i in range(len(S)-1):
                if y<=S[i+1][0]:t=(y-S[i][0])/max(1e-6,S[i+1][0]-S[i][0]);t=t*t*(3-2*t);return S[i][k]+(S[i+1][k]-S[i][k])*t
            return S[-1][k]
        def side_q(y):
            hi=min(st(y,2),qA(y)-q2m(y,wa*.5),qC(y)-q2m(y,wc*.5));return st(y,1),hi
        pw=C.get('pillar_w',.07);cuts=[S[0][0]]
        for py in sorted(C.get('pillars',G['pillars'])):cuts+=[py-pw*.5,py+pw*.5]
        cuts.append(yb)
        self.cab_pillars=[]
        for i in range(0,len(cuts),2):
            for sd in (1,-1):
                f=(lambda y,sd=sd:(side_q(y)[0]*.5,side_q(y)[1]*.5)) if sd>0 else (lambda y:(1-side_q(y)[1]*.5,1-side_q(y)[0]*.5))
                W.append(dict(name='Side glass',y0=cuts[i],y1=cuts[i+1],rng=f,side=sd))
        for (ry0,ry1,q0,q1) in C.get('roof_glass',[]):   # (T-tops, doc 65: two panels either side of a centre bar)
            W.append(dict(name='T-top roof glass',y0=ry0,y1=ry1,rng=lambda y,q0=q0,q1=q1:(q0*.5,q1*.5)))
            W.append(dict(name='T-top roof glass',y0=ry0,y1=ry1,rng=lambda y,q0=q0,q1=q1:(1-q1*.5,1-q0*.5)))
        for i in range(1,len(cuts)-1,2):
            for sd in (1,-1):
                f=(lambda y,sd=sd:(side_q(y)[0]*.5,side_q(y)[1]*.5)) if sd>0 else (lambda y:(1-side_q(y)[1]*.5,1-side_q(y)[0]*.5))
                self.cab_pillars.append(dict(name='Satin B-pillar',y0=cuts[i],y1=cuts[i+1],rng=f))
        out=[]
        for w in W:
            raw=w['rng']
            ys=[w['y0']+(w['y1']-w['y0'])*k/400 for k in range(401)];ok=[y for y in ys if raw(y)[1]-raw(y)[0]>.002]
            if len(ok)<3:continue
            y0,y1=ok[0],ok[-1]
            def rng(y,raw=raw,y0=y0,y1=y1):
                a,b=raw(y);d=min(y-y0,y1-y)
                if d<rho:
                    k=rho/self.cab_len(y)*(1-math.sqrt(max(0,1-(1-max(0,d)/rho)**2)));a,b=a+k,b-k
                return a,max(a,b)
            out.append(dict(w,y0=y0,y1=y1,rng=rng))
        return out
    def cab_patch(self,w,off,mat,name=None,ny=None,nt=14):
        """Mesh a (t,y) band onto the cabin surface; returns the mesh and its boundary loop in (t,y)."""
        y0,y1=w['y0'],w['y1'];ny=ny or max(8,int((y1-y0)/.025));v=[];f=[];uv=[]
        for i in range(ny+1):
            y=y0+(y1-y0)*i/ny;a,b=w['rng'](y)
            for j in range(nt+1):
                t=a+(b-a)*j/nt;uv.append((t,y));v.append(self.cab_S(t,y,off))
        for i in range(ny):
            for j in range(nt):k=i*(nt+1)+j;f.append((k,k+1,k+nt+2,k+nt+1))
        o=mesh(name or w['name'],v,f,mat)
        idx=[j for j in range(nt+1)]+[i*(nt+1)+nt for i in range(1,ny+1)]+[ny*(nt+1)+j for j in range(nt-1,-1,-1)]+[i*(nt+1) for i in range(ny-1,0,-1)]
        loop=[]
        for k in idx:
            if not loop or abs(uv[k][0]-loop[-1][0])+abs(uv[k][1]-loop[-1][1])>1e-5:loop.append(uv[k])
        return o,loop
    def cabin(self):
        C=self.C;G=self.G;M=self.M;rm=M['trim'] if C.get('roof_mat')=='trim' else self.roofmat
        W=self.cab_windows();loops=[]
        for w in W:
            o,loop=self.cab_patch(w,-.004,M['glass']);loops.append(loop)
            curve(w['name']+' rubber seal',[self.cab_S(t,y,.003) for t,y in loop],M['paint'] if C.get('seal')=='paint' else M['trim'],.011,True)
        self.cowl()
        if C.get('blackout'):
            for w in self.cab_pillars:self.cab_patch(w,.003,M['trim'])
        # The skin: a fine (t,y) grid without the faces inside an opening; the hole's edge vertices snap onto the
        # opening's outline (no booleans, so the thin shell can't trip the exact solver; the seal hides the seam).
        yA,yB=G['yr0'],G['yf0'];NT=120;NY=max(40,int((yB-yA)/.02));Lm=self.cab_len((yA+yB)*.5)
        def inside(t,y):
            for w in W:
                if w['y0']<=y<=w['y1']:
                    a,b=w['rng'](y)
                    if a<t<b:return True
            return False
        grid=[(j/NT,yA+(yB-yA)*i/NY) for i in range(NY+1) for j in range(NT+1)];f=[];keep=set();drop=set()
        for i in range(NY):
            for j in range(NT):
                k=i*(NT+1)+j;q=(k,k+1,k+NT+2,k+NT+1)
                if inside((j+.5)/NT,yA+(yB-yA)*(i+.5)/NY):drop.update(q)
                else:f.append(q);keep.update(q)
        edge_v=keep&drop;flat=[(t*Lm,y) for lp in loops for (t,y) in lp]
        # Densify the outlines so a snapped vertex lands on the curve, not a chord's end.
        dense=[]
        for lp in loops:
            for a,b in zip(lp,lp[1:]+lp[:1]):
                for k in range(4):u=k/4;dense.append((a[0]+(b[0]-a[0])*u,a[1]+(b[1]-a[1])*u))
        from mathutils.kdtree import KDTree
        kd=KDTree(len(dense))
        for k,(t,y) in enumerate(dense):kd.insert((t*Lm,y,0),k)
        kd.balance();pos=list(grid)
        for k in edge_v:
            t,y=grid[k];_,i_,_=kd.find((t*Lm,y,0));pos[k]=dense[i_]
        v=[self.cab_S(t,y) for t,y in pos]
        skin=mesh('Lofted cabin skin',v,f,rm);sm=skin.modifiers.new('Roof metal thickness','SOLIDIFY');sm.thickness=.012;sm.offset=0
        # Wipers lie at the screen's base.
        yt,yb=C.get('screen',(G['yf1']+.02,G['yf0']-.02))
        for x in [-.32,.18]:
            yw=self.cab_emerge(True)-.03
            pts=[self.cab_S(.5-x/Lm+k*.06,yw-.04-k*.03,.02) for k in (-1,0,1)]
            curve('Front wiper',pts,M['trim'],.008)
        if self.R['rear']=='hatch' and C.get('rear_screen'):
            y=C['rear_screen'][0]+.06;curve('Rear wiper',[self.cab_S(.5+k*.08,y+abs(k)*.02,.02) for k in (-1.5,0,1.5)],M['trim'],.011)

    def greenhouse(self):
        if self.C:return self.cabin()
        G=self.G;M=self.M;b=G['belt'];r=G['roof'];wb=G['wb'];wr=G['wr'];inset=.052
        rm=self.roofmat
        def sx(z):return wb+(wr-wb)*(z-b)/(r-b)
        front=[(-wb,G['yf0'],b),(wb,G['yf0'],b),(wr,G['yf1'],r),(-wr,G['yf1'],r)]
        rear=[(wb,G['yr0'],b),(-wb,G['yr0'],b),(-wr,G['yr1'],r),(wr,G['yr1'],r)]
        fi=inset_quad(front,inset);ri=inset_quad(rear,inset)
        self.glass_patch('Curved front windscreen',fi,(0,1,0),.014)
        self.frame('Pressed windscreen surround',front,fi,rm)
        if self.R['rear']=='barn':
            # Van: split rear doors, each with a small window.
            for s in [-1,1]:
                q=[Vector(p) for p in ri];mid=[(q[0]+q[1])*.5,(q[2]+q[3])*.5]
                half=[q[0],mid[0],mid[1],q[3]] if s>0 else [mid[0],q[1],q[2],mid[1]]
                lower=[half[0],half[1],half[1].lerp(half[2],.45),half[0].lerp(half[3],.45)]
                upper=[half[0].lerp(half[3],.5),half[1].lerp(half[2],.5),half[2],half[3]]
                self.glass_patch('Van rear door panel',inset_quad(lower,.02),(0,-1,0),.004,rm)
                self.glass_patch('Van rear door window',inset_quad(upper,.035),(0,-1,0),.008)
            box('Rear door split',(0,G['yr0']-.03,(b+r)*.5),(.03,.03,r-b),M['seam'],.01)
        else:
            self.glass_patch('Rear glazing',ri,(0,-1,0),.010)
        self.frame('Pressed rear surround',rear,ri,rm)
        # Roof: gently crowned sheet meeting both headers.
        rv=[];rf=[];ya,yb=G['yr1'],G['yf1']
        for j in range(17):
            y=ya+(yb-ya)*j/16
            for i in range(21):
                u=-1+2*i/20;rv.append((u*wr,y,r+G['crown']*(1-u*u)*math.sin(math.pi*j/16)+G.get('arc',0)*math.sin(math.pi*j/16)
                                      +G.get('bubble',0)*max(0,math.cos(math.pi*u*1.6))*math.sin(math.pi*j/16)))   # (bubble: the RX-7's double hump, doc 66)
        for j in range(16):
            for i in range(20):a=j*21+i;rf.append((a,a+1,a+22,a+21))
        if 'canvas_roof' in self.R['extras']:
            roof=mesh('Folding canvas roof',rv,rf,M['canvas']);s=roof.modifiers.new('Canvas thickness','SOLIDIFY');s.thickness=.02
            for k in range(1,6):
                y=ya+(yb-ya)*k/6;curve('Canvas bow',[(u*wr*.98,y,r+G['crown']*(1-u*u)*math.sin(math.pi*k/6)+.012) for u in [-1+i/8 for i in range(17)]],M['canvas'],.012,poly=True)
            # Rolled-back fabric at the rear header.
            cylinder('Rolled canvas',(0,ya+.12,r+.05),.07,2*wr-.04,M['canvas'],'X',24)
        else:
            roof=mesh('Crowned roof panel',rv,rf,rm);s=roof.modifiers.new('Roof metal thickness','SOLIDIFY');s.thickness=.012
            if 'sunroof' in self.R['extras']:
                yc=G['yf1']-.55;cz=r+G['crown']*.9+.012
                self.glass_patch('Tilting sunroof',[(-.36,yc-.28,cz),(.36,yc-.28,cz),(.36,yc+.28,cz),(-.36,yc+.28,cz)],(0,0,1),.008)
        # Sides: frame, glass split by pillars, pillar trims.
        for side in [-1,1]:
            outer=[(side*wb,G['yr0'],b),(side*wb,G['yf0'],b),(side*wr,G['yf1'],r),(side*wr,G['yr1'],r)]
            inner=inset_quad(outer,inset+.008)
            self.frame('Pressed side window surround',outer,inner,rm)
            zb=(inner[0].z+inner[1].z)*.5;zt=(inner[2].z+inner[3].z)*.5
            yRB,yFB,yFT,yRT=inner[0].y,inner[1].y,inner[2].y,inner[3].y
            def X(z):return side*(sx(z)+.004)
            def cb(y):return min(max(y,yRB),yFB)
            def ct(y):return min(max(y,yRT),yFT)
            cuts=[min(yRB,yRT)]
            for p in sorted(G['pillars']):cuts+=[p-.05,p+.05]
            cuts.append(max(yFB,yFT))
            for i in range(0,len(cuts),2):
                ya_,yb_=cuts[i],cuts[i+1]
                quad=[(X(zb),cb(ya_),zb),(X(zb),cb(yb_),zb),(X(zt),ct(yb_),zt),(X(zt),ct(ya_),zt)]
                if abs(quad[2][1]-quad[3][1])<.03:  # front/rear window tapering to a point
                    quad[3]=(X(zt),ct(ya_)-.03,zt)
                rearmost=i==0;front=i==len(cuts)-2
                if G['quarter']=='panel' and not front:
                    self.glass_patch('Van side panel',[Vector(q)+Vector((side*.004,0,0)) for q in quad],(side,0,0),.004,rm)
                else:self.glass_patch('Side glass',quad,(side,0,0),.006)
            for p in G['pillars']:
                mesh('Flat satin pillar',[(X(zb)+side*.004,p-.05,zb),(X(zb)+side*.004,p+.05,zb),(X(zt)+side*.004,p+.05,zt),(X(zt)+side*.004,p-.05,zt)],[(0,1,2,3)],M['trim'],False)
        # Wipers
        g=Vector(fi[0]).lerp(Vector(fi[1]),.5)
        for x in [-.32,.18]:
            curve('Front wiper',[(x-.2,fi[0][1]-.02,b+.07),(x,fi[0][1]-.05,b+.10),(x+.18,fi[0][1]-.06,b+.12)],M['trim'],.008)
        if self.R['rear'] in ('hatch',):
            curve('Rear wiper',[(.26,ri[0][1]-.02,b+.09),(0,ri[0][1]-.03,b+.11),(-.28,ri[0][1]-.03,b+.12)],M['trim'],.011)

    # Details ----------------------------------------------------------
    def bumper(self,front):
        B=self.R['bumper'];style=B['front' if front else 'rear'];M=self.M
        if style=='none':return
        if style=='lip':   # (doc 66: an integrated nose; the shell is the bumper, with a dark lip under it)
            p2,n2=self.outline(B['z']-B['h']*.5,front,-.01,span=92);sweep('Integrated bumper lip',p2,n2,rrect(B['d']*.7,.05,.018,2),M['trim']);return
        mat={'chrome':M['chrome'],'dark':M['trim'],'body':M['paint']}[style]
        pts,nor=self.outline(B['z'],front,-.02,span=98)
        prof=[(a,b) for a,b in rrect(B['d']+.02,B['h'],min(.05,B['h']*.45))]
        sweep('Wrap-around bumper',pts,nor,prof,mat)
        if style=='chrome':
            p2,n2=self.outline(B['z'],front,B['d']-.005,span=70);sweep('Bumper rubber strip',p2,n2,rrect(.02,B['h']*.3,.008,2),M['trim'])
            for side in [-1,1]:
                y=self.end_y(.42,B['z'],front)
                if y is not None:cylinder('Bumper overrider',(side*.42,y+(1 if front else -1)*(B['d']+.02),B['z']+.03),.035,.2,M['chrome'],'Z',16)
        elif style=='body':
            p2,n2=self.outline(B['z']-B['h']*.5-.03,front,.0,span=90);sweep('Lower bumper lip',p2,n2,rrect(B['d']*.8,.06,.02,2),M['trim'])
        else:
            p2,n2=self.outline(B['z']+.01,front,B['d']-.005,span=80);sweep('Bumper insert strip',p2,n2,rrect(.02,B['h']*.35,.01,2),M['insert'])

    def headlamps(self):
        L=self.R['lamps'];M=self.M;s=L['style']
        for side in [-1,1]:
            if s in ('round','quad'):
                xs=[L['x']]+([L['x2']] if s=='quad' else [])
                for x in xs:
                    y=self.end_y(x,L['z']);r=L['r'];yw=self.yaw(side*x,L['z']);pv=(side*x,y,L['z'])
                    # Body-colour cowl sunk into the panel (up to 8 cm behind the lamp plane) and reaching the
                    # most forward point of the shell round the rim, so the lamp face is never buried.
                    t0,t1=self.spans.get((side,x),(-.02,.02));t1=min(t1,.06);back=max(t0,-.08)-.02;f=y+t1
                    self.place(cylinder('Headlamp cowl',(side*x,y+(back+t1-.006)*.5,L['z']),r+.03,t1-.006-back,M['paint'],'Y',48,.012),pv,yw)
                    self.place(cylinder('Lamp chrome lip',(side*x,f-.004,L['z']),r+.004,.04,M['chrome'],'Y',40),pv,yw)
                    self.place(sphere('Convex headlamp lens',(side*x,f+.012,L['z']),(r*.9,.03,r*.9),M['lamp']),pv,yw)
                    for k in range(3):self.place(box('Lamp lens flute',(side*x,f+.036,L['z']-r*.4+k*r*.4),(r*1.3,.004,.006),M['lamp'],0),pv,yw)
            elif s=='rect':
                y=self.end_y(L['x'],L['z']);w,h=L['w'],L['h'];yw=self.yaw(side*L['x'],L['z']);pv=(side*L['x'],y,L['z'])
                self.place(box('Lamp recess',(side*L['x'],y-.05,L['z']),(w+.03,.06,h+.03),M['trim'],.02),pv,yw)
                self.place(box('Rectangular headlamp lens',(side*L['x'],y-.012,L['z']),(w,.04,h),M['lamp'],.022),pv,yw)
                for k in range(1,4):self.place(box('Lens flute',(side*(L['x']-w*.5+w*k/4),y+.01,L['z']),(.006,.006,h*.8),M['lamp'],0),pv,yw)
                self.place(box('Chrome lamp surround',(side*L['x'],y-.02,L['z']+h*.5+.012),(w+.03,.03,.012),M['chrome'],.005),pv,yw)
                ya=self.end_y(L['x']+w*.5+.06,L['z']-h*.25)
                if ya:box('Front indicator',(side*(L['x']+w*.5+.055),ya-.01,L['z']-h*.25),(.07,.04,h*.45),M['amber'],.015)
            elif s=='pod':
                z=self.surface_z(L['x'],self.P[-1][0]-.45)+.14;y=self.P[-1][0]-.34
                curve('Lamp stalk',[(side*L['x'],y-.05,z-.16),(side*L['x'],y-.02,z-.03)],M['trim'],.025,poly=True)
                cylinder('Pod lamp shell',(side*L['x'],y,z),L['r'],.18,M['paint'],'Y',40,.02)
                sphere('Pod shell back',(side*L['x'],y-.09,z),(L['r'],.08,L['r']),M['paint'])
                cylinder('Pod chrome lip',(side*L['x'],y+.09,z),L['r']*.95,.02,M['chrome'],'Y',40)
                sphere('Convex headlamp lens',(side*L['x'],y+.10,z),(L['r']*.85,.03,L['r']*.85),M['lamp'])
            if s not in ('rect','none'):
                ya=self.end_y(L['x']+.12 if s!='quad' else L['x2']+.12,L['z']-.14)
                if ya and s!='pod':box('Front indicator',(side*(L['x']+.14 if s!='quad' else L['x2']+.12),ya-.005,L['z']-.14),(.09,.04,.045),M['amber'],.015)
                elif s=='pod':
                    ya=self.end_y(.60,.62)
                    if ya:cylinder('Front indicator',(side*.60,ya-.005,.62),.035,.04,M['amber'],'Y',20)

    def grille(self):
        g=self.R['grille'];M=self.M;L=self.R['lamps'];z=L['z']
        yN=self.end_y(0,z)
        if g=='slats':
            box('Grille inset',(0,yN-.02,z),(.62,.05,.12),M['trim'],.04)
            for dz in [-.03,0,.03]:box('Grille horizontal blade',(0,yN+.005,z+dz),(.56,.008,.008),M['insert'],.003)
        elif g=='chrome':
            w=2*(L['x']-L.get('w',L.get('r',.1)*2)*.5-.05);h=.16
            box('Grille dark back',(0,yN-.03,z),(w,.05,h),M['trim'],.02)
            box('Grille chrome surround',(0,yN-.012,z),(w+.03,.024,h+.03),M['chrome'],.012)
            box('Grille dark back front',(0,yN-.0,z),(w-.02,.012,h-.02),M['trim'],.005)
            for k in range(5):box('Chrome grille bar',(0,yN+.012,z-h*.4+k*h*.2),(w-.03,.012,.012),M['chrome'],.004)
            box('Grille centre badge',(0,yN+.024,z),(.07,.012,.07),M['cream'],.02)
        elif g=='full':
            w=2*(L['x']+L['r']+.05);h=L['r']*2+.08
            box('Full-width grille panel',(0,yN-.025,z),(w,.05,h),M['trim'],.03)
            for k in range(4):box('Grille chrome line',(0,yN+.004,z-h*.3+k*h*.2),(w*.42,.008,.008),M['chrome'],.003)
            box('Grille badge',(0,yN+.012,z),(.10,.012,.05),M['chrome'],.015)
        elif g=='whisker':
            curve('Chrome whisker bar',[(-.32,yN-.04,z-.05),(-.12,yN+.01,z-.01),(0,yN+.015,z-.005),(.12,yN+.01,z-.01),(.32,yN-.04,z-.05)],M['chrome'],.018)
            cylinder('Nose badge',(0,yN+.02,z+.07),.045,.02,M['chrome'],'Y',32)
            box('Lower intake',(0,self.end_y(0,.60)-.02,.60),(.44,.05,.07),M['trim'],.03)
        elif g=='slot':
            box('Slot grille',(0,yN-.02,z-.02),(.62,.05,.06),M['trim'],.025)
            box('Nose badge',(0,yN+.008,z+.07),(.09,.012,.05),M['chrome'],.018)
        elif g=='tall':
            w=.42;h=.28;y=self.end_y(0,z-.02)
            box('Tall grille back',(0,y-.02,z-.02),(w,.06,h),M['trim'],.03)
            box('Tall grille chrome frame',(0,y+.0,z-.02),(w+.04,.03,h+.04),M['chrome'],.03)
            box('Tall grille back front',(0,y+.012,z-.02),(w-.02,.012,h-.02),M['trim'],.01)
            for k in range(9):box('Vertical chrome grille bar',(-w*.44+k*w*.11,y+.024,z-.02),(.012,.012,h-.03),M['chrome'],.004)
            cylinder('Bonnet mascot',(0,self.P[-1][0]-.18,self.surface_z(0,self.P[-1][0]-.18)+.02),.02,.07,M['chrome'],'Z',12)
        elif g=='chevron':
            w=.46;h=.26;y=self.end_y(0,z-.18)
            box('Grille back',(0,y-.02,z-.18),(w,.05,h),M['trim'],.03)
            box('Grille chrome frame',(0,y-.005,z-.18),(w+.04,.03,h+.04),M['chrome'],.02)
            box('Grille back front',(0,y+.008,z-.18),(w-.02,.012,h-.02),M['trim'],.008)
            for k in range(2):
                zz=z-.12-k*.1
                for sd in [-1,1]:box('Chevron',(sd*.1,y+.02,zz),(.2,.012,.03),M['chrome'],.006,rot=(0,sd*.35,0))
        # Bonnet shut lines (compact style) and a badge
        yF=self.P[-1][0];yc=self.G['yf0']+.05
        for side in [-1,1]:
            pts=[]
            for i in range(25):
                t=i/24;y=yF-.05-(yF-.05-yc)*t;x=side*(.30+.20*t);pts.append((x,y,self.surface_z(x,y)+.003))
            self.groove(pts,width=.006)
        if 'ribs' in self.R['extras']:
            for k in range(-3,4):
                pts=[]
                for i in range(13):
                    y=yF-.12-(yF-.12-yc)*i/12;x=k*.09;pts.append((x,y,self.surface_z(x,y)+.004))
                curve('Corrugated bonnet rib',pts,self.M['paint'],.012)

    # Face outlines (doc 66, phase 2) ---------------------------------
    # recipe face=[dict(kind='lamp'|'grille'|'intake'|'panel', pts=[(x,z),...] (front view, the right-hand one when
    #   mirrored), mirror (default: lamps yes, grilles/intakes no), surround=.02 (dark rim, m), round=.25 (corner
    #   rounding), bars=n (grille/intake blades), projectors=n (round lamp units in a lamp), mat='lamp'|'amber'|...)],
    #   face_plate=z or None. Each outline is laid onto the built nose (rays, so fenders and pods count), slightly
    #   proud, as the toy cars' lamps are: no booleans, so any shape is safe.
    def nose_hit(self,x,z,front=True):
        if not hasattr(self,'_bvh'):
            from mathutils.bvhtree import BVHTree
            self._bvh=BVHTree.FromObject(self.body,bpy.context.evaluated_depsgraph_get())
        y0=(self.P[-1][0]+1) if front else (self.P[0][0]-1);d=Vector((0,-1,0)) if front else Vector((0,1,0))
        hit,nrm,_,_=self._bvh.ray_cast(Vector((x,y0,z)),d,3)
        if hit is None:return None,None
        if nrm.dot(-d)<0:nrm=-nrm
        return hit,nrm
    @staticmethod
    def grow2d(pts,g):
        n=len(pts);area=sum(pts[i].x*pts[(i+1)%n].y-pts[(i+1)%n].x*pts[i].y for i in range(n));sg=1 if area>0 else -1;out=[]
        for i in range(n):
            a,b,c=pts[i-1],pts[i],pts[(i+1)%n];e1=(b-a);e2=(c-b)
            if e1.length<1e-6 or e2.length<1e-6:out.append(b.copy());continue
            e1.normalize();e2.normalize();n1=Vector((e1.y,-e1.x))*sg;n2=Vector((e2.y,-e2.x))*sg;m=(n1+n2)
            if m.length<1e-6:m=n1
            m.normalize();k=max(.5,m.dot(n1));out.append(b+m*(g/k))
        return out
    def face_patch(self,name,pts2,lift,mat,th,front=True,rings=5):
        """Fill a front-view outline (2D Vectors x,z) laid on the nose, lifted along the surface normal."""
        c=sum(pts2,Vector((0,0)))/len(pts2);n=len(pts2);v=[];f=[]
        def at(q):
            hit,nrm=self.nose_hit(q.x,q.y,front)
            if hit is None:return None
            return hit+nrm*lift
        cp=at(c)
        if cp is None:return None
        v.append(cp)
        for r in range(1,rings+1):
            for q in pts2:
                p_=at(c.lerp(q,r/rings));v.append(p_ if p_ is not None else v[-1])
        for k in range(n):f.append((0,1+k,1+(k+1)%n))
        for r in range(rings-1):
            for k in range(n):a=1+r*n+k;b=1+r*n+(k+1)%n;f.append((a,b,b+n,a+n))
        o=mesh(name,v,f,mat);m=o.modifiers.new('Face part thickness','SOLIDIFY');m.thickness=th;m.offset=-1
        return o
    def face(self,items=None,front=True):
        M=self.M;items=items if items is not None else self.R.get('face',[])
        mats={'lamp':M['lamp'],'amber':M['amber'],'trim':M['trim'],'paint':M['paint'],'red':M['red'],'insert':M['insert']}
        for it in items:
            kind=it['kind'];sides=[1,-1] if it.get('mirror',kind in ('lamp','panel')) else [1]
            for sd in sides:
                if 'oval' in it:   # oval=(cx,cz,rx,rz,p): a superellipse mouth (p<1 squarer)
                    cx,cz,rx,rz,pe=it['oval'];raw=[]
                    for k in range(36):
                        a=k*math.tau/36;c=math.cos(a);sn=math.sin(a)
                        raw.append(Vector((sd*cx+rx*math.copysign(abs(c)**pe,c),cz+rz*math.copysign(abs(sn)**pe,sn))))
                    pts=raw
                else:
                    raw=[Vector((sd*x,z)) for x,z in it['pts']]
                    if sd<0:raw.reverse()
                    pts=rounded_loop(raw,it.get('round',.2)) if it.get('round',.2)>0 else raw
                pts=[Vector((q.x,q.y)) for q in pts]
                sur=it.get('surround',.022 if kind=='lamp' else .018)
                label={'lamp':'Outlined headlamp','grille':'Outlined grille','intake':'Outlined air intake','panel':'Outlined face panel'}[kind]
                if sur>0:self.face_patch(label+' surround',self.grow2d(pts,sur),.004,M['trim'],.010,front)
                mat=mats[it.get('mat',{'lamp':'lamp','grille':'trim','intake':'trim','panel':'paint'}[kind])]
                self.face_patch(label,pts,.010 if kind=='lamp' else .006,mat,.012 if kind=='lamp' else .008,front)
                zs=[q.y for q in pts];z0,z1=min(zs),max(zs)
                def span(z):
                    xs=[]
                    for a,b in zip(pts,pts[1:]+pts[:1]):
                        if (a.y-z)*(b.y-z)<0:xs.append(a.x+(b.x-a.x)*(z-a.y)/(b.y-a.y))
                    return (min(xs),max(xs)) if len(xs)>=2 else None
                nb=it.get('bars',0)
                for k in range(nb):
                    z=z0+(z1-z0)*(k+1)/(nb+1);sp=span(z)
                    if not sp:continue
                    xa,xb=sp[0]+.025,sp[1]-.025
                    if xb-xa<.04:continue
                    q=[Vector((xa,z-.008)),Vector((xb,z-.008)),Vector((xb,z+.008)),Vector((xa,z+.008))]
                    self.face_patch(label+' blade',q,.016,M['insert'] if kind=='grille' else M['trim'],.008,front,2)
                for k in range(it.get('projectors',0)):
                    cx=sorted(q.x for q in pts);cz=(z0+z1)*.5;xa,xb=cx[0],cx[-1]
                    x=xa+(xb-xa)*(k+.5)/it['projectors'];r=min((z1-z0)*.36,(xb-xa)/it['projectors']*.36)
                    hit,nrm=self.nose_hit(x,cz,front)
                    if hit is None:continue
                    c_=hit+nrm*.022;cylinder('Lamp projector bezel',c_,r*1.12,.012,M['chrome'],'Y',24)
                    sphere('Lamp projector lens',c_+nrm*.008,(r,.012,r),M['lamp'])

    def tail(self):
        T=self.R['tail'];M=self.M;s=T['style']
        for side in [-1,1]:
            if s in ('vertical','fin'):
                y=self.end_y(T['x'],T['z'],False);w,h=T['w'],T['h']
                box('Rear lamp surround',(side*T['x'],y+.03,T['z']),(w+.04,.08,h+.05),M['trim'],.03)
                box('Red rear lamp',(side*T['x'],y-.012,T['z']+h*.12),(w,.03,h*.66),M['red'],.02)
                box('Reverse lamp',(side*T['x'],y-.012,T['z']-h*.36),(w,.028,h*.18),M['reverse'],.01)
                for dz in [-.04,0,.04]:box('Moulded tail lens rib',(side*T['x'],y-.028,T['z']+h*.12+dz),(w*.8,.006,.008),M['red'],.002)
                if s=='fin':
                    pts=[(side*(self.surface_x(-1.2,.9)-.08),yy,self.surface_z(self.surface_x(-1.2,.9)-.08,yy)) for yy in [-1.0,-1.3,-1.6]]
                    curve('Tail fin crease',[(p[0],p[1],p[2]+(i*.03)) for i,p in enumerate(pts)],M['paint'],.035)
            elif s=='horizontal':
                y=self.end_y(T['x'],T['z'],False);w,h=T['w'],T['h']
                box('Rear lamp surround',(side*T['x'],y+.03,T['z']),(w+.04,.08,h+.04),M['trim'],.03)
                box('Amber rear indicator',(side*(T['x']+w*.34),y-.012,T['z']),(w*.3,.03,h*.8),M['amber'],.015)
                box('Red rear lamp',(side*(T['x']-w*.02),y-.012,T['z']),(w*.36,.03,h*.8),M['red'],.015)
                box('Reverse lamp',(side*(T['x']-w*.36),y-.012,T['z']),(w*.26,.028,h*.8),M['reverse'],.012)
                for dz in [-.03,.03]:box('Moulded tail lens rib',(side*T['x'],y-.028,T['z']+dz),(w*.9,.006,.007),M['red'],.002)
            elif s=='round':
                for k,(dx,mat,r) in enumerate([(0,M['red'],T['r']),(-.17,M['reverse'],T['r']*.7)]):
                    x=T['x']+dx;y=self.end_y(x,T['z'],False)
                    cylinder('Round lamp bezel',(side*x,y+.01,T['z']),r+.015,.05,M['chrome'],'Y',32)
                    sphere('Round rear lens',(side*x,y-.015,T['z']),(r,.025,r),mat)
            elif s=='twin_round':
                # The Tuner's signature (doc 65): two round lamps each side, red with a bright inner ring.
                for x in (T['x'],T['x2']):
                    y=self.end_y(x,T['z'],False);r=T['r']
                    cylinder('Round lamp bezel',(side*x,y+.01,T['z']),r+.016,.05,M['chrome'],'Y',32)
                    sphere('Round rear lens',(side*x,y-.015,T['z']),(r,.025,r),M['red'])
                    cylinder('Lamp inner ring',(side*x,y-.04,T['z']),r*.42,.008,M['reverse'],'Y',24,0)
            elif s=='boomerang':
                # Across the corner, then a limb swept up and forward along the haunch (doc 65).
                y=self.end_y(T['x'],T['z'],False);w,h=T['w'],T['h']
                box('Rear lamp surround',(side*T['x'],y+.03,T['z']),(w+.04,.08,h+.04),M['trim'],.03)
                box('Red rear lamp',(side*T['x'],y-.012,T['z']),(w,.03,h),M['red'],.015)
                xo=T['x']+w*.5-.04;yo=y+.14
                box('Rear lamp sweep',(side*xo,yo,T['z']+.05),(.06,.26,h*.85),M['red'],.015,rot=(-.35,0,0))
            yr=self.end_y(T['x'],.47,False)
            if yr:box('Rear bumper reflector',(side*T['x'],yr-.02,.47),(.12,.014,.034),M['reflector'],.012)

    def body_details(self):
        R=self.R;M=self.M;G=self.G;b=G['belt'];yF=self.P[-1][0];yR=self.P[0][0]
        sill=self.section(0)[1]
        # Number plates, badges, exhaust, underbody
        pz=R.get('face_plate',.60 if R['bumper']['z']<.55 else .45)
        if pz is not None:y=self.end_y(0,pz if 'face_plate' in R else .60);box('Front number plate',(0,y+.01,pz),(.40,.02,.10),M['plate'],.014)
        y=self.end_y(0,.70,False);box('Rear plate recess',(0,y+.01,.70),(.52,.03,.14),M['seam'],.028)
        box('Rear number plate',(0,y-.006,.70),(.42,.016,.105),M['plate'],.013)
        yb=self.end_y(.32,.86,False)
        if yb is not None:box('Rear badge',(.32,yb-.004,.86),(.11,.012,.03),M['chrome'],.01)   # (none on a tail lower than the badge)
        cylinder('Exhaust chrome outlet',(-.42,yR+.03,.30),.05,.18,M['chrome'],'Y',24)
        cylinder('Exhaust dark bore',(-.42,yR-.062,.30),.038,.008,M['trim'],'Y',24)
        box('Underbody',(0,(yF+yR)*.5,sill+.01),(1.3,(yF-yR)-.4,.05),M['trim'],.04)
        # Doors: shut lines, handles; sills.
        fa,ra,r=self.fa,self.ra,self.wr
        pillars=sorted(G['pillars'],reverse=True) or [max(G['yr0']+.12,self.ra+self.wr+.14)];yb=pillars[0];zs=sill+.08
        top=b+.01
        def door(front_top,front_low,back):
            return [(front_top,top),(front_top-.06,top-.16),(front_low,zs+.12),(front_low-.06,zs),(back+.04,zs),(back,zs+.08),(back,top)]
        doors=[]
        doors.append(door(min(G['yf0']-.06,fa-r-.02),fa-r-.14,yb))
        if R['doors']==4 and len(pillars)>=1:
            back=pillars[1] if len(pillars)>1 else G['yr0']+.16
            back=max(back,ra+r+.10)
            if yb-back>.35:doors.append([(yb-.04,top),(yb-.04,zs+.08),(yb-.08,zs),(back+.06,zs),(back,zs+.14),(back,top)])
        for side in [-1,1]:
            for d in doors:
                pts=[(side*(self.surface_x(y,z)+.008),y,z) for y,z in d]
                self.groove(pts)
                hy=d[-1][0]+.16 if d is doors[0] else d[0][0]-.16;hz=top-.10
                box('Recessed door handle pocket',(side*(self.surface_x(hy,hz)+.002),hy,hz),(.018,.20,.06),M['seam'],.02)
                box('Door handle',(side*(self.surface_x(hy,hz)+.014),hy,hz+.004),(.025,.16,.03),M['chrome'] if R['bumper']['front']=='chrome' else M['trim'],.012)
            box('Rocker protection',(side*(self.surface_x(0,sill+.07)-.01),(fa+ra)*.5,sill+.06),(.06,fa-ra-2*r-.24,.09),M['trim'],.03)
            # Fuel flap on the right rear quarter.
            if side>0:
                fy=ra+r+.1 if R['doors']==2 else (ra-.05 if ra-.05>yR+.3 else ra+r+.1);fz=b-.14
                loop=[(0,fy+.1,fz-.07),(0,fy-.1,fz-.07),(0,fy-.1,fz+.07),(0,fy+.1,fz+.07)]
                self.groove([(self.surface_x(p.y,p.z),p.y,p.z) for p in rounded_loop(loop,.25)],cyclic=True,width=.006)
            # Mirrors
            my=G['yf0']-.12;mz=b+.07;mx=self.surface_x(my,b-.02)
            if R['mirrors']=='round':
                wy=fa-.05;wz=self.surface_z(.62,wy)
                curve('Wing mirror stem',[(side*.62,wy,wz),(side*.64,wy,wz+.12)],M['chrome'],.012,poly=True)
                cylinder('Round wing mirror',(side*.645,wy-.02,wz+.14),.055,.04,M['chrome'],'Y',24)
                cylinder('Wing mirror glass',(side*.645,wy-.043,wz+.14),.044,.006,M['alloy'],'Y',24)
            else:
                curve('Mirror stem',[(side*(mx-.05),my+.02,mz),(side*(mx+.12),my,mz+.02)],M['trim'],.026)
                box('Rounded mirror housing',(side*(mx+.2),my-.02,mz+.04),(.2,.17,.12),M['paint'],.042)
                box('Mirror reflective insert',(side*(mx+.2),my-.108,mz+.04),(.15,.012,.08),M['alloy'],.025)
        # Arch mouldings / flares, laid on the real (fendered, cut) body: each vertex is ray-cast onto
        # the shell from outside. The analytic surface_x collapsed above the shell top and laid the
        # moulding flat across low bonnets.
        from mathutils.bvhtree import BVHTree
        tree=BVHTree.FromObject(self.body,bpy.context.evaluated_depsgraph_get())
        def shell_x(side,y,z):
            hit,nrm,_,_=tree.ray_cast(Vector((side*3,y,z)),Vector((-side,0,0)),3)
            # Only the wing's side counts: hits far inboard (bonnet, bumper) or on a surface facing up
            # (the top of the fender, which a horizontal ray only grazes) would lay the lip flat.
            if hit is None or not self.track-.26<=abs(hit.x)<=self.track+.3 or abs(nrm.x)<.45:return None
            return abs(hit.x)
        def settle(xs):
            # Bridge one- or two-point gaps, then smooth each continuous run so the lip runs clean.
            xs=list(xs)
            for i in range(len(xs)):
                if xs[i] is None:
                    lo=next((j for j in range(i-1,max(-1,i-3),-1) if xs[j] is not None),None);hi=next((j for j in range(i+1,min(len(xs),i+3)) if xs[j] is not None),None)
                    if lo is not None and hi is not None:xs[i]=xs[lo]+(xs[hi]-xs[lo])*(i-lo)/(hi-lo)
            for _ in range(3):
                xs=[x if x is None or i in (0,len(xs)-1) or xs[i-1] is None or xs[i+1] is None else (xs[i-1]+2*x+xs[i+1])*.25 for i,x in enumerate(xs)]
            return xs
        for side in [-1,1]:
            for cy in [fa,ra]:
                flare=R['arches']=='flare';paint=R['arches'] in ('paint','flare');av=[];af=[];rin=r+self.FENDER+.004;rout=rin+(.13 if flare else .075 if paint else .055)
                angles=[-.10+(math.pi+.20)*k/40 for k in range(41)]
                rings={rr:settle([shell_x(side,cy+rr*math.cos(a),r+rr*math.sin(a)) for a in angles]) for rr in [rin,rout]}
                # Continuous runs of at least six segments (short stubs read as broken trim); each run
                # tapers to nothing over its last four segments so the lip fades into the wing.
                ok=[all(rings[rr][q] is not None for rr in (rin,rout) for q in (k,k+1)) and all(abs(rings[rin][q]-rings[rout][q])<.06 for q in (k,k+1)) for k in range(40)];k=0
                lift=(.07 if flare else .03 if paint else .022);runs=[]
                while k<40:
                    if not ok[k]:k+=1;continue
                    e=k
                    while e<40 and ok[e]:e+=1
                    if e-k>=6:runs.append((k,e))
                    k=e
                # One unbroken lip round most of the arch, or none: split runs left loose fins hanging
                # either side of an up-facing wing top.
                if len(runs)!=1 or runs[0][1]-runs[0][0]<28:runs=[]
                for k,e in runs:
                    if True:
                        base=len(av)
                        for q in range(k,e+1):
                            f=min(1,(q-k)/4,(e-q)/4);a=angles[q];ro=rin+(rout-rin)*max(f,.08)
                            xo=rings[rin][q]+(rings[rout][q]-rings[rin][q])*max(f,.08)
                            av.append((side*(rings[rin][q]+lift*max(f,.3)),cy+rin*math.cos(a),r+rin*math.sin(a)))
                            av.append((side*(xo+lift*.4*max(f,.3)),cy+ro*math.cos(a),r+ro*math.sin(a)))
                        for q in range(e-k):af.append((base+q*2,base+q*2+1,base+q*2+3,base+q*2+2))
                if not af:continue
                arch=mesh('Moulded wheel arch',av,af,M['paint'] if paint else M['trim']);sol=arch.modifiers.new('Trim wall','SOLIDIFY');sol.thickness=.03 if paint else .025
                bevel(arch,.009,2)
        # Hatch / boot shut lines
        if R['rear'] in ('hatch',):
            xs=G['wb']*.70;pts=[]
            for sd in [1,-1]:
                for i in range(13):
                    t=i/12 if sd==1 else 1-i/12;x=sd*(xs+.04*t);z=b+.02-(b-.62)*t;yy=self.end_y(x,z,False);pts.append((x,yy-.009,z))
                if sd==1:
                    for i in range(1,24):
                        x=xs+.04-2*(xs+.04)*i/24;z=.62;pts.append((x,self.end_y(x,z,False)-.009,z))
            self.groove(pts)
            zh=min(.90,self.surface_z(0,yR+.08)-.06);yh=self.end_y(0,zh,False)   # (below the top of a low tail)
            if yh is not None:box('Hatch release',(0,yh-.02,zh),(.22,.04,.04),M['trim'],.016)
        elif R['rear']=='boot':
            try:   # (a low tail, as the Regale study's, can miss the probes: then no shut line)
                xs=self.surface_x(G['yr0']-.1,b-.02)-.14;yl=G['yr0']-.08;pts=[]
                zl=.66
                for i in range(9):
                    t=i/8;y=yl+(yR+.05-yl)*t;pts.append((xs,y,self.surface_z(xs,y)+.003))
                for i in range(9):
                    t=i/8;z=self.surface_z(xs,yR+.05)-(self.surface_z(xs,yR+.05)-zl)*t;y=self.end_y(xs,z,False);pts.append((xs,y-.008,z))
                for i in range(1,16):
                    x=xs-2*xs*i/16;pts.append((x,self.end_y(x,zl,False)-.008,zl))
                for i in range(9):
                    t=i/8;z=zl+(self.surface_z(-xs,yR+.05)-zl)*t;y=self.end_y(-xs,z,False);pts.append((-xs,y-.008,z))
                for i in range(9):
                    t=1-i/8;y=yl+(yR+.05-yl)*t;pts.append((-xs,y,self.surface_z(-xs,y)+.003))
                self.groove(pts,cyclic=True,width=.007)
            except TypeError:pass
            yl_=self.end_y(0,.86,False)
            if yl_ is not None:box('Boot lock',(0,yl_-.012,.86),(.08,.02,.04),M['chrome'],.01)

    def interior(self):
        M=self.M;G=self.G;yf0=G['yf0'];yr0=G['yr0'];wb=G['wb'];floor=.52
        box('Cabin carpet floor',(0,(yf0+yr0)*.5,floor),(2*wb-.3,yf0-yr0-.1,.036),M['cloth'],.025)
        for side in [-1,1]:
            if self.C:   # (under a lofted cabin the cards stop at the belt; on low cars they stood out of the shoulders)
                top=G['belt']+.03;box('Inner door card',(side*(wb-.14),(yf0+yr0)*.5,(.54+top)*.5),(.028,yf0-yr0-.2,top-.54),M['trim'],.021)
            else:box('Inner door card',(side*(wb-.14),(yf0+yr0)*.5,.79),(.028,yf0-yr0-.2,.50),M['trim'],.021)
        box('Dashboard',(0,yf0-.20,G['belt']-.02),(2*wb-.3,.30,.17),M['trim'],.055)
        fy=yf0-.78
        for side in [-1,1]:
            box('Seat pedestal',(side*.36,fy,.566),(.34,.37,.08),M['trim'],.015)
            box('Front seat cushion',(side*.36,fy,.68),(.43,.47,.17),M['cloth'],.07)
            if self.C:
                top=min(1.271,self.cab_z(.58,fy-.22)-.06);hz=min(1.311,G['roof']-.2,self.cab_z(.50,fy-.25)-.15)
                box('Front seat back',(side*.36,fy-.22,(.70+top)*.5),(.43,.18,top-.70),M['cloth'],.065,rot=(-.10,0,0))
                if hz>top-.02:box('Seat headrest',(side*.36,fy-.25,hz),(.26,.14,.20),M['cloth'],.045)
            else:
                box('Front seat back',(side*.36,fy-.22,.986),(.43,.18,.57),M['cloth'],.065,rot=(-.10,0,0))
                box('Seat headrest',(side*.36,fy-.25,min(1.311,G['roof']-.2)),(.26,.14,.20),M['cloth'],.045)
        ry=fy-.72
        # (Two-seaters, doc 65: no rear bench; a low parcel shelf under the sloping glass instead.)
        if 'two_seater' in self.R['extras']:box('Parcel shelf',(0,(fy-.4+yr0)*.5,.70),(2*wb-.4,max(.1,fy-.4-yr0),.04),M['trim'],.01)
        elif ry>yr0+.2 and self.R['rear']!='barn':
            box('Rear bench cushion',(0,ry,.655),(2*wb-.4,.31,.19),M['cloth'],.05)
            box('Rear bench back',(0,ry-.14,.935),(2*wb-.4,.15,.46),M['cloth'],.055)
        if self.R['rear']=='barn':
            box('Bulkhead',(0,fy-.42,1.1),(2*wb-.3,.04,1.0),M['trim'],.02)
            for i,(x,y,s) in enumerate([(-.3,yr0+.45,.4),(.25,yr0+.35,.35),(-.05,yr0+.8,.3)]):
                box('Parcel',(x,y,floor+s*.5+.02),(s,s*.9,s),M['cream'] if i%2==0 else M['wood'],.02)
        wc=Vector((-.36,yf0-.35,G['belt']+.02))
        def sp(a):return wc+Vector((.145*math.cos(a),.035*math.sin(a),.145*math.sin(a)))
        curve('Steering wheel',[sp(a*math.tau/32) for a in range(32)],M['trim'],.018,True)
        curve('Steering column',[wc,(-.36,wc.y+.22,wc.z-.12)],M['trim'],.03,poly=True)
        cylinder('Steering wheel hub',wc,.047,.045,M['trim'],'Y',24)
        for a in [0,math.pi,math.pi*1.5]:curve('Steering wheel spoke',[wc,sp(a)],M['trim'],.016,poly=True)
        box('Instrument binnacle',(-.36,yf0-.26,G['belt']+.08),(.28,.12,.12),M['trim'],.035)
        for xx in [-.43,-.29]:cylinder('Instrument dial',(xx,yf0-.325,G['belt']+.09),.042,.006,M['disc'],'Y',24)

    def extras(self):
        E=self.R['extras'];M=self.M;G=self.G;b=G['belt'];r=G['roof'];wr=G['wr'];yF=self.P[-1][0];yR=self.P[0][0]
        sill=self.section(0)[1]
        from mathutils.bvhtree import BVHTree
        tree=BVHTree.FromObject(self.body,bpy.context.evaluated_depsgraph_get())
        def real_x(side,y,z,facing=.3):
            # The fendered, cut shell (the analytic surface_x misses the wing bulges).
            # Outward-facing hits on this side only: through a wheel opening the ray meets the far side's
            # inner wall, which abs() used to fold back into a strip floating across the arch.
            hit,nrm,_,_=tree.ray_cast(Vector((side*3,y,z)),Vector((-side,0,0)),3)
            return abs(hit.x) if hit is not None and hit.x*side>.3 and nrm.x*side>facing else None
        def side_line(z,y0,y1,off,prof,mat,name,facing=.3):
            # Runs only where there is body: a trim line breaks at wheel arches instead of bridging them.
            for side in [-1,1]:
                run=[]
                for y in [y1+(y0-y1)*i/60 for i in range(61)]+[None]:
                    x=real_x(side,y,z,facing) if y is not None else None
                    if x is None:
                        if len(run)>=4:sweep(name,run,[Vector((side,0,0))]*len(run),prof,mat)
                        run=[]
                    else:run.append(Vector((side*(x+off),y,z)))
        if 'belt_chrome' in E:side_line(b-.035,yR+.12,yF-.12,.0,rrect(.012,.014,.006,2),M['chrome'],'Chrome belt moulding')
        if 'side_spear' in E:side_line(.66,self.ra+self.wr+.23,self.fa-self.wr-.23,.0,rrect(.014,.02,.008,2),M['chrome'],'Chrome side spear')
        if 'rubbing_strip' in E:side_line(.62,self.ra+self.wr+.23,self.fa-self.wr-.23,.0,rrect(.025,.05,.018,2),M['trim'],'Side rubbing strip')
        if 'stripe' in E:side_line(b-.1,yR+.08,yF-.08,-.004,[(0,-.035),(.008,-.035),(.008,.035),(0,.035)],M['cream'],'Painted side stripe',.85)
        if 'signwriting' in E:
            for side in [-1,1]:
                # Cream side panel band with a fish logo on the van's load area.
                y0,y1=self.ra-self.wr-.02,self.G['pillars'][0]-.1;zc=(b+r)*.5+.02
                x=side*(G['wb']+(G['wr']-G['wb'])*(zc-b)/(r-b)+.03)
                box('Signwriting panel',(x,(y0+y1)*.5,zc),(.012,(y1-y0)*.92,.30),M['cream'],.03)
                sphere('Kipper fish logo',(x+side*.01,(y0+y1)*.5+.06,zc),(.01,.20,.08),M['amber'])
                box('Kipper fish tail',(x+side*.01,(y0+y1)*.5-.17,zc),(.01,.08,.10),M['amber'],.01,rot=(.78,0,0))
                cylinder('Fish eye',(x+side*.02,(y0+y1)*.5+.18,zc+.02),.012,.01,M['trim'],'X',12)
        if 'woodie' in E:
            y0,y1=yR+.14,self.fa-self.wr-.08
            for side in [-1,1]:
                pts=[Vector((side*(self.surface_x(y,.78)+.004),y,.78)) for y in [y1+(y0-y1)*i/30 for i in range(31)]]
                sweep('Wood side panel',pts,[Vector((side,0,0))]*len(pts),[(0,-.17),(.01,-.17),(.01,.17),(0,.17)],M['wood'])
                for z in [.61,.95]:
                    pts=[Vector((side*(self.surface_x(y,z)+.008),y,z)) for y in [y1+(y0-y1)*i/30 for i in range(31)]]
                    sweep('Wood panel frame',pts,[Vector((side,0,0))]*len(pts),rrect(.016,.028,.01,2),M['cream'])
        if 'roof_rails' in E or 'roof_rack' in E:
            ya,yb=G['yr1']+.08,G['yf1']-.06
            for side in [-1,1]:
                x=side*(wr-.1);pts=[Vector((x,y,r+G['crown']*.25+.09)) for y in [ya+(yb-ya)*i/20 for i in range(21)]]
                sweep('Roof rail',pts,[Vector((side,0,0))]*len(pts),rrect(.04,.04,.018,2),M['trim'])
                for y in [ya+.02,(ya+yb)*.5,yb-.02]:box('Roof rail foot',(x,y,r+G['crown']*.25+.045),(.05,.10,.09),M['trim'],.015)
            if 'roof_rack' in E:
                for y in [ya+.35,yb-.35]:cylinder('Roof rack bar',(0,y,r+G['crown']*.25+.12),.022,2*wr-.1,M['alloy'],'X',16)
        if 'luggage' in E:
            z0=r+G['crown']*.25+.14
            for x,y,s,mat in [(-.22,G['yr1']+.55,(.50,.36,.24),M['cream']),(.26,G['yr1']+.50,(.40,.30,.20),M['red']),(.0,G['yr1']+.95,(.55,.30,.16),M['wood'])]:
                box('Roof luggage',(x,y,z0+s[2]*.5),s,mat,.04)
                box('Luggage strap',(x,y,z0+s[2]*.5),(s[0]+.01,.04,s[2]+.01),M['trim'],.01)
            box('Picnic hamper',(.24,G['yr1']+.95,z0+.1),(.34,.24,.2),M['wood'],.03)
        if 'ladder' in E:
            ya,yb=G['yr1']+.1,G['yf1']+.1
            for side in [-1,1]:
                cylinder('Ladder rail',(side*.28,(ya+yb)*.5,r+.16),.022,yb-ya,M['alloy'],'Y',16)
                for y in [ya+.1,yb-.1]:box('Roof bar leg',(side*(wr-.1),y,r+.08),(.05,.05,.16),M['trim'],.01)
            for y in [ya+.1,yb-.1]:cylinder('Roof bar',(0,y,r+.14),.02,2*wr-.12,M['trim'],'X',12)
            for k in range(9):cylinder('Ladder rung',(0,ya+.08+(yb-ya-.16)*k/8,r+.16),.013,.56,M['alloy'],'X',10)
        if 'mudflaps' in E:
            for side in [-1,1]:
                for cy in [self.fa,self.ra]:box('Mud flap',(side*(self.track+.02),cy-self.wr-.08,.22),(.22,.018,.26),M['trim'],.01)
        if 'fog_lamps' in E:
            z=self.R['bumper']['z']-.01
            for side in [-1,1]:
                y=self.end_y(.36,z)
                if y:
                    cylinder('Fog lamp bezel',(side*.36,y+self.R['bumper']['d']+.02,z+.12),.06,.05,M['trim'],'Y',24)
                    sphere('Fog lamp lens',(side*.36,y+self.R['bumper']['d']+.045,z+.12),(.05,.018,.05),M['amber'])
        if 'spoiler' in E:
            y=G['yr1']-.02;zr=pchip(self.C['roofline'],y-.06) if self.C else r
            box('Roof spoiler',(0,y-.06,zr-.02),(2*wr-.06,.2,.045),M['paint'],.02,rot=(-.18,0,0))
        if 'louvres' in E:
            q=inset_quad([(G['wb'],G['yr0'],b),(-G['wb'],G['yr0'],b),(-G['wr'],G['yr1'],r),(G['wr'],G['yr1'],r)],.07)
            up=(Vector(q[3])-Vector(q[0])).normalized();nrm=(Vector(q[1])-Vector(q[0])).cross(up).normalized()
            if nrm.z<0:nrm=-nrm
            tilt=math.atan2(up.z,-up.y)
            for k in range(8):
                t=(k+.5)/8;a=Vector(q[0]).lerp(Vector(q[3]),t);c=Vector(q[1]).lerp(Vector(q[2]),t)
                box('Rear window louvre',(a+c)*.5+nrm*.03,((a-c).length,.07,.012),M['trim'],.004,rot=(-tilt+.25,0,0))
            for sd in [-1,1]:
                a=Vector(q[0 if sd>0 else 1]);c=Vector(q[3 if sd>0 else 2]);box('Louvre side rail',(a+c)*.5+nrm*.025,(.03,(c-a).length,.02),M['trim'],.006,rot=(-tilt,0,0))
        if 'taxi_sign' in E:
            z0=r+G['crown']*.25;y=(G['yf1']+G['yr1'])*.5
            box('Taxi sign',(0,y,z0+.13),(.62,.22,.2),M['cream'],.04)
            for sd in[-1,1]:box('Taxi sign face',(0,y+sd*.115,z0+.13),(.5,.01,.12),M['amber'],.01)
            box('Taxi sign base',(0,y,z0+.02),(.5,.18,.04),M['trim'],.01)
            side_line(.74,self.ra+self.wr+.23,self.fa-self.wr-.23,-.004,[(0,-.045),(.008,-.045),(.008,.045),(0,.045)],M['trim'],'Taxi chequer band')
        if 'cone' in E:
            z0=r+G['crown']*.25;y=(G['yf1']+G['yr1'])*.5-.1
            bpy.ops.mesh.primitive_cone_add(vertices=20,radius1=.02,radius2=.26,depth=.62,location=(0,y,z0+.36));o=bpy.context.object;finish(o,'Giant cone',M['wood'])
            for k,(dz,sc,mat) in enumerate([(.72,.30,M['cream']),(.94,.24,M['livery']),(1.1,.14,M['cream'])]):sphere('Ice cream scoop',(0,y,z0+dz),(sc,sc,sc*.9),mat)
            cylinder('Cherry',(0,y,z0+1.24),.05,.08,M['red'],'Z',12)
            for sd in[-1,1]:
                x=sd*(G['wb']+.02);y0,y1=self.ra-self.wr,self.G['pillars'][0]-.12;zc=(b+r)*.5
                box('Serving hatch',(x,(y0+y1)*.5,zc+.05),(.02,(y1-y0)*.62,.42),M['cream'],.03)
                box('Hatch awning',(x+sd*.14,(y0+y1)*.5,zc+.32),(.3,(y1-y0)*.66,.03),M['livery'],.01,rot=(0,sd*.35,0))
        if 'poptop' in E:
            z0=r+G['crown']*.25;ya,yb=G['yr1']+.2,G['yf1']-.1
            box('Pop-top roof',(0,(ya+yb)*.5,z0+.28),(2*wr-.06,yb-ya,.1),M['cream'],.04,rot=(-.06,0,0))
            box('Pop-top canvas',(0,(ya+yb)*.5-.06,z0+.13),(2*wr-.12,yb-ya-.1,.24),M['canvas'],.02)
            for sd in[-1,1]:box('Canvas window',(sd*(wr-.06),(ya+yb)*.5,z0+.14),(.01,.5,.12),M['glass'],.01)
        if 'post_horn' in E:
            for sd in[-1,1]:
                y0,y1=self.ra-self.wr-.02,self.G['pillars'][0]-.1;zc=(b+r)*.5+.02
                x=sd*(G['wb']+(G['wr']-G['wb'])*(zc-b)/(r-b)+.03)
                cylinder('Post horn',(x,(y0+y1)*.5,zc),.16,.012,M['amber'],'X',24);cylinder('Post horn hole',(x+sd*.008,(y0+y1)*.5,zc),.08,.012,M['paint'],'X',20)
                box('Post horn bell',(x,(y0+y1)*.5+.22,zc-.06),(.012,.12,.1),M['amber'],.02)
                box('Mail stripe',(x,(y0+y1)*.5,zc-.3),(.012,(y1-y0)*.95,.06),M['amber'],.01)
        if 'splitter' in E:
            B=self.R['bumper'];p,nm=self.outline(B['z']-B['h']*.5-.02,True,.0,span=90)
            sweep('Front splitter',p,nm,[(a,b) for a,b in rrect(self.R.get('splitter_d',.10),.025,.01,2)],M['trim'])
        if 'skirts' in E:side_line(sill+.12,self.ra+self.wr+.23,self.fa-self.wr-.23,.0,rrect(.05,.10,.035,3),M['paint'],'Side skirt')
        if 'wing' in E:
            yw=yR+.22;zd=self.surface_z(.5,yw);h=.26 if self.R['rear']=='boot' else .20
            for side in [-1,1]:box('Wing upright',(side*.52,yw,zd+h*.5),(.05,.16,h),M['trim'],.015)
            box('Rear wing aerofoil',(0,yw-.02,zd+h+.02),(2*G['wb']+.12,.34,.05),M['paint'],.022,rot=(.08,0,0))
            for side in [-1,1]:box('Wing endplate',(side*(G['wb']+.07),yw-.02,zd+h+.02),(.02,.38,.16),M['trim'],.01)
        if 'popup_lids' in E:
            # Pop-up headlamps closed (the RX-7's): a lid on each wing ahead of the screen, standing a few mm proud
            # inside a shut line (doc 66).
            for side in [-1,1]:
                xl=.56;yl=self.fa+.42;n=6;v=[];f=[]
                for i_ in range(n+1):
                    for j_ in range(n+1):
                        dx=-.12+.24*j_/n;dy=-.15+.29*i_/n;x=side*(xl+dx);v.append((x,yl+dy,self.top_z(x,yl+dy)+.006))
                for i_ in range(n):
                    for j_ in range(n):k=i_*(n+1)+j_;f.append((k,k+1,k+n+2,k+n+1))
                o=mesh('Pop-up headlamp lid',v,f,M['paint']);m=o.modifiers.new('Lid thickness','SOLIDIFY');m.thickness=.012;bevel(o,.005,2)
                pts=[]
                for (dx,dy) in [(-.135,-.165),(.135,-.165),(.135,.155),(-.135,.155)]:x=side*(xl+dx);pts.append((x,yl+dy,self.top_z(x,yl+dy)+.002))
                pts.append(pts[0]);self.groove(pts,width=.007)
        if 'swept_lamps' in E:
            # Headlamps swept back up the wings (the Z's, doc 66): a long tapering lens lying on the wing in a dark
            # surround, wide at the nose and narrowing up the wing.
            for side in [-1,1]:
                n=10;rows=[]
                for k in range(n+1):
                    t=k/n;yy=yF-.07-.58*t;xc=.64-.10*t;hw=.065-.035*t
                    rows.append([(side*(xc+d),yy) for d in (-hw,hw)])
                for label,grow,lift,mat,th in (('Swept headlamp surround',.022,.006,M['trim'],.012),('Swept headlamp lens',0,.016,M['lamp'],.014)):
                    v=[];f=[]
                    for k,r_ in enumerate(rows):
                        for (x,yy),sg in zip(r_,(-1,1)):
                            x=x+side*sg*grow;v.append((x,yy+(grow if k==0 else 0),self.top_z(x,yy)+lift))
                    for k in range(n):f.append((2*k,2*k+1,2*k+3,2*k+2))
                    o=mesh(label,v,f,mat);m=o.modifiers.new('Lens thickness','SOLIDIFY');m.thickness=th;bevel(o,.006,2)
        if 'hoop_wing' in E:
            # The A80's hoop wing (doc 65): a low blade on two legs rising from the rear quarters, close over the boot.
            yw=yR+.30;zq=self.surface_z(.70,yw)
            if zq is not None:
                for side in [-1,1]:curve('Hoop wing leg',[(side*.72,yw,zq-.02),(side*.70,yw-.04,zq+.12),(side*.64,yw-.06,zq+.19)],M['paint'],.035)
                box('Hoop wing blade',(0,yw-.07,zq+.21),(1.36,.26,.035),M['paint'],.015,rot=(-.10,0,0))
        if 'side_scoop' in E:
            from mathutils.bvhtree import BVHTree
            if not hasattr(self,'_bvh'):self._bvh=BVHTree.FromObject(self.body,bpy.context.evaluated_depsgraph_get())
            for side in [-1,1]:
                y0,y1=self.ra+self.wr+.12,self.ra+self.wr+.50;z0,z1=G['belt']-.25,G['belt']-.05;n=8;v=[];f=[]
                for i in range(n+1):
                    for j in range(n+1):
                        t=j/n;z=z0+(z1-z0)*t;y=y0+(y1-y0)*i/n;y=y1-(y1-y)*(.55+.45*t)   # (a wedge: longer at the top)
                        hit,_,_,_=self._bvh.ray_cast(Vector((side*2,y,z)),Vector((-side,0,0)),2)
                        v.append((hit.x+side*.006,y,z) if hit is not None else (side*self.surface_x(y,z),y,z))
                for i in range(n):
                    for j in range(n):k=i*(n+1)+j;f.append((k,k+1,k+n+2,k+n+1))
                o=mesh('Side air intake',v,f,M['trim']);m=o.modifiers.new('Intake thickness','SOLIDIFY');m.thickness=.010;bevel(o,.004,2)
                for k in range(3):
                    zz=z0+(z1-z0)*(k+1)/4;ya=y1-(y1-y0)*(.55+.45*(k+1)/4)+.03
                    hit,_,_,_=self._bvh.ray_cast(Vector((side*2,(ya+y1)*.5,zz)),Vector((-side,0,0)),2)
                    if hit is not None:box('Intake blade',(hit.x+side*.016,(ya+y1)*.5,zz),(.012,y1-ya-.04,.012),M['insert'],.004)
        if 'tail_panel' in E:
            # A full-width dark panel across the tail, the lamps set into it (doc 65).
            T=self.R['tail'];yp=self.end_y(0,T['z'],False)
            if yp is not None:box('Tail lamp panel',(0,yp+.012,T['z']),(2*(T.get('x2',T['x'])+.15),.03,T.get('r',T.get('h',.1)*.5)*2+.08),M['trim'],.02)
        if 'ducktail' in E:
            y=yR+.12;box('Ducktail lip',(0,y,self.surface_z(0,y)+.03),(2*(self.surface_x(y,self.section(y)[2]-.05)-.1),.22,.05),M['paint'],.02,rot=(.30,0,0))
        # ---- The race ladder (doc 65: a box car climbing from C to S, Richard 6 Oct). Each tier adds on the last.
        if 'roof_wing' in E or 'gt_wing' in E:
            # A wing on the roof's rear edge (box cars have no deck): a small one for A, a towering swan-neck one for S.
            big='gt_wing' in E;r=G['roof'];wr=G['wr'];y=G['yr1']+(.04 if big else .08)
            h=.32 if big else .10;span=2*wr+(.44 if big else .06);chord=.44 if big else .28;zw=r+G['crown']*.2+h
            for side in [-1,1]:
                xs=side*wr*.5
                if big:curve('Swan-neck wing mount',[(xs,y+.10,r+.01),(xs,y+.02,r+h*.55),(xs,y-.10,zw+.05)],M['trim'],.022)
                else:box('Roof wing upright',(xs,y,r+h*.5),(.04,.10,h),M['trim'],.01)
            box('Roof wing aerofoil',(0,y-.08,zw),(span,chord,.045),M['paint'],.02,rot=(.12 if big else .06,0,0))
            for side in [-1,1]:box('Roof wing endplate',(side*(span*.5+.012),y-.08,zw-.03),(.022,chord+.10,.22 if big else .12),M['trim'],.01)
            if big:box('Wing gurney flap',(0,y-.08-chord*.5+.01,zw+.035),(span-.02,.014,.045),M['trim'],.004)
        if 'canards' in E:
            # Dive planes on the front corners.
            B=self.R['bumper']
            for side in [-1,1]:
                for k,dz in enumerate((.0,.09)):
                    z=B['z']+dz;y=self.end_y(.60,z)
                    if y is None:continue
                    box('Front canard',(side*.64,y+.05,z),(.20,.12,.014),M['trim'],.004,rot=(0,side*.28,side*.15))
        if 'diffuser' in E:
            # A rear diffuser: an undertray with fins, out from under the tail.
            yd=yR+.10;box('Rear diffuser tray',(0,yd-.02,.30),(1.25,.36,.022),M['trim'],.006)
            for k in range(5):box('Diffuser fin',(-.48+.24*k,yd-.06,.36),(.016,.30,.13),M['trim'],.004)
        if 'roof_scoop' in E:
            # An airbox scoop on the roof (the Espace F1's), feeding the engine behind the seats.
            r=G['roof'];yc=G['yf1']-.55;z=r+G['crown']*.9
            box('Roof air scoop',(0,yc,z+.07),(.40,.56,.14),M['paint'],.045)
            box('Roof scoop mouth',(0,yc+.27,z+.09),(.30,.03,.08),M['trim'],.012)
        if 'side_exhaust' in E:
            # Twin side pipes ahead of the rear wheels.
            sill=self.section(0)[1]
            for side in [-1,1]:
                for k in range(2):
                    y=self.ra+self.wr+.22+k*.10;z=sill+.05;x=side*(self.surface_x(y,z)+.035)
                    cylinder('Side exhaust pipe',(x,y,z),.034,.10,M['chrome'],'X',20)
                    cylinder('Side exhaust bore',(x+side*.052,y,z),.024,.006,M['trim'],'X',20,0)
        if 'roundel' in E:
            # A door roundel with the race number in seven-segment digits.
            num=str(self.R.get('race_number',7));zd=G['belt']-.2
            yd=(G['pillars'][0]+min(G['yf0'],self.fa-self.wr))*.5-.05 if G['pillars'] else (self.fa+self.ra)*.5
            SEG={'0':'abcdef','1':'bc','2':'abged','3':'abgcd','4':'fgbc','5':'afgcd','6':'afgedc','7':'abc','8':'abcdefg','9':'abcdfg'}
            for side in [-1,1]:
                x=side*(self.surface_x(yd,zd)+.006)
                cylinder('Number roundel',(x,yd,zd),.16,.012,M['cream'],'X',40,0)
                cylinder('Roundel ring',(x-side*.002,yd,zd),.175,.01,M['trim'],'X',40,0)
                for i,ch in enumerate(num):
                    cy=yd+side*(i-(len(num)-1)*.5)*.10;xs=x+side*.009;w,h=.035,.06
                    pos={'a':(0,h,1),'g':(0,0,1),'d':(0,-h,1),'b':(w,h*.5,0),'c':(w,-h*.5,0),'f':(-w,h*.5,0),'e':(-w,-h*.5,0)}
                    for sg in SEG.get(ch,''):
                        dy,dz,horiz=pos[sg]
                        box('Race number segment',(xs,cy+side*dy,zd+dz),(.01,.07 if horiz else .016,.016 if horiz else .062),M['trim'],.003)
        if 'wheel_tubs' in E and 'bed' in E:
            # Wheel tubs: dark liners rising from the bed floor over each rear wheel (the tyres showed inside the bed).
            w,lo,hi=self.section(self.ra)
            R_=self.wr+.07;n=24
            for side in [-1,1]:   # (a half-round arch over the wheel only: a full cylinder showed under the tail)
                xa,xb=side*(w-.08-.34),side*(w-.08);v=[];f=[]
                for i in range(n+1):
                    a=math.pi*i/n;y=self.ra+R_*math.cos(a);z=self.wr+R_*math.sin(a);v+=[(xa,y,z),(xb,y,z)]
                for i in range(n):f.append((2*i,2*i+1,2*i+3,2*i+2))
                f.append(tuple(range(0,2*n+2,2)));f.append(tuple(range(2*n+1,0,-2)))
                bevel(mesh('Bed wheel tub',v,f,M['trim']),.015,2)
        if 'jump_seats' in E and 'bed' in E:
            # Two rear-facing jump seats bolted in the load bed (the BRAT's), each with a grab handle.
            yb0=self.P[0][0]+.13;yb1=G['yr0']-.12;w,lo,hi=self.section((yb0+yb1)*.5);fl=lo+.30
            for side in [-1,1]:
                x=side*.30;yc=yb1-.30
                box('Jump seat cushion',(x,yc,fl+.16),(.36,.32,.10),M['cloth'],.03)
                box('Jump seat back',(x,yb1-.10,fl+.38),(.36,.07,.40),M['cloth'],.03)
                box('Jump seat frame',(x,yc,fl+.07),(.30,.26,.10),M['trim'],.01)
                curve('Jump seat grab handle',[(x-side*.16,yb1-.14,fl+.56),(x-side*.16,yb1-.30,fl+.62),(x-side*.16,yb1-.44,fl+.40)],M['chrome'],.014)
        if 'skid_plate' in E:
            # An alloy skid plate under the nose (rally raid), with bolt heads.
            # Tucked in under the bumper and sloping back beneath the car (out in front it read as a loose ramp).
            B=self.R['bumper'];zf=B['z']-B['h']*.5-.01;yf=self.end_y(0,zf+.04) or yF;a=.30
            box('Alloy skid plate',(0,yf-.04-.2*math.cos(a),zf-.2*math.sin(a)),(.82,.40,.022),M['alloy'],.008,rot=(a,0,0))
        if 'race_stripes' in E:
            # Twin go-faster stripes down the centre: bonnet, roof and boot (doc 65's kit experiments).
            sm=material('StripeColour',self.R.get('stripe_colour',(.92,.92,.88)),.1,.35);r=G['roof'];wr=G['wr']
            def strip(pts,width):
                sweep('Go-faster stripe',[Vector(q)+Vector((0,0,.004)) for q in pts],[Vector((0,0,1))]*len(pts),   # (thickness up, width across)
                      [(0,-width*.5),(.005,-width*.5),(.005,width*.5),(0,width*.5)],sm,[Vector((1,0,0))]*len(pts))
            for x in (-.11,.11):
                strip([(x,y,self.top_z(x,y)) for y in [yF-.06+(G['yf0']+.08-(yF-.06))*i/24 for i in range(25)]],.13)
                if not self.C:
                    strip([(x,y,r+G['crown']*(1-(x/wr)**2)*math.sin(math.pi*(y-G['yr1'])/(G['yf1']-G['yr1']))+.012)
                           for y in [G['yr1']+.05+(G['yf1']-G['yr1']-.10)*i/16 for i in range(17)]],.13)
                if G['yr0']-.08>yR+.16 and 'bed' not in E:strip([(x,y,self.top_z(x,y)) for y in [yR+.06+(G['yr0']-.08-yR-.06)*i/12 for i in range(13)]],.13)
        if 'bonnet_vents' in E:
            # Two louvred vents let into the bonnet (doc 65's kit experiments).
            yc=yF-(yF-G['yf0'])*.42
            for side in [-1,1]:
                xc=side*.30;n=5;v=[];f=[]
                for i_ in range(n+1):
                    for j_ in range(n+1):
                        x=xc-.11+.22*j_/n;y=yc-.12+.24*i_/n;v.append((x,y,self.top_z(x,y)+.005))
                for i_ in range(n):
                    for j_ in range(n):k=i_*(n+1)+j_;f.append((k,k+1,k+n+2,k+n+1))
                o=mesh('Bonnet vent',v,f,M['trim']);m=o.modifiers.new('Vent thickness','SOLIDIFY');m.thickness=.008;bevel(o,.004,2)
                for k in range(5):
                    y=yc-.09+.045*k;box('Vent louvre',(xc,y,self.top_z(xc,y)+.014),(.19,.012,.010),M['paint'],.004,rot=(.35,0,0))
        if 'scoop' in E:
            y=(yF+G['yf0'])*.5;z=self.surface_z(0,y)
            box('Bonnet scoop',(0,y,z+.03),(.40,.34,.09),M['paint'],.035)
            box('Bonnet scoop mouth',(0,y+.165,z+.05),(.32,.02,.05),M['trim'],.01)
        if 'spots' in E:
            B=self.R['bumper']
            for x in [-.5,-.18,.18,.5]:
                y=self.end_y(abs(x),B['z'])
                if y is None:continue
                y+=B['d']+.02;z=B['z']+B['h']*.5+.08
                box('Spot lamp bracket',(x,y-.04,z-.07),(.03,.03,.1),M['trim'],.008)
                cylinder('Spot lamp housing',(x,y,z),.075,.08,M['chrome'],'Y',24)
                sphere('Spot lamp lens',(x,y+.042,z),(.064,.02,.064),M['lamp'])
                cylinder('Spot lamp cover strap',(x,y+.05,z),.068,.006,M['trim'],'Y',16,r=0)
        if 'roof_vent' in E:box('Roof vent',(0,G['yf1']-.35,r+G['crown']*.9+.02),(.22,.18,.04),M['trim'],.015)
        if 'spare_on_roof' in E:
            # Seated on the crowned roof panel (was floating 16 cm above it), on a low clamp plate.
            z0=r+G['crown']+.02;yc=(G['yr1']+G['yf1'])*.5-.1
            box('Spare clamp plate',(0,yc,z0-.005),(.34,.34,.03),M['trim'],.008)
            cylinder('Spare tyre',(0,yc,z0+.06),.26,.12,M['rubber'],'Z',32,.03)
            cylinder('Spare wheel',(0,yc,z0+.07),.17,.12,M['steel'],'Z',24,0)
            box('Spare strap',(0,yc,z0+.13),(.04,.56,.02),M['trim'],.005)
        if 'bull_bar' in E:
            B=self.R['bumper'];yb=self.end_y(.3,B['z']) or yF;y=yb+B['d']+.12;z0=B['z']-.05
            for x in [-.42,.42]:cylinder('Bull bar upright',(x,y,z0+.32),.035,.64,M['trim'],'Z',16,.01)
            pts=[(-.42,y,z0+.64),(-.30,y+.03,z0+.76),(0,y+.04,z0+.80),(.30,y+.03,z0+.76),(.42,y,z0+.64)]
            curve('Bull bar hoop',pts,M['trim'],.035)
            cylinder('Bull bar cross tube',(0,y,z0+.36),.03,.84,M['trim'],'X',16,.01)
            for x in [-.42,.42]:box('Bull bar mount',(x,y-.08,z0+.12),(.06,.16,.06),M['trim'],.01)
        if 'snorkel' in E:
            # Up the outside of the wing just ahead of the windscreen, its head at the roof line.
            yw=G['yf0']+.10;x=self.surface_x(yw,G['belt']-.1)+.07;zt=r-.08
            cylinder('Snorkel body',(x,yw,(G['belt']-.15+zt)*.5),.055,zt-(G['belt']-.15),M['trim'],'Z',16,.01)
            box('Snorkel head',(x,yw,zt+.06),(.12,.16,.12),M['trim'],.02)
        if 'spare_rear' in E:
            z=.95;yt=self.end_y(0,z,False) or yR;y=yt-.13
            cylinder('Rear spare tyre',(0,y,z),.36,.20,M['rubber'],'Y',32,.04)
            cylinder('Rear spare wheel',(0,y-.02,z),.22,.21,M['steel'],'Y',24,0)
            box('Spare carrier',(0,yt-.02,z),(.18,.06,.5),M['trim'],.01)
            box('Spare cover badge',(0,y-.11,z),(.16,.01,.16),M['paint'],.01)
        if 'light_bar' in E:
            yb=G['yf1']-.12;zb=r+G['crown']*.6+.13
            cylinder('Light bar tube',(0,yb,zb),.03,2*wr-.12,M['trim'],'X',16,.01)
            for x in [-.48,-.16,.16,.48]:
                cylinder('Roof spot housing',(x,yb+.06,zb+.09),.075,.08,M['trim'],'Y',24)
                sphere('Roof spot lens',(x,yb+.105,zb+.09),(.064,.02,.064),M['lamp'])
            for side in [-1,1]:box('Light bar leg',(side*(wr-.12),yb,zb-.07),(.05,.05,.14),M['trim'],.01)
        if 'side_steps' in E:
            for side in [-1,1]:
                y0,y1=self.ra+self.wr+.18,self.fa-self.wr-.18
                box('Side step',(side*(self.section(0)[0]+.04),(y0+y1)*.5,sill+.02),(.16,y1-y0,.05),M['trim'],.01)
                for y in [y0+.1,y1-.1]:box('Side step bracket',(side*(self.section(0)[0]-.02),y,sill+.07),(.06,.05,.1),M['trim'],.005)
        if 'bed' in E:
            yb0=yR+.13;yb1=G['yr0']-.12
            # A roll hoop behind the cab and rails along the bed sides.
            xh=self.section(yb1)[0]-.12;zt=self.section(yb1)[2]
            pts=[(-xh,yb1-.08,zt),(-xh+.06,yb1-.08,zt+.42),(xh-.06,yb1-.08,zt+.42),(xh,yb1-.08,zt)]
            curve('Roll hoop',pts,M['trim'],.035,poly=True)
            for side in [-1,1]:
                w,lo,hi=self.section((yb0+yb1)*.5);x=side*(w-.06)
                cylinder('Bed rail',(x,(yb0+yb1)*.5,hi+.05),.02,yb1-yb0-.1,M['alloy'],'Y',12,0)
        if 'tow_crane' in E:
            # An A-frame crane at the back of the bed, its boom out over the tail with a hook on a chain, a winch drum,
            # and a wheel-lift bar under the rear bumper.
            yb0=yR+.13;w,lo,hi=self.section(yb0+.3);z0=hi+.02
            for side in [-1,1]:curve('Crane leg',[(side*(w-.14),yb0+.55,z0),(side*.12,yb0+.18,z0+1.05)],M['amber'],.045,poly=True)
            curve('Crane boom',[(0,yb0+.55,z0+.62),(0,yb0+.12,z0+1.08),(0,yR-.42,z0+.86)],M['amber'],.05,poly=True)
            cylinder('Winch drum',(0,yb0+.62,z0+.12),.11,.46,M['steel'],'X',20)
            for k in range(6):sphere('Tow chain link',(0,yR-.42,z0+.78-k*.07),(.018,.018,.03),M['steel'],8,6)
            curve('Tow hook',[(0,yR-.42,z0+.36),(0,yR-.48,z0+.30),(0,yR-.42,z0+.24),(0,yR-.36,z0+.30)],M['steel'],.02)
            box('Wheel-lift bar',(0,yR-.28,.32),(1.2,.10,.08),M['amber'],.02)
            for side in [-1,1]:box('Wheel-lift cradle',(side*.48,yR-.38,.32),(.16,.22,.06),M['steel'],.015)
        if 'roof_tyres' in E:
            # Spare tyres stacked flat on the rack (the parts van).
            z0=r+G['crown']*.25+.14
            for k,(y,stack) in enumerate([(G['yr1']+.45,3),(G['yr1']+1.15,2)]):
                for j in range(stack):
                    cylinder('Roof tyre',(0,y,z0+.06+j*.13),.30,.12,M['rubber'],'Z',28,.03)
                    cylinder('Roof tyre rim',(0,y,z0+.07+j*.13),.16,.122,M['alloy'],'Z',20,0)
            for side in [-1,1]:box('Rack strap',(side*.16,G['yr1']+.8,z0+.22),(.04,1.2,.02),M['amber'],.005)
        if 'panel_rack' in E:
            # An A-frame rack on the bed with fresh body panels leaning on it, each a different colour (the body shop).
            yb0=yR+.12;yb1=G['yr0']-.1;w,lo,hi=self.section((yb0+yb1)*.5);z0=hi+.02;ym=(yb0+yb1)*.5
            for y in [yb0+.12,yb1-.12]:curve('Rack frame',[(-.5,y,z0),(0,y,z0+.95),(.5,y,z0)],M['steel'],.03,poly=True)
            cylinder('Rack ridge',(0,ym,z0+.95),.03,yb1-yb0-.2,M['steel'],'Y',12)
            for k,(side,mat) in enumerate([(-1,M['livery']),(1,M['cream']),(-1,M['amber']),(1,M['paint'])]):
                off=.06*(k//2)
                box('Body panel',(side*(.28+off),ym+(k-1.5)*.08,z0+.48),(.03,yb1-yb0-.45,.80),mat,.03,rot=(0,side*-.5,0))
        if 'pit_badge' in E:
            # A door roundel: spanner (garage), tyre (parts) or paint drop (body shop).
            yd=(G['pillars'][0]+min(G['yf0'],self.fa-self.wr))*.5-.05;zd=G['belt']-.22;kind=self.R.get('badge','spanner')
            for side in [-1,1]:
                x=side*(self.surface_x(yd,zd)+.006)
                cylinder('Pit badge',(x,yd,zd),.16,.012,M['cream'],'X',40,0);cylinder('Pit badge ring',(x-side*.002,yd,zd),.175,.01,M['trim'],'X',40,0)
                xs=x+side*.009
                if kind=='spanner':
                    box('Spanner shaft',(xs,yd,zd),(.01,.2,.035),M['trim'],.004,rot=(.78,0,0))
                    for s2 in[-1,1]:cylinder('Spanner jaw',(xs,yd+s2*.075,zd+s2*.075),.04,.012,M['trim'],'X',16,0)
                elif kind=='tyre':
                    cylinder('Badge tyre',(xs,yd,zd),.10,.012,M['trim'],'X',28,0);cylinder('Badge hub',(xs+side*.004,yd,zd),.045,.012,M['amber'],'X',20,0)
                else:
                    sphere('Paint drop',(xs,yd,zd-.02),(.01,.06,.06),M['livery'],12,8);box('Paint drop tip',(xs,yd,zd+.05),(.01,.05,.05),M['livery'],.01,rot=(.78,0,0))
        if 'livery' in E:self.livery()
        if 'antenna' in E:
            y=self.fa-.1;x=-.66;z=self.surface_z(x,y)
            curve('Radio antenna',[(x,y,z),(x,y-.12,z+.62)],M['chrome'],.005,poly=True)
            sphere('Antenna tip',(x,y-.12,z+.62),(.012,.012,.012),M['chrome'],12,6)

    def livery(self):
        """Flagship race livery: twin stripes nose-to-tail, side flash and door number."""
        M=self.M;G=self.G;yF=self.P[-1][0];yR=self.P[0][0];r=G['roof'];wr=G['wr']
        def strip(name,pts,width,mat,lift=.004):
            ups=[Vector((1,0,0))]*len(pts);nrm=[Vector((0,0,1))]*len(pts)
            sweep(name,[Vector(p)+Vector((0,0,lift)) for p in pts],nrm,[(0,-width*.5),(.004,-width*.5),(.004,width*.5),(0,width*.5)],mat,ups)
        for x in [-.13,.13]:
            bonnet=[(x,y,self.surface_z(x,y)) for y in [yF-.04+(G['yf0']+.06-(yF-.04))*i/20 for i in range(21)]]
            roof=[(x,y,r+G['crown']*(1-(x/wr)**2)*math.sin(math.pi*(y-G['yr1'])/(G['yf1']-G['yr1']))+.012) for y in [G['yr1']+.04+(G['yf1']-G['yr1']-.08)*i/16 for i in range(17)]]
            deck=[(x,y,self.surface_z(x,y)) for y in [yR+.05+(G['yr0']-.06-yR-.05)*i/8 for i in range(9)]]
            for label,pts in [('Bonnet stripe',bonnet),('Roof stripe',roof),('Deck stripe',deck)]:
                strip(label+' red edge',pts,.17,M['livery'],.003);strip(label,pts,.13,M['cream'],.006)
        for side in [-1,1]:
            y0,y1=self.ra+self.wr+.08,self.fa-self.wr-.08
            for z,wdt,mat,off in [(.62,.10,M['livery'],.004),(.72,.04,M['amber'],.004),(.77,.02,M['cream'],.004)]:
                pts=[Vector((side*(self.surface_x(y,z)+off),y,z+(y-y0)*.04)) for y in [y1+(y0-y1)*i/30 for i in range(31)]]
                sweep('Side flash',pts,[Vector((side,0,0))]*len(pts),[(0,-wdt*.5),(.006,-wdt*.5),(.006,wdt*.5),(0,wdt*.5)],mat)
            # Door roundel with the number 1.
            yd=(G['pillars'][0]+min(G['yf0'],self.fa-self.wr))*.5-.05;zd=G['belt']-.2;x=side*(self.surface_x(yd,zd)+.006)
            cylinder('Number roundel',(x,yd,zd),.15,.012,M['cream'],'X',40,0)
            cylinder('Roundel ring',(x-side*.002,yd,zd),.165,.01,M['livery'],'X',40,0)
            xs=x+side*.009
            box('Number one stem',(xs,yd,zd-.005),(.01,.045,.19),M['trim'],.004)
            box('Number one flag',(xs,yd-side*.035,zd+.06),(.01,.07,.035),M['trim'],.004,rot=(-.6*side,0,0))
            box('Number one foot',(xs,yd,zd-.10),(.01,.12,.03),M['trim'],.004)
            # Sponsor patches on the front wing.
            yw=self.fa+.05;zw=G['belt']-.1
            box('Sponsor patch',(side*(self.surface_x(yw,zw)+.004),yw,zw),(.01,.24,.07),M['amber'],.01)
            box('Sponsor lettering',(side*(self.surface_x(yw,zw)+.01),yw,zw),(.01,.18,.02),M['trim'],.004)

    # Wheels -----------------------------------------------------------
    def wheel(self,side,cy,name):
        W=self.R['wheel'];M=self.M;s=self.wr/.425;cx=side*self.track;cz=self.wr
        before=set(bpy.context.scene.objects)
        road=W.get('tyre') in ('road','slick');self.rimk=1.13 if road else 1.0
        prof=[(-.144,.303),(-.164,.340),(-.164,.378),(-.144,.406),(-.112,.422),(-.075,.425),(-.069,.416),(-.057,.416),(-.051,.425),
              (.051,.425),(.057,.416),(.069,.416),(.075,.425),(.112,.422),(.144,.406),(.164,.378),(.164,.340),(.144,.303)]
        if road:   # (doc 66: a road-car low profile, square shoulders, two grooves; no tread blocks)
            prof=[(-.150,.338),(-.168,.360),(-.168,.395),(-.156,.416),(-.128,.425),(-.060,.425),(-.054,.417),(-.040,.417),(-.034,.425),
                  (.034,.425),(.040,.417),(.054,.417),(.060,.425),(.128,.425),(.156,.416),(.168,.395),(.168,.360),(.150,.338)]
        if W.get('tyre')=='slick':   # (class S racing: a smooth crown, no grooves)
            prof=[(-.150,.338),(-.168,.360),(-.168,.395),(-.156,.416),(-.128,.425),(.128,.425),(.156,.416),(.168,.395),(.168,.360),(.150,.338)]
        wv=[];wf=[];n=40
        for u,rr in prof:
            for k in range(n):a=k*math.tau/n;wv.append((cx+u*s*.95*W.get('width',1),cy+rr*s*math.sin(a),cz+rr*s*math.cos(a)))
        for j in range(len(prof)):
            for k in range(n):wf.append((j*n+k,j*n+(k+1)%n,((j+1)%len(prof))*n+(k+1)%n,((j+1)%len(prof))*n+k))
        mesh('Profiled tyre',wv,wf,M['rubber'])
        # Tread blocks on the crown (off-roaders: big staggered knobbly blocks that stand proud of the tyre).
        if road:pass
        elif W.get('tread')=='knobbly':
            ww=W.get('width',1)
            for k in range(26):
                a=k*math.tau/26
                for u in ([-.095,.03] if k%2==0 else [-.03,.095]):
                    box('Knobbly tread block',(cx+u*s*ww,cy+.428*s*math.sin(a),cz+.428*s*math.cos(a)),(.075*s*ww,.06,.026),M['rubber'],.006,rot=(-a,0,0))
                for sd in [-1,1]:box('Shoulder lug',(cx+sd*.15*s*ww,cy+.41*s*math.sin(a+.12),cz+.41*s*math.cos(a+.12)),(.03,.05,.03),M['rubber'],.005,rot=(-a,0,0))
        else:
            for k in range(36):
                a=k*math.tau/36
                for u in [-.07,.07]:
                    box('Tread block',(cx+u*s,cy+.424*s*math.sin(a),cz+.424*s*math.cos(a)),(.09*s,.028,.008),M['rubber'],0,rot=(-a,0,0))
        outer=cx+side*.149*s
        def torus(label,r,minor,x,mat,seg=40):
            tv=[];tf=[]
            for a in range(seg):
                th=a*math.tau/seg
                for bb in range(8):
                    ph=bb*math.tau/8;rad=r+minor*math.cos(ph);tv.append((x+minor*math.sin(ph),cy+rad*math.sin(th),cz+rad*math.cos(th)))
            for a in range(seg):
                for bb in range(8):tf.append((a*8+bb,((a+1)%seg)*8+bb,((a+1)%seg)*8+(bb+1)%8,a*8+(bb+1)%8))
            return mesh(label,tv,tf,mat)
        cylinder('Brake disc',(cx+side*.066*s,cy,cz),.248*s,.025,M['disc'],vertices=28,r=0)
        k_=self.rimk
        cylinder('Rim barrel',(cx+side*.087*s,cy,cz),.305*s*k_,.09,M['trim'],vertices=32)
        torus('Tyre sidewall bead',.330*s*k_,.004,cx+side*.160*s,M['rubber'])
        style=W['style']
        if W.get('whitewall'):
            av=[];af=[];ns=56
            for k in range(ns):
                a=k*math.tau/ns
                for rr in [.318*s,.378*s]:av.append((cx+side*.158*s,cy+rr*math.sin(a),cz+rr*math.cos(a)))
            for k in range(ns):af.append((k*2,k*2+1,((k+1)%ns)*2+1,((k+1)%ns)*2))
            ww=mesh('Whitewall band',av,af,M['cream']);sol=ww.modifiers.new('Band','SOLIDIFY');sol.thickness=.008
        hubmat=M['paint'] if W.get('hub')=='paint' else M['chrome']
        fin={'alloy':M['alloy'],'gunmetal':material('RimGunmetal',(.12,.13,.14),.85,.32),'bronze':material('RimBronze',(.40,.26,.09),.85,.30),
             'white':material('RimWhite',(.86,.87,.86),.3,.30),'black':material('RimBlack',(.03,.035,.04),.4,.35)}[W.get('finish','alloy')]
        if style in ('alloy5','star','spoke4','six','mesh','split'):
            k_=self.rimk;dish=W.get('dish',0)   # dish: how deep the spoke face sits inside the lip (doc 66)
            torus('Alloy rim lip',.29*s*k_,.019,outer,M['chrome'] if W.get('lip')=='polished' else fin)
            count={'alloy5':5,'star':8,'spoke4':4,'six':6,'mesh':10,'split':6}[style];half={'star':.018,'spoke4':.055,'six':.040,'mesh':.012,'split':.022}.get(style,.032)
            def spoke(a,r0,r1,h0,h1,label):
                sv=[]
                for xx in [-.019,.019]:
                    for rad,w in [(r0,-h0),(r1,-h1),(r1,h1),(r0,h0)]:
                        xr=outer+xx-side*dish*(1-(rad-.077)/.21)
                        sv.append((xr,cy+math.sin(a)*rad*s*k_+math.cos(a)*w,cz+math.cos(a)*rad*s*k_-math.sin(a)*w))
                sp=mesh(label,sv,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],fin);bevel(sp,.006,2)
            for i in range(count):
                a=i*math.tau/count
                if style=='split':
                    spoke(a,.077,.17,half*1.6,half*1.4,'Split spoke root')
                    for da in (-.11,.11):spoke(a+da*.5,.17,.285,half,half*.8,'Split spoke arm')
                elif style=='mesh':
                    for da in (-.30,.30):spoke(a,.077,.285,half,half,'Mesh spoke')   # (pairs cross into a mesh)
                    spoke(a+.31,.15,.285,half,half,'Mesh spoke')
                else:spoke(a,.077,.285,half,half*.75,'Tapered cast alloy spoke')
            for i in range(4):
                a=i*math.tau/4+.4;cylinder('Wheel nut',(outer+side*.028,cy+math.sin(a)*.07*s,cz+math.cos(a)*.07*s),.013,.012,M['trim'],vertices=10,r=0)
            cylinder('Hub centre',(outer+side*.027,cy,cz),.10*s,.05,fin)
            cylinder('Hub badge',(outer+side*.055,cy,cz),.044*s,.008,hubmat,vertices=24)
        elif style=='steel':
            cylinder('Pressed steel wheel',(outer-side*.01,cy,cz),.29*s,.03,M['steel'],vertices=48)
            torus('Steel rim lip',.29*s,.016,outer,M['steel'])
            torus('Steel wheel ridge',.19*s,.012,outer+side*.012,M['steel'])
            for i in range(8):
                a=i*math.tau/8;cylinder('Steel wheel vent',(outer+side*.006,cy+math.sin(a)*.24*s,cz+math.cos(a)*.24*s),.024*s,.012,M['trim'],vertices=12,r=0)
            sphere('Chrome hubcap dome',(outer+side*.02,cy,cz),(.05,.13*s,.13*s),M['chrome'])
            cylinder('Hubcap badge',(outer+side*.07,cy,cz),.04*s,.006,hubmat,vertices=24,r=0)
        elif style=='hubcap':
            cylinder('Plastic wheel trim',(outer,cy,cz),.30*s,.035,M['alloy'],vertices=56)
            torus('Wheel trim lip',.285*s,.014,outer+side*.016,M['alloy'])
            for i in range(12):
                a=i*math.tau/12;box('Wheel trim slot',(outer+side*.016,cy+math.sin(a)*.2*s,cz+math.cos(a)*.2*s),(.012,.03*s,.09*s),M['trim'],0,rot=(-a,0,0))
            cylinder('Trim centre',(outer+side*.02,cy,cz),.09*s,.02,M['alloy'],vertices=32)
            cylinder('Trim badge',(outer+side*.034,cy,cz),.045*s,.006,hubmat,vertices=24,r=0)
        elif style=='dish':
            cylinder('Chrome dish rim',(outer-side*.005,cy,cz),.27*s,.03,M['chrome'],vertices=56)
            torus('Chrome rim lip',.29*s,.016,outer,M['chrome'])
            sphere('Chrome dog-dish cap',(outer+side*.018,cy,cz),(.045,.17*s,.17*s),M['chrome'])
            cylinder('Cap badge',(outer+side*.064,cy,cz),.05*s,.006,M['paint'],vertices=24,r=0)
        objs=list(set(bpy.context.scene.objects)-before)
        bpy.ops.object.select_all(action='DESELECT')
        for o in objs:o.select_set(True)
        bpy.context.view_layer.objects.active=objs[0];bpy.ops.object.convert(target='MESH');bpy.ops.object.join();w=bpy.context.object;w.name=name+'_Mesh'
        pivot=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(pivot);pivot.location=(cx,cy,cz)
        bpy.context.view_layer.update();mw=w.matrix_world.copy();w.parent=pivot;w.matrix_world=mw

    # Panel gaps ------------------------------------------------------
    def groove(self,pts,cyclic=False,width=.008):
        """A real shut line: recorded here, cut into the body by cut_grooves() so doors, bonnet, boot
        and fuel flap read as separate panels (tubes laid on the paint floated off it like pipes)."""
        self.grooves.append(([Vector(p) for p in pts],cyclic,width))
    def cut_grooves(self):
        if not self.grooves:return
        from mathutils.bvhtree import BVHTree
        tree=BVHTree.FromObject(self.body,bpy.context.evaluated_depsgraph_get());v=[];f=[]
        for P,cyclic,w in self.grooves:
            dense=[]
            for a,b in zip(P,P[1:]+([P[0]] if cyclic else [])):
                n=max(1,int((b-a).length/.012))
                for i in range(n):dense.append(a.lerp(b,i/n))
            if not cyclic:dense.append(P[-1])
            ring=[]
            for p in dense:
                loc,nrm,_,dist=tree.find_nearest(p)
                ring.append(None if loc is None or dist>.04 else (loc,nrm.normalized()))
            runs=[];cur=[]
            for x in ring+[None]:
                if x is None:
                    if len(cur)>1:runs.append(cur)
                    cur=[]
                else:cur.append(x)
            closed=cyclic and len(runs)==1 and len(runs[0])==len(ring)
            for run in runs:
                base=len(v);m=len(run)
                for i,(loc,nrm) in enumerate(run):
                    a=run[(i-1)%m if closed else max(0,i-1)][0];b=run[(i+1)%m if closed else min(m-1,i+1)][0]
                    s=(b-a).cross(nrm).normalized()
                    for sx,out in [(-1,.006),(1,.006),(1,-.011),(-1,-.011)]:v.append(loc+s*(w*.5*sx)+nrm*out)
                for i in range(m if closed else m-1):
                    j=(i+1)%m
                    for k in range(4):f.append((base+i*4+k,base+i*4+(k+1)%4,base+j*4+(k+1)%4,base+j*4+k))
                if not closed:f.append(tuple(base+k for k in range(4)));f.append(tuple(base+(m-1)*4+k for k in range(3,-1,-1)))
        if not f:return
        cutter=mesh('Panel gap cutter',v,f,self.M['seam'],smooth=False)
        mod=self.body.modifiers.new('Panel gaps','BOOLEAN');mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cutter;mod.use_self=True
        try:mod.material_mode='TRANSFER'
        except Exception:pass
        bpy.context.view_layer.objects.active=self.body;bpy.ops.object.modifier_apply(modifier=mod.name)
        bpy.data.objects.remove(cutter,do_unlink=True)

    def build(self):
        self.shell();self.greenhouse();self.bumper(True);self.bumper(False);self.headlamps();self.grille();self.face();self.tail()
        self.body_details();self.interior();self.extras();self.cut_grooves()
        if '--profile' in args:
            dg=bpy.context.evaluated_depsgraph_get();tally={}
            for o in bpy.context.scene.objects:
                if o.type not in {'MESH','CURVE'}:continue
                e=o.evaluated_get(dg);m=e.to_mesh();key=o.name.split('.')[0];tally[key]=tally.get(key,0)+sum(len(p.vertices)-2 for p in m.polygons);e.to_mesh_clear()
            print('PROFILE',sorted(tally.items(),key=lambda kv:-kv[1])[:25])
        # Join stationary parts first, then wheels (so the join ignores their children).
        bpy.ops.object.select_all(action='DESELECT')
        parts=[o for o in bpy.context.scene.objects if o.type in {'MESH','CURVE'} and o.parent is None]
        for o in parts:o.select_set(True)
        bpy.context.view_layer.objects.active=self.body;bpy.ops.object.convert(target='MESH');bpy.ops.object.join();bpy.context.object.name='Body'
        if self.R.get('lower'):bpy.context.object.location.z-=self.R['lower']   # (a lowered kit sits down over the same wheels; negative lifts an off-roader)
        for side,letter in [(-1,'L'),(1,'R')]:
            for cy,end in [(self.fa,'F'),(self.ra,'R')]:
                self.wheel(side,cy,'Wheel_'+end+letter)
                box('Fixed brake caliper',(side*(self.track+.091*self.wr/.425),cy-.19,self.wr),(.055,.085,.19),self.M['insert'],.026)
        bpy.ops.object.select_all(action='DESELECT')
        body=bpy.data.objects['Body'];extra=[o for o in bpy.context.scene.objects if o.type=='MESH' and o.parent is None and o!=body]
        for o in extra+[body]:o.select_set(True)
        bpy.context.view_layer.objects.active=body;bpy.ops.object.convert(target='MESH');bpy.ops.object.join()

def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)

def stats(cid):
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    pts=[o.matrix_world@v.co for o in meshes for v in o.data.vertices]
    xs=sorted(abs(p[0]) for p in pts)
    return {'body_id':cid,'shoulder_995':round(2*xs[int(len(xs)*.995)]+.02,3),'length':round(max(p[1] for p in pts)-min(p[1] for p in pts)+.01,3),'triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes),
            'min':[round(min(p[i] for p in pts),3) for i in range(3)],'max':[round(max(p[i] for p in pts),3) for i in range(3)]}

def export(cid):
    SOURCE.mkdir(parents=True,exist_ok=True);TARGET.mkdir(parents=True,exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/(cid+'.blend')))
    kw=dict(use_selection=False,object_types={'MESH','EMPTY'},apply_unit_scale=True,bake_space_transform=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,use_mesh_modifiers=True,bake_anim=False)
    bpy.ops.export_scene.fbx(filepath=str(TARGET/(cid+'.fbx')),**kw)
    return export_lods([o for o in bpy.context.scene.objects if o.type=='MESH'],TARGET,cid)

def render(cid,paint,out):
    scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
    scene.render.resolution_x=1200;scene.render.resolution_y=800
    if scene.world is None:scene.world=bpy.data.worlds.new('World')
    try:scene.world.use_nodes=True
    except Exception:pass
    bg=scene.world.node_tree.nodes['Background'];bg.inputs[0].default_value=(.65,.72,.76,1);bg.inputs[1].default_value=.3
    bpy.data.materials['BodyPaint'].node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(*paint,1)
    floor=material('Review floor',(.68,.65,.54),0,.85);box('Review floor',(0,0,-.05),(60,60,.1),floor,0)
    def area(name,loc,power,size,color):
        d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='DISK';d.size=size;d.color=color
        o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,.8))-o.location).to_track_quat('-Z','Y').to_euler()
    area('Warm key',(-3,4,6),500,5,(1,.9,.77));area('Cool fill',(4,1,3),200,4,(.7,.86,1));area('Rim',(1,-4,4),380,3,(1,.96,.86))
    d=bpy.data.cameras.new('Cam');cam=bpy.data.objects.new('Cam',d);scene.collection.objects.link(cam);scene.camera=cam;d.type='ORTHO';d.ortho_scale=5.2
    scene.view_settings.view_transform='AgX'
    out.mkdir(parents=True,exist_ok=True)
    # Close-ups (--closeups) catch surface faults: the nose/bonnet and a front wheel arch.
    views=[('front',(4.4,6.1,3.0),(0,0,.85),5.2),('rear',(-4.6,-6.0,2.9),(0,0,.85),5.2),('side',(7,0,1.3),(0,0,.85),5.2)]
    if '--closeups' in args:views+=[('nose',(2.2,5.2,2.4),(0,1.3,.7),2.0),('arch',(4.0,2.6,1.2),(.8,1.0,.55),1.6),('top',(.4,1.0,5.0),(0,.4,.8),3.4),('cowl',(1.3,2.9,1.9),(0,.35,1.0),1.3)]
    for label,loc,aim,scale in views:
        cam.location=loc;d.ortho_scale=scale;cam.rotation_euler=(Vector(aim)-cam.location).to_track_quat('-Z','Y').to_euler()
        scene.render.filepath=str(out/f'{cid}-{label}.png');bpy.ops.render.render(write_still=True)

def lineup(ids,out):
    """The whole field in one scene: two staggered rows facing the camera (doc 65)."""
    clear_scene();scene=bpy.context.scene;per=6 if len(ids)>12 else (len(ids)+1)//2;rows=(len(ids)+per-1)//per
    depth=(rows-1)*5.6;A=Vector((0,-depth*.5,.6));scale=19.5+max(0,depth-5.6)*.95
    for i,cid in enumerate(ids):
        row,col=divmod(i,per);n=min(per,len(ids)-row*per)
        with bpy.data.libraries.load(str(out/'cars'/(cid+'.blend'))) as (src,dst):dst.objects=list(src.objects)
        off=Vector((-((col-(n-1)*.5)*3.0+(row%2)*1.5),-row*5.6,0))   # (the camera looks back down -y: first car on the left)
        for o in dst.objects:
            if o is None or o.type not in {'MESH','EMPTY','CURVE'} or o.name.startswith('Review floor'):continue   # (each car's own review floor overlapped the rest and shadowed it black)
            scene.collection.objects.link(o)
            if o.parent is None:o.location=o.location+off
    scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True
    scene.render.resolution_x=3000;scene.render.resolution_y=1700
    scene.world=bpy.data.worlds.new('World')
    try:scene.world.use_nodes=True
    except Exception:pass
    bg=scene.world.node_tree.nodes['Background'];bg.inputs[0].default_value=(.65,.72,.76,1);bg.inputs[1].default_value=.3
    floor=material('Review floor',(.68,.65,.54),0,.85);box('Review floor',(0,0,-.05),(120,120,.1),floor,0)
    def area(name,loc,power,size,color):
        d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='DISK';d.size=size;d.color=color
        o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=A+Vector(loc);o.rotation_euler=(A+Vector((0,0,.2))-o.location).to_track_quat('-Z','Y').to_euler()
    k=(scale/19.5)**1.2   # (lights back off and brighten with the grid's size; squared washed the paint out)
    area('Warm key',(-9*scale/19.5,12.8*scale/19.5,16),5200*k,16,(1,.9,.77));area('Cool fill',(12*scale/19.5,4.8,8),2000*k,12,(.7,.86,1));area('Rim',(3,-13.2*scale/19.5,11),3600*k,10,(1,.96,.86))
    d=bpy.data.cameras.new('Cam');cam=bpy.data.objects.new('Cam',d);scene.collection.objects.link(cam);scene.camera=cam;d.type='ORTHO'
    scene.view_settings.view_transform='AgX'
    f=scale/19.5
    for label,loc,aim,scale in [('field-front',tuple(A+Vector((9*f,19.8*f,10.4*f))),tuple(A),scale),('field-rear',tuple(A+Vector((-9*f,-21.2*f,10.4*f))),tuple(A),scale)]:
        cam.location=loc;d.ortho_scale=scale;cam.rotation_euler=(Vector(aim)-cam.location).to_track_quat('-Z','Y').to_euler()
        scene.render.filepath=str(out/f'{label}.png');bpy.ops.render.render(write_still=True)

PAINTS={'pebble':(.85,.30,.25),'tallboy':(.95,.75,.20),'marlow':(.20,.35,.70),'dover':(.15,.45,.40),'heron':(.55,.12,.12),
        'bramble':(.30,.55,.20),'picnic':(.40,.65,.85),'kipper':(.90,.45,.15),'doodle':(.55,.75,.70),'otter':(.60,.35,.10),
        'sprint':(.85,.85,.82),'comet':(.10,.12,.14),'rally':(.15,.30,.75),'roadster':(.75,.08,.08),'zenith':(.05,.20,.55),'puddle':(.08,.56,.49),
        'bubble':(.95,.55,.70),'beetle':(.45,.70,.90),'taxi':(.98,.78,.10),'camper':(.35,.70,.55),'gelato':(.98,.85,.88),'minibus':(.95,.62,.15),'ritz':(.12,.18,.30),'postvan':(.80,.06,.05),
        'cob':(.30,.42,.30),'drover':(.85,.85,.82),'scamp':(.90,.55,.12),'fell':(.55,.12,.10),'tumbler':(.75,.68,.45),'hauler':(.20,.45,.70),
        'raijin':(.98,.58,.0),'hoshi':(.62,.04,.04),'regale_toy':(.30,.16,.62),'hayate':(.04,.22,.62),'kaze':(.90,.33,.04),
        'kumo':(.04,.32,.18),'tsubasa':(.80,.08,.06),'sakura':(.74,.06,.32),'shiro':(.95,.94,.90),
        'fubuki':(.06,.20,.60),'hachi':(.92,.92,.88),'kogarashi':(.75,.06,.08),'arashi':(.38,.02,.08),'taiyo':(.70,.72,.74),
        'midori':(.20,.55,.15),'hibari':(.95,.75,.05),'tsuki':(.45,.80,.10),'yoru':(.20,.07,.30),'nagare':(.30,.32,.36),
        'kage':(.03,.03,.04),'chibi':(.10,.60,.62),'kitsune':(.70,.22,.05),
        'sabaku':(.92,.92,.88),'yama':(.45,.06,.06),'tanuki':(.95,.78,.08),'tora':(.92,.40,.06),'kaminari':(.78,.06,.08),'kiri':(.70,.55,.12),
        'sprint_tuner':(.85,.85,.82),'comet_tuner':(.10,.12,.14),'rally_tuner':(.15,.30,.75),'pitgarage':(.08,.72,.66),'pitparts':(1.0,.74,.06),'pitbody':(1.0,.34,.64)}
def paint_of(cid):
    """A kit wears its C car's paint unless it has its own."""
    return PAINTS.get(cid) or PAINTS[cid[:-6] if cid.endswith('_tuner') else cid]
report={}
for cid in ONLY:
    clear_scene();car=Car(cid,RECIPES[cid]);car.build();info=stats(cid)
    if EXPORT:info['lods']=export(cid)
    if RENDER:render(cid,paint_of(cid),RENDER)
    if LINEUP:
        bpy.data.materials['BodyPaint'].node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(*paint_of(cid),1)
        (LINEUP/'cars').mkdir(parents=True,exist_ok=True);bpy.ops.wm.save_as_mainfile(filepath=str(LINEUP/'cars'/(cid+'.blend')),copy=True)
    report[cid]=info;print('FAMILY_CAR_BUILT',json.dumps(info))
print('FAMILY_CARS_DONE',json.dumps(report))
if LINEUP:lineup(ONLY,LINEUP)
