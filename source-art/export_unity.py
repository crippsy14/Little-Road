"""Export the preserved Blender prototypes into Unity without modifying sources."""
from pathlib import Path
import bpy
from mathutils import Vector

root = Path(__file__).resolve().parents[1]
target = root / 'unity/Assets/Resources/Cars'
target.mkdir(parents=True, exist_ok=True)
for name in ['little_car']:  # coupe/utility/formula retired 2026-09-28
    bpy.ops.wm.open_mainfile(filepath=str(root / 'source-art' / (name + '.blend')))
    # The preserved prototypes shared one material between front and reversing
    # lenses. Give rear-facing lamp geometry its own identity without changing
    # vertices, pivots or the preserved Blender sources.
    reverse=bpy.data.materials.new('ReverseLight');reverse.diffuse_color=(.70,.75,.76,1)
    changed=0
    for obj in bpy.context.scene.objects:
        if obj.type!='MESH':continue
        slots={i for i,m in enumerate(obj.data.materials) if m and 'Headlamp' in m.name}
        if not slots:continue
        target_slot=len(obj.data.materials);obj.data.materials.append(reverse)
        for face in obj.data.polygons:
            centre=sum((obj.matrix_world@obj.data.vertices[i].co for i in face.vertices),Vector())/len(face.vertices)
            if face.material_index in slots and centre.y<0:face.material_index=target_slot;changed+=1
    print('REVERSE_LENS_FACES',name,changed)
    # Blender +Y nose becomes Unity +Z. FBX carries units, independent wheels,
    # material identities and transforms. Unity importer applies axis conversion.
    bpy.ops.export_scene.fbx(filepath=str(target / (name + '.fbx')),
        use_selection=False, object_types={'MESH', 'EMPTY'},
        apply_unit_scale=True, bake_space_transform=True,
        axis_forward='-Z', axis_up='Y', add_leaf_bones=False,
        use_mesh_modifiers=True, bake_anim=False)
    print('UNITY_FBX_EXPORTED', name)
