"""Authored Little Road compact study; Blender +Y nose, metres, separate wheels.

Creates editable source, Unity FBX, geometry metadata and actual 3D review renders.
The older four prototypes and their Godot exports are left intact.
"""
from pathlib import Path
import json, math, sys
import bpy, bmesh
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
RECIPE=json.loads((ROOT/'source-art/masters/compact_v1.recipe.json').read_text())
args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
OUT=Path(args[args.index('--review-output')+1]) if '--review-output' in args else ROOT/'review/unity-03'
OUT=OUT.resolve()
OUT.mkdir(parents=True,exist_ok=True)
TARGET=ROOT/'unity/Assets/Resources/Cars'
TARGET.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)

def material(name,color,metal=0,rough=.4):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1)
    p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
    return m

paint=material('BodyPaint',RECIPE['paint'],.22,.27)
paint.node_tree.nodes['Principled BSDF'].inputs['Coat Weight'].default_value=.35
paint.node_tree.nodes['Principled BSDF'].inputs['Coat Roughness'].default_value=.22
glass=material('MasterGlass',(.055,.075,.09),0,.18)
glass.node_tree.nodes['Principled BSDF'].inputs['Transmission Weight'].default_value=0
glass.node_tree.nodes['Principled BSDF'].inputs['Alpha'].default_value=.72
rubber=material('TyreRubber',(.025,.032,.036),0,.82)
trim=material('CharcoalTrim',(.024,.033,.037),.05,.48)
seam=material('PanelSeam',(.024,.054,.051),0,.55)
alloy=material('WarmAlloy',(.57,.61,.61),.75,.24)
disc=material('BrakeMetal',(.080,.095,.10),.55,.52)
cloth=material('InteriorCloth',(.065,.088,.094),0,.9)
lamp=material('WarmHeadlamp',(.86,.86,.69),.25,.16)
red=material('TailLight',(.60,.026,.015),.18,.22)
reverse=material('ReverseLight',(.70,.75,.76),.08,.22)
reflector=material('RearReflector',(.42,.012,.008),0,.3)
bumper_insert=material('BumperInsert',(.22,.25,.26),.08,.5)
amber=material('Amber',(.92,.31,.025),.12,.24)
plate=material('NumberPlate',(.84,.83,.68),0,.45)

def finish(o,name,mat,smooth=True):
    o.name=name;o.data.materials.append(mat)
    if o.type=='MESH':
        for p in o.data.polygons:p.use_smooth=smooth
    return o

def bevel(o,width=.03,segments=3):
    m=o.modifiers.new('Manufactured corner radius','BEVEL');m.width=width;m.segments=segments
    m=o.modifiers.new('Panel normals','WEIGHTED_NORMAL');m.keep_sharp=True;m.weight=40
    return o

def box(name,loc,size,mat,r=.025):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.dimensions=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    finish(o,name,mat);return bevel(o,r,3) if r else o

def mesh(name,v,f,mat,smooth=True):
    data=bpy.data.meshes.new(name);data.from_pydata(v,[],f);data.update()
    bm=bmesh.new();bm.from_mesh(data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(data);bm.free()
    o=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(o);return finish(o,name,mat,smooth)

def cylinder(name,loc,radius,depth,mat,axis='X',vertices=40):
    rot=(0,math.pi/2,0) if axis=='X' else (math.pi/2,0,0) if axis=='Y' else (0,0,0)
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=radius,depth=depth,location=loc,rotation=rot)
    return bevel(finish(bpy.context.object,name,mat),.008,2)

def curve(name,points,mat,r=.01,cyclic=False,poly=False):
    data=bpy.data.curves.new(name,'CURVE');data.dimensions='3D';data.resolution_u=10;data.bevel_depth=r;data.bevel_resolution=2
    if cyclic or poly:
        # Closed four-corner seals must not overshoot like auto-handle ellipses.
        s=data.splines.new('POLY');s.points.add(len(points)-1)
        for p,co in zip(s.points,points):p.co=(*co,1)
    else:
        s=data.splines.new('BEZIER');s.bezier_points.add(len(points)-1)
        for p,co in zip(s.bezier_points,points):p.co=co;p.handle_left_type='AUTO';p.handle_right_type='AUTO'
    s.use_cyclic_u=cyclic;o=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(o);o.data.materials.append(mat);return o

# Deliberate shoulder/bonnet profiles rather than a stretched box. Cross-sections
# are smooth rounded rectangles; wheel openings are real cut-outs in this shell.
profiles=[(-1.51,.57,.40,.86),(-1.43,.76,.34,.99),(-1.20,.87,.32,1.055),
          (-.75,.90,.32,1.065),(-.1,.91,.32,1.07),(.55,.90,.33,1.025),
          (1.05,.86,.36,1.02),(1.40,.82,.40,1.00),(1.52,.76,.46,.98)]

def section(y):
    for i in range(len(profiles)-1):
        if y<=profiles[i+1][0]:break
    a=profiles[max(0,i-1)];b=profiles[i];c=profiles[i+1];d=profiles[min(len(profiles)-1,i+2)]
    t=max(0,min(1,(y-b[0])/(c[0]-b[0])))
    span=c[0]-b[0]
    return tuple((2*t**3-3*t*t+1)*b[k]+(t**3-2*t*t+t)*span*(c[k]-a[k])/(c[0]-a[0])+
                 (-2*t**3+3*t*t)*c[k]+(t**3-t*t)*span*(d[k]-b[k])/(d[0]-b[0]) for k in [1,2,3])

def surface_x(y,z):
    w,lo,hi=section(y);u=min(.9999,abs((z-(lo+hi)*.5)/((hi-lo)*.5)))
    return w*max(.00001,1-u**(2/.48))**(.40/2)

v=[];f=[];ny=65;nr=48
for j in range(ny):
    y=profiles[0][0]+(profiles[-1][0]-profiles[0][0])*j/(ny-1);w,lo,hi=section(y)
    for k in range(nr):
        a=k*math.tau/nr;c=math.cos(a);s=math.sin(a)
        v.append((w*math.copysign(abs(c)**.40,c),y,(lo+hi)*.5+(hi-lo)*.5*math.copysign(abs(s)**.48,s)))
for j in range(ny-1):
    for k in range(nr):f.append((j*nr+k,j*nr+(k+1)%nr,(j+1)*nr+(k+1)%nr,(j+1)*nr+k))
f.extend([tuple(range(nr-1,-1,-1)),tuple((ny-1)*nr+k for k in range(nr))])
body=mesh('Continuous formed body shell',v,f,paint)
# Preserve the uninterrupted formed-sheet normals. Boolean openings otherwise
# tilt neighbouring vertex normals toward the cut walls and dent the highlights.
normal_source=body.copy();normal_source.data=body.data.copy()
bpy.context.collection.objects.link(normal_source)
normal_source.name='Temporary uncut shading reference'
for side in [-1,1]:
    for y in [RECIPE['front_axle'],RECIPE['rear_axle']]:
        bpy.ops.mesh.primitive_cylinder_add(vertices=64,radius=.485,depth=.70,location=(side*.99,y,.425),rotation=(0,math.pi/2,0))
        cutter=bpy.context.object;mod=body.modifiers.new('Actual wheel arch opening','BOOLEAN');mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cutter
        bpy.context.view_layer.objects.active=body;bpy.ops.object.modifier_apply(modifier=mod.name);bpy.data.objects.remove(cutter,do_unlink=True)

# Seat each headlamp in a real circular aperture. Moving the lens into the
# curved nose without cutting the shell hides the bottom half of the lamp.
for side in [-1,1]:
    bpy.ops.mesh.primitive_cylinder_add(vertices=64,radius=.181,depth=.34,location=(side*.546,1.47,.777),rotation=(math.pi/2,0,0))
    cutter=bpy.context.object;mod=body.modifiers.new('Recessed headlamp aperture','BOOLEAN');mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cutter
    bpy.context.view_layer.objects.active=body;bpy.ops.object.modifier_apply(modifier=mod.name);bpy.data.objects.remove(cutter,do_unlink=True)

normal_transfer=body.modifiers.new('Continuous panel reflections','DATA_TRANSFER')
normal_transfer.object=normal_source;normal_transfer.use_loop_data=True
normal_transfer.data_types_loops={'CUSTOM_NORMAL'};normal_transfer.loop_mapping='POLYINTERP_NEAREST'
bpy.context.view_layer.objects.active=body
bpy.ops.object.modifier_apply(modifier=normal_transfer.name)
bpy.data.objects.remove(normal_source,do_unlink=True)

# A real passenger well: the original closed body shell ran through the seat
# backs at beltline height. Cut its deck down to the floor, leaving sill and
# wheel-arch structure intact. Existing exterior normals remain on the shell.
body.data.materials.append(trim)
cutter=box('Temporary passenger well cutter',(0,-.325,1.10),(1.26,1.59,1.20),paint,.045)
cutter.data.materials.append(trim)
for polygon in cutter.data.polygons:polygon.material_index=1
bpy.context.view_layer.objects.active=cutter;cutter.select_set(True)
for modifier in list(cutter.modifiers):bpy.ops.object.modifier_apply(modifier=modifier.name)
mod=body.modifiers.new('Open passenger cabin','BOOLEAN');mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cutter
bpy.context.view_layer.objects.active=body;bpy.ops.object.modifier_apply(modifier=mod.name)
bpy.data.objects.remove(cutter,do_unlink=True)

# Rubber arch mouldings follow the side surface around each actual opening.
for side in [-1,1]:
    for cy in [RECIPE['front_axle'],RECIPE['rear_axle']]:
        av=[];af=[]
        for k in range(41):
            a=-.13+(math.pi+.26)*k/40
            for r in [.483,.533]:
                y=cy+r*math.cos(a);z=.425+r*math.sin(a);av.append((side*(surface_x(y,z)+.025),y,z))
        for k in range(40):af.append((k*2,k*2+1,k*2+3,k*2+2))
        arch=mesh('Moulded wheel arch',av,af,trim);sol=arch.modifiers.new('Trim wall','SOLIDIFY');sol.thickness=.025
        bevel(arch,.009,2)

# Roof is a separate gently crowned sheet. Pillars surround real apertures;
# there is no opaque cabin block hidden behind the glazing.
rv=[];rf=[]
for j in range(17):
    y=-.91+1.075*j/16
    for i in range(21):
        # Bring the crown down to the common frame edge at BOTH headers.
        # The previous constant crosswise crown required a vertical filler strip,
        # leaving a visible raised ledge above the windscreen.
        u=-1+2*i/20;z=1.626+.060*(1-u*u)*math.sin(math.pi*j/16)
        rv.append((u*.66,y,z))
for j in range(16):
    for i in range(20):a=j*21+i;rf.append((a,a+1,a+22,a+21))
roof=mesh('Crowned roof panel',rv,rf,paint);m=roof.modifiers.new('Roof metal thickness','SOLIDIFY');m.thickness=.012

def rounded_loop(corners,fraction=.10):
    result=[]
    for k,p in enumerate(corners):
        p=Vector(p);a=p.lerp(Vector(corners[(k-1)%4]),fraction);b=p.lerp(Vector(corners[(k+1)%4]),fraction)
        for j in range(6):
            t=j/5;result.append((1-t)**2*a+2*(1-t)*t*p+t*t*b)
    return result

def glass_patch(name,corners,bulge_axis,bulge=.02):
    boundary=rounded_loop(corners);center=sum(boundary,Vector())/len(boundary);n=len(boundary)
    verts=[center+Vector(bulge_axis)*bulge];faces=[]
    for ring in range(1,5):
        radius=ring/4
        verts.extend([center.lerp(p,radius)+Vector(bulge_axis)*bulge*(1-radius*radius) for p in boundary])
    for k in range(n):faces.append((0,1+k,1+(k+1)%n))
    for ring in range(3):
        for k in range(n):a=1+ring*n+k;b=1+ring*n+(k+1)%n;faces.append((a,b,b+n,a+n))
    o=mesh(name,verts,faces,glass)
    if o.data.polygons[0].normal.dot(Vector(bulge_axis))<0:
        bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.reverse_faces(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
    curve(name+' recessed seal',boundary,trim,.009,True);return o

def frame(name,outer,inner):
    outside=rounded_loop(outer,.024);inside=rounded_loop(inner);n=len(outside)
    normal=(Vector(outer[1])-Vector(outer[0])).cross(Vector(outer[2])-Vector(outer[0])).normalized()
    centre=sum((Vector(p) for p in outer),Vector())/4
    if normal.dot(Vector((centre.x,centre.y,0)))<0:normal=-normal
    fv=[];ff=[]
    for ring in range(5):
        t=ring/4
        fv.extend([a.lerp(b,t)+normal*(.012*math.sin(math.pi*t)) for a,b in zip(outside,inside)])
    for ring in range(4):
        for k in range(n):
            a=ring*n+k;b=ring*n+(k+1)%n;ff.append((a,b,b+n,a+n))
    o=mesh(name,fv,ff,paint)
    m=o.modifiers.new('Pressed frame thickness','SOLIDIFY');m.thickness=.018;bevel(o,.012,2)
    return o

front=[(-.713,.566,1.060),(.713,.566,1.060),(.605,.202,1.575),(-.605,.202,1.575)]
rear=[(.702,-1.153,1.085),(-.702,-1.153,1.085),(-.603,-.935,1.575),(.603,-.935,1.575)]
glass_patch('Curved front windscreen',front,(0,1,0),.013)
glass_patch('Rear hatch glazing',rear,(0,-1,0),.010)
frame('Pressed windscreen surround',[(-.766,.60,1.015),(.766,.60,1.015),(.66,.165,1.626),(-.66,.165,1.626)],front)
frame('Pressed rear hatch surround',[(.78,-1.18,1.025),(-.78,-1.18,1.025),(-.66,-.91,1.626),(.66,-.91,1.626)],rear)
# The roof crown terminates directly on the frame, with no upright filler.
for side in [-1,1]:
    def sx(z):return side*(.785-(z-1.02)*(.125/.606))
    outer=[(side*.785,-1.18,1.025),(side*.766,.60,1.015),(side*.66,.165,1.626),(side*.66,-.91,1.626)]
    inner=[(sx(1.085),-1.095,1.085),(sx(1.066),.495,1.066),(sx(1.572),.128,1.572),(sx(1.575),-.872,1.575)]
    frame('Pressed side window surround',outer,inner)
    glass_patch('Front door side glass',[(sx(1.081),-.29,1.081),inner[1],inner[2],(sx(1.573),-.29,1.573)],(side,0,0),.006)
    glass_patch('Rear quarter glass',[inner[0],(sx(1.081),-.39,1.081),(sx(1.574),-.39,1.574),inner[3]],(side,0,0),.006)
    mesh('Flat satin B pillar',[(sx(1.079)+side*.004,-.39,1.079),(sx(1.079)+side*.004,-.29,1.079),
        (sx(1.576)+side*.004,-.29,1.576),(sx(1.576)+side*.004,-.39,1.576)],[(0,1,2,3)],trim,False)
    door=[(.49,1.027),(.39,.86),(.29,.52),(.20,.395),(-.23,.397),(-.37,.53),(-.55,.88),(-.59,1.055)]
    curve('Door shut line',[(side*(surface_x(y,z)+.008),y,z) for y,z in door],seam,.006)
    box('Recessed door handle pocket',(side*.911,-.42,.985),(.018,.24,.065),seam,.022)
    box('Satin door handle',(side*.925,-.42,.988),(.025,.18,.032),trim,.012)
    curve('Mirror stem',[(side*.755,.44,1.06),(side*.924,.43,1.075)],trim,.029)
    box('Rounded mirror housing',(side*.998,.40,1.095),(.22,.20,.125),trim,.044)
    box('Mirror reflective insert',(side*.998,.296,1.095),(.155,.012,.082),alloy,.027)
    box('Rocker protection',(side*.865,-.01,.363),(.065,.72,.095),trim,.035)
    # Separate lens/rim/recess, with round miniature-car character.
    for n,r,yy,mat in [('Lamp recess',.190,1.490,trim),('Lamp chrome lip',.169,1.518,alloy)]:
        cylinder(n,(side*.546,yy,.777),r,.045,mat,'Y')
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32,ring_count=12,location=(side*.546,1.546,.777))
    lens=finish(bpy.context.object,'Convex headlamp lens',lamp);lens.scale=(.148,.033,.148)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    box('Rear lamp rubber surround',(side*.626,-1.421,.932),(.200,.065,.317),trim,.036)
    box('Vertical red rear lamp',(side*.626,-1.463,.963),(.160,.027,.222),red,.032)
    box('Reverse lamp',(side*.626,-1.492,.820),(.154,.022,.049),reverse,.012)
    for dz in [-.05,0,.05]:
        box('Moulded tail lens rib',(side*.626,-1.480,.963+dz),(.120,.006,.008),red,.002)
    box('Front wing indicator',(side*(surface_x(.91,.947)+.009),.91,.947),(.025,.116,.045),amber,.016)

box('Front charcoal bumper',(0,1.435,.470),(1.48,.18,.25),trim,.087)
box('Rear charcoal bumper',(0,-1.448,.437),(1.52,.17,.25),trim,.075)
box('Grille inset',(0,1.534,.717),(.51,.027,.102),trim,.043)
for zz in [.696,.722,.748]:
    box('Grille horizontal blade',(0,1.550,zz),(.432,.008,.008),bumper_insert,.003)
box('Small bonnet emblem',(0,1.526,.933),(.065,.010,.037),alloy,.010)
box('Front number plate',(0,1.537,.468),(.43,.024,.117),plate,.018)
box('Rear plate recess',(0,-1.546,.462),(.57,.019,.153),seam,.031)
box('Rear number plate',(0,-1.559,.462),(.43,.016,.111),plate,.014)
box('Hatch release',(0,-1.481,.996),(.23,.04,.043),trim,.017)
curve('Front wiper',[(-.39,.562,1.090),(-.15,.542,1.120),(.32,.541,1.120)],trim,.008)
curve('Rear wiper',[(.28,-1.161,1.115),(.12,-1.152,1.135),(-.29,-1.152,1.135)],trim,.012)
box('High mounted brake lens',(0,-.960,1.563),(.30,.030,.035),red,.012)

# Follow the actual formed shell for the hatch joint, rather than floating a
# rectangular decal in front of a rounded surface. The wheel/overall envelope
# and existing body identity remain unchanged during this detail revision.
def rear_surface_y(x,z):
    low,high=profiles[0][0],-.70
    for _ in range(28):
        y=(low+high)*.5;w,lo,hi=section(y)
        inside=(abs(x)/w)**(2/.40)+(abs((z-(lo+hi)*.5)/((hi-lo)*.5)))**(2/.48)<=1
        if inside:high=y
        else:low=y
    return high-.009
hatch=[]
for side in [1,-1]:
    for i in range(13):
        t=i/12 if side==1 else 1-i/12
        x=side*(.520+.055*t);z=1.045-.42*t;hatch.append((x,rear_surface_y(x,z),z))
    if side==1:
        for i in range(1,24):
            x=.575-1.15*i/24;z=.625-.035*(1-(x/.575)**2);hatch.append((x,rear_surface_y(x,z),z))
curve('Recessed tailgate shut line',hatch,seam,.005,poly=True)
box('Rear bumper inset',(0,-1.541,.445),(.90,.018,.19),bumper_insert,.055)
box('Hatch badge',(0,rear_surface_y(0,.875)-.007,.875),(.065,.018,.063),alloy,.018)
for side in [-1,1]:
    box('Rear bumper reflector',(side*.62,-1.538,.442),(.13,.014,.036),reflector,.013)
# Fuel door on the rear quarter, projected onto the existing body surface.
fuel=[]
for p in rounded_loop([(0,-.63,.865),(0,-.90,.865),(0,-.90,1.008),(0,-.63,1.008)],.2):
    fuel.append((surface_x(p.y,p.z)+.009,p.y,p.z))
curve('Fuel filler door seam',fuel,seam,.0045,True)
for side in [-1,1]:
    bonnet=[]
    for i in range(33):
        t=i/32;y=1.48-.86*t;x=side*(.30+.25*t);w,lo,hi=section(y)
        z=(lo+hi)*.5+(hi-lo)*.5*max(0,1-(abs(x)/w)**(2/.40))**(.48/2)
        bonnet.append((x,y,z+.003))
    curve('Bonnet shut line',bonnet,seam,.0035,poly=True)
cylinder('Exhaust chrome outlet',(-.43,-1.508,.278),.063,.13,alloy,'Y',32)
cylinder('Exhaust dark bore',(-.43,-1.578,.278),.048,.008,trim,'Y',32)
box('Underbody',(0,-.04,.319),(1.35,2.48,.055),trim,.04)

# A floor, inner trim and supported seats occupy the hollow passenger well.
box('Cabin carpet floor',(0,-.325,.523),(1.20,1.51,.036),cloth,.025)
for side in [-1,1]:
    box('Inner door card',(side*.620,-.31,.794),(.028,1.38,.50),trim,.021)
    box('Inner door armrest',(side*.587,-.14,.88),(.075,.33,.050),trim,.018)
box('Dashboard',(0,.37,1.0),(1.30,.30,.17),trim,.055)
for side in [-1,1]:
    box('Seat pedestal',(side*.36,-.18,.566),(.34,.37,.08),trim,.015)
    box('Front seat cushion',(side*.36,-.18,.68),(.43,.47,.17),cloth,.07)
    o=box('Front seat back',(side*.36,-.40,.986),(.43,.18,.57),cloth,.065);o.rotation_euler.x=-.10
    box('Seat headrest',(side*.36,-.43,1.311),(.26,.14,.20),cloth,.045)
box('Rear bench cushion',(0,-.88,.655),(1.10,.31,.19),cloth,.05)
box('Rear bench back',(0,-1.02,.935),(1.10,.15,.46),cloth,.055)
box('Centre tunnel',(0,-.12,.59),(.13,.81,.105),trim,.030)
wheel_centre=Vector((-.36,.07,1.025))
def steering_point(angle):
    return wheel_centre+Vector((.145*math.cos(angle),.035*math.sin(angle),.145*math.sin(angle)))
curve('Steering wheel',[steering_point(a*math.tau/32) for a in range(32)],trim,.018,True)
curve('Steering column',[wheel_centre,(-.36,.30,.91)],trim,.030,poly=True)
cylinder('Steering wheel hub',wheel_centre,.047,.045,trim,'Y',24)
for angle in [0,math.pi,math.pi*1.5]:
    curve('Steering wheel spoke',[wheel_centre,steering_point(angle)],trim,.016,poly=True)
box('Instrument binnacle',(-.36,.285,1.095),(.28,.12,.14),trim,.035)
for xx in [-.43,-.29]:
    cylinder('Instrument dial',(xx,.220,1.108),.045,.006,disc,'Y',24)

# Wheel module: rounded tyre profile, deep rim, five shaped spokes, hub and disc.
def make_wheel(side,cy,name):
    before=set(bpy.context.scene.objects)
    cx=side*RECIPE['wheel_half_track'];cz=RECIPE['wheel_radius']
    tyre_profile=[(-.144,.303),(-.164,.340),(-.164,.378),(-.144,.406),(-.112,.422),
        (-.075,.425),(-.069,.416),(-.057,.416),(-.051,.425),(.051,.425),
        (.057,.416),(.069,.416),(.075,.425),(.112,.422),(.144,.406),(.164,.378),(.164,.340),(.144,.303)]
    wv=[];wf=[];n=56
    for u,r in tyre_profile:
        for k in range(n):
            a=k*math.tau/n;rr=r
            wv.append((cx+u,cy+rr*math.sin(a),cz+rr*math.cos(a)))
    for j in range(len(tyre_profile)):
        for k in range(n):wf.append((j*n+k,j*n+(k+1)%n,((j+1)%len(tyre_profile))*n+(k+1)%n,((j+1)%len(tyre_profile))*n+k))
    mesh('Profiled tyre',wv,wf,rubber)
    outer=cx+side*.149
    cylinder('Brake disc',(cx+side*.066,cy,cz),.248,.025,disc)
    for i in range(14):
        a=i*math.tau/14
        cylinder('Disc ventilation recess',(cx+side*.080,cy+math.sin(a)*.212,cz+math.cos(a)*.212),.008,.002,trim,vertices=8)
    cylinder('Rim barrel',(cx+side*.087,cy,cz),.305,.09,trim)
    # Raised outer rim ring and sidewall moulding, each a closed lathed tube.
    for label,r,minor,x,mat in [('Alloy rim lip',.29,.019,outer,alloy),('Tyre sidewall bead',.330,.004,cx+side*.169,rubber)]:
        tv=[];tf=[]
        for a in range(56):
            theta=a*math.tau/56
            for b in range(8):
                phi=b*math.tau/8;rad=r+minor*math.cos(phi);tv.append((x+minor*math.sin(phi),cy+rad*math.sin(theta),cz+rad*math.cos(theta)))
        for a in range(56):
            for b in range(8):tf.append((a*8+b,((a+1)%56)*8+b,((a+1)%56)*8+(b+1)%8,a*8+(b+1)%8))
        mesh(label,tv,tf,mat)
    for i in range(5):
        a=i*math.tau/5
        sv=[]
        for xx in [-.019,.019]:
            for radius,width in [(.077,-.032),(.285,-.024),(.285,.024),(.077,.032)]:
                sv.append((outer+xx,cy+math.sin(a)*radius+math.cos(a)*width,cz+math.cos(a)*radius-math.sin(a)*width))
        spoke=mesh('Tapered cast alloy spoke',sv,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],alloy)
        bevel(spoke,.007,3)
        cylinder('Recessed hub bolt',(outer+side*.028,cy+math.sin(a)*.072,cz+math.cos(a)*.072),.014,.012,trim,vertices=12)
    cylinder('Hub centre',(outer+side*.027,cy,cz),.10,.05,alloy)
    cylinder('Teal hub badge',(outer+side*.055,cy,cz),.044,.008,paint,vertices=24)
    objects=list(set(bpy.context.scene.objects)-before)
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.convert(target='MESH');bpy.ops.object.join();wheel=bpy.context.object;wheel.name=name+'_Mesh'
    pivot=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(pivot);pivot.location=(cx,cy,cz)
    bpy.context.view_layer.update();world=wheel.matrix_world.copy();wheel.parent=pivot;wheel.matrix_world=world

for side,letter in [(-1,'L'),(1,'R')]:
    for cy,end in [(RECIPE['front_axle'],'F'),(RECIPE['rear_axle'],'R')]:
        make_wheel(side,cy,'Wheel_'+end+letter)
        box('Fixed brake caliper',(side*(RECIPE['wheel_half_track']+.091),cy-.19,.425),(.055,.085,.19),bumper_insert,.026)

# Join stationary pieces after applying modifiers, retaining material slots.
bpy.ops.object.select_all(action='DESELECT')
parts=[o for o in bpy.context.scene.objects if o.type in {'MESH','CURVE'} and o.parent is None]
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.convert(target='MESH')
# A lower, gently wider greenhouse gives the compact a planted silhouette.
# Apply the same continuous mapping to glass, seals and interior so they fit.
for o in parts:
    if o.type!='MESH':continue
    inverse=o.matrix_world.inverted()
    for vertex in o.data.vertices:
        p=o.matrix_world@vertex.co
        if p.z>1.08:
            h=p.z-1.08;p.x*=1+.04*min(1,h/.55);p.z=1.08+h*.88
            vertex.co=inverse@p
bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.convert(target='MESH');bpy.ops.object.join();bpy.context.object.name='Body'
for o in bpy.context.scene.objects:
    if o.type=='MESH':
        bpy.context.view_layer.objects.active=o;o.select_set(True)
        # Initial UV unwrap, explicitly a study layout, not yet approved for
        # decals or constrained-variant generation.
        bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.02);bpy.ops.object.mode_set(mode='OBJECT');o.select_set(False)

meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
vertices=sum(len(o.data.vertices) for o in meshes)
triangles=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes)
points=[o.matrix_world@v.co for o in meshes for v in o.data.vertices]
metadata={'body_id':RECIPE['body_id'],'recipe_version':RECIPE['recipe_version'],'vertices':vertices,'triangles':triangles,
          'bounds_blender':{'min':[min(p[i] for p in points) for i in range(3)],'max':[max(p[i] for p in points) for i in range(3)]},
          'wheel_pivots':[o.name for o in bpy.context.scene.objects if o.name.startswith('Wheel_') and o.type=='EMPTY'],
          'status':'Master study; requires Unity/gallery/contact review. UV islands are provisional, no approved decal templates.'}
(OUT/'compact-v1-geometry.json').write_text(json.dumps(metadata,indent=2))
bpy.context.scene.world.color=(.2,.2,.2)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'source-art/masters/compact_v1.blend'))
bpy.ops.export_scene.fbx(filepath=str(TARGET/'compact_v1.fbx'),use_selection=False,object_types={'MESH','EMPTY'},apply_unit_scale=True,bake_space_transform=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,use_mesh_modifiers=True,bake_anim=False)

# Derive reduced meshes from copies, leaving the authored source intact.
# Review transitions in Unity before accepting these as production LODs.
sys.path.insert(0,str(ROOT/'source-art'))
from compact_lods import export_lods
metadata['lod_triangles']={'LOD0':triangles,**export_lods(meshes,TARGET)}
(OUT/'compact-v1-geometry.json').write_text(json.dumps(metadata,indent=2))

# Actual model review, with consistent studio lighting and no painted-over art.
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True
scene.render.resolution_x=1600;scene.render.resolution_y=1050;scene.render.resolution_percentage=100
scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.65,.72,.76,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.25
floor_mat=material('Review floor',(.68,.65,.54),0,.85);box('Review floor',(0,0,-.075),(200,200,.10),floor_mat,0)
def area(name,loc,power,size,color):
    data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size;data.color=color
    o=bpy.data.objects.new(name,data);scene.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,.8))-o.location).to_track_quat('-Z','Y').to_euler()
area('Large warm key',(-3,4,6),450,5,(1,.90,.77));area('Cool fill',(4,1,3),180,4,(.70,.86,1));area('Roof rim',(1,-4,4),350,3,(1,.96,.86))
data=bpy.data.cameras.new('Review camera');cam=bpy.data.objects.new('Review camera',data);scene.collection.objects.link(cam);scene.camera=cam;data.type='ORTHO';data.ortho_scale=4.9
scene.view_settings.view_transform='AgX'
for label,loc in [('front-quarter',(4.4,6.1,3.25)),('rear-quarter',(-4.6,-6.0,3.0)),('side',(6,0,2.0))]:
    cam.location=loc;cam.rotation_euler=(Vector((0,0,.80))-cam.location).to_track_quat('-Z','Y').to_euler()
    scene.render.filepath=str(OUT/('compact-v1-'+label+'.png'));bpy.ops.render.render(write_still=True)
print('COMPACT_MASTER_EXPORTED',json.dumps(metadata))
