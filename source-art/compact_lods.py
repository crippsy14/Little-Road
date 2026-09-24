"""Derive compact LODs while preserving single-pane glazing exactly."""
import bpy, bmesh, json
from pathlib import Path

def export_lods(meshes, target, name='compact_v1'):
    sources={o:o.data for o in meshes}
    counts={}
    for level,ratio in [(1,.48),(2,.22)]:
        for o,source in sources.items():
            o.data=source.copy()
            glass_slots={i for i,m in enumerate(o.data.materials) if m and 'Glass' in m.name}
            pane=None
            if glass_slots:
                # Extract panes before simplification. Reducing a single-polygon
                # pane can alter its outline/normal and expose the cabin at LOD swaps.
                pane=bpy.data.objects.new(o.name+'_PreservedGlass',source.copy())
                bpy.context.collection.objects.link(pane);pane.matrix_world=o.matrix_world.copy()
                bm=bmesh.new();bm.from_mesh(pane.data)
                bmesh.ops.delete(bm,geom=[f for f in bm.faces if f.material_index not in glass_slots],context='FACES')
                bm.to_mesh(pane.data);bm.free()
                bm=bmesh.new();bm.from_mesh(o.data)
                bmesh.ops.delete(bm,geom=[f for f in bm.faces if f.material_index in glass_slots],context='FACES')
                bm.to_mesh(o.data);bm.free()
            bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
            mod=o.modifiers.new('Distance reduction','DECIMATE');mod.ratio=ratio;mod.use_collapse_triangulate=True
            bpy.ops.object.modifier_apply(modifier=mod.name)
            # Recompute face-area-weighted normals after topology changes;
            # interpolated pre-reduction corner normals made the roof look dented.
            mod=o.modifiers.new('Reduced panel normals','WEIGHTED_NORMAL');mod.keep_sharp=True;mod.weight=50
            bpy.ops.object.modifier_apply(modifier=mod.name)
            if pane:
                pane.select_set(True);bpy.ops.object.join()
        counts['LOD'+str(level)]=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes)
        bpy.ops.export_scene.fbx(filepath=str(target/(name+'_lod'+str(level)+'.fbx')),
            use_selection=False,object_types={'MESH','EMPTY'},apply_unit_scale=True,bake_space_transform=True,
            axis_forward='-Z',axis_up='Y',add_leaf_bones=False,use_mesh_modifiers=True,bake_anim=False)
        for o,source in sources.items():o.data=source
    return counts

if __name__=='__main__':
    root=Path(__file__).resolve().parents[1]
    bpy.ops.wm.open_mainfile(filepath=str(root/'source-art/masters/compact_v1.blend'))
    counts=export_lods([o for o in bpy.context.scene.objects if o.type=='MESH'],root/'unity/Assets/Resources/Cars')
    path=root/'review/unity-03/compact-v1-geometry.json';metadata=json.loads(path.read_text())
    metadata['lod_triangles'].update(counts);path.write_text(json.dumps(metadata,indent=2))
    print('COMPACT_LODS_EXPORTED',counts)
