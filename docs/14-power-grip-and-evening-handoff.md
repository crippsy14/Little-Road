# Power, grip and the next driving iteration

User decisions from the evening of 16 September 2026; recorded 17 September. This supplements the racing, expanded ownership, driver variation and collision specifications. It does not reduce the concept-image quality target.

## How the car should work

Gearing defines a theoretical road-speed ceiling. Engine power, engine torque, turbo upgrades, vehicle mass, aerodynamic resistance, hills and available traction determine the actual acceleration and attainable speed. A weak engine with long gearing may never reach its gearing ceiling. A stronger engine may reach that ceiling sooner without raising it; a longer gearbox can then make the additional power useful at higher speed.

Acceleration must taper naturally as speed increases. A throttle input asks for engine effort, not an instantaneous fraction of a speed cap. Close gearing trades maximum speed for pull; reduced mass improves acceleration. Turbo parts increase available torque and power, with future larger-turbo variants requiring a measured spool/response trade-off before those characteristics are advertised. The first implemented turbos use steady power multipliers; turbo lag is not simulated yet.

Tyres supply a shared traction budget for turning, accelerating and braking. Hard cornering leaves less traction for acceleration; too much requested steering at speed produces a wider attainable turn. Better tyres can therefore make existing engine power more usable, especially on corner exits. Steering parts change response without inventing grip. Track surface affects the tyre budget. Suspension, wheel diameter and aero remain separate planned systems; do not sell unimplemented benefits.

The garage must distinguish estimated level-road speed from the gearing ceiling. Both are estimates under stated conditions, not a guarantee of lap performance. Body shape is cosmetic in the current milestone; selecting a visually fast body must not grant hidden power.

## Current implementation boundary

The source now uses a torque-limited launch, a power-limited high-speed force, rolling resistance and quadratic drag, slope load, and a shared cornering/longitudinal tyre limit. AI uses the same car controller and applies throttle/brake to pursue its planned speed. The catalogue has seven slots, adding naturally aspirated stock induction plus Street and Club turbo purchases. Version-three saves preserve older version-one/two progression and give existing cars stock induction.

These are original arcade handling units, not a claim of real vehicle simulation. Acceleration curves, traction transitions, braking balance, hill behaviour and race difficulty require repeated driving trials. Earlier race matrices used different physics and cannot validate the revised model.

## Track and AI work carried forward

- Replace the narrow racing experience with a substantially wider authored circuit, varied corner radii, useful straights and braking zones, runoff and clear edges. Widen bridges, terrain support, gates and safety clearances together. Preserve the village's adventure character.
- Use the width for different racing lines and meaningful overtaking, rather than fixed parallel queues. Faster equipment must be able to convert clean-air pace into progress through traffic.
- Repair reliable rejoining after crashes or leaving the circuit. Getting stranded off-course is a defect. Recovery cannot skip checkpoints or spawn into another car.
- Keep fixed event loadouts and natural driver variation. No rubber-banding, forced results, timer jitter or deliberately incompetent bottlenecks.
- Give higher classes appropriately sporty silhouettes, including prototype and open-wheel bodies, while preserving cosmetic/functional independence.
- Validate car/environment contact and moderately costly crashes. Larger collision envelopes are an implementation step, not proof that all clipping is eliminated.

## Verification sequence for the next session

1. Measure actual-car straight-line acceleration (0–10/15/20 m/s, 90% attainable speed), coasting, braking, corner exit and hill climb across stock, engine, gearbox, turbo and tyre combinations. The fast model-level tests are the first check, not a replacement for physical runs.
2. Finish the circuit width and rejoining work, then run clean and deliberate-impact comparisons. Inspect actual contact images and penetration measurements for every body family.
3. Rerun matched-seed solo, two-/three-car passing, alternating-bend/blocked-course and full 24-car matrices. Equipment comparisons use the same driver seeds and start positions. Report anomalous recoveries and traffic losses.
4. Establish the intended stock-to-winning progression again from those results before requesting the user's balance sense-check.
5. Continue model/world screenshot comparisons toward all visual quality gates. Test the DualSense on real hardware and profile the visible 24-car race. Audio, more tracks, more parts and the broader body roster remain unfinished.

The user requested a couple of model/world passes and an evening stopping point. Save the evidence and outstanding work; do not treat this handoff as completion of the whole game.
