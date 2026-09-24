extends SceneTree

func _initialize() -> void: call_deferred("run")

func run() -> void:
	# Replay measured finish times for UI inspection; not a new race trial.
	var data: Dictionary=JSON.parse_string(FileAccess.get_file_as_string("res://../review/balance-field-conservative-passing.json"))
	var measured: Dictionary=data.runs.back()
	var game=load("res://scenes/main.tscn").instantiate()
	root.add_child(game)
	game.capture_mode=true
	game.state=AdventureState.new()
	game.start_race()
	game.race.active=false
	for entry in measured.field:
		game.race.records[int(entry.driver)-1].finish=entry.finish
		game.race.records[int(entry.driver)-1].gate=25
	game.race.last_position=measured.place
	game.car.place_on_road(15)
	game.snap_camera()
	game.race_finished(measured.place,measured.player_seconds)
	var directory:=ProjectSettings.globalize_path("res://../review/captures/results-06")
	DirAccess.make_dir_recursive_absolute(directory)
	await create_timer(.5).timeout
	await game.capture_screenshot(directory.path_join("01-results.png"))
	game.show_standings()
	await create_timer(.3).timeout
	await game.capture_screenshot(directory.path_join("02-standings-top.png"))
	var scroll: ScrollContainer=game.standings_label.get_parent()
	scroll.scroll_vertical=10000
	await create_timer(.3).timeout
	await game.capture_screenshot(directory.path_join("03-standings-bottom.png"))
	print("RESULTS_UI_CAPTURE_COMPLETE")
	quit()
