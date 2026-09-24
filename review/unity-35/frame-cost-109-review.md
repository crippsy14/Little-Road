#109 swept-axis reuse and bounded performance checkpoint

Retain the small exact geometry cleanup. Each swept collision query computes each body's perpendicular axis once; the original slab arithmetic, thresholds, order and output remain unchanged. No scheduling, physics, race decisions, UI or graphics changes. This is a modest cost reduction, not a new racing-speed gain.

## Verification

511 editor checks pass.40,000 deterministic varied swept queries match frozen108 collision booleans and fraction bits exactly, including parallel, tilted, zero/vertical heading, stationary, touching and moving cases.10,000 warm queries allocate zero bytes. The Editor-only Safety108Oracle is copied from archived108 before the change.172 source/script inputs and the exported assembly are frozen.

Four sequential normal-speed visible1080p races compare108 against109 with scheduling OFF to isolate this change: same24-car mixed field, seed1709,120fps cap, vsync off, nondevelopment, HUD on. No competing editor/game or screenshot capture during measurement. All24 finish each race; all processes exit0. No runtime errors, dropped/resized frames or measured warm main-driver/worker allocations. All ten recorded-parity checks pass on both tracks, including contact events, motion, checkpoints and finishing times/ranks. Cumulative dynamic planner diagnostics also match exactly. This is fixture-scoped evidence.

| Measurement | Bayview108 | Bayview109 | Cedar108 | Cedar109 |
|---|---:|---:|---:|---:|
| Whole-AI joined wall ms/physics tick |3.11631|3.09484|2.52933|2.51300|
| Frame p95 ms |26.864|26.678|16.221|16.235|
| Frame p99 ms |38.448|38.042|33.193|32.890|
| Mean frame ms |10.875|10.868|9.536|9.525|

Whole-AI measured reductions are0.689% and0.645%. These are small single-pair observations, not statistical proof of a durable percentage improvement. Cedar p95 moves slightly worse. Retention is justified by exact equivalence and eliminating repeated work with minimal complexity; do not advertise a visibly smoother109 or a lap-time gain. Cedar meets frame targets in this pair, Bayview and per-frame AI p95 remain outside targets. Startup maxima remain2.21s/0.61s for109. Performance acceptance remains open.

##108 incident follow-up and moving feedback

The user reports the108 Cedar preview is "Sooo much smoother!" Preserve that positive moving-feedback result, independently of the numeric gates. The existing108 archive remains untouched.

The read-only inspector retains nearby10Hz speed/pedal/recovery and maximum impulse/mass samples for all six108 scheduler candidates. Around the previously flagged contacts the largest sampled impulse/mass is about0.082m/s and the winning contact surface is the road support. Bayview car23 and Cedar seed1710 car3 continue accelerating through their flagged high-relative-speed entries. These observations do not support calling the relative-speed events mechanically severe crashes. However the sample stores only the largest contact impulse since the previous sample, not an event-specific vector, and cannot establish the exact impulse for each paired callback. Preserve the original failed gates. Future contact criteria should use event impulse/body overlap/lost time rather than relative speed alone.

The Bayview seed1711 recovery is a real remaining issue: car8's first recovery sample has body extent8.445m against the8m half-road, centre offset7.245m, speed26.17m/s.153 sampled recovery states precede its46.069s finish. Inspect the approach/path tracking and recovery trigger during the next lost-time audit. Do not eliminate useful crash recovery globally or discount the field-time regression.

## Bounded decision and next action

Close this exact performance cleanup.109 is the retained source baseline;108's three-group scheduler remains a named opt-in experiment with positive moving feedback and open incident/recovery gates, not an adventure/default promotion. Keep scheduling configuration fixed when measuring the next pace change. The performance improvement is sufficient to begin the bounded research-led pace audit, while keeping frame tails as a regression gate and overall performance acceptance open. Avoid another indefinite micro-optimization loop.

Fresh primary-source research is complete in docs41. Next capture same-tick pedal-stage and candidate-choice evidence on both tracks with the three-group setup, attribute slow sectors and the isolated Bayview recovery, then make one evidenced correction. Evaluate whole-field time and car0 across three paired seeds; distinguish pace changes from altered planning cadence. No broad weakening of safety constraints. Existing10Hz command-stage instrumentation and analyzers can be reused.

Milestone review: accepted showroom/HUD/world unchanged; recorded unscheduled racing preserved; smooth scheduled preview positively reviewed by the user. Campaign, progression, exploration, endurance and production readiness remain incomplete. Physical controller/small-window acceptance and the known connected second-race recovery issue remain open. No shortcut changed; full goal stays active.
