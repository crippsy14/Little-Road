extends CharacterBody3D
class_name LittleCar

const CAR_MODEL = preload("res://assets/little_car.glb")
const BODY_ASSETS := {"compact":"little_car","utility":"utility_car","coupe":"coupe_car","formula":"formula_car"}
static var shared_hulls: Dictionary={}
var body_style: String="compact"
var road_world: RoadWorld
var model: Node3D
var speed := 0.0
var steering := 0.0
var engine_level := 0
var trail_tyres := false
var controlled := true
var scripted := false
var scripted_throttle := 0.0
var scripted_steer := 0.0
var surface_name := "TARMAC"
var wheel_nodes: Array[Node3D] = []
var total_driven := 0.0
var reset_position := Vector3(-25,9,35)
var impact_cooldown := 0.0
var paint_color := Color("389d95")
var tuning: Dictionary = PartsCatalog.stats(PartsCatalog.stock())
var driving_enabled := true
var lateral_contact := false
var collision: CollisionShape3D
var previous_contacts: Array[int]=[]
var impact_events:=0
var last_impact: Dictionary={}

func _ready() -> void:
	name = "PlayerCar"
	collision_layer=2
	collision_mask=3
	floor_snap_length = 1.0
	floor_max_angle = deg_to_rad(50)
	var asset: String=BODY_ASSETS.get(body_style,"little_car")
	model = load("res://assets/"+asset+".glb").instantiate()
	add_child(model)
	for node in model.find_children("Wheel_*", "Node3D", true, false):
		wheel_nodes.append(node)
	collision=CollisionShape3D.new()
	collision.name="VisibleCarHull"
	if not shared_hulls.has(asset):
		var hull:=ConvexPolygonShape3D.new()
		var points:=PackedVector3Array()
		var data=JSON.parse_string(FileAccess.get_file_as_string("res://assets/"+asset+"_hull.json"))
		for point in data: points.append(Vector3(point[0],point[1],point[2]))
		hull.points=points
		hull.margin=.01
		shared_hulls[asset]=hull
	collision.shape=shared_hulls[asset]
	add_child(collision)
	set_paint(paint_color)

func build_visible_hull() -> ConvexPolygonShape3D:
	var unique: Dictionary={}
	# Include the full steering envelope, not just the neutral wheel positions.
	for turn in [-.30,0.0,.30]:
		for wheel in wheel_nodes:
			if String(wheel.name).begins_with("Wheel_F"): wheel.rotation.y=turn
		for item in model.find_children("*","MeshInstance3D",true,false):
			var mesh_node: MeshInstance3D=item
			var local: Transform3D=model.global_transform.affine_inverse()*mesh_node.global_transform
			for surface in range(mesh_node.mesh.get_surface_count()):
				var vertices: PackedVector3Array=mesh_node.mesh.surface_get_arrays(surface)[Mesh.ARRAY_VERTEX]
				for vertex in vertices: unique[local*vertex]=true
	for wheel in wheel_nodes:
		if String(wheel.name).begins_with("Wheel_F"): wheel.rotation.y=0
	var points:=PackedVector3Array()
	for point in unique: points.append(point)
	var hull:=ConvexPolygonShape3D.new()
	hull.points=points
	hull.margin=.01
	return hull

func set_paint(color: Color) -> void:
	paint_color = color
	if not model: return
	for node in model.find_children("*", "MeshInstance3D", true, false):
		var mesh: MeshInstance3D = node
		for i in range(mesh.mesh.get_surface_count()):
			var source := mesh.mesh.surface_get_material(i)
			if source and source.resource_name.begins_with("BodyPaint"):
				var m: StandardMaterial3D = source.duplicate()
				m.albedo_color = color
				mesh.set_surface_override_material(i,m)

func _physics_process(delta: float) -> void:
	if not controlled: return
	if not driving_enabled:
		velocity=Vector3(0,-2,0)
		speed=0
		move_and_slide()
		return
	var throttle := scripted_throttle if scripted else Input.get_axis("brake","accelerate")
	var turn := scripted_steer if scripted else Input.get_axis("right","left")
	var handbrake := false if scripted else Input.is_action_pressed("handbrake")
	var near := road_world.nearest_road(global_position)
	var on_dirt: bool = near.offset > 5.1
	surface_name = "TRAIL" if on_dirt else "TARMAC"
	var grip: float=tuning.dirt_grip if on_dirt else tuning.road_grip
	var top_speed: float=tuning.gear_ceiling
	if handbrake: grip *= .32
	steering = move_toward(steering,turn,tuning.response*delta)
	var steer_rate: float = lerpf(1.15,.72,clampf(abs(speed)/30.0,0,1))*tuning.yaw
	var yaw_request:=steering*steer_rate*clampf(speed/5.0,-1,1)
	var grade:=0.0
	if is_on_floor():
		var floor_normal:=get_floor_normal()
		var rise: float=-floor_normal.dot(-global_basis.z)/maxf(.1,floor_normal.y)
		grade=9.81*rise/sqrt(1+rise*rise)
	var motion:=DriveModel.step(tuning,speed,throttle,yaw_request,grip,grade,delta)
	speed=motion.speed
	if handbrake: speed=move_toward(speed,0,8*delta)
	rotation.y += float(motion.yaw)*delta
	var target := -global_basis.z*speed
	velocity.x = lerpf(velocity.x,target.x,1.0-exp(-grip*delta))
	velocity.z = lerpf(velocity.z,target.z,1.0-exp(-grip*delta))
	velocity.y -= 22.0*delta
	if is_on_floor(): velocity.y = -2.0
	var before := global_position
	var incoming_velocity:=velocity
	move_and_slide()
	total_driven += Vector2(global_position.x-before.x,global_position.z-before.z).length()
	impact_cooldown = maxf(0,impact_cooldown-delta)
	# Triangle joints can set the wall flag while every reported normal is a floor.
	# Only a genuinely lateral contact should apply the collision speed penalty.
	lateral_contact=false
	var contacts: Array[int]=[]
	var strongest_closing:=0.0
	var impact_normal:=Vector3.ZERO
	var impacted_car:=false
	for contact in range(get_slide_collision_count()):
		var hit:=get_slide_collision(contact)
		if absf(hit.get_normal().y)<cos(floor_max_angle):
			lateral_contact=true
			var collider_id:=int(hit.get_collider_id())
			contacts.append(collider_id)
			var closing:=maxf(0,-(incoming_velocity-hit.get_collider_velocity()).dot(hit.get_normal()))
			if not previous_contacts.has(collider_id) and closing>strongest_closing:
				strongest_closing=closing
				impact_normal=hit.get_normal()
				impacted_car=hit.get_collider() is LittleCar
	if strongest_closing>1 and impact_cooldown<=0:
		var facing:=(-global_basis.z).dot(impact_normal)
		var loss:=minf(absf(speed)*.80,strongest_closing*lerpf(.18,.68,absf(facing)))
		if impacted_car and facing>.5:
			# A rear strike transfers a small amount of forward motion to the victim.
			speed=minf(top_speed,speed+strongest_closing*.15)
			loss=0
		else:
			speed=signf(speed)*maxf(0,absf(speed)-loss)
		impact_cooldown=.6
		impact_events+=1
		last_impact={"closing_speed":strongest_closing,"speed_loss":loss,"rear_strike":impacted_car and facing>.5}
	previous_contacts=contacts
	var normal := get_floor_normal() if is_on_floor() else Vector3.UP
	var local_normal := global_basis.inverse()*normal
	model.rotation.x = lerp_angle(model.rotation.x,atan2(local_normal.z,local_normal.y),delta*6)
	model.rotation.z = lerp_angle(model.rotation.z,-steering*clampf(abs(speed)/25.0,0,1)*.065-atan2(local_normal.x,local_normal.y),delta*6)
	collision.transform=model.transform
	for wheel in wheel_nodes:
		wheel.rotation.x -= speed*delta/.425
		if String(wheel.name).begins_with("Wheel_F"): wheel.rotation.y = steering*.30
	if global_position.y < -2 or abs(global_position.x)>430 or abs(global_position.z)>460:
		reset_to_road()
	if not scripted and Input.is_action_just_pressed("reset"): reset_to_road()

func reset_to_road() -> void:
	var near := road_world.nearest_road(global_position)
	var pose := road_world.sample_road(near.distance)
	global_position = pose.origin+Vector3.UP*.5
	rotation = Vector3(0,pose.basis.get_euler().y,0)
	velocity = Vector3.ZERO
	speed = 0

func place_on_road(distance: float) -> void:
	var pose := road_world.sample_road(distance)
	global_position = pose.origin+Vector3.UP*.35
	rotation = Vector3(0,pose.basis.get_euler().y,0)
	velocity = Vector3.ZERO
	speed = 0
	steering = 0
	scripted_steer = 0
	scripted_throttle = 0
	impact_cooldown = 0
	lateral_contact = false
	previous_contacts.clear()
	impact_events=0
	last_impact={}
