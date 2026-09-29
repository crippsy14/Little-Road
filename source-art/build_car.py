"""Original Little Road car. Run with Blender --background --python this_file.

The .blend remains editable; glTF contains independent body and wheel meshes.
Blender +Y is the nose; the exported Godot asset faces -Z.
"""
from pathlib import Path
import math
import bpy
import bmesh
import sys
import json
import itertools
from mathutils import Vector, Matrix

ROOT = Path(__file__).resolve().parents[1]
# The coupe, utility and formula variants were retired placeholders (2026-09-28).
ASSET = 'little_car'
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

def mat(name, color, metal=0, rough=.45):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    p = m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value = (*color, 1)
    p.inputs['Metallic'].default_value = metal
    p.inputs['Roughness'].default_value = rough
    return m

paint = mat('BodyPaint', (.065, .48, .44), .15, .3)
glass = mat('MidnightGlass', (.043, .085, .12), .35, .2)
rubber = mat('TyreRubber', (.035, .042, .05), 0, .85)
trim = mat('CharcoalTrim', (.11, .14, .16), .15, .45)
silver = mat('WarmAlloy', (.61, .63, .62), .65, .26)
lamp = mat('WarmHeadlamp', (.98, .91, .66), .1, .2)
red = mat('TailLight', (.72, .06, .025), .15, .25)
amber = mat('Amber', (.95, .38, .045), .1, .3)
plate = mat('NumberPlate', (.9, .89, .76), 0, .6)

def finish(obj, name, material, bevel=0):
    obj.name = name
    obj.data.materials.append(material)
    if bevel:
        mod = obj.modifiers.new('Soft manufactured edges', 'BEVEL')
        mod.width = bevel
        mod.segments = 5
        obj.modifiers.new('Weighted normals', 'WEIGHTED_NORMAL')
    return obj

def box(name, loc, scale, material, bevel=.04):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj=bpy.context.object
    obj.dimensions=scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(obj,name,material,bevel)

def mesh(name, verts, faces, material, bevel=0):
    data=bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    if len(faces)>1:
        bm=bmesh.new()
        bm.from_mesh(data)
        bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
        bm.to_mesh(data)
        bm.free()
    data.update()
    obj=bpy.data.objects.new(name,data)
    bpy.context.collection.objects.link(obj)
    return finish(obj,name,material,bevel)

def cylinder(name, loc, radius, depth, material, rot=(0,0,0), vertices=24):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=rot)
    obj=finish(bpy.context.object,name,material,.016)
    return obj

# Shaped hull rings: y, half-width, lower sill, shoulder height.
profiles=[(-1.45,.68,.4,.87),(-1.3,.83,.32,.99),(-.7,.88,.32,1.03),(.6,.88,.32,.97),(1.25,.79,.39,.88),(1.47,.64,.48,.81)]
verts=[]
for y,w,lo,hi in profiles:
    verts.extend([(-w*.89,y,lo),(w*.89,y,lo),(w,y,hi-.12),(w*.91,y,hi),(-w*.91,y,hi),(-w,y,hi-.12)])
faces=[tuple(range(5,-1,-1))]
for r in range(len(profiles)-1):
    for i in range(6): faces.append((r*6+i,r*6+(i+1)%6,(r+1)*6+(i+1)%6,(r+1)*6+i))
faces.append(tuple(range((len(profiles)-1)*6,len(profiles)*6)))
mesh('SculptedBody',verts,faces,paint,.13)

# Cabin: lower ring and narrower roof ring, genuinely sloping front/rear.
verts=[(-.76,-1.17,.96),(.76,-1.17,.96),(.78,.69,.95),(-.78,.69,.95),(-.61,-.91,1.63),(.61,-.91,1.63),(.62,.2,1.64),(-.62,.2,1.64)]
mesh('CabinShell',verts,[(0,1,2,3),(0,4,5,1),(1,5,6,2),(2,6,7,3),(3,7,4,0),(4,7,6,5)],paint,.13)
mesh('Windshield',[(-.64,.646,1.06),(.64,.646,1.06),(.54,.26,1.565),(-.54,.26,1.565)],[(0,1,2,3)],glass,.013)
mesh('RearWindow',[(-.64,-1.139,1.06),(-.535,-.944,1.54),(.535,-.944,1.54),(.64,-1.139,1.06)],[(0,1,2,3)],glass,.014)
for side in [-1,1]:
    # Windows lie slightly outside the cabin to avoid z-fighting.
    def window(n, coords):
        v=[(side*x,y,z) for x,y,z in coords]
        f=(0,1,2,3) if side<0 else (3,2,1,0)
        mesh(n,v,[f],glass,.01)
    window('FrontSideGlass',[(.766,-.30,1.07),(.768,.565,1.05),(.633,.18,1.55),(.628,-.30,1.55)])
    window('RearSideGlass',[(.759,-1.06,1.075),(.764,-.40,1.07),(.628,-.40,1.55),(.621,-.86,1.55)])
    box('DoorHandle',(side*.861,-.37,.93),(.045,.18,.045),trim,.018)
    box('WingMirror',(side*.955,.43,1.09),(.24,.2,.13),paint,.055)
    box('MirrorGlass',(side*1.08,.415,1.09),(.012,.135,.075),glass,.01)
    box('SillTrim',(side*.864,-.02,.43),(.045,1.58,.11),trim,.03)
    cylinder('FrontHeadlamp',(side*.55,1.399,.78),.17,.055,lamp,(math.pi/2,0,0))
    cylinder('HeadlightRim',(side*.55,1.369,.78),.2,.065,trim,(math.pi/2,0,0))
    box('RearLamp',(side*.62,-1.455,.85),(.19,.085,.25),red,.065)
    box('RearReverseLamp',(side*.62,-1.502,.80),(.14,.015,.065),lamp,.015)
    box('Indicator',(side*.854,.87,.91),(.023,.115,.055),amber,.018)

box('FrontBumper',(0,1.438,.50),(1.38,.18,.22),trim,.07)
box('Grille',(0,1.479,.72),(.65,.04,.12),trim,.04)
box('FrontPlate',(0,1.54,.48),(.4,.02,.105),plate,.015)
box('RearBumper',(0,-1.438,.46),(1.47,.18,.23),trim,.065)
box('RearPlate',(0,-1.54,.48),(.43,.02,.11),plate,.015)
box('HatchHandle',(0,-1.467,.90),(.22,.045,.05),trim,.02)
box('RoofLip',(0,-.92,1.64),(1.25,.12,.06),paint,.035)

# Wheel pivots are separate named empties; meshes parented with local transforms.
for side,side_name in [(-1,'L'),(1,'R')]:
    for y,end in [( .90,'F'),(-.91,'R')]:
        loc=Vector((side*.84,y,.43))
        pivot=bpy.data.objects.new('Wheel_'+end+side_name,None)
        bpy.context.collection.objects.link(pivot)
        pivot.location=loc
        components=[]
        components.append(cylinder('Tyre',loc,.425,.255,rubber,(0,math.pi/2,0),32))
        outer=loc+Vector((side*.139,0,0))
        components.append(cylinder('AlloyRim',outer,.285,.025,silver,(0,math.pi/2,0)))
        components.append(cylinder('DarkInset',outer+Vector((side*.017,0,0)),.218,.014,trim,(0,math.pi/2,0)))
        components.append(cylinder('Hub',outer+Vector((side*.038,0,0)),.08,.035,silver,(0,math.pi/2,0)))
        for a in range(5):
            angle=a*math.tau/5
            center=outer+Vector((side*.037,math.sin(angle)*.14,math.cos(angle)*.14))
            spoke=box('Spoke',center,(.026,.074,.23),silver,.015)
            spoke.rotation_euler.x=-angle
            components.append(spoke)
        for obj in components:
            world=obj.matrix_world.copy()
            obj.parent=pivot
            obj.matrix_world=world

# Join non-wheel body pieces to reduce runtime draw calls by material.
bpy.ops.object.select_all(action='DESELECT')
body_parts=[o for o in bpy.context.scene.objects if o.type=='MESH' and o.parent is None]
for o in body_parts:
    o.select_set(True)
bpy.context.view_layer.objects.active=body_parts[0]
bpy.ops.object.convert(target='MESH')
bpy.ops.object.join()
bpy.context.object.name='Body'

# Apply shading after bevels; retain large panel normals but soften curved edges.
for obj in bpy.context.scene.objects:
    if obj.type=='MESH':
        for polygon in obj.data.polygons: polygon.use_smooth=True

# Bake the convex hull once rather than feeding render vertices to physics at
# runtime. Include front-wheel steering extremes and keep the editable sources.
bpy.context.view_layer.update()
points=[]
for angle in [-.30,0,.30]:
    for obj in bpy.context.scene.objects:
        if obj.name.startswith('Wheel_F'): obj.rotation_euler.z=angle
    bpy.context.view_layer.update()
    depsgraph=bpy.context.evaluated_depsgraph_get()
    for obj in bpy.context.scene.objects:
        if obj.type!='MESH': continue
        evaluated=obj.evaluated_get(depsgraph)
        mesh_data=evaluated.to_mesh()
        points.extend([evaluated.matrix_world @ v.co for v in mesh_data.vertices])
        evaluated.to_mesh_clear()
for obj in bpy.context.scene.objects:
    if obj.name.startswith('Wheel_F'): obj.rotation_euler.z=0
# Intersect 26 supporting planes to make a small, conservative outer hull.
# All source vertices lie inside these planes, including the steering envelope.
# Unlike inward mesh decimation, this never shrinks through the rendered model.
planes=[]
for direction in itertools.product([-1,0,1],repeat=3):
    if direction==(0,0,0): continue
    normal=Vector(direction).normalized()
    planes.append((normal,max(normal.dot(point) for point in points)+.002))
hull_points=set()
for selected in itertools.combinations(planes,3):
    matrix=Matrix([entry[0] for entry in selected])
    if abs(matrix.determinant())<.0001: continue
    point=matrix.inverted() @ Vector([entry[1] for entry in selected])
    if all(normal.dot(point)<=distance+.0001 for normal,distance in planes):
        hull_points.add(tuple(round(v,5) for v in point))
hull=[[p[0],p[2],-p[1]] for p in sorted(hull_points)]
bpy.context.view_layer.update()
bpy.context.scene.world.color=(.3,.3,.3)
ROOT.joinpath('game/assets').mkdir(parents=True,exist_ok=True)
(ROOT/'game/assets'/f'{ASSET}_hull.json').write_text(json.dumps(hull))
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'source-art'/f'{ASSET}.blend'))
bpy.ops.export_scene.gltf(filepath=str(ROOT/'game/assets'/f'{ASSET}.glb'),export_format='GLB',export_apply=True)
print('LITTLE_ROAD_CAR_EXPORTED',ASSET,'hull vertices',len(hull))
