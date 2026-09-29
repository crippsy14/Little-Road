# Anticipation, racing intent and immediate collision response

21 September 2026. Design decision following the user's moving review of traffic15. This extends [the racing AI research](29-racing-ai-history-and-design-decision.md); it describes the next implementation, not behaviour already delivered.

## Observation and code evidence

The user saw substantial improvement, but cars sometimes choose an available lane at the expense of a useful racing route, including leaving the reference with no car ahead.

Inspection of `TrafficDriverPolicy.cs` identifies plausible causes; individual visible incidents have not yet been matched to their decision traces:

- `Decide` treats any opponent within `max(100 m, speed * 4 + 25 m)` of route distance as nearby, including cars behind and laterally clear. That disables clear-track reference delegation. Delegation also requires line error under 0.7 m, so a car already displaced can remain in general lane selection even after traffic clears.
- Candidate utility rewards metres travelled along each candidate's own geometry over three seconds. A longer curved path can score more distance without making more progress around the circuit. Compare candidates on the same track-progress coordinate instead.
- The lane-switch cost is 3, whereas reference-exit preference is only 0.12 per metre. Commitment can therefore outlive its purpose. Increasing reference attraction indiscriminately would recreate the original queue; release must depend on opportunity and clearance.
- Prediction, tactical choice, speed repair and emergency fallback are mixed in one policy. This makes it difficult to tell whether braking is normal corner preparation, following, an aborted pass or an imminent collision.

These findings support the user's diagnosis but do not establish a single cause for every delay. Traffic15 still fails the full-field acceptance checks.

The user's further assessment is that the cars do not feel concerned enough with moving forward and going fast. Treat **purposeful pace** as an explicit product requirement: a competent baseline driver continually seeks fast legal progress using its available equipment, while traffic and corners constrain how it achieves that. Merely avoiding incidents is insufficient. This is not a blanket speed increase or a request to suppress necessary braking.

## What Alien: Isolation contributes

Tommy Thompson's [original account](https://www.gamedeveloper.com/design/the-perfect-organism-the-ai-of-alien-isolation) reports Andy Bray's presentation and direct answers from the lead AI programmer. It describes a director managing encounter pressure and a separate creature that acts through its senses. This is a separation of responsibilities, rather than simply two concentric detection rings. Bray's original [nucl.ai recording](https://archives.nucl.ai/recording/its-in-the-vents-the-ai-of-alien-isolation/) could not be retrieved in this review; it was not watched.

Thompson's [subsequent investigation with OpenCAGE and its author Matt Filer](https://www.gamedeveloper.com/design/revisiting-the-ai-of-alien-isolation) examines the game's behaviour data directly. It describes different sensory conditions, behaviour transitions and task-specific responses. This is useful evidence for keeping perception, intent and reaction explicit.

Our application is an engineering inference: each racing driver should have a longer-view decision process and a narrowly responsible immediate safety response. A global encounter director is not required for this change. All racers continue to use observed opponent motion and ordinary vehicle physics.

## Proposed responsibilities

### 1. Anticipation and racing intent

At the existing staggered planning frequency, consider track position, relative motion, braking distance, upcoming corners and likely available exits. Start with a provisional 3–5 second horizon, extended to cover the necessary braking approach. Range follows speed, available grip and braking capability rather than a fixed circle. Cars behind still matter for merges and side occupancy; their presence alone is not a reason to abandon a clear reference approach.

Produce explicit intents: clear-track pace, follow, prepare pass, committed pass, yield/abort, and rejoin reference. Each carries a target corridor, speed envelope, relevant opponent IDs, benefit estimate and release conditions. Retain a decision while it remains useful and feasible; release stale intent when the obstacle or benefit disappears. Safety can interrupt commitment immediately.

On clear road, approach the reference smoothly from the actual pose. A lane change must have a reason: expected progress/time benefit, clearance, corner preparation or recovery. Do not snap back onto the reference or cut across a car alongside. Reference adherence is guidance rather than a universal reward.

### 2. Feasible trajectory and pace comparison

Use the validated local controller to follow a small set of useful alternatives generated from the intent. Predict achievable acceleration, braking and corner speed for each setup. Enforce road, scenery and body clearance before ranking alternatives.

Compare **unwrapped route progress at equal elapsed time**, with a terminal estimate for the coming corner exit. Where practical also estimate time to a common downstream route station. Keep raw distance driven only as telemetry. A fast entry that forces a poor exit should not win on a short horizon. Track the prediction error against actual motion.

Returning to the reference is an evaluated transition with a safe merge corridor. Avoid forcing the reference when an alternative is demonstrably faster or required by occupied space. The existing reference is a useful baseline, not a proof of mathematical optimality.

### 3. Immediate collision response

Run a cheap bounded check at the control/physics rate using fresh observed poses and velocities. Check swept body envelopes along the intended near-term motion, including cars beside us and static obstacles. The envelope is shaped by direction, closing rate, body dimensions, latency and available braking/grip; two nearby cars travelling in parallel are not automatically an emergency.

Use time to predicted conflict and required deceleration to decide whether the normal speed envelope is sufficient, additional braking is needed, or the manoeuvre must be aborted. Full braking is reserved for an immediate threat requiring it. A stationary obstacle at high speed may require action well outside a small visual inner ring. Ordinary corner braking and safe following remain anticipation responsibilities, so the driver does not wait for an emergency to slow down.

Preserve a feasible steering path while braking. Avoid lateral escape into occupied space or off the road. A rear car closing on a driver who is maintaining its lane should not automatically cause that front driver to brake. Release an intervention promptly once the predicted conflict clears, with enough hysteresis to prevent pedal chatter.

The safety result is a constraint on the chosen trajectory/control command, not a second independent steering vector blended into it. If no collision-free action remains, report the unavoidable-risk state and apply a physically feasible response; do not teleport or promise impossible avoidance.

## Diagnostics required before tuning

Record intent, triggering car, anticipated time to conflict, required deceleration, safety intervention reason, candidate route progress, candidate travel distance, predicted exit speed, reference feasibility/rejection reason, commitment age and release reason. Distinguish planned braking from emergency braking. Provide an optional overlay of the anticipation area, swept near-term body envelope and chosen trajectory. This should explain why an apparently empty road caused a lane change.

For purposeful pace, record time lost against a matched solo sector baseline, actual versus feasible target speed, and time with acceleration available but unused. Gate the latter by grip/braking envelope, current path clearance and upcoming corners: coasting or partial throttle can be correct. Measure delay between a safely usable gap opening and acceleration resuming, and time spent following when an evaluated safe alternative offers meaningful progress. Report why speed is constrained (corner, traffic, merge, recovery, equipment, or unexplained). Do not score raw full-throttle percentage as competence. Use these traces alongside moving review; no one metric fully captures racing intent.

## Acceptance sequence

1. **Clear-road invariance:** solo reference start, displaced starts on both sides, and a harmless non-closing car behind. The nearby rear car must not materially degrade pace or cause unexplained route changes. An off-line start should converge smoothly when the merge is clear. Compare lap time and line error to matched controls; establish numerical tolerances from repeatability before tuning.
   Include a leader pulling away and a cleared obstruction: the follower should promptly use newly available acceleration, rather than retain a stale following/braking decision. Evaluate sector pace and release delay as well as collision avoidance.
2. **Score sanity:** two geometrically different paths with equal circuit progress must not reward the longer distance alone. Exercise start/finish wrap, persistent manoeuvres beyond half a lap, and corner entry versus exit trade-offs. The existing signed half-lap anchor calculation also needs correction before promotion.
3. **Anticipation without panic:** catch a slower car on a straight and before a bend; plan a viable pass or follow early. Repeat with a receding car ahead and an adjacent car at equal speed; proximity alone must not trigger hard braking.
4. **Immediate threats:** abrupt leader braking, a stopped car at several approach speeds, and a closing lateral gap. Measure reaction delay, deceleration, clearance, contacts, road bounds and clean resumption. Separate avoidable setups from deliberately unavoidable ones.
5. **Shared corners and passing completion:** equal cars alongside on either side, then mixed performance, two- and three-car tests. Hold usable space through the exit and merge only when the whole car is clear. Re-run all six fixed-corridor controls.
6. **24-car field:** retain the current start, checkpoint and first-lap deadlines, passage counts and incident review rules. Add emergency-intervention time and unnecessary lane-change counts. Do not relax deadlines to accommodate the new planner.
7. **Performance and regression:** one visible 1× process for frame-time measurement, separate subsystem timings, paired solo controls, existing recovery and traffic fixtures. Preserve the frozen traffic15 comparison build.

The first implementation slice should correct progress scoring and traffic relevance, add explicit clear-road/rejoin intent and decision traces, then establish the separate safety contract. Each slice should be measured before another full-grid visual review. Personalities remain later work.

## Implementation evidence, 21 September

Candidates 16–17 implement common-coordinate route progress and terminal route speed, rear-traffic relevance based on closing motion, explicit safe rejoining when traffic clears, and stable reference control once joined. Manoeuvre progress is accumulated across route wrap and clamped at completion, eliminating the signed half-lap restart. Telemetry now includes relevant-car count, reference rejection code, predicted route gain, arc travel and exit route speed. This is the first slice; a separate immediate-safety component and corner-exit terminal valuation remain outstanding.

Candidate 17 passed 248 editor checks and all 51 checks in ten pace fixtures (`review/unity-34/pace-17`). Genuine starts more than 3 m away from the reference on both sides regained it within 1.3–1.4 seconds, with under 0.6 s loss over the remaining course and no contacts/recovery/departures. Harmless rear traffic matched the leading car's exact motion and commands in an otherwise identical two-car reference control. These fixtures begin mid-lap and do not establish full-lap or full-field acceptance.

Fixture correction is retained in the evidence: candidate 16 initially compared one-car and two-car scenes bit-for-bit (which exposed small physical differences), and its right-side start was already near the reference. Candidate 17 compares the same two-car scene and places both rejoin starts where the reference is near the road centre. The old results remain saved rather than overwritten.

Candidate 17's established traffic matrix passed 103/105 checks. The faster rear car passed both opponents on the initial straight in the three-car test, and stock/club solo controls matched the reference exactly. The narrow corridor still had six barrier contact events and four unnecessary leader-braking samples. Its trace shows the leader rejecting its continued reference path because of a predicted rear impact. Candidate 18 extends existing rear-following responsibility to continued reference driving; early near-term and alongside/merge checks remain. This change requires runtime validation; no full racing acceptance is claimed here.

Candidate 18 runtime validation now passes all **105/105 established traffic checks**. The narrow corridor has zero contacts and zero unnecessary leader braking; the faster car passes after the corridor ends. Candidate 17's six full-lap corridor cases also remain **31/31**, with unchanged lap times from candidate 15. Pace/corridor suites were run on 17; the 18 code change concerns predicted following traffic, and those results are not represented as new 18 executions.

The 18 full-field test still fails, **7/11**: 23/24 eventually finish, only **16/24 by 117 seconds**, versus 17/24 in the watched 15 run. Contact events fall from 46 to 24, physical recoveries rise from 2 to 7, and slow-car time stays approximately unchanged (463.6 to 462.1 seconds summed across cars). All cars meet start and checkpoint 1 deadlines, but only 16 meet checkpoint 2's deadline. These are mixed results, not full-field acceptance. The 18 run is accelerated with default graphics, while the watched 15 run used direct graphics at 1x; do not use this comparison as rendering-performance evidence.

**Next concrete fixture:** simultaneous two-car and three-car entries into Cedar's first tightening corner, with mirrored sides and street/club equipment. Start before the braking zone at declared speeds, require physical body clearance and valid checkpoint passage through the exit, and compare against solo entry controls. Record actual-versus-predicted opponent positions and which car/interval rejects each candidate. Use this to distinguish prediction-induced deadlock from genuinely infeasible manoeuvres before changing margins or running another broad field-tuning loop. Continue toward the separate immediate-safety component; it has not been implemented by this slice.

That fixture is now implemented and documented in [doc27](27-recovery-and-racecraft-fixtures.md). Candidate 21 passes 91/91 corner checks and 105/105 established traffic checks after fixing opponent lateral prediction to use the road frame. However, its full field regresses to 11 timely finishes and 1115.1 slow-car seconds. Keep it experimental. The reproduced, instrumented launch shows all nine paths rejected as early as 1.6 seconds in staggered accelerating traffic. The next fixture must cover that configuration; equal-speed cars abreast at a corner are insufficient. Model observed acceleration and physically achievable movement, and distinguish speed adjustment from abandoning a route. The synchronized trace proves a prediction discrepancy, not that every rejected alternative was safe. Full evidence and the exact replay-prefix comparison are in `review/unity-34/notes.md` and `field-21-launch-diagnostics/launch-diagnosis.json`.

Candidate 22 adds acceleration estimated from observed speed history, fading over the prediction horizon, without reading opponents' controls or equipment. It retains 91/91 corner and 105/105 traffic checks. All 24 now finish the standing-grid lap within 117 seconds, with no resets/recovery and only 2.8 summed slow-car seconds. Full-field acceptance remains incomplete: five cars briefly cross the road body boundary, there are five physical car collisions, and intermediate checkpoints 4–8 miss their deadlines. The next investigation is actual driven trajectory and road-contact telemetry; preserve the forward urgency rather than solve boundary failures by indiscriminately slowing the field. Separate immediate safety and moving review remain required.

Candidate 23's instrumented replay matches all 24,438 original motion/control samples and identifies strong sideways road-support impulses before every departing car's first excursion. An isolated sweep-collision comparison removes these, but has separate angular-contact limitations. Candidate 24 instead gives this deliberately flat fixture one level support surface, retaining speculative car collision. The full field now passes 11/11 main checks, with all 24 finishing by 112.078 seconds after green and no body departures or physical recovery. Passing/following and shared-corner matrices remain 105/105 and 91/91. Six physical car collisions and late passage at checkpoints 4–8 remain. The next AI work must address credible near-term car conflicts and lost sector pace, with this physical artifact removed. Do not equate a main-check pass with complete racing or presentation acceptance.

Its separate visible 1x run reproduces every recorded motion/control sample from the accelerated run and passes the same 11 main checks. Mean frame time is 8.588 ms, p95 10.509 ms on the current RTX 4070 Ti SUPER system, with direct graphics and a 1600×900 window request. Sampled chase views show the field using multiple lateral positions. This is limited visual/performance evidence for the fixture; six contacts and checkpoint delays remain, and user acceptance of race feel is still outstanding. Detailed conditions and pending work are in `review/unity-34/notes.md`.

## Player review direction, 21 September

The candidate 25 player review confirms improved urgency but excessive braking and rigid left/centre/right path choices. The original Bayview races are the requested feel reference. Prioritize braking share and matched solo sector-loss analysis, then continuous passing trajectories shaped by available space and upcoming corners. Line adherence is contextual: a good pass legitimately leaves the reference line. Reduced contact count alone is not racing acceptance. Candidate 27's guard probes avoid contact but regress ordinary street pace; candidate 28 attempts a bounded correction and remains unvalidated. Current detailed evidence and build/network investigation are in review/unity-34/notes.md.
