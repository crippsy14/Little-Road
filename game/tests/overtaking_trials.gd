extends SceneTree

func _initialize() -> void: call_deferred("run")

func run() -> void:
	var world:=RoadWorld.new()
	root.add_child(world)
	var player:=LittleCar.new()
	player.road_world=world
	player.scripted=true
	root.add_child(player)
	var race:=CoastRace.new()
	root.add_child(race)
	race.autopilot_player=true
	var reports: Array=[]
	for count in [2,3]:
		for seed_value in [11,22,33]:
			race.clear()
			await physics_frame
			await physics_frame
			var developed:={"engine":"engine_club","tyres":"tyres_sport","transmission":"transmission_long","brakes":"brakes_race","steering":"steering_precision","chassis":"chassis_light"}
			player.tuning=PartsCatalog.stats(developed)
			race.start(world,player,count,seed_value)
			for i in range(count):
				var vehicle:=race.cars[i]
				var fit:=PartsCatalog.stock()
				if i==1 and count==3:
					fit.engine="engine_workshop"
					fit.tyres="tyres_touring"
					fit.transmission="transmission_long"
					fit.steering="steering_quick"
				if vehicle!=player: vehicle.tuning=PartsCatalog.stats(fit)
				var distance:=-4.0 if i==0 else (-6.0 if i==1 and count==3 else -18.0)
				var lane:=-2.25 if i==1 and count==3 else 2.25
				vehicle.place_on_road(distance)
				vehicle.position+=world.sample_road(distance).basis.x*lane
				race.records[i].merge({"progress":distance,"best_progress":distance,"near":fposmod(distance,world.total_length),"lane":lane,"target_lane":lane,"previous":vehicle.position},true)
			var passed: Array[int]=[]
			var events: Array=[]
			var maximum_stall:=0.0
			for frame in range(60*240):
				await physics_frame
				var me: Dictionary=race.records[race.player_index]
				for i in range(count-1):
					var other: Dictionary=race.records[i]
					if me.progress>other.progress+4 and me.gate>=other.gate and not passed.has(i):
						passed.append(i)
						events.append({"passed_driver":i+1,"seconds":race.elapsed,"distance":me.near})
				for record in race.records: maximum_stall=maxf(maximum_stall,record.stuck)
				if not race.active: break
			var recoveries:=0
			var finished:=0
			for record in race.records:
				recoveries+=record.recoveries
				if record.finish>=0: finished+=1
			var success: bool=finished==count and recoveries==0 and passed.size()==count-1 and race.last_position==1
			reports.append({"cars":count,"seed":seed_value,"passed":success,"finished":finished,"recoveries":recoveries,"player_place":race.last_position,"player_seconds":race.records[race.player_index].finish,"overtakes":events,"maximum_stall":maximum_stall})
			var output:=FileAccess.open(ProjectSettings.globalize_path("res://../review/overtaking-trials.json"),FileAccess.WRITE)
			output.store_string(JSON.stringify({"protocol":"Faster car starts behind a stock car; three-car case adds an intermediate-performance car in the adjacent lane. Production physics, driver variation and race code; require physical passes, first place, all finishers and zero recoveries.","runs":reports},"  "))
			output.close()
			print("OVERTAKING cars=",count," seed=",seed_value," passed=",success," position=",race.last_position," overtakes=",passed.size()," recoveries=",recoveries)
	var all_passed:=reports.all(func(report): return report.passed)
	quit(0 if all_passed else 1)
