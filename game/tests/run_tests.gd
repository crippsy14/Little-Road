extends SceneTree

const STATE = preload("res://scripts/game_state.gd")
var failures: Array[String] = []
var results: Array[Dictionary] = []

func _initialize() -> void:
	call_deferred("run")

func check(condition: bool,description: String) -> void:
	results.append({"name":description,"passed":condition})
	print(("PASS " if condition else "FAIL ")+description)
	if not condition: failures.append(description)

func run() -> void:
	var state := STATE.new()
	check(not state.claim_delivery_reward(),"Quest reward is unavailable before delivery")
	check(not state.deliver_package(),"Parcel cannot be delivered before acceptance")
	check(state.accept_delivery() and not state.accept_delivery(),"Delivery can be accepted only once")
	check(state.deliver_package() and not state.deliver_package(),"Parcel handover is idempotent")
	check(state.claim_delivery_reward() and state.credits==350,"Completed delivery grants the expected reward")
	check(not state.claim_delivery_reward() and state.credits==350,"Repeated reward claim cannot duplicate money")
	check(not state.buy_engine() and state.credits==350,"Unaffordable part does not spend money")
	state.credits=900
	check(state.buy_engine() and not state.buy_engine() and state.credits==450,"Engine purchase charges once")
	check(state.buy_tyres() and not state.buy_tyres() and state.credits==0,"Tyre purchase charges once")
	check(state.collect("postcard_0") and not state.collect("postcard_0") and state.credits==50,"Collectible ID grants one reward")
	check(state.fit_part("tyres_stock") and state.credits==50 and state.owned.has("tyres_trail"),"Refitting stock equipment is free and retains purchased parts")
	check(not state.fit_part("engine_club"),"Cannot fit an unowned part")
	var serial:=state.begin_race()
	check(state.finish_race(serial,"bayview_C",24)==100,"Last place earns useful race money")
	check(state.finish_race(serial,"bayview_C",1)==0,"A settled race cannot pay twice")
	check(state.finish_race(state.begin_race(),"missing_track",1)==0,"Missing tracks cannot award prizes")
	var base:=PartsCatalog.stats(PartsCatalog.stock())
	var fit:=PartsCatalog.stock()
	fit.transmission="transmission_long"
	var tall:=PartsCatalog.stats(fit)
	check(absf(tall.max_speed-base.max_speed)<.01 and tall.acceleration<base.acceleration,"Long gearing alone cannot overcome a weak engine")
	fit.engine="engine_club"
	check(PartsCatalog.stats(fit).max_speed>tall.max_speed,"A stronger engine can exploit long gearing")
	var save_path := ProjectSettings.globalize_path("res://../review/test-save.json")
	DirAccess.make_dir_recursive_absolute(save_path.get_base_dir())
	state.save_path=save_path
	check(state.save()==OK,"Save writes successfully")
	check(state.save()==OK,"Saving again safely replaces an existing save")
	var loaded := STATE.new()
	loaded.save_path=save_path
	var load_ok:=loaded.load_save()
	if not load_ok or loaded.encode()!=state.encode(): print("SAVE_DIAGNOSTIC ",load_ok," ",loaded.encode()," expected ",state.encode())
	check(load_ok and loaded.encode()==state.encode(),"Save/load round trip preserves progression and equipment")
	var bad := FileAccess.open(save_path+".invalid",FileAccess.WRITE)
	bad.store_string('{"version":1,"credits":-50}')
	bad.close()
	loaded.save_path=save_path+".invalid"
	var before := loaded.encode()
	check(not loaded.load_save() and loaded.encode()==before,"Malformed save is rejected without altering state")
	var legacy_data:={"version":1,"credits":1234,"quest_stage":3,"engine_level":1,"trail_tyres":true,"collected":["postcard_0"],"race_wins":2,"town_service":false,"paint_index":2}
	var legacy:=FileAccess.open(save_path+".legacy",FileAccess.WRITE)
	legacy.store_string(JSON.stringify(legacy_data))
	legacy.close()
	loaded.save_path=save_path+".legacy"
	check(loaded.load_save() and loaded.credits==1234 and loaded.equipped.engine=="engine_workshop" and loaded.equipped.tyres=="tyres_trail" and loaded.race_wins==2,"Version-one saves retain money, purchased parts and race wins")
	# Instantiate the real world and physics controller, rather than testing a duplicate.
	var game = load("res://scenes/main.tscn").instantiate()
	root.add_child(game)
	game.capture_mode=true
	game.state=STATE.new()
	game.apply_equipment()
	game.car.scripted=true
	for frame in range(35): await physics_frame
	check(game.car.is_on_floor(),"Player settles on actual road collision")
	var road_height: float=game.world.nearest_road(game.car.position).point.y
	check(absf(game.car.position.y-road_height)<.1,"Visual wheel baseline rests at the road surface")
	var start: Vector3=game.car.position
	game.car.scripted_throttle=1
	for frame in range(120): await physics_frame
	check(game.car.position.distance_to(start)>7 and game.car.speed>8,"Throttle advances the actual car on the road")
	game.car.scripted_throttle=-1
	for frame in range(100): await physics_frame
	check(game.car.speed<0,"Brake stops the car then allows reverse")
	game.car.scripted_throttle=0
	game.car.position=Vector3(190,-4,-60)
	await physics_frame
	await physics_frame
	check(game.car.position.y>0 and absf(game.car.speed)<.01,"Falling below sea level recovers safely to the road")
	game.car.place_on_road(0)
	game.car.scripted_throttle=.7
	var max_offset:=0.0
	var starting_distance:=0.0
	var reached_distance:=0.0
	# Exercise slopes and corners for 30 simulated seconds with path-following inputs.
	for frame in range(1800):
		var near: Dictionary=game.world.nearest_road(game.car.position)
		var aim: Vector3=game.world.sample_road(near.distance+12).origin-game.car.position
		aim.y=0
		var forward: Vector3=-game.car.global_basis.z
		var angle:=forward.signed_angle_to(aim.normalized(),Vector3.UP)
		game.car.scripted_steer=clampf(angle*2.0,-1,1)
		max_offset=maxf(max_offset,near.offset)
		reached_distance=maxf(reached_distance,near.distance)
		await physics_frame
	check(reached_distance>250 and max_offset<8,"Steering and ground-following traverse hills and bends")
	game.car.scripted_throttle=0
	game.car.scripted_steer=0
	game.start_race()
	check(game.race.cars.size()==24 and game.race.records.size()==24,"Race creates player plus 23 physical opponents")
	check(game.race.countdown==3 and not game.car.driving_enabled,"Countdown holds the full grid before launch")
	game.car.position=game.world.sample_road(game.world.total_length*.5).origin
	game.race.countdown=0
	game.race._physics_process(1.0/60.0)
	check(game.race.records[23].gate==1,"Teleporting past checkpoints cannot advance race progress")
	game.start_race()
	check(game.race.records[23].gate==1 and game.race.cars.size()==24 and game.race.elapsed==0,"Retry replaces the field and resets all race progress")
	# Cross the real final checkpoint, then let the result screen stay open.
	game.race._physics_process(3.01)
	var finish_pose: Transform3D=game.world.sample_road(0)
	var finish_forward: Vector3=-finish_pose.basis.z
	game.race.countdown=0
	game.race.elapsed=90
	game.race.records[23].gate=24
	game.race.records[23].progress=game.world.total_length*3-.05
	game.race.records[23].near=game.world.total_length-.05
	game.race.records[23].previous=finish_pose.origin-finish_forward*.05
	game.car.position=finish_pose.origin+finish_forward*.05
	game.race._physics_process(1.0/60.0)
	check(game.race.player_has_finished() and game.race.active and game.dialog.visible,"Player finish opens results while the remaining field stays active")
	var paid_credits: int=game.state.credits
	game._process(.016)
	var before_finish: Vector3=game.race.cars[0].position
	for frame in range(120): await physics_frame
	check(game.race.cars[0].position.distance_to(before_finish)>1 and not game.race.suspended,"Rivals continue driving behind the open results screen")
	check(game.state.credits==paid_credits,"Background finishing cannot pay the player again")
	check(game.car.collision_layer==0,"The finished player cannot block trailing opponents")
	game.show_standings()
	check(game.standings_label.text.contains("YOU") and game.standings_label.text.split("\n").size()==24,"Full standings include every driver and the player")
	game.close_dialog()
	check(game.race.cars.is_empty() and game.car.collision_layer==2 and game.car.driving_enabled,"Leaving results clears rivals and restores the player's road collision")
	game.race.clear()
	game.queue_free()
	await process_frame
	var report := {"passed":failures.is_empty(),"checks":results,"driving":{"maximum_road_offset":max_offset,"furthest_distance":reached_distance},"scope":"Headless scripted checks; visual quality, human handling and physical controller remain separate."}
	var output := FileAccess.open(ProjectSettings.globalize_path("res://../review/test-results.json"),FileAccess.WRITE)
	output.store_string(JSON.stringify(report,"  "))
	output.close()
	print("LITTLE_ROAD_TESTS ",results.size()-failures.size(),"/",results.size())
	quit(0 if failures.is_empty() else 1)
