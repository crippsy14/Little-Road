extends RoadWorld

# Regression fixture: four alternating bends immediately after the start.
# Uses the production road mesh/collision, physical cars and race controller.
func make_road_points() -> void:
	build_road_points([Vector3(-25,8,50),Vector3(-35,8,22),Vector3(-15,8,-6),Vector3(-35,8,-34),Vector3(-15,8,-62),Vector3(45,8,-105),Vector3(125,8,-75),Vector3(150,8,0),Vector3(105,8,65),Vector3(30,8,90)])

func height_at(_x: float,_z: float) -> float: return 7.68
func make_village() -> void: pass
func make_village_details() -> void: pass
func make_landmarks() -> void: pass
func make_bridge() -> void: pass
func make_nature() -> void: pass
func make_road() -> void:
	road_strip(11.6,-.02,"sand")
	road_strip(9.0,.01,"road")
