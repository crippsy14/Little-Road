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
from mathutils import Vector, noise

ROOT=Path(__file__).resolve().parents[1]
args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
RENDER=Path(args[args.index('--render')+1]).resolve() if '--render' in args else None
EXPORT='--no-export' not in args
TARGET=ROOT/'unity/Assets/Resources/Bayview'
SOURCE=ROOT/'source-art/bayview'

# ---------------------------------------------------------------- materials

PALETTE={
    'Tint_Wall':(.97,.89,.72),'Tint_Shutter':(.10,.55,.62),'Tint_Awning':(.92,.22,.24),'Tint_Door':(.12,.42,.62),
    'WhiteTrim':(.97,.96,.92),'RoofTile':(.86,.34,.18),'RoofTileLight':(.95,.48,.26),'RoofSlate':(.30,.36,.46),'RoofSlateDark':(.22,.26,.33),'Tint_WallStone':(.55,.57,.6),'SlateRock':(.33,.34,.38),
    'Stone':(.86,.80,.66),'StoneDark':(.66,.60,.50),'Wood':(.56,.34,.17),'WoodDark':(.36,.22,.12),
    'Glass':(.16,.34,.52),'GlassLit':(1.0,.86,.52),'Metal':(.30,.33,.36),'MetalLight':(.72,.75,.78),'Gold':(.98,.74,.16),
    'Leaf':(.23,.48,.17),'LeafLight':(.36,.60,.21),'LeafDark':(.11,.31,.14),'Bark':(.42,.28,.16),
    'FlowerRed':(.95,.20,.24),'FlowerPink':(1.0,.56,.74),'FlowerYellow':(1.0,.84,.22),'FlowerWhite':(1,1,.97),'FlowerBlue':(.52,.56,1.0),
    'Soil':(.40,.26,.15),'Water':(.25,.68,.86),'Canvas':(.97,.95,.88),'Red':(.90,.16,.14),'Yellow':(1.0,.80,.10),
    'Black':(.05,.05,.06),'Rope':(.78,.66,.44),'Sand':(.96,.87,.64),'Blue':(.16,.40,.86),'Green':(.18,.62,.36),
    'Blossom':(1.0,.70,.82),'BlossomDeep':(.96,.52,.70),'Autumn':(.96,.50,.14),'AutumnRed':(.86,.26,.16),'BirchBark':(.94,.93,.89),
    'Fir':(.10,.34,.24),'FirLight':(.16,.44,.28),'Rock':(.58,.55,.50),'RockDark':(.42,.40,.38),'Moss':(.42,.56,.26),'Reed':(.50,.62,.28),'ReedHead':(.40,.25,.13),'Poplar':(.34,.60,.24),
    'PitOrange':(1.0,.40,.06),'PitWhite':(.97,.97,.95),'PitNavy':(.10,.15,.34),'PitGarage':(.08,.72,.66),'PitParts':(1.0,.74,.06),'PitBody':(1.0,.34,.64),'Tarmac':(.22,.23,.25),
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
SPIN_HUB=None;SPIN_AXIS='Y'
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
FOLIAGE=('Leaf','Fir','Poplar','Blossom','Autumn','Moss')
def sphere(name,loc,scale,material,subdiv=2):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdiv,radius=1,location=loc);o=bpy.context.object;o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    # Canopies: lumpy leaf masses rather than smooth balls (noise along the normal).
    size=min(scale)
    if material.startswith(FOLIAGE) and size>=.45:
        seed=Vector((loc[0]*1.7+3.1,loc[1]*2.3+7.7,loc[2]*1.1+1.3))
        for v in o.data.vertices:
            d=noise.noise(v.co*(1.9/size)+seed)*.8+noise.noise(v.co*(4.5/size)+seed*2)*.35
            v.co+=v.normal*d*size*.22
        o.data.update()
    return _finish(o,name,material,smooth=True)
def satellites(r,z0,z1,materials,n,seed,size=(.45,.7)):
    """Small leaf clumps round a canopy's edge: breaks the silhouette up."""
    rnd=random.Random(seed)
    for k in range(n):
        a=k*math.tau/n+rnd.uniform(-.25,.25);rr=r*rnd.uniform(.85,1.05);s=rnd.uniform(*size)
        sphere('Leaf clump',(math.cos(a)*rr,math.sin(a)*rr,rnd.uniform(z0,z1)),(s,s,s*.85),materials[k%len(materials)],2)
def star_cone(name,z0,r,h,material,points=9,seed=0):
    """Fir tier: a cone whose rim alternates bough tips and notches, tips drooping."""
    rnd=random.Random(seed);verts=[(0,0,z0+h)];n=points*2
    for k in range(n):
        a=k*math.tau/n+rnd.uniform(-.06,.06);rr=r*(1 if k%2==0 else .68);verts.append((math.cos(a)*rr,math.sin(a)*rr,z0-(.18 if k%2==0 else 0)))
    verts.append((0,0,z0+.1))
    faces=[(0,1+k,1+(k+1)%n) for k in range(n)]+[(n+1,1+(k+1)%n,1+k) for k in range(n)]
    faces=[(f[0],f[2],f[1]) for f in faces[:n]]+faces[n:]
    return poly(name,verts,faces,material,smooth=False)
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
        if eave>2.4:
            for side in(-1,1):cyl('Gutter',(cx,cy+side*(half-.02),eave+.02),.09,length,'Metal','X',10)
            for sx in(-1,1):
                cyl('Downpipe',(cx+sx*(w/2-.35),cy+d/2+.14,(eave+.3)/2),.055,eave-.3,'Metal','Z',8)
                box('Downpipe shoe',(cx+sx*(w/2-.35),cy+d/2+.24,.25),(.14,.3,.12),'Metal',.02)
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
        if eave>2.4:
            for side in(-1,1):cyl('Gutter',(cx+side*(half-.02),cy,eave+.02),.09,length,'Metal','Y',10)
            for sx in(-1,1):
                cyl('Downpipe',(cx+sx*(w/2+.14),cy+d/2-.35,(eave+.3)/2),.055,eave-.3,'Metal','Z',8)
                box('Downpipe shoe',(cx+sx*(w/2+.24),cy+d/2-.35,.25),(.3,.14,.12),'Metal',.02)
        for side in(-1,1):
            y=cy+side*(d/2)
            wedge('Gable',(cx-w/2,y,eave),(cx+w/2,y,eave),(cx,y,eave+rise),(0,side,0),'Tint_Wall')
            for s in(-1,1):box('Fascia',(cx+s*half*.5,cy+side*(d/2+over*.8),eave+rise*.5+.02),(math.hypot(half,rise)+.1,.12,.22),'WhiteTrim',.03,rot=(0,s*slope,0))

def window(x,y,z,w=1.2,h=1.5,shutters=True,flowers=True,face=-1,glass='Glass'):
    """Window on a wall whose outward normal is face*Y (face=-1 front)."""
    o=face*.02
    box('Window reveal',(x,y+face*.145,z),(w+.1,.02,h+.1),'Black',0)
    box('Window lintel',(x,y+face*.1,z+h/2+.3),(w+.6,.2,.26),'Stone',.04)
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
    # (Street review, 5 Oct: a 16 cm slab floated or showed as a wedge where the garden falls away.) A deep block, sunk.
    if face<0:box('Step',(x,y+face*.35,z0-.17),(w+.7,.7,.66),'Stone',.05)   # (back doors: none; 5 Oct, a buried back step showed as a sliver on the next lawn)
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
    box('Planter',(x,y,.15),(w,d,1.1),'Stone',.08)   # (down to -0.4 m: on a falling garden it stood in the air)
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
    # (Street review, 4 Oct: clear of the window's flower box; 5 Oct: at x=3.1 it still sat under the window's end and read as
    # merged with its flower box.) On the plain wall between the door and the window.
    planter_box(-.45,-d/2-1.0,1.3)
    side_paint(w,(1.7,4.4))
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
    side_paint(w,(1.7,4.9),(-1.5,1.5))
    collision('Body',(0,0,h/2),(w+.3,d+.3,h))

def shop(kind='shop'):
    """One-and-a-half storey shop: big display windows, striped awning, sign board, crates of goods."""
    w,d,h=9.0,8.0,5.4;walls(w,d,h)
    gable_roof(0,0,w,d,h,2.2)
    # The back (it faces the harbour and the next street): storeroom windows and a back door.
    for x in(-2.6,2.6):window(x,d/2,3.4,1.1,1.2,shutters=True,flowers=False,face=1)
    window(-2.6,d/2,1.4,1.0,1.1,shutters=False,flowers=False,face=1);door(2.4,d/2,0,1.1,2.3,face=1,canopy=False)
    box('Shopfront',(0,-d/2-.1,1.6),(w-.9,.2,3.0),'Tint_Door',.06)
    for x in(-2.4,2.4):
        box('Display window',(x,-d/2-.22,1.75),(2.6,.08,2.1),'GlassLit' if kind=='cafe' else 'Glass',.02)
        box('Display frame',(x,-d/2-.19,1.75),(2.85,.1,2.35),'WhiteTrim',.04)
        box('Display bar',(x,-d/2-.28,1.75),(.07,.06,2.1),'WhiteTrim',.02)
    door(0,-d/2-.1,0,1.4,2.6,canopy=False)
    awning(0,-d/2-.1,3.95,w-.6,1.8)   # (clear of the door head: street review, 4 Oct)
    sign_board(0,-d/2-.15,4.55,w-2.2,.85,'WhiteTrim',kind)
    if kind=='cafe':
        # (Closer in: the outer chairs stood at the kerb, street review, 4 Oct.)
        for i,(x,y) in enumerate([(-3.3,-d/2-2.9),(0,-d/2-3.3),(3.3,-d/2-2.9)]):
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
    lobes=[((0,0,3.4),1.9),((.9,.3,3.0),1.3),((-.8,-.4,3.1),1.35),((.2,-.7,4.2),1.2),((-.3,.6,4.3),1.1)]
    rnd=random.Random(7)
    for k in range(9):
        a=k*math.tau/9+rnd.uniform(-.2,.2);r=rnd.uniform(1.25,1.7);lobes.append(((math.cos(a)*r,math.sin(a)*r,rnd.uniform(2.7,4.3)),rnd.uniform(.55,.8)))
    for i,(p,s) in enumerate(lobes):
        sphere('Canopy',p,(s,s,s*.9),'Leaf' if i%2==0 else 'LeafLight',3 if s>1 else 2)
    for s in(-1,1):cyl('Bough',(s*.45,0,2.5),.1,1.4,'Bark','X',8)
    collision('Trunk',(0,0,1.5),(.6,.6,3))

def tree_cypress():
    cyl('Trunk',(0,0,.5),.2,1,'Bark','Z',8)
    # A flame of overlapping lumpy sections, narrowing and leaning a touch as it rises.
    for k,(z,r,h) in enumerate([(2.0,.95,1.5),(3.1,.88,1.4),(4.2,.72,1.25),(5.2,.52,1.05),(6.0,.3,.7)]):
        sphere('Cypress',(.03*k,.02*k,z),(r,r*.96,h),'LeafDark',2)
    collision('Trunk',(0,0,1.5),(.6,.6,3))

def tree_pine():
    """Umbrella (stone) pine: tall bare trunk, flat puffy crown."""
    cyl('Trunk',(0,0,2.6),.26,5.2,'Bark','Z',10,.05,r2=.18)
    for s in(-1,1):cyl('Branch',(s*.7,0,5.0),.1,1.8,'Bark','X',8)
    crown=[(0,0,2.4),(1.5,.4,1.5),(-1.5,-.3,1.6),(.4,1.3,1.4),(-.3,-1.4,1.4),(2.2,-.8,.9),(-2.1,.9,.9),(1.1,-1.9,.8),(-1.0,1.9,.8)]
    for i,(x,y,r) in enumerate(crown):
        sphere('Crown',(x,y,6.0+(i%2)*.25+(.15 if i>4 else 0)),(r,r,r*.5),'Leaf' if i%2 else 'LeafDark',3 if r>1.3 else 2)
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

WALL_RND=random.Random(29)
def sea_wall():
    """12 m masonry sea-wall segment: top at 0, foot at -14. Face toward -Y (the sea). Coping, drain, weed line."""
    L,H=12.0,14.0
    box('Wall core',(0,.6,-H/2),(L,1.2,H),'StoneDark',.02)
    for row in range(int(H/.9)):
        z=-(row+.5)*.9;off=(row%2)*.9
        for k in range(-1,int(L/1.8)+2):
            x0=max(-L/2,-L/2+off+(k-.5)*1.8+.03);x1=min(L/2,-L/2+off+(k+.5)*1.8-.03)
            if x1-x0<.3:continue
            box('Wall block',((x0+x1)/2,-.06,z),(x1-x0,.2,.84),'StoneDark' if WALL_RND.random()<.1 else 'Stone',.05)
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


# ---------------------------------------------------------------- Hillcrest (hilltop market town)

def church():
    """Hilltop chapel: nave with arched door and rose window, square bell tower with clock and pyramid cap."""
    w,d,h=8.0,13.0,6.5
    box('Nave',(0,1.5,h/2),(w,d,h),'Tint_Wall',.12,segments=3)
    box('Nave plinth',(0,1.5,.3),(w+.2,d+.2,.6),'Stone',.06)
    gable_roof(0,1.5,w,d,h,3.2,.45,'RoofTile',ridge_axis='Y')
    box('Door arch',(0,-d/2+1.5-.08,1.7),(1.9,.2,3.4),'Stone',.08)
    box('Church door',(0,-d/2+1.5-.16,1.45),(1.4,.1,2.9),'WoodDark',.05)
    cyl('Door arch top',(0,-d/2+1.5-.16,2.9),.7,.1,'WoodDark','Y',20)
    cyl('Rose window frame',(0,-d/2+1.5-.1,h+1.1),.95,.16,'Stone','Y',28);cyl('Rose window',(0,-d/2+1.5-.2,h+1.1),.75,.08,'GlassLit','Y',28)
    for k in range(6):
        a=k*math.pi/3;box('Rose spoke',(math.cos(a)*.37,-d/2+1.5-.26,h+1.1+math.sin(a)*.37),(.7,.04,.06),'Stone',.01,rot=(0,-a,0))
    for y in(-2,2,5):
        for sd in(-1,1):
            box('Nave window',(sd*(w/2+.06),y+1.5,3.6),(.2,1.0,2.2),'Glass',.05);cyl('Nave window arch',(sd*(w/2+.06),y+1.5,4.7),.5,.2,'Glass','X',16)
    tx,ty=w/2+.2,-d/2+2.4
    box('Bell tower',(tx,ty,6.5),(3.4,3.4,13),'Tint_Wall',.1)
    box('Tower plinth',(tx,ty,.3),(3.6,3.6,.6),'Stone',.05)
    for z in(7.2,12.6):box('Tower band',(tx,ty,z),(3.6,3.6,.3),'Stone',.05)
    for sd in(-1,1):box('Belfry opening',(tx,ty+sd*1.72,11.2),(1.4,.1,2.0),'Black',.05);box('Belfry opening',(tx+sd*1.72,ty,11.2),(.1,1.4,2.0),'Black',.05)
    cyl('Bell',(tx,ty,11.0),.5,.8,'Gold','Z',16,.05,r2=.3)
    cyl('Clock face',(tx,ty-1.74,9.0),.7,.1,'WhiteTrim','Y',28);cyl('Clock rim',(tx,ty-1.72,9.0),.78,.08,'Gold','Y',28)
    box('Clock hand',(tx,ty-1.82,9.15),(.05,.03,.4),'Black',.01);box('Clock hand',(tx+.12,ty-1.82,9.0),(.3,.03,.05),'Black',.01)
    cyl('Tower cap',(tx,ty,14.4),2.6,2.4,'RoofTile','Z',4,.05,r2=.05)
    cyl('Cross post',(tx,ty,16.1),.06,1.0,'Gold','Z',8);box('Cross bar',(tx,ty,16.3),(.5,.06,.06),'Gold',.01)
    collision('Nave',(0,1.5,h/2),(w+.3,d+.3,h));collision('Tower',(tx,ty,6.5),(3.6,3.6,13))

def market_stall():
    """Market stall: striped canopy on poles, counter of crates heaped with produce."""
    for x in(-1.4,1.4):
        for y in(-.9,.9):cyl('Stall pole',(x,y,1.2),.05,2.4,'WoodDark','Z',8)
    for k in range(6):
        box('Canopy stripe',(-1.5+k*.6+.3,0,2.45),(.6,2.3,.06),'Tint_Awning' if k%2==0 else 'Canvas',.02,rot=(.12,0,0))
    for k in range(6):cyl('Canopy scallop',(-1.5+k*.6+.3,-1.17,2.28),.3,.05,'Tint_Awning' if k%2==0 else 'Canvas','Y',12,.01)
    box('Counter',(0,-.55,.8),(3.0,.9,.12),'Wood',.03)
    for x in(-1.35,1.35):box('Counter leg',(x,-.55,.4),(.12,.8,.8),'WoodDark',.02)
    rnd=random.Random(3)
    for i,x in enumerate((-.95,0,.95)):
        box('Produce crate',(x,-.55,1.02),(.8,.7,.3),'Wood',.04)
        for s in(-1,1):box('Crate slat gap',(x,-.55+s*.36,1.02),(.78,.02,.03),'WoodDark',0)
        colour=['FlowerRed','FlowerYellow','LeafLight','FlowerBlue','FlowerPink'][i+rnd.randint(0,2)]
        # Heaped produce: a bottom layer of six and a top layer of three, sizes varying.
        for k in range(6):
            r=rnd.uniform(.11,.14);sphere('Produce',(x-.25+(k%3)*.25+rnd.uniform(-.02,.02),-.55+(k//3-.5)*.25,1.17+r),(r,r,r*.9),colour,2)
        for k in range(3):
            r=rnd.uniform(.11,.13);sphere('Produce',(x-.125+(k%2)*.25,-.55+(k//2-.25)*.2,1.42+r*.6),(r,r,r*.9),colour,2)
        if colour=='LeafLight':
            for k in range(3):sphere('Produce leaf',(x-.2+k*.2,-.5,1.5),(.1,.05,.03),'Leaf',1)
    box('Price board',(1.2,-1.05,1.6),(.6,.05,.45),'Black',.02,rot=(.2,0,0))
    for k in range(3):box('Chalk price',(1.12+(k%2)*.06,-1.09,1.72-k*.12),(.34-(k%2)*.12,.01,.04),'WhiteTrim',0,rot=(.2,0,0))
    collision('Stall',(0,-.3,.9),(3.1,1.5,1.8))

def town_gate():
    """Stone arch over the road with a sign board (SignAnchor) and potted cypresses. Road runs along Y; 10 m clear span."""
    for sd in(-1,1):
        box('Gate pier',(sd*6.0,0,3.2),(1.6,1.6,6.4),'Stone',.08)
        box('Pier cap',(sd*6.0,0,6.55),(1.9,1.9,.3),'WhiteTrim',.05)
        cyl('Pier finial',(sd*6.0,0,7.0),.3,.6,'Stone','Z',12,.04,r2=.1)
        cyl('Gate pot',(sd*7.6,-.6,.4),.45,.8,'RoofTile','Z',14,.04,r2=.55)
        sphere('Pot cypress',(sd*7.6,-.6,2.0),(.45,.45,1.4),'LeafDark',2)
    box('Arch beam',(0,0,6.9),(13.6,1.2,1.0),'Stone',.08)
    for k in range(9):box('Arch voussoir',(-4.8+k*1.2,-.62,6.9),(1.1,.1,.9),'StoneDark' if k%2 else 'Stone',.03)
    sign_board(0,-.6,5.7,7.0,1.2,'WhiteTrim','gate')
    # The same name on the far side (street review, 4 Oct: from behind the board was blank).
    for k in range(9):box('Arch voussoir',(-4.8+k*1.2,.62,6.9),(1.1,.1,.9),'StoneDark' if k%2 else 'Stone',.03)
    sign_board(0,.6,5.7,7.0,1.2,'WhiteTrim','gateback',face=1)
    collision('Left pier',(-6,0,3.2),(1.6,1.6,6.4));collision('Right pier',(6,0,3.2),(1.6,1.6,6.4))

def vine_row():
    """12 m vineyard row along X: posts, wire, leafy vines with grape bunches."""
    for k in range(7):cyl('Vine post',(-6+k*2,0,.75),.05,1.5,'WoodDark','Z',6)
    for z in(.7,1.3):cyl('Vine wire',(0,0,z),.012,12,'Metal','X',4)
    rnd=random.Random(11)
    # One continuous leafy hedge trained along the wires, lumpy along its length.
    for k in range(4):
        o=sphere('Vine canopy',(-4.5+k*3.0,0,1.05),(1.75,.34,.36),'Leaf' if k%2 else 'LeafLight',3)
        seed=Vector((k*3.1,1.7,.4))
        for v in o.data.vertices:v.co+=v.normal*(noise.noise(v.co*2.3+seed)*.09+noise.noise(v.co*6+seed)*.03)
        o.data.update()
    for k in range(9):
        x=-5.4+k*1.35+rnd.uniform(-.2,.2)
        for s2 in(-1,1):sphere('Grapes',(x,s2*.3,.8+rnd.uniform(-.05,.05)),(.09,.08,.15),'FlowerBlue',1)

def motors():
    """Hillcrest Motors: bigger workshop with an open bay (7 m wide), sign board, tyre stack and a fuel pump."""
    w,d,h=13.0,11.0,5.2
    for sd in(-1,1):box('Workshop wall',(sd*(w/2-.6),0,h/2),(1.2,d,h),'Tint_Wall',.1)
    box('Workshop back',(0,d/2-.2,h/2),(w,.4,h),'Tint_Wall',.1)
    box('Bay lintel',(0,-d/2+.3,h-.5),(w,.6,1.0),'Tint_Wall',.08)
    box('Workshop roof',(0,0,h+.15),(w+.8,d+.8,.3),'RoofSlate',.08)
    box('Workshop floor',(0,0,.02),(w-2.4,d-.4,.04),'StoneDark',.01)
    for k in range(9):box('Roof ridge',(0,-d/2+.6+k*1.25,h+.36),(w+.6,.12,.14),'MetalLight',.02)
    sign_board(0,-d/2+.1,h+1.05,w-2.5,1.1,'Yellow','motors')
    box('Bay stripe',(-3.4,-d/2-.2,.03),(.18,1.4,.02),'Yellow',.005);box('Bay stripe',(3.4,-d/2-.2,.03),(.18,1.4,.02),'Yellow',.005)
    for k in range(4):cyl('Tyre stack',(-w/2-1.1,-d/2+1.2,.25+k*.42),.45,.38,'Black','Z',16,.06)
    box('Fuel pump',(w/2+1.3,-d/2+1.0,.95),(.7,.5,1.9),'Red',.1);box('Pump face',(w/2+1.3,-d/2+.74,1.3),(.45,.04,.5),'WhiteTrim',.02)
    sphere('Pump globe',(w/2+1.3,-d/2+1.0,2.1),(.25,.25,.25),'GlassLit',2)
    box('Workbench',(0,d/2-1.2,.9),(5,.9,.12),'Wood',.03)
    for x in(-2.2,2.2):box('Bench leg',(x,d/2-1.2,.45),(.12,.8,.9),'Metal',.02)
    collision('Left wall',(-(w/2-.6),0,h/2),(1.2,d,h));collision('Right wall',(w/2-.6,0,h/2),(1.2,d,h));collision('Back wall',(0,d/2-.2,h/2),(w,.4,h))
    collision('Pump',(w/2+1.3,-d/2+1.0,.95),(.8,.6,1.9));collision('Tyres',(-w/2-1.1,-d/2+1.2,.85),(1.0,1.0,1.7))


# ---------------------------------------------------------------- Hillcrest Valley

def arch_ring(name,cx,cy,z0,half,rise,thick,depth,material,count=13,face=-1,alt=None):
    """Voussoir ring on an elliptical arch in the XZ plane at y=cy (springing at z0)."""
    for k in range(count):
        a0=math.pi*k/count;a1=math.pi*(k+1)/count;am=(a0+a1)/2
        x=cx-math.cos(am)*(half+thick/2);z=z0+math.sin(am)*(rise+thick/2)
        seg=math.hypot(math.cos(a1)*half-math.cos(a0)*half,(math.sin(a1)-math.sin(a0))*rise)
        ang=math.atan2(math.cos(am)*rise,math.sin(am)*half)
        box(name,(x,cy,z),(seg+.08,depth,thick),alt if (alt and k%2) else material,.05,rot=(0,-ang,0))

def spandrel(cx,cy,z0,half,rise,top,depth,material):
    """Fills between an elliptical arch and a flat top edge (both sides). Pass the ring's OUTER half-width and rise and a
    depth under the ring's (street review, 5 Oct: from the intrados, the steps' corners showed as a staircase inside the
    opening, and flush faces flickered against the voussoirs)."""
    n=12
    for side in(-1,1):
        for k in range(n):
            a0=math.pi/2*k/n;a1=math.pi/2*(k+1)/n
            x0=cx+side*math.sin(a0)*half;x1=cx+side*math.sin(a1)*half
            z0a=z0+math.cos(a0)*rise;z1a=z0+math.cos(a1)*rise
            xs=sorted((x0,x1));w=xs[1]-xs[0]+.02;zb=min(z0a,z1a)
            box('Spandrel',((x0+x1)/2,cy,(zb+top)/2),(w,depth,top-zb),material,0)

def tunnel_portal():
    """Headland Tunnel mouth: dressed-stone face with an arched opening matching the bore
    (12.4 m wide, 4.6 m walls, 3 m vault), voussoirs, keystone, pilasters, cornice, name board."""
    half,wall,vault=6.2,4.6,3.0
    W,H=28.0,15.5
    for side in(-1,1):
        box('Portal pier',(side*(half+(W/2-half)/2),.3,H/2),(W/2-half,2.4,H),'Stone',.1)
        box('Pier plinth',(side*(half+(W/2-half)/2),-.1,.5),(W/2-half+.3,2.9,1.0),'StoneDark',.06)
        box('Pilaster',(side*(half+1.1),-1.0,H/2-.4),(1.4,.5,H-.8),'StoneDark',.08)
        box('Wing wall',(side*(W/2+3.5),1.2,H*.35),(7,2.0,H*.7),'Stone',.1,rot=(0,0,0))
        box('Wing coping',(side*(W/2+3.5),1.2,H*.7+.2),(7.3,2.4,.4),'WhiteTrim',.06)
        cyl('Portal lamp',(side*(half+2.6),-1.4,6.2),.28,.7,'GlassLit','Z',8,.02,r2=.34)
        box('Lamp arm',(side*(half+2.6),-1.1,6.8),(.1,.8,.1),'Metal',.02)
        cyl('Portal planter',(side*(half+4.8),-2.3,.5),.9,1.0,'RoofTile','Z',16,.05,r2=1.1)
        for k in range(5):sphere('Planter bloom',(side*(half+4.8)+math.cos(k*1.3)*.55,-2.3+math.sin(k*1.3)*.55,1.2),(.35,.35,.3),['FlowerRed','FlowerYellow','FlowerPink','Leaf','FlowerWhite'][k],1)
    box('Portal top',(0,.3,(wall+vault+.9+H)/2),(2*half+.2,2.4,H-(wall+vault+.9)),'Stone',.1)
    spandrel(0,.3,wall,half+.9,vault+.9,wall+vault+1.0,2.4,'Stone')
    arch_ring('Voussoir',0,-.95,wall,half,vault,.95,.6,'WhiteTrim',15,alt='StoneDark')
    for side in(-1,1):box('Arch jamb',(side*(half+.45),-.95,wall/2),(.9,.6,wall),'WhiteTrim',.05)
    box('Keystone',(0,-1.05,wall+vault+.55),(1.2,.8,1.5),'Gold',.08)
    box('Cornice',(0,-.4,H+.2),(W+.8,3.4,.5),'WhiteTrim',.08)
    for k in range(9):box('Crenel',(-W/2+1.5+k*3.12,-.4,H+.85),(1.4,3.0,.8),'Stone',.06)
    sign_board(0,-.95,wall+vault+2.6,13.0,1.9,'Yellow','portal')
    box('Road apron',(0,-3,.02),(2*half+.2,4,.04),'StoneDark',.01)
    collision('Left',(-(half+(W/2-half)/2),.3,H/2),(W/2-half,2.4,H));collision('Right',(half+(W/2-half)/2,.3,H/2),(W/2-half,2.4,H))
    collision('Top',(0,.3,(wall+vault+1+H)/2),(2*half,2.4,H-(wall+vault+1)))

def bastion(x0,x1,zTop,zBase,sea,yFoot,yTop,skip=None,yBack=6.0):
    """Battered masonry sea bastion: the face leans back from yFoot (at zBase) to yTop (at zTop).
    Cornice and corbels at the top, coursed masonry, a wet band and weed line at the waterline
    and a crenellated parapet with a gap (skip) where the road or railway runs through."""
    v=[(x0,yFoot,zBase),(x1,yFoot,zBase),(x1,yTop,zTop),(x0,yTop,zTop),(x0,yBack,zBase),(x1,yBack,zBase),(x1,yBack,zTop),(x0,yBack,zTop)]
    poly('Bastion',v,[(0,1,2,3),(5,4,7,6),(4,0,3,7),(1,5,6,2),(3,2,6,7),(4,5,1,0)],'Stone')
    def fy(z):return yFoot+(yTop-yFoot)*(z-zBase)/(zTop-zBase)
    tilt=-math.atan2(yTop-yFoot,zTop-zBase);mid=(x0+x1)/2;row=0;z=zTop-1.4
    while z>sea-1.5:
        box('Course',(mid,fy(z)-.03,z),(x1-x0-.1,.1,.08),'StoneDark',0,rot=(tilt,0,0))
        x=x0+1.6+(row%2)*1.5;k=0
        while x<x1-1.5:
            if (row*3+k)%4==0:box('Course block',(x,fy(z+.62)-.06,z+.62),(2.8,.14,1.1),'StoneDark',.04,rot=(tilt,0,0))
            x+=3.0;k+=1
        z-=1.25;row+=1
    box('Wet stone',(mid,fy(sea-1)-.06,sea-1.0),(x1-x0,.12,2.4),'RockDark',.02,rot=(tilt,0,0))
    box('Weed line',(mid,fy(sea)-.12,sea+.15),(x1-x0,.18,.45),'LeafDark',.04,rot=(tilt,0,0))
    box('Bastion cornice',(mid,yTop-.25,zTop-.2),(x1-x0+.4,.8,.4),'WhiteTrim',.05)
    x=x0+.8
    while x<x1-.6:box('Corbel',(x,yTop-.2,zTop-.85),(.45,.5,.8),'Stone',.04);x+=1.6
    spans=[(x0,x1)] if not skip else [(x0,skip[0]),(skip[1],x1)]
    for a,b in spans:
        if b-a<.5:continue
        box('Parapet',((a+b)/2,yTop+.35,zTop+.55),(b-a,.7,1.1),'Stone',.05)
        box('Parapet coping',((a+b)/2,yTop+.35,zTop+1.15),(b-a+.1,.85,.15),'WhiteTrim',.03)
        x=a+.7
        while x<b-.5:box('Merlon',(x,yTop+.35,zTop+1.65),(1.0,.75,.85),'Stone',.05);x+=2.2
        collision('Parapet',((a+b)/2,yTop+.35,zTop+.8),(b-a,.7,1.6))
    collision('Bastion',(mid,(yTop+yBack)/2,(zBase+zTop-.3)/2),(x1-x0,yBack-yTop,zTop-.3-zBase))

def gate_tower(x,y,z0,z1,sea,r=4.6):
    """Round flanking tower rising from the sea: battered foot, string courses, arched windows,
    machicolations, merlons, tall slate cone and a pennant (Tint_Awning)."""
    cyl('Tower drum',(x,y,(z0+z1)/2),r,z1-z0,'Stone','Z',32,.08)
    cyl('Tower batter',(x,y,(z0+sea)/2),r+1.6,sea-z0,'StoneDark','Z',32,.06,r2=r+.15)
    cyl('Tower weed',(x,y,sea+.15),r+.2,.45,'LeafDark','Z',32,.03)
    for z in(0,8,15):cyl('Tower string',(x,y,z),r+.15,.35,'WhiteTrim','Z',32,.03)
    for z in(4.2,11.2,18.4):
        box('Tower window',(x,y-r+.05,z),(1.0,.4,1.7),'Glass',.05);box('Window arch',(x,y-r-.02,z+1.0),(1.4,.35,.4),'WhiteTrim',.05)
        box('Window sill',(x,y-r-.05,z-.95),(1.3,.4,.2),'WhiteTrim',.03)
    for k in range(3):a=math.pi*(.25+k*.25);box('Arrow slit',(x+math.cos(a)*(r+.02)*(1 if x>0 else -1),y-math.sin(a)*(r+.02),-6+k*3),(.3,.3,1.4),'Black',.02,rot=(0,0,a))
    cyl('Machicolation',(x,y,z1+.4),r+.6,.8,'Stone','Z',32,.05)
    for k in range(16):box('Machicolation corbel',(x+math.cos(k*math.tau/16)*(r+.3),y+math.sin(k*math.tau/16)*(r+.3),z1-.3),(.5,.5,.7),'Stone',.04,rot=(0,0,k*math.tau/16))
    for k in range(14):a=k*math.tau/14;box('Merlon',(x+math.cos(a)*(r+.35),y+math.sin(a)*(r+.35),z1+1.4),(1.2,.7,1.1),'Stone',.05,rot=(0,0,a))
    cyl('Tower roof',(x,y,z1+5.3),r+1.0,6.8,'RoofSlate','Z',32,.05,r2=.15)
    cyl('Pennant pole',(x,y,z1+10.2),.07,3.2,'MetalLight','Z',8);box('Pennant',(x+1.15,y,z1+11.1),(2.3,.05,1.0),'Tint_Awning',.02)
    collision('Tower',(x,y,(z0+z1)/2),(2*r,2*r,z1-z0))

def headland_gate():
    """Bayview mouth of the Headland Tunnel: a gatehouse on a sea bastion. Twin round towers rise
    from the water either side of a crenellated gate block; the road arch matches the bore (12.4 m
    wide, 4.6 m walls, 3 m vault). Name board (SignAnchor_portal), crest, banners and lanterns.
    Origin at road level on the portal line; -Y faces the sea and Bayview."""
    half,wall,vault=6.2,4.6,3.0;top=17.0;sea=-31.8;base=-44.0;front=-2.5;depth=6.5;cy=front+depth/2
    bastion(-26,26,0,base,sea,-6.2,-3.2,skip=(-5.2,5.2))
    for side in(-1,1):box('Gate cheek',(side*(half+(11.2-half)/2),cy,top/2),(11.2-half,depth,top),'Stone',.1)
    spandrel(0,cy,wall,half,vault,wall+vault+2,depth,'Stone')
    box('Gate lintel block',(0,cy,(wall+vault+2+top)/2),(2*half+.2,depth,top-(wall+vault+2)),'Stone',.1)
    arch_ring('Gate soffit',0,cy,wall,half,vault,.3,depth,'StoneDark',17)
    for side in(-1,1):box('Soffit wall',(side*(half+.15),cy,wall/2),(.3,depth,wall),'StoneDark',0)
    arch_ring('Gate voussoir',0,front-.2,wall,half,vault,1.0,.7,'WhiteTrim',15,alt='StoneDark')
    for side in(-1,1):box('Gate jamb',(side*(half+.5),front-.2,wall/2),(1.0,.7,wall),'WhiteTrim',.05)
    box('Keystone',(0,front-.45,wall+vault+.6),(1.4,.9,1.7),'Gold',.08)
    # Machicolated parapet walk with merlons front and back.
    box('Gate string',(0,front-.1,top-.3),(22.4,.5,.4),'WhiteTrim',.05)
    for k in range(11):box('Corbel',(-10+k*2,front-.25,top-1.0),(.5,.5,.9),'Stone',.05)
    for k in range(8):
        for yy in(front+.45,front+depth-.45):box('Gate merlon',(-9.8+k*2.8,yy,top+.7),(1.6,.9,1.4),'Stone',.06)
    # Name board, crest with crown, hanging banners, lanterns.
    sign_board(0,front,wall+vault+3.3,15.5,2.3,'Yellow','portal')
    box('Crest shield',(0,front-.2,top-3.4),(2.8,.35,3.2),'Tint_Shutter',.3)
    for s in(-1,1):box('Crest chevron',(s*.55,front-.42,top-3.5),(1.6,.12,.45),'Gold',.03,rot=(0,s*.6,0))
    box('Crest crown',(0,front-.25,top-1.45),(1.9,.35,.45),'Gold',.05)
    for k in range(3):box('Crown point',(-.65+k*.65,front-.25,top-1.05),(.35,.3,.45),'Gold',.04)
    for side in(-1,1):
        box('Banner rod',(side*8.5,front-.35,top-2.1),(2.7,.14,.14),'Metal',.02)
        box('Banner',(side*8.5,front-.3,top-5.6),(2.1,.1,6.8),'Tint_Awning',.03)
        box('Banner stripe',(side*8.5,front-.37,top-5.6),(.6,.06,6.6),'Gold',.02)
        box('Lantern bracket',(side*(half+2.3),front-.5,7.1),(.1,1.0,.1),'Metal',.02)
        cyl('Lantern',(side*(half+2.3),front-1.0,6.5),.36,.95,'GlassLit','Z',8,.02,r2=.44)
        cyl('Lantern cap',(side*(half+2.3),front-1.0,7.1),.48,.3,'Metal','Z',8,.02,r2=.08)
    for side in(-1,1):gate_tower(side*15.8,-3.6,base,23.0,sea)
    for side in(-1,1):
        collision('Gate cheek',(side*(half+(11.2-half)/2),cy,top/2),(11.2-half,depth,top))
    collision('Gate top',(0,cy,(wall+vault+.4+top)/2),(2*half,depth,top-(wall+vault+.4)))

def rail_gate():
    """Bayview mouth of the railway tunnel: a smaller gatehouse on its own sea bastion. Square
    towers with slate pyramids, a clock over the arch, a semaphore signal, a dark tunnel beyond.
    Rail opening 7.8 m wide, 5 m high. Origin at rail level on the portal line; -Y faces the sea."""
    half,wall,vault=3.9,3.0,2.0;top=11.0;sea=-35.8;base=-44.0;front=-2.0;depth=6.0;cy=front+depth/2
    bastion(-14,14,0,base,sea,-4.6,-2.8,skip=(-2.9,2.9))
    for side in(-1,1):box('Gate cheek',(side*(half+(7.6-half)/2),cy,top/2),(7.6-half,depth,top),'Stone',.1)
    spandrel(0,cy,wall,half,vault,wall+vault+1.2,depth,'Stone')
    box('Gate lintel block',(0,cy,(wall+vault+1.2+top)/2),(2*half+.2,depth,top-(wall+vault+1.2)),'Stone',.1)
    arch_ring('Gate soffit',0,cy,wall,half,vault,.25,depth,'StoneDark',13)
    for side in(-1,1):box('Soffit wall',(side*(half+.12),cy,wall/2),(.25,depth,wall),'StoneDark',0)
    box('Tunnel dark',(0,front+depth-.3,(wall+vault)/2+.2),(2*half+.2,.3,wall+vault+.6),'Black',0)
    arch_ring('Gate voussoir',0,front-.15,wall,half,vault,.8,.6,'WhiteTrim',13,alt='StoneDark')
    for side in(-1,1):box('Gate jamb',(side*(half+.4),front-.15,wall/2),(.8,.6,wall),'WhiteTrim',.05)
    box('Keystone',(0,front-.35,wall+vault+.5),(1.1,.7,1.3),'Gold',.06)
    box('Gate string',(0,front-.1,top-.3),(15.4,.5,.35),'WhiteTrim',.05)
    for k in range(6):
        for yy in(front+.4,front+depth-.4):box('Gate merlon',(-6.25+k*2.5,yy,top+.6),(1.4,.8,1.2),'Stone',.06)
    sign_board(0,front,wall+vault+1.35,10.5,1.5,'Yellow','railway')
    cyl('Gate clock',(0,front-.1,top-1.7),1.05,.25,'WhiteTrim','Y',28);cyl('Clock rim',(0,front-.05,top-1.7),1.18,.18,'Gold','Y',28)
    box('Clock hand',(0,front-.3,top-1.45),(.08,.05,.6),'Black',.01);box('Clock hand',(.28,front-.3,top-1.7),(.55,.05,.08),'Black',.01)
    for side in(-1,1):
        x=side*10.2
        box('Rail tower',(x,-1.2,(base+15)/2),(5.4,5.4,15-base),'Stone',.1)
        box('Tower batter',(x,-1.2,(base+sea)/2),(7.0,7.0,sea-base),'StoneDark',.06)
        box('Tower weed',(x,-1.2,sea+.15),(5.6,5.6,.45),'LeafDark',.03)
        for z in(0,7,14.6):box('Tower string',(x,-1.2,z),(5.8,5.8,.35),'WhiteTrim',.03)
        for z in(3.6,10.4):box('Tower window',(x,-3.95,z),(.9,.3,1.5),'Glass',.05);box('Window arch',(x,-4.0,z+.9),(1.2,.3,.35),'WhiteTrim',.04)
        s=3.3;h0=15.0;v=[(x-s,-1.2-s,h0),(x+s,-1.2-s,h0),(x+s,-1.2+s,h0),(x-s,-1.2+s,h0),(x,-1.2,h0+5.2)]
        poly('Tower pyramid',v,[(0,1,4),(1,2,4),(2,3,4),(3,0,4),(3,2,1,0)],'RoofSlate')
        cyl('Pennant pole',(x,-1.2,h0+6.4),.06,2.6,'MetalLight','Z',8);box('Pennant',(x+.9,-1.2,h0+7.1),(1.8,.05,.8),'Tint_Awning',.02)
        collision('Tower',(x,-1.2,(base+15)/2),(5.4,5.4,15-base))
        collision('Gate cheek',(side*(half+(7.6-half)/2),cy,top/2),(7.6-half,depth,top))
    # Semaphore signal on the bastion beside the line.
    cyl('Signal post',(-5.4,-1.2,3.0),.12,6.0,'WhiteTrim','Z',10)
    box('Signal arm',(-4.6,-1.45,5.3),(1.7,.1,.34),'Red',.03);box('Signal stripe',(-4.3,-1.52,5.3),(.3,.06,.36),'WhiteTrim',.01)
    sphere('Signal lamp',(-5.4,-1.5,5.9),(.18,.18,.18),'GlassLit',1)
    collision('Gate top',(0,cy,(wall+vault+.4+top)/2),(2*half,depth,top-(wall+vault+.4)))

def town_hall():
    """Hillcrest town hall: three storeys over an arcade, balcony, and a 30 m clock tower."""
    w,d,h=20.0,12.0,11.0
    box('Hall',(0,0,h/2),(w,d,h),'Tint_Wall',.14,segments=3)
    box('Hall plinth',(0,0,.35),(w+.3,d+.3,.7),'Stone',.06)
    box('Hall cornice',(0,0,h+.2),(w+.6,d+.6,.4),'WhiteTrim',.06)
    for x in(-w/2+.2,w/2-.2):box('Corner quoin',(x,-d/2+.1,h/2),(.6,.6,h),'Stone',.05)
    gable_roof(0,0,w,d,h+.4,2.8,.5)
    # (Street review, 5 Oct: the arches stood mid-depth under the slab, which hid their crowns from the street, so only
    # broken-looking springers showed; and the last pier stood 2 m past the end.) Arches on the arcade's front edge.
    ay=-d/2-2.9
    for k in range(5):
        x=-8+k*4
        box('Arcade pier',(x-2,ay,1.9),(.8,.8,3.8),'Stone',.06)
        arch_ring('Arcade arch',x,ay,2.6,1.6,1.0,.4,.8,'WhiteTrim',9)
        spandrel(x,ay,2.6,2.0,1.4,3.95,.7,'Stone')
        box('Arcade door',(x,-d/2-.05,1.4),(1.8,.12,2.8),'Tint_Door',.05)
    box('Arcade pier',(10,ay,1.9),(.8,.8,3.8),'Stone',.06)
    box('Arcade roof',(0,-d/2-1.6,4.1),(w+.2,3.4,.5),'Stone',.06)
    for k in range(5):
        window(-8+k*4,-d/2,6.0,1.3,1.9,flowers=True)
        window(-8+k*4,-d/2,9.1,1.1,1.5,flowers=False)
    box('Balcony',(0,-d/2-.9,4.55),(7,1.8,.25),'Stone',.05)
    for k in range(14):cyl('Balustrade',(-3.3+k*.51,-d/2-1.7,5.05),.09,.8,'WhiteTrim','Z',8)
    box('Balustrade rail',(0,-d/2-1.7,5.5),(7,.25,.14),'WhiteTrim',.03)
    sign_board(0,-d/2-.05,7.7,9,1.0,'WhiteTrim','hall')
    tx,ty,th=0,1.0,30.0
    box('Clock tower',(tx,ty,th/2),(5.2,5.2,th),'Tint_Wall',.1)
    for z in(h+.3,19.5,25.5):box('Tower band',(tx,ty,z),(5.6,5.6,.4),'WhiteTrim',.05)
    for face,(fx,fy,axis) in enumerate([(0,-2.66,'Y'),(0,2.66,'Y'),(2.66,0,'X'),(-2.66,0,'X')]):
        cyl('Clock face',(tx+fx,ty+fy,22.4),1.55,.16,'WhiteTrim',axis,32);cyl('Clock rim',(tx+fx*1.01,ty+fy*1.01,22.4),1.7,.12,'Gold',axis,32)
        o=1 if (fx+fy)>0 else -1
        if axis=='Y':
            box('Hour hand',(tx,ty+fy+o*.12,22.75),(.12,.05,.8),'Black',.01);box('Minute hand',(tx+.45,ty+fy+o*.12,22.4),(1.05,.05,.1),'Black',.01)
        else:
            box('Hour hand',(tx+fx+o*.12,ty,22.75),(.05,.12,.8),'Black',.01);box('Minute hand',(tx+fx+o*.12,ty+.45,22.4),(.05,1.05,.1),'Black',.01)
    for sd in(-1,1):
        box('Belfry arch',(tx,ty+sd*2.62,27.7),(1.8,.1,2.4),'Black',.05);box('Belfry arch',(tx+sd*2.62,ty,27.7),(.1,1.8,2.4),'Black',.05)
    cyl('Hall bell',(tx,ty,27.6),.7,1.1,'Gold','Z',16,.05,r2=.4)
    box('Tower cornice',(tx,ty,th+.2),(6.0,6.0,.4),'WhiteTrim',.05)
    cyl('Tower spire',(tx,ty,th+3.3),3.8,5.8,'RoofTile','Z',4,.05,r2=.06)
    cyl('Flag pole',(tx,ty,th+8.0),.07,3.6,'MetalLight','Z',8)
    box('Flag',(tx+.9,ty,th+9.1),(1.8,.05,1.1),'Tint_Awning',.02)
    collision('Hall',(0,0,h/2),(w+.4,d+.4,h));collision('Arcade',(0,-d/2-1.6,2),(w,.9,4));collision('Tower',(tx,ty,th/2),(5.4,5.4,th))

def side_paint(w,rows,ys=(-1.6,1.6)):
    """Trompe-l'oeil windows on a terrace's side walls (Ligurian painted facades): flat frame, closed shutters, sill.
    Under 0.12 m proud, so they fit the gap to a neighbour; at a row's end they stop the side being a blank wall."""
    for s in(-1,1):
        x=s*w/2
        for z in rows:
            for y in ys:
                box('Painted frame',(x+s*.02,y,z),(.04,1.2,1.7),'WhiteTrim',0)
                for t in(-1,1):box('Painted shutter',(x+s*.045,y+t*.27,z),(.03,.5,1.45),'Tint_Shutter',0)
                box('Painted shutter gap',(x+s*.062,y,z),(.01,.04,1.45),'Black',0)
                box('Painted sill',(x+s*.06,y,z-.95),(.12,1.4,.1),'WhiteTrim',0)

def terrace(kind='a'):
    """Tall narrow town house (5.4 m frontage, three storeys): 'a' balconies, 'b' shop below."""
    w,d,h=5.4,8.0,9.6;walls(w,d,h,plinth=.45)
    box('Roof slab',(0,0,h+.25),(w+.5,d+.6,.5),'RoofTile',.1)
    for k in range(5):box('Roof course',(0,-d/2+.8+k*1.6,h+.55),(w+.3,.9,.2),'RoofTileLight' if k%2 else 'RoofTile',.06)
    box('Chimney',(1.4,1.5,h+1.4),(.8,.8,2.0),'Stone',.05);box('Chimney cap',(1.4,1.5,h+2.45),(1.0,1.0,.14),'WhiteTrim',.03)
    for k in range(2):cyl('Chimney pot',(1.25+k*.3,1.5,h+2.72),.1,.4,'RoofTile','Z',12)
    cyl('Gutter',(0,-d/2-.28,h+.02),.09,w+.5,'Metal','X',10)
    cyl('Downpipe',(w/2-.2,d/2+.12,h/2),.055,h,'Metal','Z',8)
    if kind=='b':
        box('Shopfront',(0,-d/2-.08,1.55),(w-.6,.2,2.9),'Tint_Door',.05)
        box('Display window',(-.9,-d/2-.2,1.8),(2.4,.08,2.0),'GlassLit',.02);box('Display frame',(-.9,-d/2-.17,1.8),(2.65,.1,2.25),'WhiteTrim',.04)
        door(1.6,-d/2-.08,0,1.0,2.4,canopy=False)
        # (Street review, 4 Oct: the valance hung below the door head; raised 0.3 m.)
        awning(0,-d/2-.08,4.0,w-.3,1.5,6)   # (5 Oct: 0.2 m higher, the scallops clear the door frame)
        # (Street review, 5 Oct: the first-floor window boxes covered the lower half of the letters.) The board sits
        # between the awning and the first-floor sills, which stand higher on a shop (below).
        sign_board(0,-d/2-.12,4.42,w-1.0,.56,'WhiteTrim','shop')
    elif kind=='c':
        # Arcaded ground floor: a deep round arch over the door and a shop window.
        # (Street review, 5 Oct: a black panel behind a part-filled arch read as a hole.) A shaded stone back wall, a
        # framed shop window, the door, and a glazed fanlight under the arch with a white transom.
        box('Arcade back wall',(0,-d/2-.02,1.75),(w-1.3,.06,3.4),'StoneDark',0)
        box('Arcade window',(-.75,-d/2-.07,1.3),(2.1,.05,2.2),'Glass',.01);box('Arcade window frame',(-.75,-d/2-.05,1.3),(2.3,.06,2.4),'WhiteTrim',.02)
        door(1.35,-d/2-.05,0,1.0,2.4,canopy=False)
        box('Arcade fanlight',(0,-d/2-.07,2.85),(3.7,.05,.75),'Glass',.01);box('Arcade transom',(0,-d/2-.1,2.5),(3.95,.1,.14),'WhiteTrim',.02)
        for s2 in(-1,1):box('Arcade pier',(s2*(w/2-.35),-d/2-.3,1.25),(.7,.6,2.5),'Stone',.05)
        arch_ring('Arcade arch',0,-d/2-.3,2.5,w/2-.7,.85,.42,.6,'Stone',11)
        spandrel(0,-d/2-.3,2.5,w/2-.7+.42,.85+.42,3.8,.52,'Stone')
        box('Arcade lintel',(0,-d/2-.3,3.95),(w,.6,.4),'Stone',.05)
    elif kind=='d':
        door(-1.3,-d/2,0,1.1,2.4)
        window(1.2,-d/2,1.7,1.2,1.4)
        # First-floor bay window on brackets.
        box('Bay',(0,-d/2-.55,4.9),(2.8,1.1,2.2),'Tint_Wall',.08);box('Bay roof',(0,-d/2-.6,6.1),(3.1,1.3,.2),'RoofTile',.06,rot=(.12,0,0))
        for x in(-.95,0,.95):
            box('Bay glass',(x,-d/2-1.12,4.95),(.72,.04,1.4),'Glass',.01)
            if x<.9:box('Bay mullion',(x+.47,-d/2-1.13,4.95),(.08,.05,1.5),'WhiteTrim',.01)
        for s2 in(-1,1):box('Bay bracket',(s2*1.1,-d/2-.45,3.7),(.2,.8,.4),'Stone',.04)
        # Two dormers in the roof.
        for x in(-1.3,1.3):
            box('Dormer',(x,-d/2+.8,h+1.0),(1.3,1.0,1.1),'Tint_Wall',.05);box('Dormer roof',(x,-d/2+.8,h+1.65),(1.6,1.3,.2),'RoofTile',.05)
            box('Dormer glass',(x,-d/2+.28,h+1.0),(.8,.04,.7),'Glass',.01)
    else:
        door(-1.3,-d/2,0,1.1,2.4)
        window(1.2,-d/2,1.7,1.2,1.4)
    for z in(5.0,8.0):
        for x in(-1.25,1.25):
            if kind=='d' and z<6:continue
            shop_floor=kind=='b' and z<6
            window(x,-d/2,5.8 if shop_floor else z,1.0,1.6,flowers=(kind in('a','c') or (z<6 and not shop_floor)))
    # The back: plain sash windows and a back door (rows are seen from the next street too).
    for z in(1.7,5.0,8.0):
        for x in((-1.25,) if z<3 else (-1.25,1.25)):window(x,d/2,z,.9,1.3,shutters=False,flowers=False,face=1)
    door(1.3,d/2,0,1.0,2.3,face=1,canopy=False)
    # Shops ('b') have no balcony: its railing stood in front of the sign board (street review, 4 Oct).
    if kind=='a':
        box('Juliet balcony',(0,-d/2-.55,4.05),(w-1.2,1.1,.16),'Stone',.04)
        for k in range(9):cyl('Balcony rail',(-(w-1.4)/2+k*(w-1.4)/8,-d/2-1.05,4.5),.03,.8,'Metal','Z',6)
        box('Balcony top',(0,-d/2-1.05,4.92),(w-1.3,.07,.07),'Metal',.02)
    side_paint(w,(5.0,8.0))
    collision('Body',(0,0,h/2),(w+.3,d+.3,h))
def terrace_a():terrace('a')
def terrace_b():terrace('b')
def terrace_c():terrace('c')
def terrace_d():terrace('d')
def terrace_back(w,d,rows):
    """Plain back of a terrace house: sash windows on each floor and a back door."""
    for z in rows:
        for x in((-1.25,) if z<3 else (-1.25,1.25)):window(x,d/2,z,.9,1.3,shutters=False,flowers=False,face=1)
    door(1.3,d/2,0,1.0,2.3,face=1,canopy=False)

def terrace_e():
    """Four-storey palazzetto: rusticated ground floor, a cornice on modillions and a rooftop altana (loggia)."""
    w,d,h=5.4,8.0,12.6;walls(w,d,h,plinth=.45)
    box('Roof slab',(0,0,h+.25),(w+.5,d+.6,.5),'RoofTile',.1)
    box('Cornice',(0,-d/2-.22,h-.02),(w+.7,.6,.32),'WhiteTrim',.05)
    for k in range(7):box('Modillion',(-2.4+k*.8,-d/2-.24,h-.36),(.18,.45,.22),'WhiteTrim',.02)
    for k in range(4):box('Rustication',(0,-d/2-.05,.8+k*.78),(w,.1,.5),'Stone',.04)
    box('String course',(0,-d/2-.08,3.45),(w+.1,.22,.22),'WhiteTrim',.04)
    door(-1.3,-d/2,0,1.1,2.4);window(1.2,-d/2,1.7,1.2,1.4,flowers=False)
    for z,hh in((5.0,1.7),(8.0,1.5),(11.0,1.2)):
        for x in(-1.25,1.25):window(x,-d/2,z,1.0,hh,flowers=(z<9))
    box('Balcony',(0,-d/2-.55,4.0),(w-1.0,1.1,.18),'Stone',.04)
    for k in range(11):cyl('Baluster',(-(w-1.3)/2+k*(w-1.3)/10,-d/2-1.03,4.45),.07,.75,'WhiteTrim','Z',8,r2=.05)
    box('Balcony top',(0,-d/2-1.03,4.86),(w-1.1,.16,.1),'WhiteTrim',.03)
    terrace_back(w,d,(1.7,5.0,8.0,11.0))
    # The altana: a small roofed loggia on the roof terrace, set back from the street.
    ay,az=1.3,h+.5
    for sx in(-1,1):
        for sy in(-1,1):box('Altana pier',(sx*1.95,ay+sy*1.45,az+1.05),(.34,.34,2.1),'Tint_Wall',.04)
    box('Altana back',(0,ay+1.45,az+1.05),(3.9,.24,2.1),'Tint_Wall',.04)
    box('Altana shade',(0,ay+1.3,az+1.05),(3.5,.04,1.9),'StoneDark',0)   # (5 Oct: black read as a hole in the roof)
    hip_roof(0,ay,4.2,3.2,az+2.1,.9,.3)
    for x in(-.95,.95):cyl('Altana rail post',(x,ay-1.45,az+.45),.05,.9,'Metal','Z',8)
    box('Altana rail',(0,ay-1.45,az+.88),(3.6,.07,.07),'Metal',.02)
    for x in(-1.0,1.0):cyl('Roof pot',(x*1.8,-d/2+.9,az+.3),.25,.6,'RoofTile','Z',12,r2=.32);sphere('Roof shrub',(x*1.8,-d/2+.9,az+.85),(.38,.38,.42),'LeafDark',2)
    box('Chimney',(1.9,-2.7,h+1.4),(.7,.7,1.8),'Stone',.05);box('Chimney cap',(1.9,-2.7,h+2.36),(.9,.9,.14),'WhiteTrim',.03)
    cyl('Gutter',(0,-d/2-.32,h-.25),.09,w+.5,'Metal','X',10)
    side_paint(w,(5.0,8.0,11.0))
    collision('Body',(0,0,h/2),(w+.3,d+.3,h))

def terrace_f():
    """Two-storey cottage between the tall houses: a tiled pitched roof with its eaves to the street, and a dormer."""
    w,d,h=5.4,8.0,6.6;walls(w,d,h,plinth=.45)
    gable_roof(0,0,w,d,h,2.6,.16)
    box('Dormer',(0,-1.75,h+1.4),(1.6,1.4,1.6),'Tint_Wall',.05)
    box('Dormer roof',(0,-1.8,h+2.28),(1.95,1.75,.18),'RoofTile',.05,rot=(.12,0,0))
    box('Dormer frame',(0,-2.46,h+1.62),(1.05,.06,.85),'WhiteTrim',.03);box('Dormer glass',(0,-2.5,h+1.62),(.8,.04,.62),'Glass',.01)
    # (Street review, 5 Oct: the window's flower box sat on the porch bench, in every town.) The window over the bench has a
    # plain sill, and the bench stands clear of the wall with a back rail.
    door(-1.3,-d/2,0,1.1,2.4);window(1.2,-d/2,1.9,1.2,1.3,flowers=False)
    for x in(-1.25,1.25):window(x,-d/2,4.6,1.0,1.5)
    box('Porch bench',(1.2,-d/2-.75,.42),(1.4,.45,.08),'Wood',.03);box('Porch bench back',(1.2,-d/2-.55,.68),(1.4,.06,.22),'Wood',.02)
    for s in(-1,1):box('Bench leg',(1.2+s*.55,-d/2-.75,.05),(.1,.4,.7),'Wood',.02)
    terrace_back(w,d,(1.7,4.6))
    box('Chimney',(1.9,.6,h+2.4),(.7,.7,2.2),'Stone',.05);box('Chimney cap',(1.9,.6,h+3.56),(.9,.9,.14),'WhiteTrim',.03)
    for k in range(2):cyl('Chimney pot',(1.75+k*.3,.6,h+3.83),.1,.4,'RoofTile','Z',12)
    side_paint(w,(4.6,))
    collision('Body',(0,0,h/2),(w+.3,d+.3,h))

def terrace_g():
    """Painted Ligurian house: a gable to the street with an oculus, painted pediments and string courses."""
    w,d,h=5.4,8.0,10.2;walls(w,d,h,plinth=.45)
    gable_roof(0,0,w,d,h,2.2,.16,'RoofTile','Y')
    cyl('Oculus frame',(0,-d/2-.06,h+.85),.5,.14,'WhiteTrim','Y',20);cyl('Oculus glass',(0,-d/2-.12,h+.85),.36,.06,'Glass','Y',20)
    for z in(3.45,6.65):box('String course',(0,-d/2-.08,z),(w+.1,.2,.2),'WhiteTrim',.04)
    door(0,-d/2,0,1.2,2.5,canopy=False)
    box('Fanlight frame',(0,-d/2-.05,2.95),(1.5,.12,.5),'WhiteTrim',.03);box('Fanlight',(0,-d/2-.1,2.95),(1.1,.04,.32),'Glass',.01)
    for x in(-1.85,1.85):window(x,-d/2,1.7,.8,1.3,shutters=False,flowers=False)
    for z in(4.9,8.1):
        for x in(-1.25,1.25):
            window(x,-d/2,z,1.0,1.5,flowers=(z<6))
            top=z+.75+.48
            wedge('Painted pediment',(x-.8,-d/2-.14,top),(x+.8,-d/2-.14,top),(x,-d/2-.14,top+.42),(0,-1,0),'WhiteTrim',.1)
    terrace_back(w,d,(1.7,4.9,8.1))
    box('Chimney',(-1.6,2.4,h+1.5),(.7,.7,2.2),'Stone',.05);box('Chimney cap',(-1.6,2.4,h+2.66),(.9,.9,.14),'WhiteTrim',.03)
    side_paint(w,(4.9,8.1))
    collision('Body',(0,0,h/2),(w+.3,d+.3,h))

def hip_roof(cx,cy,w,d,eave,rise,over=.5,material='RoofTile'):
    """Hipped roof over a w x d plan (w >= d): four slopes, ridge along X, hip caps, gutters all round."""
    W,D=w/2+over,d/2+over;r=max(0,W-D)
    v=[(cx-W,cy-D,eave),(cx+W,cy-D,eave),(cx+W,cy+D,eave),(cx-W,cy+D,eave),(cx-r,cy,eave+rise),(cx+r,cy,eave+rise)]
    poly('Hip roof',v,[(0,1,5,4),(2,3,4,5),(1,2,5),(3,0,4),(3,2,1,0)],material)
    for a,b in((0,4),(1,5),(2,5),(3,4)):
        A,B=Vector(v[a]),Vector(v[b]);m=(A+B)/2;dv=B-A
        o=cyl('Hip cap',tuple(m),.12,dv.length,'RoofTileLight','Z',10);o.rotation_mode='QUATERNION';o.rotation_quaternion=Vector((0,0,1)).rotation_difference(dv.normalized())
    if r>0:cyl('Ridge cap',(cx,cy,eave+rise+.05),.14,2*r+.2,'RoofTileLight','X',12)
    for s in(-1,1):
        cyl('Gutter',(cx,cy+s*(D-.02),eave-.02),.09,2*W,'Metal','X',10);cyl('Gutter',(cx+s*(W-.02),cy,eave-.02),.09,2*D,'Metal','Y',10)
    cyl('Downpipe',(cx+w/2+.14,cy+d/2-.3,eave/2),.055,eave,'Metal','Z',8)

def house_c():
    """Villa: rendered two storeys under a hipped roof, a three-arch loggia, bracketed eaves."""
    w,d,h=10.0,8.0,6.6;walls(w,d,h)
    hip_roof(0,0,w,d,h,2.2)
    for k in range(9):box('Eave bracket',(-4.4+k*1.1,-d/2-.25,h-.3),(.16,.5,.3),'WhiteTrim',.03)
    # Loggia: three arches on columns, shadowed recess behind.
    box('Loggia shadow',(0,-d/2-.02,1.5),(6.4,.06,2.9),'StoneDark',0)
    for x in(-2.1,0,2.1):arch_ring('Loggia arch',x,-d/2-.35,2.2,.85,.6,.3,.5,'WhiteTrim',9)
    for x in(-3.15,-1.05,1.05,3.15):cyl('Loggia column',(x,-d/2-.35,1.1),.18,2.2,'Stone','Z',12)
    box('Loggia entablature',(0,-d/2-.35,3.25),(6.8,.6,.35),'Stone',.04)
    door(0,-d/2,0,1.2,2.3,canopy=False)
    for x in(-2.1,2.1):window(x,-d/2,1.4,1.0,1.3,shutters=False,flowers=False)
    for x in(-3.6,-1.2,1.2,3.6):window(x,-d/2,4.6,1.0,1.4,flowers=(abs(x)>2))
    box('Balconette',(0,-d/2-.4,3.7),(1.8,.7,.14),'Stone',.03)
    for x in(-3,0,3):window(x,d/2,4.6,1.0,1.3,flowers=False,face=1)
    for x in(-3,3):window(x,d/2,1.6,1.0,1.3,shutters=False,flowers=False,face=1)
    for s in(-1,1):cyl('Garden urn',(s*(w/2+.6),-d/2-1.0,.4),.3,.8,'Stone','Z',12,.04,r2=.42);sphere('Urn shrub',(s*(w/2+.6),-d/2-1.0,1.05),(.4,.4,.45),'LeafDark',2)
    box('Chimney',(2.8,1.4,h+2.2),(.8,.8,2.2),'Stone',.05);box('Chimney cap',(2.8,1.4,h+3.35),(1.0,1.0,.14),'WhiteTrim',.03)
    side_paint(w,(1.4,4.6),(-1.8,1.8))
    collision('Body',(0,0,h/2),(w+.3,d+.3,h))

def house_d():
    """Tower house: three narrow storeys, a loggia top floor under a low hipped roof, sill flowers."""
    w,d,h=5.2,7.0,10.4;walls(w,d,h,plinth=.5)
    hip_roof(0,0,w,d,h,1.4,.55)
    door(-1.2,-d/2,0,1.1,2.4);window(1.2,-d/2,1.7,1.1,1.3,flowers=False)
    for x in(-1.2,1.2):window(x,-d/2,4.6,1.0,1.5)
    # Top-floor loggia: two openings on a centre pier; behind them a shaded back wall with a glazed door in each bay
    # (street review, 4 Oct: a flat black panel read as a void), and potted flowers on the sill.
    box('Top loggia back',(0,-d/2-.02,8.2),(4.0,.06,2.0),'StoneDark',0)
    for x in(-1.0,1.0):
        box('Loggia door frame',(x,-d/2-.06,8.05),(.86,.05,1.62),'WhiteTrim',.02);box('Loggia door glass',(x,-d/2-.09,8.08),(.66,.04,1.42),'Glass',.01)
        box('Loggia door bar',(x,-d/2-.11,8.08),(.05,.03,1.42),'WhiteTrim',.0)
    for x in(-1.0,1.0):arch_ring('Top arch',x,-d/2-.25,7.9,.8,.5,.28,.4,'WhiteTrim',9)
    for x in(-1.95,0,1.95):box('Top pier',(x,-d/2-.25,8.0),(.35,.4,1.9),'Stone',.03)
    box('Top sill',(0,-d/2-.35,7.1),(4.4,.6,.16),'Stone',.03)
    for k in range(5):
        cyl('Sill pot',(-1.6+k*.8,-d/2-.4,7.3),.13,.22,'RoofTile','Z',10,r2=.17)
        sphere('Sill pot flowers',(-1.6+k*.8,-d/2-.4,7.5),(.18,.18,.14),['FlowerRed','FlowerPink','FlowerYellow','FlowerWhite','FlowerRed'][k],1)
    box('String course',(0,0,6.6),(w+.25,d+.25,.2),'WhiteTrim',.03)
    for x in(-1.2,1.2):window(x,d/2,4.6,.9,1.3,shutters=False,flowers=False,face=1)
    window(0,d/2,8.2,1.1,1.4,flowers=False,face=1)
    side_paint(w,(1.7,4.6),(-1.5,1.5))
    collision('Body',(0,0,h/2),(w+.3,d+.3,h))


def castle_keep():
    """Square keep (15 m, 24 m to the parapet) with corner turrets and a great flag."""
    s,h=15.0,24.0
    box('Keep',(0,0,h/2),(s,s,h),'Stone',.1)
    box('Keep batter',(0,0,1.5),(s+1.2,s+1.2,3),'StoneDark',.08)
    for z in(8,16):box('Keep string',(0,0,z),(s+.3,s+.3,.35),'WhiteTrim',.04)
    for k in range(6):
        for f in(-1,1):
            box('Merlon',(-s/2+1.2+k*2.5,f*(s/2),h+.7),(1.2,.8,1.2),'Stone',.05)
            box('Merlon',(f*(s/2),-s/2+1.2+k*2.5,h+.7),(.8,1.2,1.2),'Stone',.05)
    for cx in(-1,1):
        for cy in(-1,1):
            x,y=cx*s/2,cy*s/2
            cyl('Turret',(x,y,h+2.5),1.8,5.0,'Stone','Z',16,.05)
            cyl('Turret cone',(x,y,h+6.9),2.2,3.8,'RoofSlate','Z',16,.05,r2=.08)
    door(0,-s/2,0,2.4,3.6,canopy=False)
    for z in(10,18):
        for x in(-4,0,4):box('Keep window',(x,-s/2-.03,z),(.9,.2,2.0),'GlassLit' if z==18 and x==0 else 'Black',.03)
    cyl('Great flag pole',(0,0,h+6),.1,12,'MetalLight','Z',8)
    box('Great flag',(1.8,0,h+10.4),(3.6,.06,2.2),'Tint_Awning',.03)
    box('Great flag stripe',(1.8,-.04,h+10.4),(3.6,.05,.6),'Gold',.02)
    collision('Keep',(0,0,h/2),(s+1.2,s+1.2,h))

# ---------------------------------------------------------------- Hillcrest Castle (grand)
# A fairytale hill-top castle that reads from anywhere in the valley: a donjon with needle-spired
# corner turrets and a belfry to ~58 m, a palazzo wing with a loggia over the valley, hoarded
# curtain towers, and the Zip Tower that sends cars down a cable to the lower town.
# Frontage is -Y (Unity +Z, outward from the castle centre when placed on the ring).

def spire(x,y,z,r,h,material='RoofSlate',verts=16,finial=True):
    """Needle spire with a flared eave ring and a gold ball finial."""
    cyl('Spire eave',(x,y,z+.25),r+.35,.5,material,'Z',verts,.05,r2=r)
    cyl('Spire',(x,y,z+.5+h/2),r,h,material,'Z',verts,.04,r2=.06)
    if finial:
        sphere('Finial ball',(x,y,z+.6+h),(.28,.28,.28),'Gold',2)
        cyl('Finial rod',(x,y,z+h+1.3),.05,1.6,'Gold','Z',8)

def crenels(cx,cy,z,w,d,material='Stone',step=2.3,size=(1.1,.7,1.1)):
    """Merlons round a w x d parapet top."""
    nx=max(2,int(w/step));ny=max(2,int(d/step))
    for k in range(nx):
        x=cx-w/2+(k+.5)*w/nx
        for f in(-1,1):box('Merlon',(x,cy+f*(d/2-size[1]/2),z+size[2]/2),size,material,.05)
    for k in range(ny):
        y=cy-d/2+(k+.5)*d/ny
        for f in(-1,1):box('Merlon',(cx+f*(w/2-size[1]/2),y,z+size[2]/2),(size[1],size[0],size[2]),material,.05)

def lancet(x,y,z,w,h,face=-1,axis='Y',lit=False):
    """Round-headed window: dark (or lit) glass under a white stone arch, with a sill."""
    g='GlassLit' if lit else 'Glass'
    if axis=='Y':
        box('Lancet glass',(x,y+face*.02,z),(w,.2,h),g,.02)
        cyl('Lancet head',(x,y+face*.02,z+h/2),w/2,.2,g,'Y',16,.01)
        box('Lancet surround',(x,y-face*.05,z+.1),(w+.4,.2,h+.5),'WhiteTrim',.05)
        box('Lancet sill',(x,y+face*.15,z-h/2-.1),(w+.5,.3,.18),'WhiteTrim',.04)
    else:
        box('Lancet glass',(x+face*.02,y,z),(.2,w,h),g,.02)
        cyl('Lancet head',(x+face*.02,y,z+h/2),w/2,.2,g,'X',16,.01)
        box('Lancet surround',(x-face*.05,y,z+.1),(.2,w+.4,h+.5),'WhiteTrim',.05)
        box('Lancet sill',(x+face*.15,y,z-h/2-.1),(.3,w+.5,.18),'WhiteTrim',.04)

def castle_tower():
    """Round curtain tower, 8 m across, 21 m to a timber hoarding gallery, tall flared slate roof,
    dormer, gold finial and pennant."""
    r,h=4.0,21.0
    cyl('Tower drum',(0,0,h/2),r,h,'Stone','Z',32,.08)
    cyl('Tower batter',(0,0,1.6),r+.8,3.2,'StoneDark','Z',32,.06,r2=r)
    for z in(7,14):cyl('Tower string',(0,0,z),r+.15,.3,'WhiteTrim','Z',32,.03)
    # Hoarding: a jettied timber gallery with a slot band, under the roof.
    cyl('Hoarding floor',(0,0,h+.15),r+.9,.3,'WoodDark','Z',32,.03)
    cyl('Hoarding',(0,0,h+1.3),r+.8,2.0,'Wood','Z',32,.03)
    for k in range(16):
        a=k*math.tau/16;box('Hoarding post',(math.cos(a)*(r+.85),math.sin(a)*(r+.85),h+1.3),(.22,.22,2.1),'WoodDark',.02,rot=(0,0,a))
        box('Hoarding slot',(math.cos(a+.2)*(r+.86),math.sin(a+.2)*(r+.86),h+1.6),(.5,.12,.5),'Black',.01,rot=(0,0,a+.2+math.pi/2))
    for k in range(10):
        a=k*math.tau/10;box('Corbel',(math.cos(a)*(r+.45),math.sin(a)*(r+.45),h-.4),(.5,.9,.8),'Stone',.04,rot=(0,0,a))
    cyl('Roof eave',(0,0,h+2.55),r+1.5,.5,'RoofSlate','Z',32,.05,r2=r+1.0)
    cyl('Tower roof',(0,0,h+7.3),r+1.0,9.0,'RoofSlate','Z',32,.05,r2=.12)
    # Dormer facing out (-Y).
    box('Dormer',(0,-(r+.2),h+4.6),(1.4,1.6,1.8),'Stone',.05)
    box('Dormer window',(0,-(r+1.02),h+4.5),(.8,.05,1.1),'GlassLit',.02)
    cyl('Dormer roof',(0,-(r+.3),h+5.9),1.0,1.4,'RoofSlate','Z',4,.03,r2=.05)
    sphere('Finial ball',(0,0,h+12.0),(.35,.35,.35),'Gold',2)
    cyl('Pennant pole',(0,0,h+13.4),.07,2.6,'MetalLight','Z',8)
    box('Pennant',(.95,0,h+14.2),(1.9,.05,.9),'Tint_Awning',.02)
    for k in range(4):
        a=math.pi*1.5+(k-1.5)*.55;lancet(math.cos(a)*(r+.02),math.sin(a)*(r+.02),5+k%2*6.5,.6,1.5,-1)
    collision('Drum',(0,0,h/2),(2*r+.8,2*r+.8,h))

def castle_wall():
    """20 m curtain wall run (3 m thick, 10 m high): crenellated walk, heraldic banner, lanterns."""
    L,h,t=20.0,10.0,3.0
    box('Curtain wall',(0,0,h/2),(L,t,h),'Stone',.08)
    box('Wall batter',(0,-.3,1.0),(L,t+.8,2.0),'StoneDark',.06)
    box('Wall string',(0,0,h-.2),(L+.02,t+.3,.35),'WhiteTrim',.04)
    for k in range(9):box('Merlon',(-L/2+1.1+k*2.22,-t/2+.35,h+.65),(1.2,.7,1.1),'Stone',.05)
    for k in range(9):box('Inner merlon',(-L/2+1.1+k*2.22,t/2-.25,h+.4),(1.2,.5,.6),'Stone',.05)
    for k in(-1,1):box('Arrow slit',(k*6.5,-t/2-.02,5.4),(.3,.1,1.6),'Black',.02)
    # A long red banner with a gold chevron hung from the parapet.
    box('Banner rod',(0,-t/2-.3,h-.6),(3.4,.12,.12),'Gold',.03)
    box('Wall banner',(0,-t/2-.28,h-3.6),(2.8,.06,5.6),'Tint_Awning',.02)
    for s in(-1,1):box('Banner chevron',(s*.55,-t/2-.33,h-4.6),(1.5,.04,.34),'Gold',.01,rot=(0,s*.6,0))
    wedge('Banner tail',(-1.4,-t/2-.28,h-6.4),(1.4,-t/2-.28,h-6.4),(0,-t/2-.28,h-7.4),(0,-1,0),'Tint_Awning',.06)
    collision('Wall',(0,0,h/2),(L,t,h))

def castle_gatehouse():
    """Twin-towered gatehouse (road along Y, 9 m clear arch): drawbridge down on chains, heraldic
    shield, machicolations, tall slate cones and banners."""
    half,wall,vault=4.5,4.4,2.6
    for side in(-1,1):
        cx=side*(half+4.0)
        cyl('Gate drum',(cx,0,9),3.6,18,'Stone','Z',28,.08)
        cyl('Gate batter',(cx,0,1.4),4.3,2.8,'StoneDark','Z',28,.06,r2=3.6)
        cyl('Gate string',(cx,0,12),3.75,.3,'WhiteTrim','Z',28,.03)
        for k in range(12):
            a=k*math.tau/12;box('Merlon',(cx+math.cos(a)*3.9,math.sin(a)*3.9,18.7),(1.0,.6,1.1),'Stone',.05,rot=(0,0,a))
            box('Corbel',(cx+math.cos(a)*3.8,math.sin(a)*3.8,17.6),(.4,.8,.8),'Stone',.04,rot=(0,0,a))
        cyl('Gate cone eave',(cx,0,19.6),4.5,.5,'RoofSlate','Z',28,.05,r2=4.0)
        cyl('Gate cone',(cx,0,23.6),4.0,7.6,'RoofSlate','Z',28,.05,r2=.1)
        sphere('Finial ball',(cx,0,27.6),(.3,.3,.3),'Gold',2)
        box('Banner',(cx,-3.75,11.0),(1.8,.08,4.4),'Tint_Awning',.02)
        box('Banner stripe',(cx,-3.8,11.0),(.5,.06,4.2),'Gold',.01)
        lancet(cx,-3.62,15.2,.7,1.6,-1,lit=True)
    top=wall+vault+11
    box('Gate block',(0,0,(wall+vault+top)/2+.3),(2*half+1.2,6.4,top-wall-vault+.6),'Stone',.08)
    spandrel(0,0,wall,half+.2,vault+.4,wall+vault+.9,6.4,'Stone')
    arch_ring('Gate voussoir',0,-3.25,wall,half,vault,.8,.3,'WhiteTrim',13,alt='StoneDark')
    for side in(-1,1):box('Gate jamb',(side*(half+.3),0,wall/2),(.6,6.4,wall),'StoneDark',.04)
    for k in range(8):cyl('Portcullis bar',(-half+.6+k*(2*half-1.2)/7,-2.6,wall+vault-.3),.07,1.2,'Metal','Z',6)
    for k in range(7):box('Gate merlon',(-half+.3+k*1.6,-2.9,top+1.0),(1.0,.6,1.1),'Stone',.05)
    for k in range(7):box('Machicolation',(-half+.3+k*1.6,-3.35,top-.5),(.5,.9,1.0),'Stone',.04)
    # Heraldic shield over the arch: red field, gold chevron, a crown.
    box('Shield',(0,-3.35,wall+vault+3.2),(2.2,.2,2.4),'Tint_Awning',.25)
    for s in(-1,1):box('Shield chevron',(s*.42,-3.47,wall+vault+3.3),(1.2,.06,.32),'Gold',.02,rot=(0,s*.7,0))
    for k in range(3):cyl('Crown point',(-.6+k*.6,-3.4,wall+vault+4.8),.14,.5,'Gold','Z',8,.02,r2=.02)
    box('Crown band',(0,-3.4,wall+vault+4.5),(1.6,.14,.25),'Gold',.03)
    sign_board(0,-3.4,wall+vault+1.3,7.0,1.1,'WhiteTrim','gate')
    # Drawbridge lowered onto the approach: it pivots at the gate and its far end rests on the bank, falling 0.29 m
    # over 7 m with Via Crest there (the deck top rides ~8 cm above the road all along), chains up to the gate block.
    fall=math.atan2(.29,7.0)
    def deck(d):return -3.2-d*math.cos(fall),-.02-d*math.sin(fall)
    y,z=deck(3.5);box('Drawbridge',(0,y,z-.1),(2*half-.4,7.0,.2),'Wood',.04,rot=(fall,0,0))
    for k in range(9):y,z=deck(.2+k*.75);box('Drawbridge plank',(0,y,z+.02),(2*half-.5,.08,.05),'WoodDark',0,rot=(fall,0,0))
    foot=deck(6.6)[1]+.06;head=wall+vault+1.8
    # Close-set links, alternate ones turned a quarter, so each reads as a chain rather than loose blocks.
    links=int(math.hypot(6.6,head-foot)/.3)
    for s in(-1,1):
        for k in range(links+1):
            t=k/links;box('Chain link',(s*(half-.5),-3.2-6.6*(1-t),foot+t*(head-foot)),(.07,.36,.2) if k%2 else (.2,.36,.07),'Metal',.02,rot=(math.atan2(head-foot,6.6),0,0))
    collision('Left',(-(half+4.0),0,9),(7.4,7.4,18));collision('Right',(half+4.0,0,9),(7.4,7.4,18))
    collision('Over',(0,0,(wall+vault+top)/2+.3),(2*half,6.4,top-wall-vault))

def castle_donjon():
    """The great keep: 18 m square, 30 m to a machicolated parapet, needle-spired corner turrets and a
    belfry rising to ~58 m (open bell stage, octagonal spire, gold orb and vane). Lit windows."""
    s,h=18.0,30.0
    box('Donjon',(0,0,h/2),(s,s,h),'Stone',.1)
    box('Donjon batter',(0,0,1.8),(s+1.6,s+1.6,3.6),'StoneDark',.08)
    for z in(10,20):box('Donjon string',(0,0,z),(s+.3,s+.3,.35),'WhiteTrim',.04)
    for f in(-1,1):
        for k in range(8):
            box('Machicolation',(-s/2+1.1+k*2.26,f*(s/2+.35),h-.6),(.5,.9,1.2),'Stone',.04)
            box('Machicolation',(f*(s/2+.35),-s/2+1.1+k*2.26,h-.6),(.9,.5,1.2),'Stone',.04)
    box('Parapet',(0,0,h+.4),(s+1.2,s+1.2,.8),'Stone',.05)
    crenels(0,0,h+.8,s+1.2,s+1.2)
    for cx in(-1,1):
        for cy in(-1,1):
            x,y=cx*(s/2+.2),cy*(s/2+.2)
            cyl('Corner turret',(x,y,h+1.5),2.2,9.0,'Stone','Z',20,.05)
            for k in range(8):
                a=k*math.tau/8;box('Turret corbel',(x+math.cos(a)*2.1,y+math.sin(a)*2.1,h-2.6),(.4,.6,.9),'Stone',.03,rot=(0,0,a))
            cyl('Turret corbel ring',(x,y,h-1.9),2.4,.4,'WhiteTrim','Z',20,.03,r2=2.2)
            lancet(x,y-cy*2.18,h+2.8,.45,1.1,-cy,lit=True)
            spire(x,y,h+6.0,2.2,9.5)
    # Belfry: stone shaft, open bell stage (arches each side), octagonal spire.
    b=7.0;bz=h+.8
    box('Belfry shaft',(0,0,bz+6),(b,b,12),'Stone',.08)
    box('Belfry string',(0,0,bz+12.1),(b+.4,b+.4,.4),'WhiteTrim',.04)
    for cx in(-1,1):
        for cy in(-1,1):box('Bell pier',(cx*(b/2-.55),cy*(b/2-.55),bz+15),(1.1,1.1,5.6),'Stone',.05)
    box('Bell stage cornice',(0,0,bz+18.1),(b+.6,b+.6,.6),'WhiteTrim',.05)
    box('Bell stage floor',(0,0,bz+12.3),(b-.2,b-.2,.3),'StoneDark',.03)
    cyl('Bell',(0,0,bz+15.2),1.3,2.2,'Gold','Z',20,.08,r2=.7)
    cyl('Bell crown',(0,0,bz+16.6),.35,.6,'Gold','Z',12,.04)
    box('Bell yoke',(0,0,bz+17.1),(b-.6,.35,.35),'WoodDark',.03)
    for cx in(-1,1):
        for cy in(-1,1):spire(cx*(b/2-.4),cy*(b/2-.4),bz+18.4,.55,3.2,finial=False)
    cyl('Belfry spire eave',(0,0,bz+18.7),b*.62,.6,'RoofSlate','Z',8,.05,r2=b*.5)
    cyl('Belfry spire',(0,0,bz+25.0),b*.5,12.0,'RoofSlate','Z',8,.05,r2=.08)
    sphere('Belfry orb',(0,0,bz+31.3),(.55,.55,.55),'Gold',2)
    cyl('Vane rod',(0,0,bz+33.0),.07,3.0,'Gold','Z',8)
    box('Vane',(.7,0,bz+33.6),(1.6,.05,.55),'Gold',.02)
    for k in range(4):
        a=k*math.pi/2;cyl('Compass arm',(math.cos(a)*.5,math.sin(a)*.5,bz+32.4),.035,1.0,'Gold','X' if k%2==0 else 'Y',6)
    # Clock on the front of the belfry shaft.
    cyl('Clock face',(0,-b/2-.08,bz+8.5),1.8,.2,'WhiteTrim','Y',32,.05)
    torus('Clock ring',(0,-b/2-.2,bz+8.5),1.8,.12,'Gold','Y',32,8)
    box('Clock hand',(.35,-b/2-.25,bz+8.9),(.12,.06,1.3),'Black',.02,rot=(0,-.5,0))
    box('Clock hand',(-.3,-b/2-.25,bz+8.3),(.12,.06,.9),'Black',.02,rot=(0,1.9,0))
    # Great door, tall lit windows on every face, a heraldic banner down the front.
    door(0,-s/2-.8,0,3.0,4.4,canopy=False)   # in front of the batter
    for f,ax in((-1,'Y'),(1,'Y'),(-1,'X'),(1,'X')):
        for z in(13.5,23.5):
            for o in(-5,5):
                if ax=='Y':lancet(o,f*s/2,z,1.1,2.6,f,lit=(z>20)^(o>0))
                else:lancet(f*s/2,o,z,1.1,2.6,f,lit=(z<20)^(o>0))
    box('Donjon banner rod',(0,-s/2-.4,h-2.4),(3.6,.14,.14),'Gold',.03)
    box('Donjon banner',(0,-s/2-.35,h-7.0),(3.0,.08,9.0),'Tint_Awning',.02)
    box('Banner crown band',(0,-s/2-.42,h-4.6),(1.6,.05,.3),'Gold',.02)
    for k in range(3):cyl('Banner crown point',(-.55+k*.55,-s/2-.42,h-4.1),.12,.5,'Gold','Z',8,.02,r2=.02)
    wedge('Banner tail',(-1.5,-s/2-.35,h-11.5),(1.5,-s/2-.35,h-11.5),(0,-s/2-.35,h-12.8),(0,-1,0),'Tint_Awning',.08)
    collision('Donjon',(0,0,h/2),(s+1.6,s+1.6,h))

def castle_palace():
    """Palazzo wing, 30 x 11 m, three storeys in warm render with stone quoins: an arcade below,
    a flower-boxed piano nobile, an open loggia on top facing -Y, terracotta hipped roof, chimneys."""
    w,d,h=30.0,11.0,15.0
    box('Palace',(0,0,h/2),(w,d,h),'Tint_Wall',.08)
    box('Palace plinth',(0,0,.5),(w+.3,d+.3,1.0),'StoneDark',.05)
    for sx in(-1,1):
        for sy in(-1,1):
            for k in range(7):box('Quoin',(sx*(w/2-.2),sy*(d/2-.2),1.2+k*2.0),(.9 if k%2 else .6,.9 if k%2 else .6,.9),'Stone',.04)
    for z in(5.0,10.2):box('Palace string',(0,0,z),(w+.3,d+.3,.3),'WhiteTrim',.04)
    box('Cornice',(0,0,h-.2),(w+.9,d+.9,.5),'WhiteTrim',.06)
    # Ground floor arcade (front, -Y).
    n=7
    for k in range(n):
        x=-w/2+2.2+k*(w-4.4)/(n-1)
        arch_ring('Arcade arch',x,-d/2-.02,0,1.3,1.2,.4,.3,'Stone',9)
        box('Arcade shadow',(x,-d/2+.02,1.6),(2.4,.2,3.2),'Black',.02)
        if k<n-1:box('Arcade pier',(x+2.05,-d/2-.05,1.9),(.8,.3,3.8),'Stone',.04)
    # Piano nobile: tall shuttered windows with balconies and flowers.
    for k in range(8):
        x=-w/2+2.6+k*(w-5.2)/7
        window(x,-d/2,7.6,1.3,2.2,shutters=True,flowers=(k%2==0),face=-1,glass='GlassLit' if k in(2,5) else 'Glass')
        window(x,d/2,7.6,1.3,2.2,shutters=True,flowers=False,face=1)
    box('Balcony',(0,-d/2-.8,6.2),(7.0,1.6,.25),'Stone',.04)
    for k in range(15):cyl('Baluster',(-3.3+k*.47,-d/2-1.45,6.75),.09,.9,'WhiteTrim','Z',8,.02)
    box('Balcony rail',(0,-d/2-1.45,7.25),(7.0,.22,.16),'WhiteTrim',.03)
    # Top floor loggia: open arches with lit interior and a railing.
    box('Loggia back',(0,-d/2+1.8,12.6),(w-2,.2,4.4),'Tint_Door',.02)
    for k in range(9):
        x=-w/2+2.0+k*(w-4.0)/8
        if k<8:
            cx=x+(w-4.0)/16
            arch_ring('Loggia arch',cx,-d/2-.02,10.5,1.2,1.0,.35,.3,'WhiteTrim',9)
        cyl('Loggia column',(x,-d/2+.4,11.9),.22,2.9,'WhiteTrim','Z',12,.03)
    box('Loggia rail',(0,-d/2+.1,11.1),(w-2.4,.2,.9),'WhiteTrim',.04)
    for k in range(4):box('Loggia lamp',(-9+k*6,-d/2+1.5,13.6),(.5,.5,.7),'GlassLit',.1)
    hip_roof(0,0,w,d,h+.1,4.2,.7,'RoofTile')
    for x in(-10,-2,7):
        box('Chimney',(x,1.5,h+4.2),(1.0,1.0,3.0),'Tint_Wall',.05)
        box('Chimney cap',(x,1.5,h+5.8),(1.4,1.4,.25),'WhiteTrim',.04)
    collision('Palace',(0,0,h/2),(w,d,h))

def zip_tower():
    """Castle Zip Tower, set into the curtain wall: a drive-in bay on the courtyard side (+Y), a hoist
    shaft, and a launch arch at the top facing out (-Y) with a timber boom and a spinning pulley that
    carries the cable down to the lower town. Cable anchor at the pulley's lower edge."""
    w,d,h=11.0,12.0,24.0
    for sx in(-1,1):box('Zip tower side',(sx*(w/2-.8),0,h/2),(1.6,d,h),'Stone',.08)
    for sx in(-1,1):box('Zip tower batter',(sx*(w/2-.2),0,1.2),(2.2,d+1.0,2.4),'StoneDark',.06)
    # Courtyard face (+Y): the bay arch the car drives in under. Outer face (-Y): solid to the launch
    # deck, then the launch arch the car swings out through.
    arch_ring('Bay arch',0,d/2+.02,3.6,3.9,2.4,.7,.35,'WhiteTrim',13,face=1)
    spandrel(0,d/2-.8,3.6,3.9,2.4,h,1.6,'Stone')
    box('Outer lower wall',(0,-d/2+.8,5.1),(w,1.6,10.2),'Stone',.08)
    box('Launch sill',(0,-d/2-.2,10.0),(w-2.6,2.4,.5),'WhiteTrim',.05)
    # 3.8 m rise: the trolley, cradle and car pass level through it (crown 14.0 m).
    arch_ring('Launch arch',0,-d/2-.02,10.2,3.9,3.8,.8,.35,'WhiteTrim',13)
    spandrel(0,-d/2+.8,10.2,3.9,3.8,h,1.6,'Stone')
    for sx in(-1,1):box('Hoist ledge',(sx*(w/2-2.0),-1.0,9.95),(1.0,d-4.0,.5),'WoodDark',.03)
    for z in(8,16):box('Tower string',(0,0,z),(w+.3,d+.3,.3),'WhiteTrim',.04)
    box('Zip parapet',(0,0,h+.4),(w+.8,d+.8,.8),'Stone',.05)
    crenels(0,0,h+.8,w+.8,d+.8)
    for sx in(-1,1):
        for sy in(-1,1):cyl('Zip turret',(sx*(w/2+.2),sy*(d/2+.2),h+1.8),1.4,4.0,'Stone','Z',16,.05)
        for sy in(-1,1):spire(sx*(w/2+.2),sy*(d/2+.2),h+3.8,1.4,5.0)
    hip_roof(0,0,w-1.0,d-1.0,h+.9,5.0,.4,'RoofSlate')
    # Hoist: headframe beam and winch drum in the shaft.
    box('Hoist beam',(0,0,h-2.0),(w-2.8,.8,.8),'WoodDark',.04)
    cyl('Winch drum',(0,1.2,h-3.2),.7,3.0,'Wood','X',16,.04)
    for sx in(-1,1):cyl('Hoist rope',(sx*.9,0,h/2+2),.05,h-6,'Rope','Z',6)
    # Boom out of the launch arch, the pulley on its end.
    for sx in(-1,1):box('Boom',(sx*.9,-d/2-2.2,13.6),(.35,5.4,.45),'WoodDark',.03,rot=(-.18,0,0))
    box('Boom tie',(0,-d/2-2.6,14.0),(2.2,.4,.4),'WoodDark',.03)   # clear of the trolley's climb to the pulley
    global SPIN_HUB,SPIN_AXIS
    SPIN_HUB=(0,-d/2-4.6,14.9);SPIN_AXIS='X'
    SPIN.append(torus('Pulley rim',SPIN_HUB,.75,.14,'Yellow','X',32,10))
    for k in range(6):
        a=k*math.pi/3;SPIN.append(box('Pulley spoke',(0,SPIN_HUB[1]+math.cos(a)*.37,SPIN_HUB[2]+math.sin(a)*.37),(.12,.74,.12),'Yellow',.02,rot=(a,0,0)))
    SPIN.append(cyl('Pulley hub',SPIN_HUB,.22,.4,'Metal','X',16,.03))
    anchor('CableAnchor',(0,-d/2-4.6,14.9+.75))
    anchor('BayAnchor',(0,1.0,0))
    sign_board(0,d/2+.1,6.3,6.0,1.0,'Yellow','zipback',face=1)
    box('Zip banner',(0,-d/2-.1,19.5),(4.0,.08,5.0),'Tint_Awning',.02)
    collision('Left',(-(w/2-.8),0,h/2),(1.6,d,h));collision('Right',(w/2-.8,0,h/2),(1.6,d,h))
    collision('Back',(0,-d/2+.8,5.1),(w,1.6,10.2))

def zip_station():
    """Lower-town zip station: stone piers and a timber headframe over a drive-through bay (along Y),
    the pulley on a boom facing the castle (-Y), a striped canopy and a sign."""
    w,h=9.0,15.0
    for sx in(-1,1):
        box('Station pier',(sx*(w/2),0,3.0),(1.6,6.0,6.0),'Stone',.08)
        for sy in(-1,1):box('Headframe leg',(sx*(w/2),sy*2.2,6+(h-6)/2),(.5,.5,h-6),'WoodDark',.04,rot=(0,-sx*.08,0))
        box('Headframe brace',(sx*(w/2-.3),0,10.5),(.3,4.6,.3),'WoodDark',.03,rot=(.9,0,0))
    box('Headframe beam',(0,0,h),(w+1.6,5.0,.9),'WoodDark',.04)
    box('Station roof',(0,0,h+1.2),(w+3.0,7.0,.3),'RoofTile',.05)
    wedge('Roof gable',(-(w/2+1.5),-3.5,h+1.3),((w/2+1.5),-3.5,h+1.3),(0,-3.5,h+3.6),(0,-1,0),'Tint_Wall',.2)
    wedge('Roof gable',(-(w/2+1.5),3.5,h+1.3),((w/2+1.5),3.5,h+1.3),(0,3.5,h+3.6),(0,1,0),'Tint_Wall',.2)
    for s in(-1,1):box('Roof slope',(s*(w/2+1.5)*.5,0,h+2.45),(math.hypot(w/2+1.5,2.3)+.2,7.4,.25),'RoofTile',.05,rot=(0,s*math.atan2(2.3,w/2+1.5),0))
    awning(0,3.0,6.2,w-1.8,1.8,10,face=1)
    awning(0,-3.0,6.2,w-1.8,1.8,10,face=-1)
    for sx in(-1,1):box('Deck edge',(sx*(w/2-1.3),0,.12),(.4,8.0,.24),'Yellow',.03)
    sign_board(0,3.2,8.3,6.4,1.1,'Yellow','zipback',face=1)
    for sx in(-1,1):box('Boom',(sx*.9,-4.8,h-.8),(.35,6.4,.45),'WoodDark',.03,rot=(.12,0,0))
    global SPIN_HUB,SPIN_AXIS
    SPIN_HUB=(0,-7.6,h-.3);SPIN_AXIS='X'
    SPIN.append(torus('Pulley rim',SPIN_HUB,.75,.14,'Yellow','X',32,10))
    for k in range(6):
        a=k*math.pi/3;SPIN.append(box('Pulley spoke',(0,SPIN_HUB[1]+math.cos(a)*.37,SPIN_HUB[2]+math.sin(a)*.37),(.12,.74,.12),'Yellow',.02,rot=(a,0,0)))
    SPIN.append(cyl('Pulley hub',SPIN_HUB,.22,.4,'Metal','X',16,.03))
    anchor('CableAnchor',(0,-7.6,h-.3+.75))
    anchor('BayAnchor',(0,0,0))
    collision('Left pier',(-(w/2),0,3.0),(1.6,6.0,6.0));collision('Right pier',(w/2,0,3.0),(1.6,6.0,6.0))

def zip_trolley():
    """Cable trolley and car cradle. Origin on the cable; the cradle holds a car whose ground contact
    sits 2.7 m below it (the car envelope is 2.2 x 3.2 x 1.7 m)."""
    box('Trolley body',(0,0,-.35),(.5,1.8,.55),'Yellow',.12)
    for sy in(-1,1):
        cyl('Trolley wheel',(0,sy*.55,0),.28,.22,'Metal','X',20,.03)
        cyl('Trolley wheel groove',(0,sy*.55,0),.2,.26,'Black','X',16,.02)
    box('Trolley side plate',(.2,0,-.1),(.06,1.6,.6),'Yellow',.02);box('Trolley side plate',(-.2,0,-.1),(.06,1.6,.6),'Yellow',.02)
    cyl('Hanger',(0,0,-.95),.08,1.0,'Metal','Z',10)
    box('Spreader',(0,0,-1.5),(.25,3.2,.25),'Yellow',.05)
    box('Spreader cross',(0,0,-1.5),(2.7,.25,.25),'Yellow',.05)
    for sx in(-1,1):
        for sy in(-1,1):
            box('Cradle strap',(sx*1.3,sy*1.5,-2.15),(.12,.18,1.3),'Rope',.02)
            box('Strap',(sx*.65,sy*1.5,-1.55),(1.3,.12,.1),'Rope',.02)
        box('Cradle rail',(sx*1.3,0,-2.75),(.14,3.4,.14),'Yellow',.03)
    for sy in(-1,1):box('Cradle bar',(0,sy*1.5,-2.8),(2.8,.16,.16),'Yellow',.04)

def station():
    """Hillcrest station: booking hall with clock gable, long canopy on iron columns over the
    platform (platform edge along +Y, 2.5 m behind the building line)."""
    w,d,h=22.0,8.0,5.6
    walls(w,d,h)
    gable_roof(0,0,w,d,h,2.1,.4,'RoofSlate')
    box('Clock gable',(0,-d/2-.05,h+1.2),(6,.3,2.4),'Tint_Wall',.08)
    cyl('Station clock',(0,-d/2-.25,h+1.25),.8,.14,'WhiteTrim','Y',28);cyl('Clock rim',(0,-d/2-.2,h+1.25),.9,.1,'Gold','Y',28)
    box('Hand',(0,-d/2-.35,h+1.45),(.06,.04,.5),'Black',.01);box('Hand',(.2,-d/2-.35,h+1.25),(.42,.04,.06),'Black',.01)
    for x in(-6,6):door(x,-d/2,0,1.6,2.8)
    for x in(-9,-3,3,9):window(x,-d/2,1.9,1.4,1.6,flowers=True)
    sign_board(0,-d/2-.05,4.4,9,.9,'Tint_Shutter','station')
    box('Platform',(0,d/2+3.2,.45),(w+16,6.4,.9),'Stone',.05)
    box('Platform edge',(0,d/2+6.3,.92),(w+16,.5,.06),'Yellow',.01)
    for k in range(8):
        x=-w/2-6+k*(w+12)/7;cyl('Canopy column',(x,d/2+4.0,2.3),.14,3.2,'Tint_Shutter','Z',10)
    box('Canopy',(0,d/2+3.3,4.0),(w+14,6.2,.25),'Tint_Shutter',.05,rot=(-.06,0,0))
    for k in range(20):cyl('Canopy valance',(-w/2-6.5+k*(w+13)/19,d/2+6.4,3.72),.3,.05,'WhiteTrim','Y',10,.01)
    for x in(-12,12):box('Station bench',(x,d/2+2.2,1.3),(2.2,.6,.12),'Wood',.03)
    for x in(-15,15):cyl('Platform lamp',(x,d/2+5.4,2.8),.1,3.8,'Tint_Shutter','Z',8);sphere('Lamp globe',(x,d/2+5.4,4.8),(.3,.3,.3),'GlassLit',1)
    collision('Hall',(0,0,h/2),(w+.3,d+.3,h))

def mill():
    """Riverside water mill (waterwheel on the -X gable turns in Unity, Spin_X)."""
    w,d,h=9.0,11.0,7.0;walls(w,d,h)
    gable_roof(0,0,w,d,h,2.8,.45,'RoofTile','Y')
    door(0,-d/2,0,1.8,2.6)
    for z in(2.0,5.0):window(-2.5 if z<3 else 2.5,-d/2,z,1.1,1.3)
    for x in(2.6,3.5):box('Sack',(x,-d/2-1.1,.35),(.7,.5,.7),'Canvas',.2)
    box('Wheel axle housing',(-w/2-.4,0,3.4),(.8,1.2,1.2),'WoodDark',.05)
    global SPIN_HUB,SPIN_AXIS
    SPIN_HUB=(-w/2-1.4,0,3.4);SPIN_AXIS='X'
    parts=[cyl('Wheel hub',(-w/2-1.4,0,3.4),.45,1.6,'WoodDark','X',16)]
    for side in(-.7,.7):
        for k in range(16):
            a0=k*math.tau/16;a1=(k+1)*math.tau/16;am=(a0+a1)/2
            o=box('Wheel rim',(-w/2-1.4+side,math.cos(am)*3.2,3.4+math.sin(am)*3.2),(.18,1.3,.3),'Wood',.03,rot=(am+math.pi/2,0,0));parts.append(o)
    for k in range(12):
        a=k*math.tau/12
        o=box('Paddle',(-w/2-1.4,math.cos(a)*2.9,3.4+math.sin(a)*2.9),(1.6,.12,.9),'WoodDark',.02,rot=(a-math.pi/2,0,0));parts.append(o)
        o=box('Spoke',(-w/2-1.4,math.cos(a)*1.6,3.4+math.sin(a)*1.6),(.3,.14,3.1),'Wood',.02,rot=(a-math.pi/2,0,0));parts.append(o)
    SPIN.extend(parts)
    box('Mill race',(-w/2-1.4,0,-.3),(2.2,8,.6),'StoneDark',.04)
    box('Mill race water',(-w/2-1.4,0,.04),(1.6,7.7,.06),'Water',.01)
    collision('Body',(0,0,h/2),(w+.3,d+.3,h))

def bandstand():
    """Octagonal bandstand with a striped canopy."""
    cyl('Bandstand base',(0,0,.5),4.6,1.0,'Stone','Z',8,.06)
    for k in range(8):
        a=k*math.tau/8+math.pi/8;cyl('Bandstand column',(math.cos(a)*4.1,math.sin(a)*4.1,2.6),.13,3.2,'WhiteTrim','Z',10)
    for k in range(16):
        a=k*math.tau/16;box('Bandstand rail',(math.cos(a)*4.3,math.sin(a)*4.3,1.4),(1.7,.08,.08),'WhiteTrim',.02,rot=(0,0,a+math.pi/2))
    cyl('Bandstand canopy',(0,0,5.0),5.2,1.9,'Tint_Awning','Z',8,.05,r2=.3)
    cyl('Canopy trim',(0,0,4.1),5.25,.22,'Gold','Z',8,.02)
    sphere('Canopy finial',(0,0,6.1),(.28,.28,.4),'Gold',1)
    collision('Base',(0,0,.5),(9,9,1))

def wind_turbine():
    """46 m white turbine; the rotor (Spin_Y) faces -Y."""
    for k in range(6):cyl('Turbine mast',(0,0,4+k*7.7),1.3-k*.12,7.8,'WhiteTrim','Z',20,.02,r2=1.3-(k+1)*.12)
    box('Nacelle',(0,.6,46.6),(1.8,5.0,2.0),'WhiteTrim',.4)
    global SPIN_HUB,SPIN_AXIS
    SPIN_HUB=(0,-2.2,46.6);SPIN_AXIS='Y'
    parts=[sphere('Rotor nose',(0,-2.3,46.6),(.9,1.2,.9),'Red',2)]
    for k in range(3):
        a=k*math.tau/3+.3;d=Vector((math.cos(a),0,math.sin(a)))
        for t,wd in((5,1.4),(11,1.1),(17,.7)):
            o=box('Blade',Vector((0,-2.2,46.6))+d*t,(6.2,.18,wd),'WhiteTrim',.05);o.rotation_euler=(0,-a,0);parts.append(o)
        o=box('Blade tip',Vector((0,-2.2,46.6))+d*20.2,(1.2,.2,.5),'Red',.05);o.rotation_euler=(0,-a,0);parts.append(o)
    SPIN.extend(parts)
    collision('Mast',(0,0,20),(2.6,2.6,40))

def balloon():
    """Hot-air balloon (~16 m tall): striped envelope (Tint_Awning / Canvas / Tint_Shutter), wicker basket."""
    # Envelope: 12 gores lathed from a teardrop profile, alternating colours.
    prof=[(1.9,7.6),(3.6,8.6),(5.2,10.2),(6.2,12.2),(6.6,14.2),(6.3,16.2),(5.2,17.9),(3.4,19.2),(1.2,19.9),(0.01,20.05)]
    cols=['Tint_Awning','Canvas','Tint_Shutter','Canvas']
    for k in range(12):
        a0=k*math.tau/12;a1=(k+1)*math.tau/12;verts=[];faces=[]
        for r,z in prof:
            for a in(a0,(a0+a1)/2,a1):verts.append((math.cos(a)*r,math.sin(a)*r,z))
        for i in range(len(prof)-1):
            for j in range(2):b0=i*3+j;faces.append((b0,b0+1,b0+4,b0+3))
        poly('Envelope gore',verts,faces,cols[k%4],smooth=True)
    cyl('Mouth',(0,0,7.4),1.6,1.6,'Tint_Shutter','Z',16,.02,r2=2.2)
    for k in range(4):
        a=k*math.tau/4+math.pi/4;box('Rope',(math.cos(a)*1.2,math.sin(a)*1.2,4.4),(.04,.04,4.4),'Rope',0,rot=(math.sin(a)*.18,-math.cos(a)*.18,0))
    box('Basket',(0,0,1.3),(1.8,1.8,1.3),'Wood',.2)
    box('Basket rim',(0,0,1.98),(1.95,1.95,.14),'WoodDark',.05)
    cyl('Burner',(0,0,2.9),.35,.6,'Metal','Z',10)

def grandstand():
    """Raceway grandstand, 40 m: tiered coloured seats, roof on columns, name board."""
    L=40.0
    for k in range(7):
        box('Terrace step',(0,.8+k*1.05,.4+k*.7),(L,1.1,.8+k*1.4),'Stone',.03)
        box('Seat row',(0,.55+k*1.05,.95+k*1.4),(L-.4,.5,.25),['Red','Yellow','Blue','Green'][k%4],.04)
    box('Stand back',(0,8.3,5.5),(L,.4,11),'Tint_Wall',.05)
    for x in range(-20,21,8):cyl('Stand column',(x,7.8,6.6),.2,13,'Tint_Shutter','Z',10)
    box('Stand roof',(0,4.2,12.6),(L+2,9.5,.4),'Tint_Shutter',.08,rot=(-.12,0,0))
    sign_board(0,-.6,14.0,18,1.6,'Yellow','stand')
    for k in range(11):box('Flag',(-20+k*4,8.1,13.9),(.9,.05,.6),['Red','Yellow','Blue','Green','WhiteTrim'][k%5],.01)
    # Rear facade (the paddock side): concourse arches with kiosks, a blue band, a window row,
    # team banners, stair towers at both ends and the raceway name.
    for x in(-15,-5,5,15):
        box('Concourse opening',(x,8.52,1.6),(4.0,.06,3.2),'Glass',.02)
        box('Concourse frame',(x,8.56,3.3),(4.5,.1,.3),'WhiteTrim',.02)
        for s in(-1,1):box('Concourse frame',(x+s*2.12,8.56,1.6),(.26,.1,3.2),'WhiteTrim',.02)
    for x in(-15,15):awning(x,8.5,3.9,4.4,1.3,8,face=1)
    box('Rear band',(0,8.56,4.6),(L,.1,.5),'Tint_Shutter',.02)
    for k in range(14):box('Rear window',(-18.2+k*2.8,8.54,6.5),(1.5,.06,1.1),'Glass',.02)
    for k,x in enumerate((-10,0,10)):box('Rear banner',(x,8.6,8.4),(1.6,.06,2.6),['Red','Yellow','Blue'][k],.02)
    box('Rear cornice',(0,8.6,10.8),(L+.2,.2,.4),'WhiteTrim',.03)
    sign_board(0,8.5,9.3,14,1.4,'Yellow','standback',face=1)
    for s in(-1,1):
        box('Stair tower',(s*21.6,5.5,5.8),(3.2,6,11.6),'Tint_Shutter',.08)
        box('Stair slit',(s*21.6,8.52,6),(.9,.06,8.5),'Glass',.02)
        collision('Stair tower',(s*21.6,5.5,5.8),(3.2,6,11.6))
    collision('Stand',(0,4.2,5.5),(L,8.6,11))

def train_loco():
    """Toy steam locomotive (~10 m) with tender coupling at +Y; chimney smoke is added in Unity."""
    box('Loco frame',(0,0,1.05),(2.3,10,.35),'Black',.05)
    cyl('Boiler',(0,-1.6,2.35),1.05,6.2,'Tint_Wall','Y',24,.05)
    for y in(-4.2,-2.4,-.6):cyl('Boiler band',(0,y,2.35),1.09,.16,'Gold','Y',24)
    cyl('Smokebox',(0,-4.5,2.35),1.08,.9,'Black','Y',24);cyl('Smokebox door',(0,-4.98,2.35),.8,.1,'MetalLight','Y',24)
    cyl('Chimney',(0,-4.3,3.9),.34,1.4,'Black','Z',16,.05,r2=.46);cyl('Dome',(0,-2.0,3.5),.5,.5,'Gold','Z',16,.1,r2=.35)
    box('Cab',(0,2.6,2.9),(2.5,2.8,2.9),'Tint_Wall',.1);box('Cab roof',(0,2.6,4.45),(2.8,3.2,.2),'Black',.05)
    for sd in(-1,1):box('Cab window',(sd*1.26,2.3,3.4),(.06,1.0,.8),'GlassLit',.02)
    for k,y in enumerate((-3.4,-1.6,.2)):
        for sd in(-1,1):cyl('Driving wheel',(sd*1.05,y,.9),.85,.22,'Tint_Awning','X',20,.03);cyl('Hub',(sd*1.18,y,.9),.25,.1,'Gold','X',12)
    for sd in(-1,1):box('Coupling rod',(sd*1.2,-1.6,.95),(.06,3.9,.12),'MetalLight',.02)
    box('Buffer beam',(0,-5.05,1.1),(2.4,.3,.5),'Red',.03)
    for sd in(-1,1):cyl('Buffer',(sd*.8,-5.3,1.1),.18,.3,'Black','Y',10)
    cyl('Headlamp',(0,-5.05,3.3),.24,.2,'GlassLit','Y',12)
    box('Tender',(0,6.4,1.9),(2.4,3.4,1.9),'Tint_Shutter',.1)
    for y in(5.4,7.4):
        for sd in(-1,1):cyl('Tender wheel',(sd*1.05,y,.6),.55,.2,'Black','X',16,.03)
    sphere('Coal',(0,6.2,2.9),(1.0,1.3,.4),'Black',2)

def train_coach():
    """Toy coach (~11 m), windows lit warm; couples at both ends along Y."""
    box('Coach body',(0,0,2.3),(2.5,11,2.6),'Tint_Wall',.2)
    box('Coach roof',(0,0,3.75),(2.7,11.2,.35),'RoofSlate',.18)
    box('Coach waist',(0,0,1.8),(2.56,11.02,.3),'Tint_Shutter',.03)
    for k in range(7):
        for sd in(-1,1):box('Coach window',(sd*1.27,-4.3+k*1.43,2.7),(.06,1.0,.9),'GlassLit',.02)
    for y in(-3.8,3.8):
        box('Bogie',(0,y,.75),(2.0,2.4,.4),'Black',.05)
        for yy in(-.8,.8):
            for sd in(-1,1):cyl('Coach wheel',(sd*1.0,y+yy,.55),.45,.18,'Black','X',14,.03)
    for y in(-5.6,5.6):box('Gangway',(0,y,2.2),(1.2,.4,2.2),'Black',.05)

def olive_tree():
    cyl('Olive trunk',(0,0,.9),.32,1.8,'Bark','Z',9,.08,r2=.22)
    cyl('Olive limb',(.3,0,1.9),.14,1.1,'Bark','X',8)
    for i,(x,y,z,r) in enumerate([(0,0,2.5,1.4),(.9,.3,2.3,1.0),(-.8,-.2,2.35,1.0),(.2,-.8,2.9,.9),(-.3,.8,2.9,.9)]):
        sphere('Olive crown',(x,y,z),(r,r,r*.7),'LeafLight' if i%2 else 'Leaf',2)
    satellites(1.3,2.2,3.0,['LeafLight','Leaf'],8,19,(.4,.6))
    collision('Trunk',(0,0,1),(.6,.6,2))

def field_row(kind):
    """12 m flower-field row: lavender (purple mounds) or sunflowers (stems with yellow heads)."""
    rnd=random.Random(5 if kind=='lavender' else 9)
    if kind=='lavender':
        for k in range(16):
            x=-5.6+k*.75;sphere('Lavender mound',(x,rnd.uniform(-.05,.05),.35),(.42,.36,.38),'Leaf',1)
            sphere('Lavender bloom',(x,rnd.uniform(-.08,.08),.62),(.36,.3,.18),'FlowerBlue' if k%3 else 'FlowerPink',1)
    else:
        for k in range(13):
            x=-5.6+k*.93;z=1.5+rnd.uniform(-.2,.25)
            cyl('Sunflower stem',(x,0,z/2),.05,z,'Leaf','Z',6)
            cyl('Sunflower head',(x,-.12,z),.34,.12,'FlowerYellow','Y',12,.02);cyl('Sunflower eye',(x,-.19,z),.16,.06,'WoodDark','Y',10)
            sphere('Sunflower leaf',(x+.15,0,z*.55),(.3,.12,.18),'LeafLight',1)
def lavender_row():field_row('lavender')
def sunflower_row():field_row('sunflower')

def hotel():
    """Grand Hotel: four storeys, balconies, a rooftop sign board and striped canopy."""
    w,d,h=16.0,10.0,13.5;walls(w,d,h)
    box('Hotel roof',(0,0,h+.3),(w+.6,d+.6,.6),'RoofTile',.1)
    box('Roof terrace rail',(0,-d/2+.2,h+1.0),(w,.1,.8),'WhiteTrim',.02)
    box('Hotel canopy',(0,-d/2-1.6,3.3),(5.5,3.2,.25),'Tint_Awning',.06)
    for x in(-2.5,2.5):cyl('Canopy post',(x,-d/2-3.0,1.6),.08,3.2,'Gold','Z',8)
    door(0,-d/2,0,2.2,2.8,canopy=False)
    for z in(1.9,5.4,8.6,11.8):
        for x in(-6,-3,3,6)if z<3 else(-6,-3,0,3,6):
            window(x,-d/2,z,1.2,1.7,flowers=z>3)
    for z in(4.55,7.75,10.95):
        box('Hotel balcony',(0,-d/2-.6,z-.8),(w-2,1.2,.18),'Stone',.04)
        box('Hotel rail',(0,-d/2-1.15,z-.35),(w-2,.08,.08),'Metal',.02)
    sign_board(0,-d/2+1.0,h+2.0,9,1.3,'Tint_Shutter','hotel')
    collision('Body',(0,0,h/2),(w+.3,d+.3,h))

def map_board():
    """Viewpoint information board on two posts (SignAnchor for the title)."""
    for x in(-1.1,1.1):cyl('Board post',(x,0,.9),.08,1.8,'WoodDark','Z',8)
    box('Board',(0,0,1.45),(2.6,.12,1.3),'Wood',.05,rot=(.2,0,0))
    box('Board face',(0,-.08,1.45),(2.3,.04,1.05),'Canvas',.02,rot=(.2,0,0))
    for k,(x,z,c) in enumerate([(-.6,1.6,'Green'),(.2,1.3,'Blue'),(.7,1.7,'Yellow'),(-.2,1.2,'Red')]):box('Map mark',(x,-.12,z),(.25,.03,.18),c,.01,rot=(.2,0,0))
    sign_board(0,-.05,2.35,2.6,.42,'Yellow','map')
    collision('Board',(0,0,.9),(2.8,.4,1.8))

def flag_pole():
    cyl('Flag pole base',(0,0,.2),.35,.4,'Stone','Z',12);cyl('Flag pole',(0,0,4.2),.07,8,'MetalLight','Z',8);sphere('Flag knob',(0,0,8.25),(.12,.12,.12),'Gold',1)
    box('Flag',(.95,0,7.3),(1.9,.05,1.2),'Tint_Awning',.02);box('Flag stripe',(.95,-.03,7.3),(1.9,.04,.4),'Tint_Shutter',.01)
    collision('Pole',(0,0,4),(.4,.4,8))

def flag_wales():
    """Y Ddraig Goch on a pole (Quarry Hills' square; street review, 4 Oct): white over green, a red dragon passant."""
    cyl('Flag pole base',(0,0,.2),.35,.4,'Stone','Z',12);cyl('Flag pole',(0,0,4.2),.07,8,'MetalLight','Z',8);sphere('Flag knob',(0,0,8.25),(.12,.12,.12),'Gold',1)
    box('Flag white',(1.05,0,7.6),(2.1,.05,.66),'WhiteTrim',.0);box('Flag green',(1.05,0,6.94),(2.1,.05,.66),'Green',.0)
    # The dragon: one flat outline (head toward the pole, wing raised, tail curled up), on both faces.
    outline=[(.08,.66),(.16,.74),(.22,.70),(.26,.62),(.34,.66),(.40,.86),(.50,.80),(.56,.92),(.62,.76),(.70,.64),(.82,.66),(.92,.80),(.96,.74),
        (.88,.58),(.76,.52),(.74,.36),(.80,.24),(.72,.24),(.66,.40),(.50,.42),(.42,.28),(.46,.18),(.38,.18),(.34,.36),(.28,.50),(.20,.56),(.10,.60)]
    for y,rev in((-.035,False),(.035,True)):
        v=[(.05+u*2.0,y,6.61+w*1.32) for u,w in outline];f=list(range(len(v)))
        poly('Dragon',v,[tuple(reversed(f)) if rev else tuple(f)],'Red')
    collision('Pole',(0,0,4),(.4,.4,8))

# ---------------------------------------------------------------- Quarry Hills: Welsh cottages, quarry plant, rocks (4 Oct)

def slate_roof(cx,cy,w,d,eave,rise,over=.35):
    """Blue-grey slate gable roof (ridge along X): thin courses, a clay ridge, stone gable copings; gables in the wall stone."""
    half=d/2+over;length=w+2*over;slope=math.atan2(rise,d/2);rows=max(4,int(math.hypot(d/2,rise)/.3))
    for side in(-1,1):
        for i in range(rows):
            t=(i+.5)/rows;y=cy+side*half*(1-t);z=eave+rise*t-.05
            box('Slate course',(cx,y,z+.08),(length,math.hypot(half,rise)/rows+.05,.09),'RoofSlateDark' if i%4==1 else 'RoofSlate',.02,rot=(-side*slope,0,0))
        cyl('Gutter',(cx,cy+side*(half-.02),eave+.02),.08,length,'Metal','X',10)
    cyl('Ridge tile',(cx,cy,eave+rise+.1),.16,length+.05,'RoofTile','X',12)
    for side in(-1,1):
        x=cx+side*(w/2)
        wedge('Gable',(x,cy-d/2,eave),(x,cy+d/2,eave),(x,cy,eave+rise),(side,0,0),'Tint_WallStone')
        for s in(-1,1):box('Gable coping',(cx+side*(w/2+.05),cy+s*half*.5,eave+rise*.5+.1),(.4,math.hypot(half,rise)+.1,.16),'StoneDark',.03,rot=(-s*slope,0,0))

def cottage(kind='a'):
    """Quarrymen's cottage (Quarry Hills; street review, 4 Oct: the town read as Ligurian): two low storeys of grey rubble
    stone under slate, small white sashes, a painted door under a stone lintel, quoins, a gable chimney. 'b' has a slate
    porch and three windows up; 'c' a bay and a dormer."""
    w,d,h=5.4,7.0,5.3
    box('Walls',(0,0,h/2),(w,d,h),'Tint_WallStone',.08,segments=3)
    box('Plinth',(0,0,.2),(w+.12,d+.12,.4),'StoneDark',.04)
    for sx in(-1,1):
        for k in range(7):box('Quoin',(sx*(w/2-.02),-d/2-.02,.45+k*.68),(.5 if k%2 else .34,.08,.3),'StoneDark',.03)
    slate_roof(0,0,w,d,h,2.3)
    box('Chimney',(w/2-.45,0,h+2.6),(.8,.9,1.9),'StoneDark',.05);box('Chimney cap',(w/2-.45,0,h+3.6),(1.0,1.1,.12),'StoneDark',.02)
    for k in range(2):cyl('Chimney pot',(w/2-.45,-.18+k*.36,h+3.8),.11,.38,'RoofTile','Z',12)
    dx=0 if kind=='b' else -1.35
    door(dx,-d/2,0,1.0,2.2,canopy=False)
    box('Door lintel',(dx,-d/2-.06,2.42),(1.5,.14,.26),'StoneDark',.03)
    ups=(-1.55,0,1.55) if kind=='b' else (-1.3,1.3)
    if kind=='b':
        for x in(-1.8,1.8):window(x,-d/2,1.45,.95,1.15,shutters=False,flowers=False)
        # Slate porch: two stone cheeks and a little slate roof.
        for sx in(-1,1):box('Porch cheek',(sx*.85,-d/2-.55,1.15),(.25,1.1,2.3),'Tint_WallStone',.04)
        for s in(-1,1):box('Porch slate',(s*.55,-d/2-.6,2.62),(1.2,1.3,.08),'RoofSlate',.02,rot=(0,s*.45,0))
    elif kind=='c':
        box('Bay',(1.3,-d/2-.4,1.3),(2.1,.8,2.2),'Tint_WallStone',.05);box('Bay roof',(1.3,-d/2-.45,2.5),(2.4,1.0,.12),'RoofSlate',.03,rot=(.15,0,0))
        for x in(.7,1.3,1.9):box('Bay glass',(x,-d/2-.82,1.4),(.48,.04,1.1),'Glass',.01)
        box('Dormer',(-1.2,-1.6,h+1.0),(1.4,1.3,1.4),'Tint_WallStone',.04);box('Dormer roof',(-1.2,-1.65,h+1.8),(1.7,1.6,.14),'RoofSlate',.03,rot=(.12,0,0))
        box('Dormer glass',(-1.2,-2.27,h+1.0),(.8,.04,.8),'Glass',.01)
    else:
        window(1.3,-d/2,1.45,1.0,1.2,shutters=False,flowers=False)
    for x in ups:window(x,-d/2,3.9,.9,1.05,shutters=False,flowers=(kind=='a'))
    for z in(1.45,3.9):window(-1.3,d/2,z,.85,1.05,shutters=False,flowers=False,face=1)
    door(1.3,d/2,0,.95,2.1,face=1,canopy=False)
    collision('Body',(0,0,h/2),(w+.3,d+.3,h))
def cottage_a():cottage('a')
def cottage_b():cottage('b')
def cottage_c():cottage('c')

def tyre(name,loc,r,width):
    cyl(name,loc,r,width,'Black','X',24,.08)
    cyl('Hub',(loc[0]+(width/2+.01)*(1 if loc[0]>0 else -1),loc[1],loc[2]),r*.45,.05,'Yellow','X',16)

def haul_truck():
    """Articulated quarry dump truck (~10 m): cab and engine at the front (-Y), the tipper body (Spin_X, hinged at the
    back so Unity can tip it), six big tyres, a ladder and a beacon."""
    global SPIN_HUB,SPIN_AXIS
    box('Front frame',(0,-2.6,1.35),(2.0,4.0,.6),'Metal',.06)
    box('Engine hood',(0,-3.4,2.15),(2.2,2.4,1.2),'Yellow',.12)
    box('Grille',(0,-4.62,2.05),(1.7,.08,.9),'Black',.02)
    box('Cab',(-.3,-1.85,3.05),(1.9,1.6,1.5),'Yellow',.1)
    box('Cab glass',(-.3,-2.66,3.15),(1.5,.04,.85),'Glass',.01)
    box('Cab side glass',(-1.27,-1.85,3.15),(.04,1.2,.85),'Glass',.01)
    box('Cab roof',(-.3,-1.85,3.85),(2.2,1.9,.12),'Yellow',.03)
    cyl('Beacon',(-.3,-1.7,4.02),.12,.2,'Autumn','Z',12)
    cyl('Exhaust',(.85,-2.4,3.2),.08,1.6,'Metal','Z',8)
    for k in range(4):box('Ladder rung',(1.15,-1.2,.9+k*.35),(.06,.5,.05),'Metal',.01)
    box('Hitch',(0,-.4,1.3),(1.0,1.0,.8),'Metal',.05)
    box('Rear frame',(0,2.2,1.3),(2.1,5.6,.5),'Metal',.06)
    for y in(-3.2,1.2,3.4):
        for sx in(-1,1):tyre('Tyre',(sx*1.35,y,1.0),1.0,.85)
    # Tipper body: a steel box with a raised front lip over the cab; hinged at the back axle.
    bin=[]
    bin.append(box('Tipper floor',(0,2.15,2.0),(2.9,5.6,.2),'Yellow',.04))
    for sx in(-1,1):bin.append(box('Tipper side',(sx*1.45,2.15,2.75),(.14,5.6,1.5),'Yellow',.04))
    bin.append(box('Tipper front',(0,-.65,3.0),(2.9,.16,2.0),'Yellow',.04))
    bin.append(box('Tipper canopy',(0,-.95,3.95),(2.9,.8,.12),'Yellow',.03,rot=(-.2,0,0)))
    for y in(.4,1.9,3.4):
        for sx in(-1,1):bin.append(box('Tipper rib',(sx*1.53,y,2.75),(.08,.18,1.5),'Metal',.02))
    bin.append(box('Load',(0,2.0,3.15),(2.6,5.0,.9),'SlateRock',.2))
    for k in range(5):bin.append(sphere('Load lump',(-.9+k*.45,1.0+(k%3)*1.1,3.6),(.45,.5,.35),'SlateRock',1))
    SPIN.extend(bin);SPIN_HUB=(0,4.9,1.9);SPIN_AXIS='X'
    collision('Truck',(0,0,2.0),(3.2,10.0,4.0))

def excavator_base():
    """Tracked excavator undercarriage: two crawler tracks and the slewing ring (the upper house is excavator_top)."""
    for sx in(-1,1):
        box('Track frame',(sx*1.35,0,.55),(.75,4.4,.8),'Metal',.1)
        box('Track',(sx*1.35,0,.55),(.85,4.7,1.0),'Black',.3)
        for k in range(5):cyl('Roller',(sx*1.8,-1.6+k*.8,.35),.22,.06,'Yellow','X',12)
    box('Car body',(0,0,1.05),(2.0,2.4,.5),'Metal',.06)
    cyl('Slew ring',(0,0,1.35),1.1,.2,'Black','Z',24)
    collision('Base',(0,0,.7),(3.4,4.8,1.4))

def excavator_top():
    """Excavator upper house (slews in Unity about its centre): cab, engine housing, counterweight; the boom, stick and
    bucket are one Spin_X part pivoted at the boom foot, so the arm can dip and lift."""
    global SPIN_HUB,SPIN_AXIS
    box('House',(.2,.6,2.15),(2.6,3.4,1.5),'Yellow',.12)
    box('Counterweight',(.2,2.35,1.95),(2.7,.7,1.1),'Metal',.1)
    box('Cab',(-.75,-.9,2.75),(1.0,1.4,1.8),'Yellow',.08)
    box('Cab glass',(-.75,-1.62,2.95),(.85,.04,1.2),'Glass',.01);box('Cab side',(-1.27,-.9,2.95),(.04,1.1,1.1),'Glass',.01)
    cyl('Exhaust',(.9,1.5,3.3),.08,1.0,'Metal','Z',8)
    arm=[]
    foot=Vector((.35,-1.2,2.2));elbow=Vector((.35,-4.6,5.0));tip=Vector((.35,-6.6,1.6))
    for a,b,t in((foot,elbow,.55),(elbow,tip,.42)):
        d=b-a;o=box('Boom',tuple((a+b)/2),(t,d.length,t*1.3),'Yellow',.06);o.rotation_mode='QUATERNION';o.rotation_quaternion=Vector((0,1,0)).rotation_difference(d.normalized());arm.append(o)
    arm.append(cyl('Ram',(.35,-2.7,2.9),.11,2.4,'MetalLight','Y',8))
    arm.append(box('Bucket back',(.35,-6.7,1.15),(1.3,.18,1.0),'Metal',.05,rot=(.4,0,0)))
    arm.append(box('Bucket floor',(.35,-7.15,.7),(1.3,.9,.14),'Metal',.04,rot=(-.3,0,0)))
    for sx in(-1,1):arm.append(box('Bucket cheek',(.35+sx*.62,-7.0,1.0),(.08,1.0,.9),'Metal',.03))
    for k in range(4):arm.append(box('Bucket tooth',(-.1+k*.3,-7.6,.5),(.1,.3,.1),'MetalLight',.02))
    SPIN.extend(arm);SPIN_HUB=tuple(foot);SPIN_AXIS='X'
    collision('House',(.2,.6,2.15),(2.8,3.6,1.8))

def wheel_loader():
    """Wheel loader: articulated body, cab, and the lift arms with the bucket as one Spin_X part pivoted at the front frame."""
    global SPIN_HUB,SPIN_AXIS
    box('Rear body',(0,1.4,1.6),(2.2,2.8,1.4),'Yellow',.12);box('Rear grille',(0,2.82,1.6),(1.6,.06,.9),'Black',.02)
    box('Front frame',(0,-1.2,1.3),(1.6,2.2,.7),'Yellow',.08)
    box('Cab',(0,.1,2.95),(1.6,1.5,1.5),'Yellow',.08);box('Cab glass',(0,-.66,3.05),(1.35,.04,1.0),'Glass',.01)
    for sx in(-1,1):box('Cab side glass',(sx*.81,.1,3.05),(.04,1.1,1.0),'Glass',.01)
    box('Cab roof',(0,.1,3.75),(1.8,1.7,.1),'Yellow',.03);cyl('Beacon',(0,.4,3.9),.11,.18,'Autumn','Z',12)
    cyl('Exhaust',(.6,1.9,2.8),.07,.9,'Metal','Z',8)
    for y in(-1.4,1.6):
        for sx in(-1,1):tyre('Tyre',(sx*1.2,y,.85),.85,.7)
    arm=[]
    for sx in(-1,1):arm.append(box('Lift arm',(sx*.7,-2.2,1.55),(.22,2.6,.32),'Yellow',.04,rot=(.25,0,0)))
    arm.append(box('Bucket back',(0,-3.55,1.05),(2.5,.16,1.1),'Metal',.04))
    arm.append(box('Bucket floor',(0,-4.0,.55),(2.5,.9,.12),'Metal',.03,rot=(-.15,0,0)))
    for sx in(-1,1):arm.append(box('Bucket cheek',(sx*1.22,-3.85,.95),(.08,.9,.9),'Metal',.03))
    arm.append(box('Bucket edge',(0,-4.45,.5),(2.5,.12,.08),'MetalLight',.02))
    SPIN.extend(arm);SPIN_HUB=(0,-1.0,1.85);SPIN_AXIS='X'
    collision('Loader',(0,-.4,1.8),(2.8,7.6,3.6))

def rock_mesh(name,loc,size,material,seed,layers=0,rough=.14):
    """A weathered rock: an ico sphere stretched to `size`, roughened with noise, flattened underneath; `layers` steps it
    into beds (split slate)."""
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=3,radius=1,location=(0,0,0));o=bpy.context.object
    s=Vector((seed*1.7+.3,seed*2.9+1.1,seed*.7+2.3))
    for v in o.data.vertices:
        c=v.co.copy();n0=v.normal.copy();c.x*=size[0]/2;c.y*=size[1]/2;c.z*=size[2]/2
        c+=n0*(noise.noise(c*.9+s)*rough+noise.noise(c*2.6+s)*rough*.35)*min(size)
        if layers>0:q=round(c.z/layers)*layers;c.z=c.z*.45+q*.55
        c.z=max(c.z,-size[2]*.3)
        v.co=c
    o.data.update();o.location=(loc[0],loc[1],loc[2]+size[2]*.3)
    return _finish(o,name,material,0)

def rock_stand_a():
    """Standing stone: a tall slate monolith (about 2.8 m) with a fallen slab and a stone at its foot."""
    rock_mesh('Monolith',(0,0,0),(1.1,.6,3.4),'SlateRock',1,.22,.1)
    rock_mesh('Fallen slab',(1.4,.5,0),(1.8,.9,.6),'SlateRock',2,.12,.1)
    rock_mesh('Stone',(-1.0,.7,0),(.7,.6,.5),'RockDark',3)
    collision('Monolith',(0,0,1.2),(1.1,.7,2.6))
def rock_stand_b():
    """A pair of slate slabs leaning together, like a tumbled gateway."""
    o=rock_mesh('Slab',(-.55,0,0),(.6,1.5,3.0),'SlateRock',4,.2,.08);o.rotation_euler=(0,.18,0)
    o=rock_mesh('Slab',(.65,.1,0),(.55,1.3,2.4),'SlateRock',5,.2,.08);o.rotation_euler=(0,-.15,.2)
    rock_mesh('Boulder',(0,1.2,0),(.9,.8,.6),'RockDark',6)
    collision('Slabs',(0,0,1.1),(2.0,1.6,2.2))
def rock_stand_c():
    """A big split boulder (about 3 m across) with a rubble skirt."""
    rock_mesh('Boulder half',(-.6,0,0),(1.8,2.4,2.2),'SlateRock',7,.35,.16)
    rock_mesh('Boulder half',(.75,.1,0),(1.4,2.2,1.9),'SlateRock',8,.35,.16)
    for k in range(6):a=k*1.05;rock_mesh('Rubble',(math.cos(a)*1.9,math.sin(a)*1.7,0),(.5,.45,.35),'RockDark',9+k)
    collision('Boulder',(0,0,.9),(3.2,2.6,1.8))
def rock_stand_d():
    """A low cluster of three rounded field boulders."""
    for k,(x,y,s) in enumerate(((0,0,1.6),(1.3,.4,1.0),(-1.0,.6,.8))):rock_mesh('Field boulder',(x,y,0),(s*1.2,s,s*.8),'RockDark' if k else 'Rock',20+k,0,.18)
    collision('Boulders',(0,0,.5),(3.2,2.0,1.0))

def slate_stack():
    """Split roofing slates stood on edge in wooden racks, and a sawn block (the quay and the quarry yard)."""
    for k in range(2):
        x=-.8+k*1.6
        box('Pallet',(x,0,.1),(1.4,1.1,.2),'Wood',.02)
        for i in range(14):box('Slate',(x-.6+i*.092,0,.55),(.05,1.0,.7),'RoofSlateDark' if i%3==0 else 'RoofSlate',.01)
        for sx in(-1,1):box('Rack post',(x+sx*.68,0,.45),(.06,1.12,.9),'WoodDark',.01)
    box('Block',(0,1.2,.45),(1.6,.9,.9),'SlateRock',.05)
    collision('Stack',(0,.3,.5),(3.2,2.0,1.0))

def tyre_wall():
    """4.8 m trackside tyre wall: two rows of stacked tyres, painted red/white every other stack."""
    for k in range(6):
        x=-2.0+k*.8
        for row,y in enumerate((-.2,.5)):
            for z in range(3 if row==0 else 2):
                cyl('Tyre',(x,y,.2+z*.36),.38,.34,'Red' if (k+z)%2==0 and row==0 else ('WhiteTrim' if row==0 else 'Black'),'Z',14,.06)
    box('Tyre strap',(0,-.6,.55),(4.8,.04,.18),'Blue',.02)
    collision('Wall',(0,.15,.55),(4.9,1.5,1.1))

def ad_board():
    """6 m trackside advertising board on two legs (SignAnchor for the brand)."""
    box('Board',(0,0,1.0),(6.0,.14,1.2),'Tint_Wall',.05)
    box('Board trim',(0,.02,1.0),(6.2,.1,1.36),'Tint_Shutter',.04)
    for x in(-2.6,2.6):box('Board leg',(x,.25,.45),(.12,.5,.9),'Metal',.02,rot=(.4,0,0))
    anchor('SignAnchor_board',(0,-.1,1.0))
    collision('Board',(0,0,.9),(6.2,.5,1.8))

# ---------------------------------------------------------------- valley nature

def tree_fir():
    """Mountain fir: stacked cones on a short trunk."""
    cyl('Fir trunk',(0,0,.7),.24,1.4,'Bark','Z',8,.04,r2=.18)
    for k,(z,r,h) in enumerate([(1.8,2.1,2.4),(3.3,1.7,2.2),(4.6,1.25,2.0),(5.8,.8,1.7)]):
        star_cone('Fir tier',z-h/2,r,h,'Fir' if k%2==0 else 'FirLight',9,k)
    collision('Trunk',(0,0,1.5),(.6,.6,3))

def tree_poplar():
    """Lombardy poplar: tall slim column for roadside avenues."""
    cyl('Poplar trunk',(0,0,.8),.2,1.6,'Bark','Z',8,.04,r2=.16)
    sphere('Poplar crown',(0,0,5.0),(1.05,1.05,4.2),'Poplar',2)
    sphere('Poplar crown light',(.25,-.2,6.4),(.7,.7,2.4),'LeafLight',2)
    collision('Trunk',(0,0,1.5),(.5,.5,3))

def tree_blossom():
    """Peach blossom tree: pink puffs on a twisty trunk (a nod to Peach Town)."""
    cyl('Blossom trunk',(0,0,.9),.24,1.8,'Bark','Z',8,.05,r2=.16)
    for side in(-1,1):cyl('Blossom limb',(side*.45,0,1.95),.1,1.1,'Bark','X',8)
    for i,(x,y,z,r) in enumerate([(0,0,2.8,1.5),(1.0,.3,2.5,1.0),(-1.0,-.2,2.55,1.05),(.3,.9,3.0,.9),(-.3,-.9,3.1,.9),(0,0,3.6,.9)]):
        sphere('Blossom',(x,y,z),(r,r,r*.85),'Blossom' if i%2 else 'BlossomDeep',2)
    satellites(1.35,2.3,3.5,['Blossom','BlossomDeep'],8,11)
    collision('Trunk',(0,0,1),(.6,.6,2))

def tree_birch():
    """Silver birch: white trunk with dark marks, airy light canopy."""
    cyl('Birch trunk',(0,0,2.4),.16,4.8,'BirchBark','Z',8,.03,r2=.1)
    for k in range(6):box('Birch mark',(0,-.15,.6+k*.7),(.14,.04,.07),'Black',.01,rot=(0,0,k*1.1))
    for i,(x,y,z,r) in enumerate([(0,0,4.6,1.3),(.7,.2,4.0,.9),(-.6,-.3,4.2,.9),(.1,.5,5.3,.8)]):sphere('Birch leaves',(x,y,z),(r,r,r*1.15),'LeafLight' if i%2 else 'Leaf',2)
    satellites(1.15,3.8,5.4,['LeafLight','Leaf'],8,17,(.35,.55))
    collision('Trunk',(0,0,1.5),(.4,.4,3))

def tree_autumn():
    """Early-turning maple: orange and red canopy for colour."""
    cyl('Maple trunk',(0,0,1.2),.26,2.4,'Bark','Z',10,.05,r2=.18)
    for i,(p,r) in enumerate([((0,0,3.2),1.8),((.9,.3,2.9),1.2),((-.8,-.4,3.0),1.25),((.2,-.7,4.0),1.1),((-.3,.6,4.1),1.0)]):
        sphere('Maple leaves',p,(r,r,r*.9),'Autumn' if i%2==0 else 'AutumnRed',2)
    satellites(1.55,2.6,4.2,['Autumn','AutumnRed'],9,13)
    collision('Trunk',(0,0,1.5),(.6,.6,3))

def rock_a():
    """Rounded boulder with a mossy cap."""
    rnd=random.Random(21)
    sphere('Boulder',(0,0,.7),(1.4,1.1,.95),'Rock',2)
    sphere('Boulder side',(.8,.3,.45),(.8,.7,.6),'RockDark',2)
    sphere('Boulder moss',(-.2,-.1,1.35),(.8,.6,.25),'Moss',1)
    collision('Boulder',(0,0,.7),(2.4,1.9,1.4))

def rock_b():
    """Flat-topped slab stack."""
    for k,(z,sx,sy,rz) in enumerate([(.35,2.2,1.6,.1),(.9,1.7,1.3,-.3),(1.35,1.1,.9,.5)]):
        box('Rock slab',(k*.15,0,z),(sx,sy,.55),'Rock' if k%2==0 else 'RockDark',.18,rot=(0,0,rz))
    collision('Slabs',(0,0,.8),(2.2,1.7,1.6))

def rock_cluster():
    """Scatter of small stones (non-solid dressing)."""
    rnd=random.Random(33)
    for k in range(7):
        x,y=rnd.uniform(-1.6,1.6),rnd.uniform(-1.2,1.2);r=rnd.uniform(.2,.5)
        sphere('Stone',(x,y,r*.5),(r*1.2,r,r*.8),'Rock' if k%3 else 'RockDark',1)

def reeds():
    """River reed clump with cattail heads."""
    rnd=random.Random(8)
    for k in range(14):
        x,y=rnd.uniform(-.6,.6),rnd.uniform(-.5,.5);h=rnd.uniform(1.0,1.8)
        box('Reed',(x,y,h/2),(.05,.05,h),'Reed',0,rot=(rnd.uniform(-.15,.15),rnd.uniform(-.15,.15),0))
        if k%3==0:cyl('Cattail',(x,y,h+.1),.06,.32,'ReedHead','Z',8,.02)

def grass_tuft():
    """Toy grass tuft: a fan of chunky blades."""
    for k in range(7):
        a=k*math.tau/7;h=.45+(k%3)*.12
        box('Blade',(math.cos(a)*.1,math.sin(a)*.1,h/2),(.08,.03,h),'LeafLight' if k%2 else 'Leaf',0,rot=(math.sin(a)*.35,-math.cos(a)*.35,a))

def flower_bush():
    """Round shrub dotted with blooms."""
    rnd=random.Random(12);col=['FlowerRed','FlowerYellow','FlowerPink','FlowerWhite','FlowerBlue']
    for k in range(4):sphere('Shrub',(rnd.uniform(-.4,.4),rnd.uniform(-.4,.4),.45),(.6,.6,.5),'Leaf' if k%2 else 'LeafDark',2)
    for k in range(14):
        a=rnd.uniform(0,math.tau);z=rnd.uniform(.35,.9);sphere('Bloom',(math.cos(a)*.62,math.sin(a)*.62,z),(.1,.1,.1),col[k%5],1)

# ---------------------------------------------------------------- Pit Stop (every town's services)

PIT_BAY=9.0
PIT_COLOUR={'garage':'PitGarage','parts':'PitParts','body':'PitBody'}
def torus(name,loc,R,r,material,axis='Y',major=40,minor=14):
    rot={'Z':(0,0,0),'X':(0,math.pi/2,0),'Y':(math.pi/2,0,0)}[axis]
    bpy.ops.mesh.primitive_torus_add(major_radius=R,minor_radius=r,major_segments=major,minor_segments=minor,location=loc,rotation=rot)
    return _finish(bpy.context.object,name,material,smooth=True)
def swept(name,points,x0,x1,thick,material):
    """A sheet swept along X through (y,z) profile points, `thick` below the profile."""
    v=[];f=[];n=len(points)
    for (y,z) in points:v+=[(x0,y,z),(x1,y,z),(x0,y,z-thick),(x1,y,z-thick)]
    for i in range(n-1):
        a,b=4*i,4*(i+1)
        f+=[(a,a+1,b+1,b),(a+2,b+2,b+3,a+3),(a,b,b+2,a+2),(a+1,a+3,b+3,b+1)]
    f+=[(0,2,3,1),(4*(n-1),4*(n-1)+1,4*(n-1)+3,4*(n-1)+2)]
    o=poly(name,v,f,material)
    bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(o.data);bm.free();o.data.update()
    return o
def pit_icon(kind,cx,y,z):
    """White pictogram on a bay badge (the badge faces -Y)."""
    if kind=='garage':
        box('Icon wrench handle',(cx,y,z),(.26,.08,1.35),'PitWhite',.05,rot=(0,math.radians(40),0))
        for s in(-1,1):box('Icon wrench jaw',(cx+.52+s*.2,y,z+.6+s*.05),(.2,.08,.42),'PitWhite',.04,rot=(0,math.radians(40),0))
        torus('Icon wrench ring',(cx-.47,y,z-.52),.2,.08,'PitWhite','Y',20,8)
    elif kind=='parts':
        cyl('Icon cog',(cx,y,z),.5,.08,'PitWhite','Y',28,.02)
        for k in range(8):
            a=k*math.tau/8;box('Icon cog tooth',(cx+math.cos(a)*.6,y,z+math.sin(a)*.6),(.24,.08,.24),'PitWhite',.03,rot=(0,-a,0))
        cyl('Icon cog hole',(cx,y-.03,z),.2,.06,PIT_COLOUR[kind],'Y',20,.01)
    else:
        box('Icon car body',(cx,y,z-.08),(1.45,.08,.38),'PitWhite',.1)
        box('Icon car cabin',(cx-.05,y,z+.24),(.78,.08,.34),'PitWhite',.1)
        for s in(-1,1):cyl('Icon car wheel',(cx+s*.45,y-.02,z-.3),.2,.08,'PitNavy','Y',18,.02)
def pit_interior(kind,cx,D,H):
    back=D/2-.6
    for x in(-2.4,2.4):box('Bay ceiling light',(cx+x*.5,0,H-.12),(2.6,.35,.08),'GlassLit',.02)
    if kind=='garage':
        for s in(-1,1):
            box('Lift post',(cx+s*2.7,.6,2.1),(.42,.42,4.2),'PitNavy',.06);collision('Lift post',(cx+s*2.7,.6,2.1),(.45,.45,4.2))
            box('Lift runner',(cx+s*.85,.6,1.5),(.55,5.2,.14),'PitParts',.04)
        box('Lift cross beam',(cx,.6,4.0),(5.8,.35,.3),'PitNavy',.05)
        box('Tool board',(cx,back,2.5),(5.2,.1,2.2),'PitNavy',.04)
        for k in range(7):box('Tool',(cx-2.1+k*.7,back-.08,2.4+(k%2)*.35),(.14,.06,.8+(k%3)*.2),'MetalLight',.03)
        box('Tool chest',(cx+3.2,back-.5,.55),(1.2,.7,1.1),'Red',.06);collision('Chest',(cx+3.2,back-.5,.55),(1.2,.7,1.1))
    elif kind=='parts':
        for k,x in enumerate((-2.7,0,2.7)):
            for s in(-1,1):box('Rack upright',(cx+x+s*1.15,back-.3,1.7),(.1,.6,3.4),'PitNavy',.02)
            for z in(.5,1.6,2.7):
                box('Rack shelf',(cx+x,back-.3,z),(2.4,.65,.07),'MetalLight',.01)
                for t in range(3):
                    if (k+t)%2==0:cyl('Shelf tyre',(cx+x-.75+t*.75,back-.3,z+.3),.28,.24,'Black','X',14,.05)
                    else:box('Parts box',(cx+x-.75+t*.75,back-.3,z+.22),(.55,.45,.42),'PitParts' if t==1 else 'PitOrange',.04)
            collision('Rack',(cx+x,back-.3,1.7),(2.5,.7,3.4))
        box('Engine stand',(cx,.4,.5),(1.1,.8,1.0),'PitNavy',.05);box('Engine block',(cx,.4,1.3),(1.2,.9,.7),'Metal',.08)
        for s in(-1,1):box('Cylinder head',(cx+s*.35,.4,1.75),(.4,.85,.22),'MetalLight',.04)
        cyl('Air filter',(cx,.4,2.0),.35,.18,'Red','Z',20,.03);collision('Engine',(cx,.4,.9),(1.3,1.0,1.8))
        box('Parts counter',(cx-2.6,-D/2+2.6,.55),(2.6,.8,1.1),'PitWhite',.05);box('Counter top',(cx-2.6,-D/2+2.6,1.13),(2.8,.95,.08),'PitParts',.02)
        collision('Counter',(cx-2.6,-D/2+2.6,.55),(2.6,.8,1.1))
    else:
        cyl('Turntable',(cx,.4,.07),2.7,.14,'PitNavy','Z',40,.04)
        cyl('Turntable ring',(cx,.4,.15),2.75,.04,'PitBody','Z',40,.01)
        # Show car: a chunky body-shop hero on the turntable.
        box('Show car body',(cx,.4,.75),(1.9,3.6,.7),'PitBody',.28);box('Show car cabin',(cx,.55,1.35),(1.6,1.9,.6),'Glass',.22)
        box('Show car roof',(cx,.55,1.68),(1.5,1.6,.1),'PitBody',.05)
        for sx in(-1,1):
            for sy in(-1,1):cyl('Show car wheel',(cx+sx*.95,.4+sy*1.15,.42),.38,.3,'Black','X',18,.06)
        box('Spray booth frame',(cx,back-.8,2.4),(6.6,.25,.25),'MetalLight',.03)
        for s in(-1,1):box('Spray booth post',(cx+s*3.2,back-.8,1.2),(.2,.2,2.4),'MetalLight',.02)
        for k in range(6):cyl('Paint can',(cx-3.4+k*.3,back-.3,1.35),.12,.3,['Red','PitBody','Blue','PitParts','PitGarage','PitOrange'][k],'Z',12,.02)
        box('Paint shelf',(cx-2.65,back-.3,1.18),(1.9,.4,.06),'Wood',.01)
        collision('Show car',(cx,.4,.8),(2.0,3.7,1.6))
def pit_stop(kinds=('parts','garage','body'),pylon=-1):
    """Pit Stop: the uniform services base in every town (bays: garage = local races, fitting and
    body swaps; parts = performance parts; body = new models and paint). One silhouette from any
    distance: a swept orange wing roof, a chequered band, colour-coded bays with round icon
    badges, and a roadside pylon topped by a giant spinning tyre. Frontage -Y (Unity +Z).
    `pylon` is the Blender X side for the pylon; Unity mirrors X on import (-1 lands on Unity +X)."""
    n=len(kinds);W=n*PIT_BAY+1.6;D=12.0;H=5.4
    box('Back wall',(0,D/2-.2,H/2),(W,.4,H),'PitWhite',.08);collision('Back wall',(0,D/2-.2,H/2),(W,.4,H))
    box('Ceiling slab',(0,0,H+.1),(W,D,.2),'PitWhite',.05)
    for s in(-1,1):
        box('End wall',(s*(W/2-.4),0,H/2),(.8,D,H),'PitWhite',.08);collision('End wall',(s*(W/2-.4),0,H/2),(.8,D,H))
        box('End wall base band',(s*(W/2+.02),0,.35),(.06,D,.7),'PitOrange',.02)
        box('End wall stripe',(s*(W/2+.02),0,H-.9),(.06,D,.35),'PitOrange',.02)
    box('Back base band',(0,D/2+.02,.35),(W,.06,.7),'PitOrange',.02)
    centres=[-W/2+.8+PIT_BAY*(i+.5) for i in range(n)]
    for i in range(1,n):
        x=-W/2+.8+PIT_BAY*i;box('Bay divider',(x,0,H/2),(.8,D,H),'PitWhite',.08);collision('Divider',(x,0,H/2),(.8,D,H))
    box('Lintel',(0,-D/2+.25,H-.5),(W,.5,1.0),'PitWhite',.06)
    # Chequered band across the whole front.
    sq=.4;cols=int(W/sq)
    for r in range(2):
        for c in range(cols):box('Chequer',(-W/2+sq*(c+.5),-D/2-.03,H+.2+sq*(r+.5)),(sq,.06,sq),'Black' if (c+r)%2==0 else 'PitWhite',.0)
    box('Parapet',(0,-D/2+.2,H+.6),(W,.4,.8),'PitWhite',.04)
    for i,(kind,cx) in enumerate(zip(kinds,centres)):
        colour=PIT_COLOUR[kind]
        box('Bay floor',(cx,0,.02),(PIT_BAY-.8,D-.4,.04),'StoneDark',.01)
        for s in(-1,1):box('Door frame',(cx+s*3.95,-D/2-.1,2.25),(.36,.34,4.5),colour,.06)
        box('Door head',(cx,-D/2-.1,4.55),(8.26,.34,.34),colour,.06)
        cyl('Rolled door',(cx,-D/2+.55,4.2),.3,7.4,colour,'X',16,.04)
        box('Bay sign board',(cx,-D/2-.06,4.98),(5.8,.12,.72),'PitWhite',.05)
        anchor('SignAnchor_'+kind,(cx,-D/2-.2,4.98))
        for s in(-1,1):box('Bay approach line',(cx+s*3.6,-D/2-3,.025),(.18,6,.02),colour,.005)
        box('Bay threshold',(cx,-D/2-.4,.025),(7.4,.5,.02),colour,.005)
        # Hosts park at the bay's edge ahead of the columns, clear of the drive-in lane (+-1.25 m).
        anchor('HostAnchor_'+kind,(cx+(-3.0 if kind!='parts' else 3.0),-D/2-5.4,0))
        pit_interior(kind,cx,D,H)
    # The wing: an orange roof sweeping up over the forecourt, white lip, on navy columns.
    back_y,front_y=D/2+.8,-D/2-5.5
    pts=[]
    for k in range(16):
        t=k/15;y=back_y+(front_y-back_y)*t;z=H+.85+2.1*t*t+.25*math.sin(t*math.pi)
        pts.append((y,z))
    swept('Wing roof',pts,-W/2-1.2,W/2+1.2,.38,'PitOrange')
    # White soffit: the wing's underside stays bright over the forecourt instead of shading brown.
    swept('Wing soffit',[(y,z-.385) for (y,z) in pts],-W/2-1.15,W/2+1.15,.03,'PitWhite')
    fy,fz=pts[-1];box('Wing lip',(0,fy-.05,fz-.2),(W+2.5,.18,.5),'PitWhite',.06)
    # Racing stripes over the wing, and each bay's round badge hung under its front edge.
    for sx in(-.9,.9):
        for a,b in zip(pts,pts[1:]):
            my,mz=(a[0]+b[0])/2,(a[1]+b[1])/2;ln=math.hypot(b[0]-a[0],b[1]-a[1])
            box('Wing stripe',(sx,my,mz+.01),(.9,ln+.02,.03),'PitWhite',.0,rot=(math.atan2(b[1]-a[1],b[0]-a[0])-math.pi if b[0]<a[0] else math.atan2(b[1]-a[1],b[0]-a[0]),0,0))
    by=-D/2-3.6;bz=[z for (y,z) in pts if y<=by][0]-.38
    for kind,cx in zip(kinds,centres):
        colour=PIT_COLOUR[kind];cz=bz-1.85
        for s in(-1,1):box('Badge hanger',(cx+s*.9,by+.1,(bz+cz+1.2)/2),(.08,.08,bz-cz-1.2),'PitNavy',.01)
        cyl('Badge rim',(cx,by+.12,cz),1.5,.22,'PitWhite','Y',36,.05)
        cyl('Badge',(cx,by-.04,cz),1.28,.14,colour,'Y',36,.04)
        pit_icon(kind,cx,by-.16,cz)
    for k in range(int(W/1.2)):
        x=-W/2+.6+k*1.2;box('Wing light',(x,fy+1.2,fz-.5),(.5,.5,.05),'GlassLit',.01)
    for x in [-W/2+.4]+[-W/2+.8+PIT_BAY*i for i in range(1,n)]+[W/2-.4]:
        colz=pts[-3][1]-.4;cy=pts[-3][0]
        cyl('Wing column',(x,cy,colz/2),.24,colz,'PitNavy','Z',14,.04);collision('Column',(x,cy,colz/2),(.5,.5,colz))
        for y in(D/2-1.0,0):box('Wing post',(x,y,H+.55),(.3,.3,.7),'PitNavy',.03)
    # Pennants along the lip in the bay colours.
    for k in range(int((W+2)/0.7)):
        x=-W/2-1+.35+k*.7;box('Pennant',(x,fy-.12,fz-.62),(.32,.03,.32),[PIT_COLOUR[c] for c in kinds][k%n],.0,rot=(0,math.radians(45),0))
    # Roadside pylon: legs, brand board and a giant spinning tyre.
    px,py=pylon*(W/2+4.2),-D/2-6.5
    box('Pylon plinth',(px,py,.3),(3.6,1.6,.6),'StoneDark',.06)
    for s in(-1,1):box('Pylon leg',(px+s*1.15,py,5.6),(.5,.5,10.6),'PitOrange',.08)
    box('Pylon board',(px,py,8.6),(3.6,.34,2.6),'PitNavy',.1);box('Pylon board rim',(px,py+.04,8.6),(3.9,.3,2.9),'PitWhite',.1)
    anchor('SignAnchor_brand',(px,py-.25,8.6))
    for r in range(2):
        for c in range(9):box('Pylon chequer',(px-1.6+c*.4,py-.2,6.9+r*.4),(.4,.06,.4),'Black' if (c+r)%2==0 else 'PitWhite',.0)
    global SPIN_HUB,SPIN_AXIS
    hub=(px,py,12.4);SPIN_HUB=hub;SPIN_AXIS='Y'
    parts=[torus('Sign tyre',hub,1.7,.62,'Black','Y',44,16)]
    for k in range(28):
        a=k*math.tau/28;parts.append(box('Tread block',(px+math.cos(a)*2.3,py,12.4+math.sin(a)*2.3),(.34,.9,.18),'Black',.04,rot=(0,-a,0)))
    parts.append(cyl('Sign rim',hub,1.25,.5,'PitOrange','Y',36,.05))
    for k in range(5):
        a=k*math.tau/5;parts.append(box('Rim spoke',(px+math.cos(a)*.62,py-.28,12.4+math.sin(a)*.62),(1.1,.12,.26),'PitWhite',.05,rot=(0,-a,0)))
    parts.append(cyl('Rim cap',(px,py-.32,12.4),.3,.14,'PitWhite','Y',20,.03))
    SPIN.extend(parts)
    collision('Pylon',(px,py,3),(3.6,1.6,6))
    # Forecourt kit: tyre stacks, oil drums, air post, chequered flag.
    tx=-pylon*(W/2+2.0)
    for k in range(4):cyl('Forecourt tyres',(tx,-D/2-1.2,.2+k*.36),.42,.34,'Black' if k%2 else 'PitOrange','Z',16,.06)
    collision('Tyres',(tx,-D/2-1.2,.75),(1,1,1.5))
    for k in range(3):cyl('Oil drum',(-pylon*(W/2+1.6)+k*.0,D/2-1.5-k*1.0,.5),.34,1.0,'PitOrange' if k!=1 else 'PitNavy','Z',16,.04)
    collision('Drums',(-pylon*(W/2+1.6),D/2-2.5,.5),(.8,3,1))
    box('Air post',(-pylon*(W/2+1.0),-D/2-4.5,.8),(.35,.35,1.6),'PitGarage',.06);box('Air post head',(-pylon*(W/2+1.0),-D/2-4.65,1.45),(.5,.1,.35),'PitWhite',.03)
    collision('Air post',(-pylon*(W/2+1.0),-D/2-4.5,.8),(.4,.4,1.6))

def pit_garages():
    """Raceway pit row, 44 m: four team garages (open roller doors, a team car or tyre racks inside)
    under a glazed hospitality deck with a chequered roof fascia, a race control tower with a
    wrap-around glass cab at +X and a timing pylon at -X. Frontage (the pit lane) -Y."""
    B,D,H,n=8.5,12.0,5.0,4;x0=-21.0;x1=x0+n*B;mid=(x0+x1)/2;front=-D/2
    teams=['Red','Blue','Green','Yellow']
    box('Pit plinth',(0,0,-.6),(44,D+.4,1.4),'StoneDark',.04)
    box('Pit apron',(0,front-2.0,.02),(46,4.0,.04),'Tarmac',.01)
    box('Pit lane line',(0,front-3.8,.05),(46,.18,.02),'WhiteTrim',.005)
    for k in range(n+1):
        x=x0+k*B;box('Garage pier',(x,0,H/2),(.7,D,H),'Tint_Wall',.06);collision('Pier',(x,0,H/2),(.7,D,H))
    box('Garage back',(mid,D/2-.2,H/2),(n*B,.4,H),'Tint_Wall',.06);collision('Garage back',(mid,D/2-.2,H/2),(n*B,.4,H))
    for k in range(n):
        cx=x0+B/2+k*B;team=teams[k]
        box('Garage floor',(cx,0,.03),(B-.7,D-.4,.05),'PitWhite',.01)
        box('Team fascia',(cx,front+.3,H-.45),(B-.7,.6,.9),team,.04)
        sign_board(cx,front+.05,H-.45,B-1.9,.72,'WhiteTrim','bay%d'%(k+1))
        cyl('Roller door',(cx,front+.75,H-1.15),.32,B-.8,'MetalLight','X',16)
        box('Box line',(cx-B/2+.6,front-2.0,.05),(.14,3.6,.02),'Yellow',.005);box('Box line',(cx+B/2-.6,front-2.0,.05),(.14,3.6,.02),'Yellow',.005)
        box('Box stop',(cx,front-.5,.05),(B-1.4,.25,.02),team,.005)
        box('Team banner',(cx,D/2-.45,3.0),(B-2.2,.08,2.2),team,.02)
        for x in(-2,2):box('Garage light',(cx+x*.6,0,H-.1),(2.4,.3,.08),'GlassLit',.02)
        if k%2==0:
            # Team car on stands, nose to the pit lane.
            box('Team car body',(cx,-.6,.85),(1.9,4.2,.6),team,.26);box('Team car cabin',(cx,-.3,1.4),(1.4,1.7,.5),'Glass',.2)
            box('Team car wing',(cx,1.55,1.45),(2.0,.45,.08),'Black',.03)
            for s in(-1,1):box('Wing post',(cx+s*.5,1.55,1.25),(.08,.2,.35),'Black',.02)
            for sx in(-1,1):
                for sy in(-1,1):cyl('Team car wheel',(cx+sx*1.0,-.6+sy*1.35,.62),.4,.34,'Black','X',18,.06)
            for sy in(-1,1):box('Car stand',(cx,-.6+sy*1.35,.3),(1.6,.3,.5),'MetalLight',.03)
            collision('Team car',(cx,-.6,.9),(2.2,4.4,1.6))
        else:
            for r in range(3):
                for t in range(4):cyl('Stacked tyre',(cx-2.6+r*.9,D/2-1.4,.22+t*.38),.36,.34,'Black','Z',16,.05)
                cyl('Tyre cover',(cx-2.6+r*.9,D/2-1.4,1.72),.37,.06,team,'Z',16,.02)
            collision('Tyres',(cx-1.7,D/2-1.4,.8),(2.8,.9,1.6))
            box('Pit cart',(cx+2.2,D/2-1.6,.6),(1.6,.8,1.0),team,.06);collision('Cart',(cx+2.2,D/2-1.6,.6),(1.6,.8,1.0))
        box('Tool chest',(cx+B/2-1.2,D/2-1.0,.55),(1.1,.6,1.1),'PitNavy',.05)
    # Hospitality deck: slab, glass balcony, glazed lounge, cantilevered roof with a chequered fascia.
    box('Deck slab',(mid,0,H+.15),(n*B+.7,D+.4,.3),'WhiteTrim',.05)
    box('Balcony glass',(mid,front-.1,H+.8),(n*B+.6,.06,.9),'Glass',.01)
    box('Balcony rail',(mid,front-.1,H+1.3),(n*B+.6,.1,.1),'MetalLight',.02)
    for k in range(18):box('Rail post',(x0+.1+k*2.0,front-.1,H+.8),(.08,.08,1.0),'MetalLight',.01)
    box('Lounge glass',(mid,front+2.2,H+1.9),(n*B,.12,3.2),'Glass',.02)
    for k in range(21):box('Lounge mullion',(x0+k*1.7,front+2.12,H+1.9),(.12,.14,3.2),'WhiteTrim',.02)
    box('Lounge back',(mid,D/2-.3,H+1.9),(n*B,.4,3.2),'Tint_Wall',.05)
    for x in(x0,x1):box('Lounge end',(x,(front+2.2+D/2)/2,H+1.9),(.4,D/2-front-2.2,3.2),'Tint_Wall',.05)
    box('Pit roof',(mid,-.6,H+3.7),(n*B+1.2,D+1.6,.4),'WhiteTrim',.06)
    sq=.5;cols=int((n*B+1.2)/sq)
    for r in range(2):
        for c in range(cols):box('Chequer',(x0-.6+sq*(c+.5),front-1.43,H+3.45+sq*(r+.5)),(sq,.06,sq),'Black' if (c+r)%2==0 else 'PitWhite',.0)
    for k in range(4):box('Roof unit',(x0+5+k*8.5,2.5,H+4.3),(2.2,1.6,.8),'MetalLight',.05)
    # Race control tower.
    tx=x1+4.2
    box('Tower core',(tx,0,6.1),(6.4,7.4,12.2),'Tint_Wall',.08);collision('Tower',(tx,0,6.1),(6.4,7.4,12.2))
    for z in(2.4,5.6,8.8):
        box('Tower windows',(tx,-3.72,z),(5.2,.1,1.4),'Glass',.02)
        box('Tower windows',(tx+3.22,0,z),(.1,6.0,1.4),'Glass',.02)
    for s in(-1,1):box('Tower stripe',(tx+s*2.9,-3.76,6.1),(.34,.12,12.2),'Tint_Shutter',.02)
    box('Tower door',(tx-1.5,-3.72,1.2),(1.4,.1,2.4),'PitNavy',.03)
    box('Cab floor',(tx,0,12.35),(9.4,9.4,.3),'WhiteTrim',.05)
    box('Cab glass',(tx,0,13.9),(8.8,8.8,2.8),'Glass',.03)
    for sx in(-1,1):
        for sy in(-1,1):box('Cab corner',(tx+sx*4.35,sy*4.35,13.9),(.3,.3,2.8),'WhiteTrim',.03)
    for k in(-1,0,1):box('Cab mullion',(tx+k*2.2,-4.42,13.9),(.14,.1,2.8),'WhiteTrim',.02)
    box('Cab roof',(tx,0,15.5),(10.2,10.2,.4),'RoofSlate',.08)
    box('Cab sill',(tx,0,12.62),(9.0,9.0,.24),'Tint_Shutter',.03)
    sign_board(tx,-4.6,16.6,7.6,1.4,'Yellow','control')
    cyl('Mast',(tx+3.4,3.0,18.2),.07,5.0,'MetalLight','Z',8);cyl('Mast light',(tx+3.4,3.0,20.8),.16,.3,'Red','Z',10)
    for sy in(-1,1):box('Dish',(tx-3.2,2.6+sy*.9,16.1),(.9,.2,.9),'PitWhite',.2)
    # Timing pylon at the other end: six team-coloured position plates under a yellow cap.
    px=x0-2.4
    box('Timing pylon',(px,front+1.8,6.6),(1.8,1.2,13.2),'PitNavy',.08);collision('Pylon',(px,front+1.8,6.6),(1.8,1.2,13.2))
    for r in range(6):
        box('Position plate',(px,front+1.16,11.4-r*1.55),(1.4,.08,1.15),teams[r%4],.03)
        box('Position digit',(px-.3,front+1.1,11.4-r*1.55),(.4,.06,.75),'PitWhite',.02)
    box('Timing cap',(px,front+1.8,13.5),(2.4,1.6,.6),'Yellow',.08)
    # Paddock side (+Y): team-coloured transporter shutters, crew doors, a lit lounge window band,
    # a blue service band and the paddock name, so the car-park view is not a blank wall.
    rear=D/2+.03
    for k in range(n):
        cx=x0+B/2+k*B;team=teams[k]
        box('Rear shutter',(cx-.9,rear,1.8),(4.6,.08,3.6),team,.02)
        for z in range(1,9):box('Shutter slat',(cx-.9,rear+.05,z*.42),(4.5,.04,.05),'Black',.005)
        box('Shutter head',(cx-.9,rear+.1,3.75),(5.0,.2,.3),'MetalLight',.02)
        box('Crew door',(cx+2.6,rear,1.1),(1.1,.08,2.2),'PitNavy',.02)
        box('Crew window',(cx+2.6,rear,3.1),(1.1,.06,.8),'GlassLit',.01)
        box('Team number plate',(cx+2.6,rear+.02,4.3),(1.3,.06,.5),team,.02)
    box('Rear band',(mid,rear+.02,H-.1),(n*B+.7,.1,.5),'Tint_Shutter',.02)
    box('Rear lounge windows',(mid,D/2-.07,H+1.9),(n*B-1.2,.06,1.6),'GlassLit',.01)
    for k in range(18):box('Rear mullion',(x0+1.0+k*1.9,D/2-.03,H+1.9),(.12,.06,1.6),'WhiteTrim',.01)
    sign_board(mid,D/2-.1,H+3.1,11,1.0,'Yellow','paddockback',face=1)
    for z in(2.4,5.6,8.8):box('Tower rear windows',(tx,3.72,z),(5.2,.1,1.4),'Glass',.02)
    box('Tower rear door',(tx+1.4,3.72,1.2),(1.4,.1,2.4),'PitNavy',.03)

def pit_stop_2():pit_stop(('garage','parts'),1)
def pit_stop_3():pit_stop(('parts','garage','body'))

ASSETS={'house_a':house_a,'house_b':house_b,'shop':shop,'cafe':cafe,'post_office':post_office,
    'lamp_post':lamp_post,'bench':bench,'fountain':fountain,'tree_round':tree_round,'tree_cypress':tree_cypress,
    'tree_pine':tree_pine,'palm':palm,'bush':bush,
    'sea_wall':sea_wall,'promenade_rail':promenade_rail,'harbour_steps':harbour_steps,'pier':pier,'boat':boat,'beach_hut':beach_hut,'parasol':parasol,
    'lighthouse':lighthouse,'windmill':windmill,
    'barn':barn,'fence':fence,'hay_bale':hay_bale,'tractor':tractor,'tent':tent,'campfire':campfire,'caravan':caravan,
    'telescope':telescope,'kiosk':kiosk,'flower_bed':flower_bed,'picnic_table':picnic_table,'chevron_sign':chevron_sign,'telegraph_pole':telegraph_pole,
    'church':church,'market_stall':market_stall,'town_gate':town_gate,'vine_row':vine_row,'motors':motors,
    'tunnel_portal':tunnel_portal,'headland_gate':headland_gate,'rail_gate':rail_gate,'town_hall':town_hall,'terrace_a':terrace_a,'terrace_b':terrace_b,'terrace_c':terrace_c,'terrace_d':terrace_d,'terrace_e':terrace_e,'terrace_f':terrace_f,'terrace_g':terrace_g,'house_c':house_c,'house_d':house_d,'castle_tower':castle_tower,'castle_wall':castle_wall,
    'castle_gatehouse':castle_gatehouse,'castle_keep':castle_keep,'castle_donjon':castle_donjon,'castle_palace':castle_palace,'zip_tower':zip_tower,'zip_station':zip_station,'zip_trolley':zip_trolley,'station':station,'mill':mill,'bandstand':bandstand,'wind_turbine':wind_turbine,
    'balloon':balloon,'grandstand':grandstand,'train_loco':train_loco,'train_coach':train_coach,'olive_tree':olive_tree,'lavender_row':lavender_row,
    'sunflower_row':sunflower_row,'hotel':hotel,'map_board':map_board,'flag_pole':flag_pole,'flag_wales':flag_wales,'cottage_a':cottage_a,'cottage_b':cottage_b,'cottage_c':cottage_c,'haul_truck':haul_truck,'excavator_base':excavator_base,'excavator_top':excavator_top,'wheel_loader':wheel_loader,'rock_stand_a':rock_stand_a,'rock_stand_b':rock_stand_b,'rock_stand_c':rock_stand_c,'rock_stand_d':rock_stand_d,'slate_stack':slate_stack,'tyre_wall':tyre_wall,'ad_board':ad_board,
    'tree_fir':tree_fir,'tree_poplar':tree_poplar,'tree_blossom':tree_blossom,'tree_birch':tree_birch,'tree_autumn':tree_autumn,
    'rock_a':rock_a,'rock_b':rock_b,'rock_cluster':rock_cluster,'reeds':reeds,'grass_tuft':grass_tuft,'flower_bush':flower_bush,'pit_stop_2':pit_stop_2,'pit_stop_3':pit_stop_3,'pit_garages':pit_garages}
ONLY=[a for a in args if a in ASSETS] or list(ASSETS)

# ---------------------------------------------------------------- build / export / render

def reset():
    global SPIN_HUB,SPIN_AXIS
    bpy.ops.wm.read_factory_settings(use_empty=True);PARTS.clear();SPIN.clear();SPIN_HUB=None;SPIN_AXIS='Y'

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
        spin=join_group(list(SPIN),'Spin_'+SPIN_AXIS)
        hub=Vector(SPIN_HUB) if SPIN_HUB else Vector((0,-2.8,9.6))
        spin.data.transform(Matrix.Translation(-(hub-spin.location)));spin.location=hub
    bpy.ops.object.select_all(action='DESELECT')
    for o in PARTS:o.select_set(True)
    bpy.context.view_layer.objects.active=PARTS[0]
    bpy.ops.object.convert(target='MESH')
    # Parts share material datablocks, so the join already yields one slot per colour.
    bpy.ops.object.join();v=bpy.context.object;v.name='Visual'
    assert len({m.name for m in v.data.materials})==len(v.data.materials),'duplicate material slots'
    return v

# Fabric that moves in the wind (Kit.shader KIT_FLAG). Flags fly from a pole along +X; banners hang.
FLAGS_FLYING=('Pennant','Flag','Flag stripe','Great flag','Great flag stripe')
FLAGS_HANGING=('Banner','Banner stripe','Banner chevron','Banner tail','Wall banner','Donjon banner','Banner crown band','Banner crown point','Zip banner','Rear banner','Team banner')
def flag_freedom():
    """Writes a 'Freedom' point attribute on flag parts (0 where the cloth is held, 1 at its free edge),
    shared across the pieces of one flag (stripe, chevron, tail), so they move together. Only parts
    thin along Y (facing +-Y) count: those are the ones the shader waves along that axis."""
    flags=[o for o in PARTS if o.type=='MESH' and (o.name.split('.')[0] in FLAGS_FLYING or o.name.split('.')[0] in FLAGS_HANGING)]
    if not flags:return
    for o in flags:
        bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o;bpy.ops.object.convert(target='MESH')
    def box(o):
        ws=[o.matrix_world@v.co for v in o.data.vertices];return (Vector((min(p.x for p in ws),min(p.y for p in ws),min(p.z for p in ws))),Vector((max(p.x for p in ws),max(p.y for p in ws),max(p.z for p in ws))))
    boxes={o:box(o) for o in flags}
    flags=[o for o in flags if (boxes[o][1]-boxes[o][0]).y<.2]
    # Group pieces whose boxes touch (a flag and its stripe, a banner and its chevrons and tail).
    groups=[]
    for o in flags:
        lo,hi=boxes[o];hit=[g for g in groups if any(lo.x<=boxes[p][1].x+.1 and hi.x>=boxes[p][0].x-.1 and lo.y<=boxes[p][1].y+.1 and hi.y>=boxes[p][0].y-.1 and lo.z<=boxes[p][1].z+.1 and hi.z>=boxes[p][0].z-.1 for p in g)]
        merged=[o]+[p for g in hit for p in g];groups=[g for g in groups if g not in hit]+[merged]
    for g in groups:
        lo=Vector((min(boxes[p][0].x for p in g),min(boxes[p][0].y for p in g),min(boxes[p][0].z for p in g)));hi=Vector((max(boxes[p][1].x for p in g),max(boxes[p][1].y for p in g),max(boxes[p][1].z for p in g)))
        flying=any(p.name.split('.')[0] in FLAGS_FLYING for p in g)
        for p in g:
            a=p.data.attributes.get('Freedom') or p.data.attributes.new('Freedom','FLOAT','POINT')
            for i,v in enumerate(p.data.vertices):
                w=p.matrix_world@v.co
                f=(w.x-lo.x)/max(hi.x-lo.x,.01) if flying else (hi.z-w.z)/max(hi.z-lo.z,.01)
                a.data[i].value=max(.001,min(1,f))
    print('FLAGS',len(groups))

def kit_surface(o):
    """KitSurface (u along the face, v up it) and KitHeight (height above ground) UV layers, read
    by the LittleRoad/Kit shader for stone courses, tiles, grain and base weathering."""
    me=o.data;mw=o.matrix_world;rot=mw.to_3x3()
    surf=me.uv_layers.new(name='KitSurface');high=me.uv_layers.new(name='KitHeight');free=me.attributes.get('Freedom')
    for poly in me.polygons:
        n=(rot@poly.normal).normalized()
        t=Vector((-n.y,n.x,0));t=t.normalized() if t.length>1e-5 else Vector((1,0,0))
        for li in poly.loop_indices:
            co=mw@me.vertices[me.loops[li].vertex_index].co
            surf.data[li].uv=(co.x,co.y) if abs(n.z)>.7 else (co.x*t.x+co.y*t.y,co.z)
            high.data[li].uv=(co.z,free.data[me.loops[li].vertex_index].value if free else 0)

def build(name):
    reset();random.seed(name);ASSETS[name]()
    flag_freedom()
    v=join_visual(name)
    for o in list(bpy.data.objects):
        if o.type=='MESH' and (o.name=='Visual' or o.name.startswith('Spin_')):kit_surface(o)
    tris=sum(len(p.vertices)-2 for p in v.data.polygons)
    SOURCE.mkdir(parents=True,exist_ok=True);bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/(name+'.blend')))
    if EXPORT:
        TARGET.mkdir(parents=True,exist_ok=True)
        bpy.ops.export_scene.fbx(filepath=str(TARGET/(name+'.fbx')),use_selection=False,object_types={'MESH','EMPTY'},
            apply_unit_scale=True,bake_space_transform=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,use_mesh_modifiers=True,bake_anim=False)
    print(f'BAYVIEW_KIT {name} tris={tris}')
    return v

def render_lineup(names,path):
    reset();x=0;spacing={'house_a':12,'house_b':10,'shop':12,'cafe':14,'post_office':12,'fountain':8,'lighthouse':14,'windmill':12,'boat':8,'beach_hut':5,'parasol':5,'pier':6,'barn':11,'caravan':7,'tractor':5,'kiosk':5,'flower_bed':6,'church':16,'market_stall':5,'town_gate':17,'vine_row':14,'motors':17,'tunnel_portal':44,'headland_gate':56,'rail_gate':32,'town_hall':26,'terrace_a':7,'terrace_b':7,'terrace_c':7,'terrace_d':7,'terrace_e':7,'terrace_f':7,'terrace_g':7,'house_c':13,'house_d':7,'castle_tower':13,'castle_donjon':30,'castle_palace':36,'zip_tower':22,'zip_station':24,'zip_trolley':8,'castle_wall':22,'castle_gatehouse':26,'castle_keep':22,'station':40,'mill':16,'bandstand':12,'wind_turbine':44,'balloon':16,'grandstand':44,'train_loco':6,'train_coach':6,'olive_tree':5,'lavender_row':14,'sunflower_row':14,'hotel':20,'map_board':5,'flag_pole':5,'pit_stop_2':34,'pit_stop_3':44,'pit_garages':56}
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
        render_lineup([n for n in ONLY if n in('church','market_stall','town_gate','vine_row','motors')],RENDER/'hillcrest.png')
        render_lineup([n for n in ONLY if n in('tunnel_portal','town_hall','hotel','terrace_a','terrace_b','station')],RENDER/'valley-town.png')
        render_lineup([n for n in ONLY if n in('terrace_a','terrace_b','terrace_c','terrace_d','house_c','house_d')],RENDER/'houses.png')
        render_lineup([n for n in ONLY if n in('terrace_a','terrace_e','terrace_f','terrace_g','terrace_d','terrace_f','terrace_c')],RENDER/'terraces.png')
        render_lineup([n for n in ONLY if n in('headland_gate','rail_gate')],RENDER/'headland-gates.png')
        render_lineup([n for n in ONLY if n in('castle_tower','castle_wall','castle_gatehouse','castle_keep','mill','bandstand')],RENDER/'valley-castle.png')
        render_lineup([n for n in ONLY if n in('tree_fir','tree_poplar','tree_blossom','tree_birch','tree_autumn','rock_a','rock_b','rock_cluster','reeds','grass_tuft','flower_bush')],RENDER/'valley-nature.png')
        render_lineup([n for n in ONLY if n in('castle_donjon','castle_palace','castle_gatehouse','castle_tower','castle_wall')],RENDER/'castle-grand.png')
        render_lineup([n for n in ONLY if n in('zip_tower','zip_station','zip_trolley')],RENDER/'zipline.png')
        render_lineup([n for n in ONLY if n in('cottage_a','cottage_b','cottage_c','terrace_b','house_d','town_gate','flag_wales')],RENDER/'quarry-town.png')
        render_lineup([n for n in ONLY if n in('haul_truck','excavator_base','wheel_loader','rock_stand_a','rock_stand_b','rock_stand_c','rock_stand_d','slate_stack')],RENDER/'quarry-plant.png')
        render_lineup([n for n in ONLY if n in('pit_stop_2','pit_stop_3')],RENDER/'pit-stop.png')
        render_lineup([n for n in ONLY if n in('pit_garages','grandstand')],RENDER/'raceway.png')
        render_lineup([n for n in ONLY if n in('wind_turbine','balloon','grandstand','train_loco','train_coach','olive_tree','lavender_row','sunflower_row','map_board','flag_pole')],RENDER/'valley-props.png')
