extends RefCounted
class_name AdventureState

const VERSION := 3
const CATALOG = preload("res://scripts/parts_catalog.gd")
var credits := 0
var quest_stage := 0
var engine_level := 0
var trail_tyres := false
var collected: Array[String] = []
var race_wins := 0
var town_service := false
var paint_index := 0
var save_path := "user://little-road-save.json"
var equipped: Dictionary = CATALOG.stock()
var owned: Array[String] = ["engine_stock","turbo_stock","tyres_stock","transmission_stock","brakes_stock","steering_stock","chassis_stock"]
var race_serial := 0
var settled_serial := 0
var best_results: Dictionary = {}

func buy_part(id: String) -> bool:
	if not CATALOG.PARTS.has(id) or owned.has(id): return false
	var price: int=CATALOG.PARTS[id].price
	if credits<price: return false
	credits-=price
	owned.append(id)
	return true

func fit_part(id: String) -> bool:
	if not owned.has(id) or not CATALOG.PARTS.has(id): return false
	equipped[CATALOG.PARTS[id].slot]=id
	engine_level=0 if equipped.engine=="engine_stock" else 1
	trail_tyres=equipped.tyres=="tyres_trail"
	return true

func begin_race() -> int:
	race_serial+=1
	return race_serial

func finish_race(serial: int, event: String, place: int) -> int:
	if serial!=race_serial or serial<=settled_serial or place<1 or place>24: return 0
	# Only authored events can award money. More tracks get explicit definitions.
	if event!="bayview_C": return 0
	var payout:=CATALOG.reward(place,"C")
	settled_serial=serial
	credits+=payout
	best_results[event]=mini(place,int(best_results.get(event,25)))
	if place==1: race_wins+=1
	return payout

func accept_delivery() -> bool:
	if quest_stage!=0: return false
	quest_stage = 1
	return true

func deliver_package() -> bool:
	if quest_stage!=1: return false
	quest_stage = 2
	return true

func claim_delivery_reward() -> bool:
	if quest_stage!=2: return false
	quest_stage = 3
	credits += 350
	return true

func buy_engine() -> bool:
	if not buy_part("engine_workshop"): return false
	return fit_part("engine_workshop")

func buy_tyres() -> bool:
	if not buy_part("tyres_trail"): return false
	return fit_part("tyres_trail")

func collect(id: String) -> bool:
	if collected.has(id): return false
	collected.append(id)
	credits += 50
	return true

func encode() -> Dictionary:
	return {"version":VERSION,"credits":credits,"quest_stage":quest_stage,"engine_level":engine_level,"trail_tyres":trail_tyres,"collected":collected,"race_wins":race_wins,"town_service":town_service,"paint_index":paint_index,"equipped":equipped.duplicate(),"owned":owned.duplicate(),"race_serial":race_serial,"settled_serial":settled_serial,"best_results":best_results.duplicate()}

func save() -> Error:
	var temporary := save_path+".tmp"
	var file := FileAccess.open(temporary,FileAccess.WRITE)
	if not file: return FileAccess.get_open_error()
	file.store_string(JSON.stringify(encode(),"  "))
	file.close()
	return DirAccess.rename_absolute(temporary,save_path)

func load_save() -> bool:
	if not FileAccess.file_exists(save_path): return false
	var file := FileAccess.open(save_path,FileAccess.READ)
	if not file: return false
	var data = JSON.parse_string(file.get_as_text())
	if not data is Dictionary: return false
	if data.get("version",0)!=1 and data.get("version",0)!=2 and data.get("version",0)!=VERSION: return false
	# Reject invalid saves as a unit. Never partially apply untrusted data.
	for key in ["credits","quest_stage","engine_level","race_wins","paint_index"]:
		if not data.has(key) or not (data[key] is float or data[key] is int): return false
	if data.credits<0 or data.credits>10000000 or data.quest_stage<0 or data.quest_stage>3: return false
	if data.engine_level<0 or data.engine_level>1 or data.race_wins<0 or data.paint_index<0 or data.paint_index>3: return false
	if not data.get("collected") is Array or not data.get("trail_tyres") is bool or not data.get("town_service") is bool: return false
	var ids: Array[String] = []
	for item in data.collected:
		if not item is String or ids.has(item): return false
		ids.append(item)
	var new_owned: Array[String]=["engine_stock","turbo_stock","tyres_stock","transmission_stock","brakes_stock","steering_stock","chassis_stock"]
	var new_fit:=CATALOG.stock()
	var new_serial:=0
	var new_settled:=0
	var new_results: Dictionary={}
	if data.version==1:
		# Preserve investments from the early two-part prototype.
		if data.engine_level==1:
			new_owned.append("engine_workshop")
			new_fit.engine="engine_workshop"
		if data.trail_tyres:
			new_owned.append("tyres_trail")
			new_fit.tyres="tyres_trail"
	else:
		if not data.get("owned") is Array or not data.get("equipped") is Dictionary: return false
		new_owned.clear()
		for id in data.owned:
			if not id is String or not CATALOG.PARTS.has(id) or new_owned.has(id): return false
			new_owned.append(id)
		if data.version==2 and not new_owned.has("turbo_stock"): new_owned.append("turbo_stock")
		for slot in CATALOG.SLOTS:
			if data.version==2 and slot=="turbo": continue
			var id = data.equipped.get(slot)
			if not id is String or not new_owned.has(id) or CATALOG.PARTS[id].slot!=slot: return false
			if not new_owned.has(slot+"_stock"): return false
			new_fit[slot]=id
		for key in ["race_serial","settled_serial"]:
			if not data.get(key) is float and not data.get(key) is int: return false
			if data[key]<0 or data[key]!=floor(data[key]): return false
		new_serial=int(data.race_serial)
		new_settled=int(data.settled_serial)
		if new_settled>new_serial or not data.get("best_results") is Dictionary: return false
		for event in data.best_results:
			var place = data.best_results[event]
			if not event is String or not (place is float or place is int): return false
			if place<1 or place>24 or place!=floor(place): return false
			new_results[event]=int(place)
	credits = int(data.credits)
	quest_stage = int(data.quest_stage)
	engine_level = int(data.engine_level)
	trail_tyres = data.trail_tyres
	collected = ids
	race_wins = int(data.race_wins)
	town_service = data.town_service
	paint_index = int(data.paint_index)
	owned=new_owned
	equipped=new_fit
	race_serial=new_serial
	settled_serial=new_settled
	best_results=new_results
	return true
