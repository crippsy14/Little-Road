extends Node3D
class_name RoadWorld

var road: PackedVector3Array = []
var road_distances: PackedFloat32Array = []
var total_length := 0.0
var materials: Dictionary = {}
var random := RandomNumberGenerator.new()
var sea_shader: ShaderMaterial
var static_root: Node3D
var foliage_mesh: SphereMesh

func _ready() -> void:
	random.seed = 271828
	static_root = Node3D.new()
	static_root.name = "Scenery"
	add_child(static_root)
	make_materials()
	make_road_points()
	make_environment()
	make_clouds()
	make_terrain()
	make_road()
	make_village()
	make_landmarks()
	make_nature()
	make_village_details()
	make_bridge()
	batch_static_details()

func material(key: String, color: Color, roughness: float = 0.9) -> StandardMaterial3D:
	var m := StandardMaterial3D.new()
	m.albedo_color = color
	m.roughness = roughness
	materials[key] = m
	return m

func make_materials() -> void:
	material("road", Color("5f6969"))
	material("line", Color("f4e9c9"))
	material("sand", Color("c6b58b"))
	material("stone", Color("b7aa8d"))
	material("wall", Color("f0dfb8"))
	material("wall_rose", Color("d7a28b"))
	material("wall_cream", Color("eadbc2"))
	material("wall_sage", Color("b7c2a0"))
	material("roof", Color("b66740"))
	material("roof_dark", Color("885b48"))
	material("wood", Color("755b3f"))
	material("teal", Color("426f6b"))
	material("navy", Color("263f49"))
	material("glass", Color("597b81"), 0.22)
	material("leaf", Color("65824b"))
	material("leaf_light", Color("8b9a58"))
	material("trunk", Color("79604b"))
	material("flower", Color("d996ae"))
	material("flower_gold", Color("e2bb54"))
	material("rubber",Color("263538"))
	material("metal",Color("566267"),.38)
	material("soil",Color("534933"))
	material("leaf_deep",Color("315840"))
	material("leaf_gold",Color("899343"))
	material("cliff",Color("817d70"))
	material("white",Color("eee5d1"))
	material("orange",Color("cb7045"))
	material("cloud",Color("f7f1e6"))
	# Original procedural surface detail in world space avoids stretched mesh UVs.
	for key in ["road","stone","wall","wall_cream","wall_rose","wall_sage","roof","wood"]:
		var base: Color=materials[key].albedo_color
		var shader:=Shader.new()
		shader.code="""shader_type spatial;
uniform vec4 tint : source_color;
uniform float scale = 5.0;
uniform float strength = 0.07;
varying vec3 wp;
void vertex(){wp=(MODEL_MATRIX*vec4(VERTEX,1.0)).xyz;}
float hash(vec3 p){return fract(sin(dot(p,vec3(127.1,311.7,74.7)))*43758.5453);}
void fragment(){float grain=hash(floor(wp*scale)); float broad=sin(wp.x*1.7+sin(wp.z*2.1))*sin(wp.z*1.5+wp.y*.6); ALBEDO=tint.rgb*(1.0+(grain-.5)*strength+broad*strength*.25); ROUGHNESS=.92;}
"""
		var textured:=ShaderMaterial.new()
		textured.shader=shader
		textured.set_shader_parameter("tint",base)
		textured.set_shader_parameter("scale",30.0 if key=="road" else 7.0)
		textured.set_shader_parameter("strength",.18 if key=="road" else .11)
		materials[key]=textured

func box(pos: Vector3, size: Vector3, mat: String, parent: Node3D = null, collision: bool = false) -> MeshInstance3D:
	var mesh := MeshInstance3D.new()
	var shape := BoxMesh.new()
	shape.size = size
	mesh.mesh = shape
	mesh.material_override = materials[mat]
	(parent if parent else static_root).add_child(mesh)
	mesh.position = pos
	if collision:
		var body := StaticBody3D.new()
		mesh.add_child(body)
		var col := CollisionShape3D.new()
		var form := BoxShape3D.new()
		form.size = size
		col.shape = form
		body.add_child(col)
	return mesh

func cylinder(pos: Vector3, radius: float, height: float, mat: String, parent: Node3D = null, top: float = -1.0) -> MeshInstance3D:
	var mesh := MeshInstance3D.new()
	var shape := CylinderMesh.new()
	shape.bottom_radius = radius
	shape.top_radius = radius if top < 0.0 else top
	shape.height = height
	shape.radial_segments = 12
	mesh.mesh = shape
	mesh.material_override = materials[mat]
	(parent if parent else static_root).add_child(mesh)
	mesh.position = pos
	return mesh

func text_sign(words: String, pos: Vector3, size: int, parent: Node3D, color := Color("f8eccf")) -> Label3D:
	var label := Label3D.new()
	label.text = words
	label.font_size = size
	label.pixel_size = 0.008
	label.modulate = color
	label.outline_size = 0
	label.no_depth_test = false
	parent.add_child(label)
	label.position = pos
	return label

func make_road_points() -> void:
	var controls: Array[Vector3] = [Vector3(-25,8,50),Vector3(-30,8,-30),Vector3(-16,10,-100),Vector3(45,17,-180),Vector3(125,22,-180),Vector3(158,12,-85),Vector3(118,8,15),Vector3(38,8,74)]
	build_road_points(controls)

func build_road_points(controls: Array[Vector3]) -> void:
	for i in range(controls.size()):
		var a: Vector3 = controls[posmod(i-1,controls.size())]
		var b: Vector3 = controls[i]
		var c: Vector3 = controls[(i+1)%controls.size()]
		var d: Vector3 = controls[(i+2)%controls.size()]
		for n in range(20):
			var t := float(n)/20.0
			road.append(0.5*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t))
	road.append(road[0])
	road_distances.append(0.0)
	for i in range(1,road.size()):
		total_length += road[i].distance_to(road[i-1])
		road_distances.append(total_length)

func sample_road(distance: float) -> Transform3D:
	var d := fposmod(distance,total_length)
	for i in range(road.size()-1):
		if road_distances[i+1] >= d:
			var fraction := (d-road_distances[i])/(road_distances[i+1]-road_distances[i])
			var pos := road[i].lerp(road[i+1],fraction)
			var direction := (road[i+1]-road[i]).normalized()
			return Transform3D(Basis.looking_at(direction,Vector3.UP),pos)
	return Transform3D.IDENTITY

func nearest_road(pos: Vector3) -> Dictionary:
	var best := INF
	var location := Vector3.ZERO
	var distance := 0.0
	for i in range(road.size()-1):
		var a := Vector2(road[i].x,road[i].z)
		var b := Vector2(road[i+1].x,road[i+1].z)
		var p := Vector2(pos.x,pos.z)
		var t := clampf((p-a).dot(b-a)/(b-a).length_squared(),0.0,1.0)
		var r := a.lerp(b,t)
		var delta := p.distance_squared_to(r)
		if delta < best:
			best = delta
			location = road[i].lerp(road[i+1],t)
			distance = lerpf(road_distances[i],road_distances[i+1],t)
	return {"offset":sqrt(best),"point":location,"distance":distance}

func height_at(x: float,z: float) -> float:
	var coast := 164.0 + 32.0*sin(z*0.017)
	var land := 9.0 + sin(x*.033)*cos(z*.025)*3.0
	land += 35.0*exp(-pow((x+100.0)/80.0,2)-pow((z+200.0)/120.0,2))
	land += 16.0*exp(-pow((x-70.0)/65.0,2)-pow((z+255.0)/70.0,2))
	land += 18.0*exp(-pow((x-137.0)/42.0,2)-pow((z+200.0)/50.0,2))
	land = lerpf(land,-12.0,smoothstep(coast-25,coast+20,x))
	# A connected tidal inlet opens a view from the village toward the peninsula.
	var inlet:=Vector2((x-80.0)/73.0,(z+87.0)/64.0).length()
	land=lerpf(land,-6.0,1.0-smoothstep(.72,1.08,inlet))
	var post_pad := Vector2(x-131.0,z+196.0).length()
	land = lerpf(land,23.0,1.0-smoothstep(5.5,13.0,post_pad))
	var near := nearest_road(Vector3(x,0,z))
	var influence := 1.0-smoothstep(7.0,19.0,near.offset)
	land = lerpf(land,near.point.y-0.32,influence)
	# The bridge deck retains the road collision; the channel passes beneath it.
	if near.distance>174 and near.distance<235:
		var channel:=smoothstep(174,185,near.distance)*(1.0-smoothstep(225,235,near.distance))
		land=lerpf(land,-3.0,channel*(1.0-smoothstep(12,24,near.offset)))
	# Flat village building pads.
	if x > -65 and x < -4 and z > -42 and z < 47:
		land = lerpf(land,7.85,1.0-smoothstep(22,32,abs(x+31)))
	return land

func terrain_surface_height(x: float,z: float) -> float:
	# Match the triangles actually rendered/collided, including steep banks.
	var x0:=floorf((x+240.0)/7.0)*7.0-240.0
	var z0:=floorf((z+350.0)/7.0)*7.0-350.0
	var u:=(x-x0)/7.0
	var v:=(z-z0)/7.0
	var h00:=height_at(x0,z0)
	var h11:=height_at(x0+7,z0+7)
	if v<=u:
		var h10:=height_at(x0+7,z0)
		return h00+(h10-h00)*u+(h11-h10)*v
	var h01:=height_at(x0,z0+7)
	return h00+(h11-h01)*u+(h01-h00)*v

func make_environment() -> void:
	var environment := WorldEnvironment.new()
	var settings := Environment.new()
	var sky := Sky.new()
	var sky_mat := ProceduralSkyMaterial.new()
	sky_mat.sky_top_color = Color("447fb1")
	sky_mat.sky_horizon_color = Color("a8cadd")
	sky_mat.sky_curve=.35
	sky_mat.ground_bottom_color = Color("788879")
	sky_mat.ground_horizon_color = Color("d2e2de")
	sky_mat.sun_angle_max = 18
	sky.sky_material = sky_mat
	settings.background_mode = Environment.BG_SKY
	settings.sky = sky
	settings.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	settings.ambient_light_color = Color("ccd9d9")
	settings.ambient_light_energy = 0.28
	settings.tonemap_mode = Environment.TONE_MAPPER_ACES
	settings.ssao_enabled = true
	settings.ssao_radius = 2
	settings.ssao_intensity = 1.4
	settings.fog_enabled = true
	settings.fog_light_color = Color("b7d1d4")
	settings.fog_density = 0.0007
	settings.fog_sky_affect = 0.12
	environment.environment = settings
	add_child(environment)
	var sun := DirectionalLight3D.new()
	sun.rotation_degrees = Vector3(-42,-36,0)
	sun.light_color = Color("fff1d2")
	sun.light_energy = 1.05
	sun.light_angular_distance = 2.0
	sun.shadow_enabled = true
	sun.directional_shadow_max_distance = 180
	add_child(sun)
	var ocean := MeshInstance3D.new()
	var plane := PlaneMesh.new()
	plane.size = Vector2(2200,2200)
	ocean.mesh = plane
	var shader := Shader.new()
	shader.code = "shader_type spatial; uniform vec3 shallow : source_color = vec3(0.18,0.49,0.57); void fragment(){ float wave=sin(VERTEX.x*0.21+TIME*0.8)*sin(VERTEX.z*0.18+TIME*0.5); ALBEDO=shallow+vec3(wave*0.025); ROUGHNESS=0.29; METALLIC=0.15; }"
	sea_shader = ShaderMaterial.new()
	sea_shader.shader = shader
	ocean.material_override = sea_shader
	ocean.position = Vector3(0,0,0)
	add_child(ocean)

func make_terrain() -> void:
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	var step := 7.0
	for xi in range(75):
		for zi in range(83):
			var x := -240.0+xi*step
			var z := -350.0+zi*step
			var points: Array[Vector3] = [Vector3(x,height_at(x,z),z),Vector3(x+step,height_at(x+step,z),z),Vector3(x+step,height_at(x+step,z+step),z+step),Vector3(x,height_at(x,z+step),z+step)]
			for index in [0,1,2,0,2,3]:
				var p := points[index]
				var shade := 0.96+0.08*sin(p.x*.037)*cos(p.z*.033)
				var color := Color("6d8548")*shade
				if p.y < 4: color = Color("c4b88e")*shade
				if p.y > 26: color = Color("60774f")*shade
				st.set_color(color)
				st.add_vertex(p)
	st.index()
	st.generate_normals()
	var mesh := MeshInstance3D.new()
	mesh.mesh = st.commit()
	var mat := StandardMaterial3D.new()
	mat.vertex_color_use_as_albedo = true
	mat.vertex_color_is_srgb = true
	mat.roughness = 1
	var terrain_noise:=FastNoiseLite.new()
	terrain_noise.seed=1927
	terrain_noise.frequency=.06
	var texture:=NoiseTexture2D.new()
	texture.width=512
	texture.height=512
	texture.seamless=true
	texture.noise=terrain_noise
	var ramp:=Gradient.new()
	ramp.set_color(0,Color(.70,.74,.65))
	ramp.set_color(1,Color(1.0,.98,.85))
	texture.color_ramp=ramp
	mat.albedo_texture=texture
	mat.uv1_triplanar=true
	mat.uv1_world_triplanar=true
	mat.uv1_scale=Vector3.ONE*.018
	mesh.material_override = mat
	add_child(mesh)
	mesh.create_trimesh_collision()

func road_strip(width: float, lift: float, mat: String) -> void:
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	for i in range(road.size()-1):
		var tangent_a := (road[(i+1)%(road.size()-1)]-road[posmod(i-1,road.size()-1)]).normalized()
		var tangent_b := (road[(i+2)%(road.size()-1)]-road[i]).normalized()
		var right_a := tangent_a.cross(Vector3.UP).normalized()*width*.5
		var right_b := tangent_b.cross(Vector3.UP).normalized()*width*.5
		var a := road[i]+Vector3.UP*lift
		var b := road[i+1]+Vector3.UP*lift
		for p in [a-right_a,b-right_b,b+right_b,a-right_a,b+right_b,a+right_a]: st.add_vertex(p)
	st.generate_normals()
	var mesh := MeshInstance3D.new()
	mesh.mesh = st.commit()
	mesh.material_override = materials[mat]
	add_child(mesh)
	if mat == "road": mesh.create_trimesh_collision()

func make_road() -> void:
	road_strip(11.6,-0.02,"sand")
	road_strip(9.0,0.01,"road")
	for i in range(road.size()-1):
		var a := road[i]
		var b := road[i+1]
		var direction := (b-a).normalized()
		var right := direction.cross(Vector3.UP).normalized()
		var length := a.distance_to(b)
		for sign_side in [-1,1]:
			var line := box((a+b)*.5+right*4.12*sign_side+Vector3.UP*.035,Vector3(.10,.02,length+.05),"line")
			line.basis = Basis.looking_at(direction,Vector3.UP)
		if i%2 == 0:
			var dash := box((a+b)*.5+Vector3.UP*.04,Vector3(.12,.025,length*.5),"line")
			dash.basis = Basis.looking_at(direction,Vector3.UP)
		if i > 72 and i < 123 and i%2 == 0:
			var wall := box((a+b)*.5+right*5.4+Vector3.UP*.48,Vector3(.58,.9,length+0.15),"stone",null,true)
			wall.basis = Basis.looking_at(direction,Vector3.UP)

func house(pos: Vector3, width: float, depth: float, height: float, wall: String, heading: float, name_text: String = "") -> void:
	var root := Node3D.new()
	static_root.add_child(root)
	root.position = pos
	root.rotation.y = heading
	box(Vector3(0,-.55,0),Vector3(width+1.2,1.4,depth+1.4),"stone",root)
	box(Vector3(0,height*.5,0),Vector3(width,height,depth),wall,root,true)
	box(Vector3(0,.38,depth*.5+.03),Vector3(width,.55,.08),"stone",root)
	var roof_vertices := PackedVector3Array([Vector3(-width*.56,height,depth*.58),Vector3(width*.56,height,depth*.58),Vector3(0,height+2,depth*.58),Vector3(-width*.56,height,-depth*.58),Vector3(width*.56,height,-depth*.58),Vector3(0,height+2,-depth*.58)])
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	for k in [0,1,2,5,4,3,0,2,5,0,5,3,2,1,4,2,4,5]: st.add_vertex(roof_vertices[k])
	st.generate_normals()
	var roof := MeshInstance3D.new()
	roof.mesh = st.commit()
	roof.material_override = materials["roof"]
	root.add_child(roof)
	# Tile rows give a readable roof pattern without individual tile objects.
	for row in range(7):
		var f := float(row)/7.0
		for s in [-1,1]:
			var ridge := box(Vector3(s*width*.56*f,height+2*(1-f)+.035,0),Vector3(.07,.08,depth*1.16),"roof_dark",root)
			ridge.rotation.z = s*atan2(-2.0,width*.56)
	box(Vector3(-width*.26,height+1.6,-depth*.2),Vector3(.65,1.5,.65),wall,root)
	box(Vector3(-width*.26,height+2.39,-depth*.2),Vector3(.8,.14,.8),"stone",root)
	for side in [-1,1]:
		for floor_i in range(2):
			var yp := 1.8+floor_i*2.45
			if yp+1.0 > height: continue
			var xp: float = side*width*.3
			box(Vector3(xp,yp,depth*.5+.07),Vector3(1.35,1.6,.12),"wood",root)
			box(Vector3(xp,yp,depth*.5+.14),Vector3(1.08,1.32,.03),"glass",root)
			box(Vector3(xp,yp,depth*.5+.18),Vector3(.07,1.36,.035),"wall",root)
			box(Vector3(xp,yp,depth*.5+.18),Vector3(1.12,.07,.035),"wall",root)
			for shutter in [-1,1]: box(Vector3(xp+shutter*.84,yp,depth*.5+.08),Vector3(.35,1.65,.14),"teal",root)
			box(Vector3(xp,yp-.91,depth*.5+.31),Vector3(1.68,.22,.65),"wood",root)
			for plant in range(4): flower(Vector3(xp-.55+plant*.37,yp-.67,depth*.5+.31),root)
	box(Vector3(0,1.4,depth*.5+.08),Vector3(1.28,2.8,.13),"teal",root)
	# Side elevations are prominent from the driving camera.
	for flank in [-1,1]:
		var facade:=Node3D.new()
		root.add_child(facade)
		facade.position.x=flank*(width*.5+.03)
		facade.rotation.y=flank*PI*.5
		for level in range(2):
			if 2.4+level*2.4>height: continue
			for section in [-1,1]:
				var wp:=Vector3(section*depth*.26,1.7+level*2.4,0)
				box(wp,Vector3(1.15,1.6,.12),"wood",facade)
				box(wp+Vector3(0,0,.09),Vector3(.91,1.31,.05),"glass",facade)
				box(wp+Vector3(0,0,.13),Vector3(.07,1.35,.06),"white",facade)
				box(wp+Vector3(0,0,.13),Vector3(.95,.07,.06),"white",facade)
				box(wp+Vector3(0,-.87,.12),Vector3(1.35,.14,.45),"stone",facade)
				for shutter in [-1,1]: box(wp+Vector3(shutter*.75,0,.04),Vector3(.27,1.65,.12),"teal",facade)
		for row in range(int(height/.55)):
			for edge in [-1,1]: box(Vector3(edge*(depth*.5-.21),.3+row*.55,.02),Vector3(.44,.49,.08),"stone",facade)
	if not name_text.is_empty():
		box(Vector3(0,3.4,depth*.5+.16),Vector3(width*.9,.85,.22),"navy",root)
		text_sign(name_text,Vector3(0,3.4,depth*.5+.285),43,root)
		for awning in range(8):
			var fabric := box(Vector3(-width*.46+(awning+.5)*width*.115,2.84,depth*.5+.65),Vector3(width*.116,.1,1.1),"wall" if awning%2 else "teal",root)
			fabric.rotation.x = .2

func flower(pos: Vector3,parent: Node3D) -> void:
	cylinder(pos,.18,.36,"leaf",parent,.13)
	cylinder(pos+Vector3(0,.18,0),.15,.11,"flower" if random.randf()>.45 else "flower_gold",parent)

func make_village() -> void:
	# Fronts face the main road; buildings leave generous driving space.
	garage(Vector3(-43,8,9),PI*.5)
	house(Vector3(-43,8,-8),7.5,7,6.8,"wall_rose",PI*.5,"TIDES & TYRES")
	house(Vector3(-45,8,-26),9,8,7.1,"wall",PI*.5)
	house(Vector3(-10,8,-9),7,6,6.6,"wall_sage",-PI*.5,"THE DAILY MILE")
	house(Vector3(-9,8,-29),8,7,7.8,"wall_cream",-PI*.5)
	house(Vector3(-8,8,22),7.5,7,5.6,"wall_rose",-PI*.5)
	box(Vector3(-39,8.02,10),Vector3(8,.12,15),"stone")
	for z in range(-38,40,6):
		for x in [-36.0,-18.0]:
			if abs(z-10)<8 and x < -30: continue
			var ground:=minf(7.7,height_at(x,z)-.05)
			box(Vector3(x,(8.17+ground)*.5,z),Vector3(2.5,8.17-ground,5.95),"stone")
			for joint in range(1,6): box(Vector3(x,8.177,z-3+joint),Vector3(2.4,.012,.025),"sand")
	for z in [-32.0,-12.0,26.0,41.0]:
		lamp_post(Vector3(-35.5,8,z))
		var planter := box(Vector3(-16.5,8.4,z),Vector3(1.3,.75,1.3),"stone")
		for j in range(4): flower(Vector3(-16.8+(j%2)*.5,8.96,z-.3+(j/2)*.5),static_root)
	signpost(Vector3(-17,8,40),"BAYVIEW", "COAST ROAD  ↑")

func lamp_post(pos: Vector3) -> void:
	cylinder(pos+Vector3.UP*2.2,.07,4.4,"navy")
	box(pos+Vector3(0,4.4,0),Vector3(.58,.08,.58),"navy")
	box(pos+Vector3(0,4.65,0),Vector3(.35,.45,.35),"wall")
	box(pos+Vector3(0,4.91,0),Vector3(.65,.10,.65),"navy")

func signpost(pos: Vector3,title: String,subtitle: String) -> void:
	box(pos+Vector3(0,1.5,0),Vector3(.2,3,.2),"wood")
	box(pos+Vector3(0,2.5,0),Vector3(3.6,1.65,.16),"wood")
	text_sign(title, pos+Vector3(0,2.76,.09),42,static_root)
	text_sign(subtitle,pos+Vector3(0,2.2,.09),22,static_root)

func make_landmarks() -> void:
	var pos := Vector3(150,0,-195)
	pos.y = height_at(pos.x,pos.z)
	cylinder(pos+Vector3.UP*6,2.3,12,"wall",null,1.65)
	cylinder(pos+Vector3.UP*9,2.0,1.6,"roof")
	cylinder(pos+Vector3.UP*12.5,2.3,.6,"navy")
	cylinder(pos+Vector3.UP*13.4,1.5,1.4,"glass")
	cylinder(pos+Vector3.UP*14.5,2.1,1,"roof",null,.12)
	house(Vector3(131,height_at(131,-196),-196),6,6,3.2,"wall_cream",0,"LIGHTHOUSE POST")
	signpost(Vector3(56,18,-191),"LIGHTHOUSE", "FOLLOW THE COAST  →")
	# A roadside overlook with a low wall and bench.
	var overlook := Vector3(175,height_at(175,-75),-75)
	box(overlook,Vector3(10,.25,8),"stone")
	box(overlook+Vector3(4.7,.6,0),Vector3(.45,1.3,8),"stone",null,true)
	box(overlook+Vector3(2,.8,0),Vector3(.75,.18,3),"wood")
	box(overlook+Vector3(2.4,1.3,0),Vector3(.15,.75,3),"wood")

func tree(pos: Vector3,scale_factor: float) -> void:
	pos.y=terrain_surface_height(pos.x,pos.z)-.08
	var root:=Node3D.new()
	static_root.add_child(root)
	root.position=pos
	root.scale=Vector3.ONE*scale_factor
	cylinder(Vector3(0,1.6,0),.19,3.2,"trunk",root,.12)
	var kind:=random.randf()
	if kind<.20:
		# Narrow coastal cypresses break up the former round-tree silhouette.
		for layer in range(5):
			var radius:=.8-float(layer)*.105
			blob(Vector3(sin(layer*1.7)*.12,2.2+layer*.88,cos(layer*1.3)*.1),Vector3(radius,1.35,radius*.83),"leaf_deep" if layer%2 else "leaf",root)
	elif kind<.42:
		for layer in range(5):
			var radius:=1.55-layer*.23
			for cluster in range(4):
				var angle:=cluster*TAU/4+layer*.63
				blob(Vector3(cos(angle)*radius*.35,2.3+layer*.75,sin(angle)*radius*.35),Vector3(radius,.85,radius),"leaf_deep" if layer%2 else "leaf",root)
	else:
		for branch in range(5):
			var angle:=branch*TAU/5+.3
			var end:=Vector3(cos(angle)*1.5,3.2+float(branch%2)*.5,sin(angle)*1.5)
			beam(Vector3(0,2.0,0),end,.11,"trunk",root)
			blob(end+Vector3.UP*.5,Vector3(1.4,1.7,1.4),"leaf" if branch%2 else "leaf_light",root)
		blob(Vector3(0,4.35,0),Vector3(1.6,1.5,1.6),"leaf_gold",root)

func make_nature() -> void:
	for i in range(210):
		var x := random.randf_range(-145,180)
		var z := random.randf_range(-265,145)
		var pos := Vector3(x,0,z)
		var near := nearest_road(pos)
		if near.offset < 12 or (x > -65 and x < 0 and z>-50 and z<50): continue
		pos.y = height_at(x,z)
		if pos.y < 4: continue
		tree(pos,random.randf_range(.65,1.65))
	for i in range(60):
		var x := random.randf_range(155,210)
		var z := random.randf_range(-270,130)
		var y := height_at(x,z)
		if y>18 or y < -3 or nearest_road(Vector3(x,y,z)).offset<8: continue
		var rock := MeshInstance3D.new()
		var sphere := SphereMesh.new()
		sphere.radial_segments = 7
		sphere.rings = 3
		rock.mesh = sphere
		rock.material_override = materials["stone"]
		static_root.add_child(rock)
		rock.position = Vector3(x,y,z)
		rock.scale = Vector3(random.randf_range(2,7),random.randf_range(2,6),random.randf_range(2,7))
	# Small plants and weathered rocks make the road shoulder less empty without
	# putting new physical obstacles inside the driving envelope.
	for index in range(100):
		var pose:=sample_road(float(index)*total_length/100)
		var side: float=-1 if index%2 else 1
		var position:=pose.origin+pose.basis.x*side*random.randf_range(8.5,14)
		if position.x> -65 and position.x<5 and position.z> -65 and position.z<60: continue
		position.y=height_at(position.x,position.z)
		if position.y<3: continue
		if index%4==0:
			blob(position+Vector3.UP*.3,Vector3(.85,.6,.7),"stone")
		else:
			shrub(position,random.randf_range(.35,.65),index%3==0)

func make_clouds() -> void:
	var cloud_mesh:=QuadMesh.new()
	cloud_mesh.size=Vector2(130,58)
	var shader:=Shader.new()
	shader.code="""shader_type spatial;
render_mode unshaded, blend_mix, depth_draw_never, cull_disabled;
float hash(vec2 p){return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453);}
float noise(vec2 p){vec2 i=floor(p); vec2 f=fract(p); f=f*f*(3.0-2.0*f); return mix(mix(hash(i),hash(i+vec2(1,0)),f.x),mix(hash(i+vec2(0,1)),hash(i+vec2(1,1)),f.x),f.y);}
void fragment(){
 vec2 p=(UV-.5)*vec2(2.0,2.7);
 float clouds=0.0;
 for(int i=0;i<5;i++){float x=float(i)*.31-.62; vec2 q=p-vec2(x,sin(float(i)*1.9)*.15); clouds=max(clouds,1.0-length(q/vec2(.43,.62)));}
 float detail=noise(UV*14.0)*.20+noise(UV*31.0)*.07;
 ALPHA=smoothstep(.06,.34,clouds+detail-.12)*.90;
 ALBEDO=mix(vec3(.76,.83,.85),vec3(1.0,.98,.93),smoothstep(0.0,1.0,UV.y));
}
"""
	var cloud_material:=ShaderMaterial.new()
	cloud_material.shader=shader
	var cloud_rng:=RandomNumberGenerator.new()
	cloud_rng.seed=4051
	for group in range(20):
		var angle:=float(group)*TAU/20
		var center:=Vector3(cos(angle)*620,cloud_rng.randf_range(120,190),sin(angle)*620)
		var cloud:=MeshInstance3D.new()
		cloud.mesh=cloud_mesh
		cloud.material_override=cloud_material
		cloud.cast_shadow=GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
		add_child(cloud)
		cloud.position=center
		cloud.look_at(Vector3(0,20,0))

func blob(pos: Vector3, size: Vector3, mat: String, parent: Node3D = null) -> MeshInstance3D:
	if not foliage_mesh:
		foliage_mesh=SphereMesh.new()
		foliage_mesh.radius=1
		foliage_mesh.height=2
		foliage_mesh.radial_segments=10
		foliage_mesh.rings=5
	var node:=MeshInstance3D.new()
	node.mesh=foliage_mesh
	node.material_override=materials[mat]
	(parent if parent else static_root).add_child(node)
	node.position=pos
	node.scale=size
	return node

func beam(a: Vector3,b: Vector3,width: float,mat: String,parent: Node3D=null) -> void:
	var member:=box((a+b)*.5,Vector3(width,width,a.distance_to(b)),mat,parent)
	member.basis=Basis.looking_at((b-a).normalized(),Vector3.UP if absf((b-a).normalized().y)<.99 else Vector3.RIGHT)

func tyre(pos: Vector3,parent: Node3D,vertical: bool=false) -> void:
	var wheel:=MeshInstance3D.new()
	var torus:=TorusMesh.new()
	torus.inner_radius=.24
	torus.outer_radius=.48
	torus.rings=16
	torus.ring_segments=8
	wheel.mesh=torus
	wheel.material_override=materials.rubber
	parent.add_child(wheel)
	wheel.position=pos
	if vertical: wheel.rotation.x=PI*.5

func garage(pos: Vector3,heading: float) -> void:
	var root:=Node3D.new()
	static_root.add_child(root)
	root.position=pos
	root.rotation.y=heading
	box(Vector3(0,-.3,0),Vector3(11,.6,8),"stone",root)
	box(Vector3(0,.035,0),Vector3(10,.07,7.8),"stone",root)
	box(Vector3(-4.8,2.1,0),Vector3(.4,4.2,7.5),"wall_cream",root,true)
	box(Vector3(4.8,2.1,0),Vector3(.4,4.2,7.5),"wall_cream",root,true)
	box(Vector3(0,2.1,-3.65),Vector3(10,4.2,.3),"wall_cream",root,true)
	for side in [-1,1]:
		box(Vector3(side*4,2.1,3.65),Vector3(2,4.2,.4),"wall_cream",root,true)
		box(Vector3(side*2.95,1.85,3.88),Vector3(.18,3.7,.24),"navy",root)
	box(Vector3(0,3.8,3.87),Vector3(6.1,.25,.3),"navy",root)
	# Tall gable above the open service bay.
	var st:=SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	for p in [Vector3(-5,4.2,3.85),Vector3(0,6.4,3.85),Vector3(5,4.2,3.85)]: st.add_vertex(p)
	st.generate_normals()
	var front:=MeshInstance3D.new()
	front.mesh=st.commit()
	front.material_override=materials.wall_cream
	root.add_child(front)
	for side in [-1,1]:
		var slope:=box(Vector3(side*2.55,5.3,0),Vector3(5.7,.22,8.4),"roof",root)
		slope.rotation.z=-side*atan2(2.2,5.1)
		beam(Vector3(0,6.52,4.24),Vector3(side*5.25,4.25,4.24),.18,"navy",root)
	box(Vector3(0,4.18,3.9),Vector3(6.6,.55,.17),"white",root)
	text_sign("HARBOUR GARAGE",Vector3(0,4.4,4.03),59,root,Color("30434d"))
	text_sign("SERVICE  /  PARTS  /  ROAD READY",Vector3(0,3.98,4.04),23,root,Color("30434d"))
	# Workbench, shelves and tools remain visible through the bay.
	box(Vector3(0,1.0,-2.8),Vector3(7,.2,.8),"wood",root)
	for x in [-3.2,3.2]: box(Vector3(x,.5,-2.8),Vector3(.12,1,.6),"metal",root)
	for level in [1.8,2.65]:
		box(Vector3(0,level,-3.3),Vector3(7,.12,.55),"wood",root)
		for j in range(8):
			cylinder(Vector3(-3+j*.82,level+.23,-3.24),.13,.36,"orange" if j%3==0 else "teal",root)
	box(Vector3(-3.7,.75,.7),Vector3(1.1,1.4,1.3),"orange",root)
	for drawer in range(4): box(Vector3(-3.7,.3+drawer*.29,1.36),Vector3(.8,.055,.08),"metal",root)
	for x in [-1.05,1.05]: box(Vector3(x,.15,.0),Vector3(.4,.23,3.8),"metal",root)
	for j in range(3): tyre(Vector3(3.7,.28+j*.33,1.6),root)
	for j in range(2): tyre(Vector3(-4.2,.52+j*.85,4.25),root,true)
	box(Vector3(0,3.5,-.2),Vector3(3.6,.06,.32),"white",root)
	var warm:=OmniLight3D.new()
	root.add_child(warm)
	warm.position=Vector3(0,3.2,0)
	warm.light_color=Color("ffd898")
	warm.light_energy=1.6
	warm.omni_range=7
	# Small sign and flags frame the entrance without obstructing the drive.
	box(Vector3(4.1,1.65,3.92),Vector3(1.0,1.8,.08),"navy",root)
	text_sign("KEEP\nEXPLORING",Vector3(4.1,1.65,4.0),27,root)
	for i in range(8):
		var flag:=box(Vector3(-2.65+i*.75,3.5-sin(float(i)/7*PI)*.22,4.12),Vector3(.45,.43,.025),"orange" if i%2 else "teal",root)
		flag.rotation.z=.12 if i%2 else -.12

func stone_border(a: Vector3,b: Vector3,height: float=.8) -> void:
	var count:=maxi(1,int(a.distance_to(b)/1.15))
	for i in range(count+1):
		var p:=a.lerp(b,float(i)/count)
		p.y=height_at(p.x,p.z)
		for row in range(2):
			var stone:=box(p+Vector3(0,(row+.5)*height*.5,0),Vector3(a.distance_to(b)/count-.025,height*.47,.48),"stone")
			stone.rotation.y=atan2((b-a).x,(b-a).z)+PI*.5
			stone.rotation.z=random.randf_range(-.035,.035)

func shrub(pos: Vector3,size: float=1.0,flowers: bool=false) -> void:
	pos.y+=terrain_surface_height(pos.x,pos.z)-height_at(pos.x,pos.z)
	for i in range(3):
		var offset:=Vector3(cos(i*2.1)*.4,.5,sin(i*2.1)*.4)*size
		blob(pos+offset,Vector3(.65,.62,.65)*size,"leaf_deep" if i==0 else "leaf",null)
	if flowers:
		for i in range(7):
			var offset:=Vector3(random.randf_range(-.7,.7),random.randf_range(.65,1.12),random.randf_range(-.5,.5))*size
			blob(pos+offset,Vector3.ONE*.15*size,"flower" if i%2 else "flower_gold")

func make_village_details() -> void:
	# Planting at the edges of the forecourts, with the racing envelope kept clear.
	for spec in [Vector2(-39,31),Vector2(-39,-37),Vector2(-15,35),Vector2(-12,-42),Vector2(-51,24),Vector2(-54,-16)]:
		var pos:=Vector3(spec.x,height_at(spec.x,spec.y),spec.y)
		tree(pos,.8)
	for z in range(-43,42,3):
		for x in [-38.8,-15.2]:
			if (x< -30 and z>-1 and z<20) or (x> -30 and z>-15 and z<-3): continue
			var p:=Vector3(x,height_at(x,z),z)
			box(p+Vector3.UP*.17,Vector3(1.7,.35,2.6),"soil")
			shrub(p+Vector3.UP*.2,.75,true)
	stone_border(Vector3(-38,0,23),Vector3(-38,0,40))
	stone_border(Vector3(-17,0,23),Vector3(-17,0,42))
	# Narrow weatherboards and roof guttering add scale to blank masonry.
	var board:=Node3D.new()
	static_root.add_child(board)
	board.position=Vector3(-37.5,8.2,21)
	board.rotation.y=.45
	box(Vector3(0,.9,0),Vector3(1.2,1.65,.12),"navy",board)
	for x in [-.64,.64]: box(Vector3(x,.85,0),Vector3(.1,1.85,.16),"wood",board)
	text_sign("GOOD CARS\nHAPPY ROADS\n\nFUEL • FIX\nEXPLORE",Vector3(0,1.0,.075),23,board)
	# Café furniture is on the forecourt, outside the track.
	for z in [-6.0,-12.0]:
		var p:=Vector3(-15.2,8.05,z)
		cylinder(p+Vector3.UP*.9,.7,.13,"wood")
		cylinder(p+Vector3.UP*.45,.08,.9,"navy")
		for s in [-1,1]:
			box(p+Vector3(.95*s,.47,0),Vector3(.55,.13,.55),"wood")
			box(p+Vector3(1.15*s,.8,0),Vector3(.12,.6,.55),"wood")
	# Shore planting and rocky outcrops create an irregular coast around the inlet.
	for i in range(100):
		var angle:=float(i)*TAU/100
		var x:=80+75*cos(angle)
		var z:=-87+65*sin(angle)
		var p:=Vector3(x,height_at(x,z),z)
		if nearest_road(p).offset<9 or p.y<0: continue
		blob(p-Vector3.UP*.4,Vector3(random.randf_range(2,4),random.randf_range(2,4),random.randf_range(2,4)),"cliff")
		if i%2==0: shrub(p+Vector3.UP*.3,1.4)
	# Low shrubs break up the monotonous grass close to the road.
	for i in range(240):
		var pose:=sample_road(random.randf_range(0,total_length))
		var p:=pose.origin+pose.basis.x*random.randf_range(10,24)*(1 if i%2 else -1)
		p.y=height_at(p.x,p.z)
		if p.y<3 or (p.x>-65 and p.x<0 and p.z>-50 and p.z<50): continue
		shrub(p,random.randf_range(.5,1.25),i%7==0)

func make_bridge() -> void:
	# Three open arches below the existing continuous driving surface.
	for distance in [190.0,204.0,218.0]:
		var root:=Node3D.new()
		static_root.add_child(root)
		root.transform=sample_road(distance)
		for segment in range(12):
			var z1:=-7.0+segment*14.0/12
			var z2:=-7.0+(segment+1)*14.0/12
			var y1:=-8.8+sqrt(maxf(0,49-z1*z1))*.87
			var y2:=-8.8+sqrt(maxf(0,49-z2*z2))*.87
			var points:=[Vector3(-4.9,-.25,z1),Vector3(4.9,-.25,z1),Vector3(4.9,-.25,z2),Vector3(-4.9,-.25,z2),Vector3(-4.9,y1,z1),Vector3(4.9,y1,z1),Vector3(4.9,y2,z2),Vector3(-4.9,y2,z2)]
			var st:=SurfaceTool.new()
			st.begin(Mesh.PRIMITIVE_TRIANGLES)
			for k in [0,1,2,0,2,3,4,7,6,4,6,5,0,4,5,0,5,1,3,2,6,3,6,7,0,3,7,0,7,4,1,5,6,1,6,2]: st.add_vertex(points[k])
			st.generate_normals()
			var mesh:=MeshInstance3D.new()
			mesh.mesh=st.commit()
			mesh.material_override=materials.stone
			root.add_child(mesh)
		for side in [-1,1]:
			for row in range(2):
				for section in range(12): box(Vector3(side*4.94,-.6-row*.6,-6.4+section*1.15),Vector3(.1,.52,1.08),"stone",root)
	for distance in range(183,227,2):
		var pose:=sample_road(distance)
		for side in [-1,1]:
			var p: Vector3=pose.origin+pose.basis.x*5.0*side
			var wall:=box(p+Vector3.UP*.47,Vector3(.6,.94,2.08),"stone",null,true)
			wall.basis=pose.basis
			var cap:=box(p+Vector3.UP*.99,Vector3(.74,.12,2.10),"wall_cream")
			cap.basis=pose.basis
	for distance in [176,180,183,197,211,225,228,232]:
		var pose:=sample_road(distance)
		var length:=4.2 if distance in [176,180,228,232] else 1.8
		var pier:=box(pose.origin-Vector3.UP*(pose.origin.y+3.4)*.5,Vector3(9.8,pose.origin.y+2.6,length),"stone")
		pier.rotation.y=pose.basis.get_euler().y

func batch_static_details() -> void:
	# Batch common primitives after dressing; physics bodies stay in the tree.
	var groups: Dictionary={}
	for item in static_root.find_children("*","MeshInstance3D",true,false):
		var node: MeshInstance3D=item
		var source: Mesh=node.mesh
		var transform:=node.global_transform
		var key: String
		var unit: Mesh
		if source is BoxMesh:
			key="box"
			transform.basis=transform.basis.scaled_local(source.size)
			var cube:=BoxMesh.new()
			cube.size=Vector3.ONE
			unit=cube
		elif source==foliage_mesh:
			key="foliage"
			unit=source
		else: continue
		key+="_"+str(node.material_override.get_instance_id())
		if not groups.has(key): groups[key]={"mesh":unit,"material":node.material_override,"transforms":[]}
		groups[key].transforms.append(transform)
		node.mesh=null
	for key in groups:
		var data: Dictionary=groups[key]
		var batch:=MultiMeshInstance3D.new()
		var mm:=MultiMesh.new()
		mm.transform_format=MultiMesh.TRANSFORM_3D
		mm.mesh=data.mesh
		mm.instance_count=data.transforms.size()
		for i in range(mm.instance_count): mm.set_instance_transform(i,data.transforms[i])
		batch.multimesh=mm
		batch.material_override=data.material
		add_child(batch)
