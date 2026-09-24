# Racing line research and the reference we will measure

21 September 2026. User clarification: the adherence reference must be a racing line. The existing `centreline-v1` trace is only a geometry/consistency baseline. It is not an ideal-line score. This note supports phase B of the [existing AI implementation plan](<C:/Users/cripp/Documents/ChatGPT/Road Trip/docs/racing-ai-implementation-plan.md>); it does not replace its phases or ability design.

## What the sources establish

Scott Mansell's Driver61 explanation distinguishes braking, turn-in, apex and exit, and relates the chosen line to corner shape, the following straight and the car. A later apex can trade some entry/minimum speed for better acceleration out of a slow turn. Treat that as context-dependent rather than a rule to apex late everywhere. [Driver61: racing line](https://driver61.com/uni/racing-line/). His chicane coaching emphasises looking and preparing through successive apexes. [Driver61: sim racing line](https://driver61.com/sim-racing/sim-racing-line/).

The Technical University of Munich team's planner offers shortest-path, minimum-curvature and minimum-time objectives. It explicitly distinguishes a smoother geometric solution from one exploiting acceleration capability. Its data representation includes track boundaries, vehicle limits, path curvature and a speed profile. It is useful primary engineering guidance, not a Unity drop-in or evidence that its car model matches ours. [TUM: global race trajectory optimisation](https://github.com/TUMFTM/global_racetrajectory_optimization).

Jain and Morari define the racing-line problem as jointly choosing path and speed to minimise lap time within vehicle handling and friction constraints. Their research demonstrates data-driven line search; reading the abstract does not validate a specific optimiser or its performance for Little Road. [Computing the racing line using Bayesian optimization](https://arxiv.org/abs/2002.04794).

All sources accessed 21 September 2026. No third-party planner code or package has been imported by this research pass.

## Our definition

For a declared track, car/loadout, surface and condition, the **reference racing line** will be the fastest repeatable clean path we have validated in actual Little Road physics, accompanied by a feasible speed profile. It is a best-known reference, not a claim of a mathematically proven global optimum.

The objective is elapsed time over a complete linked section or lap. Do not optimise each corner independently or maximise minimum corner speed: sacrificing some speed/position in an earlier corner can improve the next exit. Do not substitute the shortest path, track centre, maximum radius everywhere, a fixed apex offset or a painted outside–inside–outside pattern for measured performance.

Racing-line geometry and driver execution are distinct. A high-quality reference can exist while the follower is still failing to brake/steer accurately. Conversely, following a poor reference perfectly does not establish strong driving. Retain path geometry, target speed and actual trajectory separately in reports.

## Concrete first implementation

1. **Survey and legal corridor.** Reuse Cedar's arc-distance route and boundaries. Bake signed curvature, tangent, surface and named corner regions. Reserve the actual body footprint and a tracking margin; verify all four body corners, heading through the bend and intermediate path segments. Reject branch jumps, self-crossings, folded offsets, shortcuts and invalid gate order. Start with dry, level Cedar; grade fixtures remain separate controls.
2. **Candidate paths.** Store a periodic smooth lateral-offset path against road arc distance, with adequate spacing around tight transitions. Begin with an authored corner-aware path and a bounded minimum-curvature candidate as seeds. Allow entry, apex and exit position/timing to move together across linked corners. Optimise offline, retaining the centreline as a control rather than calling it a racing solution.
3. **Candidate speed profiles.** For each actual candidate path, calculate its length and curvature; road-centre curvature is insufficient once the line moves laterally. Start with a grip-limited speed bound, run backward braking feasibility and forward acceleration feasibility, and iterate around the closed loop until the start/finish seam is consistent. Include power, drag, mass, gearing, brakes and the combined braking/turning budget. Preview must cover stopping distance, not a fixed 65 m horizon for every car.
4. **Calibrate to the game.** Our lateral correction force and torque servo mean a textbook friction-circle prediction is only an initial model. Use recorded steady turns, straight acceleration and braking fixtures to measure prediction error. Fit conservative margins to actual behaviour; never grant the AI extra grip or directly assign its velocity/position to make a prediction come true.
5. **Search and physically verify.** Rank candidate paths by predicted lap time, then drive the most promising candidates through the ordinary command interface. Compare actual flying laps and named corner entry/apex/exit plus the following straight. Modify a bounded set of line/speed parameters, retain failures, and rerun candidates that appear to improve. The first solver can use bounded coordinate search; adopting a complex optimiser is optional and should follow evidence that it is needed.
6. **Acceptance and reuse.** Use three independently launched repeats on the frozen build, and held-out starts/nearby equipment before accepting a reference. A candidate must improve repeatable actual lap/linked-sector time without more illegal departures, assisted resets, contacts or missed gates. If the current follower cannot execute a predicted improvement, fix/identify that limitation rather than declare the candidate ideal. Reuse the authoring/baking/evaluation pipeline on Bayview and future tracks.

Initially validate stock and club references. Share geometry where measurements support it, but allow capability-dependent apex/exit choices or distinct profiles when worthwhile. Later grip/weather/fuel mass and tyre state must invalidate/reselect the appropriate capability profile. Driver skill must not redefine a weak driver's slow route as the ideal reference and thereby improve their score artificially.

## What the percentage means

The primary metric is **time within a declared tolerance corridor around the validated reference racing line during eligible clear-track driving**. Start by displaying ±1 m, with sensitivity at narrower/wider tolerances; do not freeze a success threshold before seeing measured distributions and moving gameplay.

Use continuous/locally constrained projection onto the racing path, preserving ordered route progress around close adjacent sections. Record signed cross-track error, average absolute error, p95, maximum and duration of departures per corner. Weight by elapsed simulation time, not rendered frame count. Add distance-weighted adherence as a companion so stopping or slowing inside the corridor cannot inflate the result unnoticed. Report start-grid travel separately from flying laps.

Keep total lap time, time relative to the reference, braking-point error and entry/apex/exit speeds next to adherence. A driver staying on the reference while coasting everywhere is accurate but slow, not excellent. Exclude or separately classify overtaking, defending, yielding, blocked traffic and physical recovery; retain those samples and their reasons rather than silently discard inconvenient deviations. Measure how the driver returns to its preferred line once clear.

The geometric path needs a visible debug overlay, with the tolerance band, braking regions/apex markers and actual driven trace. Render road centre and reference racing line differently in the map and graphs. A constant-offset road centreline must never be labelled an ideal racing line in UI or reports.

## Versioning and current status

Cache each reference with track geometry version, body envelope, equipment/condition inputs, physical model/build hash, solver settings and validation run IDs. Store path samples and speed profile separately from driver traits. Invalidate/recheck references after geometry or meaningful vehicle-physics changes.

Research and implementation requirements are written. Cedar's physical circuit and legacy-driver baseline exist. The input-policy seam is compiled and verified in `review/unity-28` (201 editor checks; 35/35 runtime comparisons including exact legacy solo/24-car traces). See [phase-A review](../review/iteration-35-ai-input-policy.md).

Five geometric candidates, path-specific speed planning, the reference input policy and a visible debug overlay now exist. Initial club-car pilots in `review/unity-29` complete cleanly: the minimum-curvature candidate records a 66.334 s flying lap versus 68.750 s for the road-centre control using the same new policy. These are pilot results, not full acceptance: repeats, other equipment, held-out starts and candidate comparisons remain. **No reference has yet passed the full validation protocol above.**

Human comparison laps are kept as separate evidence, never automatically used as optimiser/training input. Record the same club loadout with the overlay off, allow a familiarisation lap, and compare subsequent clean laps by trajectory, braking and corner/straight speeds as well as time. Imperfect driving is useful context and does not redefine the reference.

Follow-up validation: [milestone 37](../review/iteration-37-smooth-reference-and-human-comparison.md) establishes `cedar-smooth-v2` as the current bounded solo benchmark for the compact body and stock/club equipment. Three independent launches per setup reproduce identical traces; the held-out street/centred-start case also finishes cleanly. Consolidated evidence passes 23/23 checks. Best laps improve to 91.078 s stock and 64.976 s club with ordinary shared physics. The smooth geometry removes some curvature-driven braking from the earlier seed. This is a measured benchmark, not a proven optimum or whole-phase acceptance across all future cars/tracks; resources keep the candidate label until broader coverage. The user's lap 2 is excluded, and laps 1/3/4 remain separate comparison evidence. Physical recovery and broader capability/track validation remain next AI work.
