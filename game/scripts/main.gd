extends Node3D

const WORLD_SCRIPT = preload("res://scripts/road_world.gd")
const CAR_SCRIPT = preload("res://scripts/car_controller.gd")
const STATE_SCRIPT = preload("res://scripts/game_state.gd")
const RACE_SCRIPT = preload("res://scripts/race_event.gd")
const PARTS = preload("res://scripts/parts_catalog.gd")
var world: RoadWorld
var car: LittleCar
var state := AdventureState.new()
var camera: Camera3D
var camera_target := Vector3.ZERO
var hud: CanvasLayer
var speed_label: Label
var objective_label: Label
var money_label: Label
var area_label: Label
var prompt_label: Label
var message_label: Label
var dialog: PanelContainer
var dialog_title: Label
var dialog_text: Label
var dialog_buttons: VBoxContainer
var status_timer := 0.0
var game_time := 0.0
var npc_locations: Dictionary = {"garage":Vector3(-35,8,9),"post":Vector3(131,22,-187),"cafe":Vector3(-17,8,-9)}
var near_npc := ""
var collectibles: Dictionary = {}
var capture_mode := false
var capture_directory := ""
var review_task_started := false
var paused := false
var race: CoastRace
var race_serial:=0
var standings_label: Label
var standings_timer:=0.0
var paint_colors: Array[Color] = [Color("389d95"),Color("c76b49"),Color("e5bd59"),Color("658cba")]

func _ready() -> void:
	configure_inputs()
	for arg in OS.get_cmdline_user_args():
		if arg.begins_with("--capture-dir="):
			capture_directory = arg.trim_prefix("--capture-dir=")
			capture_mode = true
	if not capture_mode: state.load_save()
	world = WORLD_SCRIPT.new()
	add_child(world)
	car = CAR_SCRIPT.new()
	car.road_world = world
	add_child(car)
	car.place_on_road(15)
	apply_equipment()
	camera = Camera3D.new()
	camera.fov = 62
	camera.far = 1400
	add_child(camera)
	camera.current = true
	snap_camera()
	make_npcs()
	make_collectibles()
	make_hud()
	race=RACE_SCRIPT.new()
	add_child(race)
	race.player_finished.connect(race_finished)
	message("Welcome to Bayview. Mara at the garage has a journey for you.",8)
	if capture_mode: call_deferred("run_visual_review")

func configure_inputs() -> void:
	var keys := {"accelerate":[KEY_W,KEY_UP],"brake":[KEY_S,KEY_DOWN],"left":[KEY_A,KEY_LEFT],"right":[KEY_D,KEY_RIGHT],"handbrake":[KEY_SPACE],"reset":[KEY_R],"interact":[KEY_E],"pause_game":[KEY_ESCAPE],"photo":[KEY_F12]}
	for action in keys:
		if not InputMap.has_action(action): InputMap.add_action(action)
		for key in keys[action]:
			var event := InputEventKey.new()
			event.physical_keycode = key
			InputMap.action_add_event(action,event)
	for spec in [["accelerate",JOY_AXIS_TRIGGER_RIGHT,1],["brake",JOY_AXIS_TRIGGER_LEFT,1],["left",JOY_AXIS_LEFT_X,-1],["right",JOY_AXIS_LEFT_X,1]]:
		var event := InputEventJoypadMotion.new()
		event.axis = spec[1]
		event.axis_value = spec[2]
		InputMap.action_add_event(spec[0],event)
	for spec in [["interact",JOY_BUTTON_A],["handbrake",JOY_BUTTON_B],["reset",JOY_BUTTON_Y],["pause_game",JOY_BUTTON_START]]:
		var event := InputEventJoypadButton.new()
		event.button_index = spec[1]
		InputMap.action_add_event(spec[0],event)

func panel_style(color: Color, radius: int = 14) -> StyleBoxFlat:
	var style := StyleBoxFlat.new()
	style.bg_color = color
	style.corner_radius_top_left=radius
	style.corner_radius_top_right=radius
	style.corner_radius_bottom_left=radius
	style.corner_radius_bottom_right=radius
	style.content_margin_left=24
	style.content_margin_right=24
	style.content_margin_top=17
	style.content_margin_bottom=17
	return style

func label(text: String,size: int,color: Color = Color("f7efdc")) -> Label:
	var l := Label.new()
	l.text=text
	l.add_theme_font_size_override("font_size",size)
	l.add_theme_color_override("font_color",color)
	return l

func make_hud() -> void:
	hud = CanvasLayer.new()
	add_child(hud)
	var root := Control.new()
	root.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	root.mouse_filter=Control.MOUSE_FILTER_IGNORE
	hud.add_child(root)
	var badge := PanelContainer.new()
	badge.position=Vector2(32,30)
	badge.custom_minimum_size=Vector2(260,90)
	badge.add_theme_stylebox_override("panel",panel_style(Color("203c40e8")))
	root.add_child(badge)
	var title_box := VBoxContainer.new()
	badge.add_child(title_box)
	title_box.add_child(label("LITTLE ROAD",28))
	area_label=label("BAYVIEW  /  COASTAL COUNTY",12,Color("aec7b9"))
	title_box.add_child(area_label)
	var objective_panel := PanelContainer.new()
	objective_panel.position=Vector2(32,140)
	objective_panel.custom_minimum_size=Vector2(330,80)
	objective_panel.add_theme_stylebox_override("panel",panel_style(Color("243d40cc"),10))
	root.add_child(objective_panel)
	var objectives := VBoxContainer.new()
	objective_panel.add_child(objectives)
	objectives.add_child(label("YOUR NEXT LITTLE ADVENTURE",11,Color("e4c280")))
	objective_label=label("Visit Mara at Harbour Garage",17)
	objectives.add_child(objective_label)
	var purse := PanelContainer.new()
	purse.set_anchors_and_offsets_preset(Control.PRESET_TOP_RIGHT)
	purse.position=Vector2(-266,32)
	purse.custom_minimum_size=Vector2(230,62)
	purse.add_theme_stylebox_override("panel",panel_style(Color("203c40e8")))
	root.add_child(purse)
	money_label=label("",19)
	purse.add_child(money_label)
	var speed_panel := PanelContainer.new()
	speed_panel.set_anchors_and_offsets_preset(Control.PRESET_BOTTOM_RIGHT)
	speed_panel.position=Vector2(-236,-136)
	speed_panel.custom_minimum_size=Vector2(200,105)
	speed_panel.add_theme_stylebox_override("panel",panel_style(Color("203c40ed")))
	root.add_child(speed_panel)
	speed_label=label("00  MPH",32)
	speed_panel.add_child(speed_label)
	var controls := label("WASD / ARROWS  drive     SPACE  handbrake     E  talk     R  recover     ESC  pause",14)
	controls.set_anchors_and_offsets_preset(Control.PRESET_BOTTOM_LEFT)
	controls.position=Vector2(32,-44)
	controls.add_theme_color_override("font_shadow_color",Color("172e30"))
	controls.add_theme_constant_override("shadow_offset_x",1)
	controls.add_theme_constant_override("shadow_offset_y",2)
	root.add_child(controls)
	prompt_label=label("",23)
	prompt_label.set_anchors_and_offsets_preset(Control.PRESET_CENTER_BOTTOM)
	prompt_label.position=Vector2(-220,-155)
	prompt_label.custom_minimum_size=Vector2(440,40)
	prompt_label.horizontal_alignment=HORIZONTAL_ALIGNMENT_CENTER
	root.add_child(prompt_label)
	message_label=label("",18)
	message_label.set_anchors_and_offsets_preset(Control.PRESET_CENTER_BOTTOM)
	message_label.position=Vector2(-420,-93)
	message_label.custom_minimum_size=Vector2(840,42)
	message_label.horizontal_alignment=HORIZONTAL_ALIGNMENT_CENTER
	message_label.add_theme_color_override("font_shadow_color",Color("203c40"))
	message_label.add_theme_constant_override("shadow_offset_y",2)
	root.add_child(message_label)
	dialog=PanelContainer.new()
	dialog.custom_minimum_size=Vector2(600,360)
	dialog.add_theme_stylebox_override("panel",panel_style(Color("f1e7d4"),18))
	var dialog_center:=CenterContainer.new()
	dialog_center.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	dialog_center.mouse_filter=Control.MOUSE_FILTER_IGNORE
	root.add_child(dialog_center)
	dialog_center.add_child(dialog)
	var contents := VBoxContainer.new()
	contents.add_theme_constant_override("separation",16)
	dialog.add_child(contents)
	dialog_title=label("",29,Color("244247"))
	contents.add_child(dialog_title)
	dialog_text=label("",19,Color("3c5556"))
	dialog_text.autowrap_mode=TextServer.AUTOWRAP_WORD_SMART
	dialog_text.custom_minimum_size=Vector2(550,88)
	contents.add_child(dialog_text)
	dialog_buttons=VBoxContainer.new()
	dialog_buttons.add_theme_constant_override("separation",8)
	contents.add_child(dialog_buttons)
	dialog.hide()

func make_npcs() -> void:
	var post: Vector3=npc_locations["post"]
	post.y=world.height_at(post.x,post.z)
	npc_locations["post"]=post
	for spec in [["garage",Color("d6794e"),"MARA"],["post",Color("e2ba5b"),"PIP"],["cafe",Color("768faf"),"ROWAN"]]:
		var npc: Node3D = LittleCar.CAR_MODEL.instantiate()
		add_child(npc)
		npc.position=npc_locations[spec[0]]
		npc.rotation.y=PI*.5
		for node in npc.find_children("*","MeshInstance3D",true,false):
			for i in range(node.mesh.get_surface_count()):
				var m = node.mesh.surface_get_material(i)
				if m and m.resource_name.begins_with("BodyPaint"):
					var copy = m.duplicate()
					copy.albedo_color=spec[1]
					node.set_surface_override_material(i,copy)
		var tag := Label3D.new()
		tag.text=spec[2]
		tag.position=Vector3(0,2.6,0)
		tag.font_size=32
		tag.pixel_size=.009
		tag.billboard=BaseMaterial3D.BILLBOARD_ENABLED
		tag.modulate=Color("fff0ca")
		npc.add_child(tag)

func make_collectibles() -> void:
	for i in range(5):
		var id := "postcard_"+str(i)
		if state.collected.has(id): continue
		var transform := world.sample_road(world.total_length*(.12+i*.17))
		var pos := transform.origin+transform.basis.x*(7.0 if i%2==0 else -7.0)
		pos.y=world.height_at(pos.x,pos.z)+1.4
		var coin := MeshInstance3D.new()
		var mesh := CylinderMesh.new()
		mesh.top_radius=.5
		mesh.bottom_radius=.5
		mesh.height=.11
		coin.mesh=mesh
		var m := StandardMaterial3D.new()
		m.albedo_color=Color("f1c568")
		m.metallic=.45
		m.roughness=.25
		coin.material_override=m
		coin.rotation.x=PI*.5
		coin.position=pos
		add_child(coin)
		collectibles[id]=coin

func _process(delta: float) -> void:
	game_time+=delta
	if Input.is_action_just_pressed("pause_game"):
		if dialog.visible: close_dialog()
		else: show_pause()
	if not paused:
		update_camera(delta)
		update_interaction()
		for id in collectibles.keys():
			var coin: Node3D=collectibles[id]
			coin.rotation.z+=delta
			if car.global_position.distance_to(coin.position)<2.2:
				if state.collect(id):
					coin.queue_free()
					collectibles.erase(id)
					save_progress()
					message("A little discovery!  +50 credits",3)
	if status_timer>0:
		status_timer-=delta
		if status_timer<=0: message_label.text=""
	speed_label.text="%02d  MPH\n%s" % [abs(car.speed)*2.237,car.surface_name]
	money_label.text="%d cr   ·   %d / 5 finds" % [state.credits,state.collected.size()]
	var tasks := ["Visit Mara at Harbour Garage","Deliver the parcel to the lighthouse","Return to Mara for your reward","Explore, upgrade & enter Rowan's race"]
	objective_label.text=tasks[state.quest_stage]
	if race and race.active: objective_label.text=race.hud_text()
	if race: race.suspended=paused and not race.player_has_finished()
	if is_instance_valid(standings_label):
		standings_timer-=delta
		if standings_timer<=0:
			update_standings()
			standings_timer=.25
	area_label.text="BAYVIEW  /  COASTAL COUNTY" if car.position.z > -70 else "THE LIGHTHOUSE ROAD"
	if Input.is_action_just_pressed("photo"):
		capture_screenshot("user://little-road-photo.png")
		message("Photo saved to your game data folder",4)

func snap_camera() -> void:
	camera_target=car.global_position+Vector3.UP*1.0
	camera.global_position=car.global_position+car.global_basis.z*8.2+Vector3.UP*4.0
	camera.look_at(camera_target)

func update_camera(delta: float) -> void:
	var desired: Vector3 = car.global_position+car.global_basis.z*(8.2+absf(car.speed)*.06)+Vector3.UP*4.0
	var target := car.global_position+Vector3.UP*1.15-car.global_basis.z*1.9
	var query := PhysicsRayQueryParameters3D.create(target,desired)
	query.exclude=[car.get_rid()]
	query.collision_mask=1
	var hit := get_world_3d().direct_space_state.intersect_ray(query)
	if not hit.is_empty(): desired=hit.position+(target-hit.position).normalized()*.5
	camera.global_position=camera.global_position.lerp(desired,1.0-exp(-delta*5.8))
	camera_target=camera_target.lerp(target,1.0-exp(-delta*7))
	camera.look_at(camera_target)

func update_interaction() -> void:
	near_npc=""
	if race and race.active:
		prompt_label.text=""
		return
	for id in npc_locations:
		if car.global_position.distance_to(npc_locations[id])<8.0: near_npc=id
	var names := {"garage":"Mara · garage & upgrades","post":"Pip · lighthouse post","cafe":"Rowan · coast run"}
	prompt_label.text="E / Cross   "+names[near_npc] if not near_npc.is_empty() and abs(car.speed)<3 else ""
	if not prompt_label.text.is_empty() and Input.is_action_just_pressed("interact"):
		open_npc(near_npc)

func dialog_begin(title: String,body: String) -> void:
	standings_label=null
	paused=true
	car.controlled=false
	car.speed=0
	car.velocity=Vector3.ZERO
	dialog_title.text=title
	dialog_text.text=body
	for child in dialog_buttons.get_children():
		dialog_buttons.remove_child(child)
		child.queue_free()
	dialog.show()

func option(words: String,action: Callable) -> void:
	var button := Button.new()
	button.text=words
	button.custom_minimum_size=Vector2(540,42)
	button.add_theme_font_size_override("font_size",18)
	button.add_theme_color_override("font_color",Color("fff3d8"))
	button.add_theme_stylebox_override("normal",panel_style(Color("315d60"),8))
	button.add_theme_stylebox_override("hover",panel_style(Color("497c79"),8))
	button.add_theme_stylebox_override("focus",panel_style(Color("497c79"),8))
	button.pressed.connect(action)
	dialog_buttons.add_child(button)
	if dialog_buttons.get_child_count()==1: button.grab_focus()

func open_npc(id: String) -> void:
	match id:
		"garage":
			var line := "The lighthouse keeper is waiting for a parcel. Take the coast road north. There's always something worth finding along the way."
			if state.quest_stage==1: line="Pip is beside the lighthouse, on the far side of the hill. Follow the coast road and look for the red lantern roof."
			if state.quest_stage==2: line="Pip sent word! That parcel will keep the lighthouse shining. Here's something towards your next set of wheels."
			if state.quest_stage==3: line="A good car becomes your own, one little improvement at a time. What are we working on?"
			dialog_begin("Mara  /  Harbour Garage",line)
			if state.quest_stage==0: option("I'll take the parcel",func(): state.accept_delivery(); save_progress(); close_dialog())
			if state.quest_stage==2: option("Collect 350 credits",func(): state.claim_delivery_reward(); save_progress(); open_npc("garage"))
			option("Parts & fitted equipment",open_garage)
			option("Try a new paint colour · free",func(): state.paint_index=(state.paint_index+1)%4; apply_equipment(); save_progress())
			option("Back to the road",close_dialog)
		"post":
			dialog_begin("Pip  /  Lighthouse Post","You made it! The view's worth the climb, isn't it? Leave the parcel with me, then tell Mara it arrived safe." if state.quest_stage==1 else "Some drivers only see the road. Others find the little places beside it. Keep your eyes open on the way down.")
			if state.quest_stage==1: option("Deliver Mara's parcel",func(): state.deliver_package(); save_progress(); close_dialog(); message("Delivery complete. Mara has your reward.",6))
			option("See you down the road",close_dialog)
		"cafe":
			dialog_begin("Rowan  /  The Daily Mile","Bayview Coast · Class C · 24 cars · 3 laps. Every valid finish pays 100–500 credits. A stock car faces a hard field: earn, fit parts and work your way up.")
			option("Race Bayview Coast · C",func(): close_dialog(); start_race())
			option("Tracks & progression",show_race_progression)
			option("Maybe after a coffee",close_dialog)

func open_garage() -> void:
	dialog_begin("Harbour Garage · %d cr" % state.credits,PARTS.summary(state.equipped))
	for slot in PARTS.SLOTS:
		var selected: String=slot
		option(slot.capitalize()+" · "+PARTS.PARTS[state.equipped[slot]].name,func(): show_parts(selected))
	option("Back to Mara",func(): open_npc("garage"))

func show_parts(slot: String) -> void:
	dialog_begin(slot.capitalize()+" · %d cr" % state.credits,"Owned parts can be refitted free. Buying a part adds it to your garage; choose Fit to use it.")
	for id in PARTS.PARTS:
		var part: Dictionary=PARTS.PARTS[id]
		if part.slot!=slot: continue
		var selected: String=id
		var suffix: String="fitted" if state.equipped[slot]==id else ("owned" if state.owned.has(id) else str(part.price)+" cr")
		option(part.name+" · "+suffix,func(): show_part(selected))
	option("Back to all parts",open_garage)

func show_part(id: String) -> void:
	var part: Dictionary=PARTS.PARTS[id]
	var preview: Dictionary=state.equipped.duplicate()
	preview[part.slot]=id
	dialog_begin(part.name+" · %d cr" % state.credits,part.hint+"\n\nFitted:\n"+PARTS.summary(state.equipped)+"\n\nWith this part:\n"+PARTS.summary(preview))
	if not state.owned.has(id):
		option("Buy · %d cr" % part.price,func():
			if state.buy_part(id): save_progress(); show_part(id)
			else: message("Not enough credits. Every completed race earns more.",5))
	elif state.equipped[part.slot]!=id:
		option("Fit owned part · free",func(): state.fit_part(id); apply_equipment(); save_progress(); show_part(id))
	option("Back to "+String(part.slot),func(): show_parts(part.slot))

func show_race_progression() -> void:
	var best: int=state.best_results.get("bayview_C",0)
	var record: String="No finish recorded" if best==0 else "Best finish: %d / 24" % best
	dialog_begin("Coastal race calendar",record+"\n\nBayview Coast C is open. Quarry Link is planned: a top-12 Bayview result will qualify when the track is released. Later classes bring stronger fixed rival setups.")
	option("Back to Rowan",func(): open_npc("cafe"))

func apply_equipment() -> void:
	car.engine_level=state.engine_level
	car.trail_tyres=state.trail_tyres
	car.tuning=PARTS.stats(state.equipped)
	car.set_paint(paint_colors[state.paint_index])

func close_dialog() -> void:
	dialog.hide()
	paused=false
	car.controlled=true
	standings_label=null
	if race and (not race.active or race.player_has_finished()) and not race.cars.is_empty(): race.clear()

func show_pause() -> void:
	dialog_begin("A moment by the roadside","Your progress saves after discoveries, purchases and completed tasks. Take your time.")
	option("Back to the road",close_dialog)
	option("Recover to the road",func(): close_dialog(); car.reset_to_road(); snap_camera())
	if race and race.active: option("Retire from race · no payout",func(): race.clear(); close_dialog())
	option("Save and quit",func(): save_progress(); get_tree().quit())

func save_progress() -> void:
	if capture_mode: return
	var error := state.save()
	if error!=OK: message("Could not save progress. Check available disk space.",6)

func message(words: String,seconds: float=4) -> void:
	message_label.text=words
	status_timer=seconds

func start_race() -> void:
	race_serial=state.begin_race()
	save_progress()
	race.start(world,car,24,race_serial)
	snap_camera()
	message("Build your way through the field. Every finish earns credits.",5)

func race_finished(place: int, seconds: float) -> void:
	var payout:=state.finish_race(race_serial,"bayview_C",place)
	save_progress()
	dialog_begin("Finished %d / 24" % place,"Bayview Coast C · %.1f seconds\nRace earnings: %d credits · Balance: %d\n\nFit stronger parts at Harbour Garage and return to the same field." % [seconds,payout,state.credits])
	option("Race again",func(): close_dialog(); start_race())
	option("Full standings",show_standings)
	option("Return to the village",func(): race.clear(); close_dialog(); car.place_on_road(15); snap_camera())
	option("Visit the garage",func(): race.clear(); car.place_on_road(36); snap_camera(); open_garage())

func show_standings() -> void:
	dialog_begin("Bayview C · full standings","The remaining cars continue racing. Finished times are final.")
	option("Race again",func(): close_dialog(); start_race())
	option("Return to the village",func(): race.clear(); close_dialog(); car.place_on_road(15); snap_camera())
	var scroll:=ScrollContainer.new()
	scroll.custom_minimum_size=Vector2(600,300)
	scroll.horizontal_scroll_mode=ScrollContainer.SCROLL_MODE_DISABLED
	dialog_buttons.add_child(scroll)
	standings_label=label("",19,Color("304a4c"))
	var font:=SystemFont.new()
	font.font_names=PackedStringArray(["Consolas"])
	standings_label.add_theme_font_override("font",font)
	scroll.add_child(standings_label)
	update_standings()

func update_standings() -> void:
	if not is_instance_valid(standings_label) or race.records.is_empty(): return
	var lines: PackedStringArray=[]
	var place:=0
	for driver in race.standings():
		place+=1
		var record: Dictionary=race.records[driver]
		var driver_name:="YOU" if driver==23 else "Rival %02d" % (driver+1)
		var result:="%.2f s" % record.finish if record.finish>=0 else "Racing · lap %d" % mini(3,1+int((record.gate-1)/8))
		lines.append("%02d  %-12s  %s" % [place,driver_name,result])
	standings_label.text="\n".join(lines)

func capture_screenshot(path: String) -> void:
	await RenderingServer.frame_post_draw
	get_viewport().get_texture().get_image().save_png(path)

func run_visual_review() -> void:
	if review_task_started: return
	review_task_started=true
	DirAccess.make_dir_recursive_absolute(capture_directory)
	car.controlled=true
	car.scripted=true
	await get_tree().create_timer(2).timeout
	await capture_screenshot(capture_directory.path_join("01-village.png"))
	car.place_on_road(world.total_length*.42)
	snap_camera()
	await get_tree().create_timer(1).timeout
	await capture_screenshot(capture_directory.path_join("02-coast.png"))
	car.place_on_road(36)
	await get_tree().create_timer(.5).timeout
	car.controlled=false
	paused=true
	camera.position=car.position+Vector3(4,2.6,-5)
	camera.look_at(car.position+Vector3.UP*.8)
	await get_tree().create_timer(.5).timeout
	await capture_screenshot(capture_directory.path_join("03-car.png"))
	close_dialog()
	start_race()
	await get_tree().create_timer(.5).timeout
	paused=true
	car.controlled=false
	camera.position=car.position+car.global_basis.z*15+Vector3.UP*8
	camera.look_at(car.position-car.global_basis.z*12)
	await get_tree().create_timer(.2).timeout
	await capture_screenshot(capture_directory.path_join("04-grid.png"))
	paused=false
	car.controlled=true
	race.autopilot_player=true
	await get_tree().create_timer(10).timeout
	await capture_screenshot(capture_directory.path_join("05-racing.png"))
	race.clear()
	open_garage()
	await get_tree().create_timer(.2).timeout
	await capture_screenshot(capture_directory.path_join("06-garage.png"))
	close_dialog()
	paused=true
	car.controlled=false
	var bridge_pose:=world.sample_road(204)
	camera.position=bridge_pose.origin+bridge_pose.basis.x*40+Vector3.UP*5
	camera.look_at(bridge_pose.origin-Vector3.UP*4)
	await get_tree().create_timer(.3).timeout
	await capture_screenshot(capture_directory.path_join("07-bridge.png"))
	camera.position=Vector3(-29,12.0,15)
	camera.look_at(Vector3(-43,10.0,9))
	await get_tree().create_timer(.3).timeout
	await capture_screenshot(capture_directory.path_join("08-workshop.png"))
	print("LITTLE_ROAD_CAPTURE_COMPLETE ",capture_directory)
	get_tree().quit()
