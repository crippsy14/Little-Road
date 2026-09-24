extends Node
class_name CoastRace

signal player_finished(place: int, seconds: float)
signal field_finished
const CAR=preload("res://scripts/car_controller.gd")
const PARTS=preload("res://scripts/parts_catalog.gd")
const FIELD_SIZE:=24
const LAPS:=3
const GATES_PER_LAP:=8
var world: RoadWorld
var player: LittleCar
var cars: Array[LittleCar]=[]
var records: Array[Dictionary]=[]
var elapsed:=0.0
var countdown:=3.0
var active:=false
var suspended:=false
var last_position:=24
var gate_visual: Node3D
var autopilot_player:=false
var player_index:=23

func _ready() -> void:
	process_physics_priority=-20

func start(road_world: RoadWorld, player_car: LittleCar, entrant_count: int=FIELD_SIZE, driving_seed: int=0) -> void:
	clear()
	world=road_world
	player=player_car
	entrant_count=clampi(entrant_count,1,FIELD_SIZE)
	player_index=entrant_count-1
	countdown=3
	elapsed=0
	last_position=entrant_count
	active=true
	for i in range(entrant_count):
		var vehicle: LittleCar
		if i==player_index:
			vehicle=player
		else:
			vehicle=CAR.new()
			vehicle.road_world=world
			vehicle.scripted=true
			vehicle.tuning=PARTS.stats(rival_setup(i))
			vehicle.body_style="coupe" if i<6 else ("utility" if i%4==0 else "compact")
			get_parent().add_child(vehicle)
			vehicle.name="Rival_%02d" % (i+1)
			vehicle.set_paint(Color.from_hsv(fmod(i*.137,1),.45,.78))
		var distance: float=-float(i/2)*5.0-4.0
		var lane: float=-2.25 if i%2==0 else 2.25
		vehicle.place_on_road(distance)
		vehicle.position+=world.sample_road(distance).basis.x*lane
		vehicle.driving_enabled=false
		cars.append(vehicle)
		records.append({"gate":1,"progress":distance,"near":fposmod(distance,world.total_length),"lane":lane,"target_lane":lane,"lane_cooldown":0.0,"finish":-1.0,"stuck":0.0,"recoveries":0,"best_progress":distance,"previous":vehicle.position})
	configure_driving_variation(driving_seed)
	show_gate()

func configure_driving_variation(seed_value: int) -> void:
	for i in range(records.size()):
		var identity:=23 if i==player_index else i
		var rng:=RandomNumberGenerator.new()
		rng.seed=seed_value*7919+identity*104729
		var varied:=seed_value!=0
		records[i].pace=(.88+float(identity%4)*.025)*(rng.randf_range(.97,1.03) if varied else 1.0)
		records[i].braking_margin=rng.randf_range(.94,1.04) if varied else 1.0
		records[i].line_bias=rng.randf_range(-.12,.12) if varied else 0.0
		records[i].consistency=rng.randf_range(.008,.022) if varied else 0.0
		records[i].rhythm=rng.randf_range(0,TAU)
		records[i].following_time=rng.randf_range(.30,.48) if varied else .38
		records[i].passes_started=0
		records[i].passing_driver=-1
		records[i].pass_elapsed=0.0

func rival_setup(index: int) -> Dictionary:
	var fit:=PARTS.stock()
	# A fixed field. Purchases never cause these opponents to scale up.
	fit.steering="steering_quick"
	if index<18: fit.engine="engine_workshop"
	if index<11: fit.engine="engine_club"
	if index<20: fit.tyres="tyres_touring"
	if index<9: fit.tyres="tyres_sport"
	if index<15: fit.brakes="brakes_sport"
	if index<8: fit.chassis="chassis_light"
	if index<6: fit.transmission="transmission_long"
	return fit

func clear() -> void:
	for vehicle in cars:
		if is_instance_valid(vehicle) and vehicle!=player:
			vehicle.get_parent().remove_child(vehicle)
			vehicle.queue_free()
	cars.clear()
	records.clear()
	if is_instance_valid(gate_visual): gate_visual.queue_free()
	active=false
	if is_instance_valid(player): player.driving_enabled=true
	if is_instance_valid(player):
		player.collision_layer=2
		player.collision_mask=3

func _physics_process(delta: float) -> void:
	if not active: return
	for i in range(cars.size()):
		if i!=player_index: cars[i].controlled=not suspended
	if suspended: return
	if countdown>0:
		countdown=maxf(0,countdown-delta)
		for vehicle in cars: vehicle.driving_enabled=countdown==0
		return
	elapsed+=delta
	for i in range(cars.size()):
		var vehicle:=cars[i]
		var rec: Dictionary=records[i]
		if rec.finish>=0: continue
		var near:=world.nearest_road(vehicle.position)
		var advance:=wrapf(near.distance-float(rec.near),-world.total_length*.5,world.total_length*.5)
		rec.near=near.distance
		if absf(advance)<4.0: rec.progress+=advance
		var goal_distance:=float(rec.gate)*world.total_length/GATES_PER_LAP
		var gate_pose:=world.sample_road(goal_distance)
		var gate_forward: Vector3=-gate_pose.basis.z
		var before_side: float=(rec.previous-gate_pose.origin).dot(gate_forward)
		var after_side: float=(vehicle.position-gate_pose.origin).dot(gate_forward)
		var continuous: bool=vehicle.position.distance_to(rec.previous)<4.0
		if continuous and before_side<0 and after_side>=0 and vehicle.position.distance_to(gate_pose.origin)<8.0:
			rec.gate+=1
			if rec.gate>GATES_PER_LAP*LAPS:
				rec.finish=elapsed
				if i==player_index:
					var place:=1
					for other in records:
						if other.finish>=0 and other.finish<elapsed: place+=1
					last_position=place
					if is_instance_valid(gate_visual): gate_visual.queue_free()
					vehicle.driving_enabled=false
					vehicle.collision_layer=0
					vehicle.collision_mask=1
					player_finished.emit(place,elapsed)
				else:
					# Finishers leave the racing lane instead of obstructing later cars.
					vehicle.collision_layer=0
					vehicle.collision_mask=1
					vehicle.hide()
					vehicle.driving_enabled=false
			elif i==player_index: show_gate()
		rec.previous=vehicle.position
		if (i!=player_index or autopilot_player) and rec.finish<0: drive_rival(i,delta,near)
	if not player_has_finished(): last_position=position_of_player()
	if records.all(func(record): return record.finish>=0):
		active=false
		field_finished.emit()

func player_has_finished() -> bool:
	return not records.is_empty() and records[player_index].finish>=0

func standings() -> Array[int]:
	var order: Array[int]=[]
	for i in range(records.size()): order.append(i)
	order.sort_custom(func(a,b):
		var x: Dictionary=records[a]
		var y: Dictionary=records[b]
		if x.finish>=0 and y.finish>=0: return x.finish<y.finish if x.finish!=y.finish else a<b
		if x.finish>=0 or y.finish>=0: return x.finish>=0
		if x.gate!=y.gate: return x.gate>y.gate
		return x.progress>y.progress)
	return order

func drive_rival(index: int, delta: float, near: Dictionary) -> void:
	var vehicle:=cars[index]
	var rec: Dictionary=records[index]
	var speed:=absf(vehicle.speed)
	var lookahead:=8.0+speed*.42
	rec.lane_cooldown=maxf(0,float(rec.lane_cooldown)-delta)
	var wanted_lane: float=rec.target_lane
	# Slowly changing execution within the driver's sampled race form.
	var execution:=1.0+float(rec.consistency)*sin(elapsed*.24+float(rec.rhythm))
	var target_speed: float=vehicle.tuning.max_speed*float(rec.pace)*execution
	# Read bends ahead and plan braking from the same grip limits used by the car.
	for distance in [8.0,18.0,30.0,44.0]:
		var a:=world.sample_road(near.distance+distance)
		var b:=world.sample_road(near.distance+distance+8.0)
		var curvature:=a.basis.z.angle_to(b.basis.z)/8.0
		var corner_speed:=sqrt(vehicle.tuning.road_grip*1.42/maxf(curvature,.001))*float(rec.braking_margin)
		var reachable:=sqrt(corner_speed*corner_speed+2.0*minf(vehicle.tuning.braking,vehicle.tuning.road_grip*2.1)*maxf(0,distance-5))
		target_speed=minf(target_speed,reachable)
	var pose:=world.sample_road(near.distance)
	var forward: Vector3=-pose.basis.z
	var actual_lane: float=(vehicle.position-pose.origin).dot(pose.basis.x)
	if rec.passing_driver>=0:
		rec.pass_elapsed+=delta
		var passed: Dictionary=records[int(rec.passing_driver)]
		if passed.finish>=0 or rec.progress>float(passed.progress)+5:
			rec.passing_driver=-1
	var lead_index:=-1
	var lead_gap:=INF
	var lead_speed:=0.0
	rec.free_speed=target_speed
	rec.pass_safe=false
	for j in range(cars.size()):
		if j==index or records[j].finish>=0: continue
		var other:=cars[j]
		var other_pose:=world.sample_road(float(records[j].near))
		var ahead:=wrapf(float(records[j].near)-float(near.distance),-world.total_length*.5,world.total_length*.5)
		var lateral: float=(other.position-other_pose.origin).dot(other_pose.basis.x)-actual_lane
		if ahead>0 and ahead<maxf(15,speed*.8) and ahead<lead_gap and absf(lateral)<2.05:
			lead_index=j
			lead_gap=ahead
			lead_speed=maxf(0,other.velocity.dot(forward))
	if lead_index>=0:
		var candidate: float=rec.target_lane
		var safe:=false
		var best_space:=-INF
		# The road accommodates an inside, middle and outside line.
		# Choose usable space rather than blindly swapping between two queues.
		for possible_lane in [-2.65,0.0,2.65]:
			if absf(possible_lane-actual_lane)<1.2: continue
			var available:=true
			var clear_distance:=60.0
			for k in range(cars.size()):
				if k==index or records[k].finish>=0: continue
				var other_pose:=world.sample_road(float(records[k].near))
				var lane_other: float=(cars[k].position-other_pose.origin).dot(other_pose.basis.x)
				var next_lane:=move_toward(lane_other,float(records[k].target_lane),3.2)
				var longitudinal:=wrapf(float(records[k].near)-float(near.distance),-world.total_length*.5,world.total_length*.5)
				var projected:=longitudinal+(cars[k].velocity.dot(-other_pose.basis.z)-minf(target_speed,speed+3.0))
				var overlapping_now:=longitudinal> -3.8 and longitudinal<4.5
				var overlapping_soon:=projected> -3.8 and projected<4.5
				if (overlapping_now or overlapping_soon) and (absf(lane_other-possible_lane)<2.05 or absf(next_lane-possible_lane)<2.05): available=false
				if longitudinal>0 and absf(lane_other-possible_lane)<2.05: clear_distance=minf(clear_distance,longitudinal)
			var space_score:=clear_distance-absf(possible_lane-actual_lane)*.5
			if available and space_score>best_space:
				best_space=space_score
				candidate=possible_lane
				safe=true
		var physically_settled:=absf(actual_lane-float(rec.target_lane))<.55
		var committed_pass_done: bool=rec.passing_driver<0 or rec.pass_elapsed>8.0
		if safe and target_speed>lead_speed+1 and rec.lane_cooldown<=0 and physically_settled and committed_pass_done:
			wanted_lane=candidate
			rec.target_lane=candidate
			rec.lane_cooldown=3.0
			rec.passes_started+=1
			rec.passing_driver=lead_index
			rec.pass_elapsed=0.0
		rec.pass_safe=safe
		# Keep a physical gap while waiting for space or moving across the lane.
		var desired_gap:=3.5+speed*float(rec.following_time)
		var following_speed:=maxf(0,lead_speed+(lead_gap-desired_gap)*.85)
		target_speed=minf(target_speed,following_speed)
	rec.lane=move_toward(float(rec.lane),wanted_lane,delta*3.2)
	# Intervene before a traffic deflection carries the car into roadside scenery.
	var predicted:=world.nearest_road(vehicle.position+Vector3(vehicle.velocity.x,0,vehicle.velocity.z)*.35)
	var target_lane: float=rec.lane+rec.line_bias
	if near.offset>3.7 or predicted.offset>4.1:
		target_lane=0.0
		lookahead=5.0+speed*.2
		target_speed=minf(target_speed,10.0)
	var aim_pose:=world.sample_road(near.distance+lookahead)
	var aim: Vector3=aim_pose.origin+aim_pose.basis.x*target_lane-vehicle.position
	aim.y=0
	var angle:=(-vehicle.global_basis.z).signed_angle_to(aim.normalized(),Vector3.UP)
	vehicle.scripted_steer=clampf(angle*2.2,-1,1)
	if absf(angle)>.6: target_speed=minf(target_speed,12.0)
	vehicle.scripted_throttle=DriveModel.pedal_for_speed(vehicle.tuning,vehicle.speed,target_speed)
	rec.leader=lead_index
	rec.lead_gap=lead_gap if lead_index>=0 else -1.0
	rec.lead_speed=lead_speed
	rec.requested_speed=target_speed
	rec.actual_lane=(vehicle.position-pose.origin).dot(pose.basis.x)
	if rec.progress>float(rec.best_progress)+1.0:
		rec.best_progress=rec.progress
		rec.stuck=0.0
	else: rec.stuck+=delta
	if rec.stuck>6 or near.offset>18:
		# Recovery stays behind the required gate. It never grants checkpoints.
		var safe_distance:=minf(float(rec.progress),float(rec.gate)*world.total_length/GATES_PER_LAP-10)
		vehicle.place_on_road(safe_distance)
		vehicle.position+=world.sample_road(safe_distance).basis.x*float(rec.lane)
		rec.previous=vehicle.position
		rec.near=fposmod(safe_distance,world.total_length)
		rec.progress=safe_distance
		rec.best_progress=safe_distance
		rec.stuck=0
		rec.recoveries+=1

func position_of_player() -> int:
	if records.is_empty(): return FIELD_SIZE
	var place:=1
	var me: Dictionary=records[player_index]
	for i in range(records.size()):
		if i==player_index: continue
		var other: Dictionary=records[i]
		if other.finish>=0 or other.gate>me.gate or (other.gate==me.gate and other.progress>me.progress): place+=1
	return place

func show_gate() -> void:
	if is_instance_valid(gate_visual): gate_visual.queue_free()
	gate_visual=Node3D.new()
	get_parent().add_child(gate_visual)
	gate_visual.transform=world.sample_road(float(records[player_index].gate)*world.total_length/GATES_PER_LAP)
	for side in [-1,1]:
		world.box(Vector3(side*4.8,2,0),Vector3(.25,4,.25),"roof",gate_visual)
		world.box(Vector3(side*3.7,3.55,0),Vector3(2,.65,.1),"flower_gold",gate_visual)

func hud_text() -> String:
	if countdown>0: return "BAYVIEW C · 24 CARS\nSTART IN %d" % ceili(countdown)
	var lap:=mini(LAPS,1+int((int(records[player_index].gate)-1)/GATES_PER_LAP))
	return "POSITION %d / %d · LAP %d / %d\n%.1f s · GATE %d / 8" % [last_position,cars.size(),lap,LAPS,elapsed,1+(int(records[player_index].gate)-1)%8]
