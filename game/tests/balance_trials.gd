extends SceneTree

var world: RoadWorld
var car: LittleCar
var race: CoastRace
var reports: Array=[]
var output_path: String

func _initialize() -> void:
	call_deferred("run")

func equipment(profile: String) -> Dictionary:
	var fit:=PartsCatalog.stock()
	if profile in ["engine","balanced","developed"]: fit.engine="engine_workshop"
	if profile in ["tyres","balanced","developed"]: fit.tyres="tyres_touring"
	if profile in ["balanced","developed"]:
		fit.brakes="brakes_sport"
		fit.steering="steering_quick"
	if profile=="developed":
		fit={"engine":"engine_club","tyres":"tyres_sport","transmission":"transmission_long","brakes":"brakes_race","steering":"steering_precision","chassis":"chassis_light"}
	return fit

func run() -> void:
	var mode:="solo"
	for arg in OS.get_cmdline_user_args():
		if arg.begins_with("--mode="): mode=arg.trim_prefix("--mode=")
	output_path=ProjectSettings.globalize_path("res://../review/balance-"+mode+".json")
	world=RoadWorld.new()
	root.add_child(world)
	car=LittleCar.new()
	car.road_world=world
	car.scripted=true
	root.add_child(car)
	race=CoastRace.new()
	root.add_child(race)
	race.autopilot_player=true
	var profiles:=["stock","engine","tyres","balanced","developed"] if mode=="solo" else ["stock","balanced","developed"]
	var seeds:=[0,0,11,22,33] if mode=="solo" else [11,22,33]
	var run_index:=0
	for profile in profiles:
		for seed_value in seeds:
			run_index+=1
			await trial(profile,seed_value,mode,run_index)
	var failures:=reports.filter(func(report): return not report.complete or report.recoveries>0)
	print("BALANCE_COMPLETE ",mode," runs=",reports.size()," incomplete_or_recovered=",failures.size())
	quit(0 if failures.is_empty() else 1)

func trial(profile: String,seed_value: int,mode: String,run_index: int) -> void:
	race.clear()
	root.remove_child(car)
	car.queue_free()
	await physics_frame
	await physics_frame
	car=LittleCar.new()
	car.road_world=world
	car.scripted=true
	root.add_child(car)
	var fit:=equipment(profile)
	car.tuning=PartsCatalog.stats(fit)
	car.controlled=true
	race.start(world,car,1 if mode=="solo" else 24,seed_value)
	# Match the player's last-grid-slot starting distance/lane in both modes.
	var distance:=-59.0
	car.place_on_road(distance)
	car.position+=world.sample_road(distance).basis.x*2.25
	var player_record: Dictionary=race.records[race.player_index]
	player_record.merge({"progress":distance,"best_progress":distance,"near":fposmod(distance,world.total_length),"lane":2.25,"target_lane":2.25,"previous":car.position},true)
	var laps: Array=[]
	var previous_gate:=1
	var maximum_stall:=0.0
	var player_wall_frames:=0
	var wall_events: Array=[]
	var traits:={"pace":player_record.pace,"braking_margin":player_record.braking_margin,"line_bias":player_record.line_bias}
	for frame in range(60*300):
		await physics_frame
		player_record=race.records[race.player_index]
		if car.lateral_contact and player_record.finish<0:
			player_wall_frames+=1
			if wall_events.size()<30:
				var contacts: Array=[]
				for i in range(car.get_slide_collision_count()):
					var hit:=car.get_slide_collision(i)
					var collider=hit.get_collider()
					contacts.append({"normal":str(hit.get_normal()),"collider":str(collider.get_path()) if collider else ""})
				wall_events.append({"distance":player_record.near,"position":str(car.position),"contacts":contacts})
		for record in race.records: maximum_stall=maxf(maximum_stall,record.stuck)
		if player_record.gate!=previous_gate:
			if player_record.gate in [9,17,25]: laps.append(race.elapsed)
			previous_gate=player_record.gate
		if not race.active: break
	var rows: Array=[]
	var recoveries:=0
	for i in range(race.records.size()):
		var record: Dictionary=race.records[i]
		rows.append({"driver":i+1,"player":i==race.player_index,"finish":record.finish,"recoveries":record.recoveries,"equipment":fit if i==race.player_index else race.rival_setup(i)})
		recoveries+=record.recoveries
	var cost:=0
	for part in fit.values(): cost+=int(PartsCatalog.PARTS[part].price)
	var lap_durations: Array=[]
	for i in range(laps.size()): lap_durations.append(laps[i]-(laps[i-1] if i>0 else 0.0))
	var complete:=race.records.all(func(record): return record.finish>=0)
	reports.append({"run":run_index,"mode":mode,"profile":profile,"seed":seed_value,"equipment":fit,"cost":cost,"traits":traits,"player_seconds":player_record.finish,"lap_seconds":lap_durations,"place":race.last_position,"complete":complete,"recoveries":recoveries,"player_wall_frames":player_wall_frames,"wall_events":wall_events,"maximum_stall":maximum_stall,"field":rows})
	var output:=FileAccess.open(output_path,FileAccess.WRITE)
	output.store_string(JSON.stringify({"protocol":"Same controller, track, last-slot start and physics for solo/field. Seeds 0/0 repeat controls; 11/22/33 matched bounded driver variation. Times are measured checkpoint crossings; no added time noise. First lap includes the grid approach.","runs":reports},"  "))
	output.close()
	print("BALANCE ",mode," ",profile," seed=",seed_value," seconds=",player_record.finish," place=",race.last_position," finished=",complete," recoveries=",recoveries)
