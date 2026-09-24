extends SceneTree

func _initialize() -> void:
	call_deferred("run")

func run() -> void:
	var options:={"profile":"stock","scenario":"coast","seed":"0","label":"","results-image":"","trace":"false","watch":"false"}
	for arg in OS.get_cmdline_user_args():
		for key in options:
			if arg.begins_with("--"+key+"="): options[key]=arg.trim_prefix("--"+key+"=")
	var game=load("res://scenes/main.tscn").instantiate()
	root.add_child(game)
	game.capture_mode=true
	game.state=AdventureState.new()
	if options.scenario=="s_bend":
		game.remove_child(game.world)
		game.world.queue_free()
		game.world=load("res://tests/s_bend_world.gd").new()
		game.add_child(game.world)
		game.car.road_world=game.world
	var fit:=PartsCatalog.stock()
	match options.profile:
		"engine": fit.engine="engine_workshop"
		"tyres": fit.tyres="tyres_touring"
		"balanced":
			fit.engine="engine_workshop"
			fit.tyres="tyres_touring"
			fit.steering="steering_quick"
			fit.brakes="brakes_sport"
		"developed":
			fit={"engine":"engine_club","tyres":"tyres_sport","transmission":"transmission_long","brakes":"brakes_race","steering":"steering_precision","chassis":"chassis_light"}
	game.state.equipped=fit
	game.apply_equipment()
	game.car.scripted=true
	game.start_race()
	game.race.autopilot_player=true
	var seed_value:=int(options.seed)
	var grid: Array[int]=[]
	for i in range(23): grid.append(i)
	if seed_value!=0:
		var rng:=RandomNumberGenerator.new()
		rng.seed=seed_value
		for i in range(22,0,-1):
			var j:=rng.randi_range(0,i)
			var swap:=grid[i]
			grid[i]=grid[j]
			grid[j]=swap
	grid.append(23)
	for slot in range(24):
		var driver:=grid[slot]
		var distance:=-float(slot/2)*5.0-4.0
		var lane:=-2.25 if slot%2==0 else 2.25
		var vehicle=game.race.cars[driver]
		vehicle.place_on_road(distance)
		vehicle.position+=game.world.sample_road(distance).basis.x*lane
		var record: Dictionary=game.race.records[driver]
		for key in ["progress","best_progress"]: record[key]=distance
		for key in ["lane","target_lane"]: record[key]=lane
		record.near=fposmod(distance,game.world.total_length)
		record.previous=vehicle.position
	if options.scenario=="blocked":
		var pose: Transform3D=game.world.sample_road(185)
		var obstacle: MeshInstance3D=game.world.box(pose.origin+pose.basis.x*2.3+Vector3.UP*.6,Vector3(1.6,1.2,2.75),"orange",null,true)
		obstacle.name="StoppedCarFixture"
		obstacle.basis=pose.basis
	var recovery_events: Array=[]
	var stall_events: Array=[]
	var stalled: Array[bool]=[]
	stalled.resize(24)
	stalled.fill(false)
	var previous_recoveries: Array[int]=[]
	previous_recoveries.resize(24)
	previous_recoveries.fill(0)
	var maximum_stall:=0.0
	var player_trace: Array=[]
	for frame in range(60*300):
		await physics_frame
		if options.trace=="true" and frame%30==0 and not game.race.player_has_finished():
			var trace: Dictionary=game.race.records[23].duplicate()
			trace.erase("previous")
			trace["time"]=game.race.elapsed
			trace["speed"]=game.car.speed
			player_trace.append(trace)
		for i in range(24):
			var record: Dictionary=game.race.records[i]
			maximum_stall=maxf(maximum_stall,record.stuck)
			if record.stuck>1.0 and not stalled[i] and stall_events.size()<256:
				var vehicle=game.race.cars[i]
				var contacts: Array=[]
				for contact in range(vehicle.get_slide_collision_count()):
					var collider=vehicle.get_slide_collision(contact).get_collider()
					if collider: contacts.append({"path":String(collider.get_path()),"position":str(collider.global_position)})
				stall_events.append({"driver":i+1,"time":game.race.elapsed,"distance":record.near,"position":str(vehicle.position),"contacts":contacts})
			stalled[i]=record.stuck>1.0
			if record.recoveries>previous_recoveries[i]:
				recovery_events.append({"driver":i+1,"time":game.race.elapsed,"distance":record.near,"gate":record.gate})
				previous_recoveries[i]=record.recoveries
		if not game.race.active: break
	var rows: Array=[]
	var finished_count:=0
	for i in range(24):
		var r: Dictionary=game.race.records[i].duplicate()
		r.erase("previous")
		r["driver"]=i+1
		rows.append(r)
		if r.finish>=0: finished_count+=1
	var report: Dictionary={"elapsed":game.race.elapsed,"player_time":game.race.records[23].finish,"player_place":game.race.last_position,"complete":finished_count==24,"finished_count":finished_count,"entries":rows,"options":options,"grid":grid,"equipment":fit,"maximum_stall":maximum_stall,"recovery_events":recovery_events,"stall_events":stall_events,"player_trace":player_trace}
	var report_name: String=options.label if not options.label.is_empty() else "%s-%s-%s" % [options.scenario,options.profile,options.seed]
	var path:=ProjectSettings.globalize_path("res://../review/race-trials/"+report_name+".json")
	DirAccess.make_dir_recursive_absolute(path.get_base_dir())
	var output:=FileAccess.open(path,FileAccess.WRITE)
	output.store_string(JSON.stringify(report,"  "))
	output.close()
	print("RACE_TRIAL ",report_name," finished=",finished_count,"/24 place=",report.player_place," player_seconds=",report.player_time," recoveries=",recovery_events.size()," max_stall=",maximum_stall)
	if not options["results-image"].is_empty():
		game.show_standings()
		await create_timer(.5).timeout
		await game.capture_screenshot(options["results-image"])
	if options.watch=="true":
		game.show_standings()
		print("WATCH_TEST_RESULTS_READY")
		return
	quit(0 if report.complete else 1)
