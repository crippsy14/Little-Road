"""Catalogue models for every non-engine part and the Valley V6 (presentation only; tuning stays in
parts.json). Each part is a recognisable real component, and the tiers within a slot differ in shape,
not just colour: steel wheel -> alloys -> knobbly trail -> low-profile; solid rotor -> drilled ->
slotted -> carbon-ceramic; airbox -> single turbo -> turbo + intercooler -> twin turbo, and so on.

Material names carry the finish to Unity (PartPreviewStage maps them): Cast, Alloy, Chrome, Rubber,
Brass, Carbon, White and Cover (the part's tier colour).

    blender -b --python source-art/build_part_previews.py [-- --render review/tmp/parts]
"""
from pathlib import Path
import bpy, bmesh, math, json, sys

ROOT=Path(__file__).resolve().parents[1]
TARGET=ROOT/'unity/Assets/Resources/Parts'
MASTERS=ROOT/'source-art/masters'
TARGET.mkdir(parents=True,exist_ok=True);MASTERS.mkdir(parents=True,exist_ok=True)
ARGS=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
RENDER=Path(ARGS[ARGS.index('--render')+1]) if '--render' in ARGS else None
ONLY=ARGS[ARGS.index('--only')+1].split(',') if '--only' in ARGS else None
TAU=math.tau

# ---------------------------------------------------------------- scene helpers
def reset():
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    for coll in (bpy.data.meshes,bpy.data.curves,bpy.data.materials):
        for block in list(coll):
            if block.users==0:coll.remove(block)

def material(name,color,metal,rough):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1)
    p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
    m.metallic=metal;m.roughness=rough
    return m

def finishes(cover):
    return {'cast':material('PartCast',(.30,.34,.35),.55,.45),'alloy':material('PartAlloy',(.66,.70,.71),.75,.28),
            'chrome':material('PartChrome',(.88,.89,.9),.95,.12),'rubber':material('PartRubber',(.035,.045,.05),0,.8),
            'brass':material('PartBrass',(.62,.44,.16),.7,.3),'carbon':material('PartCarbon',(.07,.075,.08),.25,.35),
            'white':material('PartWhite',(.92,.92,.9),.1,.35),'cover':material('PartCover',cover,.3,.3)}

def finish(o,name,mat,r=0.0,smooth=True):
    o.name=name
    if mat is not None:o.data.materials.append(mat)
    if o.type=='MESH' and smooth:
        for p in o.data.polygons:p.use_smooth=True
    if r:
        mod=o.modifiers.new('Edge radius','BEVEL');mod.width=r;mod.segments=2;mod.limit_method='ANGLE'
        mod=o.modifiers.new('Normals','WEIGHTED_NORMAL');mod.keep_sharp=True
    return o

def place(o,loc=(0,0,0),rot=(0,0,0)):
    o.location=loc;o.rotation_euler=rot;return o

ROT={'X':(0,math.pi/2,0),'Y':(math.pi/2,0,0),'Z':(0,0,0)}

def box(name,loc,size,mat,r=.01,rot=(0,0,0)):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc,rotation=rot);o=bpy.context.object;o.scale=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return finish(o,name,mat,r)

def cylinder(name,loc,radius,depth,mat,axis='Z',vertices=32,r=.004,rot=None):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=radius,depth=depth,location=loc,rotation=rot or ROT[axis])
    return finish(bpy.context.object,name,mat,r)

def sphere(name,loc,radius,mat,scale=(1,1,1)):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24,ring_count=12,radius=radius,location=loc);o=bpy.context.object;o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return finish(o,name,mat)

def torus(name,loc,major,minor,mat,axis='Z',seg=48,minor_seg=12):
    bpy.ops.mesh.primitive_torus_add(major_segments=seg,minor_segments=minor_seg,major_radius=major,minor_radius=minor,location=loc,rotation=ROT[axis])
    return finish(bpy.context.object,name,mat)

def pipe(name,points,mat,r=.02):
    data=bpy.data.curves.new(name,'CURVE');data.dimensions='3D';data.resolution_u=8;data.bevel_depth=r;data.bevel_resolution=3;data.use_fill_caps=True
    s=data.splines.new('BEZIER');s.bezier_points.add(len(points)-1)
    for p,co in zip(s.bezier_points,points):p.co=co;p.handle_left_type='AUTO';p.handle_right_type='AUTO'
    o=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(o);o.data.materials.append(mat);return o

def lathe(name,profile,mat,loc=(0,0,0),axis='Z',seg=48,smooth=True):
    """Spin an (r,z) profile about the local Z axis (a closed profile makes a closed solid)."""
    bm=bmesh.new();rings=[]
    for i in range(seg):
        a=TAU*i/seg;c,s=math.cos(a),math.sin(a)
        rings.append([bm.verts.new((r*c,r*s,z)) for r,z in profile])
    for i in range(seg):
        A=rings[i];B=rings[(i+1)%seg]
        for j in range(len(profile)-1):
            try:bm.faces.new((A[j],B[j],B[j+1],A[j+1]))
            except ValueError:pass
    bmesh.ops.remove_doubles(bm,verts=bm.verts,dist=1e-6)
    bmesh.ops.recalc_face_normals(bm,faces=bm.faces)
    me=bpy.data.meshes.new(name);bm.to_mesh(me);bm.free()
    o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o)
    place(o,loc,ROT[axis]);return finish(o,name,mat,smooth=smooth)

def ring_of(count,radius,make,phase=0.0):
    """Call make(angle, x, y) around a circle in the local XY plane."""
    for i in range(count):
        a=phase+TAU*i/count;make(a,math.cos(a)*radius,math.sin(a)*radius)

# ---------------------------------------------------------------- wheels and tyres (axle along X)
def tyre_profile(rim,outer,width,shoulder):
    h=width/2
    return [(rim,-h*.92),(outer-shoulder,-h),(outer,-h+shoulder),(outer,h-shoulder),(outer-shoulder,h),(rim,h*.92),(rim,-h*.92)]

def tyre(m,spec):
    rim,outer,width=spec['rim'],spec['outer'],spec['width']
    lathe('Tyre carcass',tyre_profile(rim,outer-spec.get('tread',.012),width,spec.get('shoulder',.03)),m['rubber'],axis='X',seg=64)
    style=spec['tread_style']
    def blocks(count,rows,size,lift,stagger=True,twist=0.0,shoulders=False):
        for row in range(rows):
            x=(row-(rows-1)/2)*width/rows
            off=(TAU/count/2) if (stagger and row%2) else 0
            for i in range(count):
                a=off+TAU*i/count;r=outer-spec.get('tread',.012)+lift/2
                box('Tread block',(x,math.cos(a)*r,math.sin(a)*r),size,m['rubber'],r=.003,rot=(a+math.pi/2,0,twist*(1 if row%2 else -1)))
        if shoulders:
            for side in (-1,1):
                for i in range(count):
                    a=TAU*(i+.5)/count;r=outer-.03
                    box('Shoulder lug',(side*(width/2+.004),math.cos(a)*r,math.sin(a)*r),(.018,size[1]*1.2,.04),m['rubber'],r=.003,rot=(a+math.pi/2,0,0))
    if style=='ribs':      # stock: four ribs with fine sipes
        for x in (-.36,-.12,.12,.36):lathe('Tread rib',[(outer-.013,x*width-.022),(outer,x*width-.018),(outer,x*width+.018),(outer-.013,x*width+.022)],m['rubber'],axis='X',seg=64)
    elif style=='touring':blocks(52,3,(width/3.4,.034,.012),.012,twist=.25)
    elif style=='trail':blocks(26,3,(width/3.2,.05,.028),.028,shoulders=True)
    elif style=='sport':   # near-slick: two wide bands, one centre groove
        for x0,x1 in ((-.47,-.04),(.04,.47)):lathe('Tread band',[(outer-.012,x0*width),(outer,x0*width+.006),(outer,x1*width-.006),(outer-.012,x1*width)],m['rubber'],axis='X',seg=64)
    elif style=='grip':    # wide directional V blocks
        blocks(40,2,(width/2.3,.03,.013),.013,stagger=False,twist=.45)
    elif style=='rally':blocks(22,4,(width/4.6,.045,.03),.03,shoulders=True)
    # Rim barrel and lip
    lathe('Rim barrel',[(rim*.96,-width*.44),(rim+.012,-width*.46),(rim+.012,-width*.4),(rim*.96,-width*.38),(rim*.96,width*.38),(rim+.012,width*.4),(rim+.012,width*.46),(rim*.96,width*.44),(rim*.9,width*.44),(rim*.9,-width*.44),(rim*.96,-width*.44)],m[spec['rim_mat']],axis='X',seg=64)
    face=width*.3;disc=spec['wheel']
    hub=m['chrome'] if disc!='rally' else m['alloy']
    if disc=='steel':     # pressed steel: dished disc, round vent holes, chrome cap
        lathe('Steel disc',[(0,face-.02),(rim*.55,face-.01),(rim*.62,face-.03),(rim*.93,face-.035),(rim*.93,face-.05),(0,face-.05)],m[spec['rim_mat']],axis='X')
        ring_of(6,rim*.76,lambda a,y,z:cylinder('Vent hole',(face-.03,y,z),.026,.012,m['rubber'],'X'))
        lathe('Hub cap',[(0,face+.008),(rim*.33,face-.004),(rim*.36,face-.02),(0,face-.02)],hub,axis='X')
    else:
        spokes={'touring':10,'trail':5,'sport':5,'grip':6,'rally':6}[disc]
        lathe('Centre',[(0,face+.004),(rim*.24,face),(rim*.28,face-.03),(0,face-.03)],m[spec['rim_mat']],axis='X')
        wide={'touring':.025,'trail':.075,'sport':.04,'grip':.05,'rally':.055}[disc]
        def spoke(a,y,z,offset=0.0):
            r0,r1=rim*.26,rim*.93;mid=(r0+r1)/2
            b=box('Spoke',(face-.018,math.cos(a+offset)*mid,math.sin(a+offset)*mid),(.03,r1-r0,wide),m[spec['rim_mat']],r=.006,rot=(a+offset,0,0))
        ring_of(spokes,0,lambda a,y,z:spoke(a,y,z))
        if disc=='sport':ring_of(spokes,0,lambda a,y,z:spoke(a,y,z,.14))   # split five-spoke
        cap=m['cover'] if disc in('sport','trail') else hub
        cylinder('Centre cap',(face+.008,0,0),rim*.11,.012,cap,'X')
        ring_of(5 if disc!='rally' else 6,rim*.17,lambda a,y,z:cylinder('Wheel nut',(face+.004,y,z),.009,.018,m['chrome'],'X',vertices=6))
    if spec.get('valve',True):cylinder('Valve stem',(face-.02,0,rim*.9),.005,.03,m['rubber'],'X')

TYRES={
 'tyres_stock':  dict(rim=.19,outer=.29,width=.155,tread_style='ribs',wheel='steel',rim_mat='cast'),
 'tyres_touring':dict(rim=.2,outer=.295,width=.17,tread_style='touring',wheel='touring',rim_mat='alloy'),
 'tyres_trail':  dict(rim=.19,outer=.315,width=.185,tread_style='trail',wheel='trail',rim_mat='cast',shoulder=.045,tread=.028),
 'tyres_sport':  dict(rim=.215,outer=.295,width=.195,tread_style='sport',wheel='sport',rim_mat='alloy',shoulder=.02),
 'tyres_grip':   dict(rim=.225,outer=.3,width=.225,tread_style='grip',wheel='grip',rim_mat='brass',shoulder=.02),
 'tyres_rally':  dict(rim=.19,outer=.31,width=.175,tread_style='rally',wheel='rally',rim_mat='white',shoulder=.04,tread=.03),
}

# ---------------------------------------------------------------- brakes (axle along X)
def brakes(m,spec):
    R,t=spec['rotor'],spec['thick']
    rotor=m['carbon'] if spec.get('ceramic') else m['alloy']
    if spec.get('vented'):
        for x in (-t/2+.005,t/2-.005):lathe('Rotor plate',[(R*.55,x-.005),(R,x-.005),(R,x+.005),(R*.55,x+.005),(R*.55,x-.005)],rotor,axis='X',seg=64)
        ring_of(36,R*.78,lambda a,y,z:box('Cooling vane',(0,y,z),(t-.02,R*.42,.006),m['cast'],r=0,rot=(a+.35,0,0)))
    else:
        lathe('Rotor',[(R*.55,-t/2),(R,-t/2),(R,t/2),(R*.55,t/2),(R*.55,-t/2)],rotor,axis='X',seg=64)
    # Bell (hat) and hub bolts
    hat=m['cast'] if not spec.get('ceramic') else m['alloy']
    cylinder('Rotor hat',(t/2+.016,0,0),R*.52,.032,hat,'X',vertices=48,r=.006)
    cylinder('Hub centre',(t/2+.036,0,0),R*.2,.012,m['chrome'],'X',vertices=32)
    ring_of(5,R*.34,lambda a,y,z:cylinder('Hub bolt',(t/2+.036,y,z),.009,.02,m['chrome'],'X',vertices=6))
    face=t/2+.001
    if spec.get('drilled'):
        for ring,count,phase in ((R*.68,18,0),(R*.8,18,TAU/36),(R*.9,18,0)):
            ring_of(count,ring,lambda a,y,z:cylinder('Drilling',(face,y,z),.006,.004,m['rubber'],'X',vertices=10),phase)
    if spec.get('slotted'):
        ring_of(12,R*.78,lambda a,y,z:box('Slot',(face,y,z),(.003,R*.28,.006),m['rubber'],r=0,rot=(a+.6,0,0)))
    # Caliper straddling the rotor at the top rear
    pistons=spec['pistons'];arc=spec['caliper_arc'];mat=m['cover'] if spec['painted'] else m['cast']
    ca=math.radians(70);cy,cz=math.cos(ca)*(R-.035),math.sin(ca)*(R-.035)
    body_len=R*arc;depth=t+.07
    box('Caliper body',(0,cy,cz),(depth,body_len,.075),mat,r=.018,rot=(ca-math.pi/2,0,0))
    box('Caliper bridge',(0,math.cos(ca)*(R+.012),math.sin(ca)*(R+.012)),(depth,body_len*.8,.03),mat,r=.01,rot=(ca-math.pi/2,0,0))
    per_side=max(1,pistons//2)
    for side in ((-1,1) if pistons>1 else (1,)):
        for k in range(per_side):
            f=(k-(per_side-1)/2)*body_len/per_side*.9
            ty,tz=-math.sin(ca)*f,math.cos(ca)*f
            cylinder('Piston cap',(side*depth/2,cy+ty,cz+tz),.022 if pistons<6 else .018,.012,m['alloy'],'X')
    if pistons==1:box('Slide bracket',(-(depth/2+.012),cy,cz),(.02,body_len*1.15,.05),m['cast'],r=.006,rot=(ca-math.pi/2,0,0))
    cylinder('Bleed nipple',(depth/2-.01,math.cos(ca+.2)*(R+.03),math.sin(ca+.2)*(R+.03)),.006,.03,m['brass'],rot=(ca+.2-math.pi/2,0,0))
    pipe('Brake line',[(depth/2-.02,math.cos(ca-.25)*(R+.03),math.sin(ca-.25)*(R+.03)),(depth/2+.02,math.cos(ca-.6)*(R+.08),math.sin(ca-.6)*(R+.1)),(depth/2+.06,0,R+.14)],m['rubber'] if not spec['painted'] else m['alloy'],r=.006)

BRAKES={
 'brakes_stock':  dict(rotor=.125,thick=.018,pistons=1,caliper_arc=.55,painted=False),
 'brakes_sport':  dict(rotor=.145,thick=.026,pistons=2,caliper_arc=.62,painted=True,vented=True,drilled=True),
 'brakes_race':   dict(rotor=.165,thick=.03,pistons=4,caliper_arc=.72,painted=True,vented=True,slotted=True),
 'brakes_ceramic':dict(rotor=.185,thick=.034,pistons=6,caliper_arc=.8,painted=True,vented=True,drilled=True,ceramic=True),
}

# ---------------------------------------------------------------- turbo slot
def compressor(m,loc,scale,outlet_dir=1,filter_mat=None):
    x,y,z=loc;s=scale
    # Compressor volute (the "snail"): a torus that grows around the scroll, plus the tangential outlet.
    lathe('Compressor housing',[(0,-.03*s),(.1*s,-.035*s),(.135*s,-.015*s),(.14*s,.01*s),(.125*s,.035*s),(.08*s,.045*s),(.055*s,.05*s),(0,.05*s)],m['alloy'],loc=(x,y,z),axis='X')
    torus('Volute scroll',(x-.005*s,y,z),.12*s,.035*s,m['alloy'],'X',seg=48,minor_seg=16)
    lathe('Inlet',[(.05*s,0),(.055*s,.07*s),(.045*s,.07*s),(.042*s,0)],m['alloy'],loc=(x+.03*s,y,z),axis='X')
    pipe('Compressor outlet',[(x,y+.1*s*outlet_dir,z+.05*s),(x,y+.16*s*outlet_dir,z+.1*s),(x+.02*s,y+.17*s*outlet_dir,z+.2*s)],m['alloy'],r=.028*s)
    cylinder('Centre cartridge',(x-.08*s,y,z),.04*s,.08*s,m['cast'],'X')
    # Turbine side: darker cast iron scroll and flange
    lathe('Turbine housing',[(0,-.04*s),(.09*s,-.045*s),(.12*s,-.02*s),(.12*s,.02*s),(.09*s,.04*s),(0,.04*s)],m['cast'],loc=(x-.15*s,y,z),axis='X')
    torus('Turbine scroll',(x-.15*s,y,z),.105*s,.03*s,m['cast'],'X',seg=48,minor_seg=12)
    box('Turbine flange',(x-.15*s,y,z-.11*s),(.09*s,.1*s,.02*s),m['cast'],r=.004)
    # Wastegate actuator can and rod
    cylinder('Wastegate can',(x-.03*s,y-.12*s*outlet_dir,z-.03*s),.035*s,.03*s,m['cover'],'Y')
    pipe('Wastegate rod',[(x-.03*s,y-.12*s*outlet_dir,z-.03*s),(x-.1*s,y-.09*s*outlet_dir,z-.04*s),(x-.14*s,y-.06*s*outlet_dir,z-.05*s)],m['chrome'],r=.004*s)
    if filter_mat is not None:
        lathe('Cone filter',[(.05*s,0),(.068*s,.015*s),(.064*s,.09*s),(.04*s,.11*s),(0,.115*s)],filter_mat,loc=(x+.1*s,y,z),axis='X')
        for k in range(4):torus('Filter band',(x+.12*s+k*.022*s,y,z),.066*s-k*.001*s,.004*s,m['chrome'],'X',seg=32,minor_seg=6)

def intercooler(m,loc,w,h):
    x,y,z=loc
    box('Intercooler core',(x,y,z),(.06,w,h),m['alloy'],r=.004)
    for k in range(int(h/.018)):box('Fin row',(x+.032,y,z-h/2+.01+k*.018),(.004,w*.96,.006),m['cast'],r=0)
    for side in (-1,1):box('End tank',(x,y+side*(w/2+.03),z),(.08,.06,h*1.05),m['cover'],r=.015)

def turbo(m,spec):
    kind=spec['kind']
    if kind=='na':   # airbox with snorkel and hose to the throttle body
        box('Airbox base',(0,0,.12),(.3,.22,.12),m['rubber'],r=.03);box('Airbox lid',(0,0,.2),(.31,.23,.05),m['cast'],r=.02)
        for k in range(4):cylinder('Lid clip',(-.12+k*.08,.115,.17),.012,.03,m['chrome'],'Y')
        pipe('Snorkel',[(.15,0,.15),(.25,0,.17),(.31,.05,.24)],m['rubber'],r=.035)
        pipe('Intake hose',[(-.15,0,.16),(-.25,0,.16),(-.3,-.06,.2)],m['rubber'],r=.04)
        for k in range(6):torus('Hose rib',(-.2-k*.016,0,.16),.043,.005,m['rubber'],'X',seg=24,minor_seg=6)
        cylinder('Throttle body',(-.35,-.08,.21),.045,.07,m['alloy'],'X')
    elif kind=='single':
        compressor(m,(0,0,.3),1.3,filter_mat=m['cover'])
        pipe('Exhaust manifold',[(-.2,-.25,.15),(-.2,-.1,.12),(-.2,0,.16)],m['cast'],r=.03)
    elif kind=='club':
        compressor(m,(.05,-.18,.35),1.5,outlet_dir=1,filter_mat=m['cover'])
        intercooler(m,(.1,.28,.3),.42,.28)
        pipe('Charge pipe',[(.08,.05,.62),(.1,.2,.6),(.1,.28,.48)],m['chrome'],r=.035)
        cylinder('Blow-off valve',(.12,.14,.66),.03,.05,m['cover'])
    elif kind=='twin':
        for side in (-1,1):compressor(m,(0,side*.22,.32),1.2,outlet_dir=-side,filter_mat=m['cover'])
        box('Log manifold',(-.2,0,.18),(.07,.6,.06),m['cast'],r=.02)
        intercooler(m,(.22,0,.62),.5,.2)
        for side in (-1,1):pipe('Charge pipe',[(0,side*.22,.55),(.1,side*.18,.66),(.19,side*.15,.64)],m['chrome'],r=.03)

TURBOS={'turbo_stock':dict(kind='na'),'turbo_street':dict(kind='single'),'turbo_club':dict(kind='club'),'turbo_twin':dict(kind='twin')}

# ---------------------------------------------------------------- transmission (input shaft along Y)
def gearbox(m,spec):
    L=spec['length'];mat=m['cast'] if spec['painted']==False else m['cover']
    lathe('Bell housing',[(0,0),(.21,0),(.22,.02),(.16,.2),(0,.2)],m['alloy'],loc=(0,-.02,.24),axis='Y')
    box('Main case',(0,-L/2-.02,.24),(.24,L,.26),mat,r=.04)
    for k in range(spec['ribs']):box('Case rib',(0,-.06-k*(L-.08)/max(1,spec['ribs']-1),.24),(.26,.014,.28),m['alloy'],r=.004)
    lathe('Tail housing',[(0,0),(.085,0),(.07,.16),(.05,.2),(0,.2)],m['alloy'],loc=(0,-L-.02,.24),axis='Y')
    lathe('Output flange',[(0,0),(.06,0),(.06,.018),(0,.018)],m['chrome'],loc=(0,-L-.24,.24),axis='Y')
    cylinder('Input shaft',(0,.19,.24),.02,.06,m['chrome'],'Y')
    # Shift tower, lever and knob; the six-speed gets its gate plate.
    box('Shift tower',(0,-L*.55,.4),(.1,.14,.08),m['alloy'],r=.02)
    lean=spec.get('lean',.18);lever=spec['lever']
    top=(0,-L*.55-math.sin(lean)*lever,.44+math.cos(lean)*lever)
    pipe('Gear lever',[(0,-L*.55,.43),(0,(-L*.55+top[1])/2,(.43+top[2])/2),top],m['chrome'],r=.009)
    torus('Gaiter',(0,-L*.55,.45),.04,.02,m['rubber'],'Z',seg=24,minor_seg=8)
    knob=spec['knob']
    if knob=='ball':sphere('Gear knob',top,.03,m[spec['knob_mat']])
    else:cylinder('Gear knob',top,.028,.05,m[spec['knob_mat']])
    if spec.get('gate'):
        box('Gate plate',(0,-L*.55,.485),(.11,.11,.006),m['alloy'],r=.002)
        for gx in (-.03,0,.03):box('Gate slot',(gx,-L*.55,.489),(.008,.07,.004),m['rubber'],r=0)
        box('Gate cross',(0,-L*.55,.489),(.07,.008,.004),m['rubber'],r=0)
    for side in (-1,1):box('Mount',(side*.14,-L*.6,.14),(.05,.1,.05),m['rubber'],r=.01)

GEARBOXES={
 'transmission_stock':dict(length=.42,ribs=3,painted=False,lever=.24,knob='ball',knob_mat='rubber'),
 'transmission_close':dict(length=.44,ribs=5,painted=True,lever=.16,knob='ball',knob_mat='cover',lean=.05),
 'transmission_long': dict(length=.56,ribs=6,painted=True,lever=.26,knob='cyl',knob_mat='chrome'),
 'transmission_six':  dict(length=.52,ribs=7,painted=True,lever=.2,knob='ball',knob_mat='brass',gate=True,lean=.08),
}

# ---------------------------------------------------------------- steering (rack along X, column to a wheel)
def steering(m,spec):
    L=.72
    cylinder('Rack tube',(0,0,.12),.022,L,m['alloy'],'X')
    box('Rack housing',(-.1,0,.12),(.2,.09,.09),m['cover'] if spec['painted'] else m['cast'],r=.025)
    for side in (-1,1):
        lathe('Rack boot',[(0,0)]+[(0.034 if k%2 else .024,k*.012) for k in range(1,11)]+[(0,.12)],m['rubber'],loc=(side*(L/2-.06),0,.12),axis='X')
        pipe('Tie rod',[(side*(L/2+.05),0,.12),(side*(L/2+.14),.02,.13),(side*(L/2+.2),.05,.13)],m['chrome'],r=.011)
        sphere('Track rod end',(side*(L/2+.21),.05,.13),.022,m['cast'])
    col=spec.get('column',(.0,.0));wheel_c=(-.05,-.38,.62)
    pipe('Column',[(-.08,0,.17),(-.07,-.15,.34),wheel_c],m['alloy'],r=.016)
    if spec.get('ujoint'):
        box('Universal joint',(-.075,-.08,.26),(.04,.04,.04),m['chrome'],r=.008)
    # Steering wheel, facing the driver (-Y), tilted back
    R=spec['wheel_r'];tilt=math.radians(-65)
    bpy.ops.mesh.primitive_torus_add(major_segments=64,minor_segments=14,major_radius=R,minor_radius=spec['grip'],location=wheel_c,rotation=(tilt,0,0))
    finish(bpy.context.object,'Wheel rim',m[spec['rim_mat']])
    dish=spec.get('dish',0.0)
    hub=wheel_c;ax=(0,math.cos(tilt+math.pi/2),math.sin(tilt+math.pi/2))  # wheel axis
    cylinder('Wheel hub',(hub[0]+ax[0]*.02,hub[1]+ax[1]*.02,hub[2]+ax[2]*.02),.045,.04,m[spec['hub_mat']],rot=(tilt,0,0))
    for k in range(spec['spokes']):
        a=math.radians(spec['spoke_angles'][k])
        # spoke end on the rim in the wheel plane
        ex=math.cos(a)*R;ey=math.sin(a)*R
        end=(wheel_c[0]+ex,wheel_c[1]+ey*math.cos(tilt),wheel_c[2]+ey*math.sin(tilt))
        start=(hub[0]+ax[0]*dish,hub[1]+ax[1]*dish,hub[2]+ax[2]*dish)
        pipe('Wheel spoke',[start,((start[0]+end[0])/2,(start[1]+end[1])/2,(start[2]+end[2])/2),end],m[spec['spoke_mat']],r=spec.get('spoke_r',.011))
    if spec.get('quick_release'):
        cylinder('Quick release',(hub[0]-ax[0]*.03,hub[1]-ax[1]*.03,hub[2]-ax[2]*.03),.03,.03,m['brass'],rot=(tilt,0,0))

STEERING={
 'steering_stock':    dict(painted=False,wheel_r=.19,grip=.012,rim_mat='rubber',hub_mat='rubber',spoke_mat='rubber',spokes=2,spoke_angles=[0,180],spoke_r=.014),
 'steering_quick':    dict(painted=True,wheel_r=.18,grip=.015,rim_mat='rubber',hub_mat='cover',spoke_mat='alloy',spokes=3,spoke_angles=[0,180,270]),
 'steering_precision':dict(painted=True,wheel_r=.165,grip=.017,rim_mat='rubber',hub_mat='cover',spoke_mat='alloy',spokes=3,spoke_angles=[20,160,270],ujoint=True),
 'steering_rally':    dict(painted=True,wheel_r=.175,grip=.018,rim_mat='rubber',hub_mat='cover',spoke_mat='carbon',spokes=3,spoke_angles=[0,180,270],dish=-.07,ujoint=True,quick_release=True),
}

# ---------------------------------------------------------------- chassis (frame along Y)
def chassis(m,spec):
    kind=spec['kind'];L=1.9;W=.62
    if kind in('ladder','light'):
        mat=m['cast'] if kind=='ladder' else m['cover']
        for side in (-1,1):
            pipe('Frame rail',[(side*W/2,L/2,.18),(side*W/2,L/4,.16),(side*(W/2+.04),0,.12),(side*W/2,-L/4,.16),(side*W/2,-L/2,.2)],mat,r=.045 if kind=='ladder' else .038)
            for y in (L*.33,-L*.33):
                lathe('Suspension turret',[(0,0),(.08,0),(.07,.18),(.05,.2),(0,.2)],m['alloy'],loc=(side*(W/2-.02),y,.2),axis='Z')
                torus('Spring',(side*(W/2-.02),y,.44),.045,.008,m['cover'] if kind=='ladder' else m['chrome'],'Z',seg=24,minor_seg=6)
        for y in (L*.45,L*.15,-L*.15,-L*.45):
            if kind=='ladder':box('Crossmember',(0,y,.16),(W,.07,.07),mat,r=.012)
            else:cylinder('Tube crossmember',(0,y,.16),.03,W,m['alloy'],'X')
        if kind=='light':
            for side in (-1,1):
                for y in (L*.38,L*.28,L*.08,-L*.08,-L*.28,-L*.38):cylinder('Lightening hole',(side*(W/2+.036),y,.13),.02,.02,m['rubber'],'X')
            for sgn in (-1,1):pipe('X brace',[(-W/2,sgn*L*.15,.16),(0,0,.17),(W/2,-sgn*L*.15,.16)],m['alloy'],r=.018)
    else:   # carbon tub with roll hoop and seat
        box('Tub floor',(0,0,.1),(W+.1,L*.72,.05),m['carbon'],r=.02)
        for side in (-1,1):box('Sill',(side*(W/2+.02),0,.2),(.1,L*.72,.22),m['carbon'],r=.04)
        box('Front bulkhead',(0,L*.36,.25),(W+.1,.06,.32),m['carbon'],r=.03)
        box('Rear bulkhead',(0,-L*.36,.3),(W+.1,.07,.42),m['carbon'],r=.03)
        pipe('Roll hoop',[(-W/2+.04,-L*.33,.2),(-W/2+.06,-L*.33,.72),(0,-L*.33,.82),(W/2-.06,-L*.33,.72),(W/2-.04,-L*.33,.2)],m['cover'],r=.028)
        for side in (-1,1):pipe('Hoop stay',[(side*(W/2-.08),-L*.33,.7),(side*(W/2-.1),-L*.45,.45),(side*(W/2-.1),-L*.55,.22)],m['cover'],r=.02)
        box('Seat base',(-.12,-L*.1,.17),(.3,.34,.08),m['cover'],r=.03)
        box('Seat back',(-.12,-L*.22,.38),(.3,.07,.4),m['cover'],r=.03,rot=(-.25,0,0))
        for side in (-1,1):box('Aluminium subframe',(side*W/2*.8,L*.46,.18),(.05,.25,.06),m['alloy'],r=.01)

CHASSIS={'chassis_stock':dict(kind='ladder'),'chassis_light':dict(kind='light'),'chassis_carbon':dict(kind='tub')}

# ---------------------------------------------------------------- Valley V6
def valley_v6(m,spec):
    width,length=.2,.62
    box('Crankcase',(0,0,.22),(.42,length,.26),m['cast'],r=.03)
    box('Sump',(0,0,.07),(.36,length*.92,.1),m['rubber'],r=.025)
    for side in (-1,1):
        tilt=side*math.radians(30)
        cx,cz=side*.14,.42
        box('Cylinder bank',(cx,0,cz),(width,length*.95,.22),m['cast'],r=.02,rot=(0,tilt,0))
        hx,hz=side*.2,.53
        box('Cylinder head',(hx,0,hz),(width*1.1,length,.08),m['alloy'],r=.015,rot=(0,tilt,0))
        box('Cam cover',(side*.225,0,.585),(width*.95,length*.94,.06),m['cover'],r=.02,rot=(0,tilt,0))
        for i in range(3):
            y=-length*.3+i*length*.3
            pipe('Exhaust header',[(side*.3,y,.48),(side*.38,y,.42),(side*.4,y*.6,.28),(side*.38,0,.18)],m['alloy'],r=.022)
            cylinder('Coil pack',(side*.2,y,.63),.018,.05,m['rubber'],rot=(0,tilt,0))
    box('Intake plenum',(0,0,.64),(.18,length*.8,.1),m['brass'],r=.035)
    for i in range(3):
        for side in (-1,1):
            y=-length*.28+i*length*.28
            pipe('Intake runner',[(side*.05,y,.62),(side*.09,y,.6),(side*.13,y,.56)],m['alloy'],r=.022)
    cylinder('Throttle body',(0,length*.46,.66),.05,.08,m['alloy'],'Y')
    cylinder('Crank pulley',(0,length*.53,.22),.12,.04,m['rubber'],'Y')
    for side in (-1,1):cylinder('Cam pulley',(side*.2,length*.51,.52),.07,.03,m['cover'],'Y')
    pipe('Timing belt',[(0,length*.55,.1),(.22,length*.55,.52),(0,length*.55,.6),(-.22,length*.55,.52),(0,length*.55,.1)],m['rubber'],r=.01)
    cylinder('Alternator',(.26,length*.35,.28),.075,.13,m['alloy'],'Y')
    for side in (-1,1):box('Engine mount',(side*.25,-.15,.18),(.08,.14,.06),m['cast'],r=.012)

BUILDERS=[]
for pid,s in TYRES.items():BUILDERS.append((pid,tyre,s))
for pid,s in BRAKES.items():BUILDERS.append((pid,brakes,s))
for pid,s in TURBOS.items():BUILDERS.append((pid,turbo,s))
for pid,s in GEARBOXES.items():BUILDERS.append((pid,gearbox,s))
for pid,s in STEERING.items():BUILDERS.append((pid,steering,s))
for pid,s in CHASSIS.items():BUILDERS.append((pid,chassis,s))
BUILDERS.append(('engine_valley',valley_v6,{}))

# Tier colours match PartPreviewStage.Accent (stock / mid / top / Hillcrest).
def cover_colour(pid):
    parts=json.loads((ROOT/'unity/Assets/Resources/Data/parts.json').read_text())['parts']
    me=next(p for p in parts if p['id']==pid)
    if me.get('shop')=='hillcrest':return (.86,.62,.12)
    peers=sorted([p for p in parts if p['slot']==me['slot'] and p.get('shop')!='hillcrest'],key=lambda p:p['price'])
    rank=[p['id'] for p in peers].index(pid)
    return [(.19,.27,.29),(.09,.46,.39),(.77,.22,.12),(.77,.22,.12)][min(rank,3)]

def render(path):
    scene=bpy.context.scene;scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.color_type='MATERIAL'
    scene.display.shading.light='STUDIO';scene.render.resolution_x=480;scene.render.resolution_y=360
    o=bpy.context.object;import mathutils
    pts=[o.matrix_world@mathutils.Vector(c) for c in o.bound_box];c=sum(pts,mathutils.Vector())/8;rad=max((p-c).length for p in pts)
    cam=bpy.data.objects.new('cam',bpy.data.cameras.new('cam'));scene.collection.objects.link(cam);scene.camera=cam
    d=mathutils.Vector((1.6,-2.2,1.3)).normalized();cam.location=c+d*rad*3.2
    cam.rotation_euler=(c-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.lens=50
    scene.render.filepath=str(path);bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam)

records=[]
for pid,build,spec in BUILDERS:
    if ONLY and pid not in ONLY:continue
    reset();m=finishes(cover_colour(pid));build(m,spec)
    bpy.ops.object.select_all(action='SELECT')
    bpy.context.view_layer.objects.active=next(o for o in bpy.context.scene.objects if o.type in('MESH','CURVE'))
    bpy.ops.object.convert(target='MESH')
    bpy.ops.object.select_all(action='SELECT');bpy.context.view_layer.objects.active=next(o for o in bpy.context.scene.objects if o.type=='MESH')
    bpy.ops.object.join();o=bpy.context.object;o.name=pid
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(MASTERS/f'{pid}.blend'))
    bpy.ops.export_scene.fbx(filepath=str(TARGET/f'{pid}.fbx'),use_selection=True,object_types={'MESH'},apply_unit_scale=True,bake_space_transform=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False)
    tris=sum(len(p.vertices)-2 for p in o.data.polygons)
    records.append({'part_id':pid,'triangles':tris,'materials':sorted({s.material.name.split('.')[0] for s in o.material_slots if s.material})})
    if RENDER:RENDER.mkdir(parents=True,exist_ok=True);render(RENDER/f'{pid}.png')
    print('PART_PREVIEW',pid,tris)
(MASTERS/'part-previews.json').write_text(json.dumps(records,indent=2))
print('PART_PREVIEWS_EXPORTED',len(records))
