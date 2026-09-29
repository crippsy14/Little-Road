# Driver skill, overtaking and equipment balance

Added 16 September 2026 from the user's solo-lap, natural-skill-variation and small-field test requirements.

19 September follow-up: [driver personality, teammate progression and track familiarity](20-driver-personality-and-teammate-progression.md) records a requested future design pass. The example ability list is illustrative, not a fixed attribute schema. Implementation is deferred while current reliability and visual work continues.

## Intended experience

Opponents have different abilities and different days. A closely matched midfield driver can finish several places higher or lower across races because of driving execution and traffic. Equipment remains a real advantage; buying a faster car must not cause the opposition to acquire stronger parts. Do not assign desired finishing positions, add noise to recorded lap times or grant invisible speed boosts to recover a predetermined order.

Natural variation affects driving: race pace, braking judgment, line placement, consistency over a lap, following distance and the opportunities a driver takes to pass. A driver still reads the road ahead and controls the same physical car as the player. Route guidance is compatible with responsive AI; an empty-track car should not swerve randomly merely to demonstrate variation.

## Preserved Godot driver model

Each opponent has a stable identity and base pace. A recorded race seed samples form within that ability: pace ±3%, braking margin 0.94–1.04, small line offset ±0.12 m, following-time preference 0.30–0.48 s and smoothly changing pace execution of roughly 0.8–2.2%. These are initial tuning values, not final balance guarantees. Race serials select different forms during normal play. Equipment does not enter the seed selection.

Passing decisions consider the closest relevant car ahead, the driver's available pace and space in the other lane. Following speed accounts for the physical gap while a pass becomes possible. The driver retains a chosen passing lane for a minimum interval, and brakes/steers inward when heading outside the road corridor. These choices are still being stress-tested.

## Repeatable test protocol

1. **Solo controls:** use a fresh physical car, the same starting position, equipment, course, physics rate and seed twice. A difference here is a harness/physics question, not evidence of natural skill. Record contacts and recoveries.
2. **Solo variation:** run seeds 11, 22 and 33 for stock, engine-only, tyre-only, balanced and developed road setups. Measure three-lap time and individual lap splits from actual ordered checkpoint crossings. The first lap includes the grid approach.
3. **Equipment comparisons:** pair the same driver seeds across configurations. Record fitted parts, total cost, driver parameters, median/range, impacts and recoveries. A clean combined road build should beat stock consistently. Individual parts can have trade-offs; investigate a nominal road upgrade that gets slower before accepting its advertised benefit.
4. **Two-car overtaking:** put the developed car behind a slower stock car in the same lane. Require a physical pass, first place, both finishers and zero recoveries for every seed.
5. **Three-car overtaking:** add an intermediate-performance car in the adjacent lane. Require the faster car to negotiate traffic, overtake both, win and allow all three to finish without recovery.
6. **Full-field comparison:** repeat stock, balanced and developed player setups against the same 23 fixed rival equipment sets using matching variation seeds. Continue the simulation until every car finishes. Record position changes for individual rival identities and traffic cost relative to solo pace.
7. **Stress cases:** alternate several bends near the start, shuffle grids and test a stopped-car obstruction. A recovered car or timeout is evidence of a failure to solve that case, even if the race eventually ends.
8. **Human sense-check:** only present the tuning as ready after clean runs, sensible equipment ordering, plausible full-field positions and demonstrated overtaking. Scripted results do not establish enjoyable handling.

A three-seed sample is a first check, not a statistical distribution. Large time spread, contact-dependent improvements, persistent jams or a stronger setup routinely losing to stock are reasons to iterate. Position variability is assessed within the field's equipment bands; a stock rear-marker should not randomly become a front-runner.

## Unity driver model and current evidence

The Unity migration has its own controller; the Godot parameters above are not evidence that those behaviours have been ported. `RoadCar` currently samples a 0.94–1.00 pace and a preferred lateral offset within ±1.5m from the race seed and car ID. A smoothly changing execution factor targets ±2.75%, with a new target every 1.5–4.5 seconds. Equipment remains independent of seed selection. Separating permanent driver ability/personality from race-day form is still required.

The September 17 steering pass uses geometric pursuit toward the road and selected passing offset, constrained by the same equipment-dependent yaw/grip calculation as manual driving. A torque servo changes heading through the Rigidbody. Predicted lateral motion prompts earlier braking near the edge; corner planning accounts for the radius of the selected offset. Passing choices are held for at least 0.8 seconds. These decisions do not move the car along a path directly.

Build 01 evidence is in `review/unity-07`: the visible seed-1720 three-lap field finished 24/24 with no recovery resets, missed gates or 1Hz centre-position samples outside the 16m road. Stock placed 23rd in 110.826s. Maximum recorded contact penetration was 9.85cm, so the collision requirement still failed. A separate seed-11 three-car single-file test verified all three expected overtakes and the club/street/stock finish order, without resets. This is an initial sample, not accepted progression balance or a proof of no clipping.

Unity reproduction uses the exported `Little Road Unity.exe` with `--trial-auto --trial-cars 24 --trial-laps 3 --trial-seed 1720 --trial-setup stock --trial-duration 240 --trial-report <absolute-report-path>`. For the controlled three-car test, use `--trial-cars 3 --trial-grid line --trial-opponents street,club --trial-seed 11`. `tools/check-unity-passing.ps1` validates the passing report; `tools/summarize-unity-field.ps1` checks completion and sampled gate order while reporting recovery, off-road and contact failures separately. `--trial-captures <absolute-folder>` saves actual camera output.

Physical reversing/rejoining after an obstruction, deliberately induced collision time costs, matched solo/equipment matrices, rival-identity position variability and multi-seed traffic acceptance remain required. The existing teleport recovery is a last-resort fallback and does not count as successful physical rejoining.

## Godot reproduction

Run the Godot console executable with the project path and these arguments:

- `--headless --fixed-fps 60 --script res://tests/balance_trials.gd -- --mode=solo`
- `--headless --fixed-fps 60 --script res://tests/balance_trials.gd -- --mode=field`
- `--headless --fixed-fps 60 --script res://tests/overtaking_trials.gd`
- `--headless --fixed-fps 60 --script res://tests/race_trial.gd -- --profile=developed --scenario=s_bend --seed=22`
- `--headless --fixed-fps 60 --script res://tests/race_trial.gd -- --profile=developed --scenario=blocked --seed=22`

Outputs are JSON under `review/`; the field and solo runners save each completed run before beginning the next. Failed or partial batches must not be described as passed. Reports include the fitted equipment and seeds needed to investigate a result.

## Expanded racecraft scenarios - 21 September

The user's reverse-speed three-car queue, braking-zone opportunity and strong-driver/weaker-car ideas are developed in [recovery and racecraft fixtures](27-recovery-and-racecraft-fixtures.md). The queue may produce separate passes, waiting or safe three-wide racing; no formation is forced. Braking tests include both feasible passes and situations that require an abort. Driver-versus-equipment cases use paired controls and technical sections, preserving each car's real straight-line power. These extend the earlier deliberately feasible passing checks with scenarios where refusing an unsafe move is the correct result. Implementation follows physical recovery and the racecraft/ability phases in the existing AI plan.

## Equipment proving grounds and faster-track design - 21 September

See [the equipment test-course specification](24-equipment-performance-test-courses.md) for controlled straight, U-turn, hill and combined-course loadout testing, later surface variants, telemetry and how the results guide higher-performance race layouts. This supplements the seeded AI, overtaking and full-field requirements above. The new fixture suite is not implemented yet.

