# Equipment performance test courses and track design

21 September 2026. User-requested validation work alongside the parts browser. Extends [driver/balance testing](12-driver-skill-and-balance-testing.md) and the [reusable track workflow](22-reusable-content-pipeline.md). Checkpoint 19 implements the first dry straight/brake/U-turn phase. [Checkpoint 20](../review/iteration-33-physical-grade-fixtures.md) adds physical grade support, hill/combined courses and flat/uphill/downhill coasting controls, with 183 valid final runs across both matrices. Wet/off-road surfaces and fuel/tyre-condition combinations remain planned. Retain passing, impact and full-field tests in addition to these isolated measurements.

## Purpose and course set

Run the actual runtime car with controlled loadouts on small purpose-built courses. Establish whether upgrades have their intended effects, explain exceptions, catch regressions and guide both garage estimates and later track design. A higher part price or aggregate rating is not proof of better performance everywhere.

| Course | Main measurements | Requirements |
| --- | --- | --- |
| Long level straight | Launch, acceleration bands, sustainable top speed, braking | Standing and rolling starts; enough length/time for a speed plateau; separate brake run at a matched entry speed |
| Sharp U-turn | Braking, turning, exit traction, acceleration recovery | Known radius/width and approach; sector gates before braking, apex and exit; runoff and cut detection |
| Large hill | Power/gearing under load, climb speed loss and mass effects | Surveyed grade/height, smooth transitions, sustained climb and plateau; coast/descent checks for gravity errors |
| Combined circuit | Whether isolated advantages translate to lap time | Authored straight, braking/U-turn, climbing and linking sectors; fixed gates/length; sector timing explains the total |

Start with dry tarmac. Once the actual systems exist, use equivalent geometry with off-road and wet surface definitions to isolate surface effects. Keep geometry and surface versions independent. An authored bumpy off-road course can follow, but is not an otherwise identical surface comparison. These proving grounds are development tools, not extra campaign tracks or content-hour claims. They must run unattended and be driveable in a visible window.

## Control the driver and equipment separately

**Controlled maneuvers:** send scripted pedal/brake/steer commands through the production car and physics, fixing launch/brake-entry conditions and disabling AI variation. Do not move cars along a spline or set their finishing time/speed directly.

**Reference-driver laps:** hold driving policy, skill and track knowledge constant. Allow the policy to adapt braking and corner speed to available equipment, as a competent driver would; identical steering/braking inputs everywhere can conceal usable upgrades or intentionally crash weaker setups. Record the actual inputs and chosen limits.

After those pass, introduce seeded natural skill variation with paired seeds and driver identities across compared loadouts. Report distributions and failed attempts. Do not manufacture timing noise or let traffic obscure the equipment effect. Cross-check representative setups in two-/three-car passing and 24-car races afterward.

Use the existing [driver personality, teammate growth and track familiarity plan](20-driver-personality-and-teammate-progression.md) when implementing skills. That plan requires a dedicated specification pass; these fixtures do not establish a competing skill schema. Preserve fixed-driver equipment results separately from later fixed-car skill comparisons, and record ability, familiarity and temporary form independently. Solo measurements cannot validate overtaking behaviour, so retain the passing and full-field stages.

The user subsequently clarified the detailed engineering reference: [racing-ai-implementation-plan.md](<C:/Users/cripp/Documents/ChatGPT/Road Trip/docs/racing-ai-implementation-plan.md>). Use that plan for implementation, preserving doc 20's product intent. After the current hill/combined tests, AI work on the requested Tsukuba-influenced technical fixture is the next priority.

Run stock; every purchasable single-part swap into stock; coherent early-road/club setups; and selected interactions: engine × gearing, engine/turbo × tyres, brakes × tyres and power × mass. Keep the same body/collider. Later include fuel load/tank mass and tyre condition, restoring exact start state each run. Obtain all tuning through the authoritative catalogue.

Keep a small smoke matrix and a fuller balance/release matrix. Initially repeat controlled cases at least three times to establish physical repeatability, then set tolerances using observed noise and a meaningful gameplay difference. Start AI comparisons with 20 paired seeds and expand uncertain cases. These are proposed test sizes, not existing measured confidence guarantees.

## Measurements and reports

Each run records build, physics/catalogue/fixture versions, complete loadout, body/collider identity, surface, driver mode/seed, timestep, start state and completion status.

- Total and sector times; travelled distance and route distance separately; distance/time average speed. Weaving or cutting must not inflate the comparison.
- Peak speed and sustained top speed. Require a stable plateau for the latter; report "not reached" if the course ends first.
- 0–30 mph, 0–50 mph where reachable, rolling acceleration, braking time/distance at a matched entry speed, apex minimum speed, exit speed and acceleration recovery.
- Hill entry/crest speed, climb time, elevation gain, speed along grade sections and stalled/failed runs.
- Throttle, braking, steering, longitudinal/lateral acceleration, lateral slip velocity, requested/applied yaw, road departure, contacts, airborne intervals, missed gates and recoveries. Do not report wheelspin/slip ratio until the runtime models or measures it.
- Later: fuel used, loaded mass through the run, tyre condition and stint/range estimates.

Save raw time-series and metadata as JSON/CSV plus a compact report: speed-distance traces, sector deltas, medians/spread, expected-versus-observed verdicts and links to representative visible runs. Retain failures in the report rather than dropping them from averages. Set expected directions/conditional bounds before viewing results, and distinguish a meaningful effect from numerical noise.

| Upgrade | Expected useful effect | Valid exceptions |
| --- | --- | --- |
| Engine / turbo | Acceleration, climbing and potentially higher sustained speed | Grip and gearing can cap the improvement; consumption awaits fuel implementation |
| Short / long gearing | Short: acceleration; long: higher permitted speed | Longer ratios cannot create engine power and may lose on tight tracks |
| Surface-appropriate tyres | Braking, cornering and usable acceleration | More grip need not help a low-power straight; intended surface trade-offs should remain |
| Brakes | Stopping distance and later useful braking | Tyre grip remains the ceiling; no full-throttle straight benefit required |
| Steering | Response and tracking within tyre limits | It must not invent grip or top speed |
| Lower mass | Acceleration, braking and climbing where modeled | Flat equilibrium top speed may stay unchanged; weight is not aerodynamic drag |
| Tank / fuel load, later | Range versus loaded-mass penalties | Capacity and fuel actually carried are separate |

Diagnose failed expectations as data, fixture, driver-policy, physics or intended-trade-off issues before changing part values. Not every costly part should win every course.

## Use the evidence to design later tracks

The user wants Bayview to keep its early-game character. Later high-performance classes need more professional racing circuits: longer useful straights, sharper/technical turns, demanding braking and varied challenges appropriate to much faster cars with better turning ability. Do not obtain difficulty only by raising rival speed on beginner geometry.

Use the fixture data as a design tool, not a formula that generates every track identically:

1. Specify the intended class, representative entry/competitive/top loadouts and a distinct track identity before drawing the full route.
2. Measure their acceleration distances, sustainable speeds, braking distances, turning envelopes and hill behaviour. Select straight lengths and braking-zone space that let the intended cars express those differences.
3. Compose contrasting sectors: an acceleration/overtaking straight, heavy braking into a tighter turn, flowing faster bends, linked changes of direction and elevation where supported. Avoid making every advanced circuit a sequence of identical hairpins or long full-throttle corridors.
4. Provide readable approaches, sightlines, usable alternate lines, runoff and sufficient racing width for the 24-car field. A technical section can constrain overtaking without creating an AI queue or unavoidable pile-up.
5. Validate the track with the intended performance range, not just the quickest car: sector pace, braking margins, grip limits, completion, passing, recovery and impacts. Track-specific class events have declared opponent setups, eligibility, rewards and unlocks; do not silently scale opponents to the player's fitted parts.
6. Compare actual race behaviour and player enjoyment with the design brief. Keep curves and braking zones challenging but learnable, and revise geometry when measured cars cannot use it fairly. A passing automated lap is not track-fun acceptance.

A later circuit may therefore expose different parts decisions from Bayview: gearing for a long straight, brakes/tyres for a heavy stop, and control/traction through a technical sequence. Stronger turning parts should allow useful pace, while the car still needs room and readable inputs. Preserve variety across tracks rather than making each new one merely larger and faster.

## Original source audit and delivery order

Before checkpoint 20, body pitch/roll were frozen everywhere and fixtures had no physical grade support. `DriveModel.Forces` accepts slope, but the runtime passes zero. Checkpoint 20 intentionally retains zero explicit slope because physical gravity/contact supplies the grade component once; coasting controls verify this. Elevated routes now align the physical body through torque and sampled support. Grip still comes from road offset rather than a full authored-surface system. The following delivery order records the original plan; the dry hill/combined pass is now delivered and AI work is next.

1. Implement/version the dry level straight and U-turn, production-car input/telemetry runner and repeatable starts. Exercise stock plus engine, gearing, tyre and brake contrasts. Preserve Bayview race/save behaviour.
2. Add surveyed hill geometry and the necessary ground-plane/grade support, including coast/descent checks, physical alignment/contact and the existing invisible-ramp regression. Add the combined circuit through the same fixture definitions.
3. Produce the dry-road matrix; compare actual measurements to garage estimates and review early-upgrade effect sizes. Add natural driver variance as a separate second pass.
4. Introduce actual off-road/wet variants when those systems exist, then fuel/tyre/endurance combinations. Until then label them deferred.
5. Use the measurements for the next faster-class track brief and validate representative setups in passing/full-field tests before the user sense-checks the result.

Extract only route/event boundaries the second fixture needs. Avoid a broad framework rewrite or a separate toy vehicle simulation that never tests the real game car.

### Grade-support implementation boundary

The following post-checkpoint-19 audit identified the dependencies implemented and tested in checkpoint 20. It is retained to explain the design; statements about earlier constraints describe the pre-implementation source:

- `Circuit` already stores three-dimensional points and distances, and its continuous road-support mesh follows them. However, `Curvature` currently includes vertical changes in its angle; the reference driver's corner limit must use horizontal turning curvature separately from grade/crest geometry. Otherwise it will brake for a hill as if it were a hairpin.
- `RoadCar` freezes pitch and roll, applies drive along its body forward vector and lateral correction along its body right vector. Grade support must establish a physical ground frame and controlled chassis alignment/contact, allowing uphill/downhill drive and tyre forces without moving the body along a prescribed route. Preserve collision impulses and test crests; merely tilting the visible mesh is insufficient.
- Choose one gravity treatment. With PhysX gravity and resolved contact on a physical incline, verify natural along-slope acceleration before adding any explicit `DriveModel` slope force. Do not apply the same gravitational component twice. Uphill/downhill coast runs with matched entry speed must demonstrate the sign and magnitude of the effect, accounting for drag.
- Fixture spawning currently uses collider world-bottom height relative to zero; clearance telemetry does the same, and free-roam recovery uses a global `y < -3` threshold. Adapt these to measured local support before validating elevated or below-datum routes. Keep the existing flat Bayview vertical audit as a separate regression, with its required captures/output folder supplied.
- Add authored grade/elevation samples, start-state placement, climb entry/crest speed and sector time, clearance above local ground, airborne intervals and failure reasons to results. Retain the same dry flat tests so a new ground system cannot silently change existing acceleration, braking or launch behaviour.

Checkpoint 20 proves the smooth surveyed hill and combined course with the actual car. Banked roads, different surfaces, hill traffic and manual/controller acceptance remain open; do not generalise these isolated results to those cases. Next follow the [technical AI circuit brief](25-technical-ai-test-circuit.md) and the exact implementation plan linked above.
