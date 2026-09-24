extends SceneTree

func _initialize() -> void:
	var path := ProjectSettings.globalize_path("res://../../little-road-windows/ENGINE-LICENSES.txt")
	var file := FileAccess.open(path,FileAccess.WRITE)
	if not file:
		push_error("Could not write engine license notices")
		quit(1)
		return
	file.store_string("Godot Engine\n\n"+Engine.get_license_text())
	file.store_string("\n\nThird-party copyright information\n\n"+JSON.stringify(Engine.get_copyright_info(),"  "))
	file.store_string("\n\nThird-party license texts\n\n"+JSON.stringify(Engine.get_license_info(),"  "))
	file.close()
	print("Wrote engine notices to ",path)
	quit()
