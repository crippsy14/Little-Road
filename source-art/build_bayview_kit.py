"""Bayview toy-town kit: chunky Road Trip Adventure-style buildings, landmarks and props.

Run: blender --background --python source-art/build_bayview_kit.py -- [ids...] [--render DIR] [--no-export]
Per asset: source-art/bayview/<id>.blend and unity/Assets/Resources/Bayview/<id>.fbx.

Conventions (read by BayviewKit.cs):
- Blender -Y is the frontage (Unity +Z). Metres. Origin at ground level, centre of footprint.
- All visual parts are joined into one multi-material mesh named "Visual".
- "Collision*" objects are unrotated boxes; Unity turns their mesh bounds into BoxColliders.
- "SignAnchor_<name>" empties mark shop-sign text; "Spin_<axis>" objects rotate (windmill sails).
- Materials named "Tint_*" take a per-instance colour; others keep the exported colour.
"""
from pathlib import Path
import math, sys, random
import bpy, bmesh
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
RENDER=Path(args[args.index('--render')+1]).resolve() if '--render' in args else None
EXPORT='--no-export' not in args
TARGET=ROOT/'unity/Assets/Resources/Bayview'
SOURCE=ROOT/'source-art/bayview'

# ---------------------------------------------------------------- materials

PALETTE={
    'Tint_Wall':(.97,.89,.72),'Tint_Shutter':(.10,.55,.62),'Tint_Awning':(.92,.22,.24),'Tint_Door':(.12,.42,.62),
    'WhiteTrim':(.97,.96,.92),'RoofTile':(.86,.34,.18),'RoofTileLight':(.95,.48,.26),'RoofSlate':(.30,.36,.46),
    'Stone':(.86,.80,.66),'StoneDark':(.66,.60,.50),'Wood':(.56,.34,.17),'WoodDark':(.36,.22,.12),
    'Glass':(.16,.34,.52),'GlassLit':(1.0,.86,.52),'Metal':(.30,.33,.36),'MetalLight':(.72,.75,.78),'Gold':(.98,.74,.16),
    'Leaf':(.30,.62,.20),'LeafLight':(.46,.76,.26),'LeafDark':(.16,.42,.20),'Bark':(.42,.28,.16),
    'FlowerRed':(.95,.20,.24),'FlowerPink':(1.0,.56,.74),'FlowerYellow':(1.0,.84,.22),'FlowerWhite':(1,1,.97),'FlowerBlue':(.52,.56,1.0),
    'Soil':(.40,.26,.15),'Water':(.25,.68,.86),'Canvas':(.97,.95,.88),'Red':(.90,.16,.14),'Yellow':(1.0,.80,.10),
    'Black':(.05,.05,.06),'Rope':(.78,.66,.44),'Sand':(.96,.87,.64),'Blue':(.16,.40,.86),'Green':(.18,.62,.36),
}
def mat(name):
    m=bpy.data.materials.get(name)
    if m:return m
    c=PALETTE[name];m=bpy.data.materials.new(name);m.diffuse_color=(*c,1)
    try:m.use_nodes=True
    except Exception:pass
    p=m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value=(*c,1);p.inputs['Roughness'].default_value=.18 if 'Glass' in name else .55
    if name in('Metal','MetalLight','Gold'):p.inputs['Metallic'].default_value=.6
    return m

# ---------------------------------------------------------------- primitives

PARTS=[];SPIN=[]
def _finish(o,name,material,bevel=0,smooth=False,segments=2):
    o.name=name;o.data.materials.clear();o.data.materials.append(mat(material))
    for p in o.data.polygons:p.use_smooth=smooth
    if bevel>0:
        m=o.modifiers.new('Toy radius','BEVEL');m.width=bevel;m.segments=segments;m.limit_method='ANGLE'
        m=o.modifiers.new('Toy normals','WEIGHTED_NORMAL');m.keep_sharp=True
    PARTS.append(o);return o
def box(name,loc,size,material,bevel=.04,rot=(0,0,0),segments=2):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc,rotation=rot);o=bpy.context.object;o.scale=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return _finish(o,name,material,min(bevel,min(size)*.45),segments=segments)
def cyl(name,loc,r,depth,material,axis='Z',verts=24,bevel=.02,r2=None):
    rot={'Z':(0,0,0),'X':(0,math.pi/2,0),'Y':(math.pi/2,0,0)}[axis]
    if r2 is None:bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=r,depth=depth,location=loc,rotation=rot)
    else:bpy.ops.mesh.primitive_cone_add(vertices=verts,radius1=r,radius2=r2,depth=depth,location=loc,rotation=rot)
    return _finish(bpy.context.object,name,material,min(bevel,r*.4),smooth=True)
def sphere(name,loc,scale,material,subdiv=2):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdiv,radius=1,location=loc);o=bpy.context.object;o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return _finish(o,name,material,smooth=True)
def poly(name,verts,faces,material,bevel=0,smooth=False):
    me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update()
    o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o)
    return _finish(o,name,material,bevel,smooth)
def wedge(name,a,b,apex,normal,material,thick=.16):
    """Solid triangular infill (gable end): triangle a-b-apex extruded inward along -normal."""
    n=Vector(normal)*thick;A,B,C=Vector(a),Vector(b),Vector(apex)
    v=[A,B,C,A-n,B-n,C-n]
    return poly(name,[tuple(x) for x in v],[(0,1,2),(3,5,4),(0,3,4,1),(1,4,5,2),(2,5,3,0)],material)
def collision(name,loc,size):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.scale=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.name='Collision_'+name;o.display_type='WIRE';o.hide_render=True;return o
def anchor(name,loc,rot=(0,0,0)):
    o=bpy.data.objects.new(name,None);o.location=loc;o.rotation_euler=rot;bpy.context.collection.objects.link(o);return o

# ---------------------------------------------------------------- architectural pieces

def gable_roof(cx,cy,w,d,eave,rise,over=.45,material='RoofTile',ridge_axis='X'):
    """Tiled gable roof: overlapping rounded tile courses on both slopes, ridge cap and white fascia."""
    if ridge_axis=='X':
        half=d/2+over;length=w+2*over;slope=math.atan2(rise,d/2)
        rows=max(3,int(math.hypot(d/2,rise)/.42))
        for side in(-1,1):
            for i in range(rows):
                t=(i+.5)/rows;y=cy+side*half*(1-t);z=eave+rise*t-.05+(over/half)*0
                box('Roof course',(cx,y,z+.10),(length,math.hypot(half,rise)/rows+.08,.16),material if i%3 else 'RoofTileLight',.06,rot=(-side*slope,0,0))
        cyl('Ridge cap',(cx,cy,eave+rise+.14),.18,length+.1,'RoofTile','X',16)
        for side in(-1,1):
            # Triangular gable wall infill and fascia boards
            x=cx+side*(w/2)
            wedge('Gable',(x,cy-d/2,eave),(x,cy+d/2,eave),(x,cy,eave+rise),(side,0,0),'Tint_Wall')
            for s in(-1,1):box('Fascia',(cx+side*(w/2+over*.8),cy+s*half*.5,eave+rise*.5+.02),(.12,math.hypot(half,rise)+.1,.22),'WhiteTrim',.03,rot=(-s*slope,0,0))
    else:
        half=w/2+over;length=d+2*over;slope=math.atan2(rise,w/2)
        rows=max(3,int(math.hypot(w/2,rise)/.42))
        for side in(-1,1):
            for i in range(rows):
                t=(i+.5)/rows;x=cx+side*half*(1-t);z=eave+rise*t-.05
                box('Roof course',(x,cy,z+.10),(math.hypot(half,rise)/rows+.08,length,.16),material if i%3 else 'RoofTileLight',.06,rot=(0,side*slope,0))
        cyl('Ridge cap',(cx,cy,eave+rise+.14),.18,length+.1,'RoofTile','Y',16)
        for side in(-1,1):
            y=cy+side*(d/2)
            wedge('Gable',(cx-w/2,y,eave),(cx+w/2,y,eave),(cx,y,eave+rise),(0,side,0),'Tint_Wall')
            for s in(-1,1):box('Fascia',(cx+s*half*.5,cy+side*(d/2+over*.8),eave+rise*.5+.02),(math.hypot(half,rise)+.1,.12,.22),'WhiteTrim',.03,rot=(0,s*slope,0))

def window(x,y,z,w=1.2,h=1.5,shutters=True,flowers=True,face=-1,glass='Glass'):
    """Window on a wall whose outward normal is face*Y (face=-1 front)."""
    o=face*.02
    box('Window frame',(x,y+o*2,z),(w+.3,.18,h+.3),'WhiteTrim',.05)
    # Distinct depths (frame front .13, glass .17, bars .21, shutters .16) so no faces z-fight.
    box('Window glass',(x,y+face*.15,z),(w,.04,h),glass,.01)
    box('Glazing bar',(x,y+face*.19,z),(.07,.04,h),'WhiteTrim',.015);box('Glazing bar',(x,y+face*.19,z+h*.12),(w,.04,.07),'WhiteTrim',.015)
    box('Sill',(x,y+face*.22,z-h/2-.12),(w+.5,.36,.14),'WhiteTrim',.04)
    if shutters:
        for s in(-1,1):
            sx=x+s*(w/2+.36)
            box('Shutter',(sx,y+face*.10,z),(.62,.12,h+.12),'Tint_Shutter',.05)
            for k in range(5):box('Shutter slat',(sx,y+face*.18,z-h*.36+k*h*.18),(.52,.05,.07),'Tint_Shutter',.02)
    if flowers:
        box('Flower box',(x,y+face*.42,z-h/2-.32),(w+.2,.34,.3),'Wood',.05)
        rnd=random.Random(int(x*100+z*10))
        for k in range(int(w/.22)+1):
            fx=x-w/2+.1+k*.22
            sphere('Leaves',(fx,y+face*.42,z-h/2-.14),(.16,.16,.13),'Leaf',1)
            sphere('Bloom',(fx+rnd.uniform(-.05,.05),y+face*(.50+rnd.uniform(0,.06)),z-h/2-.04+rnd.uniform(0,.05)),(.09,.09,.09),rnd.choice(['FlowerRed','FlowerPink','FlowerYellow','FlowerWhite']),1)

def door(x,y,z0=0,w=1.3,h=2.5,face=-1,canopy=True):
    box('Door frame',(x,y+face*.03,z0+h/2+.06),(w+.34,.2,h+.2),'WhiteTrim',.05)
    box('Door',(x,y+face*.1,z0+h/2),(w,.1,h),'Tint_Door',.05)
    for k in range(2):box('Door panel',(x,y+face*.16,z0+h*.28+k*h*.42),(w*.7,.05,h*.3),'Tint_Door',.04)
    sphere('Door knob',(x+w*.33,y+face*.2,z0+h*.48),(.07,.07,.07),'Gold',1)
    box('Step',(x,y+face*.35,z0+.08),(w+.7,.7,.16),'Stone',.05)
    if canopy:
        box('Door canopy',(x,y+face*.45,z0+h+.35),(w+.9,.9,.16),'RoofTile',.06,rot=(face*-.25,0,0))
        for s in(-1,1):box('Canopy bracket',(x+s*(w/2+.3),y+face*.3,z0+h+.12),(.08,.5,.08),'WhiteTrim',.02,rot=(face*.7,0,0))

def awning(x,y,z,w,depth=1.5,stripes=8,face=-1):
    for i in range(stripes):
        sx=x-w/2+(i+.5)*w/stripes
        box('Awning stripe',(sx,y+face*depth/2,z-depth*.22),(w/stripes+.005,depth+.06,.07),'Tint_Awning' if i%2==0 else 'Canvas',.02,rot=(-face*.42,0,0))
        # Scalloped valance
        cyl('Valance scallop',(sx,y+face*(depth+.02),z-depth*.46-.12),w/stripes*.5,.05,'Tint_Awning' if i%2==0 else 'Canvas','Y',12,.01)
    for s in(-1,1):box('Awning arm',(x+s*(w/2-.1),y+face*depth*.5,z-depth*.3),(.05,depth,.05),'Metal',.01,rot=(-face*.42,0,0))

def sign_board(x,y,z,w,h,material='WhiteTrim',text_name='Sign',face=-1):
    box('Sign board',(x,y+face*.08,z),(w,.16,h),material,.06)
    box('Sign border',(x,y+face*.03,z),(w+.16,.08,h+.16),'Wood',.04)
    anchor('SignAnchor_'+text_name,(x,y+face*.2,z))

def planter_box(x,y,w=1.6,d=.7,face=-1):
    box('Planter',(x,y,.35),(w,d,.7),'Stone',.08)
    box('Planter soil',(x,y,.69),(w-.14,d-.14,.04),'Soil',.01)
    rnd=random.Random(int(x*37+y*11))
    for k in range(int(w/.3)):
        px=x-w/2+.2+k*.3
        sphere('Shrub',(px,y+rnd.uniform(-.1,.1),.86),(.2,.2,.2),'Leaf',1)
        sphere('Bloom',(px+rnd.uniform(-.06,.06),y+rnd.uniform(-.15,.15),1.02),(.09,.09,.09),rnd.choice(['FlowerRed','FlowerPink','FlowerYellow','FlowerBlue']),1)

def walls(w,d,h,plinth=.55):
    box('Walls',(0,0,h/2),(w,d,h),'Tint_Wall',.12,segments=3)
    box('Plinth',(0,0,plinth/2),(w+.16,d+.16,plinth),'Stone',.06)
    for s in(-1,1):
        for t in(-1,1):box('Quoin',(s*(w/2+.02),t*(d/2+.02),h/2),(.32,.32,h),'Stone',.06)
    box('Eaves band',(0,0,h-.12),(w+.18,d+.18,.24),'WhiteTrim',.05)

# ---------------------------------------------------------------- assets

def house_a():
    """Two-storey village house: tiled gable roof, shutters, flower boxes, canopy door, chimney."""
    w,d,h=8.5,7.5,6.2;walls(w,d,h)
    gable_roof(0,0,w,d,h,2.3)
    door(-2.2,-d/2)
    window(1.6,-d/2,1.7,1.9,1.5)
    for x in(-2.3,1.6):window(x,-d/2,4.4,1.3,1.4)
    for x in(-2.2,2.2):window(x,d/2,4.4,1.2,1.3,face=1,flowers=False)
    box('Chimney',(2.6,1.2,h+2.5),(.9,.9,2.4),'Stone',.06);box('Chimney cap',(2.6,1.2,h+3.75),(1.15,1.15,.18),'WhiteTrim',.04)
    for k in range(2):cyl('Chimney pot',(2.45+k*.3,1.2,h+4.05),.12,.4,'RoofTile','Z',12)
    planter_box(2.9,-d/2-.9,1.8)
    collision('Body',(0,0,h/2),(w+.3,d+.3,h))

def house_b():
    """Tall narrow townhouse: front gable, balcony with railings, round attic window."""
    w,d,h=6.2,7.5,7.6;walls(w,d,h)
    gable_roof(0,0,w,d,h,2.6,ridge_axis='Y')
    door(-1.2,-d/2)
    window(1.4,-d/2,1.7,1.2,1.5)
    box('Balcony slab',(0,-d/2-.7,3.9),(w-1.2,1.4,.2),'Stone',.05)
    for k in range(12):cyl('Railing',(-w/2+.8+k*(w-1.6)/11,-d/2-1.33,4.45),.035,.9,'Metal','Z',8)
    box('Rail top',(0,-d/2-1.33,4.92),(w-1.4,.08,.08),'Metal',.02)
    for s in(-1,1):box('Rail side',(s*(w/2-.7),-d/2-.7,4.92),(.08,1.3,.08),'Metal',.02)
    for x in(-1.25,1.25):window(x,-d/2,4.9,1.0,1.8,flowers=False)
    for k in range(5):sphere('Balcony pot',(-2+k,-d/2-1.05,4.15),(.18,.18,.18),['FlowerRed','FlowerPink','FlowerYellow','FlowerWhite','FlowerRed'][k],1)
    cyl('Attic window',(0,-d/2-.06,h+1.0),.5,.14,'WhiteTrim','Y',24);cyl('Attic glass',(0,-d/2-.12,h+1.0),.38,.08,'Glass','Y',24)
    window(0,d/2,4.9,1.2,1.4,face=1,flowers=False)
    collision('Body',(0,0,h/2),(w+.3,d+.3,h))

def shop(kind='shop'):
    """One-and-a-half storey shop: big display windows, striped awning, sign board, crates of goods."""
    w,d,h=9.0,8.0,5.4;walls(w,d,h)
    gable_roof(0,0,w,d,h,2.2)
    box('Shopfront',(0,-d/2-.1,1.6),(w-.9,.2,3.0),'Tint_Door',.06)
    for x in(-2.4,2.4):
        box('Display window',(x,-d/2-.22,1.75),(2.6,.08,2.1),'GlassLit' if kind=='cafe' else 'Glass',.02)
        box('Display frame',(x,-d/2-.19,1.75),(2.85,.1,2.35),'WhiteTrim',.04)
        box('Display bar',(x,-d/2-.28,1.75),(.07,.06,2.1),'WhiteTrim',.02)
    door(0,-d/2-.1,0,1.4,2.6,canopy=False)
    awning(0,-d/2-.1,3.55,w-.6,1.8)
    sign_board(0,-d/2-.15,4.55,w-2.2,.85,'WhiteTrim',kind)
    if kind=='cafe':
        for i,(x,y) in enumerate([(-3.3,-d/2-4.0),(0,-d/2-4.6),(3.3,-d/2-4.0)]):
            cyl('Table top',(x,y,.78),.55,.06,'WhiteTrim','Z',20);cyl('Table leg',(x,y,.4),.05,.78,'Metal','Z',8)
            cyl('Parasol pole',(x,y,1.6),.04,2.4,'WhiteTrim','Z',8)
            cyl('Parasol',(x,y,2.62),1.35,.5,'Tint_Awning' if i%2==0 else 'Canvas','Z',8,.02,r2=.06)
            for a in range(3):
                ang=a*2.094+i;cx,cy=x+math.cos(ang)*.95,y+math.sin(ang)*.95
                box('Chair seat',(cx,cy,.46),(.46,.46,.07),'Tint_Shutter',.03);box('Chair back',(cx+math.cos(ang)*.2,cy+math.sin(ang)*.2,.72),(.1,.46,.5),'Tint_Shutter',.03,rot=(0,0,ang))
                for lx,ly in((-.18,-.18),(.18,-.18),(-.18,.18),(.18,.18)):cyl('Chair leg',(cx+lx,cy+ly,.22),.025,.44,'Metal','Z',6)
        box('Menu board',(-4.2,-d/2-1.2,.7),(.7,.08,1.1),'Black',.03,rot=(.18,0,0))
    elif kind=='post':
        cyl('Post box',(3.6,-d/2-1.3,.75),.38,1.5,'Red','Z',24);sphere('Post box top',(3.6,-d/2-1.3,1.5),(.38,.38,.22),'Red')
        box('Post slot',(3.6,-d/2-1.66,1.15),(.34,.04,.07),'Black',.01)
        cyl('Clock',(0,-d/2-.22,h+1.05),.55,.14,'WhiteTrim','Y',28);cyl('Clock rim',(0,-d/2-.18,h+1.05),.64,.1,'Gold','Y',28)
        box('Clock hand',(0,-d/2-.32,h+1.2),(.05,.03,.35),'Black',.01);box('Clock hand',(.12,-d/2-.32,h+1.05),(.28,.03,.05),'Black',.01)
    else:
        for i,x in enumerate((-3.7,3.7)):
            box('Crate',(x,-d/2-.9,.3),(.8,.6,.6),'Wood',.05)
            for k in range(4):sphere('Produce',(x-.25+(k%2)*.5,-d/2-.9+(k//2-.5)*.25,.66),(.18,.18,.16),['FlowerRed','FlowerYellow','LeafLight','FlowerYellow'][(k+i)%4],1)
    collision('Body',(0,0,h/2),(w+.3,d+.3,h))

def cafe():shop('cafe')
def post_office():shop('post')

def lamp_post():
    cyl('Lamp base',(0,0,.25),.22,.5,'Tint_Shutter','Z',16);cyl('Lamp pole',(0,0,2.1),.08,3.6,'Tint_Shutter','Z',12)
    cyl('Lamp collar',(0,0,3.9),.14,.18,'Gold','Z',12)
    cyl('Lantern glass',(0,0,4.3),.26,.6,'GlassLit','Z',6,.01,r2=.32);cyl('Lantern roof',(0,0,4.72),.42,.28,'Tint_Shutter','Z',6,.02,r2=.05)
    sphere('Lantern finial',(0,0,4.92),(.08,.08,.08),'Gold',1)
    collision('Pole',(0,0,2),(.3,.3,4))

def bench():
    for x in(-.75,.75):box('Bench leg',(x,0,.35),(.12,.6,.7),'Metal',.03)
    for k in range(3):box('Seat slat',(0,-.2+k*.2,.7),(2,.16,.07),'Wood',.03)
    for k in range(2):box('Back slat',(0,.32,.95+k*.22),(2,.07,.16),'Wood',.03)
    collision('Bench',(0,0,.5),(2.1,.8,1))

def fountain():
    cyl('Basin wall',(0,0,.35),2.6,.7,'Stone','Z',32,.08);cyl('Basin water',(0,0,.62),2.35,.04,'Water','Z',32,.01)
    cyl('Column',(0,0,1.2),.35,1.6,'Stone','Z',16);cyl('Upper bowl',(0,0,1.95),1.1,.3,'Stone','Z',24,.06,r2=.5)
    cyl('Upper water',(0,0,2.08),.95,.04,'Water','Z',24,.01);sphere('Finial',(0,0,2.45),(.28,.28,.4),'Gold')
    for k in range(8):
        a=k*math.pi/4;sphere('Basin flower',(math.cos(a)*2.9,math.sin(a)*2.9,.35),(.3,.3,.25),['FlowerRed','FlowerYellow','FlowerPink','FlowerWhite'][k%4],1)
    collision('Basin',(0,0,.4),(5.4,5.4,.8))

def tree_round():
    cyl('Trunk',(0,0,1.3),.28,2.6,'Bark','Z',10,.05,r2=.2)
    for i,(p,s) in enumerate([((0,0,3.4),1.9),((.9,.3,3.0),1.3),((-.8,-.4,3.1),1.35),((.2,-.7,4.2),1.2),((-.3,.6,4.3),1.1)]):
        sphere('Canopy',p,(s,s,s*.92),'Leaf' if i%2==0 else 'LeafLight',2)
    collision('Trunk',(0,0,1.5),(.6,.6,3))

def tree_cypress():
    cyl('Trunk',(0,0,.5),.2,1,'Bark','Z',8)
    sphere('Cypress',(0,0,3.3),(.95,.95,3.0),'LeafDark',2);sphere('Cypress tip',(0,0,5.9),(.55,.55,1.0),'LeafDark',2)
    collision('Trunk',(0,0,1.5),(.6,.6,3))

def tree_pine():
    """Umbrella (stone) pine: tall bare trunk, flat puffy crown."""
    cyl('Trunk',(0,0,2.6),.26,5.2,'Bark','Z',10,.05,r2=.18)
    for s in(-1,1):cyl('Branch',(s*.7,0,5.0),.1,1.8,'Bark','X',8)
    for i,(x,y,r) in enumerate([(0,0,2.4),(1.5,.4,1.5),(-1.5,-.3,1.6),(.4,1.3,1.4),(-.3,-1.4,1.4)]):
        sphere('Crown',(x,y,6.0+(i%2)*.25),(r,r,r*.5),'Leaf' if i%2 else 'LeafDark',2)
    collision('Trunk',(0,0,2.5),(.6,.6,5))

def palm():
    for k in range(8):
        cyl('Palm trunk ring',(k*.06,0,.35+k*.7),.24-k*.012,.72,'Bark','Z',10,.04,r2=.2-k*.012)
    top=Vector((.48,0,5.9))
    for k in range(7):
        a=k*math.tau/7;d=Vector((math.cos(a),math.sin(a),0))
        pts=[top+d*(t*2.4)+Vector((0,0,.5*math.sin(t*2.6)-t*t*1.2)) for t in(0,.33,.66,1)]
        for i in range(3):
            m=(pts[i]+pts[i+1])/2;dv=pts[i+1]-pts[i]
            o=box('Frond',m,(dv.length+.1,.9-i*.22,.06),'Leaf' if k%2 else 'LeafLight',.02)
            o.rotation_euler=(0,-math.atan2(dv.z,math.hypot(dv.x,dv.y)),math.atan2(dv.y,dv.x))
    for k in range(3):sphere('Coconut',(top.x+math.cos(k*2.1)*.25,math.sin(k*2.1)*.25,top.z-.3),(.18,.18,.18),'WoodDark',1)
    collision('Trunk',(0,0,2.5),(.6,.6,5))

def bush():
    rnd=random.Random(7)
    for k in range(5):sphere('Bush',(rnd.uniform(-.6,.6),rnd.uniform(-.5,.5),.5+rnd.uniform(0,.2)),(.7,.7,.6),'Leaf' if k%2 else 'LeafLight',2)
    for k in range(6):sphere('Bush bloom',(rnd.uniform(-.8,.8),rnd.uniform(-.7,.7),.9+rnd.uniform(0,.3)),(.12,.12,.12),rnd.choice(['FlowerPink','FlowerWhite','FlowerYellow']),1)

# ---------------------------------------------------------------- harbour

def sea_wall():
    """12 m masonry sea-wall segment: top at 0, foot at -14. Face toward -Y (the sea). Coping, drain, weed line."""
    L,H=12.0,14.0
    box('Wall core',(0,.6,-H/2),(L,1.2,H),'StoneDark',.02)
    for row in range(int(H/.9)):
        z=-(row+.5)*.9;off=(row%2)*.9
        for k in range(-1,int(L/1.8)+2):
            x0=max(-L/2,-L/2+off+(k-.5)*1.8+.03);x1=min(L/2,-L/2+off+(k+.5)*1.8-.03)
            if x1-x0<.3:continue
            box('Wall block',((x0+x1)/2,-.06,z),(x1-x0,.2,.84),'Stone' if (row+k)%5 else 'StoneDark',.05)
    box('Coping',(0,-.15,.12),(L+.04,1.6,.3),'WhiteTrim',.06)
    box('Weed line',(0,-.2,-12.9),(L,.1,.35),'LeafDark',.05)
    cyl('Drain',(3.3,-.2,-9.5),.28,.3,'Black','Y',16);cyl('Drain lip',(3.3,-.3,-9.5),.36,.1,'Stone','Y',16)
    collision('Wall',(0,.6,-H/2),(L,1.2,H))

def promenade_rail():
    for x in(-3,0,3):
        cyl('Rail post',(x,0,.55),.07,1.1,'Tint_Shutter','Z',10);sphere('Post knob',(x,0,1.14),(.1,.1,.1),'Gold',1)
    for z in(.5,.95):cyl('Rail bar',(0,0,z),.04,6.0,'Tint_Shutter','X',8)
    collision('Rail',(0,0,.55),(6,.2,1.1))

def harbour_steps():
    """Stone stair descending along +X down the sea-wall face (wall face toward -Y), top at 0, foot at -11."""
    for k in range(28):
        x=-6+k*.43;z=-.2-k*.39
        box('Stair tread',(x,-1.1,z),(.5,1.8,.22),'Stone',.03)
        box('Stair body',(x,-1.1,z-.9),(.5,1.8,1.6),'StoneDark',.02)
    box('Stair landing',(6.5,-1.0,-11.0),(2.0,2.0,.3),'Stone',.04)
    for k in range(9):cyl('Stair post',(-6+k*1.5,-1.65,.45-k*1.36),.05,1.0,'Metal','Z',8)

def pier():
    """20 m timber pier on piles, running along -Y: deck at 0 (placed near sea level), bollards, rope rail, lamp."""
    L=20.0
    for k in range(int(L/.5)):box('Deck plank',(0,-k*.5-.25,0),(3.2,.46,.12),'Wood' if k%4 else 'WoodDark',.02)
    for side in(-1,1):
        box('Stringer',(side*1.45,-L/2,-.18),(.2,L,.24),'WoodDark',.02)
        for k in range(6):cyl('Pile',(side*1.5,-1.5-k*3.6,-1.9),.18,4.0,'WoodDark','Z',10)
        for k in range(4):cyl('Bollard',(side*1.25,-3-k*5,.3),.14,.45,'Black','Z',12)
    for k in range(10):cyl('Rope rail post',(1.55,-1-k*2,.5),.05,.9,'Wood','Z',8)
    cyl('Rope',(1.55,-L/2,.85),.03,L,'Rope','Y',6)
    for y in(-8,-14):box('Tyre fender',(-1.65,y,-.3),(.12,.6,.6),'Black',.2)
    for k in range(3):box('Crate',(.6-(k%2)*.3,-18.5+k*.1,.35+(k//2)*.55),(.7,.6,.55),'Wood',.05)
    cyl('Pier lamp',(-1.3,-19.3,1.6),.06,3.0,'Tint_Shutter','Z',8);sphere('Pier lamp globe',(-1.3,-19.3,3.2),(.28,.28,.28),'GlassLit',2)
    collision('Deck',(0,-L/2,-.1),(3.3,L,.3))

def boat():
    """Little fishing boat (~6.5 m), bow toward -Y, waterline at 0."""
    v=[];f=[]
    stations=[(-3.2,.05,.5,1.0),(-2.6,.9,.2,1.05),(-1.2,1.25,-.1,1.05),(1.0,1.3,-.1,1.0),(2.6,1.15,.0,1.0),(3.1,.9,.1,.95)]
    for (y,hw,zb,zt) in stations:
        v+=[(-hw,y,zt),(-hw*.8,y,zb+.35),(-hw*.25,y,zb),(hw*.25,y,zb),(hw*.8,y,zb+.35),(hw,y,zt)]
    n=6
    for i in range(len(stations)-1):
        for j in range(n-1):a=i*n+j;f.append((a,a+1,a+n+1,a+n))
    f.append(tuple(range(n))[::-1]);f.append(tuple(range((len(stations)-1)*n,len(stations)*n)))
    poly('Hull',[(x,-y,z) for x,y,z in v],f,'Tint_Wall',.04,True)
    box('Deck',(0,.2,.92),(2.2,5.4,.08),'Wood',.02)
    for s in(-1,1):box('Gunwale',(s*1.18,.2,1.02),(.14,5.8,.16),'WhiteTrim',.04)
    box('Cabin',(0,1.2,1.65),(1.7,1.8,1.4),'WhiteTrim',.1)
    box('Cabin roof',(0,1.2,2.42),(1.95,2.1,.16),'Tint_Shutter',.06)
    for s in(-1,1):box('Cabin window',(s*.86,1.2,1.85),(.04,1.2,.5),'Glass',.02)
    box('Cabin window',(0,.28,1.85),(1.3,.04,.5),'Glass',.02)
    cyl('Mast',(0,-.6,2.7),.07,3.6,'Wood','Z',8);cyl('Boom',(0,-1.4,1.6),.05,1.8,'Wood','Y',8)
    box('Flag',(0,-.35,4.3),(.03,.55,.36),'Red',.01)
    for k in range(3):cyl('Fender',(1.32,-1.4+k*1.3,.55),.16,.5,'Tint_Awning','Z',12)
    box('Lobster pot',(-.5,-1.6,1.2),(.6,.6,.5),'Rope',.06)
    sphere('Buoy',(.55,-2.2,1.15),(.22,.22,.22),'Red',1)

def beach_hut():
    w,d,h=2.6,2.6,2.5
    for k in range(9):box('Hut plank',(-w/2+(k+.5)*w/9,0,h/2),(w/9-.02,d,h),'Tint_Wall' if k%2 else 'WhiteTrim',.02)
    gable_roof(0,0,w,d,h,1.1,.25,'RoofTile',ridge_axis='Y')
    box('Hut door',(0,-d/2-.05,1.05),(1.0,.08,2.0),'Tint_Door',.04)
    box('Hut deck',(0,-d/2-.55,.1),(w+.4,1.0,.2),'Wood',.03)
    collision('Hut',(0,0,h/2),(w,d,h))

def parasol():
    cyl('Pole',(0,0,1.2),.04,2.4,'WhiteTrim','Z',8)
    cyl('Canopy',(0,0,2.3),1.5,.55,'Tint_Awning','Z',8,.02,r2=.05)
    for s in(-1,1):
        box('Deckchair seat',(s*.9,-.6,.35),(.6,1.2,.06),'Tint_Shutter',.02,rot=(-.35,0,0))
        box('Deckchair back',(s*.9,.15,.6),(.6,.06,.8),'Tint_Shutter',.02,rot=(-.5,0,0))
    box('Towel',(0,-1.8,.02),(.9,1.7,.03),'Tint_Wall',.01)

# ---------------------------------------------------------------- landmarks

def lighthouse():
    """Toy lighthouse ~16 m: tapered white tower with red bands, gallery, glowing lantern, red dome; keeper's cottage."""
    for k in range(10):
        r0=2.3-k*.11;r1=2.3-(k+1)*.11
        cyl('Tower band',(0,0,.6+k*1.2+.6),r0,1.2,'Red' if k%3==2 else 'WhiteTrim','Z',32,.02,r2=r1)
    cyl('Base',(0,0,.3),2.9,.6,'Stone','Z',32,.05)
    for k in range(3):box('Tower window',(0,-(2.1-k*.36)+.02,2.8+k*3.4),(.55,.3,.9),'Glass',.05)
    box('Tower door',(0,-2.3,1.2),(1.0,.4,1.9),'Tint_Door',.06)
    cyl('Gallery',(0,0,12.8),2.1,.3,'Red','Z',32,.05)
    for k in range(20):
        a=k*math.tau/20;cyl('Gallery post',(math.cos(a)*1.95,math.sin(a)*1.95,13.35),.04,.8,'WhiteTrim','Z',6)
    cyl('Gallery rail',(0,0,13.75),1.97,.07,'WhiteTrim','Z',32,.02)
    cyl('Lantern',(0,0,14.1),1.15,1.8,'GlassLit','Z',12,.02)
    for k in range(6):
        a=k*math.tau/6;cyl('Lantern mullion',(math.cos(a)*1.17,math.sin(a)*1.17,14.1),.05,1.8,'Red','Z',6)
    cyl('Dome',(0,0,15.4),1.35,.9,'Red','Z',24,.05,r2=.2);sphere('Finial',(0,0,16.0),(.18,.18,.3),'Gold',1)
    box('Cottage',(4.6,.8,1.5),(4.2,3.6,3.0),'WhiteTrim',.1)
    gable_roof(4.6,.8,4.2,3.6,3.0,1.4,.3,'Red')
    door(4.0,.8-1.8,0,1.0,2.1)
    window(5.6,.8-1.8,1.7,.8,.9)
    collision('Tower',(0,0,6.5),(4.4,4.4,13));collision('Cottage',(4.6,.8,1.5),(4.3,3.7,3))

def windmill():
    """Whitewashed windmill ~11 m, tiled cone cap, four lattice sails in a separate Spin_Y object (spun in Unity)."""
    for k in range(8):
        cyl('Mill tower',(0,0,.55+k*1.1),3.0-k*.14,1.1,'WhiteTrim' if k%4 else 'Canvas','Z',28,.02,r2=3.0-(k+1)*.14)
    box('Mill door',(0,-2.95,1.2),(1.2,.5,2.2),'Tint_Door',.06)
    for k in range(2):box('Mill window',(0,-(2.6-k*.4),4.5+k*3),(.7,.4,.8),'Glass',.05)
    cyl('Cap ring',(0,0,8.95),2.45,.25,'Wood','Z',28,.05)
    cyl('Cap',(0,0,10.2),2.2,2.6,'RoofTile','Z',28,.08,r2=.25);sphere('Cap knob',(0,0,11.55),(.3,.3,.3),'Wood',1)
    cyl('Axle housing',(0,-2.1,9.6),.35,1.2,'Wood','Y',16)
    sails=[cyl('Sail hub',(0,-2.8,9.6),.45,.35,'Wood','Y',16)]
    for k in range(4):
        a=k*math.pi/2+.35;d=Vector((math.cos(a),0,math.sin(a)))
        for t in(1.5,3.1,4.7):
            o=box('Sail cloth',Vector((0,-2.95,9.6))+d*t,(1.5,.06,1.1),'Canvas',.02);o.rotation_euler=(0,-a,0);sails.append(o)
        o=box('Sail spar',Vector((0,-3.0,9.6))+d*3.0,(6.0,.14,.14),'Wood',.03);o.rotation_euler=(0,-a,0);sails.append(o)
        for t in(1.6,3.2,4.8):
            o=box('Sail lattice',Vector((0,-3.02,9.6))+d*t,(.08,.1,1.3),'Wood',.02);o.rotation_euler=(0,-a,0);sails.append(o)
    SPIN.extend(sails)
    collision('Tower',(0,0,5),(5.6,5.6,10))


# ---------------------------------------------------------------- countryside and roadside

def barn():
    """Red timber barn: board-and-batten walls, big white-trimmed doors, loft hatch, weathervane."""
    w,d,h=8.0,10.0,4.6
    box('Barn walls',(0,0,h/2),(w,d,h),'Red',.1)
    for k in range(17):box('Batten',(-w/2+.25+k*(w-.5)/16,-d/2-.05,h/2),(.08,.08,h),'Red',.02)
    gable_roof(0,0,w,d,h,3.0,.4,'RoofSlate',ridge_axis='Y')
    for s in(-1,1):
        box('Door frame',(s*.95,-d/2-.06,1.5),(1.9,.06,3.1),'WhiteTrim',.02)
        box('Barn door',(s*.95,-d/2-.14,1.5),(1.7,.1,2.9),'Red',.04)
        box('Door brace',(s*.95,-d/2-.24,1.5),(.14,.05,3.3),'WhiteTrim',.02,rot=(0,s*.55,0))
    box('Loft hatch',(0,-d/2-.1,h+1.0),(1.4,.12,1.2),'WhiteTrim',.04)
    cyl('Vane post',(0,0,h+3.6),.04,1.2,'Metal','Z',8);box('Vane arrow',(0,0,h+4.1),(1.1,.05,.08),'Black',.01)
    box('Vane rooster',(.2,0,h+4.35),(.4,.05,.4),'Black',.02)
    collision('Barn',(0,0,h/2),(w+.3,d+.3,h))

def fence():
    """6 m white picket fence section along X."""
    for k in range(13):
        x=-3+k*.5;box('Picket',(x,0,.5),(.12,.05,1.0),'WhiteTrim',.02);sphere('Picket tip',(x,0,1.0),(.07,.03,.1),'WhiteTrim',1)
    for z in(.3,.72):box('Fence rail',(0,.04,z),(6.0,.05,.1),'WhiteTrim',.02)
    collision('Fence',(0,0,.5),(6,.2,1))

def hay_bale():
    cyl('Bale',(0,0,.65),.65,1.2,'Yellow','X',20,.08)
    for x in(-.35,.35):cyl('Twine',(x,0,.65),.66,.04,'Rope','X',20,.01)
    collision('Bale',(0,0,.65),(1.2,1.3,1.3))

def tractor():
    box('Tractor body',(0,.2,1.15),(1.2,2.0,.9),'Green',.12)
    box('Tractor hood',(0,-.9,1.0),(.9,1.3,.7),'Green',.1)
    box('Cab',(0,.8,2.0),(1.3,1.1,1.1),'WhiteTrim',.1);box('Cab glass',(0,.24,2.05),(1.05,.04,.75),'Glass',.02)
    box('Cab roof',(0,.8,2.62),(1.5,1.3,.12),'Green',.04)
    cyl('Exhaust',(.35,-1.2,1.8),.07,1.0,'Metal','Z',8)
    for s in(-1,1):
        cyl('Big wheel',(s*.95,.75,.85),.85,.5,'Black','X',24,.08);cyl('Hub',(s*1.21,.75,.85),.4,.06,'Yellow','X',16)
        cyl('Small wheel',(s*.75,-1.25,.5),.5,.35,'Black','X',20,.06);cyl('Hub',(s*.93,-1.25,.5),.24,.05,'Yellow','X',12)
    collision('Tractor',(0,0,1.1),(2.4,3.6,2.2))

def tent():
    """A-frame camping tent with open flap and guy lines."""
    v=[(-1.3,-1.4,0),(1.3,-1.4,0),(0,-1.5,1.6),(-1.3,1.4,0),(1.3,1.4,0),(0,1.5,1.6)]
    poly('Tent',v,[(0,2,5,3),(1,4,5,2),(0,1,2),(3,5,4)],'Tint_Wall',.02)
    box('Tent door',(0,-1.43,.55),(.7,.04,1.0),'Tint_Awning',.02)
    box('Groundsheet',(0,-2.0,.02),(1.8,1.0,.03),'Tint_Awning',.01)
    for s in(-1,1):cyl('Tent pole',(0,s*1.5,.8),.03,1.6,'Metal','Z',6)
    collision('Tent',(0,0,.8),(2.6,2.9,1.6))

def campfire():
    for k in range(8):
        a=k*math.tau/8;sphere('Fire stone',(math.cos(a)*.6,math.sin(a)*.6,.12),(.2,.18,.15),'StoneDark',1)
    for k in range(3):box('Log',(0,0,.18),(1.0,.16,.16),'WoodDark',.06,rot=(0,0,k*1.05))
    cyl('Flame',(0,0,.5),.3,.7,'Yellow','Z',8,.02,r2=.02);cyl('Flame core',(0,0,.45),.2,.5,'Red','Z',8,.02,r2=.02)
    for s in(-1,1):box('Log seat',(s*1.6,0,.25),(.45,1.6,.45),'Wood',.15)

def caravan():
    """Rounded retro caravan (~5 m), door on the -Y side, drawbar toward +X."""
    box('Caravan body',(0,0,1.45),(4.4,2.1,2.0),'WhiteTrim',.55,segments=4)
    box('Caravan stripe',(0,0,1.25),(4.42,2.12,.35),'Tint_Awning',.1)
    box('Caravan skirt',(0,0,.65),(4.3,2.0,.5),'Tint_Wall',.2)
    for x in(-1.2,1.2):box('Caravan window',(x,-1.06,1.8),(1.0,.05,.6),'Glass',.1)
    box('Caravan door',(0,-1.07,1.2),(.7,.05,1.6),'Tint_Wall',.05)
    box('Front window',(2.21,0,1.85),(.05,1.4,.5),'Glass',.1)
    cyl('Caravan wheel',(0,-1.05,.42),.42,.3,'Black','Y',20,.05);cyl('Caravan wheel',(0,1.05,.42),.42,.3,'Black','Y',20,.05)
    box('Drawbar',(2.8,0,.55),(1.4,.12,.12),'Metal',.03);cyl('Jockey wheel',(3.3,0,.2),.18,.1,'Black','Y',12)
    awning(0,-1.07,2.35,3.0,1.4,6)
    collision('Caravan',(0,0,1.3),(4.5,2.2,2.6))

def telescope():
    cyl('Scope post',(0,0,.55),.08,1.1,'Tint_Shutter','Z',10);cyl('Scope base',(0,0,.05),.3,.1,'Tint_Shutter','Z',12)
    cyl('Scope body',(0,-.15,1.28),.16,.8,'Tint_Shutter','Y',14);cyl('Scope eye',(0,.3,1.22),.09,.2,'Black','Y',10)
    cyl('Scope lens',(0,-.57,1.33),.17,.06,'Glass','Y',14)
    collision('Scope',(0,0,.7),(.5,.5,1.4))

def kiosk():
    """Ice-cream kiosk with a giant cone on the roof and a striped awning."""
    w,d,h=3.2,2.6,2.6
    box('Kiosk',(0,0,h/2),(w,d,h),'Tint_Wall',.2)
    box('Counter',(0,-d/2-.05,1.0),(w-.4,.3,.12),'WhiteTrim',.04)
    box('Serving hatch',(0,-d/2-.02,1.65),(w-.7,.06,1.0),'GlassLit',.03)
    awning(0,-d/2,2.45,w,1.0,6)
    box('Kiosk roof',(0,0,h+.1),(w+.3,d+.3,.2),'WhiteTrim',.08)
    cyl('Cone',(0,0,h+.9),.05,1.3,'Yellow','Z',16,.03,r2=.5)
    for k,c in enumerate(['FlowerPink','FlowerWhite','FlowerRed']):sphere('Scoop',(0,0,h+1.75+k*.42),(.52-k*.06,.52-k*.06,.42),c,2)
    collision('Kiosk',(0,0,h/2),(w+.2,d+.2,h))

def flower_bed():
    """Round raised flower bed with rings of colour."""
    cyl('Bed wall',(0,0,.25),2.2,.5,'Stone','Z',28,.06);cyl('Bed soil',(0,0,.5),2.0,.04,'Soil','Z',28,.01)
    for ring,(r,n,c) in enumerate([(1.7,16,'FlowerRed'),(1.15,11,'FlowerYellow'),(.6,6,'FlowerPink')]):
        for k in range(n):
            a=k*math.tau/n+ring*.3;sphere('Bed bloom',(math.cos(a)*r,math.sin(a)*r,.66+ring*.08),(.24,.24,.2),c,1)
    sphere('Bed centre',(0,0,.95),(.4,.4,.5),'Leaf',2)
    collision('Bed',(0,0,.35),(4.4,4.4,.7))

def picnic_table():
    box('Table top',(0,0,.78),(2.0,.9,.08),'Wood',.03)
    for s in(-1,1):
        box('Table bench',(0,s*.75,.45),(2.0,.32,.07),'Wood',.03)
        for x in(-.8,.8):box('Table leg',(x,s*.35,.4),(.08,.08,.85),'WoodDark',.02,rot=(s*.5,0,0))
    collision('Table',(0,0,.45),(2.1,2.0,.9))

def chevron_sign():
    """Bend warning board: yellow with black chevrons, pointing +X."""
    for s in(-1,1):cyl('Sign leg',(s*.9,0,.9),.05,1.8,'Metal','Z',8)
    box('Chevron board',(0,-.05,1.55),(2.2,.08,.8),'Yellow',.04)
    for k in range(3):
        for sgn in(-1,1):box('Chevron',(-.6+k*.6,-.1,1.55+sgn*.15),(.4,.04,.12),'Black',.02,rot=(0,sgn*.8,0))
    collision('Sign',(0,0,1.2),(2.2,.3,2.2))

def telegraph_pole():
    cyl('Pole',(0,0,3.5),.14,7.0,'WoodDark','Z',10,.03,r2=.11)
    box('Crossarm',(0,0,6.6),(2.0,.12,.12),'WoodDark',.03)
    for x in(-.8,-.3,.3,.8):cyl('Insulator',(x,0,6.75),.06,.16,'WhiteTrim','Z',8)
    collision('Pole',(0,0,3.5),(.3,.3,7))


ASSETS={'house_a':house_a,'house_b':house_b,'shop':shop,'cafe':cafe,'post_office':post_office,
    'lamp_post':lamp_post,'bench':bench,'fountain':fountain,'tree_round':tree_round,'tree_cypress':tree_cypress,
    'tree_pine':tree_pine,'palm':palm,'bush':bush,
    'sea_wall':sea_wall,'promenade_rail':promenade_rail,'harbour_steps':harbour_steps,'pier':pier,'boat':boat,'beach_hut':beach_hut,'parasol':parasol,
    'lighthouse':lighthouse,'windmill':windmill,
    'barn':barn,'fence':fence,'hay_bale':hay_bale,'tractor':tractor,'tent':tent,'campfire':campfire,'caravan':caravan,
    'telescope':telescope,'kiosk':kiosk,'flower_bed':flower_bed,'picnic_table':picnic_table,'chevron_sign':chevron_sign,'telegraph_pole':telegraph_pole}
ONLY=[a for a in args if a in ASSETS] or list(ASSETS)

# ---------------------------------------------------------------- build / export / render

def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True);PARTS.clear();SPIN.clear()

def join_group(objs,name):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:o.select_set(True)
    bpy.context.view_layer.objects.active=objs[0]
    bpy.ops.object.convert(target='MESH');bpy.ops.object.join();j=bpy.context.object;j.name=name;return j
def join_visual(name):
    if SPIN:
        # Sails become their own object, pivoted on the axle, so Unity can spin them.
        from mathutils import Matrix
        for o in SPIN:
            if o in PARTS:PARTS.remove(o)
        spin=join_group(list(SPIN),'Spin_Y')
        hub=Vector((0,-2.8,9.6))
        spin.data.transform(Matrix.Translation(-(hub-spin.location)));spin.location=hub
    bpy.ops.object.select_all(action='DESELECT')
    for o in PARTS:o.select_set(True)
    bpy.context.view_layer.objects.active=PARTS[0]
    bpy.ops.object.convert(target='MESH')
    # Parts share material datablocks, so the join already yields one slot per colour.
    bpy.ops.object.join();v=bpy.context.object;v.name='Visual'
    assert len({m.name for m in v.data.materials})==len(v.data.materials),'duplicate material slots'
    return v

def build(name):
    reset();random.seed(name);ASSETS[name]()
    v=join_visual(name)
    tris=sum(len(p.vertices)-2 for p in v.data.polygons)
    SOURCE.mkdir(parents=True,exist_ok=True);bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/(name+'.blend')))
    if EXPORT:
        TARGET.mkdir(parents=True,exist_ok=True)
        bpy.ops.export_scene.fbx(filepath=str(TARGET/(name+'.fbx')),use_selection=False,object_types={'MESH','EMPTY'},
            apply_unit_scale=True,bake_space_transform=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,use_mesh_modifiers=True,bake_anim=False)
    print(f'BAYVIEW_KIT {name} tris={tris}')
    return v

def render_lineup(names,path):
    reset();x=0;spacing={'house_a':12,'house_b':10,'shop':12,'cafe':14,'post_office':12,'fountain':8,'lighthouse':14,'windmill':12,'boat':8,'beach_hut':5,'parasol':5,'pier':6,'barn':11,'caravan':7,'tractor':5,'kiosk':5,'flower_bed':6}
    for n in names:
        before=set(bpy.data.objects);ASSETS[n]()
        step=spacing.get(n,5);x+=step/2
        for o in set(bpy.data.objects)-before:
            if o.name.startswith('Collision'):bpy.data.objects.remove(o);continue
            o.location.x+=x
        x+=step/2
    bpy.ops.mesh.primitive_plane_add(size=600,location=(x/2,0,0));g=bpy.context.object;g.data.materials.append(mat('Sand'))
    scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=48;scene.cycles.use_denoising=True;scene.view_settings.view_transform='Standard'
    scene.render.resolution_x=2400;scene.render.resolution_y=900
    world=bpy.data.worlds.new('Sky');scene.world=world;world.use_nodes=True;world.node_tree.nodes['Background'].inputs['Color'].default_value=(.55,.75,1,1);world.node_tree.nodes['Background'].inputs['Strength'].default_value=.55
    bpy.ops.object.light_add(type='SUN',rotation=(math.radians(50),0,math.radians(-35)));bpy.context.object.data.energy=2.6
    back=max(x*1.02,34);bpy.ops.object.camera_add(location=(x/2,-back,back*.30+2),rotation=(math.radians(76),0,0));scene.camera=bpy.context.object;scene.camera.data.lens=40
    scene.render.filepath=str(path);bpy.ops.render.render(write_still=True)

if __name__=='__main__':
    if EXPORT:
        import json;TARGET.mkdir(parents=True,exist_ok=True)
        # Unity's FBX import drops these colours; BayviewKit.cs reads them by material name.
        (TARGET/'palette.json').write_text(json.dumps({'entries':[{'name':k,'r':v[0],'g':v[1],'b':v[2]} for k,v in PALETTE.items()]},indent=1))
    for n in ONLY:build(n)
    if RENDER:
        RENDER.mkdir(parents=True,exist_ok=True)
        render_lineup([n for n in ONLY if n in('house_a','house_b','shop','cafe','post_office','fountain')],RENDER/'buildings.png')
        render_lineup([n for n in ONLY if n in('lighthouse','windmill','boat','beach_hut','parasol','pier')],RENDER/'landmarks.png')
        render_lineup([n for n in ONLY if n in('barn','fence','hay_bale','tractor','tent','campfire','caravan','telescope','kiosk','flower_bed','picnic_table','chevron_sign','telegraph_pole')],RENDER/'countryside.png')
        render_lineup([n for n in ONLY if n in('lamp_post','bench','tree_round','tree_cypress','tree_pine','palm','bush')],RENDER/'props.png')
