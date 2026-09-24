"""Read-only mesh/LOD inventory. Run Blender with --disable-autoexec."""
import bpy
import json
import sys
from pathlib import Path
from mathutils import Vector

output = Path(sys.argv[sys.argv.index("--") + 1])
records = []
for obj in bpy.data.objects:
    if obj.type != "MESH":
        continue
    obj.data.calc_loop_triangles()
    bounds = [obj.matrix_world @ Vector(point) for point in obj.bound_box]
    records.append({
        "object": obj.name,
        "vertices": len(obj.data.vertices),
        "triangles": len(obj.data.loop_triangles),
        "dimensions": list(obj.dimensions),
        "worldMin": [min(p[axis] for p in bounds) for axis in range(3)],
        "worldMax": [max(p[axis] for p in bounds) for axis in range(3)],
        "materials": [slot.material.name if slot.material else None for slot in obj.material_slots],
        "collections": [collection.name for collection in obj.users_collection],
    })
result = {"source": bpy.data.filepath, "blender": bpy.app.version_string,
          "scope": "Source mesh inventory; no embedded scripts executed or source saved",
          "meshes": records}
output.write_text(json.dumps(result, indent=2), encoding="utf-8")
print("LITTLE_ROAD_MODEL_INVENTORY", str(output), "meshes=", len(records))
