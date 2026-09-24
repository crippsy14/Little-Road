"""Derive a 24 m cliff section and three LODs from the credited CC0 source.

Run with --disable-autoexec; never overwrites the vendor blend file.
"""
import bpy
import bmesh
import json
import sys
from pathlib import Path
from mathutils import Vector

target = Path(sys.argv[sys.argv.index("--") + 1]).resolve()
target.mkdir(parents=True, exist_ok=True)
records = []
for level, source_level, ratio in [(0, 2, 1.0), (1, 3, 1.0), (2, 3, .24)]:
    source = bpy.data.objects[f"coastal_cliff_01_LOD{source_level}"]
    obj = source.copy()
    obj.data = source.data.copy()
    bpy.context.scene.collection.objects.link(obj)
    obj.name = f"cliff_section_{level}"
    obj.hide_set(False)
    obj.hide_viewport = False
    obj.hide_render = False
    obj.data.transform(obj.matrix_world)
    obj.matrix_world.identity()
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    for x, inner, outer in [(-12, True, False), (12, False, True)]:
        bmesh.ops.bisect_plane(bm, geom=list(bm.verts) + list(bm.edges) + list(bm.faces),
                              plane_co=(x, 0, 0), plane_no=(1, 0, 0),
                              clear_inner=inner, clear_outer=outer, dist=.00001)
    bm.to_mesh(obj.data)
    bm.free()
    for vertex in obj.data.vertices:
        vertex.co.z += .65
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    if ratio < 1:
        decimate = obj.modifiers.new("Study distant simplification", "DECIMATE")
        decimate.ratio = ratio
        bpy.ops.object.modifier_apply(modifier=decimate.name)
    obj.data.calc_loop_triangles()
    facing = Vector((0, 0, 0))
    for face in obj.data.polygons:
        if abs(face.normal.z) < .65:
            facing += face.normal * face.area
    records.append({"lod": level, "sourceLOD": source_level,
                    "vertices": len(obj.data.vertices),
                    "triangles": len(obj.data.loop_triangles),
                    "dominantHorizontalNormal": list(facing.normalized()),
                    "file": f"cliff_scan_lod{level}.fbx"})
    bpy.ops.export_scene.fbx(filepath=str(target / f"cliff_scan_lod{level}.fbx"),
                             use_selection=True, object_types={"MESH"},
                             axis_forward="-Z", axis_up="Y", bake_anim=False,
                             add_leaf_bones=False, path_mode="STRIP",
                             use_mesh_modifiers=True)
    bpy.data.objects.remove(obj, do_unlink=True)
(target / "cliff-study-export.json").write_text(json.dumps({
    "source": bpy.data.filepath,
    "changes": "Clip source X to [-12,12] metres, retain UVs, raise base 0.65m; simplify distant LOD only",
    "lods": records}, indent=2), encoding="utf-8")
print("LITTLE_ROAD_CLIFF_STUDY_EXPORTED", json.dumps(records))
