"""Original engine catalogue models; presentation only, tuning stays in parts.json."""
from pathlib import Path
import bpy, math, json

ROOT=Path(__file__).resolve().parents[1]
TARGET=ROOT/'unity/Assets/Resources/Parts'
TARGET.mkdir(parents=True,exist_ok=True)

def material(name,color,metal,rough):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1)
    p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
    return m

def finish(o,name,mat,r=.015):
    o.name=name;o.data.materials.append(mat)
    for p in o.data.polygons:p.use_smooth=True
    if r:
        mod=o.modifiers.new('Cast edge radius','BEVEL');mod.width=r;mod.segments=3
        mod=o.modifiers.new('Machined normals','WEIGHTED_NORMAL');mod.keep_sharp=True
    return o

def box(name,loc,size,mat,r=.015):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.dimensions=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return finish(o,name,mat,r)

def cylinder(name,loc,radius,depth,mat,axis='Z'):
    rotation=(math.pi/2,0,0) if axis=='Y' else (0,math.pi/2,0) if axis=='X' else (0,0,0)
    bpy.ops.mesh.primitive_cylinder_add(vertices=32,radius=radius,depth=depth,location=loc,rotation=rotation)
    return finish(bpy.context.object,name,mat,.008)

def pipe(name,points,mat,r=.022):
    data=bpy.data.curves.new(name,'CURVE');data.dimensions='3D';data.resolution_u=6;data.bevel_depth=r;data.bevel_resolution=2
    s=data.splines.new('BEZIER');s.bezier_points.add(len(points)-1)
    for p,co in zip(s.bezier_points,points):p.co=co;p.handle_left_type='AUTO';p.handle_right_type='AUTO'
    o=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(o);o.data.materials.append(mat)

records=[]
for tier,part in enumerate(['engine_stock','engine_workshop','engine_club']):
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    cast=material('EngineCast',(.30,.34,.35),.55,.45)
    machined=material('EngineAlloy',(.65,.69,.69),.7,.3)
    dark=material('EngineRubber',(.035,.048,.054),0,.75)
    cover=material('EngineCover',[ (.19,.27,.29),(.09,.46,.39),(.77,.22,.12) ][tier],.25,.3)
    brass=material('EngineBrass',(.58,.40,.16),.65,.35)
    width=.43+tier*.035;length=.60+tier*.10
    box('Cast block',(0,0,.31),(width,length,.38),cast,.028)
    box('Sump',(0,0,.105),(width*.88,length*.91,.12),dark,.025)
    box('Cylinder head',(0,0,.545),(width*1.07,length*1.04,.105),machined,.018)
    box('Coloured cam cover',(0,0,.633),(width*.91,length*.96,.085),cover,.022)
    for i in range(4):
        y=-length*.35+i*length*.7/3
        cylinder('Cam cover fixing',(-width*.36,y,.681),.017,.008,machined)
        cylinder('Cam cover fixing',(width*.36,y,.681),.017,.008,machined)
        pipe('Exhaust runner',[(width*.5,y,.52),(width*.70,y,.49),(width*.80,y,.31),(width*.90,0,.23)],machined)
        pipe('Intake runner',[(-width*.5,y,.52),(-width*.8,y,.58),(-width*.95,y,.64)],cast,.027)
    for x in [-.08,0,.08]:box('Cam cover rib',(x,0,.683),(.009,length*.63,.014),machined,.003)
    cylinder('Oil filler',(.08,-length*.32,.70),.036,.036,dark)
    box('Intake plenum',(-width*.92,0,.65),(.12,length*.80,.12),cover,.035)
    cylinder('Crank pulley',(0,length*.53,.29),.13,.044,dark,'Y')
    cylinder('Pulley centre',(0,length*.56,.29),.055,.014,machined,'Y')
    cylinder('Alternator',(width*.56,length*.32,.38),.095,.15,machined,'Y')
    cylinder('Alternator pulley',(width*.56,length*.47,.38),.058,.022,dark,'Y')
    pipe('Auxiliary belt',[(0,length*.57,.16),(width*.65,length*.57,.32),(width*.56,length*.57,.44),(-.10,length*.57,.38),(0,length*.57,.16)],dark,.01)
    if tier==0:
        cylinder('Round air cleaner',(-.20,-.04,.79),.14,.09,dark)
        cylinder('Air cleaner lid',(-.20,-.04,.842),.136,.012,machined)
    elif tier==1:
        pipe('Workshop intake',[(-.43,0,.67),(-.50,-.12,.71),(-.50,-.26,.75)],dark,.045)
        cylinder('Workshop filter',(-.50,-.33,.75),.08,.15,cover,'Y')
        for y in [-.40,-.36,-.32,-.28]:cylinder('Filter pleat',(-.50,y,.75),.083,.01,machined,'Y')
    else:
        box('Club intake rail',(-.43,0,.70),(.16,length*.85,.08),brass,.012)
        for i in range(4):
            y=-length*.31+i*length*.62/3
            cylinder('Club intake trumpet',(-.43,y,.79),.050,.12,machined)
            cylinder('Trumpet dark bore',(-.43,y,.854),.039,.007,dark)
    for side in [-1,1]:box('Mounting lug',(side*(width*.5+.035),-.20,.19),(.09,.16,.07),cast,.014)
    bpy.ops.object.select_all(action='SELECT');bpy.context.view_layer.objects.active=next(o for o in bpy.context.scene.objects if o.type=='MESH')
    bpy.ops.object.convert(target='MESH');bpy.ops.object.join();o=bpy.context.object;o.name=part
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'source-art/masters'/f'{part}.blend'))
    bpy.ops.export_scene.fbx(filepath=str(TARGET/f'{part}.fbx'),use_selection=True,object_types={'MESH'},apply_unit_scale=True,bake_space_transform=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False)
    records.append({'part_id':part,'triangles':sum(len(p.vertices)-2 for p in o.data.polygons)})
(ROOT/'source-art/masters/engine-previews.json').write_text(json.dumps(records,indent=2))
print('ENGINE_PREVIEWS_EXPORTED',json.dumps(records))
