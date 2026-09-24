extends SceneTree

func _initialize() -> void: call_deferred("run")

func mesh_bounds(model: Node3D) -> AABB:
	var bounds:=AABB()
	var first:=true
	for item in model.find_children("*","MeshInstance3D",true,false):
		var node: MeshInstance3D=item
		var local: Transform3D=model.global_transform.affine_inverse()*node.global_transform
		var box: AABB=local*node.mesh.get_aabb()
		if first:
			bounds=box
			first=false
		else: bounds=bounds.merge(box)
	return bounds

func run() -> void:
	var model: Node3D=LittleCar.CAR_MODEL.instantiate()
	root.add_child(model)
	var bounds:=mesh_bounds(model)
	print("CAR_VISIBLE_BOUNDS ",bounds," size=",bounds.size)
	var steering_bounds:=bounds
	for steer in [-.3,0,.3]:
		for node in model.find_children("Wheel_F*","Node3D",true,false): node.rotation.y=steer
		steering_bounds=steering_bounds.merge(mesh_bounds(model))
	print("CAR_STEERING_BOUNDS ",steering_bounds," size=",steering_bounds.size)
	var output:=FileAccess.open(ProjectSettings.globalize_path("res://../review/car-bounds.json"),FileAccess.WRITE)
	output.store_string(JSON.stringify({"visible":{"min":str(bounds.position),"max":str(bounds.end),"size":str(bounds.size)},"steered":{"min":str(steering_bounds.position),"max":str(steering_bounds.end),"size":str(steering_bounds.size)},"current_collision_size":"(1.6, 1.15, 2.75)"},"  "))
	output.close()
	quit()
