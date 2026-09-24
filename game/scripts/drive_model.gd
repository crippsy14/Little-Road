extends RefCounted
class_name DriveModel

# Arcade-scale forces per unit baseline mass. These are original tuning values,
# not real-world engine specifications. Gear ceiling is separate from power.
const ROLLING := .25
const DRAG := .006

static func resistance(speed: float, mass: float) -> float:
	return (ROLLING + DRAG * speed * speed) / mass

static func engine_force(tuning: Dictionary, speed: float) -> float:
	# Torque limits the launch; finite power limits force as road speed rises.
	return minf(tuning.acceleration, tuning.power / (maxf(absf(speed), 1.0) * tuning.mass))

static func pedal_for_speed(tuning: Dictionary, speed: float, target: float) -> float:
	if speed > target + .8: return -1.0
	if target < .1: return -1.0 if speed > .1 else 0.0
	return clampf((resistance(speed, tuning.mass) + (target - speed) * .8) / maxf(engine_force(tuning, speed), .01), 0, 1)

static func step(tuning: Dictionary, speed: float, throttle: float, yaw_request: float, grip: float, slope: float, delta: float) -> Dictionary:
	var tyre_budget := maxf(.1, grip * 1.65)
	var yaw_limit := tyre_budget * .95 / maxf(absf(speed), 1.0)
	var yaw := clampf(yaw_request, -yaw_limit, yaw_limit)
	var lateral := absf(yaw * speed)
	var traction := sqrt(maxf(0, tyre_budget * tyre_budget - lateral * lateral))
	var drag := resistance(speed, tuning.mass)
	var acceleration := 0.0
	if throttle > 0:
		acceleration = minf(engine_force(tuning, speed) * throttle, traction) - drag - slope
		if speed >= tuning.gear_ceiling: acceleration = minf(acceleration, 0)
	elif throttle < 0:
		acceleration = -minf(tuning.braking * -throttle, traction) - drag - slope if speed > .1 else -minf(7.0, traction)
	else:
		acceleration = -signf(speed) * drag - slope
	var next_speed := speed + acceleration * delta
	if throttle == 0 and slope == 0 and signf(next_speed) != signf(speed): next_speed = 0
	return {"speed":clampf(next_speed, -7.0, maxf(tuning.gear_ceiling, speed)), "yaw":yaw, "traction":traction, "acceleration":acceleration}
