# Recovery and racecraft fixtures

21 September 2026. Test design supporting phases C–E of the exact [racing AI implementation plan](<C:/Users/cripp/Documents/ChatGPT/Road Trip/docs/racing-ai-implementation-plan.md>). These fixtures are development evidence, not additional campaign races or content-hour claims.

## Physical recovery first

Use Cedar's actual road, compact body, shared physics and ordered gates. Compare stock/club cars starting sideways, facing backwards, outside the road, facing a barrier, and beyond an unearned gate. Include a blocked-clearance wait longer than the old six-second teleport threshold, and a separate case with a moving car approaching a rejoin.

Recovery must stabilise, assess clearance, turn/reverse if necessary, wait for a safe opening and physically rejoin. Both ends and the swept body must be checked; future traffic occupancy matters. Record state changes, applied controls, recovery/wait/reverse duration, assisted resets, contacts, gate history and physical position changes. Never count an assisted reset as a successful physical recovery. A missed gate must be approached validly rather than forgiven by the recovery controller.

The initial blocked-wait fixture uses temporary physical barriers removed at a declared time; that proves clearance waiting and reset suppression. It does not substitute for actual moving-traffic gap acceptance. The moving-traffic fixture must record that the other car passes safely before racing resumes.

## User-suggested overtaking scenarios

Implementation checkpoints: [the first physical recovery suite](../review/iteration-38-physical-recovery.md) passes all 14 declared stock/club scenarios; [blocked recovery and anticipation](../review/iteration-39-blocked-recovery-and-preview.md) adds a permanent-trap fallback, safe backward reset placement and forty-second temporary blockage controls. These remain opt-in review builds, not general recovery acceptance. Corner/high-speed cases, varied body envelopes, dense queues and fuller moving/player acceptance remain explicit follow-ups.

Treat these as scenario families rather than choreography. Their intent is to expose judgement, not require a prescribed finishing order from every seed.

### Three-car reverse-speed queue

On the long back straight, place three cars along the same preferred racing line, with the slowest equipment ahead and the fastest behind. Use declared longitudinal gaps and identical driver competence initially. Give the straight enough distance for the measured closing rates to create real opportunities. Vary the gaps and starting speeds after the first controlled case.

Measure safe lateral commitment, clear space ahead and beside the cars, time alongside, completed sustained order changes and return to a racing line. The middle car may pass first; the rear car may wait, pass separately, or use a genuinely open third lane. Three-wide is permitted only when road width, full body envelopes and the approaching bend allow it. Do not make three-wide formation an acceptance requirement. Include a version where space is insufficient and following/aborting is the correct response.

### Closing into a sharp-corner braking zone

Start a faster closing car behind an opponent before a technical corner. First establish each setup's measured braking and cornering capability. Test a feasible inside overlap established early enough to leave the opponent room, then less favourable gaps/speeds where a late dive must be rejected or abandoned.

Judge the complete manoeuvre: approach, braking, overlap at turn-in, apex space, exit and sustained position afterwards. Do not reward a momentary position gain achieved by overshooting the corner, cutting a gate, forcing contact or pushing the other car off. Include a closing gap, an opponent braking earlier than expected and an outside-pass alternative. A clean decision to remain behind can be a pass result for an unsafe-opportunity test.

### Strong driver in weaker equipment

Once abilities and personalities are implemented, put a more powerful car with a less skilled driver ahead of a weaker car driven by a more skilled driver. Use a technical section and following straight so braking, line choice and exit execution can create opportunities; the stronger car should still exploit its real power on a clear straight. Do not create the effect by imposing an arbitrary straight-line speed penalty on the weaker driver.

Run paired seeds and counterpart cases: equal ability/different equipment, equal equipment/different ability, then the combined comparison. Choose performance differences from measured solo distributions so a skill-led pass is plausible without guaranteeing it. Separately vary aggression to distinguish attempting a move from judging/executing it well. Compare opportunity creation, safe passes/aborts, exit speed, lap time and incident rates over multiple runs.

## Evidence and progression

The [historical racing-AI research and design decision](29-racing-ai-history-and-design-decision.md) now refines the next implementation: validate off-reference corridor driving, then tactical context and shared-corner behaviour, before another full-field acceptance attempt. The [situational planning and throughput note](28-situational-racing-and-field-throughput.md) defines the fixed initial 24-car cutoff and separate performance checks.

### First traffic implementation boundary

Implementation checkpoint: [local traffic planning](../review/iteration-40-local-traffic-planning.md) now passes controlled following, open-straight passing, single-width following, paired solo controls and the initial reverse-speed queue. This establishes a development baseline only; the scenario variations, explicit braking-zone/abort families and skill comparisons below remain required.

User-reviewed full-field preview subsequently exposed severe congestion ([handoff and evidence](../review/unity-33/notes.md)). Agreed next-session priority: make the 24-car field launch, maintain racing pace and exploit feasible passing space with competent baseline drivers, then add personalities. Local fixture passes alone are insufficient; review the moving full-field race before advancing to personality work.

Add a selectable `TrafficDriverPolicy` alongside the frozen legacy policy and solo reference. Keep shared observations, ordinary inputs and the same physics. Prove following and a two-car straight pass before the reverse-speed three-car queue, then braking-zone opportunities and unsafe/abort controls. Skills and personality are a later experiment; initial drivers share competence so equipment and traffic decisions can be isolated.

Use a small bounded set of forward paths blending from the car's current position/heading toward follow/left/right offsets. Check full body envelopes, usable road width, corner curvature, braking capability and predicted opponent occupancy along the manoeuvre. Retain a chosen side long enough to execute it, but explicitly abort when its clearance disappears. A pass must remain feasible through the approaching bend and exit, not merely gain longitudinal position before braking.

Integrating this policy must distinguish ordinary following from being physically stuck. The current reference/recovery fixture can treat an obstructed front path as a recovery trigger; that must not pull a competent traffic driver into recovery every time it queues behind a slower car. Let the traffic policy handle normal following/yielding, while the independent recovery layer retains wrong-heading, off-road, missed-gate and genuine no-progress handling. Keep the existing solo and recovery cases as regression controls when changing that integration.

Record the selected path, predicted gap, follow/pass/abort reason, commitment duration, target versus actual inputs, sustained order changes and incident outcomes. Paired no-opponent runs now verify unchanged clear-track motion for stock and club. Explicit abort reasons, full trajectory traces and broader scenario coverage remain follow-ups.

Preserve full inputs and equipment, exact starts, seeds, intent/commit/abort reasons, both cars' trajectories and predicted clearance. Count a pass only after a sustained relative-order change among active racers, excluding reset jumps, pits and different lap classifications. Inspect moving replays of both successes and failures. Advance from controlled two-/three-car cases to full 24-car fields after the local decisions are credible; passing these fixtures alone does not establish enjoyable race behaviour.

### Simultaneous first-corner entry, candidates 20–21

`tools/run-corner-fixtures.ps1` now runs twelve cases: solo at road offsets -4/0/+4 m, mirrored two-car entries at -4/+4 m, and a three-wide entry at -4/0/+4 m, each with street and club equipment. All begin at 245 m route progress and 18 m/s, before the first braking zone. The observed corner exit is 449.441 m, with a real gate-2 crossing required. Cars continue under normal driving/physics for 35 seconds; the analyzer scores the initial corner separately from subsequent track sections. No event or race result claims a complete lap.

The fixed deadline formula is `ceil(slowest matching solo exit * 1.3 + 2 s)`: 16 s for street and 14 s for club. Checks require every entrant through the exit, clean physical checkpoint passages, no contacts, body departures or recovery within the section, normal process exits and untruncated diagnostics. Exact setup, build hash and results live under `review/unity-34/corners-20` and `corners-21`.

Candidate 20 passed 90/91: the three-wide club entry had two physical collisions (four per-car impact records). Its last car cleared at 12.708 s, with 37 emergency-braking samples across the field. Candidate 21 fixes an observed-motion coordinate error: lateral movement was projected on the car's right axis, measuring tyre slip, then applied as a road-relative offset. It now projects on the road's right axis, allowing prediction to see normal lane changes even without sideslip.

Candidate 21 passes **91/91**. All shared entries clear within the original deadlines without contacts, departures or recovery; the three-wide club last exit is 11.308 s with four emergency samples. This establishes only the declared 18 m/s equal-equipment corner cases. Faster entries, mixed performance, staggered rows, aborts and a moving full-field review remain required.

Optional `-TrafficDiagnostics` writes bounded per-candidate conflicts: planning timestamp, opponent ID, forecast interval and position, rejection type, speed-repair flag, and progress/cost. Track samples include the matching simulation clock and selected candidate. `tools/analyze-corner-fixtures.py` compares predictions with later observed positions, grouped by forecast horizon. The later motion can itself respond to our car, so prediction error is diagnostic and does not prove a rejected alternative would have been safe. Longer-range errors remain substantial; they support the planned distinction between anticipation and immediate safety, not deleting collision checks. Diagnostic capture is capped and disabled by default; its timings are not presentation-performance evidence.

### Hard-braking response and deliberately blind control probe

`tools/run-braking-fixtures.ps1` runs eight paired cases on Cedar's back straight: street equipment at 18 m/s and club at 26 m/s, each with a 12 m starting gap and a leader physically braking during seconds 1–2. `brake-leader` retains ordinary planning. `brake-probe` holds the follower's nominal command at straight/full throttle for the first three seconds; only the immediate safety layer can constrain its pedal, then normal planning resumes. This intentionally poor nominal command isolates that component. It is not evidence that the ordinary planner would choose to collide. Pair both with `-NoImmediateSafety` controls from the same executable, preserving normal vehicle forces and contacts.

`tools/analyze-braking-fixtures.py` requires normal process exits; candidate remaining-course completion within 45 seconds; no candidate contact, recovery, missed gate, body departure, material pose overlap or saturated collision queries; no intervention before the disturbance; and resumed positive pedal within 0.3 seconds of the probe's final intervention. Ordinary planning pace must stay within 0.5 seconds of its paired control. The unsafe probe control must actually expose a contact, or it does not demonstrate emergency avoidance. Its post-contact completion is diagnostic, not a clean-driving acceptance requirement. These are partial-course tests, not full standing-lap benchmarks.

Candidate 26's ordinary cases and established passing/corner suites pass, but both guarded probes still collide. The initial analyzer also incorrectly required the deliberately unsafe controls to complete cleanly; its original 44/49 result remains archived, and v2 corrects only that control interpretation. Candidate no-contact, completion and release limits remain unchanged. Retaining this falsified result motivates the observed-braking/stopping-time correction in 27; the new guard is not accepted solely because the 25 full field had fewer contacts. See `review/unity-34/notes.md` for the current result.
