extends RefCounted
class_name PartsCatalog

# Original tuning, based on the researched system relationships, not HG2 values.
const SLOTS := ["engine", "turbo", "tyres", "transmission", "brakes", "steering", "chassis"]
const PARTS := {
	"turbo_stock":{"slot":"turbo","name":"Naturally aspirated","price":0,"power":1.0,"torque":1.0,"hint":"No forced induction. Predictable stock power delivery."},
	"turbo_street":{"slot":"turbo","name":"Street turbo","price":700,"power":1.22,"torque":1.12,"hint":"More pulling power; tyres and gearing determine how much you can use."},
	"turbo_club":{"slot":"turbo","name":"Club turbo","price":1400,"power":1.45,"torque":1.23,"hint":"Stronger high-speed acceleration. Match with tyres and long gearing."},
	"engine_stock":{"slot":"engine","name":"Stock engine","price":0,"accel":5.6,"power_limit":24.0,"hint":"A modest, dependable starting motor."},
	"engine_workshop":{"slot":"engine","name":"Workshop tune","price":450,"accel":7.1,"power_limit":29.0,"hint":"Stronger starts and hill pulling. Gearbox still limits top speed."},
	"engine_club":{"slot":"engine","name":"Club motor","price":1000,"accel":9.4,"power_limit":36.0,"hint":"Power for longer gearing. Needs good tyres in bends."},
	"tyres_stock":{"slot":"tyres","name":"Stock tyres","price":0,"road":7.0,"dirt":3.5,"hint":"Basic road grip; poor on rough ground."},
	"tyres_touring":{"slot":"tyres","name":"Touring tyres","price":350,"road":9.0,"dirt":5.0,"hint":"An affordable improvement on road and trail."},
	"tyres_trail":{"slot":"tyres","name":"Trail tyres","price":450,"road":8.0,"dirt":10.0,"hint":"Secure on dirt; less road grip than touring tyres."},
	"tyres_sport":{"slot":"tyres","name":"Road sport tyres","price":750,"road":12.0,"dirt":3.0,"hint":"Fast tarmac corners, but weaker on loose ground."},
	"transmission_stock":{"slot":"transmission","name":"Stock gearbox","price":0,"ceiling":25.0,"pull":1.0,"hint":"Balanced short gearing; can limit a stronger motor."},
	"transmission_close":{"slot":"transmission","name":"Close-ratio gearbox","price":500,"ceiling":23.0,"pull":1.20,"hint":"Better corner exits and climbing; lower maximum speed."},
	"transmission_long":{"slot":"transmission","name":"Long-ratio gearbox","price":800,"ceiling":34.0,"pull":0.84,"hint":"Higher speed ceiling; slower acceleration and needs a strong motor."},
	"brakes_stock":{"slot":"brakes","name":"Stock pads","price":0,"braking":14.0,"hint":"Allow plenty of braking distance."},
	"brakes_sport":{"slot":"brakes","name":"Sport pads","price":300,"braking":19.0,"hint":"Later braking; tyre grip still limits deceleration."},
	"brakes_race":{"slot":"brakes","name":"Race pads","price":900,"braking":25.0,"hint":"Strong braking with matching road tyres."},
	"steering_stock":{"slot":"steering","name":"Stock rack","price":0,"response":3.0,"yaw":0.88,"hint":"Calm but slow response."},
	"steering_quick":{"slot":"steering","name":"Quick rack","price":250,"response":4.5,"yaw":1.05,"hint":"Quicker direction changes without adding tyre grip."},
	"steering_precision":{"slot":"steering","name":"Precision rack","price":850,"response":6.0,"yaw":1.18,"hint":"Fast response for technical bends. Smooth inputs help."},
	"chassis_stock":{"slot":"chassis","name":"Stock chassis","price":0,"mass":1.0,"hint":"The standard car's weight."},
	"chassis_light":{"slot":"chassis","name":"Light chassis","price":650,"mass":0.87,"hint":"Better acceleration and braking from less weight."}
}

static func stock() -> Dictionary:
	var fit := {}
	for slot in SLOTS: fit[slot]=slot+"_stock"
	return fit

static func stats(fit: Dictionary) -> Dictionary:
	var e: Dictionary=PARTS[fit.engine]
	var t: Dictionary=PARTS[fit.tyres]
	var g: Dictionary=PARTS[fit.transmission]
	var b: Dictionary=PARTS[fit.brakes]
	var s: Dictionary=PARTS[fit.steering]
	var mass: float=PARTS[fit.chassis].mass
	var turbo: Dictionary=PARTS[fit.get("turbo","turbo_stock")]
	var power: float=(DriveModel.ROLLING+DriveModel.DRAG*e.power_limit*e.power_limit)*e.power_limit*turbo.power
	# Solve the flat-road power/drag equilibrium for the garage and AI planning.
	var low:=0.0
	var high: float=g.ceiling
	for iteration in range(24):
		var candidate: float=(low+high)*.5
		if (DriveModel.ROLLING+DriveModel.DRAG*candidate*candidate)*candidate<power: low=candidate
		else: high=candidate
	return {"max_speed":(low+high)*.5,"gear_ceiling":g.ceiling,"power":power,"acceleration":e.accel*g.pull*turbo.torque/mass,
		"braking":b.braking/mass,"road_grip":t.road,"dirt_grip":t.dirt,
		"response":s.response,"yaw":s.yaw,"mass":mass}

static func summary(fit: Dictionary) -> String:
	var s:=stats(fit)
	return "Est. level-road %.0f mph · Gearing %.0f mph\nLaunch %.1f · Brake %.1f · Road / trail grip %.1f / %.1f" % [s.max_speed*2.237,s.gear_ceiling*2.237,s.acceleration,s.braking,s.road_grip,s.dirt_grip]

static func reward(place: int, race_class: String = "C") -> int:
	if place<1 or place>24: return 0
	var base:=100
	if place<=18: base=130
	if place<=12: base=170
	if place<=8: base=220
	if place==4: base=280
	if place==3: base=330
	if place==2: base=400
	if place==1: base=500
	return int(base*{"C":1.0,"B":1.8,"A":3.0}.get(race_class,1.0))
