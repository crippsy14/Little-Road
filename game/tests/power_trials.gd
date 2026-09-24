extends SceneTree

var failures: Array[String]=[]
var report: Dictionary={}

func expect(ok: bool, message: String) -> void:
	print(("PASS " if ok else "FAIL ")+message)
	if not ok: failures.append(message)

func run_pull(fit: Dictionary) -> Dictionary:
	var tuning:=PartsCatalog.stats(fit)
	var speed:=0.0
	var times: Dictionary={}
	for frame in range(5400):
		speed=DriveModel.step(tuning,speed,1,0,tuning.road_grip,0,1.0/60).speed
		for mark in [10,15,20]:
			if speed>=mark and not times.has(str(mark)): times[str(mark)]=(frame+1)/60.0
	return {"speed_after_90s":speed,"times_to_mps":times,"gear_ceiling":tuning.gear_ceiling,"estimated_top":tuning.max_speed}

func _initialize() -> void:
	var stock:=PartsCatalog.stock()
	var tall:=stock.duplicate()
	tall.transmission="transmission_long"
	var engine:=tall.duplicate()
	engine.engine="engine_club"
	var boosted:=engine.duplicate()
	boosted.turbo="turbo_club"
	for entry in [["stock",stock],["tall_only",tall],["engine_and_tall",engine],["turbo_and_engine",boosted]]:
		report[entry[0]]=run_pull(entry[1])
	expect(report.stock.times_to_mps["20"]>report.stock.times_to_mps["10"]*2,"Acceleration tapers as speed rises")
	expect(absf(report.stock.speed_after_90s-report.tall_only.speed_after_90s)<.1,"Tall gearing cannot make a weak engine defeat drag")
	expect(report.engine_and_tall.speed_after_90s>report.tall_only.speed_after_90s+5,"Engine power makes longer gearing usable")
	expect(report.turbo_and_engine.times_to_mps["20"]<report.engine_and_tall.times_to_mps["20"],"Turbo power measurably improves acceleration")
	var tune:=PartsCatalog.stats(boosted)
	var straight:=DriveModel.step(tune,15,1,0,7,0,.1)
	var corner:=DriveModel.step(tune,15,1,.8,7,0,.1)
	var tyres:=DriveModel.step(tune,15,1,.8,12,0,.1)
	report.corner={"stock_tyre_accel":corner.acceleration,"sport_tyre_accel":tyres.acceleration,"straight_accel":straight.acceleration,"stock_yaw":corner.yaw,"sport_yaw":tyres.yaw}
	expect(corner.acceleration<straight.acceleration and tyres.acceleration>corner.acceleration,"Cornering uses traction; better tyres release usable engine power")
	expect(tyres.yaw>corner.yaw,"More steering cannot exceed grip; better tyres tighten the attainable turn")
	var hill:=DriveModel.step(tune,15,1,0,12,2,.1)
	expect(hill.acceleration<straight.acceleration,"Climbing consumes available engine force")
	var state:=AdventureState.new()
	state.credits=700
	expect(state.buy_part("turbo_street") and state.fit_part("turbo_street") and state.credits==0,"Turbo is a purchased, fitted functional part")
	var prior:=state.encode()
	prior.version=2
	prior.owned.erase("turbo_street")
	prior.owned.erase("turbo_stock")
	prior.equipped.erase("turbo")
	var path:=ProjectSettings.globalize_path("res://../review/test-save.json.v2")
	var file:=FileAccess.open(path,FileAccess.WRITE)
	file.store_string(JSON.stringify(prior))
	file.close()
	var migrated:=AdventureState.new()
	migrated.save_path=path
	expect(migrated.load_save() and migrated.owned.has("turbo_stock") and migrated.equipped.turbo=="turbo_stock","Version-two saves gain stock induction without losing progression")
	report.failures=failures
	var output:=FileAccess.open(ProjectSettings.globalize_path("res://../review/power-trials.json"),FileAccess.WRITE)
	output.store_string(JSON.stringify(report,"  "))
	output.close()
	print("POWER_TRIALS ",9-failures.size(),"/9")
	quit(0 if failures.is_empty() else 1)
