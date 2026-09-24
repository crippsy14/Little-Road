extends SceneTree

func _initialize() -> void: call_deferred("run")

func run() -> void:
	var scene:=Node3D.new()
	root.add_child(scene)
	var env:=WorldEnvironment.new()
	var settings:=Environment.new()
	settings.background_mode=Environment.BG_COLOR
	settings.background_color=Color("b8d1da")
	settings.ambient_light_source=Environment.AMBIENT_SOURCE_COLOR
	settings.ambient_light_color=Color("c4d8e3")
	settings.ambient_light_energy=.55
	settings.tonemap_mode=Environment.TONE_MAPPER_ACES
	settings.ssao_enabled=true
	env.environment=settings
	scene.add_child(env)
	var light:=DirectionalLight3D.new()
	light.rotation_degrees=Vector3(-45,-32,0)
	light.light_color=Color("ffeccf")
	light.shadow_enabled=true
	scene.add_child(light)
	var floor_mesh:=MeshInstance3D.new()
	var plane:=PlaneMesh.new()
	plane.size=Vector2(200,200)
	floor_mesh.mesh=plane
	var material:=StandardMaterial3D.new()
	material.albedo_color=Color("c5c0ad")
	floor_mesh.material_override=material
	scene.add_child(floor_mesh)
	var styles:=["compact","utility","coupe","formula"]
	var colors:=[Color("399e9a"),Color("dfb64a"),Color("c75f42"),Color("527faa")]
	for index in range(4):
		var car:=LittleCar.new()
		car.body_style=styles[index]
		car.controlled=false
		car.position=Vector3((index-1.5)*3.8,0,0)
		car.rotation.y=.3
		scene.add_child(car)
		car.set_paint(colors[index])
	var camera:=Camera3D.new()
	scene.add_child(camera)
	camera.position=Vector3(6.5,4.0,-10.5)
	camera.look_at(Vector3(0,.55,0))
	camera.fov=56
	var label:=Label.new()
	label.text="LITTLE ROAD  /  ORIGINAL BODY STUDIES\nOpen-wheel  ·  Coupe  ·  Utility  ·  Compact"
	label.position=Vector2(42,34)
	label.add_theme_color_override("font_color",Color("233c46"))
	label.add_theme_font_size_override("font_size",28)
	root.add_child(label)
	await create_timer(1).timeout
	await RenderingServer.frame_post_draw
	var path:=ProjectSettings.globalize_path("res://../review/captures/evening-07-final/09-body-gallery.png")
	DirAccess.make_dir_recursive_absolute(path.get_base_dir())
	root.get_texture().get_image().save_png(path)
	print("BODY_GALLERY_COMPLETE")
	quit()
